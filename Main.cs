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
        private float _promoTimer;
        private float _localeTimer;
        private bool _localeExported;
        private MapBox _lastWorld;
        private bool _worldInitPending;

        protected override void OnModLoad()
        {
            Instance = this;
            LogInfo("[超神机械师] 模组加载");
            SuperMechConfig.Init();
            SuperMechTraitGroups.Register();
            SuperMechCustomStats.Register();
            SuperMechTraits.Register();
            SuperMechSkills.Register();
            SuperMechKnowledge.Register();
            SuperMechKnowledgeSynergy.Register();
            SuperMechKnowledgeFusion.Register();
            SuperMechQi.Register();
            SuperMechCorePower.Register();
            SuperMechPerks.Register();
            SuperMechSubClass.Register();
            SuperMechBranch.Register();
            SuperMechRefinement.Register();
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
            SuperMechUI.Init();

            var harmony = new Harmony("SuperMech");
            harmony.PatchAll();
            LogInfo("[超神机械师] Harmony Patch 完成（单位面板注入+战斗挂钩）");

            SuperMechSaveData.Load();

            LogInfo("[超神机械师] Phase 1 系统注册完成");
        }

        private void Update()
        {
            if (!_localeExported)
            {
                _localeTimer += Time.unscaledDeltaTime;
                if (_localeTimer >= 4f)
                {
                    _localeExported = true;
                    try { SuperMechLocaleExport.Export(); }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 本地化导出异常: " + e.Message); }
                }
            }

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

            try { SuperMechPanel.Tick(); } catch { }

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
    }
}
