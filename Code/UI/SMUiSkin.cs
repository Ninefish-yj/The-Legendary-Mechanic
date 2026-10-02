using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 程序化UI皮肤——用代码生成纯色纹理，不依赖大图，和原版像素风格一致
    /// 配色：深灰蓝半透明背景 + 冰蓝细边框 + 银白文字（原著星海终端风格）
    /// </summary>
    public static class SMUiSkin
    {
        private static bool _initialized;
        private static Sprite _panelSprite;
        private static Sprite _buttonNormal;
        private static Sprite _buttonHover;
        private static Sprite _buttonActive;
        private static Sprite _titleBarSprite;
        private static Sprite _scrollbarSprite;
        private static Sprite _rowEven;
        private static Sprite _rowOdd;

        // 配色
        public static readonly Color BgColor = new Color(0.08f, 0.10f, 0.14f, 0.92f);
        public static readonly Color BorderColor = new Color(0.45f, 0.65f, 0.85f, 0.85f);
        public static readonly Color TextColor = new Color(0.90f, 0.95f, 1.0f, 1f);
        public static readonly Color TextDim = new Color(0.6f, 0.7f, 0.8f, 1f);
        public static readonly Color AccentColor = new Color(0.4f, 0.7f, 1.0f, 1f);
        public static readonly Color ButtonNormal = new Color(0.12f, 0.15f, 0.20f, 0.9f);
        public static readonly Color ButtonHover = new Color(0.20f, 0.28f, 0.38f, 0.95f);
        public static readonly Color ButtonActive = new Color(0.08f, 0.10f, 0.14f, 0.95f);

        public static Sprite Panel => EnsureInit() ? _panelSprite : null;
        public static Sprite ButtonNormalSprite => EnsureInit() ? _buttonNormal : null;
        public static Sprite ButtonHoverSprite => EnsureInit() ? _buttonHover : null;
        public static Sprite ButtonActiveSprite => EnsureInit() ? _buttonActive : null;
        public static Sprite TitleBar => EnsureInit() ? _titleBarSprite : null;
        public static Sprite Scrollbar => EnsureInit() ? _scrollbarSprite : null;
        public static Sprite RowEven => EnsureInit() ? _rowEven : null;
        public static Sprite RowOdd => EnsureInit() ? _rowOdd : null;

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static bool EnsureInit()
        {
            if (_initialized) return true;
            try
            {
                _panelSprite = MakeSolidSprite(BgColor, "SM_Panel");
                _buttonNormal = MakeSolidSprite(ButtonNormal, "SM_BtnNormal");
                _buttonHover = MakeSolidSprite(ButtonHover, "SM_BtnHover");
                _buttonActive = MakeSolidSprite(ButtonActive, "SM_BtnActive");
                _titleBarSprite = MakeSolidSprite(new Color(0.05f, 0.07f, 0.10f, 0.95f), "SM_TitleBar");
                _scrollbarSprite = MakeSolidSprite(new Color(0.3f, 0.5f, 0.7f, 0.6f), "SM_Scrollbar");
                _rowEven = MakeSolidSprite(new Color(0.10f, 0.13f, 0.18f, 0.5f), "SM_RowEven");
                _rowOdd = MakeSolidSprite(new Color(0.08f, 0.10f, 0.14f, 0.3f), "SM_RowOdd");
                _initialized = true;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] SMUiSkin初始化失败: " + e);
                return false;
            }
        }

        /// <summary>生成纯色Sprite（最简单可靠的方式）</summary>
        private static Sprite MakeSolidSprite(Color color, string name)
        {
            int size = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>创建Text组件</summary>
        public static Text MakeText(Transform parent, string content, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = content;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        /// <summary>创建按钮（三态SpriteSwap）</summary>
        public static Button MakeButton(Transform parent, string text, int fontSize = 14, System.Action onClick = null)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = ButtonNormalSprite;
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = btn.spriteState;
            spriteState.highlightedSprite = ButtonHoverSprite;
            spriteState.pressedSprite = ButtonActiveSprite;
            btn.spriteState = spriteState;
            var btnText = MakeText(go.transform, text, fontSize, TextAnchor.MiddleCenter);
            btnText.color = TextColor;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }
    }
}
