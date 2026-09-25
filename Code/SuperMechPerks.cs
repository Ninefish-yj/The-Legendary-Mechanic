using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 专长系统（原著 ch50）：
    /// 专长分三类：天赋专长（出生独有）、通用专长（人人可学）、职业专长（对应主职业树）。
    /// 每个单位最多挂若干专长，专长提供独特被动效果。
    /// </summary>
    public static class SuperMechPerks
    {
        // —— 天赋专长（代表，原著有上百个）——
        public const string PerkGeomind   = "sm_perk_geometric_mind";   // 几何思维（韩萧天赋）
        public const string PerkIronWill  = "sm_perk_iron_will";         // 钢铁意志
        public const string PerkBerserk   = "sm_perk_berserk";          // 暴怒
        public const string PerkQuickThink = "sm_perk_quick_think";     // 极速思维
        public const string PerkFortune   = "sm_perk_fortune";          // 幸运

        // —— 通用专长 ——
        public const string PerkSharpEye   = "sm_perk_sharp_eye";       // 锐利目光
        public const string PerkToughBody  = "sm_perk_tough_body";      // 强健体魄
        public const string PerkFastRegen = "sm_perk_fast_regen";      // 快速恢复
        public const string PerkVeteran   = "sm_perk_veteran";         // 老兵本能
        public const string PerkNightVision = "sm_perk_night_vision";  // 夜视

        // —— 职业专长（机械系代表）——
        public const string PerkMechAffinity  = "sm_perk_mech_affinity";
        public const string PerkMechSurge     = "sm_perk_magnet_surge";
        public const string PerkMechCraft     = "sm_perk_mech_craft";
        public const string PerkMechTactical  = "sm_perk_mech_tactical";

        // —— 职业专长（异能系代表）——
        public const string PerkPsiControl = "sm_perk_psi_control";
        public const string PerkPsiRange   = "sm_perk_psi_range";

        // —— 职业专长（武道系代表）——
        public const string PerkMartialBreath = "sm_perk_martial_breath";
        public const string PerkMartialFlurry = "sm_perk_martial_flurry";

        // —— 职业专长（魔法系代表）——
        public const string PerkManaEff = "sm_perk_mana_eff";
        public const string PerkSpellMaster = "sm_perk_spell_master";

        // —— 职业专长（念力系代表）——
        public const string PerkMindShield = "sm_perk_mind_shield";
        public const string PerkMindCrush = "sm_perk_mind_crush";

        // —— 高阶专长（原著后期）——
        public const string PerkMechEmperor  = "sm_perk_mech_emperor";   // 机械帝皇
        public const string PerkEnergyBody   = "sm_perk_energy_body";    // 完美能量亲和体质
        public const string PerkVirtLord     = "sm_perk_virt_lord";      // 虚拟主宰
        public const string PerkMultiResist  = "sm_perk_multi_resist";   // 多元抗性强化
        public const string PerkChampion     = "sm_perk_champion";       // 万夫莫开
        public const string PerkMindFortress = "sm_perk_mind_fortress";  // 高级心灵强韧
        public const string PerkBattleMind   = "sm_perk_battle_mind";    // 战场理智
        public const string PerkFocus        = "sm_perk_focus";          // 高度专注
        public const string PerkMaterial     = "sm_perk_material";       // 材料先驱
        public const string PerkDimAdapt     = "sm_perk_dim_adapt";      // 次级维度适应者

        public static void Register()
        {
            // 天赋专长
            AddPerk(PerkGeomind,    "几何思维（天赋）", 10, 0, 0.10f, "天赋专长，智力大幅提升。");
            AddPerk(PerkIronWill,   "钢铁意志（天赋）", 0, 0, 0.05f, "意志坚定，不易被控。");
            AddPerk(PerkBerserk,    "暴怒（天赋）",     0, 0, 0.15f, "血量越低伤害越高。");
            AddPerk(PerkQuickThink, "极速思维（天赋）", 8, 0, 0.05f, "反应速度提升。");
            AddPerk(PerkFortune,    "幸运（天赋）",     3, 0, 0.0f, "命运眷顾。");

            // 通用专长
            AddPerk(PerkSharpEye,   "锐利目光",     0, 2, 0f, "命中率提升。");
            AddPerk(PerkToughBody,  "强健体魄",     0, 0, 0f, "生命倍率+15%。");
            AddPerk(PerkFastRegen,  "快速恢复",     0, 0, 0f, "恢复速度提升。");
            AddPerk(PerkVeteran,    "老兵本能",     0, 3, 0.05f, "战斗经验丰富。");
            AddPerk(PerkNightVision, "夜视",        0, 0, 0f, "夜间视野提升。");

            // 机械系专长
            AddPerk(PerkMechAffinity,  "机械亲和",   0, 0, 0.15f, "制造速度与质量提升。");
            AddPerk(PerkMechSurge,     "械力涌动",   0, 5, 0.20f, "械力爆发，伤害提升。");
            AddPerk(PerkMechCraft,     "大师工匠",   5, 0, 0.10f, "制造品质提升。");
            AddPerk(PerkMechTactical,  "战术大师",   4, 0, 0.05f, "机械单位效率提升。");

            // 异能系专长
            AddPerk(PerkPsiControl, "异能操控",   3, 0, 0.10f, "异能精度提升。");
            AddPerk(PerkPsiRange,    "异能范围",   2, 0, 0.05f, "异能范围扩大。");

            // 武道系专长
            AddPerk(PerkMartialBreath, "吐纳法",    2, 0, 0.08f, "气力恢复加速。");
            AddPerk(PerkMartialFlurry, "连击",      0, 4, 0.10f, "攻击速度提升。");

            // 魔法系专长
            AddPerk(PerkManaEff,     "魔力增效",   3, 0, 0.10f, "魔力利用效率提升。");
            AddPerk(PerkSpellMaster, "法术大师",   6, 0, 0.15f, "法术威力提升。");

            // 念力系专长
            AddPerk(PerkMindShield, "精神屏障",   0, 0, 0f,   "精神防御提升。");
            AddPerk(PerkMindCrush,  "念动冲击",   4, 3, 0.12f, "念力伤害提升。");

            // 高阶专长（原著后期）
            AddPerk(PerkMechEmperor,  "机械帝皇",   15, 10, 2.00f, "机械亲和+200%，机械技能+1级。");
            AddPerk(PerkEnergyBody,   "完美能量亲和体质", 10, 5, 0.30f, "能量感知+120%，能量攻击+30%。");
            AddPerk(PerkVirtLord,     "虚拟主宰",   20, 0, 1.50f, "虚拟技术效果2.5倍。");
            AddPerk(PerkMultiResist,  "多元抗性强化", 5, 0, 0f,   "各项抗性提升。");
            AddPerk(PerkChampion,     "万夫莫开",   10, 8, 0.50f, "百分比提高全属性。");
            AddPerk(PerkMindFortress, "高级心灵强韧", 8, 0, 0f,   "精神抗性大幅提升。");
            AddPerk(PerkBattleMind,   "战场理智",   12, 0, 0.15f, "最高提升15%智力。");
            AddPerk(PerkFocus,        "高度专注",   8, 0, 0.10f, "学习/制造速度+10%。");
            AddPerk(PerkMaterial,     "材料先驱",   6, 0, 0.10f, "合成材料几率提升。");
            AddPerk(PerkDimAdapt,     "次级维度适应者", 5, 0, 0.06f, "次级维度全属性加成。");

            Debug.Log("[超神机械师] 专长注册完成：32个");
        }

        private static void AddPerk(string id, string name, int intell, float dmgAdd, float dmgMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
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
    }
}
