using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechEquipAffix
    {
        public class AffixDef
        {
            public string id;
            public string name;
            public string statKey;
            public float minValue;
            public float maxValue;
            public bool isMultiplier;
            public int minQuality;
        }

        public static readonly List<AffixDef> _affixPool = new List<AffixDef>
        {
            new AffixDef { id="affix_dmg", name="sm_equipaffix_737", statKey="multiplier_damage", minValue=0.05f, maxValue=0.25f, isMultiplier=true, minQuality=0 },
            new AffixDef { id="affix_hp", name="sm_equipaffix_738", statKey="multiplier_health", minValue=0.05f, maxValue=0.30f, isMultiplier=true, minQuality=0 },
            new AffixDef { id="affix_speed", name="sm_equipaffix_739", statKey="multiplier_speed", minValue=0.03f, maxValue=0.15f, isMultiplier=true, minQuality=1 },
            new AffixDef { id="affix_armor", name="sm_equipaffix_740", statKey="armor", minValue=5f, maxValue=20f, isMultiplier=false, minQuality=1 },
            new AffixDef { id="affix_crit", name="sm_equipaffix_741", statKey="multiplier_crit", minValue=0.03f, maxValue=0.12f, isMultiplier=true, minQuality=2 },
            new AffixDef { id="affix_stamina", name="sm_equipaffix_742", statKey="multiplier_stamina", minValue=0.05f, maxValue=0.20f, isMultiplier=true, minQuality=2 },
            new AffixDef { id="affix_dmg_fixed", name="sm_equipaffix_743", statKey="damage", minValue=1f, maxValue=10f, isMultiplier=false, minQuality=0 },
            new AffixDef { id="affix_hp_fixed", name="sm_equipaffix_744", statKey="health", minValue=5f, maxValue=50f, isMultiplier=false, minQuality=0 },
            new AffixDef { id="affix_qi", name="sm_equipaffix_745", statKey="sm_qi_max", minValue=100f, maxValue=1000f, isMultiplier=false, minQuality=4 },
            new AffixDef { id="affix_int", name="sm_equipaffix_746", statKey="intelligence", minValue=1f, maxValue=5f, isMultiplier=false, minQuality=3 },
            new AffixDef { id="affix_exp", name="sm_equipaffix_747", statKey="experience", minValue=0.05f, maxValue=0.20f, isMultiplier=true, minQuality=3 },
            new AffixDef { id="affix_all", name="sm_equipaffix_748", statKey="multiplier_damage", minValue=0.03f, maxValue=0.10f, isMultiplier=true, minQuality=5 },
        };

        public static readonly Dictionary<long, List<EquipAffixInstance>> _equippedAffixes = new Dictionary<long, List<EquipAffixInstance>>();

        // ========== v0.185 装备强化系统 ==========
        /// <summary>装备强化等级：actorId -> 强化等级(0-10)</summary>
        private static readonly Dictionary<long, int> _enhanceLevels = new Dictionary<long, int>();
        public const int MaxEnhanceLevel = 10;
        /// <summary>强化基础消耗（每级递增）</summary>
        public const int EnhanceBaseCost = 200;

        public class EquipAffixInstance
        {
            public string affixId;
            public string name;
            public float value;
            public string statKey;
            public bool isMultiplier;
        }

        /// <summary>获取装备强化等级</summary>
        public static int GetEnhanceLevel(Actor a)
        {
            if (a == null) return 0;
            return _enhanceLevels.TryGetValue(a.id, out int lv) ? lv : 0;
        }

        /// <summary>获取强化消耗（每级×1.5递增）</summary>
        public static int GetEnhanceCost(Actor a)
        {
            int lv = GetEnhanceLevel(a);
            return Mathf.RoundToInt(EnhanceBaseCost * Mathf.Pow(1.5f, lv));
        }

        /// <summary>获取强化成功率（+1=90%, +10=10%，线性递减）</summary>
        public static float GetEnhanceSuccessRate(Actor a)
        {
            int lv = GetEnhanceLevel(a);
            return Mathf.Max(0.1f, 1.0f - lv * 0.08f);
        }

        /// <summary>强化装备（消耗伊纳尔，成功率递减，失败降级）</summary>
        public static bool EnhanceEquip(Actor a)
        {
            if (a == null) return false;
            int lv = GetEnhanceLevel(a);
            if (lv >= MaxEnhanceLevel) return false;
            int cost = GetEnhanceCost(a);
            if (!SuperMechInal.SpendInal(a, cost)) return false;

            float rate = GetEnhanceSuccessRate(a);
            if (Random.value > rate)
            {
                // 失败：50%概率降级（+5以上才会降级）
                if (lv >= 5 && Random.value < 0.5f)
                {
                    _enhanceLevels[a.id] = lv - 1;
                    Debug.Log($"[超神机械师] 装备强化失败，降级到+{lv - 1}");
                }
                else
                {
                    Debug.Log($"[超神机械师] 装备强化失败（成功率{rate:F0%}）");
                }
                return false;
            }

            _enhanceLevels[a.id] = lv + 1;
            Debug.Log($"[超神机械师] 装备强化成功：+{lv + 1}");
            ApplyAffixes(a);
            return true;
        }

        /// <summary>获取强化属性加成（每级+5%全词缀数值）</summary>
        public static float GetEnhanceMultiplier(Actor a)
        {
            return 1f + GetEnhanceLevel(a) * 0.05f;
        }

        public static List<EquipAffixInstance> RollAffixes(int qualityLevel)
        {
            var result = new List<EquipAffixInstance>();
            int affixCount = qualityLevel switch
            {
                0 or 1 => 1,
                2 or 3 => 2,
                4 or 5 => 3,
                6 or 7 => 4,
                8 => 5,
                _ => 1
            };

            var available = new List<AffixDef>();
            foreach (var affix in _affixPool)
            {
                if (affix.minQuality <= qualityLevel) available.Add(affix);
            }

            var selected = new HashSet<string>();
            for (int i = 0; i < affixCount && available.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                var def = available[idx];
                if (selected.Contains(def.id)) continue;
                selected.Add(def.id);

                float value = UnityEngine.Random.Range(def.minValue, def.maxValue);
                value *= 1f + qualityLevel * 0.1f;

                result.Add(new EquipAffixInstance
                {
                    affixId = def.id,
                    name = def.name,
                    value = value,
                    statKey = def.statKey,
                    isMultiplier = def.isMultiplier
                });
            }
            return result;
        }

        public static void OnEquip(Actor a, int qualityLevel)
        {
            if (a == null) return;
            var affixes = RollAffixes(qualityLevel);
            _equippedAffixes[a.id] = affixes;
            ApplyAffixes(a);
        }

        public static void OnUnequip(Actor a)
        {
            if (a == null) return;
            _equippedAffixes.Remove(a.id);
        }

        public static void ApplyAffixes(Actor a)
        {
            if (a == null) return;
            ApplyAffixesToStats(a, SuperMechStats.Of(a));
        }

        /// <summary>应用词缀到指定stats（由CombatBonusCalculator在updateStats Postfix中调用）</summary>
        public static void ApplyAffixesToStats(Actor a, BaseStats stats)
        {
            if (a == null || stats == null) return;
            if (!_equippedAffixes.TryGetValue(a.id, out var affixes)) return;

            float modifyMult = SuperMechEquipModify.GetAffixMultiplier(a);
            float enhanceMult = GetEnhanceMultiplier(a); // v0.185 强化加成

            foreach (var affix in affixes)
            {
                float value = affix.value * modifyMult * enhanceMult;
                if (affix.isMultiplier)
                {
                    float cur = stats[affix.statKey];
                    if (cur <= 0f) cur = 1f;
                    stats[affix.statKey] = cur * (1f + value);
                }
                else
                {
                    stats[affix.statKey] = stats[affix.statKey] + value;
                }
            }
        }

        public static List<EquipAffixInstance> GetAffixes(Actor a)
        {
            if (a != null && _equippedAffixes.TryGetValue(a.id, out var affixes))
                return affixes;
            return new List<EquipAffixInstance>();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_equippedAffixes, alive);
        }

        public static void Clear() { _equippedAffixes.Clear(); }
    }
}
