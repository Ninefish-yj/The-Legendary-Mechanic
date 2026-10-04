using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.71.0 玩家降临窗口：降临者统计 + 玩家面板样例 + 当前任务（奖池/门槛/贡献榜前五）
    /// 原著对应：玩家面板（生命/体力/六维/潜能）、奖池任务（接取消耗+贡献度前五分配，原著第130章）
    /// </summary>
    public class SuperMechPlayerView : MonoBehaviour
    {
        private Text _countText;
        private Text _panelText;
        private Text _taskText;
        private Text _detailText;
        private static int _selectedIdx = -1;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] PlayerView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] PlayerView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch { } }

        private void BuildLayout()
        {
            // 顶部统计
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

            // 左：面板统计（降临者击杀榜）
            var panelPanel = SuperMechUiBuilder.CreatePanel(transform, "PanelList", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var ppRect = panelPanel.GetComponent<RectTransform>();
            ppRect.anchorMin = new Vector2(0, 1);
            ppRect.anchorMax = new Vector2(0.5f, 1);
            ppRect.pivot = new Vector2(0, 1);
            ppRect.offsetMin = new Vector2(0, -24);
            ppRect.offsetMax = new Vector2(0, -4);
            _panelText = SuperMechUiBuilder.AddText(panelPanel, "", 12, TextAnchor.UpperLeft);
            var ptRect = _panelText.GetComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.offsetMin = new Vector2(8, 4);
            ptRect.offsetMax = new Vector2(-8, -4);

            // 右：任务详情（奖池任务）
            var taskPanel = SuperMechUiBuilder.CreatePanel(transform, "TaskPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var tpRect = taskPanel.GetComponent<RectTransform>();
            tpRect.anchorMin = new Vector2(0.5f, 1);
            tpRect.anchorMax = new Vector2(1, 1);
            tpRect.pivot = new Vector2(1, 1);
            tpRect.offsetMin = new Vector2(0, -24);
            tpRect.offsetMax = new Vector2(0, -4);
            _taskText = SuperMechUiBuilder.AddText(taskPanel, "", 12, TextAnchor.UpperLeft);
            var ttRect = _taskText.GetComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(8, 4);
            ttRect.offsetMax = new Vector2(-8, -4);

            // 底部说明
            var bottomBar = SuperMechUiBuilder.CreatePanel(transform, "BottomBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var bRect = bottomBar.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 0);
            bRect.sizeDelta = new Vector2(0, 48);
            _detailText = SuperMechUiBuilder.AddText(bottomBar, "", 11, TextAnchor.UpperLeft);
            var dRect = _detailText.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(8, 2);
            dRect.offsetMax = new Vector2(-8, -2);
        }

        private void RefreshAll()
        {
            if (_countText == null || _panelText == null || _taskText == null || _detailText == null) return;

            string title = LocalizedTextManager.getText("sm_ui_player_title");
            _countText.text = title + " | " + LocalizedTextManager.getText("sm_ui_player_count") + " " + SuperMechPlayer.PlayerCount
                + " | " + LocalizedTextManager.getText("sm_ui_player_respawn") + " " + SuperMechPlayer.RespawningCount;

            // 面板击杀榜 Top5
            var panels = SuperMechPlayer.GetPanelRanking(5);
            var sb = new System.Text.StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_player_panel_title")).Append("\n");
            if (panels.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_player_no_panel")).Append("\n");
            }
            else
            {
                foreach (var kv in panels)
                {
                    var p = kv.Value;
                    sb.Append("· ").Append(kv.Key.name != null ? kv.Key.name : "玩家")
                      .Append("  [击").Append(p.kills).Append(" 贡").Append(p.contribution)
                      .Append(" 经").Append(p.experience).Append("]\n");
                }
            }
            _panelText.text = sb.ToString();

            // 任务详情
            var task = SuperMechPlayer.GetActiveTask();
            var sb2 = new System.Text.StringBuilder();
            sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_title")).Append("\n");
            if (task == null)
            {
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_no_task")).Append("\n");
            }
            else
            {
                sb2.Append("[").Append(task.type).Append("] ").Append(task.targetLabel).Append("\n");
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_progress")).Append(" ")
                  .Append(task.progress).Append("/").Append(task.targetCount).Append("\n");
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_pool")).Append(" ")
                  .Append(task.rewardPool.ToString("0")).Append(LocalizedTextManager.getText("sm_ui_player_task_exp")).Append("\n");
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_entry")).Append(" ")
                  .Append(task.entryCost.ToString("0")).Append(LocalizedTextManager.getText("sm_ui_player_task_exp")).Append("\n");
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_pub")).Append(" ").Append(task.publisherName).Append("\n");
                var top = SuperMechPlayer.GetTopContributors(task, 5);
                sb2.Append(LocalizedTextManager.getText("sm_ui_player_task_top")).Append("\n");
                if (top.Count == 0)
                {
                    sb2.Append("  ").Append(LocalizedTextManager.getText("sm_ui_player_no_contrib")).Append("\n");
                }
                else
                {
                    for (int i = 0; i < top.Count; i++)
                    {
                        var a = FindActor(top[i].Key);
                        sb2.Append("  ").Append(i + 1).Append(". ")
                          .Append(a != null && a.name != null ? a.name : ("#" + top[i].Key))
                          .Append("  贡").Append(top[i].Value).Append("\n");
                    }
                }
            }
            _taskText.text = sb2.ToString();

            _detailText.text = LocalizedTextManager.getText("sm_ui_player_desc");
        }

        private static Actor FindActor(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units) if (a != null && a.id == id) return a;
            return null;
        }
    }
}
