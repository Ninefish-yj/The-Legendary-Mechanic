using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechAura
    {
        private static readonly Dictionary<long, float> _suppressed = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _stunned = new Dictionary<long, float>();
        private const float AuraRange = 4f;
        private const float SuppressDuration = 5f;

        public static void TickAura()
        {
            if (World.world == null) return;

            var auraSources = new List<Actor>();
            foreach (var actor in World.world.units)
            {
                if (actor == null || !actor.isAlive()) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(actor);
                if (rank >= 8)
                    auraSources.Add(actor);
            }

            if (auraSources.Count == 0)
            {
                CleanExpired();
                return;
            }

            foreach (var source in auraSources)
            {
                int sourceRank = SuperMechAdvancement.GetExactRankIndex(source);
                if (source.current_tile == null) continue;

                foreach (var target in World.world.units)
                {
                    if (target == null || !target.isAlive()) continue;
                    if (target.id == source.id) continue;
                    if (target.current_tile == null) continue;

                    float dist = Mathf.Abs(target.current_tile.x - source.current_tile.x)
                               + Mathf.Abs(target.current_tile.y - source.current_tile.y);
                    if (dist > AuraRange) continue;

                    int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                    int rankDiff = sourceRank - targetRank;

                    if (rankDiff <= 0) continue;

                    if (rankDiff >= 2)
                    {
                        _suppressed[target.id] = Time.time + SuppressDuration;
                    }

                    if (rankDiff >= 4)
                    {
                        float stunChance = Mathf.Min((rankDiff - 3) * 0.15f, 0.60f);
                        if (Random.value < stunChance * 0.1f)
                        {
                            _stunned[target.id] = Time.time + 2f;
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
            if (_suppressed.Count == 0 && _stunned.Count == 0) return;

            var affectedIds = new HashSet<long>();
            foreach (var id in _suppressed.Keys) affectedIds.Add(id);
            foreach (var id in _stunned.Keys) affectedIds.Add(id);

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
                    stats["speed"] *= 0.5f;
                    stats["damage"] *= 0.7f;
                    stats["attack_speed"] *= 0.6f;
                }
                if (stunned)
                {
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

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_suppressed, alive);
            removed += SuperMechCleanup.CleanDict(_stunned, alive);
            return removed;
        }
    }
}
