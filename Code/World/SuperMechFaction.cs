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

        private static readonly string[] FactionNamePrefixes = {
            "龙", "虎", "鹰", "狼", "熊", "凤", "麟", "龟", "蛇", "豹",
            "星", "月", "日", "云", "风", "雷", "冰", "火", "山", "海"
        };
        private static readonly string[] FactionNameSuffixes = {
            "阁", "堂", "门", "宗", "盟", "会", "社", "团", "府", "殿"
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
            string prefix = FactionNamePrefixes[Random.Range(0, FactionNamePrefixes.Length)];
            string suffix = FactionNameSuffixes[Random.Range(0, FactionNameSuffixes.Length)];
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
