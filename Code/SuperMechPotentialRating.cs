using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 异能潜力评级系统（原著ch48：异能具有不同的发展潜力，评级EDCBAS）。
    /// 异能者觉醒时随机获得潜力评级，影响气力增长速度和基因链突破概率。
    /// E级最低，S级最高（S级潜力可成长为超A级）。
    /// 仅异能系生效，其他系不显示。
    /// </summary>
    public static class SuperMechPotentialRating
    {
        public const string RatingE = "E";
        public const string RatingD = "D";
        public const string RatingC = "C";
        public const string RatingB = "B";
        public const string RatingA = "A";
        public const string RatingS = "S";

        // 评级→气力增长倍率
        private static readonly Dictionary<string, float> QiGrowthMult = new Dictionary<string, float>
        {
            { RatingE, 0.6f },
            { RatingD, 0.8f },
            { RatingC, 1.0f },
            { RatingB, 1.3f },
            { RatingA, 1.7f },
            { RatingS, 2.5f },
        };

        // 评级→基因链突破成功率加成
        private static readonly Dictionary<string, float> GeneBreakBonus = new Dictionary<string, float>
        {
            { RatingE, -0.20f },
            { RatingD, -0.10f },
            { RatingC, 0f },
            { RatingB, 0.10f },
            { RatingA, 0.20f },
            { RatingS, 0.35f },
        };

        // 评级权重（S级极稀有）
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

        /// <summary>异能者觉醒时随机分配潜力评级。</summary>
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

        /// <summary>获取单位潜力评级（仅异能系有意义）。</summary>
        public static string GetRating(Actor a)
        {
            if (a == null) return "";
            if (_ratings.TryGetValue(a.data.id, out string r)) return r;
            return "";
        }

        /// <summary>获取气力增长倍率。</summary>
        public static float GetQiGrowthMult(Actor a)
        {
            string r = GetRating(a);
            if (string.IsNullOrEmpty(r)) return 1f;
            return QiGrowthMult.TryGetValue(r, out float v) ? v : 1f;
        }

        /// <summary>获取基因链突破加成。</summary>
        public static float GetGeneBreakBonus(Actor a)
        {
            string r = GetRating(a);
            if (string.IsNullOrEmpty(r)) return 0f;
            return GeneBreakBonus.TryGetValue(r, out float v) ? v : 0f;
        }

        /// <summary>设置评级（用于神权直接赋予）。</summary>
        public static void SetRating(Actor a, string rating)
        {
            if (a != null && !string.IsNullOrEmpty(rating))
                _ratings[a.data.id] = rating;
        }

        /// <summary>清除单位评级数据。</summary>
        public static void Clear() { _ratings.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) _ratings.Remove(a.data.id);
        }
    }
}
