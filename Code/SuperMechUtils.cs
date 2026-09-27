using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static class SuperMechUtils
    {
        public static Actor GetActor(UnitWindow window)
        {
            try
            {
                var prop = typeof(UnitWindow).GetProperty("actor",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (prop != null) return prop.GetValue(window) as Actor;
                var field = typeof(UnitWindow).GetField("actor",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                return field?.GetValue(window) as Actor;
            }
            catch { return null; }
        }

        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
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
