using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>文明科技树窗口（v0.99.5重构：6系文明科技，垂直线性布局）
    /// 能源/航天/信息/机械/生物/军事，每系5级，对照原著+现实科技路线</summary>
    public class SuperMechTechView : MonoBehaviour
    {
        private RectTransform _treeContent;
        private Text _detailText;
        private Button _researchBtn;
        private Text _researchBtnText;
        private Text _kingdomNameText;
        private Text _kingdomLabelText;
        private string _selectedId;
        private SuperMechTech.TechBranch _currentBranch = SuperMechTech.TechBranch.Energy;
        private static Kingdom _selectedKingdom;
        private readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>();

        public static Kingdom SelectedKingdom
        {
            get => _selectedKingdom != null ? _selectedKingdom : (World.world != null && World.world.kingdoms != null && World.world.kingdoms.list.Count > 0 ? World.world.kingdoms.list[0] : null);
            set => _selectedKingdom = value;
        }

        void Awake()
        {
            try { SuperMechTech.Init(); BuildUI(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] TechView初始化失败: " + e.Message + "\n" + e.StackTrace); }
        }

        private void BuildUI()
        {
            // 顶部标题栏：文明科技树 + 当前文明名称
            var titleGo = new GameObject("TitleBar");
            titleGo.transform.SetParent(transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 28);
            titleRect.anchoredPosition = Vector2.zero;
            var titleImg = titleGo.AddComponent<Image>();
            titleImg.color = new Color(0.08f, 0.1f, 0.16f, 0.95f);

            var titleTextGo = new GameObject("Title");
            titleTextGo.transform.SetParent(titleGo.transform, false);
            var titleTextRect = titleTextGo.AddComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero; titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(8, 0); titleTextRect.offsetMax = new Vector2(-8, 0);
            var titleText = titleTextGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.color = new Color(0.8f, 0.9f, 1f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = LocalizedTextManager.getText("sm_ui_civ_tech_tree");

            _kingdomNameText = titleText; // 复用，后面更新

            // 王国切换器（标题栏右侧）
            var kingdomSwitchGo = new GameObject("KingdomSwitch");
            kingdomSwitchGo.transform.SetParent(transform, false);
            var ksRect = kingdomSwitchGo.AddComponent<RectTransform>();
            ksRect.anchorMin = new Vector2(1, 1);
            ksRect.anchorMax = new Vector2(1, 1);
            ksRect.pivot = new Vector2(1, 1);
            ksRect.sizeDelta = new Vector2(200, 24);
            ksRect.anchoredPosition = new Vector2(-8, -4);

            // 左箭头
            var prevBtnGo = new GameObject("PrevBtn");
            prevBtnGo.transform.SetParent(kingdomSwitchGo.transform, false);
            var prevRect = prevBtnGo.AddComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0, 0); prevRect.anchorMax = new Vector2(0, 1);
            prevRect.sizeDelta = new Vector2(24, 0);
            var prevBtn = prevBtnGo.AddComponent<Button>();
            var prevImg = prevBtnGo.AddComponent<Image>();
            prevImg.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            var prevTxtGo = new GameObject("Label");
            prevTxtGo.transform.SetParent(prevBtnGo.transform, false);
            var prevTxtRect = prevTxtGo.AddComponent<RectTransform>();
            prevTxtRect.anchorMin = Vector2.zero; prevTxtRect.anchorMax = Vector2.one;
            var prevTxt = prevTxtGo.AddComponent<Text>();
            prevTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prevTxt.fontSize = 14; prevTxt.color = Color.white;
            prevTxt.alignment = TextAnchor.MiddleCenter;
            prevTxt.text = "<";
            prevBtn.onClick.AddListener(() => SwitchKingdom(-1));

            // 王国名称显示
            var kingdomLabelGo = new GameObject("KingdomLabel");
            kingdomLabelGo.transform.SetParent(kingdomSwitchGo.transform, false);
            var klRect = kingdomLabelGo.AddComponent<RectTransform>();
            klRect.anchorMin = new Vector2(0, 0); klRect.anchorMax = new Vector2(1, 1);
            klRect.offsetMin = new Vector2(28, 0); klRect.offsetMax = new Vector2(-28, 0);
            var klImg = kingdomLabelGo.AddComponent<Image>();
            klImg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
            _kingdomLabelText = kingdomLabelGo.AddComponent<Text>();
            _kingdomLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _kingdomLabelText.fontSize = 11;
            _kingdomLabelText.color = new Color(0.8f, 0.9f, 1f);
            _kingdomLabelText.alignment = TextAnchor.MiddleCenter;

            // 右箭头
            var nextBtnGo = new GameObject("NextBtn");
            nextBtnGo.transform.SetParent(kingdomSwitchGo.transform, false);
            var nextRect = nextBtnGo.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1, 0); nextRect.anchorMax = new Vector2(1, 1);
            nextRect.sizeDelta = new Vector2(24, 0);
            var nextBtn = nextBtnGo.AddComponent<Button>();
            var nextImg = nextBtnGo.AddComponent<Image>();
            nextImg.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            var nextTxtGo = new GameObject("Label");
            nextTxtGo.transform.SetParent(nextBtnGo.transform, false);
            var nextTxtRect = nextTxtGo.AddComponent<RectTransform>();
            nextTxtRect.anchorMin = Vector2.zero; nextTxtRect.anchorMax = Vector2.one;
            var nextTxt = nextTxtGo.AddComponent<Text>();
            nextTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nextTxt.fontSize = 14; nextTxt.color = Color.white;
            nextTxt.alignment = TextAnchor.MiddleCenter;
            nextTxt.text = ">";
            nextBtn.onClick.AddListener(() => SwitchKingdom(1));

            // 系别标签栏
            var barGo = new GameObject("BranchBar");
            barGo.transform.SetParent(transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0.5f, 1);
            barRect.sizeDelta = new Vector2(0, 30);
            barRect.anchoredPosition = new Vector2(0, -28);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

            string[] branches = { "sm_ui_tech_energy", "sm_ui_tech_space", "sm_ui_tech_info", "sm_ui_tech_mech", "sm_ui_tech_bio", "sm_ui_tech_military" };
            for (int i = 0; i < branches.Length; i++)
            {
                var btnGo = new GameObject($"Branch_{i}");
                btnGo.transform.SetParent(barGo.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(i / 6f, 0);
                btnRect.anchorMax = new Vector2((i + 1) / 6f, 1);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                var btn = btnGo.AddComponent<Button>();
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = i == 0 ? new Color(0.2f, 0.4f, 0.7f, 0.9f) : new Color(0.12f, 0.15f, 0.2f, 0.9f);
                // Text必须放在子对象上：Image和Text都是Graphic，同一GameObject只能有一个Graphic
                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRect = txtGo.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 11;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.text = LocalizedTextManager.getText(branches[i]);
                int idx = i;
                btn.onClick.AddListener(() => SwitchBranch(idx));
            }

            // 左侧树状图区域（55%宽度）
            var treeGo = new GameObject("TreeArea");
            treeGo.transform.SetParent(transform, false);
            var treeRect = treeGo.AddComponent<RectTransform>();
            treeRect.anchorMin = Vector2.zero;
            treeRect.anchorMax = new Vector2(0.55f, 1);
            treeRect.offsetMin = Vector2.zero;
            treeRect.offsetMax = new Vector2(0, -58);

            var treeScrollGo = new GameObject("Scroll");
            treeScrollGo.transform.SetParent(treeGo.transform, false);
            var treeScrollRect = treeScrollGo.AddComponent<ScrollRect>();
            var tsr = treeScrollGo.GetComponent<RectTransform>();
            tsr.anchorMin = Vector2.zero; tsr.anchorMax = Vector2.one;
            tsr.offsetMin = new Vector2(4, 4); tsr.offsetMax = new Vector2(-4, -4);
            var treeMaskImg = treeScrollGo.AddComponent<Image>();
            treeMaskImg.color = new Color(0f, 0f, 0f, 0.01f);
            treeMaskImg.raycastTarget = false;
            var treeMask = treeScrollGo.AddComponent<Mask>();
            treeMask.showMaskGraphic = true;

            var treeViewportGo = new GameObject("Viewport");
            treeViewportGo.transform.SetParent(treeScrollGo.transform, false);
            var treeViewportRt = treeViewportGo.AddComponent<RectTransform>();
            treeViewportRt.anchorMin = Vector2.zero; treeViewportRt.anchorMax = Vector2.one;
            treeViewportRt.offsetMin = Vector2.zero; treeViewportRt.offsetMax = Vector2.zero;

            var treeContentGo = new GameObject("Content");
            treeContentGo.transform.SetParent(treeViewportGo.transform, false);
            _treeContent = treeContentGo.AddComponent<RectTransform>();
            _treeContent.anchorMin = new Vector2(0, 1);
            _treeContent.anchorMax = new Vector2(0, 1);
            _treeContent.pivot = new Vector2(0, 1);
            _treeContent.sizeDelta = new Vector2(300, 400);
            var treeContentHit = treeContentGo.AddComponent<Image>();
            treeContentHit.color = new Color(0f, 0f, 0f, 0f);
            treeContentHit.raycastTarget = true;
            treeScrollRect.content = _treeContent;
            treeScrollRect.viewport = treeViewportRt;
            treeScrollRect.vertical = true;
            treeScrollRect.horizontal = false;
            treeScrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 右侧详情
            var detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.55f, 0);
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = new Vector2(0, -58);

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero; cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4); cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = SuperMechUiSkin.CardBg;

            var detailTextGo = new GameObject("DetailText");
            detailTextGo.transform.SetParent(cardGo.transform, false);
            var dtRect = detailTextGo.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero; dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(8, 40); dtRect.offsetMax = new Vector2(-8, -8);
            _detailText = detailTextGo.AddComponent<Text>();
            _detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _detailText.fontSize = 13;
            _detailText.color = SuperMechUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");

            // 研究按钮
            var researchBtnGo = new GameObject("ResearchBtn");
            researchBtnGo.transform.SetParent(cardGo.transform, false);
            var rbRect = researchBtnGo.AddComponent<RectTransform>();
            rbRect.anchorMin = new Vector2(0, 0);
            rbRect.anchorMax = new Vector2(1, 0);
            rbRect.pivot = new Vector2(0.5f, 0);
            rbRect.sizeDelta = new Vector2(0, 30);
            rbRect.anchoredPosition = new Vector2(0, 6);
            _researchBtn = researchBtnGo.AddComponent<Button>();
            var rbImg = researchBtnGo.AddComponent<Image>();
            rbImg.color = new Color(0.2f, 0.5f, 0.3f, 0.9f);
            // Text必须放在子对象上：Image和Text都是Graphic，同一GameObject只能有一个Graphic
            var rbTxtGo = new GameObject("Label");
            rbTxtGo.transform.SetParent(researchBtnGo.transform, false);
            var rbTxtRect = rbTxtGo.AddComponent<RectTransform>();
            rbTxtRect.anchorMin = Vector2.zero; rbTxtRect.anchorMax = Vector2.one;
            rbTxtRect.offsetMin = Vector2.zero; rbTxtRect.offsetMax = Vector2.zero;
            _researchBtnText = rbTxtGo.AddComponent<Text>();
            _researchBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _researchBtnText.fontSize = 14;
            _researchBtnText.color = Color.white;
            _researchBtnText.alignment = TextAnchor.MiddleCenter;
            _researchBtnText.text = LocalizedTextManager.getText("sm_ui_research");
            _researchBtn.onClick.AddListener(OnResearchClicked);
            researchBtnGo.SetActive(false);

            RefreshTree();
            UpdateKingdomName();
        }

        private void UpdateKingdomName()
        {
            var k = SelectedKingdom;
            string kName = (k != null && k.data != null) ? k.data.name : LocalizedTextManager.getText("sm_ui_none");
            // v0.175.1: 显示文明当前时代
            SuperMechTech.Era era = SuperMechTech.Era.PreIndustrial;
            if (k != null) era = SuperMechTech.GetEra(k.id);
            string eraName = SuperMechTech.GetEraName(era);
            string eraColor = era switch
            {
                SuperMechTech.Era.PreIndustrial => "#888888",
                SuperMechTech.Era.Industrial => "#cd853f",
                SuperMechTech.Era.Information => "#4fc3f7",
                SuperMechTech.Era.Interstellar => "#ba68c8",
                SuperMechTech.Era.Cosmic => "#ffd700",
                _ => "#ffffff"
            };
            if (_kingdomNameText != null)
                _kingdomNameText.text = $"{LocalizedTextManager.getText("sm_ui_civ_tech_tree")} - {kName}  <color={eraColor}>[{eraName}]</color>";
            if (_kingdomLabelText != null)
                _kingdomLabelText.text = $"{kName}·{eraName}";
        }

        /// <summary>切换王国（direction: -1上一个, 1下一个）</summary>
        private void SwitchKingdom(int direction)
        {
            if (World.world == null || World.world.kingdoms == null) return;
            var kingdoms = World.world.kingdoms.list;
            if (kingdoms.Count == 0) return;

            var current = SelectedKingdom;
            int idx = kingdoms.IndexOf(current);
            if (idx < 0) idx = 0;
            idx = (idx + direction + kingdoms.Count) % kingdoms.Count;
            SelectedKingdom = kingdoms[idx];

            _selectedId = null;
            RefreshTree();
            UpdateKingdomName();
            if (_detailText != null) _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");
            if (_researchBtn != null) _researchBtn.gameObject.SetActive(false);
        }

        private void SwitchBranch(int idx)
        {
            _currentBranch = (SuperMechTech.TechBranch)idx;
            _selectedId = null;
            var barGo = transform.Find("BranchBar");
            if (barGo != null)
            {
                for (int i = 0; i < barGo.childCount; i++)
                {
                    var child = barGo.GetChild(i);
                    var img = child.GetComponent<Image>();
                    if (img != null) img.color = i == idx ? new Color(0.2f, 0.4f, 0.7f, 0.9f) : new Color(0.12f, 0.15f, 0.2f, 0.9f);
                }
            }
            RefreshTree();
            if (_detailText != null) _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");
            if (_researchBtn != null) _researchBtn.gameObject.SetActive(false);
        }

        /// <summary>构建垂直线性树状图</summary>
        private void RefreshTree()
        {
            if (_treeContent == null) return;
            foreach (Transform child in _treeContent) Destroy(child.gameObject);
            _nodeImages.Clear();

            var kingdom = SelectedKingdom;
            long kid = kingdom != null ? kingdom.getID() : 0;
            var techs = SuperMechTech.GetByBranch(_currentBranch);

            float nodeWidth = 260f;
            float nodeHeight = 56f;
            float spacing = 14f;
            float startX = 20f;
            float startY = -20f;

            _treeContent.sizeDelta = new Vector2(300, techs.Count * (nodeHeight + spacing) + 40);

            for (int i = 0; i < techs.Count; i++)
            {
                var tech = techs[i];
                float y = startY - i * (nodeHeight + spacing);
                CreateNode(tech, startX, y, nodeWidth, nodeHeight, kid);
                // 连线
                if (i < techs.Count - 1)
                {
                    CreateVerticalLine(startX + nodeWidth / 2, y - nodeHeight / 2, y - nodeHeight / 2 - spacing);
                }
            }
        }

        private void CreateNode(SuperMechTech.TechDef tech, float x, float y, float w, float h, long kid)
        {
            bool researched = kid > 0 && SuperMechTech.IsResearched(kid, tech.id);
            bool canResearch = kid > 0 && SuperMechTech.CanResearch(kid, tech.id);

            var nodeGo = new GameObject($"Node_{tech.id}");
            nodeGo.transform.SetParent(_treeContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0, 1);
            nodeRect.anchorMax = new Vector2(0, 1);
            nodeRect.pivot = new Vector2(0, 1);
            nodeRect.sizeDelta = new Vector2(w, h);
            nodeRect.anchoredPosition = new Vector2(x, y);
            var nodeBtn = nodeGo.AddComponent<Button>();
            var nodeImg = nodeGo.AddComponent<Image>();
            nodeImg.color = researched ? new Color(0.12f, 0.3f, 0.18f, 0.95f) :
                            canResearch ? new Color(0.25f, 0.2f, 0.08f, 0.95f) :
                            new Color(0.15f, 0.15f, 0.18f, 0.95f);
            _nodeImages[tech.id] = nodeImg;

            // 状态圆点
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(nodeGo.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0, 0.5f);
            dotRect.anchorMax = new Vector2(0, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(12, 12);
            dotRect.anchoredPosition = new Vector2(12, 0);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = researched ? new Color(0.4f, 1f, 0.5f) :
                          canResearch ? new Color(1f, 0.8f, 0.3f) :
                          new Color(0.5f, 0.5f, 0.5f);
            // 不设置sprite：Image无sprite时显示纯色方块，跨平台兼容（Android无内置Background资源）

            // 科技名称+层级
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(nodeGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(30, 0); txtRect.offsetMax = new Vector2(-8, 0);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.color = researched ? new Color(0.6f, 1f, 0.7f) :
                        canResearch ? SuperMechUiSkin.TextColor :
                        SuperMechUiSkin.TextDim;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = $"T{tech.tier} {tech.name}";

            string tid = tech.id;
            nodeBtn.onClick.AddListener(() => SelectTech(tid));
        }

        private void CreateVerticalLine(float x, float yTop, float yBottom)
        {
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(_treeContent, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 1);
            lineRect.anchorMax = new Vector2(0, 1);
            lineRect.pivot = new Vector2(0.5f, 1);
            lineRect.sizeDelta = new Vector2(2, yTop - yBottom);
            lineRect.anchoredPosition = new Vector2(x, yTop);
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(0.4f, 0.4f, 0.5f, 0.4f);
        }

        private void SelectTech(string id)
        {
            _selectedId = id;
            var def = SuperMechTech.GetDef(id);
            if (def == null) return;
            var kingdom = SelectedKingdom;
            long kid = kingdom != null ? kingdom.getID() : 0;
            bool researched = kid > 0 && SuperMechTech.IsResearched(kid, id);
            bool canResearch = kid > 0 && SuperMechTech.CanResearch(kid, id);
            float points = kid > 0 ? SuperMechCivilization.GetTechPointsFromKingdomById(kid) : 0;

            _detailText.text = $"<b><size=16><color=#E8C36A>{def.name}</color></size></b>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_tier")}:</color> T{def.tier}\n\n" +
                $"{def.desc}\n\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_effect")}:</color> ";
            var effects = new List<string>();
            if (def.dmgBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_damage")}+{def.dmgBonus * 100:F0}%");
            if (def.hpBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_health")}+{def.hpBonus * 100:F0}%");
            if (def.spdBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_speed")}+{def.spdBonus * 100:F0}%");
            _detailText.text += string.Join(" ", effects) + "\n";
            if (!string.IsNullOrEmpty(def.prerequisite))
            {
                var pre = SuperMechTech.GetDef(def.prerequisite);
                _detailText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_prerequisite")}:</color> {(pre != null ? pre.name : def.prerequisite)}\n";
            }
            _detailText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cost")}:</color> {def.cost} {LocalizedTextManager.getText("sm_ui_tech_points")}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_current")}:</color> {points:F0}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_status")}:</color> " +
                $"<color={(researched ? "#66ff99" : (canResearch ? "#ffcc66" : "#ff6666"))}>" +
                $"{(researched ? LocalizedTextManager.getText("sm_ui_researched") : (canResearch ? LocalizedTextManager.getText("sm_ui_available") : LocalizedTextManager.getText("sm_ui_locked")))}</color>";

            if (_researchBtn != null)
            {
                _researchBtn.gameObject.SetActive(!researched);
                _researchBtn.interactable = canResearch;
                _researchBtnText.text = canResearch ? LocalizedTextManager.getText("sm_ui_research") : LocalizedTextManager.getText("sm_ui_locked");
                var img = _researchBtn.GetComponent<Image>();
                if (img != null) img.color = canResearch ? new Color(0.2f, 0.5f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.9f);
            }

            // 高亮选中节点
            foreach (var kv in _nodeImages)
            {
                bool isSelected = kv.Key == id;
                bool r = kid > 0 && SuperMechTech.IsResearched(kid, kv.Key);
                bool cr = kid > 0 && SuperMechTech.CanResearch(kid, kv.Key);
                kv.Value.color = isSelected ? SuperMechUiSkin.AccentDim :
                                 r ? new Color(0.12f, 0.3f, 0.18f, 0.95f) :
                                 cr ? new Color(0.25f, 0.2f, 0.08f, 0.95f) :
                                 new Color(0.15f, 0.15f, 0.18f, 0.95f);
            }
        }

        private void OnResearchClicked()
        {
            var kingdom = SelectedKingdom;
            if (kingdom == null || string.IsNullOrEmpty(_selectedId)) return;
            long kid = kingdom.getID();
            if (SuperMechTech.Research(kid, _selectedId))
            {
                RefreshTree();
                SelectTech(_selectedId);
            }
        }
    }
}
