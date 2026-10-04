using NeoModLoader.api;
using NeoModLoader.services;
using SuperMech.Code;
using UnityEngine;
using HarmonyLib;

namespace SuperMech
{


    public class Main : BasicMod<Main>
    {
        public new static Main Instance { get; private set; }
        public static string ModPath { get; private set; }
        private float _promoTimer;
        private MapBox _lastWorld;
        private bool _worldInitPending;

        protected override void OnModLoad()
        {
            Instance = this;
            ModPath = GetDeclaration().FolderPath;
            Debug.Log("[超神机械师] 模组加载: " + ModPath);
            SuperMechConfig.Init();
            SuperMechEventLogger.Init();
            SuperMechTraitGroups.Register();
            SuperMechCustomStats.Register();
            SuperMechTraits.Register();
            SuperMechSkills.Register();
            SuperMechKnowledge.Register();
            SuperMechKnowledgeSynergy.Register();
            SuperMechKnowledgeFusion.Register();
            SuperMechKnowledgeRecipe.RegisterBaseRecipes();
            SuperMechQi.Register();
            SuperMechCorePower.Register();
            SuperMechPerks.Register();
            SuperMechSubClass.Register();
            SuperMechBranch.Register();
            SuperMechSpecialization.Register();
            SuperMechMageType.Register();
            SuperMechSpell.RegisterAll();
            SuperMechMeditation.Register();
            SuperMechQiRefine.Register();
            SuperMechRelic.Register();
            SuperMechCosmicRelic.Register();
            SuperMechMageTower.Register();
            SuperMechAwakened.Register();
            SuperMechAdvancementTask.Register();
            SuperMechSpecialty.Register();
            SuperMechRankSpecialty.Register();
            SuperMechRace.Register();
            SuperMechSanctuary.Register();
            SuperMechCrafting.Register();
            SuperMechPowers.Register();
            SuperMechCrossMod.Init();

            // 事件总线订阅：解耦系统间直接调用
            RegisterEventSubscriptions();

            var harmony = new Harmony("SuperMech");
            harmony.PatchAll();
            Debug.Log("[超神机械师] Harmony Patch 完成（单位面板注入+战斗挂钩）");

            SuperMechSaveData.Load();
            SuperMechTab.Init();
            SuperMechWindowManager.Init();

            Debug.Log("[超神机械师] Phase 1 系统注册完成");
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
                }
            }

            bool paused = Time.timeScale <= 0.01f;
            if (paused) return;

            if (_worldInitPending)
            {
                try { SuperMechSaveData.TryRestoreActors(); } catch { }
                if (!SuperMechSaveData.IsRestoring) _worldInitPending = false;
            }

            _promoTimer += Time.unscaledDeltaTime;
            if (_promoTimer >= SuperMechConfig.TickInterval)
            {
                _promoTimer = 0f;
                try { SuperMechUnifiedTick.Tick(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 统一tick异常: " + e.Message); }
            }
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
