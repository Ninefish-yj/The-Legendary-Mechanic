using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechKnowledgeFusion
    {
        public class FusionRecipe
        {
            public string id;
            public string equipId;
            public string equipName;
            public string productType;
            public string desc;
            public string[] requiredKnowledge;
            public int xpCost;
            public float successRate;
            public float dmgMul;
            public float hpMul;
            public float speedMul;
            public int qiBonus;
            public string icon;
        }

        private static readonly List<FusionRecipe> _recipes = new List<FusionRecipe>();

        private static readonly Dictionary<string, FusionRecipe> _registeredEquips = new Dictionary<string, FusionRecipe>();

        private static readonly Dictionary<long, HashSet<string>> _unlockedRecipes = new Dictionary<long, HashSet<string>>();

        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();

        public static void Register()
        {
            Add("fusion_foldable_turret", "sm_fusion_foldable_turret", "sm_knowledgefusion_749", "sm_knowledgefusion_750",
                "sm_knowledgefusion_751",
                new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                5000, 0.8f, 1.5f, 1.3f, 1.1f, 200, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_emp_regulator", "sm_fusion_emp_regulator", "sm_knowledgefusion_752", "sm_knowledgefusion_750",
                "sm_knowledgefusion_753",
                new[] { "sm_know_mech_1_1_0", "sm_know_mech_1_1_1" },
                15000, 0.7f, 1.8f, 1.5f, 1.2f, 500, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_viper_mech", "sm_fusion_viper_mech", "sm_knowledgefusion_754", "sm_knowledgefusion_750",
                "sm_knowledgefusion_755",
                new[] { "sm_know_mech_2_0_0", "sm_know_mech_2_1_0", "sm_know_mech_2_2_0" },
                40000, 0.5f, 3.0f, 2.5f, 1.3f, 1000, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_god_mech", "sm_fusion_god_mech", "sm_knowledgefusion_756", "sm_knowledgefusion_750",
                "sm_knowledgefusion_757",
                new[] { "sm_know_mech_3_1_0", "sm_know_mech_4_1_0" },
                100000, 0.35f, 6.0f, 4.5f, 1.5f, 3000, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_body_armor", "sm_fusion_body_armor", "sm_knowledgefusion_758", "sm_knowledgefusion_759",
                "sm_knowledgefusion_760",
                new[] { "sm_know_martial_0_1_0", "sm_know_martial_0_1_1" },
                8000, 0.75f, 1.3f, 2.0f, 1.0f, 300, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_qi_burst_device", "sm_fusion_qi_burst_device", "sm_knowledgefusion_761", "sm_knowledgefusion_759",
                "sm_knowledgefusion_762",
                new[] { "sm_know_martial_1_2_0", "sm_know_martial_2_2_0" },
                20000, 0.65f, 2.0f, 1.5f, 1.4f, 600, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_gene_catalyst", "sm_fusion_gene_catalyst", "sm_knowledgefusion_763", "sm_knowledgefusion_764",
                "sm_knowledgefusion_765",
                new[] { "sm_know_psi_0_0_0", "sm_know_psi_1_0_0" },
                10000, 0.7f, 1.6f, 1.4f, 1.1f, 400, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_element_crystal", "sm_fusion_element_crystal", "sm_knowledgefusion_766", "sm_knowledgefusion_767",
                "sm_knowledgefusion_768",
                new[] { "sm_know_mage_0_2_0", "sm_know_mage_1_2_0" },
                12000, 0.7f, 1.7f, 1.6f, 1.2f, 500, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_soul_amplifier", "sm_fusion_soul_amplifier", "sm_knowledgefusion_769", "sm_knowledgefusion_770",
                "sm_knowledgefusion_771",
                new[] { "sm_know_mind_0_0_0", "sm_know_mind_1_0_0" },
                15000, 0.65f, 1.8f, 1.5f, 1.3f, 700, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_mech_martial", "sm_fusion_mech_martial", "sm_knowledgefusion_772", "sm_knowledgefusion_773",
                "sm_knowledgefusion_774",
                new[] { "sm_know_mech_0_0_0", "sm_know_martial_0_1_0" },
                25000, 0.5f, 2.5f, 2.0f, 1.3f, 800, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_psi_mind", "sm_fusion_psi_mind", "sm_knowledgefusion_775", "sm_knowledgefusion_773",
                "sm_knowledgefusion_776",
                new[] { "sm_know_psi_0_0_0", "sm_know_mind_0_0_0" },
                30000, 0.45f, 2.2f, 1.8f, 1.4f, 1000, "ui/Icons/actor_traits/iconChosenOne");

            Debug.Log($"[超神机械师] 知识融合配方注册完成：{_recipes.Count}个独特图纸");
        }

        private static void Add(string id, string equipId, string equipName, string productType, string desc,
            string[] required, int xpCost, float successRate,
            float dmgMul, float hpMul, float speedMul, int qiBonus, string icon)
        {
            _recipes.Add(new FusionRecipe
            {
                id = id, equipId = equipId, equipName = equipName, productType = productType, desc = desc,
                requiredKnowledge = required, xpCost = xpCost, successRate = successRate,
                dmgMul = dmgMul, hpMul = hpMul, speedMul = speedMul, qiBonus = qiBonus,
                icon = icon
            });
        }

        private static void RegisterFusionName(FusionRecipe recipe)
        {
            if (_registeredEquips.ContainsKey(recipe.equipId)) return;
            LocalizedTextManager.add(recipe.equipId, LocalizedTextManager.getText(recipe.equipName), pReplace: true);
            _registeredEquips[recipe.equipId] = recipe;
        }

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
                bool canFuse = true;
                foreach (string req in recipe.requiredKnowledge)
                {
                    if (!unlockedIds.Contains(req)) { canFuse = false; break; }
                }
                if (canFuse) list.Add(recipe);
            }
            return list;
        }

        public static bool TryFuse(Actor a, string recipeId)
        {
            if (a == null) return false;
            if (_cooldown.TryGetValue(a.id, out var cd) && Time.time < cd) return false;

            FusionRecipe recipe = null;
            foreach (var r in _recipes)
            {
                if (r.id == recipeId) { recipe = r; break; }
            }
            if (recipe == null) return false;

            if (!SuperMechAwakened.IsAwakened(a)) return false;
            if (!SuperMechAwakened.SpendXp(a, recipe.xpCost)) return false;

            _cooldown[a.id] = Time.time + 10f;

            float rate = recipe.successRate;
            int knowledgeCount = SuperMechKnowledge.GetUnlockedCount(a,
                SuperMechKnowledge.GetPrefixForClass(SuperMechBranch.GetClass(a)));
            rate += Mathf.Min(knowledgeCount * 0.01f, 0.2f);

            bool success = UnityEngine.Random.value < rate;

            if (success)
            {
                if (!_unlockedRecipes.TryGetValue(a.id, out var set))
                {
                    set = new HashSet<string>();
                    _unlockedRecipes[a.id] = set;
                }

                if (set.Contains(recipeId))
                {
                    Debug.Log($"[超神机械师] {a.name} 已学会{recipe.equipName}，融合无新收获");
                    return false;
                }

                set.Add(recipeId);
                RegisterFusionName(recipe);

                if (recipe.qiBonus > 0) SuperMechQi.AddQi(a, recipe.qiBonus);

                Debug.Log($"[超神机械师] {a.name} 知识融合成功！学会：{recipe.productType}·{recipe.equipName}");
            }
            else
            {
                Debug.Log($"[超神机械师] {a.name} 知识融合失败，消耗{recipe.xpCost}经验");
            }

            return success;
        }

        public static FusionBonus GetFusionBonus(Actor a)
        {
            var bonus = new FusionBonus();
            if (a == null || !_unlockedRecipes.TryGetValue(a.id, out var set)) return bonus;

            foreach (string recipeId in set)
            {
                foreach (var recipe in _recipes)
                {
                    if (recipe.id == recipeId)
                    {
                        bonus.dmgMul *= recipe.dmgMul;
                        bonus.hpMul *= recipe.hpMul;
                        bonus.speedMul *= recipe.speedMul;
                        break;
                    }
                }
            }
            return bonus;
        }

        public class FusionBonus
        {
            public float dmgMul = 1f;
            public float hpMul = 1f;
            public float speedMul = 1f;
        }

        public static List<FusionRecipe> GetLearnedRecipes(Actor a)
        {
            var list = new List<FusionRecipe>();
            if (a == null || !_unlockedRecipes.TryGetValue(a.id, out var set)) return list;
            foreach (var recipe in _recipes)
            {
                if (set.Contains(recipe.id)) list.Add(recipe);
            }
            return list;
        }

        private static readonly Dictionary<long, float> _craftCooldown = new Dictionary<long, float>();

        public static bool CraftEquip(Actor a, string recipeId)
        {
            if (a == null) return false;
            if (!SuperMechBranch.GetClass(a).Contains("sm_knowledgefusion_777")) return false;
            if (!_unlockedRecipes.TryGetValue(a.id, out var set) || !set.Contains(recipeId)) return false;
            if (_craftCooldown.TryGetValue(a.id, out var cd) && Time.time < cd) return false;

            FusionRecipe recipe = null;
            foreach (var r in _recipes)
            {
                if (r.id == recipeId) { recipe = r; break; }
            }
            if (recipe == null) return false;

            float craftCost = recipe.xpCost * 0.1f;
            if (SuperMechQi.GetQi(a) < craftCost) return false;
            SuperMechQi.SpendQi(a, craftCost);

            _craftCooldown[a.id] = Time.time + 15f;

            RegisterCraftedEquip(recipe);

            EquipCraftedItem(a, recipe.equipId);

            Debug.Log($"[超神机械师] {a.name} 用图纸制造出：{recipe.equipName}（消耗{craftCost:0}气力）");
            return true;
        }

        private static void RegisterCraftedEquip(FusionRecipe recipe)
        {
            string craftedId = recipe.equipId + "_crafted";
            if (_registeredEquips.ContainsKey(craftedId)) return;

            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            if (library == null) return;

            EquipmentAsset template = library.get("$amulet") ?? library.get("$ring");
            if (template == null) return;

            EquipmentAsset asset = library.clone(craftedId, template.id);
            if (asset == null) return;

            ((Asset)asset).id = craftedId;
            ((ItemAsset)asset).equipment_type = EquipmentType.Amulet;
            ((ItemAsset)asset).material = string.Empty;
            ((ItemAsset)asset).animated = false;
            ((ItemAsset)asset).is_pool_weapon = false;
            ((ItemAsset)asset).quality = Rarity.R3_Legendary;
            ((BaseUnlockableAsset)asset).base_stats = new BaseStats();
            ((BaseUnlockableAsset)asset).base_stats["multiplier_damage"] = recipe.dmgMul;
            ((BaseUnlockableAsset)asset).base_stats["multiplier_health"] = recipe.hpMul;
            ((BaseUnlockableAsset)asset).base_stats["multiplier_speed"] = recipe.speedMul;
            ((BaseUnlockableAsset)asset).unlock(true);

            LocalizedTextManager.add(craftedId, LocalizedTextManager.getText(recipe.equipName), pReplace: true);
            _registeredEquips[craftedId] = recipe;
        }

        private static void EquipCraftedItem(Actor a, string equipId)
        {
            if (a == null || a.equipment == null) return;
            string craftedId = equipId + "_crafted";

            var slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null) return;

            if (!slot.isEmpty())
            {
                Item current = slot.getItem();
                if (current != null)
                {
                    int currentIdx = SuperMechRelic.GetEquipIndex(current);
                    if (currentIdx >= 0) SuperMechEquipBag.AddToBag(a, SuperMechRelic.Equipments[currentIdx].id);
                }
                slot.takeAwayItem();
            }

            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            EquipmentAsset asset = library.get(craftedId);
            if (asset == null) return;

            try
            {
                Item item = World.world.items.generateItem(asset, a.kingdom, a.getName(), 0, a, 1, false);
                if (item == null) return;
                item.calculateValues();
                slot.setItem(item, a);
                SuperMechEquipAffix.OnEquip(a, 5);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 装备制造产物失败: {e.Message}");
            }
        }

        public static bool IsCraftOnCooldown(Actor a)
        {
            if (a != null && _craftCooldown.TryGetValue(a.id, out var cd))
                return Time.time < cd;
            return false;
        }

        public static void RestoreLearnedRecipe(Actor a, string recipeId)
        {
            if (a == null || string.IsNullOrEmpty(recipeId)) return;
            if (!_unlockedRecipes.TryGetValue(a.id, out var set))
            {
                set = new HashSet<string>();
                _unlockedRecipes[a.id] = set;
            }
            set.Add(recipeId);
            foreach (var recipe in _recipes)
            {
                if (recipe.id == recipeId)
                {
                    RegisterFusionName(recipe);
                    break;
                }
            }
        }

        public static int GetUnlockedCount(Actor a)
        {
            if (a != null && _unlockedRecipes.TryGetValue(a.id, out var set))
                return set.Count;
            return 0;
        }

        public static bool IsOnCooldown(Actor a)
        {
            if (a != null && _cooldown.TryGetValue(a.id, out var cd))
                return Time.time < cd;
            return false;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_unlockedRecipes, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }

        public static void Clear() { _unlockedRecipes.Clear(); _cooldown.Clear(); _craftCooldown.Clear(); }
    }
}
