using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口：无限星海，程序化星空背景+圆形星球+光晕</summary>
    public class SuperMechStarView : MonoBehaviour
    {
        private RectTransform _mapContent;
        private Text _infoText;
        private Text _coordText;
        private string _selectedPlanet;
        private readonly Dictionary<string, GameObject> _planetNodes = new Dictionary<string, GameObject>();
        private const float MAP_SIZE = 2400f;
        private const float VIEW_RANGE = 1200f;
        private const int STAR_COUNT = 800; // 星空背景星星数量
        private static readonly Dictionary<string, Sprite> _planetSpriteCache = new Dictionary<string, Sprite>();
        private static Sprite _glowSprite;

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

        void Awake()
        {
            try { BuildUI(); SuperMechStarSystem.Init(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] StarView初始化失败: " + e.Message); }
        }

        private void BuildUI()
        {
            // 星图区域（左侧70%）
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(transform, false);
            var mapRect = mapGo.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = new Vector2(0.7f, 1);
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = Vector2.zero;
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

            // 右侧信息面板
            var infoGo = new GameObject("InfoPanel");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.7f, 0);
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);
            infoImg.raycastTarget = false;

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

            // 生成星星（不同大小、亮度、颜色）
            for (int i = 0; i < STAR_COUNT; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);

                float size = rng.Next(1, 4); // 1-3像素
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE,
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE
                );

                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;

                // 星星颜色：白/蓝/黄/红，大部分白色
                int colorRoll = rng.Next(100);
                if (colorRoll < 70) starImg.color = new Color(1f, 1f, 1f, (float)(0.3 + rng.NextDouble() * 0.7));
                else if (colorRoll < 85) starImg.color = new Color(0.7f, 0.8f, 1f, (float)(0.3 + rng.NextDouble() * 0.6));
                else if (colorRoll < 95) starImg.color = new Color(1f, 0.9f, 0.7f, (float)(0.3 + rng.NextDouble() * 0.6));
                else starImg.color = new Color(1f, 0.6f, 0.5f, (float)(0.3 + rng.NextDouble() * 0.5));
            }

            // 生成几团星云（大的半透明圆形）
            for (int i = 0; i < 8; i++)
            {
                var nebulaGo = new GameObject($"Nebula_{i}");
                nebulaGo.transform.SetParent(starfieldGo.transform, false);
                var nebulaRect = nebulaGo.AddComponent<RectTransform>();
                nebulaRect.anchorMin = new Vector2(0.5f, 0.5f);
                nebulaRect.anchorMax = new Vector2(0.5f, 0.5f);
                nebulaRect.pivot = new Vector2(0.5f, 0.5f);
                float nebulaSize = rng.Next(200, 500);
                nebulaRect.sizeDelta = new Vector2(nebulaSize, nebulaSize);
                nebulaRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 0.8f,
                    (float)(rng.NextDouble() - 0.5) * MAP_SIZE * 0.8f
                );

                var nebulaImg = nebulaGo.AddComponent<Image>();
                nebulaImg.raycastTarget = false;
                // 星云颜色：紫/蓝/青，低透明度
                int nebulaColor = rng.Next(3);
                if (nebulaColor == 0) nebulaImg.color = new Color(0.3f, 0.1f, 0.5f, 0.08f);
                else if (nebulaColor == 1) nebulaImg.color = new Color(0.1f, 0.2f, 0.5f, 0.08f);
                else nebulaImg.color = new Color(0.1f, 0.4f, 0.4f, 0.06f);
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
            float size = p.id == SuperMechStarSystem.MAIN_PLANET ? 36 : 22;
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = new Vector2(x, y);

            // 星球光晕（外层）
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(nodeGo.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-8, -8); glowRect.offsetMax = new Vector2(8, 8);
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
            bool isUnknown = !p.discovered && p.id != SuperMechStarSystem.MAIN_PLANET;
            Color planetColor;
            int planetSeed = p.id.GetHashCode();

            if (isUnknown)
            {
                planetColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);
                glowImg.color = new Color(0.3f, 0.3f, 0.35f, 0.15f);
                bodyImg.sprite = GetPlanetSprite(planetColor, planetSeed, true);
            }
            else
            {
                planetColor = p.type switch
                {
                    SuperMechStarSystem.PlanetType.Rocky => new Color(0.65f, 0.55f, 0.45f),
                    SuperMechStarSystem.PlanetType.Gas => new Color(0.85f, 0.75f, 0.45f),
                    SuperMechStarSystem.PlanetType.Ice => new Color(0.65f, 0.85f, 1f),
                    SuperMechStarSystem.PlanetType.Lava => new Color(1f, 0.45f, 0.25f),
                    SuperMechStarSystem.PlanetType.Ocean => new Color(0.35f, 0.55f, 0.95f),
                    SuperMechStarSystem.PlanetType.Desert => new Color(0.9f, 0.8f, 0.5f),
                    SuperMechStarSystem.PlanetType.Forest => new Color(0.35f, 0.75f, 0.45f),
                    _ => Color.gray
                };
                // 已殖民的星球更亮，未殖民的暗一些
                if (p.colonyLevel > 0)
                {
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.4f);
                }
                else
                {
                    planetColor = new Color(planetColor.r * 0.6f, planetColor.g * 0.6f, planetColor.b * 0.6f, 0.8f);
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.15f);
                }
                // 主星球金色光晕
                if (p.id == SuperMechStarSystem.MAIN_PLANET)
                {
                    glowImg.color = new Color(1f, 0.85f, 0.4f, 0.5f);
                    planetColor = new Color(0.9f, 0.8f, 0.5f);
                }
                bodyImg.sprite = GetPlanetSprite(planetColor, planetSeed);
            }
            _planetNodes[p.id] = nodeGo;

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
