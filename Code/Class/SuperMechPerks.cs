using System.Collections.Generic;
using NeoModLoader.services;
using NeoModLoader.api;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPerks
    {
        public const string PerkGeomind   = "sm_perk_geometric_mind";
        public const string PerkIronWill  = "sm_perk_iron_will";
        public const string PerkBerserk   = "sm_perk_berserk";
        public const string PerkQuickThink = "sm_perk_quick_think";
        public const string PerkFortune   = "sm_perk_fortune";

        public const string PerkSharpEye   = "sm_perk_sharp_eye";
        public const string PerkToughBody  = "sm_perk_tough_body";
        public const string PerkFastRegen = "sm_perk_fast_regen";
        public const string PerkVeteran   = "sm_perk_veteran";
        public const string PerkNightVision = "sm_perk_night_vision";

        public const string PerkMechAffinity  = "sm_perk_mech_affinity";
        public const string PerkMechSurge     = "sm_perk_magnet_surge";
        public const string PerkMechCraft     = "sm_perk_mech_craft";
        public const string PerkMechTactical  = "sm_perk_mech_tactical";

        public const string PerkPsiControl = "sm_perk_psi_control";
        public const string PerkPsiRange   = "sm_perk_psi_range";

        public const string PerkMartialBreath = "sm_perk_martial_breath";
        public const string PerkMartialFlurry = "sm_perk_martial_flurry";

        public const string PerkManaEff = "sm_perk_mana_eff";
        public const string PerkSpellMaster = "sm_perk_spell_master";

        public const string PerkMindShield = "sm_perk_mind_shield";
        public const string PerkMindCrush = "sm_perk_mind_crush";

        public const string PerkMechEmperor  = "sm_perk_mech_emperor";
        public const string PerkEnergyBody   = "sm_perk_energy_body";
        public const string PerkVirtLord     = "sm_perk_virt_lord";
        public const string PerkMultiResist  = "sm_perk_multi_resist";
        public const string PerkChampion     = "sm_perk_champion";
        public const string PerkMindFortress = "sm_perk_mind_fortress";
        public const string PerkBattleMind   = "sm_perk_battle_mind";
        public const string PerkFocus        = "sm_perk_focus";
        public const string PerkMaterial     = "sm_perk_material";
        public const string PerkDimAdapt     = "sm_perk_dim_adapt";

        public static void Register()
        {
            AddPerk(PerkGeomind,    "sm_perks_000", 10, 0, 0.10f, "sm_perks_001");
            AddPerk(PerkIronWill,   "sm_perks_002", 0, 0, 0.05f, "sm_perks_003");
            AddPerk(PerkBerserk,    "sm_perks_004",     0, 0, 0.15f, "sm_perks_005");
            AddPerk(PerkQuickThink, "sm_perks_006", 8, 0, 0.05f, "sm_perks_007");
            AddPerk(PerkFortune,    "sm_perks_008",     3, 0, 0.0f, "sm_perks_009");

            AddPerk(PerkSharpEye,   "sm_perks_010",     0, 2, 0f, "sm_perks_011");
            AddPerk(PerkToughBody,  "sm_perks_012",     0, 0, 0f, "sm_perks_013");
            AddPerk(PerkFastRegen,  "sm_perks_014",     0, 0, 0f, "sm_perks_015");
            AddPerk(PerkVeteran,    "sm_perks_016",     0, 3, 0.05f, "sm_perks_017");
            AddPerk(PerkNightVision, "sm_perks_018",        0, 0, 0f, "sm_perks_019");

            AddPerk(PerkMechAffinity,  "sm_perks_020",   0, 0, 0.15f, "sm_perks_021");
            AddPerk(PerkMechSurge,     "sm_perks_022",   0, 5, 0.20f, "sm_perks_023");
            AddPerk(PerkMechCraft,     "sm_perks_024",   5, 0, 0.10f, "sm_perks_025");
            AddPerk(PerkMechTactical,  "sm_perks_026",   4, 0, 0.05f, "sm_perks_027");

            AddPerk(PerkPsiControl, "sm_perks_028",   3, 0, 0.10f, "sm_perks_029");
            AddPerk(PerkPsiRange,    "sm_perks_030",   2, 0, 0.05f, "sm_perks_031");

            AddPerk(PerkMartialBreath, "sm_perks_032",    2, 0, 0.08f, "sm_perks_033");
            AddPerk(PerkMartialFlurry, "sm_perks_034",      0, 4, 0.10f, "sm_perks_035");

            AddPerk(PerkManaEff,     "sm_perks_036",   3, 0, 0.10f, "sm_perks_037");
            AddPerk(PerkSpellMaster, "sm_perks_038",   6, 0, 0.15f, "sm_perks_039");

            AddPerk(PerkMindShield, "sm_perks_040",   0, 0, 0f,   "sm_perks_041");
            AddPerk(PerkMindCrush,  "sm_perks_042",   4, 3, 0.12f, "sm_perks_043");

            AddPerk(PerkMechEmperor,  "sm_perks_044",   15, 10, 2.00f, "sm_perks_045");
            AddPerk(PerkEnergyBody,   "sm_perks_046", 10, 5, 0.30f, "sm_perks_047");
            AddPerk(PerkVirtLord,     "sm_perks_048",   20, 0, 1.50f, "sm_perks_049");
            AddPerk(PerkMultiResist,  "sm_perks_050", 5, 0, 0f,   "sm_perks_051");
            AddPerk(PerkChampion,     "sm_perks_052",   10, 8, 0.50f, "sm_perks_053");
            AddPerk(PerkMindFortress, "sm_perks_054", 8, 0, 0f,   "sm_perks_055");
            AddPerk(PerkBattleMind,   "sm_perks_056",   12, 0, 0.15f, "sm_perks_057");
            AddPerk(PerkFocus,        "sm_perks_058",   8, 0, 0.10f, "sm_perks_059");
            AddPerk(PerkMaterial,     "sm_perks_060",   6, 0, 0.10f, "sm_perks_061");
            AddPerk(PerkDimAdapt,     "sm_perks_062", 5, 0, 0.06f, "sm_perks_063");

        }

        private static void AddPerk(string id, string name, int intell, float dmgAdd, float dmgMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconBattleReflexes", group_id = "sm_perks",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgAdd > 0) t.base_stats["damage"] = dmgAdd;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            AssetManager.traits.add(t);
        }

        private static readonly string[] TalentPerks = {
            PerkGeomind, PerkIronWill, PerkBerserk, PerkQuickThink, PerkFortune,
            PerkSharpEye, PerkToughBody, PerkFastRegen, PerkVeteran, PerkNightVision
        };

        public static void GrantRandomPerks(Actor a)
        {
            if (a == null) return;
            int count = Random.Range(1, 3);
            var available = new List<string>(TalentPerks);
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                string perkId = available[idx];
                available.RemoveAt(idx);
                if (!a.hasTrait(perkId))
                    a.addTrait(perkId);
            }
        }

        public static List<string> GetPerks(Actor a)
        {
            var list = new List<string>();
            if (a == null) return list;
            foreach (var perkId in TalentPerks)
            {
                if (a.hasTrait(perkId)) list.Add(perkId);
            }
            string[] classPerks = { PerkMechAffinity, PerkMechSurge, PerkMechCraft, PerkMechTactical,
                PerkPsiControl, PerkPsiRange, PerkMartialBreath, PerkMartialFlurry,
                PerkManaEff, PerkSpellMaster, PerkMindShield, PerkMindCrush };
            foreach (var perkId in classPerks)
            {
                if (a.hasTrait(perkId)) list.Add(perkId);
            }
            return list;
        }
    }
}
