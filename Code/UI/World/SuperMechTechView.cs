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
        private Text _statusText;
        private string _selectedId;
        private SuperMechTech.TechBranch _currentBranch = SuperMechTech.TechBranch.Energy;
        private readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>();
        private static readonly Color BranchEnergy = new Color(0.96f, 0.65f, 0.14f);
        private static readonly Color BranchSpace = new Color(0.31f, 0.76f, 0.97f);
        private static readonly Color BranchInfo = new Color(0.73f, 0.41f, 0.78f);
        private static readonly Color BranchMech = new Color(0.91f, 0.76f, 0.41f);
        private static readonly Color BranchBio = new Color(0.40f, 0.73f, 0.42f);
        private static readonly Color BranchMilitary = new Color(0.94f, 0.33f, 0.31f);

        /// <summary>v0.206 统一使用WindowManager.SelectedKingdomId，跨标签页联动</summary>
        public static Kingdom SelectedKingdom
        {
            get
            {
                if (World.world == null || World.world.kingdoms == null) return null;
                long id = SuperMechWindowManager.SelectedKingdomId;
                if (id >= 0)
                {
                    foreach (var k in World.world.kingdoms.list)
                        if (k != null && k.getID() == id) return k;
                }
                return World.world.kingdoms.list.Count > 0 ? World.world.kingdoms.list[0] : null;
            }
            set
            {
                SuperMechWindowManager.SelectedKingdomId = value != null ? value.getID() : -1;
            }
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
            titleImg.sprite = SuperMechUiSkin.Section;
            titleImg.type = Image.Type.Sliced;
            titleImg.color = Color.white;

            var titleTextGo = new GameObject("Title");
            titleTextGo.transform.SetParent(titleGo.transform, false);
            var titleTextRect = titleTextGo.AddComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero; titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(8, 0); titleTextRect.offsetMax = new Vector2(-8, 0);
            var titleText = titleTextGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 14;
            titleText.color = SuperMechUiSkin.TextColor;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = LocalizedTextManager.getText("sm_ui_civ_tech_tree");

            _kingdomNameText = titleText; // 复用，后面更新

            // 总览信息条（v0.238.3 对齐帝国盒子：时代 · 科技点 · 已研究）
            var statusGo = new GameObject("StatusBar");
            statusGo.transform.SetParent(transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1);
            statusRect.sizeDelta = new Vector2(0, 24);
            statusRect.anchoredPosition = new Vector2(0, -28);
            var statusTextGo = new GameObject("StatusText");
            statusTextGo.transform.SetParent(statusGo.transform, false);
            var stRect = statusTextGo.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero; stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(10, 0); stRect.offsetMax = new Vector2(-10, 0);
            _statusText = statusTextGo.AddComponent<Text>();
            _statusText.font = SuperMechUiSkin.DefaultFont;
            _statusText.fontSize = 11;
            _statusText.color = SuperMechUiSkin.TextDim;
            _statusText.alignment = TextAnchor.MiddleLeft;

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
            prevImg.color = SuperMechUiSkin.ButtonNormal;
            var prevTxtGo = new GameObject("Label");
            prevTxtGo.transform.SetParent(prevBtnGo.transform, false);
            var prevTxtRect = prevTxtGo.AddComponent<RectTransform>();
            prevTxtRect.anchorMin = Vector2.zero; prevTxtRect.anchorMax = Vector2.one;
            var prevTxt = prevTxtGo.AddComponent<Text>();
            prevTxt.font = SuperMechUiSkin.DefaultFont;
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
            klImg.sprite = SuperMechUiSkin.Card;
            klImg.type = Image.Type.Sliced;
            klImg.color = Color.white;
            // v0.194 修复：Text必须放在子物体上，同一物体不能同时有Image和Text
            var klTextGo = new GameObject("Label");
            klTextGo.transform.SetParent(kingdomLabelGo.transform, false);
            var klTextRect = klTextGo.AddComponent<RectTransform>();
            klTextRect.anchorMin = Vector2.zero; klTextRect.anchorMax = Vector2.one;
            klTextRect.offsetMin = Vector2.zero; klTextRect.offsetMax = Vector2.zero;
            _kingdomLabelText = klTextGo.AddComponent<Text>();
            _kingdomLabelText.font = SuperMechUiSkin.DefaultFont;
            _kingdomLabelText.fontSize = 11;
            _kingdomLabelText.color = SuperMechUiSkin.TextColor;
            _kingdomLabelText.alignment = TextAnchor.MiddleCenter;

            // 右箭头
            var nextBtnGo = new GameObject("NextBtn");
            nextBtnGo.transform.SetParent(kingdomSwitchGo.transform, false);
            var nextRect = nextBtnGo.AddComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1, 0); nextRect.anchorMax = new Vector2(1, 1);
            nextRect.sizeDelta = new Vector2(24, 0);
            var nextBtn = nextBtnGo.AddComponent<Button>();
            var nextImg = nextBtnGo.AddComponent<Image>();
            nextImg.color = SuperMechUiSkin.ButtonNormal;
            var nextTxtGo = new GameObject("Label");
            nextTxtGo.transform.SetParent(nextBtnGo.transform, false);
            var nextTxtRect = nextTxtGo.AddComponent<RectTransform>();
            nextTxtRect.anchorMin = Vector2.zero; nextTxtRect.anchorMax = Vector2.one;
            var nextTxt = nextTxtGo.AddComponent<Text>();
            nextTxt.font = SuperMechUiSkin.DefaultFont;
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
            barRect.anchoredPosition = new Vector2(0, -52);
            var barImg = barGo.AddComponent<Image>();
            barImg.sprite = SuperMechUiSkin.ProgressBg;
            barImg.type = Image.Type.Sliced;
            barImg.color = Color.white;

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
                btnImg.sprite = i == 0 ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                btnImg.type = Image.Type.Sliced;
                btnImg.color = Color.white;
                // Text必须放在子对象上：Image和Text都是Graphic，同一GameObject只能有一个Graphic
                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRect = txtGo.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.font = SuperMechUiSkin.DefaultFont;
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
            treeRect.offsetMax = new Vector2(0, -90);

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
            detailRect.offsetMax = new Vector2(0, -90);

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero; cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4); cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            // v0.238.3 对齐暖棕规范：CardBg(深蓝) → PanelVanilla(暖棕)
            cardImg.color = SuperMechUiSkin.PanelVanilla;

            var detailTextGo = new GameObject("DetailText");
            detailTextGo.transform.SetParent(cardGo.transform, false);
            var dtRect = detailTextGo.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero; dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(8, 40); dtRect.offsetMax = new Vector2(-8, -8);
            _detailText = detailTextGo.AddComponent<Text>();
            _detailText.font = SuperMechUiSkin.DefaultFont;
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
            rbImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            rbImg.type = Image.Type.Sliced;
            rbImg.color = Color.white;
            // Text必须放在子对象上：Image和Text都是Graphic，同一GameObject只能有一个Graphic
            var rbTxtGo = new GameObject("Label");
            rbTxtGo.transform.SetParent(researchBtnGo.transform, false);
            var rbTxtRect = rbTxtGo.AddComponent<RectTransform>();
            rbTxtRect.anchorMin = Vector2.zero; rbTxtRect.anchorMax = Vector2.one;
            rbTxtRect.offsetMin = Vector2.zero; rbTxtRect.offsetMax = Vector2.zero;
            _researchBtnText = rbTxtGo.AddComponent<Text>();
            _researchBtnText.font = SuperMechUiSkin.DefaultFont;
            _researchBtnText.fontSize = 14;
            _researchBtnText.color = Color.white;
            _researchBtnText.alignment = TextAnchor.MiddleCenter;
            _researchBtnText.text = LocalizedTextManager.getText("sm_ui_research");
            _researchBtn.onClick.AddListener(OnResearchClicked);
            researchBtnGo.SetActive(false);

            RefreshTree();
            UpdateKingdomName();
        }

        private static string GetEraColor(SuperMechTech.Era era)
        {
            return era switch
            {
                SuperMechTech.Era.PreIndustrial => "#888888",
                SuperMechTech.Era.Industrial => "#cd853f",
                SuperMechTech.Era.Information => "#4fc3f7",
                SuperMechTech.Era.Interstellar => "#ba68c8",
                SuperMechTech.Era.Cosmic => "#ffd700",
                _ => "#ffffff"
            };
        }

        private void UpdateKingdomName()
        {
            var k = SelectedKingdom;
            string kName = (k != null && k.data != null) ? k.data.name : LocalizedTextManager.getText("sm_ui_none");
            // v0.175.1: 显示文明当前时代
            SuperMechTech.Era era = SuperMechTech.Era.PreIndustrial;
            if (k != null) era = SuperMechTech.GetEra(k.id);
            string eraName = SuperMechTech.GetEraName(era);
            string eraColor = GetEraColor(era);
            if (_kingdomNameText != null)
                _kingdomNameText.text = $"{LocalizedTextManager.getText("sm_ui_civ_tech_tree")} - {kName}  <color={eraColor}>[{eraName}]</color>";
            if (_kingdomLabelText != null)
                _kingdomLabelText.text = $"{kName}·{eraName}";
            RefreshStatus();
        }

        /// <summary>v0.238.3 总览信息条：时代 · 科技点 · 已研究（对齐帝国盒子顶部信息行）</summary>
        private void RefreshStatus()
        {
            if (_statusText == null) return;
            var k = SelectedKingdom;
            string eraName = "?", eraColor = "#ffffff";
            float pts = 0;
            int r = 0, n = 0;
            if (k != null)
            {
                var era = SuperMechTech.GetEra(k.id);
                eraName = SuperMechTech.GetEraName(era);
                eraColor = GetEraColor(era);
                pts = SuperMechCivilization.GetTechPointsFromKingdomById(k.id);
                var techs = SuperMechTech.GetByBranch(_currentBranch);
                n = techs.Count;
                foreach (var t in techs)
                    if (SuperMechTech.IsResearched(k.id, t.id)) r++;
            }
            _statusText.text = $"<color={eraColor}>[{eraName}]</color>   " +
                $"{LocalizedTextManager.getText("sm_ui_tech_points")} {pts:F0}   " +
                $"{LocalizedTextManager.getText("sm_ui_researched")} {r}/{n}";
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
                    if (img != null)
                    {
                        img.sprite = i == idx ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                        img.type = Image.Type.Sliced;
                        img.color = Color.white;
                    }
                }
            }
            RefreshTree();
            if (_detailText != null) _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");
            if (_researchBtn != null) _researchBtn.gameObject.SetActive(false);
        }

        /// <summary>构建网格状科技树（v0.238.3：单列垂直线性 → 2列网格，对齐帝国盒子横向排布）</summary>
        private void RefreshTree()
        {
            if (_treeContent == null) return;
            foreach (Transform child in _treeContent) Destroy(child.gameObject);
            _nodeImages.Clear();

            var kingdom = SelectedKingdom;
            long kid = kingdom != null ? kingdom.getID() : 0;
            var techs = SuperMechTech.GetByBranch(_currentBranch);

            float nodeW = 140f;
            float nodeH = 48f;
            float spacing = 16f;
            float colGap = 20f;
            int cols = 2;
            int rows = (techs.Count + cols - 1) / cols;
            float startX = 12f;
            float startY = -90f;

            float contentW = cols * nodeW + (cols - 1) * colGap + 32;
            float contentH = rows * nodeH + (rows - 1) * spacing + 110;
            _treeContent.sizeDelta = new Vector2(contentW, contentH);

            // 顶部水平脊线：连接所有列的顶部
            if (cols > 1)
            {
                float ridgeX1 = startX + nodeW * 0.5f;
                float ridgeX2 = startX + nodeW * 0.5f + (cols - 1) * (nodeW + colGap);
                CreateHLine(ridgeX1, ridgeX2, startY - nodeH * 0.5f);
            }
            // 每列垂直主线：从脊线向下延伸到该列末节点中心
            for (int c = 0; c < cols; c++)
            {
                int countInCol = Mathf.Min(rows, techs.Count - c * rows);
                if (countInCol <= 0) continue;
                float colX = startX + nodeW * 0.5f + c * (nodeW + colGap);
                float topY = startY - nodeH * 0.5f;
                float bottomY = startY - (countInCol - 1) * (nodeH + spacing) - nodeH * 0.5f;
                CreateVerticalLine(colX, topY, bottomY);
            }

            for (int i = 0; i < techs.Count; i++)
            {
                var tech = techs[i];
                int col = i / rows;   // 列主序：先填满左列
                int row = i % rows;
                float x = startX + col * (nodeW + colGap);
                float y = startY - row * (nodeH + spacing);
                CreateNode(tech, x, y, nodeW, nodeH, kid);
            }

            RefreshStatus();
        }

        private Color BranchColor()
        {
            return _currentBranch switch
            {
                SuperMechTech.TechBranch.Space => BranchSpace,
                SuperMechTech.TechBranch.Info => BranchInfo,
                SuperMechTech.TechBranch.Mech => BranchMech,
                SuperMechTech.TechBranch.Bio => BranchBio,
                SuperMechTech.TechBranch.Military => BranchMilitary,
                _ => BranchEnergy
            };
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
            nodeImg.color = researched ? new Color(0.12f, 0.32f, 0.18f, 0.95f) :
                            canResearch ? new Color(0.28f, 0.22f, 0.09f, 0.95f) :
                            new Color(0.16f, 0.16f, 0.19f, 0.95f);
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

            // v0.238.3 系别图标块（右侧留文字，左对齐）
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(nodeGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.sizeDelta = new Vector2(24, 24);
            iconRect.anchoredPosition = new Vector2(30, 0);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(BranchColor().r, BranchColor().g, BranchColor().b, researched ? 0.9f : 0.45f);

            // 科技名称（图标右侧）
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(nodeGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(58, 0); txtRect.offsetMax = new Vector2(-26, 0);
            var txt = txtGo.AddComponent<Text>();
            txt.font = SuperMechUiSkin.DefaultFont;
            txt.fontSize = 12;
            txt.color = researched ? new Color(0.6f, 1f, 0.7f) :
                        canResearch ? SuperMechUiSkin.TextColor :
                        SuperMechUiSkin.TextDim;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.text = tech.name;

            // 层级角标（右上）
            var tierGo = new GameObject("Tier");
            tierGo.transform.SetParent(nodeGo.transform, false);
            var tierRect = tierGo.AddComponent<RectTransform>();
            tierRect.anchorMin = new Vector2(1, 1);
            tierRect.anchorMax = new Vector2(1, 1);
            tierRect.pivot = new Vector2(1, 1);
            tierRect.sizeDelta = new Vector2(26, 14);
            tierRect.anchoredPosition = new Vector2(-4, -3);
            var tierTxt = tierGo.AddComponent<Text>();
            tierTxt.font = SuperMechUiSkin.DefaultFont;
            tierTxt.fontSize = 9;
            tierTxt.color = new Color(0.85f, 0.80f, 0.70f, 0.85f);
            tierTxt.alignment = TextAnchor.MiddleRight;
            tierTxt.text = $"T{tech.tier}";

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

        /// <summary>v0.238.3 水平连线（顶部脊线，连接各列）</summary>
        private void CreateHLine(float x1, float x2, float y)
        {
            var lineGo = new GameObject("HLine");
            lineGo.transform.SetParent(_treeContent, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.zero;
            lineRect.pivot = new Vector2(0, 0.5f);
            lineRect.sizeDelta = new Vector2(x2 - x1, 2);
            lineRect.anchoredPosition = new Vector2(x1, y);
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
                kv.Value.color = isSelected ? SuperMechUiSkin.CyanDim :
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
