using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质，只在单位面板显示）。
    ///
    /// 【原著设定】百度百科：
    /// - 机械系有明确的14阶段转职链（机械入门者→...→超神机械师）
    /// - 其他四系原著未逐一列出阶段名，按"每系同样结构"原则同人补全
    /// - 五系统一模式：入门者→学徒→见习→系特色阶段×6→使徒→帝皇→主宰→神座→超神
    /// - 转职=选分支后自动晋升阶段，阶位(F→X)是能级境界，两套独立系统
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

        // ===== 武道系（神体）—— 同人补全14阶段（御气技巧树概念）=====
        public static readonly string[] MartialStages =
        {
            "武道入门者", "武道家学徒", "见习武道家", "炼体武道家", "格斗武道家",
            "战技武道家", "气劲武道家", "宗师武道家", "王者武道家", "使徒武道家",
            "帝皇武道家", "主宰武道家", "神座武道家", "超神武道家"
        };

        // ===== 异能系（神通）—— 同人补全14阶段（基因链概念）=====
        public static readonly string[] PsiStages =
        {
            "异能入门者", "异能者学徒", "见习异能者", "基因链觉醒者", "能力操控者",
            "能力大师", "神通觉醒者", "宗师异能者", "王者异能者", "使徒异能者",
            "帝皇异能者", "主宰异能者", "神座异能者", "超神异能者"
        };

        // ===== 魔法系（神权）—— 同人补全14阶段（魔法知识树概念）=====
        public static readonly string[] MageStages =
        {
            "魔法入门者", "法师学徒", "见习法师", "魔网编织者", "符文法师",
            "魔法大师", "神权掌握者", "宗师法师", "王者法师", "使徒法师",
            "帝皇法师", "主宰法师", "神座法师", "超神法师"
        };

        // ===== 念力系（神魂）—— 同人补全14阶段（精神修炼树概念）=====
        public static readonly string[] MindStages =
        {
            "念力入门者", "念力师学徒", "见习念力师", "精神觉醒者", "念动力者",
            "念力大师", "神魂凝练者", "宗师念力师", "王者念力师", "使徒念力师",
            "帝皇念力师", "主宰念力师", "神座念力师", "超神念力师"
        };

        // 每阶段转职的气力奖励（机械系原著数值参考）
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
            if (a == null) return false;
            int cur = GetStage(a);
            if (cur >= 14) return false;
            SetStage(a, cur + 1);
            // 转职气力奖励
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQi(a, StageQiBonus[cur]);
            }
            // 第4阶段觉醒气力属性（各系不同）
            if (cur + 1 == 4)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrMagnetic); // 械力
                else if (a.hasTrait(SuperMechTraits.ClassMartial))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrIron); // 铁属性
                else if (a.hasTrait(SuperMechTraits.ClassPsi))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrSpirit); // 精神属性
                else if (a.hasTrait(SuperMechTraits.ClassMage))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrLight); // 光属性
                else if (a.hasTrait(SuperMechTraits.ClassMind))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrDark); // 暗属性
            }
            string[] arr = GetStageArray(a);
            Debug.Log($"[超神机械师] {a.Name} 职业晋升：{(cur <= 0 ? "未入门" : arr[cur - 1])} → {arr[cur]}");
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
