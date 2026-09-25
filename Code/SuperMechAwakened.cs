using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 降临者（玩家）等级职业体系（原著ch3/ch48）。
    ///
    /// 【双轨制设计】
    /// - 土著（星海人）：只有阶位体系（F→X），无职业等级，靠修炼水到渠成
    /// - 降临者（玩家）：有面板，职业等级+经验值+进阶任务转职+潜能点
    ///
    /// 原著设定：
    /// - 主职业：机械入门者Lv1（0/200），练满经验升级
    /// - 20级进阶任务→第一次转职（选分支）
    /// - 每个职业阶段有等级上限：见习15级、神座60级
    /// - 转职后新职业从Lv1开始，经验需求暴涨
    /// - 升级获得：气力+属性点+潜能点
    /// </summary>
    public static class SuperMechAwakened
    {
        // 降临者标记特质
        public const string AwakenedTrait = "sm_awakened";

        // 职业等级数据：actorId -> (level, xp)
        private static readonly Dictionary<long, int> _level = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _xp = new Dictionary<long, float>();

        // 每个职业阶段的等级上限（原著参考）
        public static readonly int[] StageLevelCaps = {
            20,   // 1.入门者
            40,   // 2.学徒
            15,   // 3.见习
            60,   // 4.磁环
            60,   // 5.数据
            60,   // 6.战争
            60,   // 7.虚拟
            60,   // 8.星海
            60,   // 9.真理
            60,   // 10.使徒
            60,   // 11.帝皇
            60,   // 12.主宰
            60,   // 13.神座
            999   // 14.超神（无上限）
        };

        // 升级经验需求（指数增长，参考原著0/200→0/50000）
        public static float GetXpNeeded(int level, int stage)
        {
            // 基础200，每级×1.15，每阶段×2.5
            float baseXp = 200f * Mathf.Pow(1.15f, level) * Mathf.Pow(2.5f, stage);
            return Mathf.Round(baseXp);
        }

        // 升级奖励（参考原著）
        public static readonly float[] LevelQiReward = { 10f, 30f, 50f, 70f, 100f, 150f, 200f, 300f, 400f, 450f, 500f, 500f, 500f, 500f };
        public static readonly int[] LevelStatPoints = { 2, 3, 3, 4, 4, 5, 5, 8, 10, 15, 15, 20, 20, 20 };
        public static readonly int[] LevelPotentialPoints = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        public static void Register()
        {
            var t = new ActorTrait
            {
                id = AwakenedTrait,
                path_icon = "ui/Icons/actor_traits/iconClone",
                group_id = "sm_awakened",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            t.base_stats["experience"] = 2.0f; // 降临者经验获取×2
            t.base_stats["intelligence"] = 5;
            AssetManager.traits.add(t);
            LocalizedTextManager.add("trait_" + AwakenedTrait, "降临者", pReplace: true);
            LocalizedTextManager.add("trait_" + AwakenedTrait + "_info",
                "携带玩家面板的降临者，拥有职业等级与经验值体系，需完成进阶任务转职。土著则只有阶位体系。", pReplace: true);
            Debug.Log("[超神机械师] 降临者体系注册完成");
        }

        /// <summary>是否降临者（有面板）。</summary>
        public static bool IsAwakened(Actor a)
        {
            return a != null && a.hasTrait(AwakenedTrait);
        }

        /// <summary>获取职业等级。</summary>
        public static int GetLevel(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            if (_level.TryGetValue(a.data.id, out int lv)) return lv;
            return 1; // 降临者初始1级
        }

        /// <summary>获取当前经验值。</summary>
        public static float GetXp(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            if (_xp.TryGetValue(a.data.id, out float xp)) return xp;
            return 0;
        }

        /// <summary>直接设置降临者等级（存档恢复用）。</summary>
        public static void SetLevel(Actor a, int level)
        {
            if (a == null || !IsAwakened(a)) return;
            _level[a.data.id] = Mathf.Max(1, level);
        }

        /// <summary>直接设置经验值（存档恢复用）。</summary>
        public static void SetXp(Actor a, float xp)
        {
            if (a == null || !IsAwakened(a)) return;
            _xp[a.data.id] = Mathf.Max(0, xp);
        }

        /// <summary>添加经验值，自动升级。</summary>
        public static void AddXp(Actor a, float amount)
        {
            if (a == null || !IsAwakened(a) || amount <= 0) return;
            float curXp = GetXp(a) + amount;
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0) stage = 1;
            int cap = StageLevelCaps[stage - 1];

            while (lv < cap)
            {
                float needed = GetXpNeeded(lv, stage);
                if (curXp < needed) break;
                curXp -= needed;
                lv++;
                // 升级奖励
                int idx = Mathf.Clamp(stage - 1, 0, LevelQiReward.Length - 1);
                SuperMechQi.AddQiMax(a, LevelQiReward[idx]); // 升级提升气力上限
                SuperMechPotential.AddPotential(a, LevelPotentialPoints[idx]);
                Debug.Log($"[超神机械师] {a.name} 升级到Lv{lv}（阶段{stage}）");
            }

            if (lv >= cap)
            {
                // 满级，经验溢出保留，等待转职
                curXp = Mathf.Min(curXp, GetXpNeeded(lv, stage));
            }

            _level[a.data.id] = lv;
            _xp[a.data.id] = curXp;
        }

        /// <summary>是否可以转职（达到阶段等级上限）。</summary>
        public static bool CanAdvanceStage(Actor a)
        {
            if (!IsAwakened(a)) return false;
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0 || stage >= 14) return false;
            return lv >= StageLevelCaps[stage - 1];
        }

        /// <summary>转职（降临者专用，需达到等级上限）。</summary>
        public static bool TryAdvanceStage(Actor a)
        {
            if (!CanAdvanceStage(a)) return false;
            SuperMechStage.Advance(a);
            // 转职后等级重置为1，经验清零
            _level[a.data.id] = 1;
            _xp[a.data.id] = 0;
            // 第一次转职（阶段1→2）自动选分支（AI按系随机选最优分支）
            int newStage = SuperMechStage.GetStage(a);
            if (newStage == 2)
            {
                AutoSelectBranch(a);
            }
            Debug.Log($"[超神机械师] {a.name} 转职到阶段{newStage}：{SuperMechStage.GetStageName(a)}");
            return true;
        }

        /// <summary>获取等级显示文本。</summary>
        public static string GetLevelText(Actor a)
        {
            if (!IsAwakened(a)) return "";
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0) return $"Lv{lv}";
            int cap = StageLevelCaps[stage - 1];
            float xp = GetXp(a);
            float needed = GetXpNeeded(lv, stage);
            return $"Lv{lv}/{cap}（{xp:F0}/{needed:F0}）";
        }

        /// <summary>清除数据（单位死亡时）。</summary>
        public static void Clear() { _level.Clear(); _xp.Clear(); }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _level.Remove(a.data.id);
            _xp.Remove(a.data.id);
        }

        /// <summary>
        /// 降临者自动成长AI（WorldBox玩家是神，不需要逐个单位手动操作）。
        /// 降临者有面板优势，AI自动按最优路线成长：
        /// 1. 自动转职（达到等级上限）
        /// 2. 自动分配潜能点（按分支优先解锁知识）
        /// 3. 自动加神性蜕变点数（优先职业路线）
        /// 4. 自动尝试超神突破（条件满足时）
        /// 玩家仍可用神权干预，但不需要微操。
        /// </summary>
        public static void TickAutoPlay()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!IsAwakened(a)) continue;

                // 1. 自动转职（达到等级上限）
                if (CanAdvanceStage(a))
                {
                    TryAdvanceStage(a);
                }

                // 2. 自动分配潜能点（解锁知识树）
                int pot = SuperMechPotential.GetPotential(a);
                if (pot >= 2)
                {
                    AutoUnlockKnowledge(a);
                }

                // 3. 自动加神性蜕变点数（优先职业路线）
                int divPoints = SuperMechDivinity.GetPoints(a);
                if (divPoints >= SuperMechDivinity.PointsPerLayer)
                {
                    int prof = SuperMechDivinity.GetProfLayers(a);
                    int spec = SuperMechDivinity.GetSpeciesLayers(a);
                    // 优先职业路线，平衡发展
                    if (prof <= spec && prof < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "profession");
                    else if (spec < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "species");
                    else if (prof < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "profession");
                }

                // 4. 自动尝试超神突破（三条件全满足时才尝试，否则不浪费超神遗力）
                if (SuperMechTranscendence.CanAttempt(a) && SuperMechTranscendence.AllConditionsMet(a))
                {
                    // 有30%概率尝试，避免所有单位同时突破
                    if (Random.value < 0.3f)
                    {
                        SuperMechTranscendence.AttemptTranscend(a);
                    }
                }
            }
        }

        /// <summary>AI自动解锁知识树节点（按分支优先）。</summary>
        private static void AutoUnlockKnowledge(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            int unlocked = SuperMechPotential.GetUnlockedCount(a);
            string nodeId = $"{prefix}_{unlocked + 1}";
            int cost = 2;
            // 转职后其他分支费用×3（ch611），AI只解锁本系知识
            SuperMechPotential.UnlockNode(a, nodeId, cost);
        }

        /// <summary>AI自动选择分支（第一次转职时）。</summary>
        private static void AutoSelectBranch(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            var branches = SuperMechBranch.GetBranchesForClass(
                cls == "机械系" ? SuperMechTraits.ClassMech :
                cls == "武道系" ? SuperMechTraits.ClassMartial :
                cls == "异能系" ? SuperMechTraits.ClassPsi :
                cls == "魔法系" ? SuperMechTraits.ClassMage :
                SuperMechTraits.ClassMind);
            if (branches == null || branches.Count == 0) return;
            // 随机选一个分支（AI随机，玩家可用神权覆盖）
            var b = branches[Random.Range(0, branches.Count)];
            a.addTrait(b.traitId);
            Debug.Log($"[超神机械师] {a.name} 自动选择分支：{b.name}");
        }

        /// <summary>Tick：降临者获取经验（战斗中加速，非战斗缓慢获取）。</summary>
        public static void TickXp()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsAwakened(a)) continue;
                // 基础经验：每tick +5（模拟时间流逝/修炼）
                float xpGain = 5f;
                // 受伤状态判定为战斗中，额外+20
                if (a.data != null && a.data.health < a.getMaxHealth() * 0.95f)
                    xpGain += 20f;
                AddXp(a, xpGain);
            }
        }
    }
}
