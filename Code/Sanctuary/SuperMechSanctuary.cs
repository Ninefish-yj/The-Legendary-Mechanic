using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HarmonyLib;

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

        /// <summary>圣所类型（原著：五个超能职业分别对应一个圣所，第六圣所为信息态）</summary>
        public enum SanctuaryType
        {
            Mechanical = 0,    // 第一圣所：机械师（原著）
            Psi = 1,           // 第二圣所：念力师
            Biological = 2,    // 第三圣所：异能者（生物基因/异能，原著）
            Martial = 3,       // 第四圣所：武道家
            Mage = 4,          // 第五圣所：魔法师
            Information = 5    // 第六圣所：信息态（原著，额外）
        }

        /// <summary>各圣所类型（索引0~5对应第一~第六圣所）</summary>
        public static readonly SanctuaryType[] SanctuaryTypes =
        {
            SanctuaryType.Mechanical,    // 第一圣所：机械师
            SanctuaryType.Psi,           // 第二圣所：念力师
            SanctuaryType.Biological,    // 第三圣所：异能者
            SanctuaryType.Martial,       // 第四圣所：武道家
            SanctuaryType.Mage,          // 第五圣所：魔法师
            SanctuaryType.Information    // 第六圣所：信息态
        };

        /// <summary>圣所专属知识分支（访问时产出对应知识）
        /// 原著：五个超能职业分别对应一个圣所，第六圣所为信息态（额外）
        /// </summary>
        public static readonly string[][] SanctuaryKnowledgeBranches =
        {
            new[] { "mech_weapon", "mech_energy", "mech_control" },  // 第一圣所：机械师
            new[] { "psi_mind", "psi_kinesis", "psi_sense" },        // 第二圣所：念力师
            new[] { "bio_gene", "bio_ability", "bio_evolution" },    // 第三圣所：异能者（生物基因/异能）
            new[] { "martial_breath", "martial_flurry", "martial_body" }, // 第四圣所：武道家
            new[] { "spell_element", "spell_arcane", "spell_summon" }, // 第五圣所：魔法师
            new[] { "info_state", "info_virtual", "info_resurrect" } // 第六圣所：信息态
        };

        /// <summary>圣所类型名称本地化key</summary>
        public static readonly string[] SanctuaryTypeNames =
        {
            "sm_sanctuary_type_mech",
            "sm_sanctuary_type_psi",
            "sm_sanctuary_type_bio",
            "sm_sanctuary_type_martial",
            "sm_sanctuary_type_mage",
            "sm_sanctuary_type_info"
        };

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "sm_sanctuary_970");

        public static SanctuaryData Data = new SanctuaryData();
        private static readonly HashSet<long> _divinityTriggered = new HashSet<long>();

        public class SanctuaryData
        {
            public int unlocked_sanctuaries = 0;
            public int key_fragments = 0;       // v0.46.0：完整圣所钥匙（进入消耗）
            public int key_materials = 0;       // v0.46.0：钥匙材料（击杀高阶单位掉落，合成钥匙）
            public int[] sanctuary_fragments = new int[6];
            public int total_permission = 0;
            public int total_visits = 0;
            public bool message_board_unlocked = false;
            public int total_divinity_ascensions = 0;
            public int total_resurrections = 0;
            public float sanctuary_energy = 5000f;  // 圣所能量（复活媒介消耗）
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
            int cost = GetEnterCost();
            if (Data.key_fragments < cost)
            {
                Debug.Log($"[超神机械师] 圣所钥匙碎片不足（需{cost}，当前{Data.key_fragments}）");
                return false;
            }
            if (sanctuaryIndex >= 0 && sanctuaryIndex < 6)
            {
                if ((Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    return false;
                }
            }
            Data.key_fragments -= cost;
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

            string className = SuperMechProfession.GetClass(a) ?? "sm_sanctuary_971";
            string sanctuaryName = sanctuaryIndex >= 0 ? $"sm_sanctuary_972" : "sm_sanctuary_973";
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
        }

        // === v0.46.0 圣所钥匙机制（原著向）===
        // 钥匙（key_fragments）：进入圣所消耗，由钥匙材料合成
        // 钥匙材料（key_materials）：击杀S阶及以上超能者掉落
        // 权限/碎片（_unitAuthority / sanctuary_fragments）：进入圣所后积累，影响奖励，神性蜕变直接获得

        /// <summary>授予钥匙材料，达到合成阈值时自动合成钥匙（v0.46.0，原著：钥匙由稀有材料合成）</summary>
        public static void GrantKeyMaterials(int amount, string source)
        {
            if (!SuperMechConfig.SanctuaryKeyDropEnabled) return;
            if (amount <= 0) return;
            Data.key_materials += amount;
            int crafted = CraftKeys();
            Save();
            if (crafted > 0)
            {
                SMEventLogger.LogKeyAcquired(null, crafted, source + "合成");
                Debug.Log($"[超神机械师] 获得{amount}钥匙材料（来源：{source}），自动合成{crafted}把圣所钥匙，材料剩余{Data.key_materials}");
            }
            else
            {
                Debug.Log($"[超神机械师] 获得{amount}钥匙材料（来源：{source}），当前{Data.key_materials}/{SuperMechConfig.KeyMaterialsPerKey}");
            }
        }

        /// <summary>自动合成钥匙：材料达到阈值时转换为钥匙，返回合成数量</summary>
        public static int CraftKeys()
        {
            int perKey = Mathf.Max(1, SuperMechConfig.KeyMaterialsPerKey);
            int crafted = Data.key_materials / perKey;
            if (crafted > 0)
            {
                Data.key_materials -= crafted * perKey;
                Data.key_fragments += crafted;
            }
            return crafted;
        }

        /// <summary>神性蜕变获得圣所碎片（权限），随机分配到已解锁圣所（v0.46.0，原著第1039章：神性蜕变给圣所技能碎片）</summary>
        public static void GrantAuthorityFromDivinity(Actor a)
        {
            if (a == null) return;
            // 找已解锁的圣所，随机选一个+1权限
            var unlocked = new List<int>();
            for (int i = 0; i < 6; i++)
                if ((Data.unlocked_sanctuaries & (1 << i)) != 0)
                    unlocked.Add(i);
            int target = unlocked.Count > 0
                ? unlocked[UnityEngine.Random.Range(0, unlocked.Count)]
                : UnityEngine.Random.Range(0, 6);
            AddAuthority(a, target, 1);
            Data.sanctuary_fragments[target]++;
            Data.total_permission++;
            Save();
            Debug.Log($"[超神机械师] {a.name} 神性蜕变获得圣所碎片，{SanctuaryNames[target]}权限+1");
        }

        /// <summary>获取每次进入圣所消耗的钥匙数（v0.46.0：可配置）</summary>
        public static int GetEnterCost()
        {
            return Mathf.Max(1, SuperMechConfig.SanctuaryKeyCostEnter);
        }

        /// <summary>获取圣所类型</summary>
        public static SanctuaryType GetSanctuaryType(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return SanctuaryType.Mechanical;
            return SanctuaryTypes[sanctuaryIndex];
        }

        /// <summary>获取圣所类型名称（本地化）</summary>
        public static string GetSanctuaryTypeName(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return "";
            return LocalizedTextManager.getText(SanctuaryTypeNames[sanctuaryIndex]);
        }

        /// <summary>获取圣所专属知识分支列表</summary>
        public static string[] GetSanctuaryKnowledgeBranches(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return new string[0];
            return SanctuaryKnowledgeBranches[sanctuaryIndex];
        }

        /// <summary>访问圣所：根据圣所类型产出专属知识/权限（原著差异化）
        /// 第一圣所产出机械系知识，第三圣所产出生物基因知识，第六圣所产出信息态知识
        /// </summary>
        public static bool VisitSanctuary(Actor a, int sanctuaryIndex)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return false;
            if ((Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0) return false;

            // 增加访问次数和权限
            Data.total_visits++;
            AddAuthority(a, sanctuaryIndex, 10);

            // 根据圣所类型判断体系匹配（原著：五个超能职业分别对应一个圣所）
            var type = GetSanctuaryType(sanctuaryIndex);
            bool isMatchingClass = false;
            switch (type)
            {
                case SanctuaryType.Mechanical:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMech);
                    break;
                case SanctuaryType.Psi:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMind);
                    break;
                case SanctuaryType.Biological:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassPsi);
                    break;
                case SanctuaryType.Martial:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMartial);
                    break;
                case SanctuaryType.Mage:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMage);
                    break;
                case SanctuaryType.Information:
                    isMatchingClass = true; // 信息态圣所不限制职业
                    break;
            }

            // 匹配体系的单位访问获得额外权限加成
            if (isMatchingClass)
            {
                AddAuthority(a, sanctuaryIndex, 5);
                Debug.Log($"[超神机械师] {a.name} 访问第{sanctuaryIndex + 1}圣所（{GetSanctuaryTypeName(sanctuaryIndex)}），体系匹配，额外权限+5");
            }
            else
            {
                Debug.Log($"[超神机械师] {a.name} 访问第{sanctuaryIndex + 1}圣所（{GetSanctuaryTypeName(sanctuaryIndex)}）");
            }

            // v0.29.0 UI重构：推送圣所访问事件到原生事件日志
            string sanctuaryName = GetSanctuaryTypeName(sanctuaryIndex);
            SMEventLogger.LogSanctuaryVisit(a, sanctuaryIndex, sanctuaryName);

            Save();
            return true;
        }

        /// <summary>获取圣所时间比例（原著：权限越高，时间比例越趋近1:1）
        /// 基础比例1:10，每1000点权限提升10%，最高1:1
        /// </summary>
        public static float GetSanctuaryTimeRatio(Actor a, int sanctuaryIndex)
        {
            if (a == null) return 0.1f; // 基础1:10
            int authority = GetAuthority(a, sanctuaryIndex);
            float ratio = 0.1f + (authority / 1000f) * 0.1f;
            return Mathf.Clamp(ratio, 0.1f, 1.0f);
        }

        /// <summary>获取圣所时间比例描述（用于UI显示）</summary>
        public static string GetSanctuaryTimeRatioText(Actor a, int sanctuaryIndex)
        {
            float ratio = GetSanctuaryTimeRatio(a, sanctuaryIndex);
            int outsideTime = Mathf.RoundToInt(1f / ratio);
            string tTimeRatio = LocalizedTextManager.getText("sm_san_time_ratio");
            return string.Format(tTimeRatio, outsideTime);
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

    /// <summary>v0.46.0：高阶超能者死亡掉落圣所钥匙材料（原著：钥匙由稀有材料合成）</summary>
    [HarmonyPatch]
    public static class SanctuaryKeyDropPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), "die")]
        public static void DiePostfix(Actor __instance)
        {
            if (__instance == null || __instance.isAlive()) return;
            if (!SuperMechConfig.SanctuaryKeyDropEnabled) return;

            try
            {
                int rank = SuperMechAdvancement.GetExactRankIndex(__instance);
                if (rank < SuperMechConfig.KeyDropMinRank) return;

                // 按阶位掉落材料：S=1, S+=1, SS=2, X=3
                int amount = 1;
                if (rank >= 13) amount = 3;
                else if (rank >= 12) amount = 2;
                else if (rank >= 11) amount = 1;
                else amount = 1;

                string rankName = rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name)
                    : "?";
                SuperMechSanctuary.GrantKeyMaterials(amount, "击杀" + rankName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 钥匙材料掉落异常: {e.Message}");
            }
        }
    }
}
