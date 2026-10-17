using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.230 文明总览重构：卡片式布局，关键指标清晰排列
    /// 点击行打开原版KingdomWindow查看详情和操作
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
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 32);
            var topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0.08f, 0.10f, 0.16f, 0.6f);

            _countText = new GameObject("Count").AddComponent<Text>();
            _countText.transform.SetParent(topBar.transform, false);
            _countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _countText.fontSize = 13;
            _countText.color = new Color(0.85f, 0.9f, 1f);
            _countText.alignment = TextAnchor.MiddleLeft;
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(12, 0);
            countRect.offsetMax = new Vector2(-12, 0);

            // 滚动列表
            var scrollGo = new GameObject("KingdomScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.offsetMin = new Vector2(6, 6);
            scrollRect.offsetMax = new Vector2(-6, -38);
            _scroll = scrollGo.AddComponent<ScrollRect>();
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
            _scroll.viewport = vpRect;
            _scroll.content = _listContent;
            _scroll.vertical = true;
            _scroll.horizontal = false;

            _emptyText = new GameObject("Empty").AddComponent<Text>();
            _emptyText.transform.SetParent(content.transform, false);
            _emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _emptyText.fontSize = 12;
            _emptyText.color = new Color(0.6f, 0.65f, 0.75f);
            _emptyText.alignment = TextAnchor.MiddleCenter;
            _emptyText.text = LocalizedTextManager.getText("sm_ui_kingdom_none");
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.sizeDelta = new Vector2(0, 30);
        }

        private void RefreshList()
        {
            if (_listContent == null || _countText == null || _emptyText == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name != "Empty") Destroy(child.gameObject);
            }

            var kingdoms = new List<Kingdom>();
            if (World.world != null && World.world.kingdoms != null)
            {
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.wild) continue;
                    kingdoms.Add(k);
                }
            }

            _countText.text = $"{LocalizedTextManager.getText("sm_ui_kingdom_total")}: {kingdoms.Count}";
            _emptyText.gameObject.SetActive(kingdoms.Count == 0);

            // 按人口排序
            kingdoms.Sort((a, b) => b.getPopulationTotal().CompareTo(a.getPopulationTotal()));

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
            rect.sizeDelta = new Vector2(0, 64);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 68f;

            bool selected = k.getID() == SuperMechWindowManager.SelectedKingdomId;
            var bg = row.AddComponent<Image>();
            bg.color = selected ?
                new Color(0.25f, 0.45f, 0.7f, 0.35f) :
                new Color(0.06f, 0.08f, 0.14f, 0.6f);

            // 左侧：文明名称+国王+等级
            var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(k);
            string kingName = k.king != null ? k.king.name : "—";

            var nameText = new GameObject("Name").AddComponent<Text>();
            nameText.transform.SetParent(row.transform, false);
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = SuperMechUiSkin.CyanColor;
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.text = $"<b>{k.data.name}</b>  <size=10><color=#99A8C2>王:{kingName}</color></size>";
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.45f, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-4, -6);

            // 文明等级标签
            var levelText = new GameObject("Level").AddComponent<Text>();
            levelText.transform.SetParent(row.transform, false);
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 10;
            levelText.color = new Color(0.85f, 0.75f, 0.4f);
            levelText.alignment = TextAnchor.LowerLeft;
            levelText.text = SuperMechCivilization.GetLevelName(civLevel);
            var levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(0.45f, 0);
            levelRect.pivot = new Vector2(0, 0);
            levelRect.offsetMin = new Vector2(10, 6);
            levelRect.offsetMax = new Vector2(-4, 24);

            // 右侧：关键指标横向排列
            int population = k.getPopulationTotal();
            int cityCount = k.countCities();
            int warriors = k.countTotalWarriors();
            int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(k);
            int superCount = CountSuperUnits(k);

            string stats = $"人口 {population}    城市 {cityCount}    军力 {warriors}\n" +
                           $"科技 Lv{techLevel}    超能者 {superCount}";
            var statsText = new GameObject("Stats").AddComponent<Text>();
            statsText.transform.SetParent(row.transform, false);
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 11;
            statsText.color = new Color(0.8f, 0.85f, 0.95f);
            statsText.alignment = TextAnchor.MiddleRight;
            statsText.text = stats;
            var statsRect = statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.45f, 0);
            statsRect.anchorMax = Vector2.one;
            statsRect.pivot = new Vector2(1, 0.5f);
            statsRect.offsetMin = new Vector2(4, 0);
            statsRect.offsetMax = new Vector2(-10, 0);

            // 点击行：打开原版王国面板
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
