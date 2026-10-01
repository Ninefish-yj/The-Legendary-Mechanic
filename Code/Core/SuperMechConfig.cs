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

        private static void RegisterLocalization()
        {
            var categories = new (string key, string name)[]
            {
                ("General", "sm_config_549"),
                ("Awakening", "sm_config_550"),
                ("Qi", "sm_config_551"),
                ("Promotion", "sm_config_552"),
                ("AutoFavorite", "sm_config_553"),
                ("Sanctuary", "sm_config_554"),
                ("Mech", "sm_config_555"),
                ("Refinement", "sm_config_556"),
                ("Relic", "sm_config_557"),
                ("Performance", "sm_config_558"),
            };
            foreach (var (key, name) in categories)
                LocalizedTextManager.add(key, LocalizedTextManager.getText(name), pReplace: true);

            var items = new (string id, string name, string desc)[]
            {
                ("mod_enabled", "sm_config_559", "sm_config_560"),
                ("auto_awakening", "sm_config_561", "sm_config_562"),
                ("awakening_chance", "sm_config_563", "sm_config_564"),
                ("awakening_min_age", "sm_config_565", "sm_config_566"),
                ("qi_growth_rate", "sm_config_567", "sm_config_568"),
                ("qi_unlimited", "sm_config_569", "sm_config_570"),
                ("auto_promotion", "sm_config_571", "sm_config_572"),
                ("auto_promotion_max_rank", "sm_config_573", "sm_config_574"),
                ("promotion_speed", "sm_config_575", "sm_config_576"),
                ("ona_multiplier", "sm_config_577", "sm_config_578"),
                ("show_rank_in_panel", "sm_config_579", "sm_config_580"),
                ("auto_favorite_enabled", "sm_config_581", "sm_config_582"),
                ("auto_favorite_f", "sm_config_583", "sm_config_584"),
                ("auto_favorite_e", "sm_config_585", "sm_config_586"),
                ("auto_favorite_d", "sm_config_587", "sm_config_588"),
                ("auto_favorite_c", "sm_config_589", "sm_config_590"),
                ("auto_favorite_b", "sm_config_591", "sm_config_592"),
                ("auto_favorite_a", "sm_config_593", "sm_config_594"),
                ("auto_favorite_s", "sm_config_595", "sm_config_596"),
                ("auto_favorite_ss", "sm_config_597", "sm_config_598"),
                ("auto_favorite_x", "sm_config_599", "sm_config_600"),
                ("sanctuary_enabled", "sm_config_601", "sm_config_602"),
                ("sanctuary_autosave", "sm_config_603", "sm_config_604"),
                ("mech_summon_enabled", "sm_config_605", "sm_config_606"),
                ("max_summoned_units", "sm_config_607", "sm_config_608"),
                ("refinement_bonus", "sm_config_609", "sm_config_610"),
                ("relic_drop_enabled", "sm_config_611", "sm_config_612"),
                ("relic_drop_rate", "sm_config_613", "sm_config_614"),
                ("tick_interval", "sm_config_615", "sm_config_616"),
                ("max_tracked_actors", "sm_config_617", "sm_config_618"),
                ("log_verbose", "sm_config_619", "sm_config_620"),
            };
            foreach (var (id, name, desc) in items)
            {
                LocalizedTextManager.add(id, LocalizedTextManager.getText(name), pReplace: true);
                LocalizedTextManager.add(id + " Description", LocalizedTextManager.getText(desc), pReplace: true);
            }
        }


        public static void SetModEnabled(bool val) { ModEnabled = val; }

        public static void SetAutoAwakening(bool val) { AutoAwakening = val; }
        public static void SetAwakeningChance(float val) { AwakeningChance = Mathf.Clamp01(val); }
        public static void SetAwakeningMinAge(int val) { AwakeningMinAge = Mathf.Max(0, val); }

        public static void SetQiGrowthRate(float val) { QiGrowthRate = Mathf.Max(0.1f, val); }
        public static void SetQiUnlimited(bool val) { QiUnlimited = val; }

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
        public static void SetAutoFavoriteRank(int val) { }

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
    }
}
