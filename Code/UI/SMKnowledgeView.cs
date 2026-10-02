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
            catch (System.Exception e) { Debug.LogError("[超神机械师] SMKnowledgeView初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 左侧列表面板（固定宽度200px）
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            _listPanel = listGo.AddComponent<RectTransform>();
            _listPanel.anchorMin = new Vector2(0, 0);
            _listPanel.anchorMax = new Vector2(0, 1);
            _listPanel.pivot = new Vector2(0, 0.5f);
            _listPanel.sizeDelta = new Vector2(200, 0);
            _listPanel.anchoredPosition = Vector2.zero;

            // 列表滚动
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_listPanel, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(4, 4); sr.offsetMax = new Vector2(-4, -4);
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1); contentRect.sizeDelta = new Vector2(0, 100);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2; layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlHeight = true; layout.childControlWidth = true;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect; scrollRect.vertical = true; scrollRect.horizontal = false;

            // 右侧详情面板
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(1, 0);
            detailRect.anchorMax = new Vector2(1, 1);
            detailRect.pivot = new Vector2(1, 0.5f);
            detailRect.sizeDelta = new Vector2(-210, 0);
            detailRect.anchoredPosition = Vector2.zero;

            // 详情文字
            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(detailGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10); textRect.offsetMax = new Vector2(-10, -10);
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 13; _detailText.color = SMUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_knowledge");
        }

        private void RefreshList()
        {
            var content = _listPanel.Find("Scroll/Content");
            if (content == null) return;
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

                // 分类标题
                var catGo = new GameObject($"Cat_{prefix}");
                catGo.transform.SetParent(content, false);
                catGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
                var catText = catGo.AddComponent<Text>();
                catText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                catText.fontSize = 13; catText.fontStyle = FontStyle.Bold;
                catText.color = SMUiSkin.AccentColor;
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
                    knImg.color = unlocked ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(0.3f, 0.3f, 0.3f, 0.2f);
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(knGo.transform, false);
                    var tr = textGo.AddComponent<RectTransform>();
                    tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                    tr.offsetMin = new Vector2(12, 0); tr.offsetMax = Vector2.zero;
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
