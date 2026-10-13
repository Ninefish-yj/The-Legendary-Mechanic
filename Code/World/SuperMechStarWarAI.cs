using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际战争AI系统（v0.139.0）
    /// 文明自动殖民、殖民地争夺、星际舰队作战
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechStarWarAI
    {
        // === 殖民AI ===
        private static readonly Dictionary<long, float> _lastColonize = new Dictionary<long, float>();
        private const float ColonizeInterval = 500f;  // 每500游戏时间单位尝试一次殖民

        // === 战争AI ===
        private static readonly Dictionary<long, float> _lastWarAction = new Dictionary<long, float>();
        private const float WarActionInterval = 800f;  // 每800时间单位尝试一次战争行动

        // === 殖民地升级 ===
        private static readonly Dictionary<string, float> _lastUpgrade = new Dictionary<string, float>();
        private const float UpgradeInterval = 300f;  // 每300时间单位检查一次升级

        /// <summary>星际战争AI总入口</summary>
        public static void Tick()
        {
            if (!SuperMechConfig.StarSystemEnabled) return;
            if (World.world == null || World.world.kingdoms == null) return;

            double now = World.world.getCurWorldTime();

            int colonizeCount = 0;
            int warCount = 0;

            foreach (var kingdom in World.world.kingdoms.list)
            {
                if (kingdom == null || kingdom.id == null) continue;
                long kid = kingdom.id;

                // 只有科技达到T2以上的文明才能星际活动
                int techTier = SuperMechTech.GetMaxTier(kid);
                if (techTier < 2) continue;

                // 殖民AI
                if (!_lastColonize.TryGetValue(kid, out float lastCol) || now - lastCol >= ColonizeInterval)
                {
                    if (colonizeCount < 3)
                    {
                        if (TryColonize(kingdom))
                        {
                            _lastColonize[kid] = (float)now;
                            colonizeCount++;
                        }
                    }
                }

                // 战争AI（只有T3以上文明才发动战争）
                if (techTier >= 3)
                {
                    if (!_lastWarAction.TryGetValue(kid, out float lastWar) || now - lastWar >= WarActionInterval)
                    {
                        if (warCount < 2)
                        {
                            if (TryWarAction(kingdom))
                            {
                                _lastWarAction[kid] = (float)now;
                                warCount++;
                            }
                        }
                    }
                }
            }

            // 殖民地自动升级
            TryUpgradeColonies(now);
        }

        /// <summary>尝试殖民已发现的无主星球</summary>
        private static bool TryColonize(Kingdom kingdom)
        {
            if (kingdom == null) return false;

            // 获取已发现但无主的星球
            var discovered = SuperMechStarSystem.GetDiscoveredPlanets();
            var unowned = new List<SuperMechStarSystem.PlanetData>();
            foreach (var p in discovered)
            {
                if (p == null) continue;
                if (p.id == SuperMechStarSystem.MAIN_PLANET) continue; // 主星球不殖民
                if (string.IsNullOrEmpty(p.ownerKingdom) && p.colonyLevel == 0)
                    unowned.Add(p);
            }

            if (unowned.Count == 0) return false;

            // 选择最近的宜居星球
            SuperMechStarSystem.PlanetData target = null;
            float bestScore = float.MaxValue;
            foreach (var p in unowned)
            {
                // 优先选择宜居带内的星球（温度273-373K）
                float tempScore = Mathf.Abs(p.temperature - 323f); // 最适宜温度约50°C
                float distance = Vector2.Distance(Vector2.zero, new Vector2(p.x, p.y));
                float score = tempScore + distance * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    target = p;
                }
            }

            if (target == null) return false;

            // 建立殖民地
            target.ownerKingdom = kingdom.name;
            target.colonyLevel = 1;
            target.resource = 10f; // 初始资源

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师]【星际殖民】{kingdom.name} 在 {target.name} 建立殖民地");

            return true;
        }

        /// <summary>尝试战争行动（攻击敌方殖民地）</summary>
        private static bool TryWarAction(Kingdom kingdom)
        {
            if (kingdom == null) return false;

            // 寻找敌方殖民地
            var allPlanets = SuperMechStarSystem.GetAllPlanets();
            var enemyColonies = new List<SuperMechStarSystem.PlanetData>();
            foreach (var p in allPlanets)
            {
                if (p == null || p.colonyLevel == 0) continue;
                if (p.id == SuperMechStarSystem.MAIN_PLANET) continue;
                if (!string.IsNullOrEmpty(p.ownerKingdom) && p.ownerKingdom != kingdom.name)
                    enemyColonies.Add(p);
            }

            if (enemyColonies.Count == 0) return false;

            // 选择最弱的敌方殖民地
            SuperMechStarSystem.PlanetData target = null;
            int weakestLevel = int.MaxValue;
            foreach (var p in enemyColonies)
            {
                if (p.colonyLevel < weakestLevel)
                {
                    weakestLevel = p.colonyLevel;
                    target = p;
                }
            }

            if (target == null) return false;

            // 计算双方实力（用文明科技等级和殖民地等级比较）
            int attackerTech = SuperMechTech.GetMaxTier(kingdom.id);
            int defenderStrength = target.colonyLevel * 2;

            // 攻击方科技等级 > 防御方殖民地等级×2 时才能成功
            if (attackerTech > defenderStrength)
            {
                // 成功夺取殖民地
                string oldOwner = target.ownerKingdom;
                target.ownerKingdom = kingdom.name;
                target.colonyLevel = Mathf.Max(1, target.colonyLevel - 1); // 战争破坏降级

                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师]【星际战争】{kingdom.name} 从 {oldOwner} 手中夺取 {target.name}（殖民地降级为{target.colonyLevel}级）");

                return true;
            }
            else
            {
                // 攻击失败，殖民地受损
                target.colonyLevel = Mathf.Max(0, target.colonyLevel - 1);
                if (target.colonyLevel == 0)
                    target.ownerKingdom = null;

                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师]【星际战争】{kingdom.name} 攻击 {target.name} 失败，殖民地受损");

                return true; // 也算执行了战争行动
            }
        }

        /// <summary>殖民地自动升级</summary>
        private static void TryUpgradeColonies(double now)
        {
            var allPlanets = SuperMechStarSystem.GetAllPlanets();
            foreach (var p in allPlanets)
            {
                if (p == null || p.colonyLevel == 0 || p.colonyLevel >= 5) continue;
                if (p.id == SuperMechStarSystem.MAIN_PLANET) continue;

                if (!_lastUpgrade.TryGetValue(p.id, out float last) || now - last >= UpgradeInterval)
                {
                    // 资源足够时自动升级
                    if (p.resource >= p.colonyLevel * 50f)
                    {
                        p.resource -= p.colonyLevel * 50f;
                        p.colonyLevel++;
                        _lastUpgrade[p.id] = (float)now;

                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师]【殖民地升级】{p.name} 升级到 {p.colonyLevel} 级（归属：{p.ownerKingdom}）");
                    }
                }
            }
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _lastColonize.Clear();
            _lastWarAction.Clear();
            _lastUpgrade.Clear();
        }
    }
}
