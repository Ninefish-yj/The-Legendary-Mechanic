using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>虚拟创世系统（原著：机械师虚拟分支超神级专属能力）
    /// 原著设定：韩萧转职【超神机械师】时领悟【虚拟创世（伪）】
    /// 核心功能：虚实转化——将虚拟设计图直接转化为实物，省略制造过程
    /// 限制（伪）：不知道的不能造、违背规律不能造、需消耗等量资源
    /// 去伪：超神级后补完，限制减少
    /// </summary>
    public static class SuperMechVirtualGenesis
    {
        public enum GenesisLevel { None, Pseudo, True }

        public class GenesisState
        {
            public GenesisLevel level = GenesisLevel.None;
            public long awakenTime = 0;
        }

        private static readonly Dictionary<long, GenesisState> _states = new Dictionary<long, GenesisState>();

        // 原著：虚拟创世是机械师虚拟分支转职超神机械师时领悟
        public const int StageForPseudo = 14;  // 阶段14=超神机械师
        public const int RankForTrue = 13;     // X阶去伪成真

        // 制造加速倍率（原著：省略制造过程，十分适合暴兵）
        public const float PseudoCraftSpeedMultiplier = 5f;   // 伪：5倍速
        public const float TrueCraftSpeedMultiplier = 999f;   // 真：瞬发

        // 召唤机械单位的资源消耗
        public const int SummonCostBasic = 50;   // 伪：召唤普通机械单位消耗
        public const int SummonCostTrue = 200;   // 真：召唤超神机械单位消耗

        /// <summary>检查单位是否拥有虚拟创世能力</summary>
        public static bool HasGenesis(Actor a)
        {
            if (a == null) return false;
            return _states.ContainsKey(a.id) && _states[a.id].level > GenesisLevel.None;
        }

        /// <summary>获取虚拟创世状态</summary>
        public static GenesisState GetState(Actor a)
        {
            if (a == null) return null;
            if (_states.TryGetValue(a.id, out var s)) return s;
            return null;
        }

        /// <summary>检查是否满足虚拟创世觉醒条件（机械师+虚拟分支+超神机械师阶段）</summary>
        public static bool CanAwaken(Actor a)
        {
            if (a == null || !a.isAlive()) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMech)) return false;
            if (!SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech)) return false;
            if (!SuperMechBranchMastery.HasSpec(a, SuperMechBranchMastery.SpecVirtual)) return false;
            if (SuperMechStage.GetStage(a) < StageForPseudo) return false;
            return true;
        }

        /// <summary>转职超神机械师时觉醒虚拟创世（伪）</summary>
        public static bool TryAwakenPseudo(Actor a)
        {
            if (!CanAwaken(a)) return false;
            if (HasGenesis(a)) return false;

            _states[a.id] = new GenesisState
            {
                level = GenesisLevel.Pseudo,
                awakenTime = System.DateTime.Now.Ticks
            };

            Debug.Log($"[超神机械师]【虚拟创世】{a.getName()} 转职超神机械师，领悟【虚拟创世（伪）】！");
            return true;
        }

        /// <summary>X阶后去伪成真</summary>
        public static bool TryAscendToTrue(Actor a)
        {
            if (a == null) return false;
            var state = GetState(a);
            if (state == null || state.level != GenesisLevel.Pseudo) return false;
            if (SuperMechActorContextRegistry.GetRank(a) < RankForTrue) return false;

            state.level = GenesisLevel.True;
            Debug.Log($"[超神机械师]【虚拟创世】{a.getName()} 达到超神级，【虚拟创世（伪）】去伪成真！");
            return true;
        }

        /// <summary>获取制造速度倍率（虚实转化：省略制造过程）</summary>
        public static float GetCraftSpeedMultiplier(Actor a)
        {
            var state = GetState(a);
            if (state == null) return 1f;
            if (state.level == GenesisLevel.True) return TrueCraftSpeedMultiplier;
            return PseudoCraftSpeedMultiplier;
        }

        /// <summary>是否可以瞬造（虚拟创世真）</summary>
        public static bool CanInstantCraft(Actor a)
        {
            var state = GetState(a);
            return state != null && state.level == GenesisLevel.True;
        }

        // === 研究逆向（原著：造出临时产物反向推导技术，效率提升千百倍）===
        public const float PseudoResearchCostMultiplier = 0.5f;  // 伪：研究消耗减半
        public const float TrueResearchCostMultiplier = 0.2f;    // 真：研究消耗20%
        public const int VirtualResearchCostBasic = 30;          // 虚拟推演基础消耗
        public const int VirtualResearchCostTrue = 80;           // 虚拟推演（真）消耗

        /// <summary>获取知识研究消耗倍率（虚拟创世研究逆向）</summary>
        public static float GetResearchCostMultiplier(Actor a)
        {
            var state = GetState(a);
            if (state == null) return 1f;
            if (state.level == GenesisLevel.True) return TrueResearchCostMultiplier;
            return PseudoResearchCostMultiplier;
        }

        /// <summary>虚拟推演：直接解锁下一个未解锁知识（原著：造临时产物反向推导）</summary>
        public static bool VirtualResearch(Actor researcher)
        {
            if (researcher == null || !researcher.isAlive()) return false;
            if (!HasGenesis(researcher)) return false;

            var state = GetState(researcher);
            bool isTrue = state.level == GenesisLevel.True;
            int cost = isTrue ? VirtualResearchCostTrue : VirtualResearchCostBasic;

            if (SuperMechPotential.GetPotential(researcher) < cost) return false;

            // 找到下一个未解锁的知识
            string prefix = SuperMechKnowledge.GetClassPrefixByTraits(researcher);
            string nextId = SuperMechKnowledge.GetNextKnowledgeId(researcher, prefix);
            if (nextId == null) return false;

            SuperMechPotential.SpendPotential(researcher, cost);
            SuperMechKnowledge.Unlock(researcher, nextId);
            SuperMechAwakened.AddXp(researcher, 1000f);

            Debug.Log($"[超神机械师]【虚拟创世】{researcher.getName()} 虚拟推理解锁知识：{nextId}，消耗{cost}潜能点");
            return true;
        }

        /// <summary>召唤机械单位（虚实转化：直接从虚拟设计图转化为实物）</summary>
        public static bool SummonMechUnit(Actor summoner)
        {
            if (summoner == null || !summoner.isAlive()) return false;
            if (!HasGenesis(summoner)) return false;

            var state = GetState(summoner);
            bool isTrue = state.level == GenesisLevel.True;
            int cost = isTrue ? SummonCostTrue : SummonCostBasic;

            // 消耗资源（原著：转化需要消耗等量资源）
            // 简化：消耗潜能点作为资源
            if (SuperMechPotential.GetPotential(summoner) < cost) return false;
            SuperMechPotential.SpendPotential(summoner, cost);

            // 在召唤者身边生成机械单位
            WorldTile tile = summoner.currentTile;
            if (tile == null) tile = World.world.GetRandomTile();
            if (tile == null) return false;

            Actor summoned = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (summoned == null) return false;

            summoned.addTrait(SuperMechTraits.ClassMech);
            summoned.addTrait("aggressive");
            summoned.addTrait("sm_summoned");

            // 虚拟创世真：召唤超神级机械单位
            if (isTrue)
            {
                summoned.addTrait("sm_void_boost");
                SuperMechAdvancement.SetExactRank(summoned, 10); // S阶
            }
            else
            {
                int rank = Mathf.Max(0, SuperMechActorContextRegistry.GetRank(summoner) - 1);
                SuperMechAdvancement.SetExactRank(summoned, rank);
            }

            // 记录召唤者
            var ctx = SuperMechActorContextRegistry.Get(summoned);
            if (ctx != null) ctx.summonerId = summoner.id;

            // 短寿命消散（原著：虚拟转化的暂时性产物）
            var sctx = SuperMechActorContextRegistry.Get(summoned);
            if (sctx != null) sctx.expireAge = summoned.age + 600; // 600岁后消散

            Debug.Log($"[超神机械师]【虚拟创世】{summoner.getName()} 虚实转化召唤机械单位（{(isTrue ? "超神级" : "普通")}），消耗{cost}潜能点");
            return true;
        }

        /// <summary>Tick：检查去伪条件</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.units == null) return;

            foreach (var kv in _states)
            {
                Actor a = null;
                foreach (var u in World.world.units) if (u != null && u.id == kv.Key) { a = u; break; }
                if (a == null || !a.isAlive()) continue;

                var state = kv.Value;
                if (state.level == GenesisLevel.Pseudo &&
                    SuperMechActorContextRegistry.GetRank(a) >= RankForTrue)
                {
                    TryAscendToTrue(a);
                }
            }
        }

        public static void Clear()
        {
            _states.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_states, alive);
        }

        // === 存档 ===
        [System.Serializable]
        public class GenesisSaveData
        {
            public long id;
            public int level;
            public long awakenTime;
        }

        public static List<GenesisSaveData> Save()
        {
            var list = new List<GenesisSaveData>();
            foreach (var kv in _states)
            {
                list.Add(new GenesisSaveData
                {
                    id = kv.Key,
                    level = (int)kv.Value.level,
                    awakenTime = kv.Value.awakenTime
                });
            }
            return list;
        }

        public static void Load(List<GenesisSaveData> data)
        {
            _states.Clear();
            if (data == null) return;
            foreach (var d in data)
            {
                _states[d.id] = new GenesisState
                {
                    level = (GenesisLevel)d.level,
                    awakenTime = d.awakenTime
                };
            }
        }
    }
}
