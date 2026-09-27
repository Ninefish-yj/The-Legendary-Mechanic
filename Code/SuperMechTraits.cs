using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    // 特质系统：只注册必要特质，知识/气力用内部字典

    public static class SuperMechTraits
    {
        public static string GroupIcon(string groupId)
        {
            switch (groupId)
            {
                case "sm_ranks":          return "ui/Icons/actor_traits/iconBlessing";
                case "sm_classes":        return "ui/Icons/actor_traits/iconGenius";
                case "sm_branches":       return "ui/Icons/actor_traits/iconArcaneReflexes";
                case "sm_perks":          return "ui/Icons/actor_traits/iconBattleReflexes";
                case "sm_gene":           return "ui/Icons/actor_traits/iconAcidBlood";
                case "sm_mana":           return "ui/Icons/actor_traits/iconArcaneReflexes";
                case "sm_mind":           return "ui/Icons/actor_traits/iconColdAura";
                case "sm_refinement":     return "ui/Icons/actor_traits/iconFireBlood";
                case "sm_relic":          return "ui/Icons/actor_traits/iconBlessing";
                case "sm_cosmic_relic":   return "ui/Icons/actor_traits/iconChosenOne";
                case "sm_mage_tower":     return "ui/Icons/actor_traits/iconArcaneReflexes";
                case "sm_awakened":       return "ui/Icons/actor_traits/iconClone";
                case "sm_specialty":      return "ui/Icons/actor_traits/iconDragonslayer";
                case "sm_rank_specialty": return "ui/Icons/actor_traits/iconGiant";
                case "sm_subclass":       return "ui/Icons/actor_traits/iconAmbitious";
                case "sm_race":           return "ui/Icons/actor_traits/iconGiant";
                case "sm_skills":         return "ui/Icons/actor_traits/iconBattleReflexes";
                case "sm_sanctuary":      return "ui/Icons/actor_traits/iconBlessing";
                default:                  return "ui/Icons/actor_traits/iconHardSkin";
            }
        }

        public const string ClassPsi     = "sm_class_psi";      // 异能系（基因树·神通）
        public const string ClassMartial = "sm_class_martial";  // 武道系（御气技巧树·神体）
        public const string ClassMech    = "sm_class_mech";     // 机械系（机械知识树·神器）
        public const string ClassMage    = "sm_class_mage";     // 魔法系（魔法知识树·神权）
        public const string ClassMind    = "sm_class_mind";     // 念力系（精神修炼树·神魂）


        public const string SkillQiMod          = "sm_skill_qimod";
        public const string SkillVirtualPurify  = "sm_skill_virtual_purify";
        public const string SkillDimensionMarch = "sm_skill_dimension_march";

        public static void Register()
        {
            int rankIdx = 0;
            foreach (var r in SuperMechRanks.All)
            {
                if (SuperMechRanks.IsPlusRank(rankIdx)) { rankIdx++; continue; }
                var t = new ActorTrait
                {
                    id = r.id,
                    path_icon = "ui/Icons/actor_traits/iconBlessing",
                    group_id = "sm_ranks",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                if (r.damageMul > 1f) t.base_stats["multiplier_damage"] = r.damageMul;
                if (r.healthMul > 1f) t.base_stats["multiplier_health"] = r.healthMul;
                if (rankIdx >= 4) t.addCombatAction("combat_dodge");              // C阶+
                if (rankIdx >= 6) t.addCombatAction("combat_block");              // B阶+
                if (rankIdx >= 8) { t.addCombatAction("combat_dash"); t.addCombatAction("combat_backstep"); } // A阶+
                if (rankIdx >= 10) { t.addCombatAction("combat_instincts"); t.addCombatAction("combat_deflect_projectile"); } // S阶+
                if (rankIdx >= 12) { t.addCombatAction("combat_attack_range"); t.addCombatAction("combat_cast_spell"); } // SS阶+
                rankIdx++;
                LocalizedTextManager.add("trait_" + r.id, LocalizedTextManager.getText(r.name), pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info", GetRankDesc(rankIdx - 1), pReplace: true);
                AssetManager.traits.add(t);
            }

            AddClassTrait(ClassPsi,    "sm_trait_class_psi", 3, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"damage", 5f}, {"multiplier_damage", 1.05f}, {"mana", 30f} });
            AddClassTrait(ClassMartial, "sm_trait_class_martial", 0, 3, 2,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"damage", 10f}, {"attack_speed", 1.10f}, {"armor", 5f} });
            AddClassTrait(ClassMech,   "sm_trait_class_mech", 2, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"experience", 1.10f}, {"attack_speed", 1.05f} });
            AddClassTrait(ClassMage,   "sm_trait_class_mage", 4, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"mana", 50f}, {"multiplier_damage", 1.05f}, {"intelligence", 1f} });
            AddClassTrait(ClassMind,   "sm_trait_class_mind", 3, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"mana", 40f}, {"multiplier_speed", 1.05f}, {"attack_speed", 1.03f} });


            Debug.Log("[超神机械师] 特质注册完成：阶位9(主阶位) + 五系觉醒5");
        }

        private static void AddClassTrait(string id, string name, int intell, int str, int stam,
            System.Collections.Generic.Dictionary<string, float> extraStats = null)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconGenius", group_id = "sm_classes",
                needs_to_be_explored = false,
                rate_inherit = 20,  // 原著：超能者后代更高概率觉醒
                rate_birth = 3,     // 3%概率出生自带（自然觉醒）
                base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            t.base_stats["warfare"] = str;
            t.base_stats["stamina"] = stam;
            if (extraStats != null)
            {
                foreach (var kv in extraStats)
                {
                    t.base_stats[kv.Key] = kv.Value;
                }
            }
            switch (id)
            {
                case ClassMartial: // 武道系：近战格斗大师
                    t.addCombatAction("combat_dash");
                    t.addCombatAction("combat_block");
                    t.addCombatAction("combat_dodge");
                    t.addCombatAction("combat_backstep");
                    break;
                case ClassMech:    // 机械系：机甲远程+偏转弹道
                    t.addCombatAction("combat_deflect_projectile");
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_block");
                    break;
                case ClassPsi:     // 异能系：异能远程+施法
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_cast_spell");
                    t.addCombatAction("combat_dodge");
                    break;
                case ClassMage:    // 魔法系：施法为主
                    t.addCombatAction("combat_cast_spell");
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_backstep");
                    break;
                case ClassMind:    // 念力系：精神感应+闪避
                    t.addCombatAction("combat_instincts");
                    t.addCombatAction("combat_dodge");
                    t.addCombatAction("combat_random_jump");
                    break;
            }
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(name) + LocalizedTextManager.getText("sm_ui_talent"), pReplace: true);
            AssetManager.traits.add(t);
        }

        private static string GetRankDesc(int idx)
        {
            string[] descs = {
                "sm_rank_desc_00",      // F
                "sm_rank_desc_01",          // E
                "sm_rank_desc_02",            // D
                "sm_rank_desc_03",                   // D+
                "sm_rank_desc_04",  // C
                "sm_rank_desc_05",                   // C+
                "sm_rank_desc_06",               // B
                "sm_rank_desc_07",                   // B+
                "sm_rank_desc_08",      // A
                "sm_rank_desc_09",                   // A+
                "sm_rank_desc_10",      // S
                "sm_rank_desc_11",                    // S+
                "sm_rank_desc_12",  // SS
                "sm_rank_desc_13"                      // X
            };
            string key = idx >= 0 && idx < descs.Length ? descs[idx] : "sm_rank_desc_unknown";
            return LocalizedTextManager.getText(key);
        }
    }
}
