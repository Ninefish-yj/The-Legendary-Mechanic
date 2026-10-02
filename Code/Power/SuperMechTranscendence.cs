using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>超神遗力来源记录（原著还原：继承好处也要继承债务）
    /// 拿到遗力者需承担死者死亡原因同等的伤害，体质/耐力高者才能扛住
    /// </summary>
    public class LegacyPowerSource
    {
        public string sourceName;
        public int sourceRank;
        public float sourceEnergy;
        public float deathDamage;
        public string deathType;
    }

    public static class SuperMechTranscendence
    {
        private static readonly Dictionary<long, int> _legacyPower = new Dictionary<long, int>();
        private static readonly Dictionary<long, List<LegacyPowerSource>> _legacySources = new Dictionary<long, List<LegacyPowerSource>>();
        private static readonly Dictionary<long, float> _cooldown = new Dictionary<long, float>();
        private static readonly Dictionary<long, bool> _transcended = new Dictionary<long, bool>();
        private static readonly Dictionary<long, bool> _advancementTaskDone = new Dictionary<long, bool>();
        private static readonly Dictionary<long, float> _advancementProgress = new Dictionary<long, float>();
        private static readonly Dictionary<long, int> _divineCatalyst = new Dictionary<long, int>();

        // 世界中游离的超神遗力池（超神级死亡后生成，等待被感知吸收）
        private static readonly List<LegacyPowerSource> _worldLegacyPool = new List<LegacyPowerSource>();

        // SS级以上单位快照（用于死亡时生成遗力，因为死亡后对象已不存在）
        private static readonly Dictionary<long, LegacyPowerSource> _ssUnitSnapshots = new Dictionary<long, LegacyPowerSource>();

        private static readonly List<WorldTile> _legacySpawns = new List<WorldTile>();

        public static bool IsAdvancementTaskDone(Actor a)
        {
            if (a == null) return false;
            bool v; _advancementTaskDone.TryGetValue(a.id, out v); return v;
        }

        public static float GetAdvancementProgress(Actor a)
        {
            if (a == null) return 0;
            float v; _advancementProgress.TryGetValue(a.id, out v); return v;
        }

        public static string GetAdvancementTaskName(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            switch (cls)
            {
                case "sm_transcendence_1226": return "sm_transcendence_1227";
                case "sm_transcendence_1228": return "sm_transcendence_1229";
                case "sm_transcendence_1230": return "sm_transcendence_1231";
                case "sm_transcendence_1232": return "sm_transcendence_1233";
                case "sm_transcendence_1234": return "sm_transcendence_1235";
                default: return "sm_transcendence_1236";
            }
        }

        public static void TickAdvancementTask()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) < 12) continue;
                if (!SuperMechDivinity.IsDivineAwakened(a)) continue;
                if (IsAdvancementTaskDone(a)) continue;
                if (IsTranscended(a)) continue;

                float progress = GetAdvancementProgress(a);
                float gain = 0.1f * tickInterval;
                if (SuperMechQi.IsInCombat(a)) gain += 0.5f * tickInterval;
                if (a.hasTrait(SuperMechTraits.ClassMech))
                {
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        float intel = (stats["intelligence"] == 0f ? 5f : stats["intelligence"]);
                        gain += intel * 0.001f * tickInterval;
                    }
                }
                var destiny = SuperMechIntuition.GetDestiny(a);
                if (destiny != null && destiny.completed) gain += 0.3f * tickInterval;
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

        public static void TickAutoAttempt()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!CanAttempt(a)) continue;
                if (!AllConditionsMet(a)) continue;

                Debug.Log($"[超神机械师] {a.name} 满足超神突破三条件，自动尝试突破！");
                AttemptTranscend(a);

                _cooldown[a.id] = Time.time + 3600f;
            }
        }

        public static int GetLegacyPower(Actor a)
        {
            if (a == null) return 0;
            int v; _legacyPower.TryGetValue(a.id, out v); return v;
        }

        public static List<LegacyPowerSource> GetLegacySources(Actor a)
        {
            if (a == null) return null;
            List<LegacyPowerSource> v; _legacySources.TryGetValue(a.id, out v); return v;
        }

        /// <summary>超神级死亡时生成游离遗力，加入世界遗力池等待被感知吸收
        /// 原著：超神遗力是超神级强者死亡后留下的力量遗产
        /// </summary>
        public static void GenerateLegacyOnDeath(Actor deadActor, string deathType)
        {
            if (deadActor == null) return;
            int rank = SuperMechAdvancement.GetExactRankIndex(deadActor);
            if (rank < 12) return; // 只有SS级以上死亡才产生遗力

            float energy = SuperMechEnergyLevel.Calculate(deadActor);
            float deathDamage = Mathf.Max(500f, energy * 0.15f); // 承伤基准：死者能级15%，最低500

            var source = new LegacyPowerSource
            {
                sourceName = deadActor.name,
                sourceRank = rank,
                sourceEnergy = energy,
                deathDamage = deathDamage,
                deathType = deathType
            };
            _worldLegacyPool.Add(source);
            Debug.Log($"[超神机械师] {deadActor.name}（{SuperMechRanks.GetRankName(deadActor)}）死亡，生成超神遗力（承伤{deathDamage:F0}），世界遗力池现有{_worldLegacyPool.Count}份");
        }

        /// <summary>原著还原：感知并吸收遗力需承担死者死亡同等伤害
        /// 体质/耐力高者才能扛住，扛不住会受伤甚至死亡
        /// </summary>
        public static void TickLegacySense()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            if (_worldLegacyPool.Count == 0) return;

            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) < 12) continue;
                if (!SuperMechDivinity.IsDivineAwakened(a)) continue;
                if (!IsAdvancementTaskDone(a)) continue;
                if (IsTranscended(a)) continue;
                if (GetLegacyPower(a) >= 3) continue;

                // 感知概率：基础1.5%，世界遗力池越多概率越高
                float senseChance = 0.015f + Mathf.Min(0.03f, _worldLegacyPool.Count * 0.005f);
                if (Random.value >= senseChance) continue;

                // 从池中随机取一份遗力
                int idx = Random.Range(0, _worldLegacyPool.Count);
                var source = _worldLegacyPool[idx];

                // 承伤判定：基础伤害=死者死亡伤害，体质/耐力减免
                float baseDamage = source.deathDamage;
                var stats = SuperMechStats.Of(a);
                float endurance = stats != null && stats["endurance"] > 0 ? stats["endurance"] : 5f;
                float damageReduction = endurance * 15f; // 每点耐力减15伤害
                float finalDamage = Mathf.Max(50f, baseDamage - damageReduction);

                float currentHp = a.data.health;
                float maxHp = a.getMaxHealth();

                Debug.Log($"[超神机械师] {a.name} 感知到{source.sourceName}的超神遗力（能级{source.sourceEnergy:F0}），承伤{finalDamage:F0}（基础{baseDamage:F0}-耐力减免{damageReduction:F0}），当前HP{currentHp:F0}/{maxHp:F0}");

                // 承受伤害
                a.data.health = Mathf.Max(1, (int)(currentHp - finalDamage));

                // 扛不住：有概率死亡（继承债务）
                if (finalDamage > currentHp * 0.9f)
                {
                    float deathChance = Mathf.Clamp01((finalDamage - currentHp) / Mathf.Max(1f, finalDamage) + 0.3f);
                    if (Random.value < deathChance)
                    {
                        Debug.Log($"[超神机械师] {a.name} 承受遗力债务失败，被{source.sourceName}的死亡之力反噬而亡！");
                        // 死亡时也生成遗力（连锁）
                        GenerateLegacyOnDeath(a, "legacy_backlash");
                        a.dieSimpleNone();
                        _worldLegacyPool.RemoveAt(idx);
                        continue;
                    }
                }

                // 扛住了：获得遗力
                _legacyPower[a.id] = GetLegacyPower(a) + 1;
                if (!_legacySources.ContainsKey(a.id))
                    _legacySources[a.id] = new List<LegacyPowerSource>();
                _legacySources[a.id].Add(source);
                _worldLegacyPool.RemoveAt(idx);

                Debug.Log($"[超神机械师] {a.name} 扛住遗力债务，获得{source.sourceName}的超神遗力（共{GetLegacyPower(a)}份）");
            }
        }

        public static void AddLegacyPower(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            int cur = GetLegacyPower(a);
            _legacyPower[a.id] = cur + amount;
            Debug.Log($"[超神机械师] {a.name} 获得{amount}份超神遗力（共{cur + amount}）");
        }

        public static void SetLegacyPower(Actor a, int amount)
        {
            if (a == null) return;
            _legacyPower[a.id] = Mathf.Max(0, amount);
        }

        public static void SetLegacySources(Actor a, List<LegacyPowerSource> sources)
        {
            if (a == null) return;
            if (sources == null) _legacySources.Remove(a.id);
            else _legacySources[a.id] = sources;
        }

        public static bool IsTranscended(Actor a)
        {
            if (a == null) return false;
            bool v; _transcended.TryGetValue(a.id, out v); return v;
        }

        public static void SetTranscended(Actor a)
        {
            if (a == null) return;
            _transcended[a.id] = true;
        }

        public static void SetAdvancementProgress(Actor a, float progress)
        {
            if (a == null) return;
            _advancementProgress[a.id] = progress;
        }

        public static void SetAdvancementTaskDone(Actor a)
        {
            if (a == null) return;
            _advancementTaskDone[a.id] = true;
        }

        public static bool CanAttempt(Actor a)
        {
            if (a == null) return false;
            if (IsTranscended(a)) return false;
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return false;
            if (!SuperMechDivinity.IsDivineAwakened(a)) return false;
            if (!IsAdvancementTaskDone(a)) return false;
            float cd;
            if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd) return false;
            return true;
        }

        public static bool AllConditionsMet(Actor a)
        {
            if (a == null) return false;
            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            bool cond2 = CountNearbyAssistants(a) >= 4;
            bool cond3 = GetLegacyPower(a) >= 1;
            return cond1 && cond2 && cond3;
        }

        public static bool CatalyzeBreakthrough(Actor a)
        {
            if (a == null || !a.isAlive()) return false;
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return false;

            if (!_divineCatalyst.TryGetValue(a.id, out int layers)) layers = 0;
            if (layers >= 5) return false;

            _divineCatalyst[a.id] = layers + 1;
            Debug.Log($"[超神机械师] 神之催化：{a.name} 获得第{layers + 1}层催化（成功率+{(layers + 1) * 10}%）");
            return true;
        }

        public static int GetCatalystLayers(Actor a)
        {
            if (a == null) return 0;
            _divineCatalyst.TryGetValue(a.id, out int v);
            return v;
        }

        public static bool AttemptTranscend(Actor a)
        {
            if (!CanAttempt(a)) return false;

            float successRate = Random.Range(0.04f, 0.10f);

            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            if (cond1) successRate += 0.333f;

            int assistantCount = CountNearbyAssistants(a);
            bool cond2 = assistantCount >= 4;
            if (cond2) successRate += 0.333f;

            bool cond3 = GetLegacyPower(a) >= 1;
            if (cond3) successRate += 0.333f;

            successRate += Mathf.Max(0, GetLegacyPower(a) - 1) * 0.02f;

            float legendBonus = SuperMechLegend.GetBreakthroughBonus(a);
            if (legendBonus > 0) successRate += legendBonus;

            int catalyst = GetCatalystLayers(a);
            if (catalyst > 0) successRate += catalyst * 0.10f;

            successRate = Mathf.Clamp(successRate, 0.01f, 0.99f);

            bool allMet = cond1 && cond2 && cond3;

            Debug.Log($"[超神机械师] {a.name} 冲击超神级，成功率{successRate:P0}，三条件{(allMet ? "全部满足→神化进阶" : "未全满足→即使成功也不升阶")}");

            if (cond3)
            {
                _legacyPower[a.id] = GetLegacyPower(a) - 1;
                // 突破消耗一份遗力，同时移除最早的来源记录
                if (_legacySources.TryGetValue(a.id, out var sources) && sources.Count > 0)
                    sources.RemoveAt(0);
            }
            _cooldown[a.id] = Time.time + 60f;

            if (Random.value < successRate)
            {
                if (allMet)
                {
                    _transcended[a.id] = true;
                    SuperMechAdvancement.SetExactRank(a, 13);
                    float curQi = SuperMechQi.GetQi(a);
                    int curLv = SuperMechQi.GetLevel(curQi);
                    int nextLv = Mathf.Min(curLv + 1, SuperMechQi.Thresholds.Length - 1);
                    float nextThreshold = SuperMechQi.Thresholds[nextLv];
                    float growth = curQi * 0.15f;
                    float targetQi = Mathf.Max(curQi + growth, nextThreshold + 1000f);
                    SuperMechQi.SetQi(a, targetQi);
                    SuperMechQi.AddQiMax(a, targetQi - curQi + 50000f);
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        stats["intelligence"] = (stats["intelligence"]) + 200f;
                        stats["damage"] = (stats["damage"]) + 500f;
                        stats["health"] = (stats["health"]) + 5000f;
                        stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) + 0.5f;
                    }
                    Debug.Log($"[超神机械师] {a.name} 神化进阶成功！气力从{curQi:F0}(Lv{curLv})增至{targetQi:F0}(Lv{nextLv})，突破超神级！！！");
                    return true;
                }
                else
                {
                    Debug.Log($"[超神机械师] {a.name} 进阶成功但条件未全满足，阶位不变（需三条件全满足才能神化进阶）");
                    SuperMechQi.AddQiMax(a, 20000f);
                    return false;
                }
            }
            else
            {
                float damage = a.getMaxHealth() * 0.5f;
                a.data.health = Mathf.Max(1, (int)(a.data.health - damage));
                var stats = SuperMechStats.Of(a);
                if (stats != null)
                {
                    stats["intelligence"] = Mathf.Max(0f, (stats["intelligence"]) - 30f);
                    stats["damage"] = Mathf.Max(0f, (stats["damage"]) - 80f);
                }
                if (Random.value < 0.1f && a.isAlive())
                {
                    SuperMechSanctuary.MarkTranscendenceFailed(a);
                    // 突破失败死亡也生成遗力
                    GenerateLegacyOnDeath(a, "transcendence_failed");
                    a.dieSimpleNone();
                    Debug.Log($"[超神机械师] {a.name} 突破失败，恶性变异致死！化为超神遗力，无法圣所复苏");
                }
                else
                {
                    Debug.Log($"[超神机械师] {a.name} 突破失败，恶性变异！扣血{damage:F0}，属性下降");
                }
                return false;
            }
        }

        private static int CountNearbyAssistants(Actor a)
        {
            if (a == null || a.current_tile == null) return 0;
            HashSet<string> classes = new HashSet<string>();
            string myClass = SuperMechBranch.GetClass(a);
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    WorldTile t = World.world.GetTile(a.current_tile.x + dx, a.current_tile.y + dy);
                    if (t == null) continue;
                    t.doUnits(u =>
                    {
                        if (u == null || u == a) return;
                        if (SuperMechAdvancement.GetExactRankIndex(u) < 10) return;
                        string cls = SuperMechBranch.GetClass(u);
                        if (!string.IsNullOrEmpty(cls) && cls != myClass)
                            classes.Add(cls);
                    });
                }
            }
            return classes.Count;
        }

        public static string GetStatusText(Actor a)
        {
            if (a == null) return "";
            if (IsTranscended(a)) return "sm_transcendence_1237";
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank < 12) return $"{LocalizedTextManager.getText("sm_transcendence_need_ss")}（{LocalizedTextManager.getText("sm_transcendence_current")}{SuperMechRanks.GetRankName(a)}）";
            if (!SuperMechDivinity.IsDivineAwakened(a)) return "sm_transcendence_1238";

            if (!IsAdvancementTaskDone(a))
            {
                float prog = GetAdvancementProgress(a);
                return $"{LocalizedTextManager.getText("sm_transcendence_adv_task")}【{GetAdvancementTaskName(a)}】{prog:F0}%（{LocalizedTextManager.getText("sm_transcendence_need_legacy")}）";
            }

            bool cond1 = a.hasTrait("sm_cosmic_relic_owner");
            int assistants = CountNearbyAssistants(a);
            bool cond2 = assistants >= 4;
            int legacy = GetLegacyPower(a);
            bool cond3 = legacy >= 1;

            string text = $"{LocalizedTextManager.getText("sm_transcendence_cond1")}{(cond1 ? "✓" : "✗")} {LocalizedTextManager.getText("sm_transcendence_cond2")}{assistants}/4{(cond2 ? "✓" : "✗")} {LocalizedTextManager.getText("sm_transcendence_cond3")}{legacy}{(cond3 ? "✓" : "✗")}";

            // 显示遗力来源（原著还原：继承债务）
            var sources = GetLegacySources(a);
            if (sources != null && sources.Count > 0)
            {
                text += "\n" + LocalizedTextManager.getText("sm_legacy_sources") + "：";
                for (int i = 0; i < Mathf.Min(sources.Count, 3); i++)
                {
                    text += $"\n  {sources[i].sourceName}（{SuperMechRanks.GetRankName(sources[i].sourceRank)}，{LocalizedTextManager.getText("sm_legacy_damage")}{sources[i].deathDamage:F0}）";
                }
                if (sources.Count > 3)
                    text += $"\n  +{sources.Count - 3}";
            }

            if (cond1 && cond2 && cond3)
            {
                float cd;
                if (_cooldown.TryGetValue(a.id, out cd) && Time.time < cd)
                    return text + $" {LocalizedTextManager.getText("sm_transcendence_cooldown")}{cd - Time.time:F0}s";
                return text + "sm_transcendence_1239";
            }
            return text + "sm_transcendence_1240";
        }

        public static void Clear()
        {
            _legacyPower.Clear();
            _legacySources.Clear();
            _worldLegacyPool.Clear();
            _cooldown.Clear();
            _transcended.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            // 检测刚死亡的SS级以上单位，生成遗力（原著还原：超神遗力）
            foreach (var kvp in _ssUnitSnapshots)
            {
                if (!alive.Contains(kvp.Key))
                {
                    _worldLegacyPool.Add(kvp.Value);
                    Debug.Log($"[超神机械师] {kvp.Value.sourceName}（阶位{kvp.Value.sourceRank}）死亡，生成超神遗力（承伤{kvp.Value.deathDamage:F0}），世界遗力池现有{_worldLegacyPool.Count}份");
                }
            }
            _ssUnitSnapshots.Clear();

            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_legacyPower, alive);
            removed += SuperMechCleanup.CleanDict(_legacySources, alive);
            removed += SuperMechCleanup.CleanDict(_transcended, alive);
            return removed;
        }

        /// <summary>更新SS级以上单位快照（每次tick调用，用于死亡时生成遗力）
        /// </summary>
        public static void UpdateSsSnapshots()
        {
            if (World.world == null || World.world.units == null) return;
            _ssUnitSnapshots.Clear();
            foreach (Actor a in World.world.units)
            {
                if (a == null || !a.isAlive()) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank < 12) continue;
                float energy = SuperMechEnergyLevel.Calculate(a);
                _ssUnitSnapshots[a.id] = new LegacyPowerSource
                {
                    sourceName = a.name ?? "",
                    sourceRank = rank,
                    sourceEnergy = energy,
                    deathDamage = Mathf.Max(500f, energy * 0.15f),
                    deathType = "combat_or_other"
                };
            }
        }

        public static void Clear(Actor a)
        {
            if (a == null) return;
            _legacyPower.Remove(a.id);
            _legacySources.Remove(a.id);
            _cooldown.Remove(a.id);
            _transcended.Remove(a.id);
        }

        // 世界遗力池存档
        public static List<LegacyPowerSource> GetWorldLegacyPool() { return _worldLegacyPool; }
        public static void SetWorldLegacyPool(List<LegacyPowerSource> pool)
        {
            _worldLegacyPool.Clear();
            if (pool != null) _worldLegacyPool.AddRange(pool);
        }
    }
}
