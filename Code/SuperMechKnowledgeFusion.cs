using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识融合系统：消耗经验将多个知识融合，随机获得图纸/装备/特殊加成。
    /// 原著ch107："[是否进行知识融合（基础电磁原理lv4、基础能源理论lv3...），本次消耗2万经验]"
    /// ch121/ch136/ch141/ch159/ch168：知识融合是机械师获得图纸的核心方式。
    /// </summary>
    public static class SuperMechKnowledgeFusion
    {
        /// <summary>融合配方定义。</summary>
        public class FusionRecipe
        {
            public string id;
            public string name;           // 产物名称
            public string desc;
            public string[] requiredKnowledge; // 需要的知识ID（全部满足才能融合）
            public int xpCost;           // 经验消耗
            public float successRate;    // 基础成功率
            public string resultType;    // "equip" / "knowledge" / "potential" / "qi"
            public string resultId;      // 产物ID（装备ID/知识ID）
            public int resultValue;      // 产物数值（潜能点/气力）
            public int minTier;          // 最低知识阶位要求
        }

        /// <summary>所有融合配方。</summary>
        private static readonly List<FusionRecipe> _recipes = new List<FusionRecipe>();

        /// <summary>单位已融合出的图纸（actorId → HashSet<recipeId>）。</summary>
        private static readonly Dictionary<long, HashSet<string>> _unlockedRecipes = new Dictionary<long, HashSet<string>>();

        /// <summary>融合冷却（actorId → 下次可融合时间）。</summary>
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();

        public static void Register()
        {
            // 基础融合（基础知识组合，产出低级装备图纸）
            Add("fusion_basic_assembly", "基础组装图纸", "基础机械知识融合，获得基础组装图纸",
                new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                5000, 0.8f, "equip", "sm_eq_gray", 0, 0);

            Add("fusion_energy_shield", "能量护盾图纸", "能量类知识融合，获得能量护盾图纸",
                new[] { "sm_know_mech_1_1_0", "sm_know_mech_1_1_1" },
                15000, 0.7f, "equip", "sm_eq_blue", 0, 1);

            Add("fusion_virtual_intrusion", "虚拟入侵协议", "虚拟类知识融合，获得虚拟入侵能力",
                new[] { "sm_know_mech_2_2_0", "sm_know_mech_2_2_1" },
                30000, 0.6f, "potential", "", 3, 2);

            Add("fusion_god_mech", "神级机械图纸", "尖端+终极知识融合，获得神级机械图纸",
                new[] { "sm_know_mech_3_1_0", "sm_know_mech_4_1_0" },
                100000, 0.4f, "equip", "sm_eq_orange", 0, 3);

            // 武道系融合
            Add("fusion_body_hardening", "炼体秘法", "体魄类知识融合，获得炼体秘法",
                new[] { "sm_know_martial_0_1_0", "sm_know_martial_0_1_1" },
                8000, 0.75f, "qi", "", 500, 0);

            Add("fusion_qi_burst", "暴气技巧", "气劲类知识融合，获得暴气技巧",
                new[] { "sm_know_martial_1_2_0", "sm_know_martial_2_2_0" },
                20000, 0.65f, "potential", "", 2, 1);

            // 异能系融合
            Add("fusion_gene_awaken", "基因觉醒剂", "基因类知识融合，获得基因觉醒剂",
                new[] { "sm_know_psi_0_0_0", "sm_know_psi_1_0_0" },
                10000, 0.7f, "qi", "", 800, 0);

            // 魔法系融合
            Add("fusion_element_resonance", "元素共鸣水晶", "元素类知识融合，获得元素共鸣水晶",
                new[] { "sm_know_mage_0_2_0", "sm_know_mage_1_2_0" },
                12000, 0.7f, "equip", "sm_eq_purple", 0, 1);

            // 念力系融合
            Add("fusion_soul_link", "灵魂链接器", "灵魂类知识融合，获得灵魂链接器",
                new[] { "sm_know_mind_0_0_0", "sm_know_mind_1_0_0" },
                15000, 0.65f, "potential", "", 3, 0);

            // 跨系融合（稀有，需要多系知识）
            Add("fusion_cross_mech_martial", "械武者融合", "机械+武道知识融合，获得械武者专精",
                new[] { "sm_know_mech_0_0_0", "sm_know_martial_0_1_0" },
                25000, 0.5f, "potential", "", 5, 0);

            Add("fusion_cross_psi_mind", "异能念力融合", "异能+念力知识融合，获得精神异能",
                new[] { "sm_know_psi_0_0_0", "sm_know_mind_0_0_0" },
                30000, 0.45f, "qi", "", 1200, 0);

            Debug.Log($"[超神机械师] 知识融合配方注册完成：{_recipes.Count}个配方");
        }

        private static void Add(string id, string name, string desc, string[] required,
            int xpCost, float successRate, string resultType, string resultId, int resultValue, int minTier)
        {
            _recipes.Add(new FusionRecipe
            {
                id = id, name = name, desc = desc, requiredKnowledge = required,
                xpCost = xpCost, successRate = successRate,
                resultType = resultType, resultId = resultId, resultValue = resultValue,
                minTier = minTier
            });
        }

        /// <summary>获取单位可融合的配方列表。</summary>
        public static List<FusionRecipe> GetAvailableRecipes(Actor a)
        {
            var list = new List<FusionRecipe>();
            if (a == null) return list;

            string cls = SuperMechBranch.GetClass(a);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            var unlocked = SuperMechKnowledge.GetUnlockedList(a, prefix);
            var unlockedIds = new HashSet<string>();
            foreach (var def in unlocked) unlockedIds.Add(def.id);

            foreach (var recipe in _recipes)
            {
                // 检查是否所有前置知识都已解锁
                bool canFuse = true;
                foreach (string req in recipe.requiredKnowledge)
                {
                    if (!unlockedIds.Contains(req)) { canFuse = false; break; }
                }
                if (canFuse) list.Add(recipe);
            }
            return list;
        }

        /// <summary>尝试知识融合。</summary>
        public static bool TryFuse(Actor a, string recipeId)
        {
            if (a == null) return false;

            // 检查冷却
            if (_cooldown.TryGetValue(a.id, out var cd) && Time.time < cd) return false;

            // 查找配方
            FusionRecipe recipe = null;
            foreach (var r in _recipes)
            {
                if (r.id == recipeId) { recipe = r; break; }
            }
            if (recipe == null) return false;

            // 检查经验
            if (!SuperMechAwakened.IsAwakened(a)) return false; // 只有降临者有经验
            float currentXp = SuperMechAwakened.GetXp(a);
            if (currentXp < recipe.xpCost) return false;

            // 扣除经验
            SuperMechAwakened.SpendXp(a, recipe.xpCost);

            // 设置冷却（10秒）
            _cooldown[a.id] = Time.time + 10f;

            // 成功率计算：基础成功率 + 知识数量加成
            float rate = recipe.successRate;
            int knowledgeCount = SuperMechKnowledge.GetUnlockedCount(a, SuperMechKnowledge.GetPrefixForClass(SuperMechBranch.GetClass(a)));
            rate += Mathf.Min(knowledgeCount * 0.01f, 0.2f); // 最多+20%

            bool success = UnityEngine.Random.value < rate;

            if (success)
            {
                // 记录已融合
                if (!_unlockedRecipes.TryGetValue(a.id, out var set))
                {
                    set = new HashSet<string>();
                    _unlockedRecipes[a.id] = set;
                }
                set.Add(recipeId);

                // 应用产物
                ApplyResult(a, recipe);
                Debug.Log($"[超神机械师] {a.name} 知识融合成功！获得：{recipe.name}");
            }
            else
            {
                Debug.Log($"[超神机械师] {a.name} 知识融合失败，消耗{recipe.xpCost}经验");
            }

            return success;
        }

        /// <summary>应用融合产物。</summary>
        private static void ApplyResult(Actor a, FusionRecipe recipe)
        {
            switch (recipe.resultType)
            {
                case "equip":
                    // 获得装备图纸：直接给单位装备一件对应品质装备
                    int eqIdx = SuperMechRelic.GetEquipIndex(recipe.resultId);
                    if (eqIdx >= 0) SuperMechRelic.EquipItem(a, eqIdx);
                    break;
                case "potential":
                    SuperMechPotential.AddPotential(a, recipe.resultValue);
                    break;
                case "qi":
                    SuperMechQi.AddQi(a, recipe.resultValue);
                    break;
            }
        }

        /// <summary>获取单位已融合的图纸数量。</summary>
        public static int GetUnlockedCount(Actor a)
        {
            if (a != null && _unlockedRecipes.TryGetValue(a.id, out var set))
                return set.Count;
            return 0;
        }

        /// <summary>是否在融合冷却中。</summary>
        public static bool IsOnCooldown(Actor a)
        {
            if (a != null && _cooldown.TryGetValue(a.id, out var cd))
                return Time.time < cd;
            return false;
        }

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_unlockedRecipes, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }

        /// <summary>清空。</summary>
        public static void Clear() { _unlockedRecipes.Clear(); _cooldown.Clear(); }
    }
}
