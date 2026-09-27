using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechPowers
    {
        public const string SummonRanger = "sm_summon_ranger";
        public const string SummonMech  = "sm_summon_mech";
        public const string SummonAwakened = "sm_summon_awakened";
        public const string AttemptTranscend = "sm_attempt_transcend";
        public const string DisasterAlien = "sm_disaster_alien";
        public const string CheckPotential = "sm_check_potential";
        public const string UnlockArmed = "sm_unlock_armed";
        public const string UnlockEnergy = "sm_unlock_energy";
        public const string UnlockVirtual = "sm_unlock_virtual";
        public const string MechFusion = "sm_mech_fusion";
        public const string KnowledgeFusion = "sm_knowledge_fusion";
        public const string CraftEquip = "sm_craft_equip";

        public static void Register()
        {
            string[] powerNames = { "sm_powers_920", "sm_powers_921", "sm_powers_922", "sm_powers_923", "sm_powers_924", "sm_powers_925", "sm_powers_926", "sm_powers_927", "sm_powers_928", "sm_powers_929" };
            foreach (var n in powerNames) LocalizedTextManager.add(n, n, pReplace: true);

            AddSpawnPower(SummonRanger, "sm_powers_920", "actor_traits/iconStrong", "soldier", 1);
            AddSpawnPower(SummonMech, "sm_powers_921", "actor_traits/iconGiant", "titan", 3);
            AddAwakenedPower(SummonAwakened, "sm_powers_922", "actor_traits/iconChosenOne");
            AddTranscendPower(AttemptTranscend, "sm_powers_930", "iconDivineLight");

            AddDisaster(DisasterAlien, "sm_powers_924");


            Debug.Log("[超神机械师] 神权注册完成：3召唤 + 1天灾 + 1催化 = 5个核心神权");
        }

        private static void AddSpawnPower(string id, string name, string icon, string creatureId, int mechStage)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = icon,
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit(creatureId, tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    a.addTrait(SuperMechTraits.ClassMech);
                    SuperMechStage.SetStage(a, mechStage);
                    SuperMechSpecialty.AssignRandomSpecialty(a);
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddAwakenedPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = icon,
                rank = PowerRank.Rank0_free, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    SuperMechTalent.GrantTalents(a);
                    SuperMechProfession.SetProfession(a, SuperMechProfession.ProfessionType.Mechanical);
                    a.addTrait(SuperMechAwakened.AwakenedTrait);
                    if (!a.hasTrait("sm_rank_00_f"))
                        a.addTrait("sm_rank_00_f");
                    SuperMechAdvancement.SetExactRank(a, 0);
                    SuperMechSpecialty.AssignRandomSpecialty(a);
                    Debug.Log($"[超神机械师] 召唤降临者：{a.name}（机械系Lv1）");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddTranscendPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = icon,
                rank = PowerRank.Rank0_free, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                bool applied = false;
                tile.doUnits(u =>
                {
                    if (u == null || applied) return;
                    if (SuperMechTranscendence.CatalyzeBreakthrough(u))
                    {
                        int layers = SuperMechTranscendence.GetCatalystLayers(u);
                        Debug.Log($"[超神机械师] 神之催化：{u.name} 获得第{layers}层催化（成功率+{layers * 10}%）");
                        applied = true;
                    }
                    else
                    {
                        int rank = SuperMechAdvancement.GetExactRankIndex(u);
                        if (rank < 12)
                            Debug.Log($"[超神机械师] {u.name} 阶位不足（需SS阶以上），无法催化");
                        else
                            Debug.Log($"[超神机械师] {u.name} 催化层数已满（5层）");
                    }
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }

        private static void AddDisaster(string id, string name)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = "iconDiscord",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit("beast", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    a.addTrait("sm_rank_06_b");
                    a.addTrait("aggressive");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }
    }
}
