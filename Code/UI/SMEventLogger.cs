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

            // 注册历史分类（让模组事件在历史窗口有独立分类按钮）
            RegisterHistoryGroup();

            RegisterLogAsset("sm_log_awaken", "sm_log_awaken_text", new Color(0.3f, 0.8f, 1f));
            RegisterLogAsset("sm_log_promotion", "sm_log_promotion_text", new Color(1f, 0.8f, 0.2f));
            RegisterLogAsset("sm_log_fusion", "sm_log_fusion_text", new Color(0.5f, 1f, 0.5f));
            RegisterLogAsset("sm_log_sanctuary", "sm_log_sanctuary_text", new Color(0.8f, 0.5f, 1f));
            RegisterLogAsset("sm_log_iteration", "sm_log_iteration_text", new Color(1f, 0.5f, 0.5f));
            RegisterLogAsset("sm_log_resurrection", "sm_log_resurrection_text", new Color(1f, 0.3f, 0.8f));
            RegisterLogAsset("sm_log_spell", "sm_log_spell_text", new Color(0.6f, 0.4f, 1f));
            RegisterLogAsset("sm_log_skill", "sm_log_skill_text", new Color(0.4f, 0.8f, 0.6f));
            RegisterLogAsset("sm_log_concept_reshape", "sm_log_concept_reshape_text", new Color(0.2f, 1f, 0.8f));
            RegisterLogAsset("sm_log_competition", "sm_log_competition_text", new Color(1f, 0.6f, 0.3f));
            RegisterLogAsset("sm_log_key", "sm_log_key_text", new Color(0.4f, 0.7f, 1f));

            Debug.Log("[超神机械师] 事件日志系统初始化完成，注册10类日志资产+1历史分类");
        }

        /// <summary>注册历史分类Asset</summary>
        private static void RegisterHistoryGroup()
        {
            if (AssetManager.history_groups == null) return;
            if (AssetManager.history_groups.get("super_mech") != null) return;

            var group = new HistoryGroupAsset
            {
                id = "super_mech",
                icon_path = "ui/Icons/iconPurpleBook"
            };
            AssetManager.history_groups.add(group);
            LocalizedTextManager.add("history_group_super_mech", "超能者", pReplace: true);
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
            // 必须设置text_replacer，否则{0}{1}{2}占位符不会被替换
            var asset = new WorldLogAsset
            {
                id = assetId,
                locale_id = localeId,
                color = color,
                group = "super_mech",
                path_icon = "",
                random_ids = 0,
                text_replacer = delegate(WorldLogMessage msg, ref string text)
                {
                    text = text.Replace("{0}", msg.getSpecial(1))
                               .Replace("{1}", msg.getSpecial(2))
                               .Replace("{2}", msg.getSpecial(3));
                }
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

        /// <summary>天赋评级字符串转等级（F=0~S=6）</summary>
        private static int TalentRankToIndex(string rank)
        {
            switch (rank?.Trim().ToUpper())
            {
                case "F": return 0;
                case "E": return 1;
                case "D": return 2;
                case "C": return 3;
                case "B": return 4;
                case "A": return 5;
                case "S": return 6;
                default: return 0;
            }
        }

        /// <summary>推送觉醒事件日志（阈值过滤：天赋评级>=配置值才记录）</summary>
        public static void LogAwakening(Actor a, string className, string talentRank)
        {
            if (!SuperMechConfig.EventLogEnabled) return;
            if (TalentRankToIndex(talentRank) < SuperMechConfig.EventLogAwakenMinTalent) return;
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

        /// <summary>推送阶位提升事件日志（阈值过滤：新阶位>=配置值才记录）</summary>
        public static void LogPromotion(Actor a, string oldRank, string newRank, float onar, int newRankIndex = -1)
        {
            if (!SuperMechConfig.EventLogEnabled) return;
            if (newRankIndex >= 0 && newRankIndex < SuperMechConfig.EventLogPromotionMinRank) return;
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

        /// <summary>推送知识融合事件日志（默认关闭，避免刷屏）</summary>
        public static void LogFusion(Actor a, string knowledgeA, string knowledgeB, string result)
        {
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogFusionEnabled) return;
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
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogSanctuaryEnabled) return;
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
        public static void LogIteration(int iterationCount, float heritageRate, string civilizationSummary = "")
        {
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogIterationEnabled) return;
            TryInit();
            if (!_initialized) return;
            var asset = GetAsset("sm_log_iteration");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, iterationCount.ToString(), heritageRate.ToString("0%"), civilizationSummary)
                {
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
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogResurrectionEnabled) return;
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
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogConceptReshapeEnabled) return;
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

        /// <summary>推送法术学习成功事件日志（默认关闭，避免刷屏）</summary>
        public static void LogSpellLearned(Actor a, string spellNameKey)
        {
            if (!SuperMechConfig.EventLogEnabled || !SuperMechConfig.EventLogSpellEnabled) return;
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

        /// <summary>推送技能释放事件日志（默认关闭，避免刷屏）</summary>
        public static void LogSkillCast(Actor a, SuperMechSkills.SkillDef def)
        {
            if (!SuperMechConfig.EventLogEnabled) return;
            TryInit();
            if (!_initialized || a == null || def == null) return;
            var asset = GetAsset("sm_log_skill");
            if (asset == null) return;

            try
            {
                string skillName = LocalizedTextManager.getText(def.name);
                new WorldLogMessage(asset, SafeName(a), SafeStr(skillName), "")
                {
                    unit = a,
                    location = a.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送技能日志失败: {e.Message}");
            }
        }

        /// <summary>推送超能者竞争事件日志（自动竞争，v0.45.0）</summary>
        public static void LogCompetition(Actor winner, Actor loser, string result)
        {
            if (!SuperMechConfig.EventLogEnabled) return;
            TryInit();
            if (!_initialized || winner == null) return;
            var asset = GetAsset("sm_log_competition");
            if (asset == null) return;

            try
            {
                new WorldLogMessage(asset, SafeName(winner), SafeName(loser), SafeStr(result))
                {
                    unit = winner,
                    location = winner.current_position
                }.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送竞争日志失败: {e.Message}");
            }
        }

        /// <summary>v0.46.0：获得圣所钥匙事件日志（材料合成时actor可为null）</summary>
        public static void LogKeyAcquired(Actor a, int amount, string source)
        {
            if (!SuperMechConfig.EventLogEnabled) return;
            TryInit();
            if (!_initialized || amount <= 0) return;
            var asset = GetAsset("sm_log_key");
            if (asset == null) return;

            try
            {
                var msg = new WorldLogMessage(asset, SafeName(a), amount.ToString(), SafeStr(source));
                if (a != null)
                {
                    msg.unit = a;
                    msg.location = a.current_position;
                }
                msg.add();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 推送钥匙日志失败: {e.Message}");
            }
        }

        private static WorldLogAsset GetAsset(string assetId)
        {
            if (_logAssets.TryGetValue(assetId, out var asset)) return asset;
            return AssetManager.world_log_library?.get(assetId);
        }
    }
}
