using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>UI工厂：封装常见UGUI组件创建，消除重复代码</summary>
    internal static class UiFactory
    {
        /// <summary>创建Text组件</summary>
        public static Text CreateText(Transform parent, string text = "", int fontSize = 12,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color ?? Color.white;
            txt.text = text;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>创建Image组件</summary>
        public static Image CreateImage(Transform parent, Sprite sprite = null, Color? color = null)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            if (color.HasValue) img.color = color.Value;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>创建全屏填充的RectTransform</summary>
        public static RectTransform FillParent(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>创建居中的RectTransform</summary>
        public static RectTransform Center(GameObject go, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>创建按钮</summary>
        public static Button CreateButton(Transform parent, string text,
            UnityEngine.Events.UnityAction onClick, int fontSize = 12, Color? bgColor = null)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.2f, 0.25f, 0.35f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var txt = CreateText(go.transform, text, fontSize, TextAnchor.MiddleCenter, Color.white);
            return btn;
        }
    }
}
