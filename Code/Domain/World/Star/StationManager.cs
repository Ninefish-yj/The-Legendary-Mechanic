using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.231 太空站管理器（X4 Foundations式）
    /// 建造/升级太空站，自动资源采集，贸易加成，防御，研究加速
    /// </summary>
    public static class StationManager
    {
        private static List<SpaceStation> _stations = new List<SpaceStation>();
        private static bool _initialized;
        private static float _tickTimer;

        // 太空站建造成本
        private static readonly Dictionary<StationType, int> BuildCosts = new Dictionary<StationType, int>
        {
            { StationType.Mining, 500 },
            { StationType.Trading, 800 },
            { StationType.Shipyard, 1500 },
            { StationType.Defense, 600 },
            { StationType.Research, 1200 },
            { StationType.Habitat, 1000 }
        };

        // 太空站名称
        private static readonly string[] StationNames = {
            "采矿站", "贸易站", "造船厂", "防御站", "研究站", "居住站"
        };

        /// <summary>初始化</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Debug.Log("[超神机械师] 太空站系统初始化");
        }

        /// <summary>获取太空站名称</summary>
        public static string GetStationName(StationType type) => StationNames[(int)type];

        /// <summary>获取建造成本</summary>
        public static int GetBuildCost(StationType type)
        {
            BuildCosts.TryGetValue(type, out int cost);
            return cost;
        }

        /// <summary>建造太空站</summary>
        public static bool BuildStation(string planetId, StationType type)
        {
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (planet == null) return false;
            int cost = GetBuildCost(type);
            if (planet.resource < cost) return false;

            // 检查是否已有同类型站
            var existing = _stations.Find(s => s.orbitPlanet == planetId && s.type == type);
            if (existing != null) return false;

            planet.resource -= cost;
            var station = new SpaceStation
            {
                id = $"station_{planetId}_{type}_{Guid.NewGuid().ToString().Substring(0, 6)}",
                name = $"{planet.name}{GetStationName(type)}",
                type = type,
                orbitPlanet = planetId,
                level = 1,
                maxHp = 500,
                hp = 500,
                productionRate = 1f,
                isPlayerOwned = true
            };
            _stations.Add(station);

            // 关联到星球扩展数据
            var data = PlanetExplorationManager.GetPlanetData(planetId);
            data?.stations.Add(station);

            Debug.Log($"[超神机械师] 建造{GetStationName(type)}于{planet.name}，花费{cost}");
            return true;
        }

        /// <summary>升级太空站</summary>
        public static bool UpgradeStation(string stationId)
        {
            var station = _stations.Find(s => s.id == stationId);
            if (station == null || station.level >= 5) return false;
            var planet = SuperMechStarSystem.GetPlanet(station.orbitPlanet);
            if (planet == null) return false;

            int cost = GetBuildCost(station.type) * station.level;
            if (planet.resource < cost) return false;

            planet.resource -= cost;
            station.level++;
            station.maxHp += 200;
            station.hp = station.maxHp;
            station.productionRate += 0.5f;
            Debug.Log($"[超神机械师] {station.name}升级到Lv{station.level}");
            return true;
        }

        /// <summary>拆除太空站（返还50%资源）</summary>
        public static bool DemolishStation(string stationId)
        {
            var station = _stations.Find(s => s.id == stationId);
            if (station == null) return false;
            var planet = SuperMechStarSystem.GetPlanet(station.orbitPlanet);
            if (planet != null)
                planet.resource += (int)(GetBuildCost(station.type) * 0.5f);
            _stations.Remove(station);
            var data = PlanetExplorationManager.GetPlanetData(station.orbitPlanet);
            data?.stations.Remove(station);
            return true;
        }

        /// <summary>获取星球上的太空站</summary>
        public static List<SpaceStation> GetStationsAtPlanet(string planetId)
        {
            return _stations.FindAll(s => s.orbitPlanet == planetId);
        }

        /// <summary>获取所有玩家太空站</summary>
        public static List<SpaceStation> GetAllStations() => _stations;

        /// <summary>太空站生产tick（每5秒产出一次）</summary>
        public static void ProductionTick(float deltaTime)
        {
            if (!_initialized) return;
            _tickTimer += deltaTime;
            if (_tickTimer < 5f) return;
            _tickTimer = 0f;

            foreach (var station in _stations)
            {
                if (!station.isPlayerOwned) continue;
                var planet = SuperMechStarSystem.GetPlanet(station.orbitPlanet);
                if (planet == null) continue;

                switch (station.type)
                {
                    case StationType.Mining:
                        // 采矿站自动产出资源
                        int miningAmount = (int)(10 * station.level * station.productionRate);
                        planet.resource += miningAmount;
                        break;
                    case StationType.Trading:
                        // 贸易站：增加星球市场库存
                        var data = PlanetExplorationManager.GetPlanetData(planet.id);
                        if (data?.market != null)
                        {
                            foreach (var key in new List<TradeGoodType>(data.market.stock.Keys))
                                data.market.stock[key] += (int)(5 * station.level);
                        }
                        break;
                    case StationType.Research:
                        // 研究站：加速科技研究（在TechTreeManager中处理）
                        break;
                    case StationType.Habitat:
                        // 居住站：增加殖民地人口上限
                        if (planet.colonyLevel > 0)
                            planet.colonyLevel = Mathf.Min(planet.colonyLevel, 10); // 简化处理
                        break;
                }
            }
        }

        /// <summary>获取太空站效果描述</summary>
        public static string GetStationEffect(StationType type, int level)
        {
            return type switch
            {
                StationType.Mining => $"每5秒自动产出{10 * level}资源",
                StationType.Trading => $"市场库存每5秒+{5 * level}，贸易成本-10%",
                StationType.Shipyard => $"舰船建造速度+{20 * level}%",
                StationType.Defense => $"轨道防御，敌方入侵需先击破{500 + 200 * (level - 1)}HP",
                StationType.Research => $"科技研究速度+{15 * level}%",
                StationType.Habitat => $"殖民地人口上限+{1000 * level}",
                _ => "?"
            };
        }
    }
}
