using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechSpecialty
    {
        public const string PsiFire      = "sm_spec_psi_fire";
        public const string PsiIce       = "sm_spec_psi_ice";
        public const string PsiElectric  = "sm_spec_psi_electric";
        public const string PsiTransform = "sm_spec_psi_transform";
        public const string PsiControl   = "sm_spec_psi_control";
        public const string PsiLuck      = "sm_spec_psi_luck";
        public const string PsiDeath     = "sm_spec_psi_death";
        public const string PsiCarbon    = "sm_spec_psi_carbon";
        public const string PsiHeal      = "sm_spec_psi_heal";
        public const string PsiMagnet    = "sm_spec_psi_magnet";
        public const string PsiSoulFire  = "sm_spec_psi_soulfire";

        public const string MartialWave    = "sm_spec_martial_wave";
        public const string MartialFlash   = "sm_spec_martial_flash";
        public const string MartialShield  = "sm_spec_martial_shield";
        public const string MartialBurst   = "sm_spec_martial_burst";
        public const string MartialReserve = "sm_spec_martial_reserve";
        public const string MartialSky     = "sm_spec_martial_sky";
        public const string MartialSync    = "sm_spec_martial_sync";
        public const string MartialAbyss   = "sm_spec_martial_abyss";
        public const string MartialCompress = "sm_spec_martial_compress";
        public const string MartialForm     = "sm_spec_martial_form";
        public const string MartialExplode  = "sm_spec_martial_explode";
        public const string MartialShock    = "sm_spec_martial_shock";
        public const string MartialSurge    = "sm_spec_martial_surge";
        public const string MartialSword   = "sm_spec_martial_sword";
        public const string MartialBlade   = "sm_spec_martial_blade";
        public const string MartialFist    = "sm_spec_martial_fist";

        public const string MageFire     = "sm_spec_mage_fire";
        public const string MageWater    = "sm_spec_mage_water";
        public const string MageWind     = "sm_spec_mage_wind";
        public const string MageEarth    = "sm_spec_mage_earth";
        public const string MageLight    = "sm_spec_mage_light";
        public const string MageDark     = "sm_spec_mage_dark";

        public const string MindControl  = "sm_spec_mind_control";
        public const string MindDetect   = "sm_spec_mind_detect";
        public const string MindTelekinesis = "sm_spec_mind_tele";
        public const string MindTelepathy = "sm_spec_mind_telepathy";
        public const string MindShield   = "sm_spec_mind_shield";
        public const string MindCurrent  = "sm_spec_mind_current";

        public const string MechMech     = "sm_spec_mech_mech";
        public const string MechDrone    = "sm_spec_mech_drone";
        public const string MechTurret   = "sm_spec_mech_turret";
        public const string MechOverload = "sm_spec_mech_overload";
        public const string MechSurge    = "sm_spec_mech_surge";
        public const string MechWill     = "sm_spec_mech_will";

        public static void Register()
        {
            AddSpec(PsiFire,      "sm_specialty_136", 10, 0.20f, 0f, "sm_specialty_137");
            AddSpec(PsiIce,       "sm_specialty_138", 5, 0.10f, 0.15f, "sm_specialty_139");
            AddSpec(PsiElectric,  "sm_specialty_140", 8, 0.18f, 0f, "sm_specialty_141");
            AddSpec(PsiTransform, "sm_specialty_142",     5, 0.10f, 0.20f, "sm_specialty_143");
            AddSpec(PsiControl,  "sm_specialty_144",  7, 0.15f, 0f, "sm_specialty_145");
            AddSpec(PsiLuck,      "sm_specialty_146",     3, 0f, 0f,    "sm_specialty_147");
            AddSpec(PsiDeath,    "sm_specialty_148",  12, 0.30f, 0f,   "sm_specialty_149");
            AddSpec(PsiCarbon,   "sm_specialty_150", 10, 0.22f, 0f,   "sm_specialty_151");
            AddSpec(PsiHeal,     "sm_specialty_152",       8, 0f, 0.20f,      "sm_specialty_153");
            AddSpec(PsiMagnet,   "sm_specialty_154",  9, 0.18f, 0f,   "sm_specialty_155");
            AddSpec(PsiSoulFire, "sm_specialty_156",  15, 0.35f, 0f,   "sm_specialty_157");

            AddSpec(MartialWave,    "sm_specialty_158",  5, 0.15f, 0f,   "sm_specialty_159");
            AddSpec(MartialFlash,   "sm_specialty_160",     8, 0.05f, 0.10f, "sm_specialty_161");
            AddSpec(MartialShield,  "sm_specialty_162", 3, 0f, 0.20f,    "sm_specialty_163");
            AddSpec(MartialBurst,   "sm_specialty_164", 5, 0.20f, 0f,   "sm_specialty_165");
            AddSpec(MartialReserve, "sm_specialty_166", 2, 0.05f, 0.15f, "sm_specialty_167");
            AddSpec(MartialSky,     "sm_specialty_168", 6, 0f, 0.30f, "sm_specialty_169");
            AddSpec(MartialSync,    "sm_specialty_170", 8, 0.10f, 0.10f, "sm_specialty_171");
            AddSpec(MartialAbyss,   "sm_specialty_172", 10, 0.25f, 0f, "sm_specialty_173");
            AddSpec(MartialCompress, "sm_specialty_174", 4, 0.12f, 0.05f, "sm_specialty_175");
            AddSpec(MartialForm,     "sm_specialty_176", 6, 0.15f, 0.10f, "sm_specialty_177");
            AddSpec(MartialExplode,  "sm_specialty_178",     5, 0.22f, 0f,   "sm_specialty_179");
            AddSpec(MartialShock,    "sm_specialty_180",     7, 0.18f, 0f,   "sm_specialty_181");
            AddSpec(MartialSurge,    "sm_specialty_182", 3, 0.10f, 0.10f, "sm_specialty_183");
            AddSpec(MartialSword,  "sm_specialty_184",  5, 0.18f, 0f,   "sm_specialty_185");
            AddSpec(MartialBlade,  "sm_specialty_186",  4, 0.15f, 0.05f, "sm_specialty_187");
            AddSpec(MartialFist,   "sm_specialty_188",  3, 0.10f, 0.10f, "sm_specialty_189");

            AddSpec(MageFire,     "sm_specialty_190",  8, 0.20f, 0f,  "sm_specialty_191");
            AddSpec(MageWater,    "sm_specialty_192",  5, 0.05f, 0.15f, "sm_specialty_193");
            AddSpec(MageWind,     "sm_specialty_194",  6, 0.12f, 0f,  "sm_specialty_195");
            AddSpec(MageEarth,    "sm_specialty_196",  3, 0f, 0.25f,    "sm_specialty_197");
            AddSpec(MageLight,    "sm_specialty_198",  7, 0.10f, 0.10f, "sm_specialty_199");
            AddSpec(MageDark,     "sm_specialty_200",  9, 0.25f, 0f,  "sm_specialty_201");

            AddSpec(MindControl, "sm_specialty_202",  8, 0.15f, 0f,   "sm_specialty_203");
            AddSpec(MindDetect,  "sm_specialty_204",  5, 0.05f, 0.10f, "sm_specialty_205");
            AddSpec(MindTelekinesis, "sm_specialty_206", 10, 0.20f, 0f,   "sm_specialty_207");
            AddSpec(MindTelepathy, "sm_specialty_208", 6, 0.08f, 0.08f, "sm_specialty_209");
            AddSpec(MindShield,   "sm_specialty_210", 4, 0f, 0.25f,    "sm_specialty_211");
            AddSpec(MindCurrent,  "sm_specialty_212", 12, 0.30f, 0f, "sm_specialty_213");

            AddSpec(MechMech,    "sm_specialty_214",  5, 0.10f, 0.20f, "sm_specialty_215");
            AddSpec(MechDrone,   "sm_specialty_216", 8, 0.15f, 0f,   "sm_specialty_217");
            AddSpec(MechTurret,  "sm_specialty_218",  6, 0.12f, 0.05f, "sm_specialty_219");
            AddSpec(MechOverload, "sm_specialty_220",  5, 0.30f, 0f,   "sm_specialty_221");
            AddSpec(MechSurge,    "sm_specialty_222", 6, 0.15f, 0.05f, "sm_specialty_223");
            AddSpec(MechWill,     "sm_specialty_224", 8, 0.25f, 0f,   "sm_specialty_225");

            Debug.Log("[超神机械师] 五系特色专精注册完成：23个特色");
        }

        private static void AddSpec(string id, string name, int intell, float dmgMul, float hpMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconDragonslayer", group_id = "sm_specialty",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            if (hpMul > 0) t.base_stats["multiplier_health"] = 1f + hpMul;
            AssetManager.traits.add(t);
        }

        public static void AssignRandomSpecialty(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] specs = { PsiFire, PsiIce, PsiElectric, PsiTransform, PsiControl, PsiLuck,
                    PsiDeath, PsiCarbon, PsiHeal, PsiMagnet, PsiSoulFire };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
        }

        public static void GrantBranchSpecialty(Actor a, string branchTraitId)
        {
            if (a == null) return;
            string specId = null;

            if (branchTraitId == SuperMechBranch.BranchGunner) specId = MechTurret;
            else if (branchTraitId == SuperMechBranch.BranchMech) specId = MechMech;
            else if (branchTraitId == SuperMechBranch.BranchMartial) specId = MechDrone;

            else if (branchTraitId == SuperMechBranch.BranchMartialBody) specId = MartialShield;
            else if (branchTraitId == SuperMechBranch.BranchMartialTactic) specId = MartialFlash;
            else if (branchTraitId == SuperMechBranch.BranchMartialPower) specId = MartialExplode;

            else if (branchTraitId == SuperMechBranch.BranchMageSpecialist)
            {
                string[] elements = { MageFire, MageWater, MageWind, MageEarth, MageLight, MageDark };
                specId = elements[Random.Range(0, elements.Length)];
            }
            else if (branchTraitId == SuperMechBranch.BranchMageWeave) specId = MageLight;

            else if (branchTraitId == SuperMechBranch.BranchMindSoul) specId = MindControl;
            else if (branchTraitId == SuperMechBranch.BranchMindLaw) specId = MindDetect;
            else if (branchTraitId == SuperMechBranch.BranchMindReality) specId = MindTelekinesis;

            if (specId != null && !a.hasTrait(specId))
            {
                a.addTrait(specId);
                Debug.Log($"[超神机械师] {a.name} 选择分支后获得特色能力：{LocalizedTextManager.getText("trait_" + specId)}");
            }
        }
        public static List<string> GetSpecialties(Actor a)
        {
            var list = new List<string>();
            if (a == null) return list;
            string[] allSpecs = {
                PsiFire, PsiIce, PsiElectric, PsiTransform, PsiControl, PsiLuck,
                PsiDeath, PsiCarbon, PsiHeal, PsiMagnet, PsiSoulFire,
                MartialWave, MartialFlash, MartialShield, MartialBurst, MartialReserve,
                MartialSky, MartialSync, MartialAbyss, MartialCompress, MartialForm,
                MartialExplode, MartialShock, MartialSurge, MartialSword, MartialBlade, MartialFist,
                MageFire, MageWater, MageWind, MageEarth, MageLight, MageDark,
                MindControl, MindDetect, MindTelekinesis, MindTelepathy, MindShield, MindCurrent,
                MechMech, MechDrone, MechTurret, MechOverload, MechSurge, MechWill
            };
            foreach (var specId in allSpecs)
            {
                if (a.hasTrait(specId)) list.Add(specId);
            }
            return list;
        }
    }
}
