using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 晋升系统（Phase 1 简化版）。
    /// 欧纳=由单位 stats 按函数公式算出的评价值（原文 ch3：非加减、曲线上升）。
    /// </summary>
    public static class SuperMechAdvancement
    {
        /// <summary>
        /// 欧纳战斗力函数（原文：斯图尔特·欧纳创立，非线性相加，函数评价值）。
        /// 基于原版实际数值标定：普通人类 damage≈50, health≈500, armor≈5, int≈5。
        /// 设计：归一化到"相对普通人类"倍率，再乘性交互，基准值≈20欧纳(F)。
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

            // 归一化到普通人类基准（原版人类：dmg~50, hp~500, armor~5, int~5）
            float dmgRatio = Mathf.Max(dmg, 1f) / 50f;
            float hpRatio = Mathf.Max(hp, 1f) / 500f;
            float armorRatio = Mathf.Max(armor, 0f) / 10f;
            float intRatio = Mathf.Max(intell, 0f) / 10f;

            // 乘性交互：攻击^1.5 × 生存^0.7 × 护甲指数 × 智力因子 × 基准20
            float offense = Mathf.Pow(dmgRatio, 1.5f);
            float survival = Mathf.Pow(hpRatio, 0.7f) * Mathf.Pow(1f + armorRatio, 1.2f);
            float skill = 1f + intRatio;

            // 普通人类 ≈ 20欧纳(F级)，觉醒后带特质加成 ≈ 100欧纳(E级)
            return offense * survival * skill * mul * 20f;
        }

        /// <summary>遍历全场存活单位，按欧纳门槛晋升阶位。只处理已觉醒五系的单位。</summary>
        public static void TickPromotions()
        {
            if (!SuperMechConfig.AutoPromotion) return;  // 自动晋升关闭时跳过
            var list = World.world.units.units_only_alive;
            if (list == null) return;
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (!IsSuperMechUnit(a)) continue;  // 跳过普通村民
                float onar = CalcOnar(a);
                SuperMechRanks.RankDef target = null;
                int targetIdx = -1;
                for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
                {
                    if (onar >= SuperMechRanks.All[i].onarFloor) { target = SuperMechRanks.All[i]; targetIdx = i; break; }
                }
                if (target == null) continue;
                // 自动晋升上限：超过上限的阶位不自动升（需神权手动晋升）
                if (targetIdx > SuperMechConfig.AutoPromotionMaxRank) continue;
                if (a.hasTrait(target.id)) continue;
                foreach (var r in SuperMechRanks.All)
                    if (a.hasTrait(r.id) && r != target) a.removeTrait(r.id);
                a.addTrait(target.id);
                Debug.Log($"[超神机械师] 单位晋升 {target.name}（欧纳≈{onar:F0}）");
            }
        }

        /// <summary>判断单位是否已觉醒五系之一（只有觉醒单位才参与阶位/气力计算）。</summary>
        public static bool IsSuperMechUnit(Actor a)
        {
            return a.hasTrait(SuperMechTraits.ClassMech)
                || a.hasTrait(SuperMechTraits.ClassPsi)
                || a.hasTrait(SuperMechTraits.ClassMartial)
                || a.hasTrait(SuperMechTraits.ClassMage)
                || a.hasTrait(SuperMechTraits.ClassMind);
        }

        /// <summary>获取单位阶位索引（0=F, 13=X）。</summary>
        public static int GetRankIndex(Actor a)
        {
            for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
            {
                if (a.hasTrait(SuperMechRanks.All[i].id)) return i;
            }
            return 0;
        }
    }
}
