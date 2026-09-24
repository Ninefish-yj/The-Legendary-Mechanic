using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 神权：只有地图交互的才注册神权。
    /// 单位管理（觉醒/分支/知识等）由超能者面板窗口处理。
    /// </summary>
    public static class SuperMechPowers
    {
        public const string SummonRanger = "sm_summon_ranger";
        public const string SummonMech  = "sm_summon_mech";
        public const string DisasterAlien = "sm_disaster_alien";
        public const string CheckPotential = "sm_check_potential";
        public const string UnlockArmed = "sm_unlock_armed";
        public const string UnlockEnergy = "sm_unlock_energy";
        public const string UnlockVirtual = "sm_unlock_virtual";

        public static void Register()
        {
            AddSpawnPower(SummonRanger, "召唤机械游骑兵", "ui/powers/power_summon_units", "soldier",
                new[] { SuperMechTraits.MechInitiate, SuperMechTraits.RankD });
            AddSpawnPower(SummonMech, "召唤机甲单位", "ui/powers/power_summon_units", "titan",
                new[] { SuperMechTraits.MechTrainee, SuperMechTraits.RankC, SuperMechTraits.BranchWeapon });

            AddDisaster(DisasterAlien, "异化之灾（天灾）");

            // 潜能点与知识树（面板窗口调用）
            AddCheckPotentialPower(CheckPotential, "查看潜能点");
            AddUnlockKnowledgePower(UnlockArmed, "解锁知识·武装系", "armed", 2);
            AddUnlockKnowledgePower(UnlockEnergy, "解锁知识·能量系", "energy", 2);
            AddUnlockKnowledgePower(UnlockVirtual, "解锁知识·虚拟系", "virtual", 3);

            Debug.Log("[超神机械师] 神权注册完成：2召唤 + 1天灾 + 1查看 + 3知识解锁");
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

        /// <summary>查看单位潜能点/觉醒点（点击单位）。</summary>
        private static void AddCheckPotentialPower(string id, string name)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                bool found = false;
                tile.doUnits(u => {
                    if (u != null && SuperMechAdvancement.IsSuperMechUnit(u))
                    {
                        int pot = SuperMechPotential.GetPotential(u);
                        int awk = SuperMechPotential.GetAwakening(u);
                        int unlocked = SuperMechPotential.GetUnlockedCount(u);
                        float qi = SuperMechQi.GetQi(u);
                        int ql = SuperMechQi.GetLevel(qi);
                        Debug.Log($"[超神机械师] {u.Name}：气力{qi:F0}(Lv{ql}) 潜能点{pot} 觉醒点{awk} 已解锁知识{unlocked}个");
                        found = true;
                    }
                });
                return found;
            };
            AssetManager.powers.add(p);
        }

        /// <summary>解锁知识树节点（消耗潜能点，点击单位）。</summary>
        private static void AddUnlockKnowledgePower(string id, string name, string branch, int baseCost)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                bool applied = false;
                tile.doUnits(u => {
                    if (u == null || !SuperMechAdvancement.IsSuperMechUnit(u)) return;
                    string nodeId = $"{branch}_{SuperMechPotential.GetUnlockedCount(u) + 1}";
                    // 转职后其他分支费用×3（ch611）
                    int cost = baseCost;
                    bool isOwnBranch = (branch == "virtual" && u.hasTrait(SuperMechTraits.MechVirtual))
                                    || (branch == "armed" && u.hasTrait(SuperMechTraits.RouteGunner))
                                    || (branch == "energy" && u.hasTrait(SuperMechTraits.RouteMech));
                    if (!isOwnBranch && u.hasTrait(SuperMechTraits.MechMagnet)) cost *= 3;

                    if (SuperMechPotential.UnlockNode(u, nodeId, cost))
                    {
                        var stats = SuperMechStats.Of(u);
                        if (stats != null)
                        {
                            stats["intelligence"] = (stats["intelligence"] ?? 0f) + 1f;
                            stats["experience"] = (stats["experience"] ?? 1f) + 0.02f;
                        }
                        applied = true;
                    }
                    else
                    {
                        Debug.Log($"[超神机械师] {u.Name} 潜能点不足（需{cost}，有{SuperMechPotential.GetPotential(u)}）");
                    }
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }
    }
}
