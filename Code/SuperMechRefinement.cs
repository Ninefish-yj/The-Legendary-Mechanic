using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 提炼法系统（原著）：
    /// 1. 气力提炼法（ch50/ch172/ch221）：超能者自行领悟的基础气力修行法，全系通用，进阶自动觉醒。
    ///    总效果气力+10，锻炼次数0/80，每次消耗经验和体力。ch172明确"就连异能系也屁颠颠来学"。
    /// 2. 电磁因子提炼法（ch237/ch277）：机械师专属成长型技能，限制100次提炼，效果取决于智力属性。
    /// </summary>
    public static class SuperMechRefinement
    {
        public const string RefinementTrait = "sm_refinement";
        public const string EmRefinementTrait = "sm_em_refinement";

        public static void Register()
        {
            // 注册气力提炼法特质（全系通用）
            LocalizedTextManager.add("trait_" + RefinementTrait, "气力提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + RefinementTrait + "_info",
                "ch50原著。超能者自行领悟的基础气力修行法，全系通用，进阶自动觉醒。气力额外+50%增长。", pReplace: true);
            var t = new ActorTrait
            {
                id = RefinementTrait, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);

            // 注册电磁因子提炼法特质（ch237/ch277原著：机械师专属，效果取决于智力）
            LocalizedTextManager.add("trait_" + EmRefinementTrait, "电磁因子提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + EmRefinementTrait + "_info",
                "ch237/ch277原著。机械师专属成长型技能，限制100次提炼，效果取决于智力属性。气力+30%，智力越高效果越强。", pReplace: true);
            var t2 = new ActorTrait
            {
                id = EmRefinementTrait, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t2);

            // 注册传授气力提炼法神权
            var givePower = new GodPower
            {
                id = "sm_give_refinement",
                name = "传授气力提炼法",
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
            LocalizedTextManager.add("power_sm_give_refinement", "传授气力提炼法", pReplace: true);

            // 注册传授电磁因子提炼法神权（机械系专属）
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
                tile.doUnits(delegate (Actor a)
                {
                    // ch277：机械师专属
                    if (a.hasTrait(SuperMechTraits.ClassMech))
                        a.addTrait(EmRefinementTrait);
                });
                return true;
            };
            AssetManager.powers.add(giveEmPower);
            LocalizedTextManager.add("power_sm_give_em_refinement", "传授电磁因子提炼法（机械专属）", pReplace: true);

            Debug.Log("[超神机械师] 提炼法系统注册完成（气力提炼法全系通用+电磁因子提炼法机械专属）");
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

                // 气力提炼法：ch172全系通用，气力+50%
                if (hasRefine)
                {
                    SuperMechQi.AddQi(a, 1.5f);
                }

                // 电磁因子提炼法：ch237机械专属，效果取决于智力属性
                if (hasEmRefine && a.hasTrait(SuperMechTraits.ClassMech))
                {
                    float intel = 5f;
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        float? iv = stats["intelligence"];
                        if (iv.HasValue) intel = iv.Value;
                    }
                    float amount = 0.9f * (1f + intel * 0.02f); // 智力越高效果越强
                    SuperMechQi.AddQi(a, amount);
                }
            }
        }
    }
}
