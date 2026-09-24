using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超神级突破系统（原著 ch1396/ch1397/ch1398）。
    ///
    /// 【为什么原著只有韩萧突破X阶】
    /// 突破超神级需要三个条件，且有恶性变异风险：
    /// 1. 稳定生命形态：需要特殊物品，否则无限增殖/基因崩溃/细胞独立/宇宙同化
    /// 2. 四个非同系超能者助手：且都要开启神性蜕变，层次越高辅助越好
    /// 3. 超神遗力催化剂：来自突破失败身死的巅峰超A级，只有冲击超神的人才能感知
    ///
    /// 土著没有面板，感知不到超神遗力，也就找不到催化剂——这是核心壁垒。
    /// 而且突破失败会恶性变异身亡，大部分超A不敢尝试。
    ///
    /// 模组实现：
    /// - X阶（index13）不自动晋升，需要手动突破
    /// - 降临者（有面板）能感知超神遗力，可收集
    /// - 土著不能感知超神遗力，无法突破X阶（除非有降临者帮助）
    /// - 突破有成功率，失败则恶性变异（扣血/变异/死亡）
    /// </summary>
    public static class SuperMechTranscendence
    {
        // 超神遗力数量（unit.id -> count）
        private static readonly Dictionary<long, int> _legacyPower = new Dictionary<long, int>();
        // 突破冷却（防止连续尝试）
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();
        // 突破成功记录
        private static readonly Dictionary<long, bool> _transcended = new Dictionary<long, bool>();

        // 超神遗力在地图上的生成点（模拟原著中分布在宇宙各处）
        private static readonly List<WorldTile> _legacySpawns = new List<WorldTile>();
        private static bool _legacyInitialized = false;

        /// <summary>获取单位持有的超神遗力数量。</summary>
        public static int GetLegacyPower(Actor a)
        {
            if (a == null) return 0;
            int v; _legacyPower.TryGetValue(a.id, out v); return v;
        }

        /// <summary>增加超神遗力（降临者感知后收集）。</summary>
        public static void AddLegacyPower(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            int cur = GetLegacyPower(a);
            _legacyPower[a.id] = cur + amount;
            Debug.Log($"[超神机械师] {a.Name} 获得{amount}份超神遗力（共{cur + amount}）");
        }

        /// <summary>是否已突破超神级。</summary>
        public static bool IsTranscended(Actor a)
        {
            if (a == null) return false;
            bool v; _transcended.TryGetValue(a.id, out v); return v;
        }

        /// <summary>
        /// 检查是否可以尝试突破超神级。
        /// 条件（ch1396）：
        /// 1. SS阶（index12）以上
        /// 2. 神性蜕变职业路线+种族路线各≥5层
        /// 3. 至少1份超神遗力（催化剂）
        /// 4. 四个非同系超能者助手（简化：周围有3个不同系的S阶以上单位）
        /// 5. 降临者才能感知超神遗力（土著需要降临者分享）
        /// </summary>
        public static bool CanAttempt(Actor a)
        {
            if (a == null) return false;
            if (IsTranscended(a)) return false;
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return false; // 需要SS阶以上
            if (!SuperMechDivinity.IsDivineAwakened(a)) return false;
            if (SuperMechDivinity.GetProfLayers(a) < 5) return false;
            if (SuperMechDivinity.GetSpeciesLayers(a) < 5) return false;
            if (GetLegacyPower(a) < 1) return false; // 需要超神遗力
            // 突破冷却
            float cd;
            if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd) return false;
            return true;
        }

        /// <summary>
        /// 尝试突破超神级（ch1396）。
        /// 基础成功率33.3%，满足条件每个+33.3%：
        /// - 稳定生命形态（有宇宙宝物/特殊物品）+33.3%
        /// - 四个非同系助手 +33.3%
        /// - 超神遗力催化剂 +33.3%（必须有）
        /// 失败则恶性变异：扣血/变异/可能死亡
        /// </summary>
        public static bool AttemptTranscend(Actor a)
        {
            if (!CanAttempt(a)) return false;

            // 计算成功率
            float successRate = 0.333f; // 基础33.3%

            // 条件1：稳定生命形态（有宇宙宝物）
            if (a.hasTrait("sm_cosmic_relic_owner")) successRate += 0.333f;

            // 条件2：四个非同系助手（简化：周围有不同系的S阶单位）
            int assistantCount = CountNearbyAssistants(a);
            if (assistantCount >= 3) successRate += 0.333f;

            // 条件3：超神遗力催化剂（必须有，已在CanAttempt检查）
            // 每份超神遗力额外+5%
            successRate += GetLegacyPower(a) * 0.05f;

            // 降临者有面板，能看到精确成功率，额外+10%（规划优势）
            if (SuperMechAwakened.IsAwakened(a)) successRate += 0.1f;

            successRate = Mathf.Clamp(successRate, 0.1f, 0.95f);

            Debug.Log($"[超神机械师] {a.Name} 尝试突破超神级，成功率{successRate:P0}");

            // 消耗1份超神遗力
            _legacyPower[a.id] = GetLegacyPower(a) - 1;
            // 设置冷却（30秒）
            _cooldown[a.id] = Time.time + 30f;

            // 判定
            if (Random.value < successRate)
            {
                // 突破成功！
                _transcended[a.id] = true;
                // 晋升到X阶
                SuperMechAdvancement.SetExactRank(a, 13);
                // 超神级奖励
                SuperMechQi.AddQiMax(a, 500000f);
                var stats = SuperMechStats.Of(a);
                if (stats != null)
                {
                    stats["intelligence"] = (stats["intelligence"] ?? 0f) + 200f;
                    stats["damage"] = (stats["damage"] ?? 0f) + 500f;
                    stats["health"] = (stats["health"] ?? 0f) + 5000f;
                    stats["multiplier_damage"] = (stats["multiplier_damage"] ?? 1f) + 0.5f;
                }
                Debug.Log($"[超神机械师] {a.Name} 突破超神级成功！！！");
                return true;
            }
            else
            {
                // 突破失败，恶性变异（ch1396：无限增殖/基因崩溃/细胞独立/宇宙同化）
                float damage = a.data.max_health * 0.4f;
                a.data.health = Mathf.Max(1f, a.data.health - damage);
                // 随机变异：扣属性
                var stats = SuperMechStats.Of(a);
                if (stats != null)
                {
                    stats["intelligence"] = Mathf.Max(0, (stats["intelligence"] ?? 0f) - 20f);
                    stats["damage"] = Mathf.Max(0, (stats["damage"] ?? 0f) - 50f);
                }
                Debug.Log($"[超神机械师] {a.Name} 突破失败，恶性变异！扣血{damage:F0}，属性下降");
                return false;
            }
        }

        /// <summary>统计周围不同系的S阶以上助手数量。</summary>
        private static int CountNearbyAssistants(Actor a)
        {
            if (a == null || a.current_tile == null) return 0;
            HashSet<string> classes = new HashSet<string>();
            string myClass = SuperMechBranch.GetClass(a);
            // 检查周围9格
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    WorldTile t = a.current_tile.getNeighbor(dx, dy);
                    if (t == null) continue;
                    t.doUnits(u =>
                    {
                        if (u == null || u == a) return;
                        if (SuperMechAdvancement.GetExactRankIndex(u) < 10) return; // S阶以上
                        string cls = SuperMechBranch.GetClass(u);
                        if (!string.IsNullOrEmpty(cls) && cls != myClass)
                            classes.Add(cls);
                    });
                }
            }
            return classes.Count;
        }

        /// <summary>
        /// Tick：降临者感知超神遗力（ch1396：只有冲击超神的人才能感知）。
        /// SS阶以上的降临者会随机感知到超神遗力并收集。
        /// 土著不能感知。
        /// </summary>
        public static void TickLegacySense()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAwakened.IsAwakened(a)) continue; // 只有降临者能感知
                if (SuperMechAdvancement.GetExactRankIndex(a) < 12) continue; // SS阶以上
                if (IsTranscended(a)) continue;

                // 每tick有小概率感知到超神遗力（模拟在宇宙中寻找）
                if (Random.value < 0.02f) // 2%概率
                {
                    AddLegacyPower(a, 1);
                }
            }
        }

        /// <summary>获取突破状态文本（面板显示用）。</summary>
        public static string GetStatusText(Actor a)
        {
            if (a == null) return "";
            if (IsTranscended(a)) return "已突破超神级！";
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return $"需SS阶（当前{SuperMechRanks.GetRankName(rank)}）";
            if (!SuperMechDivinity.IsDivineAwakened(a)) return "需触发神性蜕变";
            int prof = SuperMechDivinity.GetProfLayers(a);
            int spec = SuperMechDivinity.GetSpeciesLayers(a);
            if (prof < 5 || spec < 5) return $"神性蜕变不足（职业{prof}/5 种族{spec}/5）";
            int legacy = GetLegacyPower(a);
            if (legacy < 1)
            {
                if (SuperMechAwakened.IsAwakened(a))
                    return "需超神遗力（正在感知中...）";
                else
                    return "需超神遗力（土著无法感知，需降临者帮助）";
            }
            float cd;
            if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd)
                return $"冷却中（{cd - Time.time:F0}秒）";
            return $"可突破！遗力{legacy}份，助手{CountNearbyAssistants(a)}/3";
        }

        /// <summary>清除数据。</summary>
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _legacyPower.Remove(a.id);
            _cooldown.Remove(a.id);
            _transcended.Remove(a.id);
        }
    }
}
