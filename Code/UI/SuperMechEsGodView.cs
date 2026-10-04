using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.74.0 异神监测窗口：异神状态 + 异能·复刻层数 + 封印/卷土重来记录 + 终局进度
    /// </summary>
    public class SuperMechEsGodView : MonoBehaviour
    {
        private static bool _warnedRefresh; // 一次性异常警告
        private Text _countText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { if (!_warnedRefresh) { _warnedRefresh = true; Debug.LogError($"[超神机械师] 异神监测窗口刷新异常(仅首次): " + e.Message); } } }

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
            var d = SuperMechEsGod.Data;

            string state = d.state == SuperMechEsGod.EventState.Idle ? LocalizedTextManager.getText("sm_ui_esgod_state_idle")
                : d.state == SuperMechEsGod.EventState.Active ? LocalizedTextManager.getText("sm_ui_esgod_state_active")
                : d.state == SuperMechEsGod.EventState.Sealed ? LocalizedTextManager.getText("sm_ui_esgod_state_escaped")
                : LocalizedTextManager.getText("sm_ui_esgod_state_defeated");

            _countText.text = LocalizedTextManager.getText("sm_ui_esgod_title") + " | "
                + LocalizedTextManager.getText("sm_ui_esgod_state") + state;

            var sb = new System.Text.StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_replicate")).Append(": ")
              .Append(d.replicateCount).Append(" 层（伤害+" )
              .Append((d.replicateCount * 0.05f * 100f).ToString("0")).Append("%，上限50%）").Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_escape")).Append(": ")
              .Append(d.sealCount).Append(" 次 | ")
              .Append(LocalizedTextManager.getText("sm_ui_esgod_final")).Append(": ")
              .Append(d.finalDefeated ? LocalizedTextManager.getText("sm_ui_esgod_final_yes") : LocalizedTextManager.getText("sm_ui_esgod_final_no")).Append("\n");
            if (d.state == SuperMechEsGod.EventState.Idle)
                sb.Append(LocalizedTextManager.getText("sm_ui_esgod_next")).Append(" ").Append(d.nextSpawnInTicks).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_desc")).Append("\n");
            _detailText.text = sb.ToString();
        }
    }
}
