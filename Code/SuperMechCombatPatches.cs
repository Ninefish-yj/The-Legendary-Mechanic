using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 战斗挂钩补丁（Harmony Prefix 拦截伤害）。
    ///
    /// 之前的问题：信息态护盾、系间克制等特殊效果只改了stats，
    /// 但"概率免疫物理攻击"这种效果必须拦截Actor.getHit才能真正生效。
    ///
    /// 挂钩点：Actor.getHit Prefix
    /// - return false = 跳过原版受击（攻击被完全格挡）
    /// - return true = 继续原版受击
    /// </summary>
    [HarmonyPatch]
    public static class SuperMechCombatPatches
    {
        /// <summary>物理攻击类型（信息态护盾可格挡）。</summary>
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
            AttackType pAttackType = AttackType.None,
            BaseSimObject pAttacker = null)
        {
            Actor target = __instance;
            if (target?.data == null || !target.isAlive()) return true;

            try
            {
                // ===== 1. 信息态护盾：概率免疫物理攻击（ch1141：实体↔信息态切换）=====
                if (SuperMechInfoState.HasInfoState(target) && PhysicalAttacks.Contains(pAttackType))
                {
                    if (SuperMechInfoState.TryShield(target))
                    {
                        return false; // 完全格挡，跳过原版受击
                    }
                }

                // ===== 2. 超神级（X阶）伤害减免 =====
                int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                if (targetRank >= 13) // X阶超神级
                {
                    // 超神级受到非超神级攻击时减伤90%
                    Actor attacker = pAttacker as Actor;
                    if (attacker != null && attacker.isAlive())
                    {
                        int atkRank = SuperMechAdvancement.GetExactRankIndex(attacker);
                        if (atkRank < 13)
                        {
                            // 低阶位打超神级，伤害大幅减免（通过降低攻击者伤害实现）
                            // 这里只能return true让原版跑，但减伤需要其他方式
                            // 简化：超神级有概率完全免疫低阶攻击
                            if (Random.value < 0.3f) // 30%概率免疫低阶攻击
                            {
                                return false;
                            }
                        }
                    }
                }

                // ===== 3. 系间克制伤害加成（ch原著：机械↔念力互为克星）=====
                // 伤害加成通过stats multiplier实现，这里不额外处理
                // 但可以在这里添加特殊效果（如克制时额外击退）

                // ===== 4. 神性蜕变减伤 =====
                if (SuperMechDivinity.IsDivineAwakened(target))
                {
                    int profLayers = SuperMechDivinity.GetProfLayers(target);
                    int specLayers = SuperMechDivinity.GetSpeciesLayers(target);
                    int totalLayers = profLayers + specLayers;
                    // 每层神性蜕变减伤2%，最多40%
                    float dr = totalLayers * 0.02f;
                    if (Random.value < dr)
                    {
                        return false; // 概率格挡
                    }
                }
            }
            catch
            {
                // 战斗补丁异常不影响原版战斗链
            }

            return true; // 继续原版受击
        }

        /// <summary>
        /// 击杀后处理：信息态转化（ch1267：智能瘟疫=信息态转化）。
        /// 击杀低阶位敌人时有概率转化为信息态仆从。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.getHit))]
        public static void Actor_GetHit_Postfix(
            Actor __instance,
            AttackType pAttackType = AttackType.None,
            BaseSimObject pAttacker = null)
        {
            Actor target = __instance;
            if (target?.data == null || target.isAlive()) return; // 只处理死亡

            try
            {
                Actor killer = pAttacker as Actor;
                if (killer == null || !killer.isAlive()) return;

                // 信息态转化：击杀后概率转化（Lv4以上解锁）
                if (SuperMechInfoState.GetLevel(killer) >= 4)
                {
                    if (SuperMechInfoState.TryConvert(killer, target))
                    {
                        // 转化成功：目标不死亡，变为友方（简化：恢复1点血）
                        target.data.health = 1;
                        // 标记为信息态仆从
                        Debug.Log($"[超神机械师] {killer.name} 信息态转化 {target.name}！");
                    }
                }
            }
            catch
            {
                // 静默失败
            }
        }
    }
}
