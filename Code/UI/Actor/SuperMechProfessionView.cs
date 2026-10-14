using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>职业管理界面：查看主职业/子职业等级/经验/转职</summary>
    public class SuperMechProfessionView : MonoBehaviour
    {
        private static SuperMechProfessionView _instance;
        public static SuperMechProfessionView Instance => _instance;

        private GameObject _panel;
        private Dropdown _unitDropdown;
        private Text _infoText;
        private List<Actor> _candidates = new List<Actor>();
        private Actor _selected;

        void Awake() { _instance = this; }
        void OnDestroy() { if (_instance == this) _instance = null; }

        public static void Toggle()
        {
            if (_instance != null && _instance._panel != null && _instance._panel.activeSelf)
            {
                _instance.Close();
                return;
            }
            if (_instance == null)
            {
                var go = new GameObject(typeof(SuperMechProfessionView).Name);
                go.transform.SetParent(SuperMechUguiWindow.GetCanvas().transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _instance = go.AddComponent<SuperMechProfessionView>();
            }
            _instance.Open();
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
            var overlayGo = new GameObject("ProfessionOverlay");
            overlayGo.transform.SetParent(SuperMechUguiWindow.GetCanvas().transform, false);
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
            panelRect.sizeDelta = new Vector2(550, 450);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

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
            titleText.text = LocalizedTextManager.getText("sm_ui_profession_title");

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

            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(panelGo.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.offsetMin = new Vector2(10, 10);
            infoRect.offsetMax = new Vector2(-10, -85);
            _infoText = infoGo.AddComponent<Text>();
            _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _infoText.fontSize = 12;
            _infoText.color = new Color(0.8f, 0.9f, 1f);
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;
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
                    if (!a.hasTrait(SuperMechAwakened.AwakenedTrait)) continue;
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
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{_selected.name}</b>\n\n");

            // 主职业
            var profType = SuperMechProfession.GetProfession(_selected);
            string mainProf = SuperMechProfession.GetProfessionName(profType);
            if (!string.IsNullOrEmpty(mainProf) && profType != SuperMechProfession.ProfessionType.None)
            {
                sb.Append($"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_main_profession")}:</color> {mainProf}\n");
                sb.Append($"  {LocalizedTextManager.getText("sm_ui_switch_count")}: {SuperMechProfession.GetSwitchCount(_selected)}\n\n");
            }

            // 子职业
            sb.Append($"<color=#66ccff>{LocalizedTextManager.getText("sm_ui_sub_profession")}:</color>\n");
            bool hasSub = false;
            foreach (var subId in SuperMechSubClass.AllSubClasses)
            {
                if (SuperMechSubClass.HasSubClass(_selected, subId))
                {
                    hasSub = true;
                    int lv = SuperMechSubClass.GetSubLevel(_selected, subId);
                    float xp = 0f;
                    if (SuperMechSubClass._subXp.TryGetValue(_selected.id.value, out var xpDict) && xpDict.TryGetValue(subId, out var x)) xp = x;
                    var def = SuperMechSubClass.GetDef(subId);
                    string name = def != null ? LocalizedTextManager.getText(def.nameKey) : subId;
                    sb.Append($"  • {name} Lv{lv} (XP:{xp:F0})\n");
                }
            }
            if (!hasSub) sb.Append($"  <color=#888888>{LocalizedTextManager.getText("sm_ui_no_sub")}</color>\n");

            // 可学习的子职业
            sb.Append($"\n<color=#99ff99>{LocalizedTextManager.getText("sm_ui_learnable_sub")}:</color>\n");
            foreach (var subId in SuperMechSubClass.AllSubClasses)
            {
                if (!SuperMechSubClass.HasSubClass(_selected, subId))
                {
                    var def = SuperMechSubClass.GetDef(subId);
                    string name = def != null ? LocalizedTextManager.getText(def.nameKey) : subId;
                    sb.Append($"  ○ {name}\n");
                }
            }

            _infoText.text = sb.ToString();
        }
    }
}
