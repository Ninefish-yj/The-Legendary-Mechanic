using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 修炼状态信息栏（参考天人武道"先天之躯"展示区，使用超神机械师原著原文）
    /// 原著依据：
    /// - ch2: E级超能者能级标准100欧纳；A级又称天灾级，能在行星地表掀起毁灭性灾难
    /// - ch48: E级"你已经初步脱离了普通人的范畴，但距离真正的非凡生命，还有很长的路要走"
    /// - ch49: 气力是超能者的基础，很大部分决定了超能者的能级与位阶
    /// - ch234: C级称为"星球探索战士基础标准"
    /// - ch371: A级是星空间的高等力量，基因进化到一定高度会碰上坚不可摧的瓶颈壁垒
    /// - ch760/ch946: 超A级是超越常规物种的全新生命形态，拥有专属种族
    /// - ch1039: 神性蜕变双路线（职业+种族）
    /// - ch1388: 超神级抵达神的领域，资讯唯一·概念永生
    /// </summary>
    public static class SuperMechCultivationStatus
    {
        // === 阶位称号与描述（原著原文/原著风格）===
        // 索引：0=无阶位, 1=F, 2=E, 3=D, 4=D+, 5=C, 6=C+, 7=B, 8=B+, 9=A, 10=A+, 11=S, 12=S+, 13=SS, 14=X
        private static readonly (string title, string desc)[] RankStatus = new (string, string)[]
        {
            ("sm_cultivationstatus_470", "sm_cultivationstatus_1300"),                              // 0: 无阶位
            ("sm_cultivationstatus_1301", "sm_cultivationstatus_1302"),         // 1: F（ch5：玩家lv1-20对应F阶）
            ("sm_cultivationstatus_1303", "sm_cultivationstatus_1304"), // 2: E（ch48原文）
            ("sm_cultivationstatus_1305", "sm_cultivationstatus_1306"),                  // 3: D
            ("sm_cultivationstatus_1307", "sm_cultivationstatus_1308"),                          // 4: D+
            ("sm_cultivationstatus_1309", "sm_cultivationstatus_1310"),       // 5: C（ch234原文）
            ("sm_cultivationstatus_1311", "sm_cultivationstatus_1312"),                            // 6: C+
            ("sm_cultivationstatus_1313", "sm_cultivationstatus_1314"),                  // 7: B
            ("sm_cultivationstatus_1315", "sm_cultivationstatus_1316"),                      // 8: B+
            ("sm_cultivationstatus_1317", "sm_cultivationstatus_1318"), // 9: A（ch2/ch371原文）
            ("sm_cultivationstatus_1319", "sm_cultivationstatus_1320"),   // 10: A+（ch371原文）
            ("sm_cultivationstatus_1321", "sm_cultivationstatus_1322"), // 11: S（ch760/ch946原文）
            ("sm_cultivationstatus_1323", "sm_cultivationstatus_1324"),                  // 12: S+
            ("sm_cultivationstatus_1325", "sm_cultivationstatus_1326"),                // 13: SS
            ("sm_cultivationstatus_1327", "sm_cultivationstatus_1328"), // 14: X（ch1388原文）
        };

        // === 气力等级描述（原著原文）===
        // ch49: 气力是超能者的基础，很大部分决定了超能者的能级与位阶
        // ch49: 达到标准，便晋升下一个气力等级，获得新的属性加成
        private static readonly (int minLv, string desc)[] QiStatus = new (int, string)[]
        {
            (1, "sm_cultivationstatus_444"),
            (3, "sm_cultivationstatus_445"),
            (6, "sm_cultivationstatus_1329"),  // ch49: 第一次气力质变在Lv6
            (11, "sm_cultivationstatus_446"),
            (16, "sm_cultivationstatus_447"),
            (21, "sm_cultivationstatus_1330"), // ch1039: 神性蜕变触发条件
            (31, "sm_cultivationstatus_448"),
        };

        // === 神性蜕变描述（原著ch1039/ch1043）===
        // 双路线：职业蜕变+种族蜕变，各10层
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

        /// <summary>获取阶位称号。</summary>
        public static string GetRankTitle(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return LocalizedTextManager.getText("sm_cultivationstatus_470");
            return RankStatus[rankIndex].title;
        }

        /// <summary>获取阶位描述。</summary>
        public static string GetRankDesc(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return RankStatus[0].desc;
            return RankStatus[rankIndex].desc;
        }

        /// <summary>获取气力等级描述。</summary>
        public static string GetQiDesc(int qiLevel)
        {
            string desc = QiStatus[0].desc;
            foreach (var (minLv, d) in QiStatus)
            {
                if (qiLevel >= minLv) desc = d;
            }
            return desc;
        }

        /// <summary>获取神性蜕变描述。</summary>
        public static string GetDivinityDesc(int profLayer, int speciesLayer)
        {
            string prof = profLayer > 0 ? ProfLayerStatus[Mathf.Min(profLayer, 10)] : "";
            string species = speciesLayer > 0 ? SpeciesLayerStatus[Mathf.Min(speciesLayer, 10)] : "";
            if (profLayer > 0 && speciesLayer > 0) return $"{prof}·{species}";
            if (profLayer > 0) return prof;
            if (speciesLayer > 0) return species;
            return LocalizedTextManager.getText("sm_cultivationstatus_471");
        }

        /// <summary>获取当前修炼状态。</summary>
        public static string GetCultivationState(Actor a, int rankIndex, int qiLevel)
        {
            // X阶超神级
            if (rankIndex >= 14) return LocalizedTextManager.getText("sm_cultivationstatus_472");
            // SS阶半步超神
            if (rankIndex >= 13) return LocalizedTextManager.getText("sm_cultivationstatus_473");
            // A阶瓶颈（ch371: 基因进化到一定高度会碰上坚不可摧的瓶颈壁垒）
            if (rankIndex >= 9 && rankIndex <= 10 && qiLevel >= 21) return LocalizedTextManager.getText("sm_cultivationstatus_474");
            // 修炼中
            return LocalizedTextManager.getText("sm_cultivationstatus_475");
        }

        /// <summary>生成完整的修炼状态文本（用于单位面板顶部展示）。</summary>
        public static string GetStatusSummary(Actor a)
        {
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);

            // 普通人（无阶位）：只显示阶位+描述，不显示气力/神性/状态
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

            // 简化：阶位+描述一行，气力一行，神性/状态只在有内容时显示
            string result = $"sm_cultivationstatus_476";
            if (profLayer > 0 || speciesLayer > 0)
                result += $"sm_cultivationstatus_477";
            if (!string.IsNullOrEmpty(state) && state != "sm_cultivationstatus_475")
                result += $"sm_cultivationstatus_478";
            return result;
        }

        /// <summary>获取简短状态（单行，用于面板标题）。</summary>
        public static string GetShortStatus(Actor a)
        {
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);
            string title = GetRankTitle(rankIndex);
            string state = GetCultivationState(a, rankIndex, SuperMechQi.GetLevel(SuperMechQi.GetQi(a)));
            return $"{title}（{state}）";
        }
    }
}
