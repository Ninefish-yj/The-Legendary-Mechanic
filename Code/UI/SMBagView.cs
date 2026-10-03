using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMBagView : MonoBehaviour
    {
        private RectTransform _gridPanel;
        private Text _detailText;
        private Button _equipBtn;
        private Button _unequipBtn;
        private string _selectedItemId;
        // 可指定查看单位（从单位面板打开时设置），不指定则用全局选中
        public static Actor OverrideActor;
        private static Actor SelectedActor => OverrideActor != null ? OverrideActor : SelectedUnit.unit;

        void Awake()
        {
            BuildLayout();
            RefreshGrid();
        }

        private void BuildLayout()
        {
            // 上方网格区域（占82%高度）
            var gridGo = new GameObject("GridPanel");
            gridGo.transform.SetParent(transform, false);
            _gridPanel = gridGo.AddComponent<RectTransform>();
            _gridPanel.anchorMin = new Vector2(0, 0.18f);
            _gridPanel.anchorMax = Vector2.one;
            _gridPanel.offsetMin = new Vector2(8, 4);
            _gridPanel.offsetMax = new Vector2(-8, -4);

            // 滚动区（带Mask裁剪）
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_gridPanel, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;
            // Mask的Image必须用极小非零alpha（0.01），alpha=0会导致Mask模板区域为空，内容被整体裁剪
            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            maskImg.raycastTarget = false;
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // 独立Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            // Content（GridLayoutGroup，左上角锚定）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1); contentRect.sizeDelta = new Vector2(0, 0);
            // Content透明命中兜底层
            var contentHit = contentGo.AddComponent<Image>();
            contentHit.color = new Color(0f, 0f, 0f, 0f);
            contentHit.raycastTarget = true;
            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(56, 56);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 9;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // 下方详情区域（占18%高度）
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = new Vector2(1, 0.18f);
            detailRect.offsetMin = new Vector2(8, 4);
            detailRect.offsetMax = new Vector2(-8, -4);
            detailGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);

            // 详情文本（左侧70%）
            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(detailGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = new Vector2(0.7f, 1);
            textRect.offsetMin = new Vector2(8, 4); textRect.offsetMax = new Vector2(-4, -4);
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 12; _detailText.color = SMUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_item");

            // 按钮区域（右侧30%）
            var btnGo = new GameObject("BtnArea");
            btnGo.transform.SetParent(detailGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.7f, 0);
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = new Vector2(4, 4);
            btnRect.offsetMax = new Vector2(-8, -4);
            var btnLayout = btnGo.AddComponent<VerticalLayoutGroup>();
            btnLayout.spacing = 4;
            btnLayout.childControlHeight = true;
            btnLayout.childControlWidth = true;

            _equipBtn = SMUiSkin.MakeButton(btnGo.transform, LocalizedTextManager.getText("sm_ui_equip"), 11, OnEquipClick);
            _unequipBtn = SMUiSkin.MakeButton(btnGo.transform, LocalizedTextManager.getText("sm_ui_unequip"), 11, OnUnequipClick);
            UpdateButtonStates();
        }

        private void RefreshGrid()
        {
            var content = _gridPanel.Find("Scroll/Viewport/Content");
            if (content == null) return;
            foreach (Transform child in content) Destroy(child.gameObject);

            if (SelectedActor == null)
            {
                _detailText.text = LocalizedTextManager.getText("sm_ui_no_actor");
                return;
            }

            var bag = SuperMechEquipBag.GetBag(SelectedActor);
            if (bag == null) return;

            foreach (var equipId in bag)
            {
                var equip = AssetManager.items.get(equipId);
                if (equip == null) continue;

                var slotGo = new GameObject($"Slot_{equipId}");
                slotGo.transform.SetParent(content, false);
                var slotImg = slotGo.AddComponent<Image>();
                slotImg.color = new Color(0.3f, 0.4f, 0.55f, 0.4f);
                var slotBtn = slotGo.AddComponent<Button>();

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.15f, 0.15f);
                iconRect.anchorMax = new Vector2(0.85f, 0.85f);
                var iconImg = iconGo.AddComponent<Image>();
                if (equip.gameplay_sprites != null && equip.gameplay_sprites.Length > 0 && equip.gameplay_sprites[0] != null)
                {
                    iconImg.sprite = equip.gameplay_sprites[0];
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = QualityColor(equip.rarity);
                }

                string eid = equipId;
                slotBtn.onClick.AddListener(() => SelectItem(eid));
            }
        }

        private Color QualityColor(int rarity)
        {
            switch (rarity)
            {
                case 1: return new Color(0.7f, 0.7f, 0.7f);
                case 2: return new Color(0.2f, 0.7f, 0.3f);
                case 3: return new Color(0.2f, 0.4f, 0.9f);
                case 4: return new Color(0.6f, 0.2f, 0.8f);
                case 5: return new Color(0.9f, 0.5f, 0.1f);
                case 6: return new Color(0.9f, 0.8f, 0.2f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void SelectItem(string id)
        {
            _selectedItemId = id;
            var equip = AssetManager.items.get(id);
            if (equip == null) return;
            string name = LocalizedTextManager.getText(equip.getLocaleID());
            string desc = LocalizedTextManager.getText(equip.getDescriptionID());
            _detailText.text = $"<b>{name}</b>\n" +
                $"{LocalizedTextManager.getText("sm_ui_rarity")}: {equip.rarity}\n" +
                $"{desc}";
            UpdateButtonStates();
        }

        private void OnEquipClick()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(_selectedItemId)) return;
            bool success = SuperMechEquipBag.EquipFromBag(SelectedActor, _selectedItemId);
            RefreshGrid();
            _selectedItemId = null;
            _detailText.text = success
                ? LocalizedTextManager.getText("sm_ui_equip_success")
                : LocalizedTextManager.getText("sm_ui_equip_fail");
            UpdateButtonStates();
        }

        private void OnUnequipClick()
        {
            if (SelectedActor == null) return;
            bool success = SuperMechEquipBag.UnequipToBag(SelectedActor);
            RefreshGrid();
            _detailText.text = success
                ? LocalizedTextManager.getText("sm_ui_unequip_success")
                : LocalizedTextManager.getText("sm_ui_unequip_fail");
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (_equipBtn != null)
            {
                bool canEquip = !string.IsNullOrEmpty(_selectedItemId) && SelectedActor != null;
                _equipBtn.interactable = canEquip;
                var img = _equipBtn.GetComponent<Image>();
                if (img != null) img.color = canEquip ? new Color(0.2f, 0.5f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            if (_unequipBtn != null)
            {
                bool canUnequip = SelectedActor != null && SuperMechRelic.GetCurrentEquipIndex(SelectedActor) >= 0;
                _unequipBtn.interactable = canUnequip;
                var img = _unequipBtn.GetComponent<Image>();
                if (img != null) img.color = canUnequip ? new Color(0.5f, 0.3f, 0.2f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }
}
