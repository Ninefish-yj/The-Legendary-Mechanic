using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechBranch
    {
        public const string BranchGunner = "sm_branch_gunner";
        public const string BranchMech = "sm_branch_mech";
        public const string BranchMartial = "sm_branch_martial";

        public const string BranchMartialBody = "sm_branch_martial_body";
        public const string BranchMartialTactic = "sm_branch_martial_tactic";
        public const string BranchMartialPower = "sm_branch_martial_power";

        public const string BranchPsiAttack = "sm_branch_psi_attack";
        public const string BranchPsiCycle = "sm_branch_psi_cycle";
        public const string BranchPsiFunc = "sm_branch_psi_func";

        public const string BranchMageSpecialist = "sm_branch_mage_specialist";
        public const string BranchMageWeave = "sm_branch_mage_weave";

        public const string BranchMindSoul = "sm_branch_mind_soul";
        public const string BranchMindLaw = "sm_branch_mind_law";
        public const string BranchMindReality = "sm_branch_mind_reality";

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
            AllBranches.Add(new BranchDef { traitId = BranchGunner, name = "sm_branch_511", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_512", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.3f; s["attack_speed"]=(s["attack_speed"])+0.2f; s["range"]=(s["range"])+2f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMech, name = "sm_branch_513", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_514", applyBonus = s => { s["experience"]=((s["experience"] == 0f ? 1f : s["experience"]))*1.5f; s["intelligence"]=(s["intelligence"])+10f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartial, name = "sm_branch_515", classTrait = SuperMechTraits.ClassMech, desc = "sm_branch_516", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.4f; s["armor"]=(s["armor"])+5f; s["damage"]=(s["damage"])+10f; } });

            AllBranches.Add(new BranchDef { traitId = BranchMartialBody, name = "sm_branch_517", classTrait = SuperMechTraits.ClassMartial, desc = "sm_branch_518", applyBonus = s => { s["attack_speed"]=(s["attack_speed"])+0.25f; s["critical_chance"]=(s["critical_chance"])+0.1f; s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.15f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartialTactic, name = "sm_branch_519", classTrait = SuperMechTraits.ClassMartial, desc = "sm_branch_520", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.4f; s["range"]=(s["range"])+3f; s["damage"]=(s["damage"])+8f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartialPower, name = "sm_branch_521", classTrait = SuperMechTraits.ClassMartial, desc = "sm_branch_522", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.5f; s["stamina"]=(s["stamina"])+20f; s["armor"]=(s["armor"])+3f; } });

            AllBranches.Add(new BranchDef { traitId = BranchPsiAttack, name = "sm_branch_523", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_524", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.5f; s["critical_chance"]=(s["critical_chance"])+0.05f; } });
            AllBranches.Add(new BranchDef { traitId = BranchPsiCycle, name = "sm_branch_525", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_526", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.3f; s["stamina"]=(s["stamina"])+25f; s["multiplier_stamina"]=((s["multiplier_stamina"] == 0f ? 1f : s["multiplier_stamina"]))*1.2f; } });
            AllBranches.Add(new BranchDef { traitId = BranchPsiFunc, name = "sm_branch_527", classTrait = SuperMechTraits.ClassPsi, desc = "sm_branch_528", applyBonus = s => { s["intelligence"]=(s["intelligence"])+15f; s["attack_speed"]=(s["attack_speed"])+0.15f; s["range"]=(s["range"])+2f; } });

            AllBranches.Add(new BranchDef { traitId = BranchMageSpecialist, name = "sm_branch_529", classTrait = SuperMechTraits.ClassMage, desc = "sm_branch_530", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.5f; s["intelligence"]=(s["intelligence"])+15f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMageWeave, name = "sm_branch_531", classTrait = SuperMechTraits.ClassMage, desc = "sm_branch_532", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.3f; s["experience"]=((s["experience"] == 0f ? 1f : s["experience"]))*1.2f; s["mana"]=(s["mana"])+50f; } });

            AllBranches.Add(new BranchDef { traitId = BranchMindSoul, name = "sm_branch_533", classTrait = SuperMechTraits.ClassMind, desc = "sm_branch_534", applyBonus = s => { s["intelligence"]=(s["intelligence"])+20f; s["critical_chance"]=(s["critical_chance"])+0.12f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMindLaw, name = "sm_branch_535", classTrait = SuperMechTraits.ClassMind, desc = "sm_branch_536", applyBonus = s => { s["multiplier_damage"]=((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]))*1.35f; s["damage"]=(s["damage"])+5f; s["health"]=(s["health"])+15f; s["intelligence"]=(s["intelligence"])+5f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMindReality, name = "sm_branch_537", classTrait = SuperMechTraits.ClassMind, desc = "sm_branch_538", applyBonus = s => { s["multiplier_health"]=((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]))*1.45f; s["armor"]=(s["armor"])+8f; s["damage"]=(s["damage"])+12f; } });

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
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_branch_541");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_branch_542");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_branch_543");
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
