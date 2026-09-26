using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质，只在单位面板显示）。
    ///
    /// 【原著设定】
    /// - 每个分支有独立的职业阶段名（ch175：枪炮师学徒/见习枪炮师）
    /// - 机械师（虚拟系）原著完整14阶段：磁环→数据→战争→虚拟→星海→真理→使徒→帝皇→主宰→神座→超神
    /// - 枪炮师高级职业叫【战争堡垒】（ch50），械武者可解锁【机甲操控师】（ch50）
    /// - 中阶4-7各分支有自己的特色名（避免直接用虚拟系机械师的"磁环/数据/战争/虚拟"）
    /// - 高阶8-14通用（星海/真理/使徒/帝皇/主宰/神座/超神是宇宙级地位，所有系通用）
    /// - 其他四系原著未逐一列出，按"每系同样结构"原则同人补全
    /// </summary>
    public static class SuperMechStage
    {
        // ===== 每个分支的完整14阶段名 =====
        // 机械系（原著ch50三分支：枪炮师/械武者/机械师）
        // 原著逻辑：各分支按自身能力命名阶段，不套用机械师的"磁环/数据/战争/虚拟"技术前缀。
        // 机械师分支：原著完整14阶段（韩萧路线）。
        // 枪炮师分支：按火力/射击能力命名，高阶用原著【战争堡垒】（ch50）。
        // 械武者分支：按近战/殖装能力命名，高阶用原著【机甲操控师】（ch50）。
        // 后7阶（星海→超神）为宇宙通用境界名，各系共用。
        private static readonly string[] Stage_Gunner =
        {
            "机械系入门者", "枪炮师学徒", "见习枪炮师",
            "精准枪炮师", "火力枪炮师", "重炮枪炮师", "战争堡垒",
            "星海枪炮师", "真理枪炮师", "使徒枪炮师", "帝皇枪炮师", "主宰枪炮师", "神座枪炮师", "超神枪炮师"
        };
        private static readonly string[] Stage_Mech =
        {
            "机械入门者", "机械师学徒", "见习机械师",
            "磁环机械师", "数据机械师", "战争机械师", "虚拟机械师",
            "星海机械师", "真理机械师", "使徒机械师", "帝皇机械师", "主宰机械师", "神座机械师", "超神机械师"
        };
        private static readonly string[] Stage_MechMartial =
        {
            "机械系入门者", "械武者学徒", "见习械武者",
            "殖装械武者", "格斗械武者", "战技械武者", "机甲操控师",
            "星海械武者", "真理械武者", "使徒械武者", "帝皇械武者", "主宰械武者", "神座械武者", "超神械武者"
        };

        // 武道系（同人三分支：体魄/战术/超能）
        private static readonly string[] Stage_MartialBody =
        {
            "武道系入门者", "体魄学徒", "见习体魄",
            "炼体武道家", "钢筋武道家", "铁骨武道家", "不灭武道家",
            "星海武道家", "真理武道家", "使徒武道家", "帝皇武道家", "主宰武道家", "神座武道家", "超神武道家"
        };
        private static readonly string[] Stage_MartialTactic =
        {
            "武道系入门者", "战术学徒", "见习战术",
            "格斗武道家", "战技武道家", "兵法武道家", "谋略武道家",
            "星海武道家", "真理武道家", "使徒武道家", "帝皇武道家", "主宰武道家", "神座武道家", "超神武道家"
        };
        private static readonly string[] Stage_MartialPower =
        {
            "武道系入门者", "超能学徒", "见习超能",
            "气劲武道家", "离体武道家", "闪气武道家", "暴气武道家",
            "星海武道家", "真理武道家", "使徒武道家", "帝皇武道家", "主宰武道家", "神座武道家", "超神武道家"
        };

        // 异能系（同人三分支：攻效/循环/功能，对应基因链三维度）
        private static readonly string[] Stage_PsiAttack =
        {
            "异能系入门者", "攻效学徒", "见习攻效",
            "能级异能者", "强化异能者", "爆发异能者", "极限异能者",
            "星海异能者", "真理异能者", "使徒异能者", "帝皇异能者", "主宰异能者", "神座异能者", "超神异能者"
        };
        private static readonly string[] Stage_PsiCycle =
        {
            "异能系入门者", "循环学徒", "见习循环",
            "续航异能者", "持久异能者", "恢复异能者", "永动异能者",
            "星海异能者", "真理异能者", "使徒异能者", "帝皇异能者", "主宰异能者", "神座异能者", "超神异能者"
        };
        private static readonly string[] Stage_PsiFunc =
        {
            "异能系入门者", "功能学徒", "见习功能",
            "操控异能者", "精细异能者", "范围异能者", "领域异能者",
            "星海异能者", "真理异能者", "使徒异能者", "帝皇异能者", "主宰异能者", "神座异能者", "超神异能者"
        };

        // 魔法系（原著两类：专精法师/魔网法师）
        private static readonly string[] Stage_MageSpecialist =
        {
            "魔法系入门者", "专精学徒", "见习专精",
            "元素法师", "符文法师", "魔导法师", "奥术法师",
            "星海法师", "真理法师", "使徒法师", "帝皇法师", "主宰法师", "神座法师", "超神法师"
        };
        private static readonly string[] Stage_MageWeave =
        {
            "魔法系入门者", "魔网学徒", "见习魔网",
            "编织法师", "链接法师", "共鸣法师", "位面法师",
            "星海法师", "真理法师", "使徒法师", "帝皇法师", "主宰法师", "神座法师", "超神法师"
        };

        // 念力系（同人三分支：灵魂/法则/现实）
        private static readonly string[] Stage_MindSoul =
        {
            "念力系入门者", "灵魂学徒", "见习灵魂",
            "精神念力师", "魂火念力师", "夺舍念力师", "亡灵念力师",
            "星海念力师", "真理念力师", "使徒念力师", "帝皇念力师", "主宰念力师", "神座念力师", "超神念力师"
        };
        private static readonly string[] Stage_MindLaw =
        {
            "念力系入门者", "法则学徒", "见习法则",
            "感知念力师", "扭曲念力师", "干涉念力师", "掌控念力师",
            "星海念力师", "真理念力师", "使徒念力师", "帝皇念力师", "主宰念力师", "神座念力师", "超神念力师"
        };
        private static readonly string[] Stage_MindReality =
        {
            "念力系入门者", "现实学徒", "见习现实",
            "具现念力师", "造物念力师", "改写念力师", "创世纪念力师",
            "星海念力师", "真理念力师", "使徒念力师", "帝皇念力师", "主宰念力师", "神座念力师", "超神念力师"
        };

        // 未选分支的通用阶段名（按系）
        private static readonly string[] Stage_GenericMech =
        {
            "机械系入门者", "机械学徒", "见习机械师",
            "磁环机械师", "数据机械师", "战争机械师", "虚拟机械师",
            "星海机械师", "真理机械师", "使徒机械师", "帝皇机械师", "主宰机械师", "神座机械师", "超神机械师"
        };
        private static readonly string[] Stage_GenericMartial =
        {
            "武道系入门者", "武道学徒", "见习武道家",
            "炼体武道家", "格斗武道家", "战技武道家", "气劲武道家",
            "星海武道家", "真理武道家", "使徒武道家", "帝皇武道家", "主宰武道家", "神座武道家", "超神武道家"
        };
        private static readonly string[] Stage_GenericPsi =
        {
            "异能系入门者", "异能学徒", "见习异能者",
            "基因链觉醒者", "能力操控者", "能力大师", "神通觉醒者",
            "星海异能者", "真理异能者", "使徒异能者", "帝皇异能者", "主宰异能者", "神座异能者", "超神异能者"
        };
        private static readonly string[] Stage_GenericMage =
        {
            "魔法系入门者", "法师学徒", "见习法师",
            "魔网编织者", "符文法师", "魔法大师", "神权掌握者",
            "星海法师", "真理法师", "使徒法师", "帝皇法师", "主宰法师", "神座法师", "超神法师"
        };
        private static readonly string[] Stage_GenericMind =
        {
            "念力系入门者", "念力学徒", "见习念力师",
            "精神觉醒者", "念动力者", "念力大师", "神魂凝练者",
            "星海念力师", "真理念力师", "使徒念力师", "帝皇念力师", "主宰念力师", "神座念力师", "超神念力师"
        };

        // 每阶段转职的气力奖励（原著原文确认）
        public static readonly float[] StageQiBonus = { 10, 30, 50, 70, 100, 120, 150, 180, 210, 240, 300, 360, 450, 700 };

        // 每阶段每次升级的气力奖励（原著原文确认）
        public static readonly float[] LevelQiBonus = { 10, 20, 50, 80, 120, 120, 150, 180, 200, 220, 300, 360, 450, 700 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        /// <summary>获取单位所属分支的阶段名数组。
        /// 原著逻辑：只有机械系有明确的14阶段分支职业链（ch50：枪炮师/械武者/机械师）；
        /// 武道/异能/魔法/念力四系原著未列出明确阶段链，用统一阶段名。</summary>
        private static string[] GetStageArray(Actor a)
        {
            if (a == null) return Stage_GenericMech;
            // 只有机械系有明确的分支职业阶段链（ch50）
            if (a.hasTrait(SuperMechBranch.BranchGunner)) return Stage_Gunner;
            if (a.hasTrait(SuperMechBranch.BranchMech)) return Stage_Mech;
            if (a.hasTrait(SuperMechBranch.BranchMartial)) return Stage_MechMartial;
            // 其他四系原著未列出明确阶段链，用统一阶段名
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return Stage_GenericMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return Stage_GenericPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return Stage_GenericMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return Stage_GenericMind;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return Stage_GenericMech;
            return Stage_GenericMech;
        }

        /// <summary>获取单位职业阶段（0=未入门，1-14）。</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        /// <summary>获取单位职业阶段名称。
        /// 原著逻辑：只有机械系有明确的14阶段分支职业链（ch50）；
        /// 武道/异能/魔法/念力四系原著未列出明确阶段链，用"阶位+职业名"格式（如"A级武道家""超A级魔法师"）。</summary>
        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            int s = GetStage(a);
            if (s <= 0) return "未入门";
            // 只有机械系有14阶段职业链
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                string[] arr = GetStageArray(a);
                int idx = Mathf.Clamp(s - 1, 0, arr.Length - 1);
                return arr[idx];
            }
            // 其他四系：阶位+职业名（原著逻辑）
            string rank = SuperMechRanks.GetRankName(a);
            string className = GetGenericClassName(a);
            return $"{rank}{className}";
        }

        /// <summary>获取其他四系的通用职业名（原著称呼）。</summary>
        private static string GetGenericClassName(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "武道家";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "异能者";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "魔法师";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "念力师";
            return "超能者";
        }

        /// <summary>设置单位职业阶段（转职时调用）。</summary>
        public static void SetStage(Actor a, int stage)
        {
            if (a == null) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, 14);
        }

        /// <summary>晋升到下一阶段，返回是否成功。仅降临者（有面板）可转职。</summary>
        public static bool Advance(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false;
            int cur = GetStage(a);
            if (cur >= 14) return false;
            string oldName = cur <= 0 ? "未入门" : GetStageName(a);
            SetStage(a, cur + 1);
            string newName = GetStageName(a);
            // 转职气力奖励（原著：转职后气力上限提升，当前值补满）
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQiMax(a, StageQiBonus[cur]);
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
            }
            // 第4阶段觉醒气力属性（各系不同）
            if (cur + 1 == 4)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrMagnetic);
                else if (a.hasTrait(SuperMechTraits.ClassMartial))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrIron);
                else if (a.hasTrait(SuperMechTraits.ClassPsi))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrSpirit);
                else if (a.hasTrait(SuperMechTraits.ClassMage))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrLight);
                else if (a.hasTrait(SuperMechTraits.ClassMind))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrDark);
            }
            Debug.Log($"[超神机械师] {a.name} 职业晋升：{oldName} → {newName}");
            return true;
        }

        /// <summary>是否达到指定阶段。</summary>
        public static bool IsAtLeast(Actor a, int stage)
        {
            return GetStage(a) >= stage;
        }

        /// <summary>清除单位阶段数据（单位死亡时调用）。</summary>
        public static void Clear() { _stage.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_stage, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null) _stage.Remove(a.data.id);
        }
    }
}
