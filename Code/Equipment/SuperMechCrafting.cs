using System.Collections.Generic;
using System.Linq;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechCrafting
    {
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();
        private static readonly HashSet<long> _summonedIds = new HashSet<long>();
        private static readonly Dictionary<long, long> _masterMap = new Dictionary<long, long>();
        private static readonly Dictionary<long, List<long>> _minions = new Dictionary<long, List<long>>();

        public struct CraftRecipe
        {
            public string id;
            public string name;
            public string desc;
            public string creatureId;
            public int minStage;
            public string requiredKnowledge;
            public float qiBase;
            public float expBase;
            public string[] traits;
            public Dictionary<string, int> cost;
        }

        public static readonly CraftRecipe[] Recipes =
        {
            new CraftRecipe {
                id = "sm_craft_ranger", name = "sm_crafting_677", desc = "sm_crafting_678",
                creatureId = "bandit", minStage = 1, requiredKnowledge = "sm_know_mech_0_0_0",
                qiBase = 5f, expBase = 10f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 5} }
            },
            new CraftRecipe {
                id = "sm_craft_drone", name = "sm_crafting_679", desc = "sm_crafting_680",
                creatureId = "alien", minStage = 2, requiredKnowledge = "sm_know_mech_0_1_0",
                qiBase = 8f, expBase = 15f,
                traits = new[] { "fast", "genius" },
                cost = new Dictionary<string, int> { {"common_metals", 8}, {"gems", 2} }
            },
            new CraftRecipe {
                id = "sm_craft_sentry", name = "sm_crafting_681", desc = "sm_crafting_682",
                creatureId = "civ_crystal_golem", minStage = 3, requiredKnowledge = "sm_know_mech_1_0_0",
                qiBase = 12f, expBase = 25f,
                traits = new[] { "tough", "immortal" },
                cost = new Dictionary<string, int> { {"common_metals", 15}, {"stone", 10} }
            },
            new CraftRecipe {
                id = "sm_craft_mech", name = "sm_crafting_683", desc = "sm_crafting_684",
                creatureId = "crabzilla", minStage = 4, requiredKnowledge = "sm_know_mech_1_1_0",
                qiBase = 20f, expBase = 40f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 25}, {"gems", 5} }
            },
            new CraftRecipe {
                id = "sm_craft_carrier", name = "sm_crafting_685", desc = "sm_crafting_686",
                creatureId = "dragon", minStage = 5, requiredKnowledge = "sm_know_mech_2_0_0",
                qiBase = 30f, expBase = 60f,
                traits = new[] { "fast", "strong" },
                cost = new Dictionary<string, int> { {"common_metals", 30}, {"wood", 20} }
            },
            new CraftRecipe {
                id = "sm_craft_fortress", name = "sm_crafting_687", desc = "sm_crafting_688",
                creatureId = "civ_crystal_golem", minStage = 6, requiredKnowledge = "sm_know_mech_2_1_0",
                qiBase = 50f, expBase = 100f,
                traits = new[] { "strong", "tough", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 10}, {"gems", 10} }
            },
            new CraftRecipe {
                id = "sm_craft_virtual", name = "sm_crafting_689", desc = "sm_crafting_690",
                creatureId = "human", minStage = 7, requiredKnowledge = "sm_know_mech_3_0_0",
                qiBase = 80f, expBase = 150f,
                traits = new[] { "immortal", "genius", "fast" },
                cost = new Dictionary<string, int> { {"gold", 20}, {"gems", 15} }
            },
            new CraftRecipe {
                id = "sm_craft_cruiser", name = "sm_crafting_691", desc = "sm_crafting_692",
                creatureId = "dragon", minStage = 8, requiredKnowledge = "sm_know_mech_3_1_0",
                qiBase = 120f, expBase = 250f,
                traits = new[] { "strong", "tough", "fast" },
                cost = new Dictionary<string, int> { {"adamantine", 20}, {"dragon_scales", 5} }
            },
            new CraftRecipe {
                id = "sm_craft_apostle", name = "sm_crafting_693", desc = "sm_crafting_694",
                creatureId = "crabzilla", minStage = 10, requiredKnowledge = "sm_know_mech_4_0_0",
                qiBase = 200f, expBase = 500f,
                traits = new[] { "strong", "tough", "immortal", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 30}, {"gold", 30}, {"gems", 20} }
            },
        };

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
            SuperMechStageTask.OnCraft(maker);

            if (SuperMechAwakened.IsAwakened(maker) && stage >= 4 && perfection >= 1.0f)
                SuperMechDivinity.AwardCraftingPoints(maker);


            float cdTime = Mathf.Max(2f, 5f - intel * 0.2f);
            _cooldown[maker.id] = now + cdTime;
            return true;
        }

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
                LocalizedTextManager.add(r.name, LocalizedTextManager.getText(r.name), pReplace: true);
                LocalizedTextManager.add(r.name + "_description", LocalizedTextManager.getText(r.desc), pReplace: true);
            }

        }

        private static bool ConsumeMaterials(Actor maker, Dictionary<string, int> cost)
        {
            if (cost == null || cost.Count == 0) return true;
            City city = maker.city;
            if (city == null)
            {
                return true;
            }
            foreach (var kv in cost)
            {
                if (city.getResourcesAmount(kv.Key) < kv.Value) return false;
            }
            foreach (var kv in cost)
            {
                city.takeResource(kv.Key, kv.Value);
            }
            return true;
        }

        public static void OnMinionKill(Actor minion, Actor victim)
        {
            long masterId;
            if (!_masterMap.TryGetValue(minion.id, out masterId)) return;
            Actor master = World.world.units.get(masterId);
            if (master == null || !master.isAlive()) return;

            float expShare = 5f * Mathf.Clamp(victim.data.level / 5f, 1f, 10f);
            if (SuperMechAwakened.IsAwakened(master))
            {
                SuperMechAwakened.AddXp(master, expShare);
            }
            SuperMechQi.AddQi(master, expShare * 0.1f);

        }

        /// <summary>材料是否充足（UI 按钮可用态检查；无城市=免材料直接可造）</summary>
        public static bool HasMaterials(Actor maker, Dictionary<string, int> cost)
        {
            if (cost == null || cost.Count == 0) return true;
            City city = maker == null ? null : maker.city;
            if (city == null) return true;
            foreach (var kv in cost)
                if (city.getResourcesAmount(kv.Key) < kv.Value) return false;
            return true;
        }

        public static int GetMinionCount(Actor maker)
        {
            List<long> list;
            if (!_minions.TryGetValue(maker.id, out list)) return 0;
            list.RemoveAll(id => World.world.units.get(id) == null || !World.world.units.get(id).isAlive());
            return list.Count;
        }

        private static int GetMechStageTier(Actor a)
        {
            return SuperMechStage.GetStage(a);
        }

        public static void Clear()
        {
            _cooldown.Clear();
            _summonedIds.Clear();
            _masterMap.Clear();
            _minions.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
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
