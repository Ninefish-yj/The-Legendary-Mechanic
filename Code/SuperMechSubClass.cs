using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 副职业系统（原著 ch233）：主职业之外的第二职业槽。
    /// 每个单位可同时拥有主职业 + 一个副职业。
    /// 副职业提供独特被动/主动能力，和主职业不冲突。
    /// 原著代表：韩萧副职业=特工lv10 + 黑夜潜行者lv10。
    /// </summary>
    public static class SuperMechSubClass
    {
        // 副职业列表
        public const string SubAgent       = "sm_sub_agent";       // 特工
        public const string SubNinja      = "sm_sub_ninja";       // 黑夜潜行者
        public const string SubHacker     = "sm_sub_hacker";      // 黑客
        public const string SubMerchant  = "sm_sub_merchant";    // 商人
        public const string SubDoctor     = "sm_sub_doctor";      // 医生
        public const string SubEngineer  = "sm_sub_engineer";    // 工程师
        public const string SubScribe     = "sm_sub_scribe";      // 学者
        public const string SubScout      = "sm_sub_scout";       // 侦察兵

        public static void Register()
        {
            AddSubClass(SubAgent,     "特工",     0, 0, 0.05f,  "情报与暗杀专精，近战伤害提升。");
            AddSubClass(SubNinja,    "黑夜潜行者", 0, 0, 0.08f,  "夜间作战，隐蔽与暴击。");
            AddSubClass(SubHacker,   "黑客",     5, 0, 0f,       "电子战专精，智力提升。");
            AddSubClass(SubMerchant, "商人",     2, 0, 0f,       "交易与资源经营。");
            AddSubClass(SubDoctor,   "医生",     3, 0, 0f,       "治疗与急救。");
            AddSubClass(SubEngineer, "工程师",   4, 0, 0.05f,    "制造与维护。");
            AddSubClass(SubScribe,   "学者",     6, 0, 0f,       "知识研究，智力提升。");
            AddSubClass(SubScout,    "侦察兵",   0, 2, 0.03f,    "侦察与追踪。");

            // 注册副职业赋予神权
            AddGivePower(SubAgent,     "赋予副职业：特工");
            AddGivePower(SubNinja,    "赋予副职业：黑夜潜行者");
            AddGivePower(SubHacker,   "赋予副职业：黑客");
            AddGivePower(SubMerchant, "赋予副职业：商人");

            Debug.Log("[超神机械师] 副职业系统注册完成：8个副职业");
        }

        private static void AddSubClass(string id, string name, int intell, int dmgAdd, float dmgMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_subclass",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgAdd > 0) t.base_stats["damage"] = dmgAdd;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            AssetManager.traits.add(t);
        }

        private static void AddGivePower(string traitId, string name)
        {
            var p = new GodPower
            {
                id = "sm_give_" + traitId,
                name = name,
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { a.addTrait(traitId); });
                return true;
            };
            AssetManager.powers.add(p);
            LocalizedTextManager.add("power_sm_give_" + traitId, name, pReplace: true);
        }
    }
}
