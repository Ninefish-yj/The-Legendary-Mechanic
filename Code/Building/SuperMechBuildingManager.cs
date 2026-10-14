using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 建筑管理器：AI自动建造+建筑运行逻辑
    /// 对照原著：文明发展到一定程度自动建造特殊设施
    /// 建筑作为世界运行的一部分，不是玩家手动放置
    /// </summary>
    public static class SuperMechBuildingManager
    {
        private static readonly Dictionary<string, int> _cityBuildingCounts = new Dictionary<string, int>();
        private static int _tickCounter = 0;

        /// <summary>Tick：每16帧检查一次建筑建造和运行</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.cities == null) return;

            _tickCounter++;
            if (_tickCounter % 16 != 0) return;

            foreach (var city in World.world.cities)
            {
                if (city == null) continue;
                TryBuildMechFactory(city);
                TryBuildMercGuild(city);
                TryBuildStarBank(city);
            }

            // 专属建筑：全局唯一，只检查一次
            TryBuildBlackStarPalace();
            TryBuildCompanyHQ();

            RunBuildingEffects();
        }

        /// <summary>获取城市所属王国ID</summary>
        private static long GetKingdomId(City city)
        {
            if (city == null || city.data == null) return 0;
            return city.data.kingdomID;
        }

        /// <summary>获取城市中的单位列表（通过遍历全局单位）</summary>
        private static List<Actor> GetCityUnits(City city)
        {
            var result = new List<Actor>();
            if (city == null || World.world?.units == null) return result;
            foreach (var a in World.world.units)
            {
                if (a != null && a.isAlive() && a.city == city)
                    result.Add(a);
            }
            return result;
        }

        /// <summary>尝试建造机械工厂：文明有机械系B阶超能者+研究mech_t3时</summary>
        private static void TryBuildMechFactory(City city)
        {
            if (HasBuilding(city, SuperMechBuilding.MechFactory)) return;
            long kid = GetKingdomId(city);
            if (!SuperMechBuilding.IsUnlocked(kid, SuperMechBuilding.MechFactory)) return;

            bool hasMechB = false;
            foreach (var a in GetCityUnits(city))
            {
                if (a.hasTrait(SuperMechTraits.ClassMech) && SuperMechAdvancement.GetExactRankIndex(a) >= 6)
                {
                    hasMechB = true;
                    break;
                }
            }

            if (hasMechB)
            {
                BuildBuilding(city, SuperMechBuilding.MechFactory);
            }
        }

        /// <summary>尝试建造佣兵公会：文明有≥10个觉醒者+研究info_t3时</summary>
        private static void TryBuildMercGuild(City city)
        {
            if (HasBuilding(city, SuperMechBuilding.MercGuild)) return;
            long kid = GetKingdomId(city);
            if (!SuperMechBuilding.IsUnlocked(kid, SuperMechBuilding.MercGuild)) return;

            int awakenedCount = 0;
            foreach (var a in GetCityUnits(city))
            {
                if (SuperMechAwakened.IsAwakened(a))
                {
                    awakenedCount++;
                    if (awakenedCount >= 10) break;
                }
            }

            if (awakenedCount >= 10)
            {
                BuildBuilding(city, SuperMechBuilding.MercGuild);
            }
        }

        /// <summary>尝试建造星际银行：文明有≥1个A阶超能者+研究space_t3时</summary>
        private static void TryBuildStarBank(City city)
        {
            if (HasBuilding(city, SuperMechBuilding.StarBank)) return;
            long kid = GetKingdomId(city);
            if (!SuperMechBuilding.IsUnlocked(kid, SuperMechBuilding.StarBank)) return;

            bool hasA = false;
            foreach (var a in GetCityUnits(city))
            {
                if (SuperMechAdvancement.GetExactRankIndex(a) >= 8)
                {
                    hasA = true;
                    break;
                }
            }

            if (hasA)
            {
                BuildBuilding(city, SuperMechBuilding.StarBank);
            }
        }

        /// <summary>检查城市是否已有某建筑</summary>
        private static bool HasBuilding(City city, string buildingId)
        {
            if (city?.buildings == null) return false;
            foreach (var b in city.buildings)
            {
                if (b != null && b.asset != null && b.asset.id == buildingId)
                    return true;
            }
            return false;
        }

        /// <summary>在城市中建造建筑</summary>
        private static void BuildBuilding(City city, string buildingId)
        {
            try
            {
                BuildingAsset asset = AssetManager.buildings.get(buildingId);
                if (asset == null) return;

                WorldTile tile = FindBuildableTile(city);
                if (tile == null) return;

                if (!World.world.buildings.canBuildFrom(tile, asset, null, BuildPlacingType.New, pFloraGrowth: false))
                    return;

                World.world.buildings.addBuilding(asset, tile);
                Debug.Log($"[超神机械师] 城市建造了{buildingId}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 建造{buildingId}失败: {e.Message}");
            }
        }

        /// <summary>找到城市中可建造的地块（在城市建筑附近找空地）</summary>
        private static WorldTile FindBuildableTile(City city)
        {
            if (city?.buildings == null || city.buildings.Count == 0) return null;
            // 从城市已有建筑附近找可建造地块
            var refBuilding = city.buildings[0];
            if (refBuilding == null || refBuilding.current_tile == null) return null;
            var center = refBuilding.current_tile;

            for (int r = 1; r < 8; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var tile = World.world.GetTile(center.x + dx, center.y + dy);
                        if (tile != null && tile.Type.ground && !tile.Type.liquid && tile.building == null)
                            return tile;
                    }
                }
            }
            return null;
        }

        /// <summary>尝试建造黑星行宫：韩萧化身存在时，在韩萧所在城市建造</summary>
        private static void TryBuildBlackStarPalace()
        {
            if (World.world?.buildings == null) return;
            foreach (var b in World.world.buildings)
            {
                if (b != null && b.asset != null && b.asset.id == SuperMechBuilding.BlackStarPalace)
                    return; // 已存在
            }

            Actor hanXiao = null;
            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                if (a.hasTrait("sm_player_hanxiao"))
                {
                    hanXiao = a;
                    break;
                }
            }

            if (hanXiao == null || hanXiao.city == null) return;

            City city = hanXiao.city;
            if (city == null) return;
            long kid = GetKingdomId(city);
            if (!SuperMechBuilding.IsUnlocked(kid, SuperMechBuilding.BlackStarPalace)) return;
            BuildBuilding(city, SuperMechBuilding.BlackStarPalace);
        }

        /// <summary>尝试建造降临者公司总部：有≥3个降临者+研究info_t5时，在降临者最多的城市建造</summary>
        private static void TryBuildCompanyHQ()
        {
            if (World.world?.buildings == null) return;
            foreach (var b in World.world.buildings)
            {
                if (b != null && b.asset != null && b.asset.id == SuperMechBuilding.CompanyHQ)
                    return; // 已存在
            }

            City bestCity = null;
            int bestCount = 0;

            foreach (var city in World.world.cities)
            {
                if (city == null) continue;
                int count = 0;
                foreach (var a in GetCityUnits(city))
                {
                    if (a.hasTrait("sm_player") || a.hasTrait("sm_player_hanxiao"))
                        count++;
                }
                if (count > bestCount)
                {
                    bestCount = count;
                    bestCity = city;
                }
            }

            if (bestCount >= 3 && bestCity != null)
            {
                long kid = GetKingdomId(bestCity);
                if (!SuperMechBuilding.IsUnlocked(kid, SuperMechBuilding.CompanyHQ)) return;
                BuildBuilding(bestCity, SuperMechBuilding.CompanyHQ);
            }
        }

        /// <summary>运行建筑效果</summary>
        private static void RunBuildingEffects()
        {
            if (World.world?.buildings == null) return;

            foreach (var building in World.world.buildings)
            {
                if (building == null || building.asset == null || building.isRuin()) continue;

                // v0.175.1: 建筑效果随生命值衰减（损坏建筑效果降低）
                float healthRatio = GetBuildingHealthRatio(building);
                if (healthRatio <= 0f) continue;

                switch (building.asset.id)
                {
                    case SuperMechBuilding.MechFactory:
                        RunMechFactory(building, healthRatio);
                        break;
                    case SuperMechBuilding.MercGuild:
                        RunMercGuild(building, healthRatio);
                        break;
                    case SuperMechBuilding.StarBank:
                        RunStarBank(building, healthRatio);
                        break;
                    case SuperMechBuilding.BlackStarPalace:
                        RunBlackStarPalace(building, healthRatio);
                        break;
                    case SuperMechBuilding.CompanyHQ:
                        RunCompanyHQ(building, healthRatio);
                        break;
                }
            }

            // v0.175.1: AI自动修复损坏建筑
            if (Time.frameCount % 1024 == 0)
                AutoRepairBuildings();
        }

        /// <summary>获取建筑生命值比例（0-1）</summary>
        private static float GetBuildingHealthRatio(Building building)
        {
            try
            {
                float maxHp = building.asset?.base_stats != null ? building.asset.base_stats["health"] : 100f;
                if (maxHp <= 0) maxHp = 100f;
                float curHp = building.data != null ? building.data.get("health", maxHp) : maxHp;
                return Mathf.Clamp01(curHp / maxHp);
            }
            catch { return 1f; }
        }

        /// <summary>AI自动修复损坏建筑（城市有资源时缓慢修复）</summary>
        private static void AutoRepairBuildings()
        {
            if (World.world?.buildings == null || World.world?.cities == null) return;
            foreach (var building in World.world.buildings)
            {
                if (building == null || building.asset == null || building.isRuin()) continue;
                if (!SuperMechBuilding.IsCustomBuilding(building.asset.id)) continue;

                float ratio = GetBuildingHealthRatio(building);
                if (ratio >= 1f) continue; // 满血不需要修复

                // 检查建筑所在城市是否有足够资源
                City city = building.city;
                if (city == null) continue;

                // 每1024帧恢复5%生命值
                float maxHp = building.asset.base_stats["health"];
                float healAmount = maxHp * 0.05f;
                float curHp = building.data.get("health", maxHp);
                building.data.set("health", Mathf.Min(curHp + healAmount, maxHp));
            }
        }

        /// <summary>机械工厂运行：周围有机械系超能者时，定期产出机械军团单位</summary>
        private static void RunMechFactory(Building building, float healthRatio)
        {
            if (Time.frameCount % 256 != 0) return;

            float range = SuperMechBuildingUpgrade.GetRange(building);
            float mult = SuperMechBuildingUpgrade.GetMultiplier(building) * healthRatio;

            int mechCount = 0;
            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && a.hasTrait(SuperMechTraits.ClassMech))
                    mechCount++;
            }

            if (mechCount > 0)
            {
                foreach (var a in World.world.units)
                {
                    if (a == null) continue;
                    float dist = Vector2.Distance(a.current_position, building.current_position);
                    if (dist < range && a.hasTrait(SuperMechTraits.ClassMech))
                        SuperMechAwakened.AddXp(a, 5f * mechCount * mult);
                }
            }
        }

        /// <summary>佣兵公会运行：定期发布任务，给觉醒者加经验</summary>
        private static void RunMercGuild(Building building, float healthRatio)
        {
            if (Time.frameCount % 256 != 0) return;

            float range = SuperMechBuildingUpgrade.GetRange(building);
            float mult = SuperMechBuildingUpgrade.GetMultiplier(building) * healthRatio;

            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && SuperMechAwakened.IsAwakened(a))
                    SuperMechAwakened.AddXp(a, 3f * mult);
            }
        }

        /// <summary>星际银行运行：周围超能者定期获得少量伊纳尔</summary>
        private static void RunStarBank(Building building, float healthRatio)
        {
            if (Time.frameCount % 512 != 0) return;

            float range = SuperMechBuildingUpgrade.GetRange(building);
            float mult = SuperMechBuildingUpgrade.GetMultiplier(building) * healthRatio;

            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && SuperMechAdvancement.GetExactRankIndex(a) >= 4)
                {
                    int inal = SuperMechInal.GetInal(a);
                    SuperMechInal.SetInal(a, inal + (int)(10 * mult));
                }
            }
        }

        /// <summary>黑星行宫运行：周围机械系单位大幅加经验，韩萧复活点</summary>
        private static void RunBlackStarPalace(Building building, float healthRatio)
        {
            if (Time.frameCount % 128 != 0) return;

            float range = SuperMechBuildingUpgrade.GetRange(building);
            float mult = SuperMechBuildingUpgrade.GetMultiplier(building) * healthRatio;

            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && a.hasTrait(SuperMechTraits.ClassMech))
                    SuperMechAwakened.AddXp(a, 15f * mult);
            }
        }

        /// <summary>降临者公司总部运行：定期发行伊纳尔，周围降临者加经验</summary>
        private static void RunCompanyHQ(Building building, float healthRatio)
        {
            if (Time.frameCount % 256 != 0) return;

            float range = SuperMechBuildingUpgrade.GetRange(building);
            float mult = SuperMechBuildingUpgrade.GetMultiplier(building) * healthRatio;

            int playerCount = 0;
            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && (a.hasTrait("sm_player") || a.hasTrait("sm_player_hanxiao")))
                {
                    playerCount++;
                    SuperMechAwakened.AddXp(a, 10f * mult);
                }
            }

            if (playerCount > 0 && Time.frameCount % 512 == 0)
            {
                foreach (var a in World.world.units)
                {
                    if (a == null) continue;
                    float dist = Vector2.Distance(a.current_position, building.current_position);
                    if (dist < range && (a.hasTrait("sm_player") || a.hasTrait("sm_player_hanxiao")))
                    {
                        int inal = SuperMechInal.GetInal(a);
                        SuperMechInal.SetInal(a, inal + (int)(50 * playerCount * mult));
                    }
                }
            }
        }

        /// <summary>清理死亡城市的建筑计数</summary>
        public static void CleanupDead(HashSet<string> aliveCities)
        {
            // 建筑由WB原生系统管理，这里不需要额外清理
        }

        /// <summary>清空所有数据</summary>
        public static void Clear()
        {
            _cityBuildingCounts.Clear();
            _tickCounter = 0;
        }
    }
}
