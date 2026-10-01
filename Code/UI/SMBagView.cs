using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMBagView : MonoBehaviour
    {
        private RectTransform _gridPanel;
        private Text _detailText;
        private static Actor SelectedActor => SelectedUnit.unit;

        void Awake()
        {
            BuildLayout();
            RefreshGrid();
        }

        private void BuildLayout()
        {
            var gridGo = new GameObject("GridPanel");
            gridGo.transform.SetParent(transform, false);
            _gridPanel = gridGo.AddComponent<RectTransform>();
            _gridPanel.anchorMin = new Vector2(0, 0.18f);
            _gridPanel.anchorMax = Vector2.one;
            _gridPanel.offsetMin = new Vector2(8, 4);
            _gridPanel.offsetMax = new Vector2(-8, -4);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_gridPanel, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1); contentRect.sizeDelta = new Vector2(0, 600);
            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(56, 56);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 9;
            scrollRect.content = contentRect; scrollRect.vertical = true;

            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = new Vector2(1, 0.18f);
            detailRect.offsetMin = new Vector2(8, 4);
            detailRect.offsetMax = new Vector2(-8, -4);
            detailGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);

            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(detailGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4); textRect.offsetMax = new Vector2(-8, -4);
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 12; _detailText.color = new Color(0.1f, 0.15f, 0.25f);
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_item");
        }

        private void RefreshGrid()
        {
            var content = _gridPanel.Find("Scroll/Content");
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
                slotImg.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
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
            var equip = AssetManager.items.get(id);
            if (equip == null) return;
            string name = LocalizedTextManager.getText(equip.getLocaleID());
            string desc = LocalizedTextManager.getText(equip.getDescriptionID());
            _detailText.text = $"<b>{name}</b>\n" +
                $"{LocalizedTextManager.getText("sm_ui_rarity")}: {equip.rarity}\n" +
                $"{desc}";
        }
    }
}
