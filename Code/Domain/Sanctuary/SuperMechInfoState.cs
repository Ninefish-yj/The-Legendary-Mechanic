using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechInfoState
    {
        private static readonly Dictionary<long, int> _infoLevel = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _shieldCooldown = new Dictionary<long, float>();

        public const int MaxLevel = 5;

        public static bool HasInfoState(Actor a)
        {
            if (a == null) return false;
            return GetLevel(a) > 0;
        }

        public static int GetLevel(Actor a)
        {
            if (a == null) return 0;
            int v; _infoLevel.TryGetValue(a.id, out v); return v;
        }

        public static void SetLevel(Actor a, int level)
        {
            if (a == null) return;
            _infoLevel[a.id] = Mathf.Clamp(level, 0, MaxLevel);
        }

        public static void Upgrade(Actor a)
        {
            if (a == null) return;
            int lv = GetLevel(a);
            if (lv < MaxLevel)
            {
                SetLevel(a, lv + 1);
                var s = SuperMechStats.Of(a);
                if (s != null)
                {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.05f;
                    s["intelligence"] = (s["intelligence"]) + 10f;
                }
            }
        }

        public static float GetAttackMultiplier(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return 1f;
            return 1f + lv * 0.05f;
        }

        public static float GetWorldTreeBonus(Actor a, Actor target)
        {
            int lv = GetLevel(a);
            if (lv <= 0 || target == null) return 1f;
            string asset = target.asset?.id ?? "";
            if (asset.Contains("plant") || asset.Contains("tree") || asset.Contains("ent") ||
                target.hasTrait("plant") || target.hasTrait("tree_ent"))
            {
                return 1f + lv * 0.10f;
            }
            return 1f;
        }

        public static bool IsHighDimensional(Actor a)
        {
            return GetLevel(a) >= MaxLevel;
        }

        public static bool TryShield(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return false;
            float cd;
            if (_shieldCooldown.TryGetValue(a.id, out cd) && Time.time < cd) return false;
            float chance = lv * 0.10f;
            if (Random.value < chance)
            {
                _shieldCooldown[a.id] = Time.time + 3f;
                return true;
            }
            return false;
        }

        public static float GetDisturbDebuff(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return 1f;
            return 1f - lv * 0.03f;
        }

        public static bool CanSenseHidden(Actor a)
        {
            return GetLevel(a) >= 2;
        }

        public static bool TryConvert(Actor killer, Actor victim)
        {
            int lv = GetLevel(killer);
            if (lv <= 0) return false;
            if (victim == null) return false;
            int killerRank = SuperMechAdvancement.GetExactRankIndex(killer);
            int victimRank = SuperMechAdvancement.GetExactRankIndex(victim);
            if (victimRank >= killerRank) return false;
            float chance = lv * 0.03f;
            return Random.value < chance;
        }

        public static void TickInfoState()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                int lv = GetLevel(a);
                if (lv <= 0) continue;

                if (lv >= 3 && SuperMechAdvancement.GetExactRankIndex(a) >= 12)
                {
                    if (SuperMechDivinity.IsDivineAwakened(a) &&
                        SuperMechTranscendence.IsAdvancementTaskDone(a) &&
                        !SuperMechTranscendence.IsTranscended(a))
                    {
                        if (Random.value < 0.01f)
                        {
                            SuperMechTranscendence.AddLegacyPower(a, 1);
                        }
                    }
                }
            }
        }

        public static string GetStatusText(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return "";
            string text = $"sm_infostate_435";
            text += $"sm_infostate_436";
            text += $"sm_infostate_437";
            text += $"sm_infostate_438";
            text += $"sm_infostate_439";
            if (lv >= 2) text += "sm_infostate_440";
            if (lv >= 3) text += "sm_infostate_441";
            if (lv >= 4) text += "sm_infostate_442";
            if (lv >= 5) text += "sm_infostate_443";
            return text.Trim();
        }

        public static void Clear() { _infoLevel.Clear(); _shieldCooldown.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_infoLevel, alive);
            removed += SuperMechCleanup.CleanDict(_shieldCooldown, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _infoLevel.Remove(a.id);
            _shieldCooldown.Remove(a.id);
        }
    }
}
