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

        protected override void OnModLoad()
        {
            Instance = this;
            LogInfo("[超神机械师] 模组加载");
            SuperMechConfig.Init();  // 配置系统最先初始化（default_config.json 由 NML 自动加载）
            SuperMechTraitGroups.Register();  // 必须最先：自定义group_id不注册会导致特质面板KeyNotFound崩溃
            SuperMechTraits.Register();
            SuperMechKnowledge.Register();
            SuperMechQi.Register();
            SuperMechCorePower.Register();
            SuperMechPerks.Register();
            SuperMechSubClass.Register();
            SuperMechBranch.Register();
            SuperMechRefinement.Register();
            SuperMechCultivation.Register();
            SuperMechRelic.Register();
            SuperMechCosmicRelic.Register();
            SuperMechMageTower.Register();
            SuperMechAwakened.Register();
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
                _localeTimer += Time.deltaTime;
                if (_localeTimer >= 4f)
                {
                    _localeExported = true;
                    try { SuperMechLocaleExport.Export(); }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 本地化导出异常: " + e.Message); }
                }
            }

            if (!SuperMechConfig.ModEnabled) return;

            // 面板窗口每帧检测单位变化
            try { SuperMechPanel.Tick(); } catch { }

            // 存档恢复：世界加载后单位逐步生成，每次tick尝试匹配恢复
            try { SuperMechSaveData.TryRestoreActors(); } catch { }

            _promoTimer += Time.deltaTime;
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
