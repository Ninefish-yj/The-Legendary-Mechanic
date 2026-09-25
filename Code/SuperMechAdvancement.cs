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
            var s = SuperMechStats.Of(a);
            float dmg = s["damage"];
            float hp = s["health"];
            float intell = s["intelligence"];
            float armor = s["armor"];
            float mul = s["multiplier_damage"] > 1f ? s["multiplier_damage"] : 1f;

            // 气力等级直接影响能级（原著ch3：气力是超能者基础，气力等级加属性）
            int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a));
            float qiFactor = 1f + qiLv * 0.15f;  // 每级气力+15%能级

            // 职业阶段影响能级转化率（原著ch3：越高阶职业，气力等级属性加成越多）
            int stage = SuperMechStage.GetStage(a);
            float stageFactor = 1f + stage * 0.1f;  // 每阶段+10%能级

            float dmgRatio = Mathf.Max(dmg, 1f) / 50f;
            float hpRatio = Mathf.Max(hp, 1f) / 500f;
            float armorRatio = Mathf.Max(armor, 0f) / 10f;
            float intRatio = Mathf.Max(intell, 0f) / 10f;

            // 曲线函数（原著ch3：计算方式并非加减，而是复杂的函数模式，总体趋势为曲线上升）
            // 攻击指数1.8（更陡峭），生存指数0.6，智力影响放大
            float offense = Mathf.Pow(dmgRatio, 1.8f);
            float survival = Mathf.Pow(hpRatio, 0.6f) * Mathf.Pow(1f + armorRatio, 1.3f);
            float skill = 1f + intRatio * 1.5f;

            return offense * survival * skill * mul * qiFactor * stageFactor * 15f
                * SuperMechConfig.OnaMultiplier * SuperMechConfig.PromotionSpeed;
        }

        /// <summary>自然觉醒：未觉醒单位达到年龄后有概率随机觉醒为五系之一。</summary>
        public static void TickAutoAwakening()
        {
            if (!SuperMechConfig.AutoAwakening) return;
            var list = World.world.units.units_only_alive;
            if (list == null) return;
            string[] classes = {
                SuperMechTraits.ClassMech, SuperMechTraits.ClassMartial,
                SuperMechTraits.ClassPsi, SuperMechTraits.ClassMage, SuperMechTraits.ClassMind
            };
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (IsSuperMechUnit(a)) continue; // 已觉醒
                if (a.age < SuperMechConfig.AwakeningMinAge) continue;
                if (Random.value > SuperMechConfig.AwakeningChance) continue;
                string cls = classes[Random.Range(0, classes.Length)];
                a.addTrait(cls);
                // 觉醒时初始化气力
                SuperMechQi.SetQi(a, 10f);
                SuperMechQi.SetQiMax(a, 10f);
                // 觉醒时随机潜力评级（原著ch1099：所有超能者都有潜力评级，决定阶位上限）
                SuperMechPotentialRating.RollRating(a);
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name}（{a.age}岁）自然觉醒为 {cls}");
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
                        // 移除旧的主阶位特质
                        foreach (var r in SuperMechRanks.All)
                        {
                            if (!SuperMechRanks.IsPlusRank(System.Array.IndexOf(SuperMechRanks.All.ToArray(), r))
                                && a.hasTrait(r.id) && r.id != targetId)
                                a.removeTrait(r.id);
                        }
                        a.addTrait(targetId);
                        SuperMechRankSpecialty.OnRankUp(a, targetIdx);
                    }
                }

                Debug.Log($"[超神机械师] {a.name} 阶位变更 {SuperMechRanks.All[targetIdx].name}（欧纳≈{onar:F0}）");

                // 星海人阶位突破确认（原著ch51/ch383/ch417）
                // ch51：气力很大部分决定能级与位阶，气力等级标准公开（lv1=10,lv2=50...）
                // ch383：突破阶位后异能产生质变，掌握新用法
                // ch417：职业等级与阶位有对应关系（六十级=C级垫底）
                // 星海人没有面板，但能通过气力层次、异能质变、职业等级感知自身阶位
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

        /// <summary>按精确阶位施加伤害/生命倍率（+位也有独立倍率）。阶位变更时调用。</summary>
        private static void ApplyRankStats(Actor a, int newRankIdx)
        {
            if (a == null || newRankIdx < 0 || newRankIdx >= SuperMechRanks.All.Count) return;
            var s = SuperMechStats.Of(a);
            if (s == null) return;

            // 移除旧阶位倍率
            if (_appliedRankIdx.TryGetValue(a.id, out int oldIdx) && oldIdx != newRankIdx)
            {
                var oldR = SuperMechRanks.All[oldIdx];
                if (oldR.damageMul > 0) s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / oldR.damageMul;
                if (oldR.healthMul > 0) s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / oldR.healthMul;
            }

            // 施加新阶位倍率
            var newR = SuperMechRanks.All[newRankIdx];
            s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * newR.damageMul;
            s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * newR.healthMul;
            _appliedRankIdx[a.id] = newRankIdx;
        }

        /// <summary>判断单位是否已觉醒五系之一。</summary>
        public static bool IsSuperMechUnit(Actor a)
        {
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

            // 土著（非降临者）阶位提升时，自动提升职业阶段
            // 原著ch267：NPC也有二十级进阶，只是没面板，靠修行和运气突破
            if (!SuperMechAwakened.IsAwakened(a))
            {
                int nativeStage = RankToStage(index);
                int curStage = SuperMechStage.GetStage(a);
                if (nativeStage > curStage)
                    SuperMechStage.SetStage(a, nativeStage);
            }
        }

        /// <summary>阶位→土著职业阶段映射（原著NPC靠修行突破，职业阶段与阶位大致对应）。</summary>
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
    }
}
