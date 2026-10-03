using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.48.0 势力/组织系统（原著：超能者组建势力，如韩萧的龙坦）
    /// A阶及以上超能者可创建势力，低阶超能者可加入，成员获得加成，同势力不互相攻击
    /// </summary>
    public static class SuperMechFaction
    {
        public class FactionData
        {
            public string id;
            public string name;
            public long leaderId;
            public List<long> memberIds = new List<long>();
            public int creationTick;
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
            "联邦", "王朝", "神国", "公社"
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

            Debug.Log($"[超神机械师] {leader.name} 创建势力[{name}]");
        }

        private static void JoinFaction(Actor actor, string factionId)
        {
            if (!_factions.TryGetValue(factionId, out var f)) return;
            f.memberIds.Add(actor.id);
            _actorFaction[actor.id] = factionId;
        }

        /// <summary>获取单位所属势力</summary>
        public static FactionData GetFaction(Actor a)
        {
            if (a == null) return null;
            if (!_actorFaction.TryGetValue(a.id, out var fid)) return null;
            if (!_factions.TryGetValue(fid, out var f)) return null;
            return f;
        }

        /// <summary>两单位是否同势力</summary>
        public static bool IsSameFaction(Actor a, Actor b)
        {
            if (a == null || b == null) return false;
            if (!_actorFaction.TryGetValue(a.id, out var fa)) return false;
            if (!_actorFaction.TryGetValue(b.id, out var fb)) return false;
            return fa == fb;
        }

        /// <summary>势力成员加成（经验获取）</summary>
        public static float GetMemberExpBonus(Actor a)
        {
            var f = GetFaction(a);
            if (f == null) return 1f;
            int memberCount = f.memberIds.Count;
            // 每多10个成员+5%经验，最多+30%
            return 1f + Mathf.Min(memberCount * 0.005f, 0.3f);
        }

        /// <summary>势力领袖加成</summary>
        public static float GetLeaderBonus(Actor a)
        {
            var f = GetFaction(a);
            if (f == null || f.leaderId != a.id) return 1f;
            return 1.1f; // 领袖伤害+10%
        }

        public static void Clear()
        {
            _factions.Clear();
            _actorFaction.Clear();
        }

        public static int FactionCount => _factions.Count;

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
        }

        public static FactionSaveData Save()
        {
            var data = new FactionSaveData();
            foreach (var f in _factions.Values)
            {
                data.factions.Add(new FactionEntry
                {
                    id = f.id,
                    name = f.name,
                    leaderId = f.leaderId,
                    memberIds = new List<long>(f.memberIds),
                    creationTick = f.creationTick
                });
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
                    creationTick = e.creationTick
                };
                _factions[f.id] = f;
                foreach (var mid in f.memberIds)
                    _actorFaction[mid] = f.id;
                // 保持nameCounter不重复
                if (int.TryParse(e.id.Replace("faction_", ""), out var n) && n > _nameCounter)
                    _nameCounter = n;
            }
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
