using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPotential
    {
        private static readonly Dictionary<long, int> _potentialMap = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _awakeningMap = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _lastQiLevel = new Dictionary<long, int>();

        public static int GetPotential(Actor a)
        {
            if (a == null) return 0;
            int v; _potentialMap.TryGetValue(a.id, out v); return v;
        }

        public static int GetAwakening(Actor a)
        {
            if (a == null) return 0;
            int v; _awakeningMap.TryGetValue(a.id, out v); return v;
        }

        public static void AddPotential(Actor a, int amount)
        {
            if (a == null) return;
            int cur = GetPotential(a);
            _potentialMap[a.id] = cur + amount;
        }

        public static void SetPotential(Actor a, int amount)
        {
            if (a == null) return;
            _potentialMap[a.id] = Mathf.Max(0, amount);
        }

        public static bool SpendPotential(Actor a, int amount)
        {
            if (a == null) return false;
            int cur = GetPotential(a);
            if (cur < amount) return false;
            _potentialMap[a.id] = cur - amount;
            return true;
        }

        public static bool IsNodeUnlocked(Actor a, string nodeId)
        {
            return SuperMechKnowledge.IsUnlocked(a, nodeId);
        }

        public static string GetKnowledgePrefix(string nodeId)
        {
            string[] parts = nodeId.Split('_');
            if (parts.Length >= 3) return parts[2];
            return "";
        }

        private static readonly Dictionary<string, string[]> PowerClassSynergy = new Dictionary<string, string[]>
        {
            { "sm_potential_1335", new[] { "mech" } },
            { "sm_potential_1336", new[] { "mech", "mage" } },
            { "sm_potential_1337", new[] { "mech", "mind" } },
            { "sm_potential_1338", new[] { "mech" } },
            { "sm_potential_1339", new[] { "mech" } },
            { "sm_potential_1340", new[] { "mech", "mind" } },
            { "sm_potential_1341", new[] { "martial" } },
            { "sm_potential_1342", new[] { "martial" } },
            { "sm_potential_1343", new[] { "martial" } },
            { "sm_potential_1344", new[] { "martial", "psi" } },
            { "sm_potential_1345", new[] { "martial" } },
            { "sm_potential_1346", new[] { "martial", "psi" } },
            { "sm_potential_1347", new[] { "mage", "psi" } },
            { "sm_potential_1348", new[] { "martial", "psi" } },
            { "sm_potential_1349", new[] { "psi", "mind" } },
            { "sm_potential_1350", new[] { "martial", "psi" } },
            { "sm_potential_1351", new[] { "psi", "mech" } },
            { "sm_potential_1352", new[] { "psi", "mage" } },
            { "sm_potential_1353", new[] { "mage" } },
            { "sm_potential_1354", new[] { "mage", "psi" } },
            { "sm_potential_1355", new[] { "mage", "mech" } },
            { "sm_potential_1356", new[] { "mage" } },
            { "sm_potential_1357", new[] { "mage", "mind" } },
            { "sm_potential_1358", new[] { "mage", "mech" } },
            { "sm_potential_1359", new[] { "mind", "mech" } },
            { "sm_potential_1360", new[] { "mind" } },
            { "sm_potential_1361", new[] { "mind" } },
            { "sm_potential_1362", new[] { "mind" } },
            { "sm_potential_1363", new[] { "mind", "psi" } },
            { "sm_potential_1364", new[] { "mind", "psi" } },
        };

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

        public static bool HasPowerSynergy(Actor a, string classPrefix)
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

        public static bool IsCrossClass(Actor a, string nodeId)
        {
            if (SuperMechTalent.IsFiveSystemGenius(a)) return false;
            string prefix = GetKnowledgePrefix(nodeId);
            string mainClass = SuperMechProfession.GetClass(a);
            if (string.IsNullOrEmpty(mainClass)) return false;
            string mainPrefix = SuperMechKnowledge.GetPrefixForClass(mainClass);
            return prefix != mainPrefix;
        }

        public static int GetActualCost(Actor a, string nodeId, int baseCost)
        {
            if (a == null || a.stats == null) return baseCost;
            if (SuperMechTalent.IsFiveSystemGenius(a)) return baseCost;

            string prefix = GetKnowledgePrefix(nodeId);
            string mainClass = SuperMechProfession.GetClass(a);
            string mainPrefix = string.IsNullOrEmpty(mainClass) ? "" : SuperMechKnowledge.GetPrefixForClass(mainClass);
            bool isCross = prefix != mainPrefix;

            if (!isCross)
            {
                if (HasPowerSynergy(a, prefix)) return Mathf.Max(1, (int)(baseCost * 0.7f));
                return baseCost;
            }

            float intel = a.stats["intelligence"];
            float multiplier = 3f;

            if (intel < 10f) multiplier = 5f;
            else if (intel >= 20f) multiplier = 2f;

            if (HasPowerSynergy(a, prefix)) multiplier *= 0.7f;

            return Mathf.Max(1, (int)(baseCost * multiplier));
        }

        public static bool UnlockNode(Actor a, string nodeId, int cost)
        {
            if (a == null) return false;
            if (IsNodeUnlocked(a, nodeId)) return false;
            int actualCost = GetActualCost(a, nodeId, cost);
            if (!SpendPotential(a, actualCost)) return false;
            SuperMechKnowledge.Unlock(a, nodeId);
            if (SuperMechAwakened.IsAwakened(a))
            {
                SuperMechAwakened.AddXp(a, 1000f);
            }
            bool cross = IsCrossClass(a, nodeId);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 解锁知识节点 {nodeId}（消耗{actualCost}潜能点{(cross ? "，跨系兼修×3" : "")}，剩余{GetPotential(a)}）");
            return true;
        }

        public static void TickPotential()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (!SuperMechAwakened.IsAwakened(a)) continue;

                float qi = SuperMechQi.GetQi(a);
                int curLv = SuperMechQi.GetLevel(qi);
                int lastLv;
                _lastQiLevel.TryGetValue(a.id, out lastLv);

                if (curLv > lastLv)
                {
                    int gained = curLv - lastLv;
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

        public static void ForceUnlockNode(Actor a, string nodeId)
        {
            if (a == null) return;
            if (IsNodeUnlocked(a, nodeId)) return;
            SuperMechKnowledge.Unlock(a, nodeId);
        }

        public static int GetUnlockedCount(Actor a)
        {
            if (a == null) return 0;
            int total = 0;
            total += SuperMechKnowledge.GetUnlockedCount(a, "mech");
            total += SuperMechKnowledge.GetUnlockedCount(a, "martial");
            total += SuperMechKnowledge.GetUnlockedCount(a, "psi");
            total += SuperMechKnowledge.GetUnlockedCount(a, "mage");
            total += SuperMechKnowledge.GetUnlockedCount(a, "mind");
            return total;
        }

        public static void Clear()
        {
            _potentialMap.Clear();
            _awakeningMap.Clear();
            _lastQiLevel.Clear();
        }

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
