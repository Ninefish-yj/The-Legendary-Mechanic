using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMUguiWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
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

        // 布局常量（9-slice边框8px，内容边距12px）
        private const int TitleBarHeight = 36;
        private const int ContentMargin = 12;
        private const int CloseBtnSize = 28;
        private const int CloseBtnMargin = 6;

        public static Canvas GetCanvas()
        {
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

        public static SMUguiWindow Create(string id, string titleKey, float width, float height)
        {
            var canvas = GetCanvas();
            var go = new GameObject($"SM_Window_{id}");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(
                (Screen.width - width) / 2f,
                (Screen.height - height) / 2f);

            var window = go.AddComponent<SMUguiWindow>();
            window.WindowId = id;
            window.Rect = rect;
            window.BuildUI(titleKey);
            window.IsOpen = true;
            return window;
        }

        private void BuildUI(string titleKey)
        {
            // 背景：9-slice面板（深灰蓝底+冰蓝边框+切角）
            _bgImage = gameObject.AddComponent<Image>();
            if (SMUiSkin.Panel != null)
            {
                _bgImage.sprite = SMUiSkin.Panel;
                _bgImage.type = Image.Type.Sliced;
                _bgImage.fillCenter = true;
            }
            else
            {
                _bgImage.color = SMUiSkin.BgColor;
            }

            // 标题栏背景（9-slice）
            var titleBarGo = new GameObject("TitleBar");
            titleBarGo.transform.SetParent(transform, false);
            var titleBarRect = titleBarGo.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, TitleBarHeight);
            titleBarRect.anchoredPosition = new Vector2(0, -4);
            var titleBarImg = titleBarGo.AddComponent<Image>();
            if (SMUiSkin.TitleBar != null)
            {
                titleBarImg.sprite = SMUiSkin.TitleBar;
                titleBarImg.type = Image.Type.Sliced;
            }
            else
            {
                titleBarImg.color = new Color(0.04f, 0.06f, 0.10f, 0.98f);
            }
            var titleDrag = titleBarGo.AddComponent<SMDragHandler>();
            titleDrag.Target = this;

            // 标题文字
            _titleText = SMUiSkin.MakeText(titleBarGo.transform, LocalizedTextManager.getText(titleKey), 16, TextAnchor.MiddleCenter);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = SMUiSkin.AccentColor;
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(CloseBtnSize + CloseBtnMargin * 2, 0);
            titleRect.offsetMax = new Vector2(-(CloseBtnSize + CloseBtnMargin * 2), 0);

            // 关闭按钮（9-slice三态）
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBarGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(CloseBtnSize, CloseBtnSize);
            closeRect.anchoredPosition = new Vector2(-CloseBtnMargin, -4);
            var closeImg = closeGo.AddComponent<Image>();
            if (SMUiSkin.ButtonNormalSprite != null)
            {
                closeImg.sprite = SMUiSkin.ButtonNormalSprite;
                closeImg.type = Image.Type.Sliced;
            }
            else
            {
                closeImg.color = SMUiSkin.CloseRed;
            }
            _closeBtn = closeGo.AddComponent<Button>();
            _closeBtn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = _closeBtn.spriteState;
            spriteState.highlightedSprite = SMUiSkin.ButtonHoverSprite;
            spriteState.pressedSprite = SMUiSkin.ButtonActiveSprite;
            _closeBtn.spriteState = spriteState;
            var closeText = SMUiSkin.MakeText(closeGo.transform, "×", 18, TextAnchor.MiddleCenter);
            closeText.color = SMUiSkin.TextColor;
            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            _closeBtn.onClick.AddListener(Close);

            // 内容区域（避开9-slice边框8px + 内容边距12px）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            Content = contentGo.AddComponent<RectTransform>();
            Content.anchorMin = Vector2.zero;
            Content.anchorMax = Vector2.one;
            Content.pivot = new Vector2(0.5f, 0.5f);
            Content.offsetMin = new Vector2(ContentMargin + 8, ContentMargin + 8);
            Content.offsetMax = new Vector2(-(ContentMargin + 8), -(TitleBarHeight + ContentMargin + 8));
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

        public void Close()
        {
            IsOpen = false;
            Destroy(gameObject);
        }

        public void SetTitle(string text)
        {
            if (_titleText != null) _titleText.text = text;
        }
    }

    public class SMDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public SMUguiWindow Target;

        public void OnPointerDown(PointerEventData e)
        {
            if (Target != null) Target.OnPointerDown(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (Target != null) Target.OnDrag(e);
        }
    }
}
