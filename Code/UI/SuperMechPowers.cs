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
                // 用原版图标加载方式
                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight");
                _modTab = TabManager.CreateTab(TabName, "sm_tab_title", "sm_tab_desc", tabIcon);
                if (_modTab == null)
                {
                    Debug.LogWarning("[超神机械师] 创建Tab失败，稍后重试");
                    return;
                }
                // NML标准流程：SetLayout → AddPowerButton → UpdateLayout
                _modTab.SetLayout(new System.Collections.Generic.List<string> { "main" });
                Debug.Log("[超神机械师] 独立Tab创建成功");
            }

            int created = 0;
            foreach (string id in PowerIds)
            {
                if (PowerButton.get(id) != null) continue;
                GodPower godPower = AssetManager.powers.get(id);
                if (godPower == null) continue;

                try
                {
                    // 用原版GodPower的图标加载方式
                    Sprite icon = godPower.getIconSprite();
                    if (icon == null)
                    {
                        icon = SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    }
                    var pb = PowerButtonCreator.CreateGodPowerButton(id, icon);
                    if (pb != null)
                    {
                        // 窗口按钮：Window类型，点击直接开窗（由Harmony补丁SMPowerButtonWindowPatch处理）
                        if (id == OpenSanctuary || id == OpenRank)
                        {
                            pb.type = PowerButtonType.Window;
                        }
                        _modTab.AddPowerButton("main", pb);
                        created++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[超神机械师] 创建按钮{id}失败: {e.Message}");
                }
            }

            if (created > 0)
            {
                _modTab.UpdateLayout();
            }

            _buttonsCreated = true;
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
