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
        private static readonly Dictionary<string, WorldLogAsset> _logAssets = new Dictionary<string, WorldLogAsset>();

        /// <summary>初始化：注册自定义WorldLogAsset到原生日志库</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            RegisterLogAsset("sm_log_awaken", "sm_log_awaken_text", new Color(0.3f, 0.8f, 1f));
            RegisterLogAsset("sm_log_promotion", "sm_log_promotion_text", new Color(1f, 0.8f, 0.2f));
            RegisterLogAsset("sm_log_fusion", "sm_log_fusion_text", new Color(0.5f, 1f, 0.5f));
            RegisterLogAsset("sm_log_sanctuary", "sm_log_sanctuary_text", new Color(0.8f, 0.5f, 1f));
            RegisterLogAsset("sm_log_iteration", "sm_log_iteration_text", new Color(1f, 0.5f, 0.5f));
            RegisterLogAsset("sm_log_resurrection", "sm_log_resurrection_text", new Color(1f, 0.3f, 0.8f));

            Debug.Log("[超神机械师] 事件日志系统初始化完成，注册6类日志资产");
        }

        /// <summary>注册自定义WorldLogAsset</summary>
        private static void RegisterLogAsset(string assetId, string localeId, Color color)
        {
            if (AssetManager.world_log_library == null)
            {
                Debug.LogWarning("[超神机械师] world_log_library未初始化，跳过日志资产注册");
                return;
            }

            var asset = new WorldLogAsset
            {
                id = assetId,
                locale_id = localeId,
                color = color
            };
            AssetManager.world_log_library.add(asset);
            _logAssets[assetId] = asset;
        }

        /// <summary>推送觉醒事件日志</summary>
        public static void LogAwakening(Actor a, string className, string talentRank)
        {
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_awaken");
            if (asset == null) return;

            new WorldLogMessage(asset, a.getName(), className, talentRank)
            {
                unit = a,
                location = a.current_position
            }.add();
        }

        /// <summary>推送阶位提升事件日志</summary>
        public static void LogPromotion(Actor a, string oldRank, string newRank, float onar)
        {
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_promotion");
            if (asset == null) return;

            new WorldLogMessage(asset, a.getName(), newRank, onar.ToString("0"))
            {
                unit = a,
                location = a.current_position
            }.add();
        }

        /// <summary>推送知识融合事件日志</summary>
        public static void LogFusion(Actor a, string knowledgeA, string knowledgeB, string result)
        {
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_fusion");
            if (asset == null) return;

            new WorldLogMessage(asset, a.getName(), knowledgeA, result)
            {
                unit = a,
                location = a.current_position
            }.add();
        }

        /// <summary>推送圣所访问事件日志</summary>
        public static void LogSanctuaryVisit(Actor a, int sanctuaryIndex, string sanctuaryName)
        {
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_sanctuary");
            if (asset == null) return;

            new WorldLogMessage(asset, a.getName(), sanctuaryName, "")
            {
                unit = a,
                location = a.current_position
            }.add();
        }

        /// <summary>推送宇宙迭代事件日志</summary>
        public static void LogIteration(int iterationCount, float heritageRate)
        {
            if (!_initialized) return;
            var asset = GetAsset("sm_log_iteration");
            if (asset == null) return;

            new WorldLogMessage(asset, iterationCount.ToString(), heritageRate.ToString("0%"), "")
            {
                // 宇宙迭代是全局事件，不绑定特定位置
            }.add();
        }

        /// <summary>推送超A复活事件日志</summary>
        public static void LogResurrection(Actor a, int reviveCount, string lostAbilities)
        {
            if (!_initialized || a == null) return;
            var asset = GetAsset("sm_log_resurrection");
            if (asset == null) return;

            new WorldLogMessage(asset, a.getName(), reviveCount.ToString(), lostAbilities)
            {
                unit = a,
                location = a.current_position
            }.add();
        }

        private static WorldLogAsset GetAsset(string assetId)
        {
            if (_logAssets.TryGetValue(assetId, out var asset)) return asset;
            return AssetManager.world_log_library?.get(assetId);
        }
    }
}
