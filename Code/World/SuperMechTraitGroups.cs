using NeoModLoader.api;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechTraitGroups
    {
        private static readonly (string id, string name, string color)[] Groups =
        {
            ("sm_ranks",        "sm_traitgroups_1016",       "#FFD700"),
            ("sm_classes",      "sm_traitgroups_1017",   "#00BFFF"),
            ("sm_branches",     "sm_traitgroups_1018",   "#9370DB"),
            ("sm_perks",        "sm_traitgroups_1019",       "#FFD700"),
            ("sm_gene",         "sm_traitgroups_1020", "#32CD32"),
            ("sm_mana",         "sm_traitgroups_1021", "#4169E1"),
            ("sm_mind",         "sm_traitgroups_1022", "#8A2BE2"),
            ("sm_refinement",   "sm_traitgroups_1023",     "#228B22"),
            ("sm_relic",        "sm_traitgroups_1024",   "#FFD700"),
            ("sm_cosmic_relic", "sm_traitgroups_1025",   "#FFA500"),
            ("sm_mage_tower",   "sm_traitgroups_1026",     "#4169E1"),
            ("sm_awakened",     "sm_traitgroups_1027",     "#FFD700"),
            ("sm_specialty",    "sm_traitgroups_1028",   "#FF69B4"),
            ("sm_rank_specialty","sm_traitgroups_1029",   "#FF4500"),
            ("sm_subclass",     "sm_traitgroups_1030",     "#708090"),
            ("sm_race",         "sm_traitgroups_1031",   "#9932CC"),
            ("sm_skills",       "sm_traitgroups_1032",   "#FF4500"),
            ("sm_sanctuary",    "sm_traitgroups_1033",       "#FFD700"),
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

                LocalizedTextManager.add("trait_group_" + id, LocalizedTextManager.getText(name), pReplace: true);
            }

            Debug.Log($"[超神机械师] 特质组注册完成：{Groups.Length} 个自定义组");
        }
    }
}
