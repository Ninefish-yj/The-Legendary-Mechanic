using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>子职业系统：职业分支下的细分路线（独立数据存储，不注册为特质）</summary>
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

        public class SubClassDef
        {
            public string id;
            public string nameKey;
            public string descKey;
            public int intBonus;
            public int dmgAdd;
            public float dmgMul;
        }

        private static readonly Dictionary<string, SubClassDef> _defs = new Dictionary<string, SubClassDef>();
        private static readonly Dictionary<long, HashSet<string>> _unitSubClasses = new Dictionary<long, HashSet<string>>();
        public static readonly Dictionary<long, Dictionary<string, float>> _subXp = new Dictionary<long, Dictionary<string, float>>();
        public static readonly Dictionary<long, Dictionary<string, int>> _subLevel = new Dictionary<long, Dictionary<string, int>>();
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();

        public static readonly int[] LevelThresholds = { 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5500, 7500 };

        public static string[] AllSubClasses = {
            SubAgent, SubNinja, SubHacker, SubMerchant, SubDoctor, SubEngineer, SubScribe, SubScout
        };

        public static void Register()
        {
            AddDef(SubAgent,     "sm_subclass_064",     0, 0, 0.05f,  "sm_subclass_065");
            AddDef(SubNinja,    "sm_subclass_066", 0, 0, 0.08f,  "sm_subclass_067");
            AddDef(SubHacker,   "sm_subclass_068",     5, 0, 0f,       "sm_subclass_069");
            AddDef(SubMerchant, "sm_subclass_070",     2, 0, 0f,       "sm_subclass_071");
            AddDef(SubDoctor,   "sm_subclass_072",     3, 0, 0f,       "sm_subclass_073");
            AddDef(SubEngineer, "sm_subclass_074",   4, 0, 0.05f,    "sm_subclass_075");
            AddDef(SubScribe,   "sm_subclass_076",     6, 0, 0f,       "sm_subclass_077");
            AddDef(SubScout,    "sm_subclass_078",   0, 2, 0.03f,    "sm_subclass_079");
        }

        private static void AddDef(string id, string nameKey, int intell, int dmgAdd, float dmgMul, string descKey)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(nameKey), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(descKey), pReplace: true);
            _defs[id] = new SubClassDef { id = id, nameKey = nameKey, descKey = descKey, intBonus = intell, dmgAdd = dmgAdd, dmgMul = dmgMul };
        }

        public static SubClassDef GetDef(string id)
        {
            return _defs.TryGetValue(id, out var def) ? def : null;
        }

        public static bool HasSubClass(Actor a, string subId)
        {
            if (a == null) return false;
            return _unitSubClasses.TryGetValue(a.id, out var set) && set.Contains(subId);
        }

        public static void AddSubClass(Actor a, string subId)
        {
            if (a == null || string.IsNullOrEmpty(subId)) return;
            if (!_unitSubClasses.TryGetValue(a.id, out var set))
            {
                set = new HashSet<string>();
                _unitSubClasses[a.id] = set;
            }
            set.Add(subId);
        }

        public static void RemoveSubClass(Actor a, string subId)
        {
            if (a == null) return;
            if (_unitSubClasses.TryGetValue(a.id, out var set))
                set.Remove(subId);
        }

        public static List<string> GetSubClasses(Actor a)
        {
            if (a == null) return new List<string>();
            if (_unitSubClasses.TryGetValue(a.id, out var set))
                return new List<string>(set);
            return new List<string>();
        }

        /// <summary>应用子职业基础属性加成（在CombatBonusCalculator中调用）</summary>
        public static void ApplyBaseBonus(Actor a, BaseStats stats)
        {
            if (a == null || stats == null) return;
            if (!_unitSubClasses.TryGetValue(a.id, out var set)) return;
            foreach (var subId in set)
            {
                if (!_defs.TryGetValue(subId, out var def)) continue;
                if (def.intBonus > 0) stats["intelligence"] += def.intBonus;
                if (def.dmgAdd > 0) stats["damage"] += def.dmgAdd;
                if (def.dmgMul > 0) stats["multiplier_damage"] = (stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"]) * (1f + def.dmgMul);
            }
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
                    if (!HasSubClass(a, subId)) continue;

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
            return HasSubClass(a, subId) ? 1 : 0;
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
                if (HasSubClass(a, subId))
                {
                    int lv = GetSubLevel(a, subId);
                    string name = LocalizedTextManager.getText("trait_" + subId);
                    parts.Add($"{name}Lv{lv}");
                }
            }
            return string.Join(" ", parts);
        }

        public static void Clear()
        {
            _unitSubClasses.Clear();
            _subXp.Clear();
            _subLevel.Clear();
            _lastHealth.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_unitSubClasses, alive);
            removed += SuperMechCleanup.CleanDict(_subLevel, alive);
            return removed;
        }
    }
}
