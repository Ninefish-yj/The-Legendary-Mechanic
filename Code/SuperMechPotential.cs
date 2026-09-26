using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 潜能点系统（原著 ch3/ch50/ch611/ch1201）。
    /// 每次升级+1潜能点，用于学习知识树节点。
    /// 转职后其他分支尖端知识费用×3（ch611）。
    /// 高阶位给"觉醒点"替代潜能点（ch1201）。
    /// </summary>
    public static class SuperMechPotential
    {
        // 潜能点追踪（unit.id -> 潜能点数）
        private static readonly Dictionary<long, int> _potentialMap = new Dictionary<long, int>();
        // 觉醒点追踪（高阶位替代潜能点）
        private static readonly Dictionary<long, int> _awakeningMap = new Dictionary<long, int>();
        // 上次气力等级（用于检测升级给潜能点）
        private static readonly Dictionary<long, int> _lastQiLevel = new Dictionary<long, int>();

        /// <summary>获取单位潜能点。</summary>
        public static int GetPotential(Actor a)
        {
            if (a == null) return 0;
            int v; _potentialMap.TryGetValue(a.id, out v); return v;
        }

        /// <summary>获取单位觉醒点。</summary>
        public static int GetAwakening(Actor a)
        {
            if (a == null) return 0;
            int v; _awakeningMap.TryGetValue(a.id, out v); return v;
        }

        /// <summary>增加潜能点。</summary>
        public static void AddPotential(Actor a, int amount)
        {
            if (a == null) return;
            int cur = GetPotential(a);
            _potentialMap[a.id] = cur + amount;
        }

        /// <summary>直接设置潜能点（存档恢复用）。</summary>
        public static void SetPotential(Actor a, int amount)
        {
            if (a == null) return;
            _potentialMap[a.id] = Mathf.Max(0, amount);
        }

        /// <summary>消耗潜能点。返回是否成功。</summary>
        public static bool SpendPotential(Actor a, int amount)
        {
            if (a == null) return false;
            int cur = GetPotential(a);
            if (cur < amount) return false;
            _potentialMap[a.id] = cur - amount;
            return true;
        }

        /// <summary>检查知识节点是否已解锁。</summary>
        public static bool IsNodeUnlocked(Actor a, string nodeId)
        {
            return SuperMechKnowledge.IsUnlocked(a, nodeId);
        }

        /// <summary>获取知识节点的系别前缀。</summary>
        public static string GetKnowledgePrefix(string nodeId)
        {
            // 格式：sm_know_{前缀}_{阶}_{分支}_{序号}
            string[] parts = nodeId.Split('_');
            if (parts.Length >= 3) return parts[2];
            return "";
        }

        // 异能-职业搭配表（具体异能 -> 适合的职业前缀，搭配时学习消耗降低）
        private static readonly Dictionary<string, string[]> PowerClassSynergy = new Dictionary<string, string[]>
        {
            { "电磁操控", new[] { "mech" } },        // 电磁操控特别适合机械系
            { "能量亲和", new[] { "mech", "mage" } }, // 能量亲和适合机械/魔法
            { "虚拟意识", new[] { "mech", "mind" } }, // 虚拟意识适合机械/念力
            { "机械心灵", new[] { "mech" } },        // 机械心灵适合机械系
            { "纳米操控", new[] { "mech" } },        // 纳米操控适合机械系
            { "量子计算", new[] { "mech", "mind" } }, // 量子计算适合机械/念力
            { "体魄强化", new[] { "martial" } },     // 体魄强化适合武道
            { "气血澎湃", new[] { "martial" } },     // 气血澎湃适合武道
            { "战斗本能", new[] { "martial" } },     // 战斗本能适合武道
            { "气劲外放", new[] { "martial", "psi" } }, // 气劲外放适合武道/异能
            { "金刚不坏", new[] { "martial" } },     // 金刚不坏适合武道
            { "血脉觉醒", new[] { "martial", "psi" } }, // 血脉觉醒适合武道/异能
            { "元素异能", new[] { "mage", "psi" } },  // 元素异能适合魔法/异能
            { "身体变异", new[] { "martial", "psi" } }, // 身体变异适合武道/异能
            { "感官强化", new[] { "psi", "mind" } },  // 感官强化适合异能/念力
            { "再生能力", new[] { "martial", "psi" } }, // 再生能力适合武道/异能
            { "物质干涉", new[] { "psi", "mech" } },  // 物质干涉适合异能/机械
            { "能量放射", new[] { "psi", "mage" } },  // 能量放射适合异能/魔法
            { "元素魔法", new[] { "mage" } },        // 元素魔法适合魔法
            { "变化术", new[] { "mage", "psi" } },    // 变化术适合魔法/异能
            { "造物术", new[] { "mage", "mech" } },   // 造物术适合魔法/机械
            { "召唤术", new[] { "mage" } },          // 召唤术适合魔法
            { "结界术", new[] { "mage", "mind" } },   // 结界术适合魔法/念力
            { "符文魔法", new[] { "mage", "mech" } }, // 符文魔法适合魔法/机械
            { "念动力", new[] { "mind", "mech" } },   // 念动力适合念力/机械
            { "灵魂感知", new[] { "mind" } },        // 灵魂感知适合念力
            { "精神冲击", new[] { "mind" } },        // 精神冲击适合念力
            { "记忆操控", new[] { "mind" } },        // 记忆操控适合念力
            { "心灵感应", new[] { "mind", "psi" } },  // 心灵感应适合念力/异能
            { "预知未来", new[] { "mind", "psi" } },  // 预知未来适合念力/异能
        };

        /// <summary>获取单位的具体异能类型列表。</summary>
        private static List<string> GetSpecificPowers(Actor a)
        {
            var list = new List<string>();
            var talents = SuperMechTalent.GetTalents(a);
            foreach (var t in talents)
            {
                if (!string.IsNullOrEmpty(t.specificPower))
                    list.Add(t.specificPower);
            }
            return list;
        }

        /// <summary>判断具体异能是否和目标职业搭配。</summary>
        private static bool HasPowerSynergy(Actor a, string classPrefix)
        {
            var powers = GetSpecificPowers(a);
            foreach (var p in powers)
            {
                if (PowerClassSynergy.TryGetValue(p, out var classes))
                {
                    foreach (var c in classes)
                    {
                        if (c == classPrefix) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>判断知识节点是否是跨系兼修（和主职业方向不同）。五系天才不惩罚。</summary>
        public static bool IsCrossClass(Actor a, string nodeId)
        {
            if (SuperMechTalent.IsFiveSystemGenius(a)) return false;  // 五系天才跨系不惩罚
            string prefix = GetKnowledgePrefix(nodeId);
            string mainClass = SuperMechProfession.GetClass(a);
            if (string.IsNullOrEmpty(mainClass)) return false;
            string mainPrefix = SuperMechKnowledge.GetPrefixForClass(mainClass);
            return prefix != mainPrefix;
        }

        /// <summary>获取知识节点的实际消耗（跨系兼修受智力和异能搭配影响，原著ch611）。</summary>
        public static int GetActualCost(Actor a, string nodeId, int baseCost)
        {
            if (SuperMechTalent.IsFiveSystemGenius(a)) return baseCost;  // 五系天才无惩罚

            string prefix = GetKnowledgePrefix(nodeId);
            string mainClass = SuperMechProfession.GetClass(a);
            string mainPrefix = string.IsNullOrEmpty(mainClass) ? "" : SuperMechKnowledge.GetPrefixForClass(mainClass);
            bool isCross = prefix != mainPrefix;

            if (!isCross)
            {
                // 本系：异能搭配有加成
                if (HasPowerSynergy(a, prefix)) return Mathf.Max(1, (int)(baseCost * 0.7f));  // 搭配-30%
                return baseCost;
            }

            // 跨系兼修：智力门槛+异能搭配
            float intel = a.stats.intelligence;
            float multiplier = 3f;  // 默认×3

            // 智力门槛（原著：双修需要高智力，智力不够效率极低）
            if (intel < 10f) multiplier = 5f;       // 智力<10，×5
            else if (intel >= 20f) multiplier = 2f;  // 智力>=20，×2

            // 异能搭配加成（如果具体异能和兼修职业搭配，消耗降低）
            if (HasPowerSynergy(a, prefix)) multiplier *= 0.7f;  // 搭配-30%

            return Mathf.Max(1, (int)(baseCost * multiplier));
        }

        /// <summary>解锁知识节点（消耗潜能点，跨系兼修×3）。返回是否成功。</summary>
        public static bool UnlockNode(Actor a, string nodeId, int cost)
        {
            if (a == null) return false;
            if (IsNodeUnlocked(a, nodeId)) return false;
            int actualCost = GetActualCost(a, nodeId, cost);
            if (!SpendPotential(a, actualCost)) return false;
            SuperMechKnowledge.Unlock(a, nodeId);
            // 降临者学习知识获得经验（ch132：学基础组装得1000经验）
            if (SuperMechAwakened.IsAwakened(a))
            {
                SuperMechAwakened.AddXp(a, 1000f);
            }
            bool cross = IsCrossClass(a, nodeId);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 解锁知识节点 {nodeId}（消耗{actualCost}潜能点{(cross ? "，跨系兼修×3" : "")}，剩余{GetPotential(a)}）");
            return true;
        }

        /// <summary>
        /// 每tick检测气力等级提升，给潜能点。
        /// 原著：每次升级+1潜能点（ch3/ch50）。
        /// 这里用气力量级提升模拟升级（每升1级气力=1潜能点）。
        /// </summary>
        public static void TickPotential()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (!SuperMechAwakened.IsAwakened(a)) continue; // 星海人不走潜能点，走传承度

                float qi = SuperMechQi.GetQi(a);
                int curLv = SuperMechQi.GetLevel(qi);
                int lastLv;
                _lastQiLevel.TryGetValue(a.id, out lastLv);

                if (curLv > lastLv)
                {
                    int gained = curLv - lastLv;
                    // 高阶位（S级以上）给觉醒点而非潜能点
                    if (a.hasTrait("sm_rank_10_s") || a.hasTrait("sm_rank_11_s_plus") || a.hasTrait("sm_rank_12_ss") || a.hasTrait("sm_rank_13_x"))
                    {
                        int curAw = GetAwakening(a);
                        _awakeningMap[a.id] = curAw + gained;
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师] {a.name} 气力Lv{lastLv}→Lv{curLv}，获得{gained}觉醒点");
                    }
                    else
                    {
                        AddPotential(a, gained);
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师] {a.name} 气力Lv{lastLv}→Lv{curLv}，获得{gained}潜能点");
                    }
                    _lastQiLevel[a.id] = curLv;
                }
                else if (lastLv == 0 && curLv > 0)
                {
                    _lastQiLevel[a.id] = curLv;
                }
            }
        }

        /// <summary>强制解锁知识节点（不消耗潜能点，星海人传承度用）。</summary>
        public static void ForceUnlockNode(Actor a, string nodeId)
        {
            if (a == null) return;
            if (IsNodeUnlocked(a, nodeId)) return;
            SuperMechKnowledge.Unlock(a, nodeId);
        }

        /// <summary>获取单位已解锁节点数量。</summary>
        public static int GetUnlockedCount(Actor a)
        {
            if (a == null) return 0;
            // 统计五系已解锁知识
            int total = 0;
            total += SuperMechKnowledge.GetUnlockedCount(a, "mech");
            total += SuperMechKnowledge.GetUnlockedCount(a, "martial");
            total += SuperMechKnowledge.GetUnlockedCount(a, "psi");
            total += SuperMechKnowledge.GetUnlockedCount(a, "mage");
            total += SuperMechKnowledge.GetUnlockedCount(a, "mind");
            return total;
        }

        /// <summary>清空所有潜能点数据（世界切换用）。</summary>
        public static void Clear()
        {
            _potentialMap.Clear();
            _awakeningMap.Clear();
            _lastQiLevel.Clear();
            // 知识解锁数据在SuperMechKnowledge中清理
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_potentialMap, alive);
            removed += SuperMechCleanup.CleanDict(_awakeningMap, alive);
            removed += SuperMechCleanup.CleanDict(_lastQiLevel, alive);
            return removed;
        }
    }
}
