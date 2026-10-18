using System;
using NeoModLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static class SuperMechUiBuilder
    {
        private static Font _font;
        private static Font GetFont()
        {
            if (_font == null)
            {
                // v0.75.14: 参考诸天神座——NML官方字体(current_font)优先，中文支持且永不为null
                _font = LocalizedTextManager.current_font;
                if (_font == null) _font = SuperMechUiSkin.DefaultFont;
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_font == null)
                {
                    // v0.75.11: 部分Unity版本无内建字体，动态创建系统字体兜底，避免AddText抛NRE导致窗口白板
                    try
                    {
                        _font = Font.CreateDynamicFontFromOSFont(
                            new[] { "Arial", "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "SimHei" }, 14);
                    }
                    catch (System.Exception e) { Debug.LogWarning("[超神机械师] 系统字体创建失败，使用默认字体: " + e.Message); }
                }
            }
            return _font;
        }

        public static GameObject CreateObject(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform AddRectTransform(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        public static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        public static void SetStretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image AddImage(GameObject go, Color color, Sprite sprite = null)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

                public static Text AddText(GameObject go, string text, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            // v0.75.12: 实机ObjectReferenceException定位——在已有go上AddComponent<Text>不稳定，
            // 改为与UiSkin.MakeText相同的"新建子对象挂Text"路径（实机已验证100%可行），并全链路判空防御
            if (go == null)
            {
                Debug.LogWarning("[超神机械师] AddText: go为null，跳过文本创建");
                return null;
            }
            Text txt = null;
            try
            {
                txt = go.GetComponent<Text>();
                if (txt == null)
                {
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(go.transform, false);
                    var rect = textGo.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    txt = textGo.AddComponent<Text>();
                }
                if (txt == null)
                {
                    Debug.LogWarning("[超神机械师] AddText: 创建Text组件失败 " + go.name);
                    return null;
                }
                txt.text = text ?? "";
                var f = GetFont(); if (f != null) txt.font = f; // 字体为null时用引擎默认，避免NRE白板
                txt.fontSize = fontSize;
                txt.alignment = anchor;
                txt.color = color ?? new Color(0.85f, 0.9f, 0.98f, 1f);
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                return txt;
            }
            catch (System.Exception e)
            {
                // 防御兜底：任何异常都不让窗口白板
                Debug.LogWarning("[超神机械师] AddText防御触发: " + (go != null ? go.name : "null") + " -> " + e.Message);
                return null;
            }
        }

public static Button AddButton(GameObject go, Action onClick, Color? normalColor = null, Color? hoverColor = null)
        {
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = normalColor ?? new Color(0.85f, 0.88f, 0.92f, 0.9f);
            colors.highlightedColor = hoverColor ?? new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.8f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static ScrollRect AddScrollView(GameObject go, out RectTransform content)
        {
            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 20f;

            var viewportGo = CreateObject("Viewport", go.transform, typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRt = AddRectTransform(viewportGo);
            SetStretch(viewportRt);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0);
            viewportImg.raycastTarget = false; // v0.194 修复：透明Image不拦截点击
            var mask = viewportGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = CreateObject("Content", viewportGo.transform, typeof(RectTransform));
            content = AddRectTransform(contentGo);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 200);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = content;
            return scroll;
        }

        public static GameObject CreatePanel(Transform parent, string name, Color bgColor, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            var go = CreateObject(name, parent, typeof(RectTransform), typeof(Image));
            var rt = AddRectTransform(go);
            SetStretch(rt, left, right, top, bottom);
            // v0.202 美化：使用圆角卡片皮肤
            var img = AddImage(go, Color.white);
            img.sprite = SuperMechUiSkin.Card;
            img.type = Image.Type.Sliced;
            return go;
        }

        public static GameObject CreateButton(Transform parent, string name, string text, Action onClick, float width = 80, float height = 28, int fontSize = 13)
        {
            var go = CreateObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = AddRectTransform(go);
            rt.sizeDelta = new Vector2(width, height);
            // v0.202 美化：使用新皮肤按钮三态
            var img = AddImage(go, Color.white);
            img.sprite = SuperMechUiSkin.ButtonNormalSprite;
            img.type = Image.Type.Sliced;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = SuperMechUiSkin.ButtonHoverSprite;
            ss.pressedSprite = SuperMechUiSkin.ButtonActiveSprite;
            btn.spriteState = ss;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txtGo = CreateObject("Text", go.transform, typeof(RectTransform));
            var txtRt = AddRectTransform(txtGo);
            SetStretch(txtRt);
            AddText(txtGo, text, fontSize, TextAnchor.MiddleCenter, SuperMechUiSkin.TextColor);
            return go;
        }

        public static GameObject CreateTextRow(Transform parent, string label, string value, float labelWidth = 120)
        {
            var go = CreateObject("Row", parent, typeof(RectTransform));
            var rt = AddRectTransform(go);
            rt.sizeDelta = new Vector2(0, 24);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var labelGo = CreateObject("Label", go.transform, typeof(RectTransform));
            var labelRt = AddRectTransform(labelGo);
            labelRt.sizeDelta = new Vector2(labelWidth, 0);
            AddText(labelGo, label, 12, TextAnchor.MiddleLeft, new Color(0.6f, 0.7f, 0.85f, 1f));

            var valueGo = CreateObject("Value", go.transform, typeof(RectTransform));
            AddText(valueGo, value, 12, TextAnchor.MiddleLeft, new Color(0.9f, 0.95f, 1f, 1f));

            return go;
        }

        // ================= v0.237 UI统一规范：共享组件 =================

        /// <summary>v0.237 标准分区标题（Section底纹+青色粗体，28px高）</summary>
        public static GameObject CreateSectionTitle(Transform parent, string text)
        {
            var go = CreateObject("SectionTitle", parent, typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.sprite = SuperMechUiSkin.Section;  // v0.240.9 原版输入框纹理
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
            var txt = AddText(go, "  " + text, 13, TextAnchor.MiddleLeft, SuperMechUiSkin.CyanColor);
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.minHeight = 28;
            layoutElem.preferredHeight = 28;
            return go;
        }

        /// <summary>v0.237 标准信息行（Card背景+灰色标签+值，28px高，值支持RichText）</summary>
        public static GameObject CreateInfoRow(Transform parent, string label, string value, float labelWidth = 100, Color? valueColor = null)
        {
            var go = CreateObject("InfoRow", parent, typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.sprite = SuperMechUiSkin.Card;  // v0.240.9 原版面板纹理
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;

            var hLayout = go.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 4, 4);
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            var labelGo = CreateObject("Label", go.transform, typeof(RectTransform));
            var labelTxt = AddText(labelGo, label, 12, TextAnchor.MiddleLeft, SuperMechUiSkin.TextDim);
            labelTxt.raycastTarget = false;
            var labelRect = AddRectTransform(labelGo);
            labelRect.sizeDelta = new Vector2(labelWidth, 24);

            var valueGo = CreateObject("Value", go.transform, typeof(RectTransform));
            var valueTxt = AddText(valueGo, value, 12, TextAnchor.MiddleLeft, valueColor ?? new Color(1f, 0.9f, 0.6f));
            valueTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueTxt.verticalOverflow = VerticalWrapMode.Truncate;
            valueTxt.raycastTarget = false;
            var valueLayout = valueGo.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1;

            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.minHeight = 28;
            return go;
        }

        /// <summary>v0.237 创建详情内容容器（VerticalLayoutGroup，供动态刷新的详情区复用）</summary>
        public static GameObject CreateInfoContainer(Transform parent, string name = "DetailContent")
        {
            var go = CreateObject(name, parent, typeof(RectTransform));
            var rt = AddRectTransform(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return go;
        }

        /// <summary>v0.237 清空容器所有子对象（详情区刷新前调用）</summary>
        public static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null) UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        /// <summary>v0.237 标准详情页签内容：分区标题+多行信息（label/value对）</summary>
        public static void FillInfoRows(RectTransform container, (string label, string value, Color? color)[] rows, params string[] sectionTitles)
        {
            int titleIdx = 0;
            foreach (var row in rows)
            {
                if (titleIdx < sectionTitles.Length && !string.IsNullOrEmpty(sectionTitles[titleIdx]))
                {
                    CreateSectionTitle(container, sectionTitles[titleIdx]);
                    titleIdx++;
                }
                CreateInfoRow(container, row.label, row.value, 100, row.color);
            }
        }

        /// <summary>v0.237 创建带滚动的内容区（ScrollRect+Content，供长内容面板复用）</summary>
        public static RectTransform CreateScrollContent(RectTransform parent, out ScrollRect scrollRect, string name = "ScrollContent")
        {
            var scrollGo = CreateObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            var scrollRt = AddRectTransform(scrollGo);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollImg.raycastTarget = false;
            scrollGo.GetComponent<Mask>().showMaskGraphic = true;
            scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewportGo = CreateObject("Viewport", scrollGo.transform, typeof(RectTransform));
            var viewportRt = AddRectTransform(viewportGo);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            var contentGo = CreateObject("Content", viewportGo.transform, typeof(RectTransform));
            var contentRt = AddRectTransform(contentGo);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = Vector2.zero;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            return contentRt;
        }

        /// <summary>v0.195 创建带页签的面板，返回内容区RectTransform（页签切换时自动重绘）</summary>
        public static RectTransform CreateTabbedPanel(RectTransform parent, string[] tabLabels, int defaultTab, Action<int, RectTransform> onDrawTab)
        {
            // 页签栏
            var tabBarGo = CreateObject("TabBar", parent, typeof(RectTransform), typeof(Image));
            var tabBarRt = AddRectTransform(tabBarGo);
            tabBarRt.anchorMin = new Vector2(0, 1);
            tabBarRt.anchorMax = new Vector2(1, 1);
            tabBarRt.pivot = new Vector2(0.5f, 1);
            tabBarRt.sizeDelta = new Vector2(0, 34);
            tabBarRt.anchoredPosition = Vector2.zero;
            var tabBarImg = tabBarGo.GetComponent<Image>();
            tabBarImg.color = new Color(0.04f, 0.06f, 0.10f, 0.9f);
            tabBarImg.raycastTarget = true;

            // 内容区
            var contentGo = CreateObject("TabContent", parent, typeof(RectTransform));
            var contentRt = AddRectTransform(contentGo);
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(4, 4);
            contentRt.offsetMax = new Vector2(-4, -38);

            int currentTab = defaultTab;
            var tabButtons = new System.Collections.Generic.List<Button>();

            // 创建页签按钮
            float tabW = 1f / tabLabels.Length;
            for (int i = 0; i < tabLabels.Length; i++)
            {
                int idx = i;
                var tabGo = CreateObject("Tab" + i, tabBarGo.transform, typeof(RectTransform), typeof(Image), typeof(Button));
                var tabRt = AddRectTransform(tabGo);
                tabRt.anchorMin = new Vector2(idx * tabW, 0);
                tabRt.anchorMax = new Vector2((idx + 1) * tabW, 1);
                tabRt.offsetMin = new Vector2(2, 2);
                tabRt.offsetMax = new Vector2(-2, -2);
                var tabImg = tabGo.GetComponent<Image>();
                tabImg.color = idx == currentTab ? new Color(0.15f, 0.25f, 0.4f, 0.95f) : new Color(0.08f, 0.12f, 0.18f, 0.8f);
                var tabBtn = tabGo.GetComponent<Button>();
                tabButtons.Add(tabBtn);
                AddText(tabGo, tabLabels[i], 12, TextAnchor.MiddleCenter,
                    idx == currentTab ? new Color(0.95f, 0.85f, 0.5f, 1f) : new Color(0.7f, 0.75f, 0.85f, 1f));

                tabBtn.onClick.AddListener(() =>
                {
                    currentTab = idx;
                    // 更新按钮样式
                    for (int j = 0; j < tabButtons.Count; j++)
                    {
                        var img = tabButtons[j].GetComponent<Image>();
                        img.color = j == idx ? new Color(0.15f, 0.25f, 0.4f, 0.95f) : new Color(0.08f, 0.12f, 0.18f, 0.8f);
                        var txt = tabButtons[j].GetComponentInChildren<Text>();
                        if (txt != null) txt.color = j == idx ? new Color(0.95f, 0.85f, 0.5f, 1f) : new Color(0.7f, 0.75f, 0.85f, 1f);
                    }
                    // 清除内容并重绘
                    foreach (Transform c in contentRt) UnityEngine.Object.Destroy(c.gameObject);
                    onDrawTab?.Invoke(idx, contentRt);
                });
            }

            // 初始绘制
            onDrawTab?.Invoke(currentTab, contentRt);
            return contentRt;
        }
    }
}
