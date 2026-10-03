using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.51.0 文明系统（原著还原）
    /// 文明=原版王国，按王国内高阶超能者实力划分等级：
    /// 普通→星际→星团→超星团→宇宙级（三大文明级别）
    /// 原著："对于高级文明来说，超A级只是争斗工具，文明本身才是宇宙的主角"
    /// </summary>
    public static class SuperMechCivilization
    {
        public enum CivLevel
        {
            Normal = 0,       // 普通文明（无A阶以上）
            Stellar = 1,      // 星际文明（≥1个A阶/天灾级）
            Cluster = 2,      // 星团级文明（≥3个A阶 或 ≥1个S阶/超A级）
            SuperCluster = 3, // 超星团级文明（≥1个SS阶/巅峰超A）
            Universal = 4     // 宇宙级文明（≥1个X阶/超神级，三大文明级别）
        }

        private static readonly Dictionary<long, CivLevel> _kingdomLevel = new Dictionary<long, CivLevel>();

        /// <summary>每tick扫描所有王国，计算文明等级</summary>
        public static void TickCivilizations()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            _kingdomLevel.Clear();

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                _kingdomLevel[k.getID()] = CalculateLevel(k);
            }
        }

        /// <summary>计算王国文明等级</summary>
        private static CivLevel CalculateLevel(Kingdom kingdom)
        {
            int countA = 0, countS = 0, countSS = 0, countX = 0;

            // 遍历王国所有单位，统计高阶超能者
            var units = World.world.units?.units_only_alive;
            if (units == null) return CivLevel.Normal;

            foreach (var a in units)
            {
                if (a == null || !a.isAlive()) continue;
                if (a.kingdom != kingdom) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank >= 13) countX++;        // X阶
                else if (rank >= 12) countSS++;  // SS阶
                else if (rank >= 10) countS++;   // S阶
                else if (rank >= 8) countA++;    // A阶
            }

            if (countX > 0) return CivLevel.Universal;
            if (countSS > 0) return CivLevel.SuperCluster;
            if (countS > 0 || countA >= 3) return CivLevel.Cluster;
            if (countA > 0) return CivLevel.Stellar;
            return CivLevel.Normal;
        }

        /// <summary>获取单位所属文明等级</summary>
        public static CivLevel GetCivLevel(Actor a)
        {
            if (a == null || a.kingdom == null) return CivLevel.Normal;
            long kid = a.kingdom.getID();
            if (_kingdomLevel.TryGetValue(kid, out var level)) return level;
            return CivLevel.Normal;
        }

        /// <summary>文明等级名称</summary>
        public static string GetLevelName(CivLevel level)
        {
            switch (level)
            {
                case CivLevel.Stellar: return "星际文明";
                case CivLevel.Cluster: return "星团级文明";
                case CivLevel.SuperCluster: return "超星团级文明";
                case CivLevel.Universal: return "宇宙级文明";
                default: return "普通文明";
            }
        }

        /// <summary>文明伤害加成（原著：文明本身才是主角，文明越强个体越强）
        /// 星际+5%，星团+10%，超星团+15%，宇宙级+25%</summary>
        public static float GetDamageBonus(CivLevel level)
        {
            switch (level)
            {
                case CivLevel.Stellar: return 1.05f;
                case CivLevel.Cluster: return 1.10f;
                case CivLevel.SuperCluster: return 1.15f;
                case CivLevel.Universal: return 1.25f;
                default: return 1f;
            }
        }

        public static void Clear()
        {
            _kingdomLevel.Clear();
        }
    }
}
