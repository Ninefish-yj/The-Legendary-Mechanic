using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechDivinity
    {
        private static readonly Dictionary<long, int> _points = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _profLayers = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _speciesLayers = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _insightProgress = new Dictionary<long, float>();
        private static readonly Dictionary<long, bool> _awakened = new Dictionary<long, bool>();

        public const int PointsPerLayer = 3;
        public const int MaxLayers = 10;

        public static int GetPoints(Actor a)
        {
            if (a == null) return 0;
            int v; _points.TryGetValue(a.id, out v); return v;
        }

        public static int GetProfLayers(Actor a)
        {
            if (a == null) return 0;
            int v; _profLayers.TryGetValue(a.id, out v); return v;
        }

        public static int GetSpeciesLayers(Actor a)
        {
            if (a == null) return 0;
            int v; _speciesLayers.TryGetValue(a.id, out v); return v;
        }

        public static int GetTotalLayers(Actor a)
        {
            return GetProfLayers(a) + GetSpeciesLayers(a);
        }

        public static bool IsDivineAwakened(Actor a)
        {
            if (a == null) return false;
            bool v; _awakened.TryGetValue(a.id, out v); return v;
        }

        public static void AddPoints(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            int cur = GetPoints(a);
            _points[a.id] = cur + amount;
            Debug.Log($"[超神机械师] {a.name} 获得{amount}神性蜕变点数（共{cur + amount}）");
        }

        public static void SetPoints(Actor a, int amount)
        {
            if (a == null) return;
            _points[a.id] = Mathf.Max(0, amount);
        }

        public static void SetLayers(Actor a, int profLayers, int speciesLayers)
        {
            if (a == null) return;
            _profLayers[a.id] = Mathf.Clamp(profLayers, 0, MaxLayers);
            _speciesLayers[a.id] = Mathf.Clamp(speciesLayers, 0, MaxLayers);
        }

        public static bool SpendPoints(Actor a, string route, int layers = 1)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false; // 只有降临者能直接加点
            int cost = layers * PointsPerLayer;
            int cur = GetPoints(a);
            if (cur < cost) return false;

            if (route == "profession")
            {
                int curLayers = GetProfLayers(a);
                if (curLayers >= MaxLayers) return false;
                _profLayers[a.id] = curLayers + layers;
                ApplyProfBonus(a, layers);
            }
            else if (route == "species")
            {
                int curLayers = GetSpeciesLayers(a);
                if (curLayers >= MaxLayers) return false;
                _speciesLayers[a.id] = curLayers + layers;
                ApplySpeciesBonus(a, layers);
            }
            _points[a.id] = cur - cost;
            return true;
        }

        public static int AwardAdvancementPoints(Actor a)
        {
            if (a == null) return 0;
            int points = 1; // 基础1点
            var stats = SuperMechStats.Of(a);
            if (stats == null) { AddPoints(a, points); return points; }

            float maxStat = Mathf.Max(stats["damage"], stats["health"],
                stats["intelligence"], stats["stamina"], stats["armor"]);
            if (maxStat > 20000) points++;

            float[] allStats = { stats["damage"], stats["health"],
                stats["intelligence"], stats["stamina"], stats["armor"] };
            System.Array.Sort(allStats);
            if (allStats.Length >= 2 && allStats[allStats.Length - 2] > 15000) points++;

            if (SuperMechAdvancement.CalcOnar(a) > 85000) points++;

            if (SuperMechPotential.GetUnlockedCount(a) >= 40) points++;

            if (a.hasTrait("sm_cosmic_relic_owner")) points++;

            AddPoints(a, points);
            Debug.Log($"[超神机械师] {a.name} 进阶获得{points}神性蜕变点数");
            return points;
        }

        public static bool AwardCraftingPoints(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false; // 只有降临者能获得
            AddPoints(a, 1);
            return true;
        }

        public static void TickNativeInsight()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f; // 分组4：每4次UnifiedTick调用才跑一次本系统

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!IsDivineAwakened(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者直接加点，不走感悟转化

                int pts = GetPoints(a);
                if (pts <= 0) continue;

                float progress;
                if (!_insightProgress.TryGetValue(a.id, out progress)) progress = 0;
                progress += 0.3f * tickInterval; // 基础转化速度
                if (a.hasTrait("sm_refinement")) progress *= 1.5f;
                var destiny = SuperMechIntuition.GetDestiny(a);
                if (destiny != null && destiny.completed) progress *= 2f;

                if (progress >= 100f)
                {
                    progress = 0;
                    if (pts >= PointsPerLayer)
                    {
                        _points[a.id] = pts - PointsPerLayer;
                        int prof = GetProfLayers(a);
                        int spec = GetSpeciesLayers(a);
                        if (prof <= spec && prof < MaxLayers)
                        {
                            _profLayers[a.id] = prof + 1;
                            ApplyProfBonus(a, 1);
                        }
                        else if (spec < MaxLayers)
                        {
                            _speciesLayers[a.id] = spec + 1;
                            ApplySpeciesBonus(a, 1);
                        }
                        Debug.Log($"[超神机械师] {a.name}（星海人）感悟转化为1层神性蜕变");
                    }
                }
                _insightProgress[a.id] = progress;
            }
        }

        public static void TriggerDivinity(Actor a)
        {
            if (a == null) return;
            if (IsDivineAwakened(a)) return;
            _awakened[a.id] = true;
            a.addTrait("sm_divinity_ascended");
            AddPoints(a, 2);
            Debug.Log($"[超神机械师] {a.name} 触发神性蜕变！获得2点初始点数");
        }

        private static void ApplyProfBonus(Actor a, int layers)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;
            string cls = SuperMechBranch.GetClass(a);
            switch (cls)
            {
                case "sm_divinity_735":
                    stats["intelligence"] = (stats["intelligence"]) + 50f * layers;
                    stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) + 0.05f * layers;
                    stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.1f * layers;
                    break;
                case "sm_divinity_736":
                    stats["damage"] = (stats["damage"]) + 80f * layers;
                    stats["warfare"] = (stats["warfare"]) + 30f * layers;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.05f * layers;
                    break;
                default:
                    stats["intelligence"] = (stats["intelligence"]) + 40f * layers;
                    stats["mana"] = (stats["mana"]) + 100f * layers;
                    stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) + 0.05f * layers;
                    break;
            }
        }

        private static void ApplySpeciesBonus(Actor a, int layers)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;
            stats["health"] = (stats["health"]) + 200f * layers;
            stats["stamina"] = (stats["stamina"]) + 100f * layers;
            stats["armor"] = (stats["armor"]) + 10f * layers;
            stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) + 0.05f * layers;
            stats["lifespan"] = (stats["lifespan"]) + 500f * layers;
        }

        public static void Clear() { _points.Clear(); _profLayers.Clear(); _speciesLayers.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_points, alive);
            removed += SuperMechCleanup.CleanDict(_profLayers, alive);
            removed += SuperMechCleanup.CleanDict(_speciesLayers, alive);
            removed += SuperMechCleanup.CleanDict(_insightProgress, alive);
            removed += SuperMechCleanup.CleanDict(_awakened, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _points.Remove(a.id);
            _profLayers.Remove(a.id);
            _speciesLayers.Remove(a.id);
            _insightProgress.Remove(a.id);
            _awakened.Remove(a.id);
        }
    }
}
