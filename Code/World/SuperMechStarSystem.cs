using System;
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
        private static readonly HashSet<string> _generatedChunks = new HashSet<string>();
        private static readonly List<VoyageData> _voyages = new List<VoyageData>();
        private static bool _initialized;
        private static float _tickTimer;
        private static int _worldSeed = 20240501; // 世界种子，同一存档星球分布一致

        public const string MAIN_PLANET = "main";
        public const float CHUNK_SIZE = 600f; // 每个星区分块大小
        public const int PLANETS_PER_CHUNK_MIN = 6;
        public const int PLANETS_PER_CHUNK_MAX = 12;

        // 星球名称生成器（前缀+后缀，模拟原著星海命名风格）
        private static readonly string[] NamePrefixes = {
            "艾", "科", "苏", "黑", "泰", "星", "卢", "昆", "诺", "加",
            "塞", "维", "奥", "瑞", "卡", "米", "法", "尼", "克", "泽",
            "安", "比", "达", "厄", "菲", "戈", "赫", "伊", "杰", "凯"
        };
        private static readonly string[] NameSuffixes = {
            "莫尔", "尔基斯", "尼尔星", "灵星", "尔克斯", "瞳星", "翰星", "德星", "里欧斯", "里奥特",
            "文托", "拉托", "德萨", "恩特", "罗萨", "塔尔", "隆德", "瓦纳", "斯托", "弗斯",
            "塔因", "洛斯", "米亚", "诺尔", "菲亚", "兰德", "玛尔", "西斯", "塔克", "维尔"
        };

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            GenerateMainPlanet();
            // 预生成主星球周围4个块（玩家初始视野内可见）
            for (int cx = -1; cx <= 1; cx++)
                for (int cy = -1; cy <= 1; cy++)
                    GenerateChunk(cx, cy);
        }

        /// <summary>生成主星球（固定在原点）</summary>
        private static void GenerateMainPlanet()
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
        }

        /// <summary>根据块坐标生成种子（保证同一块星球分布一致）</summary>
        private static int ChunkSeed(int cx, int cy)
        {
            return _worldSeed + cx * 73856093 + cy * 19349663;
        }

        /// <summary>生成一个星区分块内的星球（按需生成，已生成则跳过）</summary>
        private static void GenerateChunk(int cx, int cy)
        {
            string key = $"{cx}_{cy}";
            if (_generatedChunks.Contains(key)) return;
            _generatedChunks.Add(key);

            // 主星球所在块不重复生成主星球
            bool isCenterChunk = (cx == 0 && cy == 0);

            System.Random rng = new System.Random(ChunkSeed(cx, cy));
            int count = rng.Next(PLANETS_PER_CHUNK_MIN, PLANETS_PER_CHUNK_MAX + 1);

            for (int i = 0; i < count; i++)
            {
                // 块内随机位置（留边距避免星球在边界重叠）
                float localX = 40f + (float)rng.NextDouble() * (CHUNK_SIZE - 80f);
                float localY = 40f + (float)rng.NextDouble() * (CHUNK_SIZE - 80f);
                float worldX = cx * CHUNK_SIZE + localX;
                float worldY = cy * CHUNK_SIZE + localY;

                // 中心块跳过离主星球太近的位置
                if (isCenterChunk)
                {
                    float distToMain = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                    if (distToMain < 80f) continue;
                }

                string pid = $"chunk_{cx}_{cy}_{i}";
                if (_planets.ContainsKey(pid)) continue;

                // 随机星球类型（森林类宜居星概率低，岩石/荒漠概率高）
                PlanetType[] types = { PlanetType.Rocky, PlanetType.Rocky, PlanetType.Desert, PlanetType.Desert,
                    PlanetType.Ice, PlanetType.Gas, PlanetType.Lava, PlanetType.Ocean, PlanetType.Forest };
                PlanetType type = types[rng.Next(types.Length)];

                // 随机名称（前缀+后缀）
                string prefix = NamePrefixes[rng.Next(NamePrefixes.Length)];
                string suffix = NameSuffixes[rng.Next(NameSuffixes.Length)];
                string name = prefix + suffix;

                _planets[pid] = new PlanetData
                {
                    id = pid,
                    name = name,
                    type = type,
                    x = worldX,
                    y = worldY,
                    resource = rng.Next(100, 1000),
                    maxResource = 1000,
                    ownerKingdom = null,
                    colonyLevel = 0,
                    discovered = false
                };
            }
        }

        /// <summary>获取指定范围内的所有星球（自动生成途经的星区）</summary>
        public static List<PlanetData> GetPlanetsInRange(float centerX, float centerY, float range)
        {
            // 计算需要的块范围
            int minCx = Mathf.FloorToInt((centerX - range) / CHUNK_SIZE);
            int maxCx = Mathf.FloorToInt((centerX + range) / CHUNK_SIZE);
            int minCy = Mathf.FloorToInt((centerY - range) / CHUNK_SIZE);
            int maxCy = Mathf.FloorToInt((centerY + range) / CHUNK_SIZE);

            // 按需生成块
            for (int cx = minCx; cx <= maxCx; cx++)
                for (int cy = minCy; cy <= maxCy; cy++)
                    GenerateChunk(cx, cy);

            // 收集范围内的星球
            var list = new List<PlanetData>();
            float rangeSq = range * range;
            foreach (var p in _planets.Values)
            {
                float dx = p.x - centerX, dy = p.y - centerY;
                if (dx * dx + dy * dy <= rangeSq)
                    list.Add(p);
            }
            return list;
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

        public static List<PlanetData> GetUndiscoveredPlanets()
        {
            var list = new List<PlanetData>();
            foreach (var p in _planets.Values)
                if (!p.discovered && p.id != MAIN_PLANET) list.Add(p);
            return list;
        }

        public static void DiscoverPlanet(string planetId, string discoverer)
        {
            if (_planets.TryGetValue(planetId, out var p))
            {
                p.discovered = true;
                if (string.IsNullOrEmpty(p.ownerKingdom))
                    p.ownerKingdom = discoverer;
            }
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
            if (from == null || to == null) return false;
            // 注意：允许派遣到未发现的星球（探险任务）

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
                    age = actor.age
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

        private static float _colonyTechTimer;

        public static void TickColonies(float dt)
        {
            foreach (var p in _planets.Values)
            {
                if (p.colonyLevel > 0 && p.id != MAIN_PLANET)
                {
                    p.resource = Mathf.Min(p.maxResource, p.resource + p.colonyLevel * 0.1f * dt);
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
                            float techGain = p.resource * 0.05f; // 每次转化5%资源
                            if (techGain > 0.1f)
                            {
                                p.resource -= techGain;
                                SuperMechCivilization.AddTechPoints(kingdom, techGain);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>通过名称查找Kingdom</summary>
        private static Kingdom FindKingdomByName(string name)
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

        // ═════════════════════ 存档序列化（v0.102.0）═════════════════════

        [Serializable]
        public class StarSystemSaveData
        {
            public List<PlanetSaveData> planets = new List<PlanetSaveData>();
            public List<VoyageSaveData> voyages = new List<VoyageSaveData>();
        }

        [Serializable]
        public class PlanetSaveData
        {
            public string id;
            public string name;
            public int type;
            public float x, y;
            public float resource, maxResource;
            public string ownerKingdom;
            public int colonyLevel;
            public bool discovered;
            public List<ActorSnapshot> units = new List<ActorSnapshot>();
        }

        [Serializable]
        public class VoyageSaveData
        {
            public ActorSnapshot snapshot;
            public string fromPlanet;
            public string toPlanet;
            public float progress;
            public float speed;
        }

        public static string SerializeForSave()
        {
            try
            {
                var save = new StarSystemSaveData();
                // 无限星球：只保存有状态变化的星球（已发现/已殖民/有单位/资源变化）
                // 未发现且未殖民的星球靠种子重新生成，不需要存档
                foreach (var p in _planets.Values)
                {
                    bool hasState = p.discovered || p.colonyLevel > 0 || p.units.Count > 0
                        || !string.IsNullOrEmpty(p.ownerKingdom) || p.resource != p.maxResource;
                    if (!hasState && p.id != MAIN_PLANET) continue; // 跳过无状态变化的普通星球

                    save.planets.Add(new PlanetSaveData
                    {
                        id = p.id, name = p.name, type = (int)p.type,
                        x = p.x, y = p.y, resource = p.resource, maxResource = p.maxResource,
                        ownerKingdom = p.ownerKingdom, colonyLevel = p.colonyLevel,
                        discovered = p.discovered, units = new List<ActorSnapshot>(p.units)
                    });
                }
                foreach (var v in _voyages)
                {
                    save.voyages.Add(new VoyageSaveData
                    {
                        snapshot = v.snapshot, fromPlanet = v.fromPlanet, toPlanet = v.toPlanet,
                        progress = v.progress, speed = v.speed
                    });
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(save);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 星际系统存档序列化失败: " + e.Message);
                return "";
            }
        }

        public static void LoadFromSave(string json)
        {
            try
            {
                var save = Newtonsoft.Json.JsonConvert.DeserializeObject<StarSystemSaveData>(json);
                if (save == null) return;
                // 先初始化基础星球（靠种子生成），再用存档覆盖有状态变化的星球
                Init();
                _voyages.Clear();
                foreach (var ps in save.planets)
                {
                    var p = new PlanetData
                    {
                        id = ps.id, name = ps.name, type = (PlanetType)ps.type,
                        x = ps.x, y = ps.y, resource = ps.resource, maxResource = ps.maxResource,
                        ownerKingdom = ps.ownerKingdom, colonyLevel = ps.colonyLevel,
                        discovered = ps.discovered, units = ps.units ?? new List<ActorSnapshot>()
                    };
                    _planets[ps.id] = p; // 覆盖或新增
                }
                foreach (var vs in save.voyages)
                {
                    _voyages.Add(new VoyageData
                    {
                        snapshot = vs.snapshot, fromPlanet = vs.fromPlanet, toPlanet = vs.toPlanet,
                        progress = vs.progress, speed = vs.speed
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 星际系统存档加载失败: " + e.Message);
            }
        }

        public static void ClearAll()
        {
            _planets.Clear();
            _voyages.Clear();
            _initialized = false;
        }
    }
}
