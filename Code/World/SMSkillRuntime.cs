using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>技能运行时：冷却管理、战斗中自动触发、效果执行</summary>
    public static class SMSkillRuntime
    {
        private static readonly Dictionary<long, Dictionary<string, float>> _cooldowns = new Dictionary<long, Dictionary<string, float>>();
        private static readonly Dictionary<long, Dictionary<string, float>> _activeBuffs = new Dictionary<long, Dictionary<string, float>>();

        /// <summary>战斗中尝试释放技能，返回是否释放了技能</summary>
        public static bool TryCastSkill(Actor attacker, Actor target)
        {
            if (attacker == null || target == null || !attacker.isAlive()) return false;
            var learned = SuperMechSkills.GetLearned(attacker);
            if (learned == null || learned.Count == 0) return false;

            foreach (var def in learned)
            {
                if (def == null || def.type != SuperMechSkills.SkillType.Active) continue;
                if (IsOnCooldown(attacker.id, def.id)) continue;
                if (def.qiCost > 0 && SuperMechQi.GetQi(attacker) < def.qiCost) continue;

                // 消耗气力
                if (def.qiCost > 0)
                    SuperMechQi.SetQi(attacker, SuperMechQi.GetQi(attacker) - def.qiCost);

                // 设置冷却
                SetCooldown(attacker.id, def.id, def.cooldown);

                // 执行效果
                ExecuteEffect(attacker, target, def);

                // 事件日志
                SMEventLogger.LogSkillCast(attacker, def);

                return true;
            }
            return false;
        }

        private static void ExecuteEffect(Actor caster, Actor target, SuperMechSkills.SkillDef def)
        {
            switch (def.effectType)
            {
                case SuperMechSkills.SkillEffectType.Damage:
                    DealSkillDamage(caster, target, def);
                    break;
                case SuperMechSkills.SkillEffectType.Buff:
                    ApplyBuff(caster, def);
                    break;
                case SuperMechSkills.SkillEffectType.Debuff:
                    ApplyDebuff(target, def);
                    break;
                case SuperMechSkills.SkillEffectType.Heal:
                    HealTarget(caster, def);
                    break;
                case SuperMechSkills.SkillEffectType.Summon:
                    SummonUnits(caster, def);
                    break;
                case SuperMechSkills.SkillEffectType.Teleport:
                    Teleport(caster, def);
                    break;
            }
        }

        private static void DealSkillDamage(Actor caster, Actor target, SuperMechSkills.SkillDef def)
        {
            try
            {
                // 基于阶位计算基础伤害
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(caster);
                float baseDamage = (10f + rankIdx * 20f) * def.effectValue;
                target.getHit(baseDamage, false, AttackType.None, caster);
            }
            catch { }
        }

        private static void ApplyBuff(Actor target, SuperMechSkills.SkillDef def)
        {
            if (target == null) return;
            if (!_activeBuffs.TryGetValue(target.id, out var buffs))
            {
                buffs = new Dictionary<string, float>();
                _activeBuffs[target.id] = buffs;
            }
            buffs[def.id] = Time.time + def.effectDuration;
        }

        private static void ApplyDebuff(Actor target, SuperMechSkills.SkillDef def)
        {
            if (target == null) return;
            try
            {
                target.addStatusEffect("stunned", def.effectDuration);
            }
            catch { }
        }

        private static void HealTarget(Actor target, SuperMechSkills.SkillDef def)
        {
            if (target == null) return;
            try
            {
                float heal = target.getMaxHealth() * def.effectValue;
                target.data.health = Mathf.Min(target.getMaxHealth(), target.data.health + (int)heal);
            }
            catch { }
        }

        private static void SummonUnits(Actor caster, SuperMechSkills.SkillDef def)
        {
            // 简化版：召唤机械单位（后续扩展）
            try
            {
                int count = Mathf.FloorToInt(def.effectValue);
                for (int i = 0; i < count; i++)
                {
                    // WorldBox召唤单位API（简化处理）
                }
            }
            catch { }
        }

        private static void Teleport(Actor caster, SuperMechSkills.SkillDef def)
        {
            // 简化版：短暂无敌
            try
            {
                caster.addStatusEffect("invincible", def.effectDuration);
            }
            catch { }
        }

        public static bool IsOnCooldown(long unitId, string skillId)
        {
            if (!_cooldowns.TryGetValue(unitId, out var cds)) return false;
            if (!cds.TryGetValue(skillId, out var endTime)) return false;
            return Time.time < endTime;
        }

        public static float GetCooldownRemaining(long unitId, string skillId)
        {
            if (!_cooldowns.TryGetValue(unitId, out var cds)) return 0f;
            if (!cds.TryGetValue(skillId, out var endTime)) return 0f;
            return Mathf.Max(0f, endTime - Time.time);
        }

        private static void SetCooldown(long unitId, string skillId, float cooldown)
        {
            if (!_cooldowns.TryGetValue(unitId, out var cds))
            {
                cds = new Dictionary<string, float>();
                _cooldowns[unitId] = cds;
            }
            cds[skillId] = Time.time + cooldown;
        }

        public static bool HasActiveBuff(long unitId, string skillId)
        {
            if (!_activeBuffs.TryGetValue(unitId, out var buffs)) return false;
            if (!buffs.TryGetValue(skillId, out var endTime)) return false;
            return Time.time < endTime;
        }

        public static void Tick()
        {
            // 清理过期的buff和冷却
            var now = Time.time;
            var deadUnits = new List<long>();

            foreach (var kvp in _activeBuffs)
            {
                var expired = new List<string>();
                foreach (var buff in kvp.Value)
                {
                    if (now >= buff.Value) expired.Add(buff.Key);
                }
                foreach (var id in expired) kvp.Value.Remove(id);
                if (kvp.Value.Count == 0) deadUnits.Add(kvp.Key);
            }
            foreach (var id in deadUnits) _activeBuffs.Remove(id);
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_cooldowns, alive);
            removed += SuperMechCleanup.CleanDict(_activeBuffs, alive);
            return removed;
        }

        public static void Clear() { _cooldowns.Clear(); _activeBuffs.Clear(); }
    }
}
