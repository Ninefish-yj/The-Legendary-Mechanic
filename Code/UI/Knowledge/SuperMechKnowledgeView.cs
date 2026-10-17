using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>知识+融合一体化窗口（v0.36.0 UI合并）
    /// 顶部标签页切换：知识树 / 知识融合
    /// 融合标签页内嵌SMFusionView组件，减少独立窗口数量
    /// </summary>
    public class SuperMechKnowledgeView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _detailContent;
        private Text _detailText;
        private Button _learnBtn;
        private Text _learnBtnText;
        private string _selectedId;
        private readonly Dictionary<string, bool> _folded = new Dictionary<string, bool>();
        public static Actor OverrideActor;
        private static Actor SelectedActor => OverrideActor != null ? OverrideActor : SelectedUnit.unit;

        private GameObject _knowledgePanel;
        private GameObject _fusionPanel;
        private Button _tabKnowledge;
        private Button _tabFusion;
        private Text _tabKnowledgeText;
        private Text _tabFusionText;
        private static bool _openFusionTab = false;

        public static void OpenFusionTab() { _openFusionTab = true; }

        void Awake()
        {
            try
            {
                BuildTabBar();
                BuildKnowledgePanel();
                BuildFusionPanel();
                SwitchTab(_openFusionTab ? 1 : 0);
                _openFusionTab = false;
            }
            catch (System.Exception e) { Debug.LogError("[超神机械师] SMKnowledgeView初始化失败: " + e); }
        }

        private void BuildTabBar()
        {
            var barGo = new GameObject("TabBar");
            barGo.transform.SetParent(transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0.5f, 1);
            barRect.sizeDelta = new Vector2(0, 34);
            barRect.anchoredPosition = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();
            barImg.sprite = SuperMechUiSkin.Section;
            barImg.type = Image.Type.Sliced;
            barImg.color = Color.white;

            _tabKnowledge = CreateTabButton(barGo.transform, "sm_ui_knowledge", 0);
            _tabFusion = CreateTabButton(barGo.transform, "sm_ui_fusion", 1);
        }

        private Button CreateTabButton(Transform parent, string textKey, int index)
        {
            var btnGo = new GameObject($"Tab_{index}");
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(0, 1);
            btnRect.pivot = new Vector2(0, 0.5f);
            btnRect.sizeDelta = new Vector2(100, 0);
            btnRect.anchoredPosition = new Vector2(8 + index * 108, 0);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = SuperMechUiSkin.TabNormal;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            int tabIdx = index;
            btn.onClick.AddListener(() => SwitchTab(tabIdx));

            var txt = SuperMechUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText(textKey), 13, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.color = SuperMechUiSkin.TextDim;
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            if (index == 0) _tabKnowledgeText = txt;
            else _tabFusionText = txt;

            return btn;
        }

        private void SwitchTab(int index)
        {
            bool isKnowledge = index == 0;
            _knowledgePanel.SetActive(isKnowledge);
            _fusionPanel.SetActive(!isKnowledge);

            if (_tabKnowledgeText != null)
            {
                _tabKnowledgeText.color = isKnowledge ? SuperMechUiSkin.TextColor : SuperMechUiSkin.TextDim;
                var img = _tabKnowledge.GetComponent<Image>();
                img.sprite = isKnowledge ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            if (_tabFusionText != null)
            {
                _tabFusionText.color = !isKnowledge ? SuperMechUiSkin.TextColor : SuperMechUiSkin.TextDim;
                var img = _tabFusion.GetComponent<Image>();
                img.sprite = !isKnowledge ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        private void BuildKnowledgePanel()
        {
            _knowledgePanel = new GameObject("KnowledgePanel");
            _knowledgePanel.transform.SetParent(transform, false);
            var panelRect = _knowledgePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = new Vector2(0, 0);
            panelRect.offsetMax = new Vector2(0, -34);

            // 左侧列表面板
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(_knowledgePanel.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 0.5f);
            listRect.sizeDelta = new Vector2(200, 0);
            listRect.anchoredPosition = Vector2.zero;

            var (scroll, content) = SuperMechUiSkin.CreateScrollArea(listGo.transform, "Scroll");
            _listContent = content;

            // 右侧详情面板
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(_knowledgePanel.transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.offsetMin = new Vector2(210, 0);
            detailRect.offsetMax = Vector2.zero;

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4);
            cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = SuperMechUiSkin.CardBg;
            _detailContent = cardRect;

            // 详情滚动区
            var detailScrollGo = new GameObject("DetailScroll");
            detailScrollGo.transform.SetParent(cardGo.transform, false);
            var detailScrollRect = detailScrollGo.AddComponent<ScrollRect>();
            var dsr = detailScrollGo.GetComponent<RectTransform>();
            dsr.anchorMin = Vector2.zero; dsr.anchorMax = Vector2.one;
            dsr.offsetMin = new Vector2(8, 8); dsr.offsetMax = new Vector2(-8, -8);
            var detailMaskImg = detailScrollGo.AddComponent<Image>();
            detailMaskImg.color = new Color(0f, 0f, 0f, 0.01f);
            detailMaskImg.raycastTarget = false;
            var detailMask = detailScrollGo.AddComponent<Mask>();
            detailMask.showMaskGraphic = true;

            var detailViewportGo = new GameObject("Viewport");
            detailViewportGo.transform.SetParent(detailScrollGo.transform, false);
            var detailViewportRt = detailViewportGo.AddComponent<RectTransform>();
            detailViewportRt.anchorMin = Vector2.zero;
            detailViewportRt.anchorMax = Vector2.one;
            detailViewportRt.offsetMin = Vector2.zero;
            detailViewportRt.offsetMax = Vector2.zero;

            var detailContentGo = new GameObject("Content");
            detailContentGo.transform.SetParent(detailViewportGo.transform, false);
            var detailContentRect = detailContentGo.AddComponent<RectTransform>();
            detailContentRect.anchorMin = new Vector2(0, 1);
            detailContentRect.anchorMax = new Vector2(1, 1);
            detailContentRect.pivot = new Vector2(0, 1);
            detailContentRect.sizeDelta = new Vector2(0, 0);
            var detailContentHit = detailContentGo.AddComponent<Image>();
            detailContentHit.color = new Color(0f, 0f, 0f, 0f);
            detailContentHit.raycastTarget = true;
            var detailVlg = detailContentGo.AddComponent<VerticalLayoutGroup>();
            detailVlg.spacing = 4;
            detailVlg.padding = new RectOffset(6, 6, 6, 6);
            detailVlg.childControlHeight = true;
            detailVlg.childControlWidth = true;
            detailVlg.childForceExpandWidth = true;
            detailVlg.childForceExpandHeight = false;
            var detailFitter = detailContentGo.AddComponent<ContentSizeFitter>();
            detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailScrollRect.content = detailContentRect;
            detailScrollRect.viewport = detailViewportRt;
            detailScrollRect.vertical = true;
            detailScrollRect.horizontal = false;
            detailScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var textGo = new GameObject("DetailText");
            textGo.transform.SetParent(detailContentGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            _detailText = textGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 13;
            _detailText.color = SuperMechUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_knowledge");

            // 学习按钮（原著：超能者通过学习知识提升实力）
            var learnBtnGo = new GameObject("LearnBtn");
            learnBtnGo.transform.SetParent(detailContentGo.transform, false);
            var learnBtnRect = learnBtnGo.AddComponent<RectTransform>();
            learnBtnRect.sizeDelta = new Vector2(0, 44);
            _learnBtn = learnBtnGo.AddComponent<Button>();
            var learnBtnImg = learnBtnGo.AddComponent<Image>();
            learnBtnImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            learnBtnImg.type = Image.Type.Sliced;
            learnBtnImg.color = Color.white;
            _learnBtn.transition = Selectable.Transition.SpriteSwap;
            var lss = _learnBtn.spriteState;
            lss.highlightedSprite = SuperMechUiSkin.ButtonHoverSprite;
            lss.pressedSprite = SuperMechUiSkin.ButtonActiveSprite;
            _learnBtn.spriteState = lss;
            _learnBtnText = learnBtnGo.AddComponent<Text>();
            _learnBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _learnBtnText.fontSize = 14;
            _learnBtnText.color = Color.white;
            _learnBtnText.alignment = TextAnchor.MiddleCenter;
            _learnBtnText.text = LocalizedTextManager.getText("sm_ui_learn");
            _learnBtn.onClick.AddListener(OnLearnClicked);
            learnBtnGo.SetActive(false);

            RefreshList();
        }

        private void BuildFusionPanel()
        {
            _fusionPanel = new GameObject("FusionPanel");
            _fusionPanel.transform.SetParent(transform, false);
            var panelRect = _fusionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = new Vector2(0, 0);
            panelRect.offsetMax = new Vector2(0, -34);

            // 内嵌SMFusionView组件
            var fusionGo = new GameObject("FusionView");
            fusionGo.transform.SetParent(_fusionPanel.transform, false);
            var fusionRect = fusionGo.AddComponent<RectTransform>();
            fusionRect.anchorMin = Vector2.zero;
            fusionRect.anchorMax = Vector2.one;
            fusionRect.offsetMin = Vector2.zero;
            fusionRect.offsetMax = Vector2.zero;
            fusionGo.AddComponent<SuperMechFusionView>();
        }

        private void RefreshList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var prefixes = new List<string> { "mech", "martial", "mage", "mind", "psi" };
            var prefixNames = new Dictionary<string, string>
            {
                { "mech", "sm_class_mech" }, { "martial", "sm_class_martial" },
                { "mage", "sm_class_mage" }, { "mind", "sm_class_mind" },
                { "psi", "sm_class_psi" }
            };

            foreach (var prefix in prefixes)
            {
                var list = SuperMechKnowledge.GetAllByPrefix(prefix);
                if (list == null || list.Count == 0) continue;

                if (!_folded.ContainsKey(prefix)) _folded[prefix] = false;

                var catGo = new GameObject($"Cat_{prefix}");
                catGo.transform.SetParent(_listContent, false);
                catGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
                var catImg = catGo.AddComponent<Image>();
                if (SuperMechUiSkin.Section != null)
                {
                    catImg.sprite = SuperMechUiSkin.Section;
                    catImg.type = Image.Type.Sliced;
                }
                else
                {
                    catImg.color = SuperMechUiSkin.SectionBg;
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
                catText.color = SuperMechUiSkin.TextColor;
                catText.alignment = TextAnchor.MiddleLeft;
                catText.text = (_folded[prefix] ? "▶ " : "▼ ") + LocalizedTextManager.getText(prefixNames[prefix]);

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
                        knImg.color = selected ? SuperMechUiSkin.CyanDim :
                                      (unlocked ? SuperMechUiSkin.RowEven : SuperMechUiSkin.RowOdd);
                        var textGo = new GameObject("Text");
                        textGo.transform.SetParent(knGo.transform, false);
                        var tr = textGo.AddComponent<RectTransform>();
                        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                        tr.offsetMin = new Vector2(20, 0); tr.offsetMax = Vector2.zero;
                        var kt = textGo.AddComponent<Text>();
                        kt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        kt.fontSize = 12;
                        kt.color = unlocked ? SuperMechUiSkin.TextColor : SuperMechUiSkin.TextDim;
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
            bool canUnlock = SelectedActor != null && SuperMechKnowledge.CanUnlock(SelectedActor, id);
            int level = unlocked ? SuperMechKnowledge.GetKnowledgeLevel(SelectedActor, id) : 0;
            bool isMax = level >= SuperMechKnowledge.MaxKnowledgeLevel;

            // v0.184 知识等级显示
            string levelText = unlocked ? $"Lv{level}/{SuperMechKnowledge.MaxKnowledgeLevel}" : "-";
            string levelColor = isMax ? "#FFD700" : (level >= 3 ? "#66ff99" : "#93A4BC");

            _detailText.text = $"<b><size=16><color=#E8C36A>{kn.name}</color></size></b>\n\n" +
                $"{kn.desc}\n\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_tier")}:</color> {kn.tier}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cost")}:</color> {kn.cost}\n" +
                $"<color=#93A4BC>等级:</color> <color={levelColor}>{levelText}</color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_status")}:</color> " +
                $"<color={(unlocked ? "#66ff99" : "#ff9966")}>{(unlocked ? LocalizedTextManager.getText("sm_ui_unlocked") : LocalizedTextManager.getText("sm_ui_locked"))}</color>";

            // 更新学习/升级按钮
            if (_learnBtn != null && _learnBtn.gameObject != null)
            {
                if (unlocked && !isMax)
                {
                    // 已解锁未满级：显示升级按钮
                    _learnBtn.gameObject.SetActive(true);
                    int potential = SuperMechPotential.GetPotential(SelectedActor);
                    bool canUpgrade = potential >= SuperMechKnowledge.PotentialCostPerLevel;
                    _learnBtn.interactable = canUpgrade;
                    _learnBtnText.text = canUpgrade ? $"升级({SuperMechKnowledge.PotentialCostPerLevel}潜能)" : LocalizedTextManager.getText("sm_ui_knowledge_potential_low");
                    var img = _learnBtn.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = canUpgrade ? SuperMechUiSkin.ButtonNormalSprite : SuperMechUiSkin.ButtonDisabledSprite;
                        img.type = Image.Type.Sliced;
                        img.color = Color.white;
                    }
                }
                else if (unlocked && isMax)
                {
                    // 满级：隐藏按钮
                    _learnBtn.gameObject.SetActive(false);
                }
                else
                {
                    // 未解锁：显示学习按钮
                    _learnBtn.gameObject.SetActive(true);
                    _learnBtn.interactable = canUnlock;
                    _learnBtnText.text = canUnlock ? LocalizedTextManager.getText("sm_ui_learn") : LocalizedTextManager.getText("sm_ui_locked");
                    var img = _learnBtn.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = canUnlock ? SuperMechUiSkin.ButtonNormalSprite : SuperMechUiSkin.ButtonDisabledSprite;
                        img.type = Image.Type.Sliced;
                        img.color = Color.white;
                    }
                }
            }
            RefreshList();
        }

        private void OnLearnClicked()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(_selectedId)) return;
            bool unlocked = SuperMechKnowledge.IsUnlocked(SelectedActor, _selectedId);
            if (!unlocked)
            {
                // 未解锁：学习知识
                if (SuperMechKnowledge.Unlock(SelectedActor, _selectedId))
                {
                    Debug.Log($"[超神机械师] {SelectedActor.getName()} 学习知识: {_selectedId}");
                    SelectKnowledge(_selectedId);
                }
            }
            else
            {
                // 已解锁：升级知识（v0.184原著还原）
                if (SuperMechKnowledge.UpgradeKnowledge(SelectedActor, _selectedId))
                {
                    Debug.Log($"[超神机械师] {SelectedActor.getName()} 升级知识: {_selectedId}");
                    SelectKnowledge(_selectedId);
                }
            }
        }
    }
}
