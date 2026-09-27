using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuperMech.Code
{
    // 圣所系统：6圣所，权限=碎片不消耗，S阶复活信息丢失

    public static class SuperMechSanctuary
    {
        public static readonly string[] SanctuaryNames = {
            "sm_sanctuary_958", "sm_sanctuary_959", "sm_sanctuary_960",
            "sm_sanctuary_961", "sm_sanctuary_962", "sm_sanctuary_963"
        };
        public static readonly string[] SanctuaryClasses = {
            "sm_sanctuary_964", "sm_sanctuary_965", "sm_sanctuary_966", "sm_sanctuary_967", "sm_sanctuary_968", "sm_sanctuary_969"
        };

        public const int TotalSanctuaries = 6;
        public const float DivinityOnarThreshold = 78000f;  // 神性蜕变欧纳门槛（ch1039）
        public const int DivinityQiLevel = 21;               // 神性蜕变气力门槛（ch1039）
        public const int FragmentsToUnlock = 3;              // 集齐3碎片解锁圣所

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "sm_sanctuary_970");

        public static SanctuaryData Data = new SanctuaryData();
        private static readonly HashSet<long> _divinityTriggered = new HashSet<long>();

        public class SanctuaryData
        {
            public int unlocked_sanctuaries = 0;       // 已解锁圣所数量（共6个）
            public int key_fragments = 0;              // 通用圣所钥匙碎片
            public int[] sanctuary_fragments = new int[6];  // 各圣所碎片数 [0]=第一圣所...
            public int total_permission = 0;           // 总权限等级
            public int total_visits = 0;                // 累计进入次数
            public bool message_board_unlocked = false; // 文明留言板
            public int total_divinity_ascensions = 0;   // 累计神性蜕变次数
            public int total_resurrections = 0;         // 累计复活次数
        }

        public class DeadUnitRecord
        {
            public string name;
            public string classTrait;   // 五系觉醒特质id
            public string branchTrait;  // 分支特质id
            public int stage;           // 机械系职业阶段
            public int rankIndex;       // 阶位索引
            public float qi;            // 气力值
            public string qiAttribute;  // 气力属性
            public long diedAt;         // 死亡时间戳
            public int reviveCount;     // 累计复活次数（ch1214：次数越多信息丢失越严重）
        }

        private static readonly Dictionary<long, int> _reviveCount = new Dictionary<long, int>();
        private static readonly HashSet<long> _transcendenceFailed = new HashSet<long>();
        private static readonly Dictionary<long, int[]> _unitAuthority = new Dictionary<long, int[]>();

        public static int GetReviveCount(Actor a)
        {
            if (a == null) return 0;
            int v; _reviveCount.TryGetValue(a.id, out v); return v;
        }

        public static void SetReviveCount(Actor a, int count)
        {
            if (a == null) return;
            _reviveCount[a.id] = count;
        }

        public static int GetAuthority(Actor a, int sanctuaryIndex)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= 6) return 0;
            if (!_unitAuthority.TryGetValue(a.id, out var arr)) return 0;
            return arr[sanctuaryIndex];
        }

        public static int GetTotalAuthority(Actor a)
        {
            if (a == null) return 0;
            if (!_unitAuthority.TryGetValue(a.id, out var arr)) return 0;
            int total = 0;
            foreach (int v in arr) total += v;
            return total;
        }

        public static void AddAuthority(Actor a, int sanctuaryIndex, int amount)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= 6 || amount <= 0) return;
            if (!_unitAuthority.TryGetValue(a.id, out var arr))
            {
                arr = new int[6];
                _unitAuthority[a.id] = arr;
            }
            arr[sanctuaryIndex] += amount;
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                string[] statIds = {
                    SuperMechCustomStats.StatSanctuary1,
                    SuperMechCustomStats.StatSanctuary2,
                    SuperMechCustomStats.StatSanctuary3,
                    SuperMechCustomStats.StatSanctuary4,
                    SuperMechCustomStats.StatSanctuary5,
                    SuperMechCustomStats.StatSanctuary6
                };
                for (int i = 0; i < 6; i++)
                    stats[statIds[i]] = arr[i];
            }
        }

        public static void MarkTranscendenceFailed(Actor a)
        {
            if (a == null) return;
            _transcendenceFailed.Add(a.id);
        }

        private static readonly List<DeadUnitRecord> _deadUnits = new List<DeadUnitRecord>();
        private static readonly Dictionary<long, DeadUnitRecord> _aliveSnapshot = new Dictionary<long, DeadUnitRecord>();
        private static readonly Dictionary<long, DeadUnitRecord> _divineSnapshot = new Dictionary<long, DeadUnitRecord>();
        private static readonly HashSet<long> _divineCooldown = new HashSet<long>();
        public const int MaxDeadRecords = 20;       // 最多保留20个死者
        public const int ResurrectionCost = 5;      // 复活消耗钥匙碎片
        public const int DivineRankIndex = 13;      // X阶（超神级）索引

        public static void Load()
        {
            try
            {
                if (File.Exists(DataPath))
                {
                    string json = File.ReadAllText(DataPath);
                    Data = JsonConvert.DeserializeObject<SanctuaryData>(json) ?? new SanctuaryData();
                    if (Data.sanctuary_fragments == null || Data.sanctuary_fragments.Length < 6)
                        Data.sanctuary_fragments = new int[6];
                }
                Debug.Log($"[超神机械师] 圣所数据加载：已解锁{Data.unlocked_sanctuaries}/6，碎片[{string.Join(",", Data.sanctuary_fragments)}]，权限Lv{Data.total_permission}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 圣所数据读取失败: " + e.Message);
                Data = new SanctuaryData();
            }
        }

        public static void Save()
        {
            if (!SuperMechConfig.SanctuaryAutoSave) return; // 关闭自动保存时不写盘
            try
            {
                string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(DataPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 圣所数据保存失败: " + e.Message);
            }
        }

        public static void TickDivinity()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (_divinityTriggered.Contains(a.id)) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                float onar = SuperMechAdvancement.CalcOnar(a);
                float qi = SuperMechQi.GetQi(a);
                int qiLv = SuperMechQi.GetLevel(qi);

                if (onar >= DivinityOnarThreshold && qiLv >= DivinityQiLevel)
                {
                    TriggerDivinity(a);
                }
            }
        }

        private static void TriggerDivinity(Actor a)
        {
            _divinityTriggered.Add(a.id);
            Data.total_divinity_ascensions++;

            int sanctuaryIndex = -1;
            if (a.hasTrait(SuperMechTraits.ClassMech)) sanctuaryIndex = 0;
            else if (a.hasTrait(SuperMechTraits.ClassMartial)) sanctuaryIndex = 1;
            else if (a.hasTrait(SuperMechTraits.ClassPsi)) sanctuaryIndex = 2;
            else if (a.hasTrait(SuperMechTraits.ClassMage)) sanctuaryIndex = 3;
            else if (a.hasTrait(SuperMechTraits.ClassMind)) sanctuaryIndex = 4;

            if (sanctuaryIndex >= 0)
            {
                Data.sanctuary_fragments[sanctuaryIndex]++;
                string sname = SanctuaryNames[sanctuaryIndex];
                int personalFragments = Random.Range(1, 4); // 神性蜕变获得1-3个个人碎片
                AddAuthority(a, sanctuaryIndex, personalFragments);
                Debug.Log($"[超神机械师] {a.name} 神性蜕变！获得{sname}技能碎片（全局{Data.sanctuary_fragments[sanctuaryIndex]}/{FragmentsToUnlock}，个人权限+{personalFragments}）");

                if (Data.sanctuary_fragments[sanctuaryIndex] >= FragmentsToUnlock
                    && (Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    Data.unlocked_sanctuaries |= (1 << sanctuaryIndex);
                    Data.total_permission++;
                    Debug.Log($"[超神机械师] {sname}已解锁！权限Lv{Data.total_permission}");

                    if (CountUnlocked() >= 5 && (Data.unlocked_sanctuaries & (1 << 5)) == 0)
                    {
                        Data.sanctuary_fragments[5]++;
                        Debug.Log($"[超神机械师] 五圣所齐聚，第六圣所·信息态钥匙碎片出现！（{Data.sanctuary_fragments[5]}/{FragmentsToUnlock}）");
                        if (Data.sanctuary_fragments[5] >= FragmentsToUnlock)
                        {
                            Data.unlocked_sanctuaries |= (1 << 5);
                            Data.total_permission++;
                            Debug.Log($"[超神机械师] 第六圣所·信息态已解锁！全圣所齐聚！");
                        }
                    }
                }
            }
            else
            {
                Data.key_fragments++;
                Debug.Log($"[超神机械师] {a.name} 神性蜕变！获得圣所钥匙碎片（{Data.key_fragments}）");
            }

            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) * 1.5f;
                stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.5f;
                stats["intelligence"] = (stats["intelligence"]) + 20f;
            }
            a.addTrait("sm_divinity_ascended");

            Save();
        }

        public static bool EnterSanctuary(Actor a, int sanctuaryIndex = -1)
        {
            if (!SuperMechConfig.SanctuaryEnabled) return false;
            if (Data.key_fragments < 3)
            {
                Debug.Log("[超神机械师] 圣所钥匙碎片不足（需3）");
                return false;
            }
            if (sanctuaryIndex >= 0 && sanctuaryIndex < 6)
            {
                if ((Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    Debug.Log($"[超神机械师] 圣所{sanctuaryIndex + 1}未解锁");
                    return false;
                }
            }
            Data.key_fragments -= 3;
            Data.total_visits++;

            if (Data.total_visits >= 3 && !Data.message_board_unlocked)
            {
                Data.message_board_unlocked = true;
                Debug.Log("[超神机械师] 文明留言板已解锁！");
            }

            int authority = GetAuthority(a, sanctuaryIndex >= 0 ? sanctuaryIndex : 0);
            int knowledgeGain = Mathf.Clamp(authority + 1, 1, 20); // 权限+1，最多20点（记忆容量上限）
            SuperMechPotential.AddPotential(a, knowledgeGain);

            if (Random.value < 0.3f && sanctuaryIndex >= 0)
            {
                AddAuthority(a, sanctuaryIndex, 1);
                Debug.Log($"[超神机械师] {a.name} 在圣所中发现额外碎片！权限+1");
            }

            float buff = 1f + Data.total_visits * 0.02f;
            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * buff;
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * buff;
            }

            ApplySanctuaryClassBonus(a, s, sanctuaryIndex);

            if ((Data.unlocked_sanctuaries & (1 << 5)) != 0)
            {
                SuperMechInfoState.Upgrade(a);
            }

            Save();

            string className = GetClassTrait(a) ?? "sm_sanctuary_971";
            string sanctuaryName = sanctuaryIndex >= 0 ? $"sm_sanctuary_972" : "sm_sanctuary_973";
            Debug.Log($"[超神机械师] {a.name} 进入{sanctuaryName}！获得{knowledgeGain}点知识（潜能点），进入次数={Data.total_visits}（权限等级），系别={className}");
            return true;
        }

        private static void ApplySanctuaryClassBonus(Actor a, BaseStats s, int sanctuaryIndex)
        {
            if (a == null) return;
            int visits = Data.total_visits;

            if (sanctuaryIndex < 0 || sanctuaryIndex >= 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    if ((Data.unlocked_sanctuaries & (1 << i)) != 0)
                    {
                        sanctuaryIndex = i;
                        break;
                    }
                }
                if (sanctuaryIndex < 0) return; // 没有已解锁的圣所
            }

            switch (sanctuaryIndex)
            {
                case 0: // 第一圣所：机械技术传承（ch1039：泰尔克斯机械传承）
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.03f;
                        s["attack_speed"] = ((s["attack_speed"] == 0f ? 1f : s["attack_speed"])) * 1.05f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_964")
                        SuperMechQi.AddQiMax(a, 200f + visits * 20f);
                    break;

                case 1: // 第二圣所：武道功法传承（气力修炼法）
                    SuperMechQi.AddQiMax(a, 500f + visits * 50f);
                    if (s != null)
                    {
                        s["strength"] = (s["strength"]) + 3f + visits;
                        s["endurance"] = (s["endurance"]) + 3f + visits;
                    }
                    break;

                case 2: // 第三圣所：基因技术传承（ch1050：原始异能体是钥匙）
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.02f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_966")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 3: // 第四圣所：魔法知识传承
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["mana"] = (s["mana"]) + 50f + visits * 5f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_967")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 4: // 第五圣所：灵魂技术传承（精神力修炼）
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["mana"] = (s["mana"]) + 30f + visits * 3f; // 意志力用魔力模拟
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_968")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 5: // 第六圣所：信息态技术（ch1309：第六圣所=信息态技术）
                    SuperMechInfoState.Upgrade(a);
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 10f + visits * 2;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.05f;
                        s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.05f;
                    }
                    break;
            }
        }

        private static int CountUnlocked()
        {
            int count = 0;
            for (int i = 0; i < 6; i++)
                if ((Data.unlocked_sanctuaries & (1 << i)) != 0) count++;
            return count;
        }

        public static void GrantKeyFragment()
        {
            Data.key_fragments++;
            Save();
            Debug.Log($"[超神机械师] 获得圣所钥匙碎片（{Data.key_fragments}/3）");
        }


        public static void TickDeadTracking()
        {
            var alive = World.world.units.units_only_alive;
            if (alive == null) return;
            var aliveIds = new HashSet<long>();

            foreach (Actor a in alive)
            {
                if (a == null) continue;
                aliveIds.Add(a.id);
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                int rankIdx = SuperMechAdvancement.GetRankIndex(a);

                var rec = new DeadUnitRecord
                {
                    name = a.name ?? "sm_sanctuary_971",
                    classTrait = GetClassTrait(a),
                    branchTrait = SuperMechBranch.GetBranchTrait(a),
                    stage = SuperMechStage.GetStage(a),
                    rankIndex = rankIdx,
                    qi = SuperMechQi.GetQi(a),
                    qiAttribute = SuperMechQiAttribute.GetAttribute(a),
                    diedAt = 0,
                    reviveCount = GetReviveCount(a)
                };

                if (rankIdx >= 10 && rankIdx < DivineRankIndex)
                {
                    _aliveSnapshot[a.id] = rec;
                }
                else if (rankIdx >= DivineRankIndex)
                {
                    _divineSnapshot[a.id] = rec;
                }
            }

            var deadIds = new List<long>();
            foreach (var kv in _aliveSnapshot)
                if (!aliveIds.Contains(kv.Key)) deadIds.Add(kv.Key);
            foreach (long id in deadIds)
            {
                if (_transcendenceFailed.Contains(id))
                {
                    _aliveSnapshot.Remove(id);
                    _transcendenceFailed.Remove(id);
                    Debug.Log($"[超神机械师] {(_aliveSnapshot.ContainsKey(id) ? _aliveSnapshot[id].name : "sm_sanctuary_971")}（突破失败）化为超神遗力，无法圣所复苏");
                    continue;
                }
                var rec = _aliveSnapshot[id];
                rec.diedAt = System.DateTime.Now.Ticks;
                _deadUnits.Insert(0, rec);
                _aliveSnapshot.Remove(id);
                if (_deadUnits.Count > MaxDeadRecords) _deadUnits.RemoveAt(_deadUnits.Count - 1);
                Debug.Log($"[超神机械师] {rec.name}（S阶）已死亡，圣所记录可复活（已复活{rec.reviveCount}次）");
            }

            var divineDead = new List<long>();
            foreach (var kv in _divineSnapshot)
                if (!aliveIds.Contains(kv.Key)) divineDead.Add(kv.Key);
            foreach (long id in divineDead)
            {
                var rec = _divineSnapshot[id];
                _divineSnapshot.Remove(id);
                if (_divineCooldown.Contains(id)) continue;
                DivineRebirth(rec);
                _divineCooldown.Add(id);
            }
        }

        private static void DivineRebirth(DeadUnitRecord rec)
        {
            try
            {
                WorldTile tile = null;
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    int rx = UnityEngine.Random.Range(5, MapBox.width - 5);
                    int ry = UnityEngine.Random.Range(5, MapBox.height - 5);
                    WorldTile t = World.world.GetTile(rx, ry);
                    if (t != null && t.Type != null && t.Type.ground) { tile = t; break; }
                }
                if (tile == null) return;

                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a == null) return;

                if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
                if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);
                if (rec.stage > 0) SuperMechStage.SetStage(a, rec.stage);
                if (rec.rankIndex >= 0 && rec.rankIndex < SuperMechRanks.All.Count)
                    SuperMechAdvancement.SetExactRank(a, rec.rankIndex);
                SuperMechQi.SetQi(a, rec.qi);
                if (!string.IsNullOrEmpty(rec.qiAttribute) && rec.qiAttribute != SuperMechQiAttribute.AttrNone)
                    SuperMechQiAttribute.SetAttribute(a, rec.qiAttribute);
                a.addTrait("sm_divinity_ascended");

                Debug.Log($"[超神机械师] {rec.name}（超神级）信息态重生！在新位置重新生成");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 信息态重生异常: " + e.Message);
            }
        }

        public static List<DeadUnitRecord> GetDeadList() => _deadUnits;

        public static bool CanResurrect()
        {
            return Data.unlocked_sanctuaries != 0 && Data.key_fragments >= ResurrectionCost;
        }

        public static int GetSanctuaryForClass(string classTrait)
        {
            if (classTrait == SuperMechTraits.ClassMech) return 0;
            if (classTrait == SuperMechTraits.ClassMartial) return 1;
            if (classTrait == SuperMechTraits.ClassPsi) return 2;
            if (classTrait == SuperMechTraits.ClassMage) return 3;
            if (classTrait == SuperMechTraits.ClassMind) return 4;
            return -1;
        }

        public static Actor Resurrect(int deadIndex, WorldTile tile)
        {
            if (!CanResurrect())
            {
                Debug.Log("[超神机械师] 无法复活：无圣所解锁或钥匙碎片不足");
                return null;
            }
            if (deadIndex < 0 || deadIndex >= _deadUnits.Count) return null;
            var rec = _deadUnits[deadIndex];

            Data.key_fragments -= ResurrectionCost;
            Data.total_resurrections++;
            _deadUnits.RemoveAt(deadIndex);

            Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (a == null) return null;

            if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
            if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);

            int reviveCount = rec.reviveCount + 1;
            SetReviveCount(a, reviveCount);

            float infoLoss = Mathf.Clamp(0.1f * reviveCount, 0.1f, 0.5f); // 每次多丢10%，最多50%

            int finalRank = rec.rankIndex;
            int finalStage = rec.stage;
            bool rankDropped = false;

            if (Random.value < infoLoss)
            {
                if (finalRank > 0)
                {
                    finalRank--;
                    rankDropped = true;
                    finalStage = Mathf.Max(0, finalStage - Random.Range(1, 3));
                    Debug.Log($"[超神机械师] {rec.name} 复活降阶！{SuperMechRanks.GetRankName(rec.rankIndex)}→{SuperMechRanks.GetRankName(finalRank)}");
                }
            }

            if (finalStage > 0) SuperMechStage.SetStage(a, finalStage);
            if (finalRank >= 0 && finalRank < SuperMechRanks.All.Count)
                SuperMechAdvancement.SetExactRank(a, finalRank);

            SuperMechQi.SetQi(a, rec.qi * (1f - infoLoss));
            if (!string.IsNullOrEmpty(rec.qiAttribute) && rec.qiAttribute != SuperMechQiAttribute.AttrNone)
                SuperMechQiAttribute.SetAttribute(a, rec.qiAttribute);

            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * (1f - infoLoss);
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * (1f - infoLoss);
                s["intelligence"] = Mathf.Max(0f, ((s["intelligence"] == 0f ? 5f : s["intelligence"])) * (1f - infoLoss * 0.5f));
            }

            bool canResurrectAgain = finalRank >= 10; // S阶索引=10
            if (!canResurrectAgain)
            {
                Debug.Log($"[超神机械师] {rec.name} 已降到{SuperMechRanks.GetRankName(finalRank)}，失去圣所复活资格！");
            }

            Save();
            Debug.Log($"[超神机械师] 圣所复活：{rec.name}（第{reviveCount}次复活，信息丢失{infoLoss:P0}{(rankDropped ? "，降阶" : "")}{(canResurrectAgain ? "" : "，失去复活资格")}，消耗{ResurrectionCost}钥匙碎片）");
            return a;
        }

        private static string GetClassTrait(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return SuperMechTraits.ClassMech;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return SuperMechTraits.ClassMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return SuperMechTraits.ClassPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return SuperMechTraits.ClassMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return SuperMechTraits.ClassMind;
            return null;
        }

        public static void Register()
        {
            Load();
            var enterPower = new GodPower
            {
                id = "sm_enter_sanctuary",
                name = "sm_sanctuary_974",
                path_icon = "ui/Icons/actor_traits/iconBlessing",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            enterPower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { EnterSanctuary(a); });
                return true;
            };
            AssetManager.powers.add(enterPower);

            var divinityTrait = new ActorTrait
            {
                id = "sm_divinity_ascended",
                path_icon = "ui/Icons/actor_traits/iconBlessing",
                group_id = "sm_sanctuary",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            divinityTrait.base_stats["multiplier_damage"] = 1.2f;
            divinityTrait.base_stats["multiplier_health"] = 1.2f;
            divinityTrait.base_stats["critical_chance"] = 0.05f;
            AssetManager.traits.add(divinityTrait);

            LocalizedTextManager.add("power_sm_enter_sanctuary", LocalizedTextManager.getText("sm_sanctuary_974"), pReplace: true);
            LocalizedTextManager.add("power_sm_enter_sanctuary_desc",
                $"sm_sanctuary_975", pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended", LocalizedTextManager.getText("sm_sanctuary_976"), pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended_info", LocalizedTextManager.getText("sm_sanctuary_977"), pReplace: true);

            Debug.Log("[超神机械师] 圣所系统注册完成（6圣所+神性蜕变检测）");
        }

        public static void Clear()
        {
            _divinityTriggered.Clear();
            _reviveCount.Clear();
            _transcendenceFailed.Clear();
            _aliveSnapshot.Clear();
            _divineSnapshot.Clear();
            Data = new SanctuaryData();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_reviveCount, alive);
            removed += SuperMechCleanup.CleanDict(_aliveSnapshot, alive);
            return removed;
        }
    }
}
