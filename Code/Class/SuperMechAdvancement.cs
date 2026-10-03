using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechAdvancement
    {
        private static readonly Dictionary<long, int> _exactRank = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _appliedRankIdx = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _onarDmgBonus = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _onarHpBonus = new Dictionary<long, float>();

        // 原著能级(ch3)：欧纳博士开创，气力为核心+技能/知识/装备/专长综合加成，曲线上升
        // 原著数据点：Lv21气力182075→能级78000, Lv25气力293475→98510, Lv29气力481200→148800
        public static float CalcOnar(Actor a)
        {
            if (a == null) return 0;

            // 优先用超神机械师自己的气力体系
            float qi = SuperMechQi.GetQiMax(a);
            if (qi <= 0f)
            {
                // 没有超神机械师气力的单位，用跨模组通用能量
                float power = SuperMechCrossMod.GetUniversalPowerLevel(a);
                if (power <= 0) return 0;
                return power * SuperMechConfig.OnaMultiplier * SuperMechConfig.PromotionSpeed;
            }

            // === 收集各系统数据（Advancement只负责聚合，计算在Formulas）===
            float skillDmgSum = 0f, skillHpSum = 0f, skillSpdSum = 0f;
            int skillCount = 0;
            var skills = SuperMechSkills.GetLearned(a);
            if (skills != null)
            {
                skillCount = skills.Count;
                foreach (var s in skills)
                {
                    skillDmgSum += s.dmgMul - 1f;
                    skillHpSum += s.hpMul - 1f;
                    skillSpdSum += s.speedMul - 1f;
                }
            }

            string[] prefixes = { "mech", "martial", "power", "magic", "mind" };
            int knowledgeCount = 0;
            foreach (var p in prefixes)
                knowledgeCount += SuperMechKnowledge.GetUnlockedCount(a, p);

            int equipCount = 0;
            var bag = SuperMechEquipBag.GetBag(a);
            if (bag != null) equipCount = bag.Count;

            int perkCount = 0;
            var perks = SuperMechPerks.GetPerks(a);
            if (perks != null) perkCount = perks.Count;

            // === 纯公式计算 ===
            float baseOnar = SuperMechFormulas.OnarBase(qi);
            float bonus = SuperMechFormulas.OnarBonus(skillCount, knowledgeCount, equipCount, perkCount,
                skillDmgSum, skillHpSum, skillSpdSum);

            return baseOnar * (1f + bonus) * SuperMechConfig.OnaMultiplier * SuperMechConfig.PromotionSpeed;
        }

        public static void TickAutoAwakening()
        {
            if (!SuperMechConfig.AutoAwakening) return;
            var list = World.world.units.units_only_alive;
            if (list == null) return;
            foreach (Actor a in list)
            {
                if (a == null || a.id == null) continue;
                if (SuperMechTalent.HasTalent(a)) continue;
                if (a.age < SuperMechConfig.AwakeningMinAge) continue;
                if (Random.value > SuperMechConfig.AwakeningChance) continue;

                SuperMechTalent.GrantTalents(a);
                if (!a.hasTrait("sm_rank_01_e"))
                    a.addTrait("sm_rank_01_e");
                SetExactRank(a, 1);
                SuperMechSpecialty.AssignRandomSpecialty(a);
                SuperMechPerks.GrantRandomPerks(a);

                var talents = SuperMechTalent.GetTalents(a);
                if (talents != null && talents.Count > 0)
                {
                    var best = talents[0];
                    foreach (var t in talents)
                        if (t.rating > best.rating) best = t;
                    string classTrait = best.type switch
                    {
                        SuperMechTalent.TalentType.Mechanical => SuperMechTraits.ClassMech,
                        SuperMechTalent.TalentType.Martial => SuperMechTraits.ClassMartial,
                        SuperMechTalent.TalentType.Psi => SuperMechTraits.ClassPsi,
                        SuperMechTalent.TalentType.Mage => SuperMechTraits.ClassMage,
                        _ => SuperMechTraits.ClassMind
                    };
                    if (!a.hasTrait(classTrait)) a.addTrait(classTrait);
                    SuperMechProfession.ProfessionType pType = best.type switch
                    {
                        SuperMechTalent.TalentType.Mechanical => SuperMechProfession.ProfessionType.Mechanical,
                        SuperMechTalent.TalentType.Martial => SuperMechProfession.ProfessionType.Martial,
                        SuperMechTalent.TalentType.Psi => SuperMechProfession.ProfessionType.Psi,
                        SuperMechTalent.TalentType.Mage => SuperMechProfession.ProfessionType.Mage,
                        _ => SuperMechProfession.ProfessionType.Mind
                    };
                    SuperMechProfession.SetProfession(a, pType);
                }

                // 魔法系觉醒时分配类型（天赋/回路/魔网）
                if (a.hasTrait(SuperMechTraits.ClassMage))
                    SuperMechMageType.AssignMageType(a);

                SuperMechQi.SetQi(a, 100f);
                SuperMechQi.SetQiMax(a, 100f);
                SuperMechPotential.SetPotential(a, Random.Range(3, 6));
                SuperMechPotentialRating.RollRating(a);
                GrantStarterEquipment(a);

                // v0.39.8 寿命限制：觉醒后寿命延长为普通人3倍，突破阶位继续延长
                ApplyLifespanBonus(a, 1);

                string talentText = "";
                foreach (var t in talents)
                    talentText += $"{SuperMechTalent.GetTalentName(t.type)}({SuperMechTalent.RatingNames[t.rating]}) ";
                    Debug.Log($"[超神机械师] {a.name}（{a.age}岁）激发潜能，天赋：{talentText.Trim()}");

                // v0.28.0 UI重构：推送觉醒事件到原生事件日志
                string className = SuperMechProfession.GetClass(a);
                string talentRank = talents.Count > 0 ? SuperMechTalent.RatingNames[talents[0].rating] : "?";
                SMEventLogger.LogAwakening(a, className, talentRank);
            }
        }

        private static void GrantStarterEquipment(Actor a)
        {
            if (a == null) return;
            int count = Random.Range(1, 3);
            string[] starterIds = { "sm_eq_gray_ring", "sm_eq_green_boots" };
            for (int i = 0; i < count; i++)
            {
                string equipId = starterIds[Random.Range(0, starterIds.Length)];
                SuperMechEquipBag.AddToBag(a, equipId);
            }
        }

        public static void TickPromotions()
        {
            if (!SuperMechConfig.AutoPromotion) return;
            var list = World.world.units.units_only_alive;
            if (list == null) return;
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (!IsSuperMechUnit(a)) continue;
                float onar = CalcOnar(a);
                int targetIdx = -1;
                for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
                {
                    if (onar >= SuperMechRanks.All[i].onarFloor) { targetIdx = i; break; }
                }
                if (targetIdx < 0) continue;
                if (targetIdx > SuperMechConfig.AutoPromotionMaxRank) continue;

                if (!SuperMechAwakened.IsAwakened(a))
                {
                    int cap = SuperMechPotentialRating.GetMaxRank(a);
                    if (targetIdx > cap) targetIdx = cap;
                }

                int oldExact = GetExactRankIndex(a);

                // v0.39.7 能级战力非线性：同一阶位内能级越高战力越强，高阶位阶内差距更大
                UpdateOnarBonus(a, onar, targetIdx);

                if (oldExact == targetIdx) continue;

                // 原著：探索纪战争后期"第一个超A级的出现仿佛打破了某种极限"
                // 仅针对S阶（超A级）：首破前无人知道突破方法，只有极低概率顿悟；首破后先行者指引道路，达到能级即可突破
                // 知识要求在职业阶段提升中（原著：转职需要学习高端知识）
                string targetRankName = LocalizedTextManager.getText(SuperMechRanks.All[targetIdx].name);
                bool rankBroken = SuperMechSaveData.FirstBreakthroughRanks.Contains(targetRankName);
                if (targetIdx == 10 && targetIdx > oldExact && !rankBroken) // 仅S阶首破前
                {
                    // 首破前：无人知道突破方法，只有0.05%概率的天才能自行顿悟
                    if (UnityEngine.Random.value >= 0.0005f)
                        continue;
                }

                _exactRank[a.id] = targetIdx;

                if (!SuperMechRanks.IsPlusRank(targetIdx))
                {
                    string targetId = SuperMechRanks.All[targetIdx].id;
                    if (!a.hasTrait(targetId))
                    {
                        for (int i = 0; i < SuperMechRanks.All.Count; i++)
                        {
                            var r = SuperMechRanks.All[i];
                            if (!SuperMechRanks.IsPlusRank(i) && a.hasTrait(r.id) && r.id != targetId)
                                a.removeTrait(r.id);
                        }
                        a.addTrait(targetId);
                        SuperMechRankSpecialty.OnRankUp(a, targetIdx);
                        // 原著：武道系通过修行不同流派可改变气力属性（火的爆裂/风的速度/铁的坚韧）
                        SuperMechQiAttribute.TryMartialStyleChange(a);
                    }
                }


                if (!SuperMechAwakened.IsAwakened(a) && !SuperMechRanks.IsPlusRank(targetIdx) && targetIdx > oldExact)
                {
                    string rankName = LocalizedTextManager.getText(SuperMechRanks.All[targetIdx].name);
                    string oldRankName = oldExact >= 0 ? LocalizedTextManager.getText(SuperMechRanks.All[oldExact].name) : "F";
                    int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a));
                    if (targetIdx >= 10)
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 迈入{rankName}，气力Lv{qiLv}，能力产生质变，可掌握更高层次的技能与知识");
                    else if (targetIdx >= 8)
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 达到{rankName}（天灾级），气力Lv{qiLv}，破坏力可在行星地表掀起灾难");
                    else
                        Debug.Log($"[超神机械师]【阶位提升】{a.name} 晋升{rankName}，气力Lv{qiLv}，实力层次稳步提升");

                    // v0.39.7 首位突破记录：全图第一次突破到该阶位才记录日志+给奖励
                    bool isFirstBreakthrough = !SuperMechSaveData.FirstBreakthroughRanks.Contains(rankName);
                    if (isFirstBreakthrough)
                    {
                        SuperMechSaveData.FirstBreakthroughRanks.Add(rankName);
                        SMEventLogger.LogPromotion(a, oldRankName, rankName, onar, targetIdx);
                        ApplyFirstBreakthroughBonus(a, targetIdx);
                        Debug.Log($"[超神机械师]【首位突破】{a.name} 是全图第一个突破到{rankName}的单位！");
                    }
                }

                ApplyRankStats(a, targetIdx);
                SuperMechRace.AutoEvolve(a, targetIdx);

                CheckDivinityTrigger(a);

                if (targetIdx >= 10 && targetIdx > oldExact && !SuperMechRanks.IsPlusRank(targetIdx))
                {
                    if (SuperMechDivinity.IsDivineAwakened(a))
                        SuperMechDivinity.AwardAdvancementPoints(a);
                }
            }
        }

        /// <summary>能级战力非线性：同一阶位内能级越高战力越强，高阶位阶内差距更大</summary>
        private static void UpdateOnarBonus(Actor a, float onar, int rankIdx)
        {
            if (a == null || rankIdx < 0) return;
            var s = SuperMechStats.Of(a);
            if (s == null) return;

            // 移除之前的能级额外加成
            if (_onarDmgBonus.TryGetValue(a.id, out float oldDmg) && oldDmg > 0f)
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / (1f + oldDmg);
            if (_onarHpBonus.TryGetValue(a.id, out float oldHp) && oldHp > 0f)
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / (1f + oldHp);

            // 计算当前阶位内的能级进度
            double currentFloor = SuperMechRanks.All[rankIdx].onarFloor;
            double nextFloor = rankIdx + 1 < SuperMechRanks.All.Count ? SuperMechRanks.All[rankIdx + 1].onarFloor : currentFloor * 1.5;
            float progress = nextFloor > currentFloor ? (float)((onar - currentFloor) / (nextFloor - currentFloor)) : 0f;
            progress = Mathf.Clamp01(progress);

            // 阶内加成率随阶位递增（越到后面，1能级的战力差距越大）
            float bonusRate = rankIdx switch
            {
                >= 13 => 0.6f,   // X阶
                >= 10 => 0.45f,  // S阶以上
                >= 8 => 0.35f,   // A阶以上
                >= 6 => 0.25f,   // B阶以上
                >= 4 => 0.18f,   // C阶以上
                >= 2 => 0.12f,   // D阶以上
                _ => 0.08f       // E/F阶
            };

            float dmgBonus = progress * bonusRate;
            float hpBonus = progress * bonusRate * 0.7f;

            if (dmgBonus > 0f)
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * (1f + dmgBonus);
            if (hpBonus > 0f)
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * (1f + hpBonus);

            _onarDmgBonus[a.id] = dmgBonus;
            _onarHpBonus[a.id] = hpBonus;
        }

        /// <summary>首位突破奖励（参考西幻mod：全图第一个突破到该阶位的单位获得额外加成）</summary>
        private static void ApplyFirstBreakthroughBonus(Actor a, int rankIdx)
        {
            if (a == null) return;
            // 按阶位高低给予不同的潜能点奖励
            int bonusPoints = rankIdx switch
            {
                >= 14 => 50,   // X阶 超神级
                >= 11 => 30,   // S阶以上
                >= 9 => 20,    // A阶以上
                >= 7 => 10,    // B阶以上
                >= 5 => 5,     // C阶以上
                _ => 2         // D阶以下
            };
            SuperMechPotential.AddPotential(a, bonusPoints);
            // 额外气力加成
            float qiBonus = rankIdx * 100f;
            SuperMechQi.AddQi(a, qiBonus);
            Debug.Log($"[超神机械师] 首位突破奖励：{a.name} 获得{bonusPoints}潜能点+{qiBonus}气力");
        }

        private static void CheckDivinityTrigger(Actor a)
        {
            if (a == null) return;
            if (SuperMechDivinity.IsDivineAwakened(a)) return;
            float qiMax = SuperMechQi.GetQiMax(a);
            if (qiMax <= 0) qiMax = SuperMechQi.GetQi(a);
            int qiLv = SuperMechQi.GetLevel(qiMax);
            float onar = CalcOnar(a);
            if (qiLv >= 21 && onar >= 78000f)
            {
                SuperMechDivinity.TriggerDivinity(a);
            }
        }

        private static void ApplyRankStats(Actor a, int newRankIdx)
        {
            if (a == null || newRankIdx < 0 || newRankIdx >= SuperMechRanks.All.Count) return;
            var s = SuperMechStats.Of(a);
            if (s == null) return;

            if (_appliedRankIdx.TryGetValue(a.id, out int oldIdx) && oldIdx != newRankIdx)
            {
                if (SuperMechRanks.IsPlusRank(oldIdx))
                {
                    var oldR = SuperMechRanks.All[oldIdx];
                    var oldMain = SuperMechRanks.All[oldIdx - 1];
                    float dmgInc = oldR.damageMul / oldMain.damageMul;
                    float hpInc = oldR.healthMul / oldMain.healthMul;
                    if (dmgInc > 1f) s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / dmgInc;
                    if (hpInc > 1f) s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / hpInc;
                }
            }

            if (SuperMechRanks.IsPlusRank(newRankIdx))
            {
                var newR = SuperMechRanks.All[newRankIdx];
                var newMain = SuperMechRanks.All[newRankIdx - 1];
                float dmgInc = newR.damageMul / newMain.damageMul;
                float hpInc = newR.healthMul / newMain.healthMul;
                if (dmgInc > 1f) s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * dmgInc;
                if (hpInc > 1f) s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * hpInc;
            }
            _appliedRankIdx[a.id] = newRankIdx;

            // v0.39.8 寿命限制：突破阶位延长寿命，S阶以上永生
            ApplyLifespanBonus(a, newRankIdx);
        }

        private static readonly Dictionary<long, int> _appliedLifespanRank = new Dictionary<long, int>();

        /// <summary>寿命限制：原著中超A级寿命绵长（数百上千年）但会生老病死，只有超神级不死不灭</summary>
        private static void ApplyLifespanBonus(Actor a, int rankIdx)
        {
            if (a == null || rankIdx < 0) return;

            // X阶（超神级）才是真正的不死不灭
            if (rankIdx >= 14)
            {
                if (!a.hasTrait("immortal")) a.addTrait("immortal");
                _appliedLifespanRank[a.id] = rankIdx;
                return;
            }

            // 移除之前的寿命加成（重新计算）
            if (_appliedLifespanRank.TryGetValue(a.id, out int oldRank) && oldRank != rankIdx)
            {
                float oldBonus = GetLifespanBonus(oldRank);
                if (oldBonus > 0f && a.stats != null)
                    a.stats["lifespan"] = Mathf.Max(10f, a.stats["lifespan"] - oldBonus);
            }

            // 应用新的寿命加成
            float bonus = GetLifespanBonus(rankIdx);
            if (bonus > 0f && a.stats != null)
            {
                if (a.stats["lifespan"] <= 0f) a.stats["lifespan"] = 80f;
                a.stats["lifespan"] += bonus;
            }
            _appliedLifespanRank[a.id] = rankIdx;
        }

        public static float GetLifespanBonus(int rankIdx)
        {
            return rankIdx switch
            {
                >= 13 => 10000f,  // Ss巅峰超A：+1万岁（长生者级别，部分最初者从探索历活到星海历）
                >= 12 => 7000f,   // S+：+7000岁
                >= 11 => 5000f,   // S阶超A级：+5000岁（原著：最少数百上千年，普通超A约数千年）
                >= 10 => 3500f,   // A+：+3500岁
                >= 9 => 2000f,    // A阶天灾级：+2000岁（原著"最少的也能活个数百上千年"）
                >= 8 => 1000f,    // B+：+1000岁
                >= 7 => 600f,     // B阶：+600岁
                >= 6 => 350f,     // C+：+350岁
                >= 5 => 200f,     // C阶：+200岁
                >= 4 => 120f,     // D+：+120岁
                >= 3 => 80f,      // D阶：+80岁
                >= 1 => 50f,      // E阶觉醒：+50岁
                _ => 0f
            };
        }

        public static bool IsSuperMechUnit(Actor a)
        {
            if (a == null) return false;
            if (SuperMechTalent.HasTalent(a)) return true;
            return a.hasTrait(SuperMechTraits.ClassMech)
                || a.hasTrait(SuperMechTraits.ClassPsi)
                || a.hasTrait(SuperMechTraits.ClassMartial)
                || a.hasTrait(SuperMechTraits.ClassMage)
                || a.hasTrait(SuperMechTraits.ClassMind);
        }

        public static int GetExactRankIndex(Actor a)
        {
            if (a == null) return 0;
            // 优先从ActorContext读取（运行时主数据源）
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && ctx.exactRank >= 0) return ctx.exactRank;
            // 回退到旧字典
            if (_exactRank.TryGetValue(a.id, out int idx)) return idx;
            for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
            {
                if (!SuperMechRanks.IsPlusRank(i) && a.hasTrait(SuperMechRanks.All[i].id))
                    return i;
            }
            return 0;
        }

        public static int GetRankIndex(Actor a)
        {
            return GetExactRankIndex(a);
        }

        public static void SetExactRank(Actor a, int index)
        {
            if (a == null || index < 0 || index >= SuperMechRanks.All.Count) return;
            int oldRank = _exactRank.TryGetValue(a.id, out var o) ? o : -1;
            if (oldRank == index) return;
            _exactRank[a.id] = index;
            // 同步到ActorContext（运行时主数据源）
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.exactRank = index;
            // 发布阶位变化事件（事件总线，解耦下游系统）
            if (oldRank >= 0 && oldRank != index)
            {
                SuperMechEventBus.Publish("ActorRankChanged", new ActorRankChangedEvent
                {
                    actor = a, oldRank = oldRank, newRank = index
                });
            }
            if (!SuperMechRanks.IsPlusRank(index))
            {
                string id = SuperMechRanks.All[index].id;
                if (!a.hasTrait(id))
                {
                    for (int i = 0; i < SuperMechRanks.All.Count; i++)
                    {
                        if (SuperMechRanks.IsPlusRank(i)) continue;
                        var r = SuperMechRanks.All[i];
                        if (a.hasTrait(r.id) && r.id != id)
                            a.removeTrait(r.id);
                    }
                    a.addTrait(id);
                }
            }
            ApplyRankStats(a, index);
            SuperMechRankSpecialty.OnRankUp(a, index);
            SuperMechRace.AutoEvolve(a, index);

            if (!SuperMechAwakened.IsAwakened(a))
            {
                int nativeStage = RankToStage(index);
                int curStage = SuperMechStage.GetStage(a);
                if (nativeStage > curStage)
                    SuperMechStage.SetStage(a, nativeStage);
            }
        }

        private static int RankToStage(int rankIdx)
        {
            if (rankIdx <= 0) return 0;
            if (rankIdx <= 1) return 1;
            if (rankIdx <= 3) return 2;
            if (rankIdx <= 5) return 3;
            if (rankIdx <= 7) return 4;
            if (rankIdx <= 9) return 6;
            if (rankIdx <= 11) return 8;
            if (rankIdx <= 12) return 11;
            return 13;
        }

        public static void Clear()
        {
            _exactRank.Clear();
            _appliedRankIdx.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_exactRank, alive);
            removed += SuperMechCleanup.CleanDict(_appliedRankIdx, alive);
            return removed;
        }
    }
}
