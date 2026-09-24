using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质，只在单位面板显示）。
    ///
    /// 【原著设定】百度百科：
    /// - 只有机械系有明确的14阶段转职链（机械入门者→...→超神机械师）
    /// - 其他四系（武道/异能/魔法/念力）没有"职业阶段"概念，只有阶位+知识树+分支
    /// - 每系职业树名不同：机械知识树/御气技巧树/基因树/魔法知识树/精神修炼树
    ///
    /// 转职=选分支后自动晋升阶段，阶位(F→X)是能级境界，两套独立系统。
    /// </summary>
    public static class SuperMechStage
    {
        // ===== 机械系（神器）—— 原著14阶段（百度百科明确列出）=====
        public static readonly string[] MechStages =
        {
            "机械入门者", "机械师学徒", "见习机械师", "磁环机械师", "数据机械师",
            "战争机械师", "虚拟机械师", "星海机械师", "真理机械师", "使徒机械师",
            "帝皇机械师", "主宰机械师", "神座机械师", "超神机械师"
        };

        // 每阶段转职的气力奖励（机械系原著数值参考）
        public static readonly float[] StageQiBonus = { 0, 5, 10, 20, 50, 80, 120, 200, 300, 500, 800, 1200, 2000, 5000 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        /// <summary>是否机械系（只有机械系有职业阶段）。</summary>
        public static bool IsMech(Actor a)
        {
            return a != null && a.hasTrait(SuperMechTraits.ClassMech);
        }

        /// <summary>获取单位职业阶段（0=未入门，1-14）。仅机械系有意义。</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (!IsMech(a)) return 0; // 非机械系无职业阶段
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        /// <summary>获取单位职业阶段名称。
        /// 机械系返回阶段名（如"磁环机械师"），其他四系返回"—"（无职业阶段）。
        /// </summary>
        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            if (!IsMech(a)) return "—"; // 其他四系无职业阶段
            int s = GetStage(a);
            if (s <= 0) return "未入门";
            if (s > MechStages.Length) return MechStages[MechStages.Length - 1];
            return MechStages[s - 1];
        }

        /// <summary>设置单位职业阶段（转职时调用）。仅机械系有效。</summary>
        public static void SetStage(Actor a, int stage)
        {
            if (a == null || !IsMech(a)) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, 14);
        }

        /// <summary>晋升到下一阶段，返回是否成功。仅机械系有效。</summary>
        public static bool Advance(Actor a)
        {
            if (!IsMech(a)) return false;
            int cur = GetStage(a);
            if (cur >= 14) return false;
            SetStage(a, cur + 1);
            // 转职气力奖励
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQi(a, StageQiBonus[cur]);
            }
            // 机械系磁环阶段觉醒【磁】气力属性=械力（ch48/ch237）
            if (cur + 1 == 4)
            {
                SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrMagnetic);
            }
            Debug.Log($"[超神机械师] {a.Name} 机械职业晋升：{MechStages[cur - 1 < 0 ? 0 : cur - 1]} → {MechStages[cur]}");
            return true;
        }

        /// <summary>是否达到指定阶段。仅机械系有意义。</summary>
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
