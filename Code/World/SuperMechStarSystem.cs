using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际系统（v1.0）：多星球数据+航行+殖民
    /// 主星球是当前WorldBox地图，其他星球是简化数据
    /// 对照原著：破碎星环/柯尔顿星区/西斯特尔星区等星区设定</summary>
    public static class SuperMechStarSystem
    {
        public enum PlanetType { Rocky, Gas, Ice, Lava, Ocean, Desert, Forest }

        public class PlanetData
        {
            public string id;
            public string name;
            public PlanetType type;
            public float x; // 星图坐标
            public float y;
            public float resource; // 资源量
            public float maxResource;
            public string ownerKingdom; // 所属文明ID
            public List<long> units = new List<long>(); // 驻留单位ID
            public int colonyLevel; // 殖民等级0=未殖民
            public bool discovered;
        }

        public class VoyageData
        {
            public long unitId;
            public string fromPlanet;
            public string toPlanet;
            public float progress; // 0-1
            public float speed; // 每秒进度
        }

        private static readonly Dictionary<string, PlanetData> _planets = new Dictionary<string, PlanetData>();
        private static readonly List<VoyageData> _voyages = new List<VoyageData>();
        private static bool _initialized;
        private static float _tickTimer;

        public const string MAIN_PLANET = "main"; // 主星球（当前WorldBox地图）

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            GeneratePlanets();
        }

        /// <summary>生成星系星球（对照原著破碎星环设定）</summary>
        private static void GeneratePlanets()
        {
            // 主星球
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

            // 周边星球（随机生成8-12个）
            string[] planetNames = { "艾莫尔", "科尔基斯", "苏尼尔星", "黑灵星", "泰尔克斯", "星瞳星", "卢翰星", "昆德星", "诺里欧斯", "加里奥特", "银灵星", "赤岩星" };
            PlanetType[] types = { PlanetType.Rocky, PlanetType.Gas, PlanetType.Ice, PlanetType.Lava, PlanetType.Ocean, PlanetType.Desert, PlanetType.Forest };
            System.Random rng = new System.Random(42); // 固定种子，每次生成一致

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

        /// <summary>研究航天科技后发现新星球</summary>
        public static void DiscoverPlanets(int techTier)
        {
            int discoverCount = techTier * 2; // T1发现2个，T2发现4个...
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

        /// <summary>派遣单位航行到目标星球</summary>
        public static bool SendUnit(long unitId, string fromPlanet, string toPlanet)
        {
            if (fromPlanet == toPlanet) return false;
            var from = GetPlanet(fromPlanet);
            var to = GetPlanet(toPlanet);
            if (from == null || to == null || !to.discovered) return false;
            if (!from.units.Contains(unitId)) return false;

            // 计算航行速度（基于科技等级）
            float speed = 0.01f; // 基础100秒
            int spaceTier = GetSpaceTechTier();
            speed += spaceTier * 0.005f; // 每级科技加速

            from.units.Remove(unitId);
            _voyages.Add(new VoyageData
            {
                unitId = unitId,
                fromPlanet = fromPlanet,
                toPlanet = toPlanet,
                progress = 0,
                speed = speed
            });
            return true;
        }

        /// <summary>获取当前文明的航天科技等级</summary>
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

        /// <summary>航行Tick（每秒调用）</summary>
        public static void TickVoyages(float dt)
        {
            _tickTimer += dt;
            if (_tickTimer < 1f) return; // 每秒更新一次
            _tickTimer = 0;

            for (int i = _voyages.Count - 1; i >= 0; i--)
            {
                var v = _voyages[i];
                v.progress += v.speed;
                if (v.progress >= 1f)
                {
                    // 到达目标星球
                    var to = GetPlanet(v.toPlanet);
                    if (to != null)
                    {
                        to.units.Add(v.unitId);
                        // 自动建立殖民地
                        if (to.colonyLevel == 0)
                        {
                            to.colonyLevel = 1;
                            var actor = World.world?.units?.GetUnit(v.unitId);
                            if (actor != null && actor.kingdom != null)
                            {
                                to.ownerKingdom = actor.kingdom.data.name;
                            }
                        }
                    }
                    _voyages.RemoveAt(i);
                }
            }
        }

        /// <summary>殖民地资源产出（每秒调用）</summary>
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

        /// <summary>从殖民地运回资源</summary>
        public static float TransportResource(string planetId, float amount)
        {
            var p = GetPlanet(planetId);
            if (p == null || p.colonyLevel == 0) return 0;
            float taken = Mathf.Min(p.resource, amount);
            p.resource -= taken;
            return taken;
        }

        /// <summary>获取航行中的单位</summary>
        public static List<VoyageData> GetVoyages()
        {
            return new List<VoyageData>(_voyages);
        }

        public static void ClearAll()
        {
            _planets.Clear();
            _voyages.Clear();
            _initialized = false;
        }
    }
}
