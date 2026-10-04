using System;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.14: 参考诸天神座模组——使用 NML 原生 TabManager 页签聚合全部窗口入口。
    /// v0.75.26: 改名「星海总览」（Tab=模组全系统入口聚合，个人成长+世界+终局全景；"超能者面板"名实不符——含个人信息面板含义）。神权栏不再堆按钮：主界面右下角新增该 Tab，Tab 内为滚动分组面板。
    /// </summary>
    public static class SuperMechTab
    {
        private static bool _initialized;
        private static PowersTab _tab;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconPlanet")
                              ?? Resources.Load<Sprite>("ui/icons/iconGift")
                              ?? Resources.Load<Sprite>("ui/icons/iconQuestion");
                _tab = TabManager.CreateTab("supermech", "sm_tab_panel_title", "sm_tab_panel_desc", icon);
                if (_tab == null)
                {
                    Debug.LogWarning("[超神机械师] 超神Tab创建失败（TabManager返回null）");
                    return;
                }
                BuildContent();
                Debug.Log("[超神机械师] 超神Tab初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 超神Tab初始化失败: " + e);
            }
        }

        private static void BuildContent()
        {
            var panel = new GameObject("SuperMechPanel", typeof(RectTransform));
            panel.transform.SetParent(_tab.transform, false);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(4, 4);
            pr.offsetMax = new Vector2(-4, -4);

            // 标题
            var title = SuperMechUiSkin.MakeText(panel.transform, LocalizedTextManager.getText("sm_tab_panel_title"), 14, TextAnchor.MiddleLeft);
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0.5f, 1);
            tr.anchoredPosition = new Vector2(0, -4);
            tr.sizeDelta = new Vector2(0, 22);

            // 内容区（ScrollRect，容纳17个入口+4组标题）
            var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewGo.transform.SetParent(panel.transform, false);
            var vr = viewGo.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0, 0); vr.anchorMax = new Vector2(1, 1);
            vr.offsetMin = new Vector2(2, 2);
            vr.offsetMax = new Vector2(-2, -28);
            var vimg = viewGo.GetComponent<Image>();
            vimg.color = new Color(0.06f, 0.09f, 0.14f, 0.85f);
            vimg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewGo.transform, false);
            var cr = contentGo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(0, 0);

            var scroll = viewGo.GetComponent<ScrollRect>();
            scroll.content = cr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            float y = 0f;
            void GroupTitle(string key)
            {
                var t = SuperMechUiSkin.MakeText(contentGo.transform, LocalizedTextManager.getText(key), 12, TextAnchor.MiddleLeft);
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(8, y);
                rt.sizeDelta = new Vector2(-16, 22);
                y -= 26f;
            }

            void Entry(string nameKey, System.Action open)
            {
                var b = SuperMechUiSkin.MakeButton(contentGo.transform, LocalizedTextManager.getText(nameKey), 12, open);
                var br = b.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(1, 1);
                br.pivot = new Vector2(0.5f, 1);
                br.anchoredPosition = new Vector2(4, y);
                br.sizeDelta = new Vector2(-8, 28);
                y -= 34f;
            }

            GroupTitle("sm_ui_ov_growth");
            Entry("sm_ui_knowledge", () => SuperMechWindowManager.OpenKnowledge());
            Entry("sm_ui_bag", () => SuperMechWindowManager.OpenBag());
            Entry("sm_ui_spell_entry", () => SuperMechWindowManager.OpenSpell());
            Entry("sm_ui_craft", () => SuperMechWindowManager.OpenCraft());
            Entry("sm_ui_rank_window_title", () => SuperMechWindowManager.OpenRank());

            GroupTitle("sm_ui_ov_war");
            Entry("sm_ui_combat_title", () => SuperMechWindowManager.OpenCombatEnhance());
            Entry("sm_ui_beast_title", () => SuperMechWindowManager.OpenCosmicBeast());

            GroupTitle("sm_ui_ov_world");
            Entry("sm_ui_trade_title", () => SuperMechWindowManager.OpenTrade());
            Entry("sm_ui_genetics_title", () => SuperMechWindowManager.OpenGenetics());
            Entry("sm_ui_stargate_title", () => SuperMechWindowManager.OpenStarGate());
            Entry("sm_ui_intel_title", () => SuperMechWindowManager.OpenIntel());
            Entry("sm_ui_faction_window_title", () => SuperMechWindowManager.OpenFaction());

            GroupTitle("sm_ui_ov_finale");
            Entry("sm_ui_sanctuary", () => SuperMechWindowManager.OpenSanctuary());
            Entry("sm_ui_player_title", () => SuperMechWindowManager.OpenPlayer());
            Entry("sm_ui_legion_title", () => SuperMechWindowManager.OpenLegion());
            Entry("sm_ui_tree_title", () => SuperMechWindowManager.OpenWorldTree());
            Entry("sm_ui_esgod_title", () => SuperMechWindowManager.OpenEsGod());
            Entry("sm_ui_forge_title", () => SuperMechWindowManager.OpenForge());

            // 内容高度 = 分组标题5×26 + 按钮18×34 + 底部留白
            float h = 5 * 26f + 18 * 34f + 12f;
            cr.sizeDelta = new Vector2(0, h);
        }
    }
}
