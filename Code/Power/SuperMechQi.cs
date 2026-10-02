using NeoModLoader.api;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechQi
    {
        // 原著气力等级阈值(ch51/ch539/ch1039/ch1203/ch1402)：
        // 前6级明确：lv1=10, lv2=50, lv3=100, lv4=200, lv5=400, lv6=1000
        // Lv10≈9000(ch539"突破9000大关进入下一等级")
        // Lv21=182075(ch1039"气力：182075【Lv21】")
        // Lv25=293475(ch1203"气力：293475"+Lv25加成)
        // Lv29=481200(ch1402神化时"气力：481200"+Lv29加成)
        // Lv10~Lv21每级×1.31，Lv21~Lv29每级×1.13，Lv29+每级×1.13
        // 气力等级无上限（原著"不会终止自身对于气力的锤炼"）
        public static readonly float[] Thresholds = {
            10f, 50f, 100f, 200f, 400f, 1000f,
            1730f, 3000f, 5200f, 9000f,
            11800f, 15500f, 20300f, 26600f, 34900f,
            45700f, 59900f, 78500f, 103000f, 135000f,
            182075f, 205200f, 231300f, 260600f, 293475f,
            331900f, 375400f, 424600f, 481200f, 543800f,
            // Lv31~Lv50：每级×1.13，高等级缓慢提升
            614500f, 694300f, 784600f, 886600f, 1001800f,
            1132100f, 1279200f, 1445500f, 1633500f, 1845800f,
            2085800f, 2356900f, 2663300f, 3009600f, 3400800f,
            3842900f, 4342500f, 4907000f, 5544900f, 6265800f,
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
            "Lv41", "Lv42", "Lv43", "Lv44", "Lv45",
            "Lv46", "Lv47", "Lv48", "Lv49", "Lv50",
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
            float stageMul = 1f + stage * 0.1f;

            // 原著气力系统(ch51/ch539/ch1039/ch1203/ch1402)：
            // 统一气力常数：属性 = 0.000921 × level^4.9 × 属性系数
            // 气力常数用力量4个原著数据点最小二乘拟合，无基准无(level/10)
            // 各属性系数按Lv10原著比例：力1.000/敏1.366/耐1.521/智1.718/神秘1.085
            float qiBase = 0.000921f * Mathf.Pow(level, 4.9f) * stageMul;
            float strength = qiBase * 1.000f;   // 力量
            float agility = qiBase * 1.366f;    // 敏捷
            float endurance = qiBase * 1.521f;  // 耐力
            float intel = qiBase * 1.718f;      // 智力

            // 知识/技能/融合加成（保留倍率，这些是额外加成不是气力本身）
            var synBonus = SuperMechKnowledgeSynergy.GetBonus(a);
            float fusionMul = SuperMechMechFusion.GetFusionMultiplier(a);
            var knowFusion = SuperMechKnowledgeFusion.GetFusionBonus(a);
            var skillBonus = SuperMechSkills.GetBonus(a);

            float dmgMul = synBonus.dmgMul * fusionMul * knowFusion.dmgMul * skillBonus.dmgMul;
            float hpMul = synBonus.hpMul * fusionMul * knowFusion.hpMul * skillBonus.hpMul;
            float spdMul = synBonus.speedMul * knowFusion.speedMul * skillBonus.speedMul;

            // 固定数值加成（原著方式）：力量→伤害/护甲，敏捷→速度/攻速/暴击，耐力→生命/体力，智力→智力
            stats["damage"] = strength * 0.25f;
            stats["armor"] = Mathf.Min(100f, strength * 0.15f);
            stats["speed"] = agility * 0.04f;
            stats["attack_speed"] = agility * 0.003f;
            stats["health"] = endurance * 8f;
            stats["stamina"] = endurance * 4f;
            stats["intelligence"] = intel * 0.3f + skillBonus.intelligence;
            stats["warfare"] = strength * 0.1f + agility * 0.05f;
            stats["lifespan"] = endurance * 0.5f;

            // 倍率加成（知识/技能/融合，这些是额外加成）
            stats["multiplier_damage"] = dmgMul;
            stats["multiplier_health"] = hpMul * 1.2f;
            stats["multiplier_stamina"] = 1.1f;
            stats["multiplier_speed"] = spdMul;
            stats["multiplier_crit"] = 1f + level * 0.01f;
            stats["experience"] = 1f + level * 0.01f;

            if (level >= 6)
            {
                stats["critical_chance"] = (level - 5) * 0.005f;
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

        // 原著：气力等级越高提升越难(ch51"每一层所需的气力都比上一层多得多")
        // 高等级衰减极大，符合"憋气力是超A级提升自身的王道"
        public static float GetGrowthDecay(int level)
        {
            if (level <= 5) return 1.0f;
            if (level <= 10) return 0.7f;
            if (level <= 15) return 0.4f;
            if (level <= 20) return 0.2f;
            if (level <= 25) return 0.1f;
            if (level <= 30) return 0.05f;
            if (level <= 40) return 0.02f;
            return 0.01f;
        }

        public static void AddQi(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetQi(a);
            float max = GetQiMax(a);
            if (max <= 0) max = cur + amount;
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
                float staminaCost = remaining * 3f;
                if (stamina >= staminaCost)
                {
                    a.data.stamina = Mathf.Max(0, (int)(stamina - staminaCost));
                    spent += remaining;
                }
                else
                {
                    spent += stamina / 3f;
                    a.data.stamina = 0;
                    float healthCost = (remaining - stamina / 3f) * 2f;
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
            float tickInterval = SuperMechConfig.TickInterval * 4f;
            int maxTracked = SuperMechConfig.MaxTrackedActors;
            int processed = 0;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                if (processed >= maxTracked)
                {
                    if (SuperMechAdvancement.GetExactRankIndex(a) < 8) continue;
                }
                processed++;

                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                if (curHealth < lastH - 0.5f)
                {
                    _combatTimer[a.id] = 5f;
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
                    if (max <= 0) max = qi;
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
                        recovery += staminaCost * 2f;
                    }

                    float newCur = Mathf.Min(qi + recovery, max);
                    _qiMap[a.id] = newCur;
                    qi = newCur;

                    int curLv = GetLevel(max);
                    float growthDecay = GetGrowthDecay(curLv);
                    float maxGrowth = 0.05f * tickInterval * SuperMechConfig.QiGrowthRate * growthDecay;
                    if (a.hasTrait("sm_refinement")) maxGrowth *= SuperMechConfig.RefinementBonus;
                    if (a.hasTrait("sm_em_refinement") && a.hasTrait(SuperMechTraits.ClassMech))
                        maxGrowth *= 1.5f;
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
