using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义属性注册：参考西幻世界的BaseStatAsset注册方式。
    /// 只注册原著中真正是"属性/能量"的东西：
    /// - 气力（能量，当前值/上限，消耗条）
    /// - 魔力（魔法系能量，消耗条）
    /// - 精神力（念力系能量，消耗条）
    /// - 械感（机械亲和度，机械师核心天赋属性）
    /// - 职业等级（降临者面板属性）
    /// 能级是计算出来的战斗力函数，不是基础属性，在单位面板文本显示。
    /// 基因链/神性蜕变/信息态/潜力评级是系统/天赋/进阶，不是属性。
    /// </summary>
    public static class SuperMechCustomStats
    {
        // 原著中真正是属性/能量的
        public const string StatQi = "sm_qi";                    // 气力（当前值）
        public const string StatQiMax = "sm_qi_max";             // 气力上限
        public const string StatMana = "sm_mana";                // 魔力（魔法系能量，当前值）
        public const string StatManaMax = "sm_mana_max";         // 魔力上限
        public const string StatMindPower = "sm_mind_power";     // 精神力（念力系能量，当前值）
        public const string StatMindPowerMax = "sm_mind_power_max"; // 精神力上限
        public const string StatMechAffinity = "sm_mech_affinity"; // 械感（机械亲和度，百分比）
        public const string StatMageAffinity = "sm_mage_affinity"; // 魔感（魔法亲和度，百分比）
        public const string StatMystery = "sm_mystery";          // 神秘（原著7项基础属性之一）
        public const string StatCharm = "sm_charm";              // 魅力（原著7项基础属性之一）
        public const string StatLuck = "sm_luck";                // 幸运（原著7项基础属性之一）
        public const string StatProfessionLevel = "sm_profession_level"; // 职业等级（降临者）

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var stats = new (string id, string name, string desc, bool normalize, float min, float max, bool percent)[]
            {
                // 气力（原著核心能量，消耗条）
                (StatQi, "气力", "超能者基础能量，决定能级与位阶（原著ch3/ch50）", true, 0f, 3000000f, false),
                (StatQiMax, "气力上限", "气力最大值，随修炼/转职提升（原著ch3）", true, 0f, 3000000f, false),
                // 魔力（魔法系能量，消耗条）
                (StatMana, "魔力", "魔法系能量池，施放法术消耗（原著ch50）", true, 0f, 100000f, false),
                (StatManaMax, "魔力上限", "魔力最大值（原著ch50）", true, 0f, 100000f, false),
                // 精神力（念力系能量，消耗条）
                (StatMindPower, "精神力", "念力系能量，驱动念动力（原著ch50）", true, 0f, 100000f, false),
                (StatMindPowerMax, "精神力上限", "精神力最大值（原著ch50）", true, 0f, 100000f, false),
                // 械感（机械亲和度，百分比，原著ch626 Lv21+4282%）
                (StatMechAffinity, "械感", "机械亲和度，机械师操控机械的核心天赋（原著ch50/ch626，气力属性【磁】增加机械亲和度）", true, 0f, 50000f, true),
                // 魔感（魔法亲和度，百分比，原著领袖之证"职业特色气力属性：魔法亲和"）
                (StatMageAffinity, "魔感", "魔法亲和度，魔法师操控元素的核心天赋（原著领袖之证，职业特色气力属性：魔法亲和）", true, 0f, 50000f, true),
                // 原著7项基础属性中的3项（原版没有的）
                (StatMystery, "神秘", "原著7项基础属性之一，影响异能/魔法强度（ch3）", true, 0f, 50000f, false),
                (StatCharm, "魅力", "原著7项基础属性之一，影响社交/声望（ch3）", true, 0f, 50000f, false),
                (StatLuck, "幸运", "原著7项基础属性之一，影响暴击/掉落/突破概率（ch3）", true, 0f, 50000f, false),
                // 职业等级（降临者面板属性）
                (StatProfessionLevel, "职业等级", "降临者职业总等级（原著ch48，20级进阶转职）", false, 0f, 600f, false),
            };

            int registered = 0;
            foreach (var (id, name, desc, normalize, min, max, percent) in stats)
            {
                if (AssetManager.base_stats_library.get(id) != null) continue;

                var asset = new BaseStatAsset
                {
                    id = id,
                    hidden = false,
                    icon = "ui/Icons/actor_traits/iconBlessing",
                    normalize = normalize,
                    normalize_min = min,
                    normalize_max = max,
                    used_only_for_civs = false,
                    actor_data_attribute = false,
                    show_as_percents = percent,
                    multiplier = false,
                    sort_rank = 100 + registered,
                };
                AssetManager.base_stats_library.add(asset);

                LocalizedTextManager.add(id, name, pReplace: true);
                LocalizedTextManager.add(id + "_info", desc, pReplace: true);
                registered++;
            }

            Debug.Log($"[超神机械师] 自定义属性注册完成：{registered}个（气力/气力上限/魔力/魔力上限/精神力/精神力上限/械感/神秘/魅力/幸运/职业等级）");
        }

        /// <summary>每tick：遍历所有超神机械师单位，同步属性到BaseStats。</summary>
        public static void TickSync()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            int processed = 0;
            int maxTracked = SuperMechConfig.MaxTrackedActors;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (processed >= maxTracked)
                {
                    if (SuperMechAdvancement.GetExactRankIndex(a) < 8) continue;
                }
                processed++;
                try { SyncStats(a); } catch { }
            }
        }

        /// <summary>同步内部字典到单位BaseStats（单位面板属性栏显示）。</summary>
        public static void SyncStats(Actor a)
        {
            if (a == null || a.data == null) return;
            var stats = a.data.stats;
            if (stats == null) return;

            // 气力（原著核心能量，消耗条：当前值/上限）
            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            stats[StatQi] = qi;
            stats[StatQiMax] = qiMax;

            // 械感（机械亲和度，百分比，原著ch626 Lv21+4282%, Lv25+9806%）
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                // 原著公式拟合：affinity% = 100 * 1.2^level
                // Lv21: 100*1.2^21=4600%≈4282%, Lv25: 100*1.2^25=9500%≈9806%
                stats[StatMechAffinity] = 100f * Mathf.Pow(1.2f, qiLv);
            }

            // 神秘/魅力/幸运（原著7项基础属性，气力等级加成）
            int qiLvForStats = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            // 原著Lv21: 神秘+3123，公式拟合：mystery = level^2 * 7.1
            stats[StatMystery] = qiLvForStats * qiLvForStats * 7.1f;
            // 魅力：原著未给具体数值，按神秘的0.8估算
            stats[StatCharm] = qiLvForStats * qiLvForStats * 5.7f;
            // 幸运：原著未给具体数值，按神秘的0.3估算
            stats[StatLuck] = qiLvForStats * qiLvForStats * 2.1f;

            // 魔力（魔法系能量，消耗条）
            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                // 魔力=气力×魔法系转化率（简化，原著魔法师有独立魔力池）
                stats[StatMana] = qi * 0.8f;
                stats[StatManaMax] = qiMax * 0.8f;
                // 魔感（魔法亲和度，百分比，原著领袖之证"职业特色气力属性：魔法亲和"）
                int qiLvForMage = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                stats[StatMageAffinity] = 100f * Mathf.Pow(1.2f, qiLvForMage);
            }

            // 精神力（念力系能量，消耗条）
            if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                // 精神力=气力×念力系转化率（念力系精神力高于气力）
                stats[StatMindPower] = qi * 1.2f;
                stats[StatMindPowerMax] = qiMax * 1.2f;
            }

            // 职业等级（降临者）
            stats[StatProfessionLevel] = SuperMechStage.GetStage(a);
        }
    }
}
