using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力分系用途（原文 ch49）：
    /// 气力是统一核心属性，不同系用途不同：
    ///   武道系 → 格斗能量（已在 SuperMechQi 里加 warfare/damage）
    ///   机械系 → 制造能量（磁环后叫械力）
    ///   异能系 → 基因链能级（异能能量槽）
    ///   魔法系 → 魔力源泉（加 mana）
    ///   念力系 → 精神力反应炉（加 intelligence）
    /// 这里按气力值（SuperMechQi.GetQi）自动挂对应用途的阶段特质。
    /// </summary>
    public static class SuperMechCorePower
    {
        // 异能系：基因链能级（5阶，按气力值分阶）
        public static readonly string[] GeneChainNames = {
            "一阶基因链", "二阶基因链", "三阶基因链", "四阶基因链", "五阶基因链"
        };
        public static readonly float[] GeneChainThresholds = { 10f, 50f, 200f, 800f, 3000f };

        // 魔法系：魔力池（5层，按气力值分层）
        public static readonly string[] ManaTierNames = {
            "魔力初涌", "魔力流动", "魔力充盈", "魔力磅礴", "魔力浩瀚"
        };
        public static readonly float[] ManaTierThresholds = { 10f, 50f, 200f, 800f, 3000f };

        // 念力系：精神力（5阶，按气力值分阶）
        public static readonly string[] MindTierNames = {
            "精神觉醒", "精神外放", "精神干涉", "精神领域", "精神造物"
        };
        public static readonly float[] MindTierThresholds = { 10f, 50f, 200f, 800f, 3000f };

        public static string GeneChainId(int lv) => $"sm_gene_{lv}";
        public static string ManaTierId(int lv) => $"sm_mana_{lv}";
        public static string MindTierId(int lv) => $"sm_mind_{lv}";

        public static void Register()
        {
            // 异能系：基因链（气力用途=异能能量槽）
            for (int i = 0; i < GeneChainNames.Length; i++)
            {
                int lv = i + 1;
                string id = GeneChainId(lv);
                LocalizedTextManager.add("trait_" + id, GeneChainNames[i], pReplace: true);
                LocalizedTextManager.add("trait_" + id + "_info",
                    $"异能系基因链第{lv}阶（气力用途=异能能量槽）。", pReplace: true);
                var t = new ActorTrait
                {
                    id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_gene",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = 1f + lv * 0.25f;
                t.base_stats["intelligence"] = lv * 3f;
                AssetManager.traits.add(t);
            }

            // 魔法系：魔力池（气力用途=魔力源泉）
            for (int i = 0; i < ManaTierNames.Length; i++)
            {
                int lv = i + 1;
                string id = ManaTierId(lv);
                LocalizedTextManager.add("trait_" + id, ManaTierNames[i], pReplace: true);
                LocalizedTextManager.add("trait_" + id + "_info",
                    $"魔法系魔力池第{lv}层（气力用途=魔力源泉）。", pReplace: true);
                var t = new ActorTrait
                {
                    id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_mana",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["mana"] = lv * 50f;
                t.base_stats["intelligence"] = lv * 4f;
                AssetManager.traits.add(t);
            }

            // 念力系：精神力（气力用途=精神力反应炉）
            for (int i = 0; i < MindTierNames.Length; i++)
            {
                int lv = i + 1;
                string id = MindTierId(lv);
                LocalizedTextManager.add("trait_" + id, MindTierNames[i], pReplace: true);
                LocalizedTextManager.add("trait_" + id + "_info",
                    $"念力系精神力第{lv}阶（气力用途=精神力）。", pReplace: true);
                var t = new ActorTrait
                {
                    id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_mind",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["intelligence"] = lv * 5f;
                t.base_stats["multiplier_damage"] = 1f + lv * 0.15f;
                AssetManager.traits.add(t);
            }

            Debug.Log("[超神机械师] 气力分系用途注册完成：基因链5+魔力5+精神力5");
        }

        /// <summary>按气力值（SuperMechQi）自动更新各系用途等级。</summary>
        public static void TickCorePowers()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 统一气力值（所有系共用）
                float qi = SuperMechQi.GetQi(a);

                // 异能系：气力 → 基因链能级
                if (a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    int lv = CalcTier(qi, GeneChainThresholds);
                    ApplyTier(a, GeneChainId, GeneChainThresholds.Length, lv);
                }

                // 魔法系：气力 → 魔力池
                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    int lv = CalcTier(qi, ManaTierThresholds);
                    ApplyTier(a, ManaTierId, ManaTierThresholds.Length, lv);
                }

                // 念力系：气力 → 精神力
                if (a.hasTrait(SuperMechTraits.ClassMind))
                {
                    int lv = CalcTier(qi, MindTierThresholds);
                    ApplyTier(a, MindTierId, MindTierThresholds.Length, lv);
                }
            }
        }

        private static int CalcTier(float value, float[] thresholds)
        {
            int lv = 1;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (value >= thresholds[i]) lv = i + 1;
            }
            return lv;
        }

        private static void ApplyTier(Actor a, System.Func<int, string> idFunc, int maxLv, int targetLv)
        {
            string targetId = idFunc(targetLv);
            if (a.hasTrait(targetId))
            {
                for (int lv = 1; lv <= maxLv; lv++)
                {
                    if (lv != targetLv && a.hasTrait(idFunc(lv))) a.removeTrait(idFunc(lv));
                }
                return;
            }
            for (int lv = 1; lv <= maxLv; lv++)
            {
                string id = idFunc(lv);
                if (lv == targetLv) { if (!a.hasTrait(id)) a.addTrait(id); }
                else if (a.hasTrait(id)) a.removeTrait(id);
            }
        }
    }
}
