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
        // 已解锁的知识节点（unit.id -> HashSet<nodeId>）
        private static readonly Dictionary<long, HashSet<string>> _unlockedNodes = new Dictionary<long, HashSet<string>>();

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
            if (a == null) return false;
            HashSet<string> set;
            if (_unlockedNodes.TryGetValue(a.id, out set))
                return set.Contains(nodeId);
            return false;
        }

        /// <summary>解锁知识节点（消耗潜能点）。返回是否成功。</summary>
        public static bool UnlockNode(Actor a, string nodeId, int cost)
        {
            if (a == null) return false;
            if (IsNodeUnlocked(a, nodeId)) return false;
            if (!SpendPotential(a, cost)) return false;
            HashSet<string> set;
            if (!_unlockedNodes.TryGetValue(a.id, out set))
            {
                set = new HashSet<string>();
                _unlockedNodes[a.id] = set;
            }
            set.Add(nodeId);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 解锁知识节点 {nodeId}（消耗{cost}潜能点，剩余{GetPotential(a)}）");
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
                if (!SuperMechAwakened.IsAwakened(a)) continue; // 土著不走潜能点，走传承度

                float qi = SuperMechQi.GetQi(a);
                int curLv = SuperMechQi.GetLevel(qi);
                int lastLv;
                _lastQiLevel.TryGetValue(a.id, out lastLv);

                if (curLv > lastLv)
                {
                    int gained = curLv - lastLv;
                    // 高阶位（S级以上）给觉醒点而非潜能点
                    if (a.hasTrait("sm_rank_s") || a.hasTrait("sm_rank_s_plus") || a.hasTrait("sm_rank_ss") || a.hasTrait("sm_rank_x"))
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

        /// <summary>强制解锁知识节点（不消耗潜能点，土著传承度用）。</summary>
        public static void ForceUnlockNode(Actor a, string nodeId)
        {
            if (a == null) return;
            if (IsNodeUnlocked(a, nodeId)) return;
            HashSet<string> set;
            if (!_unlockedNodes.TryGetValue(a.id, out set))
            {
                set = new HashSet<string>();
                _unlockedNodes[a.id] = set;
            }
            set.Add(nodeId);
        }

        /// <summary>获取单位已解锁节点数量。</summary>
        public static int GetUnlockedCount(Actor a)
        {
            if (a == null) return 0;
            HashSet<string> set;
            if (_unlockedNodes.TryGetValue(a.id, out set)) return set.Count;
            return 0;
        }

        /// <summary>清空所有潜能点数据（世界切换用）。</summary>
        public static void Clear()
        {
            _potentialMap.Clear();
            _awakeningMap.Clear();
            _lastQiLevel.Clear();
            _unlockedNodes.Clear();
        }
    }
}
