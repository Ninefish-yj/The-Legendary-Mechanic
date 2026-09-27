using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系特色专精系统：
    /// 异能系：随机异能类型（火/冰/电/变身/操控/幸运...）
    /// 武道系：武道流派（刚拳/柔拳/快攻/守御...）
    /// 魔法系：元素专精（火/水/风/土/光/暗...）
    /// 念力系：精神领域（控制/探测/念动/心灵感应...）
    /// 机械系：机械类型（机甲/无人机/炮塔...）
    /// 觉醒时随机分配一个特色，各有不同效果。
    /// </summary>
    public static class SuperMechSpecialty
    {
        // —— 异能系特色 ——
        public const string PsiFire      = "sm_spec_psi_fire";     // 火焰操控
        public const string PsiIce       = "sm_spec_psi_ice";      // 冰霜操控
        public const string PsiElectric  = "sm_spec_psi_electric"; // 雷电操控
        public const string PsiTransform = "sm_spec_psi_transform";// 变身
        public const string PsiControl   = "sm_spec_psi_control";  // 物体操控
        public const string PsiLuck      = "sm_spec_psi_luck";     // 幸运
        // —— 原著出现过的异能 ——
        public const string PsiDeath     = "sm_spec_psi_death";     // 亡灵之力/掌控死亡（海拉）
        public const string PsiCarbon    = "sm_spec_psi_carbon";    // 碳元素掌控（灰烬）
        public const string PsiHeal      = "sm_spec_psi_heal";      // 治愈系（欧若拉）
        public const string PsiMagnet    = "sm_spec_psi_magnet";    // 磁场操控（万磁王）
        public const string PsiSoulFire  = "sm_spec_psi_soulfire";  // 灵魂之炎（起誓人）

        // —— 武道系特色：御气技巧（原著 ch48）——
        public const string MartialWave    = "sm_spec_martial_wave";    // 离体波动（气功波）
        public const string MartialFlash   = "sm_spec_martial_flash";   // 闪气（爆气闪躲）
        public const string MartialShield  = "sm_spec_martial_shield";  // 护体气罩
        public const string MartialBurst   = "sm_spec_martial_burst";   // 气力爆发
        public const string MartialReserve = "sm_spec_martial_reserve"; // 潜在存储（压榨细胞回气）
        public const string MartialSky     = "sm_spec_martial_sky";     // 天穹气幕（大师级）
        public const string MartialSync    = "sm_spec_martial_sync";    // 气脉同调（大师级）
        public const string MartialAbyss   = "sm_spec_martial_abyss";   // 蚀气渊流（大师级）
        // —— 更多御气技巧 ——
        public const string MartialCompress = "sm_spec_martial_compress"; // 气力压缩
        public const string MartialForm     = "sm_spec_martial_form";     // 实体化气焰
        public const string MartialExplode  = "sm_spec_martial_explode";  // 爆气
        public const string MartialShock    = "sm_spec_martial_shock";    // 震气
        public const string MartialSurge    = "sm_spec_martial_surge";    // 气力翻涌
        // —— 武道流派大类 ——
        public const string MartialSword   = "sm_spec_martial_sword";   // 剑术流派
        public const string MartialBlade   = "sm_spec_martial_blade";   // 刀术流派
        public const string MartialFist    = "sm_spec_martial_fist";    // 武技流派

        // —— 魔法系特色 ——
        public const string MageFire     = "sm_spec_mage_fire";    // 火焰专精
        public const string MageWater    = "sm_spec_mage_water";   // 水流专精
        public const string MageWind     = "sm_spec_mage_wind";    // 风暴专精
        public const string MageEarth    = "sm_spec_mage_earth";   // 大地专精
        public const string MageLight    = "sm_spec_mage_light";   // 光辉专精
        public const string MageDark     = "sm_spec_mage_dark";    // 暗影专精

        // —— 念力系特色 ——
        public const string MindControl  = "sm_spec_mind_control";  // 精神控制
        public const string MindDetect   = "sm_spec_mind_detect";   // 心灵探测
        public const string MindTelekinesis = "sm_spec_mind_tele"; // 念动
        public const string MindTelepathy = "sm_spec_mind_telepathy"; // 心灵感应
        // —— 原著念力技能 ——
        public const string MindShield   = "sm_spec_mind_shield";   // 念气护盾
        public const string MindCurrent  = "sm_spec_mind_current";  // 人造心灵潜流（克苏耶）

        // —— 机械系特色 ——
        public const string MechMech     = "sm_spec_mech_mech";     // 机甲专精
        public const string MechDrone    = "sm_spec_mech_drone";    // 无人机专精
        public const string MechTurret   = "sm_spec_mech_turret";  // 炮塔专精
        // —— 原著机械技能 ——
        public const string MechOverload = "sm_spec_mech_overload"; // 超负荷
        public const string MechSurge    = "sm_spec_mech_surge";    // 械力涌动
        public const string MechWill     = "sm_spec_mech_will";     // 意志燃烧

        public static void Register()
        {
            // 异能系特色
            AddSpec(PsiFire,      "sm_specialty_136", 10, 0.20f, 0f, "sm_specialty_137");
            AddSpec(PsiIce,       "sm_specialty_138", 5, 0.10f, 0.15f, "sm_specialty_139");
            AddSpec(PsiElectric,  "sm_specialty_140", 8, 0.18f, 0f, "sm_specialty_141");
            AddSpec(PsiTransform, "sm_specialty_142",     5, 0.10f, 0.20f, "sm_specialty_143");
            AddSpec(PsiControl,  "sm_specialty_144",  7, 0.15f, 0f, "sm_specialty_145");
            AddSpec(PsiLuck,      "sm_specialty_146",     3, 0f, 0f,    "sm_specialty_147");
            // 原著出现过的异能
            AddSpec(PsiDeath,    "sm_specialty_148",  12, 0.30f, 0f,   "sm_specialty_149");
            AddSpec(PsiCarbon,   "sm_specialty_150", 10, 0.22f, 0f,   "sm_specialty_151");
            AddSpec(PsiHeal,     "sm_specialty_152",       8, 0f, 0.20f,      "sm_specialty_153");
            AddSpec(PsiMagnet,   "sm_specialty_154",  9, 0.18f, 0f,   "sm_specialty_155");
            AddSpec(PsiSoulFire, "sm_specialty_156",  15, 0.35f, 0f,   "sm_specialty_157");

            // 武道系特色：御气技巧
            AddSpec(MartialWave,    "sm_specialty_158",  5, 0.15f, 0f,   "sm_specialty_159");
            AddSpec(MartialFlash,   "sm_specialty_160",     8, 0.05f, 0.10f, "sm_specialty_161");
            AddSpec(MartialShield,  "sm_specialty_162", 3, 0f, 0.20f,    "sm_specialty_163");
            AddSpec(MartialBurst,   "sm_specialty_164", 5, 0.20f, 0f,   "sm_specialty_165");
            AddSpec(MartialReserve, "sm_specialty_166", 2, 0.05f, 0.15f, "sm_specialty_167");
            AddSpec(MartialSky,     "sm_specialty_168", 6, 0f, 0.30f, "sm_specialty_169");
            AddSpec(MartialSync,    "sm_specialty_170", 8, 0.10f, 0.10f, "sm_specialty_171");
            AddSpec(MartialAbyss,   "sm_specialty_172", 10, 0.25f, 0f, "sm_specialty_173");
            // 更多御气技巧
            AddSpec(MartialCompress, "sm_specialty_174", 4, 0.12f, 0.05f, "sm_specialty_175");
            AddSpec(MartialForm,     "sm_specialty_176", 6, 0.15f, 0.10f, "sm_specialty_177");
            AddSpec(MartialExplode,  "sm_specialty_178",     5, 0.22f, 0f,   "sm_specialty_179");
            AddSpec(MartialShock,    "sm_specialty_180",     7, 0.18f, 0f,   "sm_specialty_181");
            AddSpec(MartialSurge,    "sm_specialty_182", 3, 0.10f, 0.10f, "sm_specialty_183");
            // 武道流派大类
            AddSpec(MartialSword,  "sm_specialty_184",  5, 0.18f, 0f,   "sm_specialty_185");
            AddSpec(MartialBlade,  "sm_specialty_186",  4, 0.15f, 0.05f, "sm_specialty_187");
            AddSpec(MartialFist,   "sm_specialty_188",  3, 0.10f, 0.10f, "sm_specialty_189");

            // 魔法系特色
            AddSpec(MageFire,     "sm_specialty_190",  8, 0.20f, 0f,  "sm_specialty_191");
            AddSpec(MageWater,    "sm_specialty_192",  5, 0.05f, 0.15f, "sm_specialty_193");
            AddSpec(MageWind,     "sm_specialty_194",  6, 0.12f, 0f,  "sm_specialty_195");
            AddSpec(MageEarth,    "sm_specialty_196",  3, 0f, 0.25f,    "sm_specialty_197");
            AddSpec(MageLight,    "sm_specialty_198",  7, 0.10f, 0.10f, "sm_specialty_199");
            AddSpec(MageDark,     "sm_specialty_200",  9, 0.25f, 0f,  "sm_specialty_201");

            // 念力系特色
            AddSpec(MindControl, "sm_specialty_202",  8, 0.15f, 0f,   "sm_specialty_203");
            AddSpec(MindDetect,  "sm_specialty_204",  5, 0.05f, 0.10f, "sm_specialty_205");
            AddSpec(MindTelekinesis, "sm_specialty_206", 10, 0.20f, 0f,   "sm_specialty_207");
            AddSpec(MindTelepathy, "sm_specialty_208", 6, 0.08f, 0.08f, "sm_specialty_209");
            // 原著念力技能
            AddSpec(MindShield,   "sm_specialty_210", 4, 0f, 0.25f,    "sm_specialty_211");
            AddSpec(MindCurrent,  "sm_specialty_212", 12, 0.30f, 0f, "sm_specialty_213");

            // 机械系特色
            AddSpec(MechMech,    "sm_specialty_214",  5, 0.10f, 0.20f, "sm_specialty_215");
            AddSpec(MechDrone,   "sm_specialty_216", 8, 0.15f, 0f,   "sm_specialty_217");
            AddSpec(MechTurret,  "sm_specialty_218",  6, 0.12f, 0.05f, "sm_specialty_219");
            // 原著机械技能
            AddSpec(MechOverload, "sm_specialty_220",  5, 0.30f, 0f,   "sm_specialty_221");
            AddSpec(MechSurge,    "sm_specialty_222", 6, 0.15f, 0.05f, "sm_specialty_223");
            AddSpec(MechWill,     "sm_specialty_224", 8, 0.25f, 0f,   "sm_specialty_225");

            Debug.Log("[超神机械师] 五系特色专精注册完成：23个特色");
        }

        private static void AddSpec(string id, string name, int intell, float dmgMul, float hpMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
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

        /// <summary>踏入超能时随机分配具体异能（仅异能系，原著：异能系天生有具体异能，其他四系无此设定）。</summary>
        public static void AssignRandomSpecialty(Actor a)
        {
            // 只有异能系有天生具体异能（原著：异能系是基因觉醒，天生有异能类型）
            // 其他四系（机械/武道/魔法/念力）的特色是后天学习的职业技能，通过知识树/阶段获得，不随机分配
            if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] specs = { PsiFire, PsiIce, PsiElectric, PsiTransform, PsiControl, PsiLuck,
                    PsiDeath, PsiCarbon, PsiHeal, PsiMagnet, PsiSoulFire };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
        }

        /// <summary>选择分支后获得对应的特色能力（其他四系的特色是后天学习的职业技能）。</summary>
        public static void GrantBranchSpecialty(Actor a, string branchTraitId)
        {
            if (a == null) return;
            string specId = null;

            // 机械系分支 → 机械专精
            if (branchTraitId == SuperMechBranch.BranchGunner) specId = MechTurret;      // 枪炮师 → 炮塔专精
            else if (branchTraitId == SuperMechBranch.BranchMech) specId = MechMech;      // 机械师 → 机甲专精
            else if (branchTraitId == SuperMechBranch.BranchMartial) specId = MechDrone;  // 械武者 → 无人机专精

            // 武道系分支 → 武道技巧
            else if (branchTraitId == SuperMechBranch.BranchMartialBody) specId = MartialShield;    // 体魄 → 护体气罩
            else if (branchTraitId == SuperMechBranch.BranchMartialTactic) specId = MartialFlash;   // 战术 → 闪气
            else if (branchTraitId == SuperMechBranch.BranchMartialPower) specId = MartialExplode;  // 超能 → 爆气

            // 魔法系分支 → 魔法属性（原著：魔法系只有专精法师/魔网法师两类）
            else if (branchTraitId == SuperMechBranch.BranchMageSpecialist)
            {
                string[] elements = { MageFire, MageWater, MageWind, MageEarth, MageLight, MageDark };
                specId = elements[Random.Range(0, elements.Length)];
            }
            else if (branchTraitId == SuperMechBranch.BranchMageWeave) specId = MageLight;  // 魔网法师 → 光辉专精

            // 念力系分支 → 精神能力
            else if (branchTraitId == SuperMechBranch.BranchMindSoul) specId = MindControl;      // 灵魂 → 精神控制
            else if (branchTraitId == SuperMechBranch.BranchMindLaw) specId = MindDetect;        // 法则 → 心灵探测
            else if (branchTraitId == SuperMechBranch.BranchMindReality) specId = MindTelekinesis; // 现实 → 念动

            if (specId != null && !a.hasTrait(specId))
            {
                a.addTrait(specId);
                Debug.Log($"[超神机械师] {a.name} 选择分支后获得特色能力：{LocalizedTextManager.getText("trait_" + specId)}");
            }
        }
        /// <summary>获取单位已有的特色能力列表。</summary>
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
