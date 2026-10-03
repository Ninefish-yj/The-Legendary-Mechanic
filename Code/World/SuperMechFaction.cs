using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.48.0 势力/组织系统（原著：超能者组建势力，如韩萧的龙坦）
    /// v0.50.0 增强：势力间关系（敌对/中立/联盟）、势力等级、敌对战斗加成
    /// A阶及以上超能者可创建势力，低阶超能者可加入，成员获得加成，同势力不互相攻击
    /// </summary>
    public static class SuperMechFaction
    {
        public enum FactionRelation { Neutral = 0, Allied = 1, Hostile = 2 }

        public class FactionData
        {
            public string id;
            public string name;
            public long leaderId;
            public List<long> memberIds = new List<long>();
            public int creationTick;
            public int level = 1;                     // 势力等级（按成员数+总能级计算）
            public float totalPower = 0f;             // 势力总战力（缓存，每tick更新）
            public Dictionary<string, FactionRelation> relations = new Dictionary<string, FactionRelation>();
        }

        private static readonly Dictionary<string, FactionData> _factions = new Dictionary<string, FactionData>();
        private static readonly Dictionary<long, string> _actorFaction = new Dictionary<long, string>(); // actorId -> factionId

        private const int MinCreateRank = 8;  // A阶可创建
        private const int MinJoinRank = 4;    // C阶可加入
        private const float CreateChance = 0.02f; // 每tick创建概率
        private const float JoinChance = 0.05f;   // 每tick加入概率
        private const int MaxFactions = 20;
        private static int _nameCounter = 0;

        // 原著风格势力命名：前缀区分降临者(地球文化)与原住民(异星科幻)，后缀通用
        // 原著：炎黄联盟是降临者专属前缀，虚灵/机械/赤色是原住民前缀；后缀军团/组织/联盟通用
        private static readonly string[] DescendantPrefixes = {
            "炎黄", "华夏", "神州", "中华", "黑星", "龙", "星辰", "昆仑",
            "蓬莱", "方丈", "九州", "五岳", "长江", "黄河", "青龙", "朱雀"
        };
        private static readonly string[] NativePrefixes = {
            "虚灵", "机械", "赤色", "圣约", "萌芽", "血金", "暗网", "银辉",
            "苍穹", "星渊", "破晓", "雷霆", "寒霜", "铁血", "暗夜", "星辉",
            "曜日", "苍蓝", "紫金", "破碎", "永恒", "自由", "荣耀", "深渊",
            "极光", "混沌", "虚空", "曜石", "赤焰", "幽影"
        };
        private static readonly string[] FactionSuffixes = {
            "军团", "教派", "协会", "组织", "联盟", "帝国", "商会", "共和国",
            "联邦", "王朝", "神国", "公社", "王国", "公国", "教廷", "议会",
            "财团", "集团", "学院", "研究院", "佣兵公会", "家族", "圣殿",
            "舰队", "共同体", "联合体", "阵线", "骑士团", "兄弟会", "评议会",
            "元老院", "部落", "氏族", "帮会", "密社", "隐修会", "堡垒",
            "道统", "冒险团", "护卫队", "革命军"
        };

        /// <summary>每tick处理势力创建和加入</summary>
        public static void TickFactions()
        {
            if (!SuperMechConfig.FactionEnabled) return;
            if (World.world == null || World.world.units == null) return;

            var allUnits = World.world.units.units_only_alive;
            if (allUnits == null) return;

            // 1. 高阶超能者创建势力
            foreach (var a in allUnits)
            {
                if (a == null || !a.isAlive()) continue;
                if (_actorFaction.ContainsKey(a.id)) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank < MinCreateRank) continue;
                if (_factions.Count >= MaxFactions) break;
                if (Random.value > CreateChance) continue;

                CreateFaction(a);
            }

            // 2. 低阶超能者加入势力
            foreach (var a in allUnits)
            {
                if (a == null || !a.isAlive()) continue;
                if (_actorFaction.ContainsKey(a.id)) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank < MinJoinRank || rank >= MinCreateRank) continue; // 中间阶位加入
                if (Random.value > JoinChance) continue;

                // 找最近的势力领袖
                FactionData best = null;
                float bestDist = float.MaxValue;
                foreach (var f in _factions.Values)
                {
                    Actor leader = FindActor(f.leaderId, allUnits);
                    if (leader == null || !leader.isAlive()) continue;
                    if (a.current_tile == null || leader.current_tile == null) continue;
                    float dist = Mathf.Abs(a.current_tile.x - leader.current_tile.x)
                               + Mathf.Abs(a.current_tile.y - leader.current_tile.y);
                    if (dist < bestDist && dist < 50f)
                    {
                        bestDist = dist;
                        best = f;
                    }
                }
                if (best != null) JoinFaction(a, best.id);
            }

            // 3. 更新势力等级和总战力
            UpdateFactionPower(allUnits);

            // 4. 自动宣战（邻近势力竞争）
            AutoDeclareWar(allUnits);
        }

        /// <summary>更新所有势力的等级和总战力</summary>
        private static void UpdateFactionPower(List<Actor> units)
        {
            foreach (var f in _factions.Values)
            {
                float power = 0f;
                int aliveCount = 0;
                foreach (var mid in f.memberIds)
                {
                    Actor m = FindActor(mid, units);
                    if (m == null || !m.isAlive()) continue;
                    aliveCount++;
                    power += SuperMechAdvancement.CalcOnar(m);
                }
                f.totalPower = power;
                // 势力等级：成员数+总战力综合计算
                f.level = SuperMechFormulas.CalcFactionLevel(aliveCount, power);
            }
        }

        /// <summary>自动宣战：邻近且战力相近的势力有概率敌对</summary>
        private static void AutoDeclareWar(List<Actor> units)
        {
            if (_factions.Count < 2) return;
            var list = new List<FactionData>(_factions.Values);
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    var fa = list[i];
                    var fb = list[j];
                    if (GetRelation(fa.id, fb.id) != FactionRelation.Neutral) continue;

                    // 领袖距离近（50格内）且战力比不超过3倍时，5%概率宣战
                    Actor la = FindActor(fa.leaderId, units);
                    Actor lb = FindActor(fb.leaderId, units);
                    if (la == null || lb == null || !la.isAlive() || !lb.isAlive()) continue;
                    if (la.current_tile == null || lb.current_tile == null) continue;
                    float dist = Mathf.Abs(la.current_tile.x - lb.current_tile.x)
                               + Mathf.Abs(la.current_tile.y - lb.current_tile.y);
                    if (dist > 50f) continue;

                    float powerRatio = fa.totalPower > 0 ? Mathf.Max(fa.totalPower, fb.totalPower) / Mathf.Max(1f, Mathf.Min(fa.totalPower, fb.totalPower)) : 1f;
                    if (powerRatio > 3f) continue;
                    if (Random.value > 0.05f) continue;

                    SetRelation(fa.id, fb.id, FactionRelation.Hostile);
                    SuperMechEventBus.Publish("FactionWarDeclared", new FactionWarEvent { factionA = fa.name, factionB = fb.name });
                    Debug.Log($"[超神机械师] 势力战争：{fa.name} ↔ {fb.name}");
                }
            }
        }

        private static Actor FindActor(long id, List<Actor> units)
        {
            foreach (var u in units) if (u != null && u.id == id) return u;
            return null;
        }

        private static void CreateFaction(Actor leader)
        {
            _nameCounter++;
            string id = "faction_" + _nameCounter;

            // 降临者(地球穿越者)用地球文化前缀，原住民用异星科幻前缀；后缀通用
            bool isDescendant = leader.hasTrait(SuperMechTraits.Descendant);
            var prefixes = isDescendant ? DescendantPrefixes : NativePrefixes;
            string prefix = prefixes[Random.Range(0, prefixes.Length)];
            string suffix = FactionSuffixes[Random.Range(0, FactionSuffixes.Length)];
            string name = prefix + suffix;

            var f = new FactionData
            {
                id = id,
                name = name,
                leaderId = leader.id,
                creationTick = 0
            };
            f.memberIds.Add(leader.id);
            _factions[id] = f;
            _actorFaction[leader.id] = id;
            // 同步到ActorContext
            var ctx = SuperMechActorContextRegistry.Get(leader);
            if (ctx != null) { ctx.factionId = id; ctx.isFactionLeader = true; }

            SuperMechEventBus.Publish("FactionCreated", new FactionCreatedEvent { leader = leader, factionName = name });
            Debug.Log($"[超神机械师] {leader.name} 创建势力[{name}]");
        }

        private static void JoinFaction(Actor actor, string factionId)
        {
            if (!_factions.TryGetValue(factionId, out var f)) return;
            f.memberIds.Add(actor.id);
            _actorFaction[actor.id] = factionId;
            // 同步到ActorContext
            var ctx = SuperMechActorContextRegistry.Get(actor);
            if (ctx != null) { ctx.factionId = factionId; ctx.isFactionLeader = false; }
        }

        /// <summary>从ActorContext读取势力ID（运行时主数据源）</summary>
        private static string GetActorFactionId(Actor a)
        {
            if (a == null) return null;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && !string.IsNullOrEmpty(ctx.factionId)) return ctx.factionId;
            // 回退到旧字典（兼容存档加载后尚未同步的情况）
            _actorFaction.TryGetValue(a.id, out var fid);
            return fid;
        }

        /// <summary>获取单位所属势力</summary>
        public static FactionData GetFaction(Actor a)
        {
            if (a == null) return null;
            string fid = GetActorFactionId(a);
            if (fid == null) return null;
            if (!_factions.TryGetValue(fid, out var f)) return null;
            return f;
        }

        /// <summary>两单位是否同势力</summary>
        public static bool IsSameFaction(Actor a, Actor b)
        {
            if (a == null || b == null) return false;
            string fa = GetActorFactionId(a);
            string fb = GetActorFactionId(b);
            return fa != null && fa == fb;
        }

        /// <summary>势力成员加成（经验获取）</summary>
        public static float GetMemberExpBonus(Actor a)
        {
            var f = GetFaction(a);
            if (f == null) return 1f;
            return SuperMechFormulas.FactionExpBonus(f.memberIds.Count);
        }

        /// <summary>势力领袖加成</summary>
        public static float GetLeaderBonus(Actor a)
        {
            var f = GetFaction(a);
            if (f == null || f.leaderId != a.id) return 1f;
            return 1.1f; // 领袖伤害+10%
        }

        /// <summary>获取两势力关系</summary>
        public static FactionRelation GetRelation(string factionA, string factionB)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return FactionRelation.Neutral;
            if (factionA == factionB) return FactionRelation.Allied;
            if (_factions.TryGetValue(factionA, out var fa) && fa.relations.TryGetValue(factionB, out var r))
                return r;
            return FactionRelation.Neutral;
        }

        /// <summary>设置两势力关系（双向）</summary>
        public static void SetRelation(string factionA, string factionB, FactionRelation relation)
        {
            if (!_factions.ContainsKey(factionA) || !_factions.ContainsKey(factionB)) return;
            _factions[factionA].relations[factionB] = relation;
            _factions[factionB].relations[factionA] = relation;
        }

        /// <summary>两单位所属势力是否敌对</summary>
        public static bool IsHostile(Actor a, Actor b)
        {
            if (a == null || b == null) return false;
            string fa = GetActorFactionId(a);
            string fb = GetActorFactionId(b);
            if (fa == null || fb == null) return false;
            return GetRelation(fa, fb) == FactionRelation.Hostile;
        }

        /// <summary>敌对势力战斗加成（攻击方伤害+15%）</summary>
        public static float GetHostileBonus(Actor attacker, Actor target)
        {
            return IsHostile(attacker, target) ? 1.15f : 1f;
        }

        /// <summary>获取势力等级</summary>
        public static int GetFactionLevel(Actor a)
        {
            var f = GetFaction(a);
            return f != null ? f.level : 0;
        }

        public static void Clear()
        {
            _factions.Clear();
            _actorFaction.Clear();
        }

        public static int FactionCount => _factions.Count;

        /// <summary>势力战争事件数据</summary>
        public struct FactionWarEvent
        {
            public string factionA;
            public string factionB;
        }

        // === 存档 ===
        [System.Serializable]
        public class FactionSaveData
        {
            public List<FactionEntry> factions = new List<FactionEntry>();
        }

        [System.Serializable]
        public class FactionEntry
        {
            public string id;
            public string name;
            public long leaderId;
            public List<long> memberIds = new List<long>();
            public int creationTick;
            public int level = 1;
            public List<string> relationKeys = new List<string>();
            public List<int> relationValues = new List<int>();
        }

        public static FactionSaveData Save()
        {
            var data = new FactionSaveData();
            foreach (var f in _factions.Values)
            {
                var entry = new FactionEntry
                {
                    id = f.id,
                    name = f.name,
                    leaderId = f.leaderId,
                    memberIds = new List<long>(f.memberIds),
                    creationTick = f.creationTick,
                    level = f.level
                };
                foreach (var kv in f.relations)
                {
                    entry.relationKeys.Add(kv.Key);
                    entry.relationValues.Add((int)kv.Value);
                }
                data.factions.Add(entry);
            }
            return data;
        }

        public static void Load(FactionSaveData data)
        {
            Clear();
            if (data == null) return;
            foreach (var e in data.factions)
            {
                var f = new FactionData
                {
                    id = e.id,
                    name = e.name,
                    leaderId = e.leaderId,
                    memberIds = new List<long>(e.memberIds),
                    creationTick = e.creationTick,
                    level = e.level > 0 ? e.level : 1
                };
                for (int i = 0; i < e.relationKeys.Count && i < e.relationValues.Count; i++)
                    f.relations[e.relationKeys[i]] = (FactionRelation)e.relationValues[i];
                _factions[f.id] = f;
                foreach (var mid in f.memberIds)
                    _actorFaction[mid] = f.id;
                // 保持nameCounter不重复
                if (int.TryParse(e.id.Replace("faction_", ""), out var n) && n > _nameCounter)
                    _nameCounter = n;
            }
            // 注：加载时Actor可能尚未恢复，不强制同步到ActorContext。
            // GetActorFactionId有回退到_actorFaction的逻辑，运行时首次访问会自动填充。
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var toRemove = new List<long>();
            foreach (var kv in _actorFaction)
                if (!alive.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var id in toRemove)
            {
                if (_actorFaction.TryGetValue(id, out var fid))
                {
                    if (_factions.TryGetValue(fid, out var f))
                    {
                        f.memberIds.Remove(id);
                        // 如果领袖死亡，解散势力
                        if (f.leaderId == id)
                        {
                            foreach (var mid in f.memberIds)
                                _actorFaction.Remove(mid);
                            _factions.Remove(fid);
                        }
                    }
                    _actorFaction.Remove(id);
                }
                removed++;
            }
            return removed;
        }
    }
}
