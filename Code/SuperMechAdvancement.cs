using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 晋升系统。
    /// 欧纳=由单位 stats 按函数公式算出的评价值（原文 ch3：非加减、曲线上升）。
    /// +位（D+/C+/B+/A+/S+）不是独立阶位，不挂特质，只用内部字典追踪，面板显示。
    /// 主阶位（F/E/D/C/B/A/S/SS/X）才挂特质。
    /// </summary>
    public static class SuperMechAdvancement
    {
        // 精确阶位索引（0-13，含+位），仅内部追踪，不挂特质
        private static readonly Dictionary<long, int> _exactRank = new Dictionary<long, int>();
        // 已施加的阶位倍率（用于阶位变更时移除旧倍率）
        private static readonly Dictionary<long, int> _appliedRankIdx = new Dictionary<long, int>();

        /// <summary>
        /// 欧纳战斗力函数（原文：斯图尔特·欧纳创立，非线性相加，函数评价值）。
        /// 基于原版实际数值标定：普通人类 damage≈50, health≈500, armor≈5, int≈5。
        /// </summary>
        public static float CalcOnar(Actor a)
        {
            if (a == null) return 0;
            // 原著ch51："气力是超能者的基础，很大部分决定了超能者的能级与位阶"
            // 原著ch3："E级超能者的能级标准是100欧纳"，刚觉醒气力≈100 → 能级≈100
            // 欧纳是斯图尔特·欧纳创立的战斗力函数（ch3），原著未给出完整公式，此处基于数据点拟合：
            //   能级 ≈ 气力值 × 职业阶段转化率 × 属性微加成
            //   刚觉醒(E级)：气力100 × 1.0 × 1.0 ≈ 100欧纳 ✓
            //   韩萧最终(X级)：气力481200 × 高阶转化率 ≈ 148800欧纳(ch1402)
            float qi = SuperMechQi.GetQiMax(a);
            if (qi <= 0) return 0;

            // 职业阶段影响转化率（原著ch3：越高阶职业，气力等级属性加成越多）
            int stage = SuperMechStage.GetStage(a);
            float stageFactor = 1f + stage * 0.03f;  // 每阶段+3%转化率

            // 属性微加成（攻击/生存/智力，小幅度，避免属性主导能级）
            var s = SuperMechStats.Of(a);
            float dmg = s != null ? s["damage"] : 0;
            float hp = s != null ? s["health"] : 0;
            float intell = s != null ? s["intelligence"] : 0;
            float attrFactor = 1f + (dmg / 100f + hp / 1000f + intell / 20f) * 0.02f;

            // 高阶位气力边际效益递减（超神级能级增长放缓，符合ch1402韩萧最终数据）
            int qiLv = SuperMechQi.GetLevel(qi);
            float decay = 1f;
            if (qiLv > 12) decay = 1f / (1f + (qiLv - 12) * 0.06f);

            return qi * stageFactor * attrFactor * decay
                * SuperMechConfig.OnaMultiplier * SuperMechConfig.PromotionSpeed;
        }

        /// <summary>自然激发潜能：未踏入超能的单位达到年龄后有概率激发潜能，获得天赋倾向（不定死系别）。</summary>
        public static void TickAutoAwakening()
        {
            if (!SuperMechConfig.AutoAwakening) return;
            var list = World.world.units.units_only_alive;
            if (list == null) return;
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (SuperMechTalent.HasTalent(a)) continue; // 已踏入超能
                if (a.age < SuperMechConfig.AwakeningMinAge) continue;
                if (Random.value > SuperMechConfig.AwakeningChance) continue;

                // 激发潜能：获得天赋倾向+E阶（原著ch1051："刚觉醒都是E级超能者"，星海人无F阶）
                // F阶仅降临者（玩家）lv1-20专属（ch476），星海人刚觉醒直接E级
                SuperMechTalent.GrantTalents(a);
                if (!a.hasTrait("sm_rank_01_e"))
                    a.addTrait("sm_rank_01_e");
                SetExactRank(a, 1);
                SuperMechSpecialty.AssignRandomSpecialty(a);
                SuperMechPerks.GrantRandomPerks(a);  // 随机赋予1-2个天赋专长

                // 自动选定主职业方向：按最高天赋评级选择系别（天赋是潜在的，选定后才获得系别特质）
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
                    // 同时设置职业方向（知识Tab检查的是这个，不是系别特质）
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

                // 初始化气力（E级标准100欧纳对应气力Lv3≈100）
                SuperMechQi.SetQi(a, 100f);
                SuperMechQi.SetQiMax(a, 100f);
                // 初始潜能点：3-5点（让新觉醒单位能解锁基础知识，原著：觉醒后获得潜能点解锁知识）
                SuperMechPotential.SetPotential(a, Random.Range(3, 6));
                // 随机潜力评级（原著ch1099：所有超能者都有潜力评级，决定阶位上限）
                SuperMechPotentialRating.RollRating(a);
                // 初始装备：1-2件低品质装备（确保背包非空）
                GrantStarterEquipment(a);

                string talentText = "";
                foreach (var t in talents)
                    talentText += $"{SuperMechTalent.GetTalentName(t.type)}({SuperMechTalent.RatingNames[t.rating]}) ";
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name}（{a.age}岁）激发潜能，天赋：{talentText.Trim()}");
            }
        }

        /// <summary>给新激发潜能的单位初始装备（1-2件低品质，确保背包非空）。</summary>
        private static void GrantStarterEquipment(Actor a)
        {
            if (a == null) return;
            // 1-2件灰色/绿色装备
            int count = Random.Range(1, 3);
            string[] starterIds = { "sm_eq_gray", "sm_eq_green" };
            for (int i = 0; i < count; i++)
            {
                string equipId = starterIds[Random.Range(0, starterIds.Length)];
                SuperMechEquipBag.AddToBag(a, equipId);
            }
        }

        /// <summary>遍历全场存活单位，按欧纳门槛晋升阶位。+位只更新内部字典，主阶位才挂特质。</summary>
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

                // 星海人（非降临者）受潜力评级上限限制（原著ch1099：评级代表最终能达到的阶位上限，玩家不适用）
                if (!SuperMechAwakened.IsAwakened(a))
                {
                    int cap = SuperMechPotentialRating.GetMaxRank(a);
                    if (targetIdx > cap) targetIdx = cap;
                }

                // 更新精确阶位字典（含+位）
                int oldExact = GetExactRankIndex(a);
                if (oldExact == targetIdx) continue;  // 阶位未变
                _exactRank[a.id] = targetIdx;

                // 只有主阶位（非+位）才挂特质
                if (!SuperMechRanks.IsPlusRank(targetIdx))
                {
                    string targetId = SuperMechRanks.All[targetIdx].id;
                    if (!a.hasTrait(targetId))
                    {
                        // 移除旧的主阶位特质（用索引遍历，避免每次ToArray）
                        for (int i = 0; i < SuperMechRanks.All.Count; i++)
                        {
                            var r = SuperMechRanks.All[i];
                            if (!SuperMechRanks.IsPlusRank(i) && a.hasTrait(r.id) && r.id != targetId)
                                a.removeTrait(r.id);
                        }
                        a.addTrait(targetId);
                        SuperMechRankSpecialty.OnRankUp(a, targetIdx);
                    }
                }

                Debug.Log($"[超神机械师] {a.name} 阶位变更 {SuperMechRanks.All[targetIdx].name}（欧纳≈{onar:F0}）");

                // 星海人阶位突破确认（原著ch51/ch383）
                // ch51：气力很大部分决定能级与位阶，气力等级标准公开（lv1=10,lv2=50...）
                // ch383：突破阶位后异能产生质变，掌握新用法
                // 星海人没有面板，但能通过气力层次、异能质变、战斗力感知自身阶位
                if (!SuperMechAwakened.IsAwakened(a) && !SuperMechRanks.IsPlusRank(targetIdx) && targetIdx > oldExact)
                {
                    string rankName = SuperMechRanks.All[targetIdx].name;
                    int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a));
                    if (targetIdx >= 10) // S阶以上，突破伴随能力质变
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 迈入{rankName}，气力Lv{qiLv}，能力产生质变，可掌握更高层次的技能与知识");
                    else if (targetIdx >= 8) // A阶（天灾级）
                        Debug.Log($"[超神机械师]【阶位突破】{a.name} 达到{rankName}（天灾级），气力Lv{qiLv}，破坏力可在行星地表掀起灾难");
                    else
                        Debug.Log($"[超神机械师]【阶位提升】{a.name} 晋升{rankName}，气力Lv{qiLv}，实力层次稳步提升");
                }

                // 按精确阶位（含+位）施加属性倍率
                ApplyRankStats(a, targetIdx);
                SuperMechRace.AutoEvolve(a, targetIdx);  // 种族进化与阶位挂钩

                // 神性蜕变触发（ch1039：气力Lv21 + 能级78000欧纳）
                // 不在阶位变更时触发，因为可能在同阶位内达到条件
                CheckDivinityTrigger(a);

                // S阶以上主阶位晋升给神性蜕变点数（ch1043）
                if (targetIdx >= 10 && targetIdx > oldExact && !SuperMechRanks.IsPlusRank(targetIdx))
                {
                    if (SuperMechDivinity.IsDivineAwakened(a))
                        SuperMechDivinity.AwardAdvancementPoints(a);
                }
            }
        }

        /// <summary>
        /// 检查神性蜕变触发条件（ch1039原文：气力Lv21 + 能级78000欧纳）。
        /// 达到条件自动触发神性蜕变，不在阶位变更时触发。
        /// </summary>
        private static void CheckDivinityTrigger(Actor a)
        {
            if (a == null) return;
            if (SuperMechDivinity.IsDivineAwakened(a)) return;
            float qiMax = SuperMechQi.GetQiMax(a);
            if (qiMax <= 0) qiMax = SuperMechQi.GetQi(a);
            int qiLv = SuperMechQi.GetLevel(qiMax);
            float onar = CalcOnar(a);
            // ch1039：气力等级达到Lv21，能级超过78000
            if (qiLv >= 21 && onar >= 78000f)
            {
                SuperMechDivinity.TriggerDivinity(a);
            }
        }

        /// <summary>按精确阶位施加+位的额外伤害/生命倍率（主阶位倍率已在特质base_stats中）。阶位变更时调用。</summary>
        private static void ApplyRankStats(Actor a, int newRankIdx)
        {
            if (a == null || newRankIdx < 0 || newRankIdx >= SuperMechRanks.All.Count) return;
            var s = SuperMechStats.Of(a);
            if (s == null) return;

            // 移除旧+位的增量倍率
            if (_appliedRankIdx.TryGetValue(a.id, out int oldIdx) && oldIdx != newRankIdx)
            {
                if (SuperMechRanks.IsPlusRank(oldIdx))
                {
                    var oldR = SuperMechRanks.All[oldIdx];
                    var oldMain = SuperMechRanks.All[oldIdx - 1]; // +位的前一个主阶位
                    float dmgInc = oldR.damageMul / oldMain.damageMul;
                    float hpInc = oldR.healthMul / oldMain.healthMul;
                    if (dmgInc > 1f) s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / dmgInc;
                    if (hpInc > 1f) s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / hpInc;
                }
            }

            // 施加新+位的增量倍率（主阶位不处理，由特质提供）
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
        }

        /// <summary>判断单位是否已踏入超能（有天赋倾向，或兼容旧存档的五系特质）。</summary>
        public static bool IsSuperMechUnit(Actor a)
        {
            if (a == null) return false;
            // 新系统：有天赋倾向就是踏入超能
            if (SuperMechTalent.HasTalent(a)) return true;
            // 兼容旧存档：有五系特质
            return a.hasTrait(SuperMechTraits.ClassMech)
                || a.hasTrait(SuperMechTraits.ClassPsi)
                || a.hasTrait(SuperMechTraits.ClassMartial)
                || a.hasTrait(SuperMechTraits.ClassMage)
                || a.hasTrait(SuperMechTraits.ClassMind);
        }

        /// <summary>获取单位精确阶位索引（0=F, 13=X，含+位）。优先从字典读，回退到特质判断。</summary>
        public static int GetExactRankIndex(Actor a)
        {
            if (a == null) return 0;
            if (_exactRank.TryGetValue(a.id, out int idx)) return idx;
            // 回退：从特质判断（只能判断主阶位）
            for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
            {
                if (!SuperMechRanks.IsPlusRank(i) && a.hasTrait(SuperMechRanks.All[i].id))
                    return i;
            }
            return 0;
        }

        /// <summary>获取单位阶位索引（兼容旧调用，返回精确阶位含+位）。</summary>
        public static int GetRankIndex(Actor a)
        {
            return GetExactRankIndex(a);
        }

        /// <summary>设置单位精确阶位（复活/初始化用），自动施加对应属性倍率。</summary>
        public static void SetExactRank(Actor a, int index)
        {
            if (a == null || index < 0 || index >= SuperMechRanks.All.Count) return;
            _exactRank[a.id] = index;
            // 只挂主阶位特质
            if (!SuperMechRanks.IsPlusRank(index))
            {
                string id = SuperMechRanks.All[index].id;
                if (!a.hasTrait(id))
                {
                    foreach (var r in SuperMechRanks.All)
                        if (!SuperMechRanks.IsPlusRank(System.Array.IndexOf(SuperMechRanks.All.ToArray(), r))
                            && a.hasTrait(r.id) && r.id != id)
                            a.removeTrait(r.id);
                    a.addTrait(id);
                }
            }
            ApplyRankStats(a, index);
            SuperMechRankSpecialty.OnRankUp(a, index);
            SuperMechRace.AutoEvolve(a, index);  // 种族进化与阶位挂钩

            // 星海人（非降临者）阶位提升时，自动提升职业阶段
            // 原著ch267：NPC也有二十级进阶，只是没面板，靠修行和运气突破
            if (!SuperMechAwakened.IsAwakened(a))
            {
                int nativeStage = RankToStage(index);
                int curStage = SuperMechStage.GetStage(a);
                if (nativeStage > curStage)
                    SuperMechStage.SetStage(a, nativeStage);
            }
        }

        /// <summary>阶位→星海人职业阶段映射（原著NPC靠修行突破，职业阶段与阶位大致对应）。</summary>
        private static int RankToStage(int rankIdx)
        {
            // F→0, E→1, D→2, C→3, B→4, A→6, S→8, SS→11, X→13
            if (rankIdx <= 0) return 0;   // F
            if (rankIdx <= 1) return 1;   // E
            if (rankIdx <= 3) return 2;   // D/D+
            if (rankIdx <= 5) return 3;   // C/C+
            if (rankIdx <= 7) return 4;   // B/B+
            if (rankIdx <= 9) return 6;   // A/A+
            if (rankIdx <= 11) return 8;  // S/S+
            if (rankIdx <= 12) return 11; // SS
            return 13;                    // X
        }

        /// <summary>清空阶位数据（世界切换用）。</summary>
        public static void Clear()
        {
            _exactRank.Clear();
            _appliedRankIdx.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_exactRank, alive);
            removed += SuperMechCleanup.CleanDict(_appliedRankIdx, alive);
            return removed;
        }
    }
}
