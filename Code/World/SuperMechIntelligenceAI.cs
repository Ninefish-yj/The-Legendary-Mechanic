using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>情报AI系统（v0.148.0）
    /// 文明情报收集：间谍活动、信息收集、情报分析
    /// 高文明等级文明有概率派遣间谍收集其他文明情报
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechIntelligenceAI
    {
        private static readonly Dictionary<long, Dictionary<long, float>> _intel = new Dictionary<long, Dictionary<long, float>>();
        private static readonly Dictionary<long, float> _lastSpyMission = new Dictionary<long, float>();
        private const float SpyInterval = 500f;  // 每500时间单位可派遣一次间谍

        /// <summary>情报AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            // 情报自然衰减（每tick少量衰减）
            DecayIntel();

            // 间谍任务（每500时间单位）
            if ((int)(now / SpyInterval) != (int)((now - 1) / SpyInterval))
            {
                DispatchSpies();
            }
        }

        /// <summary>派遣间谍</summary>
        private static void DispatchSpies()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            var kingdoms = World.world.kingdoms.list;
            if (kingdoms == null || kingdoms.Count < 2) return;

            int missions = 0;
            foreach (var k in kingdoms)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.id;

                // 文明等级2以上才能派遣间谍
                int civLevel = (int)SuperMechCivilization.GetLevel(k);
                if (civLevel < 2) continue;

                // 派遣间隔检查
                if (_lastSpyMission.TryGetValue(kid, out float last) &&
                    World.world.getCurWorldTime() - last < SpyInterval) continue;

                // 选择目标（最近的敌对或竞争文明）
                Kingdom target = FindSpyTarget(k, kingdoms);
                if (target == null) continue;

                // 派遣间谍
                _lastSpyMission[kid] = (float)World.world.getCurWorldTime();
                float intelGain = CalculateIntelGain(k, target, civLevel);

                if (!_intel.ContainsKey(kid))
                    _intel[kid] = new Dictionary<long, float>();
                if (!_intel[kid].ContainsKey(target.id))
                    _intel[kid][target.id] = 0f;

                _intel[kid][target.id] = Mathf.Min(100f, _intel[kid][target.id] + intelGain);

                if (SuperMechConfig.LogVerbose && Random.value < 0.2f)
                    Debug.Log($"[超神机械师]【情报AI】{k.name} 派遣间谍到 {target.name}，情报+{intelGain:F1}");

                missions++;
                if (missions >= 3) break; // 每轮最多3次间谍任务
            }
        }

        /// <summary>寻找间谍目标</summary>
        private static Kingdom FindSpyTarget(Kingdom self, List<Kingdom> kingdoms)
        {
            Kingdom best = null;
            float bestScore = 0f;

            foreach (var k in kingdoms)
            {
                if (k == null || k.id == self.id || k.wild || !k.isCiv()) continue;

                float dist = GetCapitalDistance(self, k);
                if (dist > 200f) continue;

                // 目标评分：距离近+实力强+敌对关系
                float score = 100f / Mathf.Max(1f, dist);
                score += SuperMechCivilization.GetLevel(k) * 5f;

                if (SuperMechDiplomacyAI.IsHostile(self, k))
                    score += 20f;

                // 已有情报的目标优先级降低
                if (_intel.TryGetValue(self.id, out var intelDict) && intelDict.TryGetValue(k.id, out float existing))
                    score -= existing * 0.5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = k;
                }
            }

            return best;
        }

        /// <summary>计算情报获取量</summary>
        private static float CalculateIntelGain(Kingdom self, Kingdom target, int selfCivLevel)
        {
            float baseGain = 5f + selfCivLevel * 2f;

            // 目标文明等级越高，情报越难获取
            int targetLevel = (int)SuperMechCivilization.GetLevel(target);
            baseGain /= Mathf.Max(1f, targetLevel * 0.5f);

            // 反情报能力（目标也有情报系统）
            if (targetLevel >= 3) baseGain *= 0.7f;

            return Mathf.Max(1f, baseGain);
        }

        /// <summary>情报自然衰减</summary>
        private static void DecayIntel()
        {
            foreach (var kv in _intel)
            {
                var targets = new List<long>(kv.Value.Keys);
                foreach (var tid in targets)
                {
                    kv.Value[tid] *= 0.999f; // 每tick衰减0.1%
                    if (kv.Value[tid] < 0.1f) kv.Value.Remove(tid);
                }
            }
        }

        /// <summary>获取首都距离</summary>
        private static float GetCapitalDistance(Kingdom k1, Kingdom k2)
        {
            if (k1?.capital == null || k2?.capital == null) return 999f;
            return Mathf.Abs(k1.capital.x - k2.capital.x) + Mathf.Abs(k1.capital.y - k2.capital.y);
        }

        /// <summary>获取对目标文明的情报值（供其他系统查询）</summary>
        public static float GetIntel(long selfId, long targetId)
        {
            if (_intel.TryGetValue(selfId, out var dict) && dict.TryGetValue(targetId, out float v))
                return v;
            return 0f;
        }

        /// <summary>判断是否对目标有足够情报（供其他系统查询）</summary>
        public static bool HasEnoughIntel(long selfId, long targetId, float threshold = 30f)
        {
            return GetIntel(selfId, targetId) >= threshold;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _intel.Clear();
            _lastSpyMission.Clear();
        }
    }
}
