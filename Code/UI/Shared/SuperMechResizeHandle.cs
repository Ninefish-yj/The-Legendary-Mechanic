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
            // 创建窗口时先把关闭按钮钉到右上角（原版空窗口预制体的关闭按钮是固定位置，窗口一宽就不在角上）
            FixCloseButton(bgRect);
            return handler;
        }

        /// <summary>
        /// 静态入口：把原版关闭按钮锚定到窗口右上角（幂等，可随时调用）。
        /// v0.239.6 改用原版 ScrollWindow.close_background 引用定位（public字段，预制体序列化），
        /// 不再靠名字/锚点特征猜按钮——原版empty预制体的关闭键在标题栏中间偏右，旧匹配全落空。
        /// </summary>
        public static void FixCloseButton(RectTransform bgRect)
        {
            if (bgRect == null) return;
            // 从 Background 反查所属 ScrollWindow（Background 是窗口根的第一个子物体）
            ScrollWindow sw = bgRect.GetComponentInParent<ScrollWindow>();
            if (sw != null && sw.close_background != null)
            {
                AnchorCloseToTopRight(sw.close_background.GetComponent<RectTransform>());
                return;
            }
            // 兜底：递归遍历 Background 下所有层级，按名称/特征匹配关闭按钮
            foreach (RectTransform r in bgRect.GetComponentsInChildren<RectTransform>(true))
            {
                if (r == null || r == bgRect) continue;
                string n = r.name ?? "";
                bool nameIsClose = n.ToLower().Contains("close") || n.ToLower().Contains("closer") || n == "X";
                bool atTopRight = r.anchorMin.x > 0.5f && r.anchorMin.y > 0.5f
                                  || (r.anchorMin.x == 1f && r.anchorMin.y == 1f)
                                  || (r.localPosition.x > 0f && r.localPosition.y > 0f);
                bool inTitleBar = r.anchorMin.y > 0.5f && r.anchorMax.y >= 0.5f && r.sizeDelta.sqrMagnitude < 10000f;
                if (!nameIsClose && !atTopRight && !inTitleBar) continue;
                AnchorCloseToTopRight(r);
                return;
            }
        }

        /// <summary>把关闭按钮锚定到右上角外部（参考帝国盒子：红叉贴在窗口右上角边框外面）</summary>
        private static void AnchorCloseToTopRight(RectTransform r)
        {
            if (r == null) return;
            r.anchorMin = new Vector2(1f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            // pivot=左下角：按钮左下角对齐窗口右上角点，按钮整体在外部右上方
            r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = new Vector2(-2f, -2f); // 轻微内移2px与边框重叠
            // SetAsLastSibling 让关闭按钮成为父物体最后子物体，始终可见可点
            r.SetAsLastSibling();
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
        /// 拖拽缩放后把原版关闭按钮钉回右上角（防跑位）。
        /// 原版关闭按钮是窗口 Background 的子物体，但空窗口预制体里它的锚点不是右上角，
        /// 窗口尺寸一变它就不在角上，所以每次缩放后重新锚定。
        /// </summary>
        private void FixCloseButton()
        {
            FixCloseButton(TargetRect);
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
