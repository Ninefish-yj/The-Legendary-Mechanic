using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPotentialRating
    {
        public const string RatingE = "E";
        public const string RatingD = "D";
        public const string RatingC = "C";
        public const string RatingB = "B";
        public const string RatingA = "A";
        public const string RatingS = "S";

        private static readonly Dictionary<string, float> QiGrowthMult = new Dictionary<string, float>
        {
            { RatingE, 0.6f },
            { RatingD, 0.8f },
            { RatingC, 1.0f },
            { RatingB, 1.3f },
            { RatingA, 1.7f },
            { RatingS, 2.5f },
        };

        private static readonly Dictionary<string, float> GeneBreakBonus = new Dictionary<string, float>
        {
            { RatingE, -0.20f },
            { RatingD, -0.10f },
            { RatingC, 0f },
            { RatingB, 0.10f },
            { RatingA, 0.20f },
            { RatingS, 0.35f },
        };

        private static readonly Dictionary<string, int> RankCap = new Dictionary<string, int>
        {
            { RatingE, 2 },   // D阶
            { RatingD, 4 },   // C阶
            { RatingC, 6 },   // B阶
            { RatingB, 8 },   // A阶
            { RatingA, 10 },  // S阶（超A）
            { RatingS, 12 },  // SS阶
        };

        private static readonly (string rating, int weight)[] RatingWeights =
        {
            (RatingE, 25),
            (RatingD, 25),
            (RatingC, 25),
            (RatingB, 15),
            (RatingA, 8),
            (RatingS, 2),
        };

        private static readonly Dictionary<long, string> _ratings = new Dictionary<long, string>();

        public static string RollRating(Actor a)
        {
            if (a == null) return RatingC;
            int total = 0;
            foreach (var r in RatingWeights) total += r.weight;
            int roll = Random.Range(0, total);
            int acc = 0;
            string result = RatingC;
            foreach (var r in RatingWeights)
            {
                acc += r.weight;
                if (roll < acc) { result = r.rating; break; }
            }
            _ratings[a.data.id] = result;
            Debug.Log($"[超神机械师] {a.name} 异能潜力评级：{result}");
            return result;
        }

        public static string GetRating(Actor a)
        {
            if (a == null) return "";
            if (_ratings.TryGetValue(a.data.id, out string r)) return r;
            return "";
        }

        public static float GetQiGrowthMult(Actor a)
        {
            string r = GetRating(a);
            if (string.IsNullOrEmpty(r)) return 1f;
            return QiGrowthMult.TryGetValue(r, out float v) ? v : 1f;
        }

        public static float GetGeneBreakBonus(Actor a)
        {
            string r = GetRating(a);
            if (string.IsNullOrEmpty(r)) return 0f;
            return GeneBreakBonus.TryGetValue(r, out float v) ? v : 0f;
        }

        public static int GetMaxRank(Actor a)
        {
            string r = GetRating(a);
            if (string.IsNullOrEmpty(r)) return 8; // 无评级默认A阶上限
            return RankCap.TryGetValue(r, out int v) ? v : 8;
        }

        public static void SetRating(Actor a, string rating)
        {
            if (a != null && !string.IsNullOrEmpty(rating))
                _ratings[a.data.id] = rating;
        }

        public static void Clear() { _ratings.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_ratings, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null) _ratings.Remove(a.data.id);
        }
    }
}
