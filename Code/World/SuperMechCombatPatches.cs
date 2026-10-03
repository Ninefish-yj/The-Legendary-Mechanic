using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch]
    public static class SuperMechCombatPatches
    {
        private static bool _prefixErrorLogged;
        private static bool _postfixErrorLogged;
        private static readonly HashSet<AttackType> PhysicalAttacks = new HashSet<AttackType>
        {
            AttackType.Weapon,
            AttackType.Eaten,
            AttackType.Explosion,
            AttackType.Gravity,
            AttackType.None
        };

        /// <summary>能级压制等级枚举（用于UI显示和日志）</summary>
        public enum SuppressionLevel
        {
            None = 0,        // 无压制（能级比<1.1）
            Minor = 1,       // 轻微压制（1.1~1.5）
            Moderate = 2,    // 明显压制（1.5~2）
            Strong = 3,      // 强烈压制（2~5）
            Overwhelm = 4,   // 碾压（5~10）
            Annihilate = 5   // 秒杀级（>10）
        }

        /// <summary>获取能级压制等级</summary>
        public static SuppressionLevel GetSuppressionLevel(float attackerEnergy, float defenderEnergy)
        {
            if (attackerEnergy <= 0 || defenderEnergy <= 0) return SuppressionLevel.None;
            float ratio = attackerEnergy / defenderEnergy;
            if (ratio >= 10f) return SuppressionLevel.Annihilate;
            if (ratio >= 5f) return SuppressionLevel.Overwhelm;
            if (ratio >= 2f) return SuppressionLevel.Strong;
            if (ratio >= 1.5f) return SuppressionLevel.Moderate;
            if (ratio >= 1.1f) return SuppressionLevel.Minor;
            return SuppressionLevel.None;
        }

        /// <summary>获取能级压制描述（用于UI/日志）</summary>
        public static string GetSuppressionDescription(SuppressionLevel level)
        {
            switch (level)
            {
                case SuppressionLevel.Minor: return LocalizedTextManager.getText("sm_combat_suppress_minor");
                case SuppressionLevel.Moderate: return LocalizedTextManager.getText("sm_combat_suppress_moderate");
                case SuppressionLevel.Strong: return LocalizedTextManager.getText("sm_combat_suppress_strong");
                case SuppressionLevel.Overwhelm: return LocalizedTextManager.getText("sm_combat_suppress_overwhelm");
                case SuppressionLevel.Annihilate: return LocalizedTextManager.getText("sm_combat_suppress_annihilate");
                default: return LocalizedTextManager.getText("sm_combat_suppress_none");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.getHit))]
        [HarmonyPriority(Priority.Low)]
        public static bool Actor_GetHit_Prefix(
            Actor __instance,
            float pDamage,
            bool pFlash,
            AttackType pAttackType,
            BaseSimObject pAttacker = null,
            bool pSkipIfShake = true,
            bool pMetallicWeapon = false,
            bool pCheckDamageReduction = true)
        {
            Actor target = __instance;
            if (target?.data == null || !target.isAlive()) return true;

            try
            {
                Actor attacker = pAttacker as Actor;

                // v0.48.0 同势力不互攻，v0.50.1 联盟势力不互攻
                if (attacker != null && SuperMechConfig.FactionEnabled &&
                    (SuperMechFaction.IsSameFaction(attacker, target) ||
                     SuperMechFaction.GetRelation(
                         SuperMechFaction.GetFaction(attacker)?.id,
                         SuperMechFaction.GetFaction(target)?.id) == SuperMechFaction.FactionRelation.Allied))
                    return false;

                // v0.44.0 技能释放：攻击者攻击时有概率触发主动技能
                if (attacker != null && attacker.isAlive() && Random.value < 0.3f)
                {
                    try { SuperMechSkillRuntime.TryCastSkill(attacker, target); } catch { }
                }

                // v0.47.0 超神内空间：X阶单位战斗时放出内空间
                if (SuperMechConfig.InnerSpaceEnabled)
                {
                    if (attacker != null && attacker.isAlive()) SuperMechInnerSpace.TryActivate(attacker);
                    SuperMechInnerSpace.TryActivate(target);

                    // 内空间伤害修正：攻击者内空间加伤，防御者内空间减伤
                    if (attacker != null && attacker.isAlive())
                    {
                        float atkMul = SuperMechInnerSpace.GetAttackMultiplier(attacker, target);
                        float defMul = SuperMechInnerSpace.GetDefenseMultiplier(target, attacker);
                        float domainMod = atkMul * defMul;
                        if (domainMod != 1f)
                        {
                            float domainDamage = pDamage * (domainMod - 1f);
                            target.data.health -= (int)domainDamage;
                        }
                    }
                }

                // v0.48.0 势力领袖伤害加成
                if (attacker != null && attacker.isAlive() && SuperMechConfig.FactionEnabled)
                {
                    float leaderBonus = SuperMechFaction.GetLeaderBonus(attacker);
                    if (leaderBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (leaderBonus - 1f));
                    }
                    // v0.50.0 敌对势力伤害加成
                    float hostileBonus = SuperMechFaction.GetHostileBonus(attacker, target);
                    if (hostileBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (hostileBonus - 1f));
                    }
                    // v0.51.0 文明伤害加成（原著：文明本身才是主角）
                    float civBonus = SuperMechCivilization.GetDamageBonus(SuperMechCivilization.GetCivLevel(attacker));
                    if (civBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (civBonus - 1f));
                    }
                    // v0.51.1 文明科技伤害加成
                    float techBonus = SuperMechCivilization.GetTechDamageBonus(SuperMechCivilization.GetTechLevel(attacker));
                    if (techBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (techBonus - 1f));
                    }
                    // v0.52.0 文明守护者伤害加成
                    if (SuperMechCivilization.IsGuardian(attacker))
                    {
                        target.data.health -= (int)(pDamage * 0.10f);
                    }
                }

                if (SuperMechInfoState.HasInfoState(target) && PhysicalAttacks.Contains(pAttackType))
                {
                    if (SuperMechInfoState.TryShield(target))
                    {
                        SuperMechCombatFeedback.OnInfoShield(target);
                        return false;
                    }
                }

                int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                if (targetRank >= 13 && attacker != null && attacker.isAlive())
                {
                    int atkRank = SuperMechAdvancement.GetExactRankIndex(attacker);
                    if (atkRank < 13 && Random.value < 0.3f)
                    {
                        return false;
                    }
                    // 原著：超神级之间战斗可触发信息态抹杀（第1450章）
                    // 被信息态抹杀的超神级无法概念重塑，真正死亡
                    if (atkRank >= 13 && Random.value < 0.05f)
                    {
                        SuperMechConceptImmortal.MarkInformationErased(target);
                        SuperMechCombatFeedback.OnInfoErase(target);
                    }
                }

                if (attacker != null && attacker.isAlive() && SuperMechAura.IsSuppressed(attacker))
                {
                    if (Random.value < 0.15f)
                    {
                        return false;
                    }
                }

                if (SuperMechDivinity.IsDivineAwakened(target))
                {
                    int profLayers = SuperMechDivinity.GetProfLayers(target);
                    int specLayers = SuperMechDivinity.GetSpeciesLayers(target);
                    int totalLayers = profLayers + specLayers;
                    float dr = Mathf.Min(totalLayers * 0.02f, 0.4f);
                    if (Random.value < dr)
                    {
                        return false;
                    }
                }

                // 原著职业克制：精神攻击穿甲
                // 念力系/异能系的精神打击可穿透护甲直接伤害本体
                // 机械系智力高幻术影响减弱，但无精神防御能力（除超A级虚拟机械师）
                if (SuperMechConfig.SpiritPierceEnabled
                    && attacker != null && attacker.isAlive() && target != null && target.isAlive()
                    && SuperMechAwakened.IsAwakened(attacker) && SuperMechAwakened.IsAwakened(target))
                {
                    bool atkPsi = attacker.hasTrait(SuperMechTraits.ClassPsi) || attacker.hasTrait(SuperMechTraits.ClassMind);
                    bool defMech = target.hasTrait(SuperMechTraits.ClassMech);
                    if (atkPsi && defMech)
                    {
                        // 精神攻击穿甲：忽略目标护甲，直接造成伤害（比例可配置）
                        float armorPierceDamage = pDamage * SuperMechConfig.SpiritPierceRatio;
                        target.data.health -= (int)armorPierceDamage;
                        SuperMechCombatFeedback.OnSpiritPierce(target);
                    }
                }

                // 气力属性克制环（游戏化扩展，原著无此设定，默认关闭）
                if (SuperMechConfig.QiAttributeCounterEnabled
                    && attacker != null && attacker.isAlive() && SuperMechAwakened.IsAwakened(attacker) && SuperMechAwakened.IsAwakened(target))
                {
                    float attrCounter = SuperMechQiAttribute.GetCounterMultiplier(attacker, target);
                    float classCounter = SuperMechQiAttribute.GetClassCounterMultiplier(attacker, target);
                    float totalCounter = attrCounter * classCounter;
                    if (totalCounter != 1f)
                    {
                        float counterDamage = pDamage * (totalCounter - 1f);
                        target.data.health -= (int)counterDamage;
                        SuperMechCombatFeedback.OnAttributeCounter(target, totalCounter > 1f);
                    }
                }

                // 能级差分级压制（原著：能级差距影响伤害/命中/闪避/抗性多维度）
                // 1.1-1.5轻微压制，1.5-2明显压制，2-5强烈压制，5-10碾压，>10秒杀级
                // 支持跨模组：超神机械师单位用CalcOnar，其他模组用ConvertModToOnar
                // v0.26.0：全部参数可配置化
                if (SuperMechConfig.CombatSuppressionEnabled
                    && attacker != null && attacker.isAlive() && target != null && target.isAlive())
                {
                    float atkEnergy = SuperMechAwakened.IsAwakened(attacker)
                        ? SuperMechAdvancement.CalcOnar(attacker)
                        : SuperMechModAdapters.ConvertModToOnar(attacker);
                    float defEnergy = SuperMechAwakened.IsAwakened(target)
                        ? SuperMechAdvancement.CalcOnar(target)
                        : SuperMechModAdapters.ConvertModToOnar(target);
                    if (defEnergy > 0 && atkEnergy > defEnergy)
                    {
                        float ratio = atkEnergy / defEnergy;

                        float intensity = SuperMechConfig.SuppressIntensity;

                        // 维度1：伤害加成
                        float dmgBonus = 0f;
                        if (ratio >= SuperMechConfig.SuppressThresholdAnnihilate) dmgBonus = SuperMechConfig.SuppressDmgAnnihilate;
                        else if (ratio >= SuperMechConfig.SuppressThresholdOverwhelm) dmgBonus = SuperMechConfig.SuppressDmgOverwhelm;
                        else if (ratio >= SuperMechConfig.SuppressThresholdStrong) dmgBonus = SuperMechConfig.SuppressDmgStrong;
                        else if (ratio >= SuperMechConfig.SuppressThresholdModerate) dmgBonus = SuperMechConfig.SuppressDmgModerate;
                        else if (ratio >= SuperMechConfig.SuppressThresholdMinor) dmgBonus = SuperMechConfig.SuppressDmgMinor;
                        if (dmgBonus > 0)
                        {
                            float suppressDamage = pDamage * dmgBonus * intensity;
                            target.data.health -= (int)suppressDamage;
                            SuperMechCombatFeedback.OnSuppression(target, ratio);
                        }

                        // 维度2：命中压制（高能级攻击低能级时，低能级闪避率降低）
                        // 通过概率性强制命中（跳过原版闪避判定）实现
                        float hitOverrideChance = 0f;
                        if (ratio >= SuperMechConfig.SuppressThresholdAnnihilate) hitOverrideChance = SuperMechConfig.SuppressHitAnnihilate;
                        else if (ratio >= SuperMechConfig.SuppressThresholdOverwhelm) hitOverrideChance = SuperMechConfig.SuppressHitOverwhelm;
                        else if (ratio >= SuperMechConfig.SuppressThresholdStrong) hitOverrideChance = SuperMechConfig.SuppressHitStrong;
                        else if (ratio >= SuperMechConfig.SuppressThresholdModerate) hitOverrideChance = SuperMechConfig.SuppressHitModerate;
                        hitOverrideChance = Mathf.Clamp01(hitOverrideChance * intensity);
                        if (hitOverrideChance > 0 && Random.value < hitOverrideChance)
                        {
                            // 强制命中：直接造成基础伤害（跳过原版闪避/护甲减免的一部分）
                            float forcedHitDamage = pDamage * 0.5f;
                            target.data.health -= (int)forcedHitDamage;
                        }

                        // 维度3：暴击压制（高能级对低能级暴击率提升）
                        float critChance = 0f;
                        if (ratio >= SuperMechConfig.SuppressThresholdOverwhelm) critChance = SuperMechConfig.SuppressCritOverwhelm;
                        else if (ratio >= SuperMechConfig.SuppressThresholdStrong) critChance = SuperMechConfig.SuppressCritStrong;
                        else if (ratio >= SuperMechConfig.SuppressThresholdModerate) critChance = SuperMechConfig.SuppressCritModerate;
                        if (critChance > 0 && Random.value < critChance)
                        {
                            float critDamage = pDamage * 0.5f; // 暴击额外50%伤害
                            target.data.health -= (int)critDamage;
                        }
                    }
                    else if (atkEnergy > 0 && defEnergy > atkEnergy)
                    {
                        // 维度4：抗性压制（高能级受到低能级攻击时，伤害减免）
                        float defRatio = defEnergy / atkEnergy;
                        float damageReduction = 0f;
                        if (defRatio >= SuperMechConfig.SuppressThresholdAnnihilate) damageReduction = SuperMechConfig.SuppressDefAnnihilate;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdOverwhelm) damageReduction = SuperMechConfig.SuppressDefOverwhelm;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdStrong) damageReduction = SuperMechConfig.SuppressDefStrong;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdModerate) damageReduction = SuperMechConfig.SuppressDefModerate;
                        if (damageReduction > 0)
                        {
                            // 抗性：恢复一部分即将受到的伤害（通过加血实现，因为pDamage已经在原版逻辑中应用）
                            float reducedDamage = pDamage * damageReduction;
                            target.data.health += (int)reducedDamage;
                        }

                        // 维度5：闪避压制（低能级攻击高能级时，高能级闪避率提升）
                        // 原著：能级差距大时，低能级甚至无法触碰到高能级
                        float dodgeChance = 0f;
                        if (defRatio >= SuperMechConfig.SuppressThresholdAnnihilate) dodgeChance = SuperMechConfig.SuppressDodgeAnnihilate;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdOverwhelm) dodgeChance = SuperMechConfig.SuppressDodgeOverwhelm;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdStrong) dodgeChance = SuperMechConfig.SuppressDodgeStrong;
                        else if (defRatio >= SuperMechConfig.SuppressThresholdModerate) dodgeChance = SuperMechConfig.SuppressDodgeModerate;
                        if (dodgeChance > 0 && Random.value < dodgeChance)
                        {
                            // 完全闪避：恢复全部伤害并跳过后续处理
                            target.data.health += (int)pDamage;
                            SuperMechCombatFeedback.OnDodge(target);
                            return true; // 继续执行原版逻辑（伤害已被恢复）
                        }
                    }
                }

                // 知识融合属性加成（图纸解锁模式：融合配方提供dmgMul/hpMul/speedMul属性倍率）
                // 原著：知识融合产出图纸，图纸决定装备属性，而非直接加伤害
                if (attacker != null && attacker.isAlive() && SuperMechAwakened.IsAwakened(attacker))
                {
                    var fusionStats = SuperMechKnowledgeRecipe.GetFusionStats(attacker);
                    if (fusionStats.dmgMul > 1.01f)
                    {
                        float fusionDamage = pDamage * (fusionStats.dmgMul - 1f);
                        target.data.health -= (int)fusionDamage;
                    }
                }
            }
            catch (System.Exception e)
            {
                if (!_prefixErrorLogged)
                {
                    _prefixErrorLogged = true;
                    Debug.LogWarning($"[超神机械师] 战斗Prefix异常(仅记录首次): {e.Message}");
                }
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.getHit))]
        public static void Actor_GetHit_Postfix(
            Actor __instance,
            float pDamage,
            bool pFlash,
            AttackType pAttackType,
            BaseSimObject pAttacker = null,
            bool pSkipIfShake = true,
            bool pMetallicWeapon = false,
            bool pCheckDamageReduction = true)
        {
            Actor target = __instance;
            if (target?.data == null || target.isAlive()) return;

            try
            {
                Actor killer = pAttacker as Actor;
                if (killer == null || !killer.isAlive() || killer.data == null) return;

                if (SuperMechInfoState.GetLevel(killer) >= 4)
                {
                    if (SuperMechInfoState.TryConvert(killer, target))
                    {
                        target.data.health = 1;
                    }
                }

                SuperMechLegend.OnKill(killer, target);

                if (SuperMechAwakened.IsAwakened(killer))
                {
                    int targetRankIdx = SuperMechAdvancement.GetExactRankIndex(target);
                    float xpGain = 100f * Mathf.Pow(2.5f, targetRankIdx);
                    SuperMechAwakened.AddXp(killer, xpGain);
                }

                SuperMechCrafting.OnMinionKill(killer, target);

                SuperMechEquipDrop.TryDrop(killer, target);

                int killerRank = SuperMechAdvancement.GetExactRankIndex(killer);
                int targetRank2 = SuperMechAdvancement.GetExactRankIndex(target);
                if (killerRank - targetRank2 >= 2 && SuperMechRelic.GetCurrentEquipIndex(target) >= 0)
                {
                    if (!SuperMechMechFusion.IsEquipProtected(target))
                    {
                        float breakChance = 0.05f * (killerRank - targetRank2);
                        if (UnityEngine.Random.value < breakChance)
                        {
                            int eqIdx = SuperMechRelic.GetCurrentEquipIndex(target);
                            SuperMechEquipBreak.BreakEquip(target, eqIdx);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (!_postfixErrorLogged)
                {
                    _postfixErrorLogged = true;
                    Debug.LogWarning($"[超神机械师] 战斗Postfix异常(仅记录首次): {e.Message}");
                }
            }
        }
    }
}
