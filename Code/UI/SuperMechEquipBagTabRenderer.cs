using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static partial class SuperMechEquipBagTab
    {
        private static MethodInfo _createMethod;
        private static FieldInfo _isEditorButtonField;

        private static void RenderBag(Actor actor)
        {
            if (_container == null || actor == null) return;

            ClearButtons();

            foreach (Transform child in _container.transform)
            {
                if (child.name != "LayoutGroup") Object.Destroy(child.gameObject);
            }

            var equippedBox = CreateCategoryBox(_container.transform, LocalizedTextManager.getText("sm_ui_equipped"), null);
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                var cur = SuperMechRelic.Equipments[currentIdx];
                Color qColor = GetQualityColor(cur.qualityLevel);

                var equipSlot = CreateItemIcon(equippedBox, cur.icon, qColor, 56, cur.name,
                    $"{LocalizedTextManager.getText("sm_ui_quality")}: {GetQualityName(cur.qualityLevel)}\n{LocalizedTextManager.getText("sm_ui_damage")}×{cur.dmgMul}  {LocalizedTextManager.getText("sm_ui_health")}×{cur.hpMul}\n{LocalizedTextManager.getText("sm_ui_unequip")}");
                var btn = equipSlot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        SuperMechEquipBag.UnequipToBag(actor);
                        RenderBag(actor);
                    });
                }
            }
            else
            {
                CreateEmptySlot(equippedBox, 56, LocalizedTextManager.getText("sm_ui_unequipped"));
            }

            var bag = SuperMechEquipBag.GetBag(actor);
            int bagCount = bag != null ? bag.Count : 0;

            var bagBox = CreateCategoryBox(_container.transform, LocalizedTextManager.getText("sm_ui_bag"), new Color(0.5f, 0.55f, 0.65f), bagCount);

            GameObject gridGo = new GameObject("BagGrid", typeof(RectTransform));
            gridGo.transform.SetParent(bagBox, false);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;

            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(44, 44);
            grid.spacing = new Vector2(6, 6);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            ContentSizeFitter gridFitter = gridGo.AddComponent<ContentSizeFitter>();
            gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (bagCount == 0)
            {
                var emptyText = SuperMechUtils.CreateText(gridGo.transform, LocalizedTextManager.getText("sm_ui_none_dash"), 12, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f, 0.6f));
                var emptyRt = emptyText.GetComponent<RectTransform>();
                emptyRt.anchorMin = Vector2.zero;
                emptyRt.anchorMax = Vector2.one;
                emptyRt.offsetMin = Vector2.zero;
                emptyRt.offsetMax = Vector2.zero;
                LayoutElement emptyLe = emptyText.AddComponent<LayoutElement>();
                emptyLe.minWidth = 200;
                emptyLe.minHeight = 40;
                return;
            }

            var sortedBag = new List<string>(bag);
            sortedBag.Sort((a, b) =>
            {
                int idxA = SuperMechRelic.GetEquipIndex(a);
                int idxB = SuperMechRelic.GetEquipIndex(b);
                int qA = idxA >= 0 ? SuperMechRelic.Equipments[idxA].qualityLevel : 0;
                int qB = idxB >= 0 ? SuperMechRelic.Equipments[idxB].qualityLevel : 0;
                return qB.CompareTo(qA);
            });

            foreach (string equipId in sortedBag)
            {
                int idx = SuperMechRelic.GetEquipIndex(equipId);
                if (idx < 0) continue;
                var def = SuperMechRelic.Equipments[idx];
                Color qColor = GetQualityColor(def.qualityLevel);

                var iconGo = CreateItemIcon(gridGo.transform, def.icon, qColor, 40, def.name,
                    $"{LocalizedTextManager.getText("sm_ui_quality")}: {GetQualityName(def.qualityLevel)}\n{LocalizedTextManager.getText("sm_ui_damage")}×{def.dmgMul}  {LocalizedTextManager.getText("sm_ui_health")}×{def.hpMul}\n{LocalizedTextManager.getText("sm_ui_equip")}");
                var btn = iconGo.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        SuperMechEquipBag.EquipFromBag(actor, equipId);
                        RenderBag(actor);
                    });
                }
            }
        }

        private static Transform CreateCategoryBox(Transform parent, string title, Color? titleColor, int count = -1)
        {
            title = LocalizedTextManager.getText(title);
            GameObject box = new GameObject("CategoryBox", typeof(RectTransform));
            box.transform.SetParent(parent, false);
            LayoutElement boxLe = box.AddComponent<LayoutElement>();
            boxLe.minHeight = 60f;
            boxLe.flexibleHeight = 0f;

            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(box.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Color borderColor = titleColor ?? new Color(0.4f, 0.45f, 0.5f);
            AddBoxBorder(box.transform, borderColor);

            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(box.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(0, 18f);
            titleRt.offsetMin = new Vector2(8, -18);
            titleRt.offsetMax = new Vector2(-8, 0);

            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = title;
            titleTxt.fontSize = 11;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = titleColor ?? new Color(0.9f, 0.85f, 0.6f);
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (count >= 0)
            {
                GameObject counterGo = new GameObject("Counter", typeof(RectTransform));
                counterGo.transform.SetParent(box.transform, false);
                RectTransform counterRt = counterGo.GetComponent<RectTransform>();
                counterRt.anchorMin = new Vector2(1, 1);
                counterRt.anchorMax = new Vector2(1, 1);
                counterRt.pivot = new Vector2(1f, 1f);
                counterRt.sizeDelta = new Vector2(60, 18f);
                counterRt.offsetMin = new Vector2(-68, -18);
                counterRt.offsetMax = new Vector2(-8, 0);

                Text counterTxt = counterGo.AddComponent<Text>();
                counterTxt.text = count.ToString();
                counterTxt.fontSize = 10;
                counterTxt.color = new Color(0.7f, 0.7f, 0.75f);
                counterTxt.alignment = TextAnchor.MiddleRight;
                counterTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (counterTxt.font == null) counterTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(box.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(6, 6);
            contentRt.offsetMax = new Vector2(-6, -24);

            VerticalLayoutGroup contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 4f;
            contentVlg.padding = new RectOffset(4, 4, 4, 4);

            ContentSizeFitter contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentGo.transform;
        }

        private static void AddBoxBorder(Transform parent, Color color)
        {
            GameObject top = new GameObject("BorderTop", typeof(RectTransform));
            top.transform.SetParent(parent, false);
            Image topImg = top.AddComponent<Image>();
            topImg.color = color;
            topImg.raycastTarget = false;
            RectTransform topRt = top.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0, 2f);

            GameObject bottom = new GameObject("BorderBottom", typeof(RectTransform));
            bottom.transform.SetParent(parent, false);
            Image bottomImg = bottom.AddComponent<Image>();
            bottomImg.color = color;
            bottomImg.raycastTarget = false;
            RectTransform bottomRt = bottom.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(0, 2f);

            GameObject left = new GameObject("BorderLeft", typeof(RectTransform));
            left.transform.SetParent(parent, false);
            Image leftImg = left.AddComponent<Image>();
            leftImg.color = color;
            leftImg.raycastTarget = false;
            RectTransform leftRt = left.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0f, 0.5f);
            leftRt.sizeDelta = new Vector2(2f, 0);

            GameObject right = new GameObject("BorderRight", typeof(RectTransform));
            right.transform.SetParent(parent, false);
            Image rightImg = right.AddComponent<Image>();
            rightImg.color = color;
            rightImg.raycastTarget = false;
            RectTransform rightRt = right.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.sizeDelta = new Vector2(2f, 0);
        }

        private static GameObject CreateEmptySlot(Transform parent, int size, string label)
        {
            var slot = new GameObject("EmptySlot", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

            var border = slot.AddComponent<Image>();
            border.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            var txt = SuperMechUtils.CreateText(slot.transform, label, 10, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f, 0.7f));
            var txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return slot;
        }

        private static void InitEquipmentButton(EquipmentButton btn)
        {
            if (_createMethod == null)
            {
                _createMethod = typeof(AugmentationButton<EquipmentAsset>).GetMethod("create",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
            }
            if (_isEditorButtonField == null)
            {
                _isEditorButtonField = typeof(AugmentationButton<EquipmentAsset>).GetField("is_editor_button",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Public);
            }
            _createMethod?.Invoke(btn, null);
            if (_isEditorButtonField != null)
                _isEditorButtonField.SetValue(btn, true);
        }

        private static EquipmentButton GetButton(Transform parent)
        {
            EquipmentButton btn;
            if (_buttonPool.Count > 0)
            {
                btn = _buttonPool.Dequeue();
                btn.transform.SetParent(parent, false);
                btn.gameObject.SetActive(true);
            }
            else
            {
                if (_equipButtonPrefab == null)
                    _equipButtonPrefab = Resources.Load<EquipmentButton>("ui/EquipmentButton");
                if (_equipButtonPrefab == null)
                {
                    Debug.LogError("[超神机械师] 无法加载原版EquipmentButton预制体 ui/EquipmentButton");
                    return null;
                }
                btn = Object.Instantiate(_equipButtonPrefab, parent);
                InitEquipmentButton(btn);
            }
            _activeButtons.Add(btn);
            return btn;
        }

        private static void ClearButtons()
        {
            foreach (var btn in _activeButtons)
            {
                btn.gameObject.SetActive(false);
                _buttonPool.Enqueue(btn);
            }
            _activeButtons.Clear();
        }

        private static GameObject CreateItemIcon(Transform parent, string iconPath, Color qColor, int size, string name, string tooltip)
        {
            EquipmentButton btn = GetButton(parent);
            if (btn == null)
            {
                var errGo = new GameObject("PrefabLoadError", typeof(RectTransform));
                errGo.transform.SetParent(parent, false);
                var errImg = errGo.AddComponent<Image>();
                errImg.color = new Color(1f, 0f, 0f, 0.8f);
                var errRt = errGo.GetComponent<RectTransform>();
                errRt.sizeDelta = new Vector2(size, size);
                var errTxt = SuperMechUtils.CreateText(errGo.transform, "ERROR", 10, TextAnchor.MiddleCenter, Color.white);
                var errTxtRt = errTxt.GetComponent<RectTransform>();
                errTxtRt.anchorMin = Vector2.zero;
                errTxtRt.anchorMax = Vector2.one;
                errTxtRt.offsetMin = Vector2.zero;
                errTxtRt.offsetMax = Vector2.zero;
                return errGo;
            }

            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image iconImg = btn.transform.Find("TiltEffect/icon").GetComponent<Image>();
            Sprite iconSprite = SpriteTextureLoader.getSprite(iconPath);
            if (iconSprite != null) iconImg.sprite = iconSprite;
            iconImg.color = Color.white;

            Image lockedBg = btn.transform.Find("TiltEffect/locked_bg")?.GetComponent<Image>();
            if (lockedBg != null) lockedBg.gameObject.SetActive(false);

            IconOutline outline = btn.GetComponentInChildren<IconOutline>();
            if (outline != null)
            {
                bool isHighQuality = qColor.r > 0.8f && qColor.g > 0.6f;
                if (isHighQuality)
                    outline.show(RarityLibrary.legendary.color_container);
                else
                    outline.gameObject.SetActive(false);
            }

            TipButton tipBtn = btn.GetComponent<TipButton>();
            if (tipBtn != null)
            {
                tipBtn.clickAction = null;
                tipBtn.textOnClick = tooltip;
                tipBtn.textOnClickDescription = string.Empty;
                tipBtn.text_description_2 = string.Empty;
            }

            Button button = btn.GetComponent<Button>();
            if (button != null) button.onClick.RemoveAllListeners();

            return btn.gameObject;
        }

        private static Color GetQualityColor(int q)
        {
            switch (q)
            {
                case 0: return new Color(0.6f, 0.6f, 0.6f);
                case 1: return new Color(0.3f, 0.8f, 0.3f);
                case 2: return new Color(0.3f, 0.5f, 1f);
                case 3: return new Color(0.7f, 0.5f, 1f);
                case 4: return new Color(0.6f, 0.2f, 0.9f);
                case 5: return new Color(1f, 0.4f, 0.7f);
                case 6: return new Color(1f, 0.6f, 0f);
                case 7: return new Color(0.8f, 0.8f, 0.9f);
                case 8: return new Color(1f, 0.84f, 0f);
                default: return Color.white;
            }
        }

        private static string GetQualityName(int q)
        {
            string[] keys = { "sm_quality_0", "sm_quality_1", "sm_quality_2", "sm_quality_3", "sm_quality_4",
                              "sm_quality_5", "sm_quality_6", "sm_quality_7", "sm_quality_8" };
            return q >= 0 && q < keys.Length ? LocalizedTextManager.getText(keys[q]) : "?";
        }

        private static void AddButton(Transform parent, string text, System.Action onClick)
        {
            GameObject obj = new GameObject("Button", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minHeight = 22;
            le.preferredHeight = 22;

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            Text t = obj.AddComponent<Text>();
            t.font = LocalizedTextManager.current_font;
            t.fontSize = 9;
            t.color = new Color(0.85f, 0.85f, 0.9f);
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            t.transform.SetParent(obj.transform, false);
            RectTransform trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }
}
