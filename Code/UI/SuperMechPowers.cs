using NeoModLoader.api;
using NeoModLoader.services;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPowers
    {
        public const string SummonAwakened = "sm_summon_awakened";
        public const string DisasterAlien = "sm_disaster_alien";
        public const string OpenSanctuary = "sm_open_sanctuary";
        public const string OpenRank = "sm_open_rank";

        public const string TabName = "SuperMech";
        private static bool _buttonsCreated;
        private static PowersTab _modTab;
        private static readonly string[] PowerIds = { SummonAwakened, DisasterAlien, OpenSanctuary, OpenRank };

        public static void Register()
        {
            AddAwakenedPower(SummonAwakened, "sm_powers_922", "actor_traits/iconChosenOne");
            AddDisaster(DisasterAlien, "sm_powers_924");
            AddSanctuaryPower(OpenSanctuary, "sm_sanctuary_974", "actor_traits/iconBlessing");
            AddWindowPower(OpenRank, "sm_rank_title", "iconDivineLight", () => SMWindowManager.OpenRank());
        }

        public static void TryCreateButtons()
        {
            if (_buttonsCreated) return;
            if (PowerButtonSelector.instance == null) return;

            // 创建独立的"超神机械师"Tab
            if (_modTab == null)
            {
                Sprite tabIcon = Resources.Load<Sprite>("iconDivineLight");
                _modTab = TabManager.CreateTab(TabName, "sm_tab_title", "sm_tab_desc", tabIcon);
                if (_modTab == null)
                {
                    Debug.LogWarning("[超神机械师] 创建Tab失败，稍后重试");
                    return;
                }
                Debug.Log("[超神机械师] 独立Tab创建成功");
            }

            int created = 0;
            foreach (string id in PowerIds)
            {
                if (PowerButton.get(id) != null) continue;
                GodPower godPower = AssetManager.powers.get(id);
                if (godPower == null) continue;

                Sprite icon = Resources.Load<Sprite>(godPower.path_icon);
                var pb = PowerButtonCreator.CreateGodPowerButton(id, icon, _modTab.transform);
                if (pb != null)
                {
                    // 手动加入Tab的内部按钮列表
                    AddButtonToTab(_modTab, pb);
                    created++;
                }
            }

            if (created > 0 && _modTab != null)
            {
                try { _modTab.findNeighbours(); }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] findNeighbours失败: {e.Message}"); }
            }

            // 运行时断言：验证所有按钮都在Tab列表中
            try { VerifyButtonsInTab(); }
            catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 按钮验证失败: {e.Message}"); }

            _buttonsCreated = true;
        }

        /// <summary>
        /// 运行时断言：验证所有按钮都正确加入Tab的_power_buttons列表
        /// </summary>
        private static void VerifyButtonsInTab()
        {
            if (_modTab == null) return;
            var field = typeof(PowersTab).GetField("_power_buttons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var list = field.GetValue(_modTab) as System.Collections.Generic.List<PowerButton>;
            if (list == null) return;

            foreach (string id in PowerIds)
            {
                var pb = PowerButton.get(id);
                if (pb == null)
                {
                    Debug.LogError($"[超神机械师] 运行时断言失败：按钮{id}未创建！");
                    continue;
                }
                if (!list.Contains(pb))
                {
                    Debug.LogError($"[超神机械师] 运行时断言失败：按钮{id}未加入Tab列表！");
                }
            }
        }

        /// <summary>
        /// 手动把按钮加入PowersTab的内部列表（_power_buttons是private，Start后不会自动收集）
        /// </summary>
        private static void AddButtonToTab(PowersTab tab, PowerButton button)
        {
            var field = typeof(PowersTab).GetField("_power_buttons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var list = field.GetValue(tab) as System.Collections.Generic.List<PowerButton>;
            if (list != null && !list.Contains(button))
            {
                list.Add(button);
            }
        }

        private static void AddAwakenedPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = icon,
                rank = PowerRank.Rank1_common, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    SuperMechTalent.GrantTalents(a);
                    SuperMechProfession.SetProfession(a, SuperMechProfession.ProfessionType.Mechanical);
                    a.addTrait(SuperMechAwakened.AwakenedTrait);
                    if (!a.hasTrait("sm_rank_00_f"))
                        a.addTrait("sm_rank_00_f");
                    SuperMechAdvancement.SetExactRank(a, 0);
                    SuperMechSpecialty.AssignRandomSpecialty(a);
                    Debug.Log($"[超神机械师] 召唤降临者：{a.name}（机械系Lv1）");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddDisaster(string id, string name)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = "iconDiscord",
                rank = PowerRank.Rank1_common,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit("beast", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    a.addTrait("sm_rank_06_b");
                    a.addTrait("aggressive");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddSanctuaryPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = icon,
                rank = PowerRank.Rank1_common,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                SMWindowManager.OpenSanctuary();
                return true;
            };
            AssetManager.powers.add(p);
            LocalizedTextManager.add("power_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("power_" + id + "_desc",
                LocalizedTextManager.getText("sm_sanctuary_975"), pReplace: true);
        }

        private static void AddWindowPower(string id, string name, string icon, System.Action onOpen)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = icon,
                rank = PowerRank.Rank1_common,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                onOpen?.Invoke();
                return true;
            };
            AssetManager.powers.add(p);
            LocalizedTextManager.add("power_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("power_" + id + "_desc",
                LocalizedTextManager.getText("sm_rank_subtitle"), pReplace: true);
        }
    }
}
