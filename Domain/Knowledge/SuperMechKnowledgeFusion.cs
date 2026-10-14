using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>知识融合系统（v0.183修复版，适配无等级的知识系统）
    /// 知识系统只有"解锁/未解锁"，融合设计为：多低阶知识→解锁高阶/终极知识
    /// 原著依据：机械师通过融合多种知识掌握终极技能</summary>
    public static class SuperMechKnowledgeFusion
    {
        /// <summary>融合配方定义</summary>
        public class FusionRecipe
        {
            public string id;
            public string resultKnowledge;   // 融合结果知识ID
            public List<string> requiredKnowledges = new List<string>(); // 需要的知识ID
            public int inalCost;
            public int minAwakenedLevel;    // 最低觉醒等级
            public float successRate;
        }

        private static readonly Dictionary<string, FusionRecipe> _recipes = new Dictionary<string, FusionRecipe>();
        private static bool _initialized;

        /// <summary>初始化融合配方</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            // 机械系融合：3个入门→解锁进阶
            AddRecipe("mech_fusion_1", "sm_know_mech_advanced",
                new List<string> { "sm_know_mech_theory", "sm_know_mech_design", "sm_know_mech_maintenance" },
                500, 3, 0.8f);

            // 机械系融合：进阶+战斗+召唤→解锁大师
            AddRecipe("mech_fusion_2", "sm_know_mech_master",
                new List<string> { "sm_know_mech_advanced", "sm_know_mech_combat", "sm_know_mech_summon" },
                2000, 5, 0.6f);

            // 异能系融合
            AddRecipe("psi_fusion_1", "sm_know_psi_advanced",
                new List<string> { "sm_know_psi_theory", "sm_know_psi_control", "sm_know_psi_meditation" },
                500, 3, 0.8f);

            AddRecipe("psi_fusion_2", "sm_know_psi_master",
                new List<string> { "sm_know_psi_advanced", "sm_know_psi_combat", "sm_know_psi_domain" },
                2000, 5, 0.6f);

            // 魔法系融合
            AddRecipe("magic_fusion_1", "sm_know_magic_advanced",
                new List<string> { "sm_know_magic_theory", "sm_know_magic_circle", "sm_know_magic_mana" },
                500, 3, 0.8f);

            // 武道系融合
            AddRecipe("martial_fusion_1", "sm_know_martial_advanced",
                new List<string> { "sm_know_martial_basic", "sm_know_martial_qi", "sm_know_martial_combat" },
                500, 3, 0.8f);

            // 跨系融合（稀有）：机械+异能→混合知识
            AddRecipe("mech_psi_hybrid", "sm_know_hybrid_mech_psi",
                new List<string> { "sm_know_mech_advanced", "sm_know_psi_advanced" },
                5000, 7, 0.4f);

            // 终极融合：四系大师→宇宙级理解
            AddRecipe("cosmic_understanding", "sm_know_cosmic_ultimate",
                new List<string> { "sm_know_mech_master", "sm_know_psi_master", "sm_know_magic_advanced", "sm_know_martial_advanced" },
                20000, 10, 0.2f);

            Debug.Log("[超神机械师] 知识融合系统初始化完成（适配无等级知识系统）");
        }

        private static void AddRecipe(string id, string result, List<string> required, int cost, int minLvl, float rate)
        {
            _recipes[id] = new FusionRecipe
            {
                id = id,
                resultKnowledge = result,
                requiredKnowledges = required,
                inalCost = cost,
                minAwakenedLevel = minLvl,
                successRate = rate
            };
        }

        /// <summary>获取所有可用融合配方</summary>
        public static List<FusionRecipe> GetAvailableRecipes(Actor a)
        {
            Init();
            var result = new List<FusionRecipe>();
            if (a == null) return result;
            int level = SuperMechAwakened.GetLevel(a);
            foreach (var recipe in _recipes.Values)
            {
                if (level >= recipe.minAwakenedLevel)
                    result.Add(recipe);
            }
            return result;
        }

        /// <summary>检查是否可以融合</summary>
        public static bool CanFuse(Actor a, string recipeId)
        {
            Init();
            if (a == null || !_recipes.TryGetValue(recipeId, out var recipe)) return false;
            if (SuperMechAwakened.GetLevel(a) < recipe.minAwakenedLevel) return false;
            if (SuperMechInal.GetInal(a) < recipe.inalCost) return false;
            foreach (var kid in recipe.requiredKnowledges)
            {
                if (!SuperMechKnowledge.IsUnlocked(a, kid)) return false;
            }
            if (SuperMechKnowledge.IsUnlocked(a, recipe.resultKnowledge)) return false;
            if (SuperMechKnowledge.GetDef(recipe.resultKnowledge) == null) return false;
            return true;
        }

        /// <summary>执行融合</summary>
        public static bool Fuse(Actor a, string recipeId)
        {
            if (!CanFuse(a, recipeId)) return false;
            var recipe = _recipes[recipeId];
            if (!SuperMechInal.SpendInal(a, recipe.inalCost)) return false;

            if (UnityEngine.Random.value > recipe.successRate)
            {
                Debug.Log($"[超神机械师] 知识融合失败（{recipeId}，成功率{recipe.successRate:F0%}）");
                return false;
            }

            bool unlocked = SuperMechKnowledge.Unlock(a, recipe.resultKnowledge);
            if (unlocked)
            {
                Debug.Log($"[超神机械师] 知识融合成功：解锁 {recipe.resultKnowledge}");
            }
            else
            {
                Debug.Log($"[超神机械师] 知识融合成功但解锁失败（前置不足）：{recipe.resultKnowledge}");
            }
            return unlocked;
        }

        /// <summary>获取融合配方描述</summary>
        public static string GetRecipeDescription(FusionRecipe recipe)
        {
            string materials = string.Join(" + ", recipe.requiredKnowledges);
            return $"材料: {materials}\n" +
                   $"结果: {recipe.resultKnowledge}\n" +
                   $"消耗: {recipe.inalCost}₡  成功率: {recipe.successRate:F0%}  等级要求: {recipe.minAwakenedLevel}";
        }

        /// <summary>获取配方进度（已拥有几个材料）</summary>
        public static string GetRecipeProgress(Actor a, FusionRecipe recipe)
        {
            if (a == null) return "0/0";
            int have = 0;
            foreach (var kid in recipe.requiredKnowledges)
            {
                if (SuperMechKnowledge.IsUnlocked(a, kid)) have++;
            }
            return $"{have}/{recipe.requiredKnowledges.Count}";
        }
    }
}
