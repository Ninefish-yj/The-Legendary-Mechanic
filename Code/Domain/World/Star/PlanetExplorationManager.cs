using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.231 星球探索管理器（无人深空式）
    /// 每个星球有独特气候/资源/生物群系，登陆探索触发随机事件
    /// </summary>
    public static class PlanetExplorationManager
    {
        private static Dictionary<string, PlanetExpansionData> _planetData = new Dictionary<string, PlanetExpansionData>();
        private static bool _initialized;

        // 气候名称
        private static readonly string[] ClimateNames = {
            "温带", "沙漠", "冰冻", "火山", "海洋", "气态", "剧毒", "辐射", "风暴", "天堂"
        };

        // 资源名称
        private static readonly string[] ResourceNames = {
            "矿物", "能源", "食物", "科技材料", "奢侈品", "生物材料"
        };

        // 生物群系名称
        private static readonly string[] BiomeNames = {
            "森林", "草原", "山地", "沼泽", "荒地", "珊瑚礁", "晶体洞", "熔岩", "冻原", "浮空岛"
        };

        /// <summary>初始化所有星球的扩展数据</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            var planets = SuperMechStarSystem.GetAllPlanets();
            foreach (var p in planets)
            {
                if (p == null) continue;
                if (!_planetData.ContainsKey(p.id))
                {
                    _planetData[p.id] = GeneratePlanetData(p);
                }
            }
            Debug.Log($"[超神机械师] 星球探索系统初始化：{_planetData.Count}个星球");
        }

        /// <summary>生成星球扩展数据（基于星球ID确定性随机）</summary>
        private static PlanetExpansionData GeneratePlanetData(PlanetData p)
        {
            var rng = new System.Random(p.id.GetHashCode());
            var data = new PlanetExpansionData
            {
                planetId = p.id,
                climate = (PlanetClimate)rng.Next(10),
                resourceType = (PlanetResourceType)rng.Next(6),
                biome = (PlanetBiome)rng.Next(10),
                dangerLevel = rng.Next(0, 11),
                isExplored = false,
                explorationCount = 0
            };

            // 宜居星球危险等级低
            if (data.climate == PlanetClimate.Temperate || data.climate == PlanetClimate.Paradise)
                data.dangerLevel = Mathf.Min(data.dangerLevel, 3);
            // 火山/辐射/剧毒危险等级高
            if (data.climate == PlanetClimate.Volcanic || data.climate == PlanetClimate.Radiated || data.climate == PlanetClimate.Toxic)
                data.dangerLevel = Mathf.Max(data.dangerLevel, 5);

            // 生成市场
            data.market = GenerateMarket(p.id, data);
            return data;
        }

        /// <summary>生成星球市场（供需价格基于气候和资源类型）</summary>
        private static PlanetMarket GenerateMarket(string planetId, PlanetExpansionData data)
        {
            var market = new PlanetMarket { planetId = planetId };
            var rng = new System.Random(planetId.GetHashCode() + 1);

            foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
            {
                int basePrice = GetBasePrice(type);
                // 特产商品价格低（产出多），非特产价格高
                bool isSpecialty = IsSpecialty(type, data.resourceType);
                int variance = rng.Next(-30, 30);
                int buy = isSpecialty ? basePrice / 2 + variance : basePrice + variance;
                int sell = isSpecialty ? basePrice / 3 + variance : basePrice * 2 + variance;
                market.buyPrice[type] = Mathf.Max(1, buy);
                market.sellPrice[type] = Mathf.Max(1, sell);
                market.stock[type] = isSpecialty ? rng.Next(50, 200) : rng.Next(0, 30);
                market.demand[type] = isSpecialty ? rng.Next(0, 30) : rng.Next(50, 100);
            }
            return market;
        }

        private static int GetBasePrice(TradeGoodType type)
        {
            return type switch
            {
                TradeGoodType.Ore => 50,
                TradeGoodType.Fuel => 80,
                TradeGoodType.Food => 30,
                TradeGoodType.TechParts => 200,
                TradeGoodType.Luxury => 500,
                TradeGoodType.Medicine => 150,
                TradeGoodType.Weapons => 300,
                TradeGoodType.Slaves => 400,
                _ => 100
            };
        }

        private static bool IsSpecialty(TradeGoodType good, PlanetResourceType resource)
        {
            return (good == TradeGoodType.Ore && resource == PlanetResourceType.Mineral) ||
                   (good == TradeGoodType.Fuel && resource == PlanetResourceType.Energy) ||
                   (good == TradeGoodType.Food && resource == PlanetResourceType.Food) ||
                   (good == TradeGoodType.TechParts && resource == PlanetResourceType.Tech) ||
                   (good == TradeGoodType.Luxury && resource == PlanetResourceType.Luxury) ||
                   (good == TradeGoodType.Medicine && resource == PlanetResourceType.Biological);
        }

        /// <summary>获取星球扩展数据</summary>
        public static PlanetExpansionData GetPlanetData(string planetId)
        {
            if (!_initialized) Init();
            _planetData.TryGetValue(planetId, out var data);
            return data;
        }

        /// <summary>登陆探索（触发随机事件）</summary>
        public static ExplorationEvent Explore(string planetId, Actor explorer = null)
        {
            var data = GetPlanetData(planetId);
            if (data == null) return null;

            data.isExplored = true;
            data.explorationCount++;

            var rng = new System.Random(DateTime.Now.Millisecond + planetId.GetHashCode());
            int roll = rng.Next(100);
            ExplorationEventType type;

            // 危险等级影响事件概率
            int dangerChance = data.dangerLevel * 3;
            if (roll < dangerChance) type = ExplorationEventType.Danger;
            else if (roll < dangerChance + 15) type = ExplorationEventType.Ruins;
            else if (roll < dangerChance + 30) type = ExplorationEventType.RareResource;
            else if (roll < dangerChance + 40) type = ExplorationEventType.AlienLife;
            else if (roll < dangerChance + 50) type = ExplorationEventType.Treasure;
            else if (roll < dangerChance + 60) type = ExplorationEventType.Signal;
            else if (roll < dangerChance + 70) type = ExplorationEventType.Anomaly;
            else type = ExplorationEventType.Nothing;

            var evt = GenerateEvent(type, data, rng);

            // 奖励资源
            if (evt.resourceReward > 0)
            {
                var planet = SuperMechStarSystem.GetPlanet(planetId);
                if (planet != null) planet.resource += evt.resourceReward;
            }

            // 危险事件伤害探索者
            if (evt.isDangerous && explorer != null && evt.dangerDamage > 0)
            {
                explorer.data.health -= evt.dangerDamage;
                if (explorer.data.health <= 0) explorer.die();
            }

            Debug.Log($"[超神机械师] 探索{planetId}：{evt.title}，奖励{evt.resourceReward}资源");
            return evt;
        }

        private static ExplorationEvent GenerateEvent(ExplorationEventType type, PlanetExpansionData data, System.Random rng)
        {
            var evt = new ExplorationEvent { type = type };
            switch (type)
            {
                case ExplorationEventType.Ruins:
                    evt.title = "发现古代遗迹";
                    evt.description = $"在{GetBiomeName(data.biome)}深处发现了一座古代文明遗迹，刻有未知文字。";
                    evt.resourceReward = rng.Next(100, 300);
                    evt.expReward = rng.Next(50, 150);
                    break;
                case ExplorationEventType.RareResource:
                    evt.title = $"发现稀有{GetResourceName(data.resourceType)}矿脉";
                    evt.description = $"探测器发现了一处高品质{GetResourceName(data.resourceType)}矿脉，储量丰富！";
                    evt.resourceReward = rng.Next(200, 500);
                    break;
                case ExplorationEventType.AlienLife:
                    evt.title = "遭遇外星生物";
                    evt.description = $"一群奇特的{GetClimateName(data.climate)}星球生物出现在视野中，似乎具有攻击性。";
                    evt.isDangerous = data.dangerLevel > 3;
                    evt.dangerDamage = evt.isDangerous ? rng.Next(10, 50) : 0;
                    evt.expReward = rng.Next(30, 100);
                    break;
                case ExplorationEventType.Danger:
                    evt.title = $"危险：{GetClimateName(data.climate)}灾害";
                    evt.description = $"突如其来的{GetClimateName(data.climate)}自然灾害袭击了探险队！";
                    evt.isDangerous = true;
                    evt.dangerDamage = rng.Next(20, 80) + data.dangerLevel * 5;
                    break;
                case ExplorationEventType.Treasure:
                    evt.title = "发现宝藏";
                    evt.description = "在一个隐蔽的洞穴中发现了装满珍宝的容器！";
                    evt.resourceReward = rng.Next(300, 800);
                    evt.expReward = rng.Next(100, 200);
                    break;
                case ExplorationEventType.Signal:
                    evt.title = "接收到神秘信号";
                    evt.description = "通讯阵列接收到一段来自深空的加密信号，来源不明。";
                    evt.expReward = rng.Next(80, 160);
                    break;
                case ExplorationEventType.Anomaly:
                    evt.title = "空间异常";
                    evt.description = "星球轨道附近出现了不稳定的空间裂隙，散发着奇异能量。";
                    evt.resourceReward = rng.Next(50, 150);
                    evt.expReward = rng.Next(60, 120);
                    break;
                default:
                    evt.title = "一无所获";
                    evt.description = "经过一番探索，没有发现什么有价值的东西。";
                    break;
            }
            return evt;
        }

        public static string GetClimateName(PlanetClimate c) => ClimateNames[(int)c];
        public static string GetResourceName(PlanetResourceType r) => ResourceNames[(int)r];
        public static string GetBiomeName(PlanetBiome b) => BiomeNames[(int)b];

        /// <summary>市场价格波动tick</summary>
        public static void MarketTick(float deltaTime)
        {
            if (!_initialized) return;
            // 每30秒波动一次价格
            foreach (var data in _planetData.Values)
            {
                if (data.market == null) continue;
                var rng = new System.Random();
                foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
                {
                    int fluctuation = rng.Next(-5, 6);
                    if (data.market.buyPrice.ContainsKey(type))
                        data.market.buyPrice[type] = Mathf.Max(1, data.market.buyPrice[type] + fluctuation);
                    if (data.market.sellPrice.ContainsKey(type))
                        data.market.sellPrice[type] = Mathf.Max(1, data.market.sellPrice[type] + fluctuation);
                }
            }
        }
    }
}
