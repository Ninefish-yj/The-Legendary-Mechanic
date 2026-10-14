using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>殖民地建筑类型</summary>
    public enum ColonyBuildingType
    {
        MiningStation,    // 采矿站：提升资源产出
        ResearchStation,  // 研究站：产出科技点
        DefenseTurret,    // 防御塔：降低事件损失
        Habitat,          // 居住舱：增加驻留单位上限
        Spaceport,        // 太空港：提升航行速度
        Refinery,         // 精炼厂：提升资源转化效率
        ShieldGenerator,  // 护盾发生器：免疫太空风暴
        CommandCenter     // 指挥中心：殖民地核心
    }

    /// <summary>殖民地建筑定义</summary>
    public class ColonyBuildingDef
    {
        public ColonyBuildingType type;
        public string nameKey;
        public string descKey;
        public string icon;
        public int maxLevel;
        public float baseCost;          // 基础伊纳尔消耗
        public float costMultiplier;    // 每级消耗倍率
        public float baseEffect;        // 基础效果值
        public float effectPerLevel;    // 每级效果增量
        public int requiredColonyLevel; // 需要殖民地等级
    }

    /// <summary>殖民地建筑实例</summary>
    public class ColonyBuilding
    {
        public ColonyBuildingType type;
        public int level;
        public bool isBuilding;         // 是否正在建设
        public float buildProgress;     // 建设进度0-1
    }

    /// <summary>殖民地建设系统</summary>
    internal static class ColonyManager
    {
        private static readonly Dictionary<ColonyBuildingType, ColonyBuildingDef> _defs = new Dictionary<ColonyBuildingType, ColonyBuildingDef>();
        private static bool _initialized;

        /// <summary>初始化建筑定义</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterBuildings();
        }

        private static void RegisterBuildings()
        {
            _defs[ColonyBuildingType.CommandCenter] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.CommandCenter,
                nameKey = "sm_colony_cmd_name",
                descKey = "sm_colony_cmd_desc",
                icon = "ui/Icons/actor_traits/iconCrown",
                maxLevel = 5,
                baseCost = 500f,
                costMultiplier = 2.0f,
                baseEffect = 1f,
                effectPerLevel = 0.5f,
                requiredColonyLevel = 0
            };
            _defs[ColonyBuildingType.MiningStation] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.MiningStation,
                nameKey = "sm_colony_mine_name",
                descKey = "sm_colony_mine_desc",
                icon = "ui/Icons/actor_traits/iconMining",
                maxLevel = 10,
                baseCost = 200f,
                costMultiplier = 1.5f,
                baseEffect = 10f,
                effectPerLevel = 10f,
                requiredColonyLevel = 1
            };
            _defs[ColonyBuildingType.ResearchStation] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.ResearchStation,
                nameKey = "sm_colony_research_name",
                descKey = "sm_colony_research_desc",
                icon = "ui/Icons/actor_traits/iconBook",
                maxLevel = 8,
                baseCost = 300f,
                costMultiplier = 1.6f,
                baseEffect = 2f,
                effectPerLevel = 2f,
                requiredColonyLevel = 2
            };
            _defs[ColonyBuildingType.DefenseTurret] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.DefenseTurret,
                nameKey = "sm_colony_defense_name",
                descKey = "sm_colony_defense_desc",
                icon = "ui/Icons/actor_traits/iconSword",
                maxLevel = 8,
                baseCost = 250f,
                costMultiplier = 1.5f,
                baseEffect = 0.1f,
                effectPerLevel = 0.1f,
                requiredColonyLevel = 1
            };
            _defs[ColonyBuildingType.Habitat] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.Habitat,
                nameKey = "sm_colony_habitat_name",
                descKey = "sm_colony_habitat_desc",
                icon = "ui/Icons/actor_traits/iconHouse",
                maxLevel = 10,
                baseCost = 150f,
                costMultiplier = 1.4f,
                baseEffect = 5f,
                effectPerLevel = 5f,
                requiredColonyLevel = 1
            };
            _defs[ColonyBuildingType.Spaceport] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.Spaceport,
                nameKey = "sm_colony_spaceport_name",
                descKey = "sm_colony_spaceport_desc",
                icon = "ui/Icons/actor_traits/iconShip",
                maxLevel = 5,
                baseCost = 400f,
                costMultiplier = 1.8f,
                baseEffect = 0.1f,
                effectPerLevel = 0.1f,
                requiredColonyLevel = 3
            };
            _defs[ColonyBuildingType.Refinery] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.Refinery,
                nameKey = "sm_colony_refinery_name",
                descKey = "sm_colony_refinery_desc",
                icon = "ui/Icons/actor_traits/iconFactory",
                maxLevel = 6,
                baseCost = 350f,
                costMultiplier = 1.7f,
                baseEffect = 0.15f,
                effectPerLevel = 0.15f,
                requiredColonyLevel = 2
            };
            _defs[ColonyBuildingType.ShieldGenerator] = new ColonyBuildingDef
            {
                type = ColonyBuildingType.ShieldGenerator,
                nameKey = "sm_colony_shield_name",
                descKey = "sm_colony_shield_desc",
                icon = "ui/Icons/actor_traits/iconShield",
                maxLevel = 3,
                baseCost = 600f,
                costMultiplier = 2.0f,
                baseEffect = 0.3f,
                effectPerLevel = 0.3f,
                requiredColonyLevel = 4
            };
        }

        /// <summary>获取建筑定义</summary>
        public static ColonyBuildingDef GetDef(ColonyBuildingType type)
        {
            _defs.TryGetValue(type, out var def);
            return def;
        }

        /// <summary>获取所有建筑定义</summary>
        public static List<ColonyBuildingDef> GetAllDefs() => new List<ColonyBuildingDef>(_defs.Values);

        /// <summary>计算升级消耗</summary>
        public static float GetUpgradeCost(ColonyBuildingDef def, int currentLevel)
        {
            return def.baseCost * Mathf.Pow(def.costMultiplier, currentLevel);
        }

        /// <summary>获取建筑当前效果值</summary>
        public static float GetEffect(ColonyBuildingDef def, int level)
        {
            return def.baseEffect + def.effectPerLevel * (level - 1);
        }

        /// <summary>计算殖民地总资源产出（每tick）</summary>
        public static float CalculateResourceOutput(PlanetData planet)
        {
            float baseOutput = 5f; // 基础产出
            float multiplier = 1f;

            if (planet.buildings != null)
            {
                foreach (var building in planet.buildings)
                {
                    if (building.level <= 0 || building.isBuilding) continue;
                    var def = GetDef(building.type);
                    if (def == null) continue;

                    switch (building.type)
                    {
                        case ColonyBuildingType.MiningStation:
                            baseOutput += GetEffect(def, building.level);
                            break;
                        case ColonyBuildingType.Refinery:
                            multiplier += GetEffect(def, building.level);
                            break;
                        case ColonyBuildingType.CommandCenter:
                            multiplier += GetEffect(def, building.level) * 0.1f;
                            break;
                    }
                }
            }

            // 殖民地等级加成
            multiplier *= 1f + planet.colonyLevel * 0.1f;

            return baseOutput * multiplier;
        }

        /// <summary>计算殖民地科技点产出（每tick）</summary>
        public static float CalculateTechOutput(PlanetData planet)
        {
            if (planet.buildings == null) return 0f;
            float output = 0f;
            foreach (var building in planet.buildings)
            {
                if (building.type == ColonyBuildingType.ResearchStation && building.level > 0 && !building.isBuilding)
                {
                    var def = GetDef(ColonyBuildingType.ResearchStation);
                    output += GetEffect(def, building.level);
                }
            }
            return output;
        }

        /// <summary>计算防御减伤比例（0-0.9）</summary>
        public static float CalculateDefenseReduction(PlanetData planet)
        {
            if (planet.buildings == null) return 0f;
            float reduction = 0f;
            foreach (var building in planet.buildings)
            {
                if (building.type == ColonyBuildingType.DefenseTurret && building.level > 0 && !building.isBuilding)
                {
                    var def = GetDef(ColonyBuildingType.DefenseTurret);
                    reduction += GetEffect(def, building.level);
                }
                if (building.type == ColonyBuildingType.ShieldGenerator && building.level > 0 && !building.isBuilding)
                {
                    var def = GetDef(ColonyBuildingType.ShieldGenerator);
                    reduction += GetEffect(def, building.level);
                }
            }
            return Mathf.Min(reduction, 0.9f);
        }

        /// <summary>获取驻留单位上限</summary>
        public static int GetUnitCapacity(PlanetData planet)
        {
            int capacity = 10; // 基础容量
            if (planet.buildings != null)
            {
                foreach (var building in planet.buildings)
                {
                    if (building.type == ColonyBuildingType.Habitat && building.level > 0 && !building.isBuilding)
                    {
                        var def = GetDef(ColonyBuildingType.Habitat);
                        capacity += Mathf.RoundToInt(GetEffect(def, building.level));
                    }
                }
            }
            return capacity;
        }

        /// <summary>获取航行速度加成</summary>
        public static float GetTravelSpeedBonus(PlanetData planet)
        {
            if (planet.buildings == null) return 0f;
            float bonus = 0f;
            foreach (var building in planet.buildings)
            {
                if (building.type == ColonyBuildingType.Spaceport && building.level > 0 && !building.isBuilding)
                {
                    var def = GetDef(ColonyBuildingType.Spaceport);
                    bonus += GetEffect(def, building.level);
                }
            }
            return bonus;
        }

        /// <summary>升级建筑（消耗伊纳尔）</summary>
        /// <summary>升级建筑（消耗殖民地资源）</summary>
        public static bool UpgradeBuilding(PlanetData planet, ColonyBuildingType type)
        {
            var def = GetDef(type);
            if (def == null) return false;
            if (planet.colonyLevel < def.requiredColonyLevel) return false;

            // 查找或创建建筑实例
            ColonyBuilding building = null;
            if (planet.buildings == null) planet.buildings = new List<ColonyBuilding>();
            foreach (var b in planet.buildings)
                if (b.type == type) { building = b; break; }

            if (building == null)
            {
                building = new ColonyBuilding { type = type, level = 0, isBuilding = true, buildProgress = 0f };
                planet.buildings.Add(building);
            }

            if (building.level >= def.maxLevel) return false;
            if (building.isBuilding) return false;

            float cost = GetUpgradeCost(def, building.level);
            if (planet.resource < cost) return false;

            planet.resource -= cost;
            building.isBuilding = true;
            building.buildProgress = 0f;
            return true;
        }

        /// <summary>推进建设进度（每tick调用）</summary>
        public static void TickConstruction(PlanetData planet, float deltaTime)
        {
            if (planet.buildings == null) return;
            foreach (var building in planet.buildings)
            {
                if (!building.isBuilding) continue;
                var def = GetDef(building.type);
                if (def == null) continue;

                // 建设时间：基础10秒，每级+5秒
                float buildTime = 10f + building.level * 5f;
                building.buildProgress += deltaTime / buildTime;

                if (building.buildProgress >= 1f)
                {
                    building.isBuilding = false;
                    building.buildProgress = 0f;
                    building.level++;
                }
            }
        }
    }
}
