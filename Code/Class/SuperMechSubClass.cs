using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechSubClass
    {
        public const string SubAgent       = "sm_sub_agent";
        public const string SubNinja      = "sm_sub_ninja";
        public const string SubHacker     = "sm_sub_hacker";
        public const string SubMerchant  = "sm_sub_merchant";
        public const string SubDoctor     = "sm_sub_doctor";
        public const string SubEngineer  = "sm_sub_engineer";
        public const string SubScribe     = "sm_sub_scribe";
        public const string SubScout      = "sm_sub_scout";

        public const int MaxSubLevel = 10;

        public static readonly Dictionary<long, Dictionary<string, float>> _subXp = new Dictionary<long, Dictionary<string, float>>();
        public static readonly Dictionary<long, Dictionary<string, int>> _subLevel = new Dictionary<long, Dictionary<string, int>>();
        public static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();

        public static readonly int[] LevelThresholds = { 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5500, 7500 };

        public static string[] AllSubClasses = {
            SubAgent, SubNinja, SubHacker, SubMerchant, SubDoctor, SubEngineer, SubScribe, SubScout
        };

        public static void Register()
        {
            AddSubClass(SubAgent,     "sm_subclass_064",     0, 0, 0.05f,  "sm_subclass_065");
            AddSubClass(SubNinja,    "sm_subclass_066", 0, 0, 0.08f,  "sm_subclass_067");
            AddSubClass(SubHacker,   "sm_subclass_068",     5, 0, 0f,       "sm_subclass_069");
            AddSubClass(SubMerchant, "sm_subclass_070",     2, 0, 0f,       "sm_subclass_071");
            AddSubClass(SubDoctor,   "sm_subclass_072",     3, 0, 0f,       "sm_subclass_073");
            AddSubClass(SubEngineer, "sm_subclass_074",   4, 0, 0.05f,    "sm_subclass_075");
            AddSubClass(SubScribe,   "sm_subclass_076",     6, 0, 0f,       "sm_subclass_077");
            AddSubClass(SubScout,    "sm_subclass_078",   0, 2, 0.03f,    "sm_subclass_079");


        }

        public static void TickSubLevels()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                bool inCombat = curHealth < lastH - 0.5f || SuperMechQi.IsInCombat(a);
                _lastHealth[a.id] = curHealth;

                if (!inCombat) continue;

                foreach (string subId in AllSubClasses)
                {
                    if (!a.hasTrait(subId)) continue;

                    float xpGain = (2f + SuperMechQi.GetLevel(SuperMechQi.GetQi(a)) * 0.3f) * tickInterval;
                    AddSubXp(a, subId, xpGain);
                }
            }
        }

        public static void AddSubXp(Actor a, string subId, float xp)
        {
            if (a == null) return;
            Dictionary<string, float> xpMap;
            if (!_subXp.TryGetValue(a.id, out xpMap))
            {
                xpMap = new Dictionary<string, float>();
                _subXp[a.id] = xpMap;
            }
            float curXp;
            xpMap.TryGetValue(subId, out curXp);
            xpMap[subId] = curXp + xp;

            int curLv = GetSubLevel(a, subId);
            if (curLv < MaxSubLevel && xpMap[subId] >= LevelThresholds[curLv])
            {
                SetSubLevel(a, subId, curLv + 1);
                if (SuperMechConfig.LogVerbose)
            }
        }

        public static int GetSubLevel(Actor a, string subId)
        {
            if (a == null) return 0;
            Dictionary<string, int> lvMap;
            if (_subLevel.TryGetValue(a.id, out lvMap))
            {
                int lv;
                if (lvMap.TryGetValue(subId, out lv)) return lv;
            }
            return a.hasTrait(subId) ? 1 : 0;
        }

        private static void SetSubLevel(Actor a, string subId, int level)
        {
            Dictionary<string, int> lvMap;
            if (!_subLevel.TryGetValue(a.id, out lvMap))
            {
                lvMap = new Dictionary<string, int>();
                _subLevel[a.id] = lvMap;
            }
            lvMap[subId] = level;

            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                if (subId == SubAgent || subId == SubNinja || subId == SubScout)
                {
                    stats["damage"] = (stats["damage"]) + 2f;
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.01f;
                }
                else if (subId == SubHacker || subId == SubScribe)
                {
                    stats["intelligence"] = (stats["intelligence"]) + 2f;
                    stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.02f;
                }
                else if (subId == SubDoctor)
                {
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) + 0.03f;
                }
                else if (subId == SubEngineer)
                {
                    stats["intelligence"] = (stats["intelligence"]) + 1f;
                    stats["armor"] = (stats["armor"]) + 1f;
                }
                else
                {
                    stats["intelligence"] = (stats["intelligence"]) + 1f;
                }
            }
        }

        public static string GetSubLevelText(Actor a)
        {
            if (a == null) return "";
            var parts = new List<string>();
            foreach (string subId in AllSubClasses)
            {
                if (a.hasTrait(subId))
                {
                    int lv = GetSubLevel(a, subId);
                    string name = LocalizedTextManager.getText("trait_" + subId);
                    parts.Add($"{name}Lv{lv}");
                }
            }
            return string.Join(" ", parts);
        }

        private static void AddSubClass(string id, string name, int intell, int dmgAdd, float dmgMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconAmbitious", group_id = "sm_subclass",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgAdd > 0) t.base_stats["damage"] = dmgAdd;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            AssetManager.traits.add(t);
        }

        public static void Clear()
        {
            _subXp.Clear();
            _subLevel.Clear();
            _lastHealth.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_subLevel, alive);
            return removed;
        }
    }
}
