using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechDimension
    {
        public class DimensionDef
        {
            public string id;
            public string name;
            public string desc;
            public string classTrait;
            public System.Action<BaseStats> applyBuff;
            public System.Action<BaseStats> removeBuff;
            public int minRankIndex = 8;
        }

        public static readonly List<DimensionDef> Dimensions = new List<DimensionDef>
        {
            new DimensionDef
            {
                id = "void", name = "sm_dimension_364", desc = "sm_dimension_365",
                classTrait = SuperMechTraits.ClassMind,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.3f;
                    s["speed"] = (s["speed"]) + 0.5f;
                    s["intelligence"] = (s["intelligence"]) + 10f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.3f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["speed"] = (s["speed"]) - 0.5f;
                    s["intelligence"] = (s["intelligence"]) - 10f;
                }
            },
            new DimensionDef
            {
                id = "underworld", name = "sm_dimension_366", desc = "sm_dimension_367",
                classTrait = SuperMechTraits.ClassPsi,
                applyBuff = s => {
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.4f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.25f;
                    s["stamina"] = (s["stamina"]) + 30f;
                },
                removeBuff = s => {
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / 1.4f;
                    if (Mathf.Approximately(s["multiplier_health"], 1f)) s["multiplier_health"] = 0f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.25f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["stamina"] = (s["stamina"]) - 30f;
                }
            },
            new DimensionDef
            {
                id = "arcane", name = "sm_dimension_368", desc = "sm_dimension_369",
                classTrait = SuperMechTraits.ClassMage,
                applyBuff = s => {
                    s["intelligence"] = (s["intelligence"]) + 20f;
                    s["mana"] = (s["mana"]) + 50f;
                    s["damage"] = (s["damage"]) + 5f;
                    s["health"] = (s["health"]) + 15f;
                },
                removeBuff = s => {
                    s["intelligence"] = (s["intelligence"]) - 20f;
                    s["mana"] = (s["mana"]) - 50f;
                    s["damage"] = (s["damage"]) - 5f;
                    s["health"] = (s["health"]) - 15f;
                }
            },
            new DimensionDef
            {
                id = "hangar", name = "sm_dimension_370", desc = "sm_dimension_371",
                classTrait = SuperMechTraits.ClassMech,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.3f;
                    s["intelligence"] = (s["intelligence"]) + 15f;
                    s["experience"] = ((s["experience"] == 0f ? 1f : s["experience"])) * 1.5f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.3f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["intelligence"] = (s["intelligence"]) - 15f;
                    s["experience"] = ((s["experience"] == 0f ? 1f : s["experience"])) / 1.5f;
                    if (Mathf.Approximately(s["experience"], 1f)) s["experience"] = 0f;
                }
            },
            new DimensionDef
            {
                id = "battle", name = "sm_dimension_372", desc = "sm_dimension_373",
                classTrait = SuperMechTraits.ClassMartial,
                applyBuff = s => {
                    s["attack_speed"] = (s["attack_speed"]) + 0.3f;
                    s["critical_chance"] = (s["critical_chance"]) + 0.15f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.2f;
                },
                removeBuff = s => {
                    s["attack_speed"] = (s["attack_speed"]) - 0.3f;
                    s["critical_chance"] = (s["critical_chance"]) - 0.15f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.2f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                }
            },
            new DimensionDef
            {
                id = "infostate", name = "sm_dimension_374", desc = "sm_dimension_375",
                classTrait = null,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.1f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.1f;
                    s["intelligence"] = (s["intelligence"]) + 15f;
                    s["armor_penetration"] = (s["armor_penetration"]) + 0.15f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_health"], 1f)) s["multiplier_health"] = 0f;
                    s["intelligence"] = (s["intelligence"]) - 15f;
                    s["armor_penetration"] = (s["armor_penetration"]) - 0.15f;
                }
            },
        };

        public static readonly Dictionary<long, string> _activeDimension = new Dictionary<long, string>();
        public static readonly Dictionary<long, long> _buffEndTime = new Dictionary<long, long>();
        public static readonly Dictionary<long, long> _cooldown = new Dictionary<long, long>();
        public const float BuffDurationSeconds = 120f;
        public const float CooldownSeconds = 300f;

        public static DimensionDef GetDef(string id)
        {
            foreach (var d in Dimensions) if (d.id == id) return d;
            return null;
        }

        public static string GetActiveDimension(Actor a)
        {
            if (a == null) return null;
            if (_activeDimension.TryGetValue(a.id, out string dim)) return dim;
            return null;
        }

        public static bool CanEnter(Actor a, DimensionDef dim)
        {
            if (a == null || dim == null) return false;
            if (_activeDimension.ContainsKey(a.id)) return false;
            if (dim.classTrait != null && !a.hasTrait(dim.classTrait)) return false;
            if (SuperMechAdvancement.GetRankIndex(a) < dim.minRankIndex) return false;
            if (_cooldown.TryGetValue(a.id, out long cdEnd))
            {
                long now = System.DateTime.Now.Ticks;
                if (now < cdEnd) return false;
            }
            return true;
        }

        public static bool Enter(Actor a, DimensionDef dim)
        {
            if (!CanEnter(a, dim)) return false;
            var stats = SuperMechStats.Of(a);
            if (stats != null) dim.applyBuff(stats);
            long now = System.DateTime.Now.Ticks;
            _activeDimension[a.id] = dim.id;
            _buffEndTime[a.id] = now + System.TimeSpan.FromSeconds(BuffDurationSeconds).Ticks;
            _cooldown[a.id] = now + System.TimeSpan.FromSeconds(CooldownSeconds).Ticks;
            if (SuperMechConfig.LogVerbose)
            return true;
        }

        public static List<DimensionDef> GetAvailableDimensions(Actor a)
        {
            var list = new List<DimensionDef>();
            if (a == null) return list;
            foreach (var dim in Dimensions)
            {
                if (dim.classTrait == null || a.hasTrait(dim.classTrait)) list.Add(dim);
            }
            return list;
        }

        public static void TickDimensionBuffs()
        {
            if (World.world == null || World.world.units == null) return;
            long now = System.DateTime.Now.Ticks;
            var expired = new List<long>();
            foreach (var kv in _buffEndTime)
            {
                if (now >= kv.Value) expired.Add(kv.Key);
            }
            foreach (long id in expired)
            {
                Actor a = null;
                foreach (var u in World.world.units) if (u != null && u.id == id) { a = u; break; }
                if (a != null && _activeDimension.TryGetValue(id, out string dimId))
                {
                    var dim = GetDef(dimId);
                    var stats = SuperMechStats.Of(a);
                    if (dim != null && stats != null) dim.removeBuff(stats);
                    if (SuperMechConfig.LogVerbose)
                }
                _activeDimension.Remove(id);
                _buffEndTime.Remove(id);
            }
        }

        public static void Clear()
        {
            _activeDimension.Clear();
            _buffEndTime.Clear();
            _cooldown.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_activeDimension, alive);
            removed += SuperMechCleanup.CleanDict(_buffEndTime, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }
    }
}
