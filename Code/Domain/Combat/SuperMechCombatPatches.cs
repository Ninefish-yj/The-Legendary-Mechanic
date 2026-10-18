using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch]
    public static class SuperMechCombatPatches
    {
        private static bool _warnedSkill; // 一次性异常警告
        private static bool _warnedSpell; // 法术释放一次性异常警告
        private static bool _warnedChoreo; // 招式触发一次性异常警告
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

                // v0.213 战斗动作编排：攻击者有概率触发自定义招式（突进斩击/机械臂冲击/无人机齐射等）
                if (attacker != null && attacker.isAlive())
                {
                    try { SuperMechChoreographer.TryTriggerMove(attacker, target); }
                    catch (System.Exception e) { if (!_warnedChoreo) { _warnedChoreo = true; Debug.LogWarning($"[超神机械师] 招式触发异常(仅首次): {e.Message}"); } }
                }

                // v0.197 战斗特效：攻击命中时显示职业施法特效
                if (attacker != null && attacker.isAlive())
                {
                    SuperMechCombatEffects.OnAttackHit(attacker, target);
                }

                // v0.200 战斗AI增强：职业差异化战斗行为（连击/暴击/闪避/低血量爆发）
                if (attacker != null && attacker.isAlive())
                {
                    float aiDamage = SuperMechCombatAI.ProcessAttack(attacker, target, pDamage);
                    if (aiDamage <= 0f)
                    {
                        return false; // 完全闪避
                    }
                    // AI额外伤害（暴击/连击/职业加成超出基础伤害的部分）
                    float bonusDamage = aiDamage - pDamage;
                    if (bonusDamage > 0f && target.data != null)
                    {
                        target.data.health -= (int)bonusDamage;
                    }

                    // v0.201 打击感增强：命中停顿+冲击闪光+冲击波（大伤害触发）
                    SuperMechHitStop.OnHit(attacker, target, aiDamage);
                }

                // v0.112.0 法术自动释放：学会法术的单位在战斗中自动施法（有冷却控制）
                if (attacker != null && attacker.isAlive())
                {
                    try { SuperMechSpellRuntime.TryAutoCast(attacker, target); } catch (System.Exception e) { if (!_warnedSpell) { _warnedSpell = true; Debug.LogWarning($"[超神机械师] 法术释放异常(仅首次): {e.Message}"); } }
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
                    // v0.198 装备套装效果：2件/4件激活伤害加成
                    var setBonus = SuperMechEquipSet.GetSetBonuses(attacker);
                    if (setBonus.dmgBonus > 0)
                    {
                        target.data.health -= (int)(pDamage * setBonus.dmgBonus * 0.01f);
                    }
                    // 防御方套装生命加成（通过减伤实现）
                    var defSetBonus = SuperMechEquipSet.GetSetBonuses(target);
                    if (defSetBonus.hpBonus > 0)
                    {
                        target.data.health += (int)(pDamage * defSetBonus.hpBonus * 0.005f);
                    }
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

                // v0.106.0 阶位差距伤害减免（对照原著：高阶位对低阶位攻击有极高防御）
                // 原著依据："一般天灾级攻击对他而言已经是挠痒痒了，甚至稍弱一点的天灾级还破不了他的防御"
                // 设计：高减免但不免疫，差距越大减免越多，最高92%（永远留8%最低伤害）
                if (attacker != null && attacker.isAlive())
                {
                    int atkRankIdx = SuperMechAdvancement.GetExactRankIndex(attacker);
                    int defRankIdx = SuperMechAdvancement.GetExactRankIndex(target);
                    float rankReduction = CombatSuppression.GetRankDamageReduction(atkRankIdx, defRankIdx);
                    if (rankReduction > 0f)
                    {
                        int reducedDamage = (int)(pDamage * rankReduction);
                        if (reducedDamage > 0)
                        {
                            target.data.health = Mathf.Min(target.getMaxHealth(), target.data.health + reducedDamage);
                        }
                    }
                }

                int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
                // v0.221 X级（索引8）概念永生保护
                if (targetRank >= 8 && attacker != null && attacker.isAlive())
                {
                    int atkRank = SuperMechAdvancement.GetExactRankIndex(attacker);
                    if (atkRank < 8 && Random.value < 0.3f)
                    {
                        return false;
                    }
                    // 原著：超神级之间战斗可触发信息态抹杀（第1450章）
                    // 被信息态抹杀的超神级无法概念重塑，真正死亡
                    if (atkRank >= 8 && Random.value < 0.05f)
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

            // v0.197 战斗视觉特效：伤害飘字+职业颜色+屏幕震动
            try
            {
                Actor atk = pAttacker as Actor;
                if (atk != null && target != null && pDamage > 0)
                {
                    SuperMechCombatVisuals.OnAttack(atk, target, pDamage, pAttackType);
                }
            }
            catch (System.Exception) { }

            // v0.201.10 血量下限保护：避免直接扣血导致负数显示和不死bug
            // v0.213.1 增加上限保护：减伤/护盾加血可能超过上限
            // 原版getHit在扣血前检查!hasHealth()，若血量已<=0会直接return，不触发死亡
            // 所以下限设为1，让原版changeHealth正常扣血后hasHealth()返回false，加入死亡队列
            if (target.data != null)
            {
                int maxHp = target.getMaxHealth();
                if (target.data.health < 1) target.data.health = 1;
                else if (target.data.health > maxHp) target.data.health = maxHp;
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
            if (target?.data == null) return;

            // v0.213.1 血量负数修复：Prefix额外伤害+原版扣血可能导致血量负数但单位未死亡
            // 若血量<=0且单位仍存活，强制触发死亡（降临者会进入复活队列）
            // v0.242.21 修复：dieAndDestroy后直接return会跳过下面的OnPlayerDeath，
            //   导致降临者被模组额外伤害打死时不进入复活队列。先记录死亡再触发原版死亡。
            if (target.data.health <= 0 && target.isAlive())
            {
                SuperMechPlayer.OnPlayerDeath(target);
                try { target.dieAndDestroy(pAttackType); } catch (System.Exception) { }
                return;
            }

            if (target.isAlive()) return;

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

                // v0.182 战斗统计：记录击杀
                SuperMechCombatStats.RecordKill(killer, target);

                // v0.183 任务系统：击杀进度
                SuperMechQuestSystem.UpdateQuestProgress(killer, SuperMechQuestSystem.QuestObjectiveType.Kill, target.data.id.ToString(), 1);
                SuperMechQuestSystem.UpdateQuestProgress(killer, SuperMechQuestSystem.QuestObjectiveType.Kill, "any", 1);
                // v0.193 任务系统：战斗胜利进度（每次击杀计为一场战斗胜利）
                SuperMechQuestSystem.UpdateQuestProgress(killer, SuperMechQuestSystem.QuestObjectiveType.WinBattle, "any", 1);

                // 降临者击杀计入讨伐任务贡献
                if (killer.hasTrait(SuperMechPlayer.PlayerTrait))
                {
                    SuperMechPlayer.OnPlayerKill(killer, target);
                    // v0.199 超A协会声望：击杀高阶敌人获得声望
                    int rep = SuperMechSuperMAReputation.GetKillReputation(target);
                    if (rep > 0) SuperMechSuperMAReputation.AddReputation(killer, rep);
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

                // v0.196 战报系统：记录战争伤亡
                SuperMechBattleReport.OnUnitKilled(killer, target);

                // v0.197 战斗特效：击杀时显示爆炸/经验特效
                SuperMechCombatEffects.OnKill(killer, target);

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
