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
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_font == null)
                {
                    // v0.75.11: 部分Unity版本无内建字体，动态创建系统字体兜底，避免AddText抛NRE导致窗口白板
                    try
                    {
                        _font = Font.CreateDynamicFontFromOSFont(
                            new[] { "Arial", "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "SimHei" }, 14);
                    }
                    catch (System.Exception) { }
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
                txt.color = color ?? new Color(0.1f, 0.15f, 0.25f, 1f);
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
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
            viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);
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
            AddImage(go, bgColor);
            return go;
        }

        public static GameObject CreateButton(Transform parent, string name, string text, Action onClick, float width = 80, float height = 28, int fontSize = 13)
        {
            var go = CreateObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = AddRectTransform(go);
            rt.sizeDelta = new Vector2(width, height);
            AddImage(go, new Color(0.82f, 0.86f, 0.92f, 0.9f));
            AddButton(go, onClick);
            var txtGo = CreateObject("Text", go.transform, typeof(RectTransform));
            var txtRt = AddRectTransform(txtGo);
            SetStretch(txtRt);
            AddText(txtGo, text, fontSize, TextAnchor.MiddleCenter);
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
            AddText(labelGo, label, 12, TextAnchor.MiddleLeft, new Color(0.3f, 0.35f, 0.45f, 1f));

            var valueGo = CreateObject("Value", go.transform, typeof(RectTransform));
            AddText(valueGo, value, 12, TextAnchor.MiddleLeft, new Color(0.1f, 0.15f, 0.25f, 1f));

            return go;
        }
    }
}
