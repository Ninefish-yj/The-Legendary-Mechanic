using System;
using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超神机械师模组配置系统
    /// 对应 default_config.json，NML 通过反射调用 Callback 方法
    /// 所有配置项运行时可在模组管理器配置面板中修改
    /// </summary>
    public static class SuperMechConfig
    {
        // ========== 总开关 ==========
        public static bool ModEnabled = true;

        // ========== 觉醒系统 ==========
        public static bool AutoAwakening = true;
        public static float AwakeningChance = 0.05f;      // 每tick自然觉醒概率
        public static int AwakeningMinAge = 16;           // 最小觉醒年龄

        // ========== 气力系统 ==========
        public static float QiGrowthRate = 1.0f;          // 气力增长倍率
        public static bool QiUnlimited = true;            // 气力无上限（原著设定）
        public static int QiDisplayDecimals = 0;          // 气力显示小数位

        // ========== 晋升系统 ==========
        public static float PromotionSpeed = 1.0f;        // 晋升速度倍率
        public static float OnaMultiplier = 1.0f;         // 欧纳计算倍率
        public static bool AutoPromotion = true;          // 自动晋升
        public static int AutoPromotionMaxRank = 12;      // 自动晋升上限阶位索引（0=F~13=X，默认12=SS，X阶需通过超神突破系统晋升（满足三条件后自动尝试或手动触发））
        public static bool ShowRankInPanel = true;        // 单位面板显示阶位

        // ========== 自动收藏（每个主阶位独立开关，参考凡人修仙传）==========
        public static bool AutoFavoriteEnabled = true;    // 自动收藏总开关
        public static bool AutoFavoriteF = false;         // F阶自动收藏
        public static bool AutoFavoriteE = false;         // E阶自动收藏
        public static bool AutoFavoriteD = false;         // D阶自动收藏
        public static bool AutoFavoriteC = false;         // C阶自动收藏
        public static bool AutoFavoriteB = false;         // B阶自动收藏
        public static bool AutoFavoriteA = true;          // A阶自动收藏
        public static bool AutoFavoriteS = true;          // S阶（超A）自动收藏
        public static bool AutoFavoriteSS = true;         // SS阶自动收藏
        public static bool AutoFavoriteX = true;          // X阶（超神）自动收藏

        // ========== 圣所系统 ==========
        public static bool SanctuaryEnabled = true;       // 圣所跨存档
        public static bool SanctuaryAutoSave = true;      // 圣所自动保存

        // ========== 机械系 ==========
        public static bool MechSummonEnabled = true;      // 机械召唤（爆兵流占位）
        public static int MaxSummonedUnits = 50;          // 最大召唤单位数（性能保护）

        // ========== 性能优化 ==========
        public static float TickInterval = 1.25f;          // 主循环间隔（秒）
        public static int MaxTrackedActors = 500;         // 最大追踪单位数
        public static bool LogVerbose = false;            // 详细日志

        // ========== 宇宙宝物 ==========
        public static bool RelicDropEnabled = true;       // 宝物掉落
        public static float RelicDropRate = 0.01f;        // 宝物掉落率

        // ========== 提炼法 ==========
        public static float RefinementBonus = 2.0f;       // 提炼法气力加成

        /// <summary>
        /// 配置初始化：从 NML 配置系统读取当前值
        /// 在 OnModLoad 中调用
        /// </summary>
        public static void Init()
        {
            try
            {
                LogInfo("sm_config_547");
                RegisterLocalization();
                LogInfo($"sm_config_548");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 配置初始化异常: " + e.Message);
            }
        }

        /// <summary>注册配置项与分类的本地化文本（NML配置面板用Id查文本）。</summary>
        private static void RegisterLocalization()
        {
            // 分类名（default_config.json中用英文key，这里注册中文显示）
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
                LocalizedTextManager.add(key, name, pReplace: true);

            // 配置项名称 + 描述（key格式：Id 和 Id+" Description"）
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
                LocalizedTextManager.add(id, name, pReplace: true);
                LocalizedTextManager.add(id + " Description", desc, pReplace: true);
            }
        }

        // ========== Callback 方法（NML 反射调用，签名：静态方法，参数为配置值） ==========

        // --- 总开关 ---
        public static void SetModEnabled(bool val) { ModEnabled = val; LogInfo($"sm_config_621"); }

        // --- 觉醒系统 ---
        public static void SetAutoAwakening(bool val) { AutoAwakening = val; LogInfo($"sm_config_622"); }
        public static void SetAwakeningChance(float val) { AwakeningChance = Mathf.Clamp01(val); LogInfo($"sm_config_623"); }
        public static void SetAwakeningMinAge(int val) { AwakeningMinAge = Mathf.Max(0, val); LogInfo($"sm_config_624"); }

        // --- 气力系统 ---
        public static void SetQiGrowthRate(float val) { QiGrowthRate = Mathf.Max(0.1f, val); LogInfo($"sm_config_625"); }
        public static void SetQiUnlimited(bool val) { QiUnlimited = val; LogInfo($"sm_config_626"); }

        // --- 晋升系统 ---
        public static void SetPromotionSpeed(float val) { PromotionSpeed = Mathf.Max(0.1f, val); LogInfo($"sm_config_627"); }
        public static void SetOnaMultiplier(float val) { OnaMultiplier = Mathf.Max(0.1f, val); LogInfo($"sm_config_628"); }
        public static void SetAutoPromotion(bool val) { AutoPromotion = val; LogInfo($"sm_config_629"); }
        public static void SetAutoPromotionMaxRank(int val)
        {
            AutoPromotionMaxRank = Mathf.Clamp(val, 0, 12);
            string[] rankNames = { "F", "E", "D", "D+", "C", "C+", "B", "B+", "A", "A+", "S", "S+", "SS", "sm_config_630" };
            LogInfo($"sm_config_631");
        }
        public static void SetShowRankInPanel(bool val) { ShowRankInPanel = val; LogInfo($"sm_config_632"); }

        // --- 自动收藏（每个主阶位独立开关）---
        public static void SetAutoFavoriteEnabled(bool val) { AutoFavoriteEnabled = val; LogInfo($"sm_config_633"); }
        public static void SetAutoFavoriteF(bool val) { AutoFavoriteF = val; LogInfo($"sm_config_634"); }
        public static void SetAutoFavoriteE(bool val) { AutoFavoriteE = val; LogInfo($"sm_config_635"); }
        public static void SetAutoFavoriteD(bool val) { AutoFavoriteD = val; LogInfo($"sm_config_636"); }
        public static void SetAutoFavoriteC(bool val) { AutoFavoriteC = val; LogInfo($"sm_config_637"); }
        public static void SetAutoFavoriteB(bool val) { AutoFavoriteB = val; LogInfo($"sm_config_638"); }
        public static void SetAutoFavoriteA(bool val) { AutoFavoriteA = val; LogInfo($"sm_config_639"); }
        public static void SetAutoFavoriteS(bool val) { AutoFavoriteS = val; LogInfo($"sm_config_640"); }
        public static void SetAutoFavoriteSS(bool val) { AutoFavoriteSS = val; LogInfo($"sm_config_641"); }
        public static void SetAutoFavoriteX(bool val) { AutoFavoriteX = val; LogInfo($"sm_config_642"); }

        /// <summary>检查指定阶位索引是否应自动收藏（+位跟随主阶位）。</summary>
        public static bool ShouldFavoriteRank(int rankIdx)
        {
            // 主阶位索引：F=0,E=1,D=2,C=4,B=6,A=8,S=10,SS=12,X=13
            if (rankIdx <= 0) return AutoFavoriteF;
            if (rankIdx <= 1) return AutoFavoriteE;
            if (rankIdx <= 3) return AutoFavoriteD;  // D和D+
            if (rankIdx <= 5) return AutoFavoriteC;  // C和C+
            if (rankIdx <= 7) return AutoFavoriteB;  // B和B+
            if (rankIdx <= 9) return AutoFavoriteA;  // A和A+
            if (rankIdx <= 11) return AutoFavoriteS; // S和S+
            if (rankIdx <= 12) return AutoFavoriteSS;
            return AutoFavoriteX;
        }

        // --- 圣所系统 ---
        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; LogInfo($"sm_config_643"); }
        public static void SetSanctuaryAutoSave(bool val) { SanctuaryAutoSave = val; LogInfo($"sm_config_644"); }
        public static void SetSanctuaryCount(int val) { /* 兼容旧配置：圣所数量已固定为6个，此选项已废弃 */ }

        // --- 机械系 ---
        public static void SetMechSummonEnabled(bool val) { MechSummonEnabled = val; LogInfo($"sm_config_645"); }
        public static void SetMaxSummonedUnits(int val) { MaxSummonedUnits = Mathf.Clamp(val, 0, 500); LogInfo($"sm_config_646"); }

        // --- 性能优化 ---
        public static void SetTickInterval(float val) { TickInterval = Mathf.Clamp(val, 1f, 60f); LogInfo($"sm_config_647"); }
        public static void SetMaxTrackedActors(int val) { MaxTrackedActors = Mathf.Clamp(val, 50, 5000); LogInfo($"sm_config_648"); }
        public static void SetLogVerbose(bool val) { LogVerbose = val; LogInfo($"sm_config_649"); }

        // --- 宇宙宝物 ---
        public static void SetRelicDropEnabled(bool val) { RelicDropEnabled = val; LogInfo($"sm_config_650"); }
        public static void SetRelicDropRate(float val) { RelicDropRate = Mathf.Clamp01(val); LogInfo($"sm_config_651"); }

        // --- 提炼法 ---
        public static void SetRefinementBonus(float val) { RefinementBonus = Mathf.Max(0f, val); LogInfo($"sm_config_652"); }

        private static void LogInfo(string msg)
        {
            if (LogVerbose) Debug.Log(msg);
        }
    }
}
