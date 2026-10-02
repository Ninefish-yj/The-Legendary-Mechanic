using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMDraggableButton : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public string ButtonId { get; private set; }
        public Action OnClick { get; set; }
        public RectTransform Rect { get; private set; }

        private static Canvas _canvas;
        private Vector2 _dragOffset;
        private bool _wasDragged;
        private float _pressTime;

        public static SMDraggableButton Create(string id, string labelKey, string iconPath, float x, float y)
        {
            var canvas = SMUguiWindow.GetCanvas();
            var go = new GameObject($"SM_Button_{id}");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64);
            rect.anchoredPosition = new Vector2(x, y);

            var btn = go.AddComponent<SMDraggableButton>();
            btn.ButtonId = id;
            btn.Rect = rect;
            btn.BuildUI(labelKey, iconPath);
            return btn;
        }

        private void BuildUI(string labelKey, string iconPath)
        {
            var img = gameObject.AddComponent<Image>();
            img.sprite = SMUiSkin.Panel;
            img.type = Image.Type.Sliced;

            if (!string.IsNullOrEmpty(iconPath))
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(36, 36);
                iconRect.anchoredPosition = new Vector2(0, 4);
                var iconImg = iconGo.AddComponent<Image>();
                // 用原版图标加载方式
                var sprite = SpriteTextureLoader.getSprite(iconPath);
                if (sprite != null)
                {
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = new Color(0.5f, 0.7f, 1f);
                }
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.pivot = new Vector2(0.5f, 0);
            labelRect.sizeDelta = new Vector2(0, 16);
            labelRect.anchoredPosition = new Vector2(0, 2);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 11;
            labelText.color = new Color(0.8f, 0.9f, 1f);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = LocalizedTextManager.getText(labelKey);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _wasDragged = false;
            _pressTime = Time.time;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rect, e.position, e.pressEventCamera, out _dragOffset);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData e)
        {
            _wasDragged = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Rect.parent as RectTransform, e.position, e.pressEventCamera, out var localPos))
            {
                Rect.anchoredPosition = localPos - _dragOffset;
            }
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!_wasDragged && Time.time - _pressTime < 0.5f)
            {
                OnClick?.Invoke();
            }
        }
    }
}
