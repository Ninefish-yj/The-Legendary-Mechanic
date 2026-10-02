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
        private static GameObject _blockerPrefab;

        private Image _bgImage;
        private Button _closeBtn;
        private Text _titleText;
        private Vector2 _dragOffset;

        public static Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            // 总是创建独立Canvas，确保sortingOrder最高，不依赖WorldBox的Canvas
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
            window.BuildUI(titleKey, width, height);
            window.IsOpen = true;
            return window;
        }

        private void BuildUI(string titleKey, float w, float h)
        {
            _bgImage = gameObject.AddComponent<Image>();
            _bgImage.sprite = SMUiAssets.GetPanelSprite();
            _bgImage.type = Image.Type.Sliced;
            if (_bgImage.sprite != null)
            {
                _bgImage.fillCenter = true;
            }

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(-120, 36);
            titleRect.anchoredPosition = new Vector2(0, -42);
            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 18;
            _titleText.color = new Color(0.9f, 0.95f, 1f);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.text = LocalizedTextManager.getText(titleKey);
            var titleDrag = titleGo.AddComponent<SMDragHandler>();
            titleDrag.Target = this;

            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.anchoredPosition = new Vector2(-57, -45);
            _closeBtn = closeGo.AddComponent<Button>();
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 20;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = "×";
            _closeBtn.onClick.AddListener(Close);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            Content = contentGo.AddComponent<RectTransform>();
            Content.anchorMin = Vector2.zero;
            Content.anchorMax = Vector2.one;
            Content.pivot = new Vector2(0.5f, 0.5f);
            // 背景图实际边框：左57/下36/右54/上42，标题栏高40，内容留10px边距
            Content.offsetMin = new Vector2(67, 46);
            Content.offsetMax = new Vector2(-64, -92);
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
