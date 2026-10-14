using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>公司AI系统（v0.140.0）
    /// 公司自动升级、自动投资、声望管理
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechCompanyAI
    {
        private static readonly Dictionary<string, int> _lastAction = new Dictionary<string, int>();
        private const int ActionInterval = 256;  // 每256tick进行一次公司决策

        /// <summary>公司AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null) return;

            var companies = SuperMechCompany.GetAllCompanies();
            if (companies == null || companies.Count == 0) return;

            int processed = 0;
            foreach (var company in companies)
            {
                if (company == null || string.IsNullOrEmpty(company.id)) continue;
                if (!_lastAction.TryGetValue(company.id, out int last) ||
                    World.world.getCurWorldTime() - last >= ActionInterval)
                {
                    if (processed >= 3) break; // 每tick最多处理3个公司
                    _lastAction[company.id] = (int)World.world.getCurWorldTime();
                    MakeDecision(company);
                    processed++;
                }
            }
        }

        /// <summary>公司决策：升级/投资/声望</summary>
        private static void MakeDecision(SuperMechCompany.CompanyDef company)
        {
            if (company == null) return;

            // 1. 优先升级（金库足够时）
            long upgradeCost = GetUpgradeCost(company.level);
            if (company.treasury >= upgradeCost && company.level < 10)
            {
                company.treasury -= upgradeCost;
                company.level++;
                company.reputation += 5;
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师]【公司AI】{LocalizedTextManager.getText(company.nameKey)} 升级到 {company.level} 级（花费{upgradeCost}伊纳尔）");
                return;
            }

            // 2. 投资殖民地（增加资源产出）
            if (company.treasury >= 100 && company.level >= 2)
            {
                var colonies = GetOwnedColonies(company);
                if (colonies.Count > 0)
                {
                    // 投资最弱的殖民地
                    SuperMechStarSystem.PlanetData weakest = null;
                    int minLevel = int.MaxValue;
                    foreach (var c in colonies)
                    {
                        if (c.colonyLevel < minLevel && c.colonyLevel < 5)
                        {
                            minLevel = c.colonyLevel;
                            weakest = c;
                        }
                    }
                    if (weakest != null)
                    {
                        weakest.resource += 50f;
                        company.treasury -= 100;
                        company.reputation += 2;
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师]【公司AI】{LocalizedTextManager.getText(company.nameKey)} 投资殖民地 {weakest.name}（+50资源）");
                        return;
                    }
                }
            }

            // 3. 积累声望（金库充足时缓慢增长）
            if (company.treasury > 500 && Random.value < 0.3f)
            {
                company.reputation += 1;
                company.treasury -= 50; // 公关费用
            }
        }

        /// <summary>获取升级费用</summary>
        private static long GetUpgradeCost(int currentLevel)
        {
            return (long)(100 * Mathf.Pow(1.5f, currentLevel - 1));
        }

        /// <summary>获取公司所属的殖民地（简化：公司所在文明的殖民地）</summary>
        private static List<SuperMechStarSystem.PlanetData> GetOwnedColonies(SuperMechCompany.CompanyDef company)
        {
            var result = new List<SuperMechStarSystem.PlanetData>();
            if (company == null || World.world == null || World.world.kingdoms == null) return result;

            // 找到公司所在的文明（通过创立者特质）
            string ownerKingdom = null;
            foreach (var kingdom in World.world.kingdoms.list)
            {
                if (kingdom == null) continue;
                // 简化：检查文明是否有公司创立者特质
                if (kingdom.name != null && company.founderId != null)
                {
                    // 这里简化处理，实际应该通过创立者找到所属文明
                    ownerKingdom = kingdom.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(ownerKingdom)) return result;

            // 获取该文明的殖民地
            var allPlanets = SuperMechStarSystem.GetAllPlanets();
            foreach (var p in allPlanets)
            {
                if (p != null && p.colonyLevel > 0 && p.ownerKingdom == ownerKingdom)
                    result.Add(p);
            }
            return result;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _lastAction.Clear();
        }
    }
}
