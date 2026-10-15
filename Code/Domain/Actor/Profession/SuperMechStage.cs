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

        public static readonly float[] StageQiBonus = { 100, 300, 500, 700, 1000, 1200, 1500, 1800, 2100, 2400, 3000, 3600, 4500, 7000 }; // v0.75.17: ×10

        public static readonly float[] LevelQiBonus = { 100, 200, 500, 800, 1200, 1200, 1500, 1800, 2000, 2200, 3000, 3600, 4500, 7000 }; // v0.75.17: ×10

        /// <summary>机械师各阶段等级上限（原著精确数据，未知项用合理估算标注）
        /// 索引0=阶段1机械入门者，索引13=阶段14超神机械师
        /// </summary>
        public static readonly int[] MechStageLevelCaps =
        {
            10,    // 1.机械入门者（原著：上限10级）
            10,    // 2.机械师学徒（原著：上限10级）
            15,    // 3.见习机械师（原著：上限15级）
            20,    // 4.磁环机械师（估算）
            20,    // 5.数据机械师（估算）
            20,    // 6.战争机械师（估算）
            20,    // 7.虚拟机械师（估算）
            25,    // 8.星海机械师（原著：上限25级，比前一阶段提高5级）
            30,    // 9.真理机械师（估算，总等级180触发进阶任务）
            30,    // 10.使徒机械师（估算，总等级200转职）
            35,    // 11.帝皇机械师（原著：上限35级）
            40,    // 12.主宰机械师（估算）
            60,    // 13.神座机械师（原著：60级，总等级320触发神性）
            999    // 14.超神机械师（无上限）
        };

        /// <summary>枪炮师各阶段等级上限（基于机械师参照+同系职业特性推演）
        /// 原著未明确枪炮师等级上限，参照机械师数据推演
        /// 索引0=阶段1机械入门者，索引13=阶段14超神枪炮师
        /// </summary>
        public static readonly int[] GunnerStageLevelCaps =
        {
            10,    // 1.机械系入门者（通用）
            10,    // 2.枪炮师学徒（参照机械师学徒）
            15,    // 3.见习枪炮师（参照见习机械师）
            15,    // 4.锋锐枪炮师（初级阶段）
            20,    // 5.火力枪炮师（中级阶段）
            20,    // 6.重炮枪炮师（中级阶段）
            25,    // 7.战争堡垒（高级职业，参照星海机械师）
            25,    // 8.星海枪炮师（高级阶段）
            30,    // 9.圣裁枪炮师（高阶）
            30,    // 10.毁灭枪炮师（高阶）
            35,    // 11.歼灭枪炮师（参照帝皇机械师）
            40,    // 12.霸主枪炮师（顶阶）
            60,    // 13.神枪枪炮师（参照神座机械师）
            999    // 14.超神枪炮师（无上限）
        };

        /// <summary>械武者各阶段等级上限（基于机械师参照+同系职业特性推演）
        /// 原著未明确械武者等级上限，参照机械师数据推演
        /// 索引0=阶段1机械入门者，索引13=阶段14超神械武者
        /// </summary>
        public static readonly int[] MartialStageLevelCaps =
        {
            10,    // 1.机械系入门者（通用）
            10,    // 2.械武者学徒（参照机械师学徒）
            15,    // 3.见习械武者（参照见习机械师）
            15,    // 4.锋刃械武者（初级阶段）
            20,    // 5.殖装械武者（中级阶段，原著"殖装技术"）
            20,    // 6.纳米械武者（中级阶段，原著"纳米手段"）
            25,    // 7.机甲操控师（高级职业，参照星海机械师）
            25,    // 8.星海械武者（高级阶段）
            30,    // 9.圣战械武者（高阶）
            30,    // 10.破军械武者（高阶）
            35,    // 11.镇山械武者（参照帝皇机械师）
            40,    // 12.霸主械武者（顶阶）
            60,    // 13.神武械武者（参照神座机械师）
            999    // 14.超神械武者（无上限）
        };

        /// <summary>转职触发的总等级节点（原著精确数据）
        /// 达到该总等级时触发对应阶段的进阶任务/转职
        /// </summary>
        public static readonly int[] AdvancementTotalLevelTriggers =
        {
            0,     // 阶段1：无
            0,     // 阶段2：无
            0,     // 阶段3：无
            0,     // 阶段4：无
            0,     // 阶段5：无
            0,     // 阶段6：无
            0,     // 阶段7：无
            0,     // 阶段8：无
            180,   // 阶段9真理机械师：总等级180触发进阶任务（原著精确）
            200,   // 阶段10使徒机械师：总等级200转职（原著精确）
            0,     // 阶段11：无
            0,     // 阶段12：无
            320,   // 阶段13神座机械师：总等级320触发神性（原著精确）
            0      // 阶段14：无
        };

        /// <summary>获取当前阶段的等级上限</summary>
        public static int GetStageLevelCap(Actor a)
        {
            if (a == null) return 999;
            int stage = GetStage(a);
            if (stage <= 0 || stage > 14) return 999;
            // 机械系三职业方向各有独立等级上限
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchGunner))
                return GunnerStageLevelCaps[stage - 1];
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMartial))
                return MartialStageLevelCaps[stage - 1];
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech))
                return MechStageLevelCaps[stage - 1];
            // 其他体系暂不限制
            return 999;
        }

        /// <summary>检查单位是否达到当前阶段的等级上限</summary>
        public static bool IsAtStageLevelCap(Actor a)
        {
            if (a == null) return false;
            int cap = GetStageLevelCap(a);
            if (cap >= 999) return false;
            return a.data.level >= cap;
        }

        /// <summary>获取下一转职触发的总等级（0表示无特殊触发）</summary>
        public static int GetNextAdvancementTrigger(Actor a)
        {
            if (a == null) return 0;
            int stage = GetStage(a);
            if (stage <= 0 || stage >= AdvancementTotalLevelTriggers.Length) return 0;
            // 下一个阶段的触发等级
            int nextStage = stage + 1;
            if (nextStage <= AdvancementTotalLevelTriggers.Length)
                return AdvancementTotalLevelTriggers[nextStage - 1];
            return 0;
        }

        /// <summary>检查是否满足转职的总等级条件</summary>
        public static bool CanAdvanceByTotalLevel(Actor a)
        {
            if (a == null) return false;
            int trigger = GetNextAdvancementTrigger(a);
            if (trigger <= 0) return true; // 无特殊触发要求
            return a.data.level >= trigger;
        }

        /// <summary>获取阶段进度描述（用于UI显示）</summary>
        public static string GetStageProgressText(Actor a)
        {
            if (a == null) return "";
            int stage = GetStage(a);
            if (stage <= 0) return "";
            int cap = GetStageLevelCap(a);
            int trigger = GetNextAdvancementTrigger(a);

            if (cap >= 999 && trigger <= 0) return "";

            string text = "";
            if (cap < 999)
                text += $"Lv{a.data.level}/{cap}";
            if (trigger > 0)
            {
                string tNeedLevel = LocalizedTextManager.getText("sm_stage_need_level");
                text += (text.Length > 0 ? "，" : "") + string.Format(tNeedLevel, trigger);
            }
            return text;
        }

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        private static string[] GetStageArray(Actor a)
        {
            if (a == null) return Stage_Mech;
            // 机械系三职业方向
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchGunner)) return Stage_Gunner;
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech)) return Stage_Mech;
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMartial)) return Stage_MechMartial;
            // 念力系四大方向
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchPsiMind)) return Stage_PsiMind;
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchPsiKinesis)) return Stage_PsiKinesis;
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchPsiSense)) return Stage_PsiSense;
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchPsiPotential)) return Stage_PsiPotential;
            // 其他体系无固定方向，用通用阶段
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return Stage_GenericMartial;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return Stage_GenericMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return Stage_PsiMind;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return Stage_GenericMind;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return Stage_Mech;
            return Stage_Mech;
        }

        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            // 优先从ActorContext读取
            var ctx = SuperMechActorContextRegistry.TryGet(a.data.id);
            if (ctx != null && ctx.stage > 0) return ctx.stage;
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
                     !(SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech) ||
                       SuperMechBranch.HasBranch(a, SuperMechBranch.BranchGunner) ||
                       SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMartial)))
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
                if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech) && SuperMechBranchMastery.GetSpec(a) != null)
                {
                    if (s == 13) result = SuperMechBranchMastery.GetThroneFormName(a);
                    else if (s >= 14) result = SuperMechBranchMastery.GetHighFormName(a);
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
            int clamped = Mathf.Clamp(stage, 0, 14);
            _stage[a.data.id] = clamped;
            // 同步到ActorContext
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.stage = clamped;

            // 转职超神机械师时：机械师虚拟分支领悟虚拟创世（原著）
            if (clamped >= 14)
            {
                SuperMechVirtualGenesis.TryAwakenPseudo(a);
            }
        }

        public static bool Advance(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false;
            int cur = GetStage(a);
            if (cur >= 14) return false;

            // 原著等级上限检查：达到当前阶段等级上限才能进阶
            if (a.hasTrait(SuperMechTraits.ClassMech) && cur > 0)
            {
                int cap = GetStageLevelCap(a);
                if (cap < 999 && a.data.level < cap)
                {
                    Debug.Log($"[超神机械师] 进阶失败: {a.name} 等级{a.data.level}未达到阶段{cur}上限{cap}");
                    return false;
                }
            }

            // 原著转职总等级节点检查（真理180/使徒200/神座320）
            if (!CanAdvanceByTotalLevel(a))
            {
                int trigger = GetNextAdvancementTrigger(a);
                Debug.Log($"[超神机械师] 进阶失败: {a.name} 总等级{a.data.level}未达到转职要求{trigger}");
                return false;
            }

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
            // v0.76.87 专精自动判定：机械系达到第7阶段（战争堡垒/机甲操控师）后按属性倾向性自动判定
            // 原著：机械师分支按知识学习倾向性自动判定主要分支，非手动选择
            if (cur + 1 == 7 && a.hasTrait(SuperMechTraits.ClassMech) && !SuperMechSpecialty.HasSpecialty(a))
            {
                string profession = "mech"; // 默认机械师
                if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchGunner)) profession = "gun";
                else if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMartial)) profession = "martial";
                SuperMechSpecialty.AutoAssignSpecialty(a, profession);
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
