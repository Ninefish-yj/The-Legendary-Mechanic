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
                LogInfo("[超神机械师] 配置系统初始化");
                RegisterLocalization();
                LogInfo($"[超神机械师] 配置: 模组启用={ModEnabled}, 自动觉醒={AutoAwakening}, 气力倍率={QiGrowthRate}");
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
                ("General", "总控"),
                ("Awakening", "觉醒系统"),
                ("Qi", "气力系统"),
                ("Promotion", "晋升系统"),
                ("AutoFavorite", "自动收藏"),
                ("Sanctuary", "圣所系统"),
                ("Mech", "机械系"),
                ("Refinement", "提炼法与宝物"),
                ("Relic", "宇宙宝物"),
                ("Performance", "性能与调试"),
            };
            foreach (var (key, name) in categories)
                LocalizedTextManager.add(key, name, pReplace: true);

            // 配置项名称 + 描述（key格式：Id 和 Id+" Description"）
            var items = new (string id, string name, string desc)[]
            {
                ("mod_enabled", "模组总开关", "关闭后所有超神机械师系统停止运行（已赋予的特质保留）"),
                ("auto_awakening", "自然觉醒", "单位是否会随时间自然觉醒为超能者（五系随机）"),
                ("awakening_chance", "觉醒概率", "每个主循环周期内单位自然觉醒的概率（默认5%）"),
                ("awakening_min_age", "最小觉醒年龄", "单位达到多少岁后才可能自然觉醒（默认16岁）"),
                ("qi_growth_rate", "气力增长倍率", "气力值增长速度倍率，越高升级越快（默认1.0x）"),
                ("qi_unlimited", "气力无上限", "原著设定：气力等级无固定上限，越往后越难提升"),
                ("auto_promotion", "自动晋升", "单位欧纳达到阶位门槛后自动晋升"),
                ("auto_promotion_max_rank", "自动晋升上限", "自动晋升最高到哪个阶位，X阶需通过超神突破系统晋升"),
                ("promotion_speed", "晋升速度", "欧纳积累速度倍率，影响整体晋升节奏"),
                ("ona_multiplier", "欧纳计算倍率", "欧纳（战斗力函数）最终结果倍率"),
                ("show_rank_in_panel", "面板显示阶位", "在单位属性面板注入阶位/职业/气力/欧纳数据行"),
                ("auto_favorite_enabled", "自动收藏总开关", "开启后达到指定阶位的单位自动加星标收藏"),
                ("auto_favorite_f", "F阶自动收藏", "F阶单位自动收藏"),
                ("auto_favorite_e", "E阶自动收藏", "E阶单位自动收藏"),
                ("auto_favorite_d", "D阶自动收藏", "D阶（含D+）单位自动收藏"),
                ("auto_favorite_c", "C阶自动收藏", "C阶（含C+）单位自动收藏"),
                ("auto_favorite_b", "B阶自动收藏", "B阶（含B+）单位自动收藏"),
                ("auto_favorite_a", "A阶自动收藏", "A阶（含A+）单位自动收藏"),
                ("auto_favorite_s", "S阶(超A)自动收藏", "S阶（含S+）单位自动收藏"),
                ("auto_favorite_ss", "SS阶自动收藏", "SS阶单位自动收藏"),
                ("auto_favorite_x", "X阶(超神)自动收藏", "X阶单位自动收藏"),
                ("sanctuary_enabled", "圣所跨存档", "圣所数据写入模组目录JSON，不随世界存档消失"),
                ("sanctuary_autosave", "圣所自动保存", "圣所数据变更时自动写入JSON文件"),
                ("mech_summon_enabled", "机械召唤", "机械师召唤无人机/机甲/机器人（爆兵流）"),
                ("max_summoned_units", "最大召唤单位", "全场机械召唤物上限，防止爆兵流拖垮性能"),
                ("refinement_bonus", "提炼法气力加成", "修炼提炼法的单位气力增长额外倍率"),
                ("relic_drop_enabled", "宇宙宝物掉落", "高阶单位死亡时有概率掉落宇宙宝物"),
                ("relic_drop_rate", "宝物掉落率", "高阶单位死亡掉落宇宙宝物的概率"),
                ("tick_interval", "主循环间隔", "晋升/气力/能量计算的执行间隔（秒）"),
                ("max_tracked_actors", "最大追踪单位", "同时参与气力/晋升计算的单位数上限"),
                ("log_verbose", "详细日志", "在Player.log中输出所有配置变更和系统运行细节"),
            };
            foreach (var (id, name, desc) in items)
            {
                LocalizedTextManager.add(id, name, pReplace: true);
                LocalizedTextManager.add(id + " Description", desc, pReplace: true);
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
            AutoPromotionMaxRank = Mathf.Clamp(val, 0, 12);
            string[] rankNames = { "F", "E", "D", "D+", "C", "C+", "B", "B+", "A", "A+", "S", "S+", "SS", "X(无上限)" };
            LogInfo($"[配置] 自动晋升上限: {rankNames[AutoPromotionMaxRank]}");
        }
        public static void SetShowRankInPanel(bool val) { ShowRankInPanel = val; LogInfo($"[配置] 面板显示阶位: {val}"); }

        // --- 自动收藏（每个主阶位独立开关）---
        public static void SetAutoFavoriteEnabled(bool val) { AutoFavoriteEnabled = val; LogInfo($"[配置] 自动收藏总开关: {val}"); }
        public static void SetAutoFavoriteF(bool val) { AutoFavoriteF = val; LogInfo($"[配置] F阶自动收藏: {val}"); }
        public static void SetAutoFavoriteE(bool val) { AutoFavoriteE = val; LogInfo($"[配置] E阶自动收藏: {val}"); }
        public static void SetAutoFavoriteD(bool val) { AutoFavoriteD = val; LogInfo($"[配置] D阶自动收藏: {val}"); }
        public static void SetAutoFavoriteC(bool val) { AutoFavoriteC = val; LogInfo($"[配置] C阶自动收藏: {val}"); }
        public static void SetAutoFavoriteB(bool val) { AutoFavoriteB = val; LogInfo($"[配置] B阶自动收藏: {val}"); }
        public static void SetAutoFavoriteA(bool val) { AutoFavoriteA = val; LogInfo($"[配置] A阶自动收藏: {val}"); }
        public static void SetAutoFavoriteS(bool val) { AutoFavoriteS = val; LogInfo($"[配置] S阶自动收藏: {val}"); }
        public static void SetAutoFavoriteSS(bool val) { AutoFavoriteSS = val; LogInfo($"[配置] SS阶自动收藏: {val}"); }
        public static void SetAutoFavoriteX(bool val) { AutoFavoriteX = val; LogInfo($"[配置] X阶自动收藏: {val}"); }

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
        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; LogInfo($"[配置] 圣所跨存档: {val}"); }
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
