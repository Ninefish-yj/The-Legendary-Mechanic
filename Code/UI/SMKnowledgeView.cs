using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMKnowledgeView : MonoBehaviour
    {
        private RectTransform _listPanel;
        private Text _detailText;
        private string _selectedId;
        private static Actor SelectedActor => SelectedUnit.unit;

        void Awake()
        {
            try
            {
                BuildLayout();
                RefreshList();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] SMKnowledgeView初始化失败: " + e);
            }
        }

        private void BuildLayout()
        {
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            _listPanel = listGo.AddComponent<RectTransform>();
            _listPanel.anchorMin = new Vector2(0, 0);
            _listPanel.anchorMax = new Vector2(0.4f, 1);
            _listPanel.offsetMin = Vector2.zero;
            _listPanel.offsetMax = new Vector2(-5, 0);
            listGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_listPanel, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(4, 4); sr.offsetMax = new Vector2(-4, -4);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1); contentRect.sizeDelta = new Vector2(0, 800);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3; layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true; layout.childControlWidth = true;
            scrollRect.content = contentRect; scrollRect.vertical = true;

            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.4f, 0); detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(5, 0); detailRect.offsetMax = Vector2.zero;
            detailGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.08f);

            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(detailGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10); textRect.offsetMax = new Vector2(-10, -10);
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 13; _detailText.color = new Color(0.1f, 0.15f, 0.25f);
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_knowledge");
        }

        private void RefreshList()
        {
            var content = _listPanel.Find("Scroll/Content");
            foreach (Transform child in content) Destroy(child.gameObject);

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

                var catGo = new GameObject($"Cat_{prefix}");
                catGo.transform.SetParent(content, false);
                catGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
                var catText = catGo.AddComponent<Text>();
                catText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                catText.fontSize = 13; catText.fontStyle = FontStyle.Bold;
                catText.color = new Color(0.2f, 0.3f, 0.5f);
                catText.alignment = TextAnchor.MiddleLeft;
                catText.text = "  " + LocalizedTextManager.getText(prefixNames[prefix]);

                foreach (var kn in list)
                {
                    var knGo = new GameObject($"Kn_{kn.id}");
                    knGo.transform.SetParent(content, false);
                    knGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                    var knBtn = knGo.AddComponent<Button>();
                    var knImg = knGo.AddComponent<Image>();
                    bool unlocked = SelectedActor != null && SuperMechKnowledge.IsUnlocked(SelectedActor, kn.id);
                    knImg.color = unlocked ? new Color(0.3f, 0.5f, 0.8f, 0.25f) : new Color(0.5f, 0.5f, 0.5f, 0.15f);
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(knGo.transform, false);
                    var tr = textGo.AddComponent<RectTransform>();
                    tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                    tr.offsetMin = new Vector2(18, 0); tr.offsetMax = Vector2.zero;
                    var kt = textGo.AddComponent<Text>();
                    kt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    kt.fontSize = 12;
                    kt.color = unlocked ? new Color(0.1f, 0.2f, 0.4f) : new Color(0.4f, 0.4f, 0.4f);
                    kt.alignment = TextAnchor.MiddleLeft;
                    kt.text = (unlocked ? "✓ " : "○ ") + kn.name;
                    string kid = kn.id;
                    knBtn.onClick.AddListener(() => SelectKnowledge(kid));
                }
            }
        }

        private void SelectKnowledge(string id)
        {
            _selectedId = id;
            var kn = SuperMechKnowledge.GetDef(id);
            if (kn == null) return;
            bool unlocked = SelectedActor != null && SuperMechKnowledge.IsUnlocked(SelectedActor, id);
            _detailText.text = $"<b>{kn.name}</b>\n\n" +
                $"{kn.desc}\n\n" +
                $"{LocalizedTextManager.getText("sm_ui_tier")}: {kn.tier}\n" +
                $"{LocalizedTextManager.getText("sm_ui_cost")}: {kn.cost}\n" +
                $"{LocalizedTextManager.getText("sm_ui_status")}: {(unlocked ? LocalizedTextManager.getText("sm_ui_unlocked") : LocalizedTextManager.getText("sm_ui_locked"))}";
        }
    }
}
