using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 法术学习窗口：左法术列表（按分支分组）+ 右详情 + 学习按钮
    /// 原著ch404：法术分阶段学习，成功率取决于智力和神秘属性
    /// </summary>
    public class SMSpellView : MonoBehaviour
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

            var (dScroll, dContent) = SMUiSkin.CreateScrollArea(detailGo.transform, "DetailScroll");
            _detailContent = dContent;

            _detailText = SMUiSkin.MakeText(dContent, "", 14, TextAnchor.UpperLeft);
            var dtRect = _detailText.rectTransform;
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(10, 60);
            dtRect.offsetMax = new Vector2(-10, -10);

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
            _learnBtnText = SMUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText("sm_ui_spell_entry"), 14, TextAnchor.MiddleCenter);
            _learnBtnText.color = Color.white;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.5f, 0.8f);
            _learnBtn.targetGraphic = btnImg;
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
                SMUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_wild"), 14, TextAnchor.MiddleCenter);
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

                var header = SMUiSkin.MakeButton(_listContent, "▼ " + branchName, 13, () => {
                    _folded[branchName] = !_folded[branchName];
                    RefreshList();
                });
                header.GetComponent<Image>().color = new Color(0.2f, 0.3f, 0.5f);

                if (!_folded[branchName])
                {
                    foreach (var spell in spells)
                    {
                        bool learned = SuperMechSpell.IsLearned(actor, spell.id);
                        int phase = SuperMechSpell.GetCurrentPhase(actor, spell.id);
                        string status = learned ? "✓" : (phase > 0 ? $"{phase}/{spell.totalPhases}" : "");
                        string label = $"  {LocalizedTextManager.getText(spell.nameKey)} {status}";

                        var btn = SMUiSkin.MakeButton(_listContent, label, 12, () => {
                            _selectedId = spell.id;
                            RefreshDetail();
                        });
                        if (_selectedId == spell.id)
                            btn.GetComponent<Image>().color = new Color(0.3f, 0.4f, 0.6f);
                    }
                }
            }
        }

        private void RefreshDetail()
        {
            var actor = SelectedActor;
            if (actor == null || string.IsNullOrEmpty(_selectedId))
            {
                _detailText.text = "";
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

            string text = $"<b>{LocalizedTextManager.getText(spell.nameKey)}</b>\n";
            text += $"{branchName} · T{spell.tier} · {status}\n\n";
            text += $"{LocalizedTextManager.getText(spell.descKey)}\n\n";
            text += $"{LocalizedTextManager.getText("sm_ui_spell_phase")}: {phase}/{spell.totalPhases}\n";
            text += $"{LocalizedTextManager.getText("sm_ui_spell_success_rate")}: {rate:F0%}\n";
            if (!learned)
            {
                string tCurrent = LocalizedTextManager.getText("sm_ui_spell_current");
                text += $"{LocalizedTextManager.getText("sm_ui_spell_xp_cost")}: {xpCost}（{tCurrent}{currentXp}）\n";
            }

            if (spell.prerequisites.Length > 0)
            {
                string tPrereq = LocalizedTextManager.getText("sm_ui_spell_prereq");
                text += $"\n{tPrereq}:\n";
                foreach (var pre in spell.prerequisites)
                {
                    var preSpell = SuperMechSpell.GetSpell(pre);
                    if (preSpell != null)
                    {
                        bool preLearned = SuperMechSpell.IsLearned(actor, pre);
                        text += $"  {(preLearned ? "✓" : "✗")} {LocalizedTextManager.getText(preSpell.nameKey)}\n";
                    }
                }
            }

            _detailText.text = text;
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
            RefreshList();
            RefreshDetail();
        }
    }
}
