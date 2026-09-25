using System.Collections.Generic;
using UnityEngine;
using NeoModLoader.services;

namespace SuperMech.Code
{
    /// <summary>
    /// 气势震慑系统（原著ch378/ch562/ch618/ch797/ch1006）
    /// 高阶级超能者释放生命层次威压，震慑低阶单位。
    /// ch378："犹如霸王色霸气一样的气势压迫"
    /// ch797："释放生命层次的威压，狠狠压向哈蒙一行人，企图震慑这群人"
    /// ch1006：超A级气势下马威
    /// 效果：低阶单位减速、减攻、有概率眩晕；同阶/高阶免疫。
    /// </summary>
    public static class SuperMechAura
    {
        // 被震慑的单位：id -> 震慑结束时间
        private static readonly Dictionary<long, float> _suppressed = new Dictionary<long, float>();
        // 被眩晕的单位：id -> 眩晕结束时间
        private static readonly Dictionary<long, float> _stunned = new Dictionary<long, float>();
        // 震慑范围（格）
        private const float AuraRange = 4f;
        // 震慑持续时间（秒）
        private const float SuppressDuration = 5f;

        /// <summary>每tick处理气势震慑：A阶以上单位对周围低阶单位施加震慑。</summary>
        public static void TickAura()
        {
            if (World.world == null) return;

            // 收集所有A阶以上的气势源
            var auraSources = new List<Actor>();
            foreach (var actor in World.world.units)
            {
                if (actor == null || !actor.isAlive()) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(actor);
                if (rank >= 8) // A阶（天灾级）以上释放气势
                    auraSources.Add(actor);
            }

            if (auraSources.Count == 0)
            {
                // 没有气势源，清理过期效果
                CleanExpired();
                return;
            }

            // 对每个气势源，检查周围低阶单位
            foreach (var source in auraSources)
            {
                int sourceRank = SuperMechAdvancement.GetExactRankIndex(source);
                if (source.current_tile == null) continue;

                // 获取周围单位（遍历所有单位检查距离）
                foreach (var target in World.world.units)
                {
                    if (target == null || !target.isAlive()) continue;
                    if (target.id == source.id) continue;
                    if (target.current_tile == null) continue;

                    // 距离检查
                    float dist = Mathf.Abs(target.current_tile.x - source.current_tile.x)
                               + Mathf.Abs(target.current_tile.y - source.current_tile.y);
                    if (dist > AuraRange) continue;

                    int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                    int rankDiff = sourceRank - targetRank;

                    // 同阶或高阶免疫
                    if (rankDiff <= 0) continue;

                    // 震慑：阶位差距≥2
                    if (rankDiff >= 2)
                    {
                        _suppressed[target.id] = Time.time + SuppressDuration;
                    }

                    // 眩晕：阶位差距≥4，概率=(差距-3)*15%，上限60%
                    if (rankDiff >= 4)
                    {
                        float stunChance = Mathf.Min((rankDiff - 3) * 0.15f, 0.60f);
                        if (Random.value < stunChance * 0.1f) // 每tick 10%概率触发
                        {
                            _stunned[target.id] = Time.time + 2f; // 眩晕2秒
                            Debug.Log($"[超神机械师]【气势震慑】{source.name}({SuperMechRanks.All[sourceRank].name}) 震慑眩晕 {target.name}(阶位差{rankDiff})");
                        }
                    }
                }
            }

            CleanExpired();
            ApplyEffects();
        }

        private static void CleanExpired()
        {
            float now = Time.time;
            var toRemove = new List<long>();
            foreach (var kv in _suppressed)
                if (kv.Value < now) toRemove.Add(kv.Key);
            foreach (var id in toRemove) _suppressed.Remove(id);

            toRemove.Clear();
            foreach (var kv in _stunned)
                if (kv.Value < now) toRemove.Add(kv.Key);
            foreach (var id in toRemove) _stunned.Remove(id);
        }

        private static void ApplyEffects()
        {
            // 只遍历被震慑/眩晕的单位（从字典取），不遍历全场
            if (_suppressed.Count == 0 && _stunned.Count == 0) return;

            // 收集所有受影响的id
            var affectedIds = new HashSet<long>();
            foreach (var id in _suppressed.Keys) affectedIds.Add(id);
            foreach (var id in _stunned.Keys) affectedIds.Add(id);

            // 在全场单位中找到这些id（WorldBox单位数通常<1000，可接受）
            foreach (var actor in World.world.units)
            {
                if (actor == null || !actor.isAlive()) continue;
                if (!affectedIds.Contains(actor.id)) continue;

                var stats = SuperMechStats.Of(actor);
                if (stats == null) continue;

                bool suppressed = _suppressed.ContainsKey(actor.id);
                bool stunned = _stunned.ContainsKey(actor.id);

                if (suppressed)
                {
                    // 震慑：速度×0.5，攻击×0.7
                    stats["speed"] *= 0.5f;
                    stats["damage"] *= 0.7f;
                    stats["attack_speed"] *= 0.6f;
                }
                if (stunned)
                {
                    // 眩晕：无法行动（速度降为0）
                    stats["speed"] = 0f;
                    stats["damage"] *= 0.1f;
                }
            }
        }

        public static bool IsSuppressed(Actor a)
        {
            if (a == null) return false;
            return _suppressed.ContainsKey(a.id);
        }

        public static bool IsStunned(Actor a)
        {
            if (a == null) return false;
            return _stunned.ContainsKey(a.id);
        }

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _suppressed.Remove(a.id);
            _stunned.Remove(a.id);
        }

        public static void Clear()
        {
            _suppressed.Clear();
            _stunned.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_suppressed, alive);
            removed += SuperMechCleanup.CleanDict(_stunned, alive);
            return removed;
        }
    }
}
