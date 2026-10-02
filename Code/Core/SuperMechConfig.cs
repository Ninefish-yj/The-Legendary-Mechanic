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
        public static bool AutoFavoriteF = false;
        public static bool AutoFavoriteE = false;
        public static bool AutoFavoriteD = false;
        public static bool AutoFavoriteC = false;
        public static bool AutoFavoriteB = false;
        public static bool AutoFavoriteA = true;
        public static bool AutoFavoriteS = true;
        public static bool AutoFavoriteSS = true;
        public static bool AutoFavoriteX = true;

        public static bool SanctuaryEnabled = true;
        public static bool SanctuaryAutoSave = true;

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

        // === 战斗压制配置（v0.26.0配置化补齐）===
        /// <summary>能级压制开关</summary>
        public static bool CombatSuppressionEnabled = true;
        /// <summary>能级压制阈值：轻微/明显/强烈/碾压/秒杀级</summary>
        public static float SuppressThresholdMinor = 1.1f;
        public static float SuppressThresholdModerate = 1.5f;
        public static float SuppressThresholdStrong = 2.0f;
        public static float SuppressThresholdOverwhelm = 5.0f;
        public static float SuppressThresholdAnnihilate = 10.0f;
        /// <summary>伤害加成倍率（对应5档）</summary>
        public static float SuppressDmgMinor = 0.10f;
        public static float SuppressDmgModerate = 0.25f;
        public static float SuppressDmgStrong = 0.50f;
        public static float SuppressDmgOverwhelm = 1.00f;
        public static float SuppressDmgAnnihilate = 2.00f;
        /// <summary>强制命中概率（对应4档，轻微无强制命中）</summary>
        public static float SuppressHitModerate = 0.10f;
        public static float SuppressHitStrong = 0.20f;
        public static float SuppressHitOverwhelm = 0.40f;
        public static float SuppressHitAnnihilate = 0.60f;
        /// <summary>暴击率（对应3档）</summary>
        public static float SuppressCritModerate = 0.08f;
        public static float SuppressCritStrong = 0.15f;
        public static float SuppressCritOverwhelm = 0.25f;
        /// <summary>抗性减免（对应4档，防守方高能级时）</summary>
        public static float SuppressDefModerate = 0.10f;
        public static float SuppressDefStrong = 0.20f;
        public static float SuppressDefOverwhelm = 0.35f;
        public static float SuppressDefAnnihilate = 0.50f;
        /// <summary>闪避率（对应4档，防守方高能级时）</summary>
        public static float SuppressDodgeModerate = 0.05f;
        public static float SuppressDodgeStrong = 0.12f;
        public static float SuppressDodgeOverwhelm = 0.25f;
        public static float SuppressDodgeAnnihilate = 0.40f;
        /// <summary>精神攻击穿甲比例（念力/异能对机械系）</summary>
        public static float SpiritPierceRatio = 0.30f;
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
                new ItemInfo("auto_favorite_f", "sm_config_583", "sm_config_584"),
                new ItemInfo("auto_favorite_e", "sm_config_585", "sm_config_586"),
                new ItemInfo("auto_favorite_d", "sm_config_587", "sm_config_588"),
                new ItemInfo("auto_favorite_c", "sm_config_589", "sm_config_590"),
                new ItemInfo("auto_favorite_b", "sm_config_591", "sm_config_592"),
                new ItemInfo("auto_favorite_a", "sm_config_593", "sm_config_594"),
                new ItemInfo("auto_favorite_s", "sm_config_595", "sm_config_596"),
                new ItemInfo("auto_favorite_ss", "sm_config_597", "sm_config_598"),
                new ItemInfo("auto_favorite_x", "sm_config_599", "sm_config_600"),
                new ItemInfo("sanctuary_enabled", "sm_config_601", "sm_config_602"),
                new ItemInfo("sanctuary_autosave", "sm_config_603", "sm_config_604"),
                new ItemInfo("mech_summon_enabled", "sm_config_605", "sm_config_606"),
                new ItemInfo("max_summoned_units", "sm_config_607", "sm_config_608"),
                new ItemInfo("refinement_bonus", "sm_config_609", "sm_config_610"),
                new ItemInfo("relic_drop_enabled", "sm_config_611", "sm_config_612"),
                new ItemInfo("relic_drop_rate", "sm_config_613", "sm_config_614"),
                new ItemInfo("tick_interval", "sm_config_615", "sm_config_616"),
                new ItemInfo("max_tracked_actors", "sm_config_617", "sm_config_618"),
                new ItemInfo("log_verbose", "sm_config_619", "sm_config_620"),
                new ItemInfo("combat_suppression_enabled", "sm_config_622", "sm_config_623"),
                new ItemInfo("spirit_pierce_ratio", "sm_config_624", "sm_config_625"),
                new ItemInfo("fusion_single_mul_cap", "sm_config_626", "sm_config_627"),
                new ItemInfo("fusion_total_mul_cap", "sm_config_628", "sm_config_629"),
                new ItemInfo("qi_attribute_counter", "sm_config_630", "sm_config_631"),
                new ItemInfo("qi_display_decimals", "sm_config_632", "sm_config_633"),
                new ItemInfo("cross_mod_energy_sync", "sm_config_634", "sm_config_635"),
                new ItemInfo("cross_mod_energy_ratio", "sm_config_636", "sm_config_637"),
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
        public static void SetAutoFavoriteF(bool val) { AutoFavoriteF = val; }
        public static void SetAutoFavoriteE(bool val) { AutoFavoriteE = val; }
        public static void SetAutoFavoriteD(bool val) { AutoFavoriteD = val; }
        public static void SetAutoFavoriteC(bool val) { AutoFavoriteC = val; }
        public static void SetAutoFavoriteB(bool val) { AutoFavoriteB = val; }
        public static void SetAutoFavoriteA(bool val) { AutoFavoriteA = val; }
        public static void SetAutoFavoriteS(bool val) { AutoFavoriteS = val; }
        public static void SetAutoFavoriteSS(bool val) { AutoFavoriteSS = val; }
        public static void SetAutoFavoriteX(bool val) { AutoFavoriteX = val; }

        public static bool ShouldFavoriteRank(int rankIdx)
        {
            if (rankIdx <= 0) return AutoFavoriteF;
            if (rankIdx <= 1) return AutoFavoriteE;
            if (rankIdx <= 3) return AutoFavoriteD;
            if (rankIdx <= 5) return AutoFavoriteC;
            if (rankIdx <= 7) return AutoFavoriteB;
            if (rankIdx <= 9) return AutoFavoriteA;
            if (rankIdx <= 11) return AutoFavoriteS;
            if (rankIdx <= 12) return AutoFavoriteSS;
            return AutoFavoriteX;
        }

        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; }
        public static void SetSanctuaryAutoSave(bool val) { SanctuaryAutoSave = val; }
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
        public static void SetSpiritPierceRatio(float val) { SpiritPierceRatio = Mathf.Clamp01(val); }
        public static void SetFusionSingleMulCap(float val) { FusionSingleMulCap = Mathf.Max(1.0f, val); }
        public static void SetFusionTotalMulCap(float val) { FusionTotalMulCap = Mathf.Max(1.0f, val); }

        public static void SetQiDisplayDecimals(int val) { QiDisplayDecimals = Mathf.Clamp(val, 0, 3); }
        public static void SetCrossModEnergySync(bool val) { CrossModEnergySync = val; }
        public static void SetCrossModEnergyRatio(float val) { CrossModEnergyRatio = Mathf.Clamp(val, 0.1f, 10f); }
    }
}
