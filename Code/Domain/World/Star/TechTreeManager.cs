using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.231 阵营科技树管理器（战锤40K式）
    /// 6大科技领域，5个层级，研究解锁舰船/建筑/加成
    /// </summary>
    public static class TechTreeManager
    {
        private static List<TechNode> _techTree = new List<TechNode>();
        private static HashSet<string> _researched = new HashSet<string>();
        private static string _currentResearch;
        private static float _researchProgress;
        private static bool _initialized;

        // 科技领域名称
        private static readonly string[] FieldNames = {
            "能源", "武器", "护盾", "引擎", "生物", "工业"
        };

        /// <summary>初始化科技树</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            BuildTechTree();
            Debug.Log($"[超神机械师] 科技树初始化：{_techTree.Count}个科技节点");
        }

        /// <summary>构建科技树（6领域×5层）</summary>
        private static void BuildTechTree()
        {
            string[] fieldIds = { "energy", "weapons", "shields", "engines", "biology", "industry" };
            string[][] tierNames = {
                new[] { "基础能源", "聚变能源", "反物质能源", "零点能源", "维度能源" },
                new[] { "动能武器", "激光武器", "等离子武器", "反物质武器", "维度武器" },
                new[] { "基础护盾", "能量护盾", "等离子护盾", "相位护盾", "维度护盾" },
                new[] { "化学引擎", "离子引擎", "曲率引擎", "超光速引擎", "跃迁引擎" },
                new[] { "基因改造", "克隆技术", "意识上传", "纳米修复", "生命升华" },
                new[] { "基础工业", "自动化", "纳米制造", "量子制造", "物质重组" }
            };

            for (int f = 0; f < 6; f++)
            {
                for (int t = 0; t < 5; t++)
                {
                    var node = new TechNode
                    {
                        id = $"{fieldIds[f]}_t{t + 1}",
                        name = tierNames[f][t],
                        description = $"解锁{tierNames[f][t]}相关技术",
                        field = (TechField)f,
                        tier = t + 1,
                        cost = 100 * (t + 1) * (t + 1),
                        researchTime = 30f * (t + 1),
                        isResearched = false
                    };
                    // 前置科技：同领域上一层
                    if (t > 0)
                        node.prerequisites.Add($"{fieldIds[f]}_t{t}");
                    // 效果
                    node.effects[$"{fieldIds[f]}_bonus"] = 0.1f * (t + 1);
                    _techTree.Add(node);
                }
            }
        }

        /// <summary>获取科技领域名称</summary>
        public static string GetFieldName(TechField field) => FieldNames[(int)field];

        /// <summary>获取所有科技节点</summary>
        public static List<TechNode> GetAllTech() => _techTree;

        /// <summary>按领域获取科技</summary>
        public static List<TechNode> GetTechByField(TechField field)
        {
            return _techTree.FindAll(t => t.field == field);
        }

        /// <summary>科技是否已研究</summary>
        public static bool IsResearched(string techId) => _researched.Contains(techId);

        /// <summary>是否可以研究（前置已完成且未研究）</summary>
        public static bool CanResearch(string techId)
        {
            var node = _techTree.Find(t => t.id == techId);
            if (node == null || node.isResearched) return false;
            if (!string.IsNullOrEmpty(_currentResearch)) return false;
            foreach (var pre in node.prerequisites)
                if (!_researched.Contains(pre)) return false;
            return true;
        }

        /// <summary>开始研究</summary>
        public static bool StartResearch(string techId)
        {
            if (!CanResearch(techId)) return false;
            var node = _techTree.Find(t => t.id == techId);
            var planet = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (planet == null || planet.resource < node.cost) return false;

            planet.resource -= node.cost;
            _currentResearch = techId;
            _researchProgress = 0f;
            Debug.Log($"[超神机械师] 开始研究：{node.name}，花费{node.cost}");
            return true;
        }

        /// <summary>研究tick</summary>
        public static void ResearchTick(float deltaTime)
        {
            if (string.IsNullOrEmpty(_currentResearch)) return;
            var node = _techTree.Find(t => t.id == _currentResearch);
            if (node == null) { _currentResearch = null; return; }

            // 研究站加成
            float speedMultiplier = 1f;
            var stations = StationManager.GetAllStations();
            foreach (var s in stations)
                if (s.type == StationType.Research)
                    speedMultiplier += 0.15f * s.level;

            _researchProgress += deltaTime * speedMultiplier;
            if (_researchProgress >= node.researchTime)
            {
                CompleteResearch(node);
            }
        }

        /// <summary>完成研究</summary>
        private static void CompleteResearch(TechNode node)
        {
            node.isResearched = true;
            _researched.Add(node.id);
            _currentResearch = null;
            _researchProgress = 0f;
            Debug.Log($"[超神机械师] 研究完成：{node.name}");
        }

        /// <summary>获取当前研究进度（0-1）</summary>
        public static float GetResearchProgress()
        {
            if (string.IsNullOrEmpty(_currentResearch)) return 0f;
            var node = _techTree.Find(t => t.id == _currentResearch);
            if (node == null) return 0f;
            return Mathf.Clamp01(_researchProgress / node.researchTime);
        }

        /// <summary>获取当前研究的科技</summary>
        public static TechNode GetCurrentResearch()
        {
            if (string.IsNullOrEmpty(_currentResearch)) return null;
            return _techTree.Find(t => t.id == _currentResearch);
        }

        /// <summary>获取某领域的总加成</summary>
        public static float GetFieldBonus(TechField field)
        {
            float bonus = 0f;
            foreach (var id in _researched)
            {
                var node = _techTree.Find(t => t.id == id);
                if (node != null && node.field == field)
                    bonus += 0.1f * node.tier;
            }
            return bonus;
        }

        /// <summary>获取已研究科技数量</summary>
        public static int GetResearchedCount() => _researched.Count;

        /// <summary>获取科技树总进度（0-1）</summary>
        public static float GetTotalProgress()
        {
            return _techTree.Count > 0 ? (float)_researched.Count / _techTree.Count : 0f;
        }
    }
}
