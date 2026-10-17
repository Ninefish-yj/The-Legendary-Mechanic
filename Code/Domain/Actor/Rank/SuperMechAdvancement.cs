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
            if (qi <= 0f) return 0;

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


            // === 纯公式计算 ===
            float baseOnar = SuperMechFormulas.OnarBase(qi);
            float bonus = SuperMechFormulas.OnarBonus(skillCount, knowledgeCount, equipCount,
                skillDmgSum, skillHpSum, skillSpdSum);

            return baseOnar * (1f + bonus) * SuperMechConfig.OnaMultiplier;
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
                if (!a.hasTrait(SuperMechAwakened.AwakenedTrait))
                    a.addTrait(SuperMechAwakened.AwakenedTrait);
                if (!a.hasTrait("sm_rank_01_e"))
                    a.addTrait("sm_rank_01_e");
                SetExactRank(a, 1);
                SuperMechElement.AssignRandomSpecialty(a);

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
                SuperMechQiRefine.AddRefineLevel(a, 1); // v0.75.18: 觉醒即习得基础气力锻炼法（原著"长久锻炼慢慢提升气力上限"）
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
                string systemName = SuperMechProfession.GetSystemName(a);
                string talentRank = talents.Count > 0 ? SuperMechTalent.RatingNames[talents[0].rating] : "?";
                SuperMechEventLogger.LogAwakening(a, systemName, talentRank);
                // 发布觉醒事件
                SuperMechEventBus.Publish("ActorAwakened", new ActorAwakenedEvent
                {
                    actor = a, className = systemName, talentRank = talentRank
                });
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
                // 仅针对超A级（索引6）：首破前无人知道突破方法，只有极低概率顿悟；首破后先行者指引道路，达到能级即可突破
                // 知识要求在职业阶段提升中（原著：转职需要学习高端知识）
                string targetRankName = LocalizedTextManager.getText(SuperMechRanks.All[targetIdx].name);
                bool rankBroken = SuperMechSaveData.FirstBreakthroughRanks.Contains(targetRankName);
                if (targetIdx == 6 && targetIdx > oldExact && !rankBroken) // v0.221 超A级索引从10改为6
                {
                    // 首破前：无人知道突破方法，只有0.05%概率的天才能自行顿悟
                    if (UnityEngine.Random.value >= 0.0005f)
                        continue;
                }

                SetExactRank(a, targetIdx);

                // v0.221 9阶位后无PlusRank，所有阶位都添加特质
                string targetId = SuperMechRanks.All[targetIdx].id;
                if (!a.hasTrait(targetId))
                {
                    for (int i = 0; i < SuperMechRanks.All.Count; i++)
                    {
                        var r = SuperMechRanks.All[i];
                        if (a.hasTrait(r.id) && r.id != targetId)
                            a.removeTrait(r.id);
                    }
                    a.addTrait(targetId);
                    SuperMechRankTrait.OnRankUp(a, targetIdx);
                    // 原著：武道系通过修行不同流派可改变气力属性（火的爆裂/风的速度/铁的坚韧）
                    SuperMechQiAttribute.TryMartialStyleChange(a);
                }

                // v0.221 超A级索引从10改为6：迈入超A级自动多习得一本锻炼法
                if (targetIdx >= 6 && targetIdx > oldExact)
                    SuperMechQiRefine.AddRefineLevel(a, 1); // v0.75.18: 迈入超A级自动多习得一本锻炼法（原著超A级标配多本）

                if (!SuperMechAwakened.IsAwakened(a) && targetIdx > oldExact)
                {
                    string rankName = LocalizedTextManager.getText(SuperMechRanks.All[targetIdx].name);
                    string oldRankName = oldExact >= 0 ? LocalizedTextManager.getText(SuperMechRanks.All[oldExact].name) : "F";
                    int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a));
                    if (targetIdx >= 6) // v0.221 超A级索引从10改为6
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 迈入{rankName}，气力Lv{qiLv}，能力产生质变，可掌握更高层次的技能与知识");
                    else if (targetIdx >= 5) // v0.221 A级（天灾级）索引从8改为5
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 达到{rankName}（天灾级），气力Lv{qiLv}，破坏力可在行星地表掀起灾难");
                    else
                        Debug.Log($"[超神机械师]【阶位提升】{a.name} 晋升{rankName}，气力Lv{qiLv}，实力层次稳步提升");

                    // v0.39.7 首位突破记录：全图第一次突破到该阶位才记录日志+给奖励
                    bool isFirstBreakthrough = !SuperMechSaveData.FirstBreakthroughRanks.Contains(rankName);
                    if (isFirstBreakthrough)
                    {
                        SuperMechSaveData.FirstBreakthroughRanks.Add(rankName);
                        SuperMechEventLogger.LogPromotion(a, oldRankName, rankName, onar, targetIdx);
                        FirstBreakthroughManager.ApplyBonus(a, targetIdx);
                        Debug.Log($"[超神机械师]【首位突破】{a.name} 是全图第一个突破到{rankName}的单位！");
                    }

                    // v0.193 任务系统：阶位提升进度追踪（用阶位id而非显示名，避免本地化差异）
                    string rankId = SuperMechRanks.All[targetIdx].id;
                    SuperMechQuestSystem.UpdateQuestProgress(a, SuperMechQuestSystem.QuestObjectiveType.ReachRank, rankId, 1);
                    SuperMechQuestSystem.UpdateQuestProgress(a, SuperMechQuestSystem.QuestObjectiveType.ReachRank, "any", 1);
                }

                ApplyRankStats(a, targetIdx);
                SuperMechRace.AutoEvolve(a, targetIdx);

                CheckDivinityTrigger(a);

                // v0.221 超A级索引从10改为6
                if (targetIdx >= 6 && targetIdx > oldExact)
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
                s["multiplier_damage"] -= oldDmg;
            if (_onarHpBonus.TryGetValue(a.id, out float oldHp) && oldHp > 0f)
                s["multiplier_health"] -= oldHp;

            double currentFloor = SuperMechRanks.All[rankIdx].onarFloor;
            double nextFloor = rankIdx + 1 < SuperMechRanks.All.Count ? SuperMechRanks.All[rankIdx + 1].onarFloor : currentFloor * 1.5;
            float progress = nextFloor > currentFloor ? (float)((onar - currentFloor) / (nextFloor - currentFloor)) : 0f;
            progress = Mathf.Clamp01(progress);

            // v0.221 9阶位阈值调整
            float bonusRate = rankIdx switch
            {
                >= 8 => 0.6f,   // X阶
                >= 6 => 0.45f,  // 超A以上
                >= 5 => 0.35f,  // A阶以上
                >= 4 => 0.25f,  // B阶以上
                >= 3 => 0.18f,  // C阶以上
                >= 2 => 0.12f,  // D阶以上
                _ => 0.08f      // E/F阶
            };

            float dmgBonus = progress * bonusRate;
            float hpBonus = progress * bonusRate * 0.7f;

            if (dmgBonus > 0f)
                s["multiplier_damage"] += dmgBonus;
            if (hpBonus > 0f)
                s["multiplier_health"] += hpBonus;

            _onarDmgBonus[a.id] = dmgBonus;
            _onarHpBonus[a.id] = hpBonus;
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

            // v0.221 9阶位后无PlusRank，阶位属性通过特质直接生效，无需额外增量计算
            _appliedRankIdx[a.id] = newRankIdx;

            // v0.39.8 寿命限制：突破阶位延长寿命，X阶以上永生
            ApplyLifespanBonus(a, newRankIdx);
        }

        private static readonly Dictionary<long, int> _appliedLifespanRank = new Dictionary<long, int>();

        /// <summary>寿命限制：原著中超A级寿命绵长（数百上千年）但会生老病死，只有超神级不死不灭</summary>
        private static void ApplyLifespanBonus(Actor a, int rankIdx)
        {
            if (a == null || rankIdx < 0) return;

            // v0.221 X阶（索引8）才是真正的不死不灭
            if (rankIdx >= 8)
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
            // v0.221 9阶位寿命加成调整
            return rankIdx switch
            {
                >= 8 => 10000f,  // X级：+1万岁（实际immortal特质不死不灭）
                >= 7 => 7000f,   // 巅峰超A级：+7000岁
                >= 6 => 5000f,   // 超A级：+5000岁（原著：最少数百上千年，普通超A约数千年）
                >= 5 => 2000f,   // A级（天灾级）：+2000岁（原著"最少的也能活个数百上千年"）
                >= 4 => 600f,    // B级：+600岁
                >= 3 => 200f,    // C级：+200岁
                >= 2 => 80f,     // D级：+80岁
                >= 1 => 50f,     // E级：+50岁
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
            // v0.221 9阶位后无PlusRank，直接遍历所有阶位
            for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
            {
                if (a.hasTrait(SuperMechRanks.All[i].id))
                    return i;
            }
            return 0;
        }

        public static int GetRankIndex(Actor a)
        {
            return GetExactRankIndex(a);
        }

        /// <summary>v0.221 旧存档14阶位索引→新9阶位索引映射</summary>
        private static int MigrateOldRankIndex(int oldIndex)
        {
            if (oldIndex < 0 || oldIndex < SuperMechRanks.All.Count) return oldIndex;
            // 14阶位→9阶位映射：巅峰/Plus阶合并到主阶位
            return oldIndex switch
            {
                1 => 0,   // F级巅峰 → F级
                3 => 1,   // E级巅峰 → E级
                5 => 2,   // D级巅峰 → D级
                7 => 3,   // C+级 → C级
                9 => 4,   // B级巅峰 → B级
                11 => 6,  // 超A级（原索引11）→ 超A级（新索引6）
                12 => 7,  // 巅峰超A级（原索引12）→ 巅峰超A级（新索引7）
                13 => 8,  // X级（原索引13）→ X级（新索引8）
                _ => Mathf.Clamp(oldIndex, 0, SuperMechRanks.All.Count - 1)
            };
        }

        public static void SetExactRank(Actor a, int index)
        {
            if (a == null || index < 0) return;
            // v0.221 旧存档兼容：14阶位索引映射到9阶位
            index = MigrateOldRankIndex(index);
            if (index >= SuperMechRanks.All.Count) index = SuperMechRanks.All.Count - 1;
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
            // v0.221 9阶位后无PlusRank，所有阶位都设置特质
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
            SuperMechRankTrait.OnRankUp(a, index);
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
            // v0.221 9阶位→职业阶段映射
            if (rankIdx <= 0) return 0;   // F级
            if (rankIdx <= 1) return 1;   // E级
            if (rankIdx <= 2) return 2;   // D级
            if (rankIdx <= 3) return 3;   // C级
            if (rankIdx <= 4) return 4;   // B级
            if (rankIdx <= 5) return 6;   // A级（天灾级）
            if (rankIdx <= 6) return 8;   // 超A级
            if (rankIdx <= 7) return 11;  // 巅峰超A级
            return 13;                    // X级
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
