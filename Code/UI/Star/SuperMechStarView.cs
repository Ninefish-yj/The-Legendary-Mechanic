using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口：无限星海，全屏模式，程序化星空背景+圆形星球+光晕</summary>
    public partial class SuperMechStarView : MonoBehaviour
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
    }
}
