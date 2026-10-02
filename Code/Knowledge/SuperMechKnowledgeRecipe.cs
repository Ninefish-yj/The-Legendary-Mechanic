using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识融合配方数据结构。
    /// 三支五层：武装（Weapon）/ 能量（Energy）/ 操控（Control），每支5层。
    /// 配方数据驱动，支持JSON配置。
    /// 知识A + 知识B → 图纸/能力/装备。
    /// </summary>
    public static class SuperMechKnowledgeRecipe
    {
        /// <summary>融合分支</summary>
        public enum FusionBranch
        {
            Weapon = 0,    // 武装：武器/护甲/机械体
            Energy = 1,    // 能量：反应堆/护盾/爆发
            Control = 2    // 操控：AI/远程/群体
        }

        /// <summary>融合结果类型</summary>
        public enum FusionResultType
        {
            Equip = 0,      // 装备图纸
            Skill = 1,      // 技能/能力
            Perk = 2,       // 专长/特质
            StatBoost = 3   // 属性提升
        }

        /// <summary>融合配方定义</summary>
        [System.Serializable]
        public class RecipeDef
        {
            public string id;                    // 配方唯一ID
            public string nameKey;               // 名称本地化key
            public string descKey;               // 描述本地化key
            public FusionBranch branch;          // 所属分支
            public int tier;                     // 层级（1~5）
            public string[] requiredKnowledge;   // 所需知识ID列表
            public int xpCost;                   // 融合消耗经验
            public float successRate;            // 成功率（0~1）
            public FusionResultType resultType;  // 结果类型
            public string resultId;              // 结果ID（装备/技能/专长ID）
            public float dmgMul;                 // 伤害倍率
            public float hpMul;                  // 生命倍率
            public float speedMul;               // 速度倍率
            public int qiBonus;                  // 气力加成
            public string icon;                  // 图标路径
        }

        /// <summary>分支名称本地化key</summary>
        public static readonly Dictionary<FusionBranch, string> BranchNameKeys = new Dictionary<FusionBranch, string>
        {
            { FusionBranch.Weapon, "sm_fusion_branch_weapon" },
            { FusionBranch.Energy, "sm_fusion_branch_energy" },
            { FusionBranch.Control, "sm_fusion_branch_control" }
        };

        /// <summary>分支描述本地化key</summary>
        public static readonly Dictionary<FusionBranch, string> BranchDescKeys = new Dictionary<FusionBranch, string>
        {
            { FusionBranch.Weapon, "sm_fusion_branch_weapon_desc" },
            { FusionBranch.Energy, "sm_fusion_branch_energy_desc" },
            { FusionBranch.Control, "sm_fusion_branch_control_desc" }
        };

        private static readonly List<RecipeDef> _recipes = new List<RecipeDef>();
        private static readonly Dictionary<string, RecipeDef> _recipeById = new Dictionary<string, RecipeDef>();
        private static readonly Dictionary<FusionBranch, List<RecipeDef>> _recipesByBranch = new Dictionary<FusionBranch, List<RecipeDef>>();

        /// <summary>注册配方</summary>
        public static void Register(RecipeDef recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.id)) return;
            if (_recipeById.ContainsKey(recipe.id)) return;

            _recipes.Add(recipe);
            _recipeById[recipe.id] = recipe;

            if (!_recipesByBranch.ContainsKey(recipe.branch))
                _recipesByBranch[recipe.branch] = new List<RecipeDef>();
            _recipesByBranch[recipe.branch].Add(recipe);
        }

        /// <summary>批量注册配方</summary>
        public static void RegisterAll(IEnumerable<RecipeDef> recipes)
        {
            if (recipes == null) return;
            foreach (var r in recipes) Register(r);
        }

        /// <summary>根据ID获取配方</summary>
        public static RecipeDef GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _recipeById.TryGetValue(id, out RecipeDef r);
            return r;
        }

        /// <summary>获取某分支的所有配方</summary>
        public static List<RecipeDef> GetByBranch(FusionBranch branch)
        {
            if (_recipesByBranch.TryGetValue(branch, out var list))
                return list;
            return new List<RecipeDef>();
        }

        /// <summary>获取某分支某层级的配方</summary>
        public static List<RecipeDef> GetByBranchAndTier(FusionBranch branch, int tier)
        {
            var result = new List<RecipeDef>();
            if (_recipesByBranch.TryGetValue(branch, out var list))
            {
                foreach (var r in list)
                {
                    if (r.tier == tier) result.Add(r);
                }
            }
            return result;
        }

        /// <summary>检查单位是否满足配方条件</summary>
        public static bool CanFuse(Actor a, RecipeDef recipe)
        {
            if (a == null || recipe == null) return false;
            if (recipe.requiredKnowledge == null || recipe.requiredKnowledge.Length == 0) return false;

            foreach (string kid in recipe.requiredKnowledge)
            {
                if (!SuperMechKnowledge.IsUnlocked(a, kid)) return false;
            }
            return true;
        }

        /// <summary>获取单位可融合的配方列表</summary>
        public static List<RecipeDef> GetAvailableRecipes(Actor a)
        {
            var result = new List<RecipeDef>();
            if (a == null) return result;
            foreach (var r in _recipes)
            {
                if (CanFuse(a, r)) result.Add(r);
            }
            return result;
        }

        /// <summary>获取所有配方数量</summary>
        public static int Count => _recipes.Count;

        /// <summary>获取分支名称（本地化）</summary>
        public static string GetBranchName(FusionBranch branch)
        {
            if (BranchNameKeys.TryGetValue(branch, out string key))
                return LocalizedTextManager.getText(key);
            return branch.ToString();
        }

        /// <summary>获取分支描述（本地化）</summary>
        public static string GetBranchDesc(FusionBranch branch)
        {
            if (BranchDescKeys.TryGetValue(branch, out string key))
                return LocalizedTextManager.getText(key);
            return "";
        }

        /// <summary>注册基础配方（机械系示例，后续可扩展到JSON配置）</summary>
        public static void RegisterBaseRecipes()
        {
            // 武装分支 - Tier1
            Register(new RecipeDef
            {
                id = "recipe_weapon_t1_foldable_turret",
                nameKey = "sm_recipe_weapon_t1_name",
                descKey = "sm_recipe_weapon_t1_desc",
                branch = FusionBranch.Weapon,
                tier = 1,
                requiredKnowledge = new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                xpCost = 5000,
                successRate = 0.8f,
                resultType = FusionResultType.Equip,
                resultId = "fusion_foldable_turret",
                dmgMul = 1.5f, hpMul = 1.3f, speedMul = 1.1f, qiBonus = 200,
                icon = "ui/Icons/actor_traits/iconBlessing"
            });

            // 能量分支 - Tier1
            Register(new RecipeDef
            {
                id = "recipe_energy_t1_reactor",
                nameKey = "sm_recipe_energy_t1_name",
                descKey = "sm_recipe_energy_t1_desc",
                branch = FusionBranch.Energy,
                tier = 1,
                requiredKnowledge = new[] { "sm_know_mech_0_1_0", "sm_know_mech_0_1_1" },
                xpCost = 5000,
                successRate = 0.75f,
                resultType = FusionResultType.StatBoost,
                resultId = "fusion_mini_reactor",
                dmgMul = 1.2f, hpMul = 1.5f, speedMul = 1.0f, qiBonus = 500,
                icon = "ui/Icons/actor_traits/iconBlessing"
            });

            // 操控分支 - Tier1
            Register(new RecipeDef
            {
                id = "recipe_control_t1_ai_core",
                nameKey = "sm_recipe_control_t1_name",
                descKey = "sm_recipe_control_t1_desc",
                branch = FusionBranch.Control,
                tier = 1,
                requiredKnowledge = new[] { "sm_know_mech_0_2_0", "sm_know_mech_0_2_1" },
                xpCost = 5000,
                successRate = 0.7f,
                resultType = FusionResultType.Perk,
                resultId = "fusion_ai_core",
                dmgMul = 1.3f, hpMul = 1.2f, speedMul = 1.4f, qiBonus = 300,
                icon = "ui/Icons/actor_traits/iconBlessing"
            });
        }

        public static void Clear()
        {
            _recipes.Clear();
            _recipeById.Clear();
            _recipesByBranch.Clear();
        }
    }
}
