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

        private static readonly List<AffixDef> _affixPool = new List<AffixDef>
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

        private static readonly Dictionary<long, List<EquipAffixInstance>> _equippedAffixes = new Dictionary<long, List<EquipAffixInstance>>();

        public class EquipAffixInstance
        {
            public string affixId;
            public string name;
            public float value;
            public string statKey;
            public bool isMultiplier;
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
            if (a == null || !_equippedAffixes.TryGetValue(a.id, out var affixes)) return;
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            foreach (var affix in affixes)
            {
                if (affix.isMultiplier)
                {
                    float cur = stats[affix.statKey];
                    if (cur <= 0f) cur = 1f;
                    stats[affix.statKey] = cur * (1f + affix.value);
                }
                else
                {
                    stats[affix.statKey] = stats[affix.statKey] + affix.value;
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
