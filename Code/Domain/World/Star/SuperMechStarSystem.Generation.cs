using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星际系统 - 星系生成部分（星系数学/星团/星球生成/轨道计算）</summary>
    public static partial class SuperMechStarSystem
    {
        /// <summary>计算位置到最近悬臂的距离（用于恒星分布密度）</summary>
        public static float DistanceToNearestArm(float x, float y)
        {
            float dx = x - GALAXY_CENTER_X;
            float dy = y - GALAXY_CENTER_Y;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < 1f) return 0f;
            float theta = Mathf.Atan2(dy, dx);

            // 对数螺旋线：r = a * e^(b*theta)，简化为角度偏移
            float minDist = float.MaxValue;
            for (int i = 0; i < GALAXY_ARM_COUNT; i++)
            {
                float armAngle = (i * Mathf.PI * 2f / GALAXY_ARM_COUNT) + r * GALAXY_ARM_TIGHTNESS * 0.01f;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(theta * Mathf.Rad2Deg, armAngle * Mathf.Rad2Deg));
                float dist = angleDiff * Mathf.Deg2Rad * r; // 弧长距离
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        /// <summary>计算位置的恒星密度系数（悬臂上密度高，核球密度最高，晕族密度低，星团密度极高）</summary>
        public static float GetStarDensity(float x, float y)
        {
            float dx = x - GALAXY_CENTER_X;
            float dy = y - GALAXY_CENTER_Y;
            float r = Mathf.Sqrt(dx * dx + dy * dy);

            // 核球区域：密度最高
            if (r < GALAXY_CORE_RADIUS)
            {
                return 2.5f * (1f - r / GALAXY_CORE_RADIUS * 0.5f);
            }

            // 星系盘外：极低密度（晕族）
            if (r > GALAXY_DISK_RADIUS) return 0.1f;

            // 悬臂上：密度高
            float armDist = DistanceToNearestArm(x, y);
            float baseDensity;
            if (armDist < GALAXY_ARM_WIDTH)
            {
                float armFactor = 1f - armDist / GALAXY_ARM_WIDTH;
                baseDensity = 1.5f + armFactor * 1.5f; // 1.5-3.0
            }
            else
            {
                baseDensity = 0.3f; // 悬臂间：低密度
            }

            // 星团内：密度额外加成（疏散星团1.5x，球状星团2.5x）
            var cluster = GetClusterAt(x, y);
            if (cluster != null)
            {
                float clusterBoost = cluster.type == ClusterType.Globular ? 2.5f : 1.5f;
                // 星团中心密度最高，边缘递减
                float cdx = x - cluster.x, cdy = y - cluster.y;
                float cdist = Mathf.Sqrt(cdx * cdx + cdy * cdy);
                float centerFactor = 1f - cdist / cluster.radius * 0.5f;
                baseDensity *= clusterBoost * centerFactor;
            }

            return baseDensity;
        }

        /// <summary>判断位置是否在星系悬臂上</summary>
        public static bool IsInSpiralArm(float x, float y)
        {
            float dx = x - GALAXY_CENTER_X;
            float dy = y - GALAXY_CENTER_Y;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < GALAXY_CORE_RADIUS) return true;
            if (r > GALAXY_DISK_RADIUS) return false;
            return DistanceToNearestArm(x, y) < GALAXY_ARM_WIDTH;
        }

        // ========== 星团系统（现实天体物理：疏散星团+球状星团） ==========
        public enum ClusterType { Open, Globular } // 疏散星团/球状星团

        public class StarCluster
        {
            public string id;
            public string name;
            public ClusterType type;
            public float x;
            public float y;
            public float radius; // 星团半径
            public int memberCount; // 成员恒星数量
        }

        private static List<StarCluster> _clusters;
        private static bool _clustersGenerated;

        /// <summary>预生成星团（沿旋臂分布疏散星团，晕族分布球状星团）</summary>
        public static void GenerateClusters()
        {
            if (_clustersGenerated) return;
            _clustersGenerated = true;
            _clusters = new List<StarCluster>();

            System.Random rng = new System.Random(_worldSeed + 99999);

            // 疏散星团（Open Cluster）：沿旋臂分布，年轻恒星松散聚集
            for (int i = 0; i < 30; i++)
            {
                // 在旋臂上随机找位置
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = GALAXY_CORE_RADIUS + (float)rng.NextDouble() * (GALAXY_DISK_RADIUS - GALAXY_CORE_RADIUS);
                float cx = GALAXY_CENTER_X + Mathf.Cos(angle) * dist;
                float cy = GALAXY_CENTER_Y + Mathf.Sin(angle) * dist;
                // 微调让它在旋臂上
                if (!IsInSpiralArm(cx, cy)) { i--; continue; }

                _clusters.Add(new StarCluster
                {
                    id = $"open_{i}",
                    name = GenerateClusterName(rng, "疏散"),
                    type = ClusterType.Open,
                    x = cx,
                    y = cy,
                    radius = 80f + (float)rng.NextDouble() * 120f, // 80-200
                    memberCount = rng.Next(50, 500)
                });
            }

            // 球状星团（Globular Cluster）：晕族中分布，古老恒星紧密聚集
            for (int i = 0; i < 15; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = GALAXY_DISK_RADIUS * 0.5f + (float)rng.NextDouble() * GALAXY_DISK_RADIUS * 1.5f;
                float cx = GALAXY_CENTER_X + Mathf.Cos(angle) * dist;
                float cy = GALAXY_CENTER_Y + Mathf.Sin(angle) * dist;

                _clusters.Add(new StarCluster
                {
                    id = $"glob_{i}",
                    name = GenerateClusterName(rng, "球状"),
                    type = ClusterType.Globular,
                    x = cx,
                    y = cy,
                    radius = 150f + (float)rng.NextDouble() * 200f, // 150-350
                    memberCount = rng.Next(1000, 100000)
                });
            }
        }

        private static string GenerateClusterName(System.Random rng, string prefix)
        {
            string[] greek = { "昴", "毕", "蟹", "猎", "仙", "英", "金", "银", "铜", "铁", "星", "云", "光", "影", "魂", "魄" };
            string[] suffix = { "星团", "M", "NGC", "星协" };
            return $"{prefix}{greek[rng.Next(greek.Length)]}{suffix[rng.Next(suffix.Length)]}";
        }

        /// <summary>获取所有星团</summary>
        public static List<StarCluster> GetClusters()
        {
            GenerateClusters();
            return _clusters;
        }

        /// <summary>获取范围内的星团</summary>
        public static List<StarCluster> GetClustersInRange(float x, float y, float range)
        {
            GenerateClusters();
            var result = new List<StarCluster>();
            foreach (var c in _clusters)
            {
                float dx = c.x - x, dy = c.y - y;
                if (dx * dx + dy * dy < (range + c.radius) * (range + c.radius))
                    result.Add(c);
            }
            return result;
        }

        /// <summary>获取位置所在的星团（如果在星团内）</summary>
        public static StarCluster GetClusterAt(float x, float y)
        {
            GenerateClusters();
            foreach (var c in _clusters)
            {
                float dx = c.x - x, dy = c.y - y;
                if (dx * dx + dy * dy < c.radius * c.radius)
                    return c;
            }
            return null;
        }

        /// <summary>按ID获取星团</summary>
        public static StarCluster GetCluster(string id)
        {
            GenerateClusters();
            foreach (var c in _clusters)
            {
                if (c.id == id) return c;
            }
            return null;
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

        /// <summary>生成一个星区分块内的星系（恒星+行星围绕）</summary>
        private static void GenerateChunk(int cx, int cy)
        {
            string key = $"{cx}_{cy}";
            if (_generatedChunks.Contains(key)) return;
            _generatedChunks.Add(key);

            bool isCenterChunk = (cx == 0 && cy == 0);
            System.Random rng = new System.Random(ChunkSeed(cx, cy));

            // 根据星系结构计算恒星密度（悬臂密集、核球最密、晕族稀少）
            float chunkCenterX = cx * CHUNK_SIZE + CHUNK_SIZE / 2f;
            float chunkCenterY = cy * CHUNK_SIZE + CHUNK_SIZE / 2f;
            float density = GetStarDensity(chunkCenterX, chunkCenterY);

            // 恒星数量根据密度调整：核球3-5个，悬臂2-4个，悬臂间1-2个，晕族0-1个
            int starCount;
            if (density > 2f) starCount = rng.Next(3, 6);       // 核球
            else if (density > 1.5f) starCount = rng.Next(2, 5); // 悬臂
            else if (density > 0.5f) starCount = rng.Next(1, 3); // 悬臂间
            else starCount = rng.Next(0, 2);                      // 晕族

            for (int s = 0; s < starCount; s++)
            {
                // 恒星位置（块内随机，留边距）
                float starLocalX = 80f + (float)rng.NextDouble() * (CHUNK_SIZE - 160f);
                float starLocalY = 80f + (float)rng.NextDouble() * (CHUNK_SIZE - 160f);
                float starX = cx * CHUNK_SIZE + starLocalX;
                float starY = cy * CHUNK_SIZE + starLocalY;

                // 中心块跳过离主星球太近的恒星
                if (isCenterChunk)
                {
                    float distToMain = Mathf.Sqrt(starX * starX + starY * starY);
                    if (distToMain < 150f) continue;
                }

                string starId = $"star_{cx}_{cy}_{s}";
                if (_planets.ContainsKey(starId)) continue;

                // 恒星类型（参考真实宇宙分布：红矮星最常见，黑洞最稀有）
                int starRoll = rng.Next(100);
                StarType sType;
                if (starRoll < 70) sType = StarType.RedDwarf;       // 70% 红矮星
                else if (starRoll < 80) sType = StarType.YellowDwarf; // 10% 黄矮星
                else if (starRoll < 85) sType = StarType.BlueGiant;   // 5% 蓝巨星
                else if (starRoll < 90) sType = StarType.RedGiant;    // 5% 红巨星
                else if (starRoll < 95) sType = StarType.WhiteDwarf;  // 5% 白矮星
                else if (starRoll < 98) sType = StarType.NeutronStar; // 3% 中子星
                else sType = StarType.BlackHole;                       // 2% 黑洞

                // 恒星名称（根据类型加后缀）
                string starPrefix = NamePrefixes[rng.Next(NamePrefixes.Length)];
                string starName = starPrefix + (sType == StarType.BlackHole ? "黑洞" :
                    sType == StarType.NeutronStar ? "中子星" :
                    sType == StarType.WhiteDwarf ? "白矮星" : "恒星");

                // 计算恒星物理参数（质量、半径、光度、年龄）
                float starMass = StarPhysics.GetStarMass(sType);
                float starRadiusPhys = StarPhysics.GetStarRadius(sType);
                float starLum = StarPhysics.GetStarLuminosity(sType);
                float starAge = (float)(rng.NextDouble() * 100); // 0-100亿年

                _planets[starId] = new PlanetData
                {
                    id = starId,
                    name = starName,
                    type = PlanetType.Star,
                    x = starX,
                    y = starY,
                    resource = 0,
                    maxResource = 0,
                    ownerKingdom = null,
                    colonyLevel = 0,
                    discovered = false,
                    parentStar = null,
                    hasRing = false,
                    starType = sType,
                    temperature = StarPhysics.GetStarBaseTemp(sType),
                    starMass = starMass,
                    starRadius = starRadiusPhys,
                    starLuminosity = starLum,
                    starAge = starAge
                };

                // 按现实恒星系结构生成行星：内侧类地行星→小行星带→外侧气态巨行星
                // 行星数量根据恒星质量调整（大质量恒星行星更多）
                int planetCount = 2 + (int)(starMass * 2) + rng.Next(0, 3);
                planetCount = Mathf.Clamp(planetCount, 2, 8);

                for (int p = 0; p < planetCount; p++)
                {
                    string pid = $"chunk_{cx}_{cy}_{s}_{p}";
                    if (_planets.ContainsKey(pid)) continue;

                    // 轨道半径：内侧密集，外侧稀疏（对数分布）
                    float orbitRadius = 40f + Mathf.Pow(p + 1, 1.3f) * 25f + (float)rng.NextDouble() * 10f;
                    float orbitAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    // 开普勒第三定律：公转速度 ∝ 1/√r（越远越慢）
                    float orbitSpeed = 0.15f / Mathf.Sqrt(orbitRadius / 50f);
                    // 轨道偏心率：内侧低（近圆），外侧高（椭圆）
                    float eccentricity = 0.02f + (float)rng.NextDouble() * 0.15f * (p / (float)planetCount);
                    float inclination = (float)(rng.NextDouble() * 15f); // 轨道倾角0-15度

                    // 计算行星温度（根据恒星光度和轨道半径，反平方定律）
                    float planetTemp = StarPhysics.CalculatePlanetTempFromLuminosity(starLum, orbitRadius);
                    bool habitable = planetTemp >= 273f && planetTemp <= 373f;

                    // 行星初始位置（椭圆轨道）
                    float planetX = starX + Mathf.Cos(orbitAngle) * orbitRadius * (1f + eccentricity * 0.3f);
                    float planetY = starY + Mathf.Sin(orbitAngle) * orbitRadius;

                    // 根据轨道距离决定行星类型（现实结构：内侧类地，外侧气态）
                    PlanetType type;
                    float normalizedDist = orbitRadius / (40f + Mathf.Pow(planetCount, 1.3f) * 25f);
                    if (normalizedDist < 0.35f)
                    {
                        // 内侧：类地行星（岩石/沙漠/熔岩）
                        PlanetType[] inner = { PlanetType.Rocky, PlanetType.Desert, PlanetType.Lava };
                        type = inner[rng.Next(inner.Length)];
                    }
                    else if (normalizedDist < 0.5f)
                    {
                        // 中间：小行星带区域，偶尔有矮行星
                        type = rng.Next(3) == 0 ? PlanetType.Rocky : PlanetType.Desert;
                    }
                    else if (normalizedDist < 0.75f)
                    {
                        // 外侧：气态巨行星
                        type = PlanetType.Gas;
                    }
                    else
                    {
                        // 最外侧：冰巨星/冰行星
                        type = rng.Next(2) == 0 ? PlanetType.Ice : PlanetType.Gas;
                    }
                    // 温度修正：太热强制熔岩，太冷强制冰
                    if (planetTemp > 600f) type = PlanetType.Lava;
                    else if (planetTemp < 150f) type = PlanetType.Ice;
                    // 宜居带提高森林/海洋概率
                    if (habitable && rng.Next(3) != 0)
                        type = rng.Next(2) == 0 ? PlanetType.Forest : PlanetType.Ocean;

                    // 计算行星物理参数
                    float pMass, pRadius, pGravity, pDensity;
                    StarPhysics.CalculatePlanetPhysics(type, rng, out pMass, out pRadius, out pGravity, out pDensity);

                    // 自转周期：类地行星10-40小时，气态巨行星5-15小时
                    float rotationPeriod = type == PlanetType.Gas ?
                        5f + (float)rng.NextDouble() * 10f :
                        10f + (float)rng.NextDouble() * 30f;

                    // 黄赤交角：0-45度（地球23.4度）
                    float axialTilt = (float)(rng.NextDouble() * 45f);

                    // 卫星数量：气态巨行星多（10-80），类地行星少（0-3）
                    int moonCount = type == PlanetType.Gas ?
                        rng.Next(10, 80) : rng.Next(0, 4);

                    // 潮汐锁定：近距离行星（<0.3归一化距离）大概率被潮汐锁定
                    bool tidallyLocked = normalizedDist < 0.25f && rng.Next(3) != 0;
                    if (tidallyLocked) rotationPeriod = orbitSpeed * 24f; // 自转=公转

                    // 行星环：气态巨行星概率高，类地行星概率低
                    bool hasRing = type == PlanetType.Gas ? rng.Next(3) == 0 : rng.Next(20) == 0;

                    // 大气层：质量大的行星大气层厚，冰行星大气层薄
                    float atmosphere = Mathf.Clamp01(pMass * 0.5f + (type == PlanetType.Gas ? 0.5f : 0f));
                    if (type == PlanetType.Ice) atmosphere *= 0.5f;

                    // 磁场：快速自转+液态金属核=强磁场
                    float magneticField = Mathf.Clamp01((24f / rotationPeriod) * 0.5f + pDensity * 0.1f);

                    // 随机名称
                    string prefix = NamePrefixes[rng.Next(NamePrefixes.Length)];
                    string suffix = NameSuffixes[rng.Next(NameSuffixes.Length)];
                    string name = prefix + suffix;

                    _planets[pid] = new PlanetData
                    {
                        id = pid,
                        name = name,
                        type = type,
                        x = planetX,
                        y = planetY,
                        resource = rng.Next(100, 1000),
                        maxResource = 1000,
                        ownerKingdom = null,
                        colonyLevel = 0,
                        discovered = false,
                        parentStar = starId,
                        orbitRadius = orbitRadius,
                        orbitAngle = orbitAngle,
                        orbitSpeed = orbitSpeed,
                        eccentricity = eccentricity,
                        inclination = inclination,
                        hasRing = hasRing,
                        temperature = planetTemp,
                        inHabitableZone = habitable,
                        mass = pMass,
                        radius = pRadius,
                        gravity = pGravity,
                        density = pDensity,
                        rotationPeriod = rotationPeriod,
                        axialTilt = axialTilt,
                        moonCount = moonCount,
                        tidallyLocked = tidallyLocked,
                        atmosphere = atmosphere,
                        magneticField = magneticField
                    };
                }
            }
        }















        /// <summary>获取行星的实时位置（考虑公转和椭圆轨道）</summary>
        public static Vector2 GetPlanetPosition(PlanetData p, float time)
        {
            if (p.parentStar == null || p.type == PlanetType.Star)
            {
                return new Vector2(p.x, p.y);
            }
            var star = GetPlanet(p.parentStar);
            if (star == null) return new Vector2(p.x, p.y);
            float angle = p.orbitAngle + time * p.orbitSpeed;
            // 椭圆轨道：x方向半径*(1+e*cos)，y方向半径
            float r = p.orbitRadius * (1f + p.eccentricity * Mathf.Cos(angle));
            return new Vector2(
                star.x + Mathf.Cos(angle) * r,
                star.y + Mathf.Sin(angle) * p.orbitRadius
            );
        }
    }
}
