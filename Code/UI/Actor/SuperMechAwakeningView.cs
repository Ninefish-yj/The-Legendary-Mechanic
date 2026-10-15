using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>超能者阶位突破界面：选择单位→查看阶位成长线→手动突破</summary>
    public class SuperMechAwakeningView : MonoBehaviour
    {
        private static SuperMechAwakeningView _instance;
        public static SuperMechAwakeningView Instance => _instance;

        private GameObject _panel;
        private RectTransform _rankLineContent;
        private Text _unitInfoText;
        private Text _breakthroughInfoText;
        private Button _breakthroughBtn;
        private Dropdown _unitDropdown;
        private List<Actor> _candidateUnits = new List<Actor>();
        private Actor _selectedUnit;

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
                var go = new GameObject(typeof(SuperMechAwakeningView).Name);
                go.transform.SetParent(SuperMechUguiWindow.GetCanvas().transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _instance = go.AddComponent<SuperMechAwakeningView>();
            }
            _instance.Open();
        }

        private void Open()
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            RefreshUnitList();
        }

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildPanel()
        {
            // 全屏遮罩
            var overlayGo = new GameObject("AwakeningOverlay");
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

            // 主面板
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 500);
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
            titleText.text = LocalizedTextManager.getText("sm_ui_awakening_title");

            // 单位选择下拉
            var dropdownGo = new GameObject("UnitDropdown");
            dropdownGo.transform.SetParent(panelGo.transform, false);
            var ddRect = dropdownGo.AddComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0, 1);
            ddRect.anchorMax = new Vector2(1, 1);
            ddRect.pivot = new Vector2(0.5f, 1);
            ddRect.sizeDelta = new Vector2(0, 30);
            ddRect.anchoredPosition = new Vector2(0, -45);
            _unitDropdown = dropdownGo.AddComponent<Dropdown>();
            var ddImg = dropdownGo.GetComponent<Image>(); if (ddImg == null) ddImg = dropdownGo.AddComponent<Image>();
            ddImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            _unitDropdown.onValueChanged.AddListener(OnUnitSelected);

            // 阶位成长线
            var lineGo = new GameObject("RankLine");
            lineGo.transform.SetParent(panelGo.transform, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0.4f);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.offsetMin = new Vector2(10, 10);
            lineRect.offsetMax = new Vector2(-10, -85);
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(0.08f, 0.12f, 0.2f, 0.6f);
            _rankLineContent = lineRect;

            // 单位信息
            var infoGo = new GameObject("UnitInfo");
            infoGo.transform.SetParent(panelGo.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 0.4f);
            infoRect.offsetMin = new Vector2(10, 50);
            infoRect.offsetMax = new Vector2(-10, -5);
            _unitInfoText = infoGo.AddComponent<Text>();
            _unitInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _unitInfoText.fontSize = 12;
            _unitInfoText.color = new Color(0.8f, 0.9f, 1f);
            _unitInfoText.alignment = TextAnchor.UpperLeft;
            _unitInfoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _unitInfoText.verticalOverflow = VerticalWrapMode.Truncate;

            // 突破按钮
            var btnGo = new GameObject("BreakthroughBtn");
            btnGo.transform.SetParent(panelGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.sizeDelta = new Vector2(200, 44);
            btnRect.anchoredPosition = new Vector2(0, 8);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.8f, 0.5f, 0.2f, 0.9f);
            _breakthroughBtn = btnGo.AddComponent<Button>();
            _breakthroughBtn.onClick.AddListener(DoBreakthrough);
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 13;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = LocalizedTextManager.getText("sm_ui_breakthrough");
            btnText.raycastTarget = false;
        }

        private void RefreshUnitList()
        {
            _candidateUnits.Clear();
            var options = new List<string>();
            if (World.world == null || World.world.units == null) return; var list = World.world.units.units_only_alive;
            if (list != null)
            {
                foreach (Actor a in list)
                {
                    if (a == null || a.id == null) continue;
                    if (!a.hasTrait(SuperMechAwakened.AwakenedTrait)) continue;
                    _candidateUnits.Add(a);
                    int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                    string rankName = rankIdx >= 0 && rankIdx < SuperMechRanks.All.Count
                        ? LocalizedTextManager.getText(SuperMechRanks.All[rankIdx].name) : "F";
                    options.Add($"{a.name} ({rankName})");
                }
            }
            _unitDropdown.ClearOptions();
            _unitDropdown.AddOptions(options);
            if (_candidateUnits.Count > 0)
            {
                _selectedUnit = _candidateUnits[0];
                RefreshRankLine();
            }
        }

        private void OnUnitSelected(int idx)
        {
            if (idx >= 0 && idx < _candidateUnits.Count)
            {
                _selectedUnit = _candidateUnits[idx];
                RefreshRankLine();
            }
        }

        private void RefreshRankLine()
        {
            // 清除旧节点
            for (int i = _rankLineContent.childCount - 1; i >= 0; i--)
                Destroy(_rankLineContent.GetChild(i).gameObject);

            if (_selectedUnit == null) return;

            int currentRank = SuperMechAdvancement.GetExactRankIndex(_selectedUnit);
            float onar = SuperMechAdvancement.CalcOnar(_selectedUnit);

            // 显示14个阶位节点
            int showCount = SuperMechRanks.All.Count;
            float nodeWidth = 1f / showCount;
            for (int i = 0; i < showCount; i++)
            {
                var rank = SuperMechRanks.All[i];
                var nodeGo = new GameObject($"RankNode_{i}");
                nodeGo.transform.SetParent(_rankLineContent, false);
                var nodeRect = nodeGo.AddComponent<RectTransform>();
                nodeRect.anchorMin = new Vector2(i * nodeWidth, 0.3f);
                nodeRect.anchorMax = new Vector2((i + 1) * nodeWidth, 0.7f);
                nodeRect.offsetMin = new Vector2(2, 0);
                nodeRect.offsetMax = new Vector2(-2, 0);
                var nodeImg = nodeGo.AddComponent<Image>();
                bool isCurrent = i == currentRank;
                bool isReached = i <= currentRank;
                bool isNext = i == currentRank + 1;
                if (isCurrent) nodeImg.color = new Color(1f, 0.85f, 0.3f, 0.9f);
                else if (isReached) nodeImg.color = new Color(0.3f, 0.6f, 0.4f, 0.7f);
                else if (isNext) nodeImg.color = new Color(0.8f, 0.5f, 0.2f, 0.6f);
                else nodeImg.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);

                var nodeTextGo = new GameObject("Text");
                nodeTextGo.transform.SetParent(nodeGo.transform, false);
                var nodeTextRect = nodeTextGo.AddComponent<RectTransform>();
                nodeTextRect.anchorMin = Vector2.zero;
                nodeTextRect.anchorMax = Vector2.one;
                nodeTextRect.offsetMin = Vector2.zero;
                nodeTextRect.offsetMax = Vector2.zero;
                var nodeText = nodeTextGo.AddComponent<Text>();
                nodeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nodeText.fontSize = 9;
                nodeText.color = isCurrent ? Color.black : (isReached ? Color.white : new Color(0.5f, 0.5f, 0.5f));
                nodeText.alignment = TextAnchor.MiddleCenter;
                nodeText.text = LocalizedTextManager.getText(rank.name);
                nodeText.raycastTarget = false;
            }

            // 单位信息
            string currentRankName = currentRank >= 0 && currentRank < SuperMechRanks.All.Count
                ? LocalizedTextManager.getText(SuperMechRanks.All[currentRank].name) : "F";
            string nextRankName = currentRank + 1 < SuperMechRanks.All.Count
                ? LocalizedTextManager.getText(SuperMechRanks.All[currentRank + 1].name) : "MAX";
            double nextFloor = currentRank + 1 < SuperMechRanks.All.Count
                ? SuperMechRanks.All[currentRank + 1].onarFloor : 0;

            _unitInfoText.text =
                $"<b>{_selectedUnit.name}</b>\n" +
                $"{LocalizedTextManager.getText("sm_ui_current_rank")}: <color=#ffd700>{currentRankName}</color>\n" +
                $"{LocalizedTextManager.getText("sm_ui_onar")}: {onar:F0}\n" +
                $"{LocalizedTextManager.getText("sm_ui_next_rank")}: {nextRankName} (需要{nextFloor:F0}能级)\n";

            if (currentRank + 1 < SuperMechRanks.All.Count)
            {
                bool canBreak = onar >= nextFloor;
                _unitInfoText.text += canBreak
                    ? $"\n<color=#66ff66>{LocalizedTextManager.getText("sm_ui_can_breakthrough")}</color>"
                    : $"\n<color=#ff9966>{LocalizedTextManager.getText("sm_ui_need_more_onar")}</color>";
                _breakthroughBtn.interactable = canBreak;
            }
            else
            {
                _unitInfoText.text += "\n<color=#ffd700>MAX RANK</color>";
                _breakthroughBtn.interactable = false;
            }
        }

        private void DoBreakthrough()
        {
            if (_selectedUnit == null) return;
            int currentRank = SuperMechAdvancement.GetExactRankIndex(_selectedUnit);
            if (currentRank + 1 >= SuperMechRanks.All.Count) return;

            float onar = SuperMechAdvancement.CalcOnar(_selectedUnit);
            double nextFloor = SuperMechRanks.All[currentRank + 1].onarFloor;
            if (onar < nextFloor) return;

            // 执行突破
            string oldTrait = SuperMechRanks.All[currentRank].id;
            string newTrait = SuperMechRanks.All[currentRank + 1].id;
            if (_selectedUnit.hasTrait(oldTrait)) _selectedUnit.removeTrait(oldTrait);
            _selectedUnit.addTrait(newTrait);
            SuperMechAdvancement.SetExactRank(_selectedUnit, currentRank + 1);

            // 首位突破奖励
            string rankName = LocalizedTextManager.getText(SuperMechRanks.All[currentRank + 1].name);
            if (FirstBreakthroughManager.IsFirstBreakthrough(rankName))
            {
                FirstBreakthroughManager.RecordFirstBreakthrough(rankName);
            }

            Debug.Log($"[超神机械师] {_selectedUnit.name} 突破到 {rankName}");
            RefreshRankLine();
        }
    }
}
