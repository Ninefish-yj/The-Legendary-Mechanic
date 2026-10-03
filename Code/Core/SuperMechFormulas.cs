using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 纯数值公式层：不依赖WorldBox类型，只做数学计算。
    /// 所有公式从各业务系统中抽取到这里，便于测试和复用。
    /// 原著依据标注在每个方法上。
    /// </summary>
    public static class SuperMechFormulas
    {
        // === 气力系统公式（原著ch51/ch539/ch1039/ch1203/ch1402）===

        /// <summary>气力基础值：0.000921 × level^4.9 × 阶段倍率</summary>
        public static float QiBase(int qiLevel, int stage)
        {
            float stageMul = 1f + stage * 0.1f;
            return 0.000921f * Mathf.Pow(Mathf.Max(1, qiLevel), 4.9f) * stageMul;
        }

        /// <summary>气力各属性值（力/敏/耐/智/神秘），Lv10原著比例</summary>
        public static (float strength, float agility, float endurance, float intelligence, float mystery)
            QiStats(int qiLevel, int stage)
        {
            float b = QiBase(qiLevel, stage);
            return (b * 1.000f, b * 1.366f, b * 1.521f, b * 1.718f, b * 1.085f);
        }

        /// <summary>气力能级(ONAR)基础：18 × qi^0.67</summary>
        public static float OnarBase(float qi)
        {
            return 18f * Mathf.Pow(Mathf.Max(1f, qi), 0.67f);
        }

        /// <summary>
        /// ONAR综合加成率（上限50%）。
        /// 输入各系统的计数，输出总加成率。纯计算，不依赖游戏对象。
        /// </summary>
        public static float OnarBonus(int skillCount, int knowledgeCount, int equipCount, int perkCount,
            float skillDmgMulSum, float skillHpMulSum, float skillSpeedMulSum)
        {
            // 技能加成：已学技能倍率偏差之和 × 0.06
            float skillBonus = (skillDmgMulSum + skillHpMulSum + skillSpeedMulSum) * 0.06f;
            // 知识加成：每知识+0.3%
            float knowledgeBonus = knowledgeCount * 0.003f;
            // 装备加成：每件+1%
            float equipBonus = equipCount * 0.01f;
            // 专长加成：每个+0.5%
            float perkBonus = perkCount * 0.005f;
            return Mathf.Min(0.5f, skillBonus + knowledgeBonus + equipBonus + perkBonus);
        }

        // === 阶位公式 ===

        /// <summary>阶位是否为加号阶（D+, C+, B+, A+, S+）</summary>
        public static bool IsPlusRank(int rankIndex)
        {
            return rankIndex == 3 || rankIndex == 5 || rankIndex == 7 || rankIndex == 9 || rankIndex == 11;
        }

        /// <summary>阶位基础能级倍率（非线性增长）</summary>
        public static float RankPowerMultiplier(int rankIndex)
        {
            // F=1, E=2, D=4, D+=6, C=10, C+=15, B=25, B+=40, A=70, A+=110, S=180, S+=280, SS=450, X=800
            float[] mult = { 1, 2, 4, 6, 10, 15, 25, 40, 70, 110, 180, 280, 450, 800 };
            if (rankIndex < 0 || rankIndex >= mult.Length) return 1f;
            return mult[rankIndex];
        }

        // === 内空间公式（原著第1430章）===

        /// <summary>内空间脉冲伤害：最大生命的5%</summary>
        public const float InnerSpacePulseFraction = 0.05f;

        /// <summary>内空间脉冲范围（格）</summary>
        public const int InnerSpacePulseRange = 10;

        /// <summary>内空间脉冲间隔（tick）</summary>
        public const int InnerSpacePulseInterval = 60;

        /// <summary>内空间领域内敌人速度倍率</summary>
        public const float InnerSpaceEnemySpeedMul = 0.7f;

        /// <summary>内空间领域内敌人攻速倍率</summary>
        public const float InnerSpaceEnemyAttackSpeedMul = 0.8f;

        /// <summary>计算内空间脉冲伤害</summary>
        public static float InnerSpacePulseDamage(float attackerMaxHealth)
        {
            return attackerMaxHealth * InnerSpacePulseFraction;
        }

        // === 光环公式 ===

        /// <summary>光环范围（格）</summary>
        public const int AuraRange = 8;

        /// <summary>光环压制持续时间（秒）</summary>
        public const float AuraSuppressDuration = 5f;

        /// <summary>光环眩晕概率：(阶差-3)*0.15，上限60%</summary>
        public static float AuraStunChance(int rankDiff)
        {
            return Mathf.Min((rankDiff - 3) * 0.15f, 0.60f);
        }

        // === 势力公式 ===

        /// <summary>势力成员经验加成：每多10人+5%，上限30%</summary>
        public static float FactionExpBonus(int memberCount)
        {
            return 1f + Mathf.Min(memberCount * 0.005f, 0.3f);
        }

        /// <summary>势力领袖伤害加成</summary>
        public const float FactionLeaderDamageBonus = 0.10f;

        /// <summary>最大势力数量</summary>
        public const int MaxFactions = 20;

        // === 神性公式 ===

        /// <summary>每层神性需要的点数</summary>
        public const int DivinityPointsPerLayer = 3;

        /// <summary>神性最大层数</summary>
        public const int DivinityMaxLayers = 10;
    }
}
