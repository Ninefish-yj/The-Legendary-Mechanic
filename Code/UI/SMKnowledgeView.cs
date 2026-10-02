using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMKnowledgeView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _detailContent;
        private Text _detailText;
        private string _selectedId;
        private readonly Dictionary<string, bool> _folded = new Dictionary<string, bool>();
        private static Actor SelectedActor => SelectedUnit.unit;

        void Awake()
        {
            try
            {
                BuildLayout();
                RefreshList();
            }
            catch (System.Exception e) { Debug.LogError("[超神机械师] SMKnowledgeView初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 左侧列表面板（固定宽度220px）
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 0.5f);
            listRect.sizeDelta = new Vector2(220, 0);
            listRect.anchoredPosition = Vector2.zero;

            // 列表滚动区
            var (scroll, content) = SMUiSkin.CreateScrollArea(listGo.transform, "Scroll");
            _listContent = content;

            // 右侧详情面板
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(1, 0);
            detailRect.anchorMax = new Vector2(1, 1);
            detailRect.pivot = new Vector2(1, 0.5f);
            detailRect.sizeDelta = new Vector2(-230, 0);
            detailRect.anchoredPosition = Vector2.zero;

            // 详情卡片背景
            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4);
            cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            if (SMUiSkin.Card != null)
            {
                cardImg.sprite = SMUiSkin.Card;
                cardImg.type = Image.Type.Sliced;
            }
            else
            {
                cardImg.color = SMUiSkin.CardBg;
            }
            _detailContent = cardRect;

            // 详情文字
            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(cardGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 12);
            textRect.offsetMax = new Vector2(-12, -12);
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 13;
            _detailText.color = SMUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_knowledge");
        }

        private void RefreshList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var prefixes = new List<string> { "mech", "martial", "power", "magic", "mind" };
            var prefixNames = new Dictionary<string, string>
            {
                { "mech", "sm_class_mech" }, { "martial", "sm_class_martial" },
                { "power", "sm_class_power" }, { "magic", "sm_class_magic" },
                { "mind", "sm_class_mind" }
            };

            foreach (var prefix in prefixes)
            {
                var list = SuperMechKnowledge.GetAllByPrefix(prefix);
                if (list == null || list.Count == 0) continue;

                if (!_folded.ContainsKey(prefix)) _folded[prefix] = false;

                // 分类标题（可折叠）
                var catGo = new GameObject($"Cat_{prefix}");
                catGo.transform.SetParent(_listContent, false);
                catGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
                var catImg = catGo.AddComponent<Image>();
                if (SMUiSkin.Section != null)
                {
                    catImg.sprite = SMUiSkin.Section;
                    catImg.type = Image.Type.Sliced;
                }
                else
                {
                    catImg.color = SMUiSkin.SectionBg;
                }
                var catBtn = catGo.AddComponent<Button>();
                string p = prefix;
                catBtn.onClick.AddListener(() => ToggleCategory(p));

                var catTextGo = new GameObject("Text");
                catTextGo.transform.SetParent(catGo.transform, false);
                var catTextRect = catTextGo.AddComponent<RectTransform>();
                catTextRect.anchorMin = Vector2.zero;
                catTextRect.anchorMax = Vector2.one;
                catTextRect.offsetMin = new Vector2(8, 0);
                catTextRect.offsetMax = Vector2.zero;
                var catText = catTextGo.AddComponent<Text>();
                catText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                catText.fontSize = 13;
                catText.fontStyle = FontStyle.Bold;
                catText.color = SMUiSkin.TextColor;
                catText.alignment = TextAnchor.MiddleLeft;
                catText.text = (_folded[prefix] ? "▶ " : "▼ ") + LocalizedTextManager.getText(prefixNames[prefix]);

                // 知识项（折叠时不显示）
                if (!_folded[prefix])
                {
                    foreach (var kn in list)
                    {
                        var knGo = new GameObject($"Kn_{kn.id}");
                        knGo.transform.SetParent(_listContent, false);
                        knGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                        var knBtn = knGo.AddComponent<Button>();
                        var knImg = knGo.AddComponent<Image>();
                        bool unlocked = SelectedActor != null && SuperMechKnowledge.IsUnlocked(SelectedActor, kn.id);
                        bool selected = _selectedId == kn.id;
                        knImg.color = selected ? SMUiSkin.AccentDim :
                                      (unlocked ? SMUiSkin.RowEven : SMUiSkin.RowOdd);
                        var textGo = new GameObject("Text");
                        textGo.transform.SetParent(knGo.transform, false);
                        var tr = textGo.AddComponent<RectTransform>();
                        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                        tr.offsetMin = new Vector2(20, 0); tr.offsetMax = Vector2.zero;
                        var kt = textGo.AddComponent<Text>();
                        kt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        kt.fontSize = 12;
                        kt.color = unlocked ? SMUiSkin.TextColor : SMUiSkin.TextDim;
                        kt.alignment = TextAnchor.MiddleLeft;
                        kt.text = (unlocked ? "✓ " : "○ ") + kn.name;
                        string kid = kn.id;
                        knBtn.onClick.AddListener(() => SelectKnowledge(kid));
                    }
                }
            }
        }

        private void ToggleCategory(string prefix)
        {
            _folded[prefix] = !_folded[prefix];
            RefreshList();
        }

        private void SelectKnowledge(string id)
        {
            _selectedId = id;
            var kn = SuperMechKnowledge.GetDef(id);
            if (kn == null) return;
            bool unlocked = SelectedActor != null && SuperMechKnowledge.IsUnlocked(SelectedActor, id);
            _detailText.text = $"<b><size=16><color=#59a5ff>{kn.name}</color></size></b>\n\n" +
                $"{kn.desc}\n\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_tier")}:</color> {kn.tier}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_cost")}:</color> {kn.cost}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_status")}:</color> " +
                $"<color={(unlocked ? "#66ff99" : "#ff9966")}>{(unlocked ? LocalizedTextManager.getText("sm_ui_unlocked") : LocalizedTextManager.getText("sm_ui_locked"))}</color>";
            RefreshList();
        }
    }
}
