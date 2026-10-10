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
    }
}
