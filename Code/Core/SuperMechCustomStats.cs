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
        public const string StatIntelligence = "sm_intelligence";
        public const string StatMechAffinity = "sm_mech_affinity";
        public const string StatMageAffinity = "sm_mage_affinity";
        public const string StatMystery = "sm_mystery";
        public const string StatCharm = "sm_charm";
        public const string StatLuck = "sm_luck";
        public const string StatProfessionLevel = "sm_profession_level";
        public const string StatPotentialPoints = "sm_potential_points";

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
                new StatInfo(StatStrength, "sm_customstats_740", "sm_customstats_741", true, 0f, 50000f, false),
                new StatInfo(StatAgility, "sm_customstats_742", "sm_customstats_743", true, 0f, 50000f, false),
                new StatInfo(StatEndurance, "sm_customstats_744", "sm_customstats_745", true, 0f, 50000f, false),
                new StatInfo(StatIntelligence, "sm_customstats_746", "sm_customstats_747", true, 0f, 50000f, false),
                new StatInfo(StatMechAffinity, "sm_customstats_707", "sm_customstats_708", true, 0f, 50000f, true),
                new StatInfo(StatMageAffinity, "sm_customstats_709", "sm_customstats_710", true, 0f, 50000f, true),
                new StatInfo(StatMystery, "sm_customstats_711", "sm_customstats_712", true, 0f, 50000f, false),
                new StatInfo(StatCharm, "sm_customstats_713", "sm_customstats_714", true, 0f, 50000f, false),
                new StatInfo(StatLuck, "sm_customstats_715", "sm_customstats_716", true, 0f, 50000f, false),
                new StatInfo(StatProfessionLevel, "sm_customstats_717", "sm_customstats_718", false, 0f, 600f, false),
                new StatInfo(StatPotentialPoints, "sm_customstats_719", "sm_customstats_720", false, 0f, 10000f, false),
            };

            int registered = 0;
            foreach (var s in stats)
            {
                if (AssetManager.base_stats_library.get(s.id) != null) continue;

                var asset = new BaseStatAsset
                {
                    id = s.id,
                    translation_key = s.name,
                    hidden = false,
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

        }

        public static void TickSync()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            int errors = 0;

            foreach (Actor a in units)
            {
                if (a == null) continue;
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

            // 原著气力层次属性加成（Lv29：力+12480, 敏+13640, 耐+17200, 智+22845, 神秘+13590）
            // 使用气力层次（非气力等级），幂函数累积加成，气力低于阈值丧失该层次加成
            stats[StatStrength] = SuperMechQiLayer.GetStrengthBonus(a);
            stats[StatAgility] = SuperMechQiLayer.GetAgilityBonus(a);
            stats[StatEndurance] = SuperMechQiLayer.GetEnduranceBonus(a);
            stats[StatMystery] = SuperMechQiLayer.GetMysteryBonus(a);
            // 智力按原著Lv29=22845拟合
            int effLayer = SuperMechQiLayer.GetEffectiveLayer(a);
            stats[StatIntelligence] = 5.044f * Mathf.Pow(Mathf.Max(1, effLayer), 2.5f);
            stats[StatCharm] = effLayer * effLayer * 5.7f;
            stats[StatLuck] = effLayer * effLayer * 2.1f;

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                // 机械亲和度按原著Lv29=14670%
                stats[StatMechAffinity] = SuperMechQiLayer.GetMechAffinityBonus(a);
            }

            stats[StatProfessionLevel] = SuperMechStage.GetStage(a);

            stats[StatPotentialPoints] = SuperMechPotential.GetPotential(a);
        }

        public static float GetStat(Actor a, string statId)
        {
            if (a == null || a.stats == null) return 0f;
            return a.stats[statId];
        }
    }
}
