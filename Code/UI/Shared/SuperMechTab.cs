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
                bg.GetComponent<Image>().color = new Color(0.22f, 0.19f, 0.15f, 0.95f); // 原版深棕面板

                // ═══ 左侧竖向标签栏（原版风格：更深的棕色侧栏）═══
                var tabBarGo = new GameObject("TabBar", typeof(RectTransform), typeof(Image));
                tabBarGo.transform.SetParent(content, false);
                var tbr = tabBarGo.GetComponent<RectTransform>();
                tbr.anchorMin = new Vector2(0, 0); tbr.anchorMax = new Vector2(0, 1);
                tbr.pivot = new Vector2(0, 1);
                tbr.anchoredPosition = Vector2.zero;
                tbr.sizeDelta = new Vector2(110, 0);  // 宽度110，全高
                var tImg = tabBarGo.GetComponent<Image>();
                tImg.color = new Color(0.16f, 0.13f, 0.10f, 0.95f); // 原版深棕侧栏
                tImg.raycastTarget = true;

                var tabs = SuperMechWindowManager.OverviewTabs;
                float tabH = 36f;
                float gapY = 2f;
                float startX = 4f;

                for (int i = 0; i < tabs.Length; i++)
                {
                    int idx = i;
                    string label = LocalizedTextManager.getText(tabs[i].key);
                    bool active = idx == _tabIndex;
                    var b = MakeTabButton(tabBarGo.transform, label, active);
                    var br = b.GetComponent<RectTransform>();

                    // 竖向排列：每个标签固定高度，从顶部往下排
                    br.anchorMin = new Vector2(0, 1);
                    br.anchorMax = new Vector2(1, 1);
                    br.pivot = new Vector2(0.5f, 1f);
                    br.offsetMin = new Vector2(startX, -(startX + (i + 1) * (tabH + gapY)));
                    br.offsetMax = new Vector2(-startX, -(startX + i * (tabH + gapY)));

                    var btn = b.GetComponent<Button>();
                    btn.onClick.AddListener(() => SelectTab(idx));
                }

                // ═══ 内容区（右侧，左侧留110给标签栏）═══
                var areaContentGo = new GameObject("ContentArea", typeof(RectTransform));
                areaContentGo.transform.SetParent(content, false);
                var acr = areaContentGo.GetComponent<RectTransform>();
                acr.anchorMin = Vector2.zero; acr.anchorMax = Vector2.one;
                acr.pivot = new Vector2(0.5f, 0.5f);
                acr.offsetMin = new Vector2(116, 6);   // 左侧留116给标签栏(110+6)
                acr.offsetMax = new Vector2(-6, -6);   // v0.225 顶部不再留状态栏

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

        /// <summary>v0.233 原版风格竖向标签按钮：未选中=透明底+棕灰字；选中=金色半透明底+白字+左侧金色条</summary>
        private static GameObject MakeTabButton(Transform parent, string text, bool active)
        {
            var go = new GameObject("TabBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = active ? new Color(0.75f, 0.60f, 0.35f, 0.25f) : new Color(0, 0, 0, 0);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = active ? new Color(0.75f, 0.60f, 0.35f, 0.25f) : new Color(0, 0, 0, 0);
            colors.highlightedColor = new Color(0.75f, 0.60f, 0.35f, 0.15f);
            colors.pressedColor = new Color(0.75f, 0.60f, 0.35f, 0.35f);
            colors.selectedColor = new Color(0.75f, 0.60f, 0.35f, 0.25f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var label = SuperMechUiSkin.MakeText(go.transform, text, 13, TextAnchor.MiddleCenter);
            label.color = active ? new Color(0.95f, 0.90f, 0.75f) : new Color(0.65f, 0.58f, 0.45f);
            var lr = label.GetComponent<RectTransform>();
            lr.offsetMin = new Vector2(6, 0);
            lr.offsetMax = new Vector2(-6, 0);

            if (active)
            {
                var barGo = new GameObject("ActiveBar", typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(go.transform, false);
                var bar = barGo.GetComponent<Image>();
                bar.color = new Color(0.85f, 0.70f, 0.40f, 1f); // 原版金色
                var barRect = barGo.GetComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0, 0.2f); barRect.anchorMax = new Vector2(0, 0.8f);
                barRect.pivot = new Vector2(0, 0.5f);
                barRect.anchoredPosition = Vector2.zero;
                barRect.sizeDelta = new Vector2(3, 0);
            }
            return go;
        }
    }
}
