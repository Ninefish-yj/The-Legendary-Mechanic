using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识融合系统：消耗经验将多个知识融合，创造独特的新装备/图纸。
    /// 原著ch107："[是否进行知识融合（基础电磁原理lv4、基础能源理论lv3...），本次消耗2万经验]"
    /// ch136：折叠式小型炮台，本体是知识融合出的固定炮台
    /// ch141：电磁脉冲调理器，在一次知识融合中意外得到
    /// ch168：蝰蛇轻装机甲，四个进阶知识组合的知识融合
    /// 融合产物是独特的新装备，不是已有的9级品质装备。
    /// </summary>
    public static class SuperMechKnowledgeFusion
    {
        /// <summary>融合配方定义——每个配方产出一个独特的新装备。</summary>
        public class FusionRecipe
        {
            public string id;
            public string equipId;        // 产物装备ID（融合成功时动态注册）
            public string equipName;      // 产物装备名称（原著图纸名）
            public string desc;
            public string[] requiredKnowledge; // 需要的知识ID
            public int xpCost;
            public float successRate;
            public float dmgMul;          // 产物装备伤害倍率
            public float hpMul;           // 产物装备生命倍率
            public float speedMul;        // 产物装备攻速倍率
            public int qiBonus;           // 产物装备气力加成
            public string icon;           // 产物装备图标
        }

        /// <summary>所有融合配方。</summary>
        private static readonly List<FusionRecipe> _recipes = new List<FusionRecipe>();

        /// <summary>已注册的融合装备（equipId → recipe），避免重复注册。</summary>
        private static readonly Dictionary<string, FusionRecipe> _registeredEquips = new Dictionary<string, FusionRecipe>();

        /// <summary>单位已融合出的图纸（actorId → HashSet<recipeId>）。</summary>
        private static readonly Dictionary<long, HashSet<string>> _unlockedRecipes = new Dictionary<long, HashSet<string>>();

        /// <summary>融合冷却（actorId → 下次可融合时间）。</summary>
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();

        public static void Register()
        {
            // ===== 机械系融合（原著图纸名）=====
            Add("fusion_foldable_turret", "sm_fusion_foldable_turret", "折叠式小型炮台",
                "ch136：知识融合出的固定炮台，加入折叠技术，非常实用",
                new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                5000, 0.8f, 1.5f, 1.3f, 1.1f, 200, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_emp_regulator", "sm_fusion_emp_regulator", "电磁脉冲调理器",
                "ch141：用电磁波调理生物体的特殊装备，提升状态",
                new[] { "sm_know_mech_1_1_0", "sm_know_mech_1_1_1" },
                15000, 0.7f, 1.8f, 1.5f, 1.2f, 500, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_viper_mech", "sm_fusion_viper_mech", "蝰蛇轻装机甲",
                "ch168：四个进阶知识组合的知识融合，进阶标准稀有装备",
                new[] { "sm_know_mech_2_0_0", "sm_know_mech_2_1_0", "sm_know_mech_2_2_0" },
                40000, 0.5f, 3.0f, 2.5f, 1.3f, 1000, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_god_mech", "sm_fusion_god_mech", "神级机械核心",
                "尖端+终极知识融合，接近古神机械师水平的造物",
                new[] { "sm_know_mech_3_1_0", "sm_know_mech_4_1_0" },
                100000, 0.35f, 6.0f, 4.5f, 1.5f, 3000, "ui/Icons/actor_traits/iconChosenOne");

            // ===== 武道系融合 =====
            Add("fusion_body_armor", "sm_fusion_body_armor", "炼体护甲",
                "体魄类知识融合，将炼体技巧化为护体装备",
                new[] { "sm_know_martial_0_1_0", "sm_know_martial_0_1_1" },
                8000, 0.75f, 1.3f, 2.0f, 1.0f, 300, "ui/Icons/actor_traits/iconBlessing");

            Add("fusion_qi_burst_device", "sm_fusion_qi_burst_device", "暴气增幅器",
                "气劲类知识融合，辅助暴气技巧的装备",
                new[] { "sm_know_martial_1_2_0", "sm_know_martial_2_2_0" },
                20000, 0.65f, 2.0f, 1.5f, 1.4f, 600, "ui/Icons/actor_traits/iconChosenOne");

            // ===== 异能系融合 =====
            Add("fusion_gene_catalyst", "sm_fusion_gene_catalyst", "基因觉醒催化剂",
                "基因类知识融合，加速基因链觉醒的特殊装备",
                new[] { "sm_know_psi_0_0_0", "sm_know_psi_1_0_0" },
                10000, 0.7f, 1.6f, 1.4f, 1.1f, 400, "ui/Icons/actor_traits/iconBlessing");

            // ===== 魔法系融合 =====
            Add("fusion_element_crystal", "sm_fusion_element_crystal", "元素共鸣水晶",
                "元素类知识融合，储存元素能量的水晶",
                new[] { "sm_know_mage_0_2_0", "sm_know_mage_1_2_0" },
                12000, 0.7f, 1.7f, 1.6f, 1.2f, 500, "ui/Icons/actor_traits/iconBlessing");

            // ===== 念力系融合 =====
            Add("fusion_soul_amplifier", "sm_fusion_soul_amplifier", "灵魂增幅器",
                "灵魂类知识融合，放大精神力的装置",
                new[] { "sm_know_mind_0_0_0", "sm_know_mind_1_0_0" },
                15000, 0.65f, 1.8f, 1.5f, 1.3f, 700, "ui/Icons/actor_traits/iconChosenOne");

            // ===== 跨系融合（稀有）=====
            Add("fusion_mech_martial", "sm_fusion_mech_martial", "械武者外骨骼",
                "机械+武道知识融合，械武者专属外骨骼装甲",
                new[] { "sm_know_mech_0_0_0", "sm_know_martial_0_1_0" },
                25000, 0.5f, 2.5f, 2.0f, 1.3f, 800, "ui/Icons/actor_traits/iconChosenOne");

            Add("fusion_psi_mind", "sm_fusion_psi_mind", "精神异能核心",
                "异能+念力知识融合，将基因异能与精神力结合",
                new[] { "sm_know_psi_0_0_0", "sm_know_mind_0_0_0" },
                30000, 0.45f, 2.2f, 1.8f, 1.4f, 1000, "ui/Icons/actor_traits/iconChosenOne");

            Debug.Log($"[超神机械师] 知识融合配方注册完成：{_recipes.Count}个独特图纸");
        }

        private static void Add(string id, string equipId, string equipName, string desc,
            string[] required, int xpCost, float successRate,
            float dmgMul, float hpMul, float speedMul, int qiBonus, string icon)
        {
            _recipes.Add(new FusionRecipe
            {
                id = id, equipId = equipId, equipName = equipName, desc = desc,
                requiredKnowledge = required, xpCost = xpCost, successRate = successRate,
                dmgMul = dmgMul, hpMul = hpMul, speedMul = speedMul, qiBonus = qiBonus,
                icon = icon
            });
        }

        /// <summary>动态注册融合产物装备到物品库（首次融合成功时调用）。</summary>
        private static void RegisterFusionEquip(FusionRecipe recipe)
        {
            if (_registeredEquips.ContainsKey(recipe.equipId)) return;

            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            if (library == null) return;

            EquipmentAsset template = library.get("$amulet") ?? library.get("$ring");
            if (template == null) return;

            EquipmentAsset asset = library.clone(recipe.equipId, template.id);
            if (asset == null) return;

            ((Asset)asset).id = recipe.equipId;
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

            // 本地化名称
            LocalizedTextManager.add(recipe.equipId, recipe.equipName, pReplace: true);

            _registeredEquips[recipe.equipId] = recipe;
            Debug.Log($"[超神机械师] 知识融合创造新装备：{recipe.equipName}（{recipe.equipId}）");
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
                bool canFuse = true;
                foreach (string req in recipe.requiredKnowledge)
                {
                    if (!unlockedIds.Contains(req)) { canFuse = false; break; }
                }
                if (canFuse) list.Add(recipe);
            }
            return list;
        }

        /// <summary>尝试知识融合。成功则创造独特新装备并给单位装备。</summary>
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

            // 成功率：基础 + 知识数量加成（最多+20%）
            float rate = recipe.successRate;
            int knowledgeCount = SuperMechKnowledge.GetUnlockedCount(a,
                SuperMechKnowledge.GetPrefixForClass(SuperMechBranch.GetClass(a)));
            rate += Mathf.Min(knowledgeCount * 0.01f, 0.2f);

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

                // 动态注册新装备（如果是首次融合出这个图纸）
                RegisterFusionEquip(recipe);

                // 给单位装备这个新装备
                EquipFusionItem(a, recipe.equipId);

                // 气力加成
                if (recipe.qiBonus > 0) SuperMechQi.AddQi(a, recipe.qiBonus);

                Debug.Log($"[超神机械师] {a.name} 知识融合成功！创造新装备：{recipe.equipName}");
            }
            else
            {
                Debug.Log($"[超神机械师] {a.name} 知识融合失败，消耗{recipe.xpCost}经验");
            }

            return success;
        }

        /// <summary>给单位装备融合产物。</summary>
        private static void EquipFusionItem(Actor a, string equipId)
        {
            if (a == null || a.equipment == null) return;

            var slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null) return;

            // 旧装备放回背包
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

            // 创建并装备融合产物
            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            EquipmentAsset asset = library.get(equipId);
            if (asset == null) return;

            try
            {
                Item item = World.world.items.generateItem(asset, a.kingdom, a.getName(), 0, a, 1, false);
                if (item == null) return;
                item.calculateValues();
                slot.setItem(item, a);
                SuperMechEquipAffix.OnEquip(a, 5); // 融合装备按粉色品质roll词条
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 装备融合产物失败: {e.Message}");
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
