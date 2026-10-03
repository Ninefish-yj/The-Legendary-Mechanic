using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>超能者AI行为系统（v0.45.0）
    /// 1. 自动竞争：相近阶位的超能者定期切磋竞争，胜者获得气力/潜能磨砺，败者负伤；
    ///    S+（超A级）以上竞争有陨落风险（原著：超A级竞争也存在真正死亡）。
    /// 2. NPC自动修炼：非降临者超能者自动积累潜能并解锁本系知识，补全"修炼→知识→能级→突破"闭环
    ///    （降临者走职业经验体系，由 SuperMechAwakened.TickAutoPlay 负责）。
    /// </summary>
    public static class SuperMechAI
    {
        // === 自动竞争 ===
        /// <summary>记录每个单位上次参与竞争的年龄（避免频繁竞争）</summary>
        private static readonly Dictionary<long, int> _lastCompetitionAge = new Dictionary<long, int>();
        /// <summary>每次Tick最多进行的竞争场次（控制节奏与性能）</summary>
        private const int MaxDuelsPerTick = 3;
        /// <summary>竞争匹配的最大距离（格；同王国的单位不受此限制）</summary>
        private const float CompetitionMaxDistance = 30f;

        // === NPC自动修炼 ===
        /// <summary>记录每个NPC上次自动解锁知识的年龄</summary>
        private static readonly Dictionary<long, int> _lastNpcKnowledgeAge = new Dictionary<long, int>();
        /// <summary>每次Tick最多自动解锁的知识条数（避免一次性大规模变动）</summary>
        private const int MaxNpcUnlocksPerTick = 20;

        /// <summary>自动竞争：Tick入口（由 SuperMechUnifiedTick 调度）</summary>
        public static void TickCompetition()
        {
            if (!SuperMechConfig.AutoCompetition) return;
            if (World.world == null || World.world.units == null) return;
            var units = World.world.units.units_only_alive;
            if (units == null || units.Count < 2) return;

            // 收集候选：超能者、非降临者、不在战斗中、达到最低阶位
            var candidates = new List<Actor>();
            foreach (Actor a in units)
            {
                if (a == null || a.id == null || !a.isAlive()) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者是玩家化身，不参与AI竞争
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (SuperMechQi.IsInCombat(a)) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) < SuperMechConfig.CompetitionMinRank) continue;
                candidates.Add(a);
            }
            if (candidates.Count < 2) return;

            int duels = 0;
            int attempts = 0;
            while (duels < MaxDuelsPerTick && attempts < 10 && candidates.Count >= 2)
            {
                attempts++;
                int i = Random.Range(0, candidates.Count);
                int j = Random.Range(0, candidates.Count);
                if (i == j) continue;

                Actor a = candidates[i];
                Actor b = candidates[j];
                int rankA = SuperMechAdvancement.GetExactRankIndex(a);
                int rankB = SuperMechAdvancement.GetExactRankIndex(b);
                // 只匹配相近阶位（原著：同层次强者互相磨砺，越阶竞争少见）
                if (Mathf.Abs(rankA - rankB) > 1) continue;
                if (!IsNearby(a, b)) continue;
                if (!CanCompete(a) || !CanCompete(b)) continue;

                _lastCompetitionAge[a.id] = a.age;
                _lastCompetitionAge[b.id] = b.age;
                ResolveDuel(a, b);
                duels++;
            }
        }

        /// <summary>是否处于可竞争范围：同王国默认可竞争，否则要求距离足够近</summary>
        private static bool IsNearby(Actor a, Actor b)
        {
            if (a == null || b == null) return false;
            if (a.kingdom != null && a.kingdom == b.kingdom) return true;
            // current_position 为 Vector2（世界坐标，1格=1单位），需保持Vector2运算
            Vector2 pa = a.current_position;
            Vector2 pb = b.current_position;
            float sqDist = (pa - pb).sqrMagnitude;
            return sqDist <= CompetitionMaxDistance * CompetitionMaxDistance;
        }

        /// <summary>竞争冷却：距离上次竞争是否已超过配置间隔</summary>
        private static bool CanCompete(Actor a)
        {
            if (a == null) return false;
            if (_lastCompetitionAge.TryGetValue(a.id, out int last))
            {
                if (a.age - last < SuperMechConfig.CompetitionInterval) return false;
            }
            return true;
        }

        /// <summary>结算一次竞争：胜者精进，败者负伤，高阶竞争小概率陨落</summary>
        private static void ResolveDuel(Actor a, Actor b)
        {
            try
            {
                float onarA = SuperMechAdvancement.CalcOnar(a);
                float onarB = SuperMechAdvancement.CalcOnar(b);
                float total = onarA + onarB;
                // 能级优势者胜率更高，但保留随机性（原著：竞争胜负不全看能级）
                float winChanceA = total > 0f ? 0.5f + (onarA - onarB) / total * 0.3f : 0.5f;
                winChanceA = Mathf.Clamp(winChanceA, 0.15f, 0.85f);

                Actor winner = Random.value < winChanceA ? a : b;
                Actor loser = winner == a ? b : a;

                int rankW = SuperMechAdvancement.GetExactRankIndex(winner);
                int rankL = SuperMechAdvancement.GetExactRankIndex(loser);
                int rankDiff = Mathf.Abs(rankW - rankL);

                // 胜者磨砺：气力上限提升 + 小概率潜能顿悟
                float qiGain = (20f + rankL * 10f) * (0.5f + Random.value * 0.5f);
                SuperMechQi.AddQiMax(winner, qiGain);
                if (Random.value < 0.25f)
                    SuperMechPotential.AddPotential(winner, 1);

                // 败者负伤：按能级差距成比例掉血，保留至少1/3血量（非致命）
                float hpRatio = Mathf.Min(0.35f, 0.10f + rankDiff * 0.05f);
                if (onarA > onarB * 1.5f || onarB > onarA * 1.5f)
                    hpRatio = Mathf.Min(0.5f, hpRatio + 0.1f);
                int dmg = (int)(loser.getMaxHealth() * hpRatio);
                int remain = Mathf.Max(loser.getMaxHealth() / 3, loser.data.health - dmg);
                loser.data.health = remain;

                // 高阶竞争（S+及以上）有陨落风险：原著超A级竞争也会真正死亡
                bool fatal = false;
                string result = LocalizedTextManager.getText("sm_ai_compete_win");
                if (rankL >= 11 && Random.value < 0.03f)
                {
                    fatal = true;
                    result = LocalizedTextManager.getText("sm_ai_compete_fatal");
                    try { loser.dieAndDestroy(AttackType.Other); }
                    catch { loser.data.health = 0; }
                }

                // 事件日志：只记录中高阶竞争（复用晋升日志的最低阶位阈值），避免刷屏
                int maxRank = Mathf.Max(rankW, rankL);
                if (maxRank >= SuperMechConfig.EventLogPromotionMinRank)
                {
                    SuperMechEventLogger.LogCompetition(winner, loser, result);
                }

                if (SuperMechConfig.LogVerbose)
                {
                    Debug.Log($"[超神机械师]【自动竞争】{winner.getName()}（阶位{rankW}）击败 {loser.getName()}（阶位{rankL}），胜者气力+{qiGain:F0}" + (fatal ? "，败者陨落" : "，败者负伤"));
                }
            }
            catch (System.Exception e)
            {
                if (SuperMechConfig.LogVerbose) Debug.LogWarning($"[超神机械师] 自动竞争异常: {e.Message}");
            }
        }

        /// <summary>NPC超能者自动修炼：自动解锁本系知识（Tick入口）</summary>
        public static void TickNpcKnowledge()
        {
            if (!SuperMechConfig.NpcAutoKnowledge) return;
            if (World.world == null || World.world.units == null) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            int unlocks = 0;
            foreach (Actor a in units)
            {
                if (a == null || a.id == null || !a.isAlive()) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者走职业体系（TickAutoPlay）
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (unlocks >= MaxNpcUnlocksPerTick) break;

                // 年龄间隔过滤（避免每个tick都扫描全量）
                if (_lastNpcKnowledgeAge.TryGetValue(a.id, out int last))
                {
                    if (a.age - last < SuperMechConfig.NpcKnowledgeInterval) continue;
                }
                if (SuperMechPotential.GetPotential(a) < 2) continue;

                string prefix = SuperMechKnowledge.GetClassPrefixByTraits(a);
                string nextId = SuperMechKnowledge.GetNextKnowledgeId(a, prefix);
                if (nextId == null) continue;

                var def = SuperMechKnowledge.GetDef(nextId);
                int cost = def != null ? def.cost : 2;
                if (SuperMechPotential.GetPotential(a) < cost) continue;

                _lastNpcKnowledgeAge[a.id] = a.age;
                if (SuperMechPotential.UnlockNode(a, nextId, cost))
                {
                    unlocks++;
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师]【NPC修炼】{a.getName()} 悟得新知识（{prefix}系）");
                }
            }
        }

        public static void Clear()
        {
            _lastCompetitionAge.Clear();
            _lastNpcKnowledgeAge.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_lastCompetitionAge, alive);
            removed += SuperMechCleanup.CleanDict(_lastNpcKnowledgeAge, alive);
            return removed;
        }
    }
}
