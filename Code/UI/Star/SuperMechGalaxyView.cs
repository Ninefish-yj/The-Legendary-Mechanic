using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.229 银河地图视图：多星系地图、舰队管理、外交入口
    /// 参考Stellaris/群星的银河地图布局
    /// </summary>
    public class SuperMechGalaxyView : MonoBehaviour
    {
        private static SuperMechGalaxyView _instance;
        public static SuperMechGalaxyView Instance => _instance;

        private GameObject _window;
        private RectTransform _mapContent;
        private ScrollRect _scrollRect;
        private GameObject _infoPanel;
        private Text _infoText;
        private RectTransform _buttonArea;
        private string _selectedSystem;
        private Dictionary<string, GameObject> _systemNodes = new Dictionary<string, GameObject>();

        private const float MAP_SIZE = 2000f;
        private const float NODE_SIZE = 24f;

        /// <summary>获取字体</summary>
        private static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>获取Canvas变换</summary>
        private static Transform GetCanvasTransform()
        {
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        /// <summary>创建按钮</summary>
        private static GameObject CreateButton(Transform parent, string text, Color bgColor, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("Button");
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.color = bgColor;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2);
            textRect.offsetMax = new Vector2(-4, -2);
            var txt = textGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = text;
            return btnGo;
        }

        /// <summary>创建操作按钮（纵向排列）</summary>
        private static void CreateActionButton(Transform parent, string text, Color bgColor, UnityEngine.Events.UnityAction action, ref float y, float height, float gap)
        {
            var btnGo = CreateButton(parent, text, bgColor, action);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = new Vector2(0, -y);
            y += height + gap;
        }

        /// <summary>打开银河地图</summary>
        public static void Open()
        {
            if (_instance != null) return;

            // 确保银河已初始化
            if (!GalaxyManager.IsInitialized)
            {
                GalaxyManager.InitGalaxy();
                AlienCivManager.EnsurePlayerCiv();
            }
            FleetManager.Init();

            var go = new GameObject("GalaxyView");
            _instance = go.AddComponent<SuperMechGalaxyView>();
            _instance.BuildWindow();
        }

        /// <summary>关闭银河地图</summary>
        public static void Close()
        {
            if (_instance != null)
            {
                Destroy(_instance._window);
                _instance = null;
            }
        }

        private void BuildWindow()
        {
            // 全屏窗口
            _window = new GameObject("GalaxyWindow");
            _window.transform.SetParent(GetCanvasTransform(), false);
            var windowRect = _window.AddComponent<RectTransform>();
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;

            // 背景
            var bgImg = _window.AddComponent<Image>();
            bgImg.color = new Color(0.008f, 0.012f, 0.035f, 0.98f);

            // 顶部栏
            BuildTopBar();

            // 地图滚动区域
            BuildMapArea();

            // 右侧详情面板（默认隐藏）
            BuildInfoPanel();

            // 底部状态栏
            BuildBottomBar();

            // 生成星系节点
            GenerateSystemNodes();
        }

        private void BuildTopBar()
        {
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(_window.transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = Vector2.one;
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 44);
            var topImg = topBar.AddComponent<Image>();
            topImg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            // 返回按钮
            var backBtn = CreateButton(topBar.transform, "← " + LocalizedTextManager.getText("sm_ui_enter_main_world"),
                new Color(0.2f, 0.3f, 0.5f, 0.9f), () => { Close(); SuperMechStarView.Open(); });
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.sizeDelta = new Vector2(120, 34);
            backRect.anchoredPosition = new Vector2(10, 0);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topBar.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(300, 30);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.fontSize = 18;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_galaxy_view");

            // 右侧按钮：舰队管理、外交
            var fleetBtn = CreateButton(topBar.transform, "🚀 " + LocalizedTextManager.getText("sm_fleet_manager"),
                new Color(0.3f, 0.5f, 0.3f, 0.9f), ShowFleetPanel);
            var fleetRect = fleetBtn.GetComponent<RectTransform>();
            fleetRect.anchorMin = new Vector2(1, 0);
            fleetRect.anchorMax = new Vector2(1, 1);
            fleetRect.pivot = new Vector2(1, 0.5f);
            fleetRect.sizeDelta = new Vector2(100, 34);
            fleetRect.anchoredPosition = new Vector2(-10, 0);

            var diplBtn = CreateButton(topBar.transform, "🤝 " + LocalizedTextManager.getText("sm_diplomacy"),
                new Color(0.5f, 0.3f, 0.5f, 0.9f), ShowDiplomacyPanel);
            var diplRect = diplBtn.GetComponent<RectTransform>();
            diplRect.anchorMin = new Vector2(1, 0);
            diplRect.anchorMax = new Vector2(1, 1);
            diplRect.pivot = new Vector2(1, 0.5f);
            diplRect.sizeDelta = new Vector2(100, 34);
            diplRect.anchoredPosition = new Vector2(-120, 0);
        }

        private void BuildMapArea()
        {
            // ScrollRect
            var scrollGo = new GameObject("MapScroll");
            scrollGo.transform.SetParent(_window.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(0, 36);
            scrollRect.offsetMax = new Vector2(0, -44);
            _scrollRect = scrollGo.AddComponent<ScrollRect>();

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0.01f, 0.015f, 0.04f, 1f);
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            _scrollRect.viewport = viewportRect;

            // Content（大地图）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _mapContent = contentGo.AddComponent<RectTransform>();
            _mapContent.sizeDelta = new Vector2(MAP_SIZE, MAP_SIZE);
            _mapContent.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContent.pivot = new Vector2(0.5f, 0.5f);
            _scrollRect.content = _mapContent;

            // 银河背景（旋涡臂）
            DrawGalaxyBackground();

            // 初始居中
            _scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
        }

        private void DrawGalaxyBackground()
        {
            // 中心光晕
            var glowGo = new GameObject("CenterGlow");
            glowGo.transform.SetParent(_mapContent, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(600, 600);
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.color = new Color(0.3f, 0.4f, 0.6f, 0.15f);

            // 随机星点
            var rng = new System.Random(GalaxyManager.Galaxy?.seed ?? 12345);
            for (int i = 0; i < 500; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float dist = (float)rng.NextDouble() * 450f;
                float x = Mathf.Cos(angle) * dist;
                float y = Mathf.Sin(angle) * dist;

                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(_mapContent, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                starRect.sizeDelta = new Vector2(2, 2);
                starRect.anchoredPosition = new Vector2(x, y);
                var starImg = starGo.AddComponent<Image>();
                starImg.color = new Color(1, 1, 1, 0.3f + (float)rng.NextDouble() * 0.5f);
            }
        }

        private void GenerateSystemNodes()
        {
            if (GalaxyManager.Galaxy == null) return;

            foreach (var sys in GalaxyManager.Galaxy.systems)
            {
                var nodeGo = new GameObject($"System_{sys.id}");
                nodeGo.transform.SetParent(_mapContent, false);
                var nodeRect = nodeGo.AddComponent<RectTransform>();
                nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
                nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
                nodeRect.pivot = new Vector2(0.5f, 0.5f);
                nodeRect.sizeDelta = new Vector2(NODE_SIZE, NODE_SIZE);
                // 坐标映射：银河坐标(-500~500) → 地图坐标(-1000~1000)
                nodeRect.anchoredPosition = new Vector2(sys.x * 2, sys.y * 2);

                // 星系图标（恒星）
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(nodeGo.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                var iconImg = iconGo.AddComponent<Image>();
                Color nodeColor = sys.discovered ?
                    new Color(1f, 0.9f, 0.6f, 1f) :
                    new Color(0.4f, 0.4f, 0.5f, 0.6f);
                // 有外星文明控制的星系用红色
                if (!string.IsNullOrEmpty(sys.ownerCiv) && sys.ownerCiv != AlienCivManager.PLAYER_CIV_ID)
                    nodeColor = new Color(1f, 0.4f, 0.4f, 0.9f);
                if (sys.ownerCiv == AlienCivManager.PLAYER_CIV_ID)
                    nodeColor = new Color(0.4f, 0.8f, 1f, 1f);
                iconImg.color = nodeColor;

                // 光晕
                var glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(nodeGo.transform, false);
                var glowRect = glowGo.AddComponent<RectTransform>();
                glowRect.anchorMin = new Vector2(0.5f, 0.5f);
                glowRect.anchorMax = new Vector2(0.5f, 0.5f);
                glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.sizeDelta = new Vector2(NODE_SIZE * 2, NODE_SIZE * 2);
                var glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.3f);

                // 名称标签
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(nodeGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.5f, 0);
                nameRect.anchorMax = new Vector2(0.5f, 0);
                nameRect.pivot = new Vector2(0.5f, 1);
                nameRect.sizeDelta = new Vector2(120, 16);
                nameRect.anchoredPosition = new Vector2(0, -4);
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = GetFont();
                nameText.fontSize = 10;
                nameText.color = sys.discovered ? new Color(0.8f, 0.85f, 1f) : new Color(0.5f, 0.5f, 0.5f);
                nameText.alignment = TextAnchor.UpperCenter;
                nameText.text = sys.discovered ? sys.name : "???";

                // 点击事件
                var btn = nodeGo.AddComponent<Button>();
                btn.targetGraphic = iconImg;
                btn.onClick.AddListener(() => SelectSystem(sys.id));

                _systemNodes[sys.id] = nodeGo;
            }
        }

        private void BuildInfoPanel()
        {
            _infoPanel = new GameObject("InfoPanel");
            _infoPanel.transform.SetParent(_window.transform, false);
            var panelRect = _infoPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.sizeDelta = new Vector2(280, 0);
            panelRect.offsetMin = new Vector2(-280, 36);
            panelRect.offsetMax = new Vector2(0, -44);
            var panelImg = _infoPanel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);
            _infoPanel.SetActive(false);

            // 关闭按钮
            var closeBtn = CreateButton(_infoPanel.transform, "×",
                new Color(0.6f, 0.2f, 0.2f, 0.9f), () => { _infoPanel.SetActive(false); _selectedSystem = null; });
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.anchoredPosition = new Vector2(-4, -4);

            // 信息文本
            var textGo = new GameObject("InfoText");
            textGo.transform.SetParent(_infoPanel.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.45f);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 40);
            textRect.offsetMax = new Vector2(-10, -10);
            _infoText = textGo.AddComponent<Text>();
            _infoText.font = GetFont();
            _infoText.fontSize = 12;
            _infoText.color = new Color(0.85f, 0.9f, 1f);
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;

            // 按钮区域
            var btnAreaGo = new GameObject("ButtonArea");
            btnAreaGo.transform.SetParent(_infoPanel.transform, false);
            _buttonArea = btnAreaGo.AddComponent<RectTransform>();
            _buttonArea.anchorMin = Vector2.zero;
            _buttonArea.anchorMax = new Vector2(1, 0.45f);
            _buttonArea.offsetMin = new Vector2(8, 8);
            _buttonArea.offsetMax = new Vector2(-8, -8);
        }

        private void BuildBottomBar()
        {
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(_window.transform, false);
            var bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = Vector2.zero;
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.sizeDelta = new Vector2(0, 36);
            var bottomImg = bottomBar.AddComponent<Image>();
            bottomImg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(bottomBar.transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(10, 0);
            statusRect.offsetMax = new Vector2(-10, 0);
            var statusText = statusGo.AddComponent<Text>();
            statusText.font = GetFont();
            statusText.fontSize = 11;
            statusText.color = new Color(0.6f, 0.7f, 0.85f);
            statusText.alignment = TextAnchor.MiddleLeft;

            int discovered = 0, total = 0;
            if (GalaxyManager.Galaxy != null)
            {
                total = GalaxyManager.Galaxy.systems.Count;
                foreach (var s in GalaxyManager.Galaxy.systems) if (s.discovered) discovered++;
            }
            statusText.text = $"星系: {discovered}/{total}  |  舰队: {FleetManager.Fleets.Count}  |  文明: {GalaxyManager.Galaxy?.civilizations.Count ?? 0}";
        }

        private void SelectSystem(string systemId)
        {
            _selectedSystem = systemId;
            _infoPanel.SetActive(true);

            var sys = GalaxyManager.GetSystem(systemId);
            if (sys == null) return;

            // 发现星系
            if (!sys.discovered)
            {
                GalaxyManager.DiscoverSystem(systemId);
                // 更新节点显示
                if (_systemNodes.TryGetValue(systemId, out var node))
                {
                    var iconImg = node.transform.Find("Icon")?.GetComponent<Image>();
                    if (iconImg != null) iconImg.color = new Color(1f, 0.9f, 0.6f, 1f);
                    var nameText = node.transform.Find("Name")?.GetComponent<Text>();
                    if (nameText != null) nameText.text = sys.name;
                }
            }

            // 显示信息
            string ownerText = string.IsNullOrEmpty(sys.ownerCiv) ?
                LocalizedTextManager.getText("sm_ui_planet_uncolonized") :
                GalaxyManager.GetCivilization(sys.ownerCiv)?.name ?? sys.ownerCiv;

            float distFromHome = GalaxyManager.GetDistance(GalaxyManager.Galaxy.playerHomeSystem, systemId);

            _infoText.text =
                $"<color=#ffd700><size=15><b>{sys.name}</b></size></color>\n" +
                $"<color=#93A4BC>类型:</color> {sys.type}\n" +
                $"<color=#93A4BC>状态:</color> {(sys.visited ? LocalizedTextManager.getText("sm_system_visited") : LocalizedTextManager.getText("sm_system_discovered"))}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_system_owner")}:</color> {ownerText}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_system_threat")}:</color> {new string('!', sys.threatLevel)}\n" +
                $"<color=#93A4BC>距离:</color> {string.Format(LocalizedTextManager.getText("sm_distance_ly"), distFromHome)}\n" +
                $"<color=#93A4BC>星球数:</color> {sys.planetIds.Count}\n" +
                $"<color=#93A4BC>资源加成:</color> x{sys.resourceBonus:F1}\n";

            // 刷新按钮
            RefreshButtons(sys);
        }

        private void RefreshButtons(StarSystemData sys)
        {
            foreach (Transform child in _buttonArea) Destroy(child.gameObject);

            float y = 0;
            const float btnH = 32, gap = 6;

            // 进入星系视图
            CreateActionButton(_buttonArea,
                LocalizedTextManager.getText("sm_system_view"),
                new Color(0.9f, 0.7f, 0.2f, 0.9f),
                () => { Close(); SuperMechStarView.Open(); },
                ref y, btnH, gap);

            // 派遣舰队（如果有舰队）
            var playerFleets = FleetManager.GetFleetsByOwner(AlienCivManager.PLAYER_CIV_ID);
            if (playerFleets.Count > 0 && sys.id != GalaxyManager.Galaxy.playerHomeSystem)
            {
                foreach (var fleet in playerFleets)
                {
                    if (fleet.status == FleetStatus.Idle || fleet.status == FleetStatus.Orbiting)
                    {
                        CreateActionButton(_buttonArea,
                            $"{LocalizedTextManager.getText("sm_send_fleet")}: {fleet.name}",
                            new Color(0.2f, 0.5f, 0.8f, 0.9f),
                            () => FleetManager.SendFleet(fleet.id, sys.id),
                            ref y, btnH, gap);
                    }
                }
            }

            // 建立接触（如果有外星文明且未接触）
            if (!string.IsNullOrEmpty(sys.ownerCiv) && sys.ownerCiv != AlienCivManager.PLAYER_CIV_ID)
            {
                var rel = GalaxyManager.GetRelation(AlienCivManager.PLAYER_CIV_ID, sys.ownerCiv);
                if (rel != null && rel.state == DiplomacyState.Unknown)
                {
                    CreateActionButton(_buttonArea,
                        LocalizedTextManager.getText("sm_civ_contact"),
                        new Color(0.6f, 0.4f, 0.8f, 0.9f),
                        () => {
                            GalaxyManager.EstablishContact(AlienCivManager.PLAYER_CIV_ID, sys.ownerCiv);
                            SelectSystem(sys.id);
                        },
                        ref y, btnH, gap);
                }
            }
        }

        private void ShowFleetPanel()
        {
            // 简单舰队面板：列出所有舰队+建造按钮
            var overlayGo = new GameObject("FleetPanel");
            overlayGo.transform.SetParent(_window.transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() => Destroy(overlayGo));

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 500);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.12f, 0.2f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.fontSize = 16;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_fleet_manager");

            // 舰队列表
            var listGo = new GameObject("List");
            listGo.transform.SetParent(panelGo.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.offsetMin = new Vector2(10, 60);
            listRect.offsetMax = new Vector2(-10, -50);
            var listText = listGo.AddComponent<Text>();
            listText.font = GetFont();
            listText.fontSize = 12;
            listText.color = new Color(0.85f, 0.9f, 1f);
            listText.alignment = TextAnchor.UpperLeft;
            listText.horizontalOverflow = HorizontalWrapMode.Wrap;
            listText.verticalOverflow = VerticalWrapMode.Truncate;

            string fleetInfo = "";
            foreach (var f in FleetManager.Fleets)
            {
                string statusText = f.status switch
                {
                    FleetStatus.Idle => LocalizedTextManager.getText("sm_fleet_idle"),
                    FleetStatus.Traveling => LocalizedTextManager.getText("sm_fleet_traveling"),
                    FleetStatus.Orbiting => LocalizedTextManager.getText("sm_fleet_orbiting"),
                    FleetStatus.Bombarding => LocalizedTextManager.getText("sm_fleet_bombarding"),
                    FleetStatus.Invading => LocalizedTextManager.getText("sm_fleet_invading"),
                    FleetStatus.Retreating => LocalizedTextManager.getText("sm_fleet_retreating"),
                    _ => "?"
                };
                fleetInfo += $"<b>{f.name}</b>\n  状态: {statusText}  舰船: {f.ships.Count}  攻击: {f.GetTotalAttack():F0}\n\n";
            }
            if (string.IsNullOrEmpty(fleetInfo)) fleetInfo = LocalizedTextManager.getText("sm_ui_planet_uncolonized");
            listText.text = fleetInfo;

            // 建造舰队按钮
            float by = 10;
            CreateActionButton(panelGo.transform,
                LocalizedTextManager.getText("sm_build_fleet"),
                new Color(0.3f, 0.6f, 0.3f, 0.9f),
                () => {
                    var newFleet = FleetManager.CreateFleet("第" + (FleetManager.Fleets.Count + 1) + "舰队",
                        AlienCivManager.PLAYER_CIV_ID, GalaxyManager.Galaxy.playerHomeSystem);
                    FleetManager.BuildShip(newFleet, ShipType.Corvette);
                    Destroy(overlayGo);
                    ShowFleetPanel();
                },
                ref by, 36, 6);
        }

        private void ShowDiplomacyPanel()
        {
            var overlayGo = new GameObject("DiplomacyPanel");
            overlayGo.transform.SetParent(_window.transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() => Destroy(overlayGo));

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(450, 550);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.12f, 0.2f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = GetFont();
            titleText.fontSize = 16;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_diplomacy");

            var listGo = new GameObject("CivList");
            listGo.transform.SetParent(panelGo.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.offsetMin = new Vector2(10, 10);
            listRect.offsetMax = new Vector2(-10, -50);
            var listText = listGo.AddComponent<Text>();
            listText.font = GetFont();
            listText.fontSize = 11;
            listText.color = new Color(0.85f, 0.9f, 1f);
            listText.alignment = TextAnchor.UpperLeft;
            listText.horizontalOverflow = HorizontalWrapMode.Wrap;
            listText.verticalOverflow = VerticalWrapMode.Truncate;

            string info = "";
            foreach (var civ in GalaxyManager.Galaxy.civilizations)
            {
                if (civ.isPlayer) continue;
                var rel = GalaxyManager.GetRelation(AlienCivManager.PLAYER_CIV_ID, civ.id);
                string stateText = rel != null ? AlienCivManager.GetDiplomacyStateText(rel.state) : "?";
                string opinionText = rel != null ? $"{rel.opinion:+0;-0;0}" : "?";
                info += $"<b>{civ.name}</b> ({civ.speciesName})\n" +
                        $"  科技: {AlienCivManager.GetTechLevelText(civ.techLevel)}\n" +
                        $"  关系: {stateText} (好感:{opinionText})\n" +
                        $"  国力: {civ.totalPower:F0}  星系: {civ.controlledSystems.Count}\n\n";
            }
            listText.text = info;
        }

        private void Update()
        {
            // 舰队航行更新
            FleetManager.FleetTick(Time.deltaTime);
            // 银河模拟
            GalaxyManager.GalaxyTick(Time.deltaTime);
            // AI外交
            AlienCivManager.AIDiplomacyTick(Time.deltaTime);
        }
    }
}
