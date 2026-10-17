using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.209 王国总览=全球对比表：所有王国关键指标横向对比
    /// 点击行打开原版KingdomWindow查看详情和操作
    /// 定位差异：原版面板=单王国详情+操作入口；总览=全球对比速览
    /// </summary>
    public class SuperMechKingdomView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _countText;
        private Text _emptyText;
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
            countRect.offsetMax = new Vector2(-8, 0);

            // v0.209 滚动列表全高度（移除底部详情，详情在原版王国面板查看）
            var scrollGo = new GameObject("KingdomScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
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

            // 获取所有王国（排除野生）
            var kingdoms = new List<Kingdom>();
            int totalKingdoms = 0;
            int wildCount = 0;
            if (World.world != null && World.world.kingdoms != null)
            {
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null) continue;
                    totalKingdoms++;
                    if (k.wild) { wildCount++; continue; }
                    kingdoms.Add(k);
                }
            }
            Debug.Log($"[超神机械师] 王国总览: 总数={totalKingdoms}, 野生={wildCount}, 文明={kingdoms.Count}");

            _countText.text = $"{LocalizedTextManager.getText("sm_ui_kingdom_total")}: {kingdoms.Count}";
            _emptyText.gameObject.SetActive(kingdoms.Count == 0);

            float y = 0f;
            foreach (var k in kingdoms)
            {
                CreateKingdomRow(k, ref y);
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void CreateKingdomRow(Kingdom k, ref float y)
        {
            var row = new GameObject($"Kingdom_{k.getID()}");
            row.transform.SetParent(_listContent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 58);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 60f;

            bool selected = k.getID() == SuperMechWindowManager.SelectedKingdomId;
            var bg = row.AddComponent<Image>();
            bg.color = selected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(1, 1, 1, 0.05f);

            // v0.209 全球对比表：关键指标横向排列
            string kingName = k.king != null ? k.king.name : "—";
            var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(k);
            int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(k);
            int superCount = CountSuperUnits(k);
            int population = k.getPopulationTotal();
            int cityCount = k.countCities();
            int warriors = k.countTotalWarriors();
            int buildingCount = k.countBuildings();

            var label = SuperMechUiBuilder.AddText(row,
                $"<b>{k.data.name}</b>  王:{kingName}  {SuperMechCivilization.GetLevelName(civLevel)}\n" +
                $"人口:{population}  城市:{cityCount}  军力:{warriors}  科技:Lv{techLevel}  建筑:{buildingCount}  超能者:{superCount}",
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
    }
}
