using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.5 气力条悬停tooltip：显示当前气力/最大气力/等级
    /// v0.242.9 重构：不存Actor引用，直接存气力值（qi/qiMax/levelName），
    ///   由UpdateQiBarValue在更新条时写入，悬停时直接显示，彻底消除actor引用失效导致的0/0问题
    /// 不依赖原版Tooltip asset系统，自绘轻量文本框
    /// </summary>
    public class SuperMechQiBarTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static GameObject _tooltipObj;
        private static Text _tooltipText;
        private static Image _tooltipBg;

        // v0.242.9 直接存储显示数据，由UpdateQiBarValue写入
        public float qi;
        public float qiMax;
        public string levelName = "";
        public bool awakened = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private static void EnsureTooltip()
        {
            if (_tooltipObj != null) return;

            _tooltipObj = new GameObject("SM_QiTooltip", typeof(RectTransform), typeof(Image));
            _tooltipObj.transform.SetParent(GameObject.Find("Canvas")?.transform, false);
            if (_tooltipObj.transform.parent == null)
            {
                Canvas canvas = Object.FindObjectOfType<Canvas>();
                if (canvas != null) _tooltipObj.transform.SetParent(canvas.transform, false);
            }
            _tooltipObj.SetActive(false);

            _tooltipBg = _tooltipObj.GetComponent<Image>();
            _tooltipBg.sprite = SuperMechUiSkin.Panel;
            _tooltipBg.type = Image.Type.Sliced;
            _tooltipBg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
            _tooltipBg.raycastTarget = false;

            var rt = _tooltipObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(200, 80);

            _tooltipText = SuperMechUiSkin.MakeText(_tooltipObj.transform, "", 11, TextAnchor.UpperLeft);
            var tr = _tooltipText.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(8, 6);
            tr.offsetMax = new Vector2(-8, -6);
            _tooltipText.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>
        /// v0.242.9 直接用存储的数据显示，不再查询SuperMechQi
        /// </summary>
        private void Show()
        {
            EnsureTooltip();
            if (_tooltipObj == null) return;

            string text;
            if (!awakened || qiMax <= 0)
            {
                text = LocalizedTextManager.getText("sm_ui_qi_tooltip_title") + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_not_awakened");
            }
            else
            {
                text = LocalizedTextManager.getText("sm_ui_qi_tooltip_title") + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_current") + ": " + (int)qi + " / " + (int)qiMax + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_level") + ": " + levelName;
            }

            _tooltipText.text = text;
            _tooltipObj.SetActive(true);

            // 位置：鼠标右上角
            Vector3 mousePos = Input.mousePosition;
            var rt = _tooltipObj.GetComponent<RectTransform>();
            float x = Mathf.Min(mousePos.x + 12, Screen.width - 210);
            float y = Mathf.Max(mousePos.y - 12, 90);
            rt.position = new Vector3(x, y, 0);

            // 自适应高度
            float preferredHeight = _tooltipText.preferredHeight + 12;
            rt.sizeDelta = new Vector2(200, Mathf.Max(60, preferredHeight));
        }

        private static void Hide()
        {
            if (_tooltipObj != null) _tooltipObj.SetActive(false);
        }
    }
}
