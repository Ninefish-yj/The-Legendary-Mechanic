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

        /// <summary>每帧更新：处理滚轮缩放、航行单位位置刷新、星星闪烁</summary>
        private void Update()
        {
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
            topImg.color = new Color(0.02f, 0.04f, 0.08f, 0.95f);
            topImg.raycastTarget = true;

            // 返回按钮
            var backGo = new GameObject("BackBtn");
            backGo.transform.SetParent(topGo.transform, false);
            var backRect = backGo.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.sizeDelta = new Vector2(120, 34);
            backRect.anchoredPosition = new Vector2(12, 0);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.15f, 0.25f, 0.4f, 0.9f);
            var backBtn = backGo.AddComponent<Button>();
            backBtn.onClick.AddListener(Close);

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

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero; titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(140, 0); titleRect.offsetMax = new Vector2(-140, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = new Color(0.5f, 0.8f, 1f);
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

            // 星图区域（左侧70%，从顶部栏下方开始）
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(transform, false);
            var mapRect = mapGo.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = new Vector2(0.7f, 1);
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
            scrollRect.content = _mapContent;
            scrollRect.viewport = vpRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 生成程序化星空背景
            var starfieldResult = StarfieldBackground.Generate(_mapContent, MAP_SIZE);
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
            _coordText.color = new Color(0.4f, 0.5f, 0.6f);
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
            ziImg.color = new Color(0.15f, 0.25f, 0.4f, 0.8f);
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
            zoImg.color = new Color(0.15f, 0.25f, 0.4f, 0.8f);
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

            // 右侧信息面板（从顶部栏下方开始）
            var infoGo = new GameObject("InfoPanel");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.7f, 0);
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = new Vector2(0, -TopBarHeight);
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);
            infoImg.raycastTarget = true; // 拦截鼠标，防止穿透到游戏

            var infoTextGo = new GameObject("InfoText");
            infoTextGo.transform.SetParent(infoGo.transform, false);
            var itRect = infoTextGo.AddComponent<RectTransform>();
            itRect.anchorMin = new Vector2(0, 0.3f); itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(8, 0); itRect.offsetMax = new Vector2(-8, -8);
            _infoText = infoTextGo.AddComponent<Text>();
            _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _infoText.fontSize = 12;
            _infoText.color = new Color(0.8f, 0.9f, 1f);
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;
            _infoText.text = LocalizedTextManager.getText("sm_ui_select_planet");

            // 交互按钮区域（下半部分30%）
            var btnAreaGo = new GameObject("ButtonArea");
            btnAreaGo.transform.SetParent(infoGo.transform, false);
            _buttonArea = btnAreaGo.AddComponent<RectTransform>();
            _buttonArea.anchorMin = Vector2.zero;
            _buttonArea.anchorMax = new Vector2(1, 0.3f);
            _buttonArea.offsetMin = new Vector2(8, 8);
            _buttonArea.offsetMax = new Vector2(-8, 0);

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
            ttImg.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);
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

            // 科幻风格外框装饰（最上层）
            AddSciFiFrame();
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
                var coronaImg = coronaGo.AddComponent<Image>();
                coronaImg.sprite = PlanetSpriteGenerator.GetGlowSprite();
                coronaImg.raycastTarget = false;
                coronaImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.25f);
            }

            _planetNodes[p.id] = nodeGo;

            // 星球名称标签（恒星和已发现的显示）
            if (!isUnknown)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(nodeGo.transform, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0);
                labelRect.anchorMax = new Vector2(0.5f, 0);
                labelRect.pivot = new Vector2(0.5f, 1);
                labelRect.sizeDelta = new Vector2(120, 16);
                labelRect.anchoredPosition = new Vector2(0, -4);
                var labelText = labelGo.AddComponent<Text>();
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.fontSize = isStar ? 13 : (isMain ? 14 : 10);
                labelText.color = isStar ? new Color(1f, 0.9f, 0.6f) : (isMain ? new Color(1f, 0.9f, 0.5f) : new Color(0.7f, 0.75f, 0.85f));
                labelText.alignment = TextAnchor.UpperCenter;
                labelText.text = p.name;
                labelText.raycastTarget = false;
            }

            // 按钮（放在最上层接收点击）
            var btn = nodeGo.AddComponent<Button>();
            // 用透明Image作为点击区域
            var hitImg = nodeGo.AddComponent<Image>();
            hitImg.color = new Color(0f, 0f, 0f, 0f);

            string pid = p.id;
            btn.onClick.AddListener(() => SelectPlanet(pid));

            // 悬停提示：显示星球名称+类型+温度
            var trigger = nodeGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            string tooltipText = BuildPlanetTooltip(p);
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => ShowTooltip(tooltipText));
            trigger.triggers.Add(enterEntry);
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(exitEntry);
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
        private void CreateClusterMarker(StarCluster c)
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
            if (c.type == ClusterType.Globular)
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
            if (c.type == ClusterType.Globular)
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

            string typeName = c.type == ClusterType.Globular ?
                LocalizedTextManager.getText("sm_ui_cluster_globular") :
                LocalizedTextManager.getText("sm_ui_cluster_open");
            string typeColor = c.type == ClusterType.Globular ? "#ffaa66" : "#66aaff";

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

            // 未发现：派遣探险队（单位航行到达后才发现）
            if (!p.discovered && !isMain)
            {
                CreateActionButton(LocalizedTextManager.getText("sm_ui_send_explorer"), new Color(0.2f, 0.5f, 0.8f, 0.9f), () =>
                {
                    // 只选玩家的存活单位
                    var allUnits = World.world.units.getSimpleList();
                    var playerUnits = new System.Collections.Generic.List<Actor>();
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.isAlive() && u.kingdom != null && u.kingdom.name == "player")
                        {
                            playerUnits.Add(u);
                        }
                    }
                    if (playerUnits.Count > 0)
                    {
                        var unit = playerUnits[UnityEngine.Random.Range(0, playerUnits.Count)];
                        bool success = SuperMechStarSystem.SendUnit(unit.id, SuperMechStarSystem.MAIN_PLANET, p.id);
                        if (success)
                        {
                            Debug.Log($"[超神机械师] 派遣探险队 {unit.data.name} 前往 {p.name}");
                            SelectPlanet(p.id);
                        }
                    }
                }, ref btnY, btnHeight, btnGap);
            }

            // 已发现但未殖民：建立殖民地
            if (p.discovered && p.colonyLevel == 0 && !isMain)
            {
                CreateActionButton(LocalizedTextManager.getText("sm_ui_colonize"), new Color(0.2f, 0.7f, 0.3f, 0.9f), () =>
                {
                    p.colonyLevel = 1;
                    p.ownerKingdom = "player";
                    SelectPlanet(p.id);
                    RefreshMap();
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
                    // 只选玩家的存活单位
                    var allUnits = World.world.units.getSimpleList();
                    var playerUnits = new System.Collections.Generic.List<Actor>();
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.isAlive() && u.kingdom != null && u.kingdom.name == "player")
                        {
                            playerUnits.Add(u);
                        }
                    }
                    if (playerUnits.Count > 0)
                    {
                        var unit = playerUnits[UnityEngine.Random.Range(0, playerUnits.Count)];
                        SuperMechStarSystem.SendUnit(unit.id, SuperMechStarSystem.MAIN_PLANET, p.id);
                        SelectPlanet(p.id);
                    }
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
                }

                CreateActionButton(string.Format(LocalizedTextManager.getText("sm_ui_collect_resource"), p.resource), new Color(0.8f, 0.3f, 0.3f, 0.9f), () =>
                {
                    float collected = Mathf.Min(p.resource, 100f * p.colonyLevel);
                    p.resource -= collected;
                    var main = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
                    if (main != null) main.resource += collected;
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

            // 建筑列表滚动区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.9f);
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
