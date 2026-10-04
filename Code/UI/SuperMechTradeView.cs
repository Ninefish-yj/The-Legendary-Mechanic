using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.65.0 跨文明贸易窗口：势力列表 + 开启贸易通道 + 活跃通道管理
    /// 原著约束：贸易不涉及圣所档案/原始异能体（见 SuperMechTrade.IsForbiddenTradeItem）
    /// </summary>
    public class SuperMechTradeView : MonoBehaviour
    {
        private RectTransform _listContent;   // 势力列表
        private RectTransform _routeContent;  // 通道列表
        private Text _countText;
        private Text _detailText;
        private static string _selA;
        private static string _selB;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] TradeView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] TradeView刷新失败: " + e); } }

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

            // 左：势力列表
            var listPanel = SuperMechUiBuilder.CreatePanel(transform, "FactionPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var lpRect = listPanel.GetComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0, 0.38f);
            lpRect.anchorMax = new Vector2(0.62f, 1);
            lpRect.pivot = new Vector2(0.5f, 1);
            lpRect.offsetMin = new Vector2(4, 4);
            lpRect.offsetMax = new Vector2(-2, -32);
            var lpTitle = SuperMechUiBuilder.AddText(listPanel, LocalizedTextManager.getText("sm_ui_trade_factions"), 12, TextAnchor.UpperCenter);
            var lpT = lpTitle.GetComponent<RectTransform>();
            lpT.anchorMin = new Vector2(0, 1);
            lpT.anchorMax = new Vector2(1, 1);
            lpT.sizeDelta = new Vector2(0, 22);

            var listScroll = new GameObject("ListScroll");
            listScroll.transform.SetParent(listPanel.transform, false);
            var lsRect = listScroll.AddComponent<RectTransform>();
            lsRect.anchorMin = Vector2.zero;
            lsRect.anchorMax = Vector2.one;
            lsRect.offsetMin = new Vector2(4, 4);
            lsRect.offsetMax = new Vector2(-4, -26);
            var lsScroll = listScroll.AddComponent<ScrollRect>();
            var lsVp = new GameObject("Viewport");
            lsVp.transform.SetParent(listScroll.transform, false);
            var lsVpRect = lsVp.AddComponent<RectTransform>();
            lsVpRect.anchorMin = Vector2.zero;
            lsVpRect.anchorMax = Vector2.one;
            lsVpRect.offsetMin = Vector2.zero;
            lsVpRect.offsetMax = Vector2.zero;
            lsVp.AddComponent<Mask>();
            var lsContent = new GameObject("Content");
            lsContent.transform.SetParent(lsVp.transform, false);
            _listContent = lsContent.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 100);
            lsScroll.viewport = lsVpRect;
            lsScroll.content = _listContent;
            lsScroll.vertical = true;
            lsScroll.horizontal = false;

            // 右：通道列表
            var routePanel = SuperMechUiBuilder.CreatePanel(transform, "RoutePanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var rpRect = routePanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.62f, 0.38f);
            rpRect.anchorMax = new Vector2(1, 1);
            rpRect.pivot = new Vector2(0.5f, 1);
            rpRect.offsetMin = new Vector2(2, 4);
            rpRect.offsetMax = new Vector2(-4, -32);
            var rpTitle = SuperMechUiBuilder.AddText(routePanel, LocalizedTextManager.getText("sm_ui_trade_routes"), 12, TextAnchor.UpperCenter);
            var rpT = rpTitle.GetComponent<RectTransform>();
            rpT.anchorMin = new Vector2(0, 1);
            rpT.anchorMax = new Vector2(1, 1);
            rpT.sizeDelta = new Vector2(0, 22);

            var routeScroll = new GameObject("RouteScroll");
            routeScroll.transform.SetParent(routePanel.transform, false);
            var rsRect = routeScroll.AddComponent<RectTransform>();
            rsRect.anchorMin = Vector2.zero;
            rsRect.anchorMax = Vector2.one;
            rsRect.offsetMin = new Vector2(4, 4);
            rsRect.offsetMax = new Vector2(-4, -26);
            var rsScroll = routeScroll.AddComponent<ScrollRect>();
            var rsVp = new GameObject("Viewport");
            rsVp.transform.SetParent(routeScroll.transform, false);
            var rsVpRect = rsVp.AddComponent<RectTransform>();
            rsVpRect.anchorMin = Vector2.zero;
            rsVpRect.anchorMax = Vector2.one;
            rsVpRect.offsetMin = Vector2.zero;
            rsVpRect.offsetMax = Vector2.zero;
            rsVp.AddComponent<Mask>();
            var rsContent = new GameObject("Content");
            rsContent.transform.SetParent(rsVp.transform, false);
            _routeContent = rsContent.AddComponent<RectTransform>();
            _routeContent.anchorMin = new Vector2(0, 1);
            _routeContent.anchorMax = new Vector2(1, 1);
            _routeContent.pivot = new Vector2(0.5f, 1);
            _routeContent.sizeDelta = new Vector2(0, 100);
            rsScroll.viewport = rsVpRect;
            rsScroll.content = _routeContent;
            rsScroll.vertical = true;
            rsScroll.horizontal = false;

            // 底部详情/操作
            var detailPanel = SuperMechUiBuilder.CreatePanel(transform, "DetailPanel", new Color(0, 0, 0, 0.15f), 4, 4, 0, 0);
            var dRect = detailPanel.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = new Vector2(1, 0.38f);
            dRect.pivot = new Vector2(0.5f, 0);
            dRect.offsetMin = new Vector2(4, 4);
            dRect.offsetMax = new Vector2(-4, 0);
            _detailText = SuperMechUiBuilder.AddText(detailPanel, LocalizedTextManager.getText("sm_ui_trade_hint"), 11, TextAnchor.UpperLeft);
            var dtRect = _detailText.GetComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(8, 8);
            dtRect.offsetMax = new Vector2(-8, -40);
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshAll()
        {
            RefreshFactionList();
            RefreshRouteList();
            RefreshDetail();
        }

        private void RefreshFactionList()
        {
            if (_listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            var factions = SuperMechFaction.GetAllFactions();
            _countText.text = $"{LocalizedTextManager.getText("sm_ui_trade_factions")}: {factions.Count}  |  " +
                $"{LocalizedTextManager.getText("sm_ui_trade_routes")}: {SuperMechTrade.GetAllRoutes().Count}";

            if (factions.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_listContent.gameObject, LocalizedTextManager.getText("sm_ui_trade_no_faction"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var f in factions)
            {
                var row = new GameObject($"Faction_{f.id}");
                row.transform.SetParent(_listContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 34);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 36f;

                bool sel = f.id == _selA || f.id == _selB;
                var bg = row.AddComponent<Image>();
                bg.color = sel ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(1, 1, 1, 0.05f);

                string leaderName = "?";
                var units = World.world.units?.units_only_alive;
                if (units != null)
                    foreach (var a in units) { if (a != null && a.id == f.leaderId) { leaderName = a.name; break; } }

                var label = SuperMechUiBuilder.AddText(row,
                    $"[{f.level}级] {f.name}  |  领袖:{leaderName}",
                    11, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-8, 0);

                var btn = row.AddComponent<Button>();
                string fid = f.id;
                btn.onClick.AddListener(() => OnFactionClick(fid));
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void OnFactionClick(string factionId)
        {
            // 两阶段选择：先选A，再选B
            if (_selA == null || _selB != null)
            {
                _selA = factionId;
                _selB = null;
            }
            else if (factionId != _selA)
            {
                _selB = factionId;
            }
            RefreshAll();
        }

        private void RefreshRouteList()
        {
            if (_routeContent == null) return;
            for (int i = _routeContent.childCount - 1; i >= 0; i--)
                Destroy(_routeContent.GetChild(i).gameObject);

            var routes = SuperMechTrade.GetAllRoutes();
            if (routes.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_routeContent.gameObject, LocalizedTextManager.getText("sm_ui_trade_no_route"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var r in routes)
            {
                var row = new GameObject($"Route_{r.id}");
                row.transform.SetParent(_routeContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 34);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 36f;

                var bg = row.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.05f);

                string nameA = GetFactionName(r.factionA);
                string nameB = GetFactionName(r.factionB);
                string status = r.active
                    ? LocalizedTextManager.getText("sm_ui_trade_status_active")
                    : LocalizedTextManager.getText("sm_ui_trade_status_blocked");

                var label = SuperMechUiBuilder.AddText(row,
                    $"{nameA} ↔ {nameB}\n{status}  |  {LocalizedTextManager.getText("sm_ui_trade_progress")}:{r.progress * 100:F0}%  |  " +
                    $"{LocalizedTextManager.getText("sm_ui_trade_deliveries")}:{r.deliveries}",
                    10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = new Vector2(0.72f, 1);
                labelRect.offsetMin = new Vector2(6, 0);
                labelRect.offsetMax = new Vector2(0, 0);

                // 中止按钮
                var btnGo = new GameObject("StopBtn");
                btnGo.transform.SetParent(row.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(1, 0.5f);
                btnRect.anchorMax = new Vector2(1, 0.5f);
                btnRect.pivot = new Vector2(1, 0.5f);
                btnRect.sizeDelta = new Vector2(52, 24);
                btnRect.anchoredPosition = new Vector2(-6, 0);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.5f, 0.25f, 0.25f, 0.9f);
                var btn = btnGo.AddComponent<Button>();
                var btnText = SuperMechUiBuilder.AddText(btnGo, LocalizedTextManager.getText("sm_ui_trade_stop"), 10, TextAnchor.MiddleCenter);
                var btr = btnText.GetComponent<RectTransform>();
                btr.anchorMin = Vector2.zero;
                btr.anchorMax = Vector2.one;
                btr.offsetMin = Vector2.zero;
                btr.offsetMax = Vector2.zero;
                string rid = r.id;
                btn.onClick.AddListener(() => { SuperMechTrade.RemoveRoute(rid); RefreshAll(); });
            }
            _routeContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void RefreshDetail()
        {
            if (_detailText == null) return;
            var sb = new System.Text.StringBuilder();
            if (_selA != null)
            {
                sb.AppendLine($"A: {GetFactionName(_selA)}");
                if (_selB != null)
                {
                    sb.AppendLine($"B: {GetFactionName(_selB)}");
                    var rel = SuperMechFaction.GetRelation(_selA, _selB);
                    if (rel == SuperMechFaction.FactionRelation.Hostile)
                    {
                        sb.AppendLine("<color=#ff9966>" + LocalizedTextManager.getText("sm_ui_trade_hostile") + "</color>");
                    }
                    else
                    {
                        sb.AppendLine(LocalizedTextManager.getText("sm_ui_trade_ready"));
                    }
                }
                else
                {
                    sb.AppendLine(LocalizedTextManager.getText("sm_ui_trade_select_b"));
                }
            }
            else
            {
                sb.AppendLine(LocalizedTextManager.getText("sm_ui_trade_hint"));
            }
            sb.AppendLine("<color=#888><size=10>" + LocalizedTextManager.getText("sm_ui_trade_forbidden") + "</size></color>");
            _detailText.text = sb.ToString();

            // 开启贸易按钮（底部）
            var parent = _detailText.transform.parent;
            // 移除旧按钮
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c.name == "OpenRouteBtn") Destroy(c.gameObject);
            }
            if (_selA != null && _selB != null
                && SuperMechFaction.GetRelation(_selA, _selB) != SuperMechFaction.FactionRelation.Hostile)
            {
                var btnGo = new GameObject("OpenRouteBtn");
                btnGo.transform.SetParent(parent, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(1, 0);
                btnRect.anchorMax = new Vector2(1, 0);
                btnRect.pivot = new Vector2(1, 0);
                btnRect.sizeDelta = new Vector2(130, 30);
                btnRect.anchoredPosition = new Vector2(-10, 8);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.25f, 0.5f, 0.35f, 0.9f);
                var btn = btnGo.AddComponent<Button>();
                var btnText = SuperMechUiBuilder.AddText(btnGo,
                    $"{LocalizedTextManager.getText("sm_ui_trade_open")}（{GetFactionName(_selA)} ↔ {GetFactionName(_selB)}）", 11, TextAnchor.MiddleCenter);
                var btr = btnText.GetComponent<RectTransform>();
                btr.anchorMin = Vector2.zero;
                btr.anchorMax = Vector2.one;
                btr.offsetMin = new Vector2(4, 0);
                btr.offsetMax = new Vector2(-4, 0);
                string a = _selA, b = _selB;
                btn.onClick.AddListener(() =>
                {
                    SuperMechTrade.TryOpenRoute(a, b);
                    _selA = null; _selB = null;
                    RefreshAll();
                });
            }
        }

        private static string GetFactionName(string factionId)
        {
            foreach (var f in SuperMechFaction.GetAllFactions())
                if (f.id == factionId) return f.name;
            return "?";
        }
    }
}
