using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口 - UI构建部分（BuildTopBar/BuildUI/AddSciFiFrame/CreateCorner/ShowColonyOverview）</summary>
    public partial class SuperMechStarView : MonoBehaviour
    {
        private void BuildTopBar()
        {
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, TopBarHeight);
            topRect.anchoredPosition = Vector2.zero;
            var topImg = topGo.AddComponent<Image>();
            topImg.sprite = SuperMechUiSkin.Section;
            topImg.type = Image.Type.Sliced;
            topImg.color = Color.white;
            topImg.raycastTarget = true;

            // 返回按钮（v0.223：关闭星图并返回主星球）
            var backGo = new GameObject("BackBtn");
            backGo.transform.SetParent(topGo.transform, false);
            var backRect = backGo.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.sizeDelta = new Vector2(120, 34);
            backRect.anchoredPosition = new Vector2(12, 0);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            var backBtn = backGo.AddComponent<Button>();
            backBtn.onClick.AddListener(() =>
            {
                Close();
                SuperMechMultiWorld.SwitchToPlanet(SuperMechStarSystem.MAIN_PLANET);
            });

            var backTextGo = new GameObject("Text");
            backTextGo.transform.SetParent(backGo.transform, false);
            var btRect = backTextGo.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero; btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero; btRect.offsetMax = Vector2.zero;
            var backText = backTextGo.AddComponent<Text>();
            backText.font = SuperMechUiSkin.DefaultFont;
            backText.fontSize = 14;
            backText.color = Color.white;
            backText.alignment = TextAnchor.MiddleCenter;
            backText.text = LocalizedTextManager.getText("sm_ui_back");

            // v0.235 全局经济按钮（伊纳尔经济总览）
            var econBtnGo = new GameObject("EconomyBtn");
            econBtnGo.transform.SetParent(topGo.transform, false);
            var econRect = econBtnGo.AddComponent<RectTransform>();
            econRect.anchorMin = new Vector2(1, 0.5f);
            econRect.anchorMax = new Vector2(1, 0.5f);
            econRect.pivot = new Vector2(1, 0.5f);
            econRect.sizeDelta = new Vector2(80, 34);
            econRect.anchoredPosition = new Vector2(-224, 0);
            var econImg = econBtnGo.AddComponent<Image>();
            econImg.color = new Color(0.3f, 0.5f, 0.35f, 0.9f);
            var econBtn = econBtnGo.AddComponent<Button>();
            econBtn.onClick.AddListener(() => SuperMechWindowManager.OpenEconomy());
            var econTextGo = new GameObject("Text");
            econTextGo.transform.SetParent(econBtnGo.transform, false);
            var econTextRect = econTextGo.AddComponent<RectTransform>();
            econTextRect.anchorMin = Vector2.zero; econTextRect.anchorMax = Vector2.one;
            var econText = econTextGo.AddComponent<Text>();
            econText.font = SuperMechUiSkin.DefaultFont;
            econText.fontSize = 12; econText.color = Color.white;
            econText.alignment = TextAnchor.MiddleCenter;
            econText.text = LocalizedTextManager.getText("sm_ui_action_economy");

            // v0.223 顶部右侧工具按钮：殖民地总览、全局自动运输
            // 殖民地总览按钮
            var colonyBtnGo = new GameObject("ColonyBtn");
            colonyBtnGo.transform.SetParent(topGo.transform, false);
            var colonyRect = colonyBtnGo.AddComponent<RectTransform>();
            colonyRect.anchorMin = new Vector2(1, 0.5f);
            colonyRect.anchorMax = new Vector2(1, 0.5f);
            colonyRect.pivot = new Vector2(1, 0.5f);
            colonyRect.sizeDelta = new Vector2(100, 34);
            colonyRect.anchoredPosition = new Vector2(-116, 0);
            var colonyImg = colonyBtnGo.AddComponent<Image>();
            colonyImg.color = new Color(0.2f, 0.3f, 0.5f, 0.9f);
            var colonyBtn = colonyBtnGo.AddComponent<Button>();
            colonyBtn.onClick.AddListener(ShowColonyOverview);
            var colonyTextGo = new GameObject("Text");
            colonyTextGo.transform.SetParent(colonyBtnGo.transform, false);
            var colonyTextRect = colonyTextGo.AddComponent<RectTransform>();
            colonyTextRect.anchorMin = Vector2.zero; colonyTextRect.anchorMax = Vector2.one;
            var colonyText = colonyTextGo.AddComponent<Text>();
            colonyText.font = SuperMechUiSkin.DefaultFont;
            colonyText.fontSize = 12; colonyText.color = Color.white;
            colonyText.alignment = TextAnchor.MiddleCenter;
            colonyText.text = LocalizedTextManager.getText("sm_ui_action_colony");

            // 全局自动运输按钮
            var autoBtnGo = new GameObject("AutoTransportBtn");
            autoBtnGo.transform.SetParent(topGo.transform, false);
            var autoRect = autoBtnGo.AddComponent<RectTransform>();
            autoRect.anchorMin = new Vector2(1, 0.5f);
            autoRect.anchorMax = new Vector2(1, 0.5f);
            autoRect.pivot = new Vector2(1, 0.5f);
            autoRect.sizeDelta = new Vector2(100, 34);
            autoRect.anchoredPosition = new Vector2(-12, 0);
            var autoImg = autoBtnGo.AddComponent<Image>();
            autoImg.color = new Color(0.2f, 0.4f, 0.3f, 0.9f);
            var autoBtn = autoBtnGo.AddComponent<Button>();
            autoBtn.onClick.AddListener(() =>
            {
                foreach (var p in SuperMechStarSystem.GetAllPlanets())
                {
                    if (p != null && p.colonyLevel > 0) p.autoTransport = !p.autoTransport;
                }
                if (_selectedPlanet != null) SelectPlanet(_selectedPlanet);
            });
            var autoTextGo = new GameObject("Text");
            autoTextGo.transform.SetParent(autoBtnGo.transform, false);
            var autoTextRect = autoTextGo.AddComponent<RectTransform>();
            autoTextRect.anchorMin = Vector2.zero; autoTextRect.anchorMax = Vector2.one;
            var autoText = autoTextGo.AddComponent<Text>();
            autoText.font = SuperMechUiSkin.DefaultFont;
            autoText.fontSize = 12; autoText.color = Color.white;
            autoText.alignment = TextAnchor.MiddleCenter;
            autoText.text = LocalizedTextManager.getText("sm_ui_action_auto_all");

            // v0.229 银河地图按钮（进入多星系视图）
            var galaxyBtnGo = new GameObject("GalaxyBtn");
            galaxyBtnGo.transform.SetParent(topGo.transform, false);
            var galaxyRect = galaxyBtnGo.AddComponent<RectTransform>();
            galaxyRect.anchorMin = new Vector2(1, 0.5f);
            galaxyRect.anchorMax = new Vector2(1, 0.5f);
            galaxyRect.pivot = new Vector2(1, 0.5f);
            galaxyRect.sizeDelta = new Vector2(100, 34);
            galaxyRect.anchoredPosition = new Vector2(-120, 0);
            var galaxyImg = galaxyBtnGo.AddComponent<Image>();
            galaxyImg.color = new Color(0.4f, 0.3f, 0.6f, 0.9f);
            var galaxyBtn = galaxyBtnGo.AddComponent<Button>();
            galaxyBtn.onClick.AddListener(() =>
            {
                Close();
                SuperMechGalaxyView.Open();
            });
            var galaxyTextGo = new GameObject("Text");
            galaxyTextGo.transform.SetParent(galaxyBtnGo.transform, false);
            var galaxyTextRect = galaxyTextGo.AddComponent<RectTransform>();
            galaxyTextRect.anchorMin = Vector2.zero; galaxyTextRect.anchorMax = Vector2.one;
            var galaxyText = galaxyTextGo.AddComponent<Text>();
            galaxyText.font = SuperMechUiSkin.DefaultFont;
            galaxyText.fontSize = 12; galaxyText.color = Color.white;
            galaxyText.alignment = TextAnchor.MiddleCenter;
            galaxyText.text = LocalizedTextManager.getText("sm_galaxy_view");

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero; titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(140, 0); titleRect.offsetMax = new Vector2(-230, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 18;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = LocalizedTextManager.getText("sm_ui_star_map");
        }

        private void BuildUI()
        {
            SuperMechStarSystem.Init();

            // 全屏深空背景
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.005f, 0.01f, 0.03f, 1f);
            bgImg.raycastTarget = true; // 拦截鼠标穿透到游戏

            // 顶部标题栏
            BuildTopBar();

            // v0.223 星图区域（全屏，参考Stellaris/群星：地图优先，详情面板选中后弹出）
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(transform, false);
            var mapRect = mapGo.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = new Vector2(0, -TopBarHeight);
            var mapImg = mapGo.AddComponent<Image>();
            mapImg.color = new Color(0.005f, 0.01f, 0.03f, 1f); // 极深太空黑
            mapImg.raycastTarget = false;

            // 滚动视图
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(mapGo.transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(4, 4); sr.offsetMax = new Vector2(-4, -4);
            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.01f);
            maskImg.raycastTarget = false;
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewportGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _mapContent = contentGo.AddComponent<RectTransform>();
            _mapContent.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContent.pivot = new Vector2(0.5f, 0.5f);
            _mapContent.sizeDelta = new Vector2(MAP_SIZE, MAP_SIZE);
            var contentHit = contentGo.AddComponent<Image>();
            contentHit.color = new Color(0f, 0f, 0f, 0f);
            contentHit.raycastTarget = true;
            // v0.223 点击地图空白处关闭详情面板
            var contentBtn = contentGo.AddComponent<Button>();
            contentBtn.transition = Selectable.Transition.None;
            contentBtn.onClick.AddListener(() =>
            {
                if (_infoPanel != null && _infoPanel.activeSelf)
                {
                    _infoPanel.SetActive(false);
                    _selectedPlanet = null;
                }
            });
            scrollRect.content = _mapContent;
            scrollRect.viewport = vpRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 生成程序化星空背景
            var starfieldResult = StarfieldBackground.Generate(_mapContent, MAP_SIZE);
            _starfieldResult = starfieldResult;
            _twinkleStars.AddRange(starfieldResult.TwinkleStars);
            _twinklePhases.AddRange(starfieldResult.TwinklePhases);

            // 坐标显示（左下角）
            var coordGo = new GameObject("CoordText");
            coordGo.transform.SetParent(mapGo.transform, false);
            var coordRect = coordGo.AddComponent<RectTransform>();
            coordRect.anchorMin = new Vector2(0, 0);
            coordRect.anchorMax = new Vector2(1, 0);
            coordRect.pivot = new Vector2(0.5f, 0);
            coordRect.sizeDelta = new Vector2(0, 20);
            coordRect.anchoredPosition = new Vector2(0, 4);
            _coordText = coordGo.AddComponent<Text>();
            _coordText.font = SuperMechUiSkin.DefaultFont;
            _coordText.fontSize = 10;
            _coordText.color = SuperMechUiSkin.TextDim;
            _coordText.alignment = TextAnchor.LowerCenter;
            _coordText.text = LocalizedTextManager.getText("sm_ui_star_empty");

            // 缩放控制按钮（右下角，手机端用）
            var zoomGo = new GameObject("ZoomControls");
            zoomGo.transform.SetParent(mapGo.transform, false);
            var zoomRect = zoomGo.AddComponent<RectTransform>();
            zoomRect.anchorMin = new Vector2(1, 0);
            zoomRect.anchorMax = new Vector2(1, 0);
            zoomRect.pivot = new Vector2(1, 0);
            zoomRect.sizeDelta = new Vector2(80, 36);
            zoomRect.anchoredPosition = new Vector2(-8, 28);

            // 放大按钮
            var zoomInGo = new GameObject("ZoomIn");
            zoomInGo.transform.SetParent(zoomGo.transform, false);
            var ziRect = zoomInGo.AddComponent<RectTransform>();
            ziRect.anchorMin = new Vector2(0, 0);
            ziRect.anchorMax = new Vector2(0.5f, 1);
            ziRect.offsetMin = Vector2.zero; ziRect.offsetMax = Vector2.zero;
            var ziImg = zoomInGo.AddComponent<Image>();
            ziImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            ziImg.type = Image.Type.Sliced;
            ziImg.color = Color.white;
            var ziBtn = zoomInGo.AddComponent<Button>();
            ziBtn.onClick.AddListener(() => {
                _zoomLevel = Mathf.Clamp(_zoomLevel + 0.2f, MinZoom, MaxZoom);
                if (_mapContent != null) _mapContent.localScale = new Vector3(_zoomLevel, _zoomLevel, 1f);
            });
            var ziTextGo = new GameObject("Text");
            ziTextGo.transform.SetParent(zoomInGo.transform, false);
            var ziTextRect = ziTextGo.AddComponent<RectTransform>();
            ziTextRect.anchorMin = Vector2.zero; ziTextRect.anchorMax = Vector2.one;
            var ziText = ziTextGo.AddComponent<Text>();
            ziText.font = SuperMechUiSkin.DefaultFont;
            ziText.fontSize = 16; ziText.color = Color.white;
            ziText.alignment = TextAnchor.MiddleCenter; ziText.text = "+";

            // 缩小按钮
            var zoomOutGo = new GameObject("ZoomOut");
            zoomOutGo.transform.SetParent(zoomGo.transform, false);
            var zoRect = zoomOutGo.AddComponent<RectTransform>();
            zoRect.anchorMin = new Vector2(0.5f, 0);
            zoRect.anchorMax = new Vector2(1, 1);
            zoRect.offsetMin = Vector2.zero; zoRect.offsetMax = Vector2.zero;
            var zoImg = zoomOutGo.AddComponent<Image>();
            zoImg.sprite = SuperMechUiSkin.ButtonNormalSprite;
            zoImg.type = Image.Type.Sliced;
            zoImg.color = Color.white;
            var zoBtn = zoomOutGo.AddComponent<Button>();
            zoBtn.onClick.AddListener(() => {
                _zoomLevel = Mathf.Clamp(_zoomLevel - 0.2f, MinZoom, MaxZoom);
                if (_mapContent != null) _mapContent.localScale = new Vector3(_zoomLevel, _zoomLevel, 1f);
            });
            var zoTextGo = new GameObject("Text");
            zoTextGo.transform.SetParent(zoomOutGo.transform, false);
            var zoTextRect = zoTextGo.AddComponent<RectTransform>();
            zoTextRect.anchorMin = Vector2.zero; zoTextRect.anchorMax = Vector2.one;
            var zoText = zoTextGo.AddComponent<Text>();
            zoText.font = SuperMechUiSkin.DefaultFont;
            zoText.fontSize = 16; zoText.color = Color.white;
            zoText.alignment = TextAnchor.MiddleCenter; zoText.text = "-";

            // v0.223 右侧详情面板（默认隐藏，选中星球后滑出；参考Stellaris/群星/无尽空间）
            var infoGo = new GameObject("InfoPanel");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(1, 0.5f);
            infoRect.sizeDelta = new Vector2(320, -TopBarHeight);
            infoRect.anchoredPosition = new Vector2(0, -TopBarHeight / 2f);
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.06f, 0.09f, 0.14f, 0.97f);
            infoImg.raycastTarget = true;
            _infoPanel = infoGo;
            infoGo.SetActive(false);  // 默认隐藏

            // 详情面板关闭按钮（右上角）
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(infoGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(28, 28);
            closeRect.anchoredPosition = new Vector2(-6, -6);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => { _infoPanel.SetActive(false); _selectedPlanet = null; });
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero; closeTextRect.anchorMax = Vector2.one;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = SuperMechUiSkin.DefaultFont;
            closeText.fontSize = 16; closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter; closeText.text = "×";

            // === Stellaris式分页标签栏 ===
            var tabBarGo = new GameObject("TabBar");
            tabBarGo.transform.SetParent(infoGo.transform, false);
            var tabBarRect = tabBarGo.AddComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0, 1);
            tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.sizeDelta = new Vector2(0, 28);
            tabBarRect.anchoredPosition = new Vector2(0, -2);
            _tabBar = tabBarGo;
            string[] tabNames = { "sm_ui_tab_overview", "sm_ui_tab_units", "sm_ui_tab_buildings", "sm_ui_tab_events" };
            for (int t = 0; t < 4; t++)
            {
                var tabGo = new GameObject($"Tab_{t}");
                tabGo.transform.SetParent(tabBarGo.transform, false);
                var tabRect = tabGo.AddComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(t * 0.25f, 0);
                tabRect.anchorMax = new Vector2((t + 1) * 0.25f, 1);
                tabRect.offsetMin = new Vector2(2, 0);
                tabRect.offsetMax = new Vector2(-2, 0);
                var tabImg = tabGo.AddComponent<Image>();
                tabImg.color = t == 0 ? new Color(0.2f, 0.3f, 0.5f, 0.9f) : new Color(0.1f, 0.15f, 0.25f, 0.7f);
                var tabBtn = tabGo.AddComponent<Button>();
                int tabIdx = t;
                tabBtn.onClick.AddListener(() =>
                {
                    _currentTab = tabIdx;
                    RefreshTabBar();
                    if (_selectedPlanet != null) { var p = SuperMechStarSystem.GetPlanet(_selectedPlanet); if (p != null) RefreshPlanetInfo(p); }
                });
                var tabTextGo = new GameObject("Text");
                tabTextGo.transform.SetParent(tabGo.transform, false);
                var tabTextRect = tabTextGo.AddComponent<RectTransform>();
                tabTextRect.anchorMin = Vector2.zero;
                tabTextRect.anchorMax = Vector2.one;
                tabTextRect.offsetMin = Vector2.zero;
                tabTextRect.offsetMax = Vector2.zero;
                var tabText = tabTextGo.AddComponent<Text>();
                tabText.font = SuperMechUiSkin.DefaultFont;
                tabText.fontSize = 11;
                tabText.color = t == 0 ? Color.white : new Color(0.7f, 0.75f, 0.85f);
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.text = LocalizedTextManager.getText(tabNames[t]);
                tabText.raycastTarget = false;
            }

            var infoTextGo = new GameObject("InfoText");
            infoTextGo.transform.SetParent(infoGo.transform, false);
            var itRect = infoTextGo.AddComponent<RectTransform>();
            itRect.anchorMin = new Vector2(0, 0.45f); itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(8, -30); itRect.offsetMax = new Vector2(-8, -8);
            _infoText = infoTextGo.AddComponent<Text>();
            _infoText.font = SuperMechUiSkin.DefaultFont;
            _infoText.fontSize = 12;
            _infoText.color = SuperMechUiSkin.TextColor;
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;
            _infoText.text = LocalizedTextManager.getText("sm_ui_select_planet");

            // 交互按钮区域（下半部分30%）
            var btnAreaGo = new GameObject("ButtonArea");
            btnAreaGo.transform.SetParent(infoGo.transform, false);
            _buttonArea = btnAreaGo.AddComponent<RectTransform>();
            _buttonArea.anchorMin = Vector2.zero;
            _buttonArea.anchorMax = new Vector2(1, 0.45f);
            _buttonArea.offsetMin = new Vector2(8, 8);
            _buttonArea.offsetMax = new Vector2(-8, 8);

            RefreshMap();

            // 初始滚动到中心（主星球位置）
            scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);

            // 悬停提示Tooltip（最上层，默认隐藏）
            _tooltip = new GameObject("Tooltip");
            _tooltip.transform.SetParent(transform, false);
            _tooltip.transform.SetAsLastSibling();
            var ttRect = _tooltip.AddComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0, 1);
            ttRect.anchorMax = new Vector2(0, 1);
            ttRect.pivot = new Vector2(0, 1);
            ttRect.sizeDelta = new Vector2(200, 60);
            var ttImg = _tooltip.AddComponent<Image>();
            ttImg.sprite = SuperMechUiSkin.Section;
            ttImg.type = Image.Type.Sliced;
            ttImg.color = Color.white;
            ttImg.raycastTarget = false;
            var ttTextGo = new GameObject("Text");
            ttTextGo.transform.SetParent(_tooltip.transform, false);
            var ttTextRect = ttTextGo.AddComponent<RectTransform>();
            ttTextRect.anchorMin = Vector2.zero;
            ttTextRect.anchorMax = Vector2.one;
            ttTextRect.offsetMin = new Vector2(6, 4);
            ttTextRect.offsetMax = new Vector2(-6, -4);
            _tooltipText = ttTextGo.AddComponent<Text>();
            _tooltipText.font = SuperMechUiSkin.DefaultFont;
            _tooltipText.fontSize = 11;
            _tooltipText.color = new Color(0.85f, 0.9f, 1f);
            _tooltipText.alignment = TextAnchor.UpperLeft;
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow = VerticalWrapMode.Truncate;
            _tooltip.SetActive(false);

            // v0.223 移除底部行动栏（三个大彩色按钮不符合主流星图布局）
            // 功能整合：返回→顶部返回按钮，殖民地总览/自动运输→顶部右侧小按钮

            // 科幻风格外框装饰（最上层）
            AddSciFiFrame();

            // v0.231 宇宙深化UI面板（探索/贸易/太空站/科技树）
            var expansionGo = new GameObject("SpaceExpansionUI");
            expansionGo.transform.SetParent(transform, false);
            expansionGo.transform.SetAsLastSibling();
            expansionGo.AddComponent<SuperMechSpaceExpansionUI>();
        }

        /// <summary>殖民地总览弹窗</summary>
        private void ShowColonyOverview()
        {
            var overlayGo = new GameObject("ColonyOverview");
            overlayGo.transform.SetParent(transform, false);
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
            panelRect.sizeDelta = new Vector2(400, 350);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 14;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_colony_overview");

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(panelGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -40);
            var contentText = contentGo.AddComponent<Text>();
            contentText.font = SuperMechUiSkin.DefaultFont;
            contentText.fontSize = 11;
            contentText.color = new Color(0.8f, 0.9f, 1f);
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Truncate;

            var sb = new System.Text.StringBuilder();
            float totalRes = 0, totalOutput = 0;
            int totalColonies = 0, totalUnits = 0;
            foreach (var p in SuperMechStarSystem.GetAllPlanets())
            {
                if (p == null || p.colonyLevel == 0 || p.id == SuperMechStarSystem.MAIN_PLANET) continue;
                totalColonies++;
                totalRes += p.resource;
                totalOutput += ColonyManager.CalculateResourceOutput(p);
                totalUnits += p.units != null ? p.units.Count : 0;
                string auto = p.autoTransport ? "<color=#66ff66>自动</color>" : "<color=#999999>手动</color>";
                sb.Append($"{p.name} Lv{p.colonyLevel} | 资源:{p.resource:F0} | 单位:{p.units.Count} | {auto}\n");
            }
            contentText.text = $"殖民地总数: {totalColonies} | 总资源: {totalRes:F0} | 总产出: +{totalOutput:F1}/s | 驻留单位: {totalUnits}\n\n" + sb.ToString();
        }

        /// <summary>添加科幻风格外框：角落L形边框+顶部/底部渐变条</summary>
        private void AddSciFiFrame()
        {
            var frameGo = new GameObject("SciFiFrame");
            frameGo.transform.SetParent(transform, false);
            frameGo.transform.SetAsLastSibling(); // 最上层
            var frameRect = frameGo.AddComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;

            Color frameColor = new Color(0.3f, 0.6f, 0.9f, 0.8f); // 科技蓝
            int cornerLen = 24; // 角落L形长度
            int cornerThick = 2; // 角落边框粗细

            // 四个角落L形边框
            CreateCorner(frameGo.transform, new Vector2(0, 1), new Vector2(1, 1), cornerLen, cornerThick, frameColor); // 左上
            CreateCorner(frameGo.transform, new Vector2(1, 1), new Vector2(0, 1), cornerLen, cornerThick, frameColor); // 右上
            CreateCorner(frameGo.transform, new Vector2(0, 0), new Vector2(1, 0), cornerLen, cornerThick, frameColor); // 左下
            CreateCorner(frameGo.transform, new Vector2(1, 0), new Vector2(0, 0), cornerLen, cornerThick, frameColor); // 右下

            // 顶部渐变条
            var topBarGo = new GameObject("TopGlow");
            topBarGo.transform.SetParent(frameGo.transform, false);
            var topBarRect = topBarGo.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 3);
            topBarRect.anchoredPosition = Vector2.zero;
            var topBarImg = topBarGo.AddComponent<Image>();
            topBarImg.color = new Color(0.3f, 0.6f, 0.9f, 0.6f);
            topBarImg.raycastTarget = false;

            // 底部渐变条
            var botBarGo = new GameObject("BottomGlow");
            botBarGo.transform.SetParent(frameGo.transform, false);
            var botBarRect = botBarGo.AddComponent<RectTransform>();
            botBarRect.anchorMin = new Vector2(0, 0);
            botBarRect.anchorMax = new Vector2(1, 0);
            botBarRect.pivot = new Vector2(0.5f, 0);
            botBarRect.sizeDelta = new Vector2(0, 3);
            botBarRect.anchoredPosition = Vector2.zero;
            var botBarImg = botBarGo.AddComponent<Image>();
            botBarImg.color = new Color(0.3f, 0.6f, 0.9f, 0.6f);
            botBarImg.raycastTarget = false;
        }

        /// <summary>创建单个角落L形边框</summary>
        private void CreateCorner(Transform parent, Vector2 anchor, Vector2 pivot, int len, int thick, Color color)
        {
            // 水平段
            var hGo = new GameObject("CornerH");
            hGo.transform.SetParent(parent, false);
            var hRect = hGo.AddComponent<RectTransform>();
            hRect.anchorMin = anchor;
            hRect.anchorMax = anchor;
            hRect.pivot = pivot;
            hRect.sizeDelta = new Vector2(len, thick);
            hRect.anchoredPosition = Vector2.zero;
            var hImg = hGo.AddComponent<Image>();
            hImg.color = color;
            hImg.raycastTarget = false;

            // 垂直段
            var vGo = new GameObject("CornerV");
            vGo.transform.SetParent(parent, false);
            var vRect = vGo.AddComponent<RectTransform>();
            vRect.anchorMin = anchor;
            vRect.anchorMax = anchor;
            vRect.pivot = pivot;
            vRect.sizeDelta = new Vector2(thick, len);
            vRect.anchoredPosition = Vector2.zero;
            var vImg = vGo.AddComponent<Image>();
            vImg.color = color;
            vImg.raycastTarget = false;
        }
    }
}
