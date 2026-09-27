using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMDraggableWindow : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float LongPressThreshold = 0.2f;
        public RectTransform WindowRect;
        private bool _dragging;
        private float _downTime;
        private Vector2 _lastLocalPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downTime = Time.unscaledTime;
            _dragging = false;
            RectTransform parentRt = WindowRect != null ? WindowRect.parent as RectTransform : null;
            if (parentRt != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
            {
                _lastLocalPos = localPos;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (WindowRect == null) return;
            if (!_dragging)
            {
                if (Time.unscaledTime - _downTime < LongPressThreshold) return;
                _dragging = true;
            }
            RectTransform parentRt = WindowRect.parent as RectTransform;
            if (parentRt == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, eventData.position, eventData.pressEventCamera, out Vector2 localNow)) return;
            WindowRect.anchoredPosition += localNow - _lastLocalPos;
            _lastLocalPos = localNow;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
        }
    }
}
