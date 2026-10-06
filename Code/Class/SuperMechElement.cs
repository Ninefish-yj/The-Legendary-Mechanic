using System.Collections.Generic;
using NeoModLoader.services;
using NeoModLoader.api;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>通用专长系统（被动）：觉醒/分支授予随机专长；区别于 RankSpecialty=晋升阶位专长、Specialization=机械专精</summary>
    public static class SuperMechElement
    {
        // 异能系四大分支（原著：力场/元素操控/体质强化/变身）
        public const string PsiForceField = "sm_spec_psi_forcefield";  // 力场
        public const string PsiElement    = "sm_spec_psi_element";     // 元素操控
        public const string PsiPhysique   = "sm_spec_psi_physique";    // 体质强化
        public const string PsiTransform  = "sm_spec_psi_transform";   // 变身

        // 武道系专精（原著：战兵分支/极道流/兽祖流等流派，精简为4个大方向）
        public const string MartialWeapon  = "sm_spec_martial_weapon";   // 战兵（兵刃）
        public const string MartialFight   = "sm_spec_martial_fight";    // 格斗（徒手/爆发）
        public const string MartialGuard   = "sm_spec_martial_guard";    // 护体（防御）
        public const string MartialExtreme = "sm_spec_martial_extreme";  // 极道（爆种）

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
            // 异能系四大分支（原著：力场/元素操控/体质强化/变身）
            AddSpec(PsiForceField, "sm_specialty_226", 8, 0.15f, 0.20f, "sm_specialty_227");
            AddSpec(PsiElement,    "sm_specialty_228", 10, 0.20f, 0f, "sm_specialty_229");
            AddSpec(PsiPhysique,   "sm_specialty_230", 6, 0.10f, 0.30f, "sm_specialty_231");
            AddSpec(PsiTransform,  "sm_specialty_142", 5, 0.10f, 0.20f, "sm_specialty_143");

            // 武道系专精（原著：战兵分支/极道流/兽祖流等流派，精简为4个大方向）
            AddSpec(MartialWeapon,  "sm_specialty_232", 6, 0.18f, 0.10f, "sm_specialty_233");
            AddSpec(MartialFight,   "sm_specialty_234", 8, 0.22f, 0f, "sm_specialty_235");
            AddSpec(MartialGuard,   "sm_specialty_236", 4, 0f, 0.30f, "sm_specialty_237");
            AddSpec(MartialExtreme, "sm_specialty_238", 10, 0.35f, 0f, "sm_specialty_239");

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

        }

        private static void AddSpec(string id, string name, int intell, float dmgMul, float hpMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconDragonslayer", group_id = "sm_specialties",
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
                string[] specs = { PsiForceField, PsiElement, PsiPhysique, PsiTransform };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
        }

        public static void GrantBranchSpecialty(Actor a)
        {
            if (a == null) return;
            string branchId = SuperMechBranch.GetBranch(a);
            string specId = null;

            // 机械系三分支（武装/能量/操控）
            if (branchId == SuperMechBranch.BranchArmed) specId = MechTurret;
            else if (branchId == SuperMechBranch.BranchEnergy) specId = MechMech;
            else if (branchId == SuperMechBranch.BranchControl) specId = MechDrone;

            // 念力系四大分支
            else if (branchId == SuperMechBranch.BranchPsiMind) specId = MindControl;
            else if (branchId == SuperMechBranch.BranchPsiKinesis) specId = MindTelekinesis;
            else if (branchId == SuperMechBranch.BranchPsiSense) specId = MindDetect;
            else if (branchId == SuperMechBranch.BranchPsiPotential) specId = MindCurrent;

            // 异能系四大分支
            else if (branchId == SuperMechBranch.BranchPsiField) specId = PsiForceField;
            else if (branchId == SuperMechBranch.BranchPsiElement) specId = PsiElement;
            else if (branchId == SuperMechBranch.BranchPsiPhysique) specId = PsiPhysique;
            else if (branchId == SuperMechBranch.BranchPsiTransform) specId = PsiTransform;

            // 武道系四大方向
            else if (branchId == SuperMechBranch.BranchMartialWeapon) specId = MartialWeapon;
            else if (branchId == SuperMechBranch.BranchMartialFight) specId = MartialFight;
            else if (branchId == SuperMechBranch.BranchMartialGuard) specId = MartialGuard;
            else if (branchId == SuperMechBranch.BranchMartialExtreme) specId = MartialExtreme;

            // 无分支时随机分配一个对应体系的专长
            else if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                string[] specs = { MechTurret, MechMech, MechDrone };
                specId = specs[Random.Range(0, specs.Length)];
            }
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] specs = { MindControl, MindTelekinesis, MindDetect, MindCurrent };
                specId = specs[Random.Range(0, specs.Length)];
            }
            else if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                string[] specs = { PsiForceField, PsiElement, PsiPhysique, PsiTransform };
                specId = specs[Random.Range(0, specs.Length)];
            }
            else if (a.hasTrait(SuperMechTraits.ClassMartial))
            {
                string[] specs = { MartialWeapon, MartialFight, MartialGuard, MartialExtreme };
                specId = specs[Random.Range(0, specs.Length)];
            }
            else if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                string[] elements = { MageFire, MageWater, MageWind, MageEarth, MageLight, MageDark };
                specId = elements[Random.Range(0, elements.Length)];
            }
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] specs = { PsiForceField, PsiElement, PsiPhysique, PsiTransform };
                specId = specs[Random.Range(0, specs.Length)];
            }

            if (specId != null && !a.hasTrait(specId))
            {
                a.addTrait(specId);
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
