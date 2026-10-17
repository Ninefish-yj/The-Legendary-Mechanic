using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.231 星际贸易管理器（EVE/精英危险式）
    /// 商品买卖、价格波动、贸易路线、运输利润
    /// </summary>
    public static class TradeManager
    {
        private static Dictionary<TradeGoodType, int> _playerCargo = new Dictionary<TradeGoodType, int>();
        private static int _cargoCapacity = 100;
        private static List<TradeRoute> _tradeRoutes = new List<TradeRoute>();
        private static bool _initialized;

        // 商品名称
        private static readonly string[] GoodNames = {
            "矿石", "燃料", "食物", "科技零件", "奢侈品", "药品", "武器", "奴隶"
        };

        /// <summary>初始化贸易系统</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
                _playerCargo[type] = 0;
            Debug.Log("[超神机械师] 星际贸易系统初始化");
        }

        /// <summary>获取商品名称</summary>
        public static string GetGoodName(TradeGoodType type) => GoodNames[(int)type];

        /// <summary>获取玩家货舱中某商品数量</summary>
        public static int GetCargo(TradeGoodType type)
        {
            _playerCargo.TryGetValue(type, out int val);
            return val;
        }

        /// <summary>获取货舱已用容量</summary>
        public static int GetCargoUsed()
        {
            int total = 0;
            foreach (var kv in _playerCargo) total += kv.Value;
            return total;
        }

        /// <summary>获取货舱容量</summary>
        public static int GetCargoCapacity() => _cargoCapacity;

        /// <summary>增加货舱容量（科技/舰船升级）</summary>
        public static void AddCargoCapacity(int amount) => _cargoCapacity += amount;

        /// <summary>从星球买入商品（玩家花钱买）</summary>
        public static bool BuyGood(string planetId, TradeGoodType type, int amount)
        {
            var data = PlanetExplorationManager.GetPlanetData(planetId);
            if (data?.market == null) return false;
            if (!data.market.sellPrice.TryGetValue(type, out int price)) return false;
            if (!data.market.stock.TryGetValue(type, out int stock)) return false;
            if (stock < amount) return false;

            int totalCost = price * amount;
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (planet == null || planet.resource < totalCost) return false;
            if (GetCargoUsed() + amount > _cargoCapacity) return false;

            planet.resource -= totalCost;
            data.market.stock[type] -= amount;
            _playerCargo[type] += amount;
            // 买入后价格上涨
            data.market.sellPrice[type] = (int)(price * 1.05f);
            Debug.Log($"[超神机械师] 买入{amount}{GetGoodName(type)}，花费{totalCost}");
            return true;
        }

        /// <summary>向星球卖出商品（玩家赚钱）</summary>
        public static bool SellGood(string planetId, TradeGoodType type, int amount)
        {
            var data = PlanetExplorationManager.GetPlanetData(planetId);
            if (data?.market == null) return false;
            if (!data.market.buyPrice.TryGetValue(type, out int price)) return false;
            if (GetCargo(type) < amount) return false;

            int totalEarn = price * amount;
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (planet != null) planet.resource += totalEarn;
            data.market.stock[type] += amount;
            _playerCargo[type] -= amount;
            // 卖出后价格下跌
            data.market.buyPrice[type] = Mathf.Max(1, (int)(price * 0.95f));
            Debug.Log($"[超神机械师] 卖出{amount}{GetGoodName(type)}，获得{totalEarn}");
            return true;
        }

        /// <summary>计算两星球间某商品的贸易利润</summary>
        public static int CalculateProfit(string fromPlanet, string toPlanet, TradeGoodType type)
        {
            var fromData = PlanetExplorationManager.GetPlanetData(fromPlanet);
            var toData = PlanetExplorationManager.GetPlanetData(toPlanet);
            if (fromData?.market == null || toData?.market == null) return 0;
            if (!fromData.market.sellPrice.TryGetValue(type, out int buyPrice)) return 0;
            if (!toData.market.buyPrice.TryGetValue(type, out int sellPrice)) return 0;
            return sellPrice - buyPrice;
        }

        /// <summary>发现贸易路线（自动扫描高利润路线）</summary>
        public static List<TradeRoute> FindTradeRoutes(string fromPlanet)
        {
            var routes = new List<TradeRoute>();
            var planets = SuperMechStarSystem.GetAllPlanets();
            foreach (var to in planets)
            {
                if (to == null || to.id == fromPlanet) continue;
                foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
                {
                    int profit = CalculateProfit(fromPlanet, to.id, type);
                    if (profit > 20) // 只显示有利润的路线
                    {
                        routes.Add(new TradeRoute
                        {
                            id = $"{fromPlanet}_{to.id}_{type}",
                            fromPlanet = fromPlanet,
                            toPlanet = to.id,
                            goodType = type,
                            profitPerUnit = profit,
                            distance = (int)Vector2.Distance(
                                new Vector2(to.x, to.y),
                                new Vector2(SuperMechStarSystem.GetPlanet(fromPlanet)?.x ?? 0, SuperMechStarSystem.GetPlanet(fromPlanet)?.y ?? 0)),
                            isActive = false
                        });
                    }
                }
            }
            routes.Sort((a, b) => b.profitPerUnit.CompareTo(a.profitPerUnit));
            return routes;
        }

        /// <summary>执行自动贸易（消耗燃料，往返获利）</summary>
        public static bool ExecuteTradeRoute(TradeRoute route)
        {
            int fuelCost = route.distance / 10;
            var fromPlanet = SuperMechStarSystem.GetPlanet(route.fromPlanet);
            if (fromPlanet == null || fromPlanet.resource < fuelCost) return false;

            // 简化：直接计算利润
            int maxUnits = Mathf.Min(_cargoCapacity - GetCargoUsed(), 50);
            if (maxUnits <= 0) return false;

            fromPlanet.resource -= fuelCost;
            int totalProfit = route.profitPerUnit * maxUnits;
            var toPlanet = SuperMechStarSystem.GetPlanet(route.toPlanet);
            if (toPlanet != null) toPlanet.resource += totalProfit;

            Debug.Log($"[超神机械师] 贸易路线{route.fromPlanet}→{route.toPlanet}：{maxUnits}单位{GetGoodName(route.goodType)}，利润{totalProfit}");
            return true;
        }

        /// <summary>获取所有商品的货舱状态文本</summary>
        public static string GetCargoStatusText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"货舱: {GetCargoUsed()}/{_cargoCapacity}");
            foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
            {
                int amount = GetCargo(type);
                if (amount > 0)
                    sb.AppendLine($"  {GetGoodName(type)}: {amount}");
            }
            return sb.ToString();
        }
    }
}
