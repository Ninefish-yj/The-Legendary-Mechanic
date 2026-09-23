using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所系统（原著：前宇宙文明诸星联留下的遗产，共六个，跨迭代/跨存档元层）。
    /// 起点问答原文："共六个，不存在于物质或次级维度"。
    /// 第三圣所 ch1145/1253 坐实，第一圣所 ch1348 有技术资料。
    /// 跨存档机制：数据存在模组目录下（不进世界存档），新档继承。
    /// </summary>
    public static class SuperMechSanctuary
    {
        public const int TotalSanctuaries = 6;

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "../Mods/超神机械师/sanctuary_data.json");

        public static SanctuaryData Data = new SanctuaryData();

        public class SanctuaryData
        {
            public int unlocked_sanctuaries = 0;       // 已解锁圣所数量（共6个）
            public int key_fragments = 0;              // 圣所钥匙碎片
            public int total_permission = 0;           // 总权限等级（每进一个新圣所+1）
            public int total_visits = 0;                // 累计进入次数
            public bool message_board_unlocked = false; // 文明留言板（权限Lv3解锁）
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(DataPath))
                {
                    string json = File.ReadAllText(DataPath);
                    Data = JsonConvert.DeserializeObject<SanctuaryData>(json) ?? new SanctuaryData();
                }
                Debug.Log($"[超神机械师] 圣所数据加载：已解锁{Data.unlocked_sanctuaries}/{TotalSanctuaries}个圣所，碎片{Data.key_fragments}，权限Lv{Data.total_permission}");
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

        /// <summary>进入圣所：消耗碎片，获得跨存档buff。每进一个新圣所权限+1。</summary>
        public static void EnterSanctuary(Actor a)
        {
            if (Data.key_fragments < 3)
            {
                Debug.Log("[超神机械师] 圣所钥匙碎片不足（需3）");
                return;
            }
            Data.key_fragments -= 3;
            Data.total_visits++;

            // 每进一个新圣所，总权限+1（原著："每次进入不同圣所都能提升一次权限"）
            if (Data.unlocked_sanctuaries < TotalSanctuaries)
            {
                Data.unlocked_sanctuaries++;
                Data.total_permission++;
            }

            // 文明留言板：权限Lv3解锁
            if (Data.total_permission >= 3 && !Data.message_board_unlocked)
            {
                Data.message_board_unlocked = true;
                Debug.Log("[超神机械师] 文明留言板已解锁！");
            }

            // 跨存档buff：永久提升单位属性
            float buff = 1f + Data.total_permission * 0.05f;
            var s = SuperMechStats.Of(a);
            s["multiplier_damage"] *= buff;
            s["multiplier_health"] *= buff;
            Save();
            Debug.Log($"[超神机械师] 进入圣所！权限Lv{Data.total_permission}，已解锁{Data.unlocked_sanctuaries}/{TotalSanctuaries}圣所");
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

            LocalizedTextManager.add("power_sm_enter_sanctuary", "进入圣所", pReplace: true);
            LocalizedTextManager.add("power_sm_enter_sanctuary_desc",
                $"消耗3块圣所钥匙碎片，进入圣所。共{TotalSanctuaries}个圣所，每进新圣所权限+1，跨存档继承。", pReplace: true);

            Debug.Log("[超神机械师] 圣所系统注册完成（6个圣所）");
        }
    }
}
