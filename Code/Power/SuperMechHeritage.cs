using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechHeritage
    {
        private static readonly Dictionary<long, float> _heritage = new Dictionary<long, float>();
        private static readonly Dictionary<long, int> _autoUnlocked = new Dictionary<long, int>();

        private static float GetCost(int unlockedCount)
        {
            return 50f * Mathf.Pow(1.3f, unlockedCount);
        }

        public static float GetHeritage(Actor a)
        {
            if (a == null) return 0;
            float v; _heritage.TryGetValue(a.id, out v); return v;
        }

        public static void AddHeritage(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetHeritage(a);
            _heritage[a.id] = cur + amount;
        }

        public static void SetHeritage(Actor a, float value)
        {
            if (a == null) return;
            _heritage[a.id] = Mathf.Max(0, value);
        }

        public static void TickHeritage()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue;

                float growth = 0.5f * tickInterval;

                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                growth *= (1f + rankIdx * 0.15f);

                if (SuperMechQi.IsInCombat(a)) growth *= 3f;

                if (a.hasTrait("sm_refinement")) growth *= 1.5f;

                AddHeritage(a, growth);

                AutoGrowStats(a, growth);

                int unlocked;
                _autoUnlocked.TryGetValue(a.id, out unlocked);
                float cost = GetCost(unlocked);
                float h = GetHeritage(a);

                while (h >= cost && unlocked < 50)
                {
                    h -= cost;
                    unlocked++;
                    string cls = SuperMechBranch.GetClass(a);
                    string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
                    string nodeId = $"{prefix}_{unlocked}";
                    SuperMechPotential.ForceUnlockNode(a, nodeId);
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        stats["intelligence"] = (stats["intelligence"]) + 0.5f;
                        stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.01f;
                    }
                    _heritage[a.id] = h;
                    _autoUnlocked[a.id] = unlocked;
                    cost = GetCost(unlocked);
                }
            }
        }

        public static int GetAutoUnlockedCount(Actor a)
        {
            if (a == null) return 0;
            int v; _autoUnlocked.TryGetValue(a.id, out v); return v;
        }

        public static void Clear() { _heritage.Clear(); _autoUnlocked.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_heritage, alive);
            removed += SuperMechCleanup.CleanDict(_autoUnlocked, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _heritage.Remove(a.id);
            _autoUnlocked.Remove(a.id);
        }

        private static void AutoGrowStats(Actor a, float growthAmount)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            string cls = SuperMechBranch.GetClass(a);
            string branch = SuperMechBranch.GetBranchTrait(a);
            if (string.IsNullOrEmpty(cls)) return;

            float rate = growthAmount * 0.002f;

            switch (cls)
            {
                case "sm_heritage_376":
                    stats["damage"] = (stats["damage"]) + rate * 1.5f;
                    stats["warfare"] = (stats["warfare"]) + rate * 1.2f;
                    stats["health"] = (stats["health"]) + rate * 2f;
                    stats["stamina"] = (stats["stamina"]) + rate * 1.5f;
                    stats["armor"] = (stats["armor"]) + rate * 0.5f;
                    stats["speed"] = (stats["speed"]) + rate * 0.3f;
                    // 武道系无固定分支，通用加成
                    stats["damage"] = (stats["damage"]) + rate * 0.6f;
                    stats["health"] = (stats["health"]) + rate * 0.6f;
                    break;

                case "sm_heritage_377":
                    stats["intelligence"] = (stats["intelligence"]) + rate * 2f;
                    stats["health"] = (stats["health"]) + rate * 1f;
                    stats["stamina"] = (stats["stamina"]) + rate * 0.8f;
                    stats["damage"] = (stats["damage"]) + rate * 0.5f;
                    if (branch == SuperMechBranch.BranchGunner)
                    {
                        stats["damage"] = (stats["damage"]) + rate * 1.5f;
                        stats["range"] = (stats["range"]) + rate * 0.5f;
                    }
                    else if (branch == SuperMechBranch.BranchMech)
                    {
                        stats["intelligence"] = (stats["intelligence"]) + rate * 1f;
                        stats["health"] = (stats["health"]) + rate * 0.5f;
                    }
                    else if (branch == SuperMechBranch.BranchMartial)
                    {
                        stats["damage"] = (stats["damage"]) + rate * 1f;
                        stats["speed"] = (stats["speed"]) + rate * 0.8f;
                        stats["armor"] = (stats["armor"]) + rate * 0.5f;
                    }
                    break;

                case "sm_heritage_378":
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.8f;
                    stats["mana"] = (stats["mana"]) + rate * 1.5f;
                    stats["damage"] = (stats["damage"]) + rate * 0.8f;
                    break;

                case "sm_heritage_379":
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.5f;
                    stats["mana"] = (stats["mana"]) + rate * 2f;
                    stats["health"] = (stats["health"]) + rate * 0.8f;
                    break;

                case "sm_heritage_380":
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.5f;
                    stats["mana"] = (stats["mana"]) + rate * 1.5f;
                    stats["speed"] = (stats["speed"]) + rate * 0.8f;
                    break;
            }
        }
    }
}
