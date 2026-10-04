using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.72.0 黑星军团·军团命令（原著细还原；v0.75.2 修正为原著贡献星级制）
    /// 原著依据：
    ///  1. 黑星佣兵团→黑星军团，信用积分+阵营贡献度体系（原著#333：信用积分120奖励4w5经验+300贡献度，贡献度前三额外奖励）；
    ///  2. 阵营成长-韩萧获利-奖励玩家良性循环（原著#832：任务完成→贡献→奖励→军衔提升→新称号；#729：出征凯旋→玩家赚奖励→回来消费）；
    ///  3. 战争雇佣子任务（原著#580：战争雇佣任务给经验与酬金）；
    ///  4. 一人即军团：机械军团百万级+械力加成（原著#702）；机械帝皇统领无数机械战兵、军团流巨大加成（原著#753）；
    ///  5. 贡献星级制（原著#534）：黑星军团共六级——1星~5星+最高级"黑星十八骑"（排行榜前18名5星成员专属）；
    ///     晋升三要素=信用积分（完成雇佣任务累积）+阵营关系（贡献≥1000达[友好]）+总贡献量
    ///     （历史累计、消费不减、不降级；1星→2星需总贡献20000点）；贡献可在阵营商店消费（50恩纳=100贡献）。
    /// 真实系统适配：军团长由降临者击杀榜第一担任；成员击杀累积总贡献（只增不减，消费不减不降级对应原著#534）；
    /// 星级=1星~5星，击杀榜前18名的5星成员获"黑星十八骑"称号（加成更高，原著：对应收益自然高很多）；
    /// 命令=集结/远征；远征期间贡献双倍（战争雇佣）。
    /// </summary>
    public static class SuperMechLegion
    {
        public const string CommanderTrait = "sm_legion_commander"; // 军团长
        public const string MemberTrait = "sm_legion_member";      // 军团成员
        public const string KnightTrait = "sm_blackstar_knight";   // 黑星十八骑（原著#534：排行榜前18名5星成员）

        public enum CommandType { None, Rally, Expedition }

        public class LegionSaveData
        {
            public long commanderId = -1;
            public Dictionary<string, int> credit = new Dictionary<string, int>(); // actorId -> 总贡献量（原著#534：历史累计、消费不减、不降级）
            public CommandType command = CommandType.None;
            public int commandTicksLeft;
            public int totalKills;
        }

        private static readonly LegionSaveData _data = new LegionSaveData();
        public static LegionSaveData Data => _data;

        // 贡献星级阈值（原著#534：六级制=1星~5星+黑星十八骑；模组贡献尺度等比缩放，晋升只增不减）
        private static readonly (int threshold, string name, float dmgBonus)[] Ranks =
        {
            (0,    "1星",      0.00f),
            (100,  "2星",      0.05f),
            (300,  "3星",      0.10f),
            (800,  "4星",      0.15f),
            (2000, "5星",      0.20f),
            (5000, "黑星十八骑", 0.30f),
        };

        private const int CreditPerKill = 10;          // 每击杀+10总贡献（原著：阵营贡献度/总贡献历史）
        private const int CommanderCut = 1;            // 军团长每10击杀+1潜能（原著：韩萧获利）
        private const float ExpeditionCreditMult = 2f; // 战争雇佣：远征期间贡献双倍（原著#580）
        private const int CommandDurationTicks = 600;  // 命令持续
        private const float MemberCountBonus = 0.02f;  // 每名成员军团长+2%伤害（原著：军团流巨大加成）

        // ============ 编制 ============

        /// <summary>刷新军团长：降临者中击杀榜第一（原著：黑星军团以玩家为核心成员，军团长统领）</summary>
        public static void RefreshCommander()
        {
            long bestId = -1;
            int bestKills = -1;
            foreach (var kv in SuperMechPlayer.Data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                var a = FindActorById(id);
                if (a == null || !a.isAlive()) continue;
                if (kv.Value.kills > bestKills) { bestKills = kv.Value.kills; bestId = id; }
            }
            if (bestId == _data.commanderId) return;

            // 移交军团长
            if (_data.commanderId > 0)
            {
                var old = FindActorById(_data.commanderId);
                if (old != null && old.hasTrait(CommanderTrait)) old.removeTrait(CommanderTrait);
            }
            _data.commanderId = bestId;
            var commander = FindActorById(bestId);
            if (commander != null && !commander.hasTrait(CommanderTrait)) commander.addTrait(CommanderTrait);
        }

        public static Actor GetCommander() => _data.commanderId > 0 ? FindActorById(_data.commanderId) : null;

        /// <summary>军团成员：降临者（sm_player）或机械召唤物（sm_summoned）</summary>
        public static bool IsMember(Actor a) => a != null && (a.hasTrait(MemberTrait) || a.hasTrait(CommanderTrait));

        /// <summary>统计在册成员数量（含军团长）</summary>
        public static int MemberCount
        {
            get
            {
                if (World.world == null || World.world.units == null) return 0;
                int n = 0;
                var units = World.world.units.units_only_alive;
                if (units == null) return 0;
                foreach (var a in units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (a.hasTrait(CommanderTrait) || a.hasTrait(MemberTrait) || a.hasTrait(SuperMechPlayer.PlayerTrait)) n++;
                }
                return n;
            }
        }

        /// <summary>星级查询（原著#534：1星~5星+黑星十八骑六级制，贡献只增不减）</summary>
        public static (string name, float dmgBonus) GetRank(int credit)
        {
            var current = Ranks[0];
            foreach (var r in Ranks)
                if (credit >= r.threshold) current = r;
            return (current.name, current.dmgBonus);
        }

        /// <summary>黑星十八骑判定（原著#534：排行榜前18名的5星成员，收益自然高很多）</summary>
        public static bool IsEighteenKnight(Actor a)
        {
            if (a == null || !a.hasTrait(MemberTrait) && !a.hasTrait(CommanderTrait) && !a.hasTrait(SuperMechPlayer.PlayerTrait)) return false;
            if (GetCredit(a) < 2000) return false; // 需达5星
            int rank = GetKillRank(a.id);
            return rank > 0 && rank <= 18;
        }

        /// <summary>降临者击杀榜名次（1=榜首）</summary>
        private static int GetKillRank(long actorId)
        {
            var list = new List<KeyValuePair<long, int>>();
            foreach (var kv in SuperMechPlayer.Data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                list.Add(new KeyValuePair<long, int>(id, kv.Value.kills));
            }
            list.Sort((x, y) => y.Value.CompareTo(x.Value));
            for (int i = 0; i < list.Count; i++)
                if (list[i].Key == actorId) return i + 1;
            return -1;
        }

        public static int GetCredit(Actor a)
        {
            if (a == null) return 0;
            return _data.credit.TryGetValue(a.id.ToString(), out int c) ? c : 0;
        }

        // ============ 主循环 ============

        public static void TickLegion(float delta)
        {
            if (!SuperMechConfig.LegionEnabled) return;
            if (World.world == null || World.world.units == null) return;

            // 军团长刷新（降临者击杀榜第一）
            RefreshCommander();

            // 命令执行
            if (_data.command != CommandType.None)
            {
                _data.commandTicksLeft--;
                if (_data.commandTicksLeft <= 0)
                {
                    _data.command = CommandType.None;
                    return;
                }
                var commander = GetCommander();
                if (commander == null || !commander.isAlive())
                {
                    _data.command = CommandType.None;
                    return;
                }
                ExecuteCommand(commander);
            }
        }

        private static void ExecuteCommand(Actor commander)
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            if (_data.command == CommandType.Rally)
            {
                // 集结：成员聚拢到军团长身边
                foreach (var a in units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (!IsMember(a) && a.id != commander.id) continue;
                    if (a.current_tile == null || commander.current_tile == null) continue;
                    float dist = Mathf.Abs(a.current_tile.x - commander.current_tile.x) + Mathf.Abs(a.current_tile.y - commander.current_tile.y);
                    if (dist > 3f) MoveToNear(a, commander);
                }
            }
            else if (_data.command == CommandType.Expedition)
            {
                // 远征：全体攻击最近的宇宙异兽（评级标准之外的宇宙威胁，军团通缉目标）
                Actor target = FindNearestBeast(commander);
                if (target == null || !target.isAlive())
                {
                    _data.command = CommandType.None;
                    return;
                }
                foreach (var a in units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (!IsMember(a) && a.id != commander.id) continue;
                    SetAttackTarget(a, target);
                    if (a.current_tile != null && target.current_tile != null)
                    {
                        float dist = Mathf.Abs(a.current_tile.x - target.current_tile.x) + Mathf.Abs(a.current_tile.y - target.current_tile.y);
                        if (dist > 2f) MoveToNear(a, target);
                    }
                }
            }
        }

        // ============ 命令接口（UI触发） ============

        public static bool TryCommand(CommandType type)
        {
            var commander = GetCommander();
            if (commander == null || !commander.isAlive()) return false;
            _data.command = type;
            _data.commandTicksLeft = CommandDurationTicks;
            return true;
        }

        public static void CancelCommand() => _data.command = CommandType.None;

        // ============ 战斗挂接 ============

        /// <summary>军团成员击杀：总贡献+星级成长；军团长获益（原著：阵营成长-韩萧获利-奖励玩家）</summary>
        public static void OnLegionKill(Actor killer, Actor target)
        {
            if (killer == null) return;
            bool isPlayer = killer.hasTrait(SuperMechPlayer.PlayerTrait);
            bool isMember = killer.hasTrait(MemberTrait) || killer.hasTrait(CommanderTrait);
            if (!isPlayer && !isMember) return;

            // 总贡献量（只增不减，原著#534：即使消费/花掉也不降级）
            int gain = CreditPerKill;
            if (_data.command == CommandType.Expedition) gain = (int)(gain * ExpeditionCreditMult);
            _data.credit[killer.id.ToString()] = GetCredit(killer) + gain;
            _data.totalKills++;

            // 军团长收益：每10击杀+1潜能（原著：韩萧获利）
            if (_data.totalKills % 10 == 0)
            {
                var commander = GetCommander();
                if (commander != null && commander.isAlive())
                    SuperMechPotential.AddPotential(commander, CommanderCut);
            }

            // 成员自动挂军团标记（避免重复遍历）
            if (!killer.hasTrait(MemberTrait) && !killer.hasTrait(CommanderTrait))
                killer.addTrait(MemberTrait);
        }

        /// <summary>军团长军团流加成（原著：一人即军团/机械帝皇军团流）</summary>
        public static float GetCommanderBonus(Actor a)
        {
            if (a == null || !a.hasTrait(CommanderTrait)) return 1f;
            return 1f + MemberCount * MemberCountBonus;
        }

        /// <summary>成员星级伤害加成（原著#534：黑星十八骑收益高很多）</summary>
        public static float GetMemberBonus(Actor a)
        {
            if (a == null) return 1f;
            if (!a.hasTrait(MemberTrait) && !a.hasTrait(CommanderTrait) && !a.hasTrait(SuperMechPlayer.PlayerTrait)) return 1f;
            if (IsEighteenKnight(a)) return 1f + Ranks[5].dmgBonus;
            var (_, dmg) = GetRank(GetCredit(a));
            return 1f + dmg;
        }

        // ============ 工具 ============

        private static Actor FindNearestBeast(Actor from)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            Actor best = null;
            float bestDist = float.MaxValue;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || !a.hasTrait(SuperMechCosmicBeast.BeastTrait)) continue;
                if (from.current_tile == null || a.current_tile == null) continue;
                float d = Mathf.Abs(from.current_tile.x - a.current_tile.x) + Mathf.Abs(from.current_tile.y - a.current_tile.y);
                if (d < bestDist) { bestDist = d; best = a; }
            }
            return best;
        }

        private static void MoveToNear(Actor a, Actor target)
        {
            if (a == null || target == null || target.current_tile == null) return;
            try
            {
                if (target.current_tile.neighbours != null && target.current_tile.neighbours.Length > 0)
                {
                    var tile = target.current_tile.neighbours[Random.Range(0, target.current_tile.neighbours.Length)];
                    a.moveTo(tile);
                }
            }
            catch { }
        }

        private static void SetAttackTarget(Actor a, Actor target)
        {
            if (a == null || target == null) return;
            try
            {
                var field = typeof(Actor).GetField("attack_target",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(a, target);
            }
            catch { }
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units) if (a != null && a.id == id) return a;
            return null;
        }

        // ============ 存档 ============

        public static LegionSaveData Save() => _data;

        public static void Load(LegionSaveData data)
        {
            _data.credit.Clear();
            if (data == null) return;
            _data.commanderId = data.commanderId;
            if (data.credit != null)
                foreach (var kv in data.credit) _data.credit[kv.Key] = kv.Value;
            _data.command = data.command;
            _data.commandTicksLeft = data.commandTicksLeft;
            _data.totalKills = data.totalKills;
        }

        public static void Clear()
        {
            _data.commanderId = -1;
            _data.credit.Clear();
            _data.command = CommandType.None;
            _data.totalKills = 0;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<string>();
            foreach (var kv in _data.credit)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                if (!alive.Contains(id)) dead.Add(kv.Key);
            }
            foreach (var key in dead)
            {
                _data.credit.Remove(key);
                removed++;
            }
            return removed;
        }
    }
}
