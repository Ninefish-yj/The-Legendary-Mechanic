using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.68.0 势力情报系统（原著向）
    /// 原著依据：
    ///  1. 情报机构（十三局/风眼/暗网）：情报汇聚、处理、分发，是势力的耳目；
    ///  2. 情报网络（风眼上下线/暗网情报中转）：渗透与情报买卖是势力的常规手段；
    ///  3. 情报优势：知己知彼——掌握对方情报的势力在对抗中占优；
    ///  4. 战争是全面博弈：切断线人网/破坏行动可削弱对手（原著：风眼被切断、悬赏刺杀）。
    /// 真实系统适配：情报点由文明科技与成员规模自动产出；行动作用于真实势力关系与战斗；
    /// 渗透加成挂接战斗补丁；破坏可暂停目标势力的贸易通道（v0.65）。
    /// </summary>
    public static class SuperMechIntel
    {
        public class FactionIntel
        {
            public float points = 0f;         // 情报点
            public int reconLevel = 0;        // 侦察深度（1=基础评估，2+=深入档案）
            public int infiltrations = 0;     // 渗透次数（每层+5%对目标伤害）
            public bool disrupted = false;    // 破坏行动生效中
            public string threatRating = "?"; // 威胁评级
        }

        public class IntelSaveData
        {
            public Dictionary<string, IntelEntry> factions = new Dictionary<string, IntelEntry>();
        }

        public class IntelEntry
        {
            public float points;
            public int reconLevel;
            public int infiltrations;
            public bool disrupted;
            public string threatRating;
        }

        private static readonly Dictionary<string, FactionIntel> _intel = new Dictionary<string, FactionIntel>();

        private const float IntelPerTechPerTick = 0.35f;   // 每Tick情报产出=科技等级×0.35
        private const float IntelPerMemberPerTick = 0.03f; // 每Tick情报产出=成员数×0.03
        private const float InfiltrateCost = 30f;          // 渗透消耗情报点
        private const float DisruptCost = 50f;             // 破坏行动消耗情报点
        private const float ReconCost = 10f;               // 侦察消耗情报点
        private const float InfiltrateBonusPerLevel = 0.05f; // 每层渗透对目标+5%伤害

        public static FactionIntel GetIntel(string factionId)
        {
            if (factionId == null) return null;
            if (!_intel.TryGetValue(factionId, out var data))
            {
                data = new FactionIntel();
                _intel[factionId] = data;
            }
            return data;
        }

        /// <summary>每Tick情报产出（原著：情报机构汇聚处理海量信息）</summary>
        public static void TickIntel(float delta)
        {
            if (!SuperMechConfig.IntelEnabled) return;
            foreach (var f in SuperMechFaction.GetAllFactions())
            {
                var data = GetIntel(f.id);
                if (data == null) continue;
                var kingdom = SuperMechFaction.GetFactionCivilization(f);
                int tech = kingdom == null ? 0 : SuperMechCivilization.GetTechLevelFromKingdom(kingdom);
                float gain = (tech * IntelPerTechPerTick + f.memberIds.Count * IntelPerMemberPerTick) * delta;
                data.points += gain;
                if (data.points > 1000f) data.points = 1000f; // 情报点上限
            }
        }

        /// <summary>侦察：建立/深化目标势力的威胁档案（原著：重大威胁目标档案）</summary>
        public static bool TryRecon(string factionId)
        {
            if (!SuperMechConfig.IntelEnabled) return false;
            var data = GetIntel(factionId);
            var f = FindFaction(factionId);
            if (data == null || f == null) return false;
            float cost = ReconCost + data.reconLevel * 15f;
            if (data.points < cost) return false;
            data.points -= cost;
            data.reconLevel++;
            data.threatRating = EvaluateThreat(f);
            SuperMechEventBus.Publish("IntelRecon", new IntelEvent { factionId = factionId, threatRating = data.threatRating });
            Debug.Log($"[超神机械师] 情报侦察完成：{f.name} 威胁评级 {data.threatRating}（档案深度 {data.reconLevel}）");
            return true;
        }

        /// <summary>渗透：获取对目标势力的情报优势（原著：知己知彼，行动成功率提升）</summary>
        public static bool TryInfiltrate(string factionId)
        {
            if (!SuperMechConfig.IntelEnabled) return false;
            var data = GetIntel(factionId);
            var f = FindFaction(factionId);
            if (data == null || f == null) return false;
            float cost = InfiltrateCost + data.infiltrations * 20f;
            if (data.points < cost) return false;
            data.points -= cost;
            data.infiltrations++;
            SuperMechEventBus.Publish("IntelInfiltrated", new IntelEvent { factionId = factionId, infiltrations = data.infiltrations });
            Debug.Log($"[超神机械师] 渗透成功：{f.name}（情报优势 {data.infiltrations} 层）");
            return true;
        }

        /// <summary>破坏行动：削弱目标——暂停其贸易通道（原著：战争是全面博弈，切断对手贸易线）</summary>
        public static bool TryDisrupt(string factionId)
        {
            if (!SuperMechConfig.IntelEnabled) return false;
            var data = GetIntel(factionId);
            var f = FindFaction(factionId);
            if (data == null || f == null) return false;
            if (data.points < DisruptCost) return false;
            data.points -= DisruptCost;
            data.disrupted = true;

            // 暂停目标势力参与的全部贸易通道（情报战切断对手经济线）
            int paused = 0;
            foreach (var route in SuperMechTrade.GetAllRoutes())
            {
                if (route.factionA == factionId || route.factionB == factionId)
                {
                    route.active = false;
                    paused++;
                }
            }
            SuperMechEventBus.Publish("IntelDisrupted", new IntelEvent { factionId = factionId, pausedRoutes = paused });
            Debug.Log($"[超神机械师] 破坏行动成功：{f.name} 的 {paused} 条贸易通道被切断");
            return true;
        }

        /// <summary>解除破坏状态（贸易通道恢复）</summary>
        public static void ClearDisrupted(string factionId)
        {
            var data = GetIntel(factionId);
            if (data != null) data.disrupted = false;
        }

        /// <summary>渗透伤害加成（挂接战斗补丁）：目标情报网被渗透后，对其作战的敌对势力获得伤害提升（原著：知己知彼）</summary>
        public static float GetIntelDamageBonus(Actor attacker, Actor target)
        {
            if (!SuperMechConfig.IntelEnabled || attacker == null || target == null) return 1f;
            var af = SuperMechFaction.GetFaction(attacker);
            var tf = SuperMechFaction.GetFaction(target);
            if (af == null || tf == null) return 1f;
            // 情报优势仅对敌对关系生效
            if (SuperMechFaction.GetRelation(af.id, tf.id) != SuperMechFaction.FactionRelation.Hostile) return 1f;
            var td = GetIntel(tf.id);
            if (td == null || td.infiltrations <= 0) return 1f;
            return 1f + td.infiltrations * InfiltrateBonusPerLevel;
        }

        /// <summary>威胁评估：基于阶位/文明等级/成员规模（原著：重大威胁档案评级）</summary>
        public static string EvaluateThreat(SuperMechFaction.FactionData f)
        {
            int maxRank = -1;
            var units = World.world.units?.units_only_alive;
            if (units != null)
            {
                foreach (var a in units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (!f.memberIds.Contains(a.id)) continue;
                    int r = SuperMechAdvancement.GetExactRankIndex(a);
                    if (r > maxRank) maxRank = r;
                }
            }
            string rating;
            if (maxRank >= 13) rating = "X（超神级威胁）";
            else if (maxRank >= 12) rating = "SS（巅峰超A级威胁）";
            else if (maxRank >= 10) rating = "S（超A级威胁）";
            else if (maxRank >= 8) rating = "A（A级威胁）";
            else if (maxRank >= 6) rating = "B（B级威胁）";
            else rating = "C（低威胁）";
            return rating + $" | 成员{f.memberIds.Count} | 最高战力阶位{(maxRank >= 0 ? SuperMechRanks.GetRankName(maxRank) : "无")}";
        }

        public static int GetThreatRankValue(string rating)
        {
            if (rating == null || rating.Length == 0) return 0;
            char c = rating[0];
            if (c == 'X') return 7;
            if (c == 'S') return 6;
            if (c == 'A') return 5;
            if (c == 'B') return 4;
            if (c == 'C') return 3;
            return 0;
        }

        private static SuperMechFaction.FactionData FindFaction(string id)
        {
            foreach (var f in SuperMechFaction.GetAllFactions())
                if (f.id == id) return f;
            return null;
        }

        public static IntelSaveData Save()
        {
            var data = new IntelSaveData();
            foreach (var kv in _intel)
            {
                data.factions[kv.Key] = new IntelEntry
                {
                    points = kv.Value.points,
                    reconLevel = kv.Value.reconLevel,
                    infiltrations = kv.Value.infiltrations,
                    disrupted = kv.Value.disrupted,
                    threatRating = kv.Value.threatRating
                };
            }
            return data;
        }

        public static void Load(IntelSaveData data)
        {
            _intel.Clear();
            if (data == null || data.factions == null) return;
            foreach (var kv in data.factions)
            {
                _intel[kv.Key] = new FactionIntel
                {
                    points = kv.Value.points,
                    reconLevel = kv.Value.reconLevel,
                    infiltrations = kv.Value.infiltrations,
                    disrupted = kv.Value.disrupted,
                    threatRating = kv.Value.threatRating
                };
            }
        }

        public static void Clear() => _intel.Clear();
    }

    /// <summary>情报事件（事件总线）</summary>
    public class IntelEvent
    {
        public string factionId;
        public string threatRating;
        public int infiltrations;
        public int pausedRoutes;
    }
}
