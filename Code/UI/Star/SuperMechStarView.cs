using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口：无限星海，全屏模式，程序化星空背景+圆形星球+光晕</summary>
    public class SuperMechStarView : MonoBehaviour
    {
        private static SuperMechStarView _instance;
        public static bool IsOpen => _instance != null;

        private RectTransform _mapContent;
        private Text _infoText;
        private Text _coordText;
        private RectTransform _buttonArea; // 交互按钮区域
        private GameObject _infoPanel; // v0.223 右侧详情面板（选中星球后才显示）
        private string _selectedPlanet;
        private GameObject _tooltip; // 悬停提示
        private Text _tooltipText;
        private float _zoomLevel = 1f; // 当前缩放级别
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 5f;
        private readonly Dictionary<string, GameObject> _planetNodes = new Dictionary<string, GameObject>();
        private const float MAP_SIZE = 2400f;
        private const float VIEW_RANGE = 1200f;
        private const int STAR_COUNT = 1500; // 星空背景星星数量（提升宇宙密度感）
        private const int TopBarHeight = 50;
        private readonly System.Collections.Generic.List<Image> _twinkleStars = new System.Collections.Generic.List<Image>();
        private readonly System.Collections.Generic.List<float> _twinklePhases = new System.Collections.Generic.List<float>();
        private float _twinkleTimer;
        private float _twinkleUpdateTimer;
        // v0.204 2.5D视差滚动：星空背景各层引用
        private StarfieldBackground.Result _starfieldResult;
        // v0.204 2.5D星球立体结构：轨道卫星数据（行星ID -> 卫星列表）
        private readonly Dictionary<string, List<MoonData>> _planetMoons = new Dictionary<string, List<MoonData>>();
        // v0.204 恒星脉动：恒星ID -> 光晕Image
        private readonly Dictionary<string, Image> _starCoronas = new Dictionary<string, Image>();
        private float _stellarPulseTimer;
        // v0.204 黑洞吸积盘：黑洞ID -> 吸积盘容器
        private readonly Dictionary<string, RectTransform> _blackHoleDisks = new Dictionary<string, RectTransform>();
        private float _blackHoleRotationTimer;

        private class MoonData
        {
            public RectTransform rect;
            public float orbitRadius;
            public float orbitSpeed;
            public float orbitAngle;
        }

        // v0.189+ 分页标签栏
        private GameObject _tabBar;
        private int _currentTab = 0;
        // v0.188+ 事件弹窗
        private GameObject _eventPopup;
        // v0.188+ 派遣单位
        private PlanetData _pendingSendPlanet;
        private GameObject _unitSelectPanel;
        private PlanetData _managePlanet;

        /// <summary>切换星图显示（全屏模式）</summary>
        public static void Toggle()
        {
            if (_instance != null) _instance.Close();
            else Open();
        }

        /// <summary>打开全屏星图</summary>
        public static void Open()
        {
            if (_instance != null) return;
            var canvas = SuperMechUguiWindow.GetCanvas();
            var go = new GameObject("SMStarMapView");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _instance = go.AddComponent<SuperMechStarView>();
            try
            {
                _instance.BuildUI();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 星图窗口构建失败: " + e);
                _instance.Close();
                _instance = null;
            }
        }

        /// <summary>关闭星图</summary>
        public void Close()
        {
            if (_instance == this) _instance = null;
            Destroy(gameObject);
        }

        /// <summary>每帧更新：处理滚轮缩放、航行单位位置刷新、星星闪烁、手机返回键</summary>
        private void Update()
        {
            // 手机返回键退出全屏星图
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[超神机械师] 手机返回键关闭星图");
                Close();
                return;
            }

            // v0.204 2.5D视差滚动：根据地图内容偏移量移动各层星空
            if (_starfieldResult != null && _mapContent != null)
            {
                StarfieldBackground.ApplyParallax(_starfieldResult, -_mapContent.anchoredPosition);
            }

            // 悬停提示跟随鼠标
            if (_tooltip != null && _tooltip.activeSelf)
            {
                UpdateTooltipPosition();
            }

            // 滚轮缩放（PC）
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _zoomLevel = Mathf.Clamp(_zoomLevel + scroll * 0.3f, MinZoom, MaxZoom);
                if (_mapContent != null)
                {
                    _mapContent.localScale = new Vector3(_zoomLevel, _zoomLevel, 1f);
                }
            }

            // 双指缩放（手机触屏）
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                // 计算两指当前距离和上一帧距离
                Vector2 curDist = touch0.position - touch1.position;
                Vector2 prevDist = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
                float delta = curDist.magnitude - prevDist.magnitude;

                if (Mathf.Abs(delta) > 0.5f)
                {
                    _zoomLevel = Mathf.Clamp(_zoomLevel + delta * 0.005f, MinZoom, MaxZoom);
                    if (_mapContent != null)
                    {
                        _mapContent.localScale = new Vector3(_zoomLevel, _zoomLevel, 1f);
                    }
                }
            }

            // 星星闪烁动画（每0.1秒更新一次，避免每帧遍历300+Image产生开销）
            _twinkleTimer += Time.deltaTime;
            _twinkleUpdateTimer += Time.deltaTime;
            if (_twinkleUpdateTimer >= 0.1f)
            {
                _twinkleUpdateTimer = 0f;
                for (int i = 0; i < _twinkleStars.Count; i++)
                {
                    if (_twinkleStars[i] == null) continue;
                    float baseAlpha = _twinkleStars[i].color.a;
                    float twinkle = Mathf.Sin(_twinkleTimer * 2f + _twinklePhases[i]) * 0.3f + 0.7f;
                    var c = _twinkleStars[i].color;
                    c.a = Mathf.Clamp01(baseAlpha * twinkle);
                    _twinkleStars[i].color = c;
                }
            }

            // v0.204 轨道卫星旋转（每帧更新，流畅动画）
            foreach (var kv in _planetMoons)
            {
                if (kv.Value == null) continue;
                foreach (var moon in kv.Value)
                {
                    if (moon.rect == null) continue;
                    moon.orbitAngle += moon.orbitSpeed * Time.deltaTime;
                    float mx = Mathf.Cos(moon.orbitAngle) * moon.orbitRadius;
                    float my = Mathf.Sin(moon.orbitAngle) * moon.orbitRadius * 0.4f; // 椭圆轨道，模拟2.5D透视
                    moon.rect.anchoredPosition = new Vector2(mx, my);
                }
            }

            // v0.204 恒星脉动（呼吸般的亮度变化）
            _stellarPulseTimer += Time.deltaTime;
            foreach (var kv in _starCoronas)
            {
                if (kv.Value == null) continue;
                float pulse = Mathf.Sin(_stellarPulseTimer * 1.5f + kv.Key.GetHashCode() % 10) * 0.15f + 0.85f;
                var c = kv.Value.color;
                c.a = 0.25f * pulse;
                kv.Value.color = c;
            }

            // v0.204 黑洞吸积盘旋转
            _blackHoleRotationTimer += Time.deltaTime;
            foreach (var kv in _blackHoleDisks)
            {
                if (kv.Value == null) continue;
                // 内层快、外层慢，差速旋转模拟真实吸积盘
                for (int i = 0; i < kv.Value.childCount; i++)
                {
                    var child = kv.Value.GetChild(i);
                    if (child == null) continue;
                    float speed = 60f - i * 15f; // 内层60度/秒，外层30度/秒
                    child.Rotate(0, 0, speed * Time.deltaTime);
                }
            }

            // 每秒刷新一次航行单位位置和行星公转
            _voyageRefreshTimer += Time.deltaTime;
            if (_voyageRefreshTimer >= 1f)
            {
                _voyageRefreshTimer = 0f;
                RefreshMap();
            }
        }

        private float _voyageRefreshTimer;





        /// <summary>构建顶部标题栏（返回按钮+标题）</summary>
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
            backText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backText.fontSize = 14;
            backText.color = Color.white;
            backText.alignment = TextAnchor.MiddleCenter;
            backText.text = LocalizedTextManager.getText("sm_ui_back");

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
            colonyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            autoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            autoText.fontSize = 12; autoText.color = Color.white;
            autoText.alignment = TextAnchor.MiddleCenter;
            autoText.text = LocalizedTextManager.getText("sm_ui_action_auto_all");

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero; titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(140, 0); titleRect.offsetMax = new Vector2(-230, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            _coordText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            ziText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            zoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                tabText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            _tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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



        /// <summary>刷新星图，绘制视野范围内的星球节点和航行单位</summary>
        public void RefreshMap()
        {
            if (_mapContent == null) return;
            // 只销毁星球节点、连线、航行单位、轨道线、星团标记，保留星空背景
            foreach (Transform child in _mapContent)
            {
                if (child.name.StartsWith("Planet_") || child.name.StartsWith("Line_") ||
                    child.name.StartsWith("Voyage_") || child.name.StartsWith("Orbit_") ||
                    child.name.StartsWith("Cluster_"))
                    Destroy(child.gameObject);
            }
            _planetNodes.Clear();
            RefreshVoyageShips();

            var planets = SuperMechStarSystem.GetPlanetsInRange(0, 0, VIEW_RANGE);

            // 绘制星团标记（疏散星团=蓝色虚线圈，球状星团=橙色虚线圈）
            var clusters = SuperMechStarSystem.GetClustersInRange(0, 0, VIEW_RANGE + 300);
            foreach (var c in clusters)
            {
                CreateClusterMarker(c);
            }

            // 绘制轨道线（行星围绕恒星的轨道）
            foreach (var p in planets)
            {
                if (p.parentStar == null || p.type == PlanetType.Star) continue;
                var star = SuperMechStarSystem.GetPlanet(p.parentStar);
                if (star == null) continue;
                CreateOrbitLine(star.x, -star.y, p.orbitRadius);
            }

            // 绘制连线（主星球到已发现的其他星球）
            var main = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (main != null)
            {
                foreach (var p in planets)
                {
                    if (p.id == SuperMechStarSystem.MAIN_PLANET || !p.discovered) continue;
                    CreateLine(main.x, -main.y, p.x, -p.y);
                }
            }

            // 绘制星球节点（使用实时公转位置）
            float time = Time.time;
            foreach (var p in planets)
            {
                var pos = SuperMechStarSystem.GetPlanetPosition(p, time);
                CreatePlanetNode(p, pos.x, -pos.y);
            }

            // 绘制航行中的单位（飞船）
            var voyages = SuperMechStarSystem.GetVoyages();
            foreach (var v in voyages)
            {
                var from = SuperMechStarSystem.GetPlanet(v.fromPlanet);
                var to = SuperMechStarSystem.GetPlanet(v.toPlanet);
                if (from == null || to == null) continue;
                // 线性插值计算当前位置
                float vx = from.x + (to.x - from.x) * v.progress;
                float vy = -(from.y + (to.y - from.y) * v.progress);
                CreateVoyageNode(v, vx, vy);
            }

            if (_coordText != null)
            {
                int discovered = SuperMechStarSystem.GetDiscoveredPlanets().Count;
                int total = planets.Count;
                int voyageCount = voyages.Count;
                _coordText.text = string.Format(LocalizedTextManager.getText("sm_ui_star_footer"), discovered, total, voyageCount);
            }
        }

        /// <summary>创建航行中的单位节点（飞船图标）</summary>
        private void CreateVoyageNode(VoyageData v, float x, float y)
        {
            var nodeGo = new GameObject($"Voyage_{v.snapshot.id}");
            nodeGo.transform.SetParent(_mapContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.sizeDelta = new Vector2(20, 20);
            nodeRect.anchoredPosition = new Vector2(x, y);

            // 飞船图标（三角形，用Image+颜色）
            var shipGo = new GameObject("Ship");
            shipGo.transform.SetParent(nodeGo.transform, false);
            var shipRect = shipGo.AddComponent<RectTransform>();
            shipRect.anchorMin = Vector2.zero; shipRect.anchorMax = Vector2.one;
            shipRect.offsetMin = Vector2.zero; shipRect.offsetMax = Vector2.zero;
            var shipImg = shipGo.AddComponent<Image>();
            shipImg.sprite = PlanetSpriteGenerator.GetGlowSprite(); // 用圆形光晕代替飞船图标
            shipImg.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            shipImg.raycastTarget = false;

            // 单位名称标签
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(nodeGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0);
            labelRect.anchorMax = new Vector2(0.5f, 0);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.sizeDelta = new Vector2(100, 14);
            labelRect.anchoredPosition = new Vector2(0, -2);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 9;
            labelText.color = new Color(0.6f, 0.9f, 1f);
            labelText.alignment = TextAnchor.UpperCenter;
            labelText.text = $"{v.snapshot.name} ({(int)(v.progress * 100)}%)";
            labelText.raycastTarget = false;

            // 航行单位点击：显示航行详情
            var voyageBtn = nodeGo.AddComponent<Button>();
            var voyageHit = nodeGo.AddComponent<Image>();
            voyageHit.color = new Color(0f, 0f, 0f, 0f);
            string voyageId = v.snapshot.id.ToString();
            voyageBtn.onClick.AddListener(() => SelectVoyage(voyageId));

            // 悬停提示
            var vTrigger = nodeGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            string vTooltip = $"<b>{v.snapshot.name}</b>\n" +
                $"<color=#88ccff>{LocalizedTextManager.getText("sm_star_voyaging")}</color>\n" +
                $"{LocalizedTextManager.getText("sm_star_progress")}: {(int)(v.progress * 100)}%\n" +
                $"{LocalizedTextManager.getText("sm_star_destination")}: {SuperMechStarSystem.GetPlanet(v.toPlanet)?.name ?? "?"}";
            var vEnter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            vEnter.callback.AddListener((data) => ShowTooltip(vTooltip));
            vTrigger.triggers.Add(vEnter);
            var vExit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            vExit.callback.AddListener((data) => HideTooltip());
            vTrigger.triggers.Add(vExit);
        }

        /// <summary>选中航行单位，显示航行详情</summary>
        private void SelectVoyage(string voyageId)
        {
            long uid;
            if (!long.TryParse(voyageId, out uid)) return;
            var voyages = SuperMechStarSystem.GetVoyages();
            foreach (var v in voyages)
            {
                if (v.snapshot.id != uid) continue;
                var from = SuperMechStarSystem.GetPlanet(v.fromPlanet);
                var to = SuperMechStarSystem.GetPlanet(v.toPlanet);
                _infoText.text =
                    $"<color=#ffd700><size=14><b>{v.snapshot.name}</b></size></color>\n" +
                    $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_voyage_status")}:</color> <color=#66ccff>{LocalizedTextManager.getText("sm_ui_voyage_flying")}</color>\n" +
                    $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_voyage_from")}:</color> {from?.name ?? "?"}\n" +
                    $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_voyage_to")}:</color> {to?.name ?? "?"}\n" +
                    $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_voyage_progress")}:</color> {(int)(v.progress * 100)}%\n" +
                    $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_voyage_eta")}:</color> {(int)((1f - v.progress) * v.totalTicks)} {LocalizedTextManager.getText("sm_ui_tick")}\n";

                // 清空按钮区域
                if (_buttonArea != null)
                {
                    foreach (Transform child in _buttonArea) Destroy(child.gameObject);
                }
                return;
            }
        }

        private void CreatePlanetNode(PlanetData p, float x, float y)
        {
            var nodeGo = new GameObject($"Planet_{p.id}");
            nodeGo.transform.SetParent(_mapContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            bool isMain = p.id == SuperMechStarSystem.MAIN_PLANET;
            bool isStar = p.type == PlanetType.Star;

            // 尺寸：2D分级制（用颜色和光晕区分天体，大小只做简单分级）
            float size;
            if (isStar)
            {
                // 恒星按类型分4级：矮星/巨星/超巨星/残骸
                size = p.starType switch
                {
                    StarType.RedDwarf => 28,        // 红矮星：小
                    StarType.YellowDwarf => 36,     // 黄矮星：中
                    StarType.WhiteDwarf => 24,      // 白矮星：小（残骸）
                    StarType.NeutronStar => 22,     // 中子星：极小（残骸）
                    StarType.BlackHole => 32,       // 黑洞：中（视觉放大）
                    StarType.RedGiant => 56,        // 红巨星：大
                    StarType.BlueGiant => 48,       // 蓝巨星：大
                    _ => 36
                };
            }
            else if (isMain) size = 44;
            else if (p.discovered)
            {
                // 行星按类型分2级：类地行星小，气态巨行星大
                size = p.type == PlanetType.Gas ? 32 : 24;
            }
            else size = 20;
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = new Vector2(x, y);

            // 星球光晕（2D风格：恒星有明显光晕，行星只有殖民地光晕）
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(nodeGo.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
            float glowExpand;
            if (isStar)
            {
                // 恒星光晕按类型分级（巨星光晕大，矮星光晕小）
                glowExpand = p.starType switch
                {
                    StarType.RedDwarf => 12,
                    StarType.YellowDwarf => 20,
                    StarType.BlueGiant => 40,
                    StarType.RedGiant => 50,
                    StarType.WhiteDwarf => 10,
                    StarType.NeutronStar => 8,
                    StarType.BlackHole => 35, // 吸积盘
                    _ => 20
                };
            }
            else glowExpand = isMain ? 18 : (p.colonyLevel > 0 ? 12 : 6);
            glowRect.offsetMin = new Vector2(-glowExpand, -glowExpand);
            glowRect.offsetMax = new Vector2(glowExpand, glowExpand);
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.raycastTarget = false;
            glowImg.sprite = PlanetSpriteGenerator.GetGlowSprite();

            // 星球本体（圆形纹理）
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(nodeGo.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = Vector2.zero; bodyRect.offsetMax = Vector2.zero;
            var bodyImg = bodyGo.AddComponent<Image>();
            bodyImg.raycastTarget = false;

            // 未发现的星球显示为灰色问号
            bool isUnknown = !p.discovered && !isMain && !isStar;
            Color planetColor;
            int planetSeed = p.id.GetHashCode();

            if (isStar)
            {
                // 恒星根据光谱类型显示不同颜色
                planetColor = p.starType switch
                {
                    StarType.RedDwarf => new Color(1f, 0.45f, 0.25f),    // 红矮星：橙红
                    StarType.YellowDwarf => new Color(1f, 0.9f, 0.6f),   // 黄矮星：金黄（太阳色）
                    StarType.BlueGiant => new Color(0.5f, 0.75f, 1f),    // 蓝巨星：蓝白
                    StarType.RedGiant => new Color(1f, 0.35f, 0.2f),     // 红巨星：深红
                    StarType.WhiteDwarf => new Color(0.9f, 0.95f, 1f),   // 白矮星：纯白
                    StarType.NeutronStar => new Color(0.7f, 0.85f, 1f),  // 中子星：淡蓝
                    StarType.BlackHole => new Color(0.08f, 0.08f, 0.12f), // 黑洞：深黑
                    _ => new Color(1f, 0.9f, 0.6f)
                };
                // 黑洞有吸积盘（特殊光晕）
                if (p.starType == StarType.BlackHole)
                {
                    glowImg.color = new Color(0.8f, 0.6f, 0.2f, 0.5f); // 吸积盘橙黄色
                    // v0.204 旋转吸积盘：多层同心环，倾斜椭圆模拟2.5D透视
                    var diskGo = new GameObject("AccretionDisk");
                    diskGo.transform.SetParent(nodeGo.transform, false);
                    diskGo.transform.SetSiblingIndex(1);
                    var diskRect = diskGo.AddComponent<RectTransform>();
                    diskRect.anchorMin = Vector2.zero; diskRect.anchorMax = Vector2.one;
                    diskRect.offsetMin = Vector2.zero; diskRect.offsetMax = Vector2.zero;
                    // 三层吸积盘：内蓝、中橙、外红
                    Color[] diskColors = {
                        new Color(0.4f, 0.7f, 1f, 0.6f),
                        new Color(1f, 0.7f, 0.3f, 0.5f),
                        new Color(1f, 0.4f, 0.2f, 0.4f)
                    };
                    float[] diskSizes = { 1.3f, 1.7f, 2.1f };
                    for (int d = 0; d < 3; d++)
                    {
                        var ringGo = new GameObject($"DiskRing_{d}");
                        ringGo.transform.SetParent(diskRect, false);
                        var ringRect = ringGo.AddComponent<RectTransform>();
                        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
                        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
                        ringRect.pivot = new Vector2(0.5f, 0.5f);
                        ringRect.sizeDelta = new Vector2(size * diskSizes[d], size * diskSizes[d] * 0.35f); // 椭圆环
                        var ringImg = ringGo.AddComponent<Image>();
                        ringImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
                        ringImg.color = diskColors[d];
                        ringImg.raycastTarget = false;
                    }
                    _blackHoleDisks[p.id] = diskRect;
                }
                else
                {
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.7f);
                }
                bodyImg.sprite = PlanetSpriteGenerator.GetPlanetSprite(planetColor, planetSeed);
            }
            else if (isUnknown)
            {
                planetColor = new Color(0.35f, 0.35f, 0.4f, 0.6f);
                glowImg.color = new Color(0.4f, 0.4f, 0.45f, 0.2f);
                bodyImg.sprite = PlanetSpriteGenerator.GetPlanetSprite(planetColor, planetSeed, true);
            }
            else
            {
                planetColor = p.type switch
                {
                    PlanetType.Rocky => new Color(0.75f, 0.65f, 0.5f),
                    PlanetType.Gas => new Color(0.95f, 0.85f, 0.5f),
                    PlanetType.Ice => new Color(0.7f, 0.9f, 1f),
                    PlanetType.Lava => new Color(1f, 0.5f, 0.3f),
                    PlanetType.Ocean => new Color(0.4f, 0.6f, 1f),
                    PlanetType.Desert => new Color(0.95f, 0.85f, 0.55f),
                    PlanetType.Forest => new Color(0.4f, 0.8f, 0.5f),
                    _ => Color.gray
                };
                if (p.colonyLevel > 0)
                {
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.5f);
                }
                else
                {
                    planetColor = new Color(planetColor.r * 0.75f, planetColor.g * 0.75f, planetColor.b * 0.75f, 0.9f);
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.2f);
                }
                if (isMain)
                {
                    glowImg.color = new Color(1f, 0.9f, 0.5f, 0.6f);
                    planetColor = new Color(1f, 0.9f, 0.55f);
                }
                bodyImg.sprite = PlanetSpriteGenerator.GetPlanetSprite(planetColor, planetSeed, false, p.type);
            }

            // 行星环（气态行星概率有环）
            if (p.hasRing && !isStar && !isUnknown)
            {
                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(nodeGo.transform, false);
                var ringRect = ringGo.AddComponent<RectTransform>();
                ringRect.anchorMin = Vector2.zero; ringRect.anchorMax = Vector2.one;
                ringRect.offsetMin = new Vector2(-size * 0.4f, -size * 0.15f);
                ringRect.offsetMax = new Vector2(size * 0.4f, size * 0.15f);
                var ringImg = ringGo.AddComponent<Image>();
                ringImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
                ringImg.color = new Color(planetColor.r * 0.8f, planetColor.g * 0.8f, planetColor.b * 0.8f, 0.4f);
                ringImg.raycastTarget = false;
            }

            // 行星大气层（有大气的行星显示淡蓝色光晕边缘）
            if (!isStar && !isUnknown && p.atmosphere > 0.3f)
            {
                var atmoGo = new GameObject("Atmosphere");
                atmoGo.transform.SetParent(nodeGo.transform, false);
                atmoGo.transform.SetSiblingIndex(1); // 在光晕之后、本体之前
                var atmoRect = atmoGo.AddComponent<RectTransform>();
                atmoRect.anchorMin = Vector2.zero; atmoRect.anchorMax = Vector2.one;
                float atmoExpand = size * 0.15f * p.atmosphere;
                atmoRect.offsetMin = new Vector2(-atmoExpand, -atmoExpand);
                atmoRect.offsetMax = new Vector2(atmoExpand, atmoExpand);
                var atmoImg = atmoGo.AddComponent<Image>();
                atmoImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
                atmoImg.raycastTarget = false;
                // 大气层颜色：根据行星类型，蓝色为主
                Color atmoColor = p.type switch
                {
                    PlanetType.Ocean => new Color(0.4f, 0.6f, 1f, 0.35f),
                    PlanetType.Forest => new Color(0.5f, 0.8f, 0.6f, 0.3f),
                    PlanetType.Ice => new Color(0.7f, 0.9f, 1f, 0.35f),
                    PlanetType.Gas => new Color(0.9f, 0.8f, 0.6f, 0.3f),
                    PlanetType.Lava => new Color(1f, 0.5f, 0.3f, 0.25f),
                    _ => new Color(0.6f, 0.75f, 1f, 0.25f)
                };
                atmoImg.color = atmoColor;
            }

            // 恒星日冕（外层大气光晕，脉动效果）
            Image coronaImg = null;
            if (isStar && p.starType != StarType.BlackHole)
            {
                var coronaGo = new GameObject("Corona");
                coronaGo.transform.SetParent(nodeGo.transform, false);
                coronaGo.transform.SetSiblingIndex(0); // 最底层
                var coronaRect = coronaGo.AddComponent<RectTransform>();
                coronaRect.anchorMin = Vector2.zero; coronaRect.anchorMax = Vector2.one;
                float coronaExpand = size * 0.8f;
                coronaRect.offsetMin = new Vector2(-coronaExpand, -coronaExpand);
                coronaRect.offsetMax = new Vector2(coronaExpand, coronaExpand);
                coronaImg = coronaGo.AddComponent<Image>();
                coronaImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
                coronaImg.raycastTarget = false;
                coronaImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.25f);
                // v0.204 保存恒星日冕引用，用于脉动效果
                _starCoronas[p.id] = coronaImg;
            }

            // v0.204 轨道卫星（类地行星和气态巨行星有1-3个卫星）
            if (!isStar && !isUnknown && p.type != PlanetType.Lava)
            {
                int moonCount = p.type == PlanetType.Gas ? 2 + (planetSeed % 2) : 1 + (planetSeed % 2);
                var moons = new List<MoonData>();
                for (int m = 0; m < moonCount; m++)
                {
                    var moonGo = new GameObject($"Moon_{m}");
                    moonGo.transform.SetParent(nodeGo.transform, false);
                    var moonRect = moonGo.AddComponent<RectTransform>();
                    moonRect.anchorMin = new Vector2(0.5f, 0.5f);
                    moonRect.anchorMax = new Vector2(0.5f, 0.5f);
                    moonRect.pivot = new Vector2(0.5f, 0.5f);
                    float moonSize = 3f + (planetSeed + m) % 3;
                    moonRect.sizeDelta = new Vector2(moonSize, moonSize);
                    var moonImg = moonGo.AddComponent<Image>();
                    moonImg.raycastTarget = false;
                    moonImg.color = new Color(0.7f + (m * 0.1f) % 0.2f, 0.7f, 0.65f, 0.9f);
                    moonImg.sprite = PlanetSpriteGenerator.GetGlowSprite();

                    var moonData = new MoonData
                    {
                        rect = moonRect,
                        orbitRadius = size * 0.8f + m * 6f,
                        orbitSpeed = 0.5f + (m * 0.3f) + (planetSeed % 10) * 0.05f,
                        orbitAngle = (planetSeed + m * 137) % 360 * Mathf.Deg2Rad
                    };
                    moons.Add(moonData);
                }
                _planetMoons[p.id] = moons;
            }

            _planetNodes[p.id] = nodeGo;
        }

        private void RefreshVoyageShips()
        {
            if (_mapContent == null) return;
            foreach (Transform child in _mapContent)
            {
                if (child.name == "VoyageShip") Destroy(child.gameObject);
            }
            var voyages = SuperMechStarSystem.GetVoyages();
            if (voyages != null)
            {
                foreach (var v in voyages)
                {
                    if (v == null) continue;
                    var fromP = SuperMechStarSystem.GetPlanet(v.fromPlanet);
                    var toP = SuperMechStarSystem.GetPlanet(v.toPlanet);
                    if (fromP == null || toP == null) continue;
                    // v0.204 3D贝塞尔曲线航线：中间拱起模拟太空轨道
                    float t = v.progress;
                    Vector2 p0 = new Vector2(fromP.x, fromP.y);
                    Vector2 p2 = new Vector2(toP.x, toP.y);
                    // 控制点：中点垂直方向偏移，模拟轨道拱起
                    Vector2 mid = (p0 + p2) * 0.5f;
                    Vector2 dir = (p2 - p0).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);
                    float arcHeight = Vector2.Distance(p0, p2) * 0.25f; // 拱起高度为距离的25%
                    Vector2 p1 = mid + normal * arcHeight;
                    // 二次贝塞尔曲线公式
                    float shipX = (1 - t) * (1 - t) * p0.x + 2 * (1 - t) * t * p1.x + t * t * p2.x;
                    float shipY = (1 - t) * (1 - t) * p0.y + 2 * (1 - t) * t * p1.y + t * t * p2.y;
                    Vector2 shipPos = PlanetToMapPosition(shipX, shipY);
                    var shipGo = new GameObject("VoyageShip");
                    shipGo.transform.SetParent(_mapContent, false);
                    var shipRect = shipGo.AddComponent<RectTransform>();
                    shipRect.anchorMin = new Vector2(0.5f, 0.5f);
                    shipRect.anchorMax = new Vector2(0.5f, 0.5f);
                    shipRect.pivot = new Vector2(0.5f, 0.5f);
                    // v0.204 近大远小：航线中间（最高点）飞船最大，两端最小
                    float depthScale = 0.7f + 0.6f * Mathf.Sin(t * Mathf.PI);
                    shipRect.sizeDelta = new Vector2(16 * depthScale, 16 * depthScale);
                    shipRect.anchoredPosition = shipPos;
                    var shipImg = shipGo.AddComponent<Image>();
                    bool bodyFlight = v.snapshot != null && v.snapshot.rank >= 6;
                    shipImg.color = bodyFlight ? new Color(1f, 0.8f, 0.3f, 0.9f) : new Color(0.5f, 0.8f, 1f, 0.9f);
                    shipImg.raycastTarget = false;
                    if (v.snapshot != null)
                    {
                        var shipLabelGo = new GameObject("ShipLabel");
                        shipLabelGo.transform.SetParent(shipGo.transform, false);
                        var shipLabelRect = shipLabelGo.AddComponent<RectTransform>();
                        shipLabelRect.anchorMin = new Vector2(0.5f, 0);
                        shipLabelRect.anchorMax = new Vector2(0.5f, 0);
                        shipLabelRect.pivot = new Vector2(0.5f, 1);
                        shipLabelRect.sizeDelta = new Vector2(100, 14);
                        shipLabelRect.anchoredPosition = new Vector2(0, -2);
                        var shipLabelText = shipLabelGo.AddComponent<Text>();
                        shipLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        shipLabelText.fontSize = 9;
                        shipLabelText.color = bodyFlight ? new Color(1f, 0.8f, 0.3f) : new Color(0.7f, 0.85f, 1f);
                        shipLabelText.alignment = TextAnchor.UpperCenter;
                        shipLabelText.text = v.snapshot.name + (bodyFlight ? "[肉身]" : "");
                        shipLabelText.raycastTarget = false;
                    }
                }
            }
        }


        /// <summary>星球世界坐标→地图UI坐标</summary>
        private Vector2 PlanetToMapPosition(float worldX, float worldY)
        {
            float mapHalf = MAP_SIZE / 2f;
            float uiX = (worldX / VIEW_RANGE) * (mapHalf * _zoomLevel);
            float uiY = -(worldY / VIEW_RANGE) * (mapHalf * _zoomLevel);
            return new Vector2(uiX, uiY);
        }


        /// <summary>刷新分页标签栏选中状态</summary>
        private void RefreshTabBar()
        {
            if (_tabBar == null) return;
            for (int i = 0; i < _tabBar.transform.childCount; i++)
            {
                var btn = _tabBar.transform.GetChild(i).GetComponent<Button>();
                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) img.color = (i == _currentTab) ? new Color(0.3f, 0.5f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.25f, 0.6f);
                }
            }
        }

        /// <summary>刷新星球信息面板</summary>
        private void RefreshPlanetInfo(PlanetData p)
        {
            if (p == null || _infoText == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b><size=16>{p.name}</size></b>\n");
            sb.Append($"类型: {p.type}\n");
            sb.Append($"坐标: ({p.x:F0}, {p.y:F0})\n");
            if (p.colonyLevel > 0)
            {
                sb.Append($"殖民地等级: Lv{p.colonyLevel}\n");
                sb.Append($"资源: {p.resource:F0}\n");
                sb.Append($"驻留单位: {p.units?.Count ?? 0}\n");
            }
            _infoText.text = sb.ToString();
        }

        /// <summary>隐藏事件弹窗</summary>
        private void HideEventPopup()
        {
            if (_eventPopup != null)
            {
                Destroy(_eventPopup);
                _eventPopup = null;
            }
        }

        /// <summary>处理事件选择结果（Stellaris式）</summary>
        private void ResolveEventChoice(PlanetData planet, StarEventResult result, int choice)
        {
            // choice: 0=战斗 1=逃跑 2=谈判
            switch (choice)
            {
                case 0: // 战斗：单位阶位越高胜率越高
                    int maxRank = 0;
                    foreach (var u in planet.units) if (u != null && u.rank > maxRank) maxRank = u.rank;
                    float winChance = Mathf.Clamp01(0.3f + maxRank * 0.07f);
                    if (UnityEngine.Random.value < winChance)
                    {
                        // 胜利：获得资源
                        float loot = 100 + maxRank * 50;
                        planet.resource = Mathf.Min(planet.maxResource, planet.resource + loot);
                        Debug.Log($"[超神机械师] 事件战斗胜利，获得{loot:F0}资源");
                    }
                    else
                    {
                        // 失败：损失1个单位
                        if (planet.units.Count > 0) planet.units.RemoveAt(0);
                        Debug.Log("[超神机械师] 事件战斗失败，损失1单位");
                    }
                    break;
                case 1: // 逃跑：70%成功，成功无损失，失败损失资源
                    if (UnityEngine.Random.value < 0.7f)
                    {
                        Debug.Log("[超神机械师] 成功逃脱事件");
                    }
                    else
                    {
                        float loss = planet.resource * 0.2f;
                        planet.resource -= loss;
                        Debug.Log($"[超神机械师] 逃跑失败，损失{loss:F0}资源");
                    }
                    break;
                case 2: // 谈判：50%成功获得资源，失败损失单位
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        float gain = 150;
                        planet.resource = Mathf.Min(planet.maxResource, planet.resource + gain);
                        Debug.Log($"[超神机械师] 谈判成功，获得{gain:F0}资源");
                    }
                    else
                    {
                        if (planet.units.Count > 0) planet.units.RemoveAt(0);
                        Debug.Log("[超神机械师] 谈判破裂，损失1单位");
                    }
                    break;
            }
            if (planet.pendingEvent == result) planet.pendingEvent = null;
            SelectPlanet(planet.id);
            RefreshMap();
        }

        /// <summary>构建星球悬停提示文本</summary>
        private string BuildPlanetTooltip(PlanetData p)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(p.name).Append("</b>\n");
            if (p.type == PlanetType.Star)
            {
                sb.Append("<color=#ffcc66>").Append(p.starType.ToString()).Append("</color>\n");
                sb.Append(LocalizedTextManager.getText("sm_star_temperature")).Append(": ").Append(p.temperature.ToString("F0")).Append("K\n");
                sb.Append(LocalizedTextManager.getText("sm_star_mass")).Append(": ").Append(p.starMass.ToString("F2")).Append(" M☉");
            }
            else
            {
                string typeName = p.type.ToString();
                sb.Append("<color=#88ccff>").Append(typeName).Append("</color>\n");
                sb.Append(LocalizedTextManager.getText("sm_star_temperature")).Append(": ").Append((p.temperature - 273).ToString("F0")).Append("°C\n");
                if (p.inHabitableZone) sb.Append("<color=#7fff7f>").Append(LocalizedTextManager.getText("sm_star_habitable")).Append("</color>\n");
                if (p.discovered)
                {
                    sb.Append(LocalizedTextManager.getText("sm_star_colony_level")).Append(": ").Append(p.colonyLevel);
                    if (p.units.Count > 0) sb.Append(" | ").Append(LocalizedTextManager.getText("sm_star_units")).Append(": ").Append(p.units.Count);
                }
                else
                {
                    sb.Append("<color=#aaaaaa>").Append(LocalizedTextManager.getText("sm_star_unexplored")).Append("</color>");
                }
            }
            return sb.ToString();
        }

        /// <summary>显示悬停提示（跟随鼠标）</summary>
        private void ShowTooltip(string text)
        {
            if (_tooltip == null || _tooltipText == null) return;
            _tooltipText.text = text;
            _tooltip.SetActive(true);
            UpdateTooltipPosition();
        }

        private void HideTooltip()
        {
            if (_tooltip != null) _tooltip.SetActive(false);
        }

        private void UpdateTooltipPosition()
        {
            if (_tooltip == null || !_tooltip.activeSelf) return;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, Input.mousePosition, null, out localPos);
            var ttRect = _tooltip.GetComponent<RectTransform>();
            ttRect.anchoredPosition = new Vector2(localPos.x + 12, localPos.y - 12);
        }

        private void CreateLine(float x1, float y1, float x2, float y2)
        {
            var lineGo = new GameObject("Line_");
            lineGo.transform.SetParent(_mapContent, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0, 0.5f);
            float dx = x2 - x1, dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            lineRect.sizeDelta = new Vector2(len, 1.5f);
            lineRect.anchoredPosition = new Vector2(x1, y1);
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(0.4f, 0.5f, 0.7f, 0.25f);
            lineImg.raycastTarget = false;
        }

        /// <summary>创建轨道线（行星围绕恒星的圆形轨道）</summary>
        private void CreateOrbitLine(float centerX, float centerY, float radius)
        {
            var orbitGo = new GameObject("Orbit_");
            orbitGo.transform.SetParent(_mapContent, false);
            var orbitRect = orbitGo.AddComponent<RectTransform>();
            orbitRect.anchorMin = new Vector2(0.5f, 0.5f);
            orbitRect.anchorMax = new Vector2(0.5f, 0.5f);
            orbitRect.pivot = new Vector2(0.5f, 0.5f);
            orbitRect.sizeDelta = new Vector2(radius * 2, radius * 2);
            orbitRect.anchoredPosition = new Vector2(centerX, centerY);
            var orbitImg = orbitGo.AddComponent<Image>();
            orbitImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
            orbitImg.color = new Color(0.3f, 0.4f, 0.6f, 0.08f); // 淡淡的圆圈
            orbitImg.raycastTarget = false;
        }

        /// <summary>创建星团标记（疏散星团=蓝色，球状星团=橙色）</summary>
        private void CreateClusterMarker(SuperMechStarSystem.StarCluster c)
        {
            var clusterGo = new GameObject($"Cluster_{c.id}");
            clusterGo.transform.SetParent(_mapContent, false);
            var clusterRect = clusterGo.AddComponent<RectTransform>();
            clusterRect.anchorMin = new Vector2(0.5f, 0.5f);
            clusterRect.anchorMax = new Vector2(0.5f, 0.5f);
            clusterRect.pivot = new Vector2(0.5f, 0.5f);
            clusterRect.sizeDelta = new Vector2(c.radius * 2, c.radius * 2);
            clusterRect.anchoredPosition = new Vector2(c.x, -c.y);

            // 星团范围圆圈（半透明）
            var circleImg = clusterGo.AddComponent<Image>();
            circleImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
            circleImg.raycastTarget = false;
            if (c.type == SuperMechStarSystem.ClusterType.Globular)
            {
                circleImg.color = new Color(1f, 0.6f, 0.3f, 0.08f); // 球状星团：橙色
            }
            else
            {
                circleImg.color = new Color(0.4f, 0.7f, 1f, 0.08f); // 疏散星团：蓝色
            }

            // 星团名称标签
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(clusterGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(160, 18);
            labelRect.anchoredPosition = new Vector2(0, c.radius + 10);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 11;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;
            if (c.type == SuperMechStarSystem.ClusterType.Globular)
            {
                labelText.color = new Color(1f, 0.7f, 0.4f, 0.7f);
                labelText.text = $"◉ {c.name}";
            }
            else
            {
                labelText.color = new Color(0.6f, 0.8f, 1f, 0.7f);
                labelText.text = $"◎ {c.name}";
            }

            // 星团点击：显示星团信息（点击区域用子GameObject，避免同一对象两个Image）
            var hitGo = new GameObject("HitArea");
            hitGo.transform.SetParent(clusterGo.transform, false);
            var hitRect = hitGo.AddComponent<RectTransform>();
            hitRect.anchorMin = Vector2.zero;
            hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = Vector2.zero;
            hitRect.offsetMax = Vector2.zero;
            var clusterHit = hitGo.AddComponent<Image>();
            clusterHit.color = new Color(0f, 0f, 0f, 0f);
            var clusterBtn = hitGo.AddComponent<Button>();
            string cid = c.id;
            clusterBtn.onClick.AddListener(() => SelectCluster(cid));
        }

        /// <summary>选中星团，显示星团信息</summary>
        private void SelectCluster(string clusterId)
        {
            var c = SuperMechStarSystem.GetCluster(clusterId);
            if (c == null || _infoText == null) return;
            _selectedPlanet = null;

            string typeName = c.type == SuperMechStarSystem.ClusterType.Globular ?
                LocalizedTextManager.getText("sm_ui_cluster_globular") :
                LocalizedTextManager.getText("sm_ui_cluster_open");
            string typeColor = c.type == SuperMechStarSystem.ClusterType.Globular ? "#ffaa66" : "#66aaff";

            // 统计星团内星球
            var allPlanets = SuperMechStarSystem.GetAllPlanets();
            int starCount = 0, planetCount = 0, discovered = 0;
            foreach (var p in allPlanets)
            {
                float dx = p.x - c.x, dy = p.y - c.y;
                if (dx * dx + dy * dy <= c.radius * c.radius)
                {
                    if (p.type == PlanetType.Star) starCount++;
                    else planetCount++;
                    if (p.discovered) discovered++;
                }
            }

            _infoText.text =
                $"<color=#ffd700><size=14><b>{c.name}</b></size></color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cluster_type")}:</color> <color={typeColor}>{typeName}</color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cluster_radius")}:</color> {c.radius:F0} ly\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cluster_stars")}:</color> {starCount}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cluster_planets")}:</color> {planetCount}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_cluster_discovered")}:</color> {discovered}/{starCount + planetCount}\n" +
                $"\n<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_coord")}:</color> ({c.x:F0}, {c.y:F0})\n";

            // 清空按钮区域
            if (_buttonArea != null)
            {
                foreach (Transform child in _buttonArea) Destroy(child.gameObject);
                var hintGo = new GameObject("Hint");
                hintGo.transform.SetParent(_buttonArea, false);
                var hintRect = hintGo.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0, 1);
                hintRect.anchorMax = new Vector2(1, 1);
                hintRect.pivot = new Vector2(0.5f, 1);
                hintRect.sizeDelta = new Vector2(0, 40);
                hintRect.anchoredPosition = new Vector2(0, -4);
                var hintText = hintGo.AddComponent<Text>();
                hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hintText.fontSize = 11;
                hintText.color = new Color(0.8f, 0.7f, 0.5f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = LocalizedTextManager.getText("sm_ui_cluster_hint");
                hintText.raycastTarget = false;
            }
        }

        private void SelectPlanet(string id)
        {
            _selectedPlanet = id;
            // v0.223 选中星球时显示右侧详情面板
            if (_infoPanel != null) _infoPanel.SetActive(true);
            var p = SuperMechStarSystem.GetPlanet(id);
            if (p == null || _infoText == null) return;

            // 高亮选中的星球（改光晕亮度）
            foreach (var kv in _planetNodes)
            {
                var glow = kv.Value.transform.Find("Glow")?.GetComponent<Image>();
                if (glow != null)
                {
                    if (kv.Key == id)
                        glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0.8f);
                    else
                        glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, glow.color.a * 0.5f);
                }
            }

            string typeName = p.type switch
            {
                PlanetType.Rocky => LocalizedTextManager.getText("sm_ui_planet_rocky"),
                PlanetType.Gas => LocalizedTextManager.getText("sm_ui_planet_gas"),
                PlanetType.Ice => LocalizedTextManager.getText("sm_ui_planet_ice"),
                PlanetType.Lava => LocalizedTextManager.getText("sm_ui_planet_lava"),
                PlanetType.Ocean => LocalizedTextManager.getText("sm_ui_planet_ocean"),
                PlanetType.Desert => LocalizedTextManager.getText("sm_ui_planet_desert"),
                PlanetType.Forest => LocalizedTextManager.getText("sm_ui_planet_forest"),
                PlanetType.Star => LocalizedTextManager.getText("sm_ui_star_type"),
                _ => "?"
            };

            // 恒星类型名称
            string starTypeName = p.starType switch
            {
                StarType.RedDwarf => LocalizedTextManager.getText("sm_ui_star_red_dwarf"),
                StarType.YellowDwarf => LocalizedTextManager.getText("sm_ui_star_yellow_dwarf"),
                StarType.BlueGiant => LocalizedTextManager.getText("sm_ui_star_blue_giant"),
                StarType.RedGiant => LocalizedTextManager.getText("sm_ui_star_red_giant"),
                StarType.WhiteDwarf => LocalizedTextManager.getText("sm_ui_star_white_dwarf"),
                StarType.NeutronStar => LocalizedTextManager.getText("sm_ui_star_neutron"),
                StarType.BlackHole => LocalizedTextManager.getText("sm_ui_star_black_hole"),
                _ => "?"
            };

            bool isStar = p.type == PlanetType.Star;
            _infoText.text =
                $"<color=#ffd700><size=14><b>{p.name}</b></size></color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_type")}:</color> {typeName}\n";

            // 恒星显示光谱类型、温度、质量、半径、光度、年龄
            if (isStar)
            {
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_type")}:</color> {starTypeName}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_temp")}:</color> {p.temperature:F0}K\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_mass")}:</color> {p.starMass:F2} M☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_radius")}:</color> {p.starRadius:F2} R☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_luminosity")}:</color> {p.starLuminosity:F2} L☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_age")}:</color> {p.starAge:F1} {LocalizedTextManager.getText("sm_ui_star_age_unit")}\n";
            }
            else
            {
                // 行星显示温度、宜居带、物理参数
                string tempColor = p.inHabitableZone ? "#7fff7f" : (p.temperature > 500 ? "#ff7f7f" : "#7f7fff");
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_temp")}:</color> <color={tempColor}>{p.temperature:F0}K ({p.temperature - 273:F0}°C)</color>\n";
                string habitableText = p.inHabitableZone
                    ? $"<color=#7fff7f>{LocalizedTextManager.getText("sm_ui_habitable")}</color>"
                    : $"<color=#aaaaaa>{LocalizedTextManager.getText("sm_ui_not_habitable")}</color>";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_habitable")}:</color> {habitableText}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_mass")}:</color> {p.mass:F2} M⊕\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_radius")}:</color> {p.radius:F2} R⊕\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_gravity")}:</color> {p.gravity:F2} g\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_density")}:</color> {p.density:F1} g/cm³\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_rotation")}:</color> {p.rotationPeriod:F1} {LocalizedTextManager.getText("sm_ui_hour")}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_axial_tilt")}:</color> {p.axialTilt:F1}°\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_moon")}:</color> {p.moonCount} {LocalizedTextManager.getText("sm_ui_star_moon_unit")}\n";
                if (p.tidallyLocked)
                    _infoText.text += $"<color=#ff9966>{LocalizedTextManager.getText("sm_ui_star_tidally_locked")}</color>\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_atmosphere")}:</color> {(p.atmosphere * 100):F0}%\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_magnetic")}:</color> {(p.magneticField * 100):F0}%\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_eccentricity")}:</color> {p.eccentricity:F3}\n";
            }

            _infoText.text +=
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_resource")}:</color> {p.resource:F0}/{p.maxResource:F0}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_colony")}:</color> {p.colonyLevel}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_units")}:</color> {p.units.Count}\n" +
                (p.colonyLevel > 0 ? $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_owner")}:</color> {p.ownerKingdom}\n" : "\n") +
                $"\n<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_coord")}:</color> ({p.x:F0}, {p.y:F0})\n";

            if (!p.discovered && p.id != SuperMechStarSystem.MAIN_PLANET)
            {
                _infoText.text += "\n<color=#ffcc66>" + LocalizedTextManager.getText("sm_ui_planet_uncolonized") + "</color>";
            }

            // 根据星球状态创建交互按钮
            RefreshButtons(p);
        }

        /// <summary>根据星球状态刷新交互按钮</summary>
        private void RefreshButtons(PlanetData p)
        {
            if (_buttonArea == null) return;
            // 清空旧按钮
            foreach (Transform child in _buttonArea) Destroy(child.gameObject);

            bool isMain = p.id == SuperMechStarSystem.MAIN_PLANET;
            bool isStar = p.type == PlanetType.Star;
            float btnY = 0f;
            const float btnHeight = 30f;
            const float btnGap = 5f;

            // 恒星：只能查看信息，不能操作
            if (isStar)
            {
                var hintGo = new GameObject("Hint");
                hintGo.transform.SetParent(_buttonArea, false);
                var hintRect = hintGo.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0, 1);
                hintRect.anchorMax = new Vector2(1, 1);
                hintRect.pivot = new Vector2(0.5f, 1);
                hintRect.sizeDelta = new Vector2(0, 40);
                hintRect.anchoredPosition = new Vector2(0, -4);
                var hintText = hintGo.AddComponent<Text>();
                hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hintText.fontSize = 11;
                hintText.color = new Color(0.8f, 0.7f, 0.5f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = LocalizedTextManager.getText("sm_ui_star_cannot_land");
                hintText.raycastTarget = false;
                return;
            }

            // 已发现：进入星球（多世界系统）
            if (p.discovered || isMain)
            {
                string enterLabel = isMain ?
                    LocalizedTextManager.getText("sm_ui_enter_main_world") :
                    LocalizedTextManager.getText("sm_ui_enter_planet");
                CreateActionButton(enterLabel, new Color(0.9f, 0.7f, 0.2f, 0.9f), () =>
                {
                    if (SuperMechMultiWorld.IsSwitching) return;
                    Close();
                    SuperMechMultiWorld.SwitchToPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);
            }

            // 未发现：派遣探险队（打开单位选择面板）
            if (!p.discovered && !isMain)
            {
                CreateActionButton(LocalizedTextManager.getText("sm_ui_send_explorer"), new Color(0.2f, 0.5f, 0.8f, 0.9f), () =>
                {
                    _pendingSendPlanet = p;
                    ShowUnitSelectPanel();
                }, ref btnY, btnHeight, btnGap);
            }

            // 已发现但未殖民：建立殖民地（消耗500资源+需要至少1个驻留单位）
            if (p.discovered && p.colonyLevel == 0 && !isMain)
            {
                int colonizeCost = 500;
                bool canColonize = p.resource >= colonizeCost && p.units.Count > 0;
                string colonizeLabel = canColonize
                    ? LocalizedTextManager.getText("sm_ui_colonize") + $" ({colonizeCost})"
                    : LocalizedTextManager.getText("sm_ui_colonize") + " (" + LocalizedTextManager.getText("sm_ui_need_unit") + ")";
                CreateActionButton(colonizeLabel, new Color(0.2f, 0.7f, 0.3f, 0.9f), () =>
                {
                    if (p.resource >= colonizeCost && p.units.Count > 0)
                    {
                        p.resource -= colonizeCost;
                        p.colonyLevel = 1;
                        p.ownerKingdom = p.units[0].kingdomName ?? "player";
                        p.maxResource = 1000; // 殖民后解锁资源上限
                        SelectPlanet(p.id);
                        RefreshMap();
                    }
                }, ref btnY, btnHeight, btnGap);
            }

            // 已殖民：升级殖民地 + 派遣单位 + 采集资源 + 运回资源 + 召回单位
            if (p.colonyLevel > 0 && !isMain)
            {
                // 殖民地升级消耗资源（每级消耗递增）
                int upgradeCost = p.colonyLevel * 200;
                string upgradeLabel = p.colonyLevel < 5
                    ? string.Format(LocalizedTextManager.getText("sm_ui_upgrade_colony"), p.colonyLevel) + $" ({upgradeCost})"
                    : LocalizedTextManager.getText("sm_ui_colony_max");
                CreateActionButton(upgradeLabel, new Color(0.8f, 0.6f, 0.2f, 0.9f), () =>
                {
                    if (p.colonyLevel < 5 && p.resource >= upgradeCost)
                    {
                        p.resource -= upgradeCost;
                        p.colonyLevel++;
                        SelectPlanet(p.id);
                    }
                }, ref btnY, btnHeight, btnGap);

                CreateActionButton(LocalizedTextManager.getText("sm_ui_send_unit"), new Color(0.5f, 0.3f, 0.8f, 0.9f), () =>
                {
                    _pendingSendPlanet = p;
                    ShowUnitSelectPanel();
                }, ref btnY, btnHeight, btnGap);

                // 有驻留单位时显示召回按钮
                if (p.units.Count > 0)
                {
                    CreateActionButton(string.Format(LocalizedTextManager.getText("sm_ui_recall_unit"), p.units.Count), new Color(0.3f, 0.5f, 0.7f, 0.9f), () =>
                    {
                        // 召回一个单位回主世界
                        if (p.units.Count > 0)
                        {
                            var snapshot = p.units[0];
                            p.units.RemoveAt(0);
                            // 在主世界生成该单位
                            SuperMechStarSystem.SpawnUnitAtMain(snapshot);
                            SelectPlanet(p.id);
                            RefreshMap();
                        }
                    }, ref btnY, btnHeight, btnGap);

                    // 单位管理：打开单独分配面板（Stellaris式人口分配）
                    int miningCnt = 0, researchCnt = 0, defenseCnt = 0, idleCnt = 0;
                    foreach (var u in p.units)
                    {
                        if (u == null) continue;
                        if (u.assignment == "mining") miningCnt++;
                        else if (u.assignment == "research") researchCnt++;
                        else if (u.assignment == "defense") defenseCnt++;
                        else idleCnt++;
                    }
                    string manageLabel = LocalizedTextManager.getText("sm_ui_unit_manage") +
                        $" (矿:{miningCnt} 研:{researchCnt} 防:{defenseCnt} 闲:{idleCnt})";
                    CreateActionButton(manageLabel, new Color(0.6f, 0.4f, 0.2f, 0.9f), () =>
                    {
                        ShowUnitManagePanel(p);
                    }, ref btnY, btnHeight, btnGap);
                }

                CreateActionButton(string.Format(LocalizedTextManager.getText("sm_ui_collect_resource"), p.resource), new Color(0.8f, 0.3f, 0.3f, 0.9f), () =>
                {
                    float collected = Mathf.Min(p.resource, 100f * p.colonyLevel);
                    p.resource -= collected;
                    var main = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
                    if (main != null) main.resource += collected;
                    SelectPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);

                // 戴森球式：自动运输开关
                string autoLabel = p.autoTransport
                    ? LocalizedTextManager.getText("sm_ui_auto_transport_on")
                    : LocalizedTextManager.getText("sm_ui_auto_transport_off");
                CreateActionButton(autoLabel, p.autoTransport ? new Color(0.3f, 0.7f, 0.4f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.9f), () =>
                {
                    p.autoTransport = !p.autoTransport;
                    SelectPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);

                // 探索按钮：有驻留单位时可触发事件
                if (p.units.Count > 0)
                {
                    CreateActionButton(LocalizedTextManager.getText("sm_ui_explore_planet"), new Color(0.6f, 0.3f, 0.8f, 0.9f), () =>
                    {
                        StarEvents.Init();
                        var rng = new System.Random();
                        var evt = StarEvents.RollEvent(rng, p.type, p.colonyLevel);
                        if (evt != null)
                        {
                            float defense = ColonyManager.CalculateDefenseReduction(p);
                            var result = StarEvents.ResolveEvent(evt, p, rng);
                            if ((result.resultType == EventResultType.DamageUnit || result.resultType == EventResultType.LoseUnit)
                                && rng.NextDouble() < defense)
                            {
                                result.resultMessage = LocalizedTextManager.getText("sm_star_evt_defended") + result.resultMessage;
                                result.unitsLost = 0;
                            }
                            p.pendingEvent = result;
                            ShowEventPopup(result);
                            SelectPlanet(p.id);
                        }
                    }, ref btnY, btnHeight, btnGap);
                }

                // 殖民地建设按钮
                CreateActionButton(LocalizedTextManager.getText("sm_ui_colony_building"), new Color(0.3f, 0.6f, 0.5f, 0.9f), () =>
                {
                    ShowColonyPanel(p);
                }, ref btnY, btnHeight, btnGap);

                // 有待处理事件时显示事件按钮
                if (p.pendingEvent != null)
                {
                    CreateActionButton(LocalizedTextManager.getText("sm_ui_view_event"), new Color(0.9f, 0.5f, 0.2f, 0.9f), () =>
                    {
                        ShowEventPopup(p.pendingEvent);
                    }, ref btnY, btnHeight, btnGap);
                }
            }

            // 主世界：显示驻留单位数量和资源
            if (isMain)
            {
                var hintGo = new GameObject("Hint");
                hintGo.transform.SetParent(_buttonArea, false);
                var hintRect = hintGo.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0, 1);
                hintRect.anchorMax = new Vector2(1, 1);
                hintRect.pivot = new Vector2(0.5f, 1);
                hintRect.sizeDelta = new Vector2(0, 50);
                hintRect.anchoredPosition = new Vector2(0, -4);
                var hintText = hintGo.AddComponent<Text>();
                hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hintText.fontSize = 11;
                hintText.color = new Color(0.7f, 0.8f, 0.9f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = string.Format(LocalizedTextManager.getText("sm_ui_main_planet_info"), p.resource);
                hintText.raycastTarget = false;
            }
        }

        /// <summary>创建一个操作按钮</summary>
        private void CreateActionButton(string text, Color bgColor, UnityEngine.Events.UnityAction action, ref float y, float height, float gap)
        {
            var btnGo = new GameObject("ActionBtn");
            btnGo.transform.SetParent(_buttonArea, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.sizeDelta = new Vector2(0, height);
            btnRect.anchoredPosition = new Vector2(0, -y);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = bgColor;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            var btnText = textGo.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = text;
            btnText.raycastTarget = false;

            y += height + gap;
        }

        /// <summary>显示事件结果弹窗</summary>
        /// <summary>显示单位选择面板（派遣单位时用）</summary>
        private void ShowUnitSelectPanel()
        {
            HideUnitSelectPanel();
            if (_pendingSendPlanet == null) return;

            // 创建半透明遮罩
            var overlayGo = new GameObject("UnitSelectOverlay");
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
            overlayBtn.onClick.AddListener(HideUnitSelectPanel);
            _unitSelectPanel = overlayGo;

            // 选择面板
            var panelGo = new GameObject("UnitSelectPanel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 350);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_select_unit") + " -> " + _pendingSendPlanet.name;

            // 单位列表（滚动区域）
            var scrollGo = new GameObject("UnitScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -45);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            // 收集玩家单位
            var allUnits = World.world.units.getSimpleList();
            var playerUnits = new System.Collections.Generic.List<Actor>();
            foreach (var u in allUnits)
            {
                if (u != null && u.isAlive() && u.kingdom != null && u.kingdom.name == "player")
                    playerUnits.Add(u);
            }

            float itemY = 0f;
            foreach (var unit in playerUnits)
            {
                if (unit == null) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(unit);
                string rankName = rank >= 0 && rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name) : "?";
                bool canBodyFlight = rank >= 6;

                var itemGo = new GameObject("UnitItem");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 32);
                itemRect.anchoredPosition = new Vector2(0, -itemY);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
                var itemBtn = itemGo.AddComponent<Button>();
                long unitId = unit.data.id;
                itemBtn.onClick.AddListener(() =>
                {
                    bool success = SuperMechStarSystem.SendUnit(unitId, SuperMechStarSystem.MAIN_PLANET, _pendingSendPlanet.id);
                    if (success)
                    {
                        Debug.Log($"[超神机械师] 派遣 {unit.data.name}({rankName}) 前往 {_pendingSendPlanet.name}");
                        HideUnitSelectPanel();
                        SelectPlanet(_pendingSendPlanet.id);
                        RefreshMap();
                    }
                });

                var itemTextGo = new GameObject("Text");
                itemTextGo.transform.SetParent(itemGo.transform, false);
                var itemTextRect = itemTextGo.AddComponent<RectTransform>();
                itemTextRect.anchorMin = Vector2.zero;
                itemTextRect.anchorMax = Vector2.one;
                itemTextRect.offsetMin = new Vector2(8, 0);
                itemTextRect.offsetMax = new Vector2(-8, 0);
                var itemText = itemTextGo.AddComponent<Text>();
                itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                itemText.fontSize = 12;
                itemText.color = canBodyFlight ? new Color(1f, 0.8f, 0.3f) : Color.white;
                itemText.alignment = TextAnchor.MiddleLeft;
                string flightTag = canBodyFlight ? " [肉身横渡]" : "";
                itemText.text = $"{unit.data.name} ({rankName}){flightTag}";
                itemText.raycastTarget = false;

                itemY += 34f;
            }

            // 无单位提示
            if (playerUnits.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(contentGo.transform, false);
                var emptyRect = emptyGo.AddComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.sizeDelta = new Vector2(300, 30);
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                emptyText.fontSize = 12;
                emptyText.color = new Color(0.6f, 0.6f, 0.6f);
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.text = LocalizedTextManager.getText("sm_ui_no_player_unit");
            }

            // 调整content高度
            contentRect.sizeDelta = new Vector2(0, Mathf.Max(300, itemY));
        }

        /// <summary>隐藏单位选择面板</summary>
        private void HideUnitSelectPanel()
        {
            if (_unitSelectPanel != null)
            {
                Destroy(_unitSelectPanel);
                _unitSelectPanel = null;
            }
            _pendingSendPlanet = null;
        }

        private GameObject _unitManagePanel;

        /// <summary>Stellaris式单位管理面板：每个单位单独分配任务</summary>
        private void ShowUnitManagePanel(PlanetData planet)
        {
            HideUnitManagePanel();
            _managePlanet = planet;

            // 遮罩
            var overlayGo = new GameObject("UnitManageOverlay");
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
            overlayBtn.onClick.AddListener(HideUnitManagePanel);
            _unitManagePanel = overlayGo;

            // 面板
            var panelGo = new GameObject("UnitManagePanel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(450, 400);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_unit_manage") + " - " + planet.name;

            // 列表内容
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(panelGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -45);

            float itemY = 0f;
            for (int i = 0; i < planet.units.Count; i++)
            {
                var u = planet.units[i];
                if (u == null) continue;
                int idx = i;

                var itemGo = new GameObject($"Unit_{idx}");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 36);
                itemRect.anchoredPosition = new Vector2(0, -itemY);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = idx % 2 == 0 ? new Color(0.1f, 0.15f, 0.25f, 0.9f) : new Color(0.08f, 0.12f, 0.2f, 0.9f);

                // 单位名称
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(itemGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0);
                nameRect.anchorMax = new Vector2(0.4f, 1);
                nameRect.offsetMin = new Vector2(8, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 11;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                string rankName = u.rank >= 0 && u.rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[u.rank].name) : "?";
                nameText.text = $"{u.name} ({rankName})";
                nameText.raycastTarget = false;

                // 四个任务按钮：空闲/采矿/研究/防御
                string[] tasks = { "idle", "mining", "research", "defense" };
                string[] taskLabels = { LocalizedTextManager.getText("sm_ui_star_idle"), LocalizedTextManager.getText("sm_ui_star_mining"), LocalizedTextManager.getText("sm_ui_star_research"), LocalizedTextManager.getText("sm_ui_star_defense") };
                Color[] taskColors = {
                    new Color(0.4f, 0.4f, 0.4f, 0.9f),
                    new Color(0.8f, 0.6f, 0.2f, 0.9f),
                    new Color(0.2f, 0.6f, 0.8f, 0.9f),
                    new Color(0.8f, 0.3f, 0.3f, 0.9f)
                };
                for (int t = 0; t < 4; t++)
                {
                    var btnGo = new GameObject($"TaskBtn_{t}");
                    btnGo.transform.SetParent(itemGo.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.4f + t * 0.15f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.4f + (t + 1) * 0.15f, 0.85f);
                    btnRect.offsetMin = new Vector2(2, 0);
                    btnRect.offsetMax = new Vector2(-2, 0);
                    var btnImg = btnGo.AddComponent<Image>();
                    bool isActive = u.assignment == tasks[t] || (tasks[t] == "idle" && string.IsNullOrEmpty(u.assignment));
                    btnImg.color = isActive ? taskColors[t] : new Color(0.2f, 0.2f, 0.2f, 0.6f);
                    var btn = btnGo.AddComponent<Button>();
                    string task = tasks[t];
                    btn.onClick.AddListener(() =>
                    {
                        u.assignment = task;
                        ShowUnitManagePanel(_managePlanet); // 刷新
                        SelectPlanet(_managePlanet.id);
                    });
                    var btnTextGo = new GameObject("Text");
                    btnTextGo.transform.SetParent(btnGo.transform, false);
                    var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.offsetMin = Vector2.zero;
                    btnTextRect.offsetMax = Vector2.zero;
                    var btnText = btnTextGo.AddComponent<Text>();
                    btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    btnText.fontSize = 11;
                    btnText.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                    btnText.alignment = TextAnchor.MiddleCenter;
                    btnText.text = taskLabels[t];
                    btnText.raycastTarget = false;
                }

                itemY += 38f;
            }

            // 无单位提示
            if (planet.units.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(contentGo.transform, false);
                var emptyRect = emptyGo.AddComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.sizeDelta = new Vector2(300, 30);
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                emptyText.fontSize = 12;
                emptyText.color = new Color(0.6f, 0.6f, 0.6f);
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.text = LocalizedTextManager.getText("sm_ui_no_unit");
            }
        }

        /// <summary>隐藏单位管理面板</summary>
        private void HideUnitManagePanel()
        {
            if (_unitManagePanel != null)
            {
                Destroy(_unitManagePanel);
                _unitManagePanel = null;
            }
            _managePlanet = null;
        }

        private void ShowEventPopup(StarEventResult result)
        {
            if (result == null) return;

            var popupGo = new GameObject("EventPopup");
            popupGo.transform.SetParent(transform, false);
            var popupRect = popupGo.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(360, 220);
            popupRect.anchoredPosition = Vector2.zero;

            var bgImg = popupGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(popupGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -8);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = result.success ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = result.eventName;
            titleText.raycastTarget = false;

            // 描述
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(popupGo.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0);
            descRect.anchorMax = new Vector2(0.95f, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.sizeDelta = new Vector2(0, 0);
            descRect.anchoredPosition = new Vector2(0, -55);
            var descText = descGo.AddComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.color = new Color(0.8f, 0.85f, 0.9f);
            descText.alignment = TextAnchor.UpperCenter;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Overflow;
            descText.text = result.eventDesc + "\n\n" + result.resultMessage;
            descText.raycastTarget = false;

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(popupGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(120, 32);
            closeRect.anchoredPosition = new Vector2(0, 12);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.3f, 0.4f, 0.6f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => Destroy(popupGo));
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 12;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = LocalizedTextManager.getText("sm_ui_close");
            closeText.raycastTarget = false;
        }

        /// <summary>显示殖民地建设面板</summary>
        private void ShowColonyPanel(PlanetData planet)
        {
            ColonyManager.Init();

            var panelGo = new GameObject("ColonyPanel");
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 400);
            panelRect.anchoredPosition = Vector2.zero;

            var bgImg = panelGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 36);
            titleRect.anchoredPosition = new Vector2(0, -6);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.color = new Color(0.9f, 0.8f, 0.5f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = planet.name + " - " + LocalizedTextManager.getText("sm_ui_colony_building");
            titleText.raycastTarget = false;

            // v0.185 殖民地总览信息
            var overviewGo = new GameObject("Overview");
            overviewGo.transform.SetParent(panelGo.transform, false);
            var overviewRect = overviewGo.AddComponent<RectTransform>();
            overviewRect.anchorMin = new Vector2(0, 1);
            overviewRect.anchorMax = new Vector2(1, 1);
            overviewRect.pivot = new Vector2(0.5f, 1);
            overviewRect.sizeDelta = new Vector2(0, 50);
            overviewRect.anchoredPosition = new Vector2(0, -42);
            var overviewText = overviewGo.AddComponent<Text>();
            overviewText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overviewText.fontSize = 11;
            overviewText.color = new Color(0.7f, 0.8f, 0.9f);
            overviewText.alignment = TextAnchor.UpperLeft;
            overviewText.horizontalOverflow = HorizontalWrapMode.Wrap;
            int buildingCount = planet.buildings != null ? planet.buildings.Count : 0;
            int unitCount = planet.units != null ? planet.units.Count : 0;
            overviewText.text = $"等级: {planet.colonyLevel}  资源: {Mathf.RoundToInt(planet.resource)}  建筑: {buildingCount}  驻留单位: {unitCount}";
            overviewText.raycastTarget = false;

            // 建筑列表滚动区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.82f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 360);

            float itemY = 0f;
            foreach (var def in ColonyManager.GetAllDefs())
            {
                // 查找当前建筑等级
                int level = 0;
                bool isBuilding = false;
                float buildProgress = 0f;
                if (planet.buildings != null)
                {
                    foreach (var b in planet.buildings)
                    {
                        if (b.type == def.type)
                        {
                            level = b.level;
                            isBuilding = b.isBuilding;
                            buildProgress = b.buildProgress;
                            break;
                        }
                    }
                }

                bool locked = planet.colonyLevel < def.requiredColonyLevel;
                bool maxed = level >= def.maxLevel;
                float cost = ColonyManager.GetUpgradeCost(def, level);

                // 建筑条目背景
                var itemGo = new GameObject("BuildingItem");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 42);
                itemRect.anchoredPosition = new Vector2(0, -itemY);

                var itemBg = itemGo.AddComponent<Image>();
                itemBg.color = locked ? new Color(0.15f, 0.15f, 0.15f, 0.8f) : new Color(0.15f, 0.18f, 0.22f, 0.8f);

                // 建筑名称和等级
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(itemGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.02f, 0);
                nameRect.anchorMax = new Vector2(0.6f, 1);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 11;
                nameText.color = locked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                string statusText = isBuilding ? $" ({Mathf.RoundToInt(buildProgress * 100)}%)" : (maxed ? " (MAX)" : $" Lv.{level}");
                nameText.text = LocalizedTextManager.getText(def.nameKey) + statusText;
                nameText.raycastTarget = false;

                // 升级按钮
                if (!locked && !maxed && !isBuilding)
                {
                    var btnGo = new GameObject("UpgradeBtn");
                    btnGo.transform.SetParent(itemGo.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.7f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.98f, 0.85f);
                    btnRect.offsetMin = Vector2.zero;
                    btnRect.offsetMax = Vector2.zero;
                    var btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = new Color(0.2f, 0.5f, 0.3f, 0.9f);
                    var btn = btnGo.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        bool success = ColonyManager.UpgradeBuilding(planet, def.type);
                        if (success)
                        {
                            Destroy(panelGo);
                            ShowColonyPanel(planet);
                            SelectPlanet(planet.id);
                        }
                    });
                    var btnTextGo = new GameObject("Text");
                    btnTextGo.transform.SetParent(btnGo.transform, false);
                    var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.offsetMin = Vector2.zero;
                    btnTextRect.offsetMax = Vector2.zero;
                    var btnText = btnTextGo.AddComponent<Text>();
                    btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    btnText.fontSize = 10;
                    btnText.color = Color.white;
                    btnText.alignment = TextAnchor.MiddleCenter;
                    btnText.text = $"{Mathf.RoundToInt(cost)}₡";
                    btnText.raycastTarget = false;
                }
                else if (locked)
                {
                    var lockGo = new GameObject("LockText");
                    lockGo.transform.SetParent(itemGo.transform, false);
                    var lockRect = lockGo.AddComponent<RectTransform>();
                    lockRect.anchorMin = new Vector2(0.7f, 0);
                    lockRect.anchorMax = new Vector2(0.98f, 1);
                    lockRect.offsetMin = Vector2.zero;
                    lockRect.offsetMax = Vector2.zero;
                    var lockText = lockGo.AddComponent<Text>();
                    lockText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    lockText.fontSize = 10;
                    lockText.color = new Color(0.5f, 0.5f, 0.5f);
                    lockText.alignment = TextAnchor.MiddleCenter;
                    lockText.text = $"需殖民地Lv.{def.requiredColonyLevel}";
                    lockText.raycastTarget = false;
                }

                itemY += 46f;
            }

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(panelGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(100, 30);
            closeRect.anchoredPosition = new Vector2(0, 8);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.4f, 0.3f, 0.3f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => Destroy(panelGo));
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 12;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = LocalizedTextManager.getText("sm_ui_close");
            closeText.raycastTarget = false;
        }
    }
}
