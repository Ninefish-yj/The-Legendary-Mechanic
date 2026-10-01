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

        public static void Register()
        {
            AddAwakenedPower(SummonAwakened, "sm_powers_922", "actor_traits/iconChosenOne");
            AddDisaster(DisasterAlien, "sm_powers_924");
            AddSanctuaryPower(OpenSanctuary, "sm_sanctuary_974", "actor_traits/iconBlessing");
            AddWindowPower(OpenRank, "sm_rank_title", "iconDivineLight", () => SMRankWindowImgui.Toggle());
            Debug.Log("[超神机械师] 神权注册完成：1召唤 + 1天灾 + 1圣所 + 1排行榜 = 4个核心神权");
        }

        private static void AddAwakenedPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = icon,
                rank = PowerRank.Rank0_free, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = false
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
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
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
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                SMSanctuaryWindowImgui.Toggle();
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
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
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
