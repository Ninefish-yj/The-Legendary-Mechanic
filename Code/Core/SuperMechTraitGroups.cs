using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechTraitGroups
    {
        private struct GroupInfo { public string id, nameKey, color; public GroupInfo(string a, string b, string c) { id=a; nameKey=b; color=c; } }
        private static readonly GroupInfo[] Groups =
        {
            new GroupInfo("sm_ranks",          "sm_traitgroups_1016", "#FFD700"),
            new GroupInfo("sm_classes",        "sm_traitgroups_1040", "#00BFFF"),
            new GroupInfo("sm_specialties",    "sm_traitgroups_1028", "#FF69B4"),
            new GroupInfo("sm_subclass",       "sm_traitgroups_1030", "#708090"),
            new GroupInfo("sm_race",           "sm_traitgroups_1031", "#9932CC"),
            new GroupInfo("sm_cultivation",    "sm_traitgroups_1041", "#00CED1"),
            new GroupInfo("sm_special",        "sm_traitgroups_1042", "#FF1493"),
            new GroupInfo("sm_items",          "sm_traitgroups_1043", "#FFA500"),
            new GroupInfo("sm_rank_specialty", "sm_traitgroups_1029", "#FF4500"),
            new GroupInfo("sm_templates",      "sm_traitgroups_1044", "#DA70D6"),
        };

        public static void Register()
        {
            int registered = 0;
            foreach (var g in Groups)
            {
                try
                {
                    var group = new ActorTraitGroupAsset
                    {
                        id = g.id,
                        name = "trait_group_" + g.id,
                        color = g.color
                    };
                    AssetManager.trait_groups.add(group);
                    LocalizedTextManager.add("trait_group_" + g.id, LocalizedTextManager.getText(g.nameKey), pReplace: true);
                    registered++;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[超神机械师] 特质分组注册失败 {g.id}: {e.Message}");
                }
            }
            Debug.Log($"[超神机械师] 特质分组注册完成: {registered}/{Groups.Length}");
        }
    }
}
