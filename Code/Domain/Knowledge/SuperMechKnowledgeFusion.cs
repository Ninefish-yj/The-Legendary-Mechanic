using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>知识融合系统（v0.184重写：基于知识等级，原著还原）
    /// 原著：知识融合（基础电磁原理lv4、基础能源理论lv3...）
    /// 融合需要材料知识达到特定等级，消耗伊纳尔，有成功率</summary>
    public static class SuperMechKnowledgeFusion
    {
        /// <summary>融合配方定义</summary>
        public class FusionRecipe
        {
            public string id;
            public string resultKnowledge;   // 融合结果知识ID
            public List<string> requiredKnowledges = new List<string>(); // 需要的知识ID
            public List<int> requiredLevels = new List<int>(); // 对应知识的最低等级
            public int inalCost;
            public int minAwakenedLevel;    // 最低觉醒等级
            public float successRate;
        }

        private static readonly Dictionary<string, FusionRecipe> _recipes = new Dictionary<string, FusionRecipe>();
        private static bool _initialized;

        /// <summary>初始化融合配方（v0.184：基于知识等级，原著还原）</summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            // 机械系融合：3个入门知识lv3+ → 解锁进阶
            AddRecipe("mech_fusion_1", "sm_know_mech_advanced",
                new List<string> { "sm_know_mech_theory", "sm_know_mech_design", "sm_know_mech_maintenance" },
                new List<int> { 3, 3, 3 },
                500, 3, 0.8f);

            // 机械系融合：进阶lv2+战斗lv3+召唤lv2 → 解锁大师
            AddRecipe("mech_fusion_2", "sm_know_mech_master",
                new List<string> { "sm_know_mech_advanced", "sm_know_mech_combat", "sm_know_mech_summon" },
                new List<int> { 2, 3, 2 },
                2000, 5, 0.6f);

            // 异能系融合
            AddRecipe("psi_fusion_1", "sm_know_psi_advanced",
                new List<string> { "sm_know_psi_theory", "sm_know_psi_control", "sm_know_psi_meditation" },
                new List<int> { 3, 3, 3 },
                500, 3, 0.8f);

            AddRecipe("psi_fusion_2", "sm_know_psi_master",
                new List<string> { "sm_know_psi_advanced", "sm_know_psi_combat", "sm_know_psi_domain" },
                new List<int> { 2, 3, 2 },
                2000, 5, 0.6f);

            // 魔法系融合
            AddRecipe("magic_fusion_1", "sm_know_magic_advanced",
                new List<string> { "sm_know_magic_theory", "sm_know_magic_circle", "sm_know_magic_mana" },
                new List<int> { 3, 3, 3 },
                500, 3, 0.8f);

            // 武道系融合
            AddRecipe("martial_fusion_1", "sm_know_martial_advanced",
                new List<string> { "sm_know_martial_basic", "sm_know_martial_qi", "sm_know_martial_combat" },
                new List<int> { 3, 3, 3 },
                500, 3, 0.8f);

            // 跨系融合（稀有）：机械进阶lv3+异能进阶lv3 → 混合知识
            AddRecipe("mech_psi_hybrid", "sm_know_hybrid_mech_psi",
                new List<string> { "sm_know_mech_advanced", "sm_know_psi_advanced" },
                new List<int> { 3, 3 },
                5000, 7, 0.4f);

            // 终极融合：四系大师lv2+ → 宇宙级理解
            AddRecipe("cosmic_understanding", "sm_know_cosmic_ultimate",
                new List<string> { "sm_know_mech_master", "sm_know_psi_master", "sm_know_magic_advanced", "sm_know_martial_advanced" },
                new List<int> { 2, 2, 3, 3 },
                20000, 10, 0.2f);

            Debug.Log("[超神机械师] 知识融合系统初始化完成（基于知识等级，原著还原）");
        }

        private static void AddRecipe(string id, string result, List<string> required, List<int> levels, int cost, int minLvl, float rate)
        {
            _recipes[id] = new FusionRecipe
            {
                id = id,
                resultKnowledge = result,
                requiredKnowledges = required,
                requiredLevels = levels,
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

        /// <summary>检查是否可以融合（v0.184：检查材料知识等级）</summary>
        public static bool CanFuse(Actor a, string recipeId)
        {
            Init();
            if (a == null || !_recipes.TryGetValue(recipeId, out var recipe)) return false;
            if (SuperMechAwakened.GetLevel(a) < recipe.minAwakenedLevel) return false;
            if (SuperMechInal.GetInal(a) < recipe.inalCost) return false;
            // 检查材料知识是否解锁且等级达标
            for (int i = 0; i < recipe.requiredKnowledges.Count; i++)
            {
                string kid = recipe.requiredKnowledges[i];
                int requiredLv = i < recipe.requiredLevels.Count ? recipe.requiredLevels[i] : 1;
                if (!SuperMechKnowledge.IsUnlocked(a, kid)) return false;
                if (SuperMechKnowledge.GetKnowledgeLevel(a, kid) < requiredLv) return false;
            }
            if (SuperMechKnowledge.IsUnlocked(a, recipe.resultKnowledge)) return false;
            if (SuperMechKnowledge.GetDef(recipe.resultKnowledge) == null) return false;
            return true;
        }

        /// <summary>获取融合需求文本（显示材料知识和等级要求）</summary>
        public static string GetRequirementText(Actor a, string recipeId)
        {
            Init();
            if (!_recipes.TryGetValue(recipeId, out var recipe)) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < recipe.requiredKnowledges.Count; i++)
            {
                string kid = recipe.requiredKnowledges[i];
                int requiredLv = i < recipe.requiredLevels.Count ? recipe.requiredLevels[i] : 1;
                int currentLv = SuperMechKnowledge.GetKnowledgeLevel(a, kid);
                bool unlocked = SuperMechKnowledge.IsUnlocked(a, kid);
                bool met = unlocked && currentLv >= requiredLv;
                var def = SuperMechKnowledge.GetDef(kid);
                string kname = def != null ? def.name : kid;
                string color = met ? "#66ff99" : "#ff6666";
                sb.AppendLine($"<color={color}>{kname} Lv{currentLv}/{requiredLv}</color>");
            }
            return sb.ToString();
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

        #region 兼容方法（旧版API残留，新版基于知识等级融合不需要这些）

        /// <summary>注册系统（兼容旧调用，实际调用Init）</summary>
        public static void Register() { Init(); }

        /// <summary>清理死亡单位数据（新版无状态，空操作）</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive) { return 0; }

        /// <summary>清空所有数据（新版无状态，空操作）</summary>
        public static void Clear() { }

        /// <summary>恢复已学配方（新版无已学配方概念，空操作）</summary>
        public static void RestoreLearnedRecipe(Actor a, string rid) { }

        /// <summary>获取已学配方列表（新版无已学配方概念，返回空列表）</summary>
        public static List<string> GetLearnedRecipes(Actor a) { return new List<string>(); }

        /// <summary>获取融合加成（新版融合直接解锁知识，无额外加成）</summary>
        public static float GetFusionBonus(Actor a) { return 0f; }

        /// <summary>检查制作冷却（新版无制作装备功能，返回false）</summary>
        public static bool IsCraftOnCooldown(Actor a) { return false; }

        /// <summary>制作装备（新版无制作装备功能，返回false）</summary>
        public static bool CraftEquip(Actor a, string recipeId) { return false; }

        #endregion
    }
}
