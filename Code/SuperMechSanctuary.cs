using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所系统（原著 ch1039/ch1050/ch1107/ch1121/ch1137）。
    /// 共6个圣所，本质是技能碎片收集系统，不是地点。
    /// - 第一圣所：机械系神性蜕变获得碎片（ch1039）
    /// - 第三圣所：异能系解锁，原始异能体是钥匙（ch1050）
    /// - 神性蜕变门槛：78000欧纳 + Lv21气力（ch1039）
    /// - 跨过机械系门槛获得第一圣所碎片（ch1121）
    /// - 跨存档机制：数据存在模组目录下（不进世界存档），新档继承。
    /// </summary>
    public static class SuperMechSanctuary
    {
        // 六圣所名称与对应系（原著 ch1039/ch1050/ch1362）：
        // 第一圣所=机械系(机械技术) 第二圣所=武道系 第三圣所=异能系(基因技术)
        // 第四圣所=魔法系 第五圣所=念力系 第六圣所=信息态技术(克制世界树，最难获取)
        public static readonly string[] SanctuaryNames = {
            "第一圣所·机械", "第二圣所·武道", "第三圣所·异能",
            "第四圣所·魔法", "第五圣所·念力", "第六圣所·信息态"
        };
        public static readonly string[] SanctuaryClasses = {
            "机械系", "武道系", "异能系", "魔法系", "念力系", "信息态"
        };

        public const int TotalSanctuaries = 6;
        public const float DivinityOnarThreshold = 78000f;  // 神性蜕变欧纳门槛（ch1039）
        public const int DivinityQiLevel = 21;               // 神性蜕变气力门槛（ch1039）
        public const int FragmentsToUnlock = 3;              // 集齐3碎片解锁圣所

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "../Mods/超神机械师/sanctuary_data.json");

        public static SanctuaryData Data = new SanctuaryData();
        // 已触发神性蜕变的单位（避免重复触发）
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

        /// <summary>死者数据备份（用于圣所复活，ch1134：圣所功能之一是复苏死者）。</summary>
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
        }

        // 可复活的死者列表（最近死亡的超能者）
        private static readonly List<DeadUnitRecord> _deadUnits = new List<DeadUnitRecord>();
        private static readonly Dictionary<long, DeadUnitRecord> _aliveSnapshot = new Dictionary<long, DeadUnitRecord>();
        // X阶（超神级）信息态重生追踪
        private static readonly Dictionary<long, DeadUnitRecord> _divineSnapshot = new Dictionary<long, DeadUnitRecord>();
        private static readonly HashSet<long> _divineCooldown = new HashSet<long>();
        public const int MaxDeadRecords = 20;       // 最多保留20个死者
        public const int ResurrectionCost = 5;      // 复活消耗钥匙碎片
        public const int DivineRankIndex = 13;      // X阶（超神级）索引
        // 每个圣所都有复活权限，只是知识分区不同（原著ch1134：圣所功能之一是复苏死者）
        // 圣所只复活超A级（S阶），原文ch1214："超A级层次理解为复活许可证"
        // 超神级（X阶）不靠圣所，靠信息态重生（ch1450：树神的信息态重生机制）

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

        /// <summary>
        /// 每tick检测神性蜕变（ch1039）：
        /// 单位达到78000欧纳 + Lv21气力时触发，获得对应圣所碎片。
        /// 机械系→第一圣所碎片，异能系→第三圣所碎片。
        /// </summary>
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

        /// <summary>触发神性蜕变，给圣所碎片和属性蜕变。</summary>
        private static void TriggerDivinity(Actor a)
        {
            _divinityTriggered.Add(a.id);
            Data.total_divinity_ascensions++;

            // 确定圣所类型：五系各对应一个圣所（原著 ch1362）
            // 第一=机械 第二=武道 第三=异能 第四=魔法 第五=念力
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
                Debug.Log($"[超神机械师] {a.Name} 神性蜕变！获得{sname}技能碎片（{Data.sanctuary_fragments[sanctuaryIndex]}/{FragmentsToUnlock}）");

                // 集齐碎片解锁圣所
                if (Data.sanctuary_fragments[sanctuaryIndex] >= FragmentsToUnlock
                    && (Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    Data.unlocked_sanctuaries |= (1 << sanctuaryIndex);
                    Data.total_permission++;
                    Debug.Log($"[超神机械师] {sname}已解锁！权限Lv{Data.total_permission}");

                    // 解锁前五圣所后，第六圣所（信息态）钥匙碎片出现（原著ch1362：最难获取）
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
                Debug.Log($"[超神机械师] {a.Name} 神性蜕变！获得圣所钥匙碎片（{Data.key_fragments}）");
            }

            // 神性蜕变属性大爆发（原著：神性蜕变是质变）
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                stats["multiplier_damage"] = (stats["multiplier_damage"] ?? 1f) * 1.5f;
                stats["multiplier_health"] = (stats["multiplier_health"] ?? 1f) * 1.5f;
                stats["intelligence"] = (stats["intelligence"] ?? 0f) + 20f;
            }
            // 给神性蜕变特质
            a.addTrait("sm_divinity_ascended");

            Save();
        }

        /// <summary>进入圣所：消耗钥匙碎片，获得该圣所的知识（潜能点），数量由权限等级决定。</summary>
        public static void EnterSanctuary(Actor a)
        {
            if (Data.key_fragments < 3)
            {
                Debug.Log("[超神机械师] 圣所钥匙碎片不足（需3）");
                return;
            }
            Data.key_fragments -= 3;
            Data.total_visits++;

            if (Data.total_permission >= 3 && !Data.message_board_unlocked)
            {
                Data.message_board_unlocked = true;
                Debug.Log("[超神机械师] 文明留言板已解锁！");
            }

            // 获得该圣所对应系的知识（潜能点），数量 = 权限等级
            int knowledgeGain = Mathf.Max(1, Data.total_permission);
            SuperMechPotential.AddPotential(a, knowledgeGain);

            // 小幅属性buff（圣所环境加持）
            float buff = 1f + Data.total_permission * 0.02f;
            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = (s["multiplier_damage"] ?? 1f) * buff;
                s["multiplier_health"] = (s["multiplier_health"] ?? 1f) * buff;
            }
            Save();

            string className = GetClassTrait(a) ?? "未知";
            Debug.Log($"[超神机械师] {a.Name} 进入圣所！获得{knowledgeGain}点知识（潜能点），权限Lv{Data.total_permission}，系别={className}");
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

        // ========== 圣所复活（ch1134：圣所功能之一是复苏死者） ==========

        /// <summary>定期追踪死者：S阶进圣所复活列表，X阶触发信息态重生。</summary>
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
                    name = a.Name ?? "未知",
                    classTrait = GetClassTrait(a),
                    branchTrait = SuperMechBranch.GetBranchTrait(a),
                    stage = SuperMechStage.GetStage(a),
                    rankIndex = rankIdx,
                    qi = SuperMechQi.GetQi(a),
                    qiAttribute = SuperMechQiAttribute.GetAttribute(a),
                    diedAt = 0
                };

                if (rankIdx >= 10 && rankIdx < DivineRankIndex)
                {
                    // S/S+/SS阶：圣所可复活
                    _aliveSnapshot[a.id] = rec;
                }
                else if (rankIdx >= DivineRankIndex)
                {
                    // X阶（超神级）：信息态重生追踪
                    _divineSnapshot[a.id] = rec;
                }
            }

            // S阶死者 → 圣所复活列表
            var deadIds = new List<long>();
            foreach (var kv in _aliveSnapshot)
                if (!aliveIds.Contains(kv.Key)) deadIds.Add(kv.Key);
            foreach (long id in deadIds)
            {
                var rec = _aliveSnapshot[id];
                rec.diedAt = System.DateTime.Now.Ticks;
                _deadUnits.Insert(0, rec);
                _aliveSnapshot.Remove(id);
                if (_deadUnits.Count > MaxDeadRecords) _deadUnits.RemoveAt(_deadUnits.Count - 1);
                Debug.Log($"[超神机械师] {rec.name}（S阶）已死亡，圣所记录可复活");
            }

            // X阶死者 → 信息态重生
            var divineDead = new List<long>();
            foreach (var kv in _divineSnapshot)
                if (!aliveIds.Contains(kv.Key)) divineDead.Add(kv.Key);
            foreach (long id in divineDead)
            {
                var rec = _divineSnapshot[id];
                _divineSnapshot.Remove(id);
                if (_divineCooldown.Contains(id)) continue;
                // 信息态重生：在随机位置重新生成，保留全部能力
                DivineRebirth(rec);
                _divineCooldown.Add(id);
            }
        }

        /// <summary>超神级信息态重生（ch1450：在其他地方重新生成）。</summary>
        private static void DivineRebirth(DeadUnitRecord rec)
        {
            try
            {
                // 找随机可走地块
                WorldTile tile = null;
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    int rx = UnityEngine.Random.Range(5, World.world.world.width - 5);
                    int ry = UnityEngine.Random.Range(5, World.world.world.height - 5);
                    WorldTile t = World.world.world.GetTile(rx, ry);
                    if (t != null && t.Type != null && t.Type.ground) { tile = t; break; }
                }
                if (tile == null) return;

                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a == null) return;

                // 恢复全部数据（信息态重生不削弱）
                if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
                if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);
                if (rec.stage > 0) SuperMechStage.SetStage(a, rec.stage);
                if (rec.rankIndex >= 0 && rec.rankIndex < SuperMechRanks.All.Count)
                    a.addTrait(SuperMechRanks.All[rec.rankIndex].id);
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

        /// <summary>获取可复活列表。</summary>
        public static List<DeadUnitRecord> GetDeadList() => _deadUnits;

        /// <summary>是否可复活：任意圣所解锁 + 钥匙碎片足够（每个圣所都有复活权限，知识分区不同）。</summary>
        public static bool CanResurrect()
        {
            return Data.unlocked_sanctuaries != 0 && Data.key_fragments >= ResurrectionCost;
        }

        /// <summary>获取死者对应系的圣所索引（-1=无对应）。</summary>
        public static int GetSanctuaryForClass(string classTrait)
        {
            if (classTrait == SuperMechTraits.ClassMech) return 0;
            if (classTrait == SuperMechTraits.ClassMartial) return 1;
            if (classTrait == SuperMechTraits.ClassPsi) return 2;
            if (classTrait == SuperMechTraits.ClassMage) return 3;
            if (classTrait == SuperMechTraits.ClassMind) return 4;
            return -1;
        }

        /// <summary>复活指定索引的死者，在指定位置生成。对应系圣所已解锁则有额外加成。</summary>
        public static Actor Resurrect(int deadIndex, WorldTile tile)
        {
            if (!CanResurrect())
            {
                Debug.Log("[超神机械师] 无法复活：无圣所解锁或钥匙碎片不足");
                return null;
            }
            if (deadIndex < 0 || deadIndex >= _deadUnits.Count) return null;
            var rec = _deadUnits[deadIndex];

            // 消耗钥匙碎片
            Data.key_fragments -= ResurrectionCost;
            Data.total_resurrections++;
            _deadUnits.RemoveAt(deadIndex);

            // 在指定位置生成新单位
            Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (a == null) return null;

            // 恢复职业数据
            if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
            if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);
            if (rec.stage > 0) SuperMechStage.SetStage(a, rec.stage);
            if (rec.rankIndex >= 0 && rec.rankIndex < SuperMechRanks.All.Count)
            {
                string rankId = SuperMechRanks.All[rec.rankIndex].id;
                a.addTrait(rankId);
            }
            SuperMechQi.SetQi(a, rec.qi * 0.8f);  // 复活后气力削弱为80%
            if (!string.IsNullOrEmpty(rec.qiAttribute) && rec.qiAttribute != SuperMechQiAttribute.AttrNone)
                SuperMechQiAttribute.SetAttribute(a, rec.qiAttribute);

            // 复活后削弱（复苏者非完全体，原著ch1211：复苏者需重新适应）
            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = (s["multiplier_damage"] ?? 1f) * 0.8f;
                s["multiplier_health"] = (s["multiplier_health"] ?? 1f) * 0.8f;
            }

            Save();
            Debug.Log($"[超神机械师] 圣所复活：{rec.name}（消耗{ResurrectionCost}钥匙碎片，累计{Data.total_resurrections}次）");
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
                name = "进入圣所",
                path_icon = "ui/Icons/actor_traits/iconHardSkin",
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

            // 注册神性蜕变特质
            var divinityTrait = new ActorTrait
            {
                id = "sm_divinity_ascended",
                path_icon = "ui/Icons/actor_traits/iconHardSkin",
                group_id = "sm_sanctuary",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            divinityTrait.base_stats["multiplier_damage"] = 1.2f;
            divinityTrait.base_stats["multiplier_health"] = 1.2f;
            divinityTrait.base_stats["critical_chance"] = 0.05f;
            AssetManager.traits.add(divinityTrait);

            LocalizedTextManager.add("power_sm_enter_sanctuary", "进入圣所", pReplace: true);
            LocalizedTextManager.add("power_sm_enter_sanctuary_desc",
                $"消耗3块圣所钥匙碎片，进入圣所获得知识（潜能点），数量=权限等级。共{TotalSanctuaries}个圣所，神性蜕变（78000欧纳+Lv21气力）获得圣所碎片。", pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended", "神性蜕变", pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended_info", "超越超A级的质变，伤害+20%生命+20%暴击+5%", pReplace: true);

            Debug.Log("[超神机械师] 圣所系统注册完成（6圣所+神性蜕变检测）");
        }
    }
}
