using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力分系用途（原著 ch49/ch50/ch269）：
    /// 气力是统一核心属性，不同系用途不同：
    ///   武道系 → 格斗能量（已在 SuperMechQi 里加 warfare/damage）
    ///   机械系 → 制造能量（磁环后叫械力）
    ///   异能系 → 基因链（通过基因树知识解锁提升，三维度：能级/操控/持久力）
    ///   魔法系 → 魔力池（通过魔法知识树解锁提升）
    ///   念力系 → 精神力（通过精神修炼树解锁提升）
    /// 阶段由知识树解锁数决定，不是气力阈值（原著：二阶基因链是进阶知识）。
    /// 不注册特质，用内部字典追踪，单位面板显示。
    /// </summary>
    public static class SuperMechCorePower
    {
        // 异能系：基因链5阶（原著：一阶→二阶→...，通过基因树知识提升）
        public static readonly string[] GeneChainNames = {
            "一阶基因链", "二阶基因链", "三阶基因链", "四阶基因链", "五阶基因链"
        };
        // 每阶需要的知识解锁数（基础+进阶+高端+尖端+终极）
        public static readonly int[] GeneChainKnowledgeReq = { 0, 3, 8, 15, 25 };

        // 魔法系：魔力池5层
        public static readonly string[] ManaTierNames = {
            "魔力初涌", "魔力流动", "魔力充盈", "魔力磅礴", "魔力浩瀚"
        };
        public static readonly int[] ManaTierKnowledgeReq = { 0, 3, 8, 15, 25 };

        // 念力系：精神力5阶
        public static readonly string[] MindTierNames = {
            "精神觉醒", "精神外放", "精神干涉", "精神领域", "精神造物"
        };
        public static readonly int[] MindTierKnowledgeReq = { 0, 3, 8, 15, 25 };

        // 内部字典追踪当前阶段（unit.id -> stage 1-5）
        private static readonly Dictionary<string, int> _geneStage = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _manaStage = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _mindStage = new Dictionary<string, int>();

        public static void Register()
        {
            // 不注册特质，阶段由知识树解锁数决定，单位面板显示
            Debug.Log("[超神机械师] 气力分系用途系统初始化：基因链/魔力池/精神力（知识树驱动，不注册特质）");
        }

        /// <summary>按知识树解锁数自动更新各系用途等级。</summary>
        public static void TickCorePowers()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 异能系：基因链（基因树知识解锁数决定阶段）
                if (a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    int knowCount = SuperMechKnowledge.GetUnlockedCount(a, "psi");
                    int targetLv = CalcStageByKnowledge(knowCount, GeneChainKnowledgeReq);
                    ApplyStage(a, _geneStage, targetLv);
                }

                // 魔法系：魔力池（魔法知识树解锁数决定阶段）
                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    int knowCount = SuperMechKnowledge.GetUnlockedCount(a, "mage");
                    int targetLv = CalcStageByKnowledge(knowCount, ManaTierKnowledgeReq);
                    ApplyStage(a, _manaStage, targetLv);
                }

                // 念力系：精神力（精神修炼树解锁数决定阶段）
                if (a.hasTrait(SuperMechTraits.ClassMind))
                {
                    int knowCount = SuperMechKnowledge.GetUnlockedCount(a, "mind");
                    int targetLv = CalcStageByKnowledge(knowCount, MindTierKnowledgeReq);
                    ApplyStage(a, _mindStage, targetLv);
                }
            }
        }

        /// <summary>根据知识解锁数计算阶段（1-5）。</summary>
        private static int CalcStageByKnowledge(int knowCount, int[] reqs)
        {
            int stage = 1;
            for (int i = reqs.Length - 1; i >= 0; i--)
            {
                if (knowCount >= reqs[i]) { stage = i + 1; break; }
            }
            return stage;
        }

        /// <summary>应用阶段（只在变化时更新字典）。</summary>
        private static void ApplyStage(Actor a, Dictionary<string, int> dict, int targetLv)
        {
            string id = a.data.id;
            if (dict.TryGetValue(id, out int cur) && cur == targetLv) return;
            dict[id] = targetLv;
        }

        /// <summary>获取异能系当前基因链阶段名。</summary>
        public static string GetGeneStageName(Actor a)
        {
            if (a == null) return "基因未觉醒";
            if (_geneStage.TryGetValue(a.data.id, out int lv) && lv >= 1 && lv <= GeneChainNames.Length)
                return GeneChainNames[lv - 1];
            return "一阶基因链";
        }

        /// <summary>获取魔法系当前魔力池阶段名。</summary>
        public static string GetManaStageName(Actor a)
        {
            if (a == null) return "魔力未觉醒";
            if (_manaStage.TryGetValue(a.data.id, out int lv) && lv >= 1 && lv <= ManaTierNames.Length)
                return ManaTierNames[lv - 1];
            return "魔力初涌";
        }

        /// <summary>获取念力系当前精神力阶段名。</summary>
        public static string GetMindStageName(Actor a)
        {
            if (a == null) return "精神未觉醒";
            if (_mindStage.TryGetValue(a.data.id, out int lv) && lv >= 1 && lv <= MindTierNames.Length)
                return MindTierNames[lv - 1];
            return "精神觉醒";
        }

        /// <summary>获取异能系基因链阶段索引（1-5）。</summary>
        public static int GetGeneStage(Actor a)
        {
            if (a == null) return 1;
            return _geneStage.TryGetValue(a.data.id, out int lv) ? lv : 1;
        }

        /// <summary>获取魔法系魔力池阶段索引（1-5）。</summary>
        public static int GetManaStage(Actor a)
        {
            if (a == null) return 1;
            return _manaStage.TryGetValue(a.data.id, out int lv) ? lv : 1;
        }

        /// <summary>获取念力系精神力阶段索引（1-5）。</summary>
        public static int GetMindStage(Actor a)
        {
            if (a == null) return 1;
            return _mindStage.TryGetValue(a.data.id, out int lv) ? lv : 1;
        }

        /// <summary>清空核心能量数据（世界切换用）。</summary>
        public static void Clear()
        {
            _geneStage.Clear();
            _manaStage.Clear();
            _mindStage.Clear();
        }
    }
}
