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
        public const string StatMechAffinity = "sm_mech_affinity"; // 械感（机械亲和度，机械师核心）
        public const string StatProfessionLevel = "sm_profession_level"; // 职业等级（降临者）

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var stats = new (string id, string name, string desc, bool normalize, float min, float max)[]
            {
                // 气力（原著核心能量，消耗条）
                (StatQi, "气力", "超能者基础能量，决定能级与位阶（原著ch3/ch50）", true, 0f, 3000000f),
                (StatQiMax, "气力上限", "气力最大值，随修炼/转职提升（原著ch3）", true, 0f, 3000000f),
                // 魔力（魔法系能量，消耗条）
                (StatMana, "魔力", "魔法系能量池，施放法术消耗（原著ch50）", true, 0f, 100000f),
                (StatManaMax, "魔力上限", "魔力最大值（原著ch50）", true, 0f, 100000f),
                // 精神力（念力系能量，消耗条）
                (StatMindPower, "精神力", "念力系能量，驱动念动力（原著ch50）", true, 0f, 100000f),
                (StatMindPowerMax, "精神力上限", "精神力最大值（原著ch50）", true, 0f, 100000f),
                // 械感（机械亲和度，机械师核心天赋属性）
                (StatMechAffinity, "械感", "机械亲和度，机械师操控机械的核心天赋（原著ch50，气力属性【磁】增加机械亲和度）", true, 0f, 10000f),
                // 职业等级（降临者面板属性）
                (StatProfessionLevel, "职业等级", "降临者职业总等级（原著ch48，20级进阶转职）", false, 0f, 600f),
            };

            int registered = 0;
            foreach (var (id, name, desc, normalize, min, max) in stats)
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
                    show_as_percents = false,
                    multiplier = false,
                    sort_rank = 100 + registered,
                };
                AssetManager.base_stats_library.add(asset);

                LocalizedTextManager.add(id, name, pReplace: true);
                LocalizedTextManager.add(id + "_info", desc, pReplace: true);
                registered++;
            }

            Debug.Log($"[超神机械师] 自定义属性注册完成：{registered}个（气力/气力上限/魔力/魔力上限/精神力/精神力上限/械感/职业等级）");
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

            // 械感（机械亲和度，机械师核心天赋）
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                // 械感=气力上限×机械系转化率+智力加成（原著：气力属性【磁】增加机械亲和度）
                float intel = a.data.stats["intelligence"];
                stats[StatMechAffinity] = qiMax * 0.05f + intel * 2f;
            }

            // 魔力（魔法系能量，消耗条）
            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                // 魔力=气力×魔法系转化率（简化，原著魔法师有独立魔力池）
                stats[StatMana] = qi * 0.8f;
                stats[StatManaMax] = qiMax * 0.8f;
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
