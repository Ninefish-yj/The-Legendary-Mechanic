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

            string spaceName = GetInnerSpaceName(a);
            Debug.Log($"[超神机械师] {a.name} 放出{spaceName}！阶位{rank}，持续{SuperMechConfig.InnerSpaceDuration}秒");
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
                    return 1f + GetDomainDamageBonus(atkRank, attacker) * 0.5f;
                }
                else if (atkRank < defRank)
                {
                    // 防御者内空间压制：攻击者伤害降低
                    return 1f - GetDomainDamagePenalty(defRank, target) * 0.5f;
                }
                // 同阶：内空间互相抵消，无加成
                return 1f;
            }

            // 只有攻击者有内空间
            if (atkDomain)
            {
                return 1f + GetDomainDamageBonus(_activeDomains[attacker.id].rank, attacker);
            }

            // 只有防御者有内空间：攻击者伤害降低
            if (defDomain)
            {
                return 1f - GetDomainDamagePenalty(_activeDomains[target.id].rank, target);
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
                    return 1f - GetDomainDamageReduction(defRank, target) * 0.5f;
                }
                return 1f; // 同阶或被压制，无减伤
            }

            if (defDomain)
            {
                return 1f - GetDomainDamageReduction(_activeDomains[target.id].rank, target);
            }

            return 1f;
        }

        /// <summary>内空间伤害加成（按阶位+职业差异化，原著：每个人内空间属性不同）</summary>
        private static float GetDomainDamageBonus(int rank, Actor a = null)
        {
            float baseVal = SuperMechConfig.InnerSpaceDamageBonus;
            if (rank >= 13) baseVal *= 2f;   // X阶：双倍
            else if (rank >= 12) baseVal *= 1.5f; // SS阶：1.5倍
            // 职业差异化：机械系/魔法师偏伤害，武道系偏穿透
            if (a != null)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech)) baseVal *= 1.3f;   // 虚空内空间：高伤害
                else if (a.hasTrait(SuperMechTraits.ClassMage)) baseVal *= 1.2f; // 元素内空间：法术伤害
                else if (a.hasTrait(SuperMechTraits.ClassMartial)) baseVal *= 1.15f; // 武道内空间
            }
            return baseVal;
        }

        /// <summary>内空间对敌人的伤害惩罚（按阶位+职业差异化）</summary>
        private static float GetDomainDamagePenalty(int rank, Actor a = null)
        {
            float baseVal = SuperMechConfig.InnerSpaceEnemyPenalty;
            if (rank >= 13) baseVal = Mathf.Min(baseVal * 2f, 0.5f);
            else if (rank >= 12) baseVal *= 1.5f;
            // 职业差异化：念力系偏压制敌人
            if (a != null)
            {
                if (a.hasTrait(SuperMechTraits.ClassMind)) baseVal *= 1.3f; // 精神内空间：强压制
            }
            return baseVal;
        }

        /// <summary>内空间减伤（按阶位+职业差异化）</summary>
        private static float GetDomainDamageReduction(int rank, Actor a = null)
        {
            float baseVal = SuperMechConfig.InnerSpaceDamageReduction;
            if (rank >= 13) baseVal = Mathf.Min(baseVal * 2f, 0.5f);
            else if (rank >= 12) baseVal *= 1.5f;
            // 职业差异化：异能系偏生存减伤
            if (a != null)
            {
                if (a.hasTrait(SuperMechTraits.ClassPsi)) baseVal *= 1.3f; // 基因内空间：强减伤
            }
            return baseVal;
        }

        /// <summary>获取职业对应的内空间名称（原著：每个人内空间属性不同）</summary>
        public static string GetInnerSpaceName(Actor a)
        {
            if (a == null) return "内空间";
            if (a.hasTrait(SuperMechTraits.ClassMech)) return "虚空内空间";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "精神内空间";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "基因内空间";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "武道内空间";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "元素内空间";
            return "内空间";
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

        /// <summary>范围脉冲：对所有活跃内空间，脉冲范围内敌人（参考天人武道领域系统，原著：内空间将周围区域变作内维度环境）</summary>
        private static int _pulseTickCounter = 0;
        public static void TickPulse()
        {
            if (!SuperMechConfig.InnerSpaceEnabled) return;
            _pulseTickCounter++;
            if (_pulseTickCounter < SuperMechConfig.InnerSpacePulseInterval) return;
            _pulseTickCounter = 0;

            CleanExpired();
            if (_activeDomains.Count == 0) return;
            if (World.world == null || World.world.units == null) return;

            int radius = SuperMechConfig.InnerSpacePulseRadius;
            var allUnits = World.world.units.units_only_alive;
            if (allUnits == null) return;

            foreach (var kv in _activeDomains)
            {
                Actor caster = null;
                foreach (var u in allUnits) if (u != null && u.id == kv.Key) { caster = u; break; }
                if (caster == null || !caster.isAlive() || caster.current_tile == null) continue;

                int cx = caster.current_tile.x;
                int cy = caster.current_tile.y;

                foreach (var enemy in allUnits)
                {
                    if (enemy == null || !enemy.isAlive() || enemy.current_tile == null) continue;
                    if (enemy.id == caster.id) continue;
                    if (enemy.kingdom != null && caster.kingdom != null && enemy.kingdom.id == caster.kingdom.id) continue;

                    int dist = Mathf.Abs(enemy.current_tile.x - cx) + Mathf.Abs(enemy.current_tile.y - cy);
                    if (dist > radius) continue;

                    PulseEnemy(caster, enemy, kv.Value.rank);
                }
            }
        }

        /// <summary>脉冲单个敌人：按职业差异化效果</summary>
        private static void PulseEnemy(Actor caster, Actor enemy, int rank)
        {
            if (enemy == null || !enemy.isAlive()) return;

            // 基础脉冲伤害：攻击者最大生命的比例
            float maxHp = 0f;
            try { maxHp = caster.getMaxHealth(); } catch { }
            if (maxHp <= 0f) return;

            float dmg = maxHp * SuperMechConfig.InnerSpacePulseDamageFraction;
            // 阶位加成
            if (rank >= 13) dmg *= 1.5f;
            else if (rank >= 12) dmg *= 1.2f;

            // 职业差异化脉冲效果
            if (caster.hasTrait(SuperMechTraits.ClassMech))
            {
                // 虚空内空间：高伤害脉冲
                dmg *= 1.3f;
            }
            else if (caster.hasTrait(SuperMechTraits.ClassMind))
            {
                // 精神内空间：伤害+降低敌人智力
                try { enemy.stats["intelligence"] = Mathf.Max(0f, enemy.stats["intelligence"] - 1f); } catch { }
            }
            else if (caster.hasTrait(SuperMechTraits.ClassPsi))
            {
                // 基因内空间：伤害+吸血
                dmg *= 1.1f;
                if (caster.isAlive() && caster.data != null)
                {
                    try { caster.data.health = Mathf.Min(caster.getMaxHealth(), caster.data.health + (int)(dmg * 0.3f)); } catch { }
                }
            }
            else if (caster.hasTrait(SuperMechTraits.ClassMage))
            {
                // 元素内空间：伤害+抽蓝
                dmg *= 1.2f;
                try { int mana = enemy.getMana(); enemy.data.mana = Mathf.Max(0, mana - (int)(dmg * 0.5f)); } catch { }
            }
            else if (caster.hasTrait(SuperMechTraits.ClassMartial))
            {
                // 武道内空间：纯伤害
                dmg *= 1.15f;
            }

            if (dmg <= 0f) return;
            try { enemy.getHit(dmg, true, AttackType.Other, caster); } catch { }
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
