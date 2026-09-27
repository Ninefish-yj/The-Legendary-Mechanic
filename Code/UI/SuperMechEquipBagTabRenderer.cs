using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
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

            int totalCount = 0;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                RenderEquipItem(gridGo.transform, actor, SuperMechRelic.Equipments[currentIdx], true);
                totalCount++;
            }

            var bag = SuperMechEquipBag.GetBag(actor);
            if (bag != null)
            {
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
                    if (idx >= 0 && idx != currentIdx)
                    {
                        RenderEquipItem(gridGo.transform, actor, SuperMechRelic.Equipments[idx], false);
                        totalCount++;
                    }
                }
            }

            ActorBag inv = actor.inventory;
            if (inv != null && inv.dict != null)
            {
                var resources = new List<(ResourceAsset asset, int amount)>();
                foreach (var kv in inv.dict)
                {
                    if (kv.Value.amount <= 0) continue;
                    ResourceAsset res = AssetManager.resources.get(kv.Key);
                    if (res == null) continue;
                    resources.Add((res, kv.Value.amount));
                }
                resources.Sort((a, b) => a.asset.order.CompareTo(b.asset.order));
                foreach (var r in resources)
                {
                    RenderResourceItem(gridGo.transform, r.asset, r.amount);
                    totalCount++;
                }
            }

            if (totalCount == 0)
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
            }
        }

        private static void RenderEquipItem(Transform parent, Actor actor, EquipDef def, bool equipped)
        {
            Color qColor = GetQualityColor(def.qualityLevel);
            string actionText = equipped
                ? LocalizedTextManager.getText("sm_ui_unequip")
                : LocalizedTextManager.getText("sm_ui_equip");
            string tooltip = $"{def.name}\n{LocalizedTextManager.getText("sm_ui_quality")}: {GetQualityName(def.qualityLevel)}\n{LocalizedTextManager.getText("sm_ui_damage")}×{def.dmgMul}  {LocalizedTextManager.getText("sm_ui_health")}×{def.hpMul}\n{actionText}";

            var iconGo = CreateItemIcon(parent, def.icon, qColor, 44, def.name, tooltip);

            if (equipped)
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
                btn.OnHover(() =>
                {
                    iconGo.transform.DOKill();
                    iconGo.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutBack);
                });
                btn.OnHoverOut(() =>
                {
                    iconGo.transform.DOKill();
                    iconGo.transform.DOScale(1f, 0.1f).SetEase(Ease.InBack);
                });
                btn.onClick.AddListener(() =>
                {
                    if (equipped)
                    {
                        SuperMechEquipBag.UnequipToBag(actor);
                    }
                    else
                    {
                        SuperMechEquipBag.EquipFromBag(actor, def.id);
                    }
                    RenderBag(actor);
                });
            }
        }

        private static void RenderResourceItem(Transform parent, ResourceAsset res, int amount)
        {
            GameObject itemGo = new GameObject("ResourceItem", typeof(RectTransform));
            itemGo.transform.SetParent(parent, false);
            RectTransform rt = itemGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            Image bg = itemGo.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.17f, 0.2f, 0.8f);

            Image iconImg = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            iconImg.transform.SetParent(itemGo.transform, false);
            RectTransform iconRt = iconImg.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            try
            {
                Sprite sprite = SpriteTextureLoader.getSprite(res.path_icon);
                if (sprite != null) iconImg.sprite = sprite;
            }
            catch { }

            Text amountTxt = new GameObject("Amount", typeof(RectTransform)).AddComponent<Text>();
            amountTxt.transform.SetParent(itemGo.transform, false);
            RectTransform amtRt = amountTxt.GetComponent<RectTransform>();
            amtRt.anchorMin = new Vector2(0, 0);
            amtRt.anchorMax = new Vector2(1, 0.4f);
            amtRt.offsetMin = new Vector2(2, 0);
            amtRt.offsetMax = new Vector2(-2, 0);
            amountTxt.text = amount.ToString();
            amountTxt.fontSize = 10;
            amountTxt.fontStyle = FontStyle.Bold;
            amountTxt.color = Color.white;
            amountTxt.alignment = TextAnchor.LowerRight;
            amountTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (amountTxt.font == null) amountTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            TipButton tipBtn = itemGo.AddComponent<TipButton>();
            tipBtn.textOnClick = res.name + "\n" + res.tooltip;
            tipBtn.textOnClickDescription = string.Empty;
            tipBtn.text_description_2 = string.Empty;

            Button resBtn = itemGo.AddComponent<Button>();
            resBtn.onClick.AddListener(() =>
            {
                itemGo.transform.DOKill();
                itemGo.transform.DOScale(0.8f, 0.1f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    itemGo.transform.DOScale(1f, 0.1f).SetEase(Ease.OutBack);
                });
            });
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
