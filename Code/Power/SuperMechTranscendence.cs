using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>超神遗力来源记录（原著还原：继承好处也要继承债务）
    /// 原著第1398章：超神遗力凝聚了死者进阶身亡时逸散的生命精华、灵魂意识、核心能量
    /// 内部有残存意识体，继承者需承受与死者死因相同类型的负荷（弱化版恶性变异）
    /// </summary>
    public class LegacyPowerSource
    {
        public string sourceName;        // 死者名字
        public int sourceRank;           // 死者阶位
        public float sourceEnergy;       // 死者能级
        public float deathDamage;        // 承伤基准
        public string deathType;         // 死因：cosmic_assimilation/genome_collapse/cell_independence/infinite_proliferation/other
        public string loadType;          // 负荷类型：mental/genetic/physical/energy
        public string consciousnessName; // 残存意识体称呼（如"提尔修斯"）
        public string wishText;          // 遗愿文本
        public bool consciousnessGone;   // 残存意识是否已消亡（继承成功后消亡）
    }

    /// <summary>死因→负荷类型映射（原著：经历弱化版恶性变异，与死者进阶时危机类型相同）
    /// </summary>
    public static class LegacyDeathTypes
    {
        public const string CosmicAssimilation = "cosmic_assimilation";  // 宇宙同化→精神负荷
        public const string GenomeCollapse = "genome_collapse";          // 基因崩溃→基因负荷
        public const string CellIndependence = "cell_independence";      // 细胞独立→肉体负荷
        public const string InfiniteProliferation = "infinite_proliferation"; // 无限增殖→能量负荷
        public const string Other = "other";                              // 其他→综合负荷

        public static string GetLoadType(string deathType)
        {
            switch (deathType)
            {
                case CosmicAssimilation: return "mental";
                case GenomeCollapse: return "genetic";
                case CellIndependence: return "physical";
                case InfiniteProliferation: return "energy";
                default: return "physical";
            }
        }

        public static string GetDeathTypeName(string deathType)
        {
            switch (deathType)
            {
                case CosmicAssimilation: return LocalizedTextManager.getText("sm_legacy_death_cosmic");
                case GenomeCollapse: return LocalizedTextManager.getText("sm_legacy_death_genome");
                case CellIndependence: return LocalizedTextManager.getText("sm_legacy_death_cell");
                case InfiniteProliferation: return LocalizedTextManager.getText("sm_legacy_death_prolif");
                default: return LocalizedTextManager.getText("sm_legacy_death_other");
            }
        }

        public static string GetLoadTypeName(string loadType)
        {
            switch (loadType)
            {
                case "mental": return LocalizedTextManager.getText("sm_legacy_load_mental");
                case "genetic": return LocalizedTextManager.getText("sm_legacy_load_genetic");
                case "physical": return LocalizedTextManager.getText("sm_legacy_load_physical");
                case "energy": return LocalizedTextManager.getText("sm_legacy_load_energy");
                default: return LocalizedTextManager.getText("sm_legacy_load_physical");
            }
        }

        /// <summary>随机一个死因（突破失败时恶性变异类型）
        /// </summary>
        public static string RandomDeathType()
        {
            float r = Random.value;
            if (r < 0.3f) return CosmicAssimilation;
            if (r < 0.55f) return GenomeCollapse;
            if (r < 0.8f) return CellIndependence;
            if (r < 0.95f) return InfiniteProliferation;
            return Other;
        }
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

        // 世界中游离的超神遗力池（冲击超神级失败后生成，等待被感知吸收）
        private static readonly List<LegacyPowerSource> _worldLegacyPool = new List<LegacyPowerSource>();

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
                if (SuperMechActorContextRegistry.GetRank(a) < 12) continue;
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
                if (SuperMechActorContextRegistry.IsAwakened(a)) gain *= 1.2f;

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

        /// <summary>超神级冲击更高层次失败时生成遗力（原著还原）
        /// 原著第1396章：完成神性蜕变的超能者冲击更高层次，因恶性变异身亡，
        /// 逸散的生命精华、灵魂意识、核心能量凝聚为超神遗力
        /// 原著第1398章：遗力内部有残存意识体，死因决定继承者承受的负荷类型
        /// </summary>
        public static void GenerateLegacyOnDeath(Actor deadActor, string deathType)
        {
            if (deadActor == null) return;
            int rank = SuperMechActorContextRegistry.GetRank(deadActor);
            if (rank < 12) return; // 只有SS级以上冲击超神级失败才产生遗力

            // 如果死因未指定，随机一个恶性变异类型
            if (string.IsNullOrEmpty(deathType) || deathType == "transcendence_failed" || deathType == "legacy_backlash")
                deathType = LegacyDeathTypes.RandomDeathType();

            float energy = SuperMechEnergyLevel.Calculate(deadActor);
            float deathDamage = Mathf.Max(500f, energy * 0.15f);
            string loadType = LegacyDeathTypes.GetLoadType(deathType);

            // 生成残存意识体（原著：死者的灵魂意识拼凑凝聚，有残缺记忆和人格）
            string consciousnessName = deadActor.name ?? LocalizedTextManager.getText("sm_legacy_unknown");
            string wishText = GenerateWishText(deadActor, deathType);

            var source = new LegacyPowerSource
            {
                sourceName = deadActor.name,
                sourceRank = rank,
                sourceEnergy = energy,
                deathDamage = deathDamage,
                deathType = deathType,
                loadType = loadType,
                consciousnessName = consciousnessName,
                wishText = wishText,
                consciousnessGone = false
            };
            _worldLegacyPool.Add(source);
            Debug.Log($"[超神机械师] {deadActor.name}（{SuperMechRanks.GetRankName(deadActor)}）冲击超神级失败，死于{LegacyDeathTypes.GetDeathTypeName(deathType)}，生成超神遗力（负荷类型：{LegacyDeathTypes.GetLoadTypeName(loadType)}，承伤{deathDamage:F0}），残存意识体「{consciousnessName}」，世界遗力池现有{_worldLegacyPool.Count}份");
        }

        /// <summary>生成残存意识体的遗愿文本（原著：提尔修斯请求继承者去看看家乡克里星）
        /// </summary>
        private static string GenerateWishText(Actor deadActor, string deathType)
        {
            string name = deadActor.name ?? "?";
            string kingdom = "";
            if (deadActor.kingdom != null) kingdom = deadActor.kingdom.data.name;
            if (string.IsNullOrEmpty(kingdom)) kingdom = LocalizedTextManager.getText("sm_legacy_wish_nokindom");

            // 按死因生成不同遗愿
            switch (deathType)
            {
                case LegacyDeathTypes.CosmicAssimilation:
                    return string.Format(LocalizedTextManager.getText("sm_legacy_wish_cosmic"), name, kingdom);
                case LegacyDeathTypes.GenomeCollapse:
                    return string.Format(LocalizedTextManager.getText("sm_legacy_wish_genome"), name);
                case LegacyDeathTypes.CellIndependence:
                    return string.Format(LocalizedTextManager.getText("sm_legacy_wish_cell"), name);
                case LegacyDeathTypes.InfiniteProliferation:
                    return string.Format(LocalizedTextManager.getText("sm_legacy_wish_prolif"), name);
                default:
                    return string.Format(LocalizedTextManager.getText("sm_legacy_wish_default"), name, kingdom);
            }
        }

        /// <summary>原著还原：感知并吸收遗力需承受与死者死因相同类型的负荷
        /// 原著第1398章：进入遗力异空间，残存意识体出现，承受弱化版恶性变异考验
        /// 死因不同负荷类型不同：宇宙同化→精神冲击，基因崩溃→基因伤害，细胞独立→肉体失控，无限增殖→能量暴走
        /// 扛住则残存意识消亡，遗力归继承者；扛不住则死亡
        /// </summary>
        public static void TickLegacySense()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            if (_worldLegacyPool.Count == 0) return;

            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                if (SuperMechActorContextRegistry.GetRank(a) < 12) continue;
                if (!SuperMechDivinity.IsDivineAwakened(a)) continue;
                if (!IsAdvancementTaskDone(a)) continue;
                if (IsTranscended(a)) continue;
                if (GetLegacyPower(a) >= 3) continue;

                // 感知概率
                float senseChance = 0.015f + Mathf.Min(0.03f, _worldLegacyPool.Count * 0.005f);
                if (Random.value >= senseChance) continue;

                int idx = Random.Range(0, _worldLegacyPool.Count);
                var source = _worldLegacyPool[idx];

                // 按负荷类型计算伤害（原著：经历弱化版恶性变异，与死者进阶时危机类型相同）
                float baseDamage = source.deathDamage;
                var stats = SuperMechStats.Of(a);
                float endurance = stats != null && stats["endurance"] > 0 ? stats["endurance"] : 5f;
                float intelligence = stats != null && stats["intelligence"] > 0 ? stats["intelligence"] : 5f;

                float damageReduction;
                string loadDesc;
                switch (source.loadType)
                {
                    case "mental":
                        // 精神负荷：智力减免（原著：宇宙泛意识精神冲击）
                        damageReduction = intelligence * 12f;
                        loadDesc = LocalizedTextManager.getText("sm_legacy_load_mental");
                        break;
                    case "genetic":
                        // 基因负荷：耐力+智力双减免
                        damageReduction = endurance * 8f + intelligence * 5f;
                        loadDesc = LocalizedTextManager.getText("sm_legacy_load_genetic");
                        break;
                    case "energy":
                        // 能量负荷：耐力减免为主
                        damageReduction = endurance * 18f;
                        loadDesc = LocalizedTextManager.getText("sm_legacy_load_energy");
                        break;
                    default: // physical
                        // 肉体负荷：耐力减免（原著：以肉度扛住）
                        damageReduction = endurance * 15f;
                        loadDesc = LocalizedTextManager.getText("sm_legacy_load_physical");
                        break;
                }
                float finalDamage = Mathf.Max(50f, baseDamage - damageReduction);

                float currentHp = a.data.health;
                float maxHp = a.getMaxHealth();

                Debug.Log($"[超神机械师] {a.name} 感知到{source.sourceName}的超神遗力（死于{LegacyDeathTypes.GetDeathTypeName(source.deathType)}），进入异空间遭遇残存意识体「{source.consciousnessName}」，承受{loadDesc}{finalDamage:F0}（基础{baseDamage:F0}-减免{damageReduction:F0}），当前HP{currentHp:F0}/{maxHp:F0}");

                // 承受负荷伤害
                a.data.health = Mathf.Max(1, (int)(currentHp - finalDamage));

                // 扛不住：被遗力反噬死亡（继承债务）
                if (finalDamage > currentHp * 0.9f)
                {
                    float deathChance = Mathf.Clamp01((finalDamage - currentHp) / Mathf.Max(1f, finalDamage) + 0.3f);
                    if (Random.value < deathChance)
                    {
                        Debug.Log($"[超神机械师] {a.name} 承受{loadDesc}失败，被{source.sourceName}的遗力反噬而亡！残存意识体「{source.consciousnessName}」再度沉寂，遗力消散于宇宙");
                        a.dieSimpleNone();
                        _worldLegacyPool.RemoveAt(idx);
                        continue;
                    }
                }

                // 扛住了：残存意识消亡，遗力归继承者
                source.consciousnessGone = true;
                _legacyPower[a.id] = GetLegacyPower(a) + 1;
                if (!_legacySources.ContainsKey(a.id))
                    _legacySources[a.id] = new List<LegacyPowerSource>();
                _legacySources[a.id].Add(source);
                _worldLegacyPool.RemoveAt(idx);

                Debug.Log($"[超神机械师] {a.name} 扛住{loadDesc}，残存意识体「{source.consciousnessName}」消亡，获得{source.sourceName}的超神遗力（共{GetLegacyPower(a)}份）。遗愿：{source.wishText}");
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
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null) return ctx.transcended;
            bool v; _transcended.TryGetValue(a.id, out v); return v;
        }

        public static void SetTranscended(Actor a)
        {
            if (a == null) return;
            _transcended[a.id] = true;
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.transcended = true;
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
            int rank = SuperMechActorContextRegistry.GetRank(a);
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
            int rank = SuperMechActorContextRegistry.GetRank(a);
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
                    SetTranscended(a);
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
                        if (SuperMechActorContextRegistry.GetRank(u) < 10) return;
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
            int rank = SuperMechActorContextRegistry.GetRank(a);
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

            // 显示遗力来源（原著还原：残存意识体+死因+负荷类型）
            var sources = GetLegacySources(a);
            if (sources != null && sources.Count > 0)
            {
                text += "\n" + LocalizedTextManager.getText("sm_legacy_sources") + "：";
                for (int i = 0; i < Mathf.Min(sources.Count, 3); i++)
                {
                    var s = sources[i];
                    string deathName = LegacyDeathTypes.GetDeathTypeName(s.deathType);
                    string loadName = LegacyDeathTypes.GetLoadTypeName(s.loadType);
                    text += $"\n  {s.sourceName}（{SuperMechRanks.GetRankName(s.sourceRank)}，{deathName}，{loadName}{s.deathDamage:F0}）";
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
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_legacyPower, alive);
            removed += SuperMechCleanup.CleanDict(_legacySources, alive);
            removed += SuperMechCleanup.CleanDict(_transcended, alive);
            return removed;
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
