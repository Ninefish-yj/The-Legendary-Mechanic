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
        public const string KnowledgeFusion = "sm_knowledge_fusion";
        public const string CraftEquip = "sm_craft_equip";

        public static void Register()
        {
            // 注册神权名称本地化（GodPower.name会被当作本地化key查找）
            string[] powerNames = { "召唤机械游骑兵", "召唤机甲单位", "召唤降临者", "冲击超神级", "异化之灾（天灾）", "异化之灾_天灾", "查看潜能点", "解锁知识·武装系", "解锁知识·能量系", "解锁知识·虚拟系" };
            foreach (var n in powerNames) LocalizedTextManager.add(n, n, pReplace: true);

            AddSpawnPower(SummonRanger, "召唤机械游骑兵", "iconSprite", "soldier", 1);
            AddSpawnPower(SummonMech, "召唤机甲单位", "iconSprite", "titan", 3);
            AddAwakenedPower(SummonAwakened, "召唤降临者", "iconSprite");
            AddTranscendPower(AttemptTranscend, "神之催化", "iconDivineLight");

            AddDisaster(DisasterAlien, "异化之灾（天灾）");

            // 以下功能已移到知识Tab「◆ 操作」区域，不再注册神权：
            // - 查看潜能点（知识Tab顶部显示）
            // - 解锁知识×3（知识Tab点击节点解锁）
            // - 械力融合（知识Tab操作按钮）
            // - 知识融合（知识Tab操作按钮）
            // - 制造装备（知识Tab操作按钮）
            // - 造兵配方×9（知识Tab操作按钮，机械系专属）

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
                    // 降临者：激发潜能+选定机械系方向（玩家有明确职业）
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

        /// <summary>神之催化：神消耗神力为SS阶以上单位施加催化效果，降低突破门槛、提升成功率（每层+10%，最多5层）。</summary>
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
    }
}
