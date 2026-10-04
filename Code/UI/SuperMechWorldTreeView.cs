using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.73.0 世界树入侵窗口：事件状态 + 树王信息 + 击杀统计 + 星际联合军加成说明
    /// 原著对应：5.0版本世界树入侵、五大树王/十叶执行官/圣树使者、星际联合军（起点#1403）
    /// </summary>
    public class SuperMechWorldTreeView : MonoBehaviour
    {
        private Text _countText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] WorldTreeView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] WorldTreeView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch { } }

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
            var d = SuperMechWorldTree.Data;

            string state = d.state == SuperMechWorldTree.EventState.Idle ? LocalizedTextManager.getText("sm_ui_tree_state_idle")
                : d.state == SuperMechWorldTree.EventState.Invading ? LocalizedTextManager.getText("sm_ui_tree_state_invading")
                : LocalizedTextManager.getText("sm_ui_tree_state_cooldown");

            _countText.text = LocalizedTextManager.getText("sm_ui_tree_title") + " | "
                + LocalizedTextManager.getText("sm_ui_tree_state") + state
                + " | " + LocalizedTextManager.getText("sm_ui_tree_invasion_count") + " " + d.invasionCount;

            var sb = new System.Text.StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_tree_king")).Append(": ")
              .Append(d.kingName != null && d.kingName.Length > 0 ? d.kingName : "（未入侵）").Append("\n");
            if (d.state == SuperMechWorldTree.EventState.Invading)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_tree_remaining")).Append(" ").Append(d.ticksLeft).Append("\n");
                sb.Append(LocalizedTextManager.getText("sm_ui_tree_killed")).Append(" ")
                  .Append(d.killedTrees).Append(" | ").Append(LocalizedTextManager.getText("sm_ui_tree_killed_kings")).Append(" ")
                  .Append(d.killedKings).Append("/1").Append("\n");
                sb.Append(LocalizedTextManager.getText("sm_ui_tree_union_on")).Append("\n");
            }
            else
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_tree_next")).Append(" ")
                  .Append(d.nextSpawnInTicks).Append("\n");
                sb.Append(LocalizedTextManager.getText("sm_ui_tree_union_off")).Append("\n");
            }
            sb.Append(LocalizedTextManager.getText("sm_ui_tree_desc")).Append("\n");
            _detailText.text = sb.ToString();
        }
    }
}
