using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechTraitGroups
    {
        private struct GroupInfo { public string id, name, color; public GroupInfo(string a, string b, string c) { id=a; name=b; color=c; } }
        private static readonly GroupInfo[] Groups =
        {
            new GroupInfo("sm_ranks",        "sm_traitgroups_1016", "#FFD700"),
            new GroupInfo("sm_classes",      "sm_traitgroups_1017", "#00BFFF"),
            new GroupInfo("sm_branches",     "sm_traitgroups_1018", "#9370DB"),
            new GroupInfo("sm_perks",        "sm_traitgroups_1019", "#FFD700"),
                        new GroupInfo("sm_relic",        "sm_traitgroups_1024", "#FFD700"),
            new GroupInfo("sm_cosmic_relic", "sm_traitgroups_1025", "#FFA500"),
            new GroupInfo("sm_mage_tower",   "sm_traitgroups_1026", "#4169E1"),
            new GroupInfo("sm_awakened",     "sm_traitgroups_1027", "#FFD700"),
            new GroupInfo("sm_specialty",    "sm_traitgroups_1028", "#FF69B4"),
            new GroupInfo("sm_rank_specialty","sm_traitgroups_1029","#FF4500"),
            new GroupInfo("sm_subclass",     "sm_traitgroups_1030", "#708090"),
            new GroupInfo("sm_race",         "sm_traitgroups_1031", "#9932CC"),
            new GroupInfo("sm_race_talents", "sm_traitgroups_1031", "#9932CC"),
            new GroupInfo("sm_skills",       "sm_traitgroups_1032", "#FF4500"),
            new GroupInfo("sm_sanctuary",    "sm_traitgroups_1033", "#FFD700"),
            new GroupInfo("sm_mage_type",    "sm_traitgroups_1034", "#9370DB"),
            new GroupInfo("sm_mage_spec",    "sm_traitgroups_1035", "#4169E1"),
            new GroupInfo("sm_specialization","sm_traitgroups_1036","#228B22"),
            new GroupInfo("sm_special",      "sm_traitgroups_1037", "#FF1493"),
        };

        public static void Register()
        {
            foreach (var g in Groups)
            {
                if (AssetManager.trait_groups.get(g.id) != null) continue;

                var group = new ActorTraitGroupAsset
                {
                    id = g.id,
                    name = "trait_group_" + g.id,
                    color = g.color
                };
                AssetManager.trait_groups.add(group);

                LocalizedTextManager.add("trait_group_" + g.id, LocalizedTextManager.getText(g.name), pReplace: true);
            }

        }
    }
}
