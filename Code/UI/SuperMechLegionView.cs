using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.72.0 黑星军团窗口：编制统计 + 贡献星级榜 + 军团命令（集结/远征）
    /// 机制：贡献星级制（总贡献晋升/消费不降级）、黑星十八骑、集结/远征命令、远征贡献双倍
    /// </summary>
    public class SuperMechLegionView : MonoBehaviour
    {
        private Text _countText;
        private Text _creditText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] LegionView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] LegionView刷新失败: " + e); } }
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

            // 中：贡献星级榜
            var creditPanel = SuperMechUiBuilder.CreatePanel(transform, "CreditPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var cpRect = creditPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0, 1);
            cpRect.anchorMax = new Vector2(0.62f, 1);
            cpRect.pivot = new Vector2(0, 1);
            cpRect.offsetMin = new Vector2(0, -24);
            cpRect.offsetMax = new Vector2(0, -4);
            _creditText = SuperMechUiBuilder.AddText(creditPanel, "", 12, TextAnchor.UpperLeft);
            var ctRect = _creditText.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = new Vector2(8, 4);
            ctRect.offsetMax = new Vector2(-8, -4);

            // 右：命令面板（按钮）
            var cmdPanel = SuperMechUiBuilder.CreatePanel(transform, "CmdPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var cmRect = cmdPanel.GetComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.62f, 1);
            cmRect.anchorMax = new Vector2(1, 1);
            cmRect.pivot = new Vector2(1, 1);
            cmRect.offsetMin = new Vector2(0, -24);
            cmRect.offsetMax = new Vector2(0, -4);

            float y = -8f;
            var btnRally = SuperMechUiBuilder.CreateButton(cmdPanel.transform, "RallyBtn", LocalizedTextManager.getText("sm_ui_legion_cmd_rally"),
                () => SuperMechLegion.TryCommand(SuperMechLegion.CommandType.Rally), 160, 26, 11);
            PlaceButton(btnRally, cmRect, y); y -= 30f;

            var btnExped = SuperMechUiBuilder.CreateButton(cmdPanel.transform, "ExpedBtn", LocalizedTextManager.getText("sm_ui_legion_cmd_exped"),
                () => SuperMechLegion.TryCommand(SuperMechLegion.CommandType.Expedition), 160, 26, 11);
            PlaceButton(btnExped, cmRect, y); y -= 30f;

            var btnCancel = SuperMechUiBuilder.CreateButton(cmdPanel.transform, "CancelBtn", LocalizedTextManager.getText("sm_ui_legion_cmd_cancel"),
                () => SuperMechLegion.CancelCommand(), 160, 26, 11);
            PlaceButton(btnCancel, cmRect, y);

            // 底部说明
            var bottomBar = SuperMechUiBuilder.CreatePanel(transform, "BottomBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var bRect = bottomBar.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 0);
            bRect.sizeDelta = new Vector2(0, 44);
            _detailText = SuperMechUiBuilder.AddText(bottomBar, "", 11, TextAnchor.UpperLeft);
            var dRect = _detailText.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(8, 2);
            dRect.offsetMax = new Vector2(-8, -2);
        }

        private static void PlaceButton(GameObject btn, RectTransform parent, float topOffset)
        {
            if (btn == null) return;
            var rect = btn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 1);
            rect.anchorMax = new Vector2(0.96f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, topOffset);
        }

        private void RefreshAll()
        {
            if (_countText == null || _creditText == null || _detailText == null) return;

            var commander = SuperMechLegion.GetCommander();
            string cmdName = commander != null && commander.name != null ? commander.name : "（未确定）";

            _countText.text = LocalizedTextManager.getText("sm_ui_legion_title") + " | "
                + LocalizedTextManager.getText("sm_ui_legion_member_count") + " " + SuperMechLegion.MemberCount
                + " | " + LocalizedTextManager.getText("sm_ui_legion_commander") + " " + cmdName;

            // 贡献星级榜 Top6
            var sb = new System.Text.StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_legion_credit_title")).Append("\n");
            var top = GetTopCredit(6);
            if (top.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_legion_no_member")).Append("\n");
            }
            else
            {
                foreach (var kv in top)
                {
                    var a = FindActor(kv.Key);
                    var (rankName, _) = SuperMechLegion.GetRank(kv.Value);
                    sb.Append("· ").Append(a != null && a.name != null ? a.name : ("#" + kv.Key))
                      .Append("  [").Append(rankName).Append("] 信用").Append(kv.Value).Append("\n");
                }
            }
            _creditText.text = sb.ToString();

            // 当前命令
            var cmd = SuperMechLegion.Data.command;
            string cmdLabel = cmd == SuperMechLegion.CommandType.Rally ? LocalizedTextManager.getText("sm_ui_legion_cmd_rally")
                : cmd == SuperMechLegion.CommandType.Expedition ? LocalizedTextManager.getText("sm_ui_legion_cmd_exped")
                : LocalizedTextManager.getText("sm_ui_legion_cmd_none");

            _detailText.text = LocalizedTextManager.getText("sm_ui_legion_desc") + " | "
                + LocalizedTextManager.getText("sm_ui_legion_cmd_current") + cmdLabel;
        }

        private static List<KeyValuePair<long, int>> GetTopCredit(int limit)
        {
            var list = new List<KeyValuePair<long, int>>();
            foreach (var kv in SuperMechLegion.Data.credit)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                var a = FindActor(id);
                if (a == null || !a.isAlive()) continue;
                list.Add(new KeyValuePair<long, int>(id, kv.Value));
            }
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (limit > 0 && list.Count > limit) list.RemoveRange(limit, list.Count - limit);
            return list;
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
