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
        }

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

            // 确定圣所类型：机械系→第一圣所(0)，异能系→第三圣所(2)，其他→通用碎片
            int sanctuaryIndex = -1;
            string sanctuaryName = "";
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                sanctuaryIndex = 0;  // 第一圣所
                sanctuaryName = "第一圣所";
            }
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                sanctuaryIndex = 2;  // 第三圣所
                sanctuaryName = "第三圣所";
            }

            if (sanctuaryIndex >= 0)
            {
                Data.sanctuary_fragments[sanctuaryIndex]++;
                Debug.Log($"[超神机械师] {a.Name} 神性蜕变！获得{sanctuaryName}技能碎片（{Data.sanctuary_fragments[sanctuaryIndex]}/{FragmentsToUnlock}）");

                // 集齐碎片解锁圣所
                if (Data.sanctuary_fragments[sanctuaryIndex] >= FragmentsToUnlock
                    && (Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    Data.unlocked_sanctuaries |= (1 << sanctuaryIndex);
                    Data.total_permission++;
                    Debug.Log($"[超神机械师] {sanctuaryName}已解锁！权限Lv{Data.total_permission}");
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

        /// <summary>进入圣所：消耗通用碎片，获得跨存档buff。</summary>
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

            float buff = 1f + Data.total_permission * 0.05f;
            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = (s["multiplier_damage"] ?? 1f) * buff;
                s["multiplier_health"] = (s["multiplier_health"] ?? 1f) * buff;
            }
            Save();
            Debug.Log($"[超神机械师] 进入圣所！权限Lv{Data.total_permission}，已解锁{CountUnlocked()}/6圣所");
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
                $"消耗3块圣所钥匙碎片，进入圣所。共{TotalSanctuaries}个圣所，神性蜕变（78000欧纳+Lv21气力）获得圣所碎片。", pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended", "神性蜕变", pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended_info", "超越超A级的质变，伤害+20%生命+20%暴击+5%", pReplace: true);

            Debug.Log("[超神机械师] 圣所系统注册完成（6圣所+神性蜕变检测）");
        }
    }
}
