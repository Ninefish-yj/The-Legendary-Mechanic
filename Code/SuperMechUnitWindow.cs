using System;
using System.Collections.Generic;
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
                if (!SuperMechConfig.ShowRankInPanel) return;
                Actor actor = GetActor(__instance);
                if (actor == null || !actor.isAlive()) return;

                // 原著：踏入超能=获得天赋倾向，职业方向后天选择。无天赋=普通人
                bool hasTalent = SuperMechTalent.HasTalent(actor);
                if (!hasTalent)
                {
                    ShowRow(__instance, "阶位", "凡人（未踏入超能之路）");
                    ShowRow(__instance, "提示", "在知识Tab点击「激发潜能」踏入超能");
                    return;
                }

                // === 主面板只留核心信息（详细信息移到知识Tab）===

                // 行1：阶位
                string rank = GetRank(actor);
                ShowRow(__instance, "阶位", rank);

                // 行2：体系（选定方向才显示，否则显示野生超能者）
                bool hasProfession = SuperMechProfession.HasProfession(actor);
                if (hasProfession)
                {
                    string cls = SuperMechProfession.GetClass(actor);
                    string clsAspect = GetClassAspect(cls);
                    ShowRow(__instance, "体系", cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）"));
                }
                else
                {
                    ShowRow(__instance, "体系", "野生超能者（未选定方向）");
                }

                // 行3：能级（原著：能级是概念，欧纳是单位）
                float onar = SuperMechAdvancement.CalcOnar(actor);
                ShowRow(__instance, "能级", $"{onar:F0}欧纳");

                // 行4：气力消耗条（原著ch3：气力有当前值/上限，消耗空→耗体力→减生命）
                // 各系表现形式不同：机械磁环后=械力，魔法=魔力，念力=精神力，武道/异能=气力
                float qi = SuperMechQi.GetQi(actor);
                float qiMax = SuperMechQi.GetQiMax(actor);
                int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : "未入流";
                string qiName = GetQiDisplayName(actor);
                string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
                ShowRow(__instance, qiName, $"{qiBar}（{qiLvText}）");

                // 行5：降临者标识
                if (SuperMechAwakened.IsAwakened(actor))
                {
                    ShowRow(__instance, "身份", "降临者");
                }

                // === 自定义属性（注册为BaseStatAsset，参与计算但原版图标栏不显示，这里手动插入）===
                ShowCustomStats(__instance, actor);

                // 提示：详细信息在知识Tab
                ShowRow(__instance, "提示", "详细修炼状态/天赋/职业树/技能在知识Tab查看");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 单位面板注入失败: {e.Message}");
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

        /// <summary>原著：五系对应神灵五方面——武道=神体/念力=神魂/魔法=神权/异能=神通/机械=神器。</summary>
        public static string GetClassAspect(string cls)
        {
            switch (cls)
            {
                case "武道系": return "神体";
                case "念力系": return "神魂";
                case "魔法系": return "神权";
                case "异能系": return "神通";
                case "机械系": return "神器";
                default: return "";
            }
        }

        private static string GetRank(Actor a)
        {
            // 从精确阶位字典读取（含+位，+位不挂特质只在面板显示）
            return SuperMechRanks.GetRankName(a);
        }

        /// <summary>
        /// 获取气力的显示名称（各系表现形式不同，原著ch3/ch50/ch237）。
        /// 机械系磁环后=械力，魔法系=魔力，念力系=精神力，武道/异能=气力。
        /// </summary>
        private static string GetQiDisplayName(Actor a)
        {
            if (!SuperMechProfession.HasProfession(a)) return "气力";
            string cls = SuperMechProfession.GetClass(a);
            switch (cls)
            {
                case "机械系":
                    // ch237：磁环阶段后气力改称械力（阶段索引3=磁环）
                    int stage = SuperMechStage.GetStage(a);
                    return stage >= 3 ? "械力" : "气力";
                case "魔法系": return "魔力";
                case "念力系": return "精神力";
                default: return "气力";
            }
        }

        /// <summary>获取知识树名（百度百科：每系职业树名各不相同）。</summary>
        public static string GetKnowledgeTreeName(string cls)
        {
            switch (cls)
            {
                case "武道系": return "御气技巧树";
                case "异能系": return "基因树";
                case "魔法系": return "魔法知识树";
                case "念力系": return "精神修炼树";
                default: return "机械知识树";
            }
        }

        private static void ShowRow(UnitWindow window, string label, object value)
        {
            try
            {
                if (_showStatRow == null)
                {
                    // 优先匹配参数最多的重载（含pLocalize参数），避免中文标签被本地化查找
                    var methods = typeof(UnitWindow).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo best = null;
                    foreach (var m in methods)
                    {
                        if (m.Name == "showStatRow" && m.GetParameters().Length >= 2)
                        {
                            if (best == null || m.GetParameters().Length > best.GetParameters().Length)
                                best = m;
                        }
                    }
                    _showStatRow = best;
                    if (_showStatRow == null)
                    {
                        Debug.LogWarning("[超神机械师] 未找到UnitWindow.showStatRow方法，单位面板注入将跳过");
                        return;
                    }
                }

                // 动态构建参数数组：前两个参数是label和value，其余用默认值填充
                ParameterInfo[] parms = _showStatRow.GetParameters();
                object[] args = new object[parms.Length];
                args[0] = label;
                args[1] = value;
                for (int i = 2; i < parms.Length; i++)
                {
                    Type pt = parms[i].ParameterType;
                    string pname = parms[i].Name;
                    if (pname == "pLocalize") { args[i] = false; continue; }  // 中文标签不本地化
                    if (pt == typeof(string)) args[i] = null;
                    else if (pt == typeof(bool)) args[i] = false;
                    else if (pt == typeof(long)) args[i] = -1L;
                    else if (pt == typeof(MetaType)) args[i] = MetaType.None;
                    else if (pt.IsEnum) args[i] = System.Enum.ToObject(pt, 0);
                    else args[i] = null;
                }
                _showStatRow.Invoke(window, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
            }
        }

        /// <summary>
        /// 显示自定义属性（注册为BaseStatAsset但原版图标栏不自动显示，这里手动插入）。
        /// 只显示非零值，避免面板臃肿。
        /// </summary>
        private static void ShowCustomStats(UnitWindow window, Actor a)
        {
            try
            {
                // 潜能点（所有超能者都有，初始5点）
                float pp = a.stats[SuperMechCustomStats.StatPotentialPoints];
                if (pp > 0) ShowRow(window, "潜能点", pp.ToString("F0"));

                // 神性蜕变层数（职业+种族各10层，ch1039）
                float div = a.stats[SuperMechCustomStats.StatDivinityLayers];
                if (div > 0) ShowRow(window, "神性蜕变", div.ToString("F0") + "层");

                // 圣所权限（6个圣所独立权限，ch1266：碎片=权限）
                int sanctuaryTotal = 0;
                string[] sanctuaryStats = {
                    SuperMechCustomStats.StatSanctuary1,
                    SuperMechCustomStats.StatSanctuary2,
                    SuperMechCustomStats.StatSanctuary3,
                    SuperMechCustomStats.StatSanctuary4,
                    SuperMechCustomStats.StatSanctuary5,
                    SuperMechCustomStats.StatSanctuary6
                };
                foreach (var s in sanctuaryStats) sanctuaryTotal += (int)a.stats[s];
                if (sanctuaryTotal > 0) ShowRow(window, "圣所权限", sanctuaryTotal + "碎片");

                // 注：魔力/精神力不单独显示——它们就是气力在魔法系/念力系的表现形式，
                // 已在主面板"气力/魔力/精神力"消耗条中显示（原著ch3/ch50：气力是五系统一基础）

                // 械感（机械亲和度，ch50：气力属性【磁】增加机械亲和度）
                float mechAff = a.stats[SuperMechCustomStats.StatMechAffinity];
                if (mechAff > 0) ShowRow(window, "械感", mechAff.ToString("F0") + "%");

                // 魔感（魔法亲和度）
                float mageAff = a.stats[SuperMechCustomStats.StatMageAffinity];
                if (mageAff > 0) ShowRow(window, "魔感", mageAff.ToString("F0") + "%");

                // 原著7属性补充：神秘/魅力/幸运（ch3）
                float mystery = a.stats[SuperMechCustomStats.StatMystery];
                if (mystery > 0) ShowRow(window, "神秘", mystery.ToString("F0"));
                float charm = a.stats[SuperMechCustomStats.StatCharm];
                if (charm > 0) ShowRow(window, "魅力", charm.ToString("F0"));
                float luck = a.stats[SuperMechCustomStats.StatLuck];
                if (luck > 0) ShowRow(window, "幸运", luck.ToString("F0"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 自定义属性显示失败: " + e.Message);
            }
        }
    }
}
