using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 机械系三分支选择系统（原著 ch50/ch611）。
    /// 20级（磁环阶段）时选择分支：
    /// - 枪炮师：远程重炮，高级职业【战争堡垒】，舍弃制造
    /// - 机械师：制造路线，80%图纸前置，长期最强
    /// - 械武者：近战机械武器+纳米殖装，可解锁【机甲操控师】
    /// 转职后其他分支尖端知识潜能点费用×3（ch611）。
    /// </summary>
    public static class SuperMechBranch
    {
        public const string BranchGunner = "sm_branch_gunner";     // 枪炮师
        public const string BranchMech = "sm_branch_mech";         // 机械师
        public const string BranchMartial = "sm_branch_martial";   // 械武者

        // 分支选择冷却（防止误点）
        private static readonly Dictionary<long, float> _selectCooldown = new Dictionary<long, float>();

        public static void Register()
        {
            AddBranchPower("sm_select_gunner", "选择分支·枪炮师", BranchGunner,
                "远程重型枪械，高级职业【战争堡垒】，舍弃制造路线，伤害+30%攻速+20%");
            AddBranchPower("sm_select_mech", "选择分支·机械师", BranchMech,
                "制造路线，80%图纸前置，长期最强，制造速度+50%完美度+20%");
            AddBranchPower("sm_select_martial", "选择分支·械武者", BranchMartial,
                "近战机械武器+纳米殖装，可解锁【机甲操控师】，生命+40%护甲+30%");

            Debug.Log("[超神机械师] 三分支选择系统注册完成");
        }

        private static void AddBranchPower(string id, string name, string branchTrait, string desc)
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
                tile.doUnits(u =>
                {
                    if (u == null || applied) return;
                    if (!u.hasTrait(SuperMechTraits.ClassMech)) return;

                    // 必须达到磁环阶段（tier4）才能选分支
                    int stage = GetMechStageTier(u);
                    if (stage < 4)
                    {
                        Debug.Log($"[超神机械师] {u.Name} 阶段不足（需磁环tier4，当前tier{stage}）");
                        return;
                    }

                    // 已有分支则不能重选
                    if (u.hasTrait(BranchGunner) || u.hasTrait(BranchMech) || u.hasTrait(BranchMartial))
                    {
                        Debug.Log($"[超神机械师] {u.Name} 已选择分支，无法重选");
                        return;
                    }

                    // 选择分支
                    u.addTrait(branchTrait);
                    applied = true;

                    // 分支专属加成
                    var stats = SuperMechStats.Of(u);
                    if (stats != null)
                    {
                        if (branchTrait == BranchGunner)
                        {
                            stats["multiplier_damage"] = (stats["multiplier_damage"] ?? 1f) * 1.3f;
                            stats["attack_speed"] = (stats["attack_speed"] ?? 0f) + 0.2f;
                            stats["range"] = (stats["range"] ?? 0f) + 2f;
                        }
                        else if (branchTrait == BranchMech)
                        {
                            stats["experience"] = (stats["experience"] ?? 1f) * 1.5f;
                            stats["intelligence"] = (stats["intelligence"] ?? 0f) + 10f;
                        }
                        else if (branchTrait == BranchMartial)
                        {
                            stats["multiplier_health"] = (stats["multiplier_health"] ?? 1f) * 1.4f;
                            stats["armor"] = (stats["armor"] ?? 0f) + 5f;
                            stats["damage"] = (stats["damage"] ?? 0f) + 10f;
                        }
                    }

                    Debug.Log($"[超神机械师] {u.Name} 选择分支：{name}");
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }

        /// <summary>获取机械师阶段tier（1-14）。</summary>
        private static int GetMechStageTier(Actor a)
        {
            string[] stages =
            {
                SuperMechTraits.MechInitiate, SuperMechTraits.MechApprentice,
                SuperMechTraits.MechTrainee, SuperMechTraits.MechMagnet,
                SuperMechTraits.MechData, SuperMechTraits.MechWar,
                SuperMechTraits.MechVirtual, SuperMechTraits.MechStarsea,
                SuperMechTraits.MechTruth, SuperMechTraits.MechApostle,
                SuperMechTraits.MechEmperor, SuperMechTraits.MechLord,
                SuperMechTraits.MechGod, SuperMechTraits.MechSupreme,
            };
            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(stages[i])) return i + 1;
            }
            return 0;
        }

        /// <summary>获取单位分支名。</summary>
        public static string GetBranchName(Actor a)
        {
            if (a == null) return "";
            if (a.hasTrait(BranchGunner)) return "枪炮师";
            if (a.hasTrait(BranchMech)) return "机械师";
            if (a.hasTrait(BranchMartial)) return "械武者";
            return "未选择";
        }
    }
}
