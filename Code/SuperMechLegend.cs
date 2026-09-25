using System.Collections.Generic;
using UnityEngine;
using NeoModLoader.services;

namespace SuperMech.Code
{
    /// <summary>
    /// 传说度系统（原著ch1196/ch1135）
    /// 传奇事迹改变个体在信息态层面的权重，影响S阶以上突破成功率。
    /// 降临者（有面板）显示精确数值；星海人（无面板）只显示"隐约感到突破契机"。
    /// </summary>
    public static class SuperMechLegend
    {
        private static readonly Dictionary<long, int> _legend = new Dictionary<long, int>();
        private static readonly Dictionary<long, string> _lastDeed = new Dictionary<long, string>();

        // 传说度等级（参考原著宇宙传说度/星域传说度/星系传说度）
        public static readonly string[] TierNames = {
            "无名之辈", "小有名气", "星系知名", "星域传说", "宇宙传说", "神话级"
        };

        public static int GetLegend(Actor a)
        {
            if (a == null) return 0;
            _legend.TryGetValue(a.data.id, out int v);
            return v;
        }

        public static void AddLegend(Actor a, int amount, string deed = "")
        {
            if (a == null || amount <= 0) return;
            if (!_legend.ContainsKey(a.data.id)) _legend[a.data.id] = 0;
            _legend[a.data.id] += amount;
            if (!string.IsNullOrEmpty(deed))
            {
                _lastDeed[a.data.id] = deed;
                Debug.Log($"[超神机械师]【传说度】{a.name} 因「{deed}」获得{amount}点传说度（当前{_legend[a.data.id]}）");
            }
        }

        public static string GetLastDeed(Actor a)
        {
            if (a == null) return "";
            _lastDeed.TryGetValue(a.data.id, out string v);
            return v ?? "";
        }

        public static int GetTier(int legend)
        {
            if (legend >= 500) return 5;  // 神话级
            if (legend >= 200) return 4;  // 宇宙传说
            if (legend >= 80) return 3;   // 星域传说
            if (legend >= 30) return 2;   // 星系知名
            if (legend >= 10) return 1;   // 小有名气
            return 0;
        }

        public static string GetTierName(Actor a)
        {
            return TierNames[Mathf.Clamp(GetTier(GetLegend(a)), 0, 5)];
        }

        /// <summary>传说度对S阶以上突破成功率的加成（每10点+1%，上限+30%）</summary>
        public static float GetBreakthroughBonus(Actor a)
        {
            int legend = GetLegend(a);
            return Mathf.Min(legend * 0.001f, 0.30f);
        }

        /// <summary>击杀单位时判定是否获得传说度（击杀阶位越高获得越多）</summary>
        public static void OnKill(Actor killer, Actor victim)
        {
            if (killer == null || victim == null) return;
            int victimRank = SuperMechAdvancement.GetExactRankIndex(victim);
            if (victimRank < 6) return; // B阶以下不值得传说度

            int baseLegend = victimRank switch
            {
                6 => 1,    // B阶
                7 => 2,    // B+
                8 => 3,    // A阶（天灾级）
                9 => 5,    // A+
                10 => 10,  // S阶（超A级）
                11 => 15,  // S+
                12 => 25,  // SS阶（巅峰超A）
                13 => 50,  // X阶（超神级）
                _ => 0
            };

            if (baseLegend > 0)
            {
                string victimName = SuperMechRanks.All[victimRank].name;
                AddLegend(killer, baseLegend, $"击杀{victimName}");
            }
        }

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _legend.Remove(a.data.id);
            _lastDeed.Remove(a.data.id);
        }

        public static void Clear()
        {
            _legend.Clear();
            _lastDeed.Clear();
        }
    }
}
