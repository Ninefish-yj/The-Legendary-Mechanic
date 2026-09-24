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
        /// 前置条件（ch1396）：
        /// 1. SS阶（index12）以上，且神性蜕变已触发
        /// 2. 处在"可进阶状态"（临界状态）
        /// 3. 超神遗力只有处在可进阶状态的巅峰超A才能感知
        /// </summary>
        public static bool CanAttempt(Actor a)
        {
            if (a == null) return false;
            if (IsTranscended(a)) return false;
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return false; // 需要SS阶以上
            if (!SuperMechDivinity.IsDivineAwakened(a)) return false;
            // 突破冷却
            float cd;
            if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd) return false;
            return true;
        }

        /// <summary>
        /// 检查三个突破条件是否全部满足（ch1396关键设定）。
        /// "若未达成全部条件，即使进阶成功，阶位不会发生变化"
        /// "若进阶时完全触发三项条件，则会开启神化进阶"
        /// </summary>
        public static bool AllConditionsMet(Actor a)
        {
            if (a == null) return false;
            // 条件1：稳定生命形态（有宇宙宝物/特殊稳定物品）
            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            // 条件2：四个非同系助手（周围有4个不同系的S阶以上单位，且都有神性蜕变）
            bool cond2 = CountNearbyAssistants(a) >= 4;
            // 条件3：超神遗力催化剂
            bool cond3 = GetLegacyPower(a) >= 1;
            return cond1 && cond2 && cond3;
        }

        /// <summary>
        /// 尝试突破超神级（ch1396）。
        /// 基础失败率90%~96%（即成功率4%~10%），每个条件+33.3%成功率。
        /// 【关键】三个条件全部满足才能真正突破到X阶（神化进阶）。
        /// 未满足全部条件时，即使成功也只是常规进阶，阶位不变。
        /// 失败则恶性变异：扣血/属性下降/可能死亡。
        /// </summary>
        public static bool AttemptTranscend(Actor a)
        {
            if (!CanAttempt(a)) return false;

            // 计算成功率（ch1396：基础90-96%失败率，即4-10%成功率）
            float successRate = Random.Range(0.04f, 0.10f); // 基础4%~10%

            // 条件1：稳定生命形态 +33.3%
            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            if (cond1) successRate += 0.333f;

            // 条件2：四个非同系助手 +33.3%
            int assistantCount = CountNearbyAssistants(a);
            bool cond2 = assistantCount >= 4;
            if (cond2) successRate += 0.333f;

            // 条件3：超神遗力催化剂 +33.3%（必须有才能感知）
            bool cond3 = GetLegacyPower(a) >= 1;
            if (cond3) successRate += 0.333f;

            // 每份额外超神遗力+2%
            successRate += Mathf.Max(0, GetLegacyPower(a) - 1) * 0.02f;

            successRate = Mathf.Clamp(successRate, 0.01f, 0.99f);

            bool allMet = cond1 && cond2 && cond3;

            Debug.Log($"[超神机械师] {a.Name} 冲击超神级，成功率{successRate:P0}，三条件{(allMet ? "全部满足→神化进阶" : "未全满足→即使成功也不升阶")}");

            // 消耗1份超神遗力
            if (cond3) _legacyPower[a.id] = GetLegacyPower(a) - 1;
            // 设置冷却（60秒）
            _cooldown[a.id] = Time.time + 60f;

            // 判定
            if (Random.value < successRate)
            {
                if (allMet)
                {
                    // 三条件全满足→神化进阶→真正突破X阶！
                    _transcended[a.id] = true;
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
                    Debug.Log($"[超神机械师] {a.Name} 神化进阶成功！突破超神级！！！");
                    return true;
                }
                else
                {
                    // 条件未全满足→常规进阶，阶位不变（ch1396原文）
                    Debug.Log($"[超神机械师] {a.Name} 进阶成功但条件未全满足，阶位不变（需三条件全满足才能神化进阶）");
                    // 给少量奖励
                    SuperMechQi.AddQiMax(a, 20000f);
                    return false; // 不算突破成功
                }
            }
            else
            {
                // 突破失败，恶性变异（ch1396：无限增殖/基因崩溃/细胞独立/宇宙同化）
                float damage = a.data.max_health * 0.5f;
                a.data.health = Mathf.Max(1f, a.data.health - damage);
                var stats = SuperMechStats.Of(a);
                if (stats != null)
                {
                    stats["intelligence"] = Mathf.Max(0, (stats["intelligence"] ?? 0f) - 30f);
                    stats["damage"] = Mathf.Max(0, (stats["damage"] ?? 0f) - 80f);
                }
                // 10%概率直接死亡（恶性变异致死）
                if (Random.value < 0.1f && a.data.health > 1f)
                {
                    a.data.health = 0f;
                    Debug.Log($"[超神机械师] {a.Name} 突破失败，恶性变异致死！");
                }
                else
                {
                    Debug.Log($"[超神机械师] {a.Name} 突破失败，恶性变异！扣血{damage:F0}，属性下降");
                }
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
        /// Tick：感知超神遗力（ch1396：只有处在可进阶状态的巅峰超A级才能感知）。
        /// 土著没有面板，但只要达到SS阶+神性蜕变也能感知（原著克苏耶/麦尼逊都是土著）。
        /// 感知到后自动收集（模拟在宇宙中寻找）。
        /// </summary>
        public static void TickLegacySense()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) < 12) continue; // SS阶以上
                if (!SuperMechDivinity.IsDivineAwakened(a)) continue; // 已触发神性蜕变
                if (IsTranscended(a)) continue;
                if (GetLegacyPower(a) >= 3) continue; // 最多存3份

                // 每tick有小概率感知到超神遗力（模拟在宇宙中寻找，ch1396韩萧感知到数十个）
                if (Random.value < 0.015f) // 1.5%概率
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

            // 显示三个条件
            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            int assistants = CountNearbyAssistants(a);
            bool cond2 = assistants >= 4;
            int legacy = GetLegacyPower(a);
            bool cond3 = legacy >= 1;

            string text = $"条件1稳定{(cond1 ? "✓" : "✗")} 条件2助手{assistants}/4{(cond2 ? "✓" : "✗")} 条件3遗力{legacy}{(cond3 ? "✓" : "✗")}";

            if (cond1 && cond2 && cond3)
            {
                float cd;
                if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd)
                    return text + $" 冷却{cd - Time.time:F0}s";
                return text + " 可神化进阶！";
            }
            return text + "（未全满足即使成功也不升阶）";
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
