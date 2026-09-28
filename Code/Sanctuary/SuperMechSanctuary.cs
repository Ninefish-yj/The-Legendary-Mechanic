using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuperMech.Code
{


    public static partial class SuperMechSanctuary
    {
        public static readonly string[] SanctuaryNames = {
            "sm_sanctuary_958", "sm_sanctuary_959", "sm_sanctuary_960",
            "sm_sanctuary_961", "sm_sanctuary_962", "sm_sanctuary_963"
        };
        public static readonly string[] SanctuaryClasses = {
            "sm_sanctuary_964", "sm_sanctuary_965", "sm_sanctuary_966", "sm_sanctuary_967", "sm_sanctuary_968", "sm_sanctuary_969"
        };

        public const int TotalSanctuaries = 6;
        public const float DivinityOnarThreshold = 78000f;
        public const int DivinityQiLevel = 21;
        public const int FragmentsToUnlock = 3;

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "sm_sanctuary_970");

        public static SanctuaryData Data = new SanctuaryData();
        private static readonly HashSet<long> _divinityTriggered = new HashSet<long>();

        public class SanctuaryData
        {
            public int unlocked_sanctuaries = 0;
            public int key_fragments = 0;
            public int[] sanctuary_fragments = new int[6];
            public int total_permission = 0;
            public int total_visits = 0;
            public bool message_board_unlocked = false;
            public int total_divinity_ascensions = 0;
            public int total_resurrections = 0;
        }

        public class DeadUnitRecord
        {
            public string name;
            public string classTrait;
            public string branchTrait;
            public int stage;
            public int rankIndex;
            public float qi;
            public string qiAttribute;
            public long diedAt;
            public int reviveCount;
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
        public const int MaxDeadRecords = 20;
        public const int ResurrectionCost = 5;
        public const int DivineRankIndex = 13;

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
            if (!SuperMechConfig.SanctuaryAutoSave) return;
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
            int knowledgeGain = Mathf.Clamp(authority + 1, 1, 20);
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
                if (sanctuaryIndex < 0) return;
            }

            switch (sanctuaryIndex)
            {
                case 0:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.03f;
                        s["attack_speed"] = ((s["attack_speed"] == 0f ? 1f : s["attack_speed"])) * 1.05f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_964")
                        SuperMechQi.AddQiMax(a, 200f + visits * 20f);
                    break;

                case 1:
                    SuperMechQi.AddQiMax(a, 500f + visits * 50f);
                    if (s != null)
                    {
                        s["strength"] = (s["strength"]) + 3f + visits;
                        s["endurance"] = (s["endurance"]) + 3f + visits;
                    }
                    break;

                case 2:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.02f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_966")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 3:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["mana"] = (s["mana"]) + 50f + visits * 5f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_967")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 4:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["mana"] = (s["mana"]) + 30f + visits * 3f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_968")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 5:
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

        public static void Register()
        {
            Load();

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
                LocalizedTextManager.getText("sm_sanctuary_975"), pReplace: true);
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
