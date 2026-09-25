using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备词条系统：装备生成时随机roll词条，影响属性。
    /// 原著：装备有随机属性（如"攻击力+15%""暴击率+5%"），高品质装备词条更多。
    /// 参考天人武道神藏系统的"基因位点"设计。
    /// </summary>
    public static class SuperMechEquipAffix
    {
        /// <summary>词条定义。</summary>
        public class AffixDef
        {
            public string id;
            public string name;
            public string statKey;   // 对应BaseStats的key
            public float minValue;   // 最小值（百分比或固定值）
            public float maxValue;   // 最大值
            public bool isMultiplier; // true=倍率, false=固定值
            public int minQuality;   // 最低品质要求（0-8）
        }

        /// <summary>词条池。</summary>
        private static readonly List<AffixDef> _affixPool = new List<AffixDef>
        {
            // 基础词条（所有品质可出）
            new AffixDef { id="affix_dmg", name="攻击", statKey="multiplier_damage", minValue=0.05f, maxValue=0.25f, isMultiplier=true, minQuality=0 },
            new AffixDef { id="affix_hp", name="生命", statKey="multiplier_health", minValue=0.05f, maxValue=0.30f, isMultiplier=true, minQuality=0 },
            new AffixDef { id="affix_speed", name="攻速", statKey="multiplier_speed", minValue=0.03f, maxValue=0.15f, isMultiplier=true, minQuality=1 },
            new AffixDef { id="affix_armor", name="护甲", statKey="multiplier_armor", minValue=0.05f, maxValue=0.20f, isMultiplier=true, minQuality=1 },
            new AffixDef { id="affix_crit", name="暴击", statKey="multiplier_crit", minValue=0.03f, maxValue=0.12f, isMultiplier=true, minQuality=2 },
            new AffixDef { id="affix_stamina", name="耐力", statKey="multiplier_stamina", minValue=0.05f, maxValue=0.20f, isMultiplier=true, minQuality=2 },
            new AffixDef { id="affix_dmg_fixed", name="攻击强化", statKey="damage", minValue=1f, maxValue=10f, isMultiplier=false, minQuality=0 },
            new AffixDef { id="affix_hp_fixed", name="生命强化", statKey="health", minValue=5f, maxValue=50f, isMultiplier=false, minQuality=0 },
            // 高级词条（高品质专属）
            new AffixDef { id="affix_qi", name="气力增幅", statKey="sm_qi_max", minValue=100f, maxValue=1000f, isMultiplier=false, minQuality=4 },
            new AffixDef { id="affix_int", name="智力", statKey="intelligence", minValue=1f, maxValue=5f, isMultiplier=false, minQuality=3 },
            new AffixDef { id="affix_exp", name="经验获取", statKey="experience", minValue=0.05f, maxValue=0.20f, isMultiplier=true, minQuality=3 },
            new AffixDef { id="affix_all", name="全属性", statKey="multiplier_damage", minValue=0.03f, maxValue=0.10f, isMultiplier=true, minQuality=5 },
        };

        /// <summary>装备实例词条（key = actorId, value = 词条列表）。</summary>
        private static readonly Dictionary<long, List<EquipAffixInstance>> _equippedAffixes = new Dictionary<long, List<EquipAffixInstance>>();

        public class EquipAffixInstance
        {
            public string affixId;
            public string name;
            public float value;
            public string statKey;
            public bool isMultiplier;
        }

        /// <summary>根据品质roll词条。</summary>
        public static List<EquipAffixInstance> RollAffixes(int qualityLevel)
        {
            var result = new List<EquipAffixInstance>();
            // 品质决定词条数量：0-1级=1条, 2-3级=2条, 4-5级=3条, 6-7级=4条, 8级=5条
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

            // 随机选取不重复的词条
            var selected = new HashSet<string>();
            for (int i = 0; i < affixCount && available.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                var def = available[idx];
                if (selected.Contains(def.id)) continue;
                selected.Add(def.id);

                float value = UnityEngine.Random.Range(def.minValue, def.maxValue);
                // 高品质词条数值更高
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

        /// <summary>单位装备时roll词条并存储。</summary>
        public static void OnEquip(Actor a, int qualityLevel)
        {
            if (a == null) return;
            var affixes = RollAffixes(qualityLevel);
            _equippedAffixes[a.id] = affixes;
            ApplyAffixes(a);
        }

        /// <summary>单位卸下装备时清除词条。</summary>
        public static void OnUnequip(Actor a)
        {
            if (a == null) return;
            _equippedAffixes.Remove(a.id);
        }

        /// <summary>应用词条属性到单位。</summary>
        public static void ApplyAffixes(Actor a)
        {
            if (a == null || !_equippedAffixes.TryGetValue(a.id, out var affixes)) return;
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            foreach (var affix in affixes)
            {
                if (affix.isMultiplier)
                {
                    if (stats.ContainsKey(affix.statKey))
                        stats[affix.statKey] = (float)stats[affix.statKey] * (1f + affix.value);
                    else
                        stats[affix.statKey] = 1f + affix.value;
                }
                else
                {
                    if (stats.ContainsKey(affix.statKey))
                        stats[affix.statKey] = (float)stats[affix.statKey] + affix.value;
                    else
                        stats[affix.statKey] = affix.value;
                }
            }
        }

        /// <summary>获取单位当前装备的词条。</summary>
        public static List<EquipAffixInstance> GetAffixes(Actor a)
        {
            if (a != null && _equippedAffixes.TryGetValue(a.id, out var affixes))
                return affixes;
            return new List<EquipAffixInstance>();
        }

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_equippedAffixes, alive);
        }

        /// <summary>清空。</summary>
        public static void Clear() { _equippedAffixes.Clear(); }
    }
}
