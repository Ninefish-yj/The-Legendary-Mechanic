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

                // 能级压制：攻击者能级远高于目标时，造成额外伤害
                if (attacker != null && attacker.isAlive() && SuperMechAwakened.IsAwakened(attacker) && SuperMechAwakened.IsAwakened(target))
                {
                    float atkEnergy = SuperMechAdvancement.CalcOnar(attacker);
                    float defEnergy = SuperMechAdvancement.CalcOnar(target);
                    if (defEnergy > 0 && atkEnergy > defEnergy * 1.5f)
                    {
                        float ratio = atkEnergy / defEnergy;
                        float bonusDamage = pDamage * Mathf.Min(ratio - 1f, 3f) * 0.5f;
                        target.data.health -= (int)bonusDamage;
                    }
                }

                // 知识融合加成：已融合配方提供伤害加成
                if (attacker != null && attacker.isAlive() && SuperMechAwakened.IsAwakened(attacker))
                {
                    int fusionCount = SuperMechKnowledgeRecipe.GetFusionCount(attacker);
                    if (fusionCount > 0)
                    {
                        float fusionBonus = Mathf.Min(fusionCount * 0.05f, 0.5f);
                        float bonusDamage = pDamage * fusionBonus;
                        target.data.health -= (int)bonusDamage;
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
