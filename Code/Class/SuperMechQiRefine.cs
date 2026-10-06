using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.20: 气力修炼法（合并提炼法+锻炼法为单一系统）
    /// 原著依据：早期【气力提炼法】技能（chapter171-172，韩萧传授玩家增加气力）
    ///           + 后期"气力锻炼法"功法（第780章《赫伯尔恒星熔炉锻炼法》、chapter1013/1039/1189/1195，超A级修行提升气力上限）
    /// 同一"提升气力上限"体系的先后叫法 → 模组合并为单一系统，两个入口：
    ///   · 主动提炼：消耗经验+体力快速提升气力上限（贴原著"提炼法"技能形态）
    ///   · 被动修行：非战斗持续提升气力上限（贴原著"锻炼法"功法形态）
    /// 属性→修炼效率→气力→能级间接链由此完整。
    /// </summary>
    public static class SuperMechQiRefine
    {
        public const string RefineMethodTrait = "sm_qi_refine";
        public const int MaxRefineLevel = 10;

        // ---- 提炼（主动）常量 ----
        public const int MaxRefineCount = 80;
        public const int RefineXpCost = 800;
        public const float RefineBaseQi = 100f;
        public const int MaxEmRefineCount = 100;

        // ---- 修行层数 ----
        private static readonly Dictionary<long, int> _refineLevel = new Dictionary<long, int>();

        // ---- 提炼进度 ----
        public static readonly Dictionary<long, int> _refineCount = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _emRefineCount = new Dictionary<long, int>();
        public static readonly Dictionary<long, float> _refineQiBonus = new Dictionary<long, float>();
        public static readonly Dictionary<long, float> _emRefineQiBonus = new Dictionary<long, float>();

        private static bool _traitRegistered;

        public static int GetRefineLevel(Actor a)
        {
            if (a == null) return 0;
            int v;
            if (_refineLevel.TryGetValue(a.data.id, out v)) return v;
            return 0;
        }

        public static bool HasRefineMethod(Actor a)
        {
            return GetRefineLevel(a) > 0;
        }

        public static void AddRefineLevel(Actor a, int add)
        {
            if (a == null || add <= 0) return;
            int cur = GetRefineLevel(a);
            int next = Mathf.Min(MaxRefineLevel, cur + add);
            if (next == cur) return;
            _refineLevel[a.data.id] = next;
            Debug.Log($"[超神机械师] {a.name} 习得气力修炼法 Lv{next}，气力上限将随长久修行缓慢提升");
        }

        public static int GetRefineCount(Actor a) => a != null && _refineCount.TryGetValue(a.data.id, out int v) ? v : 0;
        public static int GetEmRefineCount(Actor a) => a != null && _emRefineCount.TryGetValue(a.data.id, out int v) ? v : 0;
        public static float GetRefineQiBonus(Actor a) => a != null && _refineQiBonus.TryGetValue(a.data.id, out float v) ? v : 0f;
        public static float GetEmRefineQiBonus(Actor a) => a != null && _emRefineQiBonus.TryGetValue(a.data.id, out float v) ? v : 0f;

        public static void Register()
        {
            if (_traitRegistered) return;
            _traitRegistered = true;
            // 不再注册为ActorTrait，修炼法是纯数据存储
            LocalizedTextManager.add("trait_" + RefineMethodTrait, LocalizedTextManager.getText("sm_qi_refine_080"), pReplace: true);
            LocalizedTextManager.add("trait_" + RefineMethodTrait + "_info", LocalizedTextManager.getText("sm_qi_refine_081"), pReplace: true);
        }

        // ============ 入口1：被动修行（锻炼法） ============
        /// <summary>修行tick：非战斗单位按层数持续提升气力上限（原著"长久锻炼慢慢提升"）</summary>
        public static void TickCultivation()
        {
            float tickInterval = 1.25f; // 与同组修炼tick一致（每4tick执行一次）
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;
                int level = GetRefineLevel(a);
                if (level <= 0) continue;
                if (SuperMechQi.IsInCombat(a)) continue; // 战斗中靠战斗成长，和平期靠锻炼法

                float intel = 5f;
                var stats = SuperMechStats.Of(a);
                if (stats != null) intel = stats["intelligence"];

                float gain = 0.5f * level * (1f + intel * 0.02f) * tickInterval;
                SuperMechQi.AddQiMax(a, gain);
            }
        }

        // ============ 入口2：主动提炼（提炼法） ============
        /// <summary>提炼完美度：主属性决定（机械/念力/法师/心念=智力，武道=战力+耐力）</summary>
        private static float CalcPerfection(Actor a)
        {
            float mainStat = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMartial)) mainStat = (stats["warfare"] + stats["stamina"]) / 2f;
                else if (a.hasTrait(SuperMechTraits.ClassPsi)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMage)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMind)) mainStat = stats["intelligence"];
            }
            float perfection = 50f + mainStat * 2f;
            return Mathf.Clamp(perfection, 10f, 95f);
        }

        private static int CalcQiGain(float perfection)
        {
            if (perfection >= 80f) return 30;
            if (perfection >= 40f) return 20;
            return 10;
        }

        /// <summary>主动提炼（全系通用）：消耗经验，按完美度提升气力上限，80次上限</summary>
        public static bool TryRefine(Actor a)
        {
            if (a == null || !HasRefineMethod(a)) return false;
            long id = a.data.id;
            int count = GetRefineCount(a);
            if (count >= MaxRefineCount) return false;

            if (SuperMechAwakened.IsAwakened(a))
            {
                float xp = SuperMechAwakened.GetXp(a);
                if (xp < RefineXpCost) return false;
                SuperMechAwakened.AddXp(a, -RefineXpCost);
            }

            float perfection = CalcPerfection(a);
            int qiGain = CalcQiGain(perfection);

            _refineCount[id] = count + 1;
            if (!_refineQiBonus.ContainsKey(id)) _refineQiBonus[id] = RefineBaseQi;
            _refineQiBonus[id] += qiGain;

            SuperMechQi.AddQiMax(a, qiGain);
            SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
            return true;
        }

        /// <summary>机械师专属提炼（电磁因子提炼法，原著sm_refinement_082定位）：智力驱动，100次上限</summary>
        public static bool TryEmRefine(Actor a)
        {
            if (a == null || !HasRefineMethod(a)) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMech)) return false;
            long id = a.data.id;
            int count = GetEmRefineCount(a);
            if (count >= MaxEmRefineCount) return false;

            float intel = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null) intel = stats["intelligence"];
            float qiGain = 0.5f * (1f + intel * 0.02f);
            if (!_emRefineQiBonus.ContainsKey(id)) _emRefineQiBonus[id] = 0;
            _emRefineQiBonus[id] += qiGain;
            _emRefineCount[id] = count + 1;
            SuperMechQi.AddQiMax(a, qiGain);
            return true;
        }

        /// <summary>清理已死亡单位的缓存（合并自原SuperMechRefinement）</summary>
        /// <summary>v0.75.35: 换存档/宇宙迭代时清空精炼数据（防跨存档残留污染新世界）</summary>
        public static void Clear()
        {
            _refineLevel.Clear();
            _refineCount.Clear();
            _emRefineCount.Clear();
            _refineQiBonus.Clear();
            _emRefineQiBonus.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            if (alive == null) return 0;
            int removed = 0;
            foreach (System.Collections.Generic.Dictionary<long, int> d in new System.Collections.Generic.Dictionary<long, int>[] { _refineLevel, _refineCount, _emRefineCount })
            {
                var dead = new List<long>();
                foreach (var kv in d)
                    if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
                foreach (var id in dead) { d.Remove(id); removed++; }
            }
            foreach (System.Collections.Generic.Dictionary<long, float> d in new System.Collections.Generic.Dictionary<long, float>[] { _refineQiBonus, _emRefineQiBonus })
            {
                var dead = new List<long>();
                foreach (var kv in d)
                    if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
                foreach (var id in dead) { d.Remove(id); removed++; }
            }
            return removed;
        }

        /// <summary>自动提炼tick：持有气力修炼法的未觉醒单位概率自动提炼（10%通用/5%机械师）</summary>
        public static void TickRefine()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!HasRefineMethod(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue;
                if (Random.value < 0.1f) TryRefine(a);
                if (a.hasTrait(SuperMechTraits.ClassMech) && Random.value < 0.05f) TryEmRefine(a);
            }
        }
    }
}
