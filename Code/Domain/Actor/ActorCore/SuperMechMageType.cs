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
    /// 存储方式：Dictionary（非特质）
    /// </summary>
    public static class SuperMechMageType
    {
        // === 觉醒类型 ===
        public const string TypeTalent = "sm_mage_talent";     // 天赋型法师
        public const string TypeCircuit = "sm_mage_circuit";   // 回路法师
        public const string TypeWeave = "sm_mage_weave";       // 魔网法师

        // === 专精分支（回路法师用）===
        public const string SpecElement = "sm_mage_spec_element";   // 元素专精
        public const string SpecArcane = "sm_mage_spec_arcane";     // 秘术专精
        public const string SpecSummon = "sm_mage_spec_summon";     // 召唤专精

        // 单位法师类型存储（actor id -> type id）
        private static readonly Dictionary<long, string> _mageType = new Dictionary<long, string>();
        // 单位法师专精存储（actor id -> spec id）
        private static readonly Dictionary<long, string> _mageSpec = new Dictionary<long, string>();
        private static readonly Dictionary<long, int> _weaveDailyUses = new Dictionary<long, int>();

        /// <summary>注册本地化文本（不再注册为ActorTrait）</summary>
        public static void Register()
        {
            // 天赋型法师
            LocalizedTextManager.add("trait_" + TypeTalent, LocalizedTextManager.getText("sm_mage_type_talent"), pReplace: true);
            LocalizedTextManager.add("trait_" + TypeTalent + "_info", LocalizedTextManager.getText("sm_mage_type_talent_info"), pReplace: true);

            // 回路法师
            LocalizedTextManager.add("trait_" + TypeCircuit, LocalizedTextManager.getText("sm_mage_type_circuit"), pReplace: true);
            LocalizedTextManager.add("trait_" + TypeCircuit + "_info", LocalizedTextManager.getText("sm_mage_type_circuit_info"), pReplace: true);

            // 魔网法师
            LocalizedTextManager.add("trait_" + TypeWeave, LocalizedTextManager.getText("sm_mage_type_weave"), pReplace: true);
            LocalizedTextManager.add("trait_" + TypeWeave + "_info", LocalizedTextManager.getText("sm_mage_type_weave_info"), pReplace: true);

            // 专精分支
            RegisterSpecText(SpecElement, "sm_mage_spec_element", "sm_mage_spec_element_info");
            RegisterSpecText(SpecArcane, "sm_mage_spec_arcane", "sm_mage_spec_arcane_info");
            RegisterSpecText(SpecSummon, "sm_mage_spec_summon", "sm_mage_spec_summon_info");
        }

        private static void RegisterSpecText(string id, string nameKey, string descKey)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(nameKey), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(descKey), pReplace: true);
        }

        /// <summary>设置法师类型</summary>
        public static void SetMageType(Actor a, string typeId)
        {
            if (a == null) return;
            if (string.IsNullOrEmpty(typeId))
                _mageType.Remove(a.id);
            else
                _mageType[a.id] = typeId;
        }

        /// <summary>获取法师类型</summary>
        public static string GetMageType(Actor a)
        {
            if (a == null) return null;
            string type;
            _mageType.TryGetValue(a.id, out type);
            return type;
        }

        /// <summary>是否有某法师类型</summary>
        public static bool HasMageType(Actor a, string typeId)
        {
            return GetMageType(a) == typeId;
        }

        /// <summary>设置法师专精</summary>
        public static void SetMageSpec(Actor a, string specId)
        {
            if (a == null) return;
            if (string.IsNullOrEmpty(specId))
                _mageSpec.Remove(a.id);
            else
                _mageSpec[a.id] = specId;
        }

        /// <summary>获取法师专精</summary>
        public static string GetMageSpec(Actor a)
        {
            if (a == null) return null;
            string spec;
            _mageSpec.TryGetValue(a.id, out spec);
            return spec;
        }

        /// <summary>魔法系觉醒时随机分配类型</summary>
        public static void AssignMageType(Actor a)
        {
            if (a == null) return;
            if (!a.hasTrait(SuperMechTraits.ClassMage)) return;
            if (GetMageType(a) != null) return;

            float roll = Random.value;
            if (roll < 0.5f)
            {
                // 50% 天赋型法师
                SetMageType(a, TypeTalent);
            }
            else if (roll < 0.85f)
            {
                // 35% 回路法师（随机专精一系）
                SetMageType(a, TypeCircuit);
                AssignRandomSpec(a);
            }
            else
            {
                // 15% 魔网法师
                SetMageType(a, TypeWeave);
            }
        }

        /// <summary>回路法师随机分配专精分支</summary>
        private static void AssignRandomSpec(Actor a)
        {
            string[] specs = { SpecElement, SpecArcane, SpecSummon };
            string spec = specs[Random.Range(0, specs.Length)];
            SetMageSpec(a, spec);
        }

        /// <summary>获取法师类型名称</summary>
        public static string GetMageTypeName(Actor a)
        {
            if (a == null) return "";
            string type = GetMageType(a);
            if (type == TypeTalent) return LocalizedTextManager.getText("trait_" + TypeTalent);
            if (type == TypeCircuit) return LocalizedTextManager.getText("trait_" + TypeCircuit);
            if (type == TypeWeave) return LocalizedTextManager.getText("trait_" + TypeWeave);
            return "";
        }

        /// <summary>获取专精分支名称</summary>
        public static string GetSpecName(Actor a)
        {
            if (a == null || GetMageType(a) != TypeCircuit) return "";
            string spec = GetMageSpec(a);
            if (spec == SpecElement) return LocalizedTextManager.getText("trait_" + SpecElement);
            if (spec == SpecArcane) return LocalizedTextManager.getText("trait_" + SpecArcane);
            if (spec == SpecSummon) return LocalizedTextManager.getText("trait_" + SpecSummon);
            return "";
        }

        /// <summary>检查是否可以学习某分支的某层级法术
        /// 原著：回路法师只能学专精分支的高阶法术，其他分支只能学基础
        /// </summary>
        public static bool CanLearnSpell(Actor a, string branch, int tier)
        {
            if (a == null) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMage)) return true;

            string type = GetMageType(a);

            // 天赋型法师：可学所有
            if (type == TypeTalent) return true;

            // 魔网法师：可学所有（但有每日次数限制）
            if (type == TypeWeave) return true;

            // 回路法师：专精分支可学所有层级，其他分支只能学基础（tier 0-1）
            if (type == TypeCircuit)
            {
                string spec = GetMageSpec(a);
                if (string.IsNullOrEmpty(spec)) return tier <= 1;
                if (IsSpecMatch(spec, branch)) return true;
                return tier <= 1; // 其他分支只能学基础
            }

            return true;
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
            if (GetMageType(a) == TypeCircuit)
            {
                string spec = GetMageSpec(a);
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
            if (GetMageType(a) == TypeCircuit)
            {
                string spec = GetMageSpec(a);
                if (IsSpecMatch(spec, branch)) return 1.5f;
            }
            return 1f;
        }

        /// <summary>魔网法师每日使用次数（简化为固定值）</summary>
        public static int GetWeaveDailyUses(Actor a)
        {
            if (a == null || GetMageType(a) != TypeWeave) return 0;
            // 基础10次，每阶段+2次
            int stage = SuperMechStage.GetStage(a);
            return 10 + stage * 2;
        }

        public static void Clear() { _mageType.Clear(); _mageSpec.Clear(); _weaveDailyUses.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) { _mageType.Remove(a.id); _mageSpec.Remove(a.id); _weaveDailyUses.Remove(a.id); }
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_mageType, alive);
            removed += SuperMechCleanup.CleanDict(_mageSpec, alive);
            removed += SuperMechCleanup.CleanDict(_weaveDailyUses, alive);
            return removed;
        }
    }
}
