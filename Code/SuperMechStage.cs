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
    /// - 中阶4-7各分支有自己的特色名（避免直接用虚拟系机械师的"sm_stage_000"）
    /// - 高阶8-14通用（星海/真理/使徒/帝皇/主宰/神座/超神是宇宙级地位，所有系通用）
    /// - 其他四系原著未逐一列出，按"sm_stage_001"原则同人补全
    /// </summary>
    public static class SuperMechStage
    {
        // ===== 每个分支的完整14阶段名 =====
        // 机械系（原著ch50三分支：枪炮师/械武者/机械师）
        // 原著逻辑：各分支按自身能力命名阶段，不套用机械师的"sm_stage_000"技术前缀。
        // 机械师分支：原著完整14阶段（韩萧路线）。
        // 枪炮师分支：按火力/射击能力命名，高阶用原著【战争堡垒】（ch50）。
        // 械武者分支：按近战/殖装能力命名，高阶用原著【机甲操控师】（ch50）。
        // 后7阶（星海→超神）为宇宙通用境界名，各系共用。
        private static readonly string[] Stage_Gunner =
        {
            "sm_stage_002", "sm_stage_003", "sm_stage_004",
            "sm_stage_005", "sm_stage_006", "sm_stage_007", "sm_stage_008",
            "sm_stage_009", "sm_stage_010", "sm_stage_011", "sm_stage_012", "sm_stage_013", "sm_stage_014", "sm_stage_015"
        };
        private static readonly string[] Stage_Mech =
        {
            "sm_stage_016", "sm_stage_017", "sm_stage_018",
            "sm_stage_019", "sm_stage_020", "sm_stage_021", "sm_stage_022",
            "sm_stage_023", "sm_stage_024", "sm_stage_025", "sm_stage_026", "sm_stage_027", "sm_stage_028", "sm_stage_029"
        };
        private static readonly string[] Stage_MechMartial =
        {
            "sm_stage_002", "sm_stage_030", "sm_stage_031",
            "sm_stage_032", "sm_stage_033", "sm_stage_034", "sm_stage_035",
            "sm_stage_036", "sm_stage_037", "sm_stage_038", "sm_stage_039", "sm_stage_040", "sm_stage_041", "sm_stage_042"
        };

        // 武道系（同人三分支：体魄/战术/超能）
        private static readonly string[] Stage_MartialBody =
        {
            "sm_stage_043", "sm_stage_044", "sm_stage_045",
            "sm_stage_046", "sm_stage_047", "sm_stage_048", "sm_stage_049",
            "sm_stage_050", "sm_stage_051", "sm_stage_052", "sm_stage_053", "sm_stage_054", "sm_stage_055", "sm_stage_056"
        };
        private static readonly string[] Stage_MartialTactic =
        {
            "sm_stage_043", "sm_stage_057", "sm_stage_058",
            "sm_stage_059", "sm_stage_060", "sm_stage_061", "sm_stage_062",
            "sm_stage_050", "sm_stage_051", "sm_stage_052", "sm_stage_053", "sm_stage_054", "sm_stage_055", "sm_stage_056"
        };
        private static readonly string[] Stage_MartialPower =
        {
            "sm_stage_043", "sm_stage_063", "sm_stage_064",
            "sm_stage_065", "sm_stage_066", "sm_stage_067", "sm_stage_068",
            "sm_stage_050", "sm_stage_051", "sm_stage_052", "sm_stage_053", "sm_stage_054", "sm_stage_055", "sm_stage_056"
        };

        // 异能系（同人三分支：攻效/循环/功能，对应基因链三维度）
        private static readonly string[] Stage_PsiAttack =
        {
            "sm_stage_069", "sm_stage_070", "sm_stage_071",
            "sm_stage_072", "sm_stage_073", "sm_stage_074", "sm_stage_075",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_PsiCycle =
        {
            "sm_stage_069", "sm_stage_083", "sm_stage_084",
            "sm_stage_085", "sm_stage_086", "sm_stage_087", "sm_stage_088",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_PsiFunc =
        {
            "sm_stage_069", "sm_stage_089", "sm_stage_090",
            "sm_stage_091", "sm_stage_092", "sm_stage_093", "sm_stage_094",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };

        // 魔法系（原著两类：专精法师/魔网法师）
        private static readonly string[] Stage_MageSpecialist =
        {
            "sm_stage_095", "sm_stage_096", "sm_stage_097",
            "sm_stage_098", "sm_stage_099", "sm_stage_100", "sm_stage_101",
            "sm_stage_102", "sm_stage_103", "sm_stage_104", "sm_stage_105", "sm_stage_106", "sm_stage_107", "sm_stage_108"
        };
        private static readonly string[] Stage_MageWeave =
        {
            "sm_stage_095", "sm_stage_109", "sm_stage_110",
            "sm_stage_111", "sm_stage_112", "sm_stage_113", "sm_stage_114",
            "sm_stage_102", "sm_stage_103", "sm_stage_104", "sm_stage_105", "sm_stage_106", "sm_stage_107", "sm_stage_108"
        };

        // 念力系（同人三分支：灵魂/法则/现实）
        private static readonly string[] Stage_MindSoul =
        {
            "sm_stage_115", "sm_stage_116", "sm_stage_117",
            "sm_stage_118", "sm_stage_119", "sm_stage_120", "sm_stage_121",
            "sm_stage_122", "sm_stage_123", "sm_stage_124", "sm_stage_125", "sm_stage_126", "sm_stage_127", "sm_stage_128"
        };
        private static readonly string[] Stage_MindLaw =
        {
            "sm_stage_115", "sm_stage_129", "sm_stage_130",
            "sm_stage_131", "sm_stage_132", "sm_stage_133", "sm_stage_134",
            "sm_stage_122", "sm_stage_123", "sm_stage_124", "sm_stage_125", "sm_stage_126", "sm_stage_127", "sm_stage_128"
        };
        private static readonly string[] Stage_MindReality =
        {
            "sm_stage_115", "sm_stage_135", "sm_stage_136",
            "sm_stage_137", "sm_stage_138", "sm_stage_139", "sm_stage_140",
            "sm_stage_122", "sm_stage_123", "sm_stage_124", "sm_stage_125", "sm_stage_126", "sm_stage_127", "sm_stage_128"
        };

        // 未选分支的通用阶段名（按系）
        private static readonly string[] Stage_GenericMech =
        {
            "sm_stage_002", "sm_stage_141", "sm_stage_018",
            "sm_stage_019", "sm_stage_020", "sm_stage_021", "sm_stage_022",
            "sm_stage_023", "sm_stage_024", "sm_stage_025", "sm_stage_026", "sm_stage_027", "sm_stage_028", "sm_stage_029"
        };
        private static readonly string[] Stage_GenericMartial =
        {
            "sm_stage_043", "sm_stage_142", "sm_stage_143",
            "sm_stage_046", "sm_stage_059", "sm_stage_060", "sm_stage_065",
            "sm_stage_050", "sm_stage_051", "sm_stage_052", "sm_stage_053", "sm_stage_054", "sm_stage_055", "sm_stage_056"
        };
        private static readonly string[] Stage_GenericPsi =
        {
            "sm_stage_069", "sm_stage_144", "sm_stage_145",
            "sm_stage_146", "sm_stage_147", "sm_stage_148", "sm_stage_149",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_GenericMage =
        {
            "sm_stage_095", "sm_stage_150", "sm_stage_151",
            "sm_stage_152", "sm_stage_099", "sm_stage_153", "sm_stage_154",
            "sm_stage_102", "sm_stage_103", "sm_stage_104", "sm_stage_105", "sm_stage_106", "sm_stage_107", "sm_stage_108"
        };
        private static readonly string[] Stage_GenericMind =
        {
            "sm_stage_115", "sm_stage_155", "sm_stage_156",
            "sm_stage_157", "sm_stage_158", "sm_stage_159", "sm_stage_160",
            "sm_stage_122", "sm_stage_123", "sm_stage_124", "sm_stage_125", "sm_stage_126", "sm_stage_127", "sm_stage_128"
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
        /// 原著逻辑：
        /// - 机械师分支：完整14阶段职业链（ch50/ch1039）
        /// - 枪炮师/械武者分支：前7阶按能力命名（含原著【战争堡垒】【机甲操控师】），后7阶用"阶位+职业名"
        /// - 武道/异能/魔法/念力四系：原著未列出明确阶段链，用"阶位+职业名"格式</summary>
        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            int s = GetStage(a);
            string result;
            if (s <= 0) { result = "sm_stage_none"; }
            // 机械系单位：检查是否已选分支
            else if (a.hasTrait(SuperMechTraits.ClassMech) &&
                     !(a.hasTrait(SuperMechBranch.BranchMech) ||
                       a.hasTrait(SuperMechBranch.BranchGunner) ||
                       a.hasTrait(SuperMechBranch.BranchMartial)))
            { result = "sm_stage_161"; }
            // 机械师分支：完整14阶段
            else if (a.hasTrait(SuperMechBranch.BranchMech))
            {
                string[] arr = GetStageArray(a);
                int idx = Mathf.Clamp(s - 1, 0, arr.Length - 1);
                result = arr[idx];
            }
            // 枪炮师/械武者分支：前7阶有职业名，后7阶用阶位+职业名
            else if (a.hasTrait(SuperMechBranch.BranchGunner) || a.hasTrait(SuperMechBranch.BranchMartial))
            {
                if (s <= 7)
                {
                    string[] arr = GetStageArray(a);
                    int idx = Mathf.Clamp(s - 1, 0, arr.Length - 1);
                    result = arr[idx];
                }
                else
                {
                    string rank = SuperMechRanks.GetRankName(a);
                    string subClass = a.hasTrait(SuperMechBranch.BranchGunner) ? "sm_sub_gunner" : "sm_sub_mechmartial";
                    result = $"{rank}{LocalizedTextManager.getText(subClass)}";
                }
            }
            // 其他四系：阶位+职业名（原著逻辑）
            else
            {
                string rankName = SuperMechRanks.GetRankName(a);
                string className = GetGenericClassName(a);
                result = $"{rankName}{LocalizedTextManager.getText(className)}";
            }
            // 转换key为中文（阶位名已经是中文如F/E，不需要转换）
            if (result.StartsWith("sm_stage_") || result.StartsWith("sm_sub_"))
                return LocalizedTextManager.getText(result);
            return result;
        }

        /// <summary>获取其他四系的通用职业名（原著称呼）。</summary>
        private static string GetGenericClassName(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_stage_162");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_stage_163");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_stage_164");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_stage_165");
            return LocalizedTextManager.getText("sm_stage_166");
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
            string oldName = cur <= 0 ? "sm_stage_none" : GetStageName(a);
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
