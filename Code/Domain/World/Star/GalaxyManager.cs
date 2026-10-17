using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.229 银河管理器：多星系地图、星系间跳跃、外星文明生成
    /// 参考Stellaris/群星的银河地图设计
    /// </summary>
    public static class GalaxyManager
    {
        private static GalaxyData _galaxy;
        private static bool _initialized;

        public static GalaxyData Galaxy => _galaxy;
        public static bool IsInitialized => _initialized;

        // 星系名称前缀（原著风格：破碎星环/古龙星区式中文科幻命名）
        private static readonly string[] SystemPrefixes = {
            "破碎", "古龙", "西枫", "风暴", "幽暗", "璀璨", "赤虹", "苍蓝",
            "深空", "星海", "银河", "猎户", "仙女", "天狼", "参宿", "织女",
            "天津", "心宿", "角宿", "氐宿", "尾宿", "斗宿", "牛宿", "虚宿",
            "危宿", "室宿", "壁宿", "奎宿", "娄宿", "胃宿", "昴宿", "毕宿"
        };

        private static readonly string[] SystemSuffixes = {
            "星区", "星域", "星环", "星系"
        };

        // 外星文明种族名（原创科幻风格，不借用其他IP）
        private static readonly string[] AlienSpecies = {
            "泽塔", "克洛诺斯", "阿瑞斯", "雅典娜", "波塞冬", "哈迪斯", "阿波罗", "阿尔忒弥斯",
            "硅基", "能量体", "虫族", "机械族", "灵能族", "晶体族", "暗物质族", "虚空族",
            "星灵", "泰坦", "巨兽", "微光", "深渊", "晨曦", "暮星", "幻光"
        };

        private static readonly string[] CivNames = {
            "联邦", "帝国", "共和国", "王国", "联合体", "共同体", "议会", "理事会",
            "同盟", "联盟", "酋长国", "汗国", "王朝", "教廷", "公会", "财团"
        };

        private static readonly string[] Governments = {
            "民主共和", "君主专制", "军事独裁", "神权政治", "寡头政治", "联邦制",
            "部落联盟", "企业统治", "AI治理", "蜂巢意识", "议会民主", "世袭君主"
        };

        private static readonly string[] Ideologies = {
            "和平主义", "军国主义", "排外主义", "亲外主义", "平等主义", "精英主义",
            "唯物主义", "唯心主义", "集体主义", "个人主义", "扩张主义", "孤立主义"
        };

        /// <summary>初始化银河（生成星系和外星文明）</summary>
        public static void InitGalaxy(int seed = -1)
        {
            if (_initialized) return;
            if (seed < 0) seed = UnityEngine.Random.Range(1, 999999);

            _galaxy = new GalaxyData
            {
                id = "galaxy_main",
                name = LocalizedTextManager.getText("sm_galaxy_name"),
                type = GalaxyType.Spiral,
                seed = seed,
                width = 1000f,
                height = 1000f,
                playerHomeSystem = "system_home"
            };

            var rng = new System.Random(seed);

            // 生成30-50个星系（旋涡星系臂分布）
            int systemCount = rng.Next(30, 51);
            GenerateStarSystems(rng, systemCount);

            // 生成3-6个外星文明
            int civCount = rng.Next(3, 7);
            GenerateAlienCivilizations(rng, civCount);

            // 初始化外交关系
            InitDiplomacy();

            _initialized = true;
            Debug.Log($"[超神机械师] 银河初始化完成：{_galaxy.systems.Count}个星系，{_galaxy.civilizations.Count}个文明");
        }

        /// <summary>生成星系（旋涡星系臂分布）</summary>
        private static void GenerateStarSystems(System.Random rng, int count)
        {
            // 玩家母星系位于中心
            var homeSystem = new StarSystemData
            {
                id = "system_home",
                name = LocalizedTextManager.getText("sm_home_system"),
                type = StarSystemType.Single,
                x = 0,
                y = 0,
                discovered = true,
                visited = true,
                ownerCiv = null,
                resourceBonus = 1.0f,
                threatLevel = 0
            };
            _galaxy.systems.Add(homeSystem);

            // 旋涡星系：4条旋臂，星系沿臂分布
            int arms = 4;
            for (int i = 0; i < count; i++)
            {
                int arm = i % arms;
                float armAngle = (arm / (float)arms) * Mathf.PI * 2;
                float distance = 50f + (i / (float)count) * 400f; // 从内到外
                float angleOffset = (float)(rng.NextDouble() - 0.5) * 0.5f;
                float angle = armAngle + distance * 0.01f + angleOffset; // 旋臂缠绕

                float x = Mathf.Cos(angle) * distance + (float)(rng.NextDouble() - 0.5) * 40f;
                float y = Mathf.Sin(angle) * distance + (float)(rng.NextDouble() - 0.5) * 40f;

                string prefix = SystemPrefixes[rng.Next(SystemPrefixes.Length)];
                string suffix = SystemSuffixes[rng.Next(SystemSuffixes.Length)];
                string name = $"{prefix}-{rng.Next(100, 999)} {suffix}";

                var system = new StarSystemData
                {
                    id = $"system_{i:D3}",
                    name = name,
                    type = (StarSystemType)rng.Next(Enum.GetValues(typeof(StarSystemType)).Length),
                    x = x,
                    y = y,
                    discovered = false,
                    visited = false,
                    ownerCiv = null,
                    resourceBonus = 0.5f + (float)rng.NextDouble() * 1.5f,
                    threatLevel = rng.Next(0, 6)
                };
                _galaxy.systems.Add(system);
            }
        }

        /// <summary>生成外星文明</summary>
        private static void GenerateAlienCivilizations(System.Random rng, int count)
        {
            var availableSystems = new List<StarSystemData>(_galaxy.systems);
            availableSystems.RemoveAll(s => s.id == "system_home");

            for (int i = 0; i < count && availableSystems.Count > 0; i++)
            {
                // 选择一个星系作为首都
                int idx = rng.Next(availableSystems.Count);
                var capital = availableSystems[idx];
                availableSystems.RemoveAt(idx);

                string species = AlienSpecies[rng.Next(AlienSpecies.Length)];
                string civName = $"{species}{CivNames[rng.Next(CivNames.Length)]}";

                var civ = new AlienCivilization
                {
                    id = $"civ_{i:D2}",
                    name = civName,
                    speciesName = species,
                    techLevel = (CivTechLevel)rng.Next(2, Enum.GetValues(typeof(CivTechLevel)).Length),
                    skinColor = new Color(
                        (float)rng.NextDouble(),
                        (float)rng.NextDouble(),
                        (float)rng.NextDouble(),
                        1f),
                    governmentType = Governments[rng.Next(Governments.Length)],
                    ideology = Ideologies[rng.Next(Ideologies.Length)],
                    isPlayer = false,
                    economy = 50f + (float)rng.NextDouble() * 150f,
                    military = 30f + (float)rng.NextDouble() * 100f,
                    research = 20f + (float)rng.NextDouble() * 80f,
                    population = rng.Next(100000, 10000000)
                };
                civ.totalPower = civ.economy + civ.military + civ.research;

                // 控制1-4个星系
                int controlledCount = rng.Next(1, 5);
                for (int j = 0; j < controlledCount && availableSystems.Count > 0; j++)
                {
                    int sysIdx = rng.Next(availableSystems.Count);
                    var sys = availableSystems[sysIdx];
                    availableSystems.RemoveAt(sysIdx);
                    sys.ownerCiv = civ.id;
                    civ.controlledSystems.Add(sys.id);
                }

                // 首都也加入控制
                capital.ownerCiv = civ.id;
                civ.controlledSystems.Insert(0, capital.id);

                _galaxy.civilizations.Add(civ);
            }
        }

        /// <summary>初始化外交关系</summary>
        private static void InitDiplomacy()
        {
            for (int i = 0; i < _galaxy.civilizations.Count; i++)
            {
                for (int j = i + 1; j < _galaxy.civilizations.Count; j++)
                {
                    var rel = new DiplomacyRelation
                    {
                        civA = _galaxy.civilizations[i].id,
                        civB = _galaxy.civilizations[j].id,
                        state = DiplomacyState.Unknown,
                        opinion = UnityEngine.Random.Range(-30, 30),
                        tradeVolume = 0
                    };
                    _galaxy.relations.Add(rel);
                }
            }
        }

        /// <summary>获取星系</summary>
        public static StarSystemData GetSystem(string id)
        {
            if (_galaxy == null) return null;
            return _galaxy.systems.Find(s => s.id == id);
        }

        /// <summary>获取玩家母星系</summary>
        public static StarSystemData GetHomeSystem()
        {
            return GetSystem(_galaxy?.playerHomeSystem);
        }

        /// <summary>计算两个星系间的距离（光年）</summary>
        public static float GetDistance(string systemA, string systemB)
        {
            var a = GetSystem(systemA);
            var b = GetSystem(systemB);
            if (a == null || b == null) return float.MaxValue;
            return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        }

        /// <summary>获取范围内的星系</summary>
        public static List<StarSystemData> GetSystemsInRange(string centerId, float range)
        {
            var result = new List<StarSystemData>();
            var center = GetSystem(centerId);
            if (center == null || _galaxy == null) return result;

            foreach (var sys in _galaxy.systems)
            {
                if (sys.id == centerId) continue;
                float dist = Mathf.Sqrt((sys.x - center.x) * (sys.x - center.x) + (sys.y - center.y) * (sys.y - center.y));
                if (dist <= range) result.Add(sys);
            }
            return result;
        }

        /// <summary>发现星系（探索）</summary>
        public static bool DiscoverSystem(string systemId)
        {
            var sys = GetSystem(systemId);
            if (sys == null || sys.discovered) return false;
            sys.discovered = true;
            Debug.Log($"[超神机械师] 发现新星系：{sys.name}");
            return true;
        }

        /// <summary>访问星系（舰队到达）</summary>
        public static bool VisitSystem(string systemId)
        {
            var sys = GetSystem(systemId);
            if (sys == null) return false;
            sys.discovered = true;
            sys.visited = true;
            return true;
        }

        /// <summary>获取外星文明</summary>
        public static AlienCivilization GetCivilization(string id)
        {
            return _galaxy?.civilizations.Find(c => c.id == id);
        }

        /// <summary>获取两个文明的外交关系</summary>
        public static DiplomacyRelation GetRelation(string civA, string civB)
        {
            return _galaxy?.relations.Find(r =>
                (r.civA == civA && r.civB == civB) || (r.civA == civB && r.civB == civA));
        }

        /// <summary>建立接触（两个文明互相发现）</summary>
        public static bool EstablishContact(string civA, string civB)
        {
            var rel = GetRelation(civA, civB);
            if (rel == null || rel.state != DiplomacyState.Unknown) return false;

            rel.state = DiplomacyState.Neutral;
            var a = GetCivilization(civA);
            var b = GetCivilization(civB);
            if (a != null && !a.knownCivs.Contains(civB)) a.knownCivs.Add(civB);
            if (b != null && !b.knownCivs.Contains(civA)) b.knownCivs.Add(civA);

            Debug.Log($"[超神机械师] 建立外交接触：{a?.name} ↔ {b?.name}");
            return true;
        }

        /// <summary>银河模拟tick（文明发展、外交变化）</summary>
        public static void GalaxyTick(float deltaTime)
        {
            if (_galaxy == null) return;
            _galaxy.turnCount++;

            // 文明经济增长
            foreach (var civ in _galaxy.civilizations)
            {
                float growth = 0.1f * deltaTime;
                civ.economy += growth * civ.controlledSystems.Count;
                civ.military += growth * 0.5f;
                civ.research += growth * 0.3f;
                civ.totalPower = civ.economy + civ.military + civ.research;
            }

            // 外交关系缓慢变化
            foreach (var rel in _galaxy.relations)
            {
                if (rel.state == DiplomacyState.Unknown) continue;
                // 贸易增加好感
                if (rel.hasTradeAgreement)
                {
                    rel.opinion = Mathf.Clamp(rel.opinion + 1, -100, 100);
                    rel.tradeVolume += 1f * deltaTime;
                }
                // 同盟增加好感
                if (rel.hasAlliance)
                {
                    rel.opinion = Mathf.Clamp(rel.opinion + 2, -100, 100);
                }
            }
        }

        /// <summary>重置银河（新游戏）</summary>
        public static void Reset()
        {
            _galaxy = null;
            _initialized = false;
        }
    }
}
