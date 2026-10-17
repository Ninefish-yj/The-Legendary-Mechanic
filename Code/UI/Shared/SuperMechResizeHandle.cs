using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.237.7 窗口右下角拖拽缩放手柄（PC端）。
    /// 原版 WindowCreator 窗口不支持运行时缩放，这里自建手柄：
    /// 拖拽右下角手柄调整窗口 Background 的 sizeDelta，内容区（anchor拉伸）自动跟随。
    /// 手机端不启用（屏幕小，已有92%上限适配）。
    /// </summary>
    public class SuperMechResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>要缩放的窗口背景</summary>
        public RectTransform TargetRect;

        /// <summary>最小窗口尺寸（防止拖没了）</summary>
        public float MinWidth = 320f;
        public float MinHeight = 220f;

        private Vector2 _startSize;
        private Vector2 _startPointer;
        private Image _img;
        private Color _normal = new Color(0.90f, 0.90f, 0.90f, 0.55f);
        private Color _hover = new Color(0.40f, 0.80f, 0.90f, 0.90f);

        public static SuperMechResizeHandle Add(RectTransform bgRect)
        {
            var handleGo = new GameObject("SM_ResizeHandle");
            handleGo.transform.SetParent(bgRect, false);
            var hRect = handleGo.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(1f, 0f);
            hRect.anchorMax = new Vector2(1f, 0f);
            hRect.pivot = new Vector2(1f, 0f);
            hRect.anchoredPosition = new Vector2(0f, 0f);
            hRect.sizeDelta = new Vector2(22f, 22f);

            var img = handleGo.AddComponent<Image>();
            img.color = new Color(0.90f, 0.90f, 0.90f, 0.55f);
            img.raycastTarget = true;

            // 右下三角角标文字（视觉提示可拖拽）
            var markGo = new GameObject("Mark");
            markGo.transform.SetParent(handleGo.transform, false);
            var markRect = markGo.AddComponent<RectTransform>();
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.one;
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            var markText = markGo.AddComponent<Text>();
            markText.font = SuperMechUiSkin.DefaultFont;
            markText.fontSize = 12;
            markText.color = new Color(0.15f, 0.12f, 0.08f, 0.8f);
            markText.alignment = TextAnchor.LowerRight;
            markText.text = "◢";
            markText.raycastTarget = false;

            var handler = handleGo.AddComponent<SuperMechResizeHandle>();
            handler.TargetRect = bgRect;
            handler._img = img;
            return handler;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (TargetRect == null) return;
            _startSize = TargetRect.sizeDelta;
            _startPointer = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TargetRect == null) return;
            Vector2 delta = eventData.position - _startPointer;
            float newW = Mathf.Max(MinWidth, _startSize.x + delta.x);
            float newH = Mathf.Max(MinHeight, _startSize.y + delta.y);
            TargetRect.sizeDelta = new Vector2(newW, newH);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_img != null) _img.color = _hover;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_img != null) _img.color = _normal;
        }
    }
}
