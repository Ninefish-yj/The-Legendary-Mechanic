using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力等级系统（原著面板数据）。
    /// 气力是超能者核心能量，机械系磁环后改称械力。
    /// 等级阈值来自原著面板：Lv3=120, Lv6=1230, Lv10=9770, Lv15=52270, Lv19=128452, Lv21=182075。
    /// 设计：气力值用字典追踪，等级特质用未注册组隐藏（不在特质编辑器显示），
    /// 单位面板通过 SuperMechUnitWindow 显示"气力：128452【Lv19】"。
    /// </summary>
    public static class SuperMechQi
    {
        // 40级阈值（原著数据点 + 插值/外推）。
        // 原著最终面板：Lv29=481200。越往后越难涨，Lv40门槛223万，正常游戏几乎不可能达到，等效无封顶。
        public static readonly float[] Thresholds = {
            10f,       // Lv1  (ch2)
            50f,       // Lv2  (ch2)
            100f,      // Lv3  (ch2/ch5: 120)
            200f,      // Lv4  (ch2/ch54: 200)
            400f,      // Lv5  (ch2)
            1000f,     // Lv6  (ch2/ch146: 1230, 分水岭)
            2000f,     // Lv7  (插值)
            2500f,     // Lv8  (ch262: 3070)
            5000f,     // Lv9  (ch356: 5210)
            8000f,     // Lv10 (ch531: 9770)
            15000f,    // Lv11 (ch630: 17580)
            22000f,    // Lv12 (ch676: 25530)
            30000f,    // Lv13 (ch735: 35530)
            42000f,    // Lv14 (插值)
            50000f,    // Lv15 (ch760: 52270)
            65000f,    // Lv16 (插值)
            75000f,    // Lv17 (ch854: 82120)
            100000f,   // Lv18 (插值)
            120000f,   // Lv19 (ch945/ch948: 128452)
            150000f,   // Lv20 (插值)
            170000f,   // Lv21 (ch1028: 182075)
            194000f,   // Lv22 (插值)
            221000f,   // Lv23 (插值)
            252000f,   // Lv24 (插值)
            287000f,   // Lv25 (插值)
            327000f,   // Lv26 (插值)
            373000f,   // Lv27 (插值)
            425000f,   // Lv28 (插值)
            450000f,   // Lv29 (最终面板: 481200)
            553000f,   // Lv30 (外推)
            636000f,   // Lv31 (外推)
            731000f,   // Lv32 (外推)
            841000f,   // Lv33 (外推)
            967000f,   // Lv34 (外推)
            1112000f,  // Lv35 (外推)
            1279000f,  // Lv36 (外推)
            1471000f,  // Lv37 (外推)
            1692000f,  // Lv38 (外推)
            1946000f,  // Lv39 (外推)
            2238000f,  // Lv40 (外推, 等效封顶)
        };

        public static readonly string[] LevelNames = {
            "Lv1", "Lv2", "Lv3", "Lv4", "Lv5",
            "Lv6（分水岭）", "Lv7", "Lv8", "Lv9", "Lv10",
            "Lv11", "Lv12", "Lv13", "Lv14", "Lv15",
            "Lv16", "Lv17", "Lv18", "Lv19", "Lv20",
            "Lv21", "Lv22", "Lv23", "Lv24", "Lv25",
            "Lv26", "Lv27", "Lv28", "Lv29", "Lv30",
            "Lv31", "Lv32", "Lv33", "Lv34", "Lv35",
            "Lv36", "Lv37", "Lv38", "Lv39", "Lv40",
        };

        // 气力当前值追踪（unit.id -> 当前气力）
        private static readonly Dictionary<long, float> _qiMap = new Dictionary<long, float>();
        // 气力上限追踪（unit.id -> 气力上限，靠修炼法/实战突破提升）
        private static readonly Dictionary<long, float> _qiMaxMap = new Dictionary<long, float>();
        // 上次生命值（用于检测战斗：血量下降=受击）
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        // 战斗状态计时（unit.id -> 剩余战斗秒数）
        private static readonly Dictionary<long, float> _combatTimer = new Dictionary<long, float>();
        // 上次应用的气力等级（避免重复写BaseStats）
        private static readonly Dictionary<long, int> _appliedLevel = new Dictionary<long, int>();

        /// <summary>气力等级不注册特质，属性加成直接写BaseStats，等级只在单位面板显示。</summary>
        public static void Register()
        {
            Debug.Log($"[超神机械师] 气力等级系统初始化：{Thresholds.Length} 级（纯内部数据，不注册特质）");
        }

        /// <summary>把气力等级的属性加成直接写到单位BaseStats（替代特质方案）。</summary>
        public static void ApplyQiStats(Actor a, int level)
        {
            if (a == null || level <= 0) return;
            int last;
            if (_appliedLevel.TryGetValue(a.id, out last) && last == level) return;

            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            // 原著ch3：每一级增加的属性并非固定，会随着主职业转职而改变，越高阶的职业，属性加成就越多
            // 职业阶段倍率：每进阶一个阶段+15%
            int stage = SuperMechStage.GetStage(a);
            float stageMul = 1f + stage * 0.15f;

            // 气力等级用百分比加成（WorldBox基础属性小，固定值永远达不到原著Lv29力量+12480的成长感）
            // 原著ch1402：Lv29气力总加成 力量+12480/敏捷+13640/耐力+17200/智力+22845/体力上限+11427000
            // 指数增长公式：multiplier = 1 + level^1.5 × 0.03 × stageMul
            float qiMul = 1f + Mathf.Pow(level, 1.5f) * 0.03f * stageMul;

            stats["multiplier_damage"] = qiMul;
            stats["multiplier_health"] = qiMul * 1.2f;
            stats["multiplier_stamina"] = qiMul * 1.1f;
            stats["multiplier_armor"] = 1f + level * 0.02f * stageMul;
            stats["multiplier_speed"] = 1f + level * 0.01f * stageMul;
            stats["multiplier_crit"] = 1f + level * 0.015f * stageMul;
            stats["experience"] = 1f + level * 0.01f;

            // 少量固定值（模拟原著基础属性加成，主要靠百分比）
            stats["damage"] = level * 0.5f * stageMul;
            stats["health"] = level * 5f * stageMul;
            stats["stamina"] = level * 3f * stageMul;
            stats["intelligence"] = level * 0.3f * stageMul;

            if (level >= 6)  // Lv6分水岭（原著ch146），额外暴击
            {
                stats["critical_chance"] = (level - 5) * 0.005f * stageMul;
            }
            _appliedLevel[a.id] = level;
        }

        /// <summary>单位死亡/移除时清理气力数据。</summary>
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _qiMap.Remove(a.id);
            _qiMaxMap.Remove(a.id);
            _lastHealth.Remove(a.id);
            _combatTimer.Remove(a.id);
            _appliedLevel.Remove(a.id);
        }

        /// <summary>获取单位气力当前值。</summary>
        public static float GetQi(Actor a)
        {
            if (a == null) return 0;
            float v;
            _qiMap.TryGetValue(a.id, out v);
            return v;
        }

        /// <summary>获取单位气力上限（靠修炼法/实战突破提升）。</summary>
        public static float GetQiMax(Actor a)
        {
            if (a == null) return 0;
            float v;
            _qiMaxMap.TryGetValue(a.id, out v);
            return v;
        }

        /// <summary>气力增长衰减（原著：越往后越难涨）。
        /// Lv1-5: 100%, Lv6-10: 70%, Lv11-15: 40%, Lv16-20: 20%, Lv21+: 10%（神性蜕变门槛后需特殊方式）</summary>
        public static float GetGrowthDecay(int level)
        {
            if (level <= 5) return 1.0f;
            if (level <= 10) return 0.7f;    // Lv6分水岭后变慢
            if (level <= 15) return 0.4f;
            if (level <= 20) return 0.2f;
            return 0.1f;                      // Lv21+神性蜕变门槛后极难
        }

        /// <summary>给单位增加气力当前值（不超过上限）。</summary>
        public static void AddQi(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetQi(a);
            float max = GetQiMax(a);
            if (max <= 0) max = cur + amount; // 首次设置时同步上限
            _qiMap[a.id] = Mathf.Min(cur + amount, max);
        }

        /// <summary>提升气力上限（修炼法/实战突破用）。</summary>
        public static void AddQiMax(Actor a, float amount)
        {
            if (a == null) return;
            float curMax = GetQiMax(a);
            float newMax = curMax + amount;
            // 气力无上限关闭时，锁定40级门槛
            if (!SuperMechConfig.QiUnlimited && Thresholds.Length > 0)
                newMax = Mathf.Min(newMax, Thresholds[Thresholds.Length - 1]);
            _qiMaxMap[a.id] = newMax;
            // 上限提升后，当前值也同步提升（突破后气力充盈）
            float cur = GetQi(a);
            if (cur < newMax) _qiMap[a.id] = newMax;
        }

        /// <summary>直接设置气力上限（存档恢复用）。</summary>
        public static void SetQiMax(Actor a, float value)
        {
            if (a == null) return;
            _qiMaxMap[a.id] = Mathf.Max(0, value);
        }

        /// <summary>直接设置气力当前值（复活/初始化用）。</summary>
        public static void SetQi(Actor a, float value)
        {
            if (a == null) return;
            _qiMap[a.id] = Mathf.Max(0, value);
            // 首次设置时同步上限
            if (GetQiMax(a) < value) _qiMaxMap[a.id] = value;
        }

        /// <summary>
        /// 消耗气力（战斗/技能）。原著ch3链式消耗：
        /// 1. 气力足够→直接耗气力
        /// 2. 气力不足→耗光气力，剩余超比例耗体力（1:3，"超比例消耗体力值"）
        /// 3. 体力不足→耗光体力，剩余扣生命（"被自身异能榨干的超能者，都是这么死的"）
        /// 返回实际消耗的气力当量。
        /// </summary>
        public static float SpendQi(Actor a, float amount)
        {
            if (a == null || amount <= 0) return 0;
            float cur = GetQi(a);
            float spent = 0f;

            if (cur >= amount)
            {
                // 气力足够
                _qiMap[a.id] = cur - amount;
                spent = amount;
            }
            else
            {
                // 气力不足，先耗光
                spent = cur;
                _qiMap[a.id] = 0f;
                float remaining = amount - cur;

                // 超比例耗体力（1:3）
                float stamina = a.getStamina();
                float staminaCost = remaining * 3f; // 超比例
                if (stamina >= staminaCost)
                {
                    a.data.stamina = Mathf.Max(0, (int)(stamina - staminaCost));
                    spent += remaining;
                }
                else
                {
                    // 体力也不够，耗光体力，剩余扣生命
                    spent += stamina / 3f;
                    a.data.stamina = 0;
                    float healthCost = (remaining - stamina / 3f) * 2f; // 体力空后扣生命更狠
                    a.data.health = Mathf.Max(1f, a.data.health - healthCost);
                    spent += (stamina / 3f);
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师] {a.name} 气力体力双空，被异能榨干！扣生命{healthCost:F0}");
                }
            }
            return spent;
        }

        /// <summary>单位是否在战斗中（最近5秒内受过伤）。</summary>
        public static bool IsInCombat(Actor a)
        {
            if (a == null) return false;
            float t;
            return _combatTimer.TryGetValue(a.id, out t) && t > 0;
        }

        /// <summary>根据气力值计算等级。</summary>
        public static int GetLevel(float qiValue)
        {
            int lv = 0;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (qiValue >= Thresholds[i]) lv = i + 1;
            }
            return lv;
        }

        /// <summary>
        /// 每tick：战斗消耗气力 + 非战斗恢复气力 + 同步等级特质。
        /// 原著机制（ch3）：
        /// - 释放技能消耗气力，低于等级标准丧失该级加成
        /// - 自行缓慢恢复，气力空→超比例耗体力→体力空→耗生命
        /// </summary>
        public static void TickQiLevels()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;
            int maxTracked = SuperMechConfig.MaxTrackedActors;
            int processed = 0;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 性能保护：超过最大追踪数时，只处理高阶位单位（按阶位排序）
                if (processed >= maxTracked)
                {
                    // 低阶位单位跳过，高阶位单位继续处理
                    if (SuperMechAdvancement.GetExactRankIndex(a) < 8) continue; // B阶以下跳过
                }
                processed++;

                // —— 战斗检测：血量下降=受击，进入战斗状态5秒 ——
                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                if (curHealth < lastH - 0.5f)
                {
                    _combatTimer[a.id] = 5f;  // 5秒战斗状态
                }
                _lastHealth[a.id] = curHealth;

                // 减少战斗计时
                float ct;
                if (_combatTimer.TryGetValue(a.id, out ct) && ct > 0)
                {
                    _combatTimer[a.id] = ct - tickInterval;
                }

                bool inCombat = IsInCombat(a);
                float qi = GetQi(a);

                if (inCombat)
                {
                    // 战斗中消耗气力（SpendQi自动处理气力→体力→生命链式消耗）
                    int qiLv = GetLevel(qi);
                    float consume = (1f + qiLv * 0.3f) * tickInterval;
                    SpendQi(a, consume);
                    qi = GetQi(a);

                    // —— 实战突破：战斗中缓慢提升气力上限（原著：生死间突破）——
                    int curLv2 = GetLevel(qi);
                    float combatGrowth = 0.15f * tickInterval * GetGrowthDecay(curLv2);
                    if (a.hasTrait("sm_refinement")) combatGrowth *= 1.5f;
                    if (a.hasTrait("sm_divinity_ascended")) combatGrowth *= 1.5f;
                    AddQiMax(a, combatGrowth);
                }
                else
                {
                    // —— 非战斗：恢复气力当前值到上限（原著：自行缓慢恢复）——
                    float max = GetQiMax(a);
                    if (max <= 0) max = qi; // 首次初始化
                    float intel = 1f;
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        float iv = stats["intelligence"];
                        intel = 1f + iv * 0.02f;
                    }
                    // 恢复速度：基于智力，恢复到上限为止
                    float recovery = (2f + max * 0.005f) * intel * tickInterval * SuperMechConfig.QiGrowthRate;
                    if (a.hasTrait("sm_refinement")) recovery *= SuperMechConfig.RefinementBonus;
                    if (a.hasTrait(SuperMechTraits.ClassPsi))
                        recovery *= SuperMechPotentialRating.GetQiGrowthMult(a);

                    // 原著：气力可消耗体力快速恢复（体力>30%时触发，1体力=2气力）
                    float stam = a.getStamina();
                    float stamMax = a.getMaxStamina();
                    if (stamMax > 0 && stam / stamMax > 0.3f && qi < max)
                    {
                        float staminaCost = Mathf.Min(stam * 0.1f, (max - qi) / 2f);
                        a.data.stamina = Mathf.Max(0, (int)(stam - staminaCost));
                        recovery += staminaCost * 2f; // 1体力=2气力
                    }

                    // 当前值恢复到上限，不超过上限
                    float newCur = Mathf.Min(qi + recovery, max);
                    _qiMap[a.id] = newCur;
                    qi = newCur;

                    // —— 修炼法：缓慢提升气力上限（原著：修炼法锻炼提升上限）——
                    // 所有超能者都有基础修炼速度（很慢），有提炼法的加速
                    // 原著：气力越往后越难涨，高阶位增长衰减
                    int curLv = GetLevel(max);
                    float growthDecay = GetGrowthDecay(curLv);
                    float maxGrowth = 0.05f * tickInterval * SuperMechConfig.QiGrowthRate * growthDecay;
                    if (a.hasTrait("sm_refinement")) maxGrowth *= SuperMechConfig.RefinementBonus; // 提炼法加成
                    if (a.hasTrait("sm_em_refinement") && a.hasTrait(SuperMechTraits.ClassMech))
                        maxGrowth *= 1.5f; // 电磁因子提炼法再×1.5
                    // 神性蜕变开启后增长效率提升（ch1039：神性蜕变是高阶成长核心）
                    if (a.hasTrait("sm_divinity_ascended")) maxGrowth *= 1.5f;
                    AddQiMax(a, maxGrowth);
                }

                // —— 同步气力等级属性加成（直接写BaseStats，不注册特质）——
                int targetLv = GetLevel(qi);
                ApplyQiStats(a, targetLv);
            }
        }

        /// <summary>清空所有气力数据（世界切换用）。</summary>
        public static void Clear()
        {
            _qiMap.Clear();
            _qiMaxMap.Clear();
            _lastHealth.Clear();
            _combatTimer.Clear();
            _appliedLevel.Clear();
        }
    }
}
