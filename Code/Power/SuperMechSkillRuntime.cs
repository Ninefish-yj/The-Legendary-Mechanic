using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>技能运行时：冷却管理、战斗中自动触发、效果执行</summary>
    public static class SuperMechSkillRuntime
    {
        private static readonly Dictionary<long, Dictionary<string, float>> _cooldowns = new Dictionary<long, Dictionary<string, float>>();
        private static readonly Dictionary<long, Dictionary<string, float>> _activeBuffs = new Dictionary<long, Dictionary<string, float>>();
        private static readonly Dictionary<long, Dictionary<string, float>> _dotEffects = new Dictionary<long, Dictionary<string, float>>(); // v0.58.0 持续伤害
        private static readonly Dictionary<long, float> _shields = new Dictionary<long, float>(); // v0.58.0 护盾值

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
                SuperMechEventLogger.LogSkillCast(attacker, def);

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
                    Teleport(caster, target, def);
                    break;
            }
        }

        private static void DealSkillDamage(Actor caster, Actor target, SuperMechSkills.SkillDef def)
        {
            try
            {
                // 基于阶位计算基础伤害
                int rankIdx = SuperMechActorContextRegistry.GetRank(caster);
                float baseDamage = (10f + rankIdx * 20f) * def.effectValue;
                target.getHit(baseDamage, false, AttackType.None, caster);

                // v0.58.0 高阶技能范围伤害（effectValue>=3时，50%概率溅射周围敌人）
                if (def.effectValue >= 3f && Random.value < 0.5f && target.current_tile != null)
                {
                    var units = World.world.units?.units_only_alive;
                    if (units != null)
                    {
                        foreach (var nearby in units)
                        {
                            if (nearby == null || !nearby.isAlive() || nearby == target || nearby == caster) continue;
                            if (nearby.kingdom == caster.kingdom) continue; // 不打友军
                            if (nearby.current_tile == null) continue;
                            float dist = Vector2.Distance(
                                new Vector2(nearby.current_tile.x, nearby.current_tile.y),
                                new Vector2(target.current_tile.x, target.current_tile.y));
                            if (dist <= 2f)
                            {
                                nearby.getHit(baseDamage * 0.5f, false, AttackType.None, caster);
                            }
                        }
                    }
                }
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

            // v0.58.0 防御型Buff提供护盾
            if (IsDefensiveBuff(def.id))
            {
                int rankIdx = SuperMechActorContextRegistry.GetRank(target);
                float shield = (20f + rankIdx * 15f) * def.effectValue;
                if (!_shields.TryGetValue(target.id, out var current)) current = 0f;
                _shields[target.id] = current + shield;
            }
        }

        /// <summary>v0.58.0 防御型Buff判断（金刚身/魔法护盾/精神屏障）</summary>
        private static bool IsDefensiveBuff(string skillId)
        {
            return skillId == "sm_skill_vajra_body" ||
                   skillId == "sm_skill_magic_shield" ||
                   skillId == "sm_skill_mental_barrier";
        }

        /// <summary>v0.58.0 护盾吸收伤害，返回剩余伤害</summary>
        public static float AbsorbShield(Actor target, float damage)
        {
            if (target == null || damage <= 0) return damage;
            if (!_shields.TryGetValue(target.id, out var shield) || shield <= 0) return damage;
            if (shield >= damage)
            {
                _shields[target.id] = shield - damage;
                return 0f;
            }
            _shields[target.id] = 0f;
            return damage - shield;
        }

        private static void ApplyDebuff(Actor target, SuperMechSkills.SkillDef def)
        {
            if (target == null) return;
            try
            {
                target.addStatusEffect("stunned", def.effectDuration);
                // v0.58.0 持续伤害：眩晕期间每秒造成伤害
                int rankIdx = SuperMechActorContextRegistry.GetRank(target);
                float dotDamage = (5f + rankIdx * 5f) * def.effectValue;
                if (!_dotEffects.TryGetValue(target.id, out var dots))
                {
                    dots = new Dictionary<string, float>();
                    _dotEffects[target.id] = dots;
                }
                dots[def.id] = Time.time + def.effectDuration;
                // 存储伤害值（用skillId映射）
                if (!_dotDamageValues.ContainsKey(def.id)) _dotDamageValues[def.id] = dotDamage;
            }
            catch { }
        }

        private static readonly Dictionary<string, float> _dotDamageValues = new Dictionary<string, float>();

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

        /// <summary>召唤效果（v0.45.0 实装）：在施法者身边生成机械系召唤单位，阶位约低一阶，短寿命消散</summary>
        private static void SummonUnits(Actor caster, SuperMechSkills.SkillDef def)
        {
            try
            {
                if (World.world == null || World.world.units == null || caster == null) return;
                int count = Mathf.Clamp(Mathf.FloorToInt(def.effectValue), 1, 5);
                int rankIdx = SuperMechActorContextRegistry.GetRank(caster);
                for (int i = 0; i < count; i++)
                {
                    WorldTile tile = caster.current_tile;
                    if (tile == null) continue;
                    // 在施法者周围随机一格生成
                    if (tile.neighbours != null && tile.neighbours.Length > 0)
                        tile = tile.neighbours[Random.Range(0, tile.neighbours.Length)];

                    Actor summoned = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                    if (summoned == null) continue;

                    // 召唤物：机械系特质 + 战斗倾向 + 召唤物标记
                    summoned.addTrait(SuperMechTraits.ClassMech);
                    summoned.addTrait("aggressive");
                    summoned.addTrait("sm_summoned"); // v0.57.0 召唤物标记
                    // 记录召唤者ID（用ActorContext）
                    var ctx = SuperMechActorContextRegistry.Get(summoned);
                    if (ctx != null) ctx.summonerId = caster.id;
                    // 阶位约低一阶（使徒级机械，非独立超能者）
                    int minionRank = Mathf.Max(0, rankIdx - 1);
                    if (minionRank < SuperMechRanks.All.Count)
                        SuperMechAdvancement.SetExactRank(summoned, minionRank);
                    // 气力按召唤者缩放
                    float qiBase = 300f + rankIdx * 150f;
                    SuperMechQi.SetQiMax(summoned, qiBase);
                    SuperMechQi.SetQi(summoned, qiBase);
                    // 存在时间有限：设定短寿命，随时间自然消散
                    if (summoned.stats != null && def.effectDuration > 0f)
                        summoned.stats["lifespan"] = Mathf.Max(1f, def.effectDuration / 30f);
                }
            }
            catch (System.Exception e)
            {
                if (SuperMechConfig.LogVerbose) Debug.LogWarning($"[超神机械师] 召唤技能异常: {e.Message}");
            }
        }

        /// <summary>瞬移效果（v0.45.0 实装）：有目标时瞬移到目标身边，无目标时小范围闪避，获得短暂无敌</summary>
        private static void Teleport(Actor caster, Actor target, SuperMechSkills.SkillDef def)
        {
            try
            {
                if (caster == null) return;
                if (target != null && target.isAlive() && target.current_tile != null)
                {
                    caster.moveTo(target.current_tile);
                }
                else if (caster.current_tile != null && caster.current_tile.neighbours != null && caster.current_tile.neighbours.Length > 0)
                {
                    caster.moveTo(caster.current_tile.neighbours[Random.Range(0, caster.current_tile.neighbours.Length)]);
                }
                caster.addStatusEffect("invincible", def.effectDuration);
            }
            catch (System.Exception e)
            {
                if (SuperMechConfig.LogVerbose) Debug.LogWarning($"[超神机械师] 瞬移技能异常: {e.Message}");
            }
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

            // v0.58.0 持续伤害处理
            var dotDead = new List<long>();
            foreach (var kvp in _dotEffects)
            {
                var expired = new List<string>();
                foreach (var dot in kvp.Value)
                {
                    if (now >= dot.Value) { expired.Add(dot.Key); continue; }
                    // 每秒造成伤害（用时间差简化为每tick一次）
                    if (World.world != null && World.world.units != null)
                    {
                        var target = World.world.units.get(kvp.Key);
                        if (target != null && target.isAlive() && _dotDamageValues.TryGetValue(dot.Key, out var dmg))
                        {
                            target.getHit(dmg * 0.1f, false, AttackType.None, null); // 每tick10%伤害
                        }
                    }
                }
                foreach (var id in expired) kvp.Value.Remove(id);
                if (kvp.Value.Count == 0) dotDead.Add(kvp.Key);
            }
            foreach (var id in dotDead) _dotEffects.Remove(id);
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
