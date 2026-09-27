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

            GameObject gridGo = new GameObject("BagGrid", typeof(RectTransform));
            gridGo.transform.SetParent(_container.transform, false);
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

            var allItems = new List<(string equipId, int idx, bool equipped)>();

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                allItems.Add((SuperMechRelic.Equipments[currentIdx].id, currentIdx, true));
            }

            var bag = SuperMechEquipBag.GetBag(actor);
            if (bag != null)
            {
                foreach (string equipId in bag)
                {
                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx >= 0 && idx != currentIdx)
                    {
                        allItems.Add((equipId, idx, false));
                    }
                }
            }

            if (allItems.Count == 0)
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

            allItems.Sort((a, b) =>
            {
                if (a.equipped != b.equipped) return a.equipped ? -1 : 1;
                int qA = SuperMechRelic.Equipments[a.idx].qualityLevel;
                int qB = SuperMechRelic.Equipments[b.idx].qualityLevel;
                return qB.CompareTo(qA);
            });

            foreach (var item in allItems)
            {
                var def = SuperMechRelic.Equipments[item.idx];
                Color qColor = GetQualityColor(def.qualityLevel);

                string actionText = item.equipped
                    ? LocalizedTextManager.getText("sm_ui_unequip")
                    : LocalizedTextManager.getText("sm_ui_equip");

                string tooltip = $"{def.name}\n{LocalizedTextManager.getText("sm_ui_quality")}: {GetQualityName(def.qualityLevel)}\n{LocalizedTextManager.getText("sm_ui_damage")}×{def.dmgMul}  {LocalizedTextManager.getText("sm_ui_health")}×{def.hpMul}\n{actionText}";

                var iconGo = CreateItemIcon(gridGo.transform, def.icon, qColor, 44, def.name, tooltip);

                if (item.equipped)
                {
                    var equippedMark = new GameObject("EquippedMark", typeof(RectTransform));
                    equippedMark.transform.SetParent(iconGo.transform, false);
                    var markImg = equippedMark.AddComponent<Image>();
                    markImg.color = new Color(1f, 0.84f, 0f, 0.25f);
                    var markRt = equippedMark.GetComponent<RectTransform>();
                    markRt.anchorMin = Vector2.zero;
                    markRt.anchorMax = Vector2.one;
                    markRt.offsetMin = Vector2.zero;
                    markRt.offsetMax = Vector2.zero;
                    markRt.raycastTarget = false;
                }

                var btn = iconGo.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (item.equipped)
                        {
                            SuperMechEquipBag.UnequipToBag(actor);
                        }
                        else
                        {
                            SuperMechEquipBag.EquipFromBag(actor, item.equipId);
                        }
                        RenderBag(actor);
                    });
                }
            }
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
    }
}
