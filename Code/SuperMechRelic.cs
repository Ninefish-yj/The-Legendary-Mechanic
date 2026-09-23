using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 宇宙宝物/装备品质系统（原著）：
    /// 品质从低到高：劣质灰→普通白→良好绿→优质蓝→极佳紫→珍稀粉→传说橙→神器红→金装（宇宙宝物）
    /// 银装带"传承"属性。
    /// 实现：每个品质一个特质，给不同倍率的 damage/health 加成。
    /// </summary>
    public static class SuperMechRelic
    {
        // 品质从低到高
        public const string QGray   = "sm_relic_gray";    // 劣质灰
        public const string QWhite   = "sm_relic_white";   // 普通白
        public const string QGreen   = "sm_relic_green";   // 良好绿
        public const string QBlue    = "sm_relic_blue";    // 优质蓝
        public const string QPurple  = "sm_relic_purple";   // 极佳紫
        public const string QPink    = "sm_relic_pink";    // 珍稀粉
        public const string QOrange  = "sm_relic_orange";  // 传说橙
        public const string QRed     = "sm_relic_red";     // 神器红
        public const string QGold    = "sm_relic_gold";    // 金装（宇宙宝物）

        public static void Register()
        {
            AddRelic(QGray,   "劣质灰装", 0.8f, 0.8f);
            AddRelic(QWhite,  "普通白装", 1.0f, 1.0f);
            AddRelic(QGreen,  "良好绿装", 1.3f, 1.2f);
            AddRelic(QBlue,   "优质蓝装", 1.7f, 1.5f);
            AddRelic(QPurple, "极佳紫装", 2.2f, 2.0f);
            AddRelic(QPink,   "珍稀粉装", 3.0f, 2.8f);
            AddRelic(QOrange, "传说橙装", 4.5f, 4.0f);
            AddRelic(QRed,    "神器红装", 7.0f, 6.0f);
            AddRelic(QGold,   "金装（宇宙宝物）", 15.0f, 12.0f);

            // 注册"赐予宇宙宝物"神权
            var givePower = new GodPower
            {
                id = "sm_give_relic_gold",
                name = "赐予宇宙宝物（金装）",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            givePower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { a.addTrait(QGold); });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_relic_gold", "赐予宇宙宝物（金装）", pReplace: true);

            Debug.Log("[超神机械师] 宇宙宝物系统注册完成：9级品质");
        }

        private static void AddRelic(string id, string name, float dmgMul, float hpMul)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info",
                $"装备品质：{name}。伤害×{dmgMul}，生命×{hpMul}。", pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_relic",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["multiplier_damage"] = dmgMul;
            t.base_stats["multiplier_health"] = hpMul;
            AssetManager.traits.add(t);
        }
    }
}
