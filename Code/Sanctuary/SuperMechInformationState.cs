using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 信息态记录系统：单位死亡时记录信息态烙印，可用于圣所复活
    /// 用Tick扫描方式对比存活快照，不新增Harmony补丁
    /// </summary>
    public static class SuperMechInformationState
    {
        /// <summary>信息态记录（原著：以信息态形式完整记录文明数据，超A级可通过信息态复苏）
        /// 复活时通过完整快照恢复，确保新单位尽可能接近原单位
        /// </summary>
        public class InformationStateRecord
        {
            public long actorId;
            public string name;
            public string species;
            public string classTrait;
            public string branchTrait;
            public int stage;
            public int rankIndex;
            public float qi;
            public int qiLevel;
            public int qiLayer;
            public float energyLevel;
            public int potential;
            public List<string> knowledge;
            public List<string> equipBag;
            public string currentEquip;
            public int sanctuaryAuthority;
            public int reviveCount;
            public long diedAt;
            public long worldAge;
            public int iterationId;
            // v0.38.2 完整信息态：神性、遗力、技能、进阶任务
            public int divinityLevel;
            public bool divinityAwakened;
            public int legacyPower;
            public List<string> legacySourceNames;
            public List<int> legacySourceRanks;
            public List<float> legacySourceDamages;
            public List<string> legacyDeathTypes;
            public bool advancementTaskDone;
            public float advancementProgress;
            public int subLevel;
            public List<string> skills;
            public List<string> traits;
            public int deathAge; // 死亡时年龄（复活后保持年龄连续性，圣所重塑肉身为巅峰状态）
        }

        private static readonly Dictionary<long, InformationStateRecord> _aliveSnapshot = new Dictionary<long, InformationStateRecord>();
        private static readonly List<InformationStateRecord> _deadStates = new List<InformationStateRecord>();
        private const int MaxDeadRecords = 50;
        private const float ScanInterval = 5f; // 每5秒扫描一次
        private static float _scanTimer;

        /// <summary>获取所有死亡信息态</summary>
        public static List<InformationStateRecord> GetDeadStates()
        {
            return _deadStates;
        }

        /// <summary>根据ID获取死亡信息态</summary>
        public static InformationStateRecord GetDeadState(long actorId)
        {
            foreach (var s in _deadStates)
            {
                if (s.actorId == actorId) return s;
            }
            return null;
        }

        /// <summary>移除已复活的信息态</summary>
        public static void RemoveDeadState(long actorId)
        {
            _deadStates.RemoveAll(s => s.actorId == actorId);
        }

        /// <summary>Tick扫描：对比存活快照，发现死亡单位记录信息态</summary>
        public static void Tick(float deltaTime)
        {
            _scanTimer += deltaTime;
            if (_scanTimer < ScanInterval) return;
            _scanTimer = 0f;

            if (World.world == null || World.world.units == null) return;

            // 收集当前存活单位ID
            var aliveIds = new HashSet<long>();
            foreach (var a in World.world.units)
            {
                if (a == null || a.data == null) continue;
                aliveIds.Add(a.data.id);

                // 更新存活快照
                if (!_aliveSnapshot.ContainsKey(a.data.id) || Random.value < 0.1f)
                {
                    _aliveSnapshot[a.data.id] = CreateSnapshot(a);
                }
            }

            // 发现死亡单位
            var deadIds = new List<long>();
            foreach (var kv in _aliveSnapshot)
            {
                if (!aliveIds.Contains(kv.Key))
                {
                    deadIds.Add(kv.Key);
                }
            }

            // 记录死亡信息态
            foreach (var id in deadIds)
            {
                if (_aliveSnapshot.TryGetValue(id, out var record))
                {
                    record.diedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    record.worldAge = 0;
                    _deadStates.Insert(0, record);
                    if (_deadStates.Count > MaxDeadRecords)
                        _deadStates.RemoveAt(_deadStates.Count - 1);
                    Debug.Log($"[超神机械师] 信息态记录: {record.name} (阶位{record.rankIndex}, 气力{record.qi:F0})");
                }
                _aliveSnapshot.Remove(id);
            }
        }

        /// <summary>创建单位信息态快照（原著：信息态完整记录生命体的一切数据）
        /// </summary>
        private static InformationStateRecord CreateSnapshot(Actor a)
        {
            var record = new InformationStateRecord
            {
                actorId = a.data.id,
                name = a.name ?? "Unknown",
                species = a.asset?.id ?? "",
                stage = SuperMechStage.GetStage(a),
                rankIndex = SuperMechAdvancement.GetRankIndex(a),
                qi = SuperMechQi.GetQi(a),
                qiLevel = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a) > 0 ? SuperMechQi.GetQiMax(a) : SuperMechQi.GetQi(a)),
                qiLayer = SuperMechQiLayer.GetLayer(a),
                energyLevel = SuperMechAdvancement.CalcOnar(a),
                potential = SuperMechPotential.GetPotential(a),
                reviveCount = SuperMechSanctuary.GetReviveCount(a),
                iterationId = SuperMechCosmicIteration.CurrentIteration,
                // v0.38.2 完整信息态
                divinityLevel = SuperMechDivinity.GetTotalLayers(a),
                divinityAwakened = SuperMechDivinity.IsDivineAwakened(a),
                legacyPower = SuperMechTranscendence.GetLegacyPower(a),
                advancementTaskDone = SuperMechTranscendence.IsAdvancementTaskDone(a),
                advancementProgress = SuperMechTranscendence.GetAdvancementProgress(a),
                subLevel = 0,
                deathAge = a.data.age,
            };

            // 职业和分支
            if (a.hasTrait(SuperMechTraits.ClassMech)) record.classTrait = "mech";
            else if (a.hasTrait(SuperMechTraits.ClassMartial)) record.classTrait = "martial";
            else if (a.hasTrait(SuperMechTraits.ClassPsi)) record.classTrait = "psi";
            else if (a.hasTrait(SuperMechTraits.ClassMage)) record.classTrait = "mage";
            else if (a.hasTrait(SuperMechTraits.ClassMind)) record.classTrait = "mind";

            // 分支特质
            record.branchTrait = SuperMechBranch.GetBranchTrait(a);

            // 知识
            var unlockedKnowledge = SuperMechKnowledge.GetUnlockedList(a, "mech");
            record.knowledge = new List<string>();
            if (unlockedKnowledge != null)
            {
                foreach (var k in unlockedKnowledge) record.knowledge.Add(k.id);
            }

            // 遗力来源
            var legacySources = SuperMechTranscendence.GetLegacySources(a);
            record.legacySourceNames = new List<string>();
            record.legacySourceRanks = new List<int>();
            record.legacySourceDamages = new List<float>();
            record.legacyDeathTypes = new List<string>();
            if (legacySources != null)
            {
                foreach (var s in legacySources)
                {
                    record.legacySourceNames.Add(s.sourceName ?? "");
                    record.legacySourceRanks.Add(s.sourceRank);
                    record.legacySourceDamages.Add(s.deathDamage);
                    record.legacyDeathTypes.Add(s.deathType ?? "other");
                }
            }

            // 技能
            record.skills = new List<string>();
            var learnedSkills = SuperMechSkills.GetLearned(a);
            if (learnedSkills != null)
            {
                foreach (var s in learnedSkills) record.skills.Add(s.id);
            }

            // 特质（自定义特质）
            record.traits = new List<string>();
            if (a.data.saved_traits != null)
            {
                foreach (var t in a.data.saved_traits)
                {
                    if (!string.IsNullOrEmpty(t) && t.StartsWith("sm_")) record.traits.Add(t);
                }
            }

            // 圣所权限总和
            int auth = 0;
            for (int i = 0; i < 6; i++) auth += SuperMechSanctuary.GetAuthority(a, i);
            record.sanctuaryAuthority = auth;

            return record;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _aliveSnapshot.Clear();
            _deadStates.Clear();
            _scanTimer = 0f;
        }
    }
}
