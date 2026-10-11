using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 法术运行时系统：处理法术效果执行、AI自动施法、冷却管理
    /// 对照原著：魔法师职业的法术体系，超能者通过知识学习法术并在战斗中使用
    /// </summary>
    public static class SuperMechSpellRuntime
    {
        // 法术冷却记录：actorId -> (spellId -> 下次可施放时间)
        private static readonly Dictionary<long, Dictionary<string, double>> _cooldowns = new Dictionary<long, Dictionary<string, double>>();

        // 全局冷却（施法间隔，防止连续施法）
        private const float GLOBAL_COOLDOWN = 1.5f;

        // 上次施法时间
        private static readonly Dictionary<long, double> _lastCastTime = new Dictionary<long, double>();

        /// <summary>
        /// AI自动施法：在战斗中选择并释放合适的法术
        /// 由CombatPatches在攻击前调用
        /// </summary>
        public static bool TryAutoCast(Actor caster, Actor target)
        {
            if (caster == null || target == null || caster.data == null) return false;
            if (!caster.isAlive() || !target.isAlive()) return false;

            // 检查是否有学会的法术
            var learnedSpells = SuperMechSpell.GetLearnedSpells(caster);
            if (learnedSpells == null || learnedSpells.Count == 0) return false;

            // 全局冷却检查
            double now = World.world.getCurWorldTime();
            if (_lastCastTime.TryGetValue(caster.id, out double lastCast) && (now - lastCast) < GLOBAL_COOLDOWN)
                return false;

            // 选择合适的法术（按优先级：控制 > 爆发 > 普通伤害 > 防御）
            string bestSpell = SelectBestSpell(caster, target, learnedSpells);
            if (string.IsNullOrEmpty(bestSpell)) return false;

            // 冷却检查
            if (IsOnCooldown(caster.id, bestSpell)) return false;

            // 执行法术
            bool success = CastSpell(caster, target, bestSpell);
            if (success)
            {
                _lastCastTime[caster.id] = now;
                SetCooldown(caster.id, bestSpell, GetSpellCooldown(bestSpell));
            }
            return success;
        }

        /// <summary>
        /// 选择最合适的法术
        /// </summary>
        private static string SelectBestSpell(Actor caster, Actor target, List<string> learnedSpells)
        {
            float targetHpPct = target.data.health / Mathf.Max(1f, target.data.max_health);
            float casterHpPct = caster.data.health / Mathf.Max(1f, caster.data.max_health);
            float casterInt = SuperMechStats.Of(caster)["intelligence"];

            // 低血量时优先防御
            if (casterHpPct < 0.3f)
            {
                foreach (var sid in learnedSpells)
                {
                    if (IsDefensiveSpell(sid)) return sid;
                }
            }

            // 目标低血量时优先爆发
            if (targetHpPct < 0.4f)
            {
                foreach (var sid in learnedSpells)
                {
                    if (IsBurstSpell(sid) && casterInt >= GetIntRequirement(sid)) return sid;
                }
            }

            // 优先控制法术
            foreach (var sid in learnedSpells)
            {
                if (IsControlSpell(sid) && casterInt >= GetIntRequirement(sid)) return sid;
            }

            // 普通伤害法术（选伤害最高的）
            string bestDamage = null;
            float bestDmg = 0f;
            foreach (var sid in learnedSpells)
            {
                if (IsDamageSpell(sid) && casterInt >= GetIntRequirement(sid))
                {
                    float dmg = GetSpellBaseDamage(sid);
                    if (dmg > bestDmg)
                    {
                        bestDmg = dmg;
                        bestDamage = sid;
                    }
                }
            }
            if (bestDamage != null) return bestDamage;

            // 最后随便选一个
            return learnedSpells.Count > 0 ? learnedSpells[0] : null;
        }

        /// <summary>
        /// 执行法术效果
        /// </summary>
        public static bool CastSpell(Actor caster, Actor target, string spellId)
        {
            try
            {
                var spell = SuperMechSpell.GetSpell(spellId);
                if (spell == null) return false;

                float intBonus = 1f + SuperMechStats.Of(caster)["intelligence"] * 0.02f;
                float mysteryBonus = 1f + SuperMechStats.Of(caster)["mystery"] * 0.03f;

                switch (spellId)
                {
                    // === 元素系 ===
                    case "sm_spell_fire_bolt":
                        DealSpellDamage(caster, target, 8f * intBonus, AttackType.Fire);
                        return true;
                    case "sm_spell_fireball":
                        DealSpellDamage(caster, target, 20f * intBonus, AttackType.Fire);
                        return true;
                    case "sm_spell_ice_shield":
                        ApplyBuff(caster, "sm_spell_ice_shield", 10f, 5f, 15f); // 护甲+10，持续15秒
                        return true;
                    case "sm_spell_lightning_storm":
                        DealSpellDamage(caster, target, 45f * intBonus, AttackType.Acid);
                        return true;
                    case "sm_spell_meteor_swarm":
                        DealSpellDamage(caster, target, 80f * intBonus, AttackType.Fire);
                        return true;

                    // === 秘术系 ===
                    case "sm_spell_arcane_missile":
                        DealSpellDamage(caster, target, 10f * mysteryBonus, AttackType.Other);
                        return true;
                    case "sm_spell_dispel":
                        // 移除目标的增益效果
                        RemoveRandomBuff(target);
                        return true;
                    case "sm_spell_arcane_blast":
                        DealSpellDamage(caster, target, 70f * mysteryBonus, AttackType.Other);
                        return true;

                    // === 庇护系 ===
                    case "sm_spell_minor_ward":
                        ApplyBuff(caster, "sm_spell_minor_ward", 5f, 0f, 20f);
                        return true;
                    case "sm_spell_mage_armor":
                        ApplyBuff(caster, "sm_spell_mage_armor", 15f, 0f, 30f);
                        return true;
                    case "sm_spell_antimagic_field":
                        ApplyBuff(caster, "sm_spell_antimagic_field", 25f, 0f, 20f);
                        return true;

                    // === 时空系 ===
                    case "sm_spell_blink":
                        // 闪现：短暂无敌+位移（简化为闪避增益）
                        ApplyBuff(caster, "sm_spell_blink", 0f, 0f, 3f, dodgeBonus: 0.5f);
                        return true;
                    case "sm_spell_time_slow":
                        // 时间减缓：降低目标攻速和移速
                        ApplyDebuff(target, "sm_spell_time_slow", 0f, 0f, 8f, speedMul: 0.5f);
                        return true;
                    case "sm_spell_time_stop":
                        // 时间停止：目标定身（简化为大量减速）
                        ApplyDebuff(target, "sm_spell_time_stop", 0f, 0f, 5f, speedMul: 0.1f);
                        return true;

                    // === 塑能系 ===
                    case "sm_spell_energy_bolt":
                        DealSpellDamage(caster, target, 12f * intBonus, AttackType.Explosion);
                        return true;
                    case "sm_spell_energy_beam":
                        DealSpellDamage(caster, target, 35f * intBonus, AttackType.Explosion);
                        return true;
                    case "sm_spell_evocation_storm":
                        DealSpellDamage(caster, target, 65f * intBonus, AttackType.Explosion);
                        return true;

                    // === 召唤系（简化：暂时只加buff，后续接入自定义生物生成）===
                    case "sm_spell_summon_familiar":
                        ApplyBuff(caster, "sm_spell_summon_familiar", 3f, 5f, 30f);
                        return true;
                    case "sm_spell_summon_elemental":
                        ApplyBuff(caster, "sm_spell_summon_elemental", 8f, 12f, 30f);
                        return true;
                    case "sm_spell_summon_elemental_lord":
                        ApplyBuff(caster, "sm_spell_summon_elemental_lord", 15f, 25f, 45f);
                        return true;

                    // === 诅咒系 ===
                    case "sm_spell_minor_curse":
                        ApplyDebuff(target, "sm_spell_minor_curse", 0f, 5f, 15f); // 伤害-5
                        return true;
                    case "sm_spell_blessing":
                        ApplyBuff(caster, "sm_spell_blessing", 0f, 8f, 20f); // 伤害+8
                        return true;
                    case "sm_spell_death_curse":
                        ApplyDebuff(target, "sm_spell_death_curse", 0f, 20f, 10f); // 伤害-20
                        DealSpellDamage(caster, target, 30f * mysteryBonus, AttackType.Poison);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 法术释放异常 {spellId}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 造成法术伤害
        /// </summary>
        private static void DealSpellDamage(Actor caster, Actor target, float damage, AttackType type)
        {
            if (target == null || !target.isAlive()) return;
            // 法术伤害无视部分护甲（魔法穿透）
            float finalDmg = Mathf.Max(1f, damage * 0.8f);
            target.getHit(finalDmg, true, type, caster, false, false, false);
        }

        /// <summary>
        /// 应用增益效果（通过特质模拟）
        /// </summary>
        private static void ApplyBuff(Actor target, string buffId, float armorBonus, float damageBonus, float duration, float dodgeBonus = 0f)
        {
            if (target == null) return;
            var stats = SuperMechStats.Of(target);
            if (stats == null) return;

            // 简化实现：直接修改属性，持续时间由字典管理
            if (armorBonus > 0) stats["armor"] += armorBonus;
            if (damageBonus > 0) stats["damage"] += damageBonus;

            // 记录buff以便过期移除
            SuperMechBuffSystem.AddBuff(target, buffId, armorBonus, damageBonus, duration, dodgeBonus);
        }

        /// <summary>
        /// 应用减益效果
        /// </summary>
        private static void ApplyDebuff(Actor target, string debuffId, float armorPenalty, float damagePenalty, float duration, float speedMul = 1f)
        {
            if (target == null) return;
            var stats = SuperMechStats.Of(target);
            if (stats == null) return;

            if (armorPenalty > 0) stats["armor"] = Mathf.Max(0f, stats["armor"] - armorPenalty);
            if (damagePenalty > 0) stats["damage"] = Mathf.Max(0f, stats["damage"] - damagePenalty);
            if (speedMul < 1f) stats["speed"] *= speedMul;

            SuperMechBuffSystem.AddDebuff(target, debuffId, armorPenalty, damagePenalty, duration, speedMul);
        }

        /// <summary>
        /// 移除目标随机增益
        /// </summary>
        private static void RemoveRandomBuff(Actor target)
        {
            if (target == null) return;
            SuperMechBuffSystem.RemoveRandomBuff(target);
        }

        // === 冷却管理 ===

        private static bool IsOnCooldown(long actorId, string spellId)
        {
            if (!_cooldowns.TryGetValue(actorId, out var cds)) return false;
            if (!cds.TryGetValue(spellId, out double nextCast)) return false;
            return World.world.getCurWorldTime() < nextCast;
        }

        private static void SetCooldown(long actorId, string spellId, float cooldown)
        {
            if (!_cooldowns.TryGetValue(actorId, out var cds))
            {
                cds = new Dictionary<string, double>();
                _cooldowns[actorId] = cds;
            }
            cds[spellId] = World.world.getCurWorldTime() + cooldown;
        }

        private static float GetSpellCooldown(string spellId)
        {
            var spell = SuperMechSpell.GetSpell(spellId);
            if (spell == null) return 3f;
            // 高阶法术冷却更长
            return 2f + spell.tier * 1.5f;
        }

        private static float GetIntRequirement(string spellId)
        {
            var spell = SuperMechSpell.GetSpell(spellId);
            return spell?.intRequirement ?? 0f;
        }

        private static float GetSpellBaseDamage(string spellId)
        {
            switch (spellId)
            {
                case "sm_spell_fire_bolt": return 8f;
                case "sm_spell_fireball": return 20f;
                case "sm_spell_lightning_storm": return 45f;
                case "sm_spell_meteor_swarm": return 80f;
                case "sm_spell_arcane_missile": return 10f;
                case "sm_spell_arcane_blast": return 70f;
                case "sm_spell_energy_bolt": return 12f;
                case "sm_spell_energy_beam": return 35f;
                case "sm_spell_evocation_storm": return 65f;
                case "sm_spell_death_curse": return 30f;
                default: return 5f;
            }
        }

        // === 法术分类 ===

        private static bool IsDamageSpell(string spellId)
        {
            return spellId.Contains("fire") || spellId.Contains("lightning") || spellId.Contains("meteor") ||
                   spellId.Contains("arcane") || spellId.Contains("energy") || spellId.Contains("evocation") ||
                   spellId == "sm_spell_death_curse";
        }

        private static bool IsBurstSpell(string spellId)
        {
            return spellId == "sm_spell_meteor_swarm" || spellId == "sm_spell_arcane_blast" ||
                   spellId == "sm_spell_evocation_storm" || spellId == "sm_spell_lightning_storm";
        }

        private static bool IsDefensiveSpell(string spellId)
        {
            return spellId.Contains("shield") || spellId.Contains("ward") || spellId.Contains("armor") ||
                   spellId.Contains("antimagic") || spellId == "sm_spell_blink";
        }

        private static bool IsControlSpell(string spellId)
        {
            return spellId.Contains("time") || spellId.Contains("curse") || spellId == "dispel" ||
                   spellId == "sm_spell_blink";
        }

        /// <summary>
        /// 清理死亡单位的冷却数据
        /// </summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var toRemove = new List<long>();
            foreach (var id in _cooldowns.Keys)
            {
                if (!alive.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove)
            {
                _cooldowns.Remove(id);
                _lastCastTime.Remove(id);
                removed++;
            }
            return removed;
        }

        /// <summary>
        /// 全局清理
        /// </summary>
        public static void Clear()
        {
            _cooldowns.Clear();
            _lastCastTime.Clear();
        }
    }
}
