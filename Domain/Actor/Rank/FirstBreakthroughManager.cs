using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 首位突破管理器
    /// 单一职责：只负责首位突破者的奖励和记录
    /// 从Advancement中提取，遵循单一职责原则
    /// 原著：第一个超A级打破了某种极限，给后来者指引了道路
    /// </summary>
    public static class FirstBreakthroughManager
    {
        /// <summary>检查是否为该阶位首位突破者（基于阶位名称）</summary>
        public static bool IsFirstBreakthrough(string rankName)
        {
            return !SuperMechSaveData.FirstBreakthroughRanks.Contains(rankName);
        }

        /// <summary>记录首位突破（基于阶位名称）</summary>
        public static void RecordFirstBreakthrough(string rankName)
        {
            SuperMechSaveData.FirstBreakthroughRanks.Add(rankName);
        }

        /// <summary>应用首位突破奖励</summary>
        public static void ApplyBonus(Actor a, int rankIdx)
        {
            if (a == null) return;
            // 按阶位高低给予不同的潜能点奖励
            int bonusPoints = rankIdx switch
            {
                >= 14 => 50,   // X阶 超神级
                >= 11 => 30,   // S阶以上
                >= 9 => 20,    // A阶以上
                >= 7 => 10,    // B阶以上
                >= 5 => 5,     // C阶以上
                _ => 2         // D阶以下
            };
            SuperMechPotential.AddPotential(a, bonusPoints);
            // 额外气力加成
            float qiBonus = rankIdx * 100f;
            SuperMechQi.AddQi(a, qiBonus);
            Debug.Log($"[超神机械师] 首位突破奖励：{a.name} 获得{bonusPoints}潜能点+{qiBonus}气力");
        }
    }
}
