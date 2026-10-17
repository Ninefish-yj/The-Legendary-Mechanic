using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.230 组织总览重构：移除嵌套入口（超A榜/外交/战报），纯列表+详情
    /// 定位：全球组织/势力速览，点击行展开成员详情
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
            // 顶部统计栏（纯文字，无按钮）
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 32);
            var topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0, 0, 0, 0f); // v0.239.6 去自定义块：顶栏透明

            _countText = new GameObject("Count").AddComponent<Text>();
            _countText.transform.SetParent(topBar.transform, false);
            _countText.font = SuperMechUiSkin.DefaultFont;
            _countText.fontSize = 13;
            _countText.color = new Color(0.85f, 0.9f, 1f);
            _countText.alignment = TextAnchor.MiddleLeft;
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(12, 0);
            countRect.offsetMax = new Vector2(-12, 0);

            // 滚动列表（上65%）
            var scrollGo = new GameObject("FactionScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.38f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 1);
            scrollRect.offsetMin = new Vector2(6, 38);
            scrollRect.offsetMax = new Vector2(-6, -6);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = true;
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.1f);
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

            _emptyText = new GameObject("Empty").AddComponent<Text>();
            _emptyText.transform.SetParent(content.transform, false);
            _emptyText.font = SuperMechUiSkin.DefaultFont;
            _emptyText.fontSize = 12;
            _emptyText.color = new Color(0.6f, 0.65f, 0.75f);
            _emptyText.alignment = TextAnchor.MiddleCenter;
            _emptyText.text = LocalizedTextManager.getText("sm_ui_faction_none");
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.sizeDelta = new Vector2(0, 30);

            // 底部成员详情（下38%）
            var detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(transform, false);
            var dRect = detailPanel.AddComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = new Vector2(1, 0.38f);
            dRect.pivot = new Vector2(0.5f, 0);
            dRect.offsetMin = new Vector2(6, 6);
            dRect.offsetMax = new Vector2(-6, -6);
            var dImg = detailPanel.AddComponent<Image>();
            dImg.color = new Color(0, 0, 0, 0f); // v0.239.6 去自定义块：详情面板透明

            var detailTitle = new GameObject("DetailTitle").AddComponent<Text>();
            detailTitle.transform.SetParent(detailPanel.transform, false);
            detailTitle.font = SuperMechUiSkin.DefaultFont;
            detailTitle.fontSize = 12;
            detailTitle.color = SuperMechUiSkin.CyanColor;
            detailTitle.alignment = TextAnchor.UpperLeft;
            detailTitle.text = LocalizedTextManager.getText("sm_ui_faction_click");
            var dtRect = detailTitle.GetComponent<RectTransform>();
            dtRect.anchorMin = new Vector2(0, 1);
            dtRect.anchorMax = new Vector2(1, 1);
            dtRect.pivot = new Vector2(0, 1);
            dtRect.sizeDelta = new Vector2(0, 20);
            dtRect.anchoredPosition = new Vector2(10, -6);

            _detailText = new GameObject("DetailText").AddComponent<Text>();
            _detailText.transform.SetParent(detailPanel.transform, false);
            _detailText.font = SuperMechUiSkin.DefaultFont;
            _detailText.fontSize = 11;
            _detailText.color = new Color(0.8f, 0.85f, 0.95f);
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
            var dTextRect = _detailText.GetComponent<RectTransform>();
            dTextRect.anchorMin = Vector2.zero;
            dTextRect.anchorMax = Vector2.one;
            dTextRect.offsetMin = new Vector2(10, 8);
            dTextRect.offsetMax = new Vector2(-10, -28);
        }

        private void RefreshList()
        {
            if (_listContent == null || _countText == null || _emptyText == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name != "Empty") Destroy(child.gameObject);
            }

            var factions = SuperMechFaction.GetAllFactions();
            _countText.text = $"{LocalizedTextManager.getText("sm_ui_faction_total")}: {factions.Count}";
            _emptyText.gameObject.SetActive(factions.Count == 0);

            float y = 0f;
            foreach (var f in factions)
            {
                CreateFactionRow(f, ref y);
                if (f.id == _selectedFactionId) ShowMembers(f);
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void CreateFactionRow(SuperMechFaction.FactionData f, ref float y)
        {
            var row = new GameObject($"Faction_{f.id}");
            row.transform.SetParent(_listContent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 40);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 44f;

            var bg = row.AddComponent<Image>();
            bg.color = f.id == _selectedFactionId ?
                new Color(0.25f, 0.45f, 0.7f, 0.35f) : // 选中行淡蓝高亮（非大色块，保留）
                new Color(0, 0, 0, 0f); // v0.239.6 去自定义块：行背景透明

            // 等级标签
            var levelText = new GameObject("Level").AddComponent<Text>();
            levelText.transform.SetParent(row.transform, false);
            levelText.font = SuperMechUiSkin.DefaultFont;
            levelText.fontSize = 14;
            levelText.color = SuperMechUiSkin.CyanColor;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.text = $"Lv{f.level}";
            var lvRect = levelText.GetComponent<RectTransform>();
            lvRect.anchorMin = new Vector2(0, 0);
            lvRect.anchorMax = new Vector2(0, 1);
            lvRect.pivot = new Vector2(0, 0.5f);
            lvRect.sizeDelta = new Vector2(44, 0);
            lvRect.anchoredPosition = new Vector2(8, 0);

            // 名称+成员数+领袖+战力
            var leaderName = "?";
            var units = World.world.units?.units_only_alive;
            if (units != null)
            {
                foreach (var a in units) { if (a != null && a.id == f.leaderId) { leaderName = a.name; break; } }
            }

            var infoText = new GameObject("Info").AddComponent<Text>();
            infoText.transform.SetParent(row.transform, false);
            infoText.font = SuperMechUiSkin.DefaultFont;
            infoText.fontSize = 12;
            infoText.color = new Color(0.9f, 0.92f, 0.98f);
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.text = $"<b>{f.name}</b>  {f.memberIds.Count}人  领袖:{leaderName}  战力:{f.totalPower:F0}";
            var infoRect = infoText.GetComponent<RectTransform>();
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(56, 0);
            infoRect.offsetMax = new Vector2(-10, 0);

            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                _selectedFactionId = (_selectedFactionId == f.id) ? null : f.id;
                RefreshList();
            });
        }

        private void ShowMembers(SuperMechFaction.FactionData f)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#59BFD9><b>{f.name}</b></color> 成员列表：");
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
                    string role = (a.id == f.leaderId) ?
                        LocalizedTextManager.getText("sm_ui_leader") :
                        LocalizedTextManager.getText("sm_ui_member");
                    sb.AppendLine($"{idx}. {a.name}  [{rankName}]  {role}");
                    idx++;
                    break;
                }
            }
            if (idx == 1) sb.AppendLine(LocalizedTextManager.getText("sm_ui_no_alive_members"));
            _detailText.text = sb.ToString();
        }
    }
}
