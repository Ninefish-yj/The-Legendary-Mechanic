using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechDivinity
    {
        public static readonly Dictionary<long, int> _points = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _profLayers = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _speciesLayers = new Dictionary<long, int>();
        public static readonly Dictionary<long, float> _insightProgress = new Dictionary<long, float>();
        public static readonly Dictionary<long, bool> _awakened = new Dictionary<long, bool>();
        // 追踪已应用的神性加成层数，避免存档加载后属性丢失或重复叠加
        private static readonly Dictionary<long, int> _appliedProfLayers = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _appliedSpeciesLayers = new Dictionary<long, int>();

        public const int PointsPerLayer = 3;
        public const int MaxLayers = 10;

        public static int GetPoints(Actor a)
        {
            if (a == null) return 0;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && ctx.divinityPoints > 0) return ctx.divinityPoints;
            int v; _points.TryGetValue(a.id, out v); return v;
        }

        public static int GetProfLayers(Actor a)
        {
            if (a == null) return 0;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && ctx.divinityProfLayers > 0) return ctx.divinityProfLayers;
            int v; _profLayers.TryGetValue(a.id, out v); return v;
        }

        public static int GetSpeciesLayers(Actor a)
        {
            if (a == null) return 0;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && ctx.divinitySpeciesLayers > 0) return ctx.divinitySpeciesLayers;
            int v; _speciesLayers.TryGetValue(a.id, out v); return v;
        }

        public static int GetTotalLayers(Actor a)
        {
            return GetProfLayers(a) + GetSpeciesLayers(a);
        }

        public static bool IsDivineAwakened(Actor a)
        {
            if (a == null) return false;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null) return ctx.divinityTriggered;
            bool v; _awakened.TryGetValue(a.id, out v); return v;
        }

        public static void AddPoints(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            int cur = GetPoints(a);
            SetPoints(a, cur + amount);
        }

        public static void SetPoints(Actor a, int amount)
        {
            if (a == null) return;
            int clamped = Mathf.Max(0, amount);
            _points[a.id] = clamped;
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.divinityPoints = clamped;
        }

        public static void SetLayers(Actor a, int profLayers, int speciesLayers)
        {
            if (a == null) return;
            int p = Mathf.Clamp(profLayers, 0, MaxLayers);
            int s = Mathf.Clamp(speciesLayers, 0, MaxLayers);
            _profLayers[a.id] = p;
            _speciesLayers[a.id] = s;
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) { ctx.divinityProfLayers = p; ctx.divinitySpeciesLayers = s; }

            // 存档加载后重新应用未应用的加成（修复属性丢失bug）
            int appliedP = _appliedProfLayers.TryGetValue(a.id, out int ap) ? ap : 0;
            int appliedS = _appliedSpeciesLayers.TryGetValue(a.id, out int as_) ? as_ : 0;
            if (p > appliedP) { ApplyProfBonus(a, p - appliedP); _appliedProfLayers[a.id] = p; }
            if (s > appliedS) { ApplySpeciesBonus(a, s - appliedS); _appliedSpeciesLayers[a.id] = s; }
        }

        public static bool SpendPoints(Actor a, string route, int layers = 1)
        {
            if (a == null) return false;
            if (!SuperMechActorContextRegistry.IsAwakened(a)) return false;
            int cost = layers * PointsPerLayer;
            int cur = GetPoints(a);
            if (cur < cost) return false;

            if (route == "profession")
            {
                int curLayers = GetProfLayers(a);
                if (curLayers >= MaxLayers) return false;
                _profLayers[a.id] = curLayers + layers;
                ApplyProfBonus(a, layers);
                _appliedProfLayers[a.id] = curLayers + layers;
            }
            else if (route == "species")
            {
                int curLayers = GetSpeciesLayers(a);
                if (curLayers >= MaxLayers) return false;
                _speciesLayers[a.id] = curLayers + layers;
                ApplySpeciesBonus(a, layers);
                _appliedSpeciesLayers[a.id] = curLayers + layers;
            }
            _points[a.id] = cur - cost;
            return true;
        }

        public static int AwardAdvancementPoints(Actor a)
        {
            if (a == null) return 0;
            int points = 1;
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
            return points;
        }

        public static bool AwardCraftingPoints(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechActorContextRegistry.IsAwakened(a)) return false;
            AddPoints(a, 1);
            return true;
        }

        public static void TickNativeInsight()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!IsDivineAwakened(a)) continue;
                if (SuperMechActorContextRegistry.IsAwakened(a)) continue;

                int pts = GetPoints(a);
                if (pts <= 0) continue;

                float progress;
                if (!_insightProgress.TryGetValue(a.id, out progress)) progress = 0;
                progress += 0.3f * tickInterval;
                if (SuperMechQiRefine.HasRefineMethod(a)) progress *= 1.5f;
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
                            _appliedProfLayers[a.id] = prof + 1;
                        }
                        else if (spec < MaxLayers)
                        {
                            _speciesLayers[a.id] = spec + 1;
                            ApplySpeciesBonus(a, 1);
                            _appliedSpeciesLayers[a.id] = spec + 1;
                        }
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
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.divinityTriggered = true;
            AddPoints(a, 2);
            // v0.46.0：神性蜕变获得圣所碎片（权限），原著第1039章
            SuperMechSanctuary.GrantAuthorityFromDivinity(a);
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

            // v0.89.2 神性蜕变种族路线对接自定义生物种族（对照原著：不同种族的神性蜕变方向不同）
            string speciesId = SuperMechSpeciesEvolution.GetSpeciesId(a.data.id);
            if (speciesId != "human")
            {
                switch (speciesId)
                {
                    case "orc_luhan":  // 卢翰兽人：力量+耐力
                        stats["damage"] = (stats["damage"]) + 30f * layers;
                        stats["health"] = (stats["health"]) + 100f * layers;
                        break;
                    case "elf":  // 精灵：敏捷+神秘
                        stats["attack_speed"] = (stats["attack_speed"]) + 0.03f * layers;
                        stats["mana"] = (stats["mana"]) + 50f * layers;
                        break;
                    case "dwarf":  // 矮人：耐力+护甲
                        stats["armor"] = (stats["armor"]) + 15f * layers;
                        stats["health"] = (stats["health"]) + 150f * layers;
                        break;
                    case "zerg":  // 虫族：力量+繁殖
                        stats["damage"] = (stats["damage"]) + 25f * layers;
                        stats["lifespan"] = (stats["lifespan"]) + 200f * layers;
                        break;
                    case "silver_spirit":  // 银灵人：神秘+智力
                        stats["mana"] = (stats["mana"]) + 100f * layers;
                        stats["intelligence"] = (stats["intelligence"]) + 20f * layers;
                        break;
                    case "mech_life":  // 机械族：全属性均衡
                        stats["damage"] = (stats["damage"]) + 15f * layers;
                        stats["armor"] = (stats["armor"]) + 10f * layers;
                        stats["health"] = (stats["health"]) + 50f * layers;
                        break;
                    case "void_dragon":  // 虚空龙族：全属性大幅提升
                        stats["damage"] = (stats["damage"]) + 50f * layers;
                        stats["health"] = (stats["health"]) + 300f * layers;
                        stats["armor"] = (stats["armor"]) + 25f * layers;
                        break;
                    case "star_beast":  // 星游兽：耐力+寿命
                        stats["stamina"] = (stats["stamina"]) + 200f * layers;
                        stats["lifespan"] = (stats["lifespan"]) + 1000f * layers;
                        break;
                    case "black_star":  // 黑星族：全属性+机械亲和
                        stats["damage"] = (stats["damage"]) + 40f * layers;
                        stats["health"] = (stats["health"]) + 200f * layers;
                        stats["intelligence"] = (stats["intelligence"]) + 30f * layers;
                        break;
                }
            }
        }

        public static void Clear() { _points.Clear(); _profLayers.Clear(); _speciesLayers.Clear(); _appliedProfLayers.Clear(); _appliedSpeciesLayers.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_points, alive);
            removed += SuperMechCleanup.CleanDict(_profLayers, alive);
            removed += SuperMechCleanup.CleanDict(_speciesLayers, alive);
            removed += SuperMechCleanup.CleanDict(_insightProgress, alive);
            removed += SuperMechCleanup.CleanDict(_awakened, alive);
            removed += SuperMechCleanup.CleanDict(_appliedProfLayers, alive);
            removed += SuperMechCleanup.CleanDict(_appliedSpeciesLayers, alive);
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
            _appliedProfLayers.Remove(a.id);
            _appliedSpeciesLayers.Remove(a.id);
        }
    }
}
