using System;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.14: 参考诸天神座模组——NML 原生 TabManager 页签聚合思路。
    /// v0.75.28: 星海总览重设计为冷色科幻大面板（参考诸天神座 palette）。
    /// v0.75.29: 按诸天神座"每个页签=一个完整功能面板"模式改造——
    ///           星海总览=工作台：顶部横向页签栏（17 个系统页签），内容区内嵌对应系统完整面板
    ///           （复用 SuperMechWindowManager.DrawXXXContent，不再"入口列表→二级窗口"）。
    ///           圣所页签特殊：点击直接打开圣所独立空间视图。
    /// </summary>
    public static class SuperMechTab
    {
        private static int _tabIndex;
        private static RectTransform _rootContent;

        /// <summary>在窗口内容区绘制星海总览工作台（页签栏 + 内容区）</summary>
        public static void DrawOverviewContent(RectTransform content)
        {
            if (content == null) return;
            try
            {
                _rootContent = content;

                // 深色底
                var bg = new GameObject("OverviewBg", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(content, false);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = SuperMechUiSkin.SciBg;

                // ═══ 顶部页签栏（横向滚动）═══
                var tabScrollGo = new GameObject("TabScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                tabScrollGo.transform.SetParent(content, false);
                var tsr = tabScrollGo.GetComponent<RectTransform>();
                tsr.anchorMin = new Vector2(0, 1); tsr.anchorMax = new Vector2(1, 1);
                tsr.pivot = new Vector2(0.5f, 1);
                tsr.anchoredPosition = Vector2.zero;
                tsr.sizeDelta = new Vector2(0, 44);
                var tImg = tabScrollGo.GetComponent<Image>();
                tImg.color = new Color(0.04f, 0.06f, 0.10f, 0.9f);
                tImg.raycastTarget = true;

                var tabContentGo = new GameObject("TabContent", typeof(RectTransform));
                tabContentGo.transform.SetParent(tabScrollGo.transform, false);
                var tcr = tabContentGo.GetComponent<RectTransform>();
                tcr.anchorMin = new Vector2(0, 1); tcr.anchorMax = new Vector2(0, 1);
                tcr.pivot = new Vector2(0, 1);
                tcr.anchoredPosition = Vector2.zero;
                tcr.sizeDelta = new Vector2(0, 44);

                var tScroll = tabScrollGo.GetComponent<ScrollRect>();
                tScroll.content = tcr;
                tScroll.horizontal = true;
                tScroll.vertical = false;
                tScroll.movementType = ScrollRect.MovementType.Elastic;
                tScroll.scrollSensitivity = 30f;

                var tabs = SuperMechWindowManager.OverviewTabs;
                float tx = 6f;
                for (int i = 0; i < tabs.Length; i++)
                {
                    int idx = i;
                    string label = LocalizedTextManager.getText(tabs[i].key);
                    bool active = idx == _tabIndex;
                    var b = MakeTabButton(tabContentGo.transform, label, active);
                    var br = b.GetComponent<RectTransform>();
                    br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(0, 1);
                    br.pivot = new Vector2(0, 0.5f);
                    br.anchoredPosition = new Vector2(tx, 0);
                    float w = 24f + label.Length * 13f + 20f;
                    br.sizeDelta = new Vector2(w, 36);
                    var btn = b.GetComponent<Button>();
                    btn.onClick.AddListener(() => SelectTab(idx));
                    tx += w + 3f;
                }
                tcr.sizeDelta = new Vector2(tx + 6f, 0);

                // ═══ 内容区（全拉伸，各页签自己创建滚动视图）═══
                var areaContentGo = new GameObject("ContentArea", typeof(RectTransform));
                areaContentGo.transform.SetParent(content, false);
                var acr = areaContentGo.GetComponent<RectTransform>();
                acr.anchorMin = Vector2.zero; acr.anchorMax = Vector2.one;
                acr.pivot = new Vector2(0.5f, 0.5f);
                acr.offsetMin = new Vector2(6, 6);
                acr.offsetMax = new Vector2(-6, -50);

                DrawCurrent(acr);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 星海总览工作台构建失败: " + e);
            }
        }

        /// <summary>页签切换：圣所开独立空间，其余内嵌对应系统面板</summary>
        private static void SelectTab(int idx)
        {
            if (_rootContent == null) return;
            var tabs = SuperMechWindowManager.OverviewTabs;
            if (idx < 0 || idx >= tabs.Length) return;
            if (tabs[idx].draw == null)
            {
                // 圣所：打开独立圣所空间视图
                SuperMechWindowManager.OpenSanctuary();
                return;
            }
            _tabIndex = idx;
            try
            {
                // 重建工作台（销毁全部子节点后重绘）
                var children = new System.Collections.Generic.List<GameObject>();
                foreach (Transform c in _rootContent) children.Add(c.gameObject);
                foreach (var go in children) UnityEngine.Object.DestroyImmediate(go);
                DrawOverviewContent(_rootContent);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 页签切换失败: " + e);
            }
        }

        private static void DrawCurrent(RectTransform area)
        {
            var tabs = SuperMechWindowManager.OverviewTabs;
            if (_tabIndex >= 0 && _tabIndex < tabs.Length && tabs[_tabIndex].draw != null)
            {
                tabs[_tabIndex].draw(area);
            }
        }

        /// <summary>页签按钮：暗底+次要文字；选中=提亮底+金色文字+顶部青色条</summary>
        private static GameObject MakeTabButton(Transform parent, string text, bool active)
        {
            var go = new GameObject("TabBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = active ? SuperMechUiSkin.SciCardHover : SuperMechUiSkin.SciCard;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = active ? SuperMechUiSkin.SciCardHover : SuperMechUiSkin.SciCard;
            colors.highlightedColor = SuperMechUiSkin.SciCardHover;
            colors.pressedColor = SuperMechUiSkin.SciCardActive;
            colors.selectedColor = SuperMechUiSkin.SciCard;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var label = SuperMechUiSkin.MakeText(go.transform, text, 13, TextAnchor.MiddleCenter);
            label.color = active ? SuperMechUiSkin.SciGold : SuperMechUiSkin.SciMuted;
            var lr = label.GetComponent<RectTransform>();
            lr.offsetMin = new Vector2(4, 0);
            lr.offsetMax = new Vector2(-4, 0);

            if (active)
            {
                // 顶部选中条
                var barGo = new GameObject("ActiveBar", typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(go.transform, false);
                var bar = barGo.GetComponent<Image>();
                bar.color = SuperMechUiSkin.SciCyan;
                var barRect = barGo.GetComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0, 1); barRect.anchorMax = new Vector2(1, 1);
                barRect.pivot = new Vector2(0.5f, 1);
                barRect.anchoredPosition = Vector2.zero;
                barRect.sizeDelta = new Vector2(0, 3);
            }
            return go;
        }
    }
}
