using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 魔法系法术学习系统（原著ch404）。
    /// 原著设定：
    /// - 法术分支：元素/秘术/庇护/时空/塑能/召唤/诅咒祈福
    /// - 学习法术分阶段，每阶段消耗经验，越往后越多
    /// - 成功率取决于智力和神秘属性，越高阶法术阶段分得越多
    /// - 高阶法术有前置要求（先学会前置低级法术，或属性达到某数值）
    /// - 回路法师只能学专精分支高阶法术，其他分支只能学基础
    /// - 天赋型法师可学所有法术
    /// </summary>
    public static class SuperMechSpell
    {
        /// <summary>法术分支枚举</summary>
        public enum SpellBranch
        {
            Element = 0,      // 元素
            Arcane = 1,       // 秘术
            Abjuration = 2,   // 庇护
            Chronomancy = 3,  // 时空
            Evocation = 4,    // 塑能
            Summon = 5,       // 召唤
            Curse = 6         // 诅咒祈福
        }

        /// <summary>法术定义</summary>
        public class SpellDef
        {
            public string id;              // 法术唯一ID
            public string nameKey;         // 名称本地化key
            public string descKey;         // 描述本地化key
            public SpellBranch branch;     // 所属分支
            public int tier;               // 法术等阶（1基础~5终极）
            public int totalPhases;        // 学习阶段总数
            public int[] xpCostPerPhase;   // 每阶段经验消耗
            public float baseSuccessRate;  // 基础成功率
            public string[] prerequisites; // 前置法术ID
            public float intRequirement;   // 智力要求
            public float mysteryRequirement; // 神秘要求
        }

        /// <summary>单位法术学习状态</summary>
        private class SpellProgress
        {
            public int currentPhase = 0;   // 当前学习阶段（0=未开始，totalPhases=已学会）
            public bool learned = false;   // 是否已学会
        }

        private static readonly Dictionary<string, SpellDef> _spells = new Dictionary<string, SpellDef>();
        private static readonly Dictionary<long, Dictionary<string, SpellProgress>> _unitProgress = new Dictionary<long, Dictionary<string, SpellProgress>>();
        private static bool _initialized = false;

        /// <summary>分支名称本地化key</summary>
        public static readonly string[] BranchNameKeys = {
            "sm_spell_branch_element",
            "sm_spell_branch_arcane",
            "sm_spell_branch_abjuration",
            "sm_spell_branch_chronomancy",
            "sm_spell_branch_evocation",
            "sm_spell_branch_summon",
            "sm_spell_branch_curse"
        };

        /// <summary>初始化法术库</summary>
        public static void RegisterAll()
        {
            if (_initialized) return;
            _initialized = true;

            // === 元素分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_fire_bolt", nameKey = "sm_spell_fire_bolt_name", descKey = "sm_spell_fire_bolt_desc",
                branch = SpellBranch.Element, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 50, 100 }, baseSuccessRate = 0.8f,
                prerequisites = new string[0], intRequirement = 0, mysteryRequirement = 0
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_fireball", nameKey = "sm_spell_fireball_name", descKey = "sm_spell_fireball_desc",
                branch = SpellBranch.Element, tier = 2, totalPhases = 3,
                xpCostPerPhase = new[] { 100, 200, 400 }, baseSuccessRate = 0.6f,
                prerequisites = new[] { "sm_spell_fire_bolt" }, intRequirement = 10, mysteryRequirement = 5
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_ice_shield", nameKey = "sm_spell_ice_shield_name", descKey = "sm_spell_ice_shield_desc",
                branch = SpellBranch.Element, tier = 2, totalPhases = 3,
                xpCostPerPhase = new[] { 80, 160, 320 }, baseSuccessRate = 0.65f,
                prerequisites = new[] { "sm_spell_fire_bolt" }, intRequirement = 8, mysteryRequirement = 8
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_lightning_storm", nameKey = "sm_spell_lightning_storm_name", descKey = "sm_spell_lightning_storm_desc",
                branch = SpellBranch.Element, tier = 4, totalPhases = 4,
                xpCostPerPhase = new[] { 300, 600, 1200, 2400 }, baseSuccessRate = 0.35f,
                prerequisites = new[] { "sm_spell_fireball", "sm_spell_ice_shield" }, intRequirement = 30, mysteryRequirement = 20
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_meteor_swarm", nameKey = "sm_spell_meteor_swarm_name", descKey = "sm_spell_meteor_swarm_desc",
                branch = SpellBranch.Element, tier = 5, totalPhases = 5,
                xpCostPerPhase = new[] { 500, 1000, 2000, 4000, 8000 }, baseSuccessRate = 0.2f,
                prerequisites = new[] { "sm_spell_lightning_storm" }, intRequirement = 50, mysteryRequirement = 40
            });

            // === 秘术分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_arcane_missile", nameKey = "sm_spell_arcane_missile_name", descKey = "sm_spell_arcane_missile_desc",
                branch = SpellBranch.Arcane, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 60, 120 }, baseSuccessRate = 0.75f,
                prerequisites = new string[0], intRequirement = 5, mysteryRequirement = 0
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_dispel", nameKey = "sm_spell_dispel_name", descKey = "sm_spell_dispel_desc",
                branch = SpellBranch.Arcane, tier = 3, totalPhases = 3,
                xpCostPerPhase = new[] { 150, 300, 600 }, baseSuccessRate = 0.5f,
                prerequisites = new[] { "sm_spell_arcane_missile" }, intRequirement = 20, mysteryRequirement = 15
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_arcane_blast", nameKey = "sm_spell_arcane_blast_name", descKey = "sm_spell_arcane_blast_desc",
                branch = SpellBranch.Arcane, tier = 5, totalPhases = 5,
                xpCostPerPhase = new[] { 400, 800, 1600, 3200, 6400 }, baseSuccessRate = 0.25f,
                prerequisites = new[] { "sm_spell_dispel" }, intRequirement = 45, mysteryRequirement = 35
            });

            // === 庇护分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_minor_ward", nameKey = "sm_spell_minor_ward_name", descKey = "sm_spell_minor_ward_desc",
                branch = SpellBranch.Abjuration, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 40, 80 }, baseSuccessRate = 0.85f,
                prerequisites = new string[0], intRequirement = 0, mysteryRequirement = 5
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_mage_armor", nameKey = "sm_spell_mage_armor_name", descKey = "sm_spell_mage_armor_desc",
                branch = SpellBranch.Abjuration, tier = 2, totalPhases = 3,
                xpCostPerPhase = new[] { 100, 200, 400 }, baseSuccessRate = 0.6f,
                prerequisites = new[] { "sm_spell_minor_ward" }, intRequirement = 10, mysteryRequirement = 10
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_antimagic_field", nameKey = "sm_spell_antimagic_field_name", descKey = "sm_spell_antimagic_field_desc",
                branch = SpellBranch.Abjuration, tier = 4, totalPhases = 4,
                xpCostPerPhase = new[] { 350, 700, 1400, 2800 }, baseSuccessRate = 0.3f,
                prerequisites = new[] { "sm_spell_mage_armor" }, intRequirement = 35, mysteryRequirement = 25
            });

            // === 时空分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_blink", nameKey = "sm_spell_blink_name", descKey = "sm_spell_blink_desc",
                branch = SpellBranch.Chronomancy, tier = 2, totalPhases = 3,
                xpCostPerPhase = new[] { 120, 240, 480 }, baseSuccessRate = 0.55f,
                prerequisites = new string[0], intRequirement = 15, mysteryRequirement = 10
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_time_slow", nameKey = "sm_spell_time_slow_name", descKey = "sm_spell_time_slow_desc",
                branch = SpellBranch.Chronomancy, tier = 4, totalPhases = 4,
                xpCostPerPhase = new[] { 400, 800, 1600, 3200 }, baseSuccessRate = 0.3f,
                prerequisites = new[] { "sm_spell_blink" }, intRequirement = 40, mysteryRequirement = 30
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_time_stop", nameKey = "sm_spell_time_stop_name", descKey = "sm_spell_time_stop_desc",
                branch = SpellBranch.Chronomancy, tier = 5, totalPhases = 5,
                xpCostPerPhase = new[] { 600, 1200, 2400, 4800, 9600 }, baseSuccessRate = 0.15f,
                prerequisites = new[] { "sm_spell_time_slow" }, intRequirement = 60, mysteryRequirement = 50
            });

            // === 塑能分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_energy_bolt", nameKey = "sm_spell_energy_bolt_name", descKey = "sm_spell_energy_bolt_desc",
                branch = SpellBranch.Evocation, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 50, 100 }, baseSuccessRate = 0.8f,
                prerequisites = new string[0], intRequirement = 0, mysteryRequirement = 0
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_energy_beam", nameKey = "sm_spell_energy_beam_name", descKey = "sm_spell_energy_beam_desc",
                branch = SpellBranch.Evocation, tier = 3, totalPhases = 3,
                xpCostPerPhase = new[] { 200, 400, 800 }, baseSuccessRate = 0.45f,
                prerequisites = new[] { "sm_spell_energy_bolt" }, intRequirement = 20, mysteryRequirement = 15
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_evocation_storm", nameKey = "sm_spell_evocation_storm_name", descKey = "sm_spell_evocation_storm_desc",
                branch = SpellBranch.Evocation, tier = 5, totalPhases = 5,
                xpCostPerPhase = new[] { 450, 900, 1800, 3600, 7200 }, baseSuccessRate = 0.22f,
                prerequisites = new[] { "sm_spell_energy_beam" }, intRequirement = 48, mysteryRequirement = 38
            });

            // === 召唤分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_summon_familiar", nameKey = "sm_spell_summon_familiar_name", descKey = "sm_spell_summon_familiar_desc",
                branch = SpellBranch.Summon, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 80, 160 }, baseSuccessRate = 0.7f,
                prerequisites = new string[0], intRequirement = 5, mysteryRequirement = 10
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_summon_elemental", nameKey = "sm_spell_summon_elemental_name", descKey = "sm_spell_summon_elemental_desc",
                branch = SpellBranch.Summon, tier = 3, totalPhases = 3,
                xpCostPerPhase = new[] { 200, 400, 800 }, baseSuccessRate = 0.45f,
                prerequisites = new[] { "sm_spell_summon_familiar" }, intRequirement = 15, mysteryRequirement = 25
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_summon_elemental_lord", nameKey = "sm_spell_summon_elemental_lord_name", descKey = "sm_spell_summon_elemental_lord_desc",
                branch = SpellBranch.Summon, tier = 5, totalPhases = 5,
                xpCostPerPhase = new[] { 500, 1000, 2000, 4000, 8000 }, baseSuccessRate = 0.2f,
                prerequisites = new[] { "sm_spell_summon_elemental" }, intRequirement = 40, mysteryRequirement = 50
            });

            // === 诅咒祈福分支 ===
            RegisterSpell(new SpellDef {
                id = "sm_spell_minor_curse", nameKey = "sm_spell_minor_curse_name", descKey = "sm_spell_minor_curse_desc",
                branch = SpellBranch.Curse, tier = 1, totalPhases = 2,
                xpCostPerPhase = new[] { 60, 120 }, baseSuccessRate = 0.75f,
                prerequisites = new string[0], intRequirement = 0, mysteryRequirement = 10
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_blessing", nameKey = "sm_spell_blessing_name", descKey = "sm_spell_blessing_desc",
                branch = SpellBranch.Curse, tier = 2, totalPhases = 3,
                xpCostPerPhase = new[] { 100, 200, 400 }, baseSuccessRate = 0.6f,
                prerequisites = new[] { "sm_spell_minor_curse" }, intRequirement = 10, mysteryRequirement = 15
            });
            RegisterSpell(new SpellDef {
                id = "sm_spell_death_curse", nameKey = "sm_spell_death_curse_name", descKey = "sm_spell_death_curse_desc",
                branch = SpellBranch.Curse, tier = 4, totalPhases = 4,
                xpCostPerPhase = new[] { 350, 700, 1400, 2800 }, baseSuccessRate = 0.28f,
                prerequisites = new[] { "sm_spell_blessing" }, intRequirement = 30, mysteryRequirement = 40
            });
        }

        private static void RegisterSpell(SpellDef spell)
        {
            if (!_spells.ContainsKey(spell.id))
                _spells.Add(spell.id, spell);
        }

        /// <summary>获取所有法术</summary>
        public static Dictionary<string, SpellDef>.ValueCollection GetAllSpells()
        {
            return _spells.Values;
        }

        /// <summary>按分支获取法术</summary>
        public static List<SpellDef> GetSpellsByBranch(SpellBranch branch)
        {
            var result = new List<SpellDef>();
            foreach (var s in _spells.Values)
                if (s.branch == branch) result.Add(s);
            return result;
        }

        /// <summary>获取法术定义</summary>
        public static SpellDef GetSpell(string spellId)
        {
            _spells.TryGetValue(spellId, out var spell);
            return spell;
        }

        /// <summary>检查单位是否可以学习某法术（前置+属性+回路法师限制）</summary>
        public static bool CanLearn(Actor a, string spellId)
        {
            if (a == null || !a.hasTrait(SuperMechTraits.ClassMage)) return false;
            var spell = GetSpell(spellId);
            if (spell == null) return false;

            // 检查前置法术
            foreach (var pre in spell.prerequisites)
            {
                if (!IsLearned(a, pre)) return false;
            }

            // 检查属性要求
            float intVal = a.stats["intelligence"];
            float mysVal = SuperMechCustomStats.GetStat(a, SuperMechCustomStats.StatMystery);
            if (intVal < spell.intRequirement) return false;
            if (mysVal < spell.mysteryRequirement) return false;

            // 回路法师限制：非专精分支只能学tier<=1
            if (a.hasTrait(SuperMechMageType.TypeCircuit))
            {
                string spec = SuperMechMageType.GetSpecName(a);
                string branchName = BranchNameKeys[(int)spell.branch];
                bool isSpecMatch = IsSpecMatch(spec, spell.branch);
                if (!isSpecMatch && spell.tier > 1) return false;
            }

            return true;
        }

        private static bool IsSpecMatch(string specName, SpellBranch branch)
        {
            if (string.IsNullOrEmpty(specName)) return false;
            // 简化匹配：专精名包含分支关键词
            if (branch == SpellBranch.Element && specName.Contains("元素")) return true;
            if (branch == SpellBranch.Arcane && specName.Contains("秘术")) return true;
            if (branch == SpellBranch.Summon && specName.Contains("召唤")) return true;
            return false;
        }

        /// <summary>获取学习成功率（取决于智力和神秘属性+回路法师专精加成）</summary>
        public static float GetLearnSuccessRate(Actor a, string spellId)
        {
            var spell = GetSpell(spellId);
            if (spell == null) return 0f;

            float rate = spell.baseSuccessRate;
            float intVal = a.stats["intelligence"];
            float mysVal = SuperMechCustomStats.GetStat(a, SuperMechCustomStats.StatMystery);

            // 智力和神秘加成：每点+0.5%
            rate += intVal * 0.005f;
            rate += mysVal * 0.005f;

            // 回路法师专精分支+30%
            if (a.hasTrait(SuperMechMageType.TypeCircuit))
            {
                string spec = SuperMechMageType.GetSpecName(a);
                if (IsSpecMatch(spec, spell.branch)) rate += 0.3f;
            }

            // 高阶法师学低级法术更容易
            int mageStage = SuperMechStage.GetStage(a);
            if (mageStage > spell.tier * 2) rate += 0.2f;

            return Mathf.Clamp01(rate);
        }

        /// <summary>尝试学习法术的下一阶段</summary>
        public static bool TryLearnPhase(Actor a, string spellId)
        {
            if (!CanLearn(a, spellId)) return false;
            var spell = GetSpell(spellId);
            if (spell == null) return false;

            var progress = GetProgress(a, spellId);
            if (progress.learned) return false;
            if (progress.currentPhase >= spell.totalPhases) return false;

            // 检查经验
            int xpCost = spell.xpCostPerPhase[progress.currentPhase];
            int currentXp = (int)SuperMechAwakened.GetXp(a);
            if (currentXp < xpCost) return false;

            // 消耗经验
            SuperMechAwakened.AddXp(a, -xpCost);

            // 判定成功率
            float rate = GetLearnSuccessRate(a, spellId);
            if (Random.value > rate)
            {
                // 学习失败：经验已消耗，需要重新尝试
                Debug.Log($"[超神机械师] {a.name} 学习法术 {spell.nameKey} 阶段{progress.currentPhase + 1} 失败（成功率{rate:F0%}）");
                return false;
            }

            // 学习成功
            progress.currentPhase++;
            if (progress.currentPhase >= spell.totalPhases)
            {
                progress.learned = true;
                Debug.Log($"[超神机械师] {a.name} 学会了法术 {spell.nameKey}");
                // 推送事件日志
                SuperMechEventLogger.LogSpellLearned(a, spell.nameKey);
            }
            return true;
        }

        /// <summary>检查法术是否已学会</summary>
        public static bool IsLearned(Actor a, string spellId)
        {
            var progress = GetProgress(a, spellId);
            return progress.learned;
        }

        /// <summary>获取法术学习进度</summary>
        public static int GetCurrentPhase(Actor a, string spellId)
        {
            return GetProgress(a, spellId).currentPhase;
        }

        /// <summary>获取单位已学会的法术数量</summary>
        public static int GetLearnedCount(Actor a)
        {
            if (a == null) return 0;
            long id = a.data.id;
            if (!_unitProgress.TryGetValue(id, out var dict)) return 0;
            int count = 0;
            foreach (var p in dict.Values)
                if (p.learned) count++;
            return count;
        }

        private static SpellProgress GetProgress(Actor a, string spellId)
        {
            long id = a.data.id;
            if (!_unitProgress.TryGetValue(id, out var dict))
            {
                dict = new Dictionary<string, SpellProgress>();
                _unitProgress[id] = dict;
            }
            if (!dict.TryGetValue(spellId, out var progress))
            {
                progress = new SpellProgress();
                dict[spellId] = progress;
            }
            return progress;
        }

        /// <summary>清理单位数据（切换世界时调用）</summary>
        public static void ClearAll()
        {
            _unitProgress.Clear();
        }
    }
}
