using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>资源AI系统（v0.146.0）
    /// 文明资源管理：采集优先级、资源分配、资源储备
    /// 基于文明需求和资源储量智能分配
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechResourceAI
    {
        private static readonly Dictionary<long, Dictionary<string, float>> _resources = new Dictionary<long, Dictionary<string, float>>();
        private static readonly Dictionary<long, string> _priorityResource = new Dictionary<long, string>();
        private const float ResourceInterval = 150f;  // 每150时间单位评估一次资源

        // 资源类型
        public const string Food = "food";
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const string Metal = "metal";
        public const string Energy = "energy";
        public const string Knowledge = "knowledge";

        /// <summary>资源AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            // 资源产出（每tick少量产出）
            ProduceResources();

            // 资源评估和分配（每150时间单位）
            if ((int)(now / ResourceInterval) != (int)((now - 1) / ResourceInterval))
            {
                EvaluateAndAllocate();
            }
        }

        /// <summary>资源产出</summary>
        private static void ProduceResources()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.id;

                if (!_resources.ContainsKey(kid))
                    _resources[kid] = new Dictionary<string, float>();

                var res = _resources[kid];
                int pop = k.units?.Count ?? 0;
                int cities = k.cities?.Count ?? 0;

                // 基础产出
                AddResource(res, Food, pop * 0.1f);
                AddResource(res, Wood, cities * 0.5f);
                AddResource(res, Stone, cities * 0.3f);

                // 文明等级解锁高级资源
                int civLevel = (int)SuperMechCivilization.GetLevel(k);
                if (civLevel >= 2) AddResource(res, Metal, cities * 0.2f);
                if (civLevel >= 3) AddResource(res, Energy, pop * 0.05f);
                if (civLevel >= 4) AddResource(res, Knowledge, pop * 0.02f);

                // 资源消耗（人口消耗粮食）
                AddResource(res, Food, -pop * 0.08f);

                // 资源上限
                ClampResources(res);
            }
        }

        /// <summary>资源评估和分配</summary>
        private static void EvaluateAndAllocate()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.id;

                if (!_resources.TryGetValue(kid, out var res)) continue;

                // 找出最紧缺的资源
                string mostNeeded = Food;
                float lowestRatio = float.MaxValue;
                string[] types = { Food, Wood, Stone, Metal, Energy, Knowledge };
                foreach (var type in types)
                {
                    float amount = res.TryGetValue(type, out float a) ? a : 0f;
                    float need = GetResourceNeed(k, type);
                    float ratio = need > 0 ? amount / need : 1f;
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        mostNeeded = type;
                    }
                }

                _priorityResource[kid] = mostNeeded;

                // 资源紧缺时的应对
                if (lowestRatio < 0.3f)
                {
                    if (SuperMechConfig.LogVerbose && Random.value < 0.1f)
                        Debug.Log($"[超神机械师]【资源AI】{k.name} {mostNeeded}紧缺（储量{res.TryGetValue(mostNeeded, out float v) ? v : 0:F0}）");
                }
            }
        }

        /// <summary>获取资源需求量</summary>
        private static float GetResourceNeed(Kingdom k, string type)
        {
            if (k == null) return 0f;
            int pop = k.units?.Count ?? 0;
            int cities = k.cities?.Count ?? 0;

            switch (type)
            {
                case Food: return pop * 10f;
                case Wood: return cities * 20f;
                case Stone: return cities * 15f;
                case Metal: return cities * 10f;
                case Energy: return pop * 2f;
                case Knowledge: return pop * 1f;
                default: return 0f;
            }
        }

        /// <summary>添加资源</summary>
        private static void AddResource(Dictionary<string, float> res, string type, float amount)
        {
            if (!res.TryGetValue(type, out float current)) current = 0f;
            res[type] = current + amount;
        }

        /// <summary>资源上限限制</summary>
        private static void ClampResources(Dictionary<string, float> res)
        {
            string[] types = { Food, Wood, Stone, Metal, Energy, Knowledge };
            foreach (var type in types)
            {
                if (res.TryGetValue(type, out float v))
                {
                    res[type] = Mathf.Clamp(v, 0f, 10000f);
                }
            }
        }

        /// <summary>获取文明资源量（供其他系统查询）</summary>
        public static float GetResource(long kingdomId, string type)
        {
            if (_resources.TryGetValue(kingdomId, out var res) && res.TryGetValue(type, out float v))
                return v;
            return 0f;
        }

        /// <summary>获取文明优先资源（供其他系统查询）</summary>
        public static string GetPriorityResource(long kingdomId)
        {
            return _priorityResource.TryGetValue(kingdomId, out string r) ? r : Food;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _resources.Clear();
            _priorityResource.Clear();
        }
    }
}
