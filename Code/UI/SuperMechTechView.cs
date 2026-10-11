using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>科技树窗口：5系科技树，显示已研究/可研究/未解锁状态，点击研究</summary>
    public class SuperMechTechView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _detailText;
        private Button _researchBtn;
        private Text _researchBtnText;
        private string _selectedId;
        private SuperMechTech.TechBranch _currentBranch = SuperMechTech.TechBranch.Mech;
        private static Kingdom _selectedKingdom;

        public static Kingdom SelectedKingdom
        {
            get => _selectedKingdom != null ? _selectedKingdom : (World.world != null && World.world.kingdoms != null && World.world.kingdoms.list.Count > 0 ? World.world.kingdoms.list[0] : null);
            set => _selectedKingdom = value;
        }

        void Awake()
        {
            try
            {
                BuildUI();
                SuperMechTech.Init();
            }
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
                btn.onClick.AddListener(() => SwitchBranch(idx, btnImg));
            }

            // 左侧科技列表
            var listGo = new GameObject("TechList");
            listGo.transform.SetParent(transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = new Vector2(0.5f, 1);
            listRect.pivot = new Vector2(0, 0.5f);
            listRect.offsetMin = new Vector2(0, 0);
            listRect.offsetMax = new Vector2(0, -32);

            var listScrollGo = new GameObject("Scroll");
            listScrollGo.transform.SetParent(listGo.transform, false);
            var listScrollRect = listScrollGo.AddComponent<ScrollRect>();
            var lsr = listScrollGo.GetComponent<RectTransform>();
            lsr.anchorMin = Vector2.zero; lsr.anchorMax = Vector2.one;
            lsr.offsetMin = new Vector2(4, 4); lsr.offsetMax = new Vector2(-4, -4);
            var listMaskImg = listScrollGo.AddComponent<Image>();
            listMaskImg.color = new Color(0f, 0f, 0f, 0.01f);
            listMaskImg.raycastTarget = false;
            var listMask = listScrollGo.AddComponent<Mask>();
            listMask.showMaskGraphic = true;

            var listViewportGo = new GameObject("Viewport");
            listViewportGo.transform.SetParent(listScrollGo.transform, false);
            var listViewportRt = listViewportGo.AddComponent<RectTransform>();
            listViewportRt.anchorMin = Vector2.zero; listViewportRt.anchorMax = Vector2.one;
            listViewportRt.offsetMin = Vector2.zero; listViewportRt.offsetMax = Vector2.zero;

            var listContentGo = new GameObject("Content");
            listContentGo.transform.SetParent(listViewportGo.transform, false);
            _listContent = listContentGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0, 1);
            _listContent.sizeDelta = new Vector2(0, 0);
            var listContentHit = listContentGo.AddComponent<Image>();
            listContentHit.color = new Color(0f, 0f, 0f, 0f);
            listContentHit.raycastTarget = true;
            var listVlg = listContentGo.AddComponent<VerticalLayoutGroup>();
            listVlg.spacing = 2;
            listVlg.padding = new RectOffset(4, 4, 4, 4);
            listVlg.childControlHeight = true;
            listVlg.childControlWidth = true;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;
            var listFitter = listContentGo.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listScrollRect.content = _listContent;
            listScrollRect.viewport = listViewportRt;
            listScrollRect.vertical = true;
            listScrollRect.horizontal = false;
            listScrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 右侧详情
            var detailGo = new GameObject("Detail");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.5f, 0);
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

            RefreshList();
        }

        private void SwitchBranch(int idx, Image btnImg)
        {
            _currentBranch = (SuperMechTech.TechBranch)idx;
            _selectedId = null;
            // 更新标签颜色
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
            RefreshList();
            if (_detailText != null) _detailText.text = LocalizedTextManager.getText("sm_ui_select_tech");
            if (_researchBtn != null) _researchBtn.gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var kingdom = SelectedKingdom;
            long kid = kingdom != null ? kingdom.getID() : 0;
            var techs = SuperMechTech.GetByBranch(_currentBranch);

            foreach (var tech in techs)
            {
                var go = new GameObject($"Tech_{tech.id}");
                go.transform.SetParent(_listContent, false);
                go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
                var btn = go.AddComponent<Button>();
                var img = go.AddComponent<Image>();
                bool researched = kid > 0 && SuperMechTech.IsResearched(kid, tech.id);
                bool selected = _selectedId == tech.id;
                img.color = selected ? SuperMechUiSkin.AccentDim :
                              researched ? new Color(0.15f, 0.3f, 0.2f, 0.9f) : SuperMechUiSkin.RowOdd;
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(go.transform, false);
                var tr = txtGo.AddComponent<RectTransform>();
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(8, 0); tr.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 12;
                txt.color = researched ? new Color(0.5f, 1f, 0.6f) : SuperMechUiSkin.TextColor;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.text = (researched ? "✓ " : "○ ") + tech.name + $"  <color=#8fa8c8>[{tech.cost}]</color>";
                string tid = tech.id;
                btn.onClick.AddListener(() => SelectTech(tid));
            }
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

            _detailText.text = $"<b><size=16><color=#59a5ff>{def.name}</color></size></b>\n\n" +
                $"{def.desc}\n\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_tier")}:</color> {def.tier}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_cost")}:</color> {def.cost} {LocalizedTextManager.getText("sm_ui_tech_points")}\n" +
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
            RefreshList();
        }

        private void OnResearchClicked()
        {
            var kingdom = SelectedKingdom;
            if (kingdom == null || string.IsNullOrEmpty(_selectedId)) return;
            long kid = kingdom.getID();
            if (SuperMechTech.Research(kid, _selectedId))
            {
                SelectTech(_selectedId);
            }
        }
    }
}
