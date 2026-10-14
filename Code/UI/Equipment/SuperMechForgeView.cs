using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>锻造工坊界面：查看背包/锻造装备/强化装备/分解装备</summary>
    public class SuperMechForgeView : MonoBehaviour
    {
        private static SuperMechForgeView _instance;
        public static SuperMechForgeView Instance => _instance;

        private GameObject _panel;
        private Dropdown _unitDropdown;
        private Text _bagText;
        private Text _forgeText;
        private Button _forgeBtn;
        private Button _enhanceBtn;
        private Button _salvageBtn;
        private List<Actor> _candidates = new List<Actor>();
        private Actor _selected;

        void Awake() { _instance = this; }

        public void Toggle()
        {
            if (_panel != null && _panel.activeSelf) { Close(); return; }
            Open();
        }

        private void Open()
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            RefreshList();
        }

        private void Close() { if (_panel != null) _panel.SetActive(false); }

        private void BuildPanel()
        {
            var overlayGo = new GameObject("ForgeOverlay");
            overlayGo.transform.SetParent(SuperMechWindowManager.GetRoot(), false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.75f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(Close);
            _panel = overlayGo;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(650, 500);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_forge_title");

            // 单位选择
            var ddGo = new GameObject("UnitDropdown");
            ddGo.transform.SetParent(panelGo.transform, false);
            var ddRect = ddGo.AddComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0, 1);
            ddRect.anchorMax = new Vector2(1, 1);
            ddRect.pivot = new Vector2(0.5f, 1);
            ddRect.sizeDelta = new Vector2(0, 30);
            ddRect.anchoredPosition = new Vector2(0, -45);
            _unitDropdown = ddGo.AddComponent<Dropdown>();
            ddGo.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            _unitDropdown.onValueChanged.AddListener(OnSelect);

            // 左侧：背包
            var bagGo = new GameObject("BagPanel");
            bagGo.transform.SetParent(panelGo.transform, false);
            var bagRect = bagGo.AddComponent<RectTransform>();
            bagRect.anchorMin = new Vector2(0, 0);
            bagRect.anchorMax = new Vector2(0.5f, 1);
            bagRect.offsetMin = new Vector2(10, 55);
            bagRect.offsetMax = new Vector2(-5, -85);
            var bagImg = bagGo.AddComponent<Image>();
            bagImg.color = new Color(0.08f, 0.12f, 0.2f, 0.6f);
            _bagText = bagGo.AddComponent<Text>();
            _bagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bagText.fontSize = 11;
            _bagText.color = new Color(0.8f, 0.9f, 1f);
            _bagText.alignment = TextAnchor.UpperLeft;
            _bagText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bagText.verticalOverflow = VerticalWrapMode.Truncate;
            var bagRect2 = _bagText.GetComponent<RectTransform>();
            bagRect2.anchorMin = Vector2.zero;
            bagRect2.anchorMax = Vector2.one;
            bagRect2.offsetMin = new Vector2(8, 8);
            bagRect2.offsetMax = new Vector2(-8, -8);

            // 右侧：锻造信息
            var forgeGo = new GameObject("ForgePanel");
            forgeGo.transform.SetParent(panelGo.transform, false);
            var forgeRect = forgeGo.AddComponent<RectTransform>();
            forgeRect.anchorMin = new Vector2(0.5f, 0);
            forgeRect.anchorMax = new Vector2(1, 1);
            forgeRect.offsetMin = new Vector2(5, 55);
            forgeRect.offsetMax = new Vector2(-10, -85);
            var forgeImg = forgeGo.AddComponent<Image>();
            forgeImg.color = new Color(0.08f, 0.12f, 0.2f, 0.6f);
            _forgeText = forgeGo.AddComponent<Text>();
            _forgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _forgeText.fontSize = 11;
            _forgeText.color = new Color(0.8f, 0.9f, 1f);
            _forgeText.alignment = TextAnchor.UpperLeft;
            _forgeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _forgeText.verticalOverflow = VerticalWrapMode.Truncate;
            var forgeRect2 = _forgeText.GetComponent<RectTransform>();
            forgeRect2.anchorMin = Vector2.zero;
            forgeRect2.anchorMax = Vector2.one;
            forgeRect2.offsetMin = new Vector2(8, 8);
            forgeRect2.offsetMax = new Vector2(-8, -8);

            // 底部按钮：锻造/强化/分解
            string[] btnNames = { "sm_ui_forge_craft", "sm_ui_forge_enhance", "sm_ui_forge_salvage" };
            Color[] btnColors = {
                new Color(0.8f, 0.5f, 0.2f, 0.9f),
                new Color(0.3f, 0.6f, 0.8f, 0.9f),
                new Color(0.8f, 0.3f, 0.3f, 0.9f)
            };
            for (int i = 0; i < 3; i++)
            {
                var btnGo = new GameObject($"ActionBtn_{i}");
                btnGo.transform.SetParent(panelGo.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(i * 0.34f + 0.01f, 0);
                btnRect.anchorMax = new Vector2((i + 1) * 0.34f - 0.01f, 0);
                btnRect.pivot = new Vector2(0.5f, 0);
                btnRect.sizeDelta = new Vector2(0, 35);
                btnRect.anchoredPosition = new Vector2(0, 10);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = btnColors[i];
                var btn = btnGo.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() => OnAction(idx));
                var btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(btnGo.transform, false);
                var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;
                var btnText = btnTextGo.AddComponent<Text>();
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                btnText.fontSize = 12;
                btnText.color = Color.white;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.text = LocalizedTextManager.getText(btnNames[i]);
                btnText.raycastTarget = false;
            }
        }

        private void RefreshList()
        {
            _candidates.Clear();
            var options = new List<string>();
            var list = World.world.units.units_only_alive;
            if (list != null)
            {
                foreach (Actor a in list)
                {
                    if (a == null || a.id == null) continue;
                    _candidates.Add(a);
                    options.Add(a.name);
                }
            }
            _unitDropdown.ClearOptions();
            _unitDropdown.AddOptions(options);
            if (_candidates.Count > 0) { _selected = _candidates[0]; RefreshInfo(); }
        }

        private void OnSelect(int idx)
        {
            if (idx >= 0 && idx < _candidates.Count) { _selected = _candidates[idx]; RefreshInfo(); }
        }

        private void RefreshInfo()
        {
            if (_selected == null) return;
            var bag = SuperMechEquipBag.GetBag(_selected);
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{LocalizedTextManager.getText("sm_ui_equipment_bag")}</b>\n");
            if (bag != null && bag.Count > 0)
            {
                int show = System.Math.Min(bag.Count, 15);
                for (int i = 0; i < show; i++)
                {
                    var e = bag[i];
                    if (e == null) continue;
                    string rarity = e.rarity.ToString();
                    sb.Append($"  {i + 1}. [{rarity}] {e.name}\n");
                }
                if (bag.Count > 15) sb.Append($"  ... +{bag.Count - 15}\n");
            }
            else sb.Append($"  <color=#888888>{LocalizedTextManager.getText("sm_ui_empty")}</color>\n");
            _bagText.text = sb.ToString();

            var sb2 = new System.Text.StringBuilder();
            sb2.Append($"<b>{LocalizedTextManager.getText("sm_ui_forge_info")}</b>\n\n");
            sb2.Append($"{LocalizedTextManager.getText("sm_ui_forge_craft_desc")}\n\n");
            sb2.Append($"{LocalizedTextManager.getText("sm_ui_forge_enhance_desc")}\n\n");
            sb2.Append($"{LocalizedTextManager.getText("sm_ui_forge_salvage_desc")}\n");
            _forgeText.text = sb2.ToString();
        }

        private void OnAction(int idx)
        {
            if (_selected == null) return;
            switch (idx)
            {
                case 0: // 锻造
                    var recipes = SuperMechCrafting.GetAvailableRecipes(_selected);
                    if (recipes != null && recipes.Count > 0)
                    {
                        var r = recipes[UnityEngine.Random.Range(0, recipes.Count)];
                        SuperMechCrafting.TryCraft(_selected, r.id, _selected.current_tile);
                        Debug.Log($"[超神机械师] {$"_selected.name"} 锻造了 {$"r.id"}");
                    }
                    Debug.Log($"[超神机械师] {_selected.name} 锻造了一件装备");
                    break;
                case 1: // 强化（简化：随机强化一件装备）
                    var bag = SuperMechEquipBag.GetBag(_selected);
                    if (bag != null && bag.Count > 0)
                    {
                        var e = bag[UnityEngine.Random.Range(0, bag.Count)];
                        if (e != null) e.level++;
                        Debug.Log($"[超神机械师] {_selected.name} 强化了 {e.name}");
                    }
                    break;
                case 2: // 分解（简化：移除最后一件装备）
                    var bag2 = SuperMechEquipBag.GetBag(_selected);
                    if (bag2 != null && bag2.Count > 0)
                    {
                        var e = bag2[bag2.Count - 1];
                        bag2.RemoveAt(bag2.Count - 1);
                        Debug.Log($"[超神机械师] {_selected.name} 分解了 {e?.name}");
                    }
                    break;
            }
            RefreshInfo();
        }
    }
}
