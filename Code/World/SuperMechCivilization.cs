using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.51.0 文明系统（原著还原）
    /// v0.51.1 文明科技发展：科技值随时间积累，高阶超能者加速，科技等级给全文明加成
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
        private static readonly Dictionary<long, float> _kingdomTechPoints = new Dictionary<long, float>();
        private static readonly Dictionary<long, int> _kingdomTechLevel = new Dictionary<long, int>();
        private static readonly Dictionary<long, long> _kingdomGuardian = new Dictionary<long, long>(); // kingdomId -> guardianActorId

        private const float TechBaseGain = 0.1f;     // 每tick基础科技增长
        private const float TechLevelThreshold = 100f; // 每级科技需要的点数
        private const int MaxTechLevel = 10;

        /// <summary>每tick扫描所有王国，计算文明等级并积累科技值</summary>
        public static void TickCivilizations()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            _kingdomLevel.Clear();

            var units = World.world.units?.units_only_alive;

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.getID();

                // 1. 计算文明等级
                var level = CalculateLevel(k, units);
                _kingdomLevel[kid] = level;

                // 2. 积累科技值（原著：超能者带来技术突破）
                if (!_kingdomTechPoints.ContainsKey(kid)) _kingdomTechPoints[kid] = 0f;
                float techGain = TechBaseGain * (1f + (int)level * 0.5f); // 文明等级越高基础增长越快
                // 高阶超能者加速科技发展
                if (units != null)
                {
                    foreach (var a in units)
                    {
                        if (a == null || !a.isAlive() || a.kingdom != k) continue;
                        int rank = SuperMechAdvancement.GetExactRankIndex(a);
                        if (rank >= 13) techGain += 10f;        // X阶
                        else if (rank >= 12) techGain += 5f;    // SS阶
                        else if (rank >= 10) techGain += 3f;    // S阶
                        else if (rank >= 8) techGain += 1f;     // A阶
                    }
                }
                _kingdomTechPoints[kid] += techGain;

                // 3. 科技升级
                int currentLevel = _kingdomTechLevel.ContainsKey(kid) ? _kingdomTechLevel[kid] : 0;
                if (currentLevel < MaxTechLevel && _kingdomTechPoints[kid] >= TechLevelThreshold * (currentLevel + 1))
                {
                    _kingdomTechLevel[kid] = currentLevel + 1;
                    Debug.Log($"[超神机械师] 文明科技突破：{k.name} 科技Lv{currentLevel + 1}");
                }

                // 4. 更新文明守护者（王国内最强超能者，原著：超A级是文明的战略武器）
                UpdateGuardian(k, units);
            }
        }

        /// <summary>计算王国文明等级</summary>
        private static CivLevel CalculateLevel(Kingdom kingdom, List<Actor> units)
        {
            int countA = 0, countS = 0, countSS = 0, countX = 0;
            if (units == null) return CivLevel.Normal;

            foreach (var a in units)
            {
                if (a == null || !a.isAlive()) continue;
                if (a.kingdom != kingdom) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank >= 13) countX++;
                else if (rank >= 12) countSS++;
                else if (rank >= 10) countS++;
                else if (rank >= 8) countA++;
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

        /// <summary>获取单位所属文明科技等级</summary>
        public static int GetTechLevel(Actor a)
        {
            if (a == null || a.kingdom == null) return 0;
            long kid = a.kingdom.getID();
            if (_kingdomTechLevel.TryGetValue(kid, out var level)) return level;
            return 0;
        }

        /// <summary>按王国获取科技等级（用于王国面板注入）</summary>
        public static int GetTechLevelFromKingdom(Kingdom k)
        {
            if (k == null) return 0;
            long kid = k.getID();
            if (_kingdomTechLevel.TryGetValue(kid, out var level)) return level;
            return 0;
        }

        /// <summary>v0.54.0 给文明增加科技值（代理战争击杀奖励等）</summary>
        public static void AddTechPoints(Kingdom k, float amount)
        {
            if (k == null || amount <= 0) return;
            long kid = k.getID();
            if (!_kingdomTechPoints.ContainsKey(kid)) _kingdomTechPoints[kid] = 0f;
            _kingdomTechPoints[kid] += amount;
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

        /// <summary>文明等级伤害加成（原著：文明本身才是主角）
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

        /// <summary>科技等级综合加成：每级+2%伤害/+2%生命/+1%攻速</summary>
        public static float GetTechDamageBonus(int techLevel)
        {
            return 1f + techLevel * 0.02f;
        }

        public static float GetTechHealthBonus(int techLevel)
        {
            return 1f + techLevel * 0.02f;
        }

        public static float GetTechSpeedBonus(int techLevel)
        {
            return 1f + techLevel * 0.01f;
        }

        /// <summary>更新文明守护者：王国内阶位最高的超能者</summary>
        private static void UpdateGuardian(Kingdom k, List<Actor> units)
        {
            long kid = k.getID();
            Actor best = null;
            int bestRank = -1;

            if (units != null)
            {
                foreach (var a in units)
                {
                    if (a == null || !a.isAlive() || a.kingdom != k) continue;
                    int rank = SuperMechAdvancement.GetExactRankIndex(a);
                    if (rank >= 8 && rank > bestRank) // A阶以上才能成为守护者
                    {
                        bestRank = rank;
                        best = a;
                    }
                }
            }

            if (best != null)
            {
                if (!_kingdomGuardian.ContainsKey(kid) || _kingdomGuardian[kid] != best.id)
                {
                    _kingdomGuardian[kid] = best.id;
                }
            }
            else
            {
                _kingdomGuardian.Remove(kid);
            }
        }

        /// <summary>判断单位是否是其文明的守护者</summary>
        public static bool IsGuardian(Actor a)
        {
            if (a == null || a.kingdom == null) return false;
            long kid = a.kingdom.getID();
            return _kingdomGuardian.TryGetValue(kid, out var gid) && gid == a.id;
        }

        /// <summary>守护者加成：伤害+10%/生命+10%/攻速+5%（原著：超A级是文明战略武器）</summary>
        public static float GetGuardianDamageBonus() => 1.10f;
        public static float GetGuardianHealthBonus() => 1.10f;
        public static float GetGuardianSpeedBonus() => 1.05f;

        /// <summary>守护者死亡时文明科技值损失20%（原著：文明失去战略武器）</summary>
        public static void OnGuardianDeath(Actor a)
        {
            if (a == null || a.kingdom == null) return;
            long kid = a.kingdom.getID();
            if (_kingdomGuardian.TryGetValue(kid, out var gid) && gid == a.id)
            {
                if (_kingdomTechPoints.TryGetValue(kid, out var pts))
                {
                    _kingdomTechPoints[kid] = pts * 0.8f;
                    Debug.Log($"[超神机械师] 文明守护者陨落：{a.name}，{a.kingdom.name}科技值损失20%");
                }
                _kingdomGuardian.Remove(kid);
            }
        }

        /// <summary>宇宙迭代时清零科技（原著：文明毁灭后科技丢失）</summary>
        public static void OnCosmicIteration()
        {
            _kingdomTechPoints.Clear();
            _kingdomTechLevel.Clear();
        }

        public static void Clear()
        {
            _kingdomLevel.Clear();
            _kingdomTechPoints.Clear();
            _kingdomTechLevel.Clear();
        }
    }
}
