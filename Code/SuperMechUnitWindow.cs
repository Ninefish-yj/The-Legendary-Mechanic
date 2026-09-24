using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位面板注入：在原版属性行之后追加超神机械师自定义数据行。
    /// 参考登神长阶 DivineAscension 的 UnitWindowIntegration 方案：
    /// Harmony Postfix 挂 UnitWindow.showStatsRows，反射调 showStatRow。
    /// 只显示已觉醒五系的单位，凡人不显示。
    /// </summary>
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindow
    {
        private static MethodInfo _showStatRow;

        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                Actor actor = GetActor(__instance);
                if (actor == null || !actor.isAlive()) return;

                // 只显示已觉醒五系的单位
                string cls = GetClass(actor);
                if (cls == null) return;

                // 行1：阶位
                string rank = GetRank(actor);
                ShowRow(__instance, "阶位", rank);

                // 行2：职业系
                ShowRow(__instance, "职业", cls);

                // 行3：职业阶段（机械系显示阶段名，其他系显示核心能量等级）
                string stage = GetStage(actor, cls);
                if (!string.IsNullOrEmpty(stage))
                    ShowRow(__instance, "阶段", stage);

                // 行4：气力/械力（原著面板格式：128,452【Lv19】）
                float qi = SuperMechQi.GetQi(actor);
                int qiLv = SuperMechQi.GetLevel(qi);
                string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : "未入流";
                string qiLabel = cls == "机械系" ? "械力" : "气力";
                string qiValue = qi >= 1000 ? $"{qi:N0}" : $"{qi:F0}";
                ShowRow(__instance, qiLabel, $"{qiValue}【{qiLvText}】");

                // 行5：欧纳（能级）
                float onar = SuperMechAdvancement.CalcOnar(actor);
                ShowRow(__instance, "欧纳", $"{onar:F0}");

                // 行6：潜能点/觉醒点（原著 ch3/ch50/ch1201）
                int pot = SuperMechPotential.GetPotential(actor);
                int awk = SuperMechPotential.GetAwakening(actor);
                int unlocked = SuperMechPotential.GetUnlockedCount(actor);
                string potText = awk > 0 ? $"{pot}（觉醒点{awk}）" : $"{pot}";
                ShowRow(__instance, "潜能点", $"{potText} | 知识{unlocked}个");

                // 行7：圣所/神性蜕变（原著 ch1039/ch1362：六圣所=五系+信息态）
                bool divinity = actor.hasTrait("sm_divinity_ascended");
                float onarForDiv = SuperMechAdvancement.CalcOnar(actor);
                int qiLvForDiv = SuperMechQi.GetLevel(SuperMechQi.GetQi(actor));
                string divText = divinity ? "已蜕变" :
                    $"未蜕变（需78000欧纳+Lv21气力，当前{onarForDiv:F0}/Lv{qiLvForDiv}）";
                ShowRow(__instance, "神性蜕变", divText);

                // 行8：圣所解锁进度（跨存档全局数据）
                int unlocked = 0;
                for (int i = 0; i < 6; i++)
                    if ((SuperMechSanctuary.Data.unlocked_sanctuaries & (1 << i)) != 0) unlocked++;
                string sanText = $"{unlocked}/6 已解锁 | 碎片[";
                for (int i = 0; i < 6; i++)
                {
                    sanText += SuperMechSanctuary.Data.sanctuary_fragments[i];
                    if (i < 5) sanText += "/";
                }
                sanText += "]";
                ShowRow(__instance, "圣所", sanText);

                // 行9：分支（五系各三分支，机械原著ch50，其他参考同人二创）
                string branch = SuperMechBranch.GetBranchName(actor);
                if (branch != "未选择")
                    ShowRow(__instance, "分支", branch);

                // 行10：副职业等级（原著：特工lv9/黑夜潜行者lv10）
                string subText = SuperMechSubClass.GetSubLevelText(actor);
                if (!string.IsNullOrEmpty(subText))
                    ShowRow(__instance, "副职业", subText);

                // 行11：装备宝物（9级品质）
                string relic = SuperMechRelic.GetCurrentRelicName(actor);
                if (relic != "无")
                    ShowRow(__instance, "宝物", relic);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 单位面板注入异常: " + e.Message);
            }
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                var prop = typeof(UnitWindow).GetProperty("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.GetValue(window) as Actor;
                var field = typeof(UnitWindow).GetField("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        private static string GetClass(Actor a)
        {
            if (HasTrait(a, SuperMechTraits.ClassMech)) return "机械系";
            if (HasTrait(a, SuperMechTraits.ClassMartial)) return "武道系";
            if (HasTrait(a, SuperMechTraits.ClassPsi)) return "异能系";
            if (HasTrait(a, SuperMechTraits.ClassMage)) return "魔法系";
            if (HasTrait(a, SuperMechTraits.ClassMind)) return "念力系";
            return null;
        }

        private static string GetRank(Actor a)
        {
            // 从高到低找已激活的阶位特质
            for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
            {
                if (HasTrait(a, SuperMechRanks.All[i].id))
                    return SuperMechRanks.All[i].name;
            }
            return "凡人（F）";
        }

        private static string GetStage(Actor a, string cls)
        {
            return SuperMechStage.GetStageName(a);
        }

        private static bool HasTrait(Actor a, string traitId)
        {
            if (a == null || a.traits == null) return false;
            foreach (var t in a.traits)
                if (t.id == traitId) return true;
            return false;
        }

        private static void ShowRow(UnitWindow window, string label, object value)
        {
            try
            {
                if (_showStatRow == null)
                {
                    _showStatRow = typeof(UnitWindow).GetMethod("showStatRow",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new Type[] { typeof(string), typeof(object), typeof(string), typeof(MetaType), typeof(long), typeof(bool), typeof(string), typeof(string), typeof(TooltipDataGetter), typeof(bool) },
                        null);
                }
                if (_showStatRow == null) return;
                _showStatRow.Invoke(window, new object[] { label, value, null, MetaType.None, -1L, false, null, null, null, false });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow反射失败: " + e.Message);
            }
        }
    }
}
