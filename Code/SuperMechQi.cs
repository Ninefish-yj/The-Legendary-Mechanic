using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    // 气力系统：40级阈值，气力空→体力×3→生命×2

    public static class SuperMechQi
    {
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
            "sm_qi_937", "Lv7", "Lv8", "Lv9", "Lv10",
            "Lv11", "Lv12", "Lv13", "Lv14", "Lv15",
            "Lv16", "Lv17", "Lv18", "Lv19", "Lv20",
            "Lv21", "Lv22", "Lv23", "Lv24", "Lv25",
            "Lv26", "Lv27", "Lv28", "Lv29", "Lv30",
            "Lv31", "Lv32", "Lv33", "Lv34", "Lv35",
            "Lv36", "Lv37", "Lv38", "Lv39", "Lv40",
        };

        private static readonly Dictionary<long, float> _qiMap = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _qiMaxMap = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _combatTimer = new Dictionary<long, float>();
        private static readonly Dictionary<long, int> _appliedLevel = new Dictionary<long, int>();

        public static void Register()
        {
            Debug.Log($"[超神机械师] 气力等级系统初始化：{Thresholds.Length} 级（纯内部数据，不注册特质）");
        }

        public static void ApplyQiStats(Actor a, int level)
        {
            if (a == null || level <= 0) return;
            int last;
            if (_appliedLevel.TryGetValue(a.id, out last) && last == level) return;

            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            int stage = SuperMechStage.GetStage(a);
            float stageMul = 1f + stage * 0.15f;

            float qiMul = 1f + level * level * 0.02f * stageMul;

            var synBonus = SuperMechKnowledgeSynergy.GetBonus(a);
            qiMul *= synBonus.dmgMul;

            float fusionMul = SuperMechMechFusion.GetFusionMultiplier(a);
            qiMul *= fusionMul;

            var knowFusion = SuperMechKnowledgeFusion.GetFusionBonus(a);
            qiMul *= knowFusion.dmgMul;

            var skillBonus = SuperMechSkills.GetBonus(a);
            qiMul *= skillBonus.dmgMul;

            stats["multiplier_damage"] = qiMul;
            stats["multiplier_health"] = qiMul * 1.3f * synBonus.hpMul * fusionMul * knowFusion.hpMul * skillBonus.hpMul;
            stats["multiplier_stamina"] = qiMul * 1.2f;
            stats["armor"] = Mathf.Min(80f, level * 0.5f * stageMul);
            stats["multiplier_speed"] = (1f + level * 0.012f * stageMul) * synBonus.speedMul * knowFusion.speedMul * skillBonus.speedMul;
            stats["multiplier_crit"] = 1f + level * 0.018f * stageMul;
            stats["experience"] = 1f + level * 0.01f;

            stats["damage"] = level * level * 0.05f * stageMul;
            stats["health"] = level * level * 0.5f * stageMul;
            stats["stamina"] = level * level * 0.3f * stageMul;
            stats["intelligence"] = level * 0.3f * stageMul + skillBonus.intelligence;

            if (level >= 6)  // Lv6分水岭（原著ch146），额外暴击
            {
                stats["critical_chance"] = (level - 5) * 0.005f * stageMul;
            }
            _appliedLevel[a.id] = level;
        }

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _qiMap.Remove(a.id);
            _qiMaxMap.Remove(a.id);
            _lastHealth.Remove(a.id);
            _combatTimer.Remove(a.id);
            _appliedLevel.Remove(a.id);
        }

        public static float GetQi(Actor a)
        {
            if (a == null) return 0;
            float v;
            _qiMap.TryGetValue(a.id, out v);
            return v;
        }

        public static float GetQiMax(Actor a)
        {
            if (a == null) return 0;
            float v;
            _qiMaxMap.TryGetValue(a.id, out v);
            return v;
        }

        public static float GetGrowthDecay(int level)
        {
            if (level <= 5) return 1.0f;
            if (level <= 10) return 0.7f;    // Lv6分水岭后变慢
            if (level <= 15) return 0.4f;
            if (level <= 20) return 0.2f;
            return 0.1f;                      // Lv21+神性蜕变门槛后极难
        }

        public static void AddQi(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetQi(a);
            float max = GetQiMax(a);
            if (max <= 0) max = cur + amount; // 首次设置时同步上限
            _qiMap[a.id] = Mathf.Min(cur + amount, max);
        }

        public static void AddQiMax(Actor a, float amount)
        {
            if (a == null) return;
            float curMax = GetQiMax(a);
            float newMax = curMax + amount;
            if (!SuperMechConfig.QiUnlimited && Thresholds.Length > 0)
                newMax = Mathf.Min(newMax, Thresholds[Thresholds.Length - 1]);
            _qiMaxMap[a.id] = newMax;
            float cur = GetQi(a);
            if (cur < newMax) _qiMap[a.id] = newMax;
        }

        public static void SetQiMax(Actor a, float value)
        {
            if (a == null) return;
            _qiMaxMap[a.id] = Mathf.Max(0, value);
        }

        public static void SetQi(Actor a, float value)
        {
            if (a == null) return;
            _qiMap[a.id] = Mathf.Max(0, value);
            if (GetQiMax(a) < value) _qiMaxMap[a.id] = value;
        }

        public static float SpendQi(Actor a, float amount)
        {
            if (a == null || amount <= 0) return 0;
            float cur = GetQi(a);
            float spent = 0f;

            if (cur >= amount)
            {
                _qiMap[a.id] = cur - amount;
                spent = amount;
            }
            else
            {
                spent = cur;
                _qiMap[a.id] = 0f;
                float remaining = amount - cur;

                float stamina = a.getStamina();
                float staminaCost = remaining * 3f; // 超比例
                if (stamina >= staminaCost)
                {
                    a.data.stamina = Mathf.Max(0, (int)(stamina - staminaCost));
                    spent += remaining;
                }
                else
                {
                    spent += stamina / 3f;
                    a.data.stamina = 0;
                    float healthCost = (remaining - stamina / 3f) * 2f; // 体力空后扣生命更狠
                    a.data.health = (int)Mathf.Max(1f, a.data.health - healthCost);
                    spent += (stamina / 3f);
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师] {a.name} 气力体力双空，被异能榨干！扣生命{healthCost:F0}");
                }
            }
            return spent;
        }

        public static bool IsInCombat(Actor a)
        {
            if (a == null) return false;
            float t;
            return _combatTimer.TryGetValue(a.id, out t) && t > 0;
        }

        public static int GetLevel(float qiValue)
        {
            int lv = 0;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (qiValue >= Thresholds[i]) lv = i + 1;
            }
            return lv;
        }

        public static void TickQiLevels()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f; // 分组4：每4次UnifiedTick调用才跑一次本系统
            int maxTracked = SuperMechConfig.MaxTrackedActors;
            int processed = 0;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                if (processed >= maxTracked)
                {
                    if (SuperMechAdvancement.GetExactRankIndex(a) < 8) continue; // B阶以下跳过
                }
                processed++;

                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                if (curHealth < lastH - 0.5f)
                {
                    _combatTimer[a.id] = 5f;  // 5秒战斗状态
                }
                _lastHealth[a.id] = curHealth;

                float ct;
                if (_combatTimer.TryGetValue(a.id, out ct) && ct > 0)
                {
                    _combatTimer[a.id] = ct - tickInterval;
                }

                bool inCombat = IsInCombat(a);
                float qi = GetQi(a);

                if (inCombat)
                {
                    int qiLv = GetLevel(qi);
                    float consume = (1f + qiLv * 0.3f) * tickInterval;
                    SpendQi(a, consume);
                    qi = GetQi(a);

                    int curLv2 = GetLevel(qi);
                    float combatGrowth = 0.15f * tickInterval * GetGrowthDecay(curLv2);
                    if (a.hasTrait("sm_refinement")) combatGrowth *= 1.5f;
                    if (a.hasTrait("sm_divinity_ascended")) combatGrowth *= 1.5f;
                    AddQiMax(a, combatGrowth);
                }
                else
                {
                    float max = GetQiMax(a);
                    if (max <= 0) max = qi; // 首次初始化
                    float intel = 1f;
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        float iv = stats["intelligence"];
                        intel = 1f + iv * 0.02f;
                    }
                    float recovery = (2f + max * 0.005f) * intel * tickInterval * SuperMechConfig.QiGrowthRate;
                    if (a.hasTrait("sm_refinement")) recovery *= SuperMechConfig.RefinementBonus;
                    if (a.hasTrait(SuperMechTraits.ClassPsi))
                        recovery *= SuperMechPotentialRating.GetQiGrowthMult(a);

                    float stam = a.getStamina();
                    float stamMax = a.getMaxStamina();
                    if (stamMax > 0 && stam / stamMax > 0.3f && qi < max)
                    {
                        float staminaCost = Mathf.Min(stam * 0.1f, (max - qi) / 2f);
                        a.data.stamina = Mathf.Max(0, (int)(stam - staminaCost));
                        recovery += staminaCost * 2f; // 1体力=2气力
                    }

                    float newCur = Mathf.Min(qi + recovery, max);
                    _qiMap[a.id] = newCur;
                    qi = newCur;

                    int curLv = GetLevel(max);
                    float growthDecay = GetGrowthDecay(curLv);
                    float maxGrowth = 0.05f * tickInterval * SuperMechConfig.QiGrowthRate * growthDecay;
                    if (a.hasTrait("sm_refinement")) maxGrowth *= SuperMechConfig.RefinementBonus; // 提炼法加成
                    if (a.hasTrait("sm_em_refinement") && a.hasTrait(SuperMechTraits.ClassMech))
                        maxGrowth *= 1.5f; // 电磁因子提炼法再×1.5
                    if (a.hasTrait("sm_divinity_ascended")) maxGrowth *= 1.5f;
                    AddQiMax(a, maxGrowth);
                }

                int targetLv = GetLevel(qi);
                ApplyQiStats(a, targetLv);
            }
        }

        public static void Clear()
        {
            _qiMap.Clear();
            _qiMaxMap.Clear();
            _lastHealth.Clear();
            _combatTimer.Clear();
            _appliedLevel.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_qiMap, alive);
            removed += SuperMechCleanup.CleanDict(_qiMaxMap, alive);
            removed += SuperMechCleanup.CleanDict(_lastHealth, alive);
            removed += SuperMechCleanup.CleanDict(_combatTimer, alive);
            removed += SuperMechCleanup.CleanDict(_appliedLevel, alive);
            return removed;
        }
    }
}
