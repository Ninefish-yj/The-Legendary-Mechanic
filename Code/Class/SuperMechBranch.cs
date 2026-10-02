using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业分支系统
    /// 原著设定：
    /// - 机械系：枪炮师/机械师/械武者（机械师内再分大型/微型/虚拟专精）
    /// - 念力系：心智系/念动系/感应系/潜能系（四大分支，每分支下有各种流派）
    /// - 武道系：无数武技流派，无固定转职分支
    /// - 魔法系：元素/秘术/庇护/时空/塑能/召唤/诅咒祈福等法术分支，可兼修，无固定转职
    /// - 异能系：随机觉醒异能，无固定转职分支
    /// </summary>
    public static class SuperMechBranch
    {
        // 机械系三分支
        public const string BranchGunner = "sm_branch_gunner";
        public const string BranchMech = "sm_branch_mech";
        public const string BranchMartial = "sm_branch_martial";

        // 念力系四大分支（原著ch1384：心智系/念动系/感应系/潜能系）
        public const string BranchPsiMind = "sm_branch_psi_mind";
        public const string BranchPsiKinesis = "sm_branch_psi_kinesis";
        public const string BranchPsiSense = "sm_branch_psi_sense";
        public const string BranchPsiPotential = "sm_branch_psi_potential";

        public struct BranchDef
        {
            public string traitId;
            public string name;
            public string classTrait;
            public string desc;
            public System.Action<BaseStats> applyBonus;
        }

        public static readonly List<BranchDef> AllBranches = new List<BranchDef>();

        public static void Register()
        {
            // === 机械系三分支 ===
            AllBranches.Add(new BranchDef { traitId = BranchGunner, name = "sm_branch_511", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_512", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.3f; s["attack_speed"]=(s["attack_speed"])+0.2f; s["range"]=(s["range"])+2f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMech, name = "sm_branch_513", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_514", applyBonus = s => { s["experience"]=((s["experience"] == 0f ? 1f : s["experience"]))*1.5f; s["intelligence"]=(s["intelligence"])+10f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartial, name = "sm_branch_515", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_516", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.4f; s["armor"]=(s["armor"])+5f; s["damage"]=(s["damage"])+10f; } });

            // === 念力系四大分支（原著ch1384）===
            // 心智系：弗丁路线，精神攻击/控制/幻术
            AllBranches.Add(new BranchDef { traitId = BranchPsiMind, name = "sm_branch_533", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_534", applyBonus = s => { s["intelligence"]=(s["intelligence"])+20f; s["critical_chance"]=(s["critical_chance"])+0.12f; } });
            // 念动系：克苏耶路线，念动力移物/远程操控
            AllBranches.Add(new BranchDef { traitId = BranchPsiKinesis, name = "sm_branch_535", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_536", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.35f; s["damage"]=(s["damage"])+5f; s["health"]=(s["health"])+15f; s["intelligence"]=(s["intelligence"])+5f; } });
            // 感应系：感知/预知/探测
            AllBranches.Add(new BranchDef { traitId = BranchPsiSense, name = "sm_branch_537", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_538", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.45f; s["armor"]=(s["armor"])+8f; s["damage"]=(s["damage"])+12f; } });
            // 潜能系：激发自身潜能/爆发
            AllBranches.Add(new BranchDef { traitId = BranchPsiPotential, name = "sm_branch_psi_potential_name", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_psi_potential_desc", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.25f; s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.25f; s["stamina"]=(s["stamina"])+30f; } });

            // 武道系/魔法系/异能系无固定转职分支，不注册分支

            foreach (var b in AllBranches)
            {
                var bs = new BaseStats();
                b.applyBonus(bs);
                var t = new ActorTrait
                {
                    id = b.traitId,
                    path_icon = "ui/Icons/actor_traits/iconArcaneReflexes",
                    group_id = "sm_branches",
                    needs_to_be_explored = false,
                    base_stats = bs
                };
                AssetManager.traits.add(t);
                LocalizedTextManager.add("trait_" + b.traitId, LocalizedTextManager.getText(b.name), pReplace: true);
                LocalizedTextManager.add("trait_" + b.traitId + "_info", LocalizedTextManager.getText(b.desc), pReplace: true);
            }
        }

        public static string GetClass(Actor a)
        {
            if (a == null) return null;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return LocalizedTextManager.getText("sm_branch_539");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_branch_540");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_branch_543");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_branch_542");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_branch_541");
            return null;
        }

        public static bool HasAnyBranch(Actor a, string classTrait)
        {
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait && a.hasTrait(b.traitId)) return true;
            }
            return false;
        }

        public static List<BranchDef> GetBranchesForClass(string classTrait)
        {
            var list = new List<BranchDef>();
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait) list.Add(b);
            }
            return list;
        }

        public static string GetBranchName(Actor a)
        {
            if (a == null) return "";
            foreach (var b in AllBranches)
            {
                if (a.hasTrait(b.traitId)) return b.name;
            }
            return LocalizedTextManager.getText("sm_branch_544");
        }

        public static string GetBranchTrait(Actor a)
        {
            if (a == null) return null;
            foreach (var b in AllBranches)
            {
                if (a.hasTrait(b.traitId)) return b.traitId;
            }
            return null;
        }
    }
}
