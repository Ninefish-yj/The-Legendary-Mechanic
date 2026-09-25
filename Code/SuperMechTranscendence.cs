using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超神级突破系统（原著 ch1396/ch1397/ch1398/ch1039）。
    ///
    /// 【完整突破链条】
    /// 1. 气力Lv21 + 能级78000欧纳 → 触发神性蜕变（ch1039）
    /// 2. SS阶（巅峰超A）+ 完成进阶任务 → 进入可进阶状态（临界状态）
    ///    - 机械系进阶任务：弑神之炼（杀五系各一个神性蜕变超A）+ 神工者（打造100种宇宙宝物）
    ///    - 其他系有等价的苛刻任务
    ///    - 韩萧用任务结算卡跳过，土著只能靠水磨工夫完成
    /// 3. 三条件全满足 → 神化进阶，突破X阶
    ///    - 稳定生命形态（宇宙宝物）
    ///    - 四个非同系助手（都开启神性蜕变）
    ///    - 超神遗力催化剂
    ///
    /// 【为什么原著只有韩萧突破】
    /// - 进阶任务极其苛刻（弑神之炼要杀五系各一个巅峰超A）
    /// - 三条件极难同时满足（尤其是四个非同系助手和超神遗力）
    /// - 土著没有面板，看不到精确成功率和条件，只能瞎试
    /// - 失败会恶性变异身亡，大部分超A不敢尝试
    /// - 克苏耶条件齐了但没突破，就是因为进阶任务没完成
    ///
    /// 模组实现：
    /// - X阶（index13）不自动晋升，需要手动突破
    /// - 进阶任务进度随战斗/制造/修炼缓慢增长
    /// - 完成进阶任务后才能感知超神遗力
    /// - 三条件全满足才能真正升阶，否则即使成功也不升阶
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
        // 进阶任务完成记录（ch1396：弑神之炼+神工者，完成后才能进入可进阶状态）
        private static readonly Dictionary<long, bool> _advancementTaskDone = new Dictionary<long, bool>();
        // 进阶任务进度（unit.id -> progress 0-100）
        private static readonly Dictionary<long, float> _advancementProgress = new Dictionary<long, float>();

        // 超神遗力在地图上的生成点（模拟原著中分布在宇宙各处）
        private static readonly List<WorldTile> _legacySpawns = new List<WorldTile>();
        private static bool _legacyInitialized = false;

        /// <summary>是否已完成进阶任务（ch1396：弑神之炼+神工者）。</summary>
        public static bool IsAdvancementTaskDone(Actor a)
        {
            if (a == null) return false;
            bool v; _advancementTaskDone.TryGetValue(a.id, out v); return v;
        }

        /// <summary>获取进阶任务进度（0-100）。</summary>
        public static float GetAdvancementProgress(Actor a)
        {
            if (a == null) return 0;
            float v; _advancementProgress.TryGetValue(a.id, out v); return v;
        }

        /// <summary>
        /// 获取进阶任务描述（按系不同，ch1396机械系=弑神之炼+神工者）。
        /// 其他系原著未明确列出，按逻辑生成等价的苛刻任务。
        /// </summary>
        public static string GetAdvancementTaskName(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            switch (cls)
            {
                case "机械系": return "弑神之炼+神工者";
                case "武道系": return "武道尽头·以武证道";
                case "异能系": return "基因源始·异能归一";
                case "魔法系": return "秘法之巅·元素王座";
                case "念力系": return "灵魂彼岸·念动乾坤";
                default: return "超神试炼";
            }
        }

        /// <summary>
        /// Tick：进阶任务进度（ch1396：完成进阶任务才能进入可进阶状态）。
        /// 对降临者是任务，对土著是冥冥中的试炼/使命。
        /// 进度来源：战斗击杀、制造、修炼、完成冥冥使命。
        /// </summary>
        public static void TickAdvancementTask()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) < 12) continue; // SS阶以上才开始
                if (!SuperMechDivinity.IsDivineAwakened(a)) continue;
                if (IsAdvancementTaskDone(a)) continue;
                if (IsTranscended(a)) continue;

                float progress = GetAdvancementProgress(a);
                // 基础进度（修炼/时间积累，非常缓慢）
                float gain = 0.1f * tickInterval;
                // 战斗中加速（弑神之炼：击杀强敌）
                if (SuperMechQi.IsInCombat(a)) gain += 0.5f * tickInterval;
                // 机械系制造加速（神工者：打造宇宙宝物）
                if (a.hasTrait(SuperMechTraits.ClassMech))
                {
                    // 简化：智力越高，制造进度越快
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        float intel = (stats["intelligence"] == 0f ? 5f : stats["intelligence"]);
                        gain += intel * 0.001f * tickInterval;
                    }
                }
                // 完成冥冥使命加速
                var destiny = SuperMechIntuition.GetDestiny(a);
                if (destiny != null && destiny.completed) gain += 0.3f * tickInterval;
                // 降临者有面板，能看到任务要求，进度略快
                if (SuperMechAwakened.IsAwakened(a)) gain *= 1.2f;

                progress += gain;
                if (progress >= 100f)
                {
                    progress = 100f;
                    _advancementTaskDone[a.id] = true;
                    Debug.Log($"[超神机械师] {a.name} 完成进阶任务【{GetAdvancementTaskName(a)}】！进入可进阶状态，可感知超神遗力");
                }
                _advancementProgress[a.id] = progress;
            }
        }

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
            Debug.Log($"[超神机械师] {a.name} 获得{amount}份超神遗力（共{cur + amount}）");
        }

        /// <summary>直接设置超神遗力（存档恢复用）。</summary>
        public static void SetLegacyPower(Actor a, int amount)
        {
            if (a == null) return;
            _legacyPower[a.id] = Mathf.Max(0, amount);
        }

        /// <summary>是否已突破超神级。</summary>
        public static bool IsTranscended(Actor a)
        {
            if (a == null) return false;
            bool v; _transcended.TryGetValue(a.id, out v); return v;
        }

        /// <summary>标记已突破超神级（存档恢复用）。</summary>
        public static void SetTranscended(Actor a)
        {
            if (a == null) return;
            _transcended[a.id] = true;
        }

        /// <summary>直接设置进阶任务进度（存档恢复用）。</summary>
        public static void SetAdvancementProgress(Actor a, float progress)
        {
            if (a == null) return;
            _advancementProgress[a.id] = progress;
        }

        /// <summary>标记进阶任务完成（存档恢复用）。</summary>
        public static void SetAdvancementTaskDone(Actor a)
        {
            if (a == null) return;
            _advancementTaskDone[a.id] = true;
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
            if (!IsAdvancementTaskDone(a)) return false; // 必须完成进阶任务
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

            Debug.Log($"[超神机械师] {a.name} 冲击超神级，成功率{successRate:P0}，三条件{(allMet ? "全部满足→神化进阶" : "未全满足→即使成功也不升阶")}");

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
                        stats["intelligence"] = (stats["intelligence"]) + 200f;
                        stats["damage"] = (stats["damage"]) + 500f;
                        stats["health"] = (stats["health"]) + 5000f;
                        stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) + 0.5f;
                    }
                    Debug.Log($"[超神机械师] {a.name} 神化进阶成功！突破超神级！！！");
                    return true;
                }
                else
                {
                    // 条件未全满足→常规进阶，阶位不变（ch1396原文）
                    Debug.Log($"[超神机械师] {a.name} 进阶成功但条件未全满足，阶位不变（需三条件全满足才能神化进阶）");
                    // 给少量奖励
                    SuperMechQi.AddQiMax(a, 20000f);
                    return false; // 不算突破成功
                }
            }
            else
            {
                // 突破失败，恶性变异（ch1396：无限增殖/基因崩溃/细胞独立/宇宙同化）
                float damage = a.getMaxHealth() * 0.5f;
                a.data.health = Mathf.Max(1, (int)(a.data.health - damage));
                var stats = SuperMechStats.Of(a);
                if (stats != null)
                {
                    stats["intelligence"] = Mathf.Max(0f, (stats["intelligence"]) - 30f);
                    stats["damage"] = Mathf.Max(0f, (stats["damage"]) - 80f);
                }
                // 10%概率直接死亡（恶性变异致死）
                if (Random.value < 0.1f && a.data.health > 1f)
                {
                    a.data.health = 0;
                    // ch1396/ch1399：突破失败死亡者化身为超神遗力，无法圣所复苏
                    SuperMechSanctuary.MarkTranscendenceFailed(a);
                    Debug.Log($"[超神机械师] {a.name} 突破失败，恶性变异致死！化为超神遗力，无法圣所复苏");
                }
                else
                {
                    Debug.Log($"[超神机械师] {a.name} 突破失败，恶性变异！扣血{damage:F0}，属性下降");
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
                    WorldTile t = World.world.GetTile(a.current_tile.x + dx, a.current_tile.y + dy);
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
        /// 可进阶状态=完成进阶任务+SS阶+神性蜕变。
        /// 土著和降临者都能感知，和面板无关。
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
                if (!IsAdvancementTaskDone(a)) continue; // 必须完成进阶任务
                if (IsTranscended(a)) continue;
                if (GetLegacyPower(a) >= 3) continue; // 最多存3份

                // 每tick有小概率感知到超神遗力（ch1396韩萧感知到数十个）
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
            if (rank < 12) return $"需SS阶（当前{SuperMechRanks.GetRankName(a)}）";
            if (!SuperMechDivinity.IsDivineAwakened(a)) return "需触发神性蜕变（气力Lv21+78000欧纳）";

            // 进阶任务
            if (!IsAdvancementTaskDone(a))
            {
                float prog = GetAdvancementProgress(a);
                return $"进阶任务【{GetAdvancementTaskName(a)}】{prog:F0}%（完成后才能感知超神遗力）";
            }

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
        public static void Clear() { _legacyPower.Clear(); _cooldown.Clear(); _transcended.Clear(); }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _legacyPower.Remove(a.id);
            _cooldown.Remove(a.id);
            _transcended.Remove(a.id);
        }
    }
}
