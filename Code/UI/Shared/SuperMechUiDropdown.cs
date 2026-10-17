using System;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.238.0 自绘下拉组件（参考天人武道天榜 Dropdown 做法）
    /// 用 Unity 原生 Dropdown + 手写模板（选项少、不启用滚动条，选项全展开）
    /// 用途：排行榜阶位筛选/排序、后续窗口统一下拉
    /// </summary>
    public static class SuperMechUiDropdown
    {
        /// <summary>
        /// 创建下拉框。options 为选项文案，onChange 回调选项索引（0=第一个）。
        /// </summary>
        public static Dropdown Create(string name, Transform parent, string[] options, Action<int> onChange,
            float width = 120f, float height = 26f)
        {
            const float itemHeight = 24f;
            int optionCount = options != null ? options.Length : 0;
            float templateHeight = Mathf.Max(itemHeight, optionCount * itemHeight + 4f);

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            Image bg = obj.AddComponent<Image>();
            bg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.13f, 0.10f, 0.06f, 0.85f); // 暖棕（对齐原版窗口）
            bg.raycastTarget = true;

            Dropdown dropdown = obj.AddComponent<Dropdown>();
            dropdown.options.Clear();
            for (int i = 0; i < optionCount; i++)
                dropdown.options.Add(new Dropdown.OptionData(options[i]));

            Text label = SuperMechUiSkin.MakeText(obj.transform, "", 10, TextAnchor.MiddleLeft);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 0);
            labelRect.offsetMax = new Vector2(-20, 0);
            label.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            dropdown.captionText = label;

            // 箭头（右侧白色三角，用文本"▼"代替自绘sprite，避免额外资源）
            Text arrow = SuperMechUiSkin.MakeText(obj.transform, "▼", 9, TextAnchor.MiddleRight);
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.offsetMin = new Vector2(-18, 0);
            arrowRect.offsetMax = Vector2.zero;
            arrow.color = new Color(1f, 1f, 1f, 0.85f);
            arrow.raycastTarget = false;

            // Template：从父容器底边向下展开
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(obj.transform, false);
            var tRect = templateObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0);
            tRect.anchorMax = new Vector2(1, 0);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.anchoredPosition = new Vector2(0, -2f);
            tRect.sizeDelta = new Vector2(0, templateHeight);

            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
            templateBg.type = Image.Type.Sliced;
            templateBg.color = new Color(0.10f, 0.08f, 0.05f, 0.98f);

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            var vRect = viewportObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            Image vImg = viewportObj.AddComponent<Image>();
            vImg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
            vImg.color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0f;
            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.content = cRect;
            scrollRect.viewport = vRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Item 模板
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            var iRect = itemObj.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 1);
            iRect.anchorMax = new Vector2(1, 1);
            iRect.pivot = new Vector2(0.5f, 1);
            iRect.sizeDelta = new Vector2(0, itemHeight);
            itemObj.AddComponent<LayoutElement>().preferredHeight = itemHeight;

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.isOn = false;
            itemToggle.toggleTransition = Toggle.ToggleTransition.None;

            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            Image itemBg = itemBgObj.AddComponent<Image>();
            itemBg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
            itemBg.color = Color.white;
            SetFullStretch(itemBgObj);

            ColorBlock colors = itemToggle.colors;
            colors.normalColor = new Color(0.16f, 0.13f, 0.09f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.28f, 0.18f, 1f);
            colors.pressedColor = new Color(0.28f, 0.22f, 0.14f, 1f);
            colors.selectedColor = new Color(0.30f, 0.24f, 0.16f, 1f);
            colors.disabledColor = new Color(0.16f, 0.13f, 0.09f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            itemToggle.colors = colors;
            itemToggle.transition = Selectable.Transition.ColorTint;
            itemToggle.targetGraphic = itemBg;

            Text itemText = SuperMechUiSkin.MakeText(itemObj.transform, "", 10, TextAnchor.MiddleLeft);
            var itRect = itemText.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(8, 0);
            itRect.offsetMax = Vector2.zero;
            itemText.color = new Color(0.95f, 0.92f, 0.85f, 1f);

            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.itemText = itemText;
            // 注意：旧版 Unity（WorldBox 0.51.4）的 Dropdown 无 itemBackground 属性，
            // 选中高亮由 itemToggle.targetGraphic + ColorBlock 负责，无需赋值。

            dropdown.onValueChanged.AddListener(idx => { if (onChange != null) onChange(idx); });
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private static void SetFullStretch(GameObject go)
        {
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
    }
}
