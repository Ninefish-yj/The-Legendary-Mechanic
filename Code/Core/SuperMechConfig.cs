using System;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechConfig
    {
        public static bool ModEnabled = true;

        public static bool AutoAwakening = true;
        public static float AwakeningChance = 0.05f;
        public static int AwakeningMinAge = 16;

        public static float QiGrowthRate = 1.0f;
        public static bool QiUnlimited = true;
        public static int QiDisplayDecimals = 0;
        /// <summary>气力属性克制环开关（游戏化扩展，原著无此设定，默认关闭）</summary>
        public static bool QiAttributeCounterEnabled = false;

        public static float PromotionSpeed = 1.0f;
        public static float OnaMultiplier = 1.0f;
        public static bool AutoPromotion = true;
        public static int AutoPromotionMaxRank = 12;
        public static bool ShowRankInPanel = true;

        public static bool AutoFavoriteEnabled = true;
        /// <summary>自动收藏最低阶位（0=F,1=E,3=D,5=C,7=B,9=A,11=S,12=SS,13=X），默认9=A阶</summary>
        public static int AutoFavoriteMinRank = 9;

        public static bool SanctuaryEnabled = true;
        public static bool SanctuaryAutoSave = true;
        /// <summary>单位自动访问圣所（v0.31.0）：A级及以上超能者自动访问匹配体系的圣所</summary>
        public static bool AutoVisitSanctuary = true;
        /// <summary>单位自动进入圣所（v0.31.0）：S级及以上有足够钥匙时自动进入圣所修炼</summary>
        public static bool AutoEnterSanctuary = true;

        public static bool MechSummonEnabled = true;
        public static int MaxSummonedUnits = 50;

        public static float TickInterval = 1.25f;
        public static int MaxTrackedActors = 500;
        public static bool LogVerbose = false;

        public static bool RelicDropEnabled = true;
        public static float RelicDropRate = 0.01f;

        public static float RefinementBonus = 2.0f;

        public static bool CrossModEnergySync = true;
        public static float CrossModEnergyRatio = 1.0f;

        // === 战斗压制配置（精简版：只暴露总控，内部参数锁死原著默认值）===
        /// <summary>能级压制总开关</summary>
        public static bool CombatSuppressionEnabled = true;
        /// <summary>压制强度倍率（0.5~2.0），乘在每档伤害/命中上</summary>
        public static float SuppressIntensity = 1.0f;
        /// <summary>精神穿甲总开关</summary>
        public static bool SpiritPierceEnabled = true;
        /// <summary>精神攻击穿甲比例（念力/异能对机械系）</summary>
        public static float SpiritPierceRatio = 0.30f;

        // === 内部参数（不暴露配置，锁死原著默认值）===
        public static float SuppressThresholdMinor = 1.1f;
        public static float SuppressThresholdModerate = 1.5f;
        public static float SuppressThresholdStrong = 2.0f;
        public static float SuppressThresholdOverwhelm = 5.0f;
        public static float SuppressThresholdAnnihilate = 10.0f;
        public static float SuppressDmgMinor = 0.10f;
        public static float SuppressDmgModerate = 0.25f;
        public static float SuppressDmgStrong = 0.50f;
        public static float SuppressDmgOverwhelm = 1.00f;
        public static float SuppressDmgAnnihilate = 2.00f;
        public static float SuppressHitModerate = 0.10f;
        public static float SuppressHitStrong = 0.20f;
        public static float SuppressHitOverwhelm = 0.40f;
        public static float SuppressHitAnnihilate = 0.60f;
        public static float SuppressCritModerate = 0.08f;
        public static float SuppressCritStrong = 0.15f;
        public static float SuppressCritOverwhelm = 0.25f;
        public static float SuppressDefModerate = 0.10f;
        public static float SuppressDefStrong = 0.20f;
        public static float SuppressDefOverwhelm = 0.35f;
        public static float SuppressDefAnnihilate = 0.50f;
        public static float SuppressDodgeModerate = 0.05f;
        public static float SuppressDodgeStrong = 0.12f;
        public static float SuppressDodgeOverwhelm = 0.25f;
        public static float SuppressDodgeAnnihilate = 0.40f;
        /// <summary>知识融合属性加成上限（单配方和总上限）</summary>
        public static float FusionSingleMulCap = 2.0f;
        public static float FusionTotalMulCap = 3.0f;

        public static void Init()
        {
            try
            {
                RegisterLocalization();
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 配置初始化异常: " + e.Message);
            }
        }

        private struct CatInfo { public string key, name; public CatInfo(string a, string b) { key=a; name=b; } }
        private struct ItemInfo { public string id, name, desc; public ItemInfo(string a, string b, string c) { id=a; name=b; desc=c; } }
        private static void RegisterLocalization()
        {
            var categories = new CatInfo[]
            {
                new CatInfo("General", "sm_config_549"),
                new CatInfo("Awakening", "sm_config_550"),
                new CatInfo("Qi", "sm_config_551"),
                new CatInfo("Promotion", "sm_config_552"),
                new CatInfo("AutoFavorite", "sm_config_553"),
                new CatInfo("Sanctuary", "sm_config_554"),
                new CatInfo("Mech", "sm_config_555"),
                new CatInfo("Refinement", "sm_config_556"),
                new CatInfo("Relic", "sm_config_557"),
                new CatInfo("Performance", "sm_config_558"),
                new CatInfo("Combat", "sm_config_621"),
                new CatInfo("EventLog", "sm_config_710"),
            };
            foreach (var c in categories)
                LocalizedTextManager.add(c.key, LocalizedTextManager.getText(c.name), pReplace: true);

            var items = new ItemInfo[]
            {
                new ItemInfo("mod_enabled", "sm_config_559", "sm_config_560"),
                new ItemInfo("auto_awakening", "sm_config_561", "sm_config_562"),
                new ItemInfo("awakening_chance", "sm_config_563", "sm_config_564"),
                new ItemInfo("awakening_min_age", "sm_config_565", "sm_config_566"),
                new ItemInfo("qi_growth_rate", "sm_config_567", "sm_config_568"),
                new ItemInfo("qi_unlimited", "sm_config_569", "sm_config_570"),
                new ItemInfo("auto_promotion", "sm_config_571", "sm_config_572"),
                new ItemInfo("auto_promotion_max_rank", "sm_config_573", "sm_config_574"),
                new ItemInfo("promotion_speed", "sm_config_575", "sm_config_576"),
                new ItemInfo("ona_multiplier", "sm_config_577", "sm_config_578"),
                new ItemInfo("show_rank_in_panel", "sm_config_579", "sm_config_580"),
                new ItemInfo("auto_favorite_enabled", "sm_config_581", "sm_config_582"),
                new ItemInfo("auto_favorite_min_rank", "sm_config_583", "sm_config_584"),
                new ItemInfo("sanctuary_enabled", "sm_config_601", "sm_config_602"),
                new ItemInfo("sanctuary_autosave", "sm_config_603", "sm_config_604"),
                new ItemInfo("auto_visit_sanctuary", "sm_config_653", "sm_config_654"),
                new ItemInfo("auto_enter_sanctuary", "sm_config_655", "sm_config_656"),
                new ItemInfo("mech_summon_enabled", "sm_config_605", "sm_config_606"),
                new ItemInfo("max_summoned_units", "sm_config_607", "sm_config_608"),
                new ItemInfo("refinement_bonus", "sm_config_609", "sm_config_610"),
                new ItemInfo("relic_drop_enabled", "sm_config_611", "sm_config_612"),
                new ItemInfo("relic_drop_rate", "sm_config_613", "sm_config_614"),
                new ItemInfo("tick_interval", "sm_config_615", "sm_config_616"),
                new ItemInfo("max_tracked_actors", "sm_config_617", "sm_config_618"),
                new ItemInfo("log_verbose", "sm_config_619", "sm_config_620"),
                new ItemInfo("combat_suppression_enabled", "sm_config_622", "sm_config_623"),
                new ItemInfo("combat_suppression_intensity", "sm_config_700", "sm_config_701"),
                new ItemInfo("spirit_pierce_enabled", "sm_config_702", "sm_config_703"),
                new ItemInfo("spirit_pierce_ratio", "sm_config_624", "sm_config_625"),
                new ItemInfo("fusion_single_mul_cap", "sm_config_626", "sm_config_627"),
                new ItemInfo("fusion_total_mul_cap", "sm_config_628", "sm_config_629"),
                new ItemInfo("qi_attribute_counter", "sm_config_657", "sm_config_658"),
                new ItemInfo("qi_display_decimals", "sm_config_659", "sm_config_660"),
                new ItemInfo("cross_mod_energy_sync", "sm_config_661", "sm_config_662"),
                new ItemInfo("cross_mod_energy_ratio", "sm_config_663", "sm_config_664"),
                new ItemInfo("event_log_enabled", "sm_config_711", "sm_config_712"),
                new ItemInfo("event_log_awaken_min_talent", "sm_config_713", "sm_config_714"),
                new ItemInfo("event_log_promotion_min_rank", "sm_config_715", "sm_config_716"),
                new ItemInfo("event_log_fusion_enabled", "sm_config_717", "sm_config_718"),
                new ItemInfo("event_log_spell_enabled", "sm_config_719", "sm_config_720"),
                new ItemInfo("event_log_sanctuary_enabled", "sm_config_721", "sm_config_722"),
                new ItemInfo("event_log_iteration_enabled", "sm_config_723", "sm_config_724"),
                new ItemInfo("event_log_resurrection_enabled", "sm_config_725", "sm_config_726"),
                new ItemInfo("event_log_concept_reshape_enabled", "sm_config_727", "sm_config_728"),
            };
            foreach (var it in items)
            {
                LocalizedTextManager.add(it.id, LocalizedTextManager.getText(it.name), pReplace: true);
                LocalizedTextManager.add(it.id + " Description", LocalizedTextManager.getText(it.desc), pReplace: true);
            }
        }


        public static void SetModEnabled(bool val) { ModEnabled = val; }

        public static void SetAutoAwakening(bool val) { AutoAwakening = val; }
        public static void SetAwakeningChance(float val) { AwakeningChance = Mathf.Clamp01(val); }
        public static void SetAwakeningMinAge(int val) { AwakeningMinAge = Mathf.Max(0, val); }

        public static void SetQiGrowthRate(float val) { QiGrowthRate = Mathf.Max(0.1f, val); }
        public static void SetQiUnlimited(bool val) { QiUnlimited = val; }
        public static void SetQiAttributeCounterEnabled(bool val) { QiAttributeCounterEnabled = val; }

        public static void SetPromotionSpeed(float val) { PromotionSpeed = Mathf.Max(0.1f, val); }
        public static void SetOnaMultiplier(float val) { OnaMultiplier = Mathf.Max(0.1f, val); }
        public static void SetAutoPromotion(bool val) { AutoPromotion = val; }
        public static void SetAutoPromotionMaxRank(int val) { AutoPromotionMaxRank = Mathf.Clamp(val, 0, 12); }
        public static void SetShowRankInPanel(bool val) { ShowRankInPanel = val; }

        public static void SetAutoFavoriteEnabled(bool val) { AutoFavoriteEnabled = val; }
        public static void SetAutoFavoriteMinRank(int val) { AutoFavoriteMinRank = Mathf.Clamp(val, 0, 14); }

        public static bool ShouldFavoriteRank(int rankIdx)
        {
            return rankIdx >= AutoFavoriteMinRank;
        }

        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; }
        public static void SetSanctuaryAutoSave(bool val) { SanctuaryAutoSave = val; }
        public static void SetAutoVisitSanctuary(bool val) { AutoVisitSanctuary = val; }
        public static void SetAutoEnterSanctuary(bool val) { AutoEnterSanctuary = val; }
        public static void SetSanctuaryCount(int val) { }

        public static void SetMechSummonEnabled(bool val) { MechSummonEnabled = val; }
        public static void SetMaxSummonedUnits(int val) { MaxSummonedUnits = Mathf.Clamp(val, 0, 500); }

        public static void SetTickInterval(float val) { TickInterval = Mathf.Clamp(val, 1f, 60f); }
        public static void SetMaxTrackedActors(int val) { MaxTrackedActors = Mathf.Clamp(val, 50, 5000); }
        public static void SetLogVerbose(bool val) { LogVerbose = val; }

        public static void SetRelicDropEnabled(bool val) { RelicDropEnabled = val; }
        public static void SetRelicDropRate(float val) { RelicDropRate = Mathf.Clamp01(val); }

        public static void SetRefinementBonus(float val) { RefinementBonus = Mathf.Max(0f, val); }

        public static void SetCombatSuppressionEnabled(bool val) { CombatSuppressionEnabled = val; }
        public static void SetSuppressIntensity(float val) { SuppressIntensity = Mathf.Clamp(val, 0.1f, 3.0f); }
        public static void SetSpiritPierceEnabled(bool val) { SpiritPierceEnabled = val; }
        public static void SetSpiritPierceRatio(float val) { SpiritPierceRatio = Mathf.Clamp01(val); }
        public static void SetFusionSingleMulCap(float val) { FusionSingleMulCap = Mathf.Max(1.0f, val); }
        public static void SetFusionTotalMulCap(float val) { FusionTotalMulCap = Mathf.Max(1.0f, val); }

        public static void SetQiDisplayDecimals(int val) { QiDisplayDecimals = Mathf.Clamp(val, 0, 3); }
        public static void SetCrossModEnergySync(bool val) { CrossModEnergySync = val; }
        public static void SetCrossModEnergyRatio(float val) { CrossModEnergyRatio = Mathf.Clamp(val, 0.1f, 10f); }

        // === 事件日志配置（v0.39.6：阈值过滤，避免后期刷屏）===
        /// <summary>事件日志总开关</summary>
        public static bool EventLogEnabled = true;
        /// <summary>觉醒日志最低天赋评级（0=F,1=E,2=D,3=C,4=B,5=A,6=S），默认3=C级以上才记录</summary>
        public static int EventLogAwakenMinTalent = 3;
        /// <summary>晋升日志最低阶位（0=F~14=X），默认7=B阶以上才记录</summary>
        public static int EventLogPromotionMinRank = 7;
        /// <summary>知识融合日志开关（默认关，基础融合太频繁）</summary>
        public static bool EventLogFusionEnabled = false;
        /// <summary>法术习得日志开关（默认关）</summary>
        public static bool EventLogSpellEnabled = false;
        /// <summary>圣所访问日志开关（默认开，稀有事件）</summary>
        public static bool EventLogSanctuaryEnabled = true;
        /// <summary>宇宙迭代日志开关（默认开，全局大事件）</summary>
        public static bool EventLogIterationEnabled = true;
        /// <summary>超A复活日志开关（默认开，稀有事件）</summary>
        public static bool EventLogResurrectionEnabled = true;
        /// <summary>概念重塑日志开关（默认开，极稀有）</summary>
        public static bool EventLogConceptReshapeEnabled = true;

        public static void SetEventLogEnabled(bool val) { EventLogEnabled = val; }
        public static void SetEventLogAwakenMinTalent(int val) { EventLogAwakenMinTalent = Mathf.Clamp(val, 0, 6); }
        public static void SetEventLogPromotionMinRank(int val) { EventLogPromotionMinRank = Mathf.Clamp(val, 0, 14); }
        public static void SetEventLogFusionEnabled(bool val) { EventLogFusionEnabled = val; }
        public static void SetEventLogSpellEnabled(bool val) { EventLogSpellEnabled = val; }
        public static void SetEventLogSanctuaryEnabled(bool val) { EventLogSanctuaryEnabled = val; }
        public static void SetEventLogIterationEnabled(bool val) { EventLogIterationEnabled = val; }
        public static void SetEventLogResurrectionEnabled(bool val) { EventLogResurrectionEnabled = val; }
        public static void SetEventLogConceptReshapeEnabled(bool val) { EventLogConceptReshapeEnabled = val; }
    }
}
