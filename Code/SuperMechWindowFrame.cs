using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 简化版自绘窗口框架。
    /// 标题栏 + 关闭按钮 + 内容区，可拖拽。
    /// </summary>
    public class SMWindowFrame
    {
        private static readonly List<SMWindowFrame> AllFrames = new List<SMWindowFrame>(8);
        private static Canvas _canvas;

        public GameObject Root;
        public RectTransform RootRt;
        public RectTransform ContentParent;
        public Text TitleText;

        public static SMWindowFrame Create(string title, float width, float height)
        {
            var frame = new SMWindowFrame();
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

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);

            // 标题栏
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(frame.RootRt, false);
            RectTransform tbRt = titleBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.offsetMin = new Vector2(0f, -44f);
            tbRt.offsetMax = Vector2.zero;
            titleBar.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 1f);

            // 拖拽
            var drag = titleBar.AddComponent<SMWindowDrag>();
            drag.WindowRect = frame.RootRt;

            // 标题文字
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(tbRt, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(12f, 0f);
            titleRt.offsetMax = new Vector2(-50f, 0f);
            Text tt = titleGo.GetComponent<Text>();
            frame.TitleText = tt;
            tt.font = LocalizedTextManager.current_font;
            tt.fontSize = 20;
            tt.fontStyle = FontStyle.Bold;
            tt.alignment = TextAnchor.MiddleLeft;
            tt.color = new Color(0.9f, 0.85f, 0.6f, 1f);
            tt.text = title;

            // 关闭按钮
            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(tbRt, false);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-6f, 0f);
            closeRt.sizeDelta = new Vector2(32f, 32f);
            Image closeImg = closeGo.GetComponent<Image>();
            closeImg.color = new Color(0.5f, 0.15f, 0.12f, 0.95f);
            Button closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var closeLabel = new GameObject("X", typeof(RectTransform), typeof(Text));
            closeLabel.transform.SetParent(closeRt, false);
            RectTransform clr = closeLabel.GetComponent<RectTransform>();
            clr.anchorMin = Vector2.zero;
            clr.anchorMax = Vector2.one;
            clr.offsetMin = Vector2.zero;
            clr.offsetMax = Vector2.zero;
            Text cl = closeLabel.GetComponent<Text>();
            cl.font = LocalizedTextManager.current_font;
            cl.fontSize = 18;
            cl.alignment = TextAnchor.MiddleCenter;
            cl.color = Color.white;
            cl.text = "×";
            closeBtn.onClick.AddListener(() => { if (frame.Root != null) frame.Root.SetActive(false); });

            // 内容区
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(frame.RootRt, false);
            frame.ContentParent = contentGo.GetComponent<RectTransform>();
            frame.ContentParent.anchorMin = Vector2.zero;
            frame.ContentParent.anchorMax = Vector2.one;
            frame.ContentParent.offsetMin = new Vector2(8f, 8f);
            frame.ContentParent.offsetMax = new Vector2(-8f, -52f);

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

        /// <summary>在内容区创建一个文字标签。</summary>
        public Text AddLabel(string text, float x, float y, float w, float h, int fontSize = 14, TextAnchor align = TextAnchor.UpperLeft)
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
            t.font = LocalizedTextManager.current_font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>在内容区创建一个按钮。</summary>
        public Button AddButton(string text, float x, float y, float w, float h, Action onClick, Color? bgColor = null)
        {
            var go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(ContentParent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            Image img = go.GetComponent<Image>();
            img.color = bgColor ?? new Color(0.12f, 0.18f, 0.30f, 0.9f);
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(rt, false);
            RectTransform lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            Text lt = labelGo.GetComponent<Text>();
            lt.font = LocalizedTextManager.current_font;
            lt.fontSize = 13;
            lt.alignment = TextAnchor.MiddleCenter;
            lt.color = Color.white;
            lt.text = text;
            lt.horizontalOverflow = HorizontalWrapMode.Overflow;
            btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (Exception e) { Debug.LogError("[超神机械师] 按钮异常: " + e.Message); } });
            return btn;
        }

        /// <summary>清空内容区所有子对象。</summary>
        public void ClearContent()
        {
            if (ContentParent == null) return;
            for (int i = ContentParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(ContentParent.GetChild(i).gameObject);
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

    /// <summary>窗口拖拽组件。</summary>
    public class SMWindowDrag : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, UnityEngine.EventSystems.IDragHandler
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
