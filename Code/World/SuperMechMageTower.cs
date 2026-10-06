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
            // v0.82.0: 法师塔改为纯Dictionary存储，属性加成在Actor_UpdateStats_Postfix中应用
            // 不再注册为特质，避免特质面板混乱
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
            return 0;
        }

        public static void SetTowerLevel(Actor a, int level)
        {
            if (a == null || level < 0 || level > TowerIds.Length) return;
            _towerLevel[a.data.id] = level;
        }

        public static string GetTowerName(Actor a)
        {
            int lv = GetTowerLevel(a);
            return lv > 0 ? TowerNames[lv - 1] : "sm_magetower_363";
        }

        /// <summary>
        /// 应用法师塔属性加成（v0.82.0: 从特质改为Postfix手动应用）
        /// </summary>
        public static void ApplyTowerBonus(Actor a, BaseStats stats)
        {
            if (a == null || stats == null) return;
            int lv = GetTowerLevel(a);
            if (lv <= 0 || lv > TowerBonus.Length) return;
            var b = TowerBonus[lv - 1];
            stats["multiplier_damage"] *= 1f + b.dmg;
            stats["multiplier_health"] *= 1f + b.hp;
            stats["intelligence"] += b.intel;
            stats["mana"] += b.mana;
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
