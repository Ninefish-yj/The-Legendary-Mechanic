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
            foreach (var r in SuperMechRanks.All)
            {
                if (SuperMechRanks.IsPlusRank(System.Array.IndexOf(SuperMechRanks.All.ToArray(), r))) continue;
                var t = new ActorTrait
                {
                    id = r.id,
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = "sm_ranks",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                // 阶位特质只做标记，属性加成由晋升系统按精确阶位（含+位）反射施加
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
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_classes",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            t.base_stats["warfare"] = str;
            t.base_stats["stamina"] = stam;
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
    }
}
