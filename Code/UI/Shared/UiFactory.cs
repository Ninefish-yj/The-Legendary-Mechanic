using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>UI工厂：封装常见UGUI组件创建，消除重复代码</summary>
    internal static class UiFactory
    {
        /// <summary>创建Text组件（v0.194 科幻风格默认色）</summary>
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
            var f = LocalizedTextManager.current_font;
            if (f == null) f = SuperMechUiSkin.DefaultFont;
            txt.font = f;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color ?? SuperMechUiSkin.TextColor;
            txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
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

        /// <summary>创建按钮（v0.194 科幻风格三态切换+触屏友好）</summary>
        public static Button CreateButton(Transform parent, string text,
            UnityEngine.Events.UnityAction onClick, int fontSize = 14, Color? bgColor = null)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = SuperMechUiSkin.ButtonNormalSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor ?? Color.white;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = SuperMechUiSkin.ButtonHoverSprite;
            ss.pressedSprite = SuperMechUiSkin.ButtonActiveSprite;
            btn.spriteState = ss;
            btn.onClick.AddListener(onClick);
            var txt = CreateText(go.transform, text, fontSize, TextAnchor.MiddleCenter, new Color(0.95f, 0.96f, 0.98f, 1f));
            txt.fontStyle = FontStyle.Bold;
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.offsetMin = new Vector2(8, 4);
            txtRect.offsetMax = new Vector2(-8, -4);
            return btn;
        }
    }
}
