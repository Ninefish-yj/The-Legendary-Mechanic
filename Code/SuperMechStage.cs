using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质，只在单位面板显示）。
    /// 五系各有14阶段链：3入门→6职业特色→5神级→超神。
    /// 机械系14阶段为原著明确（ch237/362/477/539/684/717/770/890/957/1039）。
    /// 其他四系原著未逐一命名，按五系对应神灵五方面（神体/神通/神权/神魂）同人补全。
    /// 转职=选分支后自动晋升阶段，阶位(F→X)是能级境界，两套独立系统。
    /// </summary>
    public static class SuperMechStage
    {
        // ===== 机械系（神器）—— 原著14阶段 =====
        public static readonly string[] MechStages =
        {
            "机械爱好者", "机械师学徒", "见习机械师", "磁环", "数据", "战争",
            "虚拟", "星海", "真理", "使徒", "帝皇", "主宰", "神座", "超神机械师"
        };

        // ===== 武道系（神体）—— 同人补全14阶段 =====
        public static readonly string[] MartialStages =
        {
            "武道入门", "武道学徒", "见习武者", "肌体强化者", "格斗专家", "战术大师",
            "战场主宰", "武道宗师", "不灭战躯", "武道皇者", "武道帝者", "武道尊者", "武道神座", "超神武者"
        };

        // ===== 异能系（神通）—— 同人补全14阶段 =====
        public static readonly string[] PsiStages =
        {
            "异能入门", "异能学徒", "见习异能者", "基因链觉醒者", "异能专家", "异能大师",
            "神通觉醒", "异能宗师", "异能王者", "异能皇者", "异能帝者", "异能尊者", "异能神座", "超神异能者"
        };

        // ===== 魔法系（神权）—— 同人补全14阶段 =====
        public static readonly string[] MageStages =
        {
            "魔法入门", "魔法学徒", "见习法师", "魔网编织者", "符文专家", "魔法大师",
            "神权掌握", "魔法宗师", "魔法王者", "魔法皇者", "魔法帝者", "魔法尊者", "魔法神座", "超神法师"
        };

        // ===== 念力系（神魂）—— 同人补全14阶段 =====
        public static readonly string[] MindStages =
        {
            "念力入门", "念力学徒", "见习念者", "精神力觉醒者", "念力专家", "念力大师",
            "神魂凝练", "念力宗师", "念力王者", "念力皇者", "念力帝者", "念力尊者", "念力神座", "超神念力师"
        };

        // 每阶段转职的气力奖励（五系通用，参考机械系原著数值）
        public static readonly float[] StageQiBonus = { 0, 5, 10, 20, 50, 80, 120, 200, 300, 500, 800, 1200, 2000, 5000 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        /// <summary>获取单位所属系的阶段名数组。</summary>
        public static string[] GetStageArray(Actor a)
        {
            if (a == null) return MechStages;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return MechStages;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return MartialStages;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return PsiStages;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return MageStages;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return MindStages;
            return MechStages;
        }

        /// <summary>获取单位职业阶段（0=未入门，1-14）。</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        /// <summary>获取单位职业阶段名称（五系各有独立链）。</summary>
        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            int s = GetStage(a);
            if (s <= 0) return "未入门";
            string[] arr = GetStageArray(a);
            if (s > arr.Length) return arr[arr.Length - 1];
            return arr[s - 1];
        }

        /// <summary>设置单位职业阶段（转职时调用）。</summary>
        public static void SetStage(Actor a, int stage)
        {
            if (a == null) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, 14);
        }

        /// <summary>晋升到下一阶段，返回是否成功。</summary>
        public static bool Advance(Actor a)
        {
            int cur = GetStage(a);
            if (cur >= 14) return false;
            SetStage(a, cur + 1);
            // 转职气力奖励
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQi(a, StageQiBonus[cur]);
            }
            // 机械系磁环阶段觉醒【磁】气力属性（ch48）
            if (cur + 1 == 4 && a.hasTrait(SuperMechTraits.ClassMech))
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrMagnetic);
            }
            // 武道系肌体强化者阶段觉醒【铁】气力属性
            if (cur + 1 == 4 && a.hasTrait(SuperMechTraits.ClassMartial))
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrIron);
            }
            // 异能系基因链觉醒者阶段觉醒【精神】气力属性
            if (cur + 1 == 4 && a.hasTrait(SuperMechTraits.ClassPsi))
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrSpirit);
            }
            // 魔法系魔网编织者阶段觉醒【光】气力属性
            if (cur + 1 == 4 && a.hasTrait(SuperMechTraits.ClassMage))
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrLight);
            }
            // 念力系精神力觉醒者阶段觉醒【暗】气力属性
            if (cur + 1 == 4 && a.hasTrait(SuperMechTraits.ClassMind))
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrDark);
            }
            string[] arr = GetStageArray(a);
            Debug.Log($"[超神机械师] {a.Name} 职业晋升：{arr[cur - 1 < 0 ? 0 : cur - 1]} → {arr[cur]}");
            return true;
        }

        /// <summary>是否达到指定阶段。</summary>
        public static bool IsAtLeast(Actor a, int stage)
        {
            return GetStage(a) >= stage;
        }

        /// <summary>清除单位阶段数据（单位死亡时调用）。</summary>
        public static void Clear(Actor a)
        {
            if (a != null) _stage.Remove(a.data.id);
        }
    }
}
