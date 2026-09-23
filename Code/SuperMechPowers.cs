using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 神权（Phase 1）：机械师造兵 + 觉醒赋予 + 天灾降临。
    /// 走原版 GodPower 系统，出现在神权栏。
    /// </summary>
    public static class SuperMechPowers
    {
        public const string SummonRanger = "sm_summon_ranger";
        public const string SummonMech  = "sm_summon_mech";
        public const string AwakenPsi   = "sm_awaken_psi";
        public const string AwakenMech  = "sm_awaken_mech";
        public const string AwakenMartial = "sm_awaken_martial";
        public const string DisasterAlien = "sm_disaster_alien";

        public static void Register()
        {
            AddSpawnPower(SummonRanger, "召唤机械游骑兵", "ui/powers/power_summon_units", "soldier",
                new[] { SuperMechTraits.MechInitiate, SuperMechTraits.RankD });
            AddSpawnPower(SummonMech, "召唤机甲单位", "ui/powers/power_summon_units", "titan",
                new[] { SuperMechTraits.MechTrainee, SuperMechTraits.RankC, SuperMechTraits.BranchWeapon });

            AddTraitPower(AwakenPsi, "赋予异能系觉醒", SuperMechTraits.ClassPsi);
            AddTraitPower(AwakenMech, "赋予机械系觉醒", SuperMechTraits.ClassMech);
            AddTraitPower(AwakenMartial, "赋予武道系觉醒", SuperMechTraits.ClassMartial);

            AddDisaster(DisasterAlien, "异化之灾（天灾）");

            Debug.Log("[超神机械师] 神权注册完成：2 召唤 + 3 觉醒 + 1 天灾");
        }

        private static void AddSpawnPower(string id, string name, string icon, string creatureId, string[] traitIds)
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
                    foreach (var tid in traitIds) a.addTrait(tid);
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddTraitPower(string id, string name, string traitId)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = "ui/powers/power_bless",
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
                bool applied = false;
                tile.doUnits(u => {
                    if (u != null) {
                        u.addTrait(traitId);
                        SuperMechSpecialty.AssignRandomSpecialty(u);
                        applied = true;
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
                path_icon = "ui/powers/power_meteor",
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
                // Phase 1：在落点生成一个 B 阶位异化体（占位）
                Actor a = World.world.units.createNewUnit("beast", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    a.addTrait(SuperMechTraits.RankB);
                    a.addTrait("aggressive");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }
    }
}
