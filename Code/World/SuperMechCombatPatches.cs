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

                if (SuperMechInfoState.HasInfoState(target) && PhysicalAttacks.Contains(pAttackType))
                {
                    if (SuperMechInfoState.TryShield(target))
                    {
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
                if (attacker != null && attacker.isAlive() && target != null && target.isAlive()
                    && SuperMechAwakened.IsAwakened(attacker) && SuperMechAwakened.IsAwakened(target))
                {
                    bool atkPsi = attacker.hasTrait(SuperMechTraits.ClassPsi) || attacker.hasTrait(SuperMechTraits.ClassMind);
                    bool defMech = target.hasTrait(SuperMechTraits.ClassMech);
                    if (atkPsi && defMech)
                    {
                        // 精神攻击穿甲：忽略目标50%护甲，直接造成伤害
                        float armorPierceDamage = pDamage * 0.30f;
                        target.data.health -= (int)armorPierceDamage;
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
                    }
                }

                // 能级差分级压制（原著：能级差距影响伤害/命中/闪避/抗性多维度）
                // 1.1-1.5轻微压制，1.5-2明显压制，2-5强烈压制，5-10碾压，>10秒杀级
                // 支持跨模组：超神机械师单位用CalcOnar，其他模组用ConvertModToOnar
                if (attacker != null && attacker.isAlive() && target != null && target.isAlive())
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

                        // 维度1：伤害加成
                        float dmgBonus = 0f;
                        if (ratio >= 10f) dmgBonus = 2.0f;        // 秒杀级：伤害+200%
                        else if (ratio >= 5f) dmgBonus = 1.0f;    // 碾压：伤害+100%
                        else if (ratio >= 2f) dmgBonus = 0.5f;    // 强烈压制：伤害+50%
                        else if (ratio >= 1.5f) dmgBonus = 0.25f; // 明显压制：伤害+25%
                        else if (ratio >= 1.1f) dmgBonus = 0.10f; // 轻微压制：伤害+10%
                        if (dmgBonus > 0)
                        {
                            float suppressDamage = pDamage * dmgBonus;
                            target.data.health -= (int)suppressDamage;
                        }

                        // 维度2：命中压制（高能级攻击低能级时，低能级闪避率降低）
                        // 通过概率性强制命中（跳过原版闪避判定）实现
                        float hitOverrideChance = 0f;
                        if (ratio >= 10f) hitOverrideChance = 0.6f;    // 秒杀级：60%强制命中
                        else if (ratio >= 5f) hitOverrideChance = 0.4f; // 碾压：40%强制命中
                        else if (ratio >= 2f) hitOverrideChance = 0.2f; // 强烈压制：20%强制命中
                        else if (ratio >= 1.5f) hitOverrideChance = 0.1f; // 明显压制：10%强制命中
                        if (hitOverrideChance > 0 && Random.value < hitOverrideChance)
                        {
                            // 强制命中：直接造成基础伤害（跳过原版闪避/护甲减免的一部分）
                            float forcedHitDamage = pDamage * 0.5f;
                            target.data.health -= (int)forcedHitDamage;
                        }

                        // 维度3：暴击压制（高能级对低能级暴击率提升）
                        float critChance = 0f;
                        if (ratio >= 5f) critChance = 0.25f;    // 碾压：25%暴击
                        else if (ratio >= 2f) critChance = 0.15f; // 强烈压制：15%暴击
                        else if (ratio >= 1.5f) critChance = 0.08f; // 明显压制：8%暴击
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
                        if (defRatio >= 10f) damageReduction = 0.5f;    // 秒杀级差距：减免50%
                        else if (defRatio >= 5f) damageReduction = 0.35f; // 碾压差距：减免35%
                        else if (defRatio >= 2f) damageReduction = 0.20f; // 强烈差距：减免20%
                        else if (defRatio >= 1.5f) damageReduction = 0.10f; // 明显差距：减免10%
                        if (damageReduction > 0)
                        {
                            // 抗性：恢复一部分即将受到的伤害（通过加血实现，因为pDamage已经在原版逻辑中应用）
                            float reducedDamage = pDamage * damageReduction;
                            target.data.health += (int)reducedDamage;
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
