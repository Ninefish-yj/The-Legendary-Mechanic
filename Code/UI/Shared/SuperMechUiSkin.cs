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

        // ═════════════════════════ v0.240.6 帝国盒子暖色调（对齐原版窗口纹理） ═════════════════════════
        // 设计原则：深棕灰底 + 金色主色 + 米黄文字 + 红金按钮，与原版帝国盒子窗口视觉统一
        // 参考：WorldBox 原版 ScrollWindow（棕纸纹理 + 金色边框 + 米黄文字）
        public static readonly Color BgColor = new Color(0.12f, 0.10f, 0.07f, 0.97f);       // 面板底 深棕灰
        public static readonly Color BgLight = new Color(0.16f, 0.13f, 0.09f, 0.96f);       // 次级面板 稍浅棕
        public static readonly Color BorderColor = new Color(0.45f, 0.35f, 0.20f, 0.6f);    // 边框 淡金棕
        public static readonly Color BorderDim = new Color(0.28f, 0.22f, 0.13f, 0.5f);      // 暗边框
        public static readonly Color BorderGold = new Color(0.85f, 0.68f, 0.35f, 0.7f);     // 金色边框
        public static readonly Color TextColor = new Color(0.92f, 0.85f, 0.65f, 1f);        // 正文 米黄
        public static readonly Color TextDim = new Color(0.65f, 0.58f, 0.42f, 1f);          // 次要文字 暗金
        public static readonly Color AccentColor = new Color(0.85f, 0.68f, 0.35f, 1f);      // 主色 金色（按钮/链接/强调）
        public static readonly Color AccentDim = new Color(0.60f, 0.48f, 0.25f, 0.8f);      // 暗金
        public static readonly Color CyanColor = new Color(0.95f, 0.80f, 0.45f, 1f);        // 高亮 亮金（数值/进度）
        public static readonly Color CyanDim = new Color(0.50f, 0.40f, 0.20f, 0.8f);        // 暗金
        public static readonly Color ButtonNormal = new Color(0.22f, 0.16f, 0.09f, 0.95f);  // 按钮常态 深棕
        public static readonly Color ButtonHover = new Color(0.30f, 0.22f, 0.13f, 0.98f);   // 按钮悬停 稍浅棕
        public static readonly Color ButtonActive = new Color(0.16f, 0.12f, 0.07f, 0.98f);  // 按钮按下 更深棕
        public static readonly Color EdgeLight = new Color(0.55f, 0.42f, 0.25f, 0.3f);      // 顶高光 淡金
        public static readonly Color EdgeDark = new Color(0.06f, 0.05f, 0.03f, 0.5f);       // 底阴影
        public static readonly Color RowEven = new Color(0.15f, 0.12f, 0.08f, 0.5f);        // 偶数行 棕
        public static readonly Color RowOdd = new Color(0.11f, 0.09f, 0.06f, 0.3f);         // 奇数行 深棕
        public static readonly Color CardBg = new Color(0.16f, 0.13f, 0.08f, 0.95f);        // 卡片底 深棕
        // v0.237.6 弹窗内容区暖棕半透明：透出原版窗口棕纸纹理，视觉与原版协调（参考帝国盒子）
        public static readonly Color VanillaCard = new Color(0.16f, 0.12f, 0.07f, 0.42f);    // 信息行卡片
        public static readonly Color VanillaSection = new Color(0.13f, 0.10f, 0.06f, 0.55f); // 分区标题条
        // v0.237.12 弹窗主面板暖棕（替代烘焙深蓝的Panel sprite，对齐帝国盒子原版棕纸窗口）
        // v0.238.4 暖棕实色化：alpha 0.58→0.95、提亮至棕纸色，确保真机一眼可见（此前半透明叠深蓝底看不出）
        public static readonly Color PanelVanilla = new Color(0.32f, 0.23f, 0.14f, 0.95f); // 弹窗面板底
        public static readonly Color SectionBg = new Color(0.20f, 0.15f, 0.09f, 0.96f);     // 分区底 深棕
        public static readonly Color CloseRed = new Color(0.85f, 0.35f, 0.32f, 0.95f);      // 关闭按钮红
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
                // v0.240.8 用原版Sprite纹理（参考天人武道）：ui/special/backgroundKingdomElement + darkInputFieldEmpty
                // 所有面板/按钮/卡片用原版9-slice纹理，Image.Type.Sliced拉伸，视觉与帝国盒子完全一致
                Sprite vanillaPanel = LoadVanillaSprite("ui/special/backgroundKingdomElement");
                Sprite vanillaField = LoadVanillaSprite("ui/special/darkInputFieldEmpty");

                _panelSprite = vanillaPanel ?? MakeRoundedGlowSprite(BgColor, BorderColor, null, 64, 10, 1, "SM_Panel");
                // 按钮统一用原版面板纹理，靠Image.color和ColorTint区分状态
                _buttonNormal = vanillaPanel ?? MakeRoundedGlowSprite(ButtonNormal, BorderDim, null, 32, 8, 0, "SM_BtnNormal");
                _buttonHover = vanillaPanel ?? MakeRoundedGlowSprite(ButtonHover, AccentColor, null, 32, 8, 0, "SM_BtnHover");
                _buttonActive = vanillaPanel ?? MakeRoundedGlowSprite(ButtonActive, CyanDim, null, 32, 8, 0, "SM_BtnActive");
                _buttonSelected = vanillaPanel ?? MakeRoundedGlowSprite(new Color(0.40f, 0.30f, 0.15f, 0.95f), CyanColor, null, 32, 8, 0, "SM_BtnSelected");
                _buttonDisabled = vanillaPanel ?? MakeRoundedGlowSprite(new Color(0.10f, 0.08f, 0.05f, 0.8f), BorderDim, null, 32, 8, 0, "SM_BtnDisabled");
                _titleBarSprite = vanillaPanel ?? MakeRoundedGlowSprite(new Color(0.20f, 0.15f, 0.09f, 0.98f), CyanDim, null, 32, 8, 1, "SM_TitleBar");
                _scrollbarSprite = MakeSolidSprite(new Color(0.70f, 0.55f, 0.30f, 0.6f), "SM_Scrollbar");
                _rowEven = MakeSolidSprite(RowEven, "SM_RowEven");
                _rowOdd = MakeSolidSprite(RowOdd, "SM_RowOdd");
                _cardSprite = vanillaPanel ?? MakeRoundedGlowSprite(CardBg, BorderDim, null, 32, 8, 1, "SM_Card");
                _cardHover = vanillaPanel ?? MakeRoundedGlowSprite(new Color(0.25f, 0.19f, 0.11f, 0.98f), BorderColor, null, 32, 8, 1, "SM_CardHover");
                _sectionSprite = vanillaField ?? MakeRoundedGlowSprite(SectionBg, BorderDim, null, 32, 8, 1, "SM_Section");
                _dividerSprite = MakeSolidSprite(new Color(0.45f, 0.35f, 0.20f, 0.5f), "SM_Divider");
                _glowSprite = MakeGlowSprite(CyanColor, "SM_Glow");
                _iconCircle = MakeCircleSprite(new Color(0.12f, 0.10f, 0.07f, 0.95f), BorderColor, 32, 2, "SM_IconCircle");
                _iconHex = MakeHexSprite(new Color(0.12f, 0.10f, 0.07f, 0.95f), AccentColor, 32, 2, "SM_IconHex");
                _progressBg = vanillaField ?? MakeRoundedGlowSprite(new Color(0.08f, 0.06f, 0.04f, 0.9f), BorderDim, null, 16, 3, 0, "SM_ProgressBg");
                _progressFill = MakeRoundedGlowSprite(new Color(0.75f, 0.60f, 0.30f, 1f), CyanColor, null, 16, 3, 0, "SM_ProgressFill");
                _cornerDecor = MakeCornerDecorSprite(BorderGold, "SM_Corner");
                _tabNormal = vanillaPanel ?? MakeButtonSprite(new Color(0.14f, 0.11f, 0.07f, 0.9f), new Color(0.09f, 0.07f, 0.05f, 0.9f), BorderDim, null, null, 24, 2, "SM_TabNormal");
                _tabActive = vanillaPanel ?? MakeButtonSprite(new Color(0.30f, 0.22f, 0.12f, 1f), new Color(0.18f, 0.14f, 0.08f, 1f), AccentColor, new Color(0.75f, 0.60f, 0.35f, 0.8f), null, 24, 2, "SM_TabActive");
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

        /// <summary>加载原版Sprite纹理，失败返回null（调用方fallback到自绘Sprite）</summary>
        private static Sprite LoadVanillaSprite(string path)
        {
            try { return SpriteTextureLoader.getSprite(path); }
            catch { return null; }
        }

    }
}
