using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备品质系统（原著）：
    /// 品质从低到高：劣质灰→普通白→良好绿→优质蓝→极佳紫→珍稀粉→传说橙→神器红→金色（宇宙宝物级）→银橙（使徒兵器级）→宇宙奇观级
    /// 原著ch1040："金色品质，便代表着宇宙宝物级的装备"
    /// 原著ch1046：银橙色品质=使徒兵器伴生武器（幽祖战矛）
    /// 原著ch1008："最顶级的是一些无解的宇宙奇观，比如时空琥珀"，宇宙奇观>宇宙宝物
    /// 宇宙宝物为人造（秘法之殿/火核之地/万神权杖/高维天启传送器等），宇宙奇观为天然无解存在（时空琥珀等）。
    /// 战斗中概率掉落装备，品质随击杀者阶位提升。同时只能装备一个品质（高级替换低级）。
    /// </summary>
    public static class SuperMechRelic
    {
        // 品质从低到高（原著品质色）
        public const string QGray    = "sm_relic_gray";     // 劣质灰
        public const string QWhite   = "sm_relic_white";    // 普通白
        public const string QGreen   = "sm_relic_green";    // 良好绿
        public const string QBlue    = "sm_relic_blue";     // 优质蓝
        public const string QPurple  = "sm_relic_purple";   // 极佳紫
        public const string QPink    = "sm_relic_pink";     // 珍稀粉
        public const string QOrange  = "sm_relic_orange";   // 传说橙
        public const string QRed     = "sm_relic_red";      // 神器红
        public const string QGold    = "sm_relic_gold";     // 金色=宇宙宝物级（ch1040）
        public const string QSilver  = "sm_relic_silver";   // 银橙=使徒兵器级（ch1046）
        public const string QWonder  = "sm_relic_wonder";   // 宇宙奇观级（ch1008最顶级）

        public static readonly string[] QualityOrder = {
            QGray, QWhite, QGreen, QBlue, QPurple, QPink, QOrange, QRed, QGold, QSilver, QWonder
        };
        public static readonly string[] QualityNames = {
            "劣质灰", "普通白", "良好绿", "优质蓝", "极佳紫", "珍稀粉", "传说橙", "神器红",
            "金色·宇宙宝物", "银橙·使徒兵器", "宇宙奇观"
        };

        // 上次生命值（检测战斗结束）
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        // 战斗中累计时间（用于掉落判定）
        private static readonly Dictionary<long, float> _combatTime = new Dictionary<long, float>();

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
            AddRelic(QGold,   "金色·宇宙宝物级", 15.0f, 12.0f);  // ch1040：金色=宇宙宝物级
            AddRelic(QSilver, "银橙·使徒兵器级", 25.0f, 20.0f);  // ch1046：银橙=使徒兵器伴生武器
            AddRelic(QWonder, "宇宙奇观级", 50.0f, 40.0f);       // ch1008：最顶级，时空琥珀级

            // 注册"赐予宇宙宝物（金装）"神权
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
                tile.doUnits(delegate (Actor a) { EquipRelic(a, 8); }); // index 8 = 金色宇宙宝物级
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_relic_gold", "赐予宇宙宝物（金装）", pReplace: true);

            Debug.Log("[超神机械师] 装备品质系统注册完成：11级品质（灰→宇宙奇观）");
        }

        /// <summary>每tick：战斗中概率掉落宝物。</summary>
        public static void TickRelicDrops()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                bool inCombat = curHealth < lastH - 0.5f || SuperMechQi.IsInCombat(a);
                _lastHealth[a.id] = curHealth;

                if (inCombat)
                {
                    // 累计战斗时间
                    float ct;
                    _combatTime.TryGetValue(a.id, out ct);
                    _combatTime[a.id] = ct + tickInterval;
                }
                else
                {
                    // 脱离战斗：如果战斗时间超过10秒，概率掉落宝物
                    float ct;
                    if (_combatTime.TryGetValue(a.id, out ct) && ct >= 10f)
                    {
                        _combatTime[a.id] = 0f;
                        TryDropRelic(a);
                    }
                    else if (ct > 0)
                    {
                        _combatTime[a.id] = 0f;
                    }
                }
            }
        }

        /// <summary>尝试掉落宝物。品质随阶位提升。</summary>
        private static void TryDropRelic(Actor a)
        {
            // 基础掉落率10%，每阶位+2%
            int rank = SuperMechAdvancement.GetRankIndex(a);
            float dropChance = 0.1f + rank * 0.02f;
            if (Random.value > dropChance) return;

            // 品质roll：基础0-3，阶位越高roll上限越高（最高宇宙奇观级index10）
            int maxQuality = Mathf.Min(3 + rank / 2, 10);
            int quality = Random.Range(0, maxQuality + 1);

            EquipRelic(a, quality);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.Name} 战斗掉落宝物：{QualityNames[quality]}（掉落率{dropChance:F0%}）");
        }

        /// <summary>装备宝物（高级替换低级）。</summary>
        public static void EquipRelic(Actor a, int qualityIndex)
        {
            if (a == null || qualityIndex < 0 || qualityIndex >= QualityOrder.Length) return;

            // 检查是否已有更高级宝物
            int current = GetCurrentRelicIndex(a);
            if (current >= qualityIndex) return;

            // 移除低级宝物
            for (int i = 0; i <= current; i++)
            {
                if (a.hasTrait(QualityOrder[i])) a.removeTrait(QualityOrder[i]);
            }

            // 装备新宝物
            a.addTrait(QualityOrder[qualityIndex]);
        }

        /// <summary>获取当前装备宝物等级。</summary>
        public static int GetCurrentRelicIndex(Actor a)
        {
            for (int i = QualityOrder.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(QualityOrder[i])) return i;
            }
            return -1;
        }

        /// <summary>获取当前装备宝物名。</summary>
        public static string GetCurrentRelicName(Actor a)
        {
            int idx = GetCurrentRelicIndex(a);
            return idx >= 0 ? QualityNames[idx] : "无";
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
