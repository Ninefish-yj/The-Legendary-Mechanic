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
        public static int AutoPromotionMaxRank = 13;      // 自动晋升上限阶位索引（0=F~13=X，默认13=无上限）
        public static bool ShowRankInPanel = true;        // 单位面板显示阶位

        // ========== 自动收藏 ==========
        public static bool AutoFavoriteEnabled = true;    // 达到指定阶位自动收藏（星标）
        public static int AutoFavoriteRank = 8;           // 自动收藏阶位阈值（默认8=A级，达到该阶位及以上自动收藏）

        // ========== 圣所系统 ==========
        public static bool SanctuaryEnabled = true;       // 圣所跨存档
        public static int SanctuaryCount = 6;             // 圣所数量（原著6个）
        public static bool SanctuaryAutoSave = true;      // 圣所自动保存

        // ========== 机械系 ==========
        public static bool MechSummonEnabled = true;      // 机械召唤（爆兵流占位）
        public static int MaxSummonedUnits = 50;          // 最大召唤单位数（性能保护）

        // ========== 性能优化 ==========
        public static float TickInterval = 5.0f;          // 主循环间隔（秒）
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
                LogInfo("[超神机械师] 配置系统初始化");
                // NML 会在加载 default_config.json 后自动设置初始值
                // 这里只做日志和状态确认
                LogInfo($"[超神机械师] 配置: 模组启用={ModEnabled}, 自动觉醒={AutoAwakening}, 气力倍率={QiGrowthRate}");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 配置初始化异常: " + e.Message);
            }
        }

        // ========== Callback 方法（NML 反射调用，签名：静态方法，参数为配置值） ==========

        // --- 总开关 ---
        public static void SetModEnabled(bool val) { ModEnabled = val; LogInfo($"[配置] 模组启用: {val}"); }

        // --- 觉醒系统 ---
        public static void SetAutoAwakening(bool val) { AutoAwakening = val; LogInfo($"[配置] 自动觉醒: {val}"); }
        public static void SetAwakeningChance(float val) { AwakeningChance = Mathf.Clamp01(val); LogInfo($"[配置] 觉醒概率: {val:P0}"); }
        public static void SetAwakeningMinAge(int val) { AwakeningMinAge = Mathf.Max(0, val); LogInfo($"[配置] 最小觉醒年龄: {val}"); }

        // --- 气力系统 ---
        public static void SetQiGrowthRate(float val) { QiGrowthRate = Mathf.Max(0.1f, val); LogInfo($"[配置] 气力增长倍率: {val}x"); }
        public static void SetQiUnlimited(bool val) { QiUnlimited = val; LogInfo($"[配置] 气力无上限: {val}"); }

        // --- 晋升系统 ---
        public static void SetPromotionSpeed(float val) { PromotionSpeed = Mathf.Max(0.1f, val); LogInfo($"[配置] 晋升速度: {val}x"); }
        public static void SetOnaMultiplier(float val) { OnaMultiplier = Mathf.Max(0.1f, val); LogInfo($"[配置] 欧纳倍率: {val}x"); }
        public static void SetAutoPromotion(bool val) { AutoPromotion = val; LogInfo($"[配置] 自动晋升: {val}"); }
        public static void SetAutoPromotionMaxRank(int val)
        {
            AutoPromotionMaxRank = Mathf.Clamp(val, 0, 13);
            string[] rankNames = { "F", "E", "D", "D+", "C", "C+", "B", "B+", "A", "A+", "S", "S+", "SS", "X(无上限)" };
            LogInfo($"[配置] 自动晋升上限: {rankNames[AutoPromotionMaxRank]}");
        }
        public static void SetShowRankInPanel(bool val) { ShowRankInPanel = val; LogInfo($"[配置] 面板显示阶位: {val}"); }

        // --- 自动收藏 ---
        public static void SetAutoFavoriteEnabled(bool val) { AutoFavoriteEnabled = val; LogInfo($"[配置] 自动收藏: {val}"); }
        public static void SetAutoFavoriteRank(int val)
        {
            AutoFavoriteRank = Mathf.Clamp(val, 0, 13);
            string[] rankNames = { "F", "E", "D", "D+", "C", "C+", "B", "B+", "A", "A+", "S", "S+", "SS", "X" };
            LogInfo($"[配置] 自动收藏阈值: {rankNames[AutoFavoriteRank]}阶及以上");
        }

        // --- 圣所系统 ---
        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; LogInfo($"[配置] 圣所跨存档: {val}"); }
        public static void SetSanctuaryCount(int val) { SanctuaryCount = Mathf.Clamp(val, 1, 12); LogInfo($"[配置] 圣所数量: {val}"); }
        public static void SetSanctuaryAutoSave(bool val) { SanctuaryAutoSave = val; LogInfo($"[配置] 圣所自动保存: {val}"); }

        // --- 机械系 ---
        public static void SetMechSummonEnabled(bool val) { MechSummonEnabled = val; LogInfo($"[配置] 机械召唤: {val}"); }
        public static void SetMaxSummonedUnits(int val) { MaxSummonedUnits = Mathf.Clamp(val, 0, 500); LogInfo($"[配置] 最大召唤单位: {val}"); }

        // --- 性能优化 ---
        public static void SetTickInterval(float val) { TickInterval = Mathf.Clamp(val, 1f, 60f); LogInfo($"[配置] 主循环间隔: {val}s"); }
        public static void SetMaxTrackedActors(int val) { MaxTrackedActors = Mathf.Clamp(val, 50, 5000); LogInfo($"[配置] 最大追踪单位: {val}"); }
        public static void SetLogVerbose(bool val) { LogVerbose = val; LogInfo($"[配置] 详细日志: {val}"); }

        // --- 宇宙宝物 ---
        public static void SetRelicDropEnabled(bool val) { RelicDropEnabled = val; LogInfo($"[配置] 宝物掉落: {val}"); }
        public static void SetRelicDropRate(float val) { RelicDropRate = Mathf.Clamp01(val); LogInfo($"[配置] 宝物掉落率: {val:P2}"); }

        // --- 提炼法 ---
        public static void SetRefinementBonus(float val) { RefinementBonus = Mathf.Max(0f, val); LogInfo($"[配置] 提炼法加成: {val}x"); }

        private static void LogInfo(string msg)
        {
            if (LogVerbose) Debug.Log(msg);
        }
    }
}
