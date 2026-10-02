using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechStage
    {
        // === 机械系三分支阶段 ===
        private static readonly string[] Stage_Gunner =
        {
            "sm_stage_002", "sm_stage_003", "sm_stage_004",
            "sm_stage_005", "sm_stage_006", "sm_stage_007", "sm_stage_008",
            "sm_stage_009", "sm_stage_010", "sm_stage_011", "sm_stage_012", "sm_stage_013", "sm_stage_014", "sm_stage_015"
        };
        private static readonly string[] Stage_Mech =
        {
            "sm_stage_002", "sm_stage_017", "sm_stage_018",
            "sm_stage_019", "sm_stage_020", "sm_stage_021", "sm_stage_022",
            "sm_stage_023", "sm_stage_024", "sm_stage_025", "sm_stage_026", "sm_stage_027", "sm_stage_028", "sm_stage_029"
        };
        private static readonly string[] Stage_MechMartial =
        {
            "sm_stage_002", "sm_stage_030", "sm_stage_031",
            "sm_stage_032", "sm_stage_033", "sm_stage_034", "sm_stage_035",
            "sm_stage_036", "sm_stage_037", "sm_stage_038", "sm_stage_039", "sm_stage_040", "sm_stage_041", "sm_stage_042"
        };

        // === 念力系四大分支阶段（原著ch1384：心智/念动/感应/潜能）===
        private static readonly string[] Stage_PsiMind =
        {
            "sm_stage_069", "sm_stage_psi_mind_1", "sm_stage_psi_mind_2",
            "sm_stage_psi_mind_3", "sm_stage_psi_mind_4", "sm_stage_psi_mind_5", "sm_stage_psi_mind_6",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_PsiKinesis =
        {
            "sm_stage_069", "sm_stage_psi_kinesis_1", "sm_stage_psi_kinesis_2",
            "sm_stage_psi_kinesis_3", "sm_stage_psi_kinesis_4", "sm_stage_psi_kinesis_5", "sm_stage_psi_kinesis_6",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_PsiSense =
        {
            "sm_stage_069", "sm_stage_psi_sense_1", "sm_stage_psi_sense_2",
            "sm_stage_psi_sense_3", "sm_stage_psi_sense_4", "sm_stage_psi_sense_5", "sm_stage_psi_sense_6",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };
        private static readonly string[] Stage_PsiPotential =
        {
            "sm_stage_069", "sm_stage_psi_potential_1", "sm_stage_psi_potential_2",
            "sm_stage_psi_potential_3", "sm_stage_psi_potential_4", "sm_stage_psi_potential_5", "sm_stage_psi_potential_6",
            "sm_stage_076", "sm_stage_077", "sm_stage_078", "sm_stage_079", "sm_stage_080", "sm_stage_081", "sm_stage_082"
        };

        // === 通用阶段（武道系/魔法系/异能系无固定转职分支，使用通用阶段）===
        private static readonly string[] Stage_GenericMartial =
        {
            "sm_stage_043", "sm_stage_142", "sm_stage_143",
            "sm_stage_046", "sm_stage_059", "sm_stage_060", "sm_stage_065",
            "sm_stage_050", "sm_stage_051", "sm_stage_052", "sm_stage_053", "sm_stage_054", "sm_stage_055", "sm_stage_056"
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
            if (a == null) return Stage_Mech;
            // 机械系三分支
            if (a.hasTrait(SuperMechBranch.BranchGunner)) return Stage_Gunner;
            if (a.hasTrait(SuperMechBranch.BranchMech)) return Stage_Mech;
            if (a.hasTrait(SuperMechBranch.BranchMartial)) return Stage_MechMartial;
            // 念力系四大分支
            if (a.hasTrait(SuperMechBranch.BranchPsiMind)) return Stage_PsiMind;
            if (a.hasTrait(SuperMechBranch.BranchPsiKinesis)) return Stage_PsiKinesis;
            if (a.hasTrait(SuperMechBranch.BranchPsiSense)) return Stage_PsiSense;
            if (a.hasTrait(SuperMechBranch.BranchPsiPotential)) return Stage_PsiPotential;
            // 其他体系无固定分支，用通用阶段
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return Stage_GenericMartial;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return Stage_GenericMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return Stage_GenericMind;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return Stage_PsiMind; // 念力系未选分支默认心智系
            if (a.hasTrait(SuperMechTraits.ClassMech)) return Stage_Mech;
            return Stage_Mech;
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
            else if (a.hasTrait(SuperMechTraits.ClassMech) &&
                     !(a.hasTrait(SuperMechBranch.BranchMech) ||
                       a.hasTrait(SuperMechBranch.BranchGunner) ||
                       a.hasTrait(SuperMechBranch.BranchMartial)))
            {
                // 机械系未选分支：第一阶段显示入门者，第二阶段起提示需选择分支
                if (s <= 1) result = "sm_stage_002";
                else result = "sm_stage_161";
            }
            else if (a.hasTrait(SuperMechTraits.ClassMech) ||
                     a.hasTrait(SuperMechTraits.ClassMartial) ||
                     a.hasTrait(SuperMechTraits.ClassPsi) ||
                     a.hasTrait(SuperMechTraits.ClassMage) ||
                     a.hasTrait(SuperMechTraits.ClassMind))
            {
                string[] arr = GetStageArray(a);
                int idx = Mathf.Clamp(s - 1, 0, arr.Length - 1);

                // 机械师高阶阶段（神座/超神）按专精显示不同形态名
                if (a.hasTrait(SuperMechBranch.BranchMech) && SuperMechSpecialization.GetSpec(a) != null)
                {
                    if (s == 13) result = SuperMechSpecialization.GetThroneFormName(a);
                    else if (s >= 14) result = SuperMechSpecialization.GetHighFormName(a);
                    else result = arr[idx];
                }
                else
                {
                    result = arr[idx];
                }
            }
            else
            {
                string rankName = SuperMechRanks.GetRankName(a);
                string className = GetGenericClassName(a);
                result = $"{rankName}{className}";
            }
            if (result.StartsWith("sm_stage_") || result.StartsWith("sm_sub_") || result.StartsWith("sm_spec_"))
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
