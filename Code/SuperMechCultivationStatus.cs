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
            ("普通人", "未踏入超能之路"),                              // 0: 无阶位
            ("F阶", "初入超能，玩家过渡阶位（星海人无此阶）"),         // 1: F（ch5：玩家lv1-20对应F阶）
            ("E级超能者", "初步脱离了普通人的范畴，但距离真正的非凡生命，还有很长的路要走"), // 2: E（ch48原文）
            ("D级超能者", "中级超能者，星际常规战力"),                  // 3: D
            ("D+级", "中级巅峰，触摸高级门槛"),                          // 4: D+
            ("C级超能者", "星球探索战士基础标准，基层力量分水岭"),       // 5: C（ch234原文）
            ("C+级", "高级巅峰，文明级战力"),                            // 6: C+
            ("B级超能者", "强力超能者，星际战场主力"),                  // 7: B
            ("B+级", "强力巅峰，距天灾仅一步之遥"),                      // 8: B+
            ("天灾级", "A级分水岭，星空间高等力量，能在行星地表掀起毁灭性灾难"), // 9: A（ch2/ch371原文）
            ("天灾巅峰", "天灾级巅峰，基因进化碰上坚不可摧的瓶颈壁垒"),   // 10: A+（ch371原文）
            ("超A级", "超越常规物种的全新生命形态，拥有专属种族，宇宙顶级存在"), // 11: S（ch760/ch946原文）
            ("巅峰超A", "巅峰超A级，半步触摸神之领域"),                  // 12: S+
            ("半步超神", "神性蜕变圆满，半步踏入神之境"),                // 13: SS
            ("超神级", "抵达神的领域，资讯唯一·概念永生，宇宙首个超神级存在"), // 14: X（ch1388原文）
        };

        // === 气力等级描述（原著原文）===
        // ch49: 气力是超能者的基础，很大部分决定了超能者的能级与位阶
        // ch49: 达到标准，便晋升下一个气力等级，获得新的属性加成
        private static readonly (int minLv, string desc)[] QiStatus = new (int, string)[]
        {
            (1, "气力初生（lv1标准10点）"),
            (3, "气力渐丰（lv3标准100点）"),
            (6, "气力质变（lv6解锁气力属性强化）"),  // ch49: 第一次气力质变在Lv6
            (11, "气力深厚（高阶超能者标准）"),
            (16, "气力如海（天灾级标准）"),
            (21, "气力圆满（神性蜕变门槛78000欧纳）"), // ch1039: 神性蜕变触发条件
            (31, "气力通天（超神级标准）"),
        };

        // === 神性蜕变描述（原著ch1039/ch1043）===
        // 双路线：职业蜕变+种族蜕变，各10层
        private static readonly string[] ProfLayerStatus = new string[]
        {
            "未开启",
            "职业神性1层",
            "职业神性2层",
            "职业神性3层",
            "职业神性4层",
            "职业神性5层",
            "职业神性6层",
            "职业神性7层",
            "职业神性8层",
            "职业神性9层",
            "职业神性圆满",
        };

        private static readonly string[] SpeciesLayerStatus = new string[]
        {
            "",
            "种族神性1层",
            "种族神性2层",
            "种族神性3层",
            "种族神性4层",
            "种族神性5层",
            "种族神性6层",
            "种族神性7层",
            "种族神性8层",
            "种族神性9层",
            "种族神性圆满",
        };

        /// <summary>获取阶位称号。</summary>
        public static string GetRankTitle(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return "普通人";
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
            return "未开启神性蜕变";
        }

        /// <summary>获取当前修炼状态。</summary>
        public static string GetCultivationState(Actor a, int rankIndex, int qiLevel)
        {
            // X阶超神级
            if (rankIndex >= 14) return "已证超神，概念永生";
            // SS阶半步超神
            if (rankIndex >= 13) return "半步超神，等待契机";
            // A阶瓶颈（ch371: 基因进化到一定高度会碰上坚不可摧的瓶颈壁垒）
            if (rankIndex >= 9 && rankIndex <= 10 && qiLevel >= 21) return "瓶颈期（前面看不到路了）";
            // 修炼中
            return "修炼中";
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
            string result = $"【{title}】{desc}\n气力：{qiDesc}";
            if (profLayer > 0 || speciesLayer > 0)
                result += $"\n神性：{divinity}";
            if (!string.IsNullOrEmpty(state) && state != "修炼中")
                result += $"\n状态：{state}";
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
