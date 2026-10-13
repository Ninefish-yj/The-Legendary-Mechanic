using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

namespace SuperMech.Code
{
    /// <summary>技能系统窗口（技能图鉴 + 当前单位已学技能）
    /// 技能通过职业/阶位/知识自动学习，主动技能战斗中自动释放</summary>
    public class SuperMechSkillView : MonoBehaviour
    {
        public static Actor OverrideActor = null;
        private Text _actorText;
        private Text _learnedText;
        private Text _allText;
        private static int _selectedClass = -1; // -1=全部

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] SkillView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] SkillView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] SkillView更新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部：当前单位信息
            var actorBar = SuperMechUiBuilder.CreatePanel(transform, "ActorBar", new Color(0, 0, 0, 0.25f), 4, 4, 0, 0);
            var abRect = actorBar.GetComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0, 1);
            abRect.anchorMax = new Vector2(1, 1);
            abRect.pivot = new Vector2(0.5f, 1);
            abRect.sizeDelta = new Vector2(0, 32);
            _actorText = SuperMechUiBuilder.AddText(actorBar, "", 12, TextAnchor.MiddleLeft);
            var atRect = _actorText.GetComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.offsetMin = new Vector2(8, 0);
            atRect.offsetMax = new Vector2(-8, 0);

            // 左侧：当前单位已学技能
            var learnedPanel = SuperMechUiBuilder.CreatePanel(transform, "LearnedPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var lpRect = learnedPanel.GetComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0, 0);
            lpRect.anchorMax = new Vector2(0.45f, 1);
            lpRect.pivot = new Vector2(0, 1);
            lpRect.offsetMin = new Vector2(0, -36);
            lpRect.offsetMax = new Vector2(0, -4);
            _learnedText = SuperMechUiBuilder.AddText(learnedPanel, "", 11, TextAnchor.UpperLeft);
            var ltRect = _learnedText.GetComponent<RectTransform>();
            ltRect.anchorMin = Vector2.zero;
            ltRect.anchorMax = Vector2.one;
            ltRect.offsetMin = new Vector2(8, 4);
            ltRect.offsetMax = new Vector2(-8, -4);

            // 右侧：全部技能图鉴
            var allPanel = SuperMechUiBuilder.CreatePanel(transform, "AllPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var apRect = allPanel.GetComponent<RectTransform>();
            apRect.anchorMin = new Vector2(0.45f, 0);
            apRect.anchorMax = new Vector2(1, 1);
            apRect.pivot = new Vector2(1, 1);
            apRect.offsetMin = new Vector2(0, -36);
            apRect.offsetMax = new Vector2(0, -4);
            _allText = SuperMechUiBuilder.AddText(allPanel, "", 11, TextAnchor.UpperLeft);
            var aTrect = _allText.GetComponent<RectTransform>();
            aTrect.anchorMin = Vector2.zero;
            aTrect.anchorMax = Vector2.one;
            aTrect.offsetMin = new Vector2(8, 4);
            aTrect.offsetMax = new Vector2(-8, -4);
        }

        private void RefreshAll()
        {
            if (_actorText == null || _learnedText == null || _allText == null) return;

            Actor a = OverrideActor;
            if (a == null && World.world != null && World.world.units != null)
            {
                // 尝试获取选中单位
                var selected = World.world.units?.units_only_alive;
                if (selected != null && selected.Count > 0) a = selected[0];
            }

            // 顶部单位信息
            if (a != null)
            {
                string cls = SuperMechBranch.GetClass(a);
                string className = !string.IsNullOrEmpty(cls) ? LocalizedTextManager.getText(cls) : LocalizedTextManager.getText("sm_ui_skill_no_class");
                int stage = SuperMechStage.GetStage(a);
                _actorText.text = $"{a.name} | {className} | {LocalizedTextManager.getText("sm_ui_skill_stage")} {stage}";
            }
            else
            {
                _actorText.text = LocalizedTextManager.getText("sm_ui_skill_no_actor");
            }

            // 左侧：已学技能
            var sb = new StringBuilder();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_skill_learned")).Append("</b>\n");
            if (a != null)
            {
                var learned = SuperMechSkills.GetLearned(a);
                if (learned == null || learned.Count == 0)
                {
                    sb.Append(LocalizedTextManager.getText("sm_ui_skill_none")).Append("\n");
                }
                else
                {
                    foreach (var def in learned)
                    {
                        if (def == null) continue;
                        string typeText = def.type == SuperMechSkills.SkillType.Active
                            ? LocalizedTextManager.getText("sm_ui_skill_active")
                            : LocalizedTextManager.getText("sm_ui_skill_passive");
                        string typeColor = def.type == SuperMechSkills.SkillType.Active ? "#4fc3f7" : "#81c784";
                        sb.Append("<color=").Append(typeColor).Append(">[").Append(typeText).Append("]</color> ")
                          .Append(LocalizedTextManager.getText(def.name)).Append("\n");
                        sb.Append("  <color=#93A4BC>").Append(LocalizedTextManager.getText(def.desc)).Append("</color>\n");
                        if (def.type == SuperMechSkills.SkillType.Active)
                        {
                            sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_skill_cooldown")).Append(" ").Append(def.cooldown).Append("s")
                              .Append("  ").Append(LocalizedTextManager.getText("sm_ui_skill_qicost")).Append(" ").Append(def.qiCost).Append("\n");
                        }
                        sb.Append("\n");
                    }
                }
            }
            else
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_skill_select_actor")).Append("\n");
            }
            _learnedText.text = sb.ToString();

            // 右侧：全部技能图鉴
            sb.Clear();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_skill_codex")).Append("</b>\n");
            var allSkills = SuperMechSkills.AllSkills;
            var available = a != null ? SuperMechSkills.GetAvailable(a) : new List<SuperMechSkills.SkillDef>();
            var learnedList = a != null ? SuperMechSkills.GetLearned(a) : new List<SuperMechSkills.SkillDef>();
            foreach (var def in allSkills)
            {
                if (def == null) continue;
                bool learned = learnedList.Contains(def);
                bool canLearn = available.Contains(def);
                string statusColor = learned ? "#ffd700" : (canLearn ? "#81c784" : "#666666");
                string statusText = learned ? LocalizedTextManager.getText("sm_ui_skill_status_learned")
                    : (canLearn ? LocalizedTextManager.getText("sm_ui_skill_status_available")
                    : LocalizedTextManager.getText("sm_ui_skill_status_locked"));

                sb.Append("<color=").Append(statusColor).Append(">●</color> ")
                  .Append(LocalizedTextManager.getText(def.name)).Append(" <color=#888>[")
                  .Append(statusText).Append("]</color>\n");
                sb.Append("  <color=#93A4BC>").Append(LocalizedTextManager.getText(def.desc)).Append("</color>\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_skill_class")).Append(" ")
                  .Append(LocalizedTextManager.getText(def.requiredClass))
                  .Append("  ").Append(LocalizedTextManager.getText("sm_ui_skill_stage_req")).Append(" ").Append(def.requiredStage)
                  .Append("\n\n");
            }
            _allText.text = sb.ToString();
        }
    }
}
