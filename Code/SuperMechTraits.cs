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
                case "sm_cultivation":    return "ui/Icons/actor_traits/iconBoostedVitality";
                case "sm_know_mech":      return "ui/Icons/actor_traits/iconGenius";
                case "sm_know_martial":   return "ui/Icons/actor_traits/iconBattleReflexes";
                case "sm_know_mage":      return "ui/Icons/actor_traits/iconArcaneReflexes";
                case "sm_know_mind":      return "ui/Icons/actor_traits/iconColdAura";
                case "sm_know_psi":       return "ui/Icons/actor_traits/iconAcidBlood";
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
            // 1. 阶位链（只注册主阶位，+位不挂特质只在面板显示；属性加成由SuperMechAdvancement统一反射施加）
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
                // 阶位越高战斗动作越多（原著：高阶超能者战斗技巧更丰富）
                if (rankIdx >= 4) t.addCombatAction("combat_dodge");              // C阶+
                if (rankIdx >= 6) t.addCombatAction("combat_block");              // B阶+
                if (rankIdx >= 8) { t.addCombatAction("combat_dash"); t.addCombatAction("combat_backstep"); } // A阶+
                if (rankIdx >= 10) { t.addCombatAction("combat_instincts"); t.addCombatAction("combat_deflect_projectile"); } // S阶+
                if (rankIdx >= 12) { t.addCombatAction("combat_attack_range"); t.addCombatAction("combat_cast_spell"); } // SS阶+
                rankIdx++;
                AssetManager.traits.add(t);
            }

            // 2. 五系觉醒
            AddClassTrait(ClassPsi,    "异能系觉醒（基因树·神通）", 3, 0, 0);
            AddClassTrait(ClassMartial, "武道系觉醒（御气技巧树·神体）", 0, 3, 2);
            AddClassTrait(ClassMech,   "机械系觉醒（机械知识树·神器）", 2, 0, 0);
            AddClassTrait(ClassMage,   "魔法系觉醒（魔法知识树·神权）", 4, 0, 0);
            AddClassTrait(ClassMind,   "念力系觉醒（精神修炼树·神魂）", 3, 0, 0);

            // 3. 种族进化移至 SuperMechRace.cs（原著6阶段，与阶位挂钩自动进化）

            // 4. 职业技能
            AddSkillTrait(SkillQiMod,        "气力改装·LVMAX", "气力数值按比例增加制造机械的效率与品质", 5, 1.1f);
            AddSkillTrait(SkillVirtualPurify, "虚拟净化复原·LVMAX", "净化病毒感染的智能目标", 3, 1.05f);
            AddSkillTrait(SkillDimensionMarch, "次级维度行军·LVMAX", "打开黑色传送门进行维度行军", 8, 1.2f);

            Debug.Log("[超神机械师] 特质注册完成：阶位9(主阶位) + 五系觉醒5 + 职业技能3");
        }

        private static void AddClassTrait(string id, string name, int intell, int str, int stam)
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
