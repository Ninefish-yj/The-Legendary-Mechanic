using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 提炼法系统（原著）：
    /// 韩萧从海蓝星遗迹获得的功法，通过分解生物组织提取能量修炼气力。
    /// 有提炼法的单位，气力额外增长更快（模拟"不断从外界提取能量"）。
    /// </summary>
    public static class SuperMechRefinement
    {
        public const string RefinementTrait = "sm_refinement";

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

            Debug.Log("[超神机械师] 提炼法系统注册完成");
        }

        /// <summary>Tick：给有提炼法且觉醒武道/机械系的单位气力额外加成。</summary>
        public static void TickRefinement()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.hasTrait(RefinementTrait)) continue;
                // 只有武道/机械系（气力/械力体系）才生效
                if (!a.hasTrait(SuperMechTraits.ClassMartial) && !a.hasTrait(SuperMechTraits.ClassMech))
                    continue;

                // 提炼法：气力额外+50%（用我们自己的气力值，不碰原版stamina）
                SuperMechQi.AddQi(a, 1.5f);
            }
        }
    }
}
