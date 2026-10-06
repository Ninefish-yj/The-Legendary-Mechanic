using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SuperMechUguiWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public string WindowId { get; private set; }
        public RectTransform Rect { get; private set; }
        public RectTransform Content { get; private set; }
        public bool IsOpen { get; private set; }

        private static Canvas _canvas;

        private Image _bgImage;
        private Button _closeBtn;
        private Text _titleText;
        private Vector2 _dragOffset;

        private const int TitleBarHeight = 36;
        private const int ContentMargin = 10;
        private const int CloseBtnSize = 26;
        private const int CloseBtnMargin = 6;
        private const int BorderWidth = 2;
        private const int ResizeHandleSize = 16;

        // 伸缩限制
        public Vector2 MinSize = new Vector2(400, 300);
        public Vector2 MaxSize = new Vector2(1200, 800);

        public static Canvas GetCanvas()
        {
            // v0.75.12: Unity销毁对象时==null重载返回true，若缓存canvas已随场景失效则重建
            if (_canvas != null) return _canvas;
            var go = new GameObject("SM_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            go.AddComponent<GraphicRaycaster>();
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            DontDestroyOnLoad(go);
            return _canvas;
        }

        public static SuperMechUguiWindow Create(string id, string titleKey, float width, float height)
        {
            var canvas = GetCanvas();
            var go = new GameObject($"SM_Window_{id}");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(
                (Screen.width - width) / 2f,
                (Screen.height - height) / 2f);

            var window = go.AddComponent<SuperMechUguiWindow>();
            window.WindowId = id;
            window.Rect = rect;
            try
            {
                window.BuildUI(titleKey);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 窗口构建失败(" + id + "): " + e);
                UnityEngine.Object.Destroy(go);
                return null;
            }
            window.IsOpen = true;
            return window;
        }

        private void BuildUI(string titleKey)
        {
            // 背景：纯色半透明深灰蓝
            _bgImage = gameObject.AddComponent<Image>();
            _bgImage.color = SuperMechUiSkin.BgColor;
            _bgImage.raycastTarget = true;

            // 边框：4个冰蓝细边
            AddBorder("TopBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, BorderWidth), new Vector2(0, 0));
            AddBorder("BottomBorder", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, BorderWidth), new Vector2(0, 0));
            AddBorder("LeftBorder", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(BorderWidth, 0), new Vector2(0, 0));
            AddBorder("RightBorder", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(BorderWidth, 0), new Vector2(0, 0));

            // 标题栏背景
            var titleBarGo = new GameObject("TitleBar");
            titleBarGo.transform.SetParent(transform, false);
            var titleBarRect = titleBarGo.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, TitleBarHeight);
            titleBarRect.anchoredPosition = new Vector2(0, -BorderWidth);
            var titleBarImg = titleBarGo.AddComponent<Image>();
            titleBarImg.color = new Color(0.04f, 0.06f, 0.10f, 0.98f);
            var titleDrag = titleBarGo.AddComponent<SuperMechDragHandler>();
            titleDrag.Target = this;

            // 标题文字
            _titleText = SuperMechUiSkin.MakeText(titleBarGo.transform, LocalizedTextManager.getText(titleKey), 16, TextAnchor.MiddleCenter);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = SuperMechUiSkin.AccentColor;
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(CloseBtnSize + CloseBtnMargin * 2, 0);
            titleRect.offsetMax = new Vector2(-(CloseBtnSize + CloseBtnMargin * 2), 0);

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBarGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(CloseBtnSize, CloseBtnSize);
            closeRect.anchoredPosition = new Vector2(-CloseBtnMargin, -5);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = SuperMechUiSkin.CloseRed;
            _closeBtn = closeGo.AddComponent<Button>();
            var closeColors = _closeBtn.colors;
            closeColors.normalColor = Color.white;
            closeColors.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f);
            closeColors.pressedColor = new Color(0.6f, 0.15f, 0.15f, 1f);
            _closeBtn.colors = closeColors;
            var closeText = SuperMechUiSkin.MakeText(closeGo.transform, "×", 18, TextAnchor.MiddleCenter);
            closeText.color = Color.white;
            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            _closeBtn.onClick.AddListener(Close);

            // 内容区域
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            Content = contentGo.AddComponent<RectTransform>();
            Content.anchorMin = Vector2.zero;
            Content.anchorMax = Vector2.one;
            Content.pivot = new Vector2(0.5f, 0.5f);
            Content.offsetMin = new Vector2(ContentMargin + BorderWidth, ContentMargin + BorderWidth);
            Content.offsetMax = new Vector2(-(ContentMargin + BorderWidth), -(TitleBarHeight + ContentMargin + BorderWidth));

            // 右下角伸缩手柄
            var resizeGo = new GameObject("ResizeHandle");
            resizeGo.transform.SetParent(transform, false);
            var resizeRect = resizeGo.AddComponent<RectTransform>();
            resizeRect.anchorMin = new Vector2(1, 0);
            resizeRect.anchorMax = new Vector2(1, 0);
            resizeRect.pivot = new Vector2(1, 0);
            resizeRect.sizeDelta = new Vector2(ResizeHandleSize, ResizeHandleSize);
            resizeRect.anchoredPosition = Vector2.zero;
            var resizeImg = resizeGo.AddComponent<Image>();
            resizeImg.color = new Color(SuperMechUiSkin.AccentColor.r, SuperMechUiSkin.AccentColor.g, SuperMechUiSkin.AccentColor.b, 0.3f);
            var resizeHandler = resizeGo.AddComponent<SuperMechResizeHandler>();
            resizeHandler.Target = this;
            // 伸缩手柄上的斜线纹理（用三条线表示）
            var resizeText = SuperMechUiSkin.MakeText(resizeGo.transform, "⤡", 12, TextAnchor.MiddleCenter);
            resizeText.color = new Color(1, 1, 1, 0.6f);
            var resizeTextRect = resizeText.GetComponent<RectTransform>();
            resizeTextRect.anchorMin = Vector2.zero;
            resizeTextRect.anchorMax = Vector2.one;
            resizeTextRect.offsetMin = Vector2.zero;
            resizeTextRect.offsetMax = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rect, e.position, e.pressEventCamera, out _dragOffset);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rect.parent as RectTransform, e.position, e.pressEventCamera, out var localPos))
            {
                Rect.anchoredPosition = localPos - _dragOffset;
            }
        }

        private void AddBorder(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = SuperMechUiSkin.BorderColor;
        }

        public void Close()
        {
            IsOpen = false;
            Destroy(gameObject);
        }

        public void Resize(Vector2 delta)
        {
            if (Rect == null) return;
            Vector2 newSize = Rect.sizeDelta + delta;
            newSize.x = Mathf.Clamp(newSize.x, MinSize.x, MaxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, MinSize.y, MaxSize.y);
            Rect.sizeDelta = newSize;
        }

        public void SetTitle(string text)
        {
            if (_titleText != null) _titleText.text = text;
        }
    }

    public class SuperMechDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public SuperMechUguiWindow Target;

        public void OnPointerDown(PointerEventData e)
        {
            if (Target != null) Target.OnPointerDown(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (Target != null) Target.OnDrag(e);
        }
    }

    public class SuperMechResizeHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public SuperMechUguiWindow Target;
        private Vector2 _lastMousePos;

        public void OnPointerDown(PointerEventData e)
        {
            _lastMousePos = e.position;
            if (Target != null) Target.transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e)
        {
            if (Target == null) return;
            Vector2 delta = e.position - _lastMousePos;
            _lastMousePos = e.position;
            // 只在右下角拖拽时生效，x增加、y减少（屏幕坐标y向下为正）
            Target.Resize(new Vector2(delta.x, -delta.y));
        }
    }
}
