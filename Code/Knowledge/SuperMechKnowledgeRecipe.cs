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

        /// <summary>融合配方总属性加成（图纸解锁模式）</summary>
        public class FusionStats
        {
            public float dmgMul;    // 总伤害倍率
            public float hpMul;     // 总生命倍率
            public float speedMul;  // 总速度倍率
            public int qiBonus;     // 总气力加成
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
        // 已融合记录：单位ID -> 已融合配方ID集合
        private static readonly Dictionary<long, HashSet<string>> _fusedByActor = new Dictionary<long, HashSet<string>>();

        /// <summary>图纸等级（绿/蓝/紫/金/橙）</summary>
        public enum BlueprintGrade
        {
            Green = 1,   // 基础图纸
            Blue = 2,    // 进阶图纸
            Purple = 3,  // 高端图纸
            Gold = 4,    // 尖端图纸
            Orange = 5   // 终极图纸
        }

        /// <summary>获取配方的图纸等级</summary>
        public static BlueprintGrade GetBlueprintGrade(RecipeDef recipe)
        {
            if (recipe == null) return BlueprintGrade.Green;
            return (BlueprintGrade)Mathf.Clamp(recipe.tier, 1, 5);
        }

        /// <summary>获取图纸等级名称（本地化）</summary>
        public static string GetBlueprintGradeName(BlueprintGrade grade)
        {
            return LocalizedTextManager.getText($"sm_blueprint_grade_{(int)grade}");
        }

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

        /// <summary>注册基础配方（三支五层+跨分支，共80个）</summary>
        public static void RegisterBaseRecipes()
        {
            // 配方定义：id, nameKey, descKey, branch, tier, reqKnow1, reqKnow2, xp, rate, type, resultId, dmg, hp, spd, qi
            var defs = new[]
            {
                // === 武装分支 Tier1 (4个) ===
                new object[] { "recipe_w_t1_0", "sm_recipe_w_t1_0_name", "sm_recipe_w_t1_0_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_0", "sm_know_mech_0_0_1", 5000, 0.80f, FusionResultType.Equip, "fusion_foldable_turret", 1.5f, 1.3f, 1.1f, 200 },
                new object[] { "recipe_w_t1_1", "sm_recipe_w_t1_1_name", "sm_recipe_w_t1_1_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_1", "sm_know_mech_0_0_2", 5000, 0.80f, FusionResultType.Equip, "fusion_arm_blade", 1.4f, 1.2f, 1.2f, 150 },
                new object[] { "recipe_w_t1_2", "sm_recipe_w_t1_2_name", "sm_recipe_w_t1_2_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_0", "sm_know_mech_0_0_2", 6000, 0.75f, FusionResultType.Equip, "fusion_light_power_arm", 1.3f, 1.4f, 1.15f, 180 },
                new object[] { "recipe_w_t1_3", "sm_recipe_w_t1_3_name", "sm_recipe_w_t1_3_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_0", "sm_know_mech_0_0_3", 5500, 0.78f, FusionResultType.Equip, "fusion_basic_weapon", 1.6f, 1.1f, 1.05f, 120 },
                // === 武装分支 Tier2 (4个) ===
                new object[] { "recipe_w_t2_0", "sm_recipe_w_t2_0_name", "sm_recipe_w_t2_0_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_0", "sm_know_mech_1_0_1", 15000, 0.70f, FusionResultType.Equip, "fusion_tracking_missile", 1.8f, 1.2f, 1.15f, 400 },
                new object[] { "recipe_w_t2_1", "sm_recipe_w_t2_1_name", "sm_recipe_w_t2_1_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_1", "sm_know_mech_1_0_2", 15000, 0.70f, FusionResultType.Equip, "fusion_energy_shield", 1.2f, 1.8f, 1.0f, 500 },
                new object[] { "recipe_w_t2_2", "sm_recipe_w_t2_2_name", "sm_recipe_w_t2_2_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_0", "sm_know_mech_1_0_2", 18000, 0.65f, FusionResultType.Equip, "fusion_heavy_mech_armor", 1.5f, 2.0f, 0.95f, 600 },
                new object[] { "recipe_w_t2_3", "sm_recipe_w_t2_3_name", "sm_recipe_w_t2_3_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_1", "sm_know_mech_1_0_3", 16000, 0.68f, FusionResultType.Equip, "fusion_micro_mech_mod", 1.7f, 1.3f, 1.2f, 450 },
                // === 武装分支 Tier3 (4个) ===
                new object[] { "recipe_w_t3_0", "sm_recipe_w_t3_0_name", "sm_recipe_w_t3_0_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_0", "sm_know_mech_2_0_1", 40000, 0.60f, FusionResultType.Equip, "fusion_plasma_cannon", 2.2f, 1.3f, 1.1f, 800 },
                new object[] { "recipe_w_t3_1", "sm_recipe_w_t3_1_name", "sm_recipe_w_t3_1_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_1", "sm_know_mech_2_0_2", 40000, 0.60f, FusionResultType.Equip, "fusion_force_barrier", 1.3f, 2.2f, 1.0f, 1000 },
                new object[] { "recipe_w_t3_2", "sm_recipe_w_t3_2_name", "sm_recipe_w_t3_2_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_0", "sm_know_mech_2_0_2", 45000, 0.55f, FusionResultType.Equip, "fusion_giant_compound_mech", 2.5f, 1.8f, 1.05f, 1200 },
                new object[] { "recipe_w_t3_3", "sm_recipe_w_t3_3_name", "sm_recipe_w_t3_3_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_1", "sm_know_mech_2_0_3", 42000, 0.58f, FusionResultType.Equip, "fusion_high_density_armor", 1.6f, 2.5f, 0.9f, 1100 },
                // === 武装分支 Tier4 (4个) ===
                new object[] { "recipe_w_t4_0", "sm_recipe_w_t4_0_name", "sm_recipe_w_t4_0_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_0", "sm_know_mech_3_0_1", 100000, 0.50f, FusionResultType.Equip, "fusion_planet_buster", 3.0f, 1.5f, 1.2f, 1500 },
                new object[] { "recipe_w_t4_1", "sm_recipe_w_t4_1_name", "sm_recipe_w_t4_1_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_1", "sm_know_mech_3_0_2", 100000, 0.50f, FusionResultType.Equip, "fusion_dimension_armor", 1.5f, 3.0f, 1.1f, 2000 },
                new object[] { "recipe_w_t4_2", "sm_recipe_w_t4_2_name", "sm_recipe_w_t4_2_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_0", "sm_know_mech_3_0_2", 110000, 0.45f, FusionResultType.Equip, "fusion_annihilation_weapon", 3.5f, 1.4f, 1.15f, 2200 },
                new object[] { "recipe_w_t4_3", "sm_recipe_w_t4_3_name", "sm_recipe_w_t4_3_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_1", "sm_know_mech_3_0_3", 105000, 0.48f, FusionResultType.Equip, "fusion_super_compound_mech", 2.8f, 2.2f, 1.2f, 2500 },
                // === 武装分支 Tier5 (4个) ===
                new object[] { "recipe_w_t5_0", "sm_recipe_w_t5_0_name", "sm_recipe_w_t5_0_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_0", "sm_know_mech_4_0_1", 250000, 0.40f, FusionResultType.Equip, "fusion_cosmic_battleship", 4.0f, 3.5f, 1.5f, 5000 },
                new object[] { "recipe_w_t5_1", "sm_recipe_w_t5_1_name", "sm_recipe_w_t5_1_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_1", "sm_know_mech_4_0_2", 250000, 0.40f, FusionResultType.Equip, "fusion_god_mech", 4.5f, 4.0f, 1.8f, 6000 },
                new object[] { "recipe_w_t5_2", "sm_recipe_w_t5_2_name", "sm_recipe_w_t5_2_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_0", "sm_know_mech_4_0_2", 280000, 0.35f, FusionResultType.Equip, "fusion_endless_material", 3.8f, 4.2f, 1.6f, 7000 },
                new object[] { "recipe_w_t5_3", "sm_recipe_w_t5_3_name", "sm_recipe_w_t5_3_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_1", "sm_know_mech_4_0_3", 260000, 0.38f, FusionResultType.Equip, "fusion_ultimate_mech_eng", 5.0f, 3.0f, 2.0f, 8000 },

                // === 能量分支 Tier1 ===
                new object[] { "recipe_e_t1_0", "sm_recipe_e_t1_0_name", "sm_recipe_e_t1_0_desc", FusionBranch.Energy, 1, "sm_know_mech_0_1_0", "sm_know_mech_0_1_1", 5000, 0.75f, FusionResultType.StatBoost, "fusion_mini_reactor", 1.2f, 1.5f, 1.0f, 500 },
                new object[] { "recipe_e_t1_1", "sm_recipe_e_t1_1_name", "sm_recipe_e_t1_1_desc", FusionBranch.Energy, 1, "sm_know_mech_0_1_1", "sm_know_mech_0_1_2", 5000, 0.75f, FusionResultType.StatBoost, "fusion_energy_battery", 1.1f, 1.3f, 1.1f, 800 },
                new object[] { "recipe_e_t1_2", "sm_recipe_e_t1_2_name", "sm_recipe_e_t1_2_desc", FusionBranch.Energy, 1, "sm_know_mech_0_1_0", "sm_know_mech_0_1_2", 5500, 0.75f, FusionResultType.StatBoost, "fusion_bio_energy", 1.2f, 1.7f, 1.00f, 600 },
                new object[] { "recipe_e_t1_3", "sm_recipe_e_t1_3_name", "sm_recipe_e_t1_3_desc", FusionBranch.Energy, 1, "sm_know_mech_0_1_1", "sm_know_mech_0_1_2", 6000, 0.72f, FusionResultType.StatBoost, "fusion_optical_amplifier", 1.3f, 1.5f, 1.05f, 700 },
                // === 能量分支 Tier2 (4个) ===
                new object[] { "recipe_e_t2_0", "sm_recipe_e_t2_0_name", "sm_recipe_e_t2_0_desc", FusionBranch.Energy, 2, "sm_know_mech_1_1_0", "sm_know_mech_1_1_1", 15000, 0.65f, FusionResultType.StatBoost, "fusion_fusion_core", 1.4f, 1.6f, 1.1f, 1200 },
                new object[] { "recipe_e_t2_1", "sm_recipe_e_t2_1_name", "sm_recipe_e_t2_1_desc", FusionBranch.Energy, 2, "sm_know_mech_1_1_1", "sm_know_mech_1_1_2", 15000, 0.65f, FusionResultType.StatBoost, "fusion_energy_converter", 1.3f, 1.5f, 1.2f, 1000 },
                new object[] { "recipe_e_t2_2", "sm_recipe_e_t2_2_name", "sm_recipe_e_t2_2_desc", FusionBranch.Energy, 2, "sm_know_mech_1_1_0", "sm_know_mech_1_1_2", 15500, 0.65f, FusionResultType.StatBoost, "fusion_second_split", 1.4f, 1.8f, 1.10f, 1300 },
                new object[] { "recipe_e_t2_3", "sm_recipe_e_t2_3_name", "sm_recipe_e_t2_3_desc", FusionBranch.Energy, 2, "sm_know_mech_1_1_1", "sm_know_mech_1_1_2", 16000, 0.62f, FusionResultType.StatBoost, "fusion_adv_energy_theory", 1.5f, 1.6f, 1.15f, 1400 },
                // === 能量分支 Tier3 (4个) ===
                new object[] { "recipe_e_t3_0", "sm_recipe_e_t3_0_name", "sm_recipe_e_t3_0_desc", FusionBranch.Energy, 3, "sm_know_mech_2_1_0", "sm_know_mech_2_1_1", 40000, 0.55f, FusionResultType.StatBoost, "fusion_antimatter_engine", 1.8f, 2.0f, 1.2f, 2000 },
                new object[] { "recipe_e_t3_1", "sm_recipe_e_t3_1_name", "sm_recipe_e_t3_1_desc", FusionBranch.Energy, 3, "sm_know_mech_2_1_1", "sm_know_mech_2_1_2", 40000, 0.55f, FusionResultType.StatBoost, "fusion_zero_point", 1.6f, 2.2f, 1.1f, 2500 },
                new object[] { "recipe_e_t3_2", "sm_recipe_e_t3_2_name", "sm_recipe_e_t3_2_desc", FusionBranch.Energy, 3, "sm_know_mech_2_1_0", "sm_know_mech_2_1_2", 40500, 0.55f, FusionResultType.StatBoost, "fusion_nano_power", 1.8f, 2.2f, 1.20f, 2100 },
                new object[] { "recipe_e_t3_3", "sm_recipe_e_t3_3_name", "sm_recipe_e_t3_3_desc", FusionBranch.Energy, 3, "sm_know_mech_2_1_1", "sm_know_mech_2_1_2", 41000, 0.52f, FusionResultType.StatBoost, "fusion_high_energy_master", 1.9f, 2.0f, 1.25f, 2200 },
                // === 能量分支 Tier4 (4个) ===
                new object[] { "recipe_e_t4_0", "sm_recipe_e_t4_0_name", "sm_recipe_e_t4_0_desc", FusionBranch.Energy, 4, "sm_know_mech_3_1_0", "sm_know_mech_3_1_1", 100000, 0.45f, FusionResultType.StatBoost, "fusion_dimensional_energy", 2.2f, 2.5f, 1.3f, 4000 },
                new object[] { "recipe_e_t4_1", "sm_recipe_e_t4_1_name", "sm_recipe_e_t4_1_desc", FusionBranch.Energy, 4, "sm_know_mech_3_1_1", "sm_know_mech_3_1_2", 100000, 0.45f, FusionResultType.StatBoost, "fusion_dark_energy", 2.0f, 2.8f, 1.2f, 4500 },
                new object[] { "recipe_e_t4_2", "sm_recipe_e_t4_2_name", "sm_recipe_e_t4_2_desc", FusionBranch.Energy, 4, "sm_know_mech_3_1_0", "sm_know_mech_3_1_2", 100500, 0.45f, FusionResultType.StatBoost, "fusion_abnormal_energy", 2.2f, 2.7f, 1.30f, 4100 },
                new object[] { "recipe_e_t4_3", "sm_recipe_e_t4_3_name", "sm_recipe_e_t4_3_desc", FusionBranch.Energy, 4, "sm_know_mech_3_1_1", "sm_know_mech_3_1_2", 101000, 0.42f, FusionResultType.StatBoost, "fusion_ultra_long_energy", 2.3f, 2.5f, 1.35f, 4200 },
                // === 能量分支 Tier5 (4个) ===
                new object[] { "recipe_e_t5_0", "sm_recipe_e_t5_0_name", "sm_recipe_e_t5_0_desc", FusionBranch.Energy, 5, "sm_know_mech_4_1_0", "sm_know_mech_4_1_1", 250000, 0.35f, FusionResultType.StatBoost, "fusion_cosmic_reactor", 3.0f, 3.5f, 1.5f, 8000 },
                new object[] { "recipe_e_t5_1", "sm_recipe_e_t5_1_name", "sm_recipe_e_t5_1_desc", FusionBranch.Energy, 5, "sm_know_mech_4_1_1", "sm_know_mech_4_1_2", 250000, 0.35f, FusionResultType.StatBoost, "fusion_creation_energy", 3.5f, 3.0f, 1.6f, 10000 },
                new object[] { "recipe_e_t5_2", "sm_recipe_e_t5_2_name", "sm_recipe_e_t5_2_desc", FusionBranch.Energy, 5, "sm_know_mech_4_1_0", "sm_know_mech_4_1_2", 250500, 0.35f, FusionResultType.StatBoost, "fusion_mech_life_seed", 3.0f, 3.7f, 1.50f, 8100 },
                new object[] { "recipe_e_t5_3", "sm_recipe_e_t5_3_name", "sm_recipe_e_t5_3_desc", FusionBranch.Energy, 5, "sm_know_mech_4_1_1", "sm_know_mech_4_1_2", 251000, 0.32f, FusionResultType.StatBoost, "fusion_eternal_energy", 3.1f, 3.5f, 1.55f, 8200 },

                // === 操控分支 Tier1 ===
                new object[] { "recipe_c_t1_0", "sm_recipe_c_t1_0_name", "sm_recipe_c_t1_0_desc", FusionBranch.Control, 1, "sm_know_mech_0_2_0", "sm_know_mech_0_2_1", 5000, 0.70f, FusionResultType.Perk, "fusion_ai_core", 1.3f, 1.2f, 1.4f, 300 },
                new object[] { "recipe_c_t1_1", "sm_recipe_c_t1_1_name", "sm_recipe_c_t1_1_desc", FusionBranch.Control, 1, "sm_know_mech_0_2_1", "sm_know_mech_0_2_2", 5000, 0.70f, FusionResultType.Perk, "fusion_remote_control", 1.2f, 1.1f, 1.5f, 250 },
                new object[] { "recipe_c_t1_2", "sm_recipe_c_t1_2_name", "sm_recipe_c_t1_2_desc", FusionBranch.Control, 1, "sm_know_mech_0_2_0", "sm_know_mech_0_2_2", 5500, 0.70f, FusionResultType.Perk, "fusion_nerve_link", 1.3f, 1.4f, 1.4f, 400 },
                new object[] { "recipe_c_t1_3", "sm_recipe_c_t1_3_name", "sm_recipe_c_t1_3_desc", FusionBranch.Control, 1, "sm_know_mech_0_2_1", "sm_know_mech_0_2_2", 6000, 0.67f, FusionResultType.Perk, "fusion_basic_sensing", 1.4f, 1.2f, 1.5f, 500 },
                // === 操控分支 Tier2 (4个) ===
                new object[] { "recipe_c_t2_0", "sm_recipe_c_t2_0_name", "sm_recipe_c_t2_0_desc", FusionBranch.Control, 2, "sm_know_mech_1_2_0", "sm_know_mech_1_2_1", 15000, 0.60f, FusionResultType.Perk, "fusion_tactical_ai", 1.5f, 1.3f, 1.6f, 600 },
                new object[] { "recipe_c_t2_1", "sm_recipe_c_t2_1_name", "sm_recipe_c_t2_1_desc", FusionBranch.Control, 2, "sm_know_mech_1_2_1", "sm_know_mech_1_2_2", 15000, 0.60f, FusionResultType.Perk, "fusion_swarm_control", 1.4f, 1.4f, 1.7f, 550 },
                new object[] { "recipe_c_t2_2", "sm_recipe_c_t2_2_name", "sm_recipe_c_t2_2_desc", FusionBranch.Control, 2, "sm_know_mech_1_2_0", "sm_know_mech_1_2_2", 15500, 0.60f, FusionResultType.Perk, "fusion_primary_space", 1.5f, 1.5f, 1.6f, 700 },
                new object[] { "recipe_c_t2_3", "sm_recipe_c_t2_3_name", "sm_recipe_c_t2_3_desc", FusionBranch.Control, 2, "sm_know_mech_1_2_1", "sm_know_mech_1_2_2", 16000, 0.57f, FusionResultType.Perk, "fusion_adv_electromagnetic", 1.6f, 1.3f, 1.7f, 800 },
                // === 操控分支 Tier3 (4个) ===
                new object[] { "recipe_c_t3_0", "sm_recipe_c_t3_0_name", "sm_recipe_c_t3_0_desc", FusionBranch.Control, 3, "sm_know_mech_2_2_0", "sm_know_mech_2_2_1", 40000, 0.50f, FusionResultType.Perk, "fusion_quantum_comm", 1.7f, 1.5f, 1.8f, 1200 },
                new object[] { "recipe_c_t3_1", "sm_recipe_c_t3_1_name", "sm_recipe_c_t3_1_desc", FusionBranch.Control, 3, "sm_know_mech_2_2_1", "sm_know_mech_2_2_2", 40000, 0.50f, FusionResultType.Perk, "fusion_mind_control", 1.8f, 1.4f, 1.7f, 1100 },
                new object[] { "recipe_c_t3_2", "sm_recipe_c_t3_2_name", "sm_recipe_c_t3_2_desc", FusionBranch.Control, 3, "sm_know_mech_2_2_0", "sm_know_mech_2_2_2", 40500, 0.50f, FusionResultType.Perk, "fusion_advanced_virtual_ai", 1.7f, 1.7f, 1.8f, 1300 },
                new object[] { "recipe_c_t3_3", "sm_recipe_c_t3_3_name", "sm_recipe_c_t3_3_desc", FusionBranch.Control, 3, "sm_know_mech_2_2_1", "sm_know_mech_2_2_2", 41000, 0.47f, FusionResultType.Perk, "fusion_interstellar_nav", 1.8f, 1.5f, 1.9f, 1400 },
                // === 操控分支 Tier4 (4个) ===
                new object[] { "recipe_c_t4_0", "sm_recipe_c_t4_0_name", "sm_recipe_c_t4_0_desc", FusionBranch.Control, 4, "sm_know_mech_3_2_0", "sm_know_mech_3_2_1", 100000, 0.40f, FusionResultType.Perk, "fusion_dimensional_control", 2.0f, 1.8f, 2.0f, 2500 },
                new object[] { "recipe_c_t4_1", "sm_recipe_c_t4_1_name", "sm_recipe_c_t4_1_desc", FusionBranch.Control, 4, "sm_know_mech_3_2_1", "sm_know_mech_3_2_2", 100000, 0.40f, FusionResultType.Perk, "fusion_time_control", 2.2f, 1.6f, 2.2f, 2800 },
                new object[] { "recipe_c_t4_2", "sm_recipe_c_t4_2_name", "sm_recipe_c_t4_2_desc", FusionBranch.Control, 4, "sm_know_mech_3_2_0", "sm_know_mech_3_2_2", 100500, 0.40f, FusionResultType.Perk, "fusion_high_space_app", 2.0f, 2.0f, 2.0f, 2600 },
                new object[] { "recipe_c_t4_3", "sm_recipe_c_t4_3_name", "sm_recipe_c_t4_3_desc", FusionBranch.Control, 4, "sm_know_mech_3_2_1", "sm_know_mech_3_2_2", 101000, 0.37f, FusionResultType.Perk, "fusion_quantum_matrix", 2.1f, 1.8f, 2.1f, 2700 },
                // === 操控分支 Tier5 (4个) ===
                new object[] { "recipe_c_t5_0", "sm_recipe_c_t5_0_name", "sm_recipe_c_t5_0_desc", FusionBranch.Control, 5, "sm_know_mech_4_2_0", "sm_know_mech_4_2_1", 250000, 0.30f, FusionResultType.Perk, "fusion_cosmic_ai", 3.0f, 2.5f, 2.5f, 6000 },
                new object[] { "recipe_c_t5_1", "sm_recipe_c_t5_1_name", "sm_recipe_c_t5_1_desc", FusionBranch.Control, 5, "sm_know_mech_4_2_1", "sm_know_mech_4_2_2", 250000, 0.30f, FusionResultType.Perk, "fusion_reality_warp", 3.5f, 2.0f, 2.8f, 7000 },
                new object[] { "recipe_c_t5_2", "sm_recipe_c_t5_2_name", "sm_recipe_c_t5_2_desc", FusionBranch.Control, 5, "sm_know_mech_4_2_0", "sm_know_mech_4_2_2", 250500, 0.30f, FusionResultType.Perk, "fusion_virtual_creator", 3.0f, 2.7f, 2.5f, 6100 },
                new object[] { "recipe_c_t5_3", "sm_recipe_c_t5_3_name", "sm_recipe_c_t5_3_desc", FusionBranch.Control, 5, "sm_know_mech_4_2_1", "sm_know_mech_4_2_2", 251000, 0.27f, FusionResultType.Perk, "fusion_hyperspace_theory", 3.1f, 2.5f, 2.6f, 6200 },

                // === 跨分支配方（15个：武装+能量/武装+操控/能量+操控，每级各1个）===
                new object[] { "recipe_we_t1", "sm_recipe_we_t1_name", "sm_recipe_we_t1_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_0", "sm_know_mech_0_1_0", 8000, 0.65f, FusionResultType.Equip, "fusion_energy_blade", 1.5f, 1.4f, 1.2f, 400 },
                new object[] { "recipe_we_t2", "sm_recipe_we_t2_name", "sm_recipe_we_t2_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_0", "sm_know_mech_1_1_0", 20000, 0.55f, FusionResultType.Equip, "fusion_plasma_armor", 1.8f, 1.7f, 1.3f, 800 },
                new object[] { "recipe_we_t3", "sm_recipe_we_t3_name", "sm_recipe_we_t3_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_0", "sm_know_mech_2_1_0", 50000, 0.45f, FusionResultType.Equip, "fusion_antimatter_cannon", 2.2f, 2.0f, 1.4f, 1500 },
                new object[] { "recipe_we_t4", "sm_recipe_we_t4_name", "sm_recipe_we_t4_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_0", "sm_know_mech_3_1_0", 120000, 0.35f, FusionResultType.Equip, "fusion_dimension_weapon", 2.8f, 2.5f, 1.6f, 3000 },
                new object[] { "recipe_we_t5", "sm_recipe_we_t5_name", "sm_recipe_we_t5_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_0", "sm_know_mech_4_1_0", 300000, 0.25f, FusionResultType.Equip, "fusion_cosmic_mech", 3.8f, 3.2f, 1.8f, 7000 },
                new object[] { "recipe_wc_t1", "sm_recipe_wc_t1_name", "sm_recipe_wc_t1_desc", FusionBranch.Weapon, 1, "sm_know_mech_0_0_0", "sm_know_mech_0_2_0", 8000, 0.65f, FusionResultType.Equip, "fusion_ai_turret", 1.5f, 1.4f, 1.2f, 400 },
                new object[] { "recipe_wc_t2", "sm_recipe_wc_t2_name", "sm_recipe_wc_t2_desc", FusionBranch.Weapon, 2, "sm_know_mech_1_0_0", "sm_know_mech_1_2_0", 20000, 0.55f, FusionResultType.Equip, "fusion_swarm_weapon", 1.8f, 1.7f, 1.3f, 800 },
                new object[] { "recipe_wc_t3", "sm_recipe_wc_t3_name", "sm_recipe_wc_t3_desc", FusionBranch.Weapon, 3, "sm_know_mech_2_0_0", "sm_know_mech_2_2_0", 50000, 0.45f, FusionResultType.Equip, "fusion_quantum_sniper", 2.2f, 2.0f, 1.4f, 1500 },
                new object[] { "recipe_wc_t4", "sm_recipe_wc_t4_name", "sm_recipe_wc_t4_desc", FusionBranch.Weapon, 4, "sm_know_mech_3_0_0", "sm_know_mech_3_2_0", 120000, 0.35f, FusionResultType.Equip, "fusion_reality_breaker", 2.8f, 2.5f, 1.6f, 3000 },
                new object[] { "recipe_wc_t5", "sm_recipe_wc_t5_name", "sm_recipe_wc_t5_desc", FusionBranch.Weapon, 5, "sm_know_mech_4_0_0", "sm_know_mech_4_2_0", 300000, 0.25f, FusionResultType.Equip, "fusion_virtual_mech", 3.8f, 3.2f, 1.8f, 7000 },
                new object[] { "recipe_ec_t1", "sm_recipe_ec_t1_name", "sm_recipe_ec_t1_desc", FusionBranch.Energy, 1, "sm_know_mech_0_1_0", "sm_know_mech_0_2_0", 8000, 0.65f, FusionResultType.Equip, "fusion_energy_ai", 1.5f, 1.4f, 1.2f, 400 },
                new object[] { "recipe_ec_t2", "sm_recipe_ec_t2_name", "sm_recipe_ec_t2_desc", FusionBranch.Energy, 2, "sm_know_mech_1_1_0", "sm_know_mech_1_2_0", 20000, 0.55f, FusionResultType.Equip, "fusion_energy_swarm", 1.8f, 1.7f, 1.3f, 800 },
                new object[] { "recipe_ec_t3", "sm_recipe_ec_t3_name", "sm_recipe_ec_t3_desc", FusionBranch.Energy, 3, "sm_know_mech_2_1_0", "sm_know_mech_2_2_0", 50000, 0.45f, FusionResultType.Equip, "fusion_quantum_energy", 2.2f, 2.0f, 1.4f, 1500 },
                new object[] { "recipe_ec_t4", "sm_recipe_ec_t4_name", "sm_recipe_ec_t4_desc", FusionBranch.Energy, 4, "sm_know_mech_3_1_0", "sm_know_mech_3_2_0", 120000, 0.35f, FusionResultType.Equip, "fusion_time_energy", 2.8f, 2.5f, 1.6f, 3000 },
                new object[] { "recipe_ec_t5", "sm_recipe_ec_t5_name", "sm_recipe_ec_t5_desc", FusionBranch.Energy, 5, "sm_know_mech_4_1_0", "sm_know_mech_4_2_0", 300000, 0.25f, FusionResultType.Equip, "fusion_creation_ai", 3.8f, 3.2f, 1.8f, 7000 },
            };

            foreach (var def in defs)
            {
                Register(new RecipeDef
                {
                    id = (string)def[0],
                    nameKey = (string)def[1],
                    descKey = (string)def[2],
                    branch = (FusionBranch)def[3],
                    tier = (int)def[4],
                    requiredKnowledge = new[] { (string)def[5], (string)def[6] },
                    xpCost = (int)def[7],
                    successRate = (float)def[8],
                    resultType = (FusionResultType)def[9],
                    resultId = (string)def[10],
                    dmgMul = (float)def[11],
                    hpMul = (float)def[12],
                    speedMul = (float)def[13],
                    qiBonus = (int)def[14],
                    icon = "ui/Icons/actor_traits/iconBlessing"
                });
            }
        }

        /// <summary>检查单位是否拥有配方所需的全部知识</summary>
        public static bool HasRequiredKnowledge(Actor a, RecipeDef recipe)
        {
            if (a == null || recipe == null) return false;
            foreach (var kid in recipe.requiredKnowledge)
            {
                if (!SuperMechKnowledge.IsUnlocked(a, kid)) return false;
            }
            return true;
        }

        /// <summary>检查单位是否已融合过该配方</summary>
        public static bool HasFusion(Actor a, string recipeId)
        {
            if (a == null || string.IsNullOrEmpty(recipeId)) return false;
            if (_fusedByActor.TryGetValue(a.data.id, out var set))
                return set.Contains(recipeId);
            return false;
        }

        /// <summary>检查单位是否可以执行融合（知识+气力+未融合）</summary>
        public static bool CanFuse(Actor a, RecipeDef recipe)
        {
            if (a == null || recipe == null) return false;
            if (!HasRequiredKnowledge(a, recipe)) return false;
            if (HasFusion(a, recipe.id)) return false;
            // 气力消耗：配方qiBonus的50%（至少10）
            float cost = Mathf.Max(10, recipe.qiBonus * 0.5f);
            return SuperMechQi.GetQi(a) >= cost;
        }

        /// <summary>执行融合，返回是否成功</summary>
        public static bool TryFuse(Actor a, RecipeDef recipe)
        {
            if (!CanFuse(a, recipe)) return false;

            // 消耗气力
            float cost = Mathf.Max(10, recipe.qiBonus * 0.5f);
            SuperMechQi.AddQi(a, -cost);

            // 成功率判定
            bool success = Random.value < recipe.successRate;
            if (success)
            {
                // 记录融合结果
                if (!_fusedByActor.ContainsKey(a.data.id))
                    _fusedByActor[a.data.id] = new HashSet<string>();
                _fusedByActor[a.data.id].Add(recipe.id);
                // 气力奖励
                SuperMechQi.AddQi(a, recipe.qiBonus);
                Debug.Log($"[超神机械师] 融合成功: {a.name} → {recipe.id}");
            }
            else
            {
                Debug.Log($"[超神机械师] 融合失败: {a.name} → {recipe.id}");
            }
            return success;
        }

        /// <summary>获取单位已融合的配方数量</summary>
        public static int GetFusionCount(Actor a)
        {
            if (a == null) return 0;
            if (_fusedByActor.TryGetValue(a.data.id, out var set))
                return set.Count;
            return 0;
        }

        /// <summary>获取单位已融合配方的总属性加成（图纸解锁模式，非直接伤害加成）</summary>
        public static FusionStats GetFusionStats(Actor a)
        {
            var stats = new FusionStats { dmgMul = 1f, hpMul = 1f, speedMul = 1f, qiBonus = 0 };
            if (a == null) return stats;
            if (!_fusedByActor.TryGetValue(a.data.id, out var set)) return stats;
            foreach (var recipeId in set)
            {
                if (_recipeById.TryGetValue(recipeId, out var recipe))
                {
                    // 每个配方的属性倍率叠加（乘法叠加，上限防止爆炸，可配置）
                    float singleCap = SuperMechConfig.FusionSingleMulCap;
                    stats.dmgMul *= Mathf.Min(recipe.dmgMul, singleCap);
                    stats.hpMul *= Mathf.Min(recipe.hpMul, singleCap);
                    stats.speedMul *= Mathf.Min(recipe.speedMul, singleCap);
                    stats.qiBonus += recipe.qiBonus;
                }
            }
            // 总加成上限（可配置）
            float totalCap = SuperMechConfig.FusionTotalMulCap;
            stats.dmgMul = Mathf.Min(stats.dmgMul, totalCap);
            stats.hpMul = Mathf.Min(stats.hpMul, totalCap);
            stats.speedMul = Mathf.Min(stats.speedMul, totalCap);
            return stats;
        }

        public static void Clear()
        {
            _recipes.Clear();
            _recipeById.Clear();
            _recipesByBranch.Clear();
        }
    }
}
