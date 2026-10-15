using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 战斗效果系统（从CombatPatches拆分）
    /// 处理法术战斗效果和专精战斗效果
    /// </summary>
    public static class CombatEffects
    {
        /// <summary>法术战斗效果：攻击型法术额外伤害+防御型法术减伤</summary>
        public static void ApplySpellEffects(Actor attacker, Actor target, float pDamage)
        {
            if (attacker == null || target == null) return;
            if (!attacker.hasTrait(SuperMechTraits.ClassMage)) return;

            // 攻击型法术：20%概率释放，造成额外伤害
            if (Random.value < 0.20f)
            {
                string[] attackSpells = {
                    "sm_spell_fireball", "sm_spell_lightning_storm", "sm_spell_meteor_swarm",
                    "sm_spell_arcane_blast", "sm_spell_energy_beam", "sm_spell_evocation_storm",
                    "sm_spell_death_curse", "sm_spell_energy_bolt", "sm_spell_fire_bolt"
                };
                foreach (var sid in attackSpells)
                {
                    if (SuperMechSpell.IsLearned(attacker, sid))
                    {
                        var spell = SuperMechSpell.GetSpell(sid);
                        float bonus = spell != null ? 0.1f + spell.tier * 0.05f : 0.15f;
                        if (SuperMechKnowledge.HasUltimateKnowledge(attacker, "mage")) bonus *= 1.2f;
                        bonus *= SuperMechMageType.GetSpellPowerBonus(attacker, sid);
                        target.data.health -= (int)(pDamage * bonus);
                        break;
                    }
                }
            }

            // 防御型法术：目标学会后有概率减伤
            if (target.hasTrait(SuperMechTraits.ClassMage) && Random.value < 0.15f)
            {
                string[] defenseSpells = {
                    "sm_spell_minor_ward", "sm_spell_mage_armor", "sm_spell_antimagic_field",
                    "sm_spell_ice_shield"
                };
                foreach (var sid in defenseSpells)
                {
                    if (SuperMechSpell.IsLearned(target, sid))
                    {
                        var spell = SuperMechSpell.GetSpell(sid);
                        float reduce = spell != null ? 0.05f + spell.tier * 0.03f : 0.1f;
                        target.data.health += (int)(pDamage * reduce);
                        break;
                    }
                }
            }
        }

        /// <summary>专精战斗效果：枪炮师/械武者/机械师各专精的独有战斗机制</summary>
        public static void ApplySpecialtyEffects(Actor attacker, Actor target, float pDamage)
        {
            if (attacker == null || target == null) return;
            string spec = SuperMechSpecialty.GetSpecialty(attacker);
            if (spec == null) return;

            // === 枪炮师专精 ===
            if (spec == SuperMechSpecialty.SpecGunEagle)
            {
                if (Random.value < 0.20f)
                    target.data.health -= (int)(pDamage * 0.5f);
            }
            else if (spec == SuperMechSpecialty.SpecGunFire)
            {
                if (Random.value < 0.15f)
                    target.data.health -= (int)(pDamage * 0.4f);
            }
            else if (spec == SuperMechSpecialty.SpecGunDancer)
            {
                if (Random.value < 0.10f)
                    target.data.health -= (int)(pDamage * 1.0f);
            }
            // === 械武者专精 ===
            else if (spec == SuperMechSpecialty.SpecMartialWeapon)
            {
                if (Random.value < 0.15f)
                    target.data.health -= (int)(pDamage * 0.5f);
            }
            else if (spec == SuperMechSpecialty.SpecMartialFight)
            {
                if (Random.value < 0.20f)
                {
                    target.data.health -= (int)(pDamage * 0.3f);
                    target.addTrait("slow");
                }
            }
            else if (spec == SuperMechSpecialty.SpecMartialHeavy)
            {
                if (Random.value < 0.10f)
                    target.addTrait("stunned");
            }
            // === 机械师专精 ===
            else if (spec == SuperMechSpecialty.SpecMechArmed)
            {
                if (Random.value < 0.10f)
                    target.data.health -= (int)(pDamage * 0.3f);
            }
            else if (spec == SuperMechSpecialty.SpecMechEnergy)
            {
                if (SuperMechQi.GetQi(attacker) > SuperMechQi.GetQiMax(attacker) * 0.5f)
                    target.data.health -= (int)(pDamage * 0.1f);
            }
            else if (spec == SuperMechSpecialty.SpecMechVirtual)
            {
                if (Random.value < 0.15f)
                {
                    target.addTrait("slow");
                    target.data.health -= (int)(pDamage * 0.2f);
                }
            }
        }
    }
}
