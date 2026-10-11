using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>科技树窗口（v0.99.4重构：多分支布局，参考文明/钢铁雄心）
    /// T1共同基础在左，3分支向右展开，节点间有连线</summary>
    public class SuperMechTechView : MonoBehaviour
    {
        private RectTransform _treeContent;
        private Text _detailText;
        private Button _researchBtn;
        private Text _researchBtnText;
        private string _selectedId;
        private SuperMechTech.TechBranch _currentBranch = SuperMechTech.TechBranch.Mech;
        private static Kingdom _selectedKingdom;
        private readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>();

        public static Kingdom SelectedKingdom
        {
            get => _selectedKingdom != null ? _selectedKingdom : (World.world != null && World.world.kingdoms != null && World.world.kingdoms.list.Count > 0 ? World.world.kingdoms.list[0] : null);
            set => _selectedKingdom = value;
        }

        void Awake()
        {
            try { BuildUI(); SuperMechTech.Init(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] TechView初始化失败: " + e.Message); }
        }

        private void BuildUI()
        {
            // 顶部系别标签
            var barGo = new GameObject("BranchBar");
            barGo.transform.SetParent(transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0.5f, 1);
            barRect.sizeDelta = new Vector2(0, 32);
            barRect.anchoredPosition = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.08f, 0.1f, 0.16f, 0.9f);

            string[] branches = { "sm_ui_tech_mech", "sm_ui_tech_martial", "sm_ui_tech_mage", "sm_ui_tech_psi", "sm_ui_tech_mind" };
            for (int i = 0; i < branches.Length; i++)
            {
                var btnGo = new GameObject($"Branch_{i}");
                btnGo.transform.SetParent(barGo.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(i * 0.2f, 0);
                btnRect.anchorMax = new Vector2((i + 1) * 0.2f, 1);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                var btn = btnGo.AddComponent<Button>();
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = i == 0 ? new Color(0.2f, 0.4f, 0.7f, 0.9f) : new Color(0.15f, 0.18f, 0.25f, 0.9f);
                var txt = btnGo.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 12;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.text = LocalizedTextManager.getText(branches[i]);
                int idx = i;
                btn.onClick.AddListener(() => SwitchBranch(idx));
            }

            // 左侧树状图区域（60%宽度，支持横向滚动）
            var treeGo = new GameObject("TreeArea");
            treeGo.transform.SetParent(transform, false);
            var treeRect = treeGo.AddComponent<RectTransform>();
            treeRect.anchorMin = Vector2.zero;
            treeRect.anchorMax = new Vector2(0.6f, 1);
            treeRect.offsetMin = Vector2.zero;
            treeRect.offsetMax = new Vector2(0, -32);

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
            _treeContent.anchorMin = new Vector2(0, 0.5f);
            _treeContent.anchorMax = new Vector2(0, 0.5f);
            _treeContent.pivot = new Vector2(0, 0.5f);
            _treeContent.sizeDelta = new Vector2(900, 400);
            var treeContentHit = treeContentGo.AddComponent<Image>();
            treeContentHit.color = new Color(0f, 0f, 0f, 0f);
            treeContentHit.raycastTarget = true;
            treeScrollRect.content = _treeContent;
            treeScrollRect.viewport = treeViewportRt;
            treeScrollRect.vertical = true;
            treeScrollRect.horizontal = true;
            treeScrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 右侧详情
            var detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.6f, 0);
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = new Vector2(0, -32);

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
            _researchBtnText = researchBtnGo.AddComponent<Text>();
            _researchBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _researchBtnText.fontSize = 14;
            _researchBtnText.color = Color.white;
            _researchBtnText.alignment = TextAnchor.MiddleCenter;
            _researchBtnText.text = LocalizedTextManager.getText("sm_ui_research");
            _researchBtn.onClick.AddListener(OnResearchClicked);
            researchBtnGo.SetActive(false);

            RefreshTree();
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
                    if (img != null) img.color = i == idx ? new Color(0.2f, 0.4f, 0.7f, 0.9f) : new Color(0.15f, 0.18f, 0.25f, 0.9f);
                }
            }
            RefreshTree();
            if (_detailText != null) _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");
            if (_researchBtn != null) _researchBtn.gameObject.SetActive(false);
        }

        /// <summary>构建多分支树状图：T1在左，3分支向右展开</summary>
        private void RefreshTree()
        {
            if (_treeContent == null) return;
            foreach (Transform child in _treeContent) Destroy(child.gameObject);
            _nodeImages.Clear();

            var kingdom = SelectedKingdom;
            long kid = kingdom != null ? kingdom.getID() : 0;
            var techs = SuperMechTech.GetByBranch(_currentBranch);

            // 布局参数
            float colWidth = 160f;
            float rowHeight = 70f;
            float startX = 20f;
            float centerY = 200f;

            // 按tier和subBranch分组
            var t1 = techs.Find(t => t.tier == 1);
            var branchA = techs.FindAll(t => t.subBranch == SuperMechTech.TechSubBranch.A);
            var branchB = techs.FindAll(t => t.subBranch == SuperMechTech.TechSubBranch.B);
            var branchC = techs.FindAll(t => t.subBranch == SuperMechTech.TechSubBranch.C);
            branchA.Sort((a, b) => a.tier.CompareTo(b.tier));
            branchB.Sort((a, b) => a.tier.CompareTo(b.tier));
            branchC.Sort((a, b) => a.tier.CompareTo(b.tier));

            // T1节点（左侧居中）
            if (t1 != null)
            {
                CreateNode(t1, startX, centerY, kid);
                // T1到3个分支T2的连线
                CreateLine(startX + 60, centerY, startX + colWidth - 10, centerY - rowHeight);
                CreateLine(startX + 60, centerY, startX + colWidth - 10, centerY);
                CreateLine(startX + 60, centerY, startX + colWidth - 10, centerY + rowHeight);
            }

            // 3分支（A上/B中/C下）
            float[] branchY = { centerY - rowHeight, centerY, centerY + rowHeight };
            var branches = new List<List<SuperMechTech.TechDef>> { branchA, branchB, branchC };
            for (int b = 0; b < 3; b++)
            {
                for (int i = 0; i < branches[b].Count; i++)
                {
                    var tech = branches[b][i];
                    float x = startX + colWidth + i * colWidth;
                    float y = branchY[b];
                    CreateNode(tech, x, y, kid);
                    // 分支内连线
                    if (i > 0)
                    {
                        CreateLine(x - colWidth + 60, y, x - 10, y);
                    }
                }
            }
        }

        private void CreateNode(SuperMechTech.TechDef tech, float x, float y, long kid)
        {
            bool researched = kid > 0 && SuperMechTech.IsResearched(kid, tech.id);
            bool canResearch = kid > 0 && SuperMechTech.CanResearch(kid, tech.id);

            var nodeGo = new GameObject($"Node_{tech.id}");
            nodeGo.transform.SetParent(_treeContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0, 0.5f);
            nodeRect.anchorMax = new Vector2(0, 0.5f);
            nodeRect.pivot = new Vector2(0, 0.5f);
            nodeRect.sizeDelta = new Vector2(140, 50);
            nodeRect.anchoredPosition = new Vector2(x, y);
            var nodeBtn = nodeGo.AddComponent<Button>();
            var nodeImg = nodeGo.AddComponent<Image>();
            nodeImg.color = researched ? new Color(0.15f, 0.35f, 0.2f, 0.95f) :
                            canResearch ? new Color(0.3f, 0.25f, 0.1f, 0.95f) :
                            new Color(0.18f, 0.18f, 0.22f, 0.95f);
            _nodeImages[tech.id] = nodeImg;

            // 状态圆点
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(nodeGo.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0, 0.5f);
            dotRect.anchorMax = new Vector2(0, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(10, 10);
            dotRect.anchoredPosition = new Vector2(10, 0);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = researched ? new Color(0.4f, 1f, 0.5f) :
                          canResearch ? new Color(1f, 0.8f, 0.3f) :
                          new Color(0.5f, 0.5f, 0.5f);
            dotImg.sprite = Resources.GetBuiltinResource<Sprite>("Background");

            // 科技名称
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(nodeGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(24, 0); txtRect.offsetMax = new Vector2(-4, 0);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.color = researched ? new Color(0.6f, 1f, 0.7f) :
                        canResearch ? SuperMechUiSkin.TextColor :
                        SuperMechUiSkin.TextDim;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = tech.name;

            string tid = tech.id;
            nodeBtn.onClick.AddListener(() => SelectTech(tid));
        }

        private void CreateLine(float x1, float y1, float x2, float y2)
        {
            var lineGo = new GameObject("Line");
            lineGo.transform.SetParent(_treeContent, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0.5f);
            lineRect.anchorMax = new Vector2(0, 0.5f);
            lineRect.pivot = new Vector2(0, 0.5f);
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            lineRect.sizeDelta = new Vector2(len, 2);
            lineRect.anchoredPosition = new Vector2(x1, y1);
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(0.4f, 0.4f, 0.5f, 0.5f);
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

            string branchName = def.subBranch == SuperMechTech.TechSubBranch.Common ? LocalizedTextManager.getText("sm_ui_tech_common") :
                               def.subBranch == SuperMechTech.TechSubBranch.A ? LocalizedTextManager.getText("sm_ui_tech_branch_a") :
                               def.subBranch == SuperMechTech.TechSubBranch.B ? LocalizedTextManager.getText("sm_ui_tech_branch_b") :
                               LocalizedTextManager.getText("sm_ui_tech_branch_c");

            _detailText.text = $"<b><size=16><color=#59a5ff>{def.name}</color></size></b>\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_tech_branch")}:</color> {branchName} | <color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_tier")}:</color> T{def.tier}\n\n" +
                $"{def.desc}\n\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_effect")}:</color> ";
            var effects = new List<string>();
            if (def.dmgBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_damage")}+{def.dmgBonus * 100:F0}%");
            if (def.hpBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_health")}+{def.hpBonus * 100:F0}%");
            if (def.spdBonus > 0) effects.Add($"{LocalizedTextManager.getText("sm_ui_speed")}+{def.spdBonus * 100:F0}%");
            _detailText.text += string.Join(" ", effects) + "\n";
            if (!string.IsNullOrEmpty(def.prerequisite))
            {
                var pre = SuperMechTech.GetDef(def.prerequisite);
                _detailText.text += $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_prerequisite")}:</color> {(pre != null ? pre.name : def.prerequisite)}\n";
            }
            _detailText.text += $"<color=#8fa8c8>消耗:</color> {def.cost} {LocalizedTextManager.getText("sm_ui_tech_points")}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_current")}:</color> {points:F0}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_status")}:</color> " +
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
                                 r ? new Color(0.15f, 0.35f, 0.2f, 0.95f) :
                                 cr ? new Color(0.3f, 0.25f, 0.1f, 0.95f) :
                                 new Color(0.18f, 0.18f, 0.22f, 0.95f);
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
