using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 神性蜕变系统（原著 ch1039/ch1043/ch1053/ch1178/ch1204）。
    ///
    /// 【完整设定】
    /// 1. 触发：巅峰超A级（S阶），78000能级
    /// 2. 两条路线（ch1039）：
    ///    - 职业路线：加强主职业能力
    ///    - 种族路线：提升种族天赋+额外属性补完
    /// 3. 神性蜕变点数（ch1043）：
    ///    - 进阶（生命层次升华）获得，数量不固定
    ///    - 影响条件：最高属性>20000、次要属性>15000、能级>85000、
    ///      职业知识全满级、与宇宙宝物灵魂链接，每满足一个多1点
    ///    - 打造宇宙宝物级装备也给点数（ch1052/1053）
    /// 4. 降临者vs土著核心差异：
    ///    - 降临者（有面板）：点数可直接加点，打造宇宙宝物额外得点数，升级瞬间增强
    ///    - 土著（无面板）：点数是"经验/感悟"，需长时间锻炼转化为层数（ch1178）
    /// 5. 冥冥感应（ch1204）：高段蜕变产生，感应到蜕变使命
    /// 6. 进阶门槛（ch1043）：后续进阶要求神性蜕变达到层数，不够则卡死
    /// </summary>
    public static class SuperMechDivinity
    {
        // 神性蜕变点数（unit.id -> points）
        private static readonly Dictionary<long, int> _points = new Dictionary<long, int>();
        // 职业路线层数（unit.id -> layers）
        private static readonly Dictionary<long, int> _profLayers = new Dictionary<long, int>();
        // 种族路线层数（unit.id -> layers）
        private static readonly Dictionary<long, int> _speciesLayers = new Dictionary<long, int>();
        // 土著感悟转化进度（unit.id -> 0-100，满了转化1层）
        private static readonly Dictionary<long, float> _insightProgress = new Dictionary<long, float>();
        // 是否已触发神性蜕变
        private static readonly Dictionary<long, bool> _awakened = new Dictionary<long, bool>();

        // 每层需要的点数
        public const int PointsPerLayer = 3;
        // 最大层数
        public const int MaxLayers = 10;

        /// <summary>获取神性蜕变点数。</summary>
        public static int GetPoints(Actor a)
        {
            if (a == null) return 0;
            int v; _points.TryGetValue(a.id, out v); return v;
        }

        /// <summary>获取职业路线层数。</summary>
        public static int GetProfLayers(Actor a)
        {
            if (a == null) return 0;
            int v; _profLayers.TryGetValue(a.id, out v); return v;
        }

        /// <summary>获取种族路线层数。</summary>
        public static int GetSpeciesLayers(Actor a)
        {
            if (a == null) return 0;
            int v; _speciesLayers.TryGetValue(a.id, out v); return v;
        }

        /// <summary>是否已触发神性蜕变。</summary>
        public static bool IsDivineAwakened(Actor a)
        {
            if (a == null) return false;
            bool v; _awakened.TryGetValue(a.id, out v); return v;
        }

        /// <summary>增加神性蜕变点数（进阶/打造宝物用）。</summary>
        public static void AddPoints(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            int cur = GetPoints(a);
            _points[a.id] = cur + amount;
            Debug.Log($"[超神机械师] {a.Name} 获得{amount}神性蜕变点数（共{cur + amount}）");
        }

        /// <summary>
        /// 降临者：消耗点数加点到职业/种族路线（直接生效）。
        /// 土著不能直接加点，只能靠感悟慢慢转化。
        /// </summary>
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

        /// <summary>
        /// 进阶时获得神性蜕变点数（ch1043）。
        /// 基础1点，每满足一个条件多1点。
        /// 降临者和土著都能通过进阶获得点数。
        /// </summary>
        public static int AwardAdvancementPoints(Actor a)
        {
            if (a == null) return 0;
            int points = 1; // 基础1点
            var stats = SuperMechStats.Of(a);
            if (stats == null) { AddPoints(a, points); return points; }

            // 条件1：最高属性>20000
            float maxStat = Mathf.Max(stats["damage"] ?? 0, stats["health"] ?? 0,
                stats["intelligence"] ?? 0, stats["stamina"] ?? 0, stats["armor"] ?? 0);
            if (maxStat > 20000) points++;

            // 条件2：次要属性>15000（简化：第二高属性）
            float[] allStats = { stats["damage"] ?? 0, stats["health"] ?? 0,
                stats["intelligence"] ?? 0, stats["stamina"] ?? 0, stats["armor"] ?? 0 };
            System.Array.Sort(allStats);
            if (allStats.Length >= 2 && allStats[allStats.Length - 2] > 15000) points++;

            // 条件3：能级>85000
            if (SuperMechAdvancement.CalcOnar(a) > 85000) points++;

            // 条件4：职业知识全满级（简化：解锁>40个节点）
            if (SuperMechPotential.GetUnlockedCount(a) >= 40) points++;

            // 条件5：与宇宙宝物灵魂链接（简化：持有宇宙宝物）
            if (a.hasTrait("sm_cosmic_relic_owner")) points++;

            AddPoints(a, points);
            Debug.Log($"[超神机械师] {a.Name} 进阶获得{points}神性蜕变点数");
            return points;
        }

        /// <summary>
        /// 打造宇宙宝物获得神性蜕变点数（ch1052/1053）。
        /// 【降临者专属】只有有面板的玩家能通过这个渠道获得点数！
        /// 土著没有面板，察觉不到这个效果。
        /// </summary>
        public static bool AwardCraftingPoints(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false; // 只有降临者能获得
            AddPoints(a, 1);
            return true;
        }

        /// <summary>
        /// Tick：土著感悟转化为神性蜕变层数（ch1178）。
        /// 土著的点数是"经验/感悟"，需要长时间锻炼才能转化。
        /// 降临者不需要这个（直接加点）。
        /// </summary>
        public static void TickNativeInsight()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!IsDivineAwakened(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者直接加点，不走感悟转化

                int pts = GetPoints(a);
                if (pts <= 0) continue;

                // 感悟转化进度（非常缓慢，模拟"长时间锻炼"）
                float progress;
                if (!_insightProgress.TryGetValue(a.id, out progress)) progress = 0;
                progress += 0.3f * tickInterval; // 基础转化速度
                // 有提炼法加速
                if (a.hasTrait("sm_refinement")) progress *= 1.5f;
                // 冥冥感应完成使命加速
                var destiny = SuperMechIntuition.GetDestiny(a);
                if (destiny != null && destiny.completed) progress *= 2f;

                if (progress >= 100f)
                {
                    progress = 0;
                    // 消耗点数，转化为1层（随机选职业或种族路线）
                    if (pts >= PointsPerLayer)
                    {
                        _points[a.id] = pts - PointsPerLayer;
                        // 土著优先转化职业路线
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
                        Debug.Log($"[超神机械师] {a.Name}（土著）感悟转化为1层神性蜕变");
                    }
                }
                _insightProgress[a.id] = progress;
            }
        }

        /// <summary>触发神性蜕变（S阶时调用）。</summary>
        public static void TriggerDivinity(Actor a)
        {
            if (a == null) return;
            if (IsDivineAwakened(a)) return;
            _awakened[a.id] = true;
            a.addTrait("sm_divinity_ascended");
            // 首次触发给2点（ch1039：韩萧第一次获得2点）
            AddPoints(a, 2);
            Debug.Log($"[超神机械师] {a.Name} 触发神性蜕变！获得2点初始点数");
        }

        /// <summary>职业路线加成（每层）。</summary>
        private static void ApplyProfBonus(Actor a, int layers)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;
            string cls = SuperMechBranch.GetClass(a);
            // 职业路线加强职业能力
            switch (cls)
            {
                case "机械系":
                    stats["intelligence"] = (stats["intelligence"] ?? 0f) + 50f * layers;
                    stats["multiplier_damage"] = (stats["multiplier_damage"] ?? 1f) + 0.05f * layers;
                    stats["experience"] = (stats["experience"] ?? 1f) + 0.1f * layers;
                    break;
                case "武道系":
                    stats["damage"] = (stats["damage"] ?? 0f) + 80f * layers;
                    stats["warfare"] = (stats["warfare"] ?? 0f) + 30f * layers;
                    stats["attack_speed"] = (stats["attack_speed"] ?? 0f) + 0.05f * layers;
                    break;
                default:
                    stats["intelligence"] = (stats["intelligence"] ?? 0f) + 40f * layers;
                    stats["mana"] = (stats["mana"] ?? 0f) + 100f * layers;
                    stats["multiplier_damage"] = (stats["multiplier_damage"] ?? 1f) + 0.05f * layers;
                    break;
            }
        }

        /// <summary>种族路线加成（每层）。</summary>
        private static void ApplySpeciesBonus(Actor a, int layers)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;
            // 种族路线提升种族天赋+属性补完
            stats["health"] = (stats["health"] ?? 0f) + 200f * layers;
            stats["stamina"] = (stats["stamina"] ?? 0f) + 100f * layers;
            stats["armor"] = (stats["armor"] ?? 0f) + 10f * layers;
            stats["multiplier_health"] = (stats["multiplier_health"] ?? 1f) + 0.05f * layers;
            stats["lifespan"] = (stats["lifespan"] ?? 0f) + 500f * layers;
        }

        /// <summary>清除数据。</summary>
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
