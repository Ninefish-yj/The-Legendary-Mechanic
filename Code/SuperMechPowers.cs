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
        public const string SummonAwakened = "sm_summon_awakened";
        public const string AttemptTranscend = "sm_attempt_transcend";
        public const string DisasterAlien = "sm_disaster_alien";
        public const string CheckPotential = "sm_check_potential";
        public const string UnlockArmed = "sm_unlock_armed";
        public const string UnlockEnergy = "sm_unlock_energy";
        public const string UnlockVirtual = "sm_unlock_virtual";
        public const string MechFusion = "sm_mech_fusion";

        public static void Register()
        {
            // 注册神权名称本地化（GodPower.name会被当作本地化key查找）
            string[] powerNames = { "召唤机械游骑兵", "召唤机甲单位", "召唤降临者", "冲击超神级", "异化之灾（天灾）", "异化之灾_天灾", "查看潜能点", "解锁知识·武装系", "解锁知识·能量系", "解锁知识·虚拟系" };
            foreach (var n in powerNames) LocalizedTextManager.add(n, n, pReplace: true);

            AddSpawnPower(SummonRanger, "召唤机械游骑兵", "ui/powers/power_summon_units", "soldier", 1);
            AddSpawnPower(SummonMech, "召唤机甲单位", "ui/powers/power_summon_units", "titan", 3);
            AddAwakenedPower(SummonAwakened, "召唤降临者", "ui/powers/power_summon_units");
            AddTranscendPower(AttemptTranscend, "冲击超神级", "ui/powers/power_bless");

            AddDisaster(DisasterAlien, "异化之灾（天灾）");

            // 潜能点与知识树（面板窗口调用）
            AddCheckPotentialPower(CheckPotential, "查看潜能点");
            AddUnlockKnowledgePower(UnlockArmed, "解锁知识·武装系", "armed", 2);
            AddUnlockKnowledgePower(UnlockEnergy, "解锁知识·能量系", "energy", 2);
            AddUnlockKnowledgePower(UnlockVirtual, "解锁知识·虚拟系", "virtual", 3);

            // 械力融合（机械系专属，装备与身体融合）
            AddMechFusionPower(MechFusion, "械力融合");

            Debug.Log("[超神机械师] 神权注册完成：2召唤 + 1天灾 + 1查看 + 3知识解锁 + 1械力融合");
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

        /// <summary>召唤降临者（有面板的玩家型单位，走等级职业体系）。</summary>
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
                    a.addTrait(SuperMechTraits.ClassMech); // 默认机械系
                    a.addTrait(SuperMechAwakened.AwakenedTrait);
                    SuperMechStage.SetStage(a, 1); // 入门者
                    SuperMechSpecialty.AssignRandomSpecialty(a);
                    Debug.Log($"[超神机械师] 召唤降临者：{a.name}（机械系Lv1）");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        /// <summary>冲击超神级（ch1396：需要超神遗力+神性蜕变+助手，有恶性变异风险）。</summary>
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
                    if (SuperMechTranscendence.CanAttempt(u))
                    {
                        bool success = SuperMechTranscendence.AttemptTranscend(u);
                        Debug.Log($"[超神机械师] {u.name} 冲击超神级{(success ? "成功！" : "失败，恶性变异")}");
                        applied = true;
                    }
                    else
                    {
                        Debug.Log($"[超神机械师] {u.name} 无法突破：{SuperMechTranscendence.GetStatusText(u)}");
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
                    a.addTrait("sm_rank_06_b");
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
                        Debug.Log($"[超神机械师] {u.name}：气力{qi:F0}(Lv{ql}) 潜能点{pot} 觉醒点{awk} 已解锁知识{unlocked}个");
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
                    bool isOwnBranch = (branch == "virtual" && u.hasTrait(SuperMechBranch.BranchMartial))
                                    || (branch == "armed" && u.hasTrait(SuperMechBranch.BranchGunner))
                                    || (branch == "energy" && u.hasTrait(SuperMechBranch.BranchMech));
                    if (!isOwnBranch && SuperMechStage.GetStage(u) >= 4) cost *= 3;

                    if (SuperMechPotential.UnlockNode(u, nodeId, cost))
                    {
                        var stats = SuperMechStats.Of(u);
                        if (stats != null)
                        {
                            stats["intelligence"] = (stats["intelligence"]) + 1f;
                            stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.02f;
                        }
                        applied = true;
                    }
                    else
                    {
                        Debug.Log($"[超神机械师] {u.name} 潜能点不足（需{cost}，有{SuperMechPotential.GetPotential(u)}）");
                    }
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }

        /// <summary>械力融合：机械系专属，将装备与身体融合（原著：械武者分支机械与身体融合）。</summary>
        private static void AddMechFusionPower(string id, string name)
        {
            LocalizedTextManager.add(name, name, pReplace: true);
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
                tile.doUnits(u =>
                {
                    if (u == null || applied) return;
                    if (SuperMechMechFusion.IsFused(u))
                    {
                        SuperMechMechFusion.Unfuse(u);
                        Debug.Log($"[超神机械师] {u.name} 解除械力融合");
                        applied = true;
                    }
                    else if (SuperMechMechFusion.TryFuse(u))
                    {
                        Debug.Log($"[超神机械师] {u.name} 完成械力融合！");
                        applied = true;
                    }
                    else
                    {
                        Debug.Log($"[超神机械师] {u.name} 无法融合：需机械系+磁环阶段+蓝色以上装备");
                    }
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }
    }
}
