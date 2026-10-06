using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechTraits
    {
        public static string GroupIcon(string groupId)
        {
            switch (groupId)
            {
                case "sm_ranks":          return "ui/Icons/actor_traits/iconBlessing";
                case "sm_classes":        return "ui/Icons/actor_traits/iconGenius";
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

        public const string ClassPsi     = "sm_class_psi";
        public const string ClassMartial = "sm_class_martial";
        public const string ClassMech    = "sm_class_mech";
        public const string ClassMage    = "sm_class_mage";
        public const string ClassMind    = "sm_class_mind";
        public const string Descendant   = "sm_player"; // 降临者（玩家）特质——v0.76.54 与自动降临统一为单一 trait（sm_descendant 已合并删除）


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
                if (r.aoe > 0f) t.base_stats["area_of_effect"] = r.aoe;
                if (r.targets > 0f) t.base_stats["targets"] = r.targets;
                if (r.range > 0f) t.base_stats["range"] = r.range;
                t.rarity = rankIdx <= 3 ? Rarity.R0_Normal
                          : rankIdx <= 8 ? Rarity.R1_Rare
                          : rankIdx <= 11 ? Rarity.R2_Epic : Rarity.R3_Legendary;
                if (rankIdx >= 4) t.addCombatAction("combat_dodge");
                if (rankIdx >= 6) t.addCombatAction("combat_block");
                if (rankIdx >= 8) { t.addCombatAction("combat_dash"); t.addCombatAction("combat_backstep"); }
                if (rankIdx >= 10) { t.addCombatAction("combat_instincts"); t.addCombatAction("combat_deflect_projectile"); }
                if (rankIdx >= 12) { t.addCombatAction("combat_attack_range"); t.addCombatAction("combat_cast_spell"); }
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

            // 降临者（玩家）特质：经验值加成+重生（原著：玩家靠经验值升级）
            var descendant = new ActorTrait
            {
                id = Descendant, path_icon = "ui/Icons/actor_traits/iconGenius", group_id = "sm_descendant",
                needs_to_be_explored = false, rate_inherit = 0, rate_birth = 0,
                rarity = Rarity.R2_Epic, base_stats = new BaseStats()
            };
            descendant.base_stats["experience"] = 2.0f; // 经验值获取翻倍
            descendant.base_stats["intelligence"] = 2;
            LocalizedTextManager.add("trait_" + Descendant, LocalizedTextManager.getText("sm_player_name"), pReplace: true);
            LocalizedTextManager.add("trait_" + Descendant + "_info", LocalizedTextManager.getText("sm_player_desc"), pReplace: true);
            AssetManager.traits.add(descendant);

            // 韩萧化身特质：唯一，前世记忆+更高属性（v0.76.83）
            var hanxiao = new ActorTrait
            {
                id = "sm_player_hanxiao", path_icon = "ui/Icons/actor_traits/iconGenius", group_id = "sm_descendant",
                needs_to_be_explored = false, rate_inherit = 0, rate_birth = 0,
                rarity = Rarity.R3_Legendary, base_stats = new BaseStats()
            };
            hanxiao.base_stats["experience"] = 3.0f; // 经验值获取三倍
            hanxiao.base_stats["intelligence"] = 5;
            hanxiao.base_stats["warfare"] = 3;
            LocalizedTextManager.add("trait_sm_player_hanxiao", "韩萧化身", pReplace: true);
            LocalizedTextManager.add("trait_sm_player_hanxiao_info", "超脱者韩萧投放的化身，保留前世记忆，机械系天赋极高", pReplace: true);
            AssetManager.traits.add(hanxiao);
        }

        private static void AddClassTrait(string id, string name, int intell, int str, int stam,
            System.Collections.Generic.Dictionary<string, float> extraStats = null)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconGenius", group_id = "sm_classes",
                needs_to_be_explored = false,
                rate_inherit = 20,
                rate_birth = 3,
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
                case ClassMartial:
                    t.addCombatAction("combat_dash");
                    t.addCombatAction("combat_block");
                    t.addCombatAction("combat_dodge");
                    t.addCombatAction("combat_backstep");
                    break;
                case ClassMech:
                    t.addCombatAction("combat_deflect_projectile");
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_block");
                    break;
                case ClassPsi:
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_cast_spell");
                    t.addCombatAction("combat_dodge");
                    break;
                case ClassMage:
                    t.addCombatAction("combat_cast_spell");
                    t.addCombatAction("combat_attack_range");
                    t.addCombatAction("combat_backstep");
                    break;
                case ClassMind:
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
                "sm_rank_desc_00",
                "sm_rank_desc_01",
                "sm_rank_desc_02",
                "sm_rank_desc_03",
                "sm_rank_desc_04",
                "sm_rank_desc_05",
                "sm_rank_desc_06",
                "sm_rank_desc_07",
                "sm_rank_desc_08",
                "sm_rank_desc_09",
                "sm_rank_desc_10",
                "sm_rank_desc_11",
                "sm_rank_desc_12",
                "sm_rank_desc_13"
            };
            string key = idx >= 0 && idx < descs.Length ? descs[idx] : "sm_rank_desc_unknown";
            return LocalizedTextManager.getText(key);
        }
    }
}
