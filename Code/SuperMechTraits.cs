using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 特质注册：只注册真正的"特质"——阶位、五系觉醒、种族进化、职业技能。
    /// 职业阶段（机械14阶段）改为内部数据（SuperMechStage），不做特质。
    /// 分支选择由 SuperMechBranch 注册。
    /// 专长/副职业/宝物/提炼法各自文件注册。
    /// </summary>
    public static class SuperMechTraits
    {
        /// <summary>按特质组返回对应图标（避免全用默认iconHardSkin）。</summary>
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

        // —— 五系觉醒（真正的特质，决定单位属于哪一系）——
        public const string ClassPsi     = "sm_class_psi";      // 异能系（基因树·神通）
        public const string ClassMartial = "sm_class_martial";  // 武道系（御气技巧树·神体）
        public const string ClassMech    = "sm_class_mech";     // 机械系（机械知识树·神器）
        public const string ClassMage    = "sm_class_mage";     // 魔法系（魔法知识树·神权）
        public const string ClassMind    = "sm_class_mind";     // 念力系（精神修炼树·神魂）

        // —— 种族进化链移至 SuperMechRace.cs（原著6阶段，与阶位挂钩）——

        // —— 职业技能（原著最终面板）——
        public const string SkillQiMod          = "sm_skill_qimod";
        public const string SkillVirtualPurify  = "sm_skill_virtual_purify";
        public const string SkillDimensionMarch = "sm_skill_dimension_march";

        public static void Register()
        {
            // 1. 阶位链（只注册主阶位，+位不挂特质只在面板显示；属性加成直接写进base_stats）
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
                // 属性加成直接写进特质base_stats（原著：阶位越高战力越强）
                if (r.damageMul > 1f) t.base_stats["multiplier_damage"] = r.damageMul;
                if (r.healthMul > 1f) t.base_stats["multiplier_health"] = r.healthMul;
                // 阶位越高战斗动作越多（原著：高阶超能者战斗技巧更丰富）
                if (rankIdx >= 4) t.addCombatAction("combat_dodge");              // C阶+
                if (rankIdx >= 6) t.addCombatAction("combat_block");              // B阶+
                if (rankIdx >= 8) { t.addCombatAction("combat_dash"); t.addCombatAction("combat_backstep"); } // A阶+
                if (rankIdx >= 10) { t.addCombatAction("combat_instincts"); t.addCombatAction("combat_deflect_projectile"); } // S阶+
                if (rankIdx >= 12) { t.addCombatAction("combat_attack_range"); t.addCombatAction("combat_cast_spell"); } // SS阶+
                rankIdx++;
                // 动态添加本地化（ID带数字前缀sm_rank_00_f，cz.json中key不带前缀，这里补全）
                LocalizedTextManager.add("trait_" + r.id, r.name, pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info", GetRankDesc(rankIdx - 1), pReplace: true);
                AssetManager.traits.add(t);
            }

            // 2. 五系天赋（原著没有"觉醒"概念，超能者自然觉醒，天赋决定系别）
            //    用原版BaseStats属性模拟原著天赋：械感=智力+经验获取，体魄=战术+耐力+攻速， etc.
            AddClassTrait(ClassPsi,    "异能潜力（基因树）", 3, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"damage", 5f}, {"multiplier_damage", 1.05f}, {"mana", 30f} });
            AddClassTrait(ClassMartial, "体魄天赋（御气技巧树）", 0, 3, 2,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"damage", 10f}, {"attack_speed", 1.10f}, {"armor", 5f} });
            AddClassTrait(ClassMech,   "械感天赋（机械知识树）", 2, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"experience", 1.10f}, {"attack_speed", 1.05f} });
            AddClassTrait(ClassMage,   "魔法天赋（魔法知识树）", 4, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"mana", 50f}, {"multiplier_damage", 1.05f}, {"intelligence", 1f} });
            AddClassTrait(ClassMind,   "精神天赋（精神修炼树）", 3, 0, 0,
                new System.Collections.Generic.Dictionary<string, float> {
                    {"mana", 40f}, {"dodge", 0.05f}, {"attack_speed", 1.03f} });

            // 3. 种族进化移至 SuperMechRace.cs（原著6阶段，与阶位挂钩自动进化）

            // 4. 职业技能
            AddSkillTrait(SkillQiMod,        "气力改装·LVMAX", "气力数值按比例增加制造机械的效率与品质", 5, 1.1f);
            AddSkillTrait(SkillVirtualPurify, "虚拟净化复原·LVMAX", "净化病毒感染的智能目标", 3, 1.05f);
            AddSkillTrait(SkillDimensionMarch, "次级维度行军·LVMAX", "打开黑色传送门进行维度行军", 8, 1.2f);

            Debug.Log("[超神机械师] 特质注册完成：阶位9(主阶位) + 五系觉醒5 + 职业技能3");
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
            // 追加专属属性（原著天赋效果，用原版BaseStats key模拟）
            if (extraStats != null)
            {
                foreach (var kv in extraStats)
                {
                    t.base_stats[kv.Key] = kv.Value;
                }
            }
            // 五系各有不同战斗风格（绑定原版ActionLibrary战斗动作）
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
            AssetManager.traits.add(t);
        }

        /// <summary>阶位描述（原著设定）。</summary>
        private static string GetRankDesc(int idx)
        {
            string[] descs = {
                "凡人，未觉醒超能。",
                "能级标准100欧纳，真正的超能者。",
                "已掌握基础能力。",
                "一方强者，可担任小队队长。",
                "星球级精英。",
                "可在行星地表掀起毁灭性灾难。",
                "超A级，宇宙顶级存在。",
                "巅峰超A级，触摸超神门槛。",
                "超神级，你即是宇宙。"
            };
            return idx >= 0 && idx < descs.Length ? descs[idx] : "高阶超能者。";
        }

        private static void AddSkillTrait(string id, string name, string desc, int intell, float dmgMul)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconBattleReflexes", group_id = "sm_skills",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            t.base_stats["multiplier_damage"] = dmgMul;
            // 职业技能绑定战斗动作
            if (id == SkillQiMod) t.addCombatAction("combat_instincts");        // 气力改装→战斗本能
            if (id == SkillVirtualPurify) t.addCombatAction("combat_cast_spell"); // 虚拟净化→施法
            if (id == SkillDimensionMarch) t.addCombatAction("combat_dash");     // 维度行军→冲刺
            AssetManager.traits.add(t);
        }
    }
}
