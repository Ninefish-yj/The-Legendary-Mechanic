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
                // 深色底
                var bg = new GameObject("OverviewBg", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(content, false);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = SuperMechUiSkin.SciBg;

                // 滚动视口
                var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
                viewGo.transform.SetParent(content, false);
                var vr = viewGo.GetComponent<RectTransform>();
                vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
                vr.offsetMin = new Vector2(10, 10);
                vr.offsetMax = new Vector2(-10, -10);
                var vimg = viewGo.GetComponent<Image>();
                vimg.color = new Color(0.04f, 0.06f, 0.10f, 0.5f);
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
                scroll.scrollSensitivity = 24f;

                const float colW = 313f;
                const float gap = 10f;
                const float cardH = 46f;
                const float cardGap = 10f;
                float y = 0f;

                // 组标题（金色小标题 + 细分隔线）
                void GroupTitle(string key, Color accent)
                {
                    var t = SuperMechUiSkin.MakeText(contentGo.transform, LocalizedTextManager.getText(key), 13, TextAnchor.MiddleLeft);
                    t.color = SuperMechUiSkin.SciGold;
                    var rt = t.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = new Vector2(6, y);
                    rt.sizeDelta = new Vector2(-12, 26);
                    y -= 30f;

                    var line = new GameObject("GroupLine", typeof(RectTransform), typeof(Image));
                    line.transform.SetParent(contentGo.transform, false);
                    var lr = line.GetComponent<RectTransform>();
                    lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
                    lr.pivot = new Vector2(0.5f, 1);
                    lr.anchoredPosition = new Vector2(0, y + 6f);
                    lr.sizeDelta = new Vector2(0, 1);
                    line.GetComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.55f);
                    y -= 8f;
                }

                // 入口卡片（2列网格）
                void Entry(string nameKey, System.Action open, Color accent)
                {
                    _entryCol ^= 1;
                    float x = _entryCol == 0 ? 0f : colW + gap;
                    var b = SuperMechUiSkin.MakeSciCardButton(contentGo.transform, LocalizedTextManager.getText(nameKey), accent, open);
                    var br = b.GetComponent<RectTransform>();
                    br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(0, 1);
                    br.pivot = new Vector2(0.5f, 1);
                    br.anchoredPosition = new Vector2(x + colW / 2f, y);
                    br.sizeDelta = new Vector2(colW, cardH);
                    if (_entryCol == 1) y -= cardH + cardGap;
                }

                // 成长（青）
                GroupTitle("sm_ui_ov_growth", SuperMechUiSkin.SciCyan);
                _entryCol = 1; // 预置为"下一行完成"，使第一个入口落在左列
                Entry("sm_ui_knowledge", () => SuperMechWindowManager.OpenKnowledge(), SuperMechUiSkin.SciCyan);
                Entry("sm_ui_bag", () => SuperMechWindowManager.OpenBag(), SuperMechUiSkin.SciCyan);
                Entry("sm_ui_spell_entry", () => SuperMechWindowManager.OpenSpell(), SuperMechUiSkin.SciCyan);
                Entry("sm_ui_craft", () => SuperMechWindowManager.OpenCraft(), SuperMechUiSkin.SciCyan);
                Entry("sm_ui_rank_window_title", () => SuperMechWindowManager.OpenRank(), SuperMechUiSkin.SciCyan);
                y -= cardH + cardGap; // 奇数个入口：补完末行
                y -= 4f;

                // 战斗（红）
                GroupTitle("sm_ui_ov_war", SuperMechUiSkin.SciDanger);
                _entryCol = 1;
                Entry("sm_ui_combat_title", () => SuperMechWindowManager.OpenCombatEnhance(), SuperMechUiSkin.SciDanger);
                Entry("sm_ui_beast_title", () => SuperMechWindowManager.OpenCosmicBeast(), SuperMechUiSkin.SciDanger);
                y -= 4f;

                // 世界（紫）
                GroupTitle("sm_ui_ov_world", SuperMechUiSkin.SciPurple);
                _entryCol = 1;
                Entry("sm_ui_trade_title", () => SuperMechWindowManager.OpenTrade(), SuperMechUiSkin.SciPurple);
                Entry("sm_ui_genetics_title", () => SuperMechWindowManager.OpenGenetics(), SuperMechUiSkin.SciPurple);
                Entry("sm_ui_stargate_title", () => SuperMechWindowManager.OpenStarGate(), SuperMechUiSkin.SciPurple);
                Entry("sm_ui_intel_title", () => SuperMechWindowManager.OpenIntel(), SuperMechUiSkin.SciPurple);
                Entry("sm_ui_faction_window_title", () => SuperMechWindowManager.OpenFaction(), SuperMechUiSkin.SciPurple);
                y -= cardH + cardGap; // 奇数个入口：补完末行
                y -= 4f;

                // 终局（金）
                GroupTitle("sm_ui_ov_finale", SuperMechUiSkin.SciGold);
                _entryCol = 1;
                Entry("sm_ui_sanctuary", () => SuperMechWindowManager.OpenSanctuary(), SuperMechUiSkin.SciGold);
                Entry("sm_ui_player_title", () => SuperMechWindowManager.OpenPlayer(), SuperMechUiSkin.SciGold);
                Entry("sm_ui_legion_title", () => SuperMechWindowManager.OpenLegion(), SuperMechUiSkin.SciGold);
                Entry("sm_ui_tree_title", () => SuperMechWindowManager.OpenWorldTree(), SuperMechUiSkin.SciGold);
                Entry("sm_ui_esgod_title", () => SuperMechWindowManager.OpenEsGod(), SuperMechUiSkin.SciGold);
                Entry("sm_ui_forge_title", () => SuperMechWindowManager.OpenForge(), SuperMechUiSkin.SciGold);
                y -= 12f;

                // 内容高度：3行(成长)+1行(战斗)+3行(世界)+3行(终局)=10行 × (cardH+cardGap) + 4组标题×38 + 组间距
                float h = -y;
                cr.sizeDelta = new Vector2(0, h);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 星海总览内容构建失败: " + e);
            }
        }

        private static int _entryCol;
    }
}
