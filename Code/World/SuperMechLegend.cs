using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechLegend
    {
        private static readonly Dictionary<long, int> _legend = new Dictionary<long, int>();
        private static readonly Dictionary<long, string> _lastDeed = new Dictionary<long, string>();

        public static readonly string[] TierNames = {
            "sm_legend_876", "sm_legend_877", "sm_legend_878", "sm_legend_879", "sm_legend_880", "sm_legend_881"
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
            if (legend >= 500) return 5;
            if (legend >= 200) return 4;
            if (legend >= 80) return 3;
            if (legend >= 30) return 2;
            if (legend >= 10) return 1;
            return 0;
        }

        public static string GetTierName(Actor a)
        {
            return TierNames[Mathf.Clamp(GetTier(GetLegend(a)), 0, 5)];
        }

        public static float GetBreakthroughBonus(Actor a)
        {
            int legend = GetLegend(a);
            return Mathf.Min(legend * 0.001f, 0.30f);
        }

        public static void OnKill(Actor killer, Actor victim)
        {
            if (killer == null || victim == null) return;
            int victimRank = SuperMechAdvancement.GetExactRankIndex(victim);
            if (victimRank < 6) return;

            int baseLegend = victimRank switch
            {
                6 => 1,
                7 => 2,
                8 => 3,
                9 => 5,
                10 => 10,
                11 => 15,
                12 => 25,
                13 => 50,
                _ => 0
            };

            if (baseLegend > 0)
            {
                string victimName = SuperMechRanks.All[victimRank].name;
                AddLegend(killer, baseLegend, $"sm_legend_882");
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

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_legend, alive);
            return removed;
        }
    }
}
