using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    // 阶位系统：14级阶位，欧纳公式=气力×阶段×属性×衰减

    public static class SuperMechAdvancement
    {
        private static readonly Dictionary<long, int> _exactRank = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _appliedRankIdx = new Dictionary<long, int>();

        public static float CalcOnar(Actor a)
        {
            if (a == null) return 0;
            float qi = SuperMechQi.GetQiMax(a);
            if (qi <= 0) return 0;

            int stage = SuperMechStage.GetStage(a);
            float stageFactor = 1f + stage * 0.03f;  // 每阶段+3%转化率

            var s = SuperMechStats.Of(a);
            float dmg = s != null ? s["damage"] : 0;
            float hp = s != null ? s["health"] : 0;
            float intell = s != null ? s["intelligence"] : 0;
            float attrFactor = 1f + (dmg / 100f + hp / 1000f + intell / 20f) * 0.02f;

            int qiLv = SuperMechQi.GetLevel(qi);
            float decay = 1f;
            if (qiLv > 12) decay = 1f / (1f + (qiLv - 12) * 0.06f);

            return qi * stageFactor * attrFactor * decay
                * SuperMechConfig.OnaMultiplier * SuperMechConfig.PromotionSpeed;
        }

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

                SuperMechTalent.GrantTalents(a);
                if (!a.hasTrait("sm_rank_01_e"))
                    a.addTrait("sm_rank_01_e");
                SetExactRank(a, 1);
                SuperMechSpecialty.AssignRandomSpecialty(a);
                SuperMechPerks.GrantRandomPerks(a);  // 随机赋予1-2个天赋专长

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

                SuperMechQi.SetQi(a, 100f);
                SuperMechQi.SetQiMax(a, 100f);
                SuperMechPotential.SetPotential(a, Random.Range(3, 6));
                SuperMechPotentialRating.RollRating(a);
                GrantStarterEquipment(a);

                string talentText = "";
                foreach (var t in talents)
                    talentText += $"{SuperMechTalent.GetTalentName(t.type)}({SuperMechTalent.RatingNames[t.rating]}) ";
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name}（{a.age}岁）激发潜能，天赋：{talentText.Trim()}");
            }
        }

        private static void GrantStarterEquipment(Actor a)
        {
            if (a == null) return;
            int count = Random.Range(1, 3);
            string[] starterIds = { "sm_eq_gray", "sm_eq_green" };
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
                if (oldExact == targetIdx) continue;  // 阶位未变
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
                    }
                }

                Debug.Log($"[超神机械师] {a.name} 阶位变更 {SuperMechRanks.All[targetIdx].name}（欧纳≈{onar:F0}）");

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

                ApplyRankStats(a, targetIdx);
                SuperMechRace.AutoEvolve(a, targetIdx);  // 种族进化与阶位挂钩

                CheckDivinityTrigger(a);

                if (targetIdx >= 10 && targetIdx > oldExact && !SuperMechRanks.IsPlusRank(targetIdx))
                {
                    if (SuperMechDivinity.IsDivineAwakened(a))
                        SuperMechDivinity.AwardAdvancementPoints(a);
                }
            }
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
                    var oldMain = SuperMechRanks.All[oldIdx - 1]; // +位的前一个主阶位
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
            _exactRank[a.id] = index;
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
            SuperMechRace.AutoEvolve(a, index);  // 种族进化与阶位挂钩

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
