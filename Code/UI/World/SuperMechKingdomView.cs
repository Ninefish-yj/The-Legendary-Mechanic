using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.206 王国总览面板：展示所有原版文明王国，点击选中后联动经济/科技/建筑等面板
    /// 替代原FactionView（模组独立势力系统收进王国详情子区域）
    /// </summary>
    public class SuperMechKingdomView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _countText;
        private Text _emptyText;
        private Text _detailText;
        private ScrollRect _scroll;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] KingdomView初始化失败: " + e); } }
        void OnEnable() { try { RefreshList(); } catch (System.Exception e) { Debug.LogError("[超神机械师] KingdomView刷新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部统计栏
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, LocalizedTextManager.getText("sm_ui_kingdom_count_zero"), 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-80, 0);

            // 外交按钮
            var diplBtn = SuperMechUiSkin.MakeButton(topBar.transform, LocalizedTextManager.getText("sm_ui_diplomacy"), 10, () => SuperMechDiplomacyView.Toggle());
            var diplRect = diplBtn.GetComponent<RectTransform>();
            diplRect.anchorMin = new Vector2(1, 0.5f);
            diplRect.anchorMax = new Vector2(1, 0.5f);
            diplRect.sizeDelta = new Vector2(48, 22);
            diplRect.anchoredPosition = new Vector2(-176, 0);

            // 战报按钮
            var reportBtn = SuperMechUiSkin.MakeButton(topBar.transform, LocalizedTextManager.getText("sm_ui_battle_report"), 10, () => SuperMechBattleReportView.Toggle());
            var reportRect = reportBtn.GetComponent<RectTransform>();
            reportRect.anchorMin = new Vector2(1, 0.5f);
            reportRect.anchorMax = new Vector2(1, 0.5f);
            reportRect.sizeDelta = new Vector2(48, 22);
            reportRect.anchoredPosition = new Vector2(-124, 0);

            // 阶位排行按钮
            var rankBtn = SuperMechUiSkin.MakeButton(topBar.transform, LocalizedTextManager.getText("sm_ui_supera_ranking"), 10, () => SuperMechWindowManager.OpenRank());
            var rankRect = rankBtn.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(1, 0.5f);
            rankRect.anchorMax = new Vector2(1, 0.5f);
            rankRect.sizeDelta = new Vector2(56, 22);
            rankRect.anchoredPosition = new Vector2(-64, 0);

            // 滚动列表
            var scrollGo = new GameObject("KingdomScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.35f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 1);
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-4, -32);
            _scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>();
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _listContent = content.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 100);
            _scroll.viewport = vpRect;
            _scroll.content = _listContent;
            _scroll.vertical = true;
            _scroll.horizontal = false;

            _emptyText = SuperMechUiBuilder.AddText(content, LocalizedTextManager.getText("sm_ui_kingdom_none"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.sizeDelta = new Vector2(0, 30);

            // 底部王国详情
            var detailPanel = SuperMechUiBuilder.CreatePanel(transform, "DetailPanel", new Color(0, 0, 0, 0.15f), 4, 4, 0, 0);
            var dRect = detailPanel.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = new Vector2(1, 0.35f);
            dRect.pivot = new Vector2(0.5f, 0);
            dRect.offsetMin = new Vector2(4, 4);
            dRect.offsetMax = new Vector2(-4, 0);
            _detailText = SuperMechUiBuilder.AddText(detailPanel, LocalizedTextManager.getText("sm_ui_kingdom_click"), 11, TextAnchor.UpperLeft);
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
            if (_listContent == null || _countText == null || _emptyText == null) return;

            // 清空旧条目
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name != "EmptyText") Destroy(child.gameObject);
            }

            // 获取所有文明王国（排除野生）
            var kingdoms = new List<Kingdom>();
            if (World.world != null && World.world.kingdoms != null)
            {
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k != null && !k.wild && k.isCiv()) kingdoms.Add(k);
                }
            }

            _countText.text = $"{LocalizedTextManager.getText("sm_ui_kingdom_total")}: {kingdoms.Count}";
            _emptyText.gameObject.SetActive(kingdoms.Count == 0);

            float y = 0f;
            foreach (var k in kingdoms)
            {
                CreateKingdomRow(k, ref y);
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));

            // 如果有选中的王国，显示详情
            if (SuperMechWindowManager.SelectedKingdomId >= 0)
            {
                foreach (var k in kingdoms)
                {
                    if (k.getID() == SuperMechWindowManager.SelectedKingdomId)
                    {
                        ShowDetail(k);
                        break;
                    }
                }
            }
        }

        private void CreateKingdomRow(Kingdom k, ref float y)
        {
            var row = new GameObject($"Kingdom_{k.getID()}");
            row.transform.SetParent(_listContent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 40);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 42f;

            bool selected = k.getID() == SuperMechWindowManager.SelectedKingdomId;
            var bg = row.AddComponent<Image>();
            bg.color = selected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(1, 1, 1, 0.05f);

            // 王国名+国王+文明等级+科技等级+超能者数
            string kingName = k.king != null ? k.king.name : "—";
            var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(k);
            int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(k);
            int superCount = CountSuperUnits(k);

            var label = SuperMechUiBuilder.AddText(row,
                $"<b>{k.data.name}</b>  王:{kingName}\n" +
                $"文明:{SuperMechCivilization.GetLevelName(civLevel)}  科技:Lv{techLevel}  超能者:{superCount}",
                11, TextAnchor.MiddleLeft);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, 0);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 点击行：选中王国 + 打开原版KingdomWindow（和单位系统操作一致）
            var btn = row.AddComponent<Button>();
            Kingdom captured = k;
            btn.onClick.AddListener(() =>
            {
                SuperMechWindowManager.SelectedKingdomId = captured.getID();
                SelectedMetas.selected_kingdom = captured;
                ScrollWindow.showWindow("kingdom");
            });
        }

        private int CountSuperUnits(Kingdom k)
        {
            int count = 0;
            var units = World.world.units?.units_only_alive;
            if (units == null) return 0;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.kingdom != k) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) >= 8) count++;
            }
            return count;
        }

        private void ShowDetail(Kingdom k)
        {
            if (_detailText == null) return;

            var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(k);
            int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(k);
            float techPoints = SuperMechCivilization.GetTechPointsFromKingdom(k);
            int superCount = CountSuperUnits(k);
            long kid = k.getID();
            float culturePower = SuperMechCultureAI.GetCulturePower(kid);
            string strategy = SuperMechCivStrategyAI.GetStrategyName(kid);

            // 殖民地数量
            int colonyCount = 0;
            foreach (var p in SuperMechStarSystem.GetAllPlanets())
            {
                if (p != null && p.colonyLevel > 0 && p.id != SuperMechStarSystem.MAIN_PLANET
                    && p.ownerKingdom == k.data.name) colonyCount++;
            }

            // 该王国的模组势力
            var factions = SuperMechFaction.GetAllFactions();
            int factionCount = 0;
            foreach (var f in factions)
            {
                var leader = World.world.units?.Get(f.leaderId);
                if (leader != null && leader.kingdom == k) factionCount++;
            }

            _detailText.text =
                $"【{k.data.name}】详细信息\n" +
                $"━━━━━━━━━━━━━━━━\n" +
                $"文明等级: {SuperMechCivilization.GetLevelName(civLevel)}\n" +
                $"科技等级: Lv{techLevel} (科技点:{techPoints:F0})\n" +
                $"超能者: {superCount}人 (A阶及以上)\n" +
                $"星际殖民地: {colonyCount}个\n" +
                $"文化力: {(culturePower > 0 ? culturePower.ToString("F1") : "—")}\n" +
                $"战略: {(string.IsNullOrEmpty(strategy) || strategy == "None" ? "—" : strategy)}\n" +
                $"境内势力: {factionCount}个\n" +
                $"\n→ 切换到「经济」「科技」「建筑」标签页查看该王国详情";
        }
    }
}
