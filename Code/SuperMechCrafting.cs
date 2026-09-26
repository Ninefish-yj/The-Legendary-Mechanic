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
                id = "sm_craft_ranger", name = "制造·游骑兵", desc = "消耗金属制造游骑兵步兵（tier1）",
                creatureId = "bandit", minStage = 1, requiredKnowledge = "",
                qiBase = 5f, expBase = 10f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 5} }
            },
            // tier2: 学徒
            new CraftRecipe {
                id = "sm_craft_drone", name = "制造·侦察无人机", desc = "消耗金属+宝石制造侦察无人机（tier2）",
                creatureId = "alien", minStage = 2, requiredKnowledge = "",
                qiBase = 8f, expBase = 15f,
                traits = new[] { "fast", "genius" },
                cost = new Dictionary<string, int> { {"common_metals", 8}, {"gems", 2} }
            },
            // tier3: 见习
            new CraftRecipe {
                id = "sm_craft_sentry", name = "制造·哨戒炮", desc = "消耗金属+石头制造固定哨戒炮（tier3）",
                creatureId = "civ_crystal_golem", minStage = 3, requiredKnowledge = "",
                qiBase = 12f, expBase = 25f,
                traits = new[] { "tough", "immortal" },
                cost = new Dictionary<string, int> { {"common_metals", 15}, {"stone", 10} }
            },
            // tier4: 磁环
            new CraftRecipe {
                id = "sm_craft_mech", name = "制造·战斗机甲", desc = "消耗金属+宝石制造战斗机甲（tier4）",
                creatureId = "crabzilla", minStage = 4, requiredKnowledge = "",
                qiBase = 20f, expBase = 40f,
                traits = new[] { "strong", "tough" },
                cost = new Dictionary<string, int> { {"common_metals", 25}, {"gems", 5} }
            },
            // tier5: 数据
            new CraftRecipe {
                id = "sm_craft_carrier", name = "制造·运载机", desc = "消耗金属+木材制造运载机（tier5）",
                creatureId = "dragon", minStage = 5, requiredKnowledge = "",
                qiBase = 30f, expBase = 60f,
                traits = new[] { "fast", "strong" },
                cost = new Dictionary<string, int> { {"common_metals", 30}, {"wood", 20} }
            },
            // tier6: 战争
            new CraftRecipe {
                id = "sm_craft_fortress", name = "制造·战争堡垒", desc = "消耗精金+宝石制造战争堡垒（tier6）",
                creatureId = "civ_crystal_golem", minStage = 6, requiredKnowledge = "",
                qiBase = 50f, expBase = 100f,
                traits = new[] { "strong", "tough", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 10}, {"gems", 10} }
            },
            // tier7: 虚拟
            new CraftRecipe {
                id = "sm_craft_virtual", name = "制造·虚拟生命体", desc = "消耗金+宝石制造虚拟生命体（tier7）",
                creatureId = "human", minStage = 7, requiredKnowledge = "",
                qiBase = 80f, expBase = 150f,
                traits = new[] { "immortal", "genius", "fast" },
                cost = new Dictionary<string, int> { {"gold", 20}, {"gems", 15} }
            },
            // tier8: 星海
            new CraftRecipe {
                id = "sm_craft_cruiser", name = "制造·星际巡洋舰", desc = "消耗精金+龙鳞制造星际巡洋舰（tier8）",
                creatureId = "dragon", minStage = 8, requiredKnowledge = "",
                qiBase = 120f, expBase = 250f,
                traits = new[] { "strong", "tough", "fast" },
                cost = new Dictionary<string, int> { {"adamantine", 20}, {"dragon_scales", 5} }
            },
            // tier10: 使徒
            new CraftRecipe {
                id = "sm_craft_apostle", name = "制造·使徒兵器", desc = "消耗精金+金+宝石制造使徒兵器（tier10）",
                creatureId = "crabzilla", minStage = 10, requiredKnowledge = "",
                qiBase = 200f, expBase = 500f,
                traits = new[] { "strong", "tough", "immortal", "regeneration" },
                cost = new Dictionary<string, int> { {"adamantine", 30}, {"gold", 30}, {"gems", 20} }
            },
        };

        /// <summary>注册制造神权。</summary>
        public static void Register()
        {
            foreach (var r in Recipes)
            {
                LocalizedTextManager.add(r.name, r.name, pReplace: true);
                LocalizedTextManager.add(r.name + "_description", r.desc, pReplace: true);
            }

            foreach (var r in Recipes)
            {
                var p = new GodPower
                {
                    id = r.id,
                    name = r.name,
                    path_icon = "ui/powers/power_summon_units",
                    rank = PowerRank.Rank0_free,
                    force_map_mode = MetaType.None,
                    ignore_fast_spawn = true,
                    hold_action = false,
                    unselect_when_window = true,
                    requires_premium = false
                };
                p.click_action += (tile, powerId) =>
                {
                    if (tile == null) return false;
                    if (!SuperMechConfig.MechSummonEnabled) return false;
                    bool crafted = false;
                    tile.doUnits(u =>
                    {
                        if (u == null) return;
                        if (!u.hasTrait(SuperMechTraits.ClassMech)) return;
                        if (crafted) return;

                        int stage = GetMechStageTier(u);
                        if (stage < r.minStage)
                        {
                            Debug.Log($"[超神机械师] {u.name} 阶段不足（需要tier{r.minStage}，当前tier{stage}）");
                            return;
                        }

                        // 知识解锁检查
                        if (!string.IsNullOrEmpty(r.requiredKnowledge) && !SuperMechKnowledge.IsUnlocked(u, r.requiredKnowledge))
                        {
                            Debug.Log($"[超神机械师] {u.name} 未学习知识 {r.requiredKnowledge}");
                            return;
                        }

                        // 检查冷却
                        float now = Time.time;
                        float cd;
                        if (_cooldown.TryGetValue(u.id, out cd) && now < cd)
                        {
                            Debug.Log($"[超神机械师] {u.name} 制造冷却中（剩余{cd - now:F1}秒）");
                            return;
                        }

                        // 检查材料（从所在城市仓库扣除）
                        if (!ConsumeMaterials(u, r.cost))
                        {
                            Debug.Log($"[超神机械师] {u.name} 材料不足，无法制造{r.name}");
                            return;
                        }

                        // 计算完美度
                        float intel = 1f;
                        var stats = SuperMechStats.Of(u);
                        if (stats != null)
                        {
                            float iv = stats["intelligence"];
                            intel = 1f + iv * 0.05f;
                        }
                        float perfection = Mathf.Clamp(0.5f + intel * 0.1f + stage * 0.03f, 0.5f, 1.5f);

                        // 清理已死亡召唤物
                        _summonedIds.RemoveWhere(id => World.world.units.get(id) == null || !World.world.units.get(id).isAlive());
                        if (_summonedIds.Count >= SuperMechConfig.MaxSummonedUnits)
                        {
                            Debug.Log($"[超神机械师] 召唤物已达上限({SuperMechConfig.MaxSummonedUnits})");
                            return;
                        }

                        // 制造单位
                        Actor spawned = World.world.units.createNewUnit(
                            r.creatureId, tile, pMiracleSpawn: false, pAdultAge: true);
                        if (spawned != null)
                        {
                            _summonedIds.Add(spawned.id);
                            // 建立主人关系
                            _masterMap[spawned.id] = u.id;
                            if (!_minions.ContainsKey(u.id)) _minions[u.id] = new List<long>();
                            _minions[u.id].Add(spawned.id);

                            foreach (var tid in r.traits)
                            {
                                if (AssetManager.traits.get(tid) != null) spawned.addTrait(tid);
                            }

                            // 制造者获得气力和经验
                            float qiGain = r.qiBase * perfection;
                            float expGain = r.expBase * perfection * 100f;
                            SuperMechQi.AddQi(u, qiGain);
                            if (SuperMechAwakened.IsAwakened(u))
                            {
                                SuperMechAwakened.AddXp(u, expGain);
                            }
                            SuperMechAdvancementTask.OnCraft(u);

                            // 降临者打造高级装备获得神性蜕变点数
                            if (SuperMechAwakened.IsAwakened(u) && stage >= 4 && perfection >= 1.0f)
                            {
                                if (SuperMechDivinity.AwardCraftingPoints(u))
                                {
                                    Debug.Log($"[超神机械师] {u.name}（降临者）打造{r.name}获得1神性蜕变点数！");
                                }
                            }

                            if (SuperMechConfig.LogVerbose)
                                Debug.Log($"[超神机械师] {u.name} 制造{r.name} 完美度{perfection:F0%} 气力+{qiGain:F1}");

                            float cdTime = Mathf.Max(2f, 5f - intel * 0.2f);
                            _cooldown[u.id] = now + cdTime;
                            crafted = true;
                        }
                    });
                    return crafted;
                };
                AssetManager.powers.add(p);
            }
            Debug.Log($"[超神机械师] 制造系统注册完成：{Recipes.Length} 种制造配方");
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
