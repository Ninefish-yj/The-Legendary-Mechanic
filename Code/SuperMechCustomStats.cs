using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义属性注册：参考西幻世界的BaseStatAsset注册方式。
    /// 注册原著相关的属性到AssetManager.base_stats_library，
    /// 这些属性会显示在单位面板的属性栏里，可以被特质/装备/技能引用。
    /// 内部字典的值在tick时同步到BaseStats，用于显示和计算。
    /// </summary>
    public static class SuperMechCustomStats
    {
        // 自定义属性ID列表
        public const string StatQi = "sm_qi";                    // 气力
        public const string StatEnergyLevel = "sm_energy_level"; // 能级（欧纳）
        public const string StatMechAffinity = "sm_mech_affinity"; // 械力/机械亲和度
        public const string StatGeneChain = "sm_gene_chain";     // 基因链等级（异能系）
        public const string StatManaPool = "sm_mana_pool";       // 魔力池（魔法系）
        public const string StatMindPower = "sm_mind_power";     // 精神力（念力系）
        public const string StatDivinityLayer = "sm_divinity_layer"; // 神性蜕变层数
        public const string StatLegendPoints = "sm_legend_points";   // 传说度
        public const string StatInfoState = "sm_info_state";     // 信息态等级
        public const string StatPotential = "sm_potential";      // 潜力评级
        public const string StatQiLevel = "sm_qi_level";         // 气力等级
        public const string StatProfessionLevel = "sm_profession_level"; // 职业等级（降临者）

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var stats = new (string id, string name, string desc, bool normalize, float min, float max)[]
            {
                (StatQi, "气力", "超能者基础能量，决定能级与位阶（原著ch3/ch50）", true, 0f, 3000000f),
                (StatQiLevel, "气力等级", "气力对应的等级Lv1-40（原著ch3）", false, 0f, 40f),
                (StatEnergyLevel, "能级", "战斗力函数值，单位欧纳（原著ch3，斯图尔特·欧纳创立）", true, 0f, 200000f),
                (StatMechAffinity, "械力", "机械系专属，机械亲和度（原著ch50，气力属性【磁】）", true, 0f, 10000f),
                (StatGeneChain, "基因链", "异能系专属，基因链解锁阶段（原著ch50）", false, 0f, 5f),
                (StatManaPool, "魔力池", "魔法系专属，魔力池层数（原著ch50）", false, 0f, 5f),
                (StatMindPower, "精神力", "念力系专属，精神力强度（原著ch50）", true, 0f, 10000f),
                (StatDivinityLayer, "神性蜕变", "神性蜕变总层数（职业+种族，原著ch1039）", false, 0f, 20f),
                (StatLegendPoints, "传说度", "传奇事迹积累，影响突破概率（原著ch1196）", true, 0f, 1000f),
                (StatInfoState, "信息态", "信息态技术等级（原著ch1225，圣所推测为信息态宇宙奇观）", false, 0f, 5f),
                (StatPotential, "潜力评级", "异能潜力，决定阶位上限（原著ch1099）", false, 0f, 10f),
                (StatProfessionLevel, "职业等级", "降临者职业等级（原著ch48，20级进阶转职）", false, 0f, 600f),
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

                // 本地化
                LocalizedTextManager.add(id, name, pReplace: true);
                LocalizedTextManager.add(id + "_info", desc, pReplace: true);
                registered++;
            }

            Debug.Log($"[超神机械师] 自定义属性注册完成：{registered}个（气力/能级/械力/基因链/魔力池/精神力/神性蜕变/传说度/信息态/潜力/气力等级/职业等级）");
        }

        /// <summary>每tick：遍历所有超神机械师单位，同步自定义属性到BaseStats。</summary>
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

        /// <summary>每tick：把内部字典的值同步到单位BaseStats，用于单位面板显示。</summary>
        public static void SyncStats(Actor a)
        {
            if (a == null || a.data == null) return;
            var stats = a.data.stats;
            if (stats == null) return;

            // 气力
            float qi = SuperMechQi.GetQi(a);
            stats[StatQi] = qi;
            stats[StatQiLevel] = SuperMechQi.GetLevel(qi);

            // 能级
            stats[StatEnergyLevel] = SuperMechAdvancement.CalcOnar(a);

            // 械力（机械系）
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                stats[StatMechAffinity] = SuperMechQi.GetQiMax(a) * 0.1f; // 械力≈气力×0.1（简化）
            }

            // 基因链（异能系）
            if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                stats[StatGeneChain] = SuperMechCorePower.GetGeneStage(a);
            }

            // 魔力池（魔法系）
            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                stats[StatManaPool] = SuperMechCorePower.GetManaStage(a);
            }

            // 精神力（念力系）
            if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                stats[StatMindPower] = SuperMechCorePower.GetMindStage(a) * 100f;
            }

            // 神性蜕变层数
            stats[StatDivinityLayer] = SuperMechDivinity.GetTotalLayers(a);

            // 传说度
            stats[StatLegendPoints] = SuperMechLegend.GetLegend(a);

            // 信息态等级
            stats[StatInfoState] = SuperMechInfoState.GetLevel(a);

            // 潜力评级（string转int，F=1...S=6）
            string rating = SuperMechPotentialRating.GetRating(a);
            stats[StatPotential] = rating.Length; // 简化：评级字符串长度作为数值

            // 职业等级（降临者）
            stats[StatProfessionLevel] = SuperMechStage.GetStage(a);
        }
    }
}
