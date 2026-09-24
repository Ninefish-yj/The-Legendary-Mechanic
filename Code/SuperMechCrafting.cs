using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 机械师造兵闭环（原著 ch3/ch50）。
    /// 核心循环：机械师制造机械→获得气力/经验→升级→解锁更强制造→循环。
    /// 原著设定：完成组装获得经验，完美度影响经验量（ch3: 69%→28exp, 73%→32exp）。
    /// 制造速度受智力影响，智力越高完美度越高。
    /// </summary>
    public static class SuperMechCrafting
    {
        // 制造冷却（unit.id -> 下次可制造时间）
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();

        /// <summary>制造单位模板定义。</summary>
        public struct CraftRecipe
        {
            public string id;           // 神权id
            public string name;         // 显示名
            public string creatureId;   // WorldBox生物id
            public int minStage;        // 最低机械阶段tier
            public float qiBase;        // 基础气力奖励
            public float expBase;       // 基础经验奖励
            public string[] traits;     // 制造出的单位附带特质
        }

        // 制造配方表（按阶段解锁）
        public static readonly CraftRecipe[] Recipes =
        {
            new CraftRecipe {
                id = "sm_craft_ranger", name = "制造·游骑兵",
                creatureId = "soldier", minStage = 1, qiBase = 5f, expBase = 10f,
                traits = new[] { "aggressive" }
            },
            new CraftRecipe {
                id = "sm_craft_drone", name = "制造·侦察无人机",
                creatureId = "zebra", minStage = 2, qiBase = 8f, expBase = 15f,
                traits = new[] { "aggressive", "fast" }
            },
            new CraftRecipe {
                id = "sm_craft_mech", name = "制造·战斗机甲",
                creatureId = "titan", minStage = 4, qiBase = 20f, expBase = 40f,
                traits = new[] { "aggressive", "tough" }
            },
            new CraftRecipe {
                id = "sm_craft_fortress", name = "制造·战争堡垒",
                creatureId = "titan", minStage = 6, qiBase = 50f, expBase = 100f,
                traits = new[] { "aggressive", "tough", "strong" }
            },
            new CraftRecipe {
                id = "sm_craft_virtual", name = "制造·虚拟生命体",
                creatureId = "human", minStage = 7, qiBase = 80f, expBase = 150f,
                traits = new[] { "aggressive", "immortal", "genius" }
            },
        };

        /// <summary>注册制造神权。</summary>
        public static void Register()
        {
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
                    bool crafted = false;
                    tile.doUnits(u =>
                    {
                        if (u == null) return;
                        if (!u.hasTrait(SuperMechTraits.ClassMech)) return;
                        if (crafted) return;  // 每次只造一个

                        // 检查阶段门槛
                        int stage = GetMechStageTier(u);
                        if (stage < r.minStage)
                        {
                            Debug.Log($"[超神机械师] {u.Name} 阶段不足（需要tier{r.minStage}，当前tier{stage}）");
                            return;
                        }

                        // 检查冷却
                        float now = Time.time;
                        float cd;
                        if (_cooldown.TryGetValue(u.id, out cd) && now < cd)
                        {
                            Debug.Log($"[超神机械师] {u.Name} 制造冷却中（剩余{cd - now:F1}秒）");
                            return;
                        }

                        // 计算完美度（原著：智力影响制造速度和品质）
                        float intel = 1f;
                        var stats = SuperMechStats.Of(u);
                        if (stats != null)
                        {
                            float? iv = stats["intelligence"];
                            if (iv.HasValue) intel = 1f + iv.Value * 0.05f;
                        }
                        float perfection = Mathf.Clamp(0.5f + intel * 0.1f + stage * 0.03f, 0.5f, 1.5f);

                        // 制造单位（在点击位置，即机械师所在格）
                        Actor spawned = World.world.units.createNewUnit(
                            r.creatureId, tile, pMiracleSpawn: false, pAdultAge: true);
                        if (spawned != null)
                        {
                            foreach (var tid in r.traits)
                            {
                                if (AssetManager.traits.get(tid) != null) spawned.addTrait(tid);
                            }
                            // 给制造者气力和经验（原著：制造获得经验）
                            float qiGain = r.qiBase * perfection;
                            float expGain = r.expBase * perfection;
                            SuperMechQi.AddQi(u, qiGain);
                            // 经验通过加气力间接转化（气力升级给潜能点）
                            if (SuperMechConfig.LogVerbose)
                                Debug.Log($"[超神机械师] {u.Name} 制造{r.name} 完美度{perfection:F0%} 气力+{qiGain:F1}");

                            // 设置冷却（基础5秒，智力越高冷却越短）
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

        /// <summary>获取机械师阶段tier（1-14）。</summary>
        private static int GetMechStageTier(Actor a)
        {
            return SuperMechStage.GetStage(a);
        }
    }
}
