using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch]
    public static class SuperMechCombatPatches
    {
        private static bool _warnedSkill; // 一次性异常警告
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
                    try { SuperMechSkillRuntime.TryCastSkill(attacker, target); } catch (System.Exception e) { if (!_warnedSkill) { _warnedSkill = true; Debug.LogWarning($"[超神机械师] 技能释放异常(仅首次): {e.Message}"); } }
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
                    // v0.59.0 文明等级差异加成（高级文明打低级文明，原著：文明代差碾压）
                    int atkCiv = (int)SuperMechCivilization.GetCivLevel(attacker);
                    int defCiv = (int)SuperMechCivilization.GetCivLevel(target);
                    if (atkCiv > defCiv)
                    {
                        float diffBonus = 1f + (atkCiv - defCiv) * 0.08f; // 每级差+8%
                        target.data.health -= (int)(pDamage * (diffBonus - 1f));
                    }
                    // v0.62.0 武道系终极知识：10%概率暴击（1.5倍伤害）
                    if (SuperMechKnowledge.HasUltimateKnowledge(attacker, "martial") && Random.value < 0.10f)
                    {
                        target.data.health -= (int)(pDamage * 0.5f); // 额外50%伤害
                    }
                    // v0.63.0 独特装备：时空剪切器10%概率眩晕
                    if (SuperMechRelic.GetCurrentSpecialEffect(attacker) == "timespace_cutter" && Random.value < 0.10f)
                    {
                        target.addStatusEffect("stunned", 2f);
                    }
                    // v0.63.0 虚空暗能机甲召唤物：伤害+50%
                    if (attacker.hasTrait("sm_void_boost"))
                    {
                        target.data.health -= (int)(pDamage * 0.5f);
                    }
                    // v0.54.0 代理战争：不同文明势力间伤害加成，文明交战时额外加成
                    float proxyBonus = SuperMechFaction.GetProxyWarBonus(attacker, target);
                    if (proxyBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (proxyBonus - 1f));
                    }
                    // v0.56.0 法术实际战斗效果
                    CombatEffects.ApplySpellEffects(attacker, target, pDamage);
                }

                // 战斗加成区：专属专长 + 世界树 + 超A级协会联合
                if (attacker != null && attacker.isAlive())
                {
                    // 专属专长：超A级个体专属能力伤害加成
                    float specBonus = SuperMechExclusiveTrait.GetAttackBonus(attacker);
                    if (specBonus != 0f)
                    {
                        target.data.health -= (int)(pDamage * specBonus);
                    }
                    SuperMechExclusiveTrait.TryInstantKill(attacker, target);
                    // v0.76.86 专精实际战斗机制
                    CombatEffects.ApplySpecialtyEffects(attacker, target, pDamage);
                    // 世界树入侵：星际联合军加成 / 世界树单位凶性
                    float unionBonus = SuperMechWorldTree.GetUnionDamageBonus(attacker);
                    if (unionBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (unionBonus - 1f));
                    }
                    float unionDef = SuperMechWorldTree.GetUnionDefenseBonus(target);
                    if (unionDef != 1f)
                    {
                        target.data.health += (int)(pDamage * (1f - unionDef));
                    }
                    float treeMult = SuperMechWorldTree.GetTreeDamageMult(attacker);
                    if (treeMult != 1f)
                    {
                        target.data.health -= (int)(pDamage * (treeMult - 1f));
                    }
                    // 超A级协会联合加成
                    float councilBonus = SuperMechSupermA.GetCouncilDamageBonus(attacker);
                    if (councilBonus != 1f)
                    {
                        target.data.health -= (int)(pDamage * (councilBonus - 1f));
                    }
                }

                // v0.76.0 集体伟力清算：清算期超A级个体受文明联合压制（受击伤害×1.3，原著巅峰之殇式清算）
                float purgeMult = SuperMechSupermA.GetPurgeDamageTakenMult(target);
                if (purgeMult != 1f)
                {
                    target.data.health -= (int)(pDamage * (purgeMult - 1f));
                }
                // v0.76.5 超能者群体收编压力：警惕期非嫡系超能者伤害×0.95（原著：文明收编拉拢超能者）
                float vigilantMult = SuperMechSupermA.GetVigilantSuppressMult(target);
                if (vigilantMult != 1f)
                {
                    target.data.health -= (int)(pDamage * (1f - vigilantMult));
                }

                // v0.76.86 防御方专精效果
                string targetSpec = SuperMechSpecialty.GetSpecialty(target);
                if (targetSpec != null)
                {
                    // 重装：受到伤害-15%
                    if (targetSpec == SuperMechSpecialty.SpecMartialHeavy)
                        target.data.health += (int)(pDamage * 0.15f);
                    // 格斗：20%概率反击（对攻击者造成30%伤害）
                    else if (targetSpec == SuperMechSpecialty.SpecMartialFight && Random.value < 0.20f && attacker != null)
                        attacker.data.health -= (int)(pDamage * 0.30f);
                    // 虚拟分支：15%概率闪避（完全免伤）
                    else if (targetSpec == SuperMechSpecialty.SpecMechVirtual && Random.value < 0.15f)
                        return false;
                }

                if (SuperMechInfoState.HasInfoState(target) && PhysicalAttacks.Contains(pAttackType))
                {
                    if (SuperMechInfoState.TryShield(target))
                    {
                        SuperMechCombatFeedback.OnInfoShield(target);
                        return false;
                    }
                }

                // v0.58.0 技能护盾吸收（在原版伤害应用前，把吸收量加回health）
                float absorbed = pDamage - SuperMechSkillRuntime.AbsorbShield(target, pDamage);
                if (absorbed > 0 && target.data != null)
                {
                    target.data.health = Mathf.Min(target.getMaxHealth(), target.data.health + (int)absorbed);
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
                if (SuperMechConfig.CombatSuppressionEnabled
                    && attacker != null && attacker.isAlive() && target != null && target.isAlive())
                {
                    float atkEnergy = SuperMechAwakened.IsAwakened(attacker)
                        ? SuperMechAdvancement.CalcOnar(attacker) : 0f;
                    float defEnergy = SuperMechAwakened.IsAwakened(target)
                        ? SuperMechAdvancement.CalcOnar(target) : 0f;
                    if (defEnergy > 0 && atkEnergy > defEnergy)
                    {
                        float ratio = atkEnergy / defEnergy;

                        // 维度1：伤害加成
                        float dmgBonus = 0f;
                        if (ratio >= SuperMechConfig.SuppressThresholdAnnihilate) dmgBonus = SuperMechConfig.SuppressDmgAnnihilate;
                        else if (ratio >= SuperMechConfig.SuppressThresholdOverwhelm) dmgBonus = SuperMechConfig.SuppressDmgOverwhelm;
                        else if (ratio >= SuperMechConfig.SuppressThresholdStrong) dmgBonus = SuperMechConfig.SuppressDmgStrong;
                        else if (ratio >= SuperMechConfig.SuppressThresholdModerate) dmgBonus = SuperMechConfig.SuppressDmgModerate;
                        else if (ratio >= SuperMechConfig.SuppressThresholdMinor) dmgBonus = SuperMechConfig.SuppressDmgMinor;
                        if (dmgBonus > 0)
                        {
                            float suppressDamage = pDamage * dmgBonus;
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
                // 降临者死亡后进入复活队列（玩家不死不灭）
                SuperMechPlayer.OnPlayerDeath(target);

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

                // 降临者击杀计入讨伐任务贡献
                if (killer.hasTrait(SuperMechPlayer.PlayerTrait))
                {
                    SuperMechPlayer.OnPlayerKill(killer, target);
                }

                // 世界树入侵：击杀世界树单位计入击退进度
                SuperMechWorldTree.OnTreeKilled(killer, target);

                // 专属专长·能量虹吸：击杀回复30%气力上限
                SuperMechExclusiveTrait.OnKill(killer);

                if (SuperMechAwakened.IsAwakened(killer))
                {
                    int targetRankIdx = SuperMechAdvancement.GetExactRankIndex(target);
                    float xpGain = 100f * Mathf.Pow(2.5f, targetRankIdx);
                    SuperMechAwakened.AddXp(killer, xpGain);
                }

                SuperMechCrafting.OnMinionKill(killer, target);

                SuperMechEquipDrop.TryDrop(killer, target);

                // v0.76.48 圣所钥匙材料掉落接线（原著：击杀S阶及以上超能者掉钥匙材料，异能/念力系高阶死亡额外掉落）
                if (SuperMechConfig.SanctuaryKeyDropEnabled)
                {
                    int kRank = SuperMechAdvancement.GetExactRankIndex(target);
                    if (kRank >= SuperMechConfig.KeyDropMinRank)
                    {
                        int mats = 1;
                        if (target.hasTrait(SuperMechTraits.ClassPsi)) mats += SuperMechConfig.PsionicKeyMaterialBonus;
                        SuperMechSanctuary.GrantKeyMaterials(mats, "击杀高阶超能者");
                    }
                }

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

        /// <summary>属性计算后应用职业方向属性加成</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), "updateStats")]
        public static void Actor_UpdateStats_Postfix(Actor __instance)
        {
            if (__instance == null || !__instance.isAlive()) return;
            try
            {
                // 属性加成计算已提取到CombatBonusCalculator（单一职责）
                CombatBonusCalculator.ApplyAllBonuses(__instance, __instance.stats);
            }
            catch (System.Exception e)
            {
                if (!_postfixErrorLogged)
                {
                    _postfixErrorLogged = true;
                    Debug.LogWarning("[超神机械师] updateStats补丁异常: " + e.Message);
                }
            }
        }

        /// <summary>
        /// 单位面板消耗条更新补丁：在原版体力/法力条下方添加气力条
        /// 原著：气力是超能者核心资源，应像生命值一样直观显示
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SelectedUnitTab), "showStatBars")]
        public static void SelectedUnitTab_ShowStatBars_Postfix(SelectedUnitTab __instance, Actor pActor)
        {
            try
            {
                SuperMechUnitBars.UpdateQiBar(__instance, pActor);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 气力条更新异常: " + e.Message);
            }
        }
    }
}
