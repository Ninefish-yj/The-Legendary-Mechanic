using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.0 超A格局窗口（原异神监测改造）：个体伟力·集体伟力体系总览
    /// 显示：超A级个体列表（含异神）/ 三大文明态度（默许·清算）/ 超A级协会状态 / 异神状态
    /// 原著：异神=顶层超A之一（ch712），能存在是三大文明的默许（ch1002/1018），
    /// 超A级协会为原著后期创建（ch1016 麦尼逊推动）。
    /// </summary>
    public class SuperMechEsGodView : MonoBehaviour
    {
        private static bool _warnedRefresh; // 一次性异常警告
        private Text _countText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { if (!_warnedRefresh) { _warnedRefresh = true; Debug.LogError($"[超神机械师] 超A格局窗口刷新异常(仅首次): " + e.Message); } } }

        private void BuildLayout()
        {
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, "", 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            var bodyPanel = SuperMechUiBuilder.CreatePanel(transform, "Body", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var bRect = bodyPanel.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.offsetMin = new Vector2(0, -24);
            bRect.offsetMax = new Vector2(0, -4);
            _detailText = SuperMechUiBuilder.AddText(bodyPanel, "", 12, TextAnchor.UpperLeft);
            var dRect = _detailText.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(8, 4);
            dRect.offsetMax = new Vector2(-8, -4);
        }

        private void RefreshAll()
        {
            if (_countText == null || _detailText == null) return;

            int superACount = SuperMechSupermA.CountSuperA();
            var attitude = SuperMechSupermA.CurrentAttitude;
            bool council = SuperMechSupermA.CouncilFormed;
            var d = SuperMechEsGod.Data;

            string state = d.state == SuperMechEsGod.EventState.Idle ? LocalizedTextManager.getText("sm_ui_esgod_state_idle")
                : d.state == SuperMechEsGod.EventState.Active ? LocalizedTextManager.getText("sm_ui_esgod_state_active")
                : LocalizedTextManager.getText("sm_ui_esgod_state_defeated");

            _countText.text = LocalizedTextManager.getText("sm_ui_esgod_title") + " | "
                + LocalizedTextManager.getText("sm_ui_supera_count") + " " + superACount;

            var sb = new System.Text.StringBuilder();
            // 三大文明态度（个体伟力 vs 集体伟力）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_attitude")).Append(": ")
              .Append(attitude == SuperMechSupermA.Attitude.Purge
                  ? LocalizedTextManager.getText("sm_ui_supera_purge")
                  : LocalizedTextManager.getText("sm_ui_supera_tolerate")).Append("\n");
            // 超A级协会
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_council")).Append(": ")
              .Append(council
                  ? LocalizedTextManager.getText("sm_ui_supera_council_yes")
                  : LocalizedTextManager.getText("sm_ui_supera_council_no")).Append("\n");
            // 异神状态（顶层超A之一）
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_state")).Append(": ").Append(state).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_desc")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_desc")).Append("\n");
            _detailText.text = sb.ToString();
        }
    }
}
