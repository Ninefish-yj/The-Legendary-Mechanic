using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族进化系统 - 管理单位的种族状态和进化逻辑
    /// 对照原著：通过进化方块选择进化方向，从基础种族进化到高级种族
    /// 单位种族ID通过ActorDataStore存储，不直接修改ActorAsset
    /// </summary>
    public static class SuperMechSpeciesEvolution
    {
        private static SpeciesDataStore _store = new SpeciesDataStore();
        private static bool _initialized;

        /// <summary>初始化种族进化系统</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Debug.Log("[超神机械师] 种族进化系统初始化完成");
        }

        /// <summary>获取单位当前种族ID</summary>
        public static string GetSpeciesId(long actorId)
        {
            return _store.Get(actorId) ?? "human";
        }

        /// <summary>设置单位种族</summary>
        public static void SetSpecies(long actorId, string speciesId)
        {
            _store.Set(actorId, speciesId);
        }

        /// <summary>检查单位是否可以进化到目标种族</summary>
        public static bool CanEvolve(long actorId, string targetSpeciesId)
        {
            var current = GetSpeciesId(actorId);
            var target = SuperMechSpecies.GetSpecies(targetSpeciesId);
            if (target == null) return false;

            // 专属种族需要超A阶位
            if (target.Priority == "P3")
            {
                Actor actor = FindActorById(actorId);
                if (actor == null) return false;
                int rank = SuperMechAdvancement.GetExactRankIndex(actor);
                return rank >= 10; // S阶（超A级）
            }

            // 检查进化目标列表
            var currentSpecies = SuperMechSpecies.GetSpecies(current);
            if (currentSpecies != null && currentSpecies.EvolutionTargets.Contains(targetSpeciesId))
            {
                return true;
            }

            // P0基础种族可以进化到P1
            if (target.Priority == "P1" && currentSpecies?.Priority == "P0")
            {
                return true;
            }

            return false;
        }

        /// <summary>执行种族进化（对照原著：属性补完+种族天赋解锁）</summary>
        public static bool Evolve(long actorId, string targetSpeciesId)
        {
            if (!CanEvolve(actorId, targetSpeciesId)) return false;

            var target = SuperMechSpecies.GetSpecies(targetSpeciesId);
            var oldSpecies = GetSpeciesId(actorId);

            // 设置新种族
            SetSpecies(actorId, targetSpeciesId);

            // 属性补完（对照原著：每次种族进化带来属性补完）
            ApplySpeciesStats(actorId, target);

            // 日志
            var actorName = GetActorName(actorId);
            Debug.Log($"[超神机械师] {actorName} 种族进化: {oldSpecies} → {target.Name}");

            return true;
        }

        /// <summary>应用种族属性加成</summary>
        private static void ApplySpeciesStats(long actorId, SpeciesData species)
        {
            // 通过BaseStats系统应用属性加成
            // 具体实现依赖属性系统的API
            foreach (var stat in species.BaseStats)
            {
                // SuperMechCustomStats.AddModifier(actorId, stat.Key, stat.Value);
            }
        }

        /// <summary>获取单位种族显示名</summary>
        public static string GetSpeciesName(long actorId)
        {
            var id = GetSpeciesId(actorId);
            var species = SuperMechSpecies.GetSpecies(id);
            return species?.Name ?? LocalizedTextManager.getText("species_human");
        }

        /// <summary>获取单位种族天赋描述</summary>
        public static string GetSpeciesTalent(long actorId)
        {
            var id = GetSpeciesId(actorId);
            var species = SuperMechSpecies.GetSpecies(id);
            return species?.RacialTalent ?? "";
        }

        /// <summary>自然生成时随机分配种族（对照原著：不同星域有不同种族分布）
        /// v0.99.9 生物科技→种族互动：研究生物科技后高级种族出现概率提升</summary>
        public static string RollRandomSpecies(long kingdomId = 0)
        {
            var all = SuperMechSpecies.GetAllSpecies();
            var candidates = new List<SpeciesData>();

            // 基础：只有P0/P1种族自然生成
            // 生物T5（种族优化与变异）后P2也可自然生成
            // 生物T7（意识转移）后P3也可自然生成
            bool allowP2 = kingdomId > 0 && SuperMechTech.IsResearched(kingdomId, "bio_t5");
            bool allowP3 = kingdomId > 0 && SuperMechTech.IsResearched(kingdomId, "bio_t7");

            foreach (var s in all.Values)
            {
                if (s.Priority == "P0" || s.Priority == "P1")
                    candidates.Add(s);
                else if (s.Priority == "P2" && allowP2)
                    candidates.Add(s);
                else if (s.Priority == "P3" && allowP3)
                    candidates.Add(s);
            }

            if (candidates.Count == 0) return "human";

            // P0权重高，P1权重低，P2/P3权重更低
            // 生物T3（基因药剂）后P1权重提升
            bool boostP1 = kingdomId > 0 && SuperMechTech.IsResearched(kingdomId, "bio_t3");

            var weighted = new List<string>();
            foreach (var c in candidates)
            {
                int weight = c.Priority switch
                {
                    "P0" => 70,
                    "P1" => boostP1 ? 35 : 20,
                    "P2" => 8,
                    "P3" => 2,
                    _ => 10
                };
                for (int i = 0; i < weight; i++) weighted.Add(c.Id);
            }

            if (weighted.Count == 0) return "human";
            return weighted[UnityEngine.Random.Range(0, weighted.Count)];
        }

        /// <summary>获取单位可进化的种族列表</summary>
        public static List<string> GetEvolvableSpecies(long actorId)
        {
            var result = new List<string>();
            var all = SuperMechSpecies.GetAllSpecies();
            foreach (var s in all.Values)
            {
                if (CanEvolve(actorId, s.Id))
                {
                    result.Add(s.Id);
                }
            }
            return result;
        }

        /// <summary>自动进化（v0.142.0智能版：根据职业选择最优进化方向）</summary>
        public static bool AutoEvolve(long actorId)
        {
            var evolvable = GetEvolvableSpecies(actorId);
            if (evolvable.Count == 0) return false;

            // 获取单位职业，智能选择进化方向
            Actor a = FindActorById(actorId);
            string cls = a != null ? SuperMechBranch.GetClass(a) : "";

            // 根据职业优先级排序进化目标
            string best = evolvable[0];
            float bestScore = 0f;
            var all = SuperMechSpecies.GetAllSpecies();
            foreach (var sid in evolvable)
            {
                if (!all.TryGetValue(sid, out var sd)) continue;
                float score = 1f;

                // 机械师优先机械相关种族
                if (cls == SuperMechTraits.ClassMech && (sd.Id.Contains("mech") || sd.Id.Contains("cyborg") || sd.Id.Contains("robot")))
                    score += 3f;
                // 武道优先肉身强化种族
                else if (cls == SuperMechTraits.ClassMartial && (sd.Id.Contains("dragon") || sd.Id.Contains("beast") || sd.Id.Contains("giant")))
                    score += 3f;
                // 法师优先元素/灵能种族
                else if (cls == SuperMechTraits.ClassMage && (sd.Id.Contains("spirit") || sd.Id.Contains("element") || sd.Id.Contains("psi")))
                    score += 3f;
                // 异能者优先灵能种族
                else if (cls == SuperMechTraits.ClassPsi && (sd.Id.Contains("psi") || sd.Id.Contains("spirit") || sd.Id.Contains("mind")))
                    score += 3f;

                // 阶位越高，优先选择高阶种族
                int rank = a != null ? SuperMechAdvancement.GetExactRankIndex(a) : 0;
                score += rank * 0.1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = sid;
                }
            }

            return Evolve(actorId, best);
        }

        /// <summary>获取单位名（辅助方法）</summary>
        private static string GetActorName(long actorId)
        {
            // 通过WorldAPI获取单位名
            return $"单位{actorId}";
        }

        /// <summary>Tick：为新生成的单位随机分配种族（对照原著：不同星域有不同种族分布）</summary>
        public static void TickAssignSpecies()
        {
            if (World.world == null || World.world.units == null) return;

            foreach (var a in World.world.units)
            {
                if (a == null || a.data == null) continue;
                if (!_store.Has(a.data.id))
                {
                    // 新单位未分配种族，随机分配
                    // 降临者（玩家化身）保持人类种族
                    if (a.hasTrait(SuperMechTraits.Descendant) || a.hasTrait("sm_player_hanxiao"))
                    {
                        SetSpecies(a.data.id, "human");
                    }
                    else
                    {
                        long kid = a.kingdom != null ? a.kingdom.getID() : 0;
                        string species = RollRandomSpecies(kid);
                        SetSpecies(a.data.id, species);
                    }
                }
            }
        }

        /// <summary>存档：获取所有种族数据</summary>
        public static Dictionary<long, string> Save()
        {
            return new Dictionary<long, string>(_store.GetAll());
        }

        /// <summary>读档：恢复所有种族数据</summary>
        public static void Load(Dictionary<long, string> data)
        {
            _store.LoadAll(data);
        }

        /// <summary>通过ID查找单位</summary>
        private static Actor FindActorById(long id)
        {
            if (World.world?.units == null) return null;
            foreach (var u in World.world.units)
            {
                if (u != null && u.id == id) return u;
            }
            return null;
        }

        /// <summary>清理死亡单位数据</summary>
        public static void CleanupDead(HashSet<long> alive)
        {
            var toRemove = new List<long>();
            foreach (var kvp in _store.GetAll())
            {
                if (!alive.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove) _store.Remove(id);
        }
    }

    /// <summary>种族数据存储</summary>
    public class SpeciesDataStore : ActorDataStore<string>
    {
    }
}
