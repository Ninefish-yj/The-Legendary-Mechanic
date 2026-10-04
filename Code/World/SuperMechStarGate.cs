using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.67.0 星际航道·星门机制（原著向）
    /// 原著依据：
    ///  1. 跨星域星门是唯一稳定且快速跨星域的方法（原著第797章）；
    ///  2. 星门是技术门槛：星团级文明无法打造，超星团级文明拥有此技术；
    ///     三大文明（宇宙级）更禁止其他文明打造跨星域星门；
    ///  3. 星门跨越距离越长越难精确定位，容易出现偏差；
    ///  4. 秘密星门站可用隐形装置匿踪，防止雷达探测（原著第842章）。
    /// 真实系统适配：文明等级映射 SuperMechCivilization.CivLevel
    /// （SuperCluster=超星团级 可建造，Universal=宇宙级/三大文明 星门零偏差；
    ///  Cluster 及以下无技术，写死原著约束）；航道收益作用于贸易商队（v0.65）移动速度。
    /// </summary>
    public static class SuperMechStarGate
    {
        public class GateData
        {
            public string factionId;
            public bool built = false;
            public bool hidden = false;   // 秘密星门（匿踪）
            public int jumps = 0;         // 累计跳跃次数
        }

        public class StarGateSaveData
        {
            public List<GateEntry> gates = new List<GateEntry>();
        }

        public class GateEntry
        {
            public string factionId;
            public bool built;
            public bool hidden;
            public int jumps;
        }

        private static readonly Dictionary<string, GateData> _gates = new Dictionary<string, GateData>();

        // 数值
        public const float GateTravelSpeed = 2.5f;   // 星门航道：商队行程速度 ×2.5（原著：大幅缩短行程）
        public const float HiddenGateSpeed = 2.0f;   // 匿踪星门航道略保守
        private const float JumpBiasChance = 0.05f;  // 每次跳跃偏差概率（原著：距离越长越难精确定位）
        private const float JumpBiasLoss = 0.30f;    // 偏差造成的行程损失

        public static GateData GetGate(string factionId)
        {
            if (factionId == null) return null;
            if (!_gates.TryGetValue(factionId, out var g))
            {
                g = new GateData { factionId = factionId };
                _gates[factionId] = g;
            }
            return g;
        }

        public static bool HasGate(string factionId) => GetGate(factionId)?.built ?? false;

        /// <summary>建造星门：原著约束——只有超星团级及以上文明拥有技术；宇宙级（三大文明）星门零偏差</summary>
        public static bool TryBuildGate(string factionId)
        {
            if (!SuperMechConfig.StarGateEnabled) return false;
            var f = GetFactionById(factionId);
            if (f == null) return false;
            var kingdom = SuperMechFaction.GetFactionCivilization(f);
            if (kingdom == null) return false;

            // 原著：星门是技术门槛，星团级及以下文明无法打造
            var level = SuperMechCivilization.GetCivLevelFromKingdom(kingdom);
            if ((int)level < (int)SuperMechCivilization.CivLevel.SuperCluster) return false;

            var gate = GetGate(factionId);
            if (gate.built) return false;
            gate.built = true;
            gate.hidden = false;
            SuperMechEventBus.Publish("StarGateBuilt", new StarGateEvent { factionId = factionId });
            Debug.Log($"[超神机械师] 星门建成：{f.name}（文明等级 {SuperMechCivilization.GetLevelName(level)}）");
            return true;
        }

        /// <summary>秘密星门：匿踪切换（原著第842章：隐形装置掩盖能量反应）</summary>
        public static bool ToggleHidden(string factionId)
        {
            var gate = GetGate(factionId);
            if (gate == null || !gate.built) return false;
            gate.hidden = !gate.hidden;
            SuperMechEventBus.Publish("StarGateHiddenChanged", new StarGateEvent { factionId = factionId, hidden = gate.hidden });
            return true;
        }

        /// <summary>商队航速倍率：有星门则走星际航道（原著：大幅缩短行程）</summary>
        public static float GetTravelSpeedMultiplier(string factionId)
        {
            var gate = GetGate(factionId);
            if (gate == null || !gate.built) return 1f;
            return gate.hidden ? HiddenGateSpeed : GateTravelSpeed;
        }

        /// <summary>跳跃偏差：宇宙级（三大文明）星门零偏差；其余星门有定位偏差（原著：距离越长越难精确定位）</summary>
        public static bool TryJumpBias(string factionId)
        {
            var gate = GetGate(factionId);
            if (gate == null || !gate.built) return false;
            // 宇宙级文明星门零偏差（三大文明技术最成熟）
            var f = GetFactionById(factionId);
            if (f != null)
            {
                var kingdom = SuperMechFaction.GetFactionCivilization(f);
                if (kingdom != null && SuperMechCivilization.GetCivLevelFromKingdom(kingdom) == SuperMechCivilization.CivLevel.Universal)
                    return false;
            }
            return Random.value < JumpBiasChance;
        }

        public static float GetJumpBiasLoss() => JumpBiasLoss;

        /// <summary>文明等级门槛（供UI显示）</summary>
        public static bool CanBuild(string factionId)
        {
            var f = GetFactionById(factionId);
            if (f == null) return false;
            var kingdom = SuperMechFaction.GetFactionCivilization(f);
            if (kingdom == null) return false;
            return (int)SuperMechCivilization.GetCivLevelFromKingdom(kingdom) >= (int)SuperMechCivilization.CivLevel.SuperCluster;
        }

        /// <summary>每Tick维护：清理已消亡势力的星门</summary>
        public static void TickStarGates()
        {
            if (!SuperMechConfig.StarGateEnabled) return;
            var factions = SuperMechFaction.GetAllFactions();
            var dead = new List<string>();
            foreach (var kv in _gates)
            {
                bool exists = false;
                foreach (var f in factions) if (f.id == kv.Key) { exists = true; break; }
                if (!exists) dead.Add(kv.Key);
            }
            foreach (var id in dead) _gates.Remove(id);
        }

        private static SuperMechFaction.FactionData GetFactionById(string id)
        {
            foreach (var f in SuperMechFaction.GetAllFactions())
                if (f.id == id) return f;
            return null;
        }

        public static StarGateSaveData Save()
        {
            var data = new StarGateSaveData();
            foreach (var kv in _gates)
            {
                if (!kv.Value.built) continue;
                data.gates.Add(new GateEntry
                {
                    factionId = kv.Key, built = kv.Value.built,
                    hidden = kv.Value.hidden, jumps = kv.Value.jumps
                });
            }
            return data;
        }

        public static void Load(StarGateSaveData data)
        {
            _gates.Clear();
            if (data == null || data.gates == null) return;
            foreach (var e in data.gates)
                _gates[e.factionId] = new GateData { factionId = e.factionId, built = e.built, hidden = e.hidden, jumps = e.jumps };
        }

        public static void Clear() => _gates.Clear();
    }

    /// <summary>星门事件（事件总线）</summary>
    public class StarGateEvent
    {
        public string factionId;
        public bool hidden;
    }
}
