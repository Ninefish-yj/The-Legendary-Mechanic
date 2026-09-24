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
            SuperMechSpecialty.Register();
            SuperMechRankSpecialty.Register();
            SuperMechRace.Register();
            SuperMechSanctuary.Register();
            SuperMechCrafting.Register();
            SuperMechPowers.Register();
            SuperMechUI.Init();  // 必须在 powers 注册之后（PowerButton.OnEnable 按名字查 powers）

            // Harmony Patch：单位面板注入阶位/职业/气力/欧纳数据行
            var harmony = new Harmony("SuperMech");
            harmony.PatchAll();
            LogInfo("[超神机械师] Harmony Patch 完成（单位面板注入）");

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

            _promoTimer += Time.deltaTime;
            if (_promoTimer >= SuperMechConfig.TickInterval)
            {
                _promoTimer = 0f;
                try { SuperMechAdvancement.TickPromotions(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 晋升循环异常: " + e.Message); }
                try { SuperMechFavorite.TickAutoFavorite(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 自动收藏异常: " + e.Message); }
                try { SuperMechQi.TickQiLevels(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 气力等级异常: " + e.Message); }
                try { SuperMechPotential.TickPotential(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 潜能点异常: " + e.Message); }
                try { SuperMechCorePower.TickCorePowers(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 核心能量异常: " + e.Message); }
                try { SuperMechQiAttribute.TickAutoAssign(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 气力属性异常: " + e.Message); }
                try { SuperMechRefinement.TickRefinement(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 提炼法异常: " + e.Message); }
                try { SuperMechCultivation.TickCultivation(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 修炼功法异常: " + e.Message); }
                try { SuperMechSanctuary.TickDivinity(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 神性蜕变异常: " + e.Message); }
                try { SuperMechSanctuary.TickDeadTracking(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 死者追踪异常: " + e.Message); }
                try { SuperMechSubClass.TickSubLevels(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 副职业等级异常: " + e.Message); }
                try { SuperMechRelic.TickRelicDrops(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 宝物掉落异常: " + e.Message); }
                try { SuperMechMageTower.TickMageTowers(); }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 法师塔异常: " + e.Message); }
            }
        }
    }
}
