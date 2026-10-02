using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechMageTower
    {
        public const string Tower1 = "sm_mage_tower_1";
        public const string Tower2 = "sm_mage_tower_2";
        public const string Tower3 = "sm_mage_tower_3";
        public const string Tower4 = "sm_mage_tower_4";
        public const string Tower5 = "sm_mage_tower_5";

        public static readonly string[] TowerIds = { Tower1, Tower2, Tower3, Tower4, Tower5 };
        public static readonly string[] TowerNames = {
            "sm_magetower_357", "sm_magetower_358", "sm_magetower_359", "sm_magetower_360", "sm_magetower_361"
        };
        public struct TowerBonusInfo { public float dmg, hp, mana; public int intel; public TowerBonusInfo(float d, float h, int i, float m) { dmg=d; hp=h; intel=i; mana=m; } }
        public static readonly TowerBonusInfo[] TowerBonus = {
            new TowerBonusInfo(0.20f, 0.20f, 10, 50f),
            new TowerBonusInfo(0.40f, 0.40f, 20, 100f),
            new TowerBonusInfo(0.70f, 0.70f, 35, 200f),
            new TowerBonusInfo(1.00f, 1.00f, 50, 400f),
            new TowerBonusInfo(2.00f, 2.00f, 80, 800f),
        };

        private static readonly Dictionary<long, int> _towerLevel = new Dictionary<long, int>();

        public static void Register()
        {
            for (int i = 0; i < TowerIds.Length; i++)
            {
                var b = TowerBonus[i];
                LocalizedTextManager.add("trait_" + TowerIds[i], LocalizedTextManager.getText(TowerNames[i]), pReplace: true);
                LocalizedTextManager.add("trait_" + TowerIds[i] + "_info",
                    LocalizedTextManager.getText("sm_magetower_362"), pReplace: true);
                var t = new ActorTrait
                {
                    id = TowerIds[i],
                    path_icon = "ui/Icons/actor_traits/iconArcaneReflexes",
                    group_id = "sm_mage_tower",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = 1f + b.dmg;
                t.base_stats["multiplier_health"] = 1f + b.hp;
                t.base_stats["intelligence"] = b.intel;
                t.base_stats["mana"] = b.mana;
                AssetManager.traits.add(t);
            }

        }

        public static void TickMageTowers()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                if (!a.hasTrait(SuperMechTraits.ClassMage)) continue;

                int rank = SuperMechAdvancement.GetRankIndex(a);
                if (rank < 8) continue;

                int targetLevel = 1;
                if (rank >= 9) targetLevel = 2;
                if (rank >= 10) targetLevel = 3;
                if (rank >= 11) targetLevel = 4;
                if (rank >= 12) targetLevel = 5;

                int current = GetTowerLevel(a);
                if (current < targetLevel)
                {
                    SetTowerLevel(a, targetLevel);
                }
            }
        }

        public static int GetTowerLevel(Actor a)
        {
            if (a == null) return 0;
            if (_towerLevel.TryGetValue(a.data.id, out int lv)) return lv;
            for (int i = TowerIds.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(TowerIds[i])) { _towerLevel[a.data.id] = i + 1; return i + 1; }
            }
            return 0;
        }

        public static void SetTowerLevel(Actor a, int level)
        {
            if (a == null || level < 0 || level > TowerIds.Length) return;
            for (int i = 0; i < TowerIds.Length; i++)
            {
                if (a.hasTrait(TowerIds[i])) a.removeTrait(TowerIds[i]);
            }
            if (level > 0)
            {
                a.addTrait(TowerIds[level - 1]);
            }
            _towerLevel[a.data.id] = level;
        }

        public static string GetTowerName(Actor a)
        {
            int lv = GetTowerLevel(a);
            return lv > 0 ? TowerNames[lv - 1] : "sm_magetower_363";
        }

        public static void Clear()
        {
            _towerLevel.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_towerLevel, alive);
            return removed;
        }
    }
}
