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
            // v0.237.8 缩放后把原版关闭按钮钉回右上角（防跑位）
            FixCloseButton();
        }

        /// <summary>
        /// 原版关闭按钮是窗口 Background 的子物体。正常情况下它锚定在右上角、会随缩放自动跟随；
        /// 这里做幂等保障：若发现右上角有带Button的关闭按钮（名字含close，或位于右上区域），
        /// 强制把它锚定到右上角，确保窗口放大/缩小后关闭按钮始终在角上。
        /// </summary>
        private void FixCloseButton()
        {
            if (TargetRect == null) return;
            foreach (Transform child in TargetRect)
            {
                if (child == null || child.GetComponent<Button>() == null) continue;
                string n = child.name ?? "";
                bool nameIsClose = n.ToLower().Contains("close") || n.ToLower().Contains("closer") || n == "X";
                RectTransform r = child as RectTransform;
                if (r == null) continue;
                // 位置特征：当前锚点/本地坐标在右上区域
                bool atTopRight = r.anchorMin.x > 0.5f && r.anchorMin.y > 0.5f
                                  || (r.anchorMin.x == 1f && r.anchorMin.y == 1f)
                                  || (r.localPosition.x > 0f && r.localPosition.y > 0f);
                if (!nameIsClose && !atTopRight) continue;
                // 锚定到右上角，保持原偏移量（按钮中心距右上角约半宽+边距）
                r.anchorMin = new Vector2(1f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(-(r.sizeDelta.x * 0.5f + 6f), -(r.sizeDelta.y * 0.5f + 6f));
            }
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
