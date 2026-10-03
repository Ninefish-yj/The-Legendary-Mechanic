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
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < SuperMechConfig.InnerSpaceMinRank) return;

            // 已有活跃内空间则刷新持续时间
            if (_activeDomains.TryGetValue(a.id, out var existing) && existing.expireTime > Time.time)
            {
                existing.expireTime = Time.time + SuperMechConfig.InnerSpaceDuration;
                return;
            }

            // 按阶位计算放出概率：S=30%, SS=50%, X=80%
            float chance = 0.3f;
            if (rank >= 13) chance = 0.8f;
            else if (rank >= 12) chance = 0.5f;
            else if (rank >= 11) chance = 0.35f;

            if (Random.value > chance) return;

            _activeDomains[a.id] = new DomainState
            {
                rank = rank,
                expireTime = Time.time + SuperMechConfig.InnerSpaceDuration,
                activateCount = (existing?.activateCount ?? 0) + 1
            };

            Debug.Log($"[超神机械师] {a.name} 放出内空间！阶位{rank}，持续{SuperMechConfig.InnerSpaceDuration}秒");
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

        /// <summary>内空间伤害加成（按阶位）</summary>
        private static float GetDomainDamageBonus(int rank)
        {
            float baseVal = SuperMechConfig.InnerSpaceDamageBonus;
            if (rank >= 13) return baseVal * 2f;   // X阶：双倍
            if (rank >= 12) return baseVal * 1.5f; // SS阶：1.5倍
            return baseVal;                         // S/S+阶：基础
        }

        /// <summary>内空间对敌人的伤害惩罚（按阶位）</summary>
        private static float GetDomainDamagePenalty(int rank)
        {
            float baseVal = SuperMechConfig.InnerSpaceEnemyPenalty;
            if (rank >= 13) return Mathf.Min(baseVal * 2f, 0.5f);
            if (rank >= 12) return baseVal * 1.5f;
            return baseVal;
        }

        /// <summary>内空间减伤（按阶位）</summary>
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

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _activeDomains.Remove(a.id);
        }

        public static void Clear()
        {
            _activeDomains.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_activeDomains, alive);
        }
    }
}
