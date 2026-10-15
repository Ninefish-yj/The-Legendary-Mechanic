using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际事件类型</summary>
    public enum StarEventType
    {
        ResourceDiscovery,    // 资源发现
        AncientRuin,          // 古代遗迹
        HostileCreature,      // 敌对生物
        FriendlyCivilization, // 友好文明
        SpaceStorm,           // 太空风暴
        PirateAmbush,         // 海盗伏击
        DerelictShip,         // 废弃飞船
        CosmicAnomaly,        // 宇宙异象
        None
    }

    /// <summary>事件结果类型</summary>
    public enum EventResultType
    {
        GainResource,    // 获得资源
        LoseUnit,        // 损失单位
        GainUnit,        // 获得单位
        GainTech,        // 获得科技点
        UnlockPlanet,    // 解锁新星球
        DamageUnit,      // 单位受伤
        Nothing,         // 无事发生
        ColonyBonus      // 殖民地加成
    }

    /// <summary>星际事件定义</summary>
    public class StarEventDef
    {
        public string id;
        public string nameKey;       // 本地化key
        public string descKey;       // 本地化key
        public StarEventType type;
        public float weight;         // 出现权重
        public float minColonyLevel; // 最低殖民地等级触发
        public PlanetType[] validPlanetTypes; // 可触发的星球类型
        public EventResultType resultType;
        public float resourceGain;   // 资源获得量
        public float unitDamage;     // 单位伤害比例
        public int techGain;         // 科技点获得
        public string rewardTrait;   // 奖励特质ID
    }

    /// <summary>事件结果（运行时）</summary>
    public class StarEventResult
    {
        public string eventId;
        public string eventName;
        public string eventDesc;
        public EventResultType resultType;
        public string resultMessage;
        public float resourceGain;
        public int unitsLost;
        public int unitsGained;
        public float techGain;
        public bool success;
    }

    /// <summary>星际事件系统：探索事件+太空遭遇战</summary>
    internal static class StarEvents
    {
        private static readonly List<StarEventDef> _events = new List<StarEventDef>();
        private static bool _initialized;

        /// <summary>初始化事件库</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            // === 资源发现类 ===
            _events.Add(new StarEventDef
            {
                id = "evt_rich_mineral",
                nameKey = "sm_star_evt_rich_mineral_name",
                descKey = "sm_star_evt_rich_mineral_desc",
                type = StarEventType.ResourceDiscovery,
                weight = 20f,
                resultType = EventResultType.GainResource,
                resourceGain = 200f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_energy_crystal",
                nameKey = "sm_star_evt_energy_crystal_name",
                descKey = "sm_star_evt_energy_crystal_desc",
                type = StarEventType.ResourceDiscovery,
                weight = 15f,
                resultType = EventResultType.GainResource,
                resourceGain = 350f
            });

            // === 古代遗迹类 ===
            _events.Add(new StarEventDef
            {
                id = "evt_ancient_ruin",
                nameKey = "sm_star_evt_ancient_ruin_name",
                descKey = "sm_star_evt_ancient_ruin_desc",
                type = StarEventType.AncientRuin,
                weight = 10f,
                resultType = EventResultType.GainTech,
                techGain = 50
            });
            _events.Add(new StarEventDef
            {
                id = "evt_precursor_cache",
                nameKey = "sm_star_evt_precursor_cache_name",
                descKey = "sm_star_evt_precursor_cache_desc",
                type = StarEventType.AncientRuin,
                weight = 5f,
                resultType = EventResultType.GainTech,
                techGain = 150
            });

            // === 敌对生物类 ===
            _events.Add(new StarEventDef
            {
                id = "evt_space_beast",
                nameKey = "sm_star_evt_space_beast_name",
                descKey = "sm_star_evt_space_beast_desc",
                type = StarEventType.HostileCreature,
                weight = 12f,
                resultType = EventResultType.DamageUnit,
                unitDamage = 0.3f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_void_horror",
                nameKey = "sm_star_evt_void_horror_name",
                descKey = "sm_star_evt_void_horror_desc",
                type = StarEventType.HostileCreature,
                weight = 4f,
                resultType = EventResultType.LoseUnit,
                unitDamage = 1f
            });

            // === 友好文明类 ===
            _events.Add(new StarEventDef
            {
                id = "evt_friendly_traders",
                nameKey = "sm_star_evt_friendly_traders_name",
                descKey = "sm_star_evt_friendly_traders_desc",
                type = StarEventType.FriendlyCivilization,
                weight = 10f,
                resultType = EventResultType.GainResource,
                resourceGain = 150f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_ally_mercenary",
                nameKey = "sm_star_evt_ally_mercenary_name",
                descKey = "sm_star_evt_ally_mercenary_desc",
                type = StarEventType.FriendlyCivilization,
                weight = 6f,
                resultType = EventResultType.GainUnit
            });

            // === 太空灾害类 ===
            _events.Add(new StarEventDef
            {
                id = "evt_solar_flare",
                nameKey = "sm_star_evt_solar_flare_name",
                descKey = "sm_star_evt_solar_flare_desc",
                type = StarEventType.SpaceStorm,
                weight = 8f,
                resultType = EventResultType.DamageUnit,
                unitDamage = 0.2f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_meteor_shower",
                nameKey = "sm_star_evt_meteor_shower_name",
                descKey = "sm_star_evt_meteor_shower_desc",
                type = StarEventType.SpaceStorm,
                weight = 7f,
                resultType = EventResultType.DamageUnit,
                unitDamage = 0.25f
            });

            // === 海盗伏击（太空遭遇战）===
            _events.Add(new StarEventDef
            {
                id = "evt_pirate_scout",
                nameKey = "sm_star_evt_pirate_scout_name",
                descKey = "sm_star_evt_pirate_scout_desc",
                type = StarEventType.PirateAmbush,
                weight = 10f,
                resultType = EventResultType.DamageUnit,
                unitDamage = 0.4f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_pirate_fleet",
                nameKey = "sm_star_evt_pirate_fleet_name",
                descKey = "sm_star_evt_pirate_fleet_desc",
                type = StarEventType.PirateAmbush,
                weight = 5f,
                resultType = EventResultType.LoseUnit,
                unitDamage = 0.8f
            });

            // === 废弃飞船 ===
            _events.Add(new StarEventDef
            {
                id = "evt_derelict_scrap",
                nameKey = "sm_star_evt_derelict_scrap_name",
                descKey = "sm_star_evt_derelict_scrap_desc",
                type = StarEventType.DerelictShip,
                weight = 8f,
                resultType = EventResultType.GainResource,
                resourceGain = 100f
            });
            _events.Add(new StarEventDef
            {
                id = "evt_derelict_ai",
                nameKey = "sm_star_evt_derelict_ai_name",
                descKey = "sm_star_evt_derelict_ai_desc",
                type = StarEventType.DerelictShip,
                weight = 3f,
                resultType = EventResultType.GainTech,
                techGain = 100
            });

            // === 宇宙异象 ===
            _events.Add(new StarEventDef
            {
                id = "evt_wormhole",
                nameKey = "sm_star_evt_wormhole_name",
                descKey = "sm_star_evt_wormhole_desc",
                type = StarEventType.CosmicAnomaly,
                weight = 3f,
                resultType = EventResultType.UnlockPlanet
            });
            _events.Add(new StarEventDef
            {
                id = "evt_colony_bonus",
                nameKey = "sm_star_evt_colony_bonus_name",
                descKey = "sm_star_evt_colony_bonus_desc",
                type = StarEventType.CosmicAnomaly,
                weight = 4f,
                resultType = EventResultType.ColonyBonus,
                resourceGain = 500f
            });
        }

        /// <summary>随机选择一个事件（加权随机）</summary>
        public static StarEventDef RollEvent(System.Random rng, PlanetType planetType, int colonyLevel)
        {
            float totalWeight = 0f;
            var valid = new List<StarEventDef>();
            foreach (var evt in _events)
            {
                if (evt.minColonyLevel > colonyLevel) continue;
                if (evt.validPlanetTypes != null && evt.validPlanetTypes.Length > 0)
                {
                    bool validType = false;
                    foreach (var pt in evt.validPlanetTypes)
                        if (pt == planetType) { validType = true; break; }
                    if (!validType) continue;
                }
                valid.Add(evt);
                totalWeight += evt.weight;
            }
            if (valid.Count == 0) return null;

            float roll = (float)rng.NextDouble() * totalWeight;
            float acc = 0f;
            foreach (var evt in valid)
            {
                acc += evt.weight;
                if (roll <= acc) return evt;
            }
            return valid[valid.Count - 1];
        }

        /// <summary>执行事件，返回结果</summary>
        public static StarEventResult ResolveEvent(StarEventDef evt, PlanetData planet, System.Random rng)
        {
            var result = new StarEventResult
            {
                eventId = evt.id,
                eventName = LocalizedTextManager.getText(evt.nameKey),
                eventDesc = LocalizedTextManager.getText(evt.descKey),
                resultType = evt.resultType,
                success = true
            };

            switch (evt.resultType)
            {
                case EventResultType.GainResource:
                    float gain = evt.resourceGain * (1f + planet.colonyLevel * 0.2f);
                    planet.resource += gain;
                    result.resourceGain = gain;
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_gain_resource")
                        .Replace("{amount}", Mathf.RoundToInt(gain).ToString());
                    break;

                case EventResultType.LoseUnit:
                    int lost = Mathf.Min(planet.units.Count, 1);
                    if (planet.units.Count > 0)
                        planet.units.RemoveAt(rng.Next(planet.units.Count));
                    result.unitsLost = lost;
                    result.success = false;
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_lose_unit")
                        .Replace("{count}", lost.ToString());
                    break;

                case EventResultType.DamageUnit:
                    // 单位快照无HP系统，改为按概率损失单位
                    int dmgLost = 0;
                    if (planet.units.Count > 0 && rng.NextDouble() < evt.unitDamage)
                    {
                        planet.units.RemoveAt(rng.Next(planet.units.Count));
                        dmgLost = 1;
                    }
                    result.unitsLost = dmgLost;
                    result.success = dmgLost == 0;
                    result.resultMessage = dmgLost > 0
                        ? LocalizedTextManager.getText("sm_star_evt_result_lose_unit").Replace("{count}", dmgLost.ToString())
                        : LocalizedTextManager.getText("sm_star_evt_result_nothing");
                    break;

                case EventResultType.GainTech:
                    // 实际加到王国科技点
                    if (!string.IsNullOrEmpty(planet.ownerKingdom))
                    {
                        var kingdom = SuperMechStarSystem.FindKingdomByName(planet.ownerKingdom);
                        if (kingdom != null)
                        {
                            SuperMechCivilization.AddTechPoints(kingdom, evt.techGain);
                        }
                    }
                    result.techGain = evt.techGain;
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_gain_tech")
                        .Replace("{amount}", evt.techGain.ToString());
                    break;

                case EventResultType.UnlockPlanet:
                    // 实际解锁一个未发现的星球
                    var allPlanets = SuperMechStarSystem.GetAllPlanets();
                    var undiscovered = allPlanets.FindAll(p => !p.discovered && p.id != SuperMechStarSystem.MAIN_PLANET && p.type != PlanetType.Star);
                    if (undiscovered.Count > 0)
                    {
                        var target = undiscovered[rng.Next(undiscovered.Count)];
                        target.discovered = true;
                        result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_unlock") + "：" + target.name;
                    }
                    else
                    {
                        result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_nothing");
                    }
                    break;

                case EventResultType.GainUnit:
                    // 实际添加一个单位快照（随机种族）
                    var newSnapshot = new ActorSnapshot
                    {
                        id = System.DateTime.Now.Ticks,
                        assetId = "actor_human",
                        name = LocalizedTextManager.getText("sm_star_evt_mercenary_name"),
                        kingdomName = planet.ownerKingdom,
                        age = 20
                    };
                    planet.units.Add(newSnapshot);
                    result.unitsGained = 1;
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_gain_unit");
                    break;

                case EventResultType.ColonyBonus:
                    planet.colonyLevel = Mathf.Min(planet.colonyLevel + 1, 10);
                    planet.resource += evt.resourceGain;
                    result.resourceGain = evt.resourceGain;
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_colony_bonus");
                    break;

                default:
                    result.resultMessage = LocalizedTextManager.getText("sm_star_evt_result_nothing");
                    break;
            }

            return result;
        }

        /// <summary>获取所有事件定义（用于UI展示）</summary>
        public static List<StarEventDef> GetAllEvents() => _events;
    }
}
