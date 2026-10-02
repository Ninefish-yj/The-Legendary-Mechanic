using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 能级（欧纳）评估系统。
    /// 能级是世界客观标尺，不是经验值。
    /// 基于原著：F=1~2, E=100, E+=600, D=800, D+=1600, C=2000, B=6000+,
    /// A=10000~33000, A+=20000~33000, S=爆星级, S+=星系级, SS=星团级, X=宇宙级
    /// 14阶位自动映射，越后期提升越难，A级区间陡升。
    /// </summary>
    public static class SuperMechEnergyLevel
    {
        /// <summary>能级评估等级（14阶）</summary>
        public enum EnergyGrade
        {
            Mortal = 0,   // 凡人，能级<100
            F = 1,        // F阶，1~99
            E = 2,        // E阶，100~599
            E_Plus = 3,   // E+阶，600~799
            D = 4,        // D阶，800~1599
            D_Plus = 5,   // D+阶，1600~1999
            C = 6,        // C阶，2000~3999
            C_Plus = 7,   // C+阶，4000~5999
            B = 8,        // B阶，6000~9999
            B_Plus = 9,   // B+阶，10000~19999
            A = 10,       // A阶，20000~32999（A级区间陡升）
            A_Plus = 11,  // A+阶，33000~42999
            S = 12,       // S阶（超A），43000~82599，常态一击爆星
            S_Plus = 13,  // S+阶，82600~148799，星系~星团文明级
            X = 14        // X阶，148800+，超神级宇宙级
        }

        /// <summary>能级阈值表（原著精确数值）</summary>
        public static readonly Dictionary<EnergyGrade, float> GradeThresholds = new Dictionary<EnergyGrade, float>
        {
            { EnergyGrade.Mortal, 0f },
            { EnergyGrade.F, 1f },
            { EnergyGrade.E, 100f },
            { EnergyGrade.E_Plus, 600f },
            { EnergyGrade.D, 800f },
            { EnergyGrade.D_Plus, 1600f },
            { EnergyGrade.C, 2000f },
            { EnergyGrade.C_Plus, 4000f },
            { EnergyGrade.B, 6000f },
            { EnergyGrade.B_Plus, 10000f },
            { EnergyGrade.A, 20000f },
            { EnergyGrade.A_Plus, 33000f },
            { EnergyGrade.S, 43000f },
            { EnergyGrade.S_Plus, 82600f },
            { EnergyGrade.X, 148800f }
        };

        /// <summary>战力描述（本地化key）</summary>
        private static readonly Dictionary<EnergyGrade, string> GradeDescKeys = new Dictionary<EnergyGrade, string>
        {
            { EnergyGrade.Mortal, "sm_energy_desc_mortal" },
            { EnergyGrade.F, "sm_energy_desc_f" },
            { EnergyGrade.E, "sm_energy_desc_e" },
            { EnergyGrade.E_Plus, "sm_energy_desc_e_plus" },
            { EnergyGrade.D, "sm_energy_desc_d" },
            { EnergyGrade.D_Plus, "sm_energy_desc_d_plus" },
            { EnergyGrade.C, "sm_energy_desc_c" },
            { EnergyGrade.C_Plus, "sm_energy_desc_c_plus" },
            { EnergyGrade.B, "sm_energy_desc_b" },
            { EnergyGrade.B_Plus, "sm_energy_desc_b_plus" },
            { EnergyGrade.A, "sm_energy_desc_a" },
            { EnergyGrade.A_Plus, "sm_energy_desc_a_plus" },
            { EnergyGrade.S, "sm_energy_desc_s" },
            { EnergyGrade.S_Plus, "sm_energy_desc_s_plus" },
            { EnergyGrade.X, "sm_energy_desc_x" }
        };

        /// <summary>
        /// 非线性能级函数 f(属性, 装备, 气力, 知识, 潜能, 神性, 阶位)
        /// 越到后期提升越难，A级区间陡升。
        /// 公式：能级 = 气力贡献 × 阶位系数 + 属性贡献 + 知识贡献 + 装备贡献 + 神性贡献
        /// 其中气力贡献为幂函数（指数<1，边际递减），阶位系数随阶位非线性增长。
        /// </summary>
        public static float Calculate(Actor a)
        {
            if (a == null) return 0f;
            if (!SuperMechAwakened.IsAwakened(a)) return 0f;

            // 1. 气力贡献（幂函数，边际递减）
            float qi = SuperMechQi.GetQi(a);
            float qiContribution = CurveParams.QiCoefficient * Mathf.Pow(qi, CurveParams.QiExponent);

            // 2. 阶位系数（非线性，越后期越高）
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);
            float rankMultiplier = 1f + rankIndex * CurveParams.RankGrowthRate;
            if (rankIndex >= 10) rankMultiplier *= CurveParams.ATierBoost; // A级以上陡升

            // 3. 属性贡献（力量/敏捷/耐力/智力/神秘）
            float attrContribution = CalculateAttributeContribution(a);

            // 4. 知识贡献
            string[] prefixes = { "mech", "martial", "mage", "mind", "psi" };
            int knowledgeCount = 0;
            foreach (var p in prefixes)
                knowledgeCount += SuperMechKnowledge.GetUnlockedCount(a, p);
            float knowledgeContribution = knowledgeCount * CurveParams.KnowledgeWeight;

            // 5. 装备贡献
            var bag = SuperMechEquipBag.GetBag(a);
            int equipCount = bag != null ? bag.Count : 0;
            float equipContribution = equipCount * CurveParams.EquipWeight;

            // 6. 神性贡献
            float divinityContribution = 0f;
            if (SuperMechDivinity.IsDivineAwakened(a))
            {
                int profLayers = SuperMechDivinity.GetProfLayers(a);
                int specLayers = SuperMechDivinity.GetSpeciesLayers(a);
                divinityContribution = (profLayers + specLayers) * CurveParams.DivinityWeight;
            }

            // 7. 潜能贡献
            float potential = SuperMechPotential.GetPotential(a);
            float potentialContribution = potential * CurveParams.PotentialWeight;

            // 综合：气力为主，其他为加成
            float total = qiContribution * rankMultiplier + attrContribution + knowledgeContribution
                        + equipContribution + divinityContribution + potentialContribution;

            return Mathf.Max(0f, total);
        }

        /// <summary>计算属性贡献（力量/敏捷/耐力/智力/神秘加权）</summary>
        private static float CalculateAttributeContribution(Actor a)
        {
            float strength = SuperMechCustomStats.GetStat(a, "sm_strength");
            float agility = SuperMechCustomStats.GetStat(a, "sm_agility");
            float endurance = SuperMechCustomStats.GetStat(a, "sm_endurance");
            float intelligence = SuperMechCustomStats.GetStat(a, "sm_intelligence");
            float mystery = SuperMechCustomStats.GetStat(a, "sm_mystery");

            return (strength * 0.15f + agility * 0.15f + endurance * 0.2f
                  + intelligence * 0.25f + mystery * 0.25f) * CurveParams.AttrWeight;
        }

        /// <summary>根据能级值评估等级</summary>
        public static EnergyGrade Evaluate(float energy)
        {
            if (energy >= 148800f) return EnergyGrade.X;
            if (energy >= 82600f) return EnergyGrade.S_Plus;
            if (energy >= 43000f) return EnergyGrade.S;
            if (energy >= 33000f) return EnergyGrade.A_Plus;
            if (energy >= 20000f) return EnergyGrade.A;
            if (energy >= 10000f) return EnergyGrade.B_Plus;
            if (energy >= 6000f) return EnergyGrade.B;
            if (energy >= 4000f) return EnergyGrade.C_Plus;
            if (energy >= 2000f) return EnergyGrade.C;
            if (energy >= 1600f) return EnergyGrade.D_Plus;
            if (energy >= 800f) return EnergyGrade.D;
            if (energy >= 600f) return EnergyGrade.E_Plus;
            if (energy >= 100f) return EnergyGrade.E;
            if (energy >= 1f) return EnergyGrade.F;
            return EnergyGrade.Mortal;
        }

        /// <summary>获取单位的能级评估等级</summary>
        public static EnergyGrade EvaluateActor(Actor a)
        {
            return Evaluate(Calculate(a));
        }

        /// <summary>获取能级等级名称（本地化）</summary>
        public static string GetGradeName(EnergyGrade grade)
        {
            return grade.ToString().Replace("_Plus", "+");
        }

        /// <summary>获取战力描述（本地化）</summary>
        public static string GetGradeDesc(EnergyGrade grade)
        {
            if (GradeDescKeys.TryGetValue(grade, out string key))
            {
                return LocalizedTextManager.getText(key);
            }
            return "";
        }

        /// <summary>获取完整评估文本："1071欧纳（C阶·城市级）"</summary>
        public static string GetEvaluationText(Actor a)
        {
            float energy = Calculate(a);
            EnergyGrade grade = Evaluate(energy);
            string gradeName = GetGradeName(grade);
            string desc = GetGradeDesc(grade);
            if (grade == EnergyGrade.Mortal)
            {
                return $"{energy:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}（{gradeName}）";
            }
            return $"{energy:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}（{gradeName}·{desc}）";
        }

        /// <summary>获取能级在当前等级中的进度（0~1）</summary>
        public static float GetGradeProgress(float energy)
        {
            EnergyGrade grade = Evaluate(energy);
            if (grade == EnergyGrade.X) return 1f;
            if (!GradeThresholds.TryGetValue(grade, out float floor)) return 0f;
            EnergyGrade next = grade + 1;
            if (!GradeThresholds.TryGetValue(next, out float ceil)) return 0f;
            if (ceil <= floor) return 0f;
            return Mathf.Clamp01((energy - floor) / (ceil - floor));
        }

        /// <summary>
        /// 能级曲线参数（可配置，越后期提升越难）
        /// 原著中EDCB四阶才跨越一万欧纳，A级下限一万上限三万三，跨度极大。
        /// </summary>
        public static class CurveParams
        {
            public static float QiExponent = 0.67f;        // 气力指数（<1，边际递减）
            public static float QiCoefficient = 18f;       // 气力系数
            public static float RankGrowthRate = 0.15f;    // 阶位线性增长率
            public static float ATierBoost = 1.8f;         // A级以上陡升系数
            public static float AttrWeight = 0.5f;         // 属性权重
            public static float KnowledgeWeight = 15f;     // 每条知识加成
            public static float EquipWeight = 50f;         // 每件装备加成
            public static float DivinityWeight = 200f;     // 每层神性加成
            public static float PotentialWeight = 10f;     // 每点潜能加成
        }
    }
}
