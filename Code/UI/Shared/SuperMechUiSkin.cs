using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 程序化UI皮肤——v0.194 统一科幻星际风格（参考星际争霸+星穹铁道）
    /// 配色：深蓝黑底 + 金色标题 + 青色高亮 + 细边框 + 卡片分层
    /// 9-slice pixelsPerUnit=100（关键：NML环境下必须用100，否则边框塌缩）
    /// </summary>
    public static class SuperMechUiSkin
    {
        private static bool _initialized;
        private static Sprite _panelSprite;
        private static Sprite _buttonNormal;
        private static Sprite _buttonHover;
        private static Sprite _buttonActive;
        private static Sprite _buttonSelected;
        private static Sprite _buttonDisabled;
        private static Sprite _titleBarSprite;
        private static Sprite _scrollbarSprite;
        private static Sprite _rowEven;
        private static Sprite _rowOdd;
        private static Sprite _cardSprite;
        private static Sprite _cardHover;
        private static Sprite _sectionSprite;
        private static Sprite _dividerSprite;
        private static Sprite _glowSprite;
        private static Sprite _iconCircle;
        private static Sprite _iconHex;
        private static Sprite _progressBg;
        private static Sprite _progressFill;
        private static Sprite _cornerDecor;
        private static Sprite _tabNormal;
        private static Sprite _tabActive;
        private static Sprite _qiIcon;

        // ═════════════════════════ v0.215 优化可读性：提高对比度+统一字号层级 ═════════════════════════
        // 设计原则：高对比度(WCAG AA)、清晰字号层级、充足间距、一致圆角
        public static readonly Color BgColor = new Color(0.08f, 0.11f, 0.18f, 0.97f);       // 面板底 #141C2E（提亮30%增强可读性）
        public static readonly Color BgLight = new Color(0.12f, 0.16f, 0.25f, 0.96f);       // 次级面板 #1F2940
        public static readonly Color BorderColor = new Color(0.35f, 0.48f, 0.68f, 0.9f);    // 边框 #597AAD（提亮增强边界感）
        public static readonly Color BorderDim = new Color(0.20f, 0.28f, 0.40f, 0.7f);      // 暗边框 #334766
        public static readonly Color BorderGold = new Color(0.85f, 0.68f, 0.35f, 0.95f);    // 金色边框 #D9AD59（提亮）
        public static readonly Color TextColor = new Color(0.95f, 0.96f, 0.98f, 1f);        // 正文 #F2F5FA（提高对比度）
        public static readonly Color TextDim = new Color(0.65f, 0.72f, 0.82f, 1f);          // 次要文字 #A6B8D1（从0.55提亮到0.65）
        public static readonly Color AccentColor = new Color(0.95f, 0.80f, 0.45f, 1f);      // 金色强调 #F2CC73（标题/重要数值）
        public static readonly Color AccentDim = new Color(0.65f, 0.52f, 0.28f, 0.85f);     // 暗金
        public static readonly Color CyanColor = new Color(0.40f, 0.82f, 0.90f, 1f);        // 青色高亮 #66D1E6（数值/进度/选中）
        public static readonly Color CyanDim = new Color(0.22f, 0.50f, 0.60f, 0.8f);        // 暗青
        public static readonly Color ButtonNormal = new Color(0.14f, 0.20f, 0.32f, 0.95f);  // 按钮常态 #243352（提亮）
        public static readonly Color ButtonHover = new Color(0.22f, 0.32f, 0.48f, 0.98f);   // 按钮悬停 #38527A（增大差异）
        public static readonly Color ButtonActive = new Color(0.09f, 0.13f, 0.20f, 0.98f);  // 按钮按下 #172133
        public static readonly Color EdgeLight = new Color(0.40f, 0.55f, 0.75f, 0.45f);     // 顶高光
        public static readonly Color EdgeDark = new Color(0.04f, 0.06f, 0.10f, 0.6f);       // 底阴影
        public static readonly Color RowEven = new Color(0.10f, 0.15f, 0.24f, 0.6f);        // 偶数行（提亮）
        public static readonly Color RowOdd = new Color(0.07f, 0.11f, 0.18f, 0.4f);         // 奇数行
        public static readonly Color CardBg = new Color(0.12f, 0.17f, 0.27f, 0.95f);        // 卡片底 #1F2B45
        public static readonly Color SectionBg = new Color(0.16f, 0.22f, 0.34f, 0.96f);     // 分区底 #293857
        public static readonly Color CloseRed = new Color(0.80f, 0.30f, 0.27f, 0.95f);      // 关闭按钮红（提亮）
        public static readonly Color SuccessGreen = new Color(0.40f, 0.78f, 0.50f, 1f);     // 成功绿
        public static readonly Color DangerRed = new Color(0.90f, 0.40f, 0.45f, 1f);        // 危险红
        public static readonly Color WarningOrange = new Color(0.95f, 0.70f, 0.35f, 1f);    // 警告橙

        // 字号层级规范（大众阅读习惯）
        public const int FontSizeTitle = 18;    // 窗口标题
        public const int FontSizeSection = 15;  // 分区标题
        public const int FontSizeBody = 13;     // 正文
        public const int FontSizeSmall = 11;    // 辅助文字/标签
        public const int FontSizeMini = 10;     // 极小注释

        // 兼容旧代码的别名（星海总览等已用Sci前缀的地方继续可用）
        public static readonly Color SciBg = BgColor;
        public static readonly Color SciCard = CardBg;
        public static readonly Color SciCardHover = ButtonHover;
        public static readonly Color SciCardActive = ButtonActive;
        public static readonly Color SciBorder = BorderColor;
        public static readonly Color SciGold = AccentColor;
        public static readonly Color SciCyan = CyanColor;
        public static readonly Color SciPurple = new Color(0.55f, 0.45f, 0.85f, 1f);
        public static readonly Color SciDanger = DangerRed;
        public static readonly Color SciText = TextColor;
        public static readonly Color SciMuted = TextDim;

        // ═════════════════════════ Sprite属性 ═════════════════════════
        public static Sprite Panel => EnsureInit() ? _panelSprite : null;
        public static Sprite ButtonNormalSprite => EnsureInit() ? _buttonNormal : null;
        public static Sprite ButtonHoverSprite => EnsureInit() ? _buttonHover : null;
        public static Sprite ButtonActiveSprite => EnsureInit() ? _buttonActive : null;
        public static Sprite ButtonSelectedSprite => EnsureInit() ? _buttonSelected : null;
        public static Sprite ButtonDisabledSprite => EnsureInit() ? _buttonDisabled : null;
        public static Sprite TitleBar => EnsureInit() ? _titleBarSprite : null;
        public static Sprite Scrollbar => EnsureInit() ? _scrollbarSprite : null;
        public static Sprite RowEvenSprite => EnsureInit() ? _rowEven : null;
        public static Sprite RowOddSprite => EnsureInit() ? _rowOdd : null;
        public static Sprite Card => EnsureInit() ? _cardSprite : null;
        public static Sprite CardHover => EnsureInit() ? _cardHover : null;
        public static Sprite Section => EnsureInit() ? _sectionSprite : null;
        public static Sprite Divider => EnsureInit() ? _dividerSprite : null;
        public static Sprite Glow => EnsureInit() ? _glowSprite : null;
        public static Sprite IconCircle => EnsureInit() ? _iconCircle : null;
        public static Sprite IconHex => EnsureInit() ? _iconHex : null;
        public static Sprite ProgressBg => EnsureInit() ? _progressBg : null;
        public static Sprite ProgressFill => EnsureInit() ? _progressFill : null;
        public static Sprite CornerDecor => EnsureInit() ? _cornerDecor : null;
        public static Sprite TabNormal => EnsureInit() ? _tabNormal : null;
        public static Sprite TabActive => EnsureInit() ? _tabActive : null;
        public static Sprite QiIcon => EnsureInit() ? _qiIcon : null;

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
                // v0.202 全面美化：圆角+内发光+装饰角标（参考星穹铁道+暗黑破坏神+魔兽世界）
                _panelSprite = MakeRoundedGlowSprite(BgColor, BorderColor, CyanDim, 64, 6, 2, "SM_Panel");
                // 按钮：渐变+内发光+金色边框+顶部高光
                _buttonNormal = MakeButtonSprite(
                    new Color(0.18f, 0.26f, 0.42f, 1f),
                    new Color(0.09f, 0.14f, 0.24f, 1f),
                    new Color(0.70f, 0.56f, 0.30f, 1f),
                    new Color(0.40f, 0.58f, 0.80f, 0.9f),
                    new Color(0.04f, 0.07f, 0.12f, 0.9f),
                    32, 3, "SM_BtnNormal");
                _buttonHover = MakeButtonSprite(
                    new Color(0.26f, 0.38f, 0.58f, 1f),
                    new Color(0.14f, 0.22f, 0.36f, 1f),
                    new Color(0.90f, 0.74f, 0.40f, 1f),
                    new Color(0.50f, 0.70f, 0.92f, 0.95f),
                    new Color(0.06f, 0.10f, 0.18f, 0.9f),
                    32, 3, "SM_BtnHover");
                _buttonActive = MakeButtonSprite(
                    new Color(0.07f, 0.12f, 0.20f, 1f),
                    new Color(0.04f, 0.07f, 0.12f, 1f),
                    new Color(0.30f, 0.65f, 0.75f, 1f),
                    null,
                    new Color(0.02f, 0.04f, 0.08f, 0.9f),
                    32, 3, "SM_BtnActive");
                _buttonSelected = MakeButtonSprite(
                    new Color(0.22f, 0.40f, 0.55f, 1f),
                    new Color(0.12f, 0.24f, 0.36f, 1f),
                    new Color(0.40f, 0.80f, 0.90f, 1f),
                    new Color(0.55f, 0.85f, 0.95f, 0.9f),
                    new Color(0.05f, 0.09f, 0.15f, 0.9f),
                    32, 3, "SM_BtnSelected");
                _buttonDisabled = MakeButtonSprite(
                    new Color(0.12f, 0.14f, 0.18f, 1f),
                    new Color(0.08f, 0.09f, 0.12f, 1f),
                    new Color(0.30f, 0.32f, 0.36f, 1f),
                    null,
                    null,
                    32, 3, "SM_BtnDisabled");
                _titleBarSprite = MakeRoundedGlowSprite(new Color(0.04f, 0.06f, 0.10f, 0.98f), BorderGold, AccentDim, 32, 4, 1, "SM_TitleBar");
                _scrollbarSprite = MakeSolidSprite(new Color(0.35f, 0.65f, 0.80f, 0.7f), "SM_Scrollbar");
                _rowEven = MakeSolidSprite(RowEven, "SM_RowEven");
                _rowOdd = MakeSolidSprite(RowOdd, "SM_RowOdd");
                _cardSprite = MakeRoundedGlowSprite(CardBg, BorderDim, CyanDim, 32, 4, 1, "SM_Card");
                _cardHover = MakeRoundedGlowSprite(new Color(0.12f, 0.18f, 0.28f, 0.95f), BorderColor, CyanColor, 32, 4, 1, "SM_CardHover");
                _sectionSprite = MakeRoundedGlowSprite(SectionBg, BorderColor, AccentDim, 32, 4, 1, "SM_Section");
                _dividerSprite = MakeSolidSprite(new Color(0.30f, 0.42f, 0.60f, 0.5f), "SM_Divider");
                _glowSprite = MakeGlowSprite(CyanColor, "SM_Glow");
                _iconCircle = MakeCircleSprite(new Color(0.10f, 0.16f, 0.26f, 0.95f), BorderColor, 32, 2, "SM_IconCircle");
                _iconHex = MakeHexSprite(new Color(0.10f, 0.16f, 0.26f, 0.95f), AccentColor, 32, 2, "SM_IconHex");
                _progressBg = MakeRoundedGlowSprite(new Color(0.06f, 0.09f, 0.14f, 0.9f), BorderDim, null, 16, 3, 0, "SM_ProgressBg");
                _progressFill = MakeRoundedGlowSprite(new Color(0.25f, 0.65f, 0.75f, 1f), CyanColor, null, 16, 3, 0, "SM_ProgressFill");
                _cornerDecor = MakeCornerDecorSprite(BorderGold, "SM_Corner");
                _tabNormal = MakeButtonSprite(
                    new Color(0.10f, 0.15f, 0.24f, 0.9f),
                    new Color(0.06f, 0.09f, 0.15f, 0.9f),
                    BorderDim,
                    null, null, 24, 2, "SM_TabNormal");
                _tabActive = MakeButtonSprite(
                    new Color(0.18f, 0.28f, 0.44f, 1f),
                    new Color(0.10f, 0.18f, 0.30f, 1f),
                    AccentColor,
                    new Color(0.45f, 0.62f, 0.82f, 0.8f),
                    null, 24, 2, "SM_TabActive");
                // v0.203 气力图标：蓝色能量球+青色电光边框（原著机械系气力设定）
                _qiIcon = MakeQiIconSprite();
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
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderWidth, borderWidth, borderWidth, borderWidth));
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.194 生成带垂直渐变+顶部高光+底部阴影的按钮Sprite</summary>
        private static Sprite MakeButtonSprite(Color bgTop, Color bgBottom, Color border,
            Color? highlight = null, Color? shadow = null, int size = 32, int borderWidth = 3, string name = "Btn")
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            int contentTop = borderWidth + 1;       // 高光行
            int contentBottom = size - borderWidth - 2; // 阴影行
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    bool isCutCorner = (x <= 1 && y <= 1) || (x >= size - 2 && y <= 1) ||
                                       (x <= 1 && y >= size - 2) || (x >= size - 2 && y >= size - 2);
                    if (isCutCorner)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (isBorder)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        // 垂直渐变
                        float t = (float)(y - borderWidth) / (size - 2 * borderWidth);
                        Color c = Color.Lerp(bgTop, bgBottom, t);
                        // 顶部高光（第borderWidth行）
                        if (y == borderWidth && highlight.HasValue)
                            c = Color.Lerp(c, highlight.Value, 0.7f);
                        // 次高光
                        else if (y == borderWidth + 1 && highlight.HasValue)
                            c = Color.Lerp(c, highlight.Value, 0.3f);
                        // 底部阴影（倒数第borderWidth行）
                        if (y == size - borderWidth - 1 && shadow.HasValue)
                            c = Color.Lerp(c, shadow.Value, 0.6f);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
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

        /// <summary>生成径向发光Sprite（用于标题装饰/选中高亮）</summary>
        private static Sprite MakeGlowSprite(Color color, string name)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist) * 0.6f;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 圆角+内发光边框Sprite（参考星穹铁道卡片风格）</summary>
        private static Sprite MakeRoundedGlowSprite(Color bg, Color border, Color? innerGlow, int size, int borderRadius, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            float radius = borderRadius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 圆角判断：四个角用圆形裁剪
                    bool inCorner = false;
                    float cornerDist = 0f;
                    if (x < borderRadius && y < borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((borderRadius - x) * (borderRadius - x) + (borderRadius - y) * (borderRadius - y));
                        inCorner = cornerDist > radius;
                    }
                    else if (x >= size - borderRadius && y < borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((x - (size - borderRadius - 1)) * (x - (size - borderRadius - 1)) + (borderRadius - y) * (borderRadius - y));
                        inCorner = cornerDist > radius;
                    }
                    else if (x < borderRadius && y >= size - borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((borderRadius - x) * (borderRadius - x) + (y - (size - borderRadius - 1)) * (y - (size - borderRadius - 1)));
                        inCorner = cornerDist > radius;
                    }
                    else if (x >= size - borderRadius && y >= size - borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((x - (size - borderRadius - 1)) * (x - (size - borderRadius - 1)) + (y - (size - borderRadius - 1)) * (y - (size - borderRadius - 1)));
                        inCorner = cornerDist > radius;
                    }

                    if (inCorner)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    // 边框判断
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    // 圆角处的边框也要跟着圆
                    if (!isBorder && borderRadius > 0)
                    {
                        if ((x < borderRadius + borderWidth && y < borderRadius + borderWidth) ||
                            (x >= size - borderRadius - borderWidth && y < borderRadius + borderWidth) ||
                            (x < borderRadius + borderWidth && y >= size - borderRadius - borderWidth) ||
                            (x >= size - borderRadius - borderWidth && y >= size - borderRadius - borderWidth))
                        {
                            // 圆角区域内，距离边缘近的算边框
                            float edgeDist = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                            if (edgeDist < borderWidth) isBorder = true;
                        }
                    }

                    Color c = bg;
                    if (isBorder)
                    {
                        c = border;
                    }
                    else if (innerGlow.HasValue)
                    {
                        // 内发光：靠近边框的1-2像素混入发光色
                        float edgeDist = Mathf.Min(x - borderWidth, y - borderWidth, size - 1 - x - borderWidth, size - 1 - y - borderWidth);
                        if (edgeDist < 2f)
                        {
                            float glowAmount = (2f - edgeDist) / 2f * 0.35f;
                            c = Color.Lerp(c, innerGlow.Value, glowAmount);
                        }
                    }
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderRadius, borderRadius, borderRadius, borderRadius));
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 圆形图标背景Sprite（参考原神/星穹铁道角色图标）</summary>
        private static Sprite MakeCircleSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center + 0.5f) * (x - center + 0.5f) + (y - center + 0.5f) * (y - center + 0.5f));
                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (dist > radius - borderWidth)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        // 径向渐变：中心亮边缘暗
                        float t = dist / radius;
                        Color c = Color.Lerp(new Color(bg.r * 1.3f, bg.g * 1.3f, bg.b * 1.3f, bg.a), bg, t);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.203 气力图标：蓝色能量球+青色闪电符号（原著机械系气力设定）</summary>
        private static Sprite MakeQiIconSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            Color bgCore = new Color(0.15f, 0.35f, 0.65f, 1f);
            Color bgEdge = new Color(0.05f, 0.12f, 0.25f, 1f);
            Color borderColor = new Color(0.3f, 0.75f, 0.95f, 1f);
            Color lightning = new Color(0.7f, 0.95f, 1f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center + 0.5f) * (x - center + 0.5f) + (y - center + 0.5f) * (y - center + 0.5f));
                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (dist > radius - 2f)
                    {
                        pixels[y * size + x] = borderColor;
                    }
                    else
                    {
                        float t = dist / radius;
                        pixels[y * size + x] = Color.Lerp(bgCore, bgEdge, t);
                    }
                }
            }

            // 闪电符号（Z字形像素艺术）
            int[,] bolt = {
                {15, 6}, {16, 6}, {17, 7},
                {14, 8}, {15, 8}, {16, 8}, {17, 9},
                {13, 10}, {14, 10}, {15, 10}, {16, 10}, {17, 11},
                {14, 12}, {15, 12}, {16, 12}, {17, 13},
                {15, 14}, {16, 14}, {17, 15},
                {14, 16}, {15, 16}, {16, 16}, {17, 17},
                {15, 18}, {16, 18}, {17, 19},
                {16, 20}, {17, 21},
                {15, 22}, {16, 22}, {17, 23},
                {16, 24}, {17, 25}
            };
            for (int i = 0; i < bolt.GetLength(0); i++)
            {
                int px = bolt[i, 0];
                int py = bolt[i, 1];
                if (px >= 0 && px < size && py >= 0 && py < size)
                {
                    float dist = Mathf.Sqrt((px - center + 0.5f) * (px - center + 0.5f) + (py - center + 0.5f) * (py - center + 0.5f));
                    if (dist <= radius - 2f)
                    {
                        pixels[py * size + px] = lightning;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var qiSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            qiSprite.name = "SM_QiIcon";
            return qiSprite;
        }

        /// <summary>v0.202 六边形图标背景Sprite（参考魔兽世界职业图标/科技感）</summary>
        private static Sprite MakeHexSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float cx = size / 2f, cy = size / 2f;
            float r = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                    // 六边形距离函数（pointy-top）
                    float q = (Mathf.Sqrt(3f) / 3f * dx - 1f / 3f * dy) / r;
                    float rcoord = (2f / 3f * dy) / r;
                    float hexDist = Mathf.Max(Mathf.Abs(q), Mathf.Abs(rcoord), Mathf.Abs(q + rcoord));
                    if (hexDist > 1f)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (hexDist > 1f - (float)borderWidth / r)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        float t = hexDist;
                        Color c = Color.Lerp(new Color(bg.r * 1.25f, bg.g * 1.25f, bg.b * 1.25f, bg.a), bg, t);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 装饰角标Sprite（L形金色角标，参考暗黑破坏神UI）</summary>
        private static Sprite MakeCornerDecorSprite(Color color, string name)
        {
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // L形：底部3像素+左侧3像素，右下角为原点
                    bool isL = (y < 3 && x < size) || (x < 3 && y < size);
                    // 末端渐细
                    if (y < 3 && x > size - 4) isL = false;
                    if (x < 3 && y > size - 4) isL = false;
                    if (isL)
                    {
                        // 末端淡出
                        float alpha = 1f;
                        if (y < 3 && x > size - 8) alpha = (float)(size - x) / 8f;
                        if (x < 3 && y > size - 8) alpha = Mathf.Min(alpha, (float)(size - y) / 8f);
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0f), 100f);
            sprite.name = name;
            return sprite;
        }

        // ═════════════════════════ UI工具方法 ═════════════════════════

        /// <summary>创建Text组件（默认正文15号字，提高可读性）</summary>
        public static Text MakeText(Transform parent, string content, int fontSize = 15, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            var df = DefaultFont; if (df != null) text.font = df;
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

        /// <summary>创建按钮（三态SpriteSwap + 立体描边，默认正文14号字）</summary>
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

        // ═════════════════════════ v0.202 美化新增工具方法 ═════════════════════════

        /// <summary>创建圆角卡片（参考星穹铁道卡片风格）</summary>
        public static (GameObject go, RectTransform rt, Image img) MakeCard(Transform parent, string name, bool hoverable = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = hoverable ? CardHover : Card;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            return (go, rt, img);
        }

        /// <summary>创建带圆形/六边形背景的图标（参考原神角色图标）</summary>
        public static (GameObject go, Image bgImg, Image iconImg) MakeIconWithBg(Transform parent, string name, Sprite icon, bool hex = false, float size = 40f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = hex ? IconHex : IconCircle;
            bgImg.color = Color.white;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(size * 0.15f, size * 0.15f);
            iconRt.offsetMax = new Vector2(-size * 0.15f, -size * 0.15f);
            var iconImg = iconGo.AddComponent<Image>();
            if (icon != null) iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            return (go, bgImg, iconImg);
        }

        /// <summary>创建进度条（参考魔兽世界/暗黑破坏神血条风格）</summary>
        public static (GameObject go, Image bgImg, Image fillImg, Text labelText) MakeProgressBar(Transform parent, string name, float width, float height = 16f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = ProgressBg;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = ProgressFill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = CyanColor;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<Text>();
            var df = DefaultFont; if (df != null) labelText.font = df;
            labelText.fontSize = Mathf.Max(10, (int)(height * 0.7f));
            labelText.color = TextColor;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "";
            return (go, bgImg, fillImg, labelText);
        }

        /// <summary>创建分区标题（金色文字+左侧装饰条+底部分隔线）</summary>
        public static (GameObject go, Text titleText) MakeSectionHeader(Transform parent, string title, int fontSize = 16)
        {
            var go = new GameObject("SectionHeader");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28f);

            // 左侧金色装饰条
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(go.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f);
            barRt.anchorMax = new Vector2(0, 0.5f);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.sizeDelta = new Vector2(3f, 18f);
            barRt.anchoredPosition = new Vector2(0, 0);
            barGo.AddComponent<Image>().color = AccentColor;

            // 标题文字
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(go.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(10f, 0);
            titleRt.offsetMax = Vector2.zero;
            var titleText = titleGo.AddComponent<Text>();
            var df = DefaultFont; if (df != null) titleText.font = df;
            titleText.fontSize = fontSize;
            titleText.color = AccentColor;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = title;

            // 底部分隔线
            var divGo = new GameObject("Divider");
            divGo.transform.SetParent(go.transform, false);
            var divRt = divGo.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 0);
            divRt.anchorMax = new Vector2(1, 0);
            divRt.pivot = new Vector2(0.5f, 0);
            divRt.sizeDelta = new Vector2(0, 1);
            divGo.AddComponent<Image>().color = BorderDim;

            return (go, titleText);
        }

        /// <summary>给面板添加四个金色装饰角标（参考暗黑破坏神UI）</summary>
        public static void AddCornerDecorations(RectTransform target, float size = 16f, float offset = 2f)
        {
            if (CornerDecor == null) return;
            // 四个角：左上、右上、左下、右下
            var corners = new (Vector2 anchor, Vector2 pivot, float rot)[]
            {
                (new Vector2(0, 1), new Vector2(0, 1), 0f),      // 左上
                (new Vector2(1, 1), new Vector2(1, 1), -90f),    // 右上
                (new Vector2(0, 0), new Vector2(0, 0), 90f),     // 左下
                (new Vector2(1, 0), new Vector2(1, 0), 180f),    // 右下
            };
            foreach (var (anchor, pivot, rot) in corners)
            {
                var go = new GameObject("CornerDecor");
                go.transform.SetParent(target, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = pivot;
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(
                    anchor.x == 0 ? offset : -offset,
                    anchor.y == 0 ? offset : -offset);
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                var img = go.AddComponent<Image>();
                img.sprite = CornerDecor;
                img.color = BorderGold;
                img.raycastTarget = false;
            }
        }

        /// <summary>创建数值标签（图标+数值，参考魔兽世界属性面板）</summary>
        public static (GameObject go, Text valueText) MakeStatLabel(Transform parent, string name, Sprite icon, string value, int fontSize = 13)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20f);

            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.pivot = new Vector2(0, 0.5f);
                iconRt.sizeDelta = new Vector2(16f, 16f);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
            }

            var valGo = new GameObject("Value");
            valGo.transform.SetParent(go.transform, false);
            var valRt = valGo.AddComponent<RectTransform>();
            valRt.anchorMin = Vector2.zero;
            valRt.anchorMax = Vector2.one;
            valRt.offsetMin = new Vector2(icon != null ? 22f : 4f, 0);
            valRt.offsetMax = Vector2.zero;
            var valText = valGo.AddComponent<Text>();
            var df = DefaultFont; if (df != null) valText.font = df;
            valText.fontSize = fontSize;
            valText.color = TextColor;
            valText.alignment = TextAnchor.MiddleLeft;
            valText.text = value;
            return (go, valText);
        }
    }
}
