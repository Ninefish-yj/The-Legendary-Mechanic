using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业方向系统（独立数据存储，不注册为特质）
    /// 原著设定：
    /// - 机械系：枪炮师/机械师/械武者三个职业方向
    /// - 念力系：心智系/念动系/感应系/潜能系四大方向
    /// 注意：职业方向与分支（武装/能量/操控）是两个独立系统
    /// </summary>
    public static class SuperMechBranch
    {
        // 机械系三职业方向
        public const string BranchGunner = "sm_branch_gunner";
        public const string BranchMech = "sm_branch_mech";
        public const string BranchMartial = "sm_branch_martial";

        // 念力系四大方向（原著ch1384：心智系/念动系/感应系/潜能系）
        public const string BranchPsiMind = "sm_branch_psi_mind";
        public const string BranchPsiKinesis = "sm_branch_psi_kinesis";
        public const string BranchPsiSense = "sm_branch_psi_sense";
        public const string BranchPsiPotential = "sm_branch_psi_potential";

        public struct BranchDef
        {
            public string id;
            public string name;
            public string classTrait;
            public string desc;
            public System.Action<BaseStats> applyBonus;
        }

        public static readonly List<BranchDef> AllBranches = new List<BranchDef>();

        // 单位职业方向存储：单位ID -> 方向ID
        private static readonly Dictionary<long, string> _unitBranches = new Dictionary<long, string>();

        public static void Register()
        {
            // === 机械系三职业方向 ===
            AllBranches.Add(new BranchDef { id = BranchGunner, name = "sm_branch_511", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_512", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.3f; s["attack_speed"]=(s["attack_speed"])+0.2f; s["range"]=(s["range"])+2f; } });
            AllBranches.Add(new BranchDef { id = BranchMech, name = "sm_branch_513", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_514", applyBonus = s => { s["experience"]=((s["experience"] == 0f ? 1f : s["experience"]))*1.5f; s["intelligence"]=(s["intelligence"])+10f; } });
            AllBranches.Add(new BranchDef { id = BranchMartial, name = "sm_branch_515", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_516", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.4f; s["armor"]=(s["armor"])+5f; s["damage"]=(s["damage"])+10f; } });

            // === 念力系四大方向（原著ch1384）===
            AllBranches.Add(new BranchDef { id = BranchPsiMind, name = "sm_branch_533", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_534", applyBonus = s => { s["intelligence"]=(s["intelligence"])+20f; s["critical_chance"]=(s["critical_chance"])+0.12f; } });
            AllBranches.Add(new BranchDef { id = BranchPsiKinesis, name = "sm_branch_535", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_536", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.35f; s["damage"]=(s["damage"])+5f; s["health"]=(s["health"])+15f; s["intelligence"]=(s["intelligence"])+5f; } });
            AllBranches.Add(new BranchDef { id = BranchPsiSense, name = "sm_branch_537", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_538", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.45f; s["armor"]=(s["armor"])+8f; s["damage"]=(s["damage"])+12f; } });
            AllBranches.Add(new BranchDef { id = BranchPsiPotential, name = "sm_branch_psi_potential_name", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_psi_potential_desc", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.25f; s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.25f; s["stamina"]=(s["stamina"])+30f; } });

            // 武道系/魔法系/异能系无固定转职方向
        }

        // === 方向数据操作 ===
        public static void SetBranch(Actor a, string branchId)
        {
            if (a == null) return;
            _unitBranches[a.data.id] = branchId;
        }

        public static string GetBranch(Actor a)
        {
            if (a == null) return null;
            _unitBranches.TryGetValue(a.data.id, out string branch);
            return branch;
        }

        public static bool HasBranch(Actor a, string branchId)
        {
            if (a == null) return false;
            return GetBranch(a) == branchId;
        }

        public static bool HasAnyBranch(Actor a, string classTrait)
        {
            string branch = GetBranch(a);
            if (branch == null) return false;
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait && b.id == branch) return true;
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
            string branch = GetBranch(a);
            if (branch == null) return LocalizedTextManager.getText("sm_branch_544");
            foreach (var b in AllBranches)
            {
                if (b.id == branch) return b.name;
            }
            return LocalizedTextManager.getText("sm_branch_544");
        }

        public static string GetBranchId(Actor a)
        {
            return GetBranch(a);
        }

        /// <summary>应用方向属性加成（在计算属性时调用）</summary>
        public static void ApplyBranchBonus(Actor a, BaseStats stats)
        {
            if (a == null || stats == null) return;
            string branch = GetBranch(a);
            if (branch == null) return;
            foreach (var b in AllBranches)
            {
                if (b.id == branch)
                {
                    b.applyBonus(stats);
                    return;
                }
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

        public static void Clear()
        {
            _unitBranches.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_unitBranches, alive);
        }
    }
}
