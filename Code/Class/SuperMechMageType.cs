using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 魔法系觉醒类型系统（原著ch404）。
    /// 原著设定：
    /// 1. 天赋型法师：先天觉醒超能基因，自带法力，可学所有法术
    /// 2. 回路法师：体内植入魔法回路，只能专精一系，法术威力加成
    /// 3. 魔网法师：法力是"借"来的，与魔法实体契约，每日次数上限
    /// </summary>
    public static class SuperMechMageType
    {
        // === 觉醒类型Trait ID ===
        public const string TypeTalent = "sm_mage_talent";     // 天赋型法师
        public const string TypeCircuit = "sm_mage_circuit";   // 回路法师
        public const string TypeWeave = "sm_mage_weave";       // 魔网法师

        // === 专精分支Trait ID（回路法师用）===
        public const string SpecElement = "sm_mage_spec_element";   // 元素专精
        public const string SpecArcane = "sm_mage_spec_arcane";     // 秘术专精
        public const string SpecSummon = "sm_mage_spec_summon";     // 召唤专精

        private static readonly Dictionary<long, string> _mageSpec = new Dictionary<long, string>();
        private static readonly Dictionary<long, int> _weaveDailyUses = new Dictionary<long, int>();

        /// <summary>注册觉醒类型Trait</summary>
        public static void Register()
        {
            // 天赋型法师
            var t1 = new ActorTrait
            {
                id = TypeTalent,
                path_icon = "ui/Icons/iconPurpleBook",
                group_id = "sm_classes",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            LocalizedTextManager.add("trait_" + TypeTalent, "天赋型法师", pReplace: true);
            LocalizedTextManager.add("trait_" + TypeTalent + "_info", "先天觉醒法力，可学习所有法术分支", pReplace: true);
            AssetManager.traits.add(t1);

            // 回路法师
            var t2 = new ActorTrait
            {
                id = TypeCircuit,
                path_icon = "ui/Icons/iconPurpleBook",
                group_id = "sm_classes",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            LocalizedTextManager.add("trait_" + TypeCircuit, "回路法师", pReplace: true);
            LocalizedTextManager.add("trait_" + TypeCircuit + "_info", "植入魔法回路，专精一系法术，威力加成但无法学习其他系高阶法术", pReplace: true);
            AssetManager.traits.add(t2);

            // 魔网法师
            var t3 = new ActorTrait
            {
                id = TypeWeave,
                path_icon = "ui/Icons/iconPurpleBook",
                group_id = "sm_classes",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            LocalizedTextManager.add("trait_" + TypeWeave, "魔网法师", pReplace: true);
            LocalizedTextManager.add("trait_" + TypeWeave + "_info", "法力借自魔法实体，法术无消耗但每日有使用次数上限", pReplace: true);
            AssetManager.traits.add(t3);

            // 专精分支
            RegisterSpecTrait(SpecElement, "元素专精", "专精元素法术，威力+50%，成功率+30%");
            RegisterSpecTrait(SpecArcane, "秘术专精", "专精秘术法术，威力+50%，成功率+30%");
            RegisterSpecTrait(SpecSummon, "召唤专精", "专精召唤法术，威力+50%，成功率+30%");
        }

        private static void RegisterSpecTrait(string id, string name, string desc)
        {
            var t = new ActorTrait
            {
                id = id,
                path_icon = "ui/Icons/iconPurpleBook",
                group_id = "sm_specialties",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            AssetManager.traits.add(t);
        }

        /// <summary>魔法系觉醒时随机分配类型</summary>
        public static void AssignMageType(Actor a)
        {
            if (a == null) return;
            if (!a.hasTrait(SuperMechTraits.ClassMage)) return;
            if (a.hasTrait(TypeTalent) || a.hasTrait(TypeCircuit) || a.hasTrait(TypeWeave)) return;

            float roll = Random.value;
            if (roll < 0.5f)
            {
                // 50% 天赋型法师
                a.addTrait(TypeTalent);
            }
            else if (roll < 0.85f)
            {
                // 35% 回路法师（随机专精一系）
                a.addTrait(TypeCircuit);
                AssignRandomSpec(a);
            }
            else
            {
                // 15% 魔网法师
                a.addTrait(TypeWeave);
            }
        }

        /// <summary>回路法师随机分配专精分支</summary>
        private static void AssignRandomSpec(Actor a)
        {
            string[] specs = { SpecElement, SpecArcane, SpecSummon };
            string spec = specs[Random.Range(0, specs.Length)];
            a.addTrait(spec);
            _mageSpec[a.id] = spec;
        }

        /// <summary>获取法师类型名称</summary>
        public static string GetMageTypeName(Actor a)
        {
            if (a == null) return "";
            if (a.hasTrait(TypeTalent)) return LocalizedTextManager.getText("trait_" + TypeTalent);
            if (a.hasTrait(TypeCircuit)) return LocalizedTextManager.getText("trait_" + TypeCircuit);
            if (a.hasTrait(TypeWeave)) return LocalizedTextManager.getText("trait_" + TypeWeave);
            return "";
        }

        /// <summary>获取专精分支名称</summary>
        public static string GetSpecName(Actor a)
        {
            if (a == null || !a.hasTrait(TypeCircuit)) return "";
            if (a.hasTrait(SpecElement)) return LocalizedTextManager.getText("trait_" + SpecElement);
            if (a.hasTrait(SpecArcane)) return LocalizedTextManager.getText("trait_" + SpecArcane);
            if (a.hasTrait(SpecSummon)) return LocalizedTextManager.getText("trait_" + SpecSummon);
            return "";
        }

        /// <summary>检查是否可以学习某分支的某层级法术
        /// 原著：回路法师只能学专精分支的高阶法术，其他分支只能学基础
        /// </summary>
        public static bool CanLearnSpell(Actor a, string branch, int tier)
        {
            if (a == null) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMage)) return true;

            // 天赋型法师：可学所有
            if (a.hasTrait(TypeTalent)) return true;

            // 魔网法师：可学所有（但有每日次数限制）
            if (a.hasTrait(TypeWeave)) return true;

            // 回路法师：专精分支可学所有层级，其他分支只能学基础（tier 0-1）
            if (a.hasTrait(TypeCircuit))
            {
                string spec = GetSpecTraitId(a);
                if (spec == "") return tier <= 1;
                if (IsSpecMatch(spec, branch)) return true;
                return tier <= 1; // 其他分支只能学基础
            }

            return true;
        }

        private static string GetSpecTraitId(Actor a)
        {
            if (a.hasTrait(SpecElement)) return SpecElement;
            if (a.hasTrait(SpecArcane)) return SpecArcane;
            if (a.hasTrait(SpecSummon)) return SpecSummon;
            return "";
        }

        private static bool IsSpecMatch(string spec, string branch)
        {
            // 知识树分支名：element/arcane/summon
            if (spec == SpecElement && branch == "element") return true;
            if (spec == SpecArcane && branch == "arcane") return true;
            if (spec == SpecSummon && branch == "summon") return true;
            return false;
        }

        /// <summary>获取法术学习成功率加成
        /// 原著：成功率取决于智力和神秘属性，回路法师专精分支+30%
        /// </summary>
        public static float GetLearnSuccessBonus(Actor a, string branch)
        {
            if (a == null) return 0f;
            float bonus = 0f;

            // 智力和神秘属性加成（每点+0.5%）
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                bonus += stats["intelligence"] * 0.005f;
                bonus += stats["mystery"] * 0.005f;
            }

            // 回路法师专精分支+30%
            if (a.hasTrait(TypeCircuit))
            {
                string spec = GetSpecTraitId(a);
                if (IsSpecMatch(spec, branch)) bonus += 0.30f;
            }

            return bonus;
        }

        /// <summary>获取法术威力加成
        /// 原著：回路法师专精分支威力+50%
        /// </summary>
        public static float GetSpellPowerBonus(Actor a, string branch)
        {
            if (a == null) return 1f;
            if (a.hasTrait(TypeCircuit))
            {
                string spec = GetSpecTraitId(a);
                if (IsSpecMatch(spec, branch)) return 1.5f;
            }
            return 1f;
        }

        /// <summary>魔网法师每日使用次数（简化为固定值）</summary>
        public static int GetWeaveDailyUses(Actor a)
        {
            if (a == null || !a.hasTrait(TypeWeave)) return 0;
            // 基础10次，每阶段+2次
            int stage = SuperMechStage.GetStage(a);
            return 10 + stage * 2;
        }

        public static void Clear() { _mageSpec.Clear(); _weaveDailyUses.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) { _mageSpec.Remove(a.id); _weaveDailyUses.Remove(a.id); }
        }
    }
}
