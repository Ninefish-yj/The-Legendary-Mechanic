using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 提炼法系统（原著）：
    /// 1. 提炼法（ch?）：韩萧从海蓝星遗迹获得，分解生物组织提取能量修炼气力。
    /// 2. 电磁因子提炼法（ch54）：从环境中提取电磁因子，适合机械/异能系，气力增长更稳但量少。
    /// 有提炼法的单位，气力额外增长更快。
    /// </summary>
    public static class SuperMechRefinement
    {
        public const string RefinementTrait = "sm_refinement";
        public const string EmRefinementTrait = "sm_em_refinement";

        public static void Register()
        {
            // 注册提炼法特质
            LocalizedTextManager.add("trait_" + RefinementTrait, "提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + RefinementTrait + "_info",
                "海蓝星遗迹传承。分解生物组织提取能量，气力额外+50%增长。", pReplace: true);
            var t = new ActorTrait
            {
                id = RefinementTrait, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);

            // 注册电磁因子提炼法特质（ch54原著）
            LocalizedTextManager.add("trait_" + EmRefinementTrait, "电磁因子提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + EmRefinementTrait + "_info",
                "ch54原著。从环境中提取电磁因子修炼，适合机械/异能系，气力增长稳定+30%，战斗中额外恢复。", pReplace: true);
            var t2 = new ActorTrait
            {
                id = EmRefinementTrait, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t2);

            // 注册传授提炼法神权
            var givePower = new GodPower
            {
                id = "sm_give_refinement",
                name = "传授提炼法",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            givePower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { a.addTrait(RefinementTrait); });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_refinement", "传授提炼法", pReplace: true);

            // 注册传授电磁因子提炼法神权
            var giveEmPower = new GodPower
            {
                id = "sm_give_em_refinement",
                name = "传授电磁因子提炼法",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            giveEmPower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { a.addTrait(EmRefinementTrait); });
                return true;
            };
            AssetManager.powers.add(giveEmPower);
            LocalizedTextManager.add("power_sm_give_em_refinement", "传授电磁因子提炼法", pReplace: true);

            Debug.Log("[超神机械师] 提炼法系统注册完成（含电磁因子提炼法）");
        }

        /// <summary>Tick：给有提炼法的单位气力额外加成。</summary>
        public static void TickRefinement()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                bool hasRefine = a.hasTrait(RefinementTrait);
                bool hasEmRefine = a.hasTrait(EmRefinementTrait);
                if (!hasRefine && !hasEmRefine) continue;

                // 提炼法：只有武道/机械系生效，气力+50%
                if (hasRefine && (a.hasTrait(SuperMechTraits.ClassMartial) || a.hasTrait(SuperMechTraits.ClassMech)))
                {
                    SuperMechQi.AddQi(a, 1.5f);
                }

                // 电磁因子提炼法：机械/异能系生效，气力+30%，战斗中额外恢复
                if (hasEmRefine && (a.hasTrait(SuperMechTraits.ClassMech) || a.hasTrait(SuperMechTraits.ClassPsi)))
                {
                    float amount = 0.9f;
                    if (SuperMechQi.IsInCombat(a)) amount *= 1.5f; // 战斗中从敌方电磁场提取更多
                    SuperMechQi.AddQi(a, amount);
                }
            }
        }
    }
}
