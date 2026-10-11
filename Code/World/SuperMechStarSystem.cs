using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际系统（v1.1）：多星球数据+航行+殖民，支持主星球↔其他星球双向航行
    /// 主星球是当前WorldBox地图，其他星球是简化数据（Actor快照）
    /// 对照原著：破碎星环/柯尔顿星区/西斯特尔星区等星区设定</summary>
    public static class SuperMechStarSystem
    {
        public enum PlanetType { Rocky, Gas, Ice, Lava, Ocean, Desert, Forest }

        /// <summary>Actor快照：单位离开主星球时保存关键数据，到达时恢复</summary>
        public class ActorSnapshot
        {
            public long id;
            public string assetId; // 种族asset ID
            public string name;
            public string kingdomName;
            public int age;
        }

        public class PlanetData
        {
            public string id;
            public string name;
            public PlanetType type;
            public float x;
            public float y;
            public float resource;
            public float maxResource;
            public string ownerKingdom;
            public List<ActorSnapshot> units = new List<ActorSnapshot>(); // 驻留单位快照
            public int colonyLevel;
            public bool discovered;
        }

        public class VoyageData
        {
            public ActorSnapshot snapshot; // 航行中的单位快照
            public string fromPlanet;
            public string toPlanet;
            public float progress;
            public float speed;
        }

        private static readonly Dictionary<string, PlanetData> _planets = new Dictionary<string, PlanetData>();
        private static readonly List<VoyageData> _voyages = new List<VoyageData>();
        private static bool _initialized;
        private static float _tickTimer;

        public const string MAIN_PLANET = "main";

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            GeneratePlanets();
        }

        private static void GeneratePlanets()
        {
            _planets[MAIN_PLANET] = new PlanetData
            {
                id = MAIN_PLANET,
                name = "主世界",
                type = PlanetType.Forest,
                x = 0, y = 0,
                resource = 1000, maxResource = 1000,
                ownerKingdom = null,
                colonyLevel = 5,
                discovered = true
            };

            string[] planetNames = { "艾莫尔", "科尔基斯", "苏尼尔星", "黑灵星", "泰尔克斯", "星瞳星", "卢翰星", "昆德星", "诺里欧斯", "加里奥特" };
            PlanetType[] types = { PlanetType.Rocky, PlanetType.Gas, PlanetType.Ice, PlanetType.Lava, PlanetType.Ocean, PlanetType.Desert, PlanetType.Forest };
            System.Random rng = new System.Random(42);

            for (int i = 0; i < 10; i++)
            {
                float angle = (i / 10f) * Mathf.PI * 2 + rng.NextFloat(-0.3f, 0.3f);
                float dist = 150 + rng.NextFloat(50, 250);
                string pid = $"planet_{i}";
                _planets[pid] = new PlanetData
                {
                    id = pid,
                    name = planetNames[i % planetNames.Length],
                    type = types[rng.Next(types.Length)],
                    x = Mathf.Cos(angle) * dist,
                    y = Mathf.Sin(angle) * dist,
                    resource = rng.Next(200, 800),
                    maxResource = 1000,
                    ownerKingdom = null,
                    colonyLevel = 0,
                    discovered = false
                };
            }
        }

        public static PlanetData GetPlanet(string id)
        {
            return _planets.TryGetValue(id, out var p) ? p : null;
        }

        public static List<PlanetData> GetAllPlanets()
        {
            return new List<PlanetData>(_planets.Values);
        }

        public static List<PlanetData> GetDiscoveredPlanets()
        {
            var list = new List<PlanetData>();
            foreach (var p in _planets.Values)
                if (p.discovered) list.Add(p);
            return list;
        }

        public static void DiscoverPlanets(int techTier)
        {
            int discoverCount = techTier * 2;
            int count = 0;
            foreach (var p in _planets.Values)
            {
                if (!p.discovered && p.id != MAIN_PLANET)
                {
                    p.discovered = true;
                    count++;
                    if (count >= discoverCount) break;
                }
            }
        }

        /// <summary>派遣单位航行（支持主星球↔其他星球双向）</summary>
        public static bool SendUnit(long unitId, string fromPlanet, string toPlanet)
        {
            if (fromPlanet == toPlanet) return false;
            var from = GetPlanet(fromPlanet);
            var to = GetPlanet(toPlanet);
            if (from == null || to == null || !to.discovered) return false;

            ActorSnapshot snapshot = null;

            // 从主星球出发：从WorldBox地图中找到Actor，保存快照，移除Actor
            if (fromPlanet == MAIN_PLANET)
            {
                var actor = World.world?.units?.GetUnit(unitId);
                if (actor == null) return false;
                snapshot = new ActorSnapshot
                {
                    id = unitId,
                    assetId = actor.asset.id,
                    name = actor.data.name,
                    kingdomName = actor.kingdom?.data?.name,
                    age = actor.data.age
                };
                // 从WorldBox地图移除
                World.world.units.killUnit(actor, true);
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
                speed = speed
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
            // 找一个随机地块生成
            var tile = World.world.map.tiles.GetRandom();
            if (tile == null) return;
            var actor = World.world.units.createNewUnit(snapshot.assetId, tile, pMiracleSpawn: false, 0f, null, null, pSpawnWithItems: true, pAdultAge: true);
            if (actor != null)
            {
                actor.data.name = snapshot.name;
                actor.data.age = snapshot.age;
                Debug.Log($"[超神机械师] 单位 {snapshot.name} 从星际航行到达主星球");
            }
        }

        public static void TickColonies(float dt)
        {
            foreach (var p in _planets.Values)
            {
                if (p.colonyLevel > 0 && p.id != MAIN_PLANET)
                {
                    p.resource = Mathf.Min(p.maxResource, p.resource + p.colonyLevel * 0.1f * dt);
                }
            }
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

        public static void ClearAll()
        {
            _planets.Clear();
            _voyages.Clear();
            _initialized = false;
        }
    }
}
