using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 能级（欧纳）评估系统。
    /// 能级是世界客观标尺，不是经验值。
    /// 基于原著：E=100, D=800, C=2000, B=6000, A=10000~33000, S=爆星级
    /// 14阶位自动映射，越后期提升越难。
    /// </summary>
    public static class SuperMechEnergyLevel
    {
        /// <summary>能级评估等级</summary>
        public enum EnergyGrade
        {
            Mortal = 0,   // 凡人，能级<100
            E = 1,        // E阶，100~799
            D = 2,        // D阶，800~1999
            C = 3,        // C阶，2000~5999
            B = 4,        // B阶，6000~9999
            A = 5,        // A阶，10000~42999
            S = 6,        // S阶，43000~82599
            SS = 7,       // SS阶，82600~148799
            X = 8         // X阶，148800+，宇宙级
        }

        /// <summary>能级阈值表（与SuperMechRanks的onarFloor保持一致）</summary>
        public static readonly Dictionary<EnergyGrade, float> GradeThresholds = new Dictionary<EnergyGrade, float>
        {
            { EnergyGrade.Mortal, 0f },
            { EnergyGrade.E, 100f },
            { EnergyGrade.D, 800f },
            { EnergyGrade.C, 2000f },
            { EnergyGrade.B, 6000f },
            { EnergyGrade.A, 10000f },
            { EnergyGrade.S, 43000f },
            { EnergyGrade.SS, 82600f },
            { EnergyGrade.X, 148800f }
        };

        /// <summary>战力描述（本地化key）</summary>
        private static readonly Dictionary<EnergyGrade, string> GradeDescKeys = new Dictionary<EnergyGrade, string>
        {
            { EnergyGrade.Mortal, "sm_energy_desc_mortal" },
            { EnergyGrade.E, "sm_energy_desc_e" },
            { EnergyGrade.D, "sm_energy_desc_d" },
            { EnergyGrade.C, "sm_energy_desc_c" },
            { EnergyGrade.B, "sm_energy_desc_b" },
            { EnergyGrade.A, "sm_energy_desc_a" },
            { EnergyGrade.S, "sm_energy_desc_s" },
            { EnergyGrade.SS, "sm_energy_desc_ss" },
            { EnergyGrade.X, "sm_energy_desc_x" }
        };

        /// <summary>计算单位能级（欧纳）</summary>
        public static float Calculate(Actor a)
        {
            if (a == null) return 0f;
            return SuperMechAdvancement.CalcOnar(a);
        }

        /// <summary>根据能级值评估等级</summary>
        public static EnergyGrade Evaluate(float energy)
        {
            if (energy >= 148800f) return EnergyGrade.X;
            if (energy >= 82600f) return EnergyGrade.SS;
            if (energy >= 43000f) return EnergyGrade.S;
            if (energy >= 10000f) return EnergyGrade.A;
            if (energy >= 6000f) return EnergyGrade.B;
            if (energy >= 2000f) return EnergyGrade.C;
            if (energy >= 800f) return EnergyGrade.D;
            if (energy >= 100f) return EnergyGrade.E;
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
            return grade.ToString();
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

        /// <summary>获取完整评估文本："能级：1071欧纳（C阶·城市级）"</summary>
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

        /// <summary>能级曲线参数（可配置，越后期提升越难）</summary>
        public static class CurveParams
        {
            public static float QiExponent = 0.67f;       // 气力指数
            public static float QiCoefficient = 18f;      // 气力系数
            public static float SkillWeight = 0.06f;      // 技能加成权重
            public static float KnowledgeWeight = 0.003f; // 知识加成权重
            public static float EquipWeight = 0.01f;      // 装备加成权重
            public static float PerkWeight = 0.005f;      // 专长加成权重
        }
    }
}
