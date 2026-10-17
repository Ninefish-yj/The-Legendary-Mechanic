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
        private Text _emptyHintText;
        private GameObject _emptyStateGo;
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

            // 空状态：挂在滚动区上，居中显示（v0.237.2 修复：原来挂在Content里导致文字跑位）
            var emptyGo = new GameObject("EmptyState");
            emptyGo.transform.SetParent(scrollGo.transform, false);
            var emptyRect = emptyGo.AddComponent<RectTransform>();
            emptyRect.anchorMin = Vector2.zero;
            emptyRect.anchorMax = Vector2.one;
            emptyRect.offsetMin = new Vector2(40, 40);
            emptyRect.offsetMax = new Vector2(-40, -40);
            var emptyBg = emptyGo.AddComponent<Image>();
            // v0.237.6 暖棕半透明（透出原版窗口纹理）
            emptyBg.sprite = null;
            emptyBg.type = Image.Type.Simple;
            emptyBg.color = new Color(0, 0, 0, 0f); // v0.239.6 去自定义块：空状态不再画暖棕卡片，文字直接铺在原版窗口上
            emptyBg.raycastTarget = false;

            var emptyVlg = emptyGo.AddComponent<VerticalLayoutGroup>();
            emptyVlg.padding = new RectOffset(16, 16, 14, 14);
            emptyVlg.spacing = 8;
            emptyVlg.childAlignment = TextAnchor.MiddleCenter;
            emptyVlg.childControlWidth = true;
            emptyVlg.childControlHeight = true; // v0.239.6 接管子项高度：Text用preferred高度，居中才生效
            emptyVlg.childForceExpandWidth = true;
            emptyVlg.childForceExpandHeight = false;

            _emptyStateGo = emptyGo;
            _emptyText = new GameObject("Empty").AddComponent<Text>();
            _emptyText.transform.SetParent(emptyGo.transform, false);
            _emptyText.font = SuperMechUiSkin.DefaultFont;
            _emptyText.fontSize = 14;
            _emptyText.fontStyle = FontStyle.Bold;
            _emptyText.color = new Color(0.85f, 0.75f, 0.55f);
            _emptyText.alignment = TextAnchor.MiddleCenter;
            _emptyText.text = LocalizedTextManager.getText("sm_ui_kingdom_none");
            var emptyTextRect = _emptyText.GetComponent<RectTransform>();
            emptyTextRect.sizeDelta = new Vector2(0, 28); // v0.239.6 固定高度，防VG布局计算异常

            _emptyHintText = new GameObject("EmptyHint").AddComponent<Text>();
            _emptyHintText.transform.SetParent(emptyGo.transform, false);
            _emptyHintText.font = SuperMechUiSkin.DefaultFont;
            _emptyHintText.fontSize = 11;
            _emptyHintText.color = new Color(0.55f, 0.6f, 0.7f);
            _emptyHintText.alignment = TextAnchor.MiddleCenter;
            _emptyHintText.text = LocalizedTextManager.getText("sm_ui_kingdom_empty_hint");
            var hintRect = _emptyHintText.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(0, 20);
        }

        private void RefreshList()
        {
            if (_listContent == null || _countText == null || _emptyText == null) return;
            // v0.237.2 空态已挂在滚动区上，Content里不再有Empty子对象，直接全清
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_listContent.GetChild(i).gameObject);
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
            if (_emptyStateGo != null) _emptyStateGo.SetActive(kingdoms.Count == 0);

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
                new Color(0.25f, 0.45f, 0.7f, 0.35f) : // 选中行淡蓝高亮（非大色块，保留）
                new Color(0, 0, 0, 0f); // v0.239.6 去自定义块：行背景透明

            // 左侧：文明名称+国王+等级
            var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(k);
            string kingName = k.king != null ? k.king.name : "—";

            var nameText = new GameObject("Name").AddComponent<Text>();
            nameText.transform.SetParent(row.transform, false);
            nameText.font = SuperMechUiSkin.DefaultFont;
            nameText.fontSize = 14;
            nameText.color = SuperMechUiSkin.CyanColor;
            nameText.alignment = TextAnchor.UpperLeft;
            nameText.text = $"<b>{k.data.name}</b>  <size=10><color=#99A8C2>{LocalizedTextManager.getText("sm_ui_king")}:{kingName}</color></size>";
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.45f, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.offsetMin = new Vector2(10, 0);
            nameRect.offsetMax = new Vector2(-4, -6);

            // 文明等级标签
            var levelText = new GameObject("Level").AddComponent<Text>();
            levelText.transform.SetParent(row.transform, false);
            levelText.font = SuperMechUiSkin.DefaultFont;
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

            string stats = $"{LocalizedTextManager.getText("sm_ui_population")} {population}    {LocalizedTextManager.getText("sm_ui_city")} {cityCount}    {LocalizedTextManager.getText("sm_ui_military")} {warriors}\n" +
                           $"{LocalizedTextManager.getText("sm_ui_tech_lv")} Lv{techLevel}    {LocalizedTextManager.getText("sm_ui_supera")} {superCount}";
            var statsText = new GameObject("Stats").AddComponent<Text>();
            statsText.transform.SetParent(row.transform, false);
            statsText.font = SuperMechUiSkin.DefaultFont;
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
