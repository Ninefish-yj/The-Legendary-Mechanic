using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 程序化UI皮肤——WorldBox原生融合风格（深棕暖色调）
    /// 配色：深灰棕半透明底 + 金色强调 + 米白文字 + 石质描边
    /// 9-slice pixelsPerUnit=100（关键：NML环境下必须用100，否则边框塌缩）
    /// </summary>
    public static class SuperMechUiSkin
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
        private static Sprite _cardSprite;
        private static Sprite _sectionSprite;

        // ═════════════════════════ 原生融合色板（深棕暖色调）═════════════════════════
        public static readonly Color BgColor = new Color(0.16f, 0.13f, 0.10f, 0.94f);
        public static readonly Color BgLight = new Color(0.22f, 0.18f, 0.14f, 0.95f);
        public static readonly Color BorderColor = new Color(0.55f, 0.42f, 0.25f, 0.9f);
        public static readonly Color BorderDim = new Color(0.38f, 0.30f, 0.20f, 0.7f);
        public static readonly Color TextColor = new Color(0.92f, 0.86f, 0.74f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.58f, 0.48f, 1f);
        public static readonly Color AccentColor = new Color(0.83f, 0.66f, 0.30f, 1f);
        public static readonly Color AccentDim = new Color(0.55f, 0.42f, 0.20f, 0.8f);
        public static readonly Color ButtonNormal = new Color(0.24f, 0.19f, 0.14f, 0.92f);
        public static readonly Color ButtonHover = new Color(0.35f, 0.28f, 0.20f, 0.96f);
        public static readonly Color ButtonActive = new Color(0.18f, 0.14f, 0.10f, 0.98f);
        public static readonly Color EdgeLight = new Color(0.65f, 0.52f, 0.32f, 0.4f);
        public static readonly Color EdgeDark = new Color(0.08f, 0.06f, 0.04f, 0.6f);
        public static readonly Color RowEven = new Color(0.20f, 0.16f, 0.12f, 0.5f);
        public static readonly Color RowOdd = new Color(0.16f, 0.13f, 0.10f, 0.3f);
        public static readonly Color CardBg = new Color(0.22f, 0.18f, 0.14f, 0.9f);
        public static readonly Color SectionBg = new Color(0.30f, 0.24f, 0.17f, 0.95f);
        public static readonly Color CloseRed = new Color(0.65f, 0.25f, 0.20f, 0.95f);

        // ═════════════════════ 星海总览大面板：冷色科幻色板（参考诸天神座 palette）═════════════════════
        public static readonly Color SciBg = new Color(0.067f, 0.094f, 0.153f, 0.96f);      // #111827 面板底
        public static readonly Color SciCard = new Color(0.094f, 0.133f, 0.208f, 0.92f);    // #182235 卡片
        public static readonly Color SciCardHover = new Color(0.13f, 0.20f, 0.30f, 0.96f);  // 悬浮（青调提亮）
        public static readonly Color SciCardActive = new Color(0.06f, 0.10f, 0.16f, 0.98f); // 按下
        public static readonly Color SciBorder = new Color(0.196f, 0.263f, 0.353f, 0.9f);   // #32435A 边框
        public static readonly Color SciGold = new Color(0.91f, 0.765f, 0.416f, 1f);        // #E8C36A 金色标题
        public static readonly Color SciCyan = new Color(0.286f, 0.843f, 0.89f, 1f);        // #49D7E3 青色高亮
        public static readonly Color SciPurple = new Color(0.608f, 0.482f, 1f, 1f);         // #9B7BFF 紫色
        public static readonly Color SciDanger = new Color(0.89f, 0.365f, 0.416f, 1f);      // #E35D6A 战斗红
        public static readonly Color SciText = new Color(0.91f, 0.93f, 0.96f, 1f);          // #E8EDF5 正文
        public static readonly Color SciMuted = new Color(0.576f, 0.643f, 0.737f, 1f);      // #93A4BC 次要

        // ═════════════════════════ Sprite属性 ═════════════════════════
        public static Sprite Panel => EnsureInit() ? _panelSprite : null;
        public static Sprite ButtonNormalSprite => EnsureInit() ? _buttonNormal : null;
        public static Sprite ButtonHoverSprite => EnsureInit() ? _buttonHover : null;
        public static Sprite ButtonActiveSprite => EnsureInit() ? _buttonActive : null;
        public static Sprite TitleBar => EnsureInit() ? _titleBarSprite : null;
        public static Sprite Scrollbar => EnsureInit() ? _scrollbarSprite : null;
        public static Sprite RowEvenSprite => EnsureInit() ? _rowEven : null;
        public static Sprite RowOddSprite => EnsureInit() ? _rowOdd : null;
        public static Sprite Card => EnsureInit() ? _cardSprite : null;
        public static Sprite Section => EnsureInit() ? _sectionSprite : null;

        public static Font DefaultFont
        {
            get
            {
                var f = LocalizedTextManager.current_font;
                if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return f;
            }
        }

        private static bool EnsureInit()
        {
            if (_initialized) return true;
            try
            {
                _panelSprite = MakeSlicedSprite(BgColor, BorderColor, 64, 8, "SM_Panel");
                _buttonNormal = MakeSlicedSprite(ButtonNormal, BorderDim, 16, 3, "SM_BtnNormal");
                _buttonHover = MakeSlicedSprite(ButtonHover, BorderColor, 16, 3, "SM_BtnHover");
                _buttonActive = MakeSlicedSprite(ButtonActive, AccentColor, 16, 3, "SM_BtnActive");
                _titleBarSprite = MakeSlicedSprite(new Color(0.04f, 0.06f, 0.10f, 0.98f), BorderDim, 16, 2, "SM_TitleBar");
                _scrollbarSprite = MakeSolidSprite(new Color(0.30f, 0.50f, 0.75f, 0.6f), "SM_Scrollbar");
                _rowEven = MakeSolidSprite(RowEven, "SM_RowEven");
                _rowOdd = MakeSolidSprite(RowOdd, "SM_RowOdd");
                _cardSprite = MakeSlicedSprite(CardBg, BorderDim, 16, 2, "SM_Card");
                _sectionSprite = MakeSlicedSprite(SectionBg, BorderColor, 16, 2, "SM_Section");
                _initialized = true;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] SMUiSkin初始化失败: " + e);
                return false;
            }
        }

        /// <summary>生成9-slice Sprite（pixelsPerUnit=100，NML环境关键参数）</summary>
        private static Sprite MakeSlicedSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    bool isCorner = ((x < borderWidth || x >= size - borderWidth) &&
                                     (y < borderWidth || y >= size - borderWidth));
                    // 切角：四角最外层像素透明
                    bool isCutCorner = (x == 0 && y == 0) || (x == size - 1 && y == 0) ||
                                       (x == 0 && y == size - 1) || (x == size - 1 && y == size - 1);
                    if (isCutCorner)
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    else if (isBorder || isCorner)
                        pixels[y * size + x] = border;
                    else
                        pixels[y * size + x] = bg;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            // 关键：pixelsPerUnit=100，否则NML环境下边框塌缩
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderWidth, borderWidth, borderWidth, borderWidth));
            sprite.name = name;
            return sprite;
        }

        /// <summary>生成纯色Sprite</summary>
        private static Sprite MakeSolidSprite(Color color, string name)
        {
            int size = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        // ═════════════════════════ UI工具方法 ═════════════════════════

        /// <summary>创建Text组件</summary>
        public static Text MakeText(Transform parent, string content, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            var df = DefaultFont; if (df != null) text.font = df; // v0.75.11: 字体兜底，避免NRE白板
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

        /// <summary>创建按钮（三态SpriteSwap + 立体描边）</summary>
        public static Button MakeButton(Transform parent, string text, int fontSize = 14, System.Action onClick = null)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = ButtonNormalSprite;
            img.type = Image.Type.Sliced;
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

        /// <summary>添加立体描边（顶高光+底阴影）</summary>
        public static void Add3DEdges(RectTransform target)
        {
            var topGo = new GameObject("EdgeTop");
            topGo.transform.SetParent(target, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 1);
            topRect.anchoredPosition = Vector2.zero;
            topGo.AddComponent<Image>().color = EdgeLight;

            var botGo = new GameObject("EdgeBottom");
            botGo.transform.SetParent(target, false);
            var botRect = botGo.AddComponent<RectTransform>();
            botRect.anchorMin = new Vector2(0, 0);
            botRect.anchorMax = new Vector2(1, 0);
            botRect.pivot = new Vector2(0.5f, 0);
            botRect.sizeDelta = new Vector2(0, 1);
            botRect.anchoredPosition = Vector2.zero;
            botGo.AddComponent<Image>().color = EdgeDark;
        }

        /// <summary>创建滚动区（Mask + VLG + ContentSizeFitter + 冰蓝滚动条）</summary>
        public static (ScrollRect scroll, RectTransform content) CreateScrollArea(Transform parent, string name)
        {
            // ScrollRect + Mask 容器
            var scrollGo = new GameObject(name);
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

            // Mask的Image必须用极小非零alpha（0.01），alpha=0会导致Mask模板区域为空，内容被整体裁剪
            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            maskImg.raycastTarget = false;
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // 独立Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            // Content（左上角锚定，高度由ContentSizeFitter决定）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            // Content透明命中兜底层：纯文本行raycastTarget=false时，ScrollRect收不到拖动事件
            var contentHit = contentGo.AddComponent<Image>();
            contentHit.color = new Color(0f, 0f, 0f, 0f);
            contentHit.raycastTarget = true;

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;

            return (scrollRect, contentRect);
        }
    }
}
