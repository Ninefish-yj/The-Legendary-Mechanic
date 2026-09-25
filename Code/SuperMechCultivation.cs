using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系修炼功法（对应提炼法）：
    /// 武道/机械 = 提炼法（涨气力/stamina）
    /// 异能系   = 基因共鸣（战斗中涨基因链能级）
    /// 魔法系   = 冥想（自动涨智力/魔力池）
    /// 念力系   = 心灵锻炼（自动涨智力/精神力）
    /// </summary>
    public static class SuperMechCultivation
    {
        // 三系功法特质
        public const string PsiResonance  = "sm_pcult_resonance";   // 基因共鸣（异能）
        public const string ManaMeditation = "sm_pcult_meditation"; // 冥想（魔法）
        public const string MindTrain    = "sm_pcult_mind_train";   // 心灵锻炼（念力）

        public static void Register()
        {
            AddCultivation(PsiResonance,  "基因共鸣（异能）", 3,
                "战斗中基因链共鸣，异能系单位智力额外增长。",
                SuperMechTraits.ClassPsi);
            AddCultivation(ManaMeditation, "冥想（魔法）",   4,
                "持续冥想，魔法系单位智力与魔力池额外增长。",
                SuperMechTraits.ClassMage);
            AddCultivation(MindTrain,    "心灵锻炼（念力）", 3,
                "精神力日常锻炼，念力系单位智力与精神力额外增长。",
                SuperMechTraits.ClassMind);

            Debug.Log("[超神机械师] 三系修炼功法注册完成");
        }

        private static void AddCultivation(string id, string name, int intellGain, string desc, string classTraitId)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconBoostedVitality", group_id = "sm_cultivation",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);
            // 神权统一在 SuperMechRefinement 注册（传授修炼功法按系别自动分配）
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
