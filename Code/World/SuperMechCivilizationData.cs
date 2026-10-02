using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 文明数据记录：记录每轮宇宙迭代中的文明发展情况
    /// 包括知识解锁、遗物收集、信息态数量、超能者数量等
    /// </summary>
    public static class SuperMechCivilizationData
    {
        /// <summary>单轮文明数据快照</summary>
        public class CivilizationSnapshot
        {
            public int iteration;              // 迭代轮次
            public long startedAt;             // 开始时间戳
            public long endedAt;               // 结束时间戳
            public int totalAwakened;          // 累计觉醒超能者数
            public int maxRankReached;         // 达到的最高阶位
            public int totalKnowledgeUnlocked; // 累计解锁知识数
            public int totalRelicsFound;       // 累计发现遗物数
            public int totalInformationStates; // 累计信息态记录数
            public int totalResurrections;     // 累计复活次数
            public int totalDivinityAscensions;// 累计神性蜕变次数
            public List<string> notableUnits;  // 著名单位名字列表
            public string summary;             // 文明总结
        }

        private static readonly List<CivilizationSnapshot> _history = new List<CivilizationSnapshot>();
        private static CivilizationSnapshot _current;

        /// <summary>获取历史迭代记录</summary>
        public static List<CivilizationSnapshot> GetHistory()
        {
            return _history;
        }

        /// <summary>获取当前迭代快照</summary>
        public static CivilizationSnapshot GetCurrent()
        {
            return _current;
        }

        /// <summary>开始新一轮文明记录</summary>
        public static void StartIteration(int iteration)
        {
            _current = new CivilizationSnapshot
            {
                iteration = iteration,
                startedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                notableUnits = new List<string>(),
            };
            Debug.Log($"[超神机械师] 文明记录开始: 第{iteration}轮宇宙迭代");
        }

        /// <summary>结束当前文明记录，存入历史</summary>
        public static void EndIteration()
        {
            if (_current == null) return;

            _current.endedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateStats();
            _current.summary = GenerateSummary(_current);
            _history.Add(_current);

            // 限制历史记录数量
            if (_history.Count > 20)
                _history.RemoveAt(0);

            Debug.Log($"[超神机械师] 文明记录结束: 第{_current.iteration}轮，觉醒{_current.totalAwakened}人，最高阶位{_current.maxRankReached}");
            _current = null;
        }

        /// <summary>更新当前统计数据</summary>
        public static void UpdateStats()
        {
            if (_current == null || World.world == null || World.world.units == null) return;

            int awakened = 0;
            int maxRank = 0;
            int knowledge = 0;
            var notable = new List<string>();

            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                if (SuperMechAwakened.IsAwakened(a))
                {
                    awakened++;
                    int rank = SuperMechAdvancement.GetRankIndex(a);
                    if (rank > maxRank) maxRank = rank;
                    knowledge += SuperMechKnowledge.GetUnlockedCount(a, "mech");
                    if (rank >= 8 && notable.Count < 10) // A阶以上记入著名单位
                    {
                        notable.Add(a.name ?? "Unknown");
                    }
                }
            }

            _current.totalAwakened = awakened;
            _current.maxRankReached = maxRank;
            _current.totalKnowledgeUnlocked = knowledge;
            _current.totalRelicsFound = SuperMechSanctuary.Data.total_visits; // 用访问次数近似
            _current.totalInformationStates = SuperMechInformationState.GetDeadStates().Count;
            _current.totalResurrections = SuperMechSanctuary.Data.total_resurrections;
            _current.totalDivinityAscensions = SuperMechSanctuary.Data.total_divinity_ascensions;
            _current.notableUnits = notable;
        }

        /// <summary>生成文明总结</summary>
        private static string GenerateSummary(CivilizationSnapshot snap)
        {
            if (snap == null) return "";
            string rankName = (snap.maxRankReached >= 0 && snap.maxRankReached < SuperMechRanks.All.Count)
                ? LocalizedTextManager.getText(SuperMechRanks.All[snap.maxRankReached].name)
                : "无";
            return $"第{snap.iteration}轮文明：觉醒{snap.totalAwakened}人，最高阶位{rankName}，解锁知识{snap.totalKnowledgeUnlocked}条，复活{snap.totalResurrections}次";
        }

        /// <summary>序列化为JSON（用于存档）</summary>
        public static string Serialize()
        {
            try
            {
                return JsonConvert.SerializeObject(_history, Formatting.None);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>从JSON反序列化（用于读档）</summary>
        public static void Deserialize(string json)
        {
            _history.Clear();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonConvert.DeserializeObject<List<CivilizationSnapshot>>(json);
                if (data != null) _history.AddRange(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 文明数据反序列化失败: " + e.Message);
            }
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _history.Clear();
            _current = null;
        }
    }
}
