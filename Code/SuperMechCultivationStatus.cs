using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 修炼状态信息栏（参考天人武道"先天之躯"展示区，符合超神机械师原著）
    /// 显示：阶位称号 + 境界描述 + 气力状态 + 神性蜕变状态 + 当前状态
    /// </summary>
    public static class SuperMechCultivationStatus
    {
        // === 阶位称号与描述（原著风格）===
        // 索引：0=无阶位(凡人), 1=F, 2=E, 3=D, 4=D+, 5=C, 6=C+, 7=B, 8=B+, 9=A, 10=A+, 11=S, 12=S+, 13=SS, 14=X
        private static readonly (string title, string desc)[] RankStatus = new (string, string)[]
        {
            ("凡人", "芸芸众生，未踏入超能之路"),           // 0: 无阶位
            ("初醒者", "初入超能，气力初生，前路漫漫"),     // 1: F
            ("低阶超能者", "低级超能者，于星球崭露头角"),   // 2: E
            ("中阶超能者", "中级超能者，已是一方强者"),     // 3: D
            ("中阶巅峰", "中级巅峰，触摸高级门槛"),          // 4: D+
            ("高阶超能者", "高级超能者，星球级战力"),        // 5: C
            ("高阶巅峰", "高级巅峰，文明级战力"),            // 6: C+
            ("强力超能者", "强力超能者，可镇压一方文明"),    // 7: B
            ("强力巅峰", "强力巅峰，距天灾仅一步之遥"),      // 8: B+
            ("天灾级", "一人可灭国，行星地表掀起毁灭性灾难"),// 9: A（原著ch2：A级又称天灾级）
            ("天灾巅峰", "天灾级巅峰，触摸更高层次"),        // 10: A+
            ("超A级", "宇宙顶级强者，三大文明亦不干涉私生活"),// 11: S（原著：超A级是宇宙顶级存在）
            ("巅峰超A", "巅峰超A级，半步触摸神之领域"),      // 12: S+
            ("半步超神", "神性蜕变圆满，半步踏入神之境"),    // 13: SS
            ("超神级", "宇宙之巅，信息态生命，概念永生"),    // 14: X
        };

        // === 气力等级描述 ===
        private static readonly (int minLv, string desc)[] QiStatus = new (int, string)[]
        {
            (1, "气力初生，涓涓细流"),
            (6, "气力渐丰，溪流成河"),
            (11, "气力深厚，江河奔涌"),
            (16, "气力如海，浩瀚无边"),
            (21, "气力圆满，生生不息"),
            (31, "气力通天，超凡入圣"),
        };

        // === 神性蜕变描述 ===
        private static readonly string[] ProfLayerStatus = new string[]
        {
            "凡躯肉体",
            "神性初醒",
            "神性微光",
            "神性萌芽",
            "神性觉醒",
            "神性显化",
            "神性深厚",
            "神性凝实",
            "神性圆满",
            "神性通天",
            "职业神性圆满",
        };

        private static readonly string[] SpeciesLayerStatus = new string[]
        {
            "凡俗血脉",
            "血脉微变",
            "血脉觉醒",
            "血脉升华",
            "血脉纯净",
            "血脉深厚",
            "血脉凝实",
            "血脉通天",
            "血脉圆满",
            "血脉超凡",
            "种族神性圆满",
        };

        /// <summary>获取阶位称号。</summary>
        public static string GetRankTitle(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= RankStatus.Length) return "凡人";
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
            // 瓶颈期（A阶以上，气力达到当前阶位上限）
            if (rankIndex >= 9 && IsAtBottleneck(rankIndex, qiLevel)) return "瓶颈期（需突破）";
            // 修炼中
            return "修炼中";
        }

        /// <summary>判断是否处于瓶颈期。</summary>
        private static bool IsAtBottleneck(int rankIndex, int qiLevel)
        {
            // 各阶位气力上限
            int[] qiCaps = { 0, 3, 6, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 30, 40 };
            if (rankIndex < qiCaps.Length && qiLevel >= qiCaps[rankIndex]) return true;
            return false;
        }

        /// <summary>生成完整的修炼状态文本（用于单位面板顶部展示）。</summary>
        public static string GetStatusSummary(Actor a)
        {
            int rankIndex = SuperMechAdvancement.GetExactRankIndex(a);
            float qiValue = SuperMechQi.GetQi(a);
            int qiLevel = SuperMechQi.GetLevel(qiValue);
            int profLayer = SuperMechDivinity.GetProfLayers(a);
            int speciesLayer = SuperMechDivinity.GetSpeciesLayers(a);

            string title = GetRankTitle(rankIndex);
            string desc = GetRankDesc(rankIndex);
            string qiDesc = GetQiDesc(qiLevel);
            string divinity = GetDivinityDesc(profLayer, speciesLayer);
            string state = GetCultivationState(a, rankIndex, qiLevel);

            return $"【{title}】{desc}\n气力：{qiDesc}（Lv{qiLevel}）\n神性：{divinity}\n状态：{state}";
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
