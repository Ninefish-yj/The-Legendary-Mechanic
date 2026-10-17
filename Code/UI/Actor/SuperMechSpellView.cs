using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 法术学习窗口：左法术列表（按分支分组）+ 右详情 + 学习按钮
    /// 原著ch404：法术分阶段学习，成功率取决于智力和神秘属性
    /// </summary>
    public class SuperMechSpellView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _detailContent;
        private Transform _detailContainer;
        private Text _detailText;
        private Button _learnBtn;
        private Text _learnBtnText;
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
            catch (System.Exception e) { Debug.LogError("[超神机械师] SMSpellView初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 左侧列表面板
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
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
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.offsetMin = new Vector2(210, 0);
            detailRect.offsetMax = Vector2.zero;

            var (dScroll, dContent) = SuperMechUiSkin.CreateScrollArea(detailGo.transform, "DetailScroll");
            _detailContent = dContent;

            // v0.237 统一规范：详情区使用信息行容器
            var container = SuperMechUiBuilder.CreateInfoContainer(dContent, "DetailContainer");
            _detailContainer = container.transform;
            _detailText = SuperMechUiSkin.MakeText(container.transform, "", 14, TextAnchor.UpperLeft);
            var dtRect = _detailText.rectTransform;
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = Vector2.zero;
            dtRect.offsetMax = Vector2.zero;

            // 学习按钮
            var btnGo = new GameObject("LearnBtn");
            btnGo.transform.SetParent(detailGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.sizeDelta = new Vector2(160, 40);
            btnRect.anchoredPosition = new Vector2(0, 10);

            _learnBtn = btnGo.AddComponent<Button>();
            _learnBtnText = SuperMechUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText("sm_ui_spell_entry"), 14, TextAnchor.MiddleCenter);
            _learnBtnText.color = Color.white;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
            _learnBtn.targetGraphic = btnImg;
            _learnBtn.transition = Selectable.Transition.SpriteSwap;
            var lss = _learnBtn.spriteState;
            lss.highlightedSprite = SuperMechUiSkin.ButtonHoverSprite;
            lss.pressedSprite = SuperMechUiSkin.ButtonActiveSprite;
            _learnBtn.spriteState = lss;
            _learnBtn.onClick.AddListener(OnLearnClick);
            _learnBtn.interactable = false;
        }

        private void RefreshList()
        {
            foreach (Transform child in _listContent) Destroy(child.gameObject);
            _folded.Clear();

            var actor = SelectedActor;
            if (actor == null || !actor.hasTrait(SuperMechTraits.ClassMage))
            {
                SuperMechUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_wild"), 14, TextAnchor.MiddleCenter);
                return;
            }

            // 按分支分组
            for (int b = 0; b < 7; b++)
            {
                var branch = (SuperMechSpell.SpellBranch)b;
                var spells = SuperMechSpell.GetSpellsByBranch(branch);
                if (spells.Count == 0) continue;

                string branchName = LocalizedTextManager.getText(SuperMechSpell.BranchNameKeys[b]);
                _folded[branchName] = false;

                var header = SuperMechUiSkin.MakeButton(_listContent, "▼ " + branchName, 13, () => {
                    _folded[branchName] = !_folded[branchName];
                    RefreshList();
                });
                var headerImg = header.GetComponent<Image>();
                headerImg.sprite = SuperMechUiSkin.Section;
                headerImg.type = Image.Type.Sliced;
                headerImg.color = Color.white;

                if (!_folded[branchName])
                {
                    foreach (var spell in spells)
                    {
                        bool learned = SuperMechSpell.IsLearned(actor, spell.id);
                        int phase = SuperMechSpell.GetCurrentPhase(actor, spell.id);
                        string status = learned ? "✓" : (phase > 0 ? $"{phase}/{spell.totalPhases}" : "");
                        string label = $"  {LocalizedTextManager.getText(spell.nameKey)} {status}";

                        var btn = SuperMechUiSkin.MakeButton(_listContent, label, 12, () => {
                            _selectedId = spell.id;
                            RefreshDetail();
                        });
                        if (_selectedId == spell.id)
                        {
                            var btnImg = btn.GetComponent<Image>();
                            btnImg.sprite = SuperMechUiSkin.ButtonSelectedSprite;
                            btnImg.type = Image.Type.Sliced;
                            btnImg.color = Color.white;
                        }
                    }
                }
            }
        }

        private void RefreshDetail()
        {
            var actor = SelectedActor;
            if (actor == null || string.IsNullOrEmpty(_selectedId))
            {
                if (_detailContainer != null)
                {
                    SuperMechUiBuilder.ClearChildren(_detailContainer);
                    SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_status"), LocalizedTextManager.getText("sm_ui_select_spell_hint"), 100, SuperMechUiSkin.TextDim);
                }
                _learnBtn.interactable = false;
                return;
            }

            var spell = SuperMechSpell.GetSpell(_selectedId);
            if (spell == null) return;

            bool learned = SuperMechSpell.IsLearned(actor, _selectedId);
            int phase = SuperMechSpell.GetCurrentPhase(actor, _selectedId);
            bool canLearn = SuperMechSpell.CanLearn(actor, _selectedId) && !learned;
            float rate = SuperMechSpell.GetLearnSuccessRate(actor, _selectedId);
            int xpCost = phase < spell.totalPhases ? spell.xpCostPerPhase[phase] : 0;
            int currentXp = (int)SuperMechAwakened.GetXp(actor);

            string branchName = LocalizedTextManager.getText(SuperMechSpell.BranchNameKeys[(int)spell.branch]);
            string status = learned ? LocalizedTextManager.getText("sm_ui_spell_learned") :
                           (phase > 0 ? $"{LocalizedTextManager.getText("sm_ui_spell_learning")} {phase}/{spell.totalPhases}" :
                            LocalizedTextManager.getText("sm_ui_spell_locked"));
            Color statusColor = learned ? SuperMechUiSkin.SuccessGreen : (phase > 0 ? SuperMechUiSkin.WarningOrange : SuperMechUiSkin.TextDim);

            // v0.237 统一规范：信息行结构重建
            if (_detailContainer != null) SuperMechUiBuilder.ClearChildren(_detailContainer);

            SuperMechUiBuilder.CreateSectionTitle(_detailContainer, LocalizedTextManager.getText(spell.nameKey));
            SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_branch"), branchName);
            SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_tier"), "T" + spell.tier);
            SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_status"), status, 100, statusColor);
            SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_phase"), phase + "/" + spell.totalPhases);
            SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_success_rate"), rate.ToString("P0"));

            // 描述
            var descGo = SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_desc"), LocalizedTextManager.getText(spell.descKey), 100, SuperMechUiSkin.TextColor);

            if (!learned)
            {
                string tCurrent = LocalizedTextManager.getText("sm_ui_spell_current");
                SuperMechUiBuilder.CreateInfoRow(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_xp_cost"), $"{xpCost}（{tCurrent}{currentXp}）", 100,
                    currentXp >= xpCost ? SuperMechUiSkin.SuccessGreen : SuperMechUiSkin.DangerRed);
            }

            if (spell.prerequisites.Length > 0)
            {
                SuperMechUiBuilder.CreateSectionTitle(_detailContainer, LocalizedTextManager.getText("sm_ui_spell_prereq"));
                foreach (var pre in spell.prerequisites)
                {
                    var preSpell = SuperMechSpell.GetSpell(pre);
                    if (preSpell != null)
                    {
                        bool preLearned = SuperMechSpell.IsLearned(actor, pre);
                        SuperMechUiBuilder.CreateInfoRow(_detailContainer,
                            preLearned ? "✓" : "✗",
                            LocalizedTextManager.getText(preSpell.nameKey), 30,
                            preLearned ? SuperMechUiSkin.SuccessGreen : SuperMechUiSkin.DangerRed);
                    }
                }
            }

            _learnBtn.interactable = canLearn && currentXp >= xpCost;
            _learnBtnText.text = learned ? LocalizedTextManager.getText("sm_ui_spell_learned") :
                               (phase > 0 ? LocalizedTextManager.getText("sm_ui_spell_phase") + (phase + 1) : LocalizedTextManager.getText("sm_ui_spell_entry"));
        }

        private void OnLearnClick()
        {
            var actor = SelectedActor;
            if (actor == null || string.IsNullOrEmpty(_selectedId)) return;

            bool success = SuperMechSpell.TryLearnPhase(actor, _selectedId);
            if (success)
            {
                RefreshList();
                RefreshDetail();
            }
            else
            {
                // 学习失败：闪烁按钮
                _learnBtnText.color = Color.red;
                Invoke(nameof(ResetBtnColor), 0.5f);
            }
        }

        private void ResetBtnColor()
        {
            _learnBtnText.color = Color.white;
        }

        void OnEnable()
        {
            try
            {
                RefreshList();
                RefreshDetail();
            }
            catch (System.Exception e) { Debug.LogError("[超神机械师] SpellView刷新失败: " + e); }
        }
    }
}
