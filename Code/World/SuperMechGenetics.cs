using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.66.0 基因科研项目（原著向）
    /// 原著依据：
    ///  1. 基因链是异能/力量的基础，升级基因链可提高威力、操控力，甚至让能力进化（基因树概念）；
    ///  2. 基因优化（进化方块16层优化/基因药剂）可带来属性暴涨，是角色成长的关键路径；
    ///  3. 基因升华：已有基因通过人工进化向更高一层迈进，契合度决定进化结果（低契合=基因崩溃风险）；
    ///  4. 学习/科研需要消耗潜能点（原著：职业知识与强化均消耗潜能点）。
    /// 真实系统适配：个体基因链挂单位级潜能点（SuperMechPotential.SpendPotential）；
    /// 文明级基因科研由文明科技值自动驱动，成果惠及全体成员；攻防加成挂接战斗补丁。
    /// </summary>
    public static class SuperMechGenetics
    {
        public class GeneData
        {
            public int chainLevel = 0;      // 基因链层数（基因优化层数）
            public int sublimations = 0;    // 基因升华次数（进入更高基因阶段）
            public bool crashed = false;    // 是否经历过基因崩溃（失败记录）
        }

        public class GeneticsSaveData
        {
            public Dictionary<string, GeneSaveEntry> actors = new Dictionary<string, GeneSaveEntry>();
            public Dictionary<string, CivResearchEntry> civResearch = new Dictionary<string, CivResearchEntry>();
        }

        public class GeneSaveEntry
        {
            public int chainLevel;
            public int sublimations;
            public bool crashed;
        }

        public class CivResearchEntry
        {
            public float progress;   // 0~1
        }

        private static readonly Dictionary<long, GeneData> _genes = new Dictionary<long, GeneData>();
        private static readonly Dictionary<string, float> _civResearch = new Dictionary<string, float>(); // kingdomId -> 进度0~1

        // 数值
        private const float DamagePerLayer = 0.015f;   // 每层基因链 +1.5% 伤害
        private const float DamagePerSublimation = 0.15f; // 每次升华 +15% 伤害
        private const float DefensePerLayer = 0.005f;  // 每层基因链 -0.5% 受到伤害
        private const float DefensePerSublimation = 0.05f;
        private const float PotentialPerLayer = 2f;    // 每层基因链 +2 潜能（原著：基因药剂提升潜力）
        public const int SublimateMinLevel = 8;        // 基因升华最低层数（原著：基因链成熟才可升华）
        private const float ResearchPerTechPerTick = 0.0025f; // 科研进度=文明科技等级×该速率/每Tick

        public static GeneData GetGeneData(Actor a)
        {
            if (a == null || a.id == null) return null;
            if (!_genes.TryGetValue(a.id, out var g))
            {
                g = new GeneData();
                _genes[a.id] = g;
            }
            return g;
        }

        public static int GetChainLevel(Actor a)
        {
            var g = GetGeneData(a);
            return g == null ? 0 : g.chainLevel;
        }

        /// <summary>基因优化：消耗该单位潜能点，提升基因链一层（原著：基因药剂/进化方块带来基因优化）</summary>
        public static bool TryGeneOptimize(Actor a)
        {
            if (!SuperMechConfig.GeneticsEnabled) return false;
            if (a == null || a.id == null) return false;
            var g = GetGeneData(a);
            if (g == null) return false;

            // 成本随层数递增（原著：越到后面优化越昂贵）
            int cost = 10 + g.chainLevel * 5;
            if (!SuperMechPotential.SpendPotential(a, cost)) return false;

            g.chainLevel++;
            // 基因优化的潜力反馈（原著：基因药剂→潜力+1）
            SuperMechPotential.AddPotential(a, (int)PotentialPerLayer);
            SuperMechEventBus.Publish("GeneOptimized", new GeneEvent { actorId = a.id, chainLevel = g.chainLevel });
            Debug.Log($"[超神机械师] {a.name} 基因优化成功，基因链升至 {g.chainLevel} 层");
            return true;
        }

        /// <summary>基因升华：基因链成熟后尝试向更高基因阶段进化（原著：契合度决定结果，低契合=基因崩溃）</summary>
        public static bool TryGeneSublimate(Actor a)
        {
            if (!SuperMechConfig.GeneticsEnabled) return false;
            if (a == null || a.id == null) return false;
            var g = GetGeneData(a);
            if (g == null || g.chainLevel < SublimateMinLevel) return false;

            // 升华消耗潜能（更高阶投入）
            int cost = 100 + g.sublimations * 50;
            if (!SuperMechPotential.SpendPotential(a, cost)) return false;

            // 契合度：层数越高越稳定（原著：契合度低可能出现恶性结果）
            float compatibility = Mathf.Clamp(0.45f + g.chainLevel * 0.025f, 0.45f, 0.9f);
            if (Random.value < compatibility)
            {
                g.sublimations++;
                // v0.70.x 升华力量爆发：自动获得攻击强化Buff（原著：进化带来战力暴涨）
                SuperMechCombatEnhance.ApplyBuff(a, SuperMechCombatEnhance.BuffAtk,
                    SuperMechCombatEnhance.BuffDurationTicks, SuperMechCombatEnhance.BuffAtkValue);
                Debug.Log($"[超神机械师] {a.name} 基因升华成功！进入第 {g.sublimations} 次基因进化（获得攻击强化）");
                SuperMechEventBus.Publish("GeneSublimated", new GeneEvent { actorId = a.id, sublimations = g.sublimations });
                return true;
            }
            else
            {
                // 基因崩溃：层数掉落（原著：低契合的恶性结果）
                g.crashed = true;
                g.chainLevel = Mathf.Max(0, g.chainLevel - 2);
                Debug.Log($"[超神机械师] {a.name} 基因升华失败，基因崩溃！基因链掉落至 {g.chainLevel} 层");
                SuperMechEventBus.Publish("GeneCrashed", new GeneEvent { actorId = a.id, chainLevel = g.chainLevel });
                return false;
            }
        }

        /// <summary>伤害加成（挂接战斗补丁）</summary>
        public static float GetGeneDamageBonus(Actor a)
        {
            if (!SuperMechConfig.GeneticsEnabled || a == null) return 1f;
            var g = GetGeneData(a);
            if (g == null) return 1f;
            return 1f + g.chainLevel * DamagePerLayer + g.sublimations * DamagePerSublimation;
        }

        /// <summary>减伤加成（挂接战斗补丁，基因链是力量基础）</summary>
        public static float GetGeneDefenseBonus(Actor a)
        {
            if (!SuperMechConfig.GeneticsEnabled || a == null) return 1f;
            var g = GetGeneData(a);
            if (g == null) return 1f;
            return 1f - (g.chainLevel * DefensePerLayer + g.sublimations * DefensePerSublimation);
        }

        /// <summary>文明基因科研：科技越先进科研越快，完成后全体成员免费获得一次基因优化</summary>
        public static void TickGenetics(float delta)
        {
            if (!SuperMechConfig.GeneticsEnabled) return;
            if (World.world == null || World.world.units == null) return;

            foreach (var f in SuperMechFaction.GetAllFactions())
            {
                var kingdom = SuperMechFaction.GetFactionCivilization(f);
                if (kingdom == null) continue;
                string kid = f.id;
                if (!_civResearch.TryGetValue(kid, out var progress)) progress = 0f;

                int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(kingdom);
                progress = Mathf.Min(1f, progress + techLevel * ResearchPerTechPerTick * delta);
                _civResearch[kid] = progress;

                if (progress >= 1f)
                {
                    _civResearch[kid] = 0f;
                    // 科研完成：全体成员免费基因优化
                    int applied = 0;
                    foreach (var mid in f.memberIds)
                    {
                        var m = FindActorById(mid);
                        if (m == null || !m.isAlive()) continue;
                        var g = GetGeneData(m);
                        if (g != null) { g.chainLevel++; applied++; }
                    }
                    Debug.Log($"[超神机械师] 文明 {f.name} 基因科研完成，{applied} 名成员获得基因优化");
                    SuperMechEventBus.Publish("GeneResearchDone", new GeneEvent { factionId = f.id, applied = applied });
                }
            }
        }

        public static float GetCivResearchProgress(string factionId)
        {
            return _civResearch.TryGetValue(factionId, out var p) ? p : 0f;
        }

        public static List<KeyValuePair<Actor, GeneData>> GetGeneRanking(int limit)
        {
            var list = new List<KeyValuePair<Actor, GeneData>>();
            foreach (var kv in _genes)
            {
                var a = FindActorById(kv.Key);
                if (a == null || !a.isAlive()) continue;
                list.Add(new KeyValuePair<Actor, GeneData>(a, kv.Value));
            }
            list.Sort((x, y) => (y.Value.chainLevel * 100 + y.Value.sublimations * 1000).CompareTo(x.Value.chainLevel * 100 + x.Value.sublimations * 1000));
            if (limit > 0 && list.Count > limit) list.RemoveRange(limit, list.Count - limit);
            return list;
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units)
                if (a != null && a.id == id) return a;
            return null;
        }

        public static GeneticsSaveData Save()
        {
            var data = new GeneticsSaveData();
            foreach (var kv in _genes)
            {
                data.actors[kv.Key.ToString()] = new GeneSaveEntry
                {
                    chainLevel = kv.Value.chainLevel,
                    sublimations = kv.Value.sublimations,
                    crashed = kv.Value.crashed
                };
            }
            foreach (var kv in _civResearch)
                data.civResearch[kv.Key] = new CivResearchEntry { progress = kv.Value };
            return data;
        }

        public static void Load(GeneticsSaveData data)
        {
            _genes.Clear();
            _civResearch.Clear();
            if (data == null) return;
            foreach (var kv in data.actors)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                _genes[id] = new GeneData
                {
                    chainLevel = kv.Value.chainLevel,
                    sublimations = kv.Value.sublimations,
                    crashed = kv.Value.crashed
                };
            }
            foreach (var kv in data.civResearch)
                _civResearch[kv.Key] = kv.Value.progress;
        }

        public static void Clear()
        {
            _genes.Clear();
            _civResearch.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<long>();
            foreach (var id in _genes.Keys)
                if (!alive.Contains(id)) dead.Add(id);
            foreach (var id in dead)
            {
                _genes.Remove(id);
                removed++;
            }
            return removed;
        }
    }

    /// <summary>基因事件（事件总线）</summary>
    public class GeneEvent
    {
        public long actorId;
        public int chainLevel;
        public int sublimations;
        public string factionId;
        public int applied;
    }
}
