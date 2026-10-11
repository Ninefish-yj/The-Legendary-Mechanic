using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 能级压制系统（从CombatPatches拆分）
    /// 基于攻击者/防御者能级比计算压制等级，用于伤害加成和UI显示
    /// </summary>
    public static class CombatSuppression
    {
        public enum Level
        {
            None = 0,        // 无压制（能级比<1.1）
            Minor = 1,       // 轻微压制（1.1~1.5）
            Moderate = 2,    // 明显压制（1.5~2）
            Strong = 3,      // 强烈压制（2~5）
            Overwhelm = 4,   // 碾压（5~10）
            Annihilate = 5   // 秒杀级（>10）
        }

        /// <summary>获取能级压制等级</summary>
        public static Level GetLevel(float attackerEnergy, float defenderEnergy)
        {
            if (attackerEnergy <= 0 || defenderEnergy <= 0) return Level.None;
            float ratio = attackerEnergy / defenderEnergy;
            if (ratio >= 10f) return Level.Annihilate;
            if (ratio >= 5f) return Level.Overwhelm;
            if (ratio >= 2f) return Level.Strong;
            if (ratio >= 1.5f) return Level.Moderate;
            if (ratio >= 1.1f) return Level.Minor;
            return Level.None;
        }

        /// <summary>获取能级压制描述（用于UI/日志）</summary>
        public static string GetDescription(Level level)
        {
            switch (level)
            {
                case Level.Minor: return LocalizedTextManager.getText("sm_combat_suppress_minor");
                case Level.Moderate: return LocalizedTextManager.getText("sm_combat_suppress_moderate");
                case Level.Strong: return LocalizedTextManager.getText("sm_combat_suppress_strong");
                case Level.Overwhelm: return LocalizedTextManager.getText("sm_combat_suppress_overwhelm");
                case Level.Annihilate: return LocalizedTextManager.getText("sm_combat_suppress_annihilate");
                default: return LocalizedTextManager.getText("sm_combat_suppress_none");
            }
        }

        /// <summary>获取压制伤害倍率</summary>
        public static float GetDamageMultiplier(Level level)
        {
            switch (level)
            {
                case Level.Minor: return 1.05f;
                case Level.Moderate: return 1.10f;
                case Level.Strong: return 1.20f;
                case Level.Overwhelm: return 1.35f;
                case Level.Annihilate: return 1.50f;
                default: return 1f;
            }
        }

        /// <summary>
        /// 阶位差距伤害减免（对照原著：高阶位对低阶位攻击有极高防御）
        /// 原著依据："一般天灾级攻击对他而言已经是挠痒痒了，甚至稍弱一点的天灾级还破不了他的防御"
        /// 设计原则：高减免但不免疫，差距越大减免越多，最高92%（永远留8%最低伤害）
        /// </summary>
        /// <param name="attackerRankIndex">攻击者阶位索引(0-13)</param>
        /// <param name="defenderRankIndex">防御者阶位索引(0-13)</param>
        /// <returns>伤害减免比例(0~0.92)，0表示无减免</returns>
        public static float GetRankDamageReduction(int attackerRankIndex, int defenderRankIndex)
        {
            if (attackerRankIndex < 0 || defenderRankIndex < 0) return 0f;

            int gap = defenderRankIndex - attackerRankIndex;
            if (gap <= 1) return 0f;               // 差距0-1：无减免，同阶位正常战斗
            if (gap == 2) return 0.15f;             // 差距2：15%减免
            if (gap == 3) return 0.30f;             // 差距3：30%减免
            if (gap == 4) return 0.45f;             // 差距4：45%减免（挠痒痒级别）
            if (gap == 5) return 0.60f;             // 差距5：60%减免
            if (gap == 6) return 0.75f;             // 差距6：75%减免
            if (gap == 7) return 0.85f;             // 差距7：85%减免
            return 0.92f;                           // 差距8+：92%减免（几乎无效，但不免疫）
        }

        /// <summary>获取阶位差距减免描述（用于UI/日志）</summary>
        public static string GetRankReductionDescription(int gap)
        {
            if (gap <= 1) return LocalizedTextManager.getText("sm_rank_reduction_none");
            if (gap == 2) return LocalizedTextManager.getText("sm_rank_reduction_minor");
            if (gap == 3) return LocalizedTextManager.getText("sm_rank_reduction_moderate");
            if (gap == 4) return LocalizedTextManager.getText("sm_rank_reduction_strong");
            if (gap == 5) return LocalizedTextManager.getText("sm_rank_reduction_overwhelm");
            if (gap >= 6) return LocalizedTextManager.getText("sm_rank_reduction_annihilate");
            return LocalizedTextManager.getText("sm_rank_reduction_none");
        }
    }
}
