using NeoModLoader.api;
using NeoModLoader.services;
using SuperMech.Code;
using UnityEngine;
using HarmonyLib;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SuperMech
{


    public class Main : BasicMod<Main>
    {
        public new static Main Instance { get; private set; }
        public static string ModPath { get; private set; }
        // v0.220 极高倍速适配：用游戏时间差值追踪Tick，替代_promoTimer
        private float _lastTickGameTime = -1f;
        private MapBox _lastWorld;
        private bool _worldInitPending;

        protected override void OnModLoad()
        {
            Instance = this;
            ModPath = GetDeclaration().FolderPath;
            Debug.Log("[超神机械师] 模组加载: " + ModPath);

            // 手动加载本地化文件（确保cz.json被NML本地化系统识别）
            LoadLocales();

            SuperMechConfig.Init();
            SuperMechConfig.ApplyFromNML(GetConfig()); // v0.75.22: 启动全量同步配置值到静态字段（修复改配置不生效）
            SuperMechEventLogger.Init();

            // 统一系统注册（开闭原则：新增系统只需在SystemBootstrapper添加一行）
            SystemBootstrapper.Bootstrap();

            // 事件总线订阅：解耦系统间直接调用
            RegisterEventSubscriptions();

            var harmony = new Harmony("SuperMech");
            harmony.PatchAll();
            Debug.Log("[超神机械师] Harmony Patch 完成（单位面板注入+战斗挂钩）");

            SuperMechSaveData.Load();
            SuperMechWindowManager.Init();
            SuperMechUnitBars.Init();
            SuperMechMultiWorld.Init();
            SuperMechChoreographer.Init(); // v0.213 战斗动作编排器
            _ = SuperMechUpdateDriver.Instance; // 初始化全局Update驱动

            // v0.231 宇宙深化系统初始化
            PlanetExplorationManager.Init();
            TradeManager.Init();
            StationManager.Init();
            TechTreeManager.Init();

            Debug.Log("[超神机械师] Phase 1 系统注册完成");
        }

        /// <summary>手动加载Locales/cz.json到NML本地化系统</summary>
        private void LoadLocales()
        {
            try
            {
                string localePath = Path.Combine(ModPath, "Locales", "cz.json");
                if (!File.Exists(localePath))
                {
                    Debug.LogWarning("[超神机械师] 本地化文件不存在: " + localePath);
                    return;
                }
                string json = File.ReadAllText(localePath, System.Text.Encoding.UTF8);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                int count = 0;
                foreach (var kv in dict)
                {
                    LocalizedTextManager.add(kv.Key, kv.Value, pReplace: true);
                    count++;
                }
                Debug.Log($"[超神机械师] 本地化加载完成: {count}条文本");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 本地化加载失败: " + e.Message);
            }
        }

        private void Update()
        {
            SuperMechPowers.TryCreateButtons();
            SuperMechEventLogger.TryInit();

            if (!SuperMechConfig.ModEnabled) return;

            MapBox world = World.world;
            if (world != _lastWorld)
            {
                _lastWorld = world;
                if (world != null)
                {
                    Debug.Log("[超神机械师] 检测到世界切换，重置数据并加载存档");
                    SuperMechUnifiedTick.ClearAll();
                    SuperMechSaveData.Load();
                    _worldInitPending = true;
                    _lastTickGameTime = -1f; // 重置Tick时间追踪
                }
            }

            bool paused = Time.timeScale <= 0.01f;
            if (paused) return;

            if (_worldInitPending)
            {
                try { SuperMechSaveData.TryRestoreActors(); } catch { }
                if (!SuperMechSaveData.IsRestoring) _worldInitPending = false;
            }

            // v0.220 极高倍速适配：用游戏时间差值计算应执行的Tick次数，追赶模式
            // Time.deltaTime受maximumDeltaTime限制（默认0.333秒），10000倍速下每帧deltaTime不会超过0.333秒
            // 导致_promoTimer累加太慢，模组Tick严重跟不上游戏倍速
            // 改用Time.time差值：直接计算自上次Tick以来经过了多少游戏时间，然后执行对应次数的Tick
            if (_lastTickGameTime < 0f) _lastTickGameTime = Time.time;
            float elapsedGame = Time.time - _lastTickGameTime;
            if (elapsedGame >= SuperMechConfig.TickInterval)
            {
                int tickCount = Mathf.FloorToInt(elapsedGame / SuperMechConfig.TickInterval);
                // 限制每帧最多执行30次Tick，防止极高倍速下一帧执行几千次导致卡死
                // 超过部分累积到下一帧继续追赶
                int maxPerFrame = 30;
                if (tickCount > maxPerFrame)
                {
                    Debug.LogWarning($"[超神机械师] 极高倍速追赶：应执行{tickCount}次Tick，限制为{maxPerFrame}次/帧，剩余将在下一帧继续");
                    tickCount = maxPerFrame;
                }
                for (int i = 0; i < tickCount; i++)
                {
                    try { SuperMechUnifiedTick.Tick(); }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 统一tick异常: " + e.Message); }
                }
                _lastTickGameTime += tickCount * SuperMechConfig.TickInterval;
            }

            // v0.231 宇宙深化系统实时tick（不依赖游戏倍速）
            float dt = Time.deltaTime;
            try { PlanetExplorationManager.MarketTick(dt); } catch { }
            try { StationManager.ProductionTick(dt); } catch { }
            try { TechTreeManager.ResearchTick(dt); } catch { }
        }

        /// <summary>
        /// 事件总线订阅注册：系统间通过事件通信，不直接调用。
        /// 新系统接入时在此添加订阅，逐步替代直接调用。
        /// </summary>
        private static void RegisterEventSubscriptions()
        {
            // 阶位变化 → 事件日志
            SuperMechEventBus.Subscribe<ActorRankChangedEvent>("ActorRankChanged", evt =>
            {
                if (evt.actor == null) return;
                string oldName = evt.oldRank >= 0 && evt.oldRank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[evt.oldRank].name) : "?";
                string newName = evt.newRank >= 0 && evt.newRank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[evt.newRank].name) : "?";
                SuperMechEventLogger.LogPromotion(evt.actor, oldName, newName, 0f, evt.newRank);
            });

            // 势力创建 → 事件日志
            SuperMechEventBus.Subscribe<FactionCreatedEvent>("FactionCreated", evt =>
            {
                if (evt.leader == null) return;
                Debug.Log($"[超神机械师] 势力创建事件: {evt.leader.name} 创建了{evt.factionName}");
            });

            // 圣所进入 → 调试日志（事件日志已在Sanctuary内部直接调用）
            SuperMechEventBus.Subscribe<SanctuaryEnteredEvent>("SanctuaryEntered", evt =>
            {
                if (evt.actor == null) return;
                var ctx = SuperMechActorContextRegistry.Get(evt.actor);
                if (ctx != null) ctx.sanctuaryVisits++;
            });

            // 知识解锁 → 调试日志
            SuperMechEventBus.Subscribe<KnowledgeUnlockedEvent>("KnowledgeUnlocked", evt =>
            {
                if (evt.actor == null) return;
                Debug.Log($"[超神机械师] 知识解锁事件: {evt.actor.name} 学会了{evt.knowledgeName}");
            });

            // 觉醒 → 同步ActorContext标记
            SuperMechEventBus.Subscribe<ActorAwakenedEvent>("ActorAwakened", evt =>
            {
                if (evt.actor == null) return;
                var ctx = SuperMechActorContextRegistry.Get(evt.actor);
                if (ctx != null) ctx.hasTalent = true;
            });

            Debug.Log("[超神机械师] 事件总线订阅注册完成");
        }
    }
}
