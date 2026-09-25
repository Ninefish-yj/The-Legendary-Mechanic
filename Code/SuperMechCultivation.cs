using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 三系提炼法变种（原著：所有修炼气力的方法都叫提炼法）：
    /// 武道/机械 = 气力提炼法 + 电磁因子提炼法（在SuperMechRefinement）
    /// 异能系   = 基因提炼法（战斗中基因链共鸣涨能级）
    /// 魔法系   = 魔力提炼法（冥想涨智力/魔力池）
    /// 念力系   = 精神提炼法（心灵锻炼涨智力/精神力）
    /// </summary>
    public static class SuperMechCultivation
    {
        // 三系提炼法变种特质
        public const string PsiResonance  = "sm_pcult_resonance";   // 基因提炼法（异能）
        public const string ManaMeditation = "sm_pcult_meditation"; // 魔力提炼法（魔法）
        public const string MindTrain    = "sm_pcult_mind_train";   // 精神提炼法（念力）

        public static void Register()
        {
            AddCultivation(PsiResonance,  "基因提炼法（异能系）", 3,
                "战斗中基因链共鸣，异能系单位智力额外增长。",
                SuperMechTraits.ClassPsi);
            AddCultivation(ManaMeditation, "魔力提炼法（魔法系）",   4,
                "持续冥想提炼魔力，魔法系单位智力与魔力池额外增长。",
                SuperMechTraits.ClassMage);
            AddCultivation(MindTrain,    "精神提炼法（念力系）", 3,
                "精神力日常锻炼提炼，念力系单位智力与精神力额外增长。",
                SuperMechTraits.ClassMind);

            Debug.Log("[超神机械师] 三系提炼法变种注册完成");
        }

        private static void AddCultivation(string id, string name, int intellGain, string desc, string classTraitId)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);
            // 神权统一在 SuperMechRefinement 注册（传授提炼法按系别自动分配）
        }

        public static void TickCultivation()
        {
            // 先把 stats 取到局部变量再用索引器（与晋升系统一致，避免直接链式访问触发字段访问限制）
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                var s = SuperMechStats.Of(a);

                if (a.hasTrait(PsiResonance) && a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
                if (a.hasTrait(ManaMeditation) && a.hasTrait(SuperMechTraits.ClassMage))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.6f;
                    float mana = s["mana"];
                    if (mana < 1000) s["mana"] = mana + 3f;
                }
                if (a.hasTrait(MindTrain) && a.hasTrait(SuperMechTraits.ClassMind))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
            }
        }
    }
}
