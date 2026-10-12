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
        private string _selectedPlanet;
        private readonly Dictionary<string, GameObject> _planetNodes = new Dictionary<string, GameObject>();
        private const float MAP_SIZE = 2400f;
        private const float VIEW_RANGE = 1200f;
        private const int STAR_COUNT = 800; // 星空背景星星数量
        private const int TopBarHeight = 50;
        private static readonly Dictionary<string, Sprite> _planetSpriteCache = new Dictionary<string, Sprite>();
        private static Sprite _glowSprite;
        private static Sprite _nebulaSprite; // 圆形星云渐变纹理

        /// <summary>生成圆形星云纹理（大尺寸柔和径向渐变）</summary>
        private static Sprite GetNebulaSprite()
        {
            if (_nebulaSprite != null) return _nebulaSprite;
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                    // 柔和的星云渐变：中心较亮，边缘渐隐，带一点噪声感
                    float alpha = dist < 1f ? Mathf.Pow(1f - dist, 1.5f) * 0.4f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _nebulaSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _nebulaSprite;
        }

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

        /// <summary>生成圆形光晕纹理（中心亮边缘透明的径向渐变）</summary>
        private static Sprite GetGlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                    float alpha = dist < 1f ? Mathf.Pow(1f - dist, 2f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _glowSprite;
        }

        /// <summary>生成圆形星球纹理（带光照渐变+表面噪声，纯代码实现）</summary>
        private static Sprite GetPlanetSprite(Color baseColor, int seed, bool isUnknown = false)
        {
            string key = $"{baseColor.r:F2}_{baseColor.g:F2}_{baseColor.b:F2}_{seed}_{isUnknown}";
            if (_planetSpriteCache.TryGetValue(key, out var cached)) return cached;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var rng = new System.Random(seed);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);

                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (isUnknown)
                    {
                        // 未知星球：灰色半透明
                        tex.SetPixel(x, y, new Color(0.3f, 0.3f, 0.35f, 0.4f * (1f - dist * 0.3f)));
                        continue;
                    }

                    // 光照：左上光源，右下暗
                    float nx = dx / (size / 2f);
                    float ny = dy / (size / 2f);
                    float light = 1f - (nx * 0.5f + ny * 0.5f) * 0.4f;
                    light = Mathf.Clamp01(light);

                    // 表面噪声（模拟星球纹理）
                    float noise = (float)(rng.NextDouble() - 0.5) * 0.15f;

                    // 边缘暗化（球体感）
                    float edge = 1f - dist * dist * 0.5f;

                    Color c = baseColor * light * edge;
                    c.r = Mathf.Clamp01(c.r + noise);
                    c.g = Mathf.Clamp01(c.g + noise);
                    c.b = Mathf.Clamp01(c.b + noise);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _planetSpriteCache[key] = sprite;
            return sprite;
        }

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
            GenerateStarfield();

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
            _coordText.text = "星海浩瀚，探索未知";

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
            itRect.anchorMin = Vector2.zero; itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(8, 8); itRect.offsetMax = new Vector2(-8, -8);
            _infoText = infoTextGo.AddComponent<Text>();
            _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _infoText.fontSize = 12;
            _infoText.color = new Color(0.8f, 0.9f, 1f);
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;
            _infoText.text = LocalizedTextManager.getText("sm_ui_select_planet");

            RefreshMap();

            // 初始滚动到中心（主星球位置）
            scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);

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

        /// <summary>生成程序化星空背景（随机星星+星云）</summary>
        private void GenerateStarfield()
        {
            var starfieldGo = new GameObject("Starfield");
            starfieldGo.transform.SetParent(_mapContent, false);
            var sfRect = starfieldGo.AddComponent<RectTransform>();
            sfRect.anchorMin = Vector2.zero;
            sfRect.anchorMax = Vector2.one;
            sfRect.offsetMin = Vector2.zero;
            sfRect.offsetMax = Vector2.zero;
            sfRect.SetAsFirstSibling(); // 放在最底层

            System.Random rng = new System.Random(20240501); // 固定种子，星空分布一致

            // 第一层：远景星星（小、暗、数量多，1200颗）
            for (int i = 0; i < 1200; i++)
            {
                var starGo = new GameObject($"FarStar_{i}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);

                float size = (float)(1 + rng.NextDouble() * 2); // 1-3像素
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 1.5f,
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 1.5f
                );

                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;

                // 远景星星：偏暗，蓝白色为主
                int colorRoll = rng.Next(100);
                if (colorRoll < 80) starImg.color = new Color(0.8f, 0.85f, 1f, (float)(0.2 + rng.NextDouble() * 0.4));
                else if (colorRoll < 95) starImg.color = new Color(1f, 0.95f, 0.8f, (float)(0.2 + rng.NextDouble() * 0.3));
                else starImg.color = new Color(1f, 0.7f, 0.6f, (float)(0.2 + rng.NextDouble() * 0.3));
            }

            // 第二层：近景亮星（大、亮、数量少，300颗）
            for (int i = 0; i < 300; i++)
            {
                var starGo = new GameObject($"NearStar_{i}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);

                float size = (float)(3 + rng.NextDouble() * 5); // 3-8像素
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 1.2f,
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 1.2f
                );

                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;

                // 近景亮星：高亮度，颜色丰富
                int colorRoll = rng.Next(100);
                if (colorRoll < 60) starImg.color = new Color(1f, 1f, 1f, (float)(0.6 + rng.NextDouble() * 0.4));
                else if (colorRoll < 80) starImg.color = new Color(0.7f, 0.85f, 1f, (float)(0.5 + rng.NextDouble() * 0.4));
                else if (colorRoll < 92) starImg.color = new Color(1f, 0.9f, 0.7f, (float)(0.5 + rng.NextDouble() * 0.4));
                else starImg.color = new Color(1f, 0.6f, 0.5f, (float)(0.5 + rng.NextDouble() * 0.3));
            }

            // 生成星云（圆形柔和渐变，12团）
            var nebulaSprite = GetNebulaSprite();
            for (int i = 0; i < 12; i++)
            {
                var nebulaGo = new GameObject($"Nebula_{i}");
                nebulaGo.transform.SetParent(starfieldGo.transform, false);
                var nebulaRect = nebulaGo.AddComponent<RectTransform>();
                nebulaRect.anchorMin = new Vector2(0.5f, 0.5f);
                nebulaRect.anchorMax = new Vector2(0.5f, 0.5f);
                nebulaRect.pivot = new Vector2(0.5f, 0.5f);
                float nebulaSize = (float)(300 + rng.NextDouble() * 600); // 300-900像素
                nebulaRect.sizeDelta = new Vector2(nebulaSize, nebulaSize);
                nebulaRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE,
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE
                );

                var nebulaImg = nebulaGo.AddComponent<Image>();
                nebulaImg.sprite = nebulaSprite;
                nebulaImg.raycastTarget = false;
                // 星云颜色：紫/蓝/青/粉，柔和透明度
                int nebulaColor = rng.Next(4);
                if (nebulaColor == 0) nebulaImg.color = new Color(0.4f, 0.15f, 0.6f, 0.15f); // 紫
                else if (nebulaColor == 1) nebulaImg.color = new Color(0.15f, 0.25f, 0.6f, 0.15f); // 蓝
                else if (nebulaColor == 2) nebulaImg.color = new Color(0.1f, 0.45f, 0.45f, 0.12f); // 青
                else nebulaImg.color = new Color(0.5f, 0.2f, 0.4f, 0.12f); // 粉
            }
        }

        /// <summary>刷新星图，绘制视野范围内的星球节点</summary>
        public void RefreshMap()
        {
            if (_mapContent == null) return;
            // 只销毁星球节点和连线，保留星空背景
            foreach (Transform child in _mapContent)
            {
                if (child.name.StartsWith("Planet_") || child.name.StartsWith("Line_"))
                    Destroy(child.gameObject);
            }
            _planetNodes.Clear();

            var planets = SuperMechStarSystem.GetPlanetsInRange(0, 0, VIEW_RANGE);

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

            // 绘制星球节点
            foreach (var p in planets)
            {
                CreatePlanetNode(p, p.x, -p.y);
            }

            if (_coordText != null)
            {
                int discovered = SuperMechStarSystem.GetDiscoveredPlanets().Count;
                int total = planets.Count;
                _coordText.text = $"已探索星区：{discovered}/{total} 星球  |  拖动星图探索未知星海";
            }
        }

        private void CreatePlanetNode(SuperMechStarSystem.PlanetData p, float x, float y)
        {
            var nodeGo = new GameObject($"Planet_{p.id}");
            nodeGo.transform.SetParent(_mapContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            bool isMain = p.id == SuperMechStarSystem.MAIN_PLANET;
            float size = isMain ? 48 : (p.discovered ? 30 : 24); // 主世界最大，已发现次之，未发现最小
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = new Vector2(x, y);

            // 星球光晕（外层，主世界更大更亮）
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(nodeGo.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
            float glowExpand = isMain ? 20 : 10;
            glowRect.offsetMin = new Vector2(-glowExpand, -glowExpand);
            glowRect.offsetMax = new Vector2(glowExpand, glowExpand);
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.raycastTarget = false;
            glowImg.sprite = GetGlowSprite(); // 圆形径向渐变光晕

            // 星球本体（圆形纹理）
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(nodeGo.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = Vector2.zero; bodyRect.offsetMax = Vector2.zero;
            var bodyImg = bodyGo.AddComponent<Image>();
            bodyImg.raycastTarget = false;

            // 未发现的星球显示为灰色问号
            bool isUnknown = !p.discovered && !isMain;
            Color planetColor;
            int planetSeed = p.id.GetHashCode();

            if (isUnknown)
            {
                planetColor = new Color(0.35f, 0.35f, 0.4f, 0.6f);
                glowImg.color = new Color(0.4f, 0.4f, 0.45f, 0.2f);
                bodyImg.sprite = GetPlanetSprite(planetColor, planetSeed, true);
            }
            else
            {
                planetColor = p.type switch
                {
                    SuperMechStarSystem.PlanetType.Rocky => new Color(0.75f, 0.65f, 0.5f),
                    SuperMechStarSystem.PlanetType.Gas => new Color(0.95f, 0.85f, 0.5f),
                    SuperMechStarSystem.PlanetType.Ice => new Color(0.7f, 0.9f, 1f),
                    SuperMechStarSystem.PlanetType.Lava => new Color(1f, 0.5f, 0.3f),
                    SuperMechStarSystem.PlanetType.Ocean => new Color(0.4f, 0.6f, 1f),
                    SuperMechStarSystem.PlanetType.Desert => new Color(0.95f, 0.85f, 0.55f),
                    SuperMechStarSystem.PlanetType.Forest => new Color(0.4f, 0.8f, 0.5f),
                    _ => Color.gray
                };
                // 已殖民的星球更亮，有明显光晕
                if (p.colonyLevel > 0)
                {
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.5f);
                }
                else
                {
                    // 已发现但未殖民：中等亮度
                    planetColor = new Color(planetColor.r * 0.75f, planetColor.g * 0.75f, planetColor.b * 0.75f, 0.9f);
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.2f);
                }
                // 主星球：金色大光晕
                if (isMain)
                {
                    glowImg.color = new Color(1f, 0.9f, 0.5f, 0.6f);
                    planetColor = new Color(1f, 0.9f, 0.55f);
                }
                bodyImg.sprite = GetPlanetSprite(planetColor, planetSeed);
            }
            _planetNodes[p.id] = nodeGo;

            // 星球名称标签（主世界和已发现的显示）
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
                labelText.fontSize = isMain ? 14 : 11;
                labelText.color = isMain ? new Color(1f, 0.9f, 0.5f) : new Color(0.8f, 0.85f, 0.95f);
                labelText.alignment = TextAnchor.UpperCenter;
                labelText.text = p.name;
                labelText.raycastTarget = false;
            }

            // 按钮（放在最上层接收点击）
            var btn = nodeGo.AddComponent<Button>();
            // 用透明Image作为点击区域
            var hitImg = nodeGo.AddComponent<Image>();
            hitImg.color = new Color(0f, 0f, 0f, 0f);

            // 星球名称（未发现显示?）
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(nodeGo.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0);
            nameRect.anchorMax = new Vector2(0.5f, 0);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.sizeDelta = new Vector2(100, 16);
            nameRect.anchoredPosition = new Vector2(0, -4);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 10;
            if (isUnknown)
            {
                nameText.color = new Color(0.45f, 0.45f, 0.5f);
                nameText.text = "?";
            }
            else
            {
                nameText.color = p.id == SuperMechStarSystem.MAIN_PLANET ?
                    new Color(1f, 0.9f, 0.5f) : new Color(0.75f, 0.85f, 0.95f);
                nameText.text = p.name;
            }
            nameText.alignment = TextAnchor.UpperCenter;

            string pid = p.id;
            btn.onClick.AddListener(() => SelectPlanet(pid));
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
                SuperMechStarSystem.PlanetType.Rocky => LocalizedTextManager.getText("sm_ui_planet_rocky"),
                SuperMechStarSystem.PlanetType.Gas => LocalizedTextManager.getText("sm_ui_planet_gas"),
                SuperMechStarSystem.PlanetType.Ice => LocalizedTextManager.getText("sm_ui_planet_ice"),
                SuperMechStarSystem.PlanetType.Lava => LocalizedTextManager.getText("sm_ui_planet_lava"),
                SuperMechStarSystem.PlanetType.Ocean => LocalizedTextManager.getText("sm_ui_planet_ocean"),
                SuperMechStarSystem.PlanetType.Desert => LocalizedTextManager.getText("sm_ui_planet_desert"),
                SuperMechStarSystem.PlanetType.Forest => LocalizedTextManager.getText("sm_ui_planet_forest"),
                _ => "?"
            };

            _infoText.text =
                $"<color=#ffd700><size=14><b>{p.name}</b></size></color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_type")}:</color> {typeName}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_resource")}:</color> {p.resource:F0}/{p.maxResource:F0}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_colony")}:</color> {p.colonyLevel}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_units")}:</color> {p.units.Count}\n" +
                (p.colonyLevel > 0 ? $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_owner")}:</color> {p.ownerKingdom}\n" : "\n") +
                $"\n<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_coord")}:</color> ({p.x:F0}, {p.y:F0})\n";

            if (!p.discovered && p.id != SuperMechStarSystem.MAIN_PLANET)
            {
                _infoText.text += "\n<color=#ffcc66>" + LocalizedTextManager.getText("sm_ui_planet_uncolonized") + "</color>";
            }
        }
    }
}
