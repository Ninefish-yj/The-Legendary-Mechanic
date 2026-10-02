using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 宇宙迭代模拟系统：记录宇宙迭代次数，大重启时保留圣所遗产
    /// 原著中宇宙会经历多次迭代，圣所记录每轮文明的知识和信息态
    /// </summary>
    public static class SuperMechCosmicIteration
    {
        /// <summary>当前宇宙迭代次数</summary>
        public static int CurrentIteration { get; private set; } = 0;

        /// <summary>遗产保留率基础值（0~1），实际保留率随机波动并受圣所权限影响</summary>
        public const float BaseHeritageRetentionRate = 0.3f;

        /// <summary>遗产保留率最大波动范围（±20%）</summary>
        public const float HeritageRetentionFluctuation = 0.2f;

        /// <summary>圣所权限对保留率的加成（每100点权限+5%）</summary>
        public const float AuthorityRetentionBonus = 0.0005f;

        /// <summary>是否已初始化</summary>
        private static bool _initialized = false;

        /// <summary>上一轮的遗产（跨迭代继承的数据）</summary>
        public class HeritageData
        {
            public int iteration;              // 来自第几轮
            public int retainedFragments;      // 保留的钥匙碎片
            public int retainedAuthority;      // 保留的权限
            public List<string> retainedKnowledge; // 保留的知识ID
            public string summary;             // 上一轮文明总结
        }

        private static HeritageData _pendingHeritage;

        /// <summary>初始化宇宙迭代系统（加载世界时调用）</summary>
        public static void Initialize(int savedIteration, string savedCivilizationJson)
        {
            if (_initialized) return;
            _initialized = true;

            CurrentIteration = savedIteration;
            SuperMechCivilizationData.Deserialize(savedCivilizationJson);

            // 开始新一轮文明记录
            SuperMechCivilizationData.StartIteration(CurrentIteration);

            // 应用上一轮遗产
            ApplyHeritage();

            Debug.Log($"[超神机械师] 宇宙迭代系统初始化: 第{CurrentIteration}轮，历史记录{SuperMechCivilizationData.GetHistory().Count}轮");
        }

        /// <summary>触发大重启（世界重置时调用）</summary>
        public static void TriggerGreatRestart()
        {
            if (!_initialized) return;

            // 结束当前文明记录
            SuperMechCivilizationData.EndIteration();

            // 计算遗产
            _pendingHeritage = CalculateHeritage();

            // 迭代次数+1
            CurrentIteration++;

            Debug.Log($"[超神机械师] 宇宙大重启! 进入第{CurrentIteration}轮迭代，遗产碎片{_pendingHeritage.retainedFragments}");
        }

        /// <summary>计算实际遗产保留率（随机波动+圣所权限加成）
        /// 原著：遗产保留不是固定比例，而是随机抽取部分信息融入，与圣所权限、迭代深度、文明等级相关
        /// </summary>
        private static float CalculateRetentionRate()
        {
            // 基础随机波动：±20%
            float randomFactor = Random.Range(-HeritageRetentionFluctuation, HeritageRetentionFluctuation);
            // 圣所权限加成：每100点权限+5%
            float authorityBonus = SuperMechSanctuary.Data.total_permission * AuthorityRetentionBonus;
            // 迭代深度加成：迭代越深，圣所积累越多，保留率越高
            float iterationBonus = CurrentIteration * 0.01f;
            return Mathf.Clamp01(BaseHeritageRetentionRate + randomFactor + authorityBonus + iterationBonus);
        }

        /// <summary>计算本轮遗产（大重启时保留的资源）</summary>
        private static HeritageData CalculateHeritage()
        {
            float retentionRate = CalculateRetentionRate();

            var heritage = new HeritageData
            {
                iteration = CurrentIteration,
                retainedFragments = Mathf.FloorToInt(SuperMechSanctuary.Data.key_fragments * retentionRate),
                retainedAuthority = Mathf.FloorToInt(SuperMechSanctuary.Data.total_permission * retentionRate),
                retainedKnowledge = new List<string>(),
                summary = SuperMechCivilizationData.GetHistory().Count > 0
                    ? SuperMechCivilizationData.GetHistory()[SuperMechCivilizationData.GetHistory().Count - 1].summary
                    : "",
            };

            // 随机保留部分知识（最多10条，随机抽取而非按比例）
            if (World.world != null && World.world.units != null)
            {
                var allKnowledge = new HashSet<string>();
                foreach (var a in World.world.units)
                {
                    if (a == null) continue;
                    var unlocked = SuperMechKnowledge.GetUnlockedList(a, "mech");
                    if (unlocked != null)
                    {
                        foreach (var k in unlocked) allKnowledge.Add(k.id);
                    }
                }
                int retainCount = Mathf.Min(10, Mathf.FloorToInt(allKnowledge.Count * retentionRate));
                var list = new List<string>(allKnowledge);
                // Fisher-Yates随机打乱
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    string temp = list[i]; list[i] = list[j]; list[j] = temp;
                }
                for (int i = 0; i < retainCount && i < list.Count; i++)
                {
                    heritage.retainedKnowledge.Add(list[i]);
                }
            }

            Debug.Log($"[超神机械师] 遗产保留率: {retentionRate:F0%} (随机+权限加成)");
            return heritage;
        }

        /// <summary>应用上一轮遗产到新世界</summary>
        private static void ApplyHeritage()
        {
            if (_pendingHeritage == null) return;

            // 保留钥匙碎片
            SuperMechSanctuary.Data.key_fragments += _pendingHeritage.retainedFragments;

            Debug.Log($"[超神机械师] 遗产继承: 来自第{_pendingHeritage.iteration}轮，碎片+{_pendingHeritage.retainedFragments}，知识+{_pendingHeritage.retainedKnowledge.Count}条");
            _pendingHeritage = null;
        }

        /// <summary>获取待应用的遗产（用于UI显示）</summary>
        public static HeritageData GetPendingHeritage()
        {
            return _pendingHeritage;
        }

        /// <summary>获取迭代统计信息</summary>
        public static string GetStatsText()
        {
            var history = SuperMechCivilizationData.GetHistory();
            int totalAwakened = 0;
            int maxRank = 0;
            foreach (var h in history)
            {
                totalAwakened += h.totalAwakened;
                if (h.maxRankReached > maxRank) maxRank = h.maxRankReached;
            }

            return $"当前迭代: 第{CurrentIteration}轮\n" +
                $"历史文明: {history.Count}轮\n" +
                $"累计觉醒: {totalAwakened}人\n" +
                $"历史最高阶位: {(maxRank >= 0 && maxRank < SuperMechRanks.All.Count ? LocalizedTextManager.getText(SuperMechRanks.All[maxRank].name) : "无")}\n" +
                $"遗产保留率: 基础{BaseHeritageRetentionRate * 100:F0}%（随机波动+权限加成）";
        }

        /// <summary>重置系统（新世界创建时）</summary>
        public static void Reset()
        {
            _initialized = false;
            CurrentIteration = 0;
            _pendingHeritage = null;
            SuperMechCivilizationData.Clear();
        }

        /// <summary>定期更新文明统计（每60秒调用一次）</summary>
        public static void TickUpdate()
        {
            if (!_initialized) return;
            SuperMechCivilizationData.UpdateStats();
        }
    }
}
