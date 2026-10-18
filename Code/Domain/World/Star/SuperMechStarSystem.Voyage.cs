using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际系统 - 航行与殖民地部分（派遣/航行Tick/殖民地Tick/资源运输）</summary>
    public static partial class SuperMechStarSystem
    {
        /// <summary>派遣单位航行（支持主星球↔其他星球双向）</summary>
        public static bool SendUnit(long unitId, string fromPlanet, string toPlanet)
        {
            if (fromPlanet == toPlanet) return false;
            var from = GetPlanet(fromPlanet);
            var to = GetPlanet(toPlanet);
            if (from == null || to == null) return false;

            // 航行方式判定：肉身横渡（超A以上）or 飞船（需要航天科技）
            bool canBodyFlight = false;
            int unitRank = -1;
            long kingdomId = 0;

            if (fromPlanet == MAIN_PLANET)
            {
                Actor actor = FindActorById(unitId);
                if (actor != null)
                {
                    unitRank = SuperMechAdvancement.GetExactRankIndex(actor);
                    if (actor.kingdom != null) kingdomId = actor.kingdom.data.id;
                }
            }
            else if (from.units.Count > 0)
            {
                var unitSnapshot = from.units.Find(u => u.id == unitId);
                if (unitSnapshot != null)
                {
                    unitRank = unitSnapshot.rank;
                    if (!string.IsNullOrEmpty(unitSnapshot.kingdomName))
                    {
                        var k = FindKingdomByName(unitSnapshot.kingdomName);
                        if (k != null) kingdomId = k.data.id;
                    }
                }
            }

            // 超A级（rank>=10）可肉身横渡宇宙，不需要飞船和科技
            canBodyFlight = unitRank >= 6;

            // 非肉身横渡需要航天科技space_t3（亚光速飞船）
            if (!canBodyFlight && fromPlanet != toPlanet && toPlanet != MAIN_PLANET)
            {
                if (kingdomId > 0 && !SuperMechTech.IsFeatureUnlocked(kingdomId, SuperMechTech.TechFeature.StarTravel))
                {
                    Debug.Log($"[超神机械师] 航行被阻止：单位阶位不足（需超A肉身横渡）或需研究{SuperMechTech.GetFeatureRequirementName(SuperMechTech.TechFeature.StarTravel)}");
                    return false;
                }
            }

            ActorSnapshot snapshot = null;

            // 从主星球出发：从WorldBox地图中找到Actor，保存快照，移除Actor
            if (fromPlanet == MAIN_PLANET)
            {
                Actor actor = FindActorById(unitId);
                if (actor == null) return false;
                snapshot = new ActorSnapshot
                {
                    id = unitId,
                    assetId = actor.asset.id,
                    name = actor.data.name,
                    kingdomName = actor.kingdom?.data?.name,
                    age = actor.age,
                    rank = SuperMechAdvancement.GetExactRankIndex(actor)
                };
                // 从WorldBox地图移除
                actor.dieSimpleNone();
            }
            else
            {
                // 从其他星球出发：从快照列表中找到并移除
                snapshot = from.units.Find(u => u.id == unitId);
                if (snapshot == null) return false;
                from.units.Remove(snapshot);
            }

            // 计算航行速度
            float speed = 0.01f;
            int spaceTier = GetSpaceTechTier();
            speed += spaceTier * 0.005f;

            _voyages.Add(new VoyageData
            {
                snapshot = snapshot,
                fromPlanet = fromPlanet,
                toPlanet = toPlanet,
                progress = 0,
                speed = speed,
                totalTicks = (int)(1f / speed)
            });
            return true;
        }

        private static int GetSpaceTechTier()
        {
            int tier = 0;
            var k = World.world?.kingdoms?.list;
            if (k == null || k.Count == 0) return 0;
            long kid = k[0].getID();
            for (int t = 1; t <= 8; t++)
            {
                if (SuperMechTech.IsResearched(kid, $"space_t{t}")) tier = t;
            }
            return tier;
        }

        /// <summary>航行Tick（支持到达主星球时生成Actor）</summary>
        public static void TickVoyages(float dt)
        {
            _tickTimer += dt;
            if (_tickTimer < 1f) return;
            _tickTimer = 0;

            for (int i = _voyages.Count - 1; i >= 0; i--)
            {
                var v = _voyages[i];
                v.progress += v.speed;
                if (v.progress >= 1f)
                {
                    var to = GetPlanet(v.toPlanet);
                    if (to != null)
                    {
                        if (v.toPlanet == MAIN_PLANET)
                        {
                            // 到达主星球：在WorldBox地图中生成Actor
                            SpawnActorOnMainPlanet(v.snapshot);
                        }
                        else
                        {
                            // 到达其他星球：保存快照
                            to.units.Add(v.snapshot);
                            // 如果是未发现的星球，探险队到达后自动发现
                            if (!to.discovered)
                            {
                                to.discovered = true;
                                Debug.Log($"[超神机械师] 探险队 {v.snapshot.name} 发现了星球 {to.name}");
                            }
                            if (to.colonyLevel == 0)
                            {
                                to.colonyLevel = 1;
                                to.ownerKingdom = v.snapshot.kingdomName;
                            }
                        }
                    }
                    _voyages.RemoveAt(i);
                }
            }
        }

        /// <summary>在主星球生成Actor（根据快照）</summary>
        private static void SpawnActorOnMainPlanet(ActorSnapshot snapshot)
        {
            if (World.world == null || snapshot == null) return;
            // 找一个随机陆地地块生成
            var tile = GetRandomLandTile();
            if (tile == null) return;
            var actor = World.world.units.createNewUnit(snapshot.assetId, tile, pMiracleSpawn: false, 0f, null, null, pSpawnWithItems: true, pAdultAge: true);
            if (actor != null)
            {
                actor.data.name = snapshot.name;
                Debug.Log($"[超神机械师] 单位 {snapshot.name} 从星际航行到达主星球");
            }
        }

        /// <summary>公开接口：将单位快照生成到主星球（用于召回单位）</summary>
        public static void SpawnUnitAtMain(ActorSnapshot snapshot)
        {
            SpawnActorOnMainPlanet(snapshot);
        }

        private static float _colonyTechTimer;
        private static float _autoTransportTimer;
        private static float _eventTimer;
        private static System.Random _eventRng = new System.Random();

        public static void TickColonies(float dt)
        {
            // 确保事件和殖民地系统初始化
            StarEvents.Init();
            ColonyManager.Init();

            foreach (var p in _planets.Values)
            {
                if (p.colonyLevel > 0 && p.id != MAIN_PLANET)
                {
                    // 推进建设进度
                    ColonyManager.TickConstruction(p, dt);

                    // 殖民地资源产出（使用建筑系统计算）
                    float output = ColonyManager.CalculateResourceOutput(p) * dt;
                    p.resource = Mathf.Min(p.maxResource, p.resource + output);

                    // 事件触发（冷却后随机）
                    _eventTimer += dt;
                    if (_eventTimer >= p.eventCooldown && p.units.Count > 0)
                    {
                        _eventTimer = 0f;
                        if (_eventRng.NextDouble() < 0.3f) // 30%概率触发事件
                        {
                            var evt = StarEvents.RollEvent(_eventRng, p.type, p.colonyLevel);
                            if (evt != null)
                            {
                                // 应用防御减伤：建筑防御+防御单位
                                float defense = ColonyManager.CalculateDefenseReduction(p) + ColonyManager.CalculateDefenseReduction(p);
                                defense = System.Math.Min(0.9f, defense); // 最多90%减伤
                                var result = StarEvents.ResolveEvent(evt, p, _eventRng);
                                if (result.resultType == EventResultType.DamageUnit || result.resultType == EventResultType.LoseUnit)
                                {
                                    if (_eventRng.NextDouble() < defense)
                                    {
                                        result.resultMessage = LocalizedTextManager.getText("sm_star_evt_defended") + result.resultMessage;
                                        result.unitsLost = 0;
                                    }
                                }
                                p.pendingEvent = result;
                                Debug.Log($"[超神机械师] 星际事件: {result.eventName} - {result.resultMessage}");
                            }
                        }
                    }
                }
            }

            // 资源自动运输（每15秒：殖民地资源超过80%时，运回50%到主星球）
            _autoTransportTimer += dt;
            if (_autoTransportTimer >= 15f)
            {
                _autoTransportTimer = 0;
                var main = GetPlanet(MAIN_PLANET);
                foreach (var p in _planets.Values)
                {
                    if (p.colonyLevel > 0 && p.id != MAIN_PLANET && p.maxResource > 0
                        && p.autoTransport && p.resource >= p.maxResource * 0.8f && main != null)
                    {
                        float transport = p.resource * 0.5f;
                        p.resource -= transport;
                        main.resource = Mathf.Min(main.maxResource, main.resource + transport);
                        Debug.Log($"[超神机械师] 自动运输: {p.name} -> 主世界 {transport:F0}资源");
                    }
                }
            }

            // 殖民地资源转化为文明科技点（每10秒转化一次）
            _colonyTechTimer += dt;
            if (_colonyTechTimer >= 10f)
            {
                _colonyTechTimer = 0;
                foreach (var p in _planets.Values)
                {
                    if (p.colonyLevel > 0 && p.id != MAIN_PLANET && !string.IsNullOrEmpty(p.ownerKingdom))
                    {
                        var kingdom = FindKingdomByName(p.ownerKingdom);
                        if (kingdom != null)
                        {
                            // 科技点产出（研究站建筑）
                            float techOutput = ColonyManager.CalculateTechOutput(p);
                            if (techOutput > 0.01f)
                            {
                                SuperMechCivilization.AddTechPoints(kingdom, techOutput);
                            }

                            float techGain = p.resource * 0.05f; // 每次转化5%资源
                            if (techGain > 0.1f)
                            {
                                p.resource -= techGain;
                                SuperMechCivilization.AddTechPoints(kingdom, techGain);
                            }

                            // v0.175.1 星际→经济联动：殖民地产出伊纳尔给王国超能者
                            int inalGain = p.colonyLevel * 5;
                            if (inalGain > 0 && World.world?.units != null)
                            {
                                foreach (var a in World.world.units)
                                {
                                    if (a != null && a.isAlive() && a.kingdom == kingdom
                                        && SuperMechAdvancement.GetExactRankIndex(a) >= 4)
                                    {
                                        int cur = SuperMechInal.GetInal(a);
                                        SuperMechInal.SetInal(a, cur + inalGain);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>通过名称查找Kingdom</summary>
        public static Kingdom FindKingdomByName(string name)
        {
            if (World.world?.kingdoms?.list == null) return null;
            foreach (var k in World.world.kingdoms.list)
            {
                if (k != null && k.data != null && k.data.name == name) return k;
            }
            return null;
        }

        public static float TransportResource(string planetId, float amount)
        {
            var p = GetPlanet(planetId);
            if (p == null || p.colonyLevel == 0) return 0;
            float taken = Mathf.Min(p.resource, amount);
            p.resource -= taken;
            return taken;
        }

        public static List<VoyageData> GetVoyages()
        {
            return new List<VoyageData>(_voyages);
        }

        /// <summary>获取主星球可派遣的单位（WorldBox地图中的单位）</summary>
        public static List<Actor> GetMainPlanetUnits()
        {
            var list = new List<Actor>();
            if (World.world?.units == null) return list;
            foreach (var a in World.world.units)
            {
                if (a != null && a.asset != null && a.asset.id != null)
                    list.Add(a);
            }
            return list;
        }

        /// <summary>通过ID查找单位</summary>
        private static Actor FindActorById(long id)
        {
            if (World.world?.units == null) return null;
            foreach (var u in World.world.units)
            {
                if (u != null && u.id == id) return u;
            }
            return null;
        }

        /// <summary>获取随机陆地地块</summary>
        private static WorldTile GetRandomLandTile()
        {
            if (World.world == null) return null;
            for (int i = 0; i < 50; i++)
            {
                int x = UnityEngine.Random.Range(0, MapBox.width);
                int y = UnityEngine.Random.Range(0, MapBox.height);
                var tile = World.world.GetTile(x, y);
                if (tile != null && tile.Type.ground && !tile.Type.liquid) return tile;
            }
            return null;
        }
    }
}
