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

        // ========== 星系结构配置（螺旋星系） ==========
        public const float GALAXY_CENTER_X = 0f;
        public const float GALAXY_CENTER_Y = 0f;
        public const int GALAXY_ARM_COUNT = 4; // 4条螺旋臂（类似银河系）
        public const float GALAXY_ARM_WIDTH = 200f; // 悬臂宽度
        public const float GALAXY_ARM_TIGHTNESS = 0.3f; // 螺旋紧密度（越大越紧）
        public const float GALAXY_CORE_RADIUS = 400f; // 核球半径（中心恒星密集区）
        public const float GALAXY_DISK_RADIUS = 5000f; // 星系盘半径


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

        public static void DiscoverPlanet(string planetId, string discoverer, Actor explorer = null)
        {
            if (_planets.TryGetValue(planetId, out var p))
            {
                bool wasNew = !p.discovered;
                p.discovered = true;
                if (string.IsNullOrEmpty(p.ownerKingdom))
                    p.ownerKingdom = discoverer;
                // v0.193 任务系统：星球探索进度追踪
                if (wasNew && explorer != null)
                    SuperMechQuestSystem.UpdateQuestProgress(explorer, SuperMechQuestSystem.QuestObjectiveType.ExplorePlanet, planetId, 1);
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
            public int totalTicks;
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
                        progress = v.progress, speed = v.speed, totalTicks = v.totalTicks
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
                        progress = vs.progress, speed = vs.speed, totalTicks = vs.totalTicks
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
