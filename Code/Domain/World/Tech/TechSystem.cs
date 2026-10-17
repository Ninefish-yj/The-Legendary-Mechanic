using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.234 科技系统核心管理器
    /// 管理研究点产出、研究进度、科技解锁、材料发现、文明时代判定、AI研究。
    /// 参考帝国盒子架构，独立实现。
    /// </summary>
    public static class TechSystem
    {
        /// <summary>单个王国的科技状态</summary>
        [Serializable]
        public class KingdomTechState
        {
            public HashSet<string> Researched = new HashSet<string>();
            public HashSet<string> DiscoveredMaterials = new HashSet<string>();
            public string CurrentResearch = "";
            public float ResearchProgress = 0f;
            public float ResearchPoints = 0f;
            public float LastYearOutput = 0f;
            public int CurrentEra = 1;
        }

        private static readonly Dictionary<long, KingdomTechState> _states = new Dictionary<long, KingdomTechState>();
        private static bool _initialized;
        private static double _lastTickTime = -1f;

        /// <summary>初始化</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            _lastTickTime = -1f;
            TechDataLoader.Load();
            Debug.Log("[超神机械师] 科技系统初始化完成");
        }

        /// <summary>获取王国科技状态（不存在则创建）</summary>
        public static KingdomTechState GetState(long kingdomId)
        {
            if (!_states.TryGetValue(kingdomId, out var state))
            {
                state = new KingdomTechState();
                _states[kingdomId] = state;
            }
            return state;
        }

        /// <summary>获取王国科技状态（不存在返回null）</summary>
        public static KingdomTechState TryGetState(long kingdomId)
        {
            return _states.TryGetValue(kingdomId, out var state) ? state : null;
        }

        /// <summary>是否已研究某科技</summary>
        public static bool IsResearched(long kingdomId, string techId)
        {
            var state = TryGetState(kingdomId);
            return state != null && state.Researched.Contains(techId);
        }

        /// <summary>是否已发现某材料</summary>
        public static bool IsMaterialDiscovered(long kingdomId, string materialId)
        {
            var state = TryGetState(kingdomId);
            return state != null && state.DiscoveredMaterials.Contains(materialId);
        }

        /// <summary>科技是否可研究（前置满足+材料满足+未研究）</summary>
        public static bool CanResearch(long kingdomId, string techId)
        {
            var tech = TechDataLoader.GetTech(techId);
            if (tech == null) return false;
            var state = TryGetState(kingdomId);
            if (state == null) return false;
            if (state.Researched.Contains(techId)) return false;

            // 检查前置科技
            if (tech.requires_techs != null)
            {
                foreach (var pre in tech.requires_techs)
                {
                    if (!state.Researched.Contains(pre)) return false;
                }
            }

            // 检查前置材料
            if (tech.requires_materials != null)
            {
                foreach (var mat in tech.requires_materials)
                {
                    if (!state.DiscoveredMaterials.Contains(mat)) return false;
                }
            }

            return true;
        }

        /// <summary>开始研究</summary>
        public static bool StartResearch(long kingdomId, string techId)
        {
            if (!CanResearch(kingdomId, techId)) return false;
            var state = GetState(kingdomId);
            state.CurrentResearch = techId;
            state.ResearchProgress = 0f;
            Debug.Log($"[超神机械师] 王国{kingdomId}开始研究: {techId}");
            return true;
        }

        /// <summary>每年Tick：产出研究点、推进研究、材料发现、AI研究（内部做时间节流）</summary>
        public static void YearlyTick()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            // 时间节流：用游戏时间差控制执行频率（约每1.0游戏时间单位执行一次）
            double now = World.world.getCurWorldTime();
            if (_lastTickTime > 0 && now - _lastTickTime < 1.0) return;
            _lastTickTime = now;

            foreach (var kingdom in World.world.kingdoms.list)
            {
                if (kingdom == null || kingdom.isRekt()) continue;
                long kid = kingdom.getID();
                var state = GetState(kid);

                // 1. 计算研究点产出
                float output = CalculateResearchOutput(kingdom, state);
                state.LastYearOutput = output;
                state.ResearchPoints += output;

                // 2. 推进当前研究
                if (!string.IsNullOrEmpty(state.CurrentResearch))
                {
                    var tech = TechDataLoader.GetTech(state.CurrentResearch);
                    if (tech != null)
                    {
                        float cost = tech.cost * (TechDataLoader.Data?.research?.cost_multiplier ?? 12f);
                        state.ResearchProgress += output;
                        if (state.ResearchProgress >= cost)
                        {
                            CompleteResearch(kid, state, tech);
                        }
                    }
                    else
                    {
                        state.CurrentResearch = "";
                        state.ResearchProgress = 0f;
                    }
                }

                // 3. 材料发现
                CheckMaterialDiscovery(kingdom, state);

                // 4. AI自动研究（玩家王国也自动选，但玩家可以手动改）
                if (string.IsNullOrEmpty(state.CurrentResearch))
                {
                    AiPickResearch(kid, state);
                }

                // 5. 更新文明时代
                UpdateEra(state);
            }
        }

        /// <summary>计算研究点年产出</summary>
        private static float CalculateResearchOutput(Kingdom kingdom, KingdomTechState state)
        {
            var cfg = TechDataLoader.Data?.research;
            if (cfg == null) return 0f;

            float output = 0f;

            // 城市基础产出
            int cityCount = kingdom.cities?.Count ?? 0;
            output += cfg.base_per_city * cityCount;

            // 人口产出
            output += cfg.per_population * (kingdom.getPopulationTotal() / 1000f);

            // 建筑产出（遍历城市建筑）
            if (kingdom.cities != null)
            {
                foreach (var city in kingdom.cities)
                {
                    if (city?.buildings == null) continue;
                    foreach (var building in city.buildings)
                    {
                        if (building?.asset == null) continue;
                        string type = building.asset.type ?? "";
                        if (type.Contains("library")) output += cfg.per_library;
                        else if (type.Contains("temple")) output += cfg.per_temple;
                        else if (type.Contains("university")) output += cfg.per_university;
                        else if (type.Contains("factory") || type.Contains("mill")) output += cfg.per_factory;
                    }
                }
            }

            // 圣所加成（模组特色）
            // TODO: 接入圣所建筑检测

            // 战争惩罚（遍历世界战争列表）
            if (IsKingdomAtWar(kingdom))
            {
                output *= (1f - cfg.war_penalty);
            }

            return Mathf.Max(0f, output);
        }

        /// <summary>判断王国是否处于战争中（遍历世界战争列表）</summary>
        private static bool IsKingdomAtWar(Kingdom kingdom)
        {
            if (World.world?.wars == null || World.world?.kingdoms == null || kingdom == null) return false;
            foreach (var war in World.world.wars)
            {
                if (war == null || war.isRekt()) continue;
                foreach (var other in World.world.kingdoms.list)
                {
                    if (other == null || other == kingdom) continue;
                    if (war.isInWarWith(kingdom, other)) return true;
                }
            }
            return false;
        }

        /// <summary>完成研究</summary>
        private static void CompleteResearch(long kingdomId, KingdomTechState state, TechData.TechDef tech)
        {
            state.Researched.Add(tech.id);
            state.CurrentResearch = "";
            state.ResearchProgress = 0f;
            state.ResearchPoints = 0f;

            Debug.Log($"[超神机械师] 王国{kingdomId}研究完成: {tech.id} (tier{tech.tier})");

            // 触发解锁效果
            ApplyTechUnlocks(kingdomId, tech);
        }

        /// <summary>应用科技解锁效果</summary>
        private static void ApplyTechUnlocks(long kingdomId, TechData.TechDef tech)
        {
            // 建筑解锁：通过 SuperMechBuilding.IsUnlocked 检查（已接入）
            // 知识解锁：通过知识系统检查前置
            // 职业解锁：通过觉醒系统检查
            // 加成效果：在对应系统中读取

            // TODO: v0.236 阶段接入具体解锁效果
        }

        /// <summary>材料发现检查</summary>
        private static void CheckMaterialDiscovery(Kingdom kingdom, KingdomTechState state)
        {
            if (kingdom.cities == null) return;

            // TODO: 检查仓库资源，需要确认WorldBox的资源存储API
            // 当前简化：tier<=3基础材料自动发现

            // 检查每种材料是否满足发现条件
            foreach (var mat in TechDataLoader.AllMaterials)
            {
                if (state.DiscoveredMaterials.Contains(mat.id)) continue;

                // 检查前置科技
                bool techOk = true;
                if (mat.requires_techs != null)
                {
                    foreach (var pre in mat.requires_techs)
                    {
                        if (!state.Researched.Contains(pre)) { techOk = false; break; }
                    }
                }
                if (!techOk) continue;

                // 检查资源（简化：有任意资源即发现，后续细化）
                if (mat.resources != null && mat.resources.Count > 0)
                {
                    // TODO: 实际检查仓库资源
                    // 暂时：tier<=3的基础材料自动发现
                    if (mat.tier <= 3)
                    {
                        state.DiscoveredMaterials.Add(mat.id);
                    }
                }
                else
                {
                    state.DiscoveredMaterials.Add(mat.id);
                }
            }
        }

        /// <summary>AI选择研究目标</summary>
        private static void AiPickResearch(long kingdomId, KingdomTechState state)
        {
            // 收集所有可研究的科技
            var candidates = new List<TechData.TechDef>();
            foreach (var tech in TechDataLoader.AllTechs)
            {
                if (state.Researched.Contains(tech.id)) continue;
                if (!CanResearch(kingdomId, tech.id)) continue;
                candidates.Add(tech);
            }

            if (candidates.Count == 0) return;

            // AI策略：优先低tier，同tier随机
            candidates.Sort((a, b) => a.tier.CompareTo(b.tier));
            int minTier = candidates[0].tier;
            var lowest = candidates.Where(t => t.tier == minTier).ToList();
            var picked = lowest[UnityEngine.Random.Range(0, lowest.Count)];

            StartResearch(kingdomId, picked.id);
        }

        /// <summary>更新文明时代（已研究的最高tier）</summary>
        private static void UpdateEra(KingdomTechState state)
        {
            int maxTier = 1;
            foreach (var techId in state.Researched)
            {
                var tech = TechDataLoader.GetTech(techId);
                if (tech != null && tech.tier > maxTier) maxTier = tech.tier;
            }
            state.CurrentEra = maxTier;
        }

        /// <summary>获取王国当前时代</summary>
        public static int GetEra(long kingdomId)
        {
            var state = TryGetState(kingdomId);
            return state != null ? state.CurrentEra : 1;
        }

        /// <summary>获取时代名称key</summary>
        public static string GetEraKey(int tier)
        {
            foreach (var era in TechDataLoader.Data?.eras ?? new List<TechData.EraDef>())
            {
                if (era.tier == tier) return era.key;
            }
            return "sm_era_unknown";
        }

        /// <summary>获取研究进度百分比</summary>
        public static float GetResearchProgress(long kingdomId)
        {
            var state = TryGetState(kingdomId);
            if (state == null || string.IsNullOrEmpty(state.CurrentResearch)) return 0f;
            var tech = TechDataLoader.GetTech(state.CurrentResearch);
            if (tech == null) return 0f;
            float cost = tech.cost * (TechDataLoader.Data?.research?.cost_multiplier ?? 12f);
            return Mathf.Clamp01(state.ResearchProgress / cost);
        }

        /// <summary>清空所有状态（新世界时调用）</summary>
        public static void ClearAll()
        {
            _states.Clear();
        }

        // === 存档 ===

        [Serializable]
        public class TechSaveData
        {
            public Dictionary<long, KingdomTechState> states = new Dictionary<long, KingdomTechState>();
        }

        /// <summary>序列化存档</summary>
        public static string Save()
        {
            var data = new TechSaveData { states = _states };
            return JsonUtility.ToJson(data);
        }

        /// <summary>反序列化存档</summary>
        public static void Load(string json)
        {
            _states.Clear();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<TechSaveData>(json);
                if (data?.states != null)
                {
                    foreach (var kv in data.states)
                    {
                        _states[kv.Key] = kv.Value;
                    }
                }
                Debug.Log($"[超神机械师] 科技存档加载：{_states.Count}个王国");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 科技存档加载失败: " + e.Message);
            }
        }
    }
}
