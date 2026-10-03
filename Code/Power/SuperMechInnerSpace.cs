using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.47.0 超神内空间机制（原著第1430章：超神级放出内空间）
    /// 原著：超神级（X阶）体内形成内空间，战斗时放出内空间投影，内空间内主人拥有绝对优势
    /// 高阶内空间压制低阶内空间，同阶内空间互相抵消
    /// </summary>
    public static class SuperMechInnerSpace
    {
        private static readonly Dictionary<long, DomainState> _activeDomains = new Dictionary<long, DomainState>();

        public class DomainState
        {
            public int rank;           // 放出内空间时的阶位
            public float expireTime;   // 内空间结束时间
            public int activateCount;  // 本场战斗展开次数
        }

        /// <summary>尝试放出内空间（受击或攻击时调用）</summary>
        public static void TryActivate(Actor a)
        {
            if (!SuperMechConfig.InnerSpaceEnabled) return;
            if (a == null || !a.isAlive()) return;
            int rank = SuperMechActorContextRegistry.GetRank(a);
            if (rank < SuperMechConfig.InnerSpaceMinRank) return;

            // 已有活跃内空间则刷新持续时间
            if (_activeDomains.TryGetValue(a.id, out var existing) && existing.expireTime > Time.time)
            {
                existing.expireTime = Time.time + SuperMechConfig.InnerSpaceDuration;
                return;
            }

            // 按阶位计算放出概率：X阶100%（原著超神级可随意放出内空间），SS=50%
            float chance = 0.5f;
            if (rank >= 13) chance = 1.0f;
            else if (rank >= 12) chance = 0.5f;

            if (Random.value > chance) return;

            _activeDomains[a.id] = new DomainState
            {
                rank = rank,
                expireTime = Time.time + SuperMechConfig.InnerSpaceDuration,
                activateCount = (existing?.activateCount ?? 0) + 1
            };

            // 内空间名称不按职业区分（原著：韩萧的虚空领域是个人特殊属性，非职业共性）
            Debug.Log($"[超神机械师] {a.name} 放出内空间，展开领域！阶位{rank}，持续{SuperMechConfig.InnerSpaceDuration}秒");
        }

        /// <summary>获取攻击者在内空间中的伤害倍率</summary>
        public static float GetAttackMultiplier(Actor attacker, Actor target)
        {
            if (!SuperMechConfig.InnerSpaceEnabled) return 1f;
            if (attacker == null || target == null) return 1f;

            bool atkDomain = IsActive(attacker);
            bool defDomain = IsActive(target);

            // 双方都有内空间：比较阶位，高阶压制低阶
            if (atkDomain && defDomain)
            {
                int atkRank = _activeDomains[attacker.id].rank;
                int defRank = _activeDomains[target.id].rank;
                if (atkRank > defRank)
                {
                    // 攻击者内空间压制：获得伤害加成
                    return 1f + GetDomainDamageBonus(atkRank) * 0.5f;
                }
                else if (atkRank < defRank)
                {
                    // 防御者内空间压制：攻击者伤害降低
                    return 1f - GetDomainDamagePenalty(defRank) * 0.5f;
                }
                // 同阶：内空间互相抵消，无加成
                return 1f;
            }

            // 只有攻击者有内空间
            if (atkDomain)
            {
                return 1f + GetDomainDamageBonus(_activeDomains[attacker.id].rank);
            }

            // 只有防御者有内空间：攻击者伤害降低
            if (defDomain)
            {
                return 1f - GetDomainDamagePenalty(_activeDomains[target.id].rank);
            }

            return 1f;
        }

        /// <summary>获取防御者在内空间中的减伤倍率</summary>
        public static float GetDefenseMultiplier(Actor target, Actor attacker)
        {
            if (!SuperMechConfig.InnerSpaceEnabled) return 1f;
            if (target == null || attacker == null) return 1f;

            bool defDomain = IsActive(target);
            bool atkDomain = IsActive(attacker);

            if (defDomain && atkDomain)
            {
                int defRank = _activeDomains[target.id].rank;
                int atkRank = _activeDomains[attacker.id].rank;
                if (defRank > atkRank)
                {
                    // 防御者内空间压制：获得减伤
                    return 1f - GetDomainDamageReduction(defRank) * 0.5f;
                }
                return 1f; // 同阶或被压制，无减伤
            }

            if (defDomain)
            {
                return 1f - GetDomainDamageReduction(_activeDomains[target.id].rank);
            }

            return 1f;
        }

        /// <summary>内空间伤害加成（按阶位，原著：内空间内主人获得主场优势）</summary>
        private static float GetDomainDamageBonus(int rank)
        {
            float baseVal = SuperMechConfig.InnerSpaceDamageBonus;
            if (rank >= 13) return baseVal * 2f;   // X阶：双倍
            if (rank >= 12) return baseVal * 1.5f; // SS阶：1.5倍
            return baseVal;
        }

        /// <summary>内空间对敌人的伤害惩罚（按阶位，原著：内空间压制对手）</summary>
        private static float GetDomainDamagePenalty(int rank)
        {
            float baseVal = SuperMechConfig.InnerSpaceEnemyPenalty;
            if (rank >= 13) return Mathf.Min(baseVal * 2f, 0.5f);
            if (rank >= 12) return baseVal * 1.5f;
            return baseVal;
        }

        /// <summary>内空间减伤（按阶位，原著：内空间内主人减伤）</summary>
        private static float GetDomainDamageReduction(int rank)
        {
            float baseVal = SuperMechConfig.InnerSpaceDamageReduction;
            if (rank >= 13) return Mathf.Min(baseVal * 2f, 0.5f);
            if (rank >= 12) return baseVal * 1.5f;
            return baseVal;
        }

        /// <summary>单位是否有活跃内空间</summary>
        public static bool IsActive(Actor a)
        {
            if (a == null) return false;
            if (!_activeDomains.TryGetValue(a.id, out var state)) return false;
            if (state.expireTime < Time.time)
            {
                _activeDomains.Remove(a.id);
                return false;
            }
            return true;
        }

        /// <summary>清理过期内空间</summary>
        public static void CleanExpired()
        {
            float now = Time.time;
            var toRemove = new List<long>();
            foreach (var kv in _activeDomains)
                if (kv.Value.expireTime < now) toRemove.Add(kv.Key);
            foreach (var id in toRemove) _activeDomains.Remove(id);
        }

        /// <summary>范围脉冲：放出内空间展开领域，将周围区域变作内维度环境，范围内敌人持续受压制伤害（原著第1430章）</summary>
        private static int _pulseTickCounter = 0;
        public static void TickPulse()
        {
            if (!SuperMechConfig.InnerSpaceEnabled) return;
            _pulseTickCounter++;
            if (_pulseTickCounter < SuperMechFormulas.InnerSpacePulseInterval) return;
            _pulseTickCounter = 0;

            CleanExpired();
            if (_activeDomains.Count == 0) return;
            if (World.world == null || World.world.units == null) return;

            var allUnits = World.world.units.units_only_alive;
            if (allUnits == null) return;

            foreach (var kv in _activeDomains)
            {
                Actor caster = null;
                foreach (var u in allUnits) if (u != null && u.id == kv.Key) { caster = u; break; }
                if (caster == null || !caster.isAlive() || caster.current_tile == null) continue;

                int cx = caster.current_tile.x;
                int cy = caster.current_tile.y;
                float maxHp = 0f;
                try { maxHp = caster.getMaxHealth(); } catch { }
                if (maxHp <= 0f) continue;

                float dmg = maxHp * SuperMechFormulas.InnerSpacePulseFraction;
                if (kv.Value.rank >= 13) dmg *= 1.5f;
                else if (kv.Value.rank >= 12) dmg *= 1.2f;
                if (dmg <= 0f) continue;

                foreach (var enemy in allUnits)
                {
                    if (enemy == null || !enemy.isAlive() || enemy.current_tile == null) continue;
                    if (enemy.id == caster.id) continue;
                    if (enemy.kingdom != null && caster.kingdom != null && enemy.kingdom.id == caster.kingdom.id) continue;
                    int dist = Mathf.Abs(enemy.current_tile.x - cx) + Mathf.Abs(enemy.current_tile.y - cy);
                    if (dist > SuperMechFormulas.InnerSpacePulseRange) continue;
                    try { enemy.getHit(dmg, true, AttackType.Other, caster); } catch { }
                    // 标记敌人处于领域内，施加负面效果（原著：领域带来负面影响）
                    _domainSuppressed[enemy.id] = Time.time + 2f;
                }
            }
        }

        /// <summary>领域内敌人的负面效果（减速+减攻速），每tick调用</summary>
        private static readonly Dictionary<long, float> _domainSuppressed = new Dictionary<long, float>();
        public static void TickDomainEffects()
        {
            if (_domainSuppressed.Count == 0) return;
            if (World.world == null || World.world.units == null) return;

            float now = Time.time;
            var toRemove = new List<long>();
            foreach (var kv in _domainSuppressed)
                if (kv.Value < now) toRemove.Add(kv.Key);
            foreach (var id in toRemove) _domainSuppressed.Remove(id);
            if (_domainSuppressed.Count == 0) return;

            foreach (var actor in World.world.units)
            {
                if (actor == null || !actor.isAlive()) continue;
                if (!_domainSuppressed.ContainsKey(actor.id)) continue;
                try
                {
                    actor.stats["speed"] *= SuperMechFormulas.InnerSpaceEnemySpeedMul;
                    actor.stats["attack_speed"] *= SuperMechFormulas.InnerSpaceEnemyAttackSpeedMul;
                } catch { }
            }
        }

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _activeDomains.Remove(a.id);
            _domainSuppressed.Remove(a.id);
        }

        public static void Clear()
        {
            _activeDomains.Clear();
            _domainSuppressed.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_activeDomains, alive);
            removed += SuperMechCleanup.CleanDict(_domainSuppressed, alive);
            return removed;
        }
    }
}
