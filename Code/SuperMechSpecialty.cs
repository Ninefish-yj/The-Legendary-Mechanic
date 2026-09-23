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
            AddSpec(PsiFire,      "火焰操控（异能）", 10, 0.20f, 0f, "火焰系异能，伤害大幅提升。");
            AddSpec(PsiIce,       "冰霜操控（异能）", 5, 0.10f, 0.15f, "冰霜系异能，生命提升。");
            AddSpec(PsiElectric,  "雷电操控（异能）", 8, 0.18f, 0f, "雷电系异能，伤害提升。");
            AddSpec(PsiTransform, "变身（异能）",     5, 0.10f, 0.20f, "变身系异能，生存提升。");
            AddSpec(PsiControl,  "物体操控（异能）",  7, 0.15f, 0f, "操控系异能，伤害提升。");
            AddSpec(PsiLuck,      "幸运（异能）",     3, 0f, 0f,    "幸运系异能，命运眷顾。");
            // 原著出现过的异能
            AddSpec(PsiDeath,    "亡灵之力（异能）",  12, 0.30f, 0f,   "海拉的异能，死亡能量侵蚀敌人。");
            AddSpec(PsiCarbon,   "碳元素掌控（异能）", 10, 0.22f, 0f,   "灰烬的异能，操控碳元素。");
            AddSpec(PsiHeal,     "治愈系异能",       8, 0f, 0.20f,      "欧若拉的异能，生命恢复。");
            AddSpec(PsiMagnet,   "磁场操控（异能）",  9, 0.18f, 0f,   "万磁王的异能，操控磁场。");
            AddSpec(PsiSoulFire, "灵魂之炎（异能）",  15, 0.35f, 0f,   "起誓人的异能，超高危。");

            // 武道系特色：御气技巧
            AddSpec(MartialWave,    "离体波动（御气）",  5, 0.15f, 0f,   "气功波，中距离攻击手段。");
            AddSpec(MartialFlash,   "闪气（御气）",     8, 0.05f, 0.10f, "爆气闪躲，生存提升。");
            AddSpec(MartialShield,  "护体气罩（御气）", 3, 0f, 0.20f,    "气罩护体，生命提升。");
            AddSpec(MartialBurst,   "气力爆发（御气）", 5, 0.20f, 0f,   "气力爆发，伤害提升。");
            AddSpec(MartialReserve, "潜在存储（御气）", 2, 0.05f, 0.15f, "压榨细胞快速回气。");
            AddSpec(MartialSky,     "天穹气幕（大师级）", 6, 0f, 0.30f, "大师级御气，气幕防御。");
            AddSpec(MartialSync,    "气脉同调（大师级）", 8, 0.10f, 0.10f, "大师级御气，吸纳他人气力。");
            AddSpec(MartialAbyss,   "蚀气渊流（大师级）", 10, 0.25f, 0f, "大师级御气，伤害极高。");
            // 更多御气技巧
            AddSpec(MartialCompress, "气力压缩（御气）", 4, 0.12f, 0.05f, "压缩气力，提升威力。");
            AddSpec(MartialForm,     "实体化气焰（御气）", 6, 0.15f, 0.10f, "气焰实体化，攻防兼备。");
            AddSpec(MartialExplode,  "爆气（御气）",     5, 0.22f, 0f,   "爆气爆发，伤害提升。");
            AddSpec(MartialShock,    "震气（御气）",     7, 0.18f, 0f,   "震气攻击，伤害提升。");
            AddSpec(MartialSurge,    "气力翻涌（御气）", 3, 0.10f, 0.10f, "气力翻涌，均衡提升。");
            // 武道流派大类
            AddSpec(MartialSword,  "剑术流派",  5, 0.18f, 0f,   "剑术专精，伤害提升。");
            AddSpec(MartialBlade,  "刀术流派",  4, 0.15f, 0.05f, "刀术专精，均衡提升。");
            AddSpec(MartialFist,   "武技流派",  3, 0.10f, 0.10f, "武技专精，均衡提升。");

            // 魔法系特色
            AddSpec(MageFire,     "火焰专精（魔法）",  8, 0.20f, 0f,  "火焰法术，伤害提升。");
            AddSpec(MageWater,    "水流专精（魔法）",  5, 0.05f, 0.15f, "水流法术，生存提升。");
            AddSpec(MageWind,     "风暴专精（魔法）",  6, 0.12f, 0f,  "风暴法术，伤害提升。");
            AddSpec(MageEarth,    "大地专精（魔法）",  3, 0f, 0.25f,    "大地法术，生命提升。");
            AddSpec(MageLight,    "光辉专精（魔法）",  7, 0.10f, 0.10f, "光辉法术，均衡提升。");
            AddSpec(MageDark,     "暗影专精（魔法）",  9, 0.25f, 0f,  "暗影法术，伤害极高。");

            // 念力系特色
            AddSpec(MindControl, "精神控制（念力）",  8, 0.15f, 0f,   "控制系，伤害提升。");
            AddSpec(MindDetect,  "心灵探测（念力）",  5, 0.05f, 0.10f, "探测系，均衡提升。");
            AddSpec(MindTelekinesis, "念动（念力）", 10, 0.20f, 0f,   "念动系，伤害提升。");
            AddSpec(MindTelepathy, "心灵感应（念力）", 6, 0.08f, 0.08f, "感应系，均衡提升。");
            // 原著念力技能
            AddSpec(MindShield,   "念气护盾（念力）", 4, 0f, 0.25f,    "念气护盾，生命提升。");
            AddSpec(MindCurrent,  "人造心灵潜流（念力）", 12, 0.30f, 0f, "克苏耶技能，大范围精神攻击。");

            // 机械系特色
            AddSpec(MechMech,    "机甲专精（机械）",  5, 0.10f, 0.20f, "机甲系，生存提升。");
            AddSpec(MechDrone,   "无人机专精（机械）", 8, 0.15f, 0f,   "无人机系，伤害提升。");
            AddSpec(MechTurret,  "炮塔专精（机械）",  6, 0.12f, 0.05f, "炮塔系，均衡提升。");
            // 原著机械技能
            AddSpec(MechOverload, "超负荷（机械）",  5, 0.30f, 0f,   "消耗气力，机械威力+30~50%。");
            AddSpec(MechSurge,    "械力涌动（机械）", 6, 0.15f, 0.05f, "械力涌动，均衡提升。");
            AddSpec(MechWill,     "意志燃烧（机械）", 8, 0.25f, 0f,   "意志燃烧，伤害提升。");

            Debug.Log("[超神机械师] 五系特色专精注册完成：23个特色");
        }

        private static void AddSpec(string id, string name, int intell, float dmgMul, float hpMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_specialty",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            if (hpMul > 0) t.base_stats["multiplier_health"] = 1f + hpMul;
            AssetManager.traits.add(t);
        }

        /// <summary>觉醒时随机分配特色（在赋予觉醒神权时调用）。</summary>
        public static void AssignRandomSpecialty(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] specs = { PsiFire, PsiIce, PsiElectric, PsiTransform, PsiControl, PsiLuck,
                    PsiDeath, PsiCarbon, PsiHeal, PsiMagnet, PsiSoulFire };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
            else if (a.hasTrait(SuperMechTraits.ClassMartial))
            {
                string[] specs = { MartialWave, MartialFlash, MartialShield, MartialBurst,
                    MartialReserve, MartialSky, MartialSync, MartialAbyss,
                    MartialCompress, MartialForm, MartialExplode, MartialShock, MartialSurge,
                    MartialSword, MartialBlade, MartialFist };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
            else if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                string[] specs = { MechMech, MechDrone, MechTurret,
                    MechOverload, MechSurge, MechWill };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
            else if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                string[] specs = { MageFire, MageWater, MageWind, MageEarth, MageLight, MageDark };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
            else if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                string[] specs = { MindControl, MindDetect, MindTelekinesis, MindTelepathy,
                    MindShield, MindCurrent };
                a.addTrait(specs[Random.Range(0, specs.Length)]);
            }
        }
    }
}
