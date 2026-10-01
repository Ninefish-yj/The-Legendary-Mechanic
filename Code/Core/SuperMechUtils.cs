using UnityEngine;
using UnityEngine.UI;
using NeoModLoader.services;

namespace SuperMech.Code
{
    public static class SuperMechUtils
    {
        public static Actor GetActor(UnitWindow window)
        {
            if (SelectedUnit.unit != null && SelectedUnit.unit.isAlive()) return SelectedUnit.unit;
            if (window != null && window.actor != null) return window.actor;
            return null;
        }

        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (LocalizedTextManager.current_font != null) txt.font = LocalizedTextManager.current_font;
            return txt;
        }

        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            return CreateText(parent, content, fontSize, anchor, Color.white);
        }
    }
}
