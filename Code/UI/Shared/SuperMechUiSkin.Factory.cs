using System;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>UI皮肤 - 组件工厂部分（MakeText/MakeButton/MakeListItem/MakeCard/MakeProgressBar/MakeSectionHeader等）</summary>
    public static partial class SuperMechUiSkin
    {
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

        /// <summary>创建按钮（三态SpriteSwap + hover文字变金 + 立体描边，默认正文14号字）</summary>
        public static Button MakeButton(Transform parent, string text, int fontSize = 14, System.Action onClick = null)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = ButtonNormalSprite;
            img.type = Image.Type.Sliced;  // v0.240.9 原版9-slice纹理必须Sliced
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = btn.spriteState;
            spriteState.highlightedSprite = ButtonHoverSprite;
            spriteState.pressedSprite = ButtonActiveSprite;
            btn.spriteState = spriteState;
            var btnText = MakeText(go.transform, text, fontSize, TextAnchor.MiddleCenter);
            btnText.color = TextColor;
            // hover时文字变金色，增强交互反馈
            var trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => btnText.color = AccentColor);
            trigger.triggers.Add(enterEntry);
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => btnText.color = TextColor);
            trigger.triggers.Add(exitEntry);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>创建列表项（支持hover高亮 + 选中态左侧金色条）</summary>
        public static (GameObject go, Image bgImg, Image selectBar, Text labelText) MakeListItem(Transform parent, string text, int fontSize = 13, float height = 40f)
        {
            var go = new GameObject("ListItem");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);

            // 背景（hover时变亮）
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = Card;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1, 1, 1, 0.6f);

            // 选中态左侧金色条（默认隐藏）
            var barGo = new GameObject("SelectBar");
            barGo.transform.SetParent(go.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f);
            barRt.anchorMax = new Vector2(0, 0.5f);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.sizeDelta = new Vector2(3f, height - 8f);
            barRt.anchoredPosition = new Vector2(2f, 0);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = AccentColor;
            barGo.SetActive(false);

            // 文字
            var labelText = MakeText(go.transform, text, fontSize, TextAnchor.MiddleLeft);
            var labelRt = labelText.GetComponent<RectTransform>();
            labelRt.offsetMin = new Vector2(12f, 0);
            labelRt.offsetMax = new Vector2(-8f, 0);

            // hover效果
            var trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => bgImg.color = new Color(1, 1, 1, 1f));
            trigger.triggers.Add(enterEntry);
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => bgImg.color = new Color(1, 1, 1, 0.6f));
            trigger.triggers.Add(exitEntry);

            return (go, bgImg, barImg, labelText);
        }

        /// <summary>给面板添加外阴影（四向渐变阴影，提升立体感）</summary>
        public static void AddPanelShadow(RectTransform target, float size = 8f)
        {
            // 底部阴影
            var shadowGo = new GameObject("PanelShadow");
            shadowGo.transform.SetParent(target, false);
            shadowGo.transform.SetAsFirstSibling(); // 放在面板后面
            var shadowRt = shadowGo.AddComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(-size, -size);
            shadowRt.offsetMax = new Vector2(size, size);
            var shadowImg = shadowGo.AddComponent<Image>();
            shadowImg.sprite = MakeShadowSprite(size);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0, 0, 0, 0.5f);
            shadowImg.raycastTarget = false;
        }

        private static Sprite _shadowSprite;
        private static Sprite _whiteSprite;

        /// <summary>v0.238.0 白色1×1 Sprite（下拉/遮罩等纯色Image用，Simple与Sliced通用）</summary>
        public static Sprite GetWhiteSpriteSafe()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0u,
                SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            _whiteSprite.name = "SM_White";
            return _whiteSprite;
        }

        private static Sprite MakeShadowSprite(float size)
        {
            if (_shadowSprite != null) return _shadowSprite;
            int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float edgeDist = Mathf.Min(x, y, s - 1 - x, s - 1 - y);
                    float alpha = Mathf.Clamp01(edgeDist / 6f) * 0.6f;
                    pixels[y * s + x] = new Color(0, 0, 0, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _shadowSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
            _shadowSprite.name = "SM_Shadow";
            return _shadowSprite;
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
            // v0.242.8 显式添加RectTransform：不依赖ScrollRect的[RequireComponent]运行时自动添加
            var scrollGo = new GameObject(name, typeof(RectTransform));
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

            // 左侧青色装饰条
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(go.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f);
            barRt.anchorMax = new Vector2(0, 0.5f);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.sizeDelta = new Vector2(3f, 16f);
            barRt.anchoredPosition = new Vector2(0, 0);
            barGo.AddComponent<Image>().color = CyanColor;

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
            titleText.color = CyanColor;
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
