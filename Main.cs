using NeoModLoader.api;
using NeoModLoader.services;
using SuperMech.Code;
using UnityEngine;
using HarmonyLib;

namespace SuperMech
{
    /// <summary>
    /// 《超神机械师》WorldBox 模组入口（作者：阿鱼要吃书 · 原著：齐佩甲）
    /// </summary>
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
            SuperMechConfig.Init();  // 配置系统最先初始化（default_config.json 由 NML 自动加载）
            SuperMechTraitGroups.Register();  // 必须最先：自定义group_id不注册会导致特质面板KeyNotFound崩溃
            SuperMechCustomStats.Register();  // 自定义属性（气力/能级/械力等），在特质注册前
            SuperMechTraits.Register();
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
            SuperMechUI.Init();  // 必须在 powers 注册之后（PowerButton.OnEnable 按名字查 powers）

            // Harmony Patch：单位面板注入阶位/职业/气力/欧纳数据行 + 战斗挂钩
            var harmony = new Harmony("SuperMech");
            harmony.PatchAll();
            LogInfo("[超神机械师] Harmony Patch 完成（单位面板注入+战斗挂钩）");

            // 加载存档数据（气力/职业/神性蜕变等）
            SuperMechSaveData.Load();

            LogInfo("[超神机械师] Phase 1 系统注册完成");
        }

        private void Update()
        {
            // 启动约4秒后，等语言加载与全部中文注册完成，导出一次完整 cz.json
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

            // 世界切换检测：切换世界时重置所有静态字典数据，加载新世界存档
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

            // 游戏暂停时停止模拟（Time.timeScale <= 0.01）
            bool paused = Time.timeScale <= 0.01f;
            if (paused) return;

            // 面板窗口每帧检测单位变化
            try { SuperMechPanel.Tick(); } catch { }

            // 存档恢复：世界加载后单位逐步生成，每次tick尝试匹配恢复
            if (_worldInitPending)
            {
                try { SuperMechSaveData.TryRestoreActors(); } catch { }
                if (!SuperMechSaveData.IsRestoring) _worldInitPending = false;
            }

            _promoTimer += Time.unscaledDeltaTime;
            if (_promoTimer >= SuperMechConfig.TickInterval)
            {
                _promoTimer = 0f;
                // 统一tick调度：40+系统分4组错峰执行，避免一帧内40次全量遍历
                try { SuperMechUnifiedTick.Tick(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 统一tick异常: " + e.Message); }
            }
        }
    }
}
