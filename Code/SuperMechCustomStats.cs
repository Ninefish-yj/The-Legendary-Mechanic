using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义属性注册：参考西幻世界的BaseStatAsset注册方式。
    /// 只注册原著中真正是"属性"的东西（面板上显示的数值）：
    /// 气力/气力等级/能级/魔力/精神力/职业等级。
    /// 其他（基因链/神性蜕变/信息态/潜力评级等）是系统/天赋/进阶，不是属性，保持内部字典+单位面板显示。
    /// </summary>
    public static class SuperMechCustomStats
    {
        // 原著中真正是属性的（面板数值）
        public const string StatQi = "sm_qi";                    // 气力（当前值）
        public const string StatQiMax = "sm_qi_max";             // 气力上限
        public const string StatQiLevel = "sm_qi_level";         // 气力等级Lv1-40
        public const string StatEnergyLevel = "sm_energy_level"; // 能级（欧纳）
        public const string StatMana = "sm_mana";                // 魔力（魔法系能量）
        public const string StatMindPower = "sm_mind_power";     // 精神力（念力系能量）
        public const string StatProfessionLevel = "sm_profession_level"; // 职业等级（降临者）

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var stats = new (string id, string name, string desc, bool normalize, float min, float max)[]
            {
                (StatQi, "气力", "超能者基础能量，决定能级与位阶（原著ch3/ch50）", true, 0f, 3000000f),
                (StatQiMax, "气力上限", "气力最大值，随修炼/转职提升（原著ch3）", true, 0f, 3000000f),
                (StatQiLevel, "气力等级", "气力对应的等级Lv1-40（原著ch3，Lv1=10, Lv29=45万）", false, 0f, 40f),
                (StatEnergyLevel, "能级", "战斗力函数值，单位欧纳（原著ch3，斯图尔特·欧纳创立）", true, 0f, 200000f),
                (StatMana, "魔力", "魔法系能量池（原著ch50，魔法师核心属性）", true, 0f, 100000f),
                (StatMindPower, "精神力", "念力系能量（原著ch50，念动力核心属性）", true, 0f, 100000f),
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

            Debug.Log($"[超神机械师] 自定义属性注册完成：{registered}个（气力/气力上限/气力等级/能级/魔力/精神力/职业等级）");
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

            // 气力（原著核心属性）
            float qi = SuperMechQi.GetQi(a);
            stats[StatQi] = qi;
            stats[StatQiMax] = SuperMechQi.GetQiMax(a);
            stats[StatQiLevel] = SuperMechQi.GetLevel(qi);

            // 能级（欧纳）
            stats[StatEnergyLevel] = SuperMechAdvancement.CalcOnar(a);

            // 魔力（魔法系）
            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                // 魔力=气力×魔法系转化率（简化，原著魔法师有独立魔力池）
                stats[StatMana] = qi * 0.8f;
            }

            // 精神力（念力系）
            if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                stats[StatMindPower] = qi * 1.2f; // 念力系精神力高于气力
            }

            // 职业等级（降临者）
            stats[StatProfessionLevel] = SuperMechStage.GetStage(a);
        }
    }
}
