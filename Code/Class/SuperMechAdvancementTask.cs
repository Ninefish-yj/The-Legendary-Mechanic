using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechAdvancementTask
    {
        private enum ReqType { Knowledge, Attribute, TotalLevel, Craft, Divinity }

        private struct AdvanceReq
        {
            public ReqType type;
            public int intValue;
            public string statKey;
            public float statValue;
            public int totalLevel;
            public string desc;
        }

        private static readonly Dictionary<int, AdvanceReq> FixedReqs = new Dictionary<int, AdvanceReq>
        {
            { 1, new AdvanceReq { type = ReqType.Knowledge, intValue = 1, desc = LocalizedTextManager.getText("sm_task_knowledge_1") } },
            { 2, new AdvanceReq { type = ReqType.Craft, intValue = 5, desc = LocalizedTextManager.getText("sm_task_craft_5") } },
            { 4, new AdvanceReq { type = ReqType.Knowledge, intValue = 5, statKey = "intelligence", statValue = 400f, totalLevel = 80, desc = LocalizedTextManager.getText("sm_task_advanced") } },
            { 12, new AdvanceReq { type = ReqType.Divinity, intValue = 10, desc = LocalizedTextManager.getText("sm_task_divinity_10") } },
        };

        private static readonly float[] IntThresholds = { 0, 0, 50, 150, 400, 700, 1000, 1300, 1800, 2500, 3500, 5000, 7000 };
        private static readonly int[] TotalLevelThresholds = { 0, 20, 40, 60, 80, 100, 140, 180, 220, 260, 300, 320, 340 };
        private static readonly int[] KnowledgeThresholds = { 0, 1, 2, 3, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
        private static readonly int[] CraftThresholds = { 0, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        private static readonly Dictionary<long, Dictionary<int, AdvanceReq>> _unitReqs = new Dictionary<long, Dictionary<int, AdvanceReq>>();
        public static readonly Dictionary<long, int> _craftCount = new Dictionary<long, int>();

        public static void Register()
        {
        }

        private static AdvanceReq GetReq(Actor a, int stage)
        {
            if (FixedReqs.ContainsKey(stage)) return FixedReqs[stage];
            long id = a.data.id;
            if (!_unitReqs.ContainsKey(id)) _unitReqs[id] = new Dictionary<int, AdvanceReq>();
            if (!_unitReqs[id].ContainsKey(stage)) _unitReqs[id][stage] = RollRandomReq(a, stage);
            return _unitReqs[id][stage];
        }

        private static AdvanceReq RollRandomReq(Actor a, int stage)
        {
            bool isMech = a.hasTrait(SuperMechTraits.ClassMech);
            float r = Random.value;
            if (stage >= 8)
            {
                if (r < 0.5f) return new AdvanceReq { type = ReqType.Attribute, statKey = "intelligence", statValue = IntThresholds[stage], desc = $"sm_advancementtask_501" };
                if (r < 0.75f) return new AdvanceReq { type = ReqType.TotalLevel, intValue = TotalLevelThresholds[stage], desc = $"sm_advancementtask_502" };
                return new AdvanceReq { type = ReqType.Knowledge, intValue = KnowledgeThresholds[stage], desc = $"sm_advancementtask_503" };
            }
            else
            {
                if (isMech)
                {
                    if (r < 0.4f) return new AdvanceReq { type = ReqType.Knowledge, intValue = KnowledgeThresholds[stage], desc = $"sm_advancementtask_503" };
                    if (r < 0.7f) return new AdvanceReq { type = ReqType.Craft, intValue = CraftThresholds[stage], desc = $"sm_advancementtask_504" };
                    if (r < 0.9f) return new AdvanceReq { type = ReqType.Attribute, statKey = "intelligence", statValue = IntThresholds[stage], desc = $"sm_advancementtask_501" };
                    return new AdvanceReq { type = ReqType.TotalLevel, intValue = TotalLevelThresholds[stage], desc = $"sm_advancementtask_502" };
                }
                else
                {
                    if (r < 0.45f) return new AdvanceReq { type = ReqType.Knowledge, intValue = KnowledgeThresholds[stage], desc = $"sm_advancementtask_503" };
                    if (r < 0.75f) return new AdvanceReq { type = ReqType.Attribute, statKey = "intelligence", statValue = IntThresholds[stage], desc = $"sm_advancementtask_501" };
                    return new AdvanceReq { type = ReqType.TotalLevel, intValue = TotalLevelThresholds[stage], desc = $"sm_advancementtask_502" };
                }
            }
        }

        private static string GetKnowledgePrefix(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return "mech";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "martial";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "psi";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "mage";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "mind";
            return "mech";
        }

        public static bool CheckReq(Actor a, int stage)
        {
            if (stage <= 0 || stage >= 14) return false;
            AdvanceReq req = GetReq(a, stage);
            switch (req.type)
            {
                case ReqType.Knowledge:
                    string prefix = GetKnowledgePrefix(a);
                    bool knowledgeOk = SuperMechKnowledge.GetTierKnowledgeCount(a, prefix, 1) >= req.intValue;
                    if (!string.IsNullOrEmpty(req.statKey))
                    {
                        var stats = SuperMechStats.Of(a);
                        if (stats == null || stats[req.statKey] < req.statValue) return false;
                    }
                    if (req.totalLevel > 0 && SuperMechAwakened.GetTotalLevel(a) < req.totalLevel) return false;
                    return knowledgeOk;
                case ReqType.Attribute:
                    var s = SuperMechStats.Of(a);
                    return s != null && s[req.statKey] >= req.statValue;
                case ReqType.TotalLevel:
                    return SuperMechAwakened.GetTotalLevel(a) >= req.intValue;
                case ReqType.Craft:
                    return GetCraftCount(a) >= req.intValue;
                case ReqType.Divinity:
                    return SuperMechDivinity.GetTotalLayers(a) >= req.intValue;
                default:
                    return false;
            }
        }

        public static string GetReqText(Actor a)
        {
            if (!SuperMechAwakened.IsAwakened(a)) return null;
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0 || stage >= 14) return null;
            if (!SuperMechAwakened.CanAdvanceStage(a)) return null;
            AdvanceReq req = GetReq(a, stage);
            bool met = CheckReq(a, stage);
            return $"{(met ? "✓" : "✗")} {req.desc}";
        }

        public static bool TryAdvance(Actor a)
        {
            if (!SuperMechAwakened.IsAwakened(a)) return false;
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0 || stage >= 14) return false;
            if (!SuperMechAwakened.CanAdvanceStage(a)) return false;
            if (!CheckReq(a, stage)) return false;
            bool ok = SuperMechAwakened.TryAdvanceStage(a);
            return ok;
        }

        public static void TickTasks()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAwakened.IsAwakened(a)) continue;
                if (!SuperMechAwakened.CanAdvanceStage(a)) continue;
                TryAdvance(a);
            }
        }

        public static void OnCraft(Actor a)
        {
            if (a == null) return;
            long id = a.data.id;
            _craftCount.TryGetValue(id, out int c);
            _craftCount[id] = c + 1;
        }

        public static int GetCraftCount(Actor a)
        {
            if (a == null) return 0;
            _craftCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        public static void Clear()
        {
            _unitReqs.Clear();
            _craftCount.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_unitReqs, alive);
            removed += SuperMechCleanup.CleanDict(_craftCount, alive);
            return removed;
        }
    }
}
