using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.48.0 势力/组织系统
    /// v0.50.0 增强：势力间关系（敌对/中立/联盟）、势力等级、敌对战斗加成
    /// A阶及以上超能者可创建势力，低阶超能者可加入，成员获得加成，同势力不互相攻击
    /// </summary>
    public static class SuperMechFaction
    {
        private static bool _warnedSplit, _warnedMerge; // 一次性异常警告
        public enum FactionRelation { Neutral = 0, Allied = 1, Hostile = 2 }

        /// <summary>v0.61.0 势力政体：原著中势力形式多样（黑星军团=领袖制，超A级协会=议会制，虚灵教派=宗教制）</summary>
        public enum FactionGovernment { Autocracy = 0, Council = 1, Theocracy = 2 } // 领袖制/议会制/宗教制

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
            public FactionGovernment government = FactionGovernment.Autocracy; // v0.61.0 政体
            public List<long> elders = new List<long>(); // v0.61.0 议会制元老/宗教制长老
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

            // 5. 自动联盟（强者保护弱者）
            AutoAlliance(allUnits);

            // 6. v0.60.0 势力分裂（成员实力超过领袖时叛变）
            CheckFactionSplit(allUnits);

            // 7. v0.60.0 势力覆灭（领袖死亡无继任者时解散）
            CheckFactionCollapse(allUnits);
        }

        /// <summary>v0.60.0 势力分裂：成员实力超过领袖*1.2且成员>3时，有概率叛变分裂</summary>
        private static void CheckFactionSplit(List<Actor> units)
        {
            var toSplit = new List<FactionData>();
            foreach (var f in _factions.Values)
            {
                if (f.memberIds.Count < 4) continue; // 至少4人才可能分裂
                Actor leader = FindActor(f.leaderId, units);
                if (leader == null || !leader.isAlive()) continue;
                float leaderPower = SuperMechAdvancement.CalcOnar(leader);

                foreach (var mid in f.memberIds)
                {
                    if (mid == f.leaderId) continue;
                    Actor member = FindActor(mid, units);
                    if (member == null || !member.isAlive()) continue;
                    float memberPower = SuperMechAdvancement.CalcOnar(member);
                    // 成员实力超过领袖1.2倍，且有概率叛变
                    // v0.61.0 议会制/宗教制更稳定（分裂概率减半）
                    float splitChance = f.government == FactionGovernment.Autocracy ? 0.02f : 0.01f;
                    if (memberPower > leaderPower * 1.2f && Random.value < splitChance)
                    {
                        // 分裂：成员带走部分成员创建新势力
                        SplitFaction(f, member, units);
                        break;
                    }
                }
            }
        }

        /// <summary>分裂势力：成员带走1/3成员创建新势力</summary>
        private static void SplitFaction(FactionData original, Actor rebel, List<Actor> units)
        {
            try
            {
                // 创建新势力
                CreateFaction(rebel);
                var newFaction = GetFaction(rebel);
                if (newFaction == null) return;

                // 带走1/3成员（随机）
                int takeCount = Mathf.Max(1, original.memberIds.Count / 3);
                var candidates = new List<long>(original.memberIds);
                candidates.Remove(original.leaderId);
                candidates.Remove(rebel.id);
                for (int i = 0; i < takeCount && candidates.Count > 0; i++)
                {
                    int idx = Random.Range(0, candidates.Count);
                    long mid = candidates[idx];
                    candidates.RemoveAt(idx);
                    Actor m = FindActor(mid, units);
                    if (m != null) JoinFaction(m, newFaction.id);
                }

                // 原势力和新势力敌对
                SetRelation(original.id, newFaction.id, FactionRelation.Hostile);
                Debug.Log($"[超神机械师] 势力分裂：{original.name} → {newFaction.name}（{rebel.getName()}叛变）");
            }
            catch (System.Exception e) { if (!_warnedSplit) { _warnedSplit = true; Debug.LogWarning($"[超神机械师] 势力分裂异常(仅首次): {e.Message}"); } }
        }

        /// <summary>v0.60.0 势力覆灭：领袖死亡且无A阶以上继任者时解散</summary>
        private static void CheckFactionCollapse(List<Actor> units)
        {
            var toRemove = new List<string>();
            foreach (var f in _factions.Values)
            {
                Actor leader = FindActor(f.leaderId, units);
                if (leader != null && leader.isAlive()) continue;

                // 领袖死亡，找最强成员继任
                Actor successor = null;
                float maxPower = 0f;
                foreach (var mid in f.memberIds)
                {
                    Actor m = FindActor(mid, units);
                    if (m == null || !m.isAlive()) continue;
                    float power = SuperMechAdvancement.CalcOnar(m);
                    if (power > maxPower)
                    {
                        maxPower = power;
                        successor = m;
                    }
                }

                if (successor != null)
                {
                    // v0.61.0 议会制/宗教制：元老/长老直接继承（更稳定）
                    // 领袖制：需要A阶以上继任者
                    bool canInherit = f.government != FactionGovernment.Autocracy ||
                                      SuperMechAdvancement.GetExactRankIndex(successor) >= 8;
                    if (canInherit)
                    {
                        f.leaderId = successor.id;
                        Debug.Log($"[超神机械师] 势力传承：{f.name} 新领袖 {successor.getName()}（{GetGovernmentName(f.government)}）");
                        continue;
                    }
                }
                // 无合格继任者，势力解散
                toRemove.Add(f.id);
                Debug.Log($"[超神机械师] 势力覆灭：{f.name}（领袖死亡无继任者）");
            }

            foreach (var fid in toRemove)
            {
                DissolveFaction(fid);
            }
        }

        /// <summary>解散势力：所有成员变成无势力</summary>
        private static void DissolveFaction(string factionId)
        {
            if (!_factions.TryGetValue(factionId, out var f)) return;
            foreach (var mid in f.memberIds)
            {
                _actorFaction.Remove(mid);
            }
            _factions.Remove(factionId);
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

                // v0.61.0 议会制/宗教制：更新元老/长老列表（最强的2-3个非领袖成员）
                if (f.government != FactionGovernment.Autocracy && aliveCount >= 3)
                {
                    f.elders.Clear();
                    var sorted = new List<(long id, float p)>();
                    foreach (var mid in f.memberIds)
                    {
                        if (mid == f.leaderId) continue;
                        Actor m = FindActor(mid, units);
                        if (m == null || !m.isAlive()) continue;
                        sorted.Add((mid, SuperMechAdvancement.CalcOnar(m)));
                    }
                    sorted.Sort((a, b) => b.p.CompareTo(a.p));
                    int elderCount = f.government == FactionGovernment.Council ? 3 : 2;
                    for (int i = 0; i < Mathf.Min(elderCount, sorted.Count); i++)
                        f.elders.Add(sorted[i].id);
                }
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
                    SuperMechEventLogger.LogFactionWar(fa.name, fb.name);
                    Debug.Log($"[超神机械师] 势力战争：{fa.name} ↔ {fb.name}");
                }
            }
        }

        /// <summary>自动联盟：战力差距大的邻近势力，强者有概率保护弱者</summary>
        private static void AutoAlliance(List<Actor> units)
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

                    // 战力比超过3倍时，强者3%概率联盟弱者（保护关系）
                    float powerRatio = fa.totalPower > 0 && fb.totalPower > 0
                        ? Mathf.Max(fa.totalPower, fb.totalPower) / Mathf.Min(fa.totalPower, fb.totalPower)
                        : 1f;
                    if (powerRatio < 3f) continue;
                    if (Random.value > 0.03f) continue;

                    SetRelation(fa.id, fb.id, FactionRelation.Allied);
                    Debug.Log($"[超神机械师] 势力联盟：{fa.name} ↔ {fb.name}");
                }
            }
        }

        private static Actor FindActor(long id, List<Actor> units)
        {
            foreach (var u in units) if (u != null && u.id == id) return u;
            return null;
        }

        /// <summary>找到势力中最强的存活成员（按阶位+能级）</summary>
        private static long FindStrongestMember(FactionData f, HashSet<long> alive)
        {
            long best = 0;
            float bestPower = -1f;
            var allUnits = World.world?.units?.units_only_alive;
            if (allUnits == null) return 0;
            foreach (var mid in f.memberIds)
            {
                if (!alive.Contains(mid)) continue;
                Actor m = FindActor(mid, allUnits);
                if (m == null || !m.isAlive()) continue;
                float power = SuperMechAdvancement.CalcOnar(m);
                if (power > bestPower)
                {
                    bestPower = power;
                    best = mid;
                }
            }
            return best;
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

            // v0.61.0 根据后缀选择政体（原著：教派=宗教制，协会/议会=议会制，军团/帝国=领袖制）
            FactionGovernment gov = FactionGovernment.Autocracy;
            if (suffix.Contains("教派") || suffix.Contains("教廷") || suffix.Contains("神国") || suffix.Contains("道统") || suffix.Contains("圣殿"))
                gov = FactionGovernment.Theocracy;
            else if (suffix.Contains("协会") || suffix.Contains("议会") || suffix.Contains("评议会") || suffix.Contains("元老院") || suffix.Contains("共同体") || suffix.Contains("联合体"))
                gov = FactionGovernment.Council;

            var f = new FactionData
            {
                id = id,
                name = name,
                leaderId = leader.id,
                creationTick = 0,
                government = gov
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

        /// <summary>获取所有势力（按战力降序）</summary>
        public static List<FactionData> GetAllFactions()
        {
            var list = new List<FactionData>(_factions.Values);
            list.Sort((a, b) => b.totalPower.CompareTo(a.totalPower));
            return list;
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

        /// <summary>v0.54.0 代理战争：不同文明势力间伤害+5%，文明交战时额外+10%</summary>
        public static float GetProxyWarBonus(Actor attacker, Actor target)
        {
            var fa = GetFaction(attacker);
            var fb = GetFaction(target);
            if (fa == null || fb == null) return 1f;
            if (fa.id == fb.id) return 1f; // 同势力不触发

            Kingdom civA = GetFactionCivilization(fa);
            Kingdom civB = GetFactionCivilization(fb);
            if (civA == null || civB == null || civA == civB) return 1f; // 同文明不触发

            float bonus = 1.05f; // 不同文明势力间基础代理战争加成
            // 检查两个文明是否正在战争（原版War系统）
            if (IsKingdomsAtWar(civA, civB))
            {
                bonus = 1.15f; // 文明交战时代理战争升级
            }
            return bonus;
        }

        /// <summary>获取势力所属文明（领袖所在王国）</summary>
        public static Kingdom GetFactionCivilization(FactionData f)
        {
            if (f == null) return null;
            var units = World.world.units?.units_only_alive;
            if (units == null) return null;
            foreach (var a in units)
            {
                if (a != null && a.id == f.leaderId) return a.kingdom;
            }
            return null;
        }

        /// <summary>检查两个王国是否正在战争</summary>
        private static bool IsKingdomsAtWar(Kingdom a, Kingdom b)
        {
            if (a == null || b == null || World.world?.wars == null) return false;
            foreach (var war in World.world.wars)
            {
                if (war == null || war.isRekt()) continue;
                if (war.isInWarWith(a, b)) return true;
            }
            return false;
        }

        /// <summary>v0.54.0 代理战争击杀奖励：击杀敌对文明势力成员后，所属文明获得科技值</summary>
        public static void OnProxyKill(Actor killer, Actor victim)
        {
            if (killer == null || victim == null) return;
            var fk = GetFaction(killer);
            var fv = GetFaction(victim);
            if (fk == null || fv == null || fk.id == fv.id) return;

            Kingdom civK = GetFactionCivilization(fk);
            Kingdom civV = GetFactionCivilization(fv);
            if (civK == null || civV == null || civK == civV) return;

            // 击杀敌对文明势力成员，所属文明获得科技值
            int victimRank = SuperMechAdvancement.GetExactRankIndex(victim);
            float techReward = victimRank >= 13 ? 20f : victimRank >= 12 ? 10f : victimRank >= 10 ? 5f : 2f;
            SuperMechCivilization.AddTechPoints(civK, techReward);
            Debug.Log($"[超神机械师] 代理战争：{killer.name}({fk.name})击杀{victim.name}({fv.name})，{civK.name}获得{techReward}科技值");

            // v0.60.0 势力吞并：击杀敌对势力领袖，且战力碾压时吞并
            if (fv.leaderId == victim.id && fk.totalPower > fv.totalPower * 1.5f && Random.value < 0.3f)
            {
                TryAbsorbFaction(fk, fv);
            }
        }

        /// <summary>v0.61.0 获取政体名称</summary>
        public static string GetGovernmentName(FactionGovernment gov)
        {
            switch (gov)
            {
                case FactionGovernment.Council: return "议会制";
                case FactionGovernment.Theocracy: return "宗教制";
                default: return "领袖制";
            }
        }

        /// <summary>v0.60.0 吞并势力：小势力成员加入大势力</summary>
        private static void TryAbsorbFaction(FactionData absorber, FactionData target)
        {
            try
            {
                var units = World.world.units?.units_only_alive;
                if (units == null) return;

                // v0.61.0 宗教制成员忠诚度高，投降概率+20%
                float surrenderChance = target.government == FactionGovernment.Theocracy ? 0.7f : 0.5f;

                int absorbed = 0;
                foreach (var mid in target.memberIds)
                {
                    if (mid == target.leaderId) continue; // 领袖已死
                    Actor m = FindActor(mid, units);
                    if (m == null || !m.isAlive()) continue;
                    // 成员投降加入，否则变成无势力
                    if (Random.value < surrenderChance)
                    {
                        JoinFaction(m, absorber.id);
                        absorbed++;
                    }
                    else
                    {
                        _actorFaction.Remove(mid);
                    }
                }

                // 吞并后吸收目标势力的关系
                foreach (var rel in target.relations)
                {
                    if (rel.Key == absorber.id) continue;
                    if (!absorber.relations.ContainsKey(rel.Key))
                        absorber.relations[rel.Key] = rel.Value;
                }

                _factions.Remove(target.id);
                Debug.Log($"[超神机械师] 势力吞并：{absorber.name} 吞并 {target.name}（吸收{absorbed}人）");
            }
            catch (System.Exception e) { if (!_warnedMerge) { _warnedMerge = true; Debug.LogWarning($"[超神机械师] 势力吞并异常(仅首次): {e.Message}"); } }
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
                        // v0.50.1 领袖死亡：最强成员继承，无成员则解散
                        if (f.leaderId == id)
                        {
                            if (f.memberIds.Count > 0)
                            {
                                long newLeader = FindStrongestMember(f, alive);
                                if (newLeader > 0)
                                {
                                    f.leaderId = newLeader;
                                    var ctx = SuperMechActorContextRegistry.TryGet(newLeader);
                                    if (ctx != null) ctx.isFactionLeader = true;
                                    Debug.Log($"[超神机械师] {f.name} 领袖陨落，新领袖继承");
                                }
                                else
                                {
                                    // 所有成员都死了，解散
                                    foreach (var mid in f.memberIds) _actorFaction.Remove(mid);
                                    _factions.Remove(fid);
                                }
                            }
                            else
                            {
                                _factions.Remove(fid);
                            }
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
