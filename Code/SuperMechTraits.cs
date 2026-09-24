using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 特质注册：阶位链 + 五系觉醒 + 五系分支 + 机械系完整阶段链 + 最终形态。
    /// 结构来源：原著 ch3/ch48/ch237/ch362/ch477/ch611/ch621/ch1022/ch1082 + 抖音百科/同人二创整理。
    /// </summary>
    public static class SuperMechTraits
    {
        // —— 阶位 id 常量（与 SuperMechRanks 对齐）——
        public const string RankF  = "sm_rank_f";
        public const string RankE  = "sm_rank_e";
        public const string RankD  = "sm_rank_d";
        public const string RankC  = "sm_rank_c";
        public const string RankB  = "sm_rank_b";
        public const string RankA  = "sm_rank_a";
        public const string RankS  = "sm_rank_s";

        // —— 五系觉醒（百科：异能=神通/武道=神体/魔法=神权/念力=神魂/机械=神器）——
        public const string ClassPsi    = "sm_class_psi";     // 异能系（基因树）
        public const string ClassMartial = "sm_class_martial"; // 武道系（御气技巧树）
        public const string ClassMech    = "sm_class_mech";   // 机械系（机械知识树）
        public const string ClassMage   = "sm_class_mage";    // 魔法系（魔法知识树）
        public const string ClassMind    = "sm_class_mind";    // 念力系（精神修炼树）

        // —— 机械系早期三路线（ch48：枪炮师/机械师/械武者）——
        public const string RouteGunner = "sm_route_gunner";
        public const string RouteMech   = "sm_route_mech";
        public const string RouteMartialMech = "sm_route_mech_martial";

        // —— 机械系共用前 4 阶（原著最终面板：机械爱好者→机械师学徒→见习机械师→磁环机械师）——
        public const string MechInitiate  = "sm_mech_initiate";
        public const string MechApprentice = "sm_mech_apprentice";
        public const string MechTrainee  = "sm_mech_trainee";
        public const string MechMagnet   = "sm_mech_magnet";

        // —— 虚拟系阶段链（原著最终面板14阶完整链）——
        public const string MechData     = "sm_mech_data";
        public const string MechWar      = "sm_mech_war";
        public const string MechVirtual  = "sm_mech_virtual";
        public const string MechStarsea = "sm_mech_starsea";
        public const string MechTruth    = "sm_mech_truth";
        public const string MechApostle  = "sm_mech_apostle";
        public const string MechEmperor  = "sm_mech_emperor";
        public const string MechLord     = "sm_mech_lord";
        public const string MechGod      = "sm_mech_god";
        public const string MechSupreme  = "sm_mech_supreme";  // 超神机械师（最终阶，原著面板）

        // —— 武装系同阶（ch621 重装机械师）——
        public const string MechWeaponHeavy = "sm_mech_weapon_heavy";
        // —— 能量系同阶（占位）——
        public const string MechEnergyCore  = "sm_mech_energy_core";

        // —— 神座后三分支最终形态（百科）——
        public const string FinalSupreme = "sm_final_supreme";  // 虚拟：至高天尊
        public const string FinalCosmic = "sm_final_cosmic";   // 武装：宇宙帝皇
        public const string FinalOrigin  = "sm_final_origin";   // 能量：起源神君

        // —— 种族进化链（原著最终面板：人类→虚空潜影者→...→虚空神系·王族血脉）——
        public const string RaceVoidShadow   = "sm_race_void_shadow";    // 虚空潜影者
        public const string RaceChaosObserver = "sm_race_chaos_observer"; // 混沌观察者
        public const string RaceVoidWarp     = "sm_race_void_warp";      // 虚空扭曲者
        public const string RaceVoidStar     = "sm_race_void_star";      // 虚空逐星者
        public const string RaceVoidGuide    = "sm_race_void_guide";     // 虚空引渡者
        public const string RaceVoidApostle  = "sm_race_void_apostle";   // 虚空使徒
        public const string RaceVoidGod      = "sm_race_void_god";       // 虚空神族
        public const string RaceVoidRoyal    = "sm_race_void_royal";     // 虚空神系·王族血脉

        // —— 职业技能（原著最终面板）——
        public const string SkillQiMod       = "sm_skill_qimod";        // 气力改装·LVMAX
        public const string SkillVirtualPurify = "sm_skill_virtual_purify"; // 虚拟净化复原·LVMAX
        public const string SkillDimensionMarch = "sm_skill_dimension_march"; // 次级维度行军·LVMAX

        // —— 机械系三分支 ——
        public const string BranchWeapon = "sm_branch_weapon";
        public const string BranchEnergy = "sm_branch_energy";
        public const string BranchVirtual = "sm_branch_virtual";

        // —— 异能系分支（基因链方向）——
        public const string BranchGeneCombat = "sm_gene_combat";
        public const string BranchGeneControl = "sm_gene_control";
        // —— 武道系分支（百科：敏捷/力量/防御路线）——
        public const string BranchMartialAgile = "sm_martial_agile";
        public const string BranchMartialPower = "sm_martial_power";
        public const string BranchMartialDefense = "sm_martial_defense";
        // —— 魔法系分支（百科：专精/魔网）——
        public const string BranchMageSpecialized = "sm_mage_spec";
        public const string BranchMageWeb = "sm_mage_web";
        // —— 念力系分支 ——
        public const string BranchMindPsychic = "sm_mind_psychic";
        public const string BranchMindIllusion = "sm_mind_illusion";

        // —— 念力系职业阶段（同人整理）——
        public const string MindBeginner = "sm_mind_beginner";
        public const string MindMover    = "sm_mind_mover";
        public const string MindActive   = "sm_mind_active";
        public const string MindChampion = "sm_mind_champion";
        public const string MindDreamer  = "sm_mind_dreamer";
        public const string MindDesigner = "sm_mind_designer";
        public const string MindCreator = "sm_mind_creator";

        // —— 魔法系职业阶段（同人整理）——
        public const string MageBeginner = "sm_mage_beginner";
        public const string MageElemental = "sm_mage_elemental";
        public const string MageOccult   = "sm_mage_occult";
        public const string MageKnight   = "sm_mage_knight";
        public const string MageBattle   = "sm_mage_battle";

        public static void Register()
        {
            // 1. 阶位链
            foreach (var r in SuperMechRanks.All)
            {
                var t = new ActorTrait
                {
                    id = r.id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_ranks",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = r.damageMul;
                t.base_stats["multiplier_health"] = r.healthMul;
                AssetManager.traits.add(t);
            }

            // 2. 五系觉醒
            AddClassTrait(ClassPsi,    "异能系觉醒（基因树·神通）", 3, 0, 0);
            AddClassTrait(ClassMartial, "武道系觉醒（御气技巧树·神体）", 0, 3, 2);
            AddClassTrait(ClassMech,   "机械系觉醒（机械知识树·神器）", 2, 0, 0);
            AddClassTrait(ClassMage,   "魔法系觉醒（魔法知识树·神权）", 4, 0, 0);
            AddClassTrait(ClassMind,   "念力系觉醒（精神修炼树·神魂）", 3, 0, 0);

            // 3. 机械系共用前 4 阶（原著面板：机械爱好者→机械师学徒→见习机械师→磁环机械师）
            AddMechStage(MechInitiate,  "机械爱好者", 1, "sm_mech_shared");
            AddMechStage(MechApprentice, "机械师学徒", 2, "sm_mech_shared");
            AddMechStage(MechTrainee,   "见习机械师", 3, "sm_mech_shared");
            AddMechStage(MechMagnet,    "磁环机械师（械力觉醒）", 5, "sm_mech_shared");

            // 4. 磁环阶段三路线选择（ch48）
            AddMechStage(RouteGunner,    "枪炮师（远程重炮）", 3, "sm_mech_routes");
            AddMechStage(RouteMech,       "机械师（造机为主）", 3, "sm_mech_routes");
            AddMechStage(RouteMartialMech, "械武者（机体近战）", 3, "sm_mech_routes");

            // 5. 虚拟系阶段链（原著面板14阶：数据→战争→虚拟→星海→真理→使徒→帝皇→主宰→神座→超神机械师）
            AddMechStage(MechData,      "数据机械师（虚拟系）", 7, "sm_mech_virtual");
            AddMechStage(MechWar,       "战争机械师（虚拟系）", 9, "sm_mech_virtual");
            AddMechStage(MechVirtual,   "虚拟机械师", 11, "sm_mech_virtual");
            AddMechStage(MechStarsea,   "星海机械师", 14, "sm_mech_virtual");
            AddMechStage(MechTruth,     "真理机械师", 18, "sm_mech_virtual");
            AddMechStage(MechApostle,   "使徒机械师", 23, "sm_mech_virtual");
            AddMechStage(MechEmperor,   "帝皇机械师", 30, "sm_mech_virtual");
            AddMechStage(MechLord,      "主宰机械师", 40, "sm_mech_virtual");
            AddMechStage(MechGod,       "神座机械师", 60, "sm_mech_virtual");
            AddMechStage(MechSupreme,   "超神机械师（最终阶）", 100, "sm_mech_virtual");
            AddMechStage(FinalSupreme, "至高天尊（虚拟系·超神）", 80, "sm_mech_virtual");

            // 6. 武装系阶段
            AddMechStage(MechWeaponHeavy, "重装机械师（武装系）", 11, "sm_mech_weapon");
            AddMechStage(FinalCosmic,  "宇宙帝皇（武装系·超神）", 80, "sm_mech_weapon");

            // 7. 能量系阶段
            AddMechStage(MechEnergyCore,  "能量核心机械师（能量系）", 11, "sm_mech_energy");
            AddMechStage(FinalOrigin,  "起源神君（能量系·超神）", 80, "sm_mech_energy");

            // 8. 种族进化链（原著最终面板：8阶进化）
            AddRaceStage(RaceVoidShadow,    "虚空潜影者", 2, 1.1f);
            AddRaceStage(RaceChaosObserver, "混沌观察者", 3, 1.15f);
            AddRaceStage(RaceVoidWarp,      "虚空扭曲者", 4, 1.2f);
            AddRaceStage(RaceVoidStar,      "虚空逐星者", 5, 1.25f);
            AddRaceStage(RaceVoidGuide,     "虚空引渡者", 6, 1.3f);
            AddRaceStage(RaceVoidApostle,   "虚空使徒", 8, 1.4f);
            AddRaceStage(RaceVoidGod,       "虚空神族", 10, 1.5f);
            AddRaceStage(RaceVoidRoyal,     "虚空神系·王族血脉", 15, 1.8f);

            // 9. 职业技能（原著最终面板）
            AddSkillTrait(SkillQiMod,        "气力改装·LVMAX", "气力数值按比例增加制造机械的效率与品质", 5, 1.1f);
            AddSkillTrait(SkillVirtualPurify, "虚拟净化复原·LVMAX", "净化病毒感染的智能目标", 3, 1.05f);
            AddSkillTrait(SkillDimensionMarch, "次级维度行军·LVMAX", "打开黑色传送门进行维度行军", 8, 1.2f);

            // 8. 机械系三分支
            AddBranch(BranchWeapon, "武装分支：实装火力", 1.3f, 1.0f);
            AddBranch(BranchEnergy, "能量分支：能量武器", 1.0f, 1.3f);
            AddBranch(BranchVirtual, "虚拟分支：无人机/虚拟单位", 1.1f, 1.2f);

            // 9. 异能系分支
            AddBranch(BranchGeneCombat, "异能系·战斗基因链", 1.2f, 1.0f);
            AddBranch(BranchGeneControl, "异能系·操控基因链", 1.0f, 1.1f);
            // 10. 武道系分支
            AddBranch(BranchMartialAgile, "武道系·敏捷流", 1.1f, 1.0f);
            AddBranch(BranchMartialPower, "武道系·力量流", 1.3f, 1.0f);
            AddBranch(BranchMartialDefense, "武道系·防御流", 1.0f, 1.4f);
            // 11. 魔法系分支
            AddBranch(BranchMageSpecialized, "魔法系·专精法师", 1.25f, 1.0f);
            AddBranch(BranchMageWeb, "魔法系·魔网法师", 1.0f, 1.1f);
            // 12. 念力系分支
            AddBranch(BranchMindPsychic, "念力系·念动力", 1.15f, 1.0f);
            AddBranch(BranchMindIllusion, "念力系·幻术", 1.0f, 1.1f);

            // 13. 念力系职业阶段（同人整理）——归入 sm_mind 组
            AddMechStage(MindBeginner, "初阶念力师", 2, "sm_mind");
            AddMechStage(MindMover,   "转运者", 5, "sm_mind");
            AddMechStage(MindActive,  "活动家", 9, "sm_mind");
            AddMechStage(MindChampion,"常胜者", 15, "sm_mind");
            AddMechStage(MindDreamer, "梦想家", 25, "sm_mind");
            AddMechStage(MindDesigner,"设计师", 40, "sm_mind");
            AddMechStage(MindCreator, "创造师", 60, "sm_mind");

            // 14. 魔法系职业阶段（同人整理）——归入 sm_mana 组
            AddMechStage(MageBeginner,  "初阶魔法师", 2, "sm_mana");
            AddMechStage(MageElemental,"元素法师", 6, "sm_mana");
            AddMechStage(MageOccult,   "灵异法师", 12, "sm_mana");
            AddMechStage(MageKnight,   "魔法骑士", 20, "sm_mana");
            AddMechStage(MageBattle,   "战斗法师", 20, "sm_mana");

            Debug.Log("[超神机械师] 特质注册完成：阶位14 + 五系觉醒5 + 机械链21 + 全分支12 + 念力7 + 魔法5 + 种族进化8 + 职业技能3");
        }

        private static void AddRaceStage(string id, string name, int statBonus, float healthMul)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_race",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["warfare"] = statBonus;
            t.base_stats["multiplier_health"] = healthMul;
            t.base_stats["multiplier_damage"] = 1f + statBonus * 0.03f;
            AssetManager.traits.add(t);
        }

        private static void AddSkillTrait(string id, string name, string desc, int intell, float dmgMul)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_skills",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            t.base_stats["multiplier_damage"] = dmgMul;
            AssetManager.traits.add(t);
        }

        private static void AddClassTrait(string id, string name, int intell, int str, int stam)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_classes",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            t.base_stats["warfare"] = str;
            t.base_stats["stamina"] = stam;
            AssetManager.traits.add(t);
        }

        private static void AddMechStage(string id, string name, int tier, string groupId = "sm_mech_shared")
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = groupId,
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            // 原著转职奖励（ch3/ch50/ch237/ch362/ch477/ch539/ch684/ch717/ch770/ch890/ch957/ch1039）：
            // 每次转职给气力+属性点，阶位越高给的越多。tier=1(机械爱好者)→tier=14(超神机械师)。
            t.base_stats["intelligence"] = 2f * tier;               // 智力（机械系主属性）
            t.base_stats["damage"] = 3f * tier;                     // 力量→伤害
            t.base_stats["health"] = 20f * tier;                    // 耐力→生命
            t.base_stats["stamina"] = 15f * tier;                   // 耐力→体力
            t.base_stats["speed"] = 0.1f * tier;                    // 敏捷
            t.base_stats["armor"] = 0.5f * tier;                    // 耐力→护甲
            t.base_stats["warfare"] = tier;                         // 战斗技能
            t.base_stats["multiplier_damage"] = 1f + tier * 0.05f;  // 机械威力
            t.base_stats["multiplier_health"] = 1f + tier * 0.08f;  // 生存能力
            t.base_stats["experience"] = 1f + tier * 0.02f;         // 制造/经验获取
            if (tier >= 5)  // 磁环后解锁械力，额外暴击和攻速
            {
                t.base_stats["critical_chance"] = (tier - 4) * 0.01f;
                t.base_stats["attack_speed"] = (tier - 4) * 0.03f;
            }
            AssetManager.traits.add(t);
        }

        private static void AddBranch(string id, string name, float dmgMul, float healthMul)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_branches",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["multiplier_damage"] = dmgMul;
            t.base_stats["multiplier_health"] = healthMul;
            AssetManager.traits.add(t);
        }

        private static void AddPerk(string id, string name, int intell, float dmgAdd, float dmgMul)
        {
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_perks",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgAdd > 0) t.base_stats["damage"] = dmgAdd;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            AssetManager.traits.add(t);
        }
    }
}
