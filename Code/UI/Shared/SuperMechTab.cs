using System;
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

                var bg = new GameObject("OverviewBg", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(content, false);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
                bg.GetComponent<Image>().color = SuperMechUiSkin.SciBg;

                // ═══ 顶部王国状态栏（v0.207：全局显示当前选中王国+快速切换）═══
                var statusBarGo = new GameObject("KingdomStatusBar", typeof(RectTransform), typeof(Image));
                statusBarGo.transform.SetParent(content, false);
                var sbr = statusBarGo.GetComponent<RectTransform>();
                sbr.anchorMin = new Vector2(0, 1); sbr.anchorMax = new Vector2(1, 1);
                sbr.pivot = new Vector2(0.5f, 1);
                sbr.anchoredPosition = Vector2.zero;
                sbr.sizeDelta = new Vector2(0, 26);
                var sbImg = statusBarGo.GetComponent<Image>();
                sbImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);

                // 上一个王国按钮
                var prevBtn = MakeSmallButton(statusBarGo.transform, "<", () => SwitchKingdom(-1));
                var prevRect = prevBtn.GetComponent<RectTransform>();
                prevRect.anchorMin = new Vector2(0, 0.5f); prevRect.anchorMax = new Vector2(0, 0.5f);
                prevRect.pivot = new Vector2(0, 0.5f);
                prevRect.sizeDelta = new Vector2(28, 20);
                prevRect.anchoredPosition = new Vector2(4, 0);

                // 王国名称显示（点击打开原版面板）
                var kingdomNameGo = new GameObject("KingdomName", typeof(RectTransform), typeof(Text));
                kingdomNameGo.transform.SetParent(statusBarGo.transform, false);
                var knr = kingdomNameGo.GetComponent<RectTransform>();
                knr.anchorMin = new Vector2(0, 0); knr.anchorMax = new Vector2(1, 1);
                knr.offsetMin = new Vector2(36, 0); knr.offsetMax = new Vector2(-36, 0);
                var knText = kingdomNameGo.GetComponent<Text>();
                knText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                knText.fontSize = 12;
                knText.color = new Color(1f, 0.85f, 0.4f);
                knText.alignment = TextAnchor.MiddleCenter;
                knText.text = GetKingdomStatusText();
                var knBtn = kingdomNameGo.AddComponent<Button>();
                knBtn.onClick.AddListener(() => OpenCurrentKingdomWindow());

                // 下一个王国按钮
                var nextBtn = MakeSmallButton(statusBarGo.transform, ">", () => SwitchKingdom(1));
                var nextRect = nextBtn.GetComponent<RectTransform>();
                nextRect.anchorMin = new Vector2(1, 0.5f); nextRect.anchorMax = new Vector2(1, 0.5f);
                nextRect.pivot = new Vector2(1, 0.5f);
                nextRect.sizeDelta = new Vector2(28, 20);
                nextRect.anchoredPosition = new Vector2(-4, 0);

                // ═══ 顶部页签栏（等宽铺满一排）═══
                var tabBarGo = new GameObject("TabBar", typeof(RectTransform), typeof(Image));
                tabBarGo.transform.SetParent(content, false);
                var tbr = tabBarGo.GetComponent<RectTransform>();
                tbr.anchorMin = new Vector2(0, 1); tbr.anchorMax = new Vector2(1, 1);
                tbr.pivot = new Vector2(0.5f, 1);
                tbr.anchoredPosition = new Vector2(0, -26);  // 状态栏下方
                tbr.sizeDelta = new Vector2(0, 40);  // 单排，紧凑
                var tImg = tabBarGo.GetComponent<Image>();
                tImg.color = new Color(0.04f, 0.06f, 0.10f, 0.9f);
                tImg.raycastTarget = true;

                var tabs = SuperMechWindowManager.OverviewTabs;
                int tabCount = tabs.Length;
                float tabH = 32f;
                float gapX = 4f;    // 横向间距
                float startY = -4f; // 顶部边距

                for (int i = 0; i < tabs.Length; i++)
                {
                    int idx = i;
                    string label = LocalizedTextManager.getText(tabs[i].key);
                    bool active = idx == _tabIndex;
                    var b = MakeTabButton(tabBarGo.transform, label, active);
                    var br = b.GetComponent<RectTransform>();

                    // 等宽铺满：每个标签宽度=1/tabCount
                    br.anchorMin = new Vector2((float)i / tabCount, 1f);
                    br.anchorMax = new Vector2((float)(i + 1) / tabCount, 1f);
                    br.pivot = new Vector2(0.5f, 1f);
                    br.offsetMin = new Vector2(gapX, -startY - tabH);
                    br.offsetMax = new Vector2(-gapX, -startY);

                    var btn = b.GetComponent<Button>();
                    btn.onClick.AddListener(() => SelectTab(idx));
                }

                // ═══ 内容区（全拉伸，各页签自己创建滚动视图）═══
                var areaContentGo = new GameObject("ContentArea", typeof(RectTransform));
                areaContentGo.transform.SetParent(content, false);
                var acr = areaContentGo.GetComponent<RectTransform>();
                acr.anchorMin = Vector2.zero; acr.anchorMax = Vector2.one;
                acr.pivot = new Vector2(0.5f, 0.5f);
                acr.offsetMin = new Vector2(6, 6);
                acr.offsetMax = new Vector2(-6, -72);  // 顶部留72给状态栏(26)+标签栏(40)+边距(6)

                DrawCurrent(acr);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 星海总览工作台构建失败: " + e);
            }
        }

        /// <summary>v0.208 页签切换：所有标签均为内嵌概览面板，直接重建工作台</summary>
        private static void SelectTab(int idx)
        {
            if (_rootContent == null) return;
            var tabs = SuperMechWindowManager.OverviewTabs;
            if (idx < 0 || idx >= tabs.Length) return;
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
            label.color = active ? SuperMechUiSkin.CyanColor : SuperMechUiSkin.SciMuted;
            var lr = label.GetComponent<RectTransform>();
            lr.offsetMin = new Vector2(4, 0);
            lr.offsetMax = new Vector2(-4, 0);

            if (active)
            {
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

        /// <summary>v0.207 小按钮（状态栏用）</summary>
        private static GameObject MakeSmallButton(Transform parent, string text, Action onClick)
        {
            var go = new GameObject("SmallBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.25f, 0.4f, 0.9f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            var label = SuperMechUiSkin.MakeText(go.transform, text, 12, TextAnchor.MiddleCenter);
            label.color = Color.white;
            var lr = label.GetComponent<RectTransform>();
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>v0.207 获取当前王国状态文本</summary>
        private static string GetKingdomStatusText()
        {
            if (World.world == null || World.world.kingdoms == null)
                return LocalizedTextManager.getText("sm_ui_kingdom_none");
            long id = SuperMechWindowManager.SelectedKingdomId;
            if (id < 0)
                return LocalizedTextManager.getText("sm_ui_economy_all_kingdoms");
            foreach (var k in World.world.kingdoms.list)
                if (k != null && k.getID() == id)
                    return k.data.name;
            return LocalizedTextManager.getText("sm_ui_economy_all_kingdoms");
        }

        /// <summary>v0.207 切换王国（direction: -1上一个, 1下一个）</summary>
        private static void SwitchKingdom(int direction)
        {
            if (World.world == null || World.world.kingdoms == null) return;
            var kingdoms = World.world.kingdoms.list;
            if (kingdoms.Count == 0) return;

            long curId = SuperMechWindowManager.SelectedKingdomId;
            int idx = 0;
            for (int i = 0; i < kingdoms.Count; i++)
                if (kingdoms[i] != null && kingdoms[i].getID() == curId) { idx = i; break; }
            idx = (idx + direction + kingdoms.Count) % kingdoms.Count;
            SuperMechWindowManager.SelectedKingdomId = kingdoms[idx].getID();

            // 重建工作台刷新显示
            if (_rootContent != null)
            {
                var children = new System.Collections.Generic.List<GameObject>();
                foreach (Transform c in _rootContent) children.Add(c.gameObject);
                foreach (var go in children) UnityEngine.Object.DestroyImmediate(go);
                DrawOverviewContent(_rootContent);
            }
        }

        /// <summary>v0.207 打开当前王国的原版信息面板</summary>
        private static void OpenCurrentKingdomWindow()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            long id = SuperMechWindowManager.SelectedKingdomId;
            foreach (var k in World.world.kingdoms.list)
                if (k != null && k.getID() == id)
                {
                    SelectedMetas.selected_kingdom = k;
                    ScrollWindow.showWindow("kingdom");
                    return;
                }
        }
    }
}
