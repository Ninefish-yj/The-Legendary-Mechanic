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

        private static readonly int[] _qualityMinByRank = new int[]
        {
            0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6
        };

        private static readonly int[] _qualityMaxByRank = new int[]
        {
            0, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8
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

            int minQ = _qualityMinByRank[rankIdx];
            int maxQ = _qualityMaxByRank[rankIdx];
            int quality = UnityEngine.Random.Range(minQ, maxQ + 1);

            // v0.63.0 独特装备掉落：SS阶以上死亡时5%概率掉落独特装备
            if (rankIdx >= 12 && UnityEngine.Random.value < 0.05f)
            {
                string[] uniqueIds = { "sm_eq_void_armor", "sm_eq_evolution_cube", "sm_eq_timespace_cutter" };
                string uniqueId = uniqueIds[UnityEngine.Random.Range(0, uniqueIds.Length)];
                if (SuperMechEquipBag.AddToBag(killer, uniqueId))
                {
                    Debug.Log($"[超神机械师] 独特装备掉落：{killer.name} 获得 {uniqueId}");
                    return;
                }
            }

            string equipId = SuperMechRelic.Equipments[quality].id;
            if (SuperMechEquipBag.AddToBag(killer, equipId))
            {
            }
        }

        public static string GetDropInfo(int rankIdx)
        {
            if (rankIdx < 0 || rankIdx >= _dropChanceByRank.Length) return "—";
            int minQ = _qualityMinByRank[rankIdx];
            int maxQ = _qualityMaxByRank[rankIdx];
            string minName = SuperMechRelic.Equipments[minQ].name;
            string maxName = SuperMechRelic.Equipments[maxQ].name;
            return $"{(_dropChanceByRank[rankIdx] * 100):0}% ({minName}~{maxName})";
        }
    }
}
