using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识融合窗口：左配方列表（按分支分组）+ 右详情 + 融合按钮
    /// 从单位面板打开时查看指定单位，否则查看全局选中单位
    /// </summary>
    public class SMFusionView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _detailContent;
        private Text _detailText;
        private Button _fuseBtn;
        private Text _fuseBtnText;
        private string _selectedId;
        private readonly Dictionary<string, bool> _folded = new Dictionary<string, bool>();

        public static Actor OverrideActor;
        private static Actor SelectedActor => OverrideActor != null ? OverrideActor : SelectedUnit.unit;

        void Awake()
        {
            try
            {
                BuildLayout();
                RefreshList();
            }
            catch (System.Exception e) { Debug.LogError("[超神机械师] SMFusionView初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 左侧列表面板（固定宽度200px）
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 0.5f);
            listRect.sizeDelta = new Vector2(200, 0);
            listRect.anchoredPosition = Vector2.zero;

            var (scroll, content) = SMUiSkin.CreateScrollArea(listGo.transform, "Scroll");
            _listContent = content;

            // 右侧详情面板
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.offsetMin = new Vector2(210, 0);
            detailRect.offsetMax = Vector2.zero;

            // 详情卡片背景
            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4);
            cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = SMUiSkin.CardBg;
            _detailContent = cardRect;

            // 详情滚动区
            var detailScrollGo = new GameObject("DetailScroll");
            detailScrollGo.transform.SetParent(cardGo.transform, false);
            var detailScrollRect = detailScrollGo.AddComponent<ScrollRect>();
            var dsr = detailScrollGo.GetComponent<RectTransform>();
            dsr.anchorMin = Vector2.zero; dsr.anchorMax = Vector2.one;
            dsr.offsetMin = new Vector2(8, 8); dsr.offsetMax = new Vector2(-8, 50);
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
            detailContentRect.anchorMax = new Vector2(0, 1);
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

            // 详情文字
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
            _detailText.color = SMUiSkin.TextColor;
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            _detailText.text = LocalizedTextManager.getText("sm_ui_select_recipe");

            // 底部融合按钮
            var btnGo = new GameObject("FuseBtn");
            btnGo.transform.SetParent(cardGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 0);
            btnRect.anchorMax = new Vector2(1, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.sizeDelta = new Vector2(0, 36);
            btnRect.anchoredPosition = new Vector2(0, 6);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.25f, 0.45f, 0.95f);
            _fuseBtn = btnGo.AddComponent<Button>();
            var btnColors = _fuseBtn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(0.8f, 0.9f, 1f, 1f);
            btnColors.pressedColor = new Color(0.5f, 0.6f, 0.8f, 1f);
            btnColors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            _fuseBtn.colors = btnColors;
            _fuseBtn.onClick.AddListener(OnFuseClick);
            _fuseBtn.interactable = false;

            _fuseBtnText = SMUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText("sm_ui_fuse"), 14, TextAnchor.MiddleCenter);
            _fuseBtnText.fontStyle = FontStyle.Bold;
            _fuseBtnText.color = Color.white;
            var btnTextRect = _fuseBtnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
        }

        private void RefreshList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var branches = new[] { SuperMechKnowledgeRecipe.FusionBranch.Weapon, SuperMechKnowledgeRecipe.FusionBranch.Energy, SuperMechKnowledgeRecipe.FusionBranch.Control };
            var branchNames = new Dictionary<SuperMechKnowledgeRecipe.FusionBranch, string>
            {
                { SuperMechKnowledgeRecipe.FusionBranch.Weapon, "sm_fusion_branch_weapon" },
                { SuperMechKnowledgeRecipe.FusionBranch.Energy, "sm_fusion_branch_energy" },
                { SuperMechKnowledgeRecipe.FusionBranch.Control, "sm_fusion_branch_control" }
            };

            foreach (var branch in branches)
            {
                var recipes = SuperMechKnowledgeRecipe.GetByBranch(branch);
                if (recipes == null || recipes.Count == 0) continue;

                string bKey = branch.ToString();
                if (!_folded.ContainsKey(bKey)) _folded[bKey] = false;

                // 分支标题（可折叠）
                var catGo = new GameObject($"Cat_{bKey}");
                catGo.transform.SetParent(_listContent, false);
                catGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
                var catImg = catGo.AddComponent<Image>();
                catImg.color = SMUiSkin.SectionBg;
                var catBtn = catGo.AddComponent<Button>();
                string bk = bKey;
                catBtn.onClick.AddListener(() => ToggleCategory(bk));

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
                catText.text = (_folded[bKey] ? "▶ " : "▼ ") + LocalizedTextManager.getText(branchNames[branch]);

                if (!_folded[bKey])
                {
                    foreach (var recipe in recipes)
                    {
                        var rGo = new GameObject($"Recipe_{recipe.id}");
                        rGo.transform.SetParent(_listContent, false);
                        rGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
                        var rBtn = rGo.AddComponent<Button>();
                        var rImg = rGo.AddComponent<Image>();
                        bool canFuse = SelectedActor != null && SuperMechKnowledgeRecipe.CanFuse(SelectedActor, recipe);
                        bool selected = _selectedId == recipe.id;
                        rImg.color = selected ? SMUiSkin.AccentDim :
                                      (canFuse ? SMUiSkin.RowEven : SMUiSkin.RowOdd);
                        var textGo = new GameObject("Text");
                        textGo.transform.SetParent(rGo.transform, false);
                        var tr = textGo.AddComponent<RectTransform>();
                        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                        tr.offsetMin = new Vector2(20, 0); tr.offsetMax = Vector2.zero;
                        var rt = textGo.AddComponent<Text>();
                        rt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        rt.fontSize = 12;
                        rt.color = canFuse ? SMUiSkin.TextColor : SMUiSkin.TextDim;
                        rt.alignment = TextAnchor.MiddleLeft;
                        rt.text = (canFuse ? "● " : "○ ") + LocalizedTextManager.getText(recipe.nameKey) + $" (T{recipe.tier})";
                        string rid = recipe.id;
                        rBtn.onClick.AddListener(() => SelectRecipe(rid));
                    }
                }
            }
        }

        private void ToggleCategory(string key)
        {
            _folded[key] = !_folded[key];
            RefreshList();
        }

        private void SelectRecipe(string id)
        {
            _selectedId = id;
            var recipe = SuperMechKnowledgeRecipe.GetById(id);
            if (recipe == null) return;

            bool canFuse = SelectedActor != null && SuperMechKnowledgeRecipe.CanFuse(SelectedActor, recipe);
            bool hasKnowledge = SelectedActor != null && SuperMechKnowledgeRecipe.HasRequiredKnowledge(SelectedActor, recipe);
            float qi = SelectedActor != null ? SuperMechQi.GetQi(SelectedActor) : 0;
            float qiCost = Mathf.Max(10, recipe.qiBonus * 0.5f);

            string reqKnowText = "";
            foreach (var kid in recipe.requiredKnowledge)
            {
                var kn = SuperMechKnowledge.GetDef(kid);
                string knName = kn != null ? kn.name : kid;
                bool unlocked = SelectedActor != null && SuperMechKnowledge.IsUnlocked(SelectedActor, kid);
                reqKnowText += $"  {(unlocked ? "✓" : "✗")} {knName}\n";
            }

            string tCurrent = LocalizedTextManager.getText("sm_ui_fusion_current");
            string tDamage = LocalizedTextManager.getText("sm_ui_fusion_damage");
            string tHealth = LocalizedTextManager.getText("sm_ui_fusion_health");
            string tSpeed = LocalizedTextManager.getText("sm_ui_fusion_speed");
            string tQi = LocalizedTextManager.getText("sm_ui_fusion_qi");
            _detailText.text = $"<b><size=16><color=#59a5ff>{LocalizedTextManager.getText(recipe.nameKey)}</color></size></b>\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText(recipe.descKey)}</color>\n\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_branch")}:</color> {SuperMechKnowledgeRecipe.GetBranchName(recipe.branch)}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_tier")}:</color> {recipe.tier}\n\n" +
                $"<b>{LocalizedTextManager.getText("sm_ui_required_knowledge")}</b>\n{reqKnowText}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_qi_cost")}:</color> {qiCost:F0} ({tCurrent}: {qi:F0})\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_success_rate")}:</color> {(recipe.successRate * 100):F0}%\n\n" +
                $"<b>{LocalizedTextManager.getText("sm_ui_result")}</b>\n" +
                $"  {tDamage} ×{recipe.dmgMul:F1}\n" +
                $"  {tHealth} ×{recipe.hpMul:F1}\n" +
                $"  {tSpeed} ×{recipe.speedMul:F1}\n" +
                $"  {tQi} +{recipe.qiBonus}\n\n" +
                $"<color={(canFuse ? "#66ff99" : "#ff9966")}>{(canFuse ? LocalizedTextManager.getText("sm_ui_can_fuse") : (hasKnowledge ? LocalizedTextManager.getText("sm_ui_no_xp") : LocalizedTextManager.getText("sm_ui_no_knowledge")))}</color>";

            _fuseBtn.interactable = canFuse;
            RefreshList();
        }

        private void OnFuseClick()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(_selectedId)) return;
            var recipe = SuperMechKnowledgeRecipe.GetById(_selectedId);
            if (recipe == null) return;

            bool success = SuperMechKnowledgeRecipe.TryFuse(SelectedActor, recipe);
            string resultKey = success ? "sm_ui_fuse_success" : "sm_ui_fuse_fail";
            _detailText.text += $"\n\n<b><color={(success ? "#66ff99" : "#ff6666")}>{LocalizedTextManager.getText(resultKey)}</color></b>";
            _fuseBtn.interactable = SuperMechKnowledgeRecipe.CanFuse(SelectedActor, recipe);
            RefreshList();
        }
    }
}
