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

            float dmgRatio = Mathf.Max(dmg, 1f) / 50f;
            float hpRatio = Mathf.Max(hp, 1f) / 500f;
            float armorRatio = Mathf.Max(armor, 0f) / 10f;
            float intRatio = Mathf.Max(intell, 0f) / 10f;

            float offense = Mathf.Pow(dmgRatio, 1.5f);
            float survival = Mathf.Pow(hpRatio, 0.7f) * Mathf.Pow(1f + armorRatio, 1.2f);
            float skill = 1f + intRatio;

            return offense * survival * skill * mul * 20f;
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

                Debug.Log($"[超神机械师] {a.Name} 阶位变更 {SuperMechRanks.All[targetIdx].name}（欧纳≈{onar:F0}）");

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
                if (oldR.damageMul > 0) s["multiplier_damage"] = (s["multiplier_damage"] ?? 1f) / oldR.damageMul;
                if (oldR.healthMul > 0) s["multiplier_health"] = (s["multiplier_health"] ?? 1f) / oldR.healthMul;
            }

            // 施加新阶位倍率
            var newR = SuperMechRanks.All[newRankIdx];
            s["multiplier_damage"] = (s["multiplier_damage"] ?? 1f) * newR.damageMul;
            s["multiplier_health"] = (s["multiplier_health"] ?? 1f) * newR.healthMul;
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
        }
    }
}
