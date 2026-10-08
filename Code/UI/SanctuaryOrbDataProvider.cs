using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所光球数据提供者
    /// 单一职责：只负责生成光球的业务数据（OrbInfo），不处理UI渲染
    /// 从SanctuaryView中提取，遵循UI与业务逻辑分离原则
    /// 原著：圣所记录历代迭代文明，迭代几轮就有几个光球
    /// </summary>
    public static class SanctuaryOrbDataProvider
    {
        /// <summary>
        /// 生成光球数据列表
        /// 基于真实迭代历史，迭代几轮就有几个记录
        /// </summary>
        /// <param name="sanctuaryIndex">圣所索引（0-5）</param>
        /// <returns>光球数据列表</returns>
        public static List<SanctuaryOrbInfo> GenerateOrbInfos(int sanctuaryIndex)
        {
            var result = new List<SanctuaryOrbInfo>();
            var history = SuperMechCivilizationData.GetHistory();
            int orbCount = Mathf.Max(history.Count, 1);

            for (int i = 0; i < orbCount; i++)
            {
                var snap = (i < history.Count) ? history[i] : SuperMechCivilizationData.GetCurrent();
                int iter = (snap != null) ? snap.iteration : SuperMechCosmicIteration.CurrentIteration;
                bool isCurrent = (i >= history.Count);

                // 文明名称：真实记录用著名单位或迭代编号
                string civName = BuildCivilizationName(snap, iter);

                // 成就：基于真实数据
                string achievement = BuildAchievement(snap);

                // 毁灭原因
                string destruction = isCurrent
                    ? LocalizedTextManager.getText("sm_san_civ_ongoing")
                    : LocalizedTextManager.getText("sm_san_civ_reset");

                result.Add(new SanctuaryOrbInfo
                {
                    civilizationName = civName,
                    domain = SuperMechSanctuary.GetSanctuaryTypeName(sanctuaryIndex),
                    iteration = iter,
                    achievement = achievement,
                    destructionCause = destruction,
                    snapshot = snap
                });
            }
            return result;
        }

        /// <summary>构建文明名称</summary>
        private static string BuildCivilizationName(CivilizationSnapshot snap, int iter)
        {
            if (snap != null && snap.notableUnits != null && snap.notableUnits.Count > 0)
                return snap.notableUnits[0] + LocalizedTextManager.getText("sm_san_civ_era");
            if (snap != null)
                return string.Format(LocalizedTextManager.getText("sm_san_civ_iteration"), iter);
            return LocalizedTextManager.getText("sm_san_civ_current");
        }

        /// <summary>构建成就描述</summary>
        private static string BuildAchievement(CivilizationSnapshot snap)
        {
            if (snap != null)
            {
                string rankName = (snap.maxRankReached >= 0 && snap.maxRankReached < SuperMechRanks.All.Count)
                    ? LocalizedTextManager.getText(SuperMechRanks.All[snap.maxRankReached].name)
                    : LocalizedTextManager.getText("sm_civ_none");
                return string.Format(LocalizedTextManager.getText("sm_san_civ_achievement"),
                    snap.totalAwakened, rankName, snap.totalKnowledgeUnlocked);
            }
            return LocalizedTextManager.getText("sm_san_civ_ongoing");
        }

        /// <summary>计算光球大小（越古老越小越暗）</summary>
        public static float CalculateOrbSize(int index, int totalCount)
        {
            float ageFactor = 1f - (float)index / Mathf.Max(1, totalCount);
            return 30f + ageFactor * 40f;
        }

        /// <summary>计算光球位置（环形分布）</summary>
        public static Vector2 CalculateOrbPosition(int index, int totalCount, Rect containerRect)
        {
            float ageFactor = 1f - (float)index / Mathf.Max(1, totalCount);
            float angle = (index / (float)totalCount) * Mathf.PI * 2f;
            float radius = 0.15f + ageFactor * 0.25f;
            float x = Mathf.Cos(angle) * radius * containerRect.width;
            float y = Mathf.Sin(angle) * radius * containerRect.height;
            return new Vector2(x, y);
        }

        /// <summary>计算光球透明度（越古老越暗）</summary>
        public static float CalculateOrbAlpha(int index, int totalCount)
        {
            float ageFactor = 1f - (float)index / Mathf.Max(1, totalCount);
            return 0.3f + ageFactor * 0.4f;
        }
    }

    /// <summary>光球记录的文明信息（从SanctuaryView内部类提取）</summary>
    public class SanctuaryOrbInfo
    {
        public string civilizationName;
        public string domain;
        public int iteration;
        public string achievement;
        public string destructionCause;
        public SuperMechCivilizationData.CivilizationSnapshot snapshot;
    }
}
