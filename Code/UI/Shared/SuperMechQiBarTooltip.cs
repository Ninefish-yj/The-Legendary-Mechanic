using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.5 气力条悬停tooltip：显示当前气力/最大气力/等级/恢复速率
    /// 不依赖原版Tooltip asset系统，自绘轻量文本框
    /// </summary>
    public class SuperMechQiBarTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static GameObject _tooltipObj;
        private static Text _tooltipText;
        private static Image _tooltipBg;

        public Actor actor;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (actor == null) return;
            Show(actor);
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

        private static void Show(Actor a)
        {
            EnsureTooltip();
            if (_tooltipObj == null) return;

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);

            // v0.242.8 未觉醒单位（qiMax=0）显示友好提示，而非"0/0 未入流"
            string text;
            if (qiMax <= 0)
            {
                text = LocalizedTextManager.getText("sm_ui_qi_tooltip_title") + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_not_awakened");
            }
            else
            {
                int lv = SuperMechQi.GetLevel(qiMax);
                string lvName = lv > 0 ? SuperMechQi.LevelNames[lv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");

                text = LocalizedTextManager.getText("sm_ui_qi_tooltip_title") + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_current") + ": " + (int)qi + " / " + (int)qiMax + "\n"
                    + LocalizedTextManager.getText("sm_ui_qi_tooltip_level") + ": " + lvName;
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
