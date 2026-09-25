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

        // 隐藏组：不注册到 ActorTraitGroupLibrary，因此不在特质编辑器显示
        private const string HiddenGroup = "sm_qi_hidden";

        public static string GetId(int level) => $"sm_qi_{level}";

        public static void Register()
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                int lv = i + 1;
                string id = GetId(lv);

                var t = new ActorTrait
                {
                    id = id,
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = HiddenGroup,  // 未注册组 → 不在特质编辑器显示
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                // 原著气力加成表（ch50机械师学徒基准，外推至40级）：
                // Lv1(10): 力量+1 敏捷+1 耐力+1 智力+1 体力上限+20 机械威力+1% 制造速度+1%
                // Lv2(50): 力量+3 敏捷+2 耐力+3 智力+1 体力上限+50 机械威力+2% 制造速度+2%
                // Lv3(100):力量+3 敏捷+3 耐力+5 智力+2 体力上限+100 机械威力+3% 制造速度+3%
                // 映射到WorldBox stat key：
                //   力量→damage/warfare, 敏捷→speed/attack_speed, 耐力→health/stamina/armor,
                //   智力→intelligence, 体力上限→stamina, 机械威力→multiplier_damage, 制造速度→experience
                t.base_stats["damage"] = lv * 2f;               // 力量
                t.base_stats["warfare"] = lv;                   // 战斗技能（力量）
                t.base_stats["intelligence"] = lv;              // 智力
                t.base_stats["health"] = lv * 15f;              // 耐力→生命
                t.base_stats["stamina"] = lv * 12f;             // 耐力/体力上限
                t.base_stats["armor"] = lv * 0.3f;              // 耐力→护甲
                t.base_stats["speed"] = lv * 0.04f;             // 敏捷
                t.base_stats["attack_speed"] = lv * 0.02f;      // 敏捷→攻速
                t.base_stats["multiplier_damage"] = 1f + lv * 0.02f;   // 机械威力（Lv29=1.58x）
                t.base_stats["multiplier_health"] = 1f + lv * 0.05f;   // 耐力倍率
                t.base_stats["multiplier_stamina"] = 1f + lv * 0.03f;  // 体力上限倍率
                t.base_stats["experience"] = 1f + lv * 0.01f;          // 制造速度→经验获取
                if (lv >= 6)  // Lv6分水岭（原著ch146），额外暴击
                {
                    t.base_stats["critical_chance"] = (lv - 5) * 0.01f;
                    t.base_stats["multiplier_crit"] = 1f + (lv - 5) * 0.05f;
                }
                AssetManager.traits.add(t);
            }
            Debug.Log($"[超神机械师] 气力等级注册完成：{Thresholds.Length} 级（隐藏特质，原著属性加成表）");
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

        /// <summary>消耗气力（战斗/技能）。返回实际消耗量。</summary>
        public static float SpendQi(Actor a, float amount)
        {
            if (a == null) return 0;
            float cur = GetQi(a);
            float spent = Mathf.Min(cur, amount);
            _qiMap[a.id] = cur - spent;
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

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

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
                    // —— 战斗中：消耗气力当前值（原著：释放技能消耗气力）——
                    int qiLv = GetLevel(qi);
                    float consume = (1f + qiLv * 0.3f) * tickInterval;
                    SpendQi(a, consume);
                    qi = GetQi(a);

                    // 气力空了：消耗生命（原著：气力空→耗体力→耗生命）
                    if (qi <= 0f && a.data.health > 1f)
                    {
                        a.data.health = Mathf.Max(1, (int)(a.data.health - 2f * tickInterval));
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师] {a.name} 气力耗尽，消耗生命！");
                    }

                    // —— 实战突破：战斗中缓慢提升气力上限（原著：生死间突破）——
                    float combatGrowth = 0.15f * tickInterval;
                    if (a.hasTrait("sm_refinement")) combatGrowth *= 1.5f;
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
                        float? iv = stats["intelligence"];
                        if (iv.HasValue) intel = 1f + iv.Value * 0.02f;
                    }
                    // 恢复速度：基于智力，恢复到上限为止
                    float recovery = (2f + max * 0.005f) * intel * tickInterval;
                    if (a.hasTrait("sm_refinement")) recovery *= 1.5f;
                    if (a.hasTrait(SuperMechTraits.ClassPsi))
                        recovery *= SuperMechPotentialRating.GetQiGrowthMult(a);
                    // 当前值恢复到上限，不超过上限
                    float newCur = Mathf.Min(qi + recovery, max);
                    _qiMap[a.id] = newCur;
                    qi = newCur;

                    // —— 修炼法：缓慢提升气力上限（原著：修炼法锻炼提升上限）——
                    // 所有超能者都有基础修炼速度（很慢），有提炼法的加速
                    float maxGrowth = 0.05f * tickInterval; // 基础修炼速度
                    if (a.hasTrait("sm_refinement")) maxGrowth *= 3f; // 提炼法×3
                    if (a.hasTrait("sm_em_refinement") && a.hasTrait(SuperMechTraits.ClassMech))
                        maxGrowth *= 1.5f; // 电磁因子提炼法再×1.5
                    AddQiMax(a, maxGrowth);
                }

                // —— 同步气力等级特质（低于阈值自动降级，丧失加成）——
                int targetLv = GetLevel(qi);
                for (int lv = 1; lv <= Thresholds.Length; lv++)
                {
                    string id = GetId(lv);
                    if (lv == targetLv)
                    {
                        if (targetLv > 0 && !a.hasTrait(id)) a.addTrait(id);
                    }
                    else if (a.hasTrait(id))
                    {
                        a.removeTrait(id);
                    }
                }
            }
        }

        /// <summary>清空所有气力数据（世界切换用）。</summary>
        public static void Clear()
        {
            _qiMap.Clear();
            _qiMaxMap.Clear();
            _lastHealth.Clear();
            _combatTimer.Clear();
        }
    }
}
