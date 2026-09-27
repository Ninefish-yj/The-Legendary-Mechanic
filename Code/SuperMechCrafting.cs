using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 机械师造兵闭环（原著 ch3/ch50/ch178）。
    /// 核心循环：机械师学习制造知识→消耗材料造兵→造兵获得气力/经验→升级→解锁更强制造→造兵战斗反哺经验。
    /// 原著设定：完成组装获得经验，完美度影响经验量（ch3: 69%→28exp, 73%→32exp）。
    /// 造兵击杀敌人时，制造者获得经验分成（原著：机械军团作战=机械师经验）。
    /// </summary>
    public static class SuperMechCrafting
    {
        // 制造冷却（unit.id -> 下次可制造时间）
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();
        // 召唤物追踪（性能保护：限制全场召唤物数量）
        private static readonly HashSet<long> _summonedIds = new HashSet<long>();
        // 造兵→主人映射（spawned.id -> maker.id）
        private static readonly Dictionary<long, long> _masterMap = new Dictionary<long, long>();
        // 主人→造兵列表（maker.id -> list of spawned.id）
        private static readonly Dictionary<long, List<long>> _minions = new Dictionary<long, List<long>>();

        /// <summary>制造单位模板定义。</summary>
        public struct CraftRecipe
        {
            public string id;           // 神权id
            public string name;         // 显示名
            public string desc;         // 描述
            public string creatureId;   // WorldBox生物id
            public int minStage;        // 最低机械阶段tier
            public string requiredKnowledge; // 需要的知识节点id（空=按阶段解锁）
            public float qiBase;        // 基础气力奖励
            public float expBase;       // 基础经验奖励
            public string[] traits;     // 制造出的单位附带特质
            public Dictionary<string, int> cost; // 材料消耗 {resourceId: amount}
        }

        // 制造配方表（按阶段+知识解锁）
        public static readonly CraftRecipe[] Recipes =
        {
            // tier1: 入门者即可造
            new CraftRecipe {
                id = "sm_craft_ranger", name = "sm_crafting_677", desc = "sm_crafting_678",
                creatureId = "bandit", minStage = 1, requiredKnowledge = "",
                qiBase = 5f, expBase = 10f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 5} }
            },
            // tier2: 学徒
            new CraftRecipe {
                id = "sm_craft_drone", name = "sm_crafting_679", desc = "sm_crafting_680",
                creatureId = "alien", minStage = 2, requiredKnowledge = "",
                qiBase = 8f, expBase = 15f,
                traits = new[] { "fast", "genius" },
                cost = new Dictionary<string, int> { {"common_metals", 8}, {"gems", 2} }
            },
            // tier3: 见习
            new CraftRecipe {
                id = "sm_craft_sentry", name = "sm_crafting_681", desc = "sm_crafting_682",
                creatureId = "civ_crystal_golem", minStage = 3, requiredKnowledge = "",
                qiBase = 12f, expBase = 25f,
                traits = new[] { "tough", "immortal" },
                cost = new Dictionary<string, int> { {"common_metals", 15}, {"stone", 10} }
            },
            // tier4: 磁环
            new CraftRecipe {
                id = "sm_craft_mech", name = "sm_crafting_683", desc = "sm_crafting_684",
                creatureId = "crabzilla", minStage = 4, requiredKnowledge = "",
                qiBase = 20f, expBase = 40f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 25}, {"gems", 5} }
            },
            // tier5: 数据
            new CraftRecipe {
                id = "sm_craft_carrier", name = "sm_crafting_685", desc = "sm_crafting_686",
                creatureId = "dragon", minStage = 5, requiredKnowledge = "",
                qiBase = 30f, expBase = 60f,
                traits = new[] { "fast", "strong" },
                cost = new Dictionary<string, int> { {"common_metals", 30}, {"wood", 20} }
            },
            // tier6: 战争
            new CraftRecipe {
                id = "sm_craft_fortress", name = "sm_crafting_687", desc = "sm_crafting_688",
                creatureId = "civ_crystal_golem", minStage = 6, requiredKnowledge = "",
                qiBase = 50f, expBase = 100f,
                traits = new[] { "strong", "tough", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 10}, {"gems", 10} }
            },
            // tier7: 虚拟
            new CraftRecipe {
                id = "sm_craft_virtual", name = "sm_crafting_689", desc = "sm_crafting_690",
                creatureId = "human", minStage = 7, requiredKnowledge = "",
                qiBase = 80f, expBase = 150f,
                traits = new[] { "immortal", "genius", "fast" },
                cost = new Dictionary<string, int> { {"gold", 20}, {"gems", 15} }
            },
            // tier8: 星海
            new CraftRecipe {
                id = "sm_craft_cruiser", name = "sm_crafting_691", desc = "sm_crafting_692",
                creatureId = "dragon", minStage = 8, requiredKnowledge = "",
                qiBase = 120f, expBase = 250f,
                traits = new[] { "strong", "tough", "fast" },
                cost = new Dictionary<string, int> { {"adamantine", 20}, {"dragon_scales", 5} }
            },
            // tier10: 使徒
            new CraftRecipe {
                id = "sm_craft_apostle", name = "sm_crafting_693", desc = "sm_crafting_694",
                creatureId = "crabzilla", minStage = 10, requiredKnowledge = "",
                qiBase = 200f, expBase = 500f,
                traits = new[] { "strong", "tough", "immortal", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 30}, {"gold", 30}, {"gems", 20} }
            },
        };

        /// <summary>注册制造神权。</summary>
        /// <summary>尝试制造一个机械单位（公共方法，供知识Tab按钮调用）。返回true=成功。</summary>
        public static bool TryCraft(Actor maker, string recipeId, WorldTile tile)
        {
            if (maker == null || !maker.isAlive() || tile == null) return false;
            if (!maker.hasTrait(SuperMechTraits.ClassMech)) return false;
            if (!SuperMechConfig.MechSummonEnabled) return false;

            CraftRecipe? r = null;
            foreach (var recipe in Recipes)
            {
                if (recipe.id == recipeId) { r = recipe; break; }
            }
            if (!r.HasValue) return false;

            int stage = GetMechStageTier(maker);
            if (stage < r.Value.minStage) return false;

            if (!string.IsNullOrEmpty(r.Value.requiredKnowledge) && !SuperMechKnowledge.IsUnlocked(maker, r.Value.requiredKnowledge))
                return false;

            float now = Time.time;
            float cd;
            if (_cooldown.TryGetValue(maker.id, out cd) && now < cd) return false;

            if (!ConsumeMaterials(maker, r.Value.cost)) return false;

            float intel = 1f;
            var stats = SuperMechStats.Of(maker);
            if (stats != null)
            {
                float iv = stats["intelligence"];
                intel = 1f + iv * 0.05f;
            }
            float perfection = Mathf.Clamp(0.5f + intel * 0.1f + stage * 0.03f, 0.5f, 1.5f);

            _summonedIds.RemoveWhere(id => World.world.units.get(id) == null || !World.world.units.get(id).isAlive());
            if (_summonedIds.Count >= SuperMechConfig.MaxSummonedUnits) return false;

            Actor spawned = World.world.units.createNewUnit(
                r.Value.creatureId, tile, pMiracleSpawn: false, pAdultAge: true);
            if (spawned == null) return false;

            _summonedIds.Add(spawned.id);
            _masterMap[spawned.id] = maker.id;
            if (!_minions.ContainsKey(maker.id)) _minions[maker.id] = new List<long>();
            _minions[maker.id].Add(spawned.id);

            foreach (var tid in r.Value.traits)
            {
                if (AssetManager.traits.get(tid) != null) spawned.addTrait(tid);
            }

            float qiGain = r.Value.qiBase * perfection;
            float expGain = r.Value.expBase * perfection * 100f;
            SuperMechQi.AddQi(maker, qiGain);
            if (SuperMechAwakened.IsAwakened(maker))
                SuperMechAwakened.AddXp(maker, expGain);
            SuperMechAdvancementTask.OnCraft(maker);

            if (SuperMechAwakened.IsAwakened(maker) && stage >= 4 && perfection >= 1.0f)
                SuperMechDivinity.AwardCraftingPoints(maker);

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {maker.name} 制造{r.Value.name} 完美度{perfection:F0%} 气力+{qiGain:F1}");

            float cdTime = Mathf.Max(2f, 5f - intel * 0.2f);
            _cooldown[maker.id] = now + cdTime;
            return true;
        }

        /// <summary>获取单位可制造的配方列表（阶段+知识条件满足）。</summary>
        public static List<CraftRecipe> GetAvailableRecipes(Actor maker)
        {
            var result = new List<CraftRecipe>();
            if (maker == null || !maker.hasTrait(SuperMechTraits.ClassMech)) return result;
            int stage = GetMechStageTier(maker);
            foreach (var r in Recipes)
            {
                if (stage < r.minStage) continue;
                if (!string.IsNullOrEmpty(r.requiredKnowledge) && !SuperMechKnowledge.IsUnlocked(maker, r.requiredKnowledge)) continue;
                result.Add(r);
            }
            return result;
        }

        /// <summary>获取制造冷却剩余秒数。</summary>
        public static float GetCraftCooldown(Actor maker)
        {
            if (maker == null) return 0f;
            float cd;
            if (_cooldown.TryGetValue(maker.id, out cd))
                return Mathf.Max(0f, cd - Time.time);
            return 0f;
        }

        public static void Register()
        {
            foreach (var r in Recipes)
            {
                LocalizedTextManager.add(r.name, r.name, pReplace: true);
                LocalizedTextManager.add(r.name + "_description", r.desc, pReplace: true);
            }

            // 造兵功能已移到知识Tab「◆ 操作」区域，不再注册9个神权按钮
            // 公共方法 TryCraft(Actor, recipeId, tile) 供知识Tab按钮调用
            Debug.Log($"[超神机械师] 制造系统注册完成：{Recipes.Length} 种制造配方（知识Tab操作）");
        }

        /// <summary>从单位所在城市仓库扣除材料。返回true=扣除成功。</summary>
        private static bool ConsumeMaterials(Actor maker, Dictionary<string, int> cost)
        {
            if (cost == null || cost.Count == 0) return true;
            City city = maker.city;
            if (city == null)
            {
                // 没有城市时直接允许（野外机械师从背包/世界获取材料）
                return true;
            }
            // 检查材料是否足够
            foreach (var kv in cost)
            {
                if (city.getResourcesAmount(kv.Key) < kv.Value) return false;
            }
            // 扣除材料
            foreach (var kv in cost)
            {
                city.takeResource(kv.Key, kv.Value);
            }
            return true;
        }

        /// <summary>造兵击杀敌人时调用，给主人经验分成。</summary>
        public static void OnMinionKill(Actor minion, Actor victim)
        {
            long masterId;
            if (!_masterMap.TryGetValue(minion.id, out masterId)) return;
            Actor master = World.world.units.get(masterId);
            if (master == null || !master.isAlive()) return;

            // 主人获得击杀经验的30%（原著：机械军团作战经验归机械师）
            float expShare = 5f * Mathf.Clamp(victim.data.level / 5f, 1f, 10f);
            if (SuperMechAwakened.IsAwakened(master))
            {
                SuperMechAwakened.AddXp(master, expShare);
            }
            // 主人也获得少量气力
            SuperMechQi.AddQi(master, expShare * 0.1f);

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {master.name} 的造兵 {minion.name} 击杀 {victim.name}，主人获得经验{expShare:F0}");
        }

        /// <summary>获取单位的造兵数量。</summary>
        public static int GetMinionCount(Actor maker)
        {
            List<long> list;
            if (!_minions.TryGetValue(maker.id, out list)) return 0;
            list.RemoveAll(id => World.world.units.get(id) == null || !World.world.units.get(id).isAlive());
            return list.Count;
        }

        /// <summary>获取机械师阶段tier（1-14）。</summary>
        private static int GetMechStageTier(Actor a)
        {
            return SuperMechStage.GetStage(a);
        }

        /// <summary>世界切换时清空。</summary>
        public static void Clear()
        {
            _cooldown.Clear();
            _summonedIds.Clear();
            _masterMap.Clear();
            _minions.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            // 清理造兵-主人映射
            var deadMinions = _masterMap.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in deadMinions)
            {
                long masterId;
                if (_masterMap.TryGetValue(id, out masterId))
                {
                    List<long> list;
                    if (_minions.TryGetValue(masterId, out list))
                    {
                        list.Remove(id);
                    }
                }
                _masterMap.Remove(id);
                _summonedIds.Remove(id);
                removed++;
            }
            // 清理主人列表中已死亡的主人
            var deadMasters = _minions.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in deadMasters)
            {
                _minions.Remove(id);
                removed++;
            }
            return removed;
        }
    }
}
