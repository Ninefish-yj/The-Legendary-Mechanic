using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>战斗统计与反馈系统（v0.182战斗深度）
    /// 记录击杀数/伤害输出/承受伤害/连杀数，提供战斗日志
    /// 原著依据：机械师战斗中需要精确的伤害统计和击杀记录</summary>
    public static class SuperMechCombatStats
    {
        /// <summary>单位战斗数据</summary>
        public class ActorCombatData
        {
            public int totalKills;           // 总击杀数
            public int killStreak;           // 当前连杀数
            public int maxKillStreak;        // 最高连杀数
            public float totalDamageDealt;   // 总造成伤害
            public float totalDamageTaken;   // 总承受伤害
            public int battlesWon;           // 战斗胜利次数
            public float lastDamageTime;     // 上次造成伤害时间
            public float lastKillTime;       // 上次击杀时间
            public List<string> recentKills = new List<string>(); // 最近击杀记录（最多10条）
        }

        private static readonly Dictionary<long, ActorCombatData> _stats = new Dictionary<long, ActorCombatData>();
        private static readonly List<string> _combatLog = new List<string>();
        private const int MaxLogEntries = 50;
        private const int MaxKillHistory = 10;

        /// <summary>获取单位战斗数据</summary>
        public static ActorCombatData GetStats(Actor a)
        {
            if (a == null) return null;
            if (!_stats.TryGetValue(a.id, out var data))
            {
                data = new ActorCombatData();
                _stats[a.id] = data;
            }
            return data;
        }

        /// <summary>记录造成伤害</summary>
        public static void RecordDamageDealt(Actor attacker, Actor target, float damage)
        {
            if (attacker == null || damage <= 0) return;
            var data = GetStats(attacker);
            data.totalDamageDealt += damage;
            data.lastDamageTime = Time.time;
        }

        /// <summary>记录承受伤害</summary>
        public static void RecordDamageTaken(Actor target, float damage)
        {
            if (target == null || damage <= 0) return;
            var data = GetStats(target);
            data.totalDamageTaken += damage;
        }

        /// <summary>记录击杀</summary>
        public static void RecordKill(Actor killer, Actor victim)
        {
            if (killer == null || victim == null) return;
            var data = GetStats(killer);
            data.totalKills++;
            data.killStreak++;
            data.maxKillStreak = Mathf.Max(data.maxKillStreak, data.killStreak);
            data.lastKillTime = Time.time;

            // 记录击杀历史
            string killEntry = $"{killer.data.name} 击杀 {victim.data.name}";
            data.recentKills.Insert(0, killEntry);
            if (data.recentKills.Count > MaxKillHistory)
                data.recentKills.RemoveAt(data.recentKills.Count - 1);

            // 全局战斗日志
            AddCombatLog(killEntry);

            // 连杀奖励（5连杀以上有额外经验）
            if (data.killStreak >= 5)
            {
                float bonusXp = data.killStreak * 10f;
                SuperMechAwakened.AddXp(killer, bonusXp);
            }
        }

        /// <summary>重置连杀（单位死亡时调用）</summary>
        public static void ResetKillStreak(Actor a)
        {
            if (a == null) return;
            var data = GetStats(a);
            data.killStreak = 0;
        }

        /// <summary>添加战斗日志</summary>
        public static void AddCombatLog(string message)
        {
            _combatLog.Insert(0, $"[{Time.time:F0}s] {message}");
            if (_combatLog.Count > MaxLogEntries)
                _combatLog.RemoveAt(_combatLog.Count - 1);
        }

        /// <summary>获取最近战斗日志</summary>
        public static List<string> GetRecentLogs(int count = 10)
        {
            int n = Mathf.Min(count, _combatLog.Count);
            return _combatLog.GetRange(0, n);
        }

        /// <summary>获取单位战斗评级（基于击杀数和伤害）</summary>
        public static string GetCombatRating(Actor a)
        {
            var data = GetStats(a);
            if (data == null) return "?";
            if (data.totalKills >= 100) return "SSS";
            if (data.totalKills >= 50) return "SS";
            if (data.totalKills >= 20) return "S";
            if (data.totalKills >= 10) return "A";
            if (data.totalKills >= 5) return "B";
            if (data.totalKills >= 1) return "C";
            return "D";
        }

        /// <summary>获取KDA（击杀/死亡/助攻）简化版</summary>
        public static string GetKDA(Actor a)
        {
            var data = GetStats(a);
            if (data == null) return "0/0/0";
            return $"{data.totalKills}/{data.battlesWon}/{(int)(data.totalDamageDealt / 100)}";
        }

        /// <summary>获取战斗数据摘要（用于UI显示）</summary>
        public static string GetSummary(Actor a)
        {
            var data = GetStats(a);
            if (data == null) return "";
            return $"击杀:{data.totalKills} 连杀:{data.killStreak} 最高连杀:{data.maxKillStreak}\n" +
                   $"造成伤害:{data.totalDamageDealt:F0} 承受伤害:{data.totalDamageTaken:F0}\n" +
                   $"评级:{GetCombatRating(a)} KDA:{GetKDA(a)}";
        }

        /// <summary>清除死亡单位的数据（可选，用于内存优化）</summary>
        public static void CleanupDead(HashSet<long> aliveIds)
        {
            var toRemove = new List<long>();
            foreach (var kv in _stats)
            {
                if (!aliveIds.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove)
                _stats.Remove(id);
        }

        /// <summary>清空所有数据</summary>
        public static void Clear()
        {
            _stats.Clear();
            _combatLog.Clear();
        }
    }
}
