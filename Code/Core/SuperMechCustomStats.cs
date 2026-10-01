using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechCustomStats
    {
        public const string StatQi = "sm_qi";
        public const string StatQiMax = "sm_qi_max";
        public const string StatStrength = "sm_strength";
        public const string StatAgility = "sm_agility";
        public const string StatEndurance = "sm_endurance";
        public const string StatMechAffinity = "sm_mech_affinity";
        public const string StatMageAffinity = "sm_mage_affinity";
        public const string StatMystery = "sm_mystery";
        public const string StatCharm = "sm_charm";
        public const string StatLuck = "sm_luck";
        public const string StatProfessionLevel = "sm_profession_level";
        public const string StatPotentialPoints = "sm_potential_points";
        public const string StatDivinityLayers = "sm_divinity_layers";
        public const string StatSanctuary1 = "sm_sanctuary_1";
        public const string StatSanctuary2 = "sm_sanctuary_2";
        public const string StatSanctuary3 = "sm_sanctuary_3";
        public const string StatSanctuary4 = "sm_sanctuary_4";
        public const string StatSanctuary5 = "sm_sanctuary_5";
        public const string StatSanctuary6 = "sm_sanctuary_6";

        private static bool _registered = false;

        private struct StatInfo { public string id, name, desc; public bool normalize, percent; public float min, max; public StatInfo(string a, string b, string c, bool n, float mn, float mx, bool p) { id=a; name=b; desc=c; normalize=n; min=mn; max=mx; percent=p; } }
        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var stats = new StatInfo[]
            {
                new StatInfo(StatQi, "sm_customstats_695", "sm_customstats_696", true, 0f, 3000000f, false),
                new StatInfo(StatQiMax, "sm_customstats_697", "sm_customstats_698", true, 0f, 3000000f, false),
                new StatInfo(StatStrength, "sm_customstats_740", "sm_customstats_741", true, 0f, 10000f, false),
                new StatInfo(StatAgility, "sm_customstats_742", "sm_customstats_743", true, 0f, 10000f, false),
                new StatInfo(StatEndurance, "sm_customstats_744", "sm_customstats_745", true, 0f, 10000f, false),
                new StatInfo(StatMechAffinity, "sm_customstats_707", "sm_customstats_708", true, 0f, 50000f, true),
                new StatInfo(StatMageAffinity, "sm_customstats_709", "sm_customstats_710", true, 0f, 50000f, true),
                new StatInfo(StatMystery, "sm_customstats_711", "sm_customstats_712", true, 0f, 50000f, false),
                new StatInfo(StatCharm, "sm_customstats_713", "sm_customstats_714", true, 0f, 50000f, false),
                new StatInfo(StatLuck, "sm_customstats_715", "sm_customstats_716", true, 0f, 50000f, false),
                new StatInfo(StatProfessionLevel, "sm_customstats_717", "sm_customstats_718", false, 0f, 600f, false),
                new StatInfo(StatPotentialPoints, "sm_customstats_719", "sm_customstats_720", false, 0f, 10000f, false),
                new StatInfo(StatDivinityLayers, "sm_customstats_721", "sm_customstats_722", false, 0f, 20f, false),
                new StatInfo(StatSanctuary1, "sm_customstats_723", "sm_customstats_724", false, 0f, 100f, false),
                new StatInfo(StatSanctuary2, "sm_customstats_725", "sm_customstats_726", false, 0f, 100f, false),
                new StatInfo(StatSanctuary3, "sm_customstats_727", "sm_customstats_728", false, 0f, 100f, false),
                new StatInfo(StatSanctuary4, "sm_customstats_729", "sm_customstats_730", false, 0f, 100f, false),
                new StatInfo(StatSanctuary5, "sm_customstats_731", "sm_customstats_732", false, 0f, 100f, false),
                new StatInfo(StatSanctuary6, "sm_customstats_733", "sm_customstats_734", false, 0f, 100f, false),
            };

            int registered = 0;
            foreach (var s in stats)
            {
                if (AssetManager.base_stats_library.get(s.id) != null) continue;

                // 神性蜕变和圣所权限不在属性面板显示，作为信息面板的一行信息
                bool isHidden = s.id == StatDivinityLayers ||
                    s.id == StatSanctuary1 || s.id == StatSanctuary2 || s.id == StatSanctuary3 ||
                    s.id == StatSanctuary4 || s.id == StatSanctuary5 || s.id == StatSanctuary6;

                var asset = new BaseStatAsset
                {
                    id = s.id,
                    translation_key = s.name,
                    hidden = isHidden,
                    icon = "ui/Icons/actor_traits/iconBlessing",
                    normalize = s.normalize,
                    normalize_min = s.min,
                    normalize_max = s.max,
                    used_only_for_civs = false,
                    actor_data_attribute = false,
                    show_as_percents = s.percent,
                    multiplier = false,
                    sort_rank = 100 + registered,
                };
                AssetManager.base_stats_library.add(asset);
                registered++;
            }

            Debug.Log($"[超神机械师] 自定义属性注册完成：{registered}个（气力/气力上限/械感/魔感/神秘/魅力/幸运/职业等级/潜能点/神性蜕变/6圣所权限）");
        }

        public static void TickSync()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            int processed = 0;
            int maxTracked = SuperMechConfig.MaxTrackedActors;
            int errors = 0;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                bool isAwakened = SuperMechTalent.HasTalent(a);
                if (!isAwakened && processed >= maxTracked)
                {
                    if (SuperMechAdvancement.GetExactRankIndex(a) < 8) continue;
                }
                processed++;
                try { SyncStats(a); }
                catch (System.Exception e)
                {
                    errors++;
                    if (errors <= 3)
                        Debug.LogError($"[超神机械师] SyncStats异常({a.name}): {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public static void SyncStats(Actor a)
        {
            if (a == null || a.data == null) return;
            var stats = a.stats;
            if (stats == null) return;

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            stats[StatQi] = qi;
            stats[StatQiMax] = qiMax;

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                stats[StatMechAffinity] = 100f * Mathf.Pow(1.2f, qiLv);
            }

            int qiLvForStats = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            // 原著Lv10气力总加成(ch539)：力量+71、敏捷+97、耐力+108、智力+122、神秘+77
            // 非线性增长：等级越高每一层加成越显著(ch51"气力等级越高，每一层加成越显著")
            float levelScale = Mathf.Pow(qiLvForStats / 10f, 1.3f);
            stats[StatStrength] = 71f * levelScale;
            stats[StatAgility] = 97f * levelScale;
            stats[StatEndurance] = 108f * levelScale;
            stats[StatMystery] = 77f * levelScale;
            stats[StatCharm] = qiLvForStats * qiLvForStats * 5.7f;
            stats[StatLuck] = qiLvForStats * qiLvForStats * 2.1f;

            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                int qiLvForMage = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                stats[StatMageAffinity] = 100f * Mathf.Pow(1.2f, qiLvForMage);
            }

            stats[StatProfessionLevel] = SuperMechStage.GetStage(a);

            stats[StatPotentialPoints] = SuperMechPotential.GetPotential(a);

            stats[StatDivinityLayers] = SuperMechDivinity.GetTotalLayers(a);

            stats[StatSanctuary1] = SuperMechSanctuary.GetAuthority(a, 0);
            stats[StatSanctuary2] = SuperMechSanctuary.GetAuthority(a, 1);
            stats[StatSanctuary3] = SuperMechSanctuary.GetAuthority(a, 2);
            stats[StatSanctuary4] = SuperMechSanctuary.GetAuthority(a, 3);
            stats[StatSanctuary5] = SuperMechSanctuary.GetAuthority(a, 4);
            stats[StatSanctuary6] = SuperMechSanctuary.GetAuthority(a, 5);
        }

        public static float GetStat(Actor a, string statId)
        {
            if (a == null || a.stats == null) return 0f;
            return a.stats[statId];
        }
    }
}
