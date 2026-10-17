using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.231 宇宙深化UI面板
    /// 集成：星球探索、星际贸易、太空站管理、科技树
    /// 参考：无人深空(探索)、EVE(贸易)、X4(太空站)、战锤40K(科技树)
    /// </summary>
    public class SuperMechSpaceExpansionUI : MonoBehaviour
    {
        private static SuperMechSpaceExpansionUI _instance;
        public static SuperMechSpaceExpansionUI Instance => _instance;

        private GameObject _panel;
        private RectTransform _content;
        private Text _titleText;
        private string _currentPlanetId;
        private int _currentTab; // 0=探索, 1=贸易, 2=太空站, 3=科技树

        // 探索事件弹窗
        private GameObject _eventPopup;
        private Text _eventTitle;
        private Text _eventDesc;

        void Awake()
        {
            _instance = this;
            BuildPanel();
        }

        /// <summary>构建主面板</summary>
        private void BuildPanel()
        {
            _panel = new GameObject("SpaceExpansionPanel");
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(680, 520);
            panelRect.anchoredPosition = Vector2.zero;

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);

            // 标题栏
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panel.transform, false);
            var titleRect = titleBar.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);

            _titleText = new GameObject("Title").AddComponent<Text>();
            _titleText.transform.SetParent(titleBar.transform, false);
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 16;
            _titleText.color = new Color(0.4f, 0.8f, 1f);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.text = LocalizedTextManager.getText("sm_ui_space_expansion");
            var titleTextRect = _titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(50, 0);
            titleTextRect.offsetMax = new Vector2(-50, 0);

            // 关闭按钮
            var closeBtn = CreateButton(titleBar.transform, "X", new Vector2(0, 0), new Vector2(36, 36),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), () => _panel.SetActive(false));
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(-20, 0);

            // 标签栏（4个标签）
            string[] tabNames = { "sm_ui_tab_exploration", "sm_ui_tab_trade", "sm_ui_tab_station", "sm_ui_tab_tech" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var tab = CreateButton(_panel.transform, LocalizedTextManager.getText(tabNames[i]),
                    new Vector2(10 + i * 165, -46), new Vector2(155, 32),
                    new Vector2(0, 1), new Vector2(0, 1),
                    () => { _currentTab = idx; RefreshContent(); });
            }

            // 内容区
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(_panel.transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 0);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 0.5f);
            _content.offsetMin = new Vector2(10, 10);
            _content.offsetMax = new Vector2(-10, -90);

            _panel.SetActive(false);
        }

        /// <summary>打开面板</summary>
        public void Open(string planetId)
        {
            _currentPlanetId = planetId;
            _currentTab = 0;
            _panel.SetActive(true);
            RefreshContent();
        }

        /// <summary>刷新内容</summary>
        private void RefreshContent()
        {
            // 清空内容
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            switch (_currentTab)
            {
                case 0: BuildExplorationTab(); break;
                case 1: BuildTradeTab(); break;
                case 2: BuildStationTab(); break;
                case 3: BuildTechTab(); break;
            }
        }

        #region 探索标签（无人深空式）

        private void BuildExplorationTab()
        {
            var data = PlanetExplorationManager.GetPlanetData(_currentPlanetId);
            if (data == null) return;

            float y = 0f;

            // 星球基本信息
            AddInfoRow(LocalizedTextManager.getText("sm_ui_climate"), PlanetExplorationManager.GetClimateName(data.climate), ref y);
            AddInfoRow(LocalizedTextManager.getText("sm_ui_resource_type"), PlanetExplorationManager.GetResourceName(data.resourceType), ref y);
            AddInfoRow(LocalizedTextManager.getText("sm_ui_biome"), PlanetExplorationManager.GetBiomeName(data.biome), ref y);
            AddInfoRow(LocalizedTextManager.getText("sm_ui_danger_level"), $"{data.dangerLevel}/10", ref y);
            AddInfoRow(LocalizedTextManager.getText("sm_ui_explore_count"), data.explorationCount.ToString(), ref y);
            y += 10f;

            // 探索按钮
            var exploreBtn = CreateButton(_content, LocalizedTextManager.getText("sm_ui_explore_planet_v2"),
                new Vector2(0, -y - 30), new Vector2(200, 40),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                () =>
                {
                    var evt = PlanetExplorationManager.Explore(_currentPlanetId);
                    if (evt != null) ShowEventPopup(evt);
                    RefreshContent();
                });
            var exploreImg = exploreBtn.GetComponent<Image>();
            exploreImg.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
        }

        private void ShowEventPopup(ExplorationEvent evt)
        {
            if (_eventPopup == null)
            {
                _eventPopup = new GameObject("EventPopup");
                _eventPopup.transform.SetParent(_panel.transform, false);
                var rect = _eventPopup.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(400, 250);
                var bg = _eventPopup.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.1f, 0.18f, 0.98f);

                _eventTitle = new GameObject("Title").AddComponent<Text>();
                _eventTitle.transform.SetParent(_eventPopup.transform, false);
                _eventTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _eventTitle.fontSize = 18;
                _eventTitle.color = new Color(1f, 0.85f, 0.4f);
                _eventTitle.alignment = TextAnchor.MiddleCenter;
                var tRect = _eventTitle.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 1);
                tRect.anchorMax = new Vector2(1, 1);
                tRect.sizeDelta = new Vector2(0, 40);
                tRect.anchoredPosition = new Vector2(0, -20);

                _eventDesc = new GameObject("Desc").AddComponent<Text>();
                _eventDesc.transform.SetParent(_eventPopup.transform, false);
                _eventDesc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _eventDesc.fontSize = 13;
                _eventDesc.color = new Color(0.85f, 0.9f, 1f);
                _eventDesc.alignment = TextAnchor.UpperCenter;
                _eventDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                var dRect = _eventDesc.GetComponent<RectTransform>();
                dRect.anchorMin = new Vector2(0, 0);
                dRect.anchorMax = new Vector2(1, 1);
                dRect.offsetMin = new Vector2(20, 60);
                dRect.offsetMax = new Vector2(-20, -50);

                var okBtn = CreateButton(_eventPopup.transform, LocalizedTextManager.getText("sm_ui_confirm"),
                    new Vector2(0, 20), new Vector2(120, 36),
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                    () => _eventPopup.SetActive(false));
            }
            _eventTitle.text = evt.title;
            string desc = evt.description;
            if (evt.resourceReward > 0) desc += $"\n\n获得资源: +{evt.resourceReward}";
            if (evt.expReward > 0) desc += $"\n获得经验: +{evt.expReward}";
            if (evt.isDangerous && evt.dangerDamage > 0) desc += $"\n受到伤害: -{evt.dangerDamage}";
            _eventDesc.text = desc;
            _eventPopup.SetActive(true);
        }

        #endregion

        #region 贸易标签（EVE式）

        private void BuildTradeTab()
        {
            var data = PlanetExplorationManager.GetPlanetData(_currentPlanetId);
            if (data?.market == null) return;

            float y = 0f;

            // 货舱状态
            AddInfoRow(LocalizedTextManager.getText("sm_ui_cargo"), $"{TradeManager.GetCargoUsed()}/{TradeManager.GetCargoCapacity()}", ref y);
            y += 5f;

            // 商品列表
            AddHeader(LocalizedTextManager.getText("sm_ui_goods_market"), ref y);
            foreach (TradeGoodType type in Enum.GetValues(typeof(TradeGoodType)))
            {
                if (!data.market.buyPrice.TryGetValue(type, out int buy)) continue;
                if (!data.market.sellPrice.TryGetValue(type, out int sell)) continue;
                if (!data.market.stock.TryGetValue(type, out int stock)) continue;

                string name = TradeManager.GetGoodName(type);
                int cargo = TradeManager.GetCargo(type);

                // 商品行
                var row = new GameObject($"Good_{type}");
                row.transform.SetParent(_content, false);
                var rowRect = row.AddComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.sizeDelta = new Vector2(0, 32);
                rowRect.anchoredPosition = new Vector2(0, -y);
                y += 34f;

                var label = CreateText(row.transform, $"{name}  买:{sell} 卖:{buy} 库存:{stock} 持有:{cargo}", 11, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-160, 0);

                // 买入10按钮
                var buyBtn = CreateButton(row.transform, LocalizedTextManager.getText("sm_ui_buy_10"),
                    new Vector2(-80, 0), new Vector2(70, 26),
                    new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                    () => { TradeManager.BuyGood(_currentPlanetId, type, 10); RefreshContent(); });

                // 卖出10按钮
                var sellBtn = CreateButton(row.transform, LocalizedTextManager.getText("sm_ui_sell_10"),
                    new Vector2(-4, 0), new Vector2(70, 26),
                    new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                    () => { TradeManager.SellGood(_currentPlanetId, type, 10); RefreshContent(); });
            }
        }

        #endregion

        #region 太空站标签（X4式）

        private void BuildStationTab()
        {
            float y = 0f;
            var planet = SuperMechStarSystem.GetPlanet(_currentPlanetId);
            var stations = StationManager.GetStationsAtPlanet(_currentPlanetId);

            AddHeader(LocalizedTextManager.getText("sm_ui_built_stations"), ref y);
            if (stations.Count == 0)
            {
                AddInfoRow("—", "", ref y);
            }
            else
            {
                foreach (var s in stations)
                {
                    AddInfoRow($"{s.name} Lv{s.level}", StationManager.GetStationEffect(s.type, s.level), ref y);
                }
            }
            y += 10f;

            AddHeader(LocalizedTextManager.getText("sm_ui_buildable"), ref y);
            foreach (StationType type in Enum.GetValues(typeof(StationType)))
            {
                bool exists = stations.Exists(s => s.type == type);
                int cost = StationManager.GetBuildCost(type);
                string name = StationManager.GetStationName(type);
                string effect = StationManager.GetStationEffect(type, 1);

                var row = new GameObject($"Build_{type}");
                row.transform.SetParent(_content, false);
                var rowRect = row.AddComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.sizeDelta = new Vector2(0, 36);
                rowRect.anchoredPosition = new Vector2(0, -y);
                y += 38f;

                var label = CreateText(row.transform, $"{name}  花费:{cost}  {effect}", 11, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-100, 0);

                if (!exists)
                {
                    var buildBtn = CreateButton(row.transform, LocalizedTextManager.getText("sm_ui_build"),
                        new Vector2(-10, 0), new Vector2(80, 28),
                        new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                        () => { StationManager.BuildStation(_currentPlanetId, type); RefreshContent(); });
                }
                else
                {
                    var label2 = CreateText(row.transform, LocalizedTextManager.getText("sm_ui_built"), 11, TextAnchor.MiddleRight);
                    var r2 = label2.GetComponent<RectTransform>();
                    r2.anchorMin = new Vector2(1, 0);
                    r2.anchorMax = new Vector2(1, 1);
                    r2.sizeDelta = new Vector2(80, 0);
                    r2.anchoredPosition = new Vector2(-10, 0);
                    label2.color = new Color(0.4f, 0.8f, 0.4f);
                }
            }
        }

        #endregion

        #region 科技树标签（战锤式）

        private void BuildTechTab()
        {
            float y = 0f;

            // 当前研究
            var current = TechTreeManager.GetCurrentResearch();
            if (current != null)
            {
                float progress = TechTreeManager.GetResearchProgress();
                AddInfoRow(LocalizedTextManager.getText("sm_ui_researching"), $"{current.name} ({progress * 100:F0}%)", ref y);
            }
            else
            {
                AddInfoRow(LocalizedTextManager.getText("sm_ui_researching"), LocalizedTextManager.getText("sm_ui_idle"), ref y);
            }
            AddInfoRow(LocalizedTextManager.getText("sm_ui_researched_count"), $"{TechTreeManager.GetResearchedCount()}/30", ref y);
            y += 5f;

            // 按领域显示
            foreach (TechField field in Enum.GetValues(typeof(TechField)))
            {
                AddHeader(TechTreeManager.GetFieldName(field), ref y);
                var techs = TechTreeManager.GetTechByField(field);
                foreach (var tech in techs)
                {
                    bool researched = TechTreeManager.IsResearched(tech.id);
                    bool canResearch = TechTreeManager.CanResearch(tech.id);
                    string status = researched ? "✓" : (canResearch ? LocalizedTextManager.getText("sm_ui_can_research") : LocalizedTextManager.getText("sm_ui_locked"));

                    var row = new GameObject($"Tech_{tech.id}");
                    row.transform.SetParent(_content, false);
                    var rowRect = row.AddComponent<RectTransform>();
                    rowRect.anchorMin = new Vector2(0, 1);
                    rowRect.anchorMax = new Vector2(1, 1);
                    rowRect.pivot = new Vector2(0.5f, 1);
                    rowRect.sizeDelta = new Vector2(0, 28);
                    rowRect.anchoredPosition = new Vector2(0, -y);
                    y += 30f;

                    var label = CreateText(row.transform, $"T{tech.tier} {tech.name}  花费:{tech.cost}  [{status}]", 11, TextAnchor.MiddleLeft);
                    var labelRect = label.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(8, 0);
                    labelRect.offsetMax = new Vector2(-90, 0);
                    label.color = researched ? new Color(0.4f, 0.8f, 0.4f) : (canResearch ? Color.white : new Color(0.5f, 0.5f, 0.5f));

                    if (canResearch)
                    {
                        var btn = CreateButton(row.transform, LocalizedTextManager.getText("sm_ui_research_btn"),
                            new Vector2(-10, 0), new Vector2(70, 24),
                            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                            () => { TechTreeManager.StartResearch(tech.id); RefreshContent(); });
                    }
                }
                y += 4f;
            }
        }

        #endregion

        #region 辅助方法

        private void AddInfoRow(string label, string value, ref float y)
        {
            var row = new GameObject("InfoRow");
            row.transform.SetParent(_content, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.sizeDelta = new Vector2(0, 24);
            rowRect.anchoredPosition = new Vector2(0, -y);
            y += 26f;

            var labelText = CreateText(row.transform, label, 12, TextAnchor.MiddleLeft);
            var lRect = labelText.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = new Vector2(0.4f, 1);
            lRect.offsetMin = new Vector2(12, 0);
            lRect.offsetMax = Vector2.zero;
            labelText.color = new Color(0.6f, 0.7f, 0.85f);

            var valueText = CreateText(row.transform, value, 12, TextAnchor.MiddleLeft);
            var vRect = valueText.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.4f, 0);
            vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = new Vector2(-12, 0);
            valueText.color = new Color(0.9f, 0.95f, 1f);
        }

        private void AddHeader(string text, ref float y)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(_content, false);
            var hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.sizeDelta = new Vector2(0, 28);
            hRect.anchoredPosition = new Vector2(0, -y);
            y += 30f;

            var hBg = header.AddComponent<Image>();
            hBg.color = new Color(0.15f, 0.2f, 0.35f, 0.6f);

            var hText = CreateText(header.transform, text, 13, TextAnchor.MiddleLeft);
            var htRect = hText.GetComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = new Vector2(12, 0);
            htRect.offsetMax = Vector2.zero;
            hText.color = new Color(0.4f, 0.8f, 1f);
            hText.fontStyle = FontStyle.Bold;
        }

        private GameObject CreateButton(Transform parent, string text, Vector2 pos, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var label = CreateText(go.transform, text, 12, TextAnchor.MiddleCenter);
            var lRect = label.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            return go;
        }

        private Text CreateText(Transform parent, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = anchor;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        #endregion
    }
}
