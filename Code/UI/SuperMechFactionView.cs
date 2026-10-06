using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// </summary>
    public class SuperMechFactionView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _countText;
        private Text _emptyText;
        private Text _detailText;
        private static string _selectedFactionId;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] FactionView初始化失败: " + e); } }
        void OnEnable() { try { RefreshList(); } catch (System.Exception e) { Debug.LogError("[超神机械师] FactionView刷新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部统计栏
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, LocalizedTextManager.getText("sm_ui_faction_count_zero"), 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            // 滚动列表
            var scrollGo = new GameObject("FactionScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.35f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 1);
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-4, -32);
            var scroll = scrollGo.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<UnityEngine.UI.Mask>();
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _listContent = content.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 100);
            scroll.viewport = vpRect;
            scroll.content = _listContent;
            scroll.vertical = true;
            scroll.horizontal = false;

            _emptyText = SuperMechUiBuilder.AddText(content, LocalizedTextManager.getText("sm_ui_faction_none"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.sizeDelta = new Vector2(0, 30);

            // 底部成员详情
            var detailPanel = SuperMechUiBuilder.CreatePanel(transform, "DetailPanel", new Color(0, 0, 0, 0.15f), 4, 4, 0, 0);
            var dRect = detailPanel.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = new Vector2(1, 0.35f);
            dRect.pivot = new Vector2(0.5f, 0);
            dRect.offsetMin = new Vector2(4, 4);
            dRect.offsetMax = new Vector2(-4, 0);
            _detailText = SuperMechUiBuilder.AddText(detailPanel, LocalizedTextManager.getText("sm_ui_faction_click"), 11, TextAnchor.UpperLeft);
            var dtRect = _detailText.GetComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(8, 8);
            dtRect.offsetMax = new Vector2(-8, -8);
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshList()
        {
            if (_listContent == null || _countText == null || _emptyText == null) return; // v0.75.11: 布局未构建时防御
            // 清空旧条目
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name != "EmptyText") Destroy(child.gameObject);
            }

            var factions = SuperMechFaction.GetAllFactions();
            _countText.text = $"{LocalizedTextManager.getText("sm_ui_faction_total")}: {factions.Count}";
            _emptyText.gameObject.SetActive(factions.Count == 0);

            float y = 0f;
            foreach (var f in factions)
            {
                var row = CreateFactionRow(f, ref y);
                if (f.id == _selectedFactionId) ShowMembers(f);
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private GameObject CreateFactionRow(SuperMechFaction.FactionData f, ref float y)
        {
            var row = new GameObject($"Faction_{f.id}");
            row.transform.SetParent(_listContent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 36);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 38f;

            var bg = row.AddComponent<UnityEngine.UI.Image>();
            bg.color = f.id == _selectedFactionId ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(1, 1, 1, 0.05f);

            var leaderName = "?";
            var units = World.world.units?.units_only_alive;
            if (units != null)
            {
                foreach (var a in units) { if (a != null && a.id == f.leaderId) { leaderName = a.name; break; } }
            }

            var label = SuperMechUiBuilder.AddText(row,
                $"[{f.level}级] {f.name}  |  成员:{f.memberIds.Count}  |  领袖:{leaderName}  |  战力:{f.totalPower:F0}",
                11, TextAnchor.MiddleLeft);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, 0);

            var btn = row.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => {
                _selectedFactionId = (_selectedFactionId == f.id) ? null : f.id;
                RefreshList();
            });
            return row;
        }

        private void ShowMembers(SuperMechFaction.FactionData f)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {f.name} 成员列表 ===");
            var units = World.world.units?.units_only_alive;
            int idx = 1;
            foreach (var mid in f.memberIds)
            {
                if (units == null) break;
                foreach (var a in units)
                {
                    if (a == null || a.id != mid) continue;
                    int rank = SuperMechAdvancement.GetExactRankIndex(a);
                    string rankName = (rank >= 0 && rank < SuperMechRanks.All.Count)
                        ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name)
                        : "?";
                    string role = (a.id == f.leaderId) ? "领袖" : "成员";
                    sb.AppendLine($"{idx}. {a.name} [{rankName}] {role}");
                    idx++;
                    break;
                }
            }
            if (idx == 1) sb.AppendLine("（无存活成员）");
            _detailText.text = sb.ToString();
        }
    }

    /// <summary>宇宙宝物装备窗口（v0.76.48 接线：图鉴列表+装备按钮）</summary>
}
