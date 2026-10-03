using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>超能者事件日志封装（v0.27.0 UI重构）
    /// 将觉醒/晋升/融合/迭代/复活等事件推送到WorldBox原生事件日志
    /// </summary>
    public static class SMEventLogger
    {
        private static bool _initialized;
        private static bool _initFailed;
        private static readonly Dictionary<string, WorldLogAsset> _logAssets = new Dictionary<string, WorldLogAsset>();

        /// <summary>初始化：注册自定义WorldLogAsset到原生日志库</summary>
        public static void Init()
        {
            if (_initialized) return;

            if (AssetManager.world_log_library == null)
            {
                _initFailed = true;
                Debug.LogWarning("[超神机械师] world_log_library未初始化，将延迟注册日志资产");
                return;
            }

            _initialized = true;
            _initFailed = false;

            RegisterLogAsset("sm_log_awaken", "sm_log_awaken_text", new Color(0.3f, 0.8f, 1f));
            RegisterLogAsset("sm_log_promotion", "sm_log_promotion_text", new Color(1f, 0.8f, 0.2f));
            RegisterLogAsset("sm_log_fusion", "sm_log_fusion_text", new Color(0.5f, 1f, 0.5f));
            RegisterLogAsset("sm_log_sanctuary", "sm_log_sanctuary_text", new Color(0.8f, 0.5f, 1f));
            RegisterLogAsset("sm_log_iteration", "sm_log_iteration_text", new Color(1f, 0.5f, 0.5f));
            RegisterLogAsset("sm_log_resurrection", "sm_log_resurrection_text", new Color(1f, 0.3f, 0.8f));
            RegisterLogAsset("sm_log_spell", "sm_log_spell_text", new Color(0.6f, 0.4f, 1f));
            RegisterLogAsset("sm_log_concept_reshape", "sm_log_concept_reshape_text", new Color(0.2f, 1f, 0.8f));

            Debug.Log("[超神机械师] 事件日志系统初始化完成，注册7类日志资产");
        }

        /// <summary>延迟初始化：在Update中调用，确保world_log_library已就绪</summary>
        public static void TryInit()
        {
            if (_initialized || !_initFailed) return;
            if (AssetManager.world_log_library != null)
            {
                Debug.Log("[超神机械师] world_log_library已就绪，执行延迟初始化");
                Init();
            }
        }

        /// <summary>注册自定义WorldLogAsset</summary>
        private static void RegisterLogAsset(string assetId, string localeId, Color color)
        {
            if (AssetManager.world_log_library == null)
            {
                Debug.LogWarning("[超神机械师] world_log_library未初始化，跳过日志资产注册");
                return;
            }

            // 必须设置path_icon为空字符串而非null，否则setMessage中null != ""会进入sprite加载分支
            var asset = new WorldLogAsset
            {
                id = assetId,
                locale_id = localeId,
                color = color,
                group = "super_mech",
                path_icon = "",
                random_ids = 0,
                text_replacer = null
            };
            AssetManager.world_log_library.add(asset);
            _logAssets[assetId] = asset;
        }

        /// <summary>安全获取单位名称，null时回退</summary>
        private static string SafeName(Actor a)
        {
            if (a == null) return "?";
            string name = a.getName();
            return string.IsNullOrEmpty(name) ? "?" : name;
        }

        /// <summary>安全字符串，null回退为空串</summary>
        private static string SafeStr(string s) => s ?? string.Empty;

        /// <summary>推送觉醒事件日志</summary>
        public static void LogAwakening(Actor a, string className, string talentRank)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_awaken");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a), SafeStr(className), SafeStr(talentRank))
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送觉醒日志失败: {e.Message}");
            }
        }

        /// <summary>推送阶位提升事件日志</summary>
        public static void LogPromotion(Actor a, string oldRank, string newRank, float onar)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_promotion");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a), SafeStr(newRank), onar.ToString("0"))
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送晋升日志失败: {e.Message}");
            }
        }

        /// <summary>推送知识融合事件日志</summary>
        public static void LogFusion(Actor a, string knowledgeA, string knowledgeB, string result)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_fusion");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a), SafeStr(knowledgeA), SafeStr(result))
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送融合日志失败: {e.Message}");
            }
        }

        /// <summary>推送圣所访问事件日志</summary>
        public static void LogSanctuaryVisit(Actor a, int sanctuaryIndex, string sanctuaryName)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_sanctuary");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a), SafeStr(sanctuaryName), "")
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送圣所日志失败: {e.Message}");
            }
        }

        /// <summary>推送宇宙迭代事件日志</summary>
        public static void LogIteration(int iterationCount, float heritageRate)
        {
            TryInit();
            if (!_initialized) return;
            var asset = GetAsset("sm_log_iteration");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, iterationCount.ToString(), heritageRate.ToString("0%"), "")
                {
                    // 宇宙迭代是全局事件，不绑定特定位置
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送迭代日志失败: {e.Message}");
            }
        }

        /// <summary>推送超A复活事件日志</summary>
        public static void LogResurrection(Actor a, int reviveCount, string lostAbilities)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_resurrection");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a), reviveCount.ToString(), SafeStr(lostAbilities))
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送复活日志失败: {e.Message}");
            }
        }

        /// <summary>推送超神级概念重塑事件日志（原著：超神级普通死亡后信息态重塑）</summary>
        public static void LogConceptReshape(Actor a)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_concept_reshape");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(a))
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送概念重塑日志失败: {e.Message}");
            }
        }

        /// <summary>推送法术学习成功事件日志</summary>
        public static void LogSpellLearned(Actor a, string spellNameKey)
        {
            TryInit();
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_spell");
            if (asset == null) return;

            try
            {
                string spellName = LocalizedTextManager.getText(spellNameKey);
                new WorldLogMessage(asset, SafeName(a), SafeStr(spellName), "")
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送法术日志失败: {e.Message}");
            }
        }

        private static WorldLogAsset GetAsset(string assetId)
        {
            if (_logAssets.TryGetValue(assetId, out var asset)) return asset;
            return AssetManager.world_log_library?.get(assetId);
        }
    }
}
