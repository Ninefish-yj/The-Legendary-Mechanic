using System;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechCultivationStatus
    {
        private static readonly (string title, string desc)[] RankStatus = new (string, string)[]
        {
            ("sm_cultivationstatus_470", "sm_cultivationstatus_1300"),
            ("sm_cultivationstatus_1301", "sm_cultivationstatus_1302"),
            ("sm_cultivationstatus_1303", "sm_cultivationstatus_1304"),
            ("sm_cultivationstatus_1305", "sm_cultivationstatus_1306"),
            ("sm_cultivationstatus_1307", "sm_cultivationstatus_1308"),
            ("sm_cultivationstatus_1309", "sm_cultivationstatus_1310"),
            ("sm_cultivationstatus_1311", "sm_cultivationstatus_1312"),
            ("sm_cultivationstatus_1313", "sm_cultivationstatus_1314"),
            ("sm_cultivationstatus_1315", "sm_cultivationstatus_1316"),
            ("sm_cultivationstatus_1317", "sm_cultivationstatus_1318"),
            ("sm_cultivationstatus_1319", "sm_cultivationstatus_1320"),
            ("sm_cultivationstatus_1321", "sm_cultivationstatus_1322"),
            ("sm_cultivationstatus_1323", "sm_cultivationstatus_1324"),
            ("sm_cultivationstatus_1325", "sm_cultivationstatus_1326"),
            ("sm_cultivationstatus_1327", "sm_cultivationstatus_1328"),
        };

        private static readonly (int minLv, string desc)[] QiStatus = new (int, string)[]
        {
            (1, "sm_cultivationstatus_444"),
            (3, "sm_cultivationstatus_445"),
            (6, "sm_cultivationstatus_1329"),
            (11, "sm_cultivationstatus_446"),
            (16, "sm_cultivationstatus_447"),
            (21, "sm_cultivationstatus_1330"),
            (31, "sm_cultivationstatus_448"),
        };

        private static readonly string[] ProfLayerStatus = new string[]
        {
            "sm_cultivationstatus_449",
            "sm_cultivationstatus_450",
            "sm_cultivationstatus_451",
            "sm_cultivationstatus_452",
            "sm_cultivationstatus_453",
            "sm_cultivationstatus_454",
            "sm_cultivationstatus_455",
            "sm_cultivationstatus_456",
            "sm_cultivationstatus_457",
            "sm_cultivationstatus_458",
            "sm_cultivationstatus_459",
        };

        private static readonly string[] SpeciesLayerStatus = new string[]
        {
            "",
            "sm_cultivationstatus_460",
            "sm_cultivationstatus_461",
            "sm_cultivationstatus_462",
            "sm_cultivationstatus_463",
            "sm_cultivationstatus_464",
            "sm_cultivationstatus_465",
            "sm_cultivationstatus_466",
            "sm_cultivationstatus_467",
            "sm_cultivationstatus_468",
            "sm_cultivationstatus_469",
        };

        public static string GetRankTitle(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return LocalizedTextManager.getText("sm_cultivationstatus_470");
            return RankStatus[rankIndex].title;
        }

        public static string GetRankDesc(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return RankStatus[0].desc;
            return RankStatus[rankIndex].desc;
        }

        public static string GetQiDesc(int qiLevel)
        {
            string desc = QiStatus[0].desc;
            foreach (var (minLv, d) in QiStatus)
            {
                if (qiLevel >= minLv) desc = d;
            }
            return desc;
        }

        public static string GetDivinityDesc(int profLayer, int speciesLayer)
        {
            string prof = profLayer > 0 ? ProfLayerStatus[Mathf.Min(profLayer, 10)] : "";
            string species = speciesLayer > 0 ? SpeciesLayerStatus[Mathf.Min(speciesLayer, 10)] : "";
            if (profLayer > 0 && speciesLayer > 0) return $"{prof}·{species}";
            if (profLayer > 0) return prof;
            if (speciesLayer > 0) return species;
            return LocalizedTextManager.getText("sm_cultivationstatus_471");
        }

        public static string GetCultivationState(Actor a, int rankIndex, int qiLevel)
        {
            if (rankIndex >= 14) return LocalizedTextManager.getText("sm_cultivationstatus_472");
            if (rankIndex >= 13) return LocalizedTextManager.getText("sm_cultivationstatus_473");
            if (rankIndex >= 9 && rankIndex <= 10 && qiLevel >= 21) return LocalizedTextManager.getText("sm_cultivationstatus_474");
            return LocalizedTextManager.getText("sm_cultivationstatus_475");
        }

        public static string GetStatusSummary(Actor a)
        {
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);

            if (rankIndex <= 0)
            {
                return $"【{GetRankTitle(rankIndex)}】{GetRankDesc(rankIndex)}";
            }

            float qiValue = SuperMechQi.GetQi(a);
            int qiLevel = SuperMechQi.GetLevel(qiValue);
            int profLayer = SuperMechDivinity.GetProfLayers(a);
            int speciesLayer = SuperMechDivinity.GetSpeciesLayers(a);

            string title = GetRankTitle(rankIndex);
            string desc = GetRankDesc(rankIndex);
            string qiDesc = GetQiDesc(qiLevel);
            string divinity = GetDivinityDesc(profLayer, speciesLayer);
            string state = GetCultivationState(a, rankIndex, qiLevel);

            string result = $"sm_cultivationstatus_476";
            if (profLayer > 0 || speciesLayer > 0)
                result += $"sm_cultivationstatus_477";
            if (!string.IsNullOrEmpty(state) && state != "sm_cultivationstatus_475")
                result += $"sm_cultivationstatus_478";
            return result;
        }

        public static string GetShortStatus(Actor a)
        {
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);
            string title = GetRankTitle(rankIndex);
            string state = GetCultivationState(a, rankIndex, SuperMechQi.GetLevel(SuperMechQi.GetQi(a)));
            return $"{title}（{state}）";
        }
    }
}
