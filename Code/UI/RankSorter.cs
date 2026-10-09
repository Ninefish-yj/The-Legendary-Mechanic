using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 超能者排行榜排序器
    /// 单一职责：只负责排行榜的排序计算和统计，不处理UI渲染
    /// 从RankView中提取，遵循UI与业务逻辑分离原则
    /// </summary>
    public static class RankSorter
    {
        /// <summary>排序类型：0=能级,1=气力,2=阶位,3=知识数,4=职业阶段</summary>
        public enum SortType { Onar = 0, Qi = 1, Rank = 2, Knowledge = 3, Stage = 4 }

        /// <summary>计算单位排序分数</summary>
        public static float GetScore(Actor a, int sortType)
        {
            return sortType switch
            {
                0 => SuperMechAdvancement.CalcOnar(a), // 能级
                1 => SuperMechQi.GetQiMax(a), // 气力
                2 => SuperMechAdvancement.GetExactRankIndex(a), // 阶位
                3 => SuperMechKnowledge.GetUnlockedCount(a, "mech"), // 知识数
                4 => SuperMechStage.GetStage(a), // 职业阶段
                _ => 0
            };
        }

        /// <summary>按当前排序类型对列表排序（降序）</summary>
        public static void SortList(List<Actor> list, int sortType, Dictionary<long, float> scoreCache)
        {
            foreach (var a in list)
            {
                if (!scoreCache.ContainsKey(a.getID()))
                    scoreCache[a.getID()] = GetScore(a, sortType);
            }
            try { list.Sort((a, b) => scoreCache[b.getID()].CompareTo(scoreCache[a.getID()])); }
            catch (System.Exception e) { Debug.LogWarning("[超神机械师] 排行榜排序失败: " + e.Message); }
        }

        /// <summary>计算阶位分布统计</summary>
        public static string CalculateRankStats(List<Actor> candidatePool)
        {
            int[] rankCount = new int[8]; // F,E,D,C,B,A,S,X
            foreach (var a in candidatePool)
            {
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                int tier = rankIdx switch
                {
                    0 => 0, 1 or 2 => 1, 3 or 4 => 2, 5 or 6 => 3,
                    7 => 4, 8 or 9 => 5, 10 or 11 or 12 => 6, 13 => 7,
                    _ => -1
                };
                if (tier >= 0) rankCount[tier]++;
            }

            string[] rankLabels = { "F", "E", "D", "C", "B", "A", "S", "X" };
            string stats = "";
            for (int i = 0; i < 8; i++)
            {
                if (rankCount[i] > 0)
                    stats += $"{rankLabels[i]}:{rankCount[i]} ";
            }
            return stats;
        }

        /// <summary>排序类型名称（用于UI下拉框）</summary>
        public static readonly string[] SortNames = { "能级", "气力", "阶位", "知识数", "职业阶段" };
    }
}
