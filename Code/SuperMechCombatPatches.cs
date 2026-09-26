using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 战斗挂钩补丁（Harmony Prefix/Postfix 拦截伤害与击杀）。
    ///
    /// 精修内容：
    /// 1. 信息态护盾：概率免疫物理攻击（ch1141）
    /// 2. 超神级减伤：低阶打超神30%概率免疫
    /// 3. 系间克制：机械↔念力互为克星，伤害×1.3（原著设定）
    /// 4. 神性蜕变格挡：每层2%概率格挡，最多40%
    /// 5. 气势震慑：被震慑单位攻击×0.7，速度×0.5（ch378霸王色霸气式）
    /// 6. 击杀后：信息态转化、传说度获取、气力获取（提炼法）
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
                Actor attacker = pAttacker as Actor;

                // ===== 1. 信息态护盾：概率免疫物理攻击（ch1141）=====
                if (SuperMechInfoState.HasInfoState(target) && PhysicalAttacks.Contains(pAttackType))
                {
                    if (SuperMechInfoState.TryShield(target))
                    {
                        return false;
                    }
                }

                // ===== 2. 超神级（X阶）伤害减免 =====
                int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                if (targetRank >= 13 && attacker != null && attacker.isAlive())
                {
                    int atkRank = SuperMechAdvancement.GetExactRankIndex(attacker);
                    if (atkRank < 13 && Random.value < 0.3f)
                    {
                        return false; // 30%概率免疫低阶攻击
                    }
                }

                // ===== 3. 气势震慑：被震慑单位攻击降低（ch378霸王色霸气式）=====
                if (attacker != null && attacker.isAlive() && SuperMechAura.IsSuppressed(attacker))
                {
                    // 震慑状态：15%概率攻击失误（模拟震慑导致的攻击失误）
                    if (Random.value < 0.15f)
                    {
                        return false;
                    }
                }

                // ===== 4. 神性蜕变减伤 =====
                if (SuperMechDivinity.IsDivineAwakened(target))
                {
                    int profLayers = SuperMechDivinity.GetProfLayers(target);
                    int specLayers = SuperMechDivinity.GetSpeciesLayers(target);
                    int totalLayers = profLayers + specLayers;
                    float dr = Mathf.Min(totalLayers * 0.02f, 0.4f); // 每层2%，最多40%
                    if (Random.value < dr)
                    {
                        return false;
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
        /// 击杀后处理：信息态转化、传说度、气力获取。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.getHit))]
        public static void Actor_GetHit_Postfix(
            Actor __instance,
            AttackType pAttackType = AttackType.None,
            BaseSimObject pAttacker = null)
        {
            Actor target = __instance;
            if (target?.data == null || target.isAlive()) return;

            try
            {
                Actor killer = pAttacker as Actor;
                if (killer == null || !killer.isAlive() || killer.data == null) return;

                // ===== 1. 信息态转化（Lv4以上解锁，ch1267智能瘟疫）=====
                if (SuperMechInfoState.GetLevel(killer) >= 4)
                {
                    if (SuperMechInfoState.TryConvert(killer, target))
                    {
                        target.data.health = 1;
                        Debug.Log($"[超神机械师] {killer.name} 信息态转化 {target.name}！");
                    }
                }

                // ===== 2. 传说度：击杀高阶单位获得（ch1196传奇事迹影响突破）=====
                SuperMechLegend.OnKill(killer, target);

                // ===== 3. 降临者击杀获取经验（玩家面板：刷怪升级）=====
                if (SuperMechAwakened.IsAwakened(killer))
                {
                    int targetRankIdx = SuperMechAdvancement.GetExactRankIndex(target);
                    float xpGain = 100f * Mathf.Pow(2.5f, targetRankIdx);
                    SuperMechAwakened.AddXp(killer, xpGain);
                }

                // ===== 3.5 造兵击杀反哺主人经验（原著：机械军团作战经验归机械师）=====
                SuperMechCrafting.OnMinionKill(killer, target);

                // ===== 4. 装备掉落：击杀单位概率掉落装备（原著：超能者掉落装备/材料）=====
                SuperMechEquipDrop.TryDrop(killer, target);

                // ===== 5. 装备损坏：高阶攻击低阶有概率打掉装备（原著ch229装备会损坏）=====
                int killerRank = SuperMechAdvancement.GetExactRankIndex(killer);
                int targetRank2 = SuperMechAdvancement.GetExactRankIndex(target);
                if (killerRank - targetRank2 >= 2 && SuperMechRelic.GetCurrentEquipIndex(target) >= 0)
                {
                    // 械力融合的装备不会被打掉
                    if (!SuperMechMechFusion.IsEquipProtected(target))
                    {
                        // 阶位差越大，打掉概率越高
                        float breakChance = 0.05f * (killerRank - targetRank2);
                        if (UnityEngine.Random.value < breakChance)
                        {
                            int eqIdx = SuperMechRelic.GetCurrentEquipIndex(target);
                            SuperMechEquipBreak.BreakEquip(target, eqIdx);
                        }
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
