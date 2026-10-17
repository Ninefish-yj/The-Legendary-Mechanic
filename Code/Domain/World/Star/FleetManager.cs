using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.229 舰队管理器：舰队建造、星际航行、舰队战斗、轨道轰炸、星球入侵
    /// 参考Stellaris/群星的舰队系统
    /// </summary>
    public static class FleetManager
    {
        private static List<FleetData> _fleets = new List<FleetData>();
        private static bool _initialized;

        public static List<FleetData> Fleets => _fleets;

        // 舰船建造成本（资源）
        private static readonly Dictionary<ShipType, float> ShipCosts = new Dictionary<ShipType, float>
        {
            { ShipType.Scout, 50 },
            { ShipType.Corvette, 100 },
            { ShipType.Frigate, 200 },
            { ShipType.Cruiser, 400 },
            { ShipType.Battleship, 800 },
            { ShipType.Carrier, 1000 },
            { ShipType.Transport, 150 },
            { ShipType.ColonyShip, 500 }
        };

        // 舰船属性模板
        private static readonly Dictionary<ShipType, ShipData> ShipTemplates = new Dictionary<ShipType, ShipData>
        {
            { ShipType.Scout, new ShipData { type = ShipType.Scout, maxHp = 50, attack = 5, defense = 2, speed = 10, crewCapacity = 5 } },
            { ShipType.Corvette, new ShipData { type = ShipType.Corvette, maxHp = 100, attack = 15, defense = 8, speed = 8, crewCapacity = 20 } },
            { ShipType.Frigate, new ShipData { type = ShipType.Frigate, maxHp = 200, attack = 30, defense = 15, speed = 6, crewCapacity = 50 } },
            { ShipType.Cruiser, new ShipData { type = ShipType.Cruiser, maxHp = 400, attack = 60, defense = 30, speed = 5, crewCapacity = 100 } },
            { ShipType.Battleship, new ShipData { type = ShipType.Battleship, maxHp = 800, attack = 120, defense = 60, speed = 3, crewCapacity = 200 } },
            { ShipType.Carrier, new ShipData { type = ShipType.Carrier, maxHp = 600, attack = 40, defense = 40, speed = 4, crewCapacity = 500 } },
            { ShipType.Transport, new ShipData { type = ShipType.Transport, maxHp = 150, attack = 5, defense = 10, speed = 5, crewCapacity = 200 } },
            { ShipType.ColonyShip, new ShipData { type = ShipType.ColonyShip, maxHp = 300, attack = 0, defense = 20, speed = 4, crewCapacity = 100 } }
        };

        /// <summary>初始化舰队系统</summary>
        public static void Init()
        {
            if (_initialized) return;
            _fleets = new List<FleetData>();
            _initialized = true;
        }

        /// <summary>创建舰队</summary>
        public static FleetData CreateFleet(string name, string ownerKingdom, string homeSystem)
        {
            var fleet = new FleetData
            {
                id = $"fleet_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                name = name,
                ownerKingdom = ownerKingdom,
                status = FleetStatus.Idle,
                currentSystem = homeSystem,
                fuel = 100,
                maxFuel = 100,
                morale = 100
            };
            _fleets.Add(fleet);
            Debug.Log($"[超神机械师] 创建舰队：{name}");
            return fleet;
        }

        /// <summary>建造舰船</summary>
        public static bool BuildShip(FleetData fleet, ShipType shipType, string shipName = null)
        {
            if (fleet == null) return false;
            float cost = ShipCosts[shipType];

            // 检查资源（从主星球扣除）
            var mainPlanet = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (mainPlanet == null || mainPlanet.resource < cost)
            {
                Debug.LogWarning($"[超神机械师] 资源不足，无法建造{shipType}（需要{cost}）");
                return false;
            }

            mainPlanet.resource -= cost;
            var template = ShipTemplates[shipType];
            var ship = new ShipData
            {
                id = $"ship_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                name = shipName ?? $"{shipType}-{fleet.ships.Count + 1}",
                type = shipType,
                hp = template.maxHp,
                maxHp = template.maxHp,
                attack = template.attack,
                defense = template.defense,
                speed = template.speed,
                crewCapacity = template.crewCapacity
            };
            fleet.ships.Add(ship);
            Debug.Log($"[超神机械师] 建造舰船：{ship.name}（{shipType}）");
            return true;
        }

        /// <summary>派遣舰队到目标星系</summary>
        public static bool SendFleet(string fleetId, string targetSystem)
        {
            var fleet = GetFleet(fleetId);
            if (fleet == null || fleet.status == FleetStatus.Traveling) return false;

            float distance = GalaxyManager.GetDistance(fleet.currentSystem, targetSystem);
            if (distance > fleet.fuel * 10) // 每单位燃料航行10光年
            {
                Debug.LogWarning($"[超神机械师] 燃料不足，无法航行到{targetSystem}（距离{distance:F0}光年）");
                return false;
            }

            fleet.status = FleetStatus.Traveling;
            fleet.targetSystem = targetSystem;
            fleet.travelProgress = 0;
            Debug.Log($"[超神机械师] 舰队{fleet.name}出发前往{targetSystem}（距离{distance:F0}光年）");
            return true;
        }

        /// <summary>舰队航行tick</summary>
        public static void FleetTick(float deltaTime)
        {
            foreach (var fleet in _fleets)
            {
                if (fleet.status != FleetStatus.Traveling) continue;

                float distance = GalaxyManager.GetDistance(fleet.currentSystem, fleet.targetSystem);
                float avgSpeed = 0;
                foreach (var s in fleet.ships) avgSpeed += s.speed;
                if (fleet.ships.Count > 0) avgSpeed /= fleet.ships.Count;

                // 航行速度：平均速度*deltaTime，距离换算
                float progressDelta = (avgSpeed * deltaTime * 0.1f) / Mathf.Max(distance, 1f);
                fleet.travelProgress += progressDelta;
                fleet.fuel -= progressDelta * distance * 0.01f; // 消耗燃料

                if (fleet.travelProgress >= 1f)
                {
                    // 到达目标星系
                    fleet.currentSystem = fleet.targetSystem;
                    fleet.targetSystem = null;
                    fleet.travelProgress = 0;
                    fleet.status = FleetStatus.Orbiting;
                    GalaxyManager.VisitSystem(fleet.currentSystem);

                    var sys = GalaxyManager.GetSystem(fleet.currentSystem);
                    Debug.Log($"[超神机械师] 舰队{fleet.name}到达{sys?.name}");

                    // 检查是否有敌方舰队/外星文明
                    CheckArrivalConflict(fleet);
                }
            }
        }

        /// <summary>检查到达时的冲突</summary>
        private static void CheckArrivalConflict(FleetData fleet)
        {
            var sys = GalaxyManager.GetSystem(fleet.currentSystem);
            if (sys == null) return;

            // 有外星文明控制
            if (!string.IsNullOrEmpty(sys.ownerCiv))
            {
                var civ = GalaxyManager.GetCivilization(sys.ownerCiv);
                var rel = GalaxyManager.GetRelation("player", sys.ownerCiv);
                if (rel != null && (rel.state == DiplomacyState.War || rel.state == DiplomacyState.Hostile))
                {
                    // 触发战斗
                    var enemyFleet = GetDefenseFleet(sys.ownerCiv, fleet.currentSystem);
                    if (enemyFleet != null)
                    {
                        ResolveSpaceBattle(fleet, enemyFleet);
                    }
                }
            }
        }

        /// <summary>获取防御舰队（外星文明的防御力量）</summary>
        private static FleetData GetDefenseFleet(string civId, string systemId)
        {
            var civ = GalaxyManager.GetCivilization(civId);
            if (civ == null) return null;

            // 根据军事力量生成防御舰队
            var defenseFleet = new FleetData
            {
                id = $"defense_{civId}_{systemId}",
                name = $"{civ.name}防御舰队",
                ownerKingdom = civId,
                status = FleetStatus.Orbiting,
                currentSystem = systemId
            };

            int shipCount = Mathf.CeilToInt(civ.military / 50f);
            for (int i = 0; i < shipCount; i++)
            {
                var types = new[] { ShipType.Corvette, ShipType.Frigate, ShipType.Cruiser };
                var type = types[UnityEngine.Random.Range(0, types.Length)];
                var template = ShipTemplates[type];
                defenseFleet.ships.Add(new ShipData
                {
                    id = $"def_ship_{i}",
                    name = $"{type}-{i}",
                    type = type,
                    hp = template.maxHp,
                    maxHp = template.maxHp,
                    attack = template.attack,
                    defense = template.defense,
                    speed = template.speed
                });
            }
            return defenseFleet;
        }

        /// <summary>解决星际战斗</summary>
        public static SpaceBattleResult ResolveSpaceBattle(FleetData attacker, FleetData defender)
        {
            var result = new SpaceBattleResult
            {
                attackerFleet = attacker.id,
                defenderFleet = defender.id
            };

            float atkPower = attacker.GetTotalAttack();
            float defPower = defender.GetTotalAttack() + defender.GetTotalDefense() * 0.5f;

            // 多轮战斗模拟
            int rounds = 0;
            while (attacker.ships.Count > 0 && defender.ships.Count > 0 && rounds < 10)
            {
                rounds++;
                // 攻击方造成伤害
                float atkDamage = atkPower * (0.8f + UnityEngine.Random.value * 0.4f);
                DistributeDamage(defender, atkDamage);

                if (defender.ships.Count == 0) break;

                // 防御方反击
                float defDamage = defPower * (0.8f + UnityEngine.Random.value * 0.4f);
                DistributeDamage(attacker, defDamage);
            }

            // 判定胜负
            if (attacker.ships.Count > 0 && defender.ships.Count == 0)
            {
                result.winner = "attacker";
                result.description = $"{attacker.name}击败了{defender.name}";
            }
            else if (defender.ships.Count > 0 && attacker.ships.Count == 0)
            {
                result.winner = "defender";
                result.description = $"{defender.name}击败了{attacker.name}";
            }
            else
            {
                result.winner = "draw";
                result.description = "双方战平";
            }

            result.attackerLosses = 1f - (attacker.ships.Count / (float)Mathf.Max(attacker.ships.Count + result.destroyedShips.Count, 1));
            result.defenderLosses = 1f - (defender.ships.Count / (float)Mathf.Max(defender.ships.Count + 1, 1));

            Debug.Log($"[超神机械师] 星际战斗：{result.description}（{rounds}轮）");
            return result;
        }

        /// <summary>分配伤害到舰船</summary>
        private static void DistributeDamage(FleetData fleet, float damage)
        {
            while (damage > 0 && fleet.ships.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, fleet.ships.Count);
                var ship = fleet.ships[idx];
                float effectiveDamage = Mathf.Max(1, damage - ship.defense * 0.3f);
                ship.hp -= effectiveDamage;
                damage -= effectiveDamage;

                if (ship.hp <= 0)
                {
                    fleet.ships.RemoveAt(idx);
                }
            }
        }

        /// <summary>轨道轰炸（降低星球殖民地等级和资源）</summary>
        public static bool OrbitalBombardment(string fleetId, string planetId)
        {
            var fleet = GetFleet(fleetId);
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (fleet == null || planet == null) return false;
            if (fleet.currentSystem != planet.parentStar) return false;

            fleet.status = FleetStatus.Bombarding;
            float damage = fleet.GetTotalAttack() * 0.1f;

            // 降低殖民地等级
            if (planet.colonyLevel > 0)
            {
                planet.resource = Mathf.Max(0, planet.resource - damage);
                if (planet.resource <= 0 && planet.colonyLevel > 0)
                {
                    planet.colonyLevel--;
                    planet.resource = planet.maxResource * 0.5f;
                    Debug.Log($"[超神机械师] 轨道轰炸：{planet.name}殖民地等级降至{planet.colonyLevel}");
                }
            }

            // 杀伤驻留单位
            if (planet.units.Count > 0)
            {
                int kills = Mathf.CeilToInt(damage / 50f);
                for (int i = 0; i < kills && planet.units.Count > 0; i++)
                {
                    planet.units.RemoveAt(UnityEngine.Random.Range(0, planet.units.Count));
                }
            }

            fleet.status = FleetStatus.Orbiting;
            return true;
        }

        /// <summary>星球入侵（登陆作战）</summary>
        public static bool InvadePlanet(string fleetId, string planetId)
        {
            var fleet = GetFleet(fleetId);
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (fleet == null || planet == null) return false;
            if (fleet.currentSystem != planet.parentStar) return false;

            int attackerTroops = fleet.GetTotalCrew();
            int defenderTroops = planet.units.Count * 10 + planet.colonyLevel * 50;

            if (attackerTroops <= 0) return false;

            fleet.status = FleetStatus.Invading;

            if (attackerTroops > defenderTroops)
            {
                // 入侵成功
                planet.ownerKingdom = fleet.ownerKingdom;
                planet.colonyLevel = Mathf.Max(1, planet.colonyLevel);
                // 登陆部队成为驻留单位
                foreach (var ship in fleet.ships)
                {
                    foreach (var crew in ship.crew)
                    {
                        planet.units.Add(crew);
                    }
                    ship.crew.Clear();
                }
                Debug.Log($"[超神机械师] 入侵成功：{fleet.name}占领{planet.name}");
                fleet.status = FleetStatus.Orbiting;
                return true;
            }
            else
            {
                // 入侵失败，损失部队
                int losses = Mathf.CeilToInt(attackerTroops * 0.5f);
                foreach (var ship in fleet.ships)
                {
                    while (losses > 0 && ship.crew.Count > 0)
                    {
                        ship.crew.RemoveAt(ship.crew.Count - 1);
                        losses--;
                    }
                }
                fleet.morale = Mathf.Max(0, fleet.morale - 20);
                Debug.Log($"[超神机械师] 入侵失败：{fleet.name}在{planet.name}损失惨重");
                fleet.status = FleetStatus.Retreating;
                return false;
            }
        }

        /// <summary>获取舰队</summary>
        public static FleetData GetFleet(string id)
        {
            return _fleets.Find(f => f.id == id);
        }

        /// <summary>获取某王国的所有舰队</summary>
        public static List<FleetData> GetFleetsByOwner(string owner)
        {
            return _fleets.FindAll(f => f.ownerKingdom == owner);
        }

        /// <summary>获取某星系的所有舰队</summary>
        public static List<FleetData> GetFleetsInSystem(string systemId)
        {
            return _fleets.FindAll(f => f.currentSystem == systemId);
        }

        /// <summary>添加船员到舰船</summary>
        public static bool AddCrewToShip(ShipData ship, ActorSnapshot unit)
        {
            if (ship == null || unit == null) return false;
            if (ship.crew.Count >= ship.crewCapacity) return false;
            ship.crew.Add(unit);
            return true;
        }

        /// <summary>重置</summary>
        public static void Reset()
        {
            _fleets = new List<FleetData>();
            _initialized = false;
        }
    }
}
