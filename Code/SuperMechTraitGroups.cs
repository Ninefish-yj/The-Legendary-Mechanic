using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 注册模组自定义特质组。原版 ActorTraitGroupLibrary 只有15个固定组，
    /// 我们用的 sm_ranks/sm_classes 等自定义组必须先注册，否则特质面板找不到组、
    /// 导致我们的特质不显示、原版特质面板异常全选。
    /// 必须在所有特质注册之前调用。
    /// </summary>
    public static class SuperMechTraitGroups
    {
        private static readonly (string id, string name, string color)[] Groups =
        {
            ("sm_ranks",        "阶位",       "#FFD700"),
            ("sm_classes",      "五系觉醒",   "#00BFFF"),
            ("sm_branches",     "职业分支",   "#9370DB"),
            ("sm_perks",        "专长",       "#FFD700"),
            ("sm_gene",         "异能基因链", "#32CD32"),
            ("sm_mana",         "魔法魔力池", "#4169E1"),
            ("sm_mind",         "念力精神力", "#8A2BE2"),
            ("sm_refinement",   "提炼法",     "#228B22"),
            ("sm_relic",        "装备品质",   "#FFD700"),
            ("sm_cosmic_relic", "宇宙宝物",   "#FFA500"),
            ("sm_mage_tower",   "法师塔",     "#4169E1"),
            ("sm_awakened",     "降临者",     "#FFD700"),
            ("sm_specialty",    "特色专精",   "#FF69B4"),
            ("sm_rank_specialty","阶位专长",   "#FF4500"),
            ("sm_subclass",     "副职业",     "#708090"),
            ("sm_race",         "种族进化",   "#9932CC"),
            ("sm_skills",       "职业技能",   "#FF4500"),
            ("sm_sanctuary",    "圣所",       "#FFD700"),
        };

        public static void Register()
        {
            foreach (var (id, name, color) in Groups)
            {
                if (AssetManager.trait_groups.get(id) != null) continue;

                var group = new ActorTraitGroupAsset
                {
                    id = id,
                    name = "trait_group_" + id,
                    color = color
                };
                AssetManager.trait_groups.add(group);

                // 组名本地化
                LocalizedTextManager.add("trait_group_" + id, name, pReplace: true);
            }

            Debug.Log($"[超神机械师] 特质组注册完成：{Groups.Length} 个自定义组");
        }
    }
}
