using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPowers
    {
        public const string SummonAwakened = "sm_summon_awakened";
        public const string DisasterAlien = "sm_disaster_alien";
        public const string OpenSanctuary = "sm_open_sanctuary";
        public const string OpenRank = "sm_open_rank";

        private static bool _buttonsCreated;
        private static readonly string[] PowerIds = { SummonAwakened, DisasterAlien, OpenSanctuary, OpenRank };

        public static void Register()
        {
            AddAwakenedPower(SummonAwakened, "sm_powers_922", "actor_traits/iconChosenOne");
            AddDisaster(DisasterAlien, "sm_powers_924");
            AddSanctuaryPower(OpenSanctuary, "sm_sanctuary_974", "actor_traits/iconBlessing");
            AddWindowPower(OpenRank, "sm_rank_title", "iconDivineLight", () => SMWindowManager.OpenRank());
            Debug.Log("[超神机械师] 神权数据注册完成：4个GodPower");
        }

        public static void TryCreateButtons()
        {
            if (_buttonsCreated) return;
            if (PowerButtonSelector.instance == null) return;
            if (PowerButtonSelector.instance.buttons == null) return;

            PowerButton template = null;
            foreach (Transform child in PowerButtonSelector.instance.buttons.transform)
            {
                var pb = child.GetComponent<PowerButton>();
                if (pb != null && pb.type == PowerButtonType.Active)
                {
                    template = pb;
                    break;
                }
            }

            if (template == null)
            {
                foreach (Transform child in PowerButtonSelector.instance.buttons.transform)
                {
                    var pb = child.GetComponent<PowerButton>();
                    if (pb != null)
                    {
                        template = pb;
                        break;
                    }
                }
            }

            if (template == null)
            {
                Debug.LogWarning("[超神机械师] 未找到PowerButton模板，稍后重试");
                return;
            }

            var initField = typeof(PowerButton).GetField("_initialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var viewer = PowerButtonSelector.instance.buttons.GetComponent<ButtonsViewer>();
            var viewerButtonsField = viewer != null ? typeof(ButtonsViewer).GetField("buttons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
            var viewerButtons = viewerButtonsField?.GetValue(viewer) as System.Collections.IList;

            int created = 0;
            foreach (string id in PowerIds)
            {
                if (PowerButton.get(id) != null) continue;
                GodPower godPower = AssetManager.powers.get(id);
                if (godPower == null) continue;

                var go = Object.Instantiate(template.gameObject, PowerButtonSelector.instance.buttons.transform);
                go.name = id;
                var pb = go.GetComponent<PowerButton>();
                if (pb != null)
                {
                    pb.type = PowerButtonType.Active;
                    if (initField != null) initField.SetValue(pb, false);
                    go.SetActive(false);
                    go.SetActive(true);
                    if (viewerButtons != null && !viewerButtons.Contains(pb))
                        viewerButtons.Add(pb);
                    created++;
                }
            }

            if (created > 0)
            {
                var rt = PowerButtonSelector.instance.buttons.GetComponent<RectTransform>();
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            _buttonsCreated = true;
            Debug.Log($"[超神机械师] 神权按钮创建完成：{created}个新按钮");
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
