using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechStage
    {
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

        public static readonly float[] StageQiBonus = { 10, 30, 50, 70, 100, 120, 150, 180, 210, 240, 300, 360, 450, 700 };

        public static readonly float[] LevelQiBonus = { 10, 20, 50, 80, 120, 120, 150, 180, 200, 220, 300, 360, 450, 700 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        private static string[] GetStageArray(Actor a)
        {
            if (a == null) return Stage_GenericMech;
            if (a.hasTrait(SuperMechBranch.BranchGunner)) return Stage_Gunner;
            if (a.hasTrait(SuperMechBranch.BranchMech)) return Stage_Mech;
            if (a.hasTrait(SuperMechBranch.BranchMartial)) return Stage_MechMartial;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return Stage_GenericMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return Stage_GenericPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return Stage_GenericMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return Stage_GenericMind;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return Stage_GenericMech;
            return Stage_GenericMech;
        }

        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            int s = GetStage(a);
            string result;
            if (s <= 0) { result = "sm_stage_none"; }
            else if (a.hasTrait(SuperMechTraits.ClassMech) ||
                     a.hasTrait(SuperMechTraits.ClassMartial) ||
                     a.hasTrait(SuperMechTraits.ClassPsi) ||
                     a.hasTrait(SuperMechTraits.ClassMage) ||
                     a.hasTrait(SuperMechTraits.ClassMind))
            {
                // 所有超神机械师体系：用对应阶段数组显示实际职业阶段名
                string[] arr = GetStageArray(a);
                int idx = Mathf.Clamp(s - 1, 0, arr.Length - 1);
                result = arr[idx];
            }
            else
            {
                // 兜底：阶位+体系名
                string rankName = SuperMechRanks.GetRankName(a);
                string className = GetGenericClassName(a);
                result = $"{rankName}{className}";
            }
            if (result.StartsWith("sm_stage_") || result.StartsWith("sm_sub_"))
                return LocalizedTextManager.getText(result);
            return result;
        }

        private static string GetGenericClassName(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_stage_162");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_stage_163");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_stage_164");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_stage_165");
            return LocalizedTextManager.getText("sm_stage_166");
        }

        public static void SetStage(Actor a, int stage)
        {
            if (a == null) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, 14);
        }

        public static bool Advance(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false;
            int cur = GetStage(a);
            if (cur >= 14) return false;
            string oldName = cur <= 0 ? "sm_stage_none" : GetStageName(a);
            SetStage(a, cur + 1);
            string newName = GetStageName(a);
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQiMax(a, StageQiBonus[cur]);
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
            }
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
            return true;
        }

        public static bool IsAtLeast(Actor a, int stage)
        {
            return GetStage(a) >= stage;
        }

        public static void Clear() { _stage.Clear(); }

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
