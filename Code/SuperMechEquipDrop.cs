using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechEquipDrop
    {
        private static readonly float[] _dropChanceByRank = new float[]
        {
            0.02f,
            0.05f,
            0.08f,
            0.10f,
            0.12f,
            0.15f,
            0.18f,
            0.22f,
            0.28f,
            0.35f,
            0.45f,
            0.55f,
            0.70f,
            0.90f,
        };

        private static readonly (int min, int max)[] _qualityRangeByRank = new (int, int)[]
        {
            (0, 0),
            (0, 1),
            (0, 2),
            (1, 2),
            (1, 3),
            (2, 3),
            (2, 4),
            (3, 4),
            (3, 5),
            (4, 5),
            (4, 6),
            (5, 6),
            (5, 7),
            (6, 8),
        };

        public static void TryDrop(Actor killer, Actor target)
        {
            if (killer == null || target == null) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(killer)) return;

            int rankIdx = SuperMechAdvancement.GetExactRankIndex(target);
            if (rankIdx < 0 || rankIdx >= _dropChanceByRank.Length) return;

            float dropChance = _dropChanceByRank[rankIdx];
            float luck = SuperMechCustomStats.GetStat(killer, "sm_luck");
            dropChance *= 1f + luck * 0.01f;

            if (UnityEngine.Random.value > dropChance) return;

            var (minQ, maxQ) = _qualityRangeByRank[rankIdx];
            int quality = UnityEngine.Random.Range(minQ, maxQ + 1);

            string equipId = SuperMechRelic.Equipments[quality].id;
            if (SuperMechEquipBag.AddToBag(killer, equipId))
            {
                Debug.Log($"[超神机械师] {killer.name} 击杀 {target.name} 掉落 {SuperMechRelic.Equipments[quality].name}装备！");
            }
        }

        public static string GetDropInfo(int rankIdx)
        {
            if (rankIdx < 0 || rankIdx >= _dropChanceByRank.Length) return "—";
            var (minQ, maxQ) = _qualityRangeByRank[rankIdx];
            string minName = SuperMechRelic.Equipments[minQ].name;
            string maxName = SuperMechRelic.Equipments[maxQ].name;
            return $"{(_dropChanceByRank[rankIdx] * 100):0}% ({minName}~{maxName})";
        }
    }
}
