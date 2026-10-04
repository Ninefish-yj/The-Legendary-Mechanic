using System;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.14: 参考诸天神座模组——使用 NML 原生 TabManager 页签聚合全部窗口入口。
    /// v0.75.26: 改名「星海总览」（Tab=模组全系统入口聚合，个人成长+世界+终局全景）。
    /// v0.75.27: 用户要求改回按钮形态——「星海总览」不再作为右下角页签，改为神权栏按钮
    ///           打开的总览窗口内容（17 个系统窗口入口 + 4 组标题，滚动面板）。
    ///           窗口标题栏由 SuperMechWindowManager.OpenOverview 传入（sm_tab_panel_title）。
    /// </summary>
    public static class SuperMechTab
    {
        /// <summary>在窗口内容区绘制 17 个系统窗口入口的滚动面板</summary>
        public static void DrawOverviewContent(RectTransform content)
        {
            if (content == null) return;
            try
            {
                var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
                viewGo.transform.SetParent(content, false);
                var vr = viewGo.GetComponent<RectTransform>();
                vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
                vr.offsetMin = Vector2.zero;
                vr.offsetMax = Vector2.zero;
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
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 星海总览内容构建失败: " + e);
            }
        }
    }
}
