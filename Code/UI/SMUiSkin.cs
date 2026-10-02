using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 程序化UI皮肤——用代码生成纹理，不依赖大图，和原版像素风格一致
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
                _panelSprite = MakePanelSprite();
                _buttonNormal = MakeButtonSprite(ButtonNormal);
                _buttonHover = MakeButtonSprite(ButtonHover);
                _buttonActive = MakeButtonSprite(ButtonActive);
                _titleBarSprite = MakeTitleBarSprite();
                _scrollbarSprite = MakeScrollbarSprite();
                _rowEven = MakeRowSprite(new Color(0.10f, 0.13f, 0.18f, 0.5f));
                _rowOdd = MakeRowSprite(new Color(0.08f, 0.10f, 0.14f, 0.3f));
                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>生成9-slice面板纹理：中心半透明背景+1px冰蓝边框+切角</summary>
        private static Sprite MakePanelSprite()
        {
            int size = 9;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                    bool isCorner = (x <= 1 && y <= 1) || (x >= size - 2 && y <= 1) ||
                                    (x <= 1 && y >= size - 2) || (x >= size - 2 && y >= size - 2);
                    // 切角：四角去掉最外层像素
                    bool isCutCorner = (x == 0 && y == 0) || (x == size - 1 && y == 0) ||
                                       (x == 0 && y == size - 1) || (x == size - 1 && y == size - 1);

                    if (isCutCorner)
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    else if (isBorder || isCorner)
                        pixels[y * size + x] = BorderColor;
                    else
                        pixels[y * size + x] = BgColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect,
                new Vector4(2, 2, 2, 2));
            sprite.name = "SM_Panel";
            return sprite;
        }

        /// <summary>生成按钮纹理：圆角矩形+边框</summary>
        private static Sprite MakeButtonSprite(Color bg)
        {
            int w = 12, h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    bool isCutCorner = (x <= 1 && y <= 1) || (x >= w - 2 && y <= 1) ||
                                       (x <= 1 && y >= h - 2) || (x >= w - 2 && y >= h - 2);
                    bool isOuterCorner = (x == 0 && y == 0) || (x == w - 1 && y == 0) ||
                                         (x == 0 && y == h - 1) || (x == w - 1 && y == h - 1);

                    if (isOuterCorner)
                        pixels[y * w + x] = new Color(0, 0, 0, 0);
                    else if (isBorder || isCutCorner)
                        pixels[y * w + x] = BorderColor * 0.7f;
                    else
                        pixels[y * w + x] = bg;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect,
                new Vector4(3, 3, 3, 3));
            sprite.name = "SM_Button";
            return sprite;
        }

        /// <summary>标题栏：比面板稍深+底部冰蓝分隔线</summary>
        private static Sprite MakeTitleBarSprite()
        {
            int w = 4, h = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y == 0)
                        pixels[y * w + x] = new Color(0.06f, 0.08f, 0.12f, 0.95f);
                    else
                        pixels[y * w + x] = new Color(0.10f, 0.13f, 0.18f, 0.85f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = "SM_TitleBar";
            return sprite;
        }

        /// <summary>滚动条：细冰蓝条</summary>
        private static Sprite MakeScrollbarSprite()
        {
            int w = 4, h = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.4f, 0.6f, 0.8f, 0.6f);
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
            sprite.name = "SM_Scrollbar";
            return sprite;
        }

        /// <summary>列表行背景</summary>
        private static Sprite MakeRowSprite(Color c)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[4];
            for (int i = 0; i < 4; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>创建标准文本组件</summary>
        public static Text MakeText(Transform parent, string text, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = fontSize;
            t.color = TextColor;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.text = text;
            return t;
        }

        /// <summary>创建标准按钮（三态）</summary>
        public static Button MakeButton(Transform parent, string text, int fontSize = 14)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = ButtonNormalSprite;
            img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = btn.spriteState;
            spriteState.highlightedSprite = ButtonHoverSprite;
            spriteState.pressedSprite = ButtonActiveSprite;
            btn.spriteState = spriteState;
            var btnText = MakeText(go.transform, text, fontSize, TextAnchor.MiddleCenter);
            var textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
