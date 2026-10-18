using System;
using UnityEngine.UI;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public class SuperMechWindowFrameInner
    {
        private static readonly List<SuperMechWindowFrameInner> AllFrames = new List<SuperMechWindowFrameInner>(8);
        private static Canvas _canvas;

        public GameObject Root;
        public RectTransform RootRt;
        public RectTransform ContentParent;
        public Text TitleText;

        public static SuperMechWindowFrameInner Create(string title, float width, float height)
        {
            var frame = new SuperMechWindowFrameInner();
            AllFrames.Add(frame);
            Canvas canvas = GetCanvas();

            var root = new GameObject("SMWindow_" + title, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            frame.Root = root;
            frame.RootRt = root.GetComponent<RectTransform>();
            frame.RootRt.anchorMin = new Vector2(0.5f, 0.5f);
            frame.RootRt.anchorMax = new Vector2(0.5f, 0.5f);
            frame.RootRt.pivot = new Vector2(0.5f, 0.5f);
            frame.RootRt.sizeDelta = new Vector2(width, height);
            frame.RootRt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-200f, 200f), UnityEngine.Random.Range(-100f, 100f));

            // v0.194 科幻风格：带边框面板背景
            Image bg = root.GetComponent<Image>();
            bg.sprite = null;  // v0.237.12 弹窗面板暖棕
            bg.type = Image.Type.Simple;
            bg.color = SuperMechUiSkin.PanelVanilla;

            // 标题栏
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(frame.RootRt, false);
            RectTransform tbRt = titleBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.offsetMin = new Vector2(0f, -44f);
            tbRt.offsetMax = Vector2.zero;
            Image tbImg = titleBar.GetComponent<Image>();
            tbImg.sprite = SuperMechUiSkin.TitleBar;
            tbImg.type = Image.Type.Sliced;
            tbImg.color = Color.white;

            // 标题栏底部金色分隔线
            var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(tbRt, false);
            RectTransform divRt = dividerGo.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 0);
            divRt.anchorMax = new Vector2(1, 0);
            divRt.pivot = new Vector2(0.5f, 0);
            divRt.sizeDelta = new Vector2(0, 1);
            divRt.anchoredPosition = Vector2.zero;
            dividerGo.GetComponent<Image>().color = SuperMechUiSkin.CyanDim;

            var drag = titleBar.AddComponent<SuperMechWindowDrag>();
            drag.WindowRect = frame.RootRt;

            // 标题文字（金色）
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(tbRt, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-50f, 0f);
            Text tt = titleGo.GetComponent<Text>();
            frame.TitleText = tt;
            tt.font = SafeFont();
            tt.fontSize = 20;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleLeft;
            tt.color = SuperMechUiSkin.CyanColor;
            tt.text = title;
            tt.horizontalOverflow = HorizontalWrapMode.Wrap;
            tt.verticalOverflow = VerticalWrapMode.Truncate;

            // 关闭按钮（红色+悬停效果）
            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(tbRt, false);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-8f, 0f);
            closeRt.sizeDelta = new Vector2(30f, 30f);
            Image closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            closeImg.type = Image.Type.Sliced;
            closeImg.color = SuperMechUiSkin.CloseRed;
            Button closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.transition = Selectable.Transition.ColorTint;
            var closeColors = closeBtn.colors;
            closeColors.normalColor = Color.white;
            closeColors.highlightedColor = new Color(1.2f, 0.8f, 0.8f, 1f);
            closeColors.pressedColor = new Color(0.7f, 0.5f, 0.5f, 1f);
            closeBtn.colors = closeColors;
            var closeLabel = new GameObject("X", typeof(RectTransform), typeof(Text));
            closeLabel.transform.SetParent(closeRt, false);
            RectTransform clr = closeLabel.GetComponent<RectTransform>();
            clr.anchorMin = Vector2.zero;
            clr.anchorMax = Vector2.one;
            clr.offsetMin = Vector2.zero;
            clr.offsetMax = Vector2.zero;
            Text cl = closeLabel.GetComponent<Text>();
            cl.font = SafeFont();
            cl.fontSize = 18;
            cl.fontStyle = FontStyle.Bold;
            cl.alignment = TextAnchor.MiddleCenter;
            cl.color = Color.white;
            cl.text = "×";
            closeBtn.onClick.AddListener(() => { if (frame.Root != null) frame.Root.SetActive(false); });

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(frame.RootRt, false);
            frame.ContentParent = contentGo.GetComponent<RectTransform>();
            frame.ContentParent.anchorMin = Vector2.zero;
            frame.ContentParent.anchorMax = Vector2.one;
            frame.ContentParent.offsetMin = new Vector2(10f, 10f);
            frame.ContentParent.offsetMax = new Vector2(-10f, -54f);

            // v0.202 四个金色装饰角标（参考暗黑破坏神UI）

            root.SetActive(false);
            return frame;
        }

        public void Show()
        {
            if (Root != null) Root.SetActive(true);
            Root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (Root != null) Root.SetActive(false);
        }

        public bool IsVisible => Root != null && Root.activeSelf;

        public Text AddLabel(string text, float x, float y, float w, float h, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(ContentParent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            Text t = go.GetComponent<Text>();
            t.font = SafeFont();
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color ?? SuperMechUiSkin.TextColor;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public Button AddButton(string text, float x, float y, float w, float h, Action onClick, Color? bgColor = null)
        {
            // v0.194 按钮最小高度44px（手机触屏标准）
            float btnH = Mathf.Max(h, 44f);
            var go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(ContentParent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, btnH);
            Image img = go.GetComponent<Image>();
            img.sprite = SuperMechUiSkin.ButtonNormalSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor ?? Color.white;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = SuperMechUiSkin.ButtonHoverSprite;
            ss.pressedSprite = SuperMechUiSkin.ButtonActiveSprite;
            btn.spriteState = ss;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(rt, false);
            RectTransform lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(8, 4);
            lr.offsetMax = new Vector2(-8, -4);
            Text lt = labelGo.GetComponent<Text>();
            lt.font = SafeFont();
            lt.fontSize = 14;
            lt.fontStyle = FontStyle.Bold;
            lt.alignment = TextAnchor.MiddleCenter;
            lt.color = SuperMechUiSkin.TextColor; // 米黄，帝国盒子暖色调
            lt.text = text;
            lt.horizontalOverflow = HorizontalWrapMode.Wrap;
            lt.verticalOverflow = VerticalWrapMode.Truncate;
            lt.raycastTarget = false;
            btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (Exception e) { Debug.LogError("[超神机械师] 按钮异常: " + e.Message); } });
            return btn;
        }

        private static Font SafeFont()
        {
            Font f = LocalizedTextManager.current_font;
            if (f == null) f = SuperMechUiSkin.DefaultFont;
            return f;
        }

        public void ClearContent()
        {
            if (ContentParent == null) return;
            for (int i = ContentParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(ContentParent.GetChild(i).gameObject);
            }
        }

        public static void ClearAll()
        {
            foreach (var frame in AllFrames)
            {
                if (frame.Root != null) UnityEngine.Object.DestroyImmediate(frame.Root);
            }
            AllFrames.Clear();
            if (_canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
                _canvas = null;
            }
        }

        private static Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            var go = new GameObject("SMCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 9999;
            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            _canvas = c;
            return c;
        }
    }

    public class SuperMechWindowDrag : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IDragHandler
    {
        public RectTransform WindowRect;
        private Vector2 _offset;

        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e)
        {
            if (WindowRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(WindowRect, e.position, e.pressEventCamera, out _offset);
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData e)
        {
            if (WindowRect == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(WindowRect.parent as RectTransform, e.position, e.pressEventCamera, out Vector2 local))
            {
                WindowRect.anchoredPosition = local - _offset;
            }
        }
    }
}
