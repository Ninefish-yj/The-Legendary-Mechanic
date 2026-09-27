using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechEquipDrop
    {
        private static readonly float[] _dropChanceByRank = new float[]
        {
            0.02f,  // F阶 2%
            0.05f,  // E阶 5%
            0.08f,  // D阶 8%
            0.10f,  // D+ 10%
            0.12f,  // C阶 12%
            0.15f,  // C+ 15%
            0.18f,  // B阶 18%
            0.22f,  // B+ 22%
            0.28f,  // A阶 28%
            0.35f,  // A+ 35%
            0.45f,  // S阶 45%
            0.55f,  // S+ 55%
            0.70f,  // SS阶 70%
            0.90f,  // X阶 90%
        };

        private static readonly (int min, int max)[] _qualityRangeByRank = new (int, int)[]
        {
            (0, 0),  // F阶 灰色
            (0, 1),  // E阶 灰-绿
            (0, 2),  // D阶 灰-蓝
            (1, 2),  // D+ 绿-蓝
            (1, 3),  // C阶 绿-淡紫
            (2, 3),  // C+ 蓝-淡紫
            (2, 4),  // B阶 蓝-紫
            (3, 4),  // B+ 淡紫-紫
            (3, 5),  // A阶 淡紫-粉
            (4, 5),  // A+ 紫-粉
            (4, 6),  // S阶 紫-橙
            (5, 6),  // S+ 粉-橙
            (5, 7),  // SS阶 粉-银橙
            (6, 8),  // X阶 橙-金
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
