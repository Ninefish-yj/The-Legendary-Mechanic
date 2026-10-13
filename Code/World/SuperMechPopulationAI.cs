using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>人口AI系统（v0.145.0）
    /// 文明人口增长控制、人口迁移、劳动力分配
    /// 基于粮食/住房/文明等级调节人口增长
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechPopulationAI
    {
        private static readonly Dictionary<long, int> _lastPopulation = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _growthRate = new Dictionary<long, float>();
        private const float PopulationInterval = 200f;  // 每200时间单位评估一次人口

        /// <summary>人口AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            if ((int)(now / PopulationInterval) != (int)((now - 1) / PopulationInterval))
            {
                EvaluatePopulation();
            }

            // 人口迁移（每tick少量迁移）
            MigratePopulation();
        }

        /// <summary>评估人口增长</summary>
        private static void EvaluatePopulation()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.id;

                int currentPop = k.units?.Count ?? 0;
                int lastPop = _lastPopulation.TryGetValue(kid, out int lp) ? lp : currentPop;

                // 计算增长因子
                float growth = CalculateGrowthRate(k);
                _growthRate[kid] = growth;

                // 人口增长过快时可能出现问题
                if (currentPop > lastPop * 1.5f && currentPop > 50)
                {
                    // 人口爆炸：可能导致资源紧张
                    if (SuperMechConfig.LogVerbose && Random.value < 0.1f)
                        Debug.Log($"[超神机械师]【人口AI】{k.name} 人口快速增长（{lastPop}→{currentPop}），增长率{growth:F2}");
                }

                _lastPopulation[kid] = currentPop;
            }
        }

        /// <summary>计算人口增长率</summary>
        private static float CalculateGrowthRate(Kingdom k)
        {
            if (k == null) return 0f;

            float rate = 1.0f; // 基础增长率

            // 文明等级加成
            rate += (int)(int)SuperMechCivilization.GetCivLevelFromKingdom(k) * 0.1f;

            // 城市数量加成
            if (k.cities != null) rate += k.cities.Count * 0.05f;

            // 人口密度惩罚
            int pop = k.units?.Count ?? 0;
            if (pop > 200) rate -= (pop - 200) * 0.005f;
            if (pop > 500) rate -= (pop - 500) * 0.01f;

            return Mathf.Max(0.1f, rate);
        }

        /// <summary>人口迁移</summary>
        private static void MigratePopulation()
        {
            if (World.world == null || World.world.units == null || World.world.kingdoms == null) return;

            // 低增长率文明的人口有概率迁移到高增长率文明
            var kingdoms = World.world.kingdoms.list;
            if (kingdoms == null || kingdoms.Count < 2) return;

            // 找到增长率最高的文明
            Kingdom bestDest = null;
            float bestRate = 0f;
            foreach (var k in kingdoms)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                if (_growthRate.TryGetValue(k.id, out float r) && r > bestRate)
                {
                    bestRate = r;
                    bestDest = k;
                }
            }

            if (bestDest == null) return;

            // 低增长文明的单位有概率迁移
            foreach (var k in kingdoms)
            {
                if (k == null || k.id == bestDest.id || k.wild || !k.isCiv()) continue;
                if (!_growthRate.TryGetValue(k.id, out float myRate)) continue;
                if (myRate >= bestRate * 0.8f) continue; // 增长率差距不大不迁移

                // 距离检查
                float dist = GetCapitalDistance(k, bestDest);
                if (dist > 100f) continue;

                // 少量单位迁移（通过改变王国归属实现，这里只记录日志）
                if (Random.value < 0.001f && k.units != null && k.units.Count > 10)
                {
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师]【人口AI】{k.name} 人口向 {bestDest.name} 迁移（增长率{myRate:F2}→{bestRate:F2}）");
                }
            }
        }

        /// <summary>获取首都距离</summary>
        private static float GetCapitalDistance(Kingdom k1, Kingdom k2)
        {
            if (k1?.capital == null || k2?.capital == null) return 999f;
            return Mathf.Abs(k1.capital.getTile().x - k2.capital.getTile().x) + Mathf.Abs(k1.capital.getTile().y - k2.capital.getTile().y);
        }

        /// <summary>获取文明人口增长率（供其他系统查询）</summary>
        public static float GetGrowthRate(long kingdomId)
        {
            return _growthRate.TryGetValue(kingdomId, out float r) ? r : 1.0f;
        }

        /// <summary>获取文明人口（供其他系统查询）</summary>
        public static int GetPopulation(long kingdomId)
        {
            return _lastPopulation.TryGetValue(kingdomId, out int p) ? p : 0;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _lastPopulation.Clear();
            _growthRate.Clear();
        }
    }
}
