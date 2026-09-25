using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 进阶任务系统（原著ch48/ch50：20级进阶任务完成后才能转职）。
    ///
    /// 参考主神空间ZhaKeReincarnatorTask实现：
    /// - 降临者达到阶段等级上限后，自动触发进阶任务
    /// - 生成一个同阶对手（属性=降临者属性×1.2），传送到同一格强制互相仇恨
    /// - 击杀对手=完成进阶任务，可以转职
    /// - 超时（10游戏年）=任务失败，需要重新触发
    /// - 任务奖励：大量经验+气力+潜能点
    ///
    /// 原著ch50："20级进阶任务完成！检测到【机械入门者】转职条件已完成"
    /// </summary>
    public static class SuperMechAdvancementTask
    {
        // ActorData持久键
        private const string TaskActiveKey = "sm_task_active";     // 是否有进行中的任务
        private const string TaskTargetKey = "sm_task_target";     // 对手的data.id
        private const string TaskStartTimeKey = "sm_task_start";   // 任务开始时间
        private const string TaskTierKey = "sm_task_tier";         // 任务级别（1-5对应D-S）

        // 任务级别名称
        private static readonly string[] TierNames = { "D级", "C级", "B级", "A级", "S级" };
        // 任务级别概率（D 50%、C 25%、B 15%、A 7%、S 3%）
        private static readonly float[] TierChances = { 0.50f, 0.25f, 0.15f, 0.07f, 0.03f };
        // 任务级别属性系数（D 0.8、C 0.9、B 1.0、A 1.1、S 1.2）
        private static readonly float[] TierFactors = { 0.8f, 0.9f, 1.0f, 1.1f, 1.2f };
        // 任务级别经验奖励倍数
        private static readonly float[] TierXpMult = { 1.0f, 1.5f, 2.0f, 3.0f, 5.0f };

        // 任务时限（游戏年）
        private const double TaskTimeoutYears = 10.0;
        // 一游戏年=60世界时间单位
        private const double YearUnits = 60.0;

        // 进行中的任务对手追踪（target.id -> owner.id）
        private static readonly Dictionary<long, long> _taskPairs = new Dictionary<long, long>();

        public static void Register()
        {
            Debug.Log("[超神机械师] 进阶任务系统注册完成（原著ch48/ch50：进阶任务完成后才能转职）");
        }

        /// <summary>随机抽取任务级别。</summary>
        private static int RollTier()
        {
            float r = Random.value;
            float acc = 0f;
            for (int i = 0; i < TierChances.Length; i++)
            {
                acc += TierChances[i];
                if (r <= acc) return i;
            }
            return 0;
        }

        /// <summary>检查单位是否有进行中的进阶任务。</summary>
        public static bool HasActiveTask(Actor a)
        {
            if (a == null || a.data == null) return false;
            return a.data.Get(TaskActiveKey, "0") == "1";
        }

        /// <summary>触发进阶任务（达到阶段等级上限时自动调用）。</summary>
        public static bool TryStartTask(Actor a)
        {
            if (a == null || !SuperMechAwakened.IsAwakened(a)) return false;
            if (HasActiveTask(a)) return false;
            if (!SuperMechAwakened.CanAdvanceStage(a)) return false;

            int stage = SuperMechStage.GetStage(a);
            if (stage >= 14) return false; // 超神阶段无任务

            // 抽取任务级别
            int tier = RollTier();
            float factor = TierFactors[tier];

            // 生成对手（在降临者所在格）
            WorldTile tile = a.currentTile;
            if (tile == null) return false;

            Actor target = World.world.units.createNewUnit(
                "human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (target == null) return false;

            // 对手属性=降临者属性×级别系数
            var ownerStats = SuperMechStats.Of(a);
            var targetStats = SuperMechStats.Of(target);
            if (ownerStats != null && targetStats != null)
            {
                string[] statKeys = { "strength", "dexterity", "endurance", "intelligence", "warfare" };
                foreach (string key in statKeys)
                {
                    float v = ownerStats[key];
                    targetStats[key] = Mathf.Max(1f, v * factor);
                }
            }

            // 对手挂敌对特质（疯狂+好战）
            target.addTrait("aggressive");
            target.addTrait("bloodlust");

            // 对手标记为任务目标
            target.data.Set(TaskTargetKey, a.data.id.ToString());
            target.data.Set("sm_task_npc", "1");

            // 降临者标记任务进行中
            a.data.Set(TaskActiveKey, "1");
            a.data.Set(TaskTargetKey, target.data.id.ToString());
            a.data.Set(TaskStartTimeKey, World.world.worldTime.ToString());
            a.data.Set(TaskTierKey, tier.ToString());

            // 追踪配对
            _taskPairs[target.data.id] = a.data.id;

            // 强制互相仇恨
            try
            {
                a.startFightingWith(target);
                target.startFightingWith(a);
            }
            catch { }

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 触发{TierNames[tier]}进阶任务（阶段{stage}→{stage + 1}），对手生成！");

            return true;
        }

        /// <summary>Tick：检查任务状态（对手死亡=完成，超时=失败）。</summary>
        public static void TickTasks()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            // 收集需要处理的降临者
            List<Actor> toCheck = new List<Actor>();
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAwakened.IsAwakened(a)) continue;
                if (!HasActiveTask(a)) continue;
                toCheck.Add(a);
            }

            double currentTime = World.world.worldTime;

            foreach (Actor a in toCheck)
            {
                // 检查超时
                string startStr = a.data.Get(TaskStartTimeKey, "0");
                double startTime = 0;
                double.TryParse(startStr, out startTime);
                double elapsed = (currentTime - startTime) / YearUnits;

                if (elapsed >= TaskTimeoutYears)
                {
                    // 任务超时失败
                    FailTask(a);
                    continue;
                }

                // 检查对手是否死亡
                string targetIdStr = a.data.Get(TaskTargetKey, "0");
                long targetId = 0;
                long.TryParse(targetIdStr, out targetId);
                Actor target = null;
                if (targetId > 0)
                {
                    target = World.world.units.get(targetId);
                }

                if (target == null || !target.isAlive())
                {
                    // 对手死亡=任务完成
                    CompleteTask(a);
                }
                else
                {
                    // 维持仇恨（距离过远拉回）
                    float dist = MapBox.getDistance(a.currentPosition, target.currentPosition);
                    if (dist > 6f)
                    {
                        try
                        {
                            target.currentPosition = a.currentPosition;
                            a.startFightingWith(target);
                            target.startFightingWith(a);
                        }
                        catch { }
                    }
                }
            }

            // 清理任务NPC（非任务对手的NPC）
            List<long> toRemove = new List<long>();
            foreach (var kv in _taskPairs)
            {
                Actor npc = World.world.units.get(kv.Key);
                if (npc == null || !npc.isAlive())
                {
                    toRemove.Add(kv.Key);
                }
            }
            foreach (long id in toRemove)
            {
                _taskPairs.Remove(id);
            }
        }

        /// <summary>完成进阶任务。</summary>
        private static void CompleteTask(Actor a)
        {
            string tierStr = a.data.Get(TaskTierKey, "0");
            int tier = 0;
            int.TryParse(tierStr, out tier);

            // 任务奖励：经验+气力+潜能点
            int stage = SuperMechStage.GetStage(a);
            float baseXp = SuperMechAwakened.GetXpNeeded(1, stage) * 5f; // 5级经验
            float xpReward = baseXp * TierXpMult[tier];
            float qiReward = 50f * (tier + 1);
            int potReward = 2 + tier;

            SuperMechAwakened.AddXp(a, xpReward);
            SuperMechQi.AddQiMax(a, qiReward);
            SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
            SuperMechPotential.AddPotential(a, potReward);

            // 清除任务标记
            ClearTaskMarkers(a);

            // 自动转职（任务完成=转职条件达成）
            SuperMechAwakened.TryAdvanceStage(a);

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 完成{TierNames[tier]}进阶任务！奖励：经验+{xpReward:F0}，气力+{qiReward}，潜能点+{potReward}");
        }

        /// <summary>任务失败（超时）。</summary>
        private static void FailTask(Actor a)
        {
            // 清除对手
            string targetIdStr = a.data.Get(TaskTargetKey, "0");
            long targetId = 0;
            long.TryParse(targetIdStr, out targetId);
            if (targetId > 0)
            {
                Actor target = World.world.units.get(targetId);
                if (target != null && target.isAlive())
                {
                    target.data.health = 0;
                    target.updateStats();
                }
                _taskPairs.Remove(targetId);
            }

            ClearTaskMarkers(a);

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 进阶任务超时失败，需要重新触发");
        }

        /// <summary>清除任务标记。</summary>
        private static void ClearTaskMarkers(Actor a)
        {
            a.data.Set(TaskActiveKey, "0");
            a.data.Set(TaskTargetKey, "0");
            a.data.Set(TaskStartTimeKey, "0");
            a.data.Set(TaskTierKey, "0");
        }

        /// <summary>获取任务状态文本（用于单位面板）。</summary>
        public static string GetTaskStatus(Actor a)
        {
            if (!HasActiveTask(a)) return null;
            string tierStr = a.data.Get(TaskTierKey, "0");
            int tier = 0;
            int.TryParse(tierStr, out tier);
            string startStr = a.data.Get(TaskStartTimeKey, "0");
            double startTime = 0;
            double.TryParse(startStr, out startTime);
            double elapsed = (World.world.worldTime - startTime) / YearUnits;
            double remaining = TaskTimeoutYears - elapsed;
            return $"{TierNames[tier]}进阶任务（剩余{remaining:F1}年）";
        }

        /// <summary>清空所有任务数据（世界切换）。</summary>
        public static void Clear()
        {
            _taskPairs.Clear();
        }
    }
}
