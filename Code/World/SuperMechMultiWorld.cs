using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 多世界系统 - 实现类似PoP的效果：每个星球是独立的WorldBox地图，可进入游玩，后台持续模拟
    /// </summary>
    public static class SuperMechMultiWorld
    {
        /// <summary>当前所在星球ID</summary>
        public static string CurrentPlanetId = SuperMechStarSystem.MAIN_PLANET;

        /// <summary>是否正在切换世界</summary>
        public static bool IsSwitching = false;

        /// <summary>待切换的目标星球ID</summary>
        private static string _pendingPlanetId = null;

        /// <summary>星球存档根目录</summary>
        private static string PlanetSaveRoot => Path.Combine(Application.persistentDataPath, "SuperMechPlanets");

        /// <summary>后台模拟数据（星球ID -> 模拟状态）</summary>
        private static readonly Dictionary<string, PlanetSimState> _simStates = new Dictionary<string, PlanetSimState>();

        /// <summary>星球模拟状态</summary>
        public class PlanetSimState
        {
            public string planetId;
            public int population;      // 人口
            public int kingdoms;        // 王国数
            public int cities;          // 城市数
            public float techLevel;     // 科技水平
            public float lastSimTime;   // 上次模拟时间
            public bool isInitialized;  // 是否已初始化
        }

        /// <summary>初始化多世界系统</summary>
        public static void Init()
        {
            try
            {
                if (!Directory.Exists(PlanetSaveRoot))
                {
                    Directory.CreateDirectory(PlanetSaveRoot);
                }
                Debug.Log("[超神机械师]【多世界】系统初始化完成，存档目录: " + PlanetSaveRoot);
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】初始化失败: " + e.Message);
            }
        }

        /// <summary>获取星球存档路径</summary>
        public static string GetPlanetSavePath(string planetId)
        {
            return Path.Combine(PlanetSaveRoot, planetId) + Path.DirectorySeparatorChar;
        }

        /// <summary>检查星球是否已有存档</summary>
        public static bool HasPlanetSave(string planetId)
        {
            string path = GetPlanetSavePath(planetId);
            return Directory.Exists(path) && File.Exists(path + "map.meta");
        }

        /// <summary>保存当前世界到当前星球存档</summary>
        public static bool SaveCurrentWorld()
        {
            try
            {
                string path = GetPlanetSavePath(CurrentPlanetId);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // 设置当前存档路径
                SaveManager.currentSavePath = path;

                // 保存世界
                SaveManager.saveWorldToDirectory(path, pCompress: true, pCheckFolder: false);

                // 保存模组自定义数据（科技/阶位/知识/装备等）
                // SuperMechSaveData.GetSavePath()使用current_world_seed_id作为文件名，
                // 每个星球种子不同，自动分开存储
                SuperMechSaveData.Save();

                // 更新模拟状态
                UpdateSimStateFromWorld(CurrentPlanetId);

                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师]【多世界】已保存星球 {CurrentPlanetId} 到 {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】保存世界失败: " + e.Message);
                return false;
            }
        }

        /// <summary>切换到目标星球</summary>
        public static void SwitchToPlanet(string planetId)
        {
            if (IsSwitching)
            {
                Debug.LogWarning("[超神机械师]【多世界】正在切换中，忽略重复请求");
                return;
            }

            if (planetId == CurrentPlanetId)
            {
                Debug.Log("[超神机械师]【多世界】已在目标星球");
                return;
            }

            IsSwitching = true;
            _pendingPlanetId = planetId;

            Debug.Log($"[超神机械师]【多世界】开始切换: {CurrentPlanetId} -> {planetId}");

            // 第一步：保存当前世界
            bool saved = SaveCurrentWorld();

            if (saved)
            {
                // 第二步：启动太空飞行动画（飞行完成后自动切换）
                bool flightStarted = SuperMechSpaceFlight.StartFlight(CurrentPlanetId, planetId, null, true);

                if (!flightStarted)
                {
                    // 飞行启动失败，直接切换
                    LoadPlanetWorld(planetId);
                }
                // 如果飞行启动成功，由SuperMechSpaceFlight.CompleteFlight调用LoadPlanetWorld
            }
            else
            {
                IsSwitching = false;
                _pendingPlanetId = null;
                Debug.LogError("[超神机械师]【多世界】保存失败，取消切换");
            }
        }

        /// <summary>飞行完成后实际加载星球（由SpaceFlight调用）</summary>
        public static void OnFlightComplete(string planetId)
        {
            LoadPlanetWorld(planetId);
        }

        /// <summary>加载星球世界</summary>
        private static void LoadPlanetWorld(string planetId)
        {
            try
            {
                string path = GetPlanetSavePath(planetId);

                if (HasPlanetSave(planetId))
                {
                    // 已有存档，直接加载
                    Debug.Log($"[超神机械师]【多世界】加载已有存档: {planetId}");
                    LoadWorldFromPath(path);
                }
                else
                {
                    // 没有存档，生成新星球地图
                    Debug.Log($"[超神机械师]【多世界】生成新星球地图: {planetId}");
                    GeneratePlanetWorld(planetId);
                }

                CurrentPlanetId = planetId;
                IsSwitching = false;
                _pendingPlanetId = null;
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】加载星球失败: " + e.Message);
                IsSwitching = false;
                _pendingPlanetId = null;
            }
        }

        /// <summary>从路径加载世界（通过反射调用internal方法）</summary>
        private static void LoadWorldFromPath(string path)
        {
            try
            {
                SaveManager.currentSavePath = path;

                // MapBox.instance.save_manager 是 internal SaveManager
                // loadWorld(string, bool) 是 internal 方法，需要反射调用
                FieldInfo saveMgrField = typeof(MapBox).GetField("save_manager",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (saveMgrField != null && MapBox.instance != null)
                {
                    object saveMgr = saveMgrField.GetValue(MapBox.instance);
                    if (saveMgr != null)
                    {
                        MethodInfo loadMethod = saveMgr.GetType().GetMethod("loadWorld",
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            new Type[] { typeof(string), typeof(bool) },
                            null);

                        if (loadMethod != null)
                        {
                            loadMethod.Invoke(saveMgr, new object[] { path, false });
                            return;
                        }
                    }
                }

                Debug.LogError("[超神机械师]【多世界】无法通过反射调用loadWorld");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】LoadWorldFromPath失败: " + e.Message);
            }
        }

        /// <summary>生成新星球地图</summary>
        private static void GeneratePlanetWorld(string planetId)
        {
            try
            {
                var planet = SuperMechStarSystem.GetPlanet(planetId);
                if (planet == null)
                {
                    Debug.LogError($"[超神机械师]【多世界】星球数据不存在: {planetId}");
                    return;
                }

                // 创建存档目录
                string path = GetPlanetSavePath(planetId);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // 设置存档路径
                SaveManager.currentSavePath = path;

                // 生成新地图（WorldBox默认生成）
                // 后续可以根据星球类型定制地形
                if (MapBox.instance != null)
                {
                    MapBox.instance.generateNewMap();
                }

                // 初始化模拟状态
                InitSimState(planetId, planet);

                // 立即保存
                SaveManager.saveWorldToDirectory(path, pCompress: true, pCheckFolder: false);

                Debug.Log($"[超神机械师]【多世界】新星球地图生成完成: {planetId} ({planet.type})");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】生成星球地图失败: " + e.Message);
            }
        }

        /// <summary>初始化星球模拟状态</summary>
        private static void InitSimState(string planetId, SuperMechStarSystem.PlanetData planet)
        {
            if (!_simStates.ContainsKey(planetId))
            {
                _simStates[planetId] = new PlanetSimState
                {
                    planetId = planetId,
                    population = 0,
                    kingdoms = 0,
                    cities = 0,
                    techLevel = 0,
                    lastSimTime = Time.time,
                    isInitialized = true
                };
            }
        }

        /// <summary>从当前世界更新模拟状态</summary>
        private static void UpdateSimStateFromWorld(string planetId)
        {
            try
            {
                if (World.world == null) return;

                int population = World.world.units?.Count ?? 0;
                int kingdoms = World.world.kingdoms?.list?.Count ?? 0;
                int cities = World.world.cities?.Count ?? 0;

                if (!_simStates.ContainsKey(planetId))
                {
                    _simStates[planetId] = new PlanetSimState { planetId = planetId, isInitialized = true };
                }

                var state = _simStates[planetId];
                state.population = population;
                state.kingdoms = kingdoms;
                state.cities = cities;
                state.lastSimTime = Time.time;
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】更新模拟状态失败: " + e.Message);
            }
        }

        /// <summary>后台模拟Tick（不在当前星球时模拟其发展）</summary>
        public static void BackgroundSimTick(float dt)
        {
            try
            {
                foreach (var kvp in _simStates)
                {
                    string pid = kvp.Key;
                    var state = kvp.Value;

                    // 跳过当前星球（当前星球由游戏引擎真实运行）
                    if (pid == CurrentPlanetId) continue;
                    if (!state.isInitialized) continue;

                    // 简化模拟：人口缓慢增长，科技缓慢提升
                    float simRate = 0.1f; // 模拟速率（相对于真实时间）
                    state.population += (int)(state.population * 0.001f * simRate * dt);
                    state.techLevel += 0.001f * simRate * dt;

                    // 限制上限
                    state.population = Mathf.Min(state.population, 100000);
                    state.techLevel = Mathf.Min(state.techLevel, 10f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【多世界】后台模拟失败: " + e.Message);
            }
        }

        /// <summary>获取星球模拟状态</summary>
        public static PlanetSimState GetSimState(string planetId)
        {
            if (_simStates.TryGetValue(planetId, out var state))
                return state;
            return null;
        }

        /// <summary>返回主世界</summary>
        public static void ReturnToMainWorld()
        {
            if (CurrentPlanetId != SuperMechStarSystem.MAIN_PLANET)
            {
                SwitchToPlanet(SuperMechStarSystem.MAIN_PLANET);
            }
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _simStates.Clear();
            CurrentPlanetId = SuperMechStarSystem.MAIN_PLANET;
            IsSwitching = false;
            _pendingPlanetId = null;
        }
    }
}
