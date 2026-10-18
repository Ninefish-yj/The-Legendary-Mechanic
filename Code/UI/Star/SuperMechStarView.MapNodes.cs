using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口 - 地图节点渲染部分（RefreshMap/CreatePlanetNode/CreateVoyageNode/CreateLine/CreateOrbitLine/CreateClusterMarker）</summary>
    public partial class SuperMechStarView : MonoBehaviour
    {
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
            labelText.font = SuperMechUiSkin.DefaultFont;
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
                    // v0.224 未殖民地行星保持原色，不再×0.75变暗
                    glowImg.color = new Color(planetColor.r, planetColor.g, planetColor.b, 0.25f);
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

            // v0.232 点击交互：透明Image作为点击区域+Button绑定选中事件
            var hitGo = new GameObject("HitArea");
            hitGo.transform.SetParent(nodeGo.transform, false);
            var hitRect = hitGo.AddComponent<RectTransform>();
            hitRect.anchorMin = Vector2.zero; hitRect.anchorMax = Vector2.one;
            hitRect.offsetMin = new Vector2(-10, -10); // 点击区域比视觉大10px，更容易点中
            hitRect.offsetMax = new Vector2(10, 10);
            var hitImg = hitGo.AddComponent<Image>();
            hitImg.color = new Color(0, 0, 0, 0); // 完全透明
            hitImg.raycastTarget = true;
            var hitBtn = hitGo.AddComponent<Button>();
            string planetId = p.id;
            hitBtn.onClick.AddListener(() => SelectPlanet(planetId));

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
                        shipLabelText.font = SuperMechUiSkin.DefaultFont;
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
            labelText.font = SuperMechUiSkin.DefaultFont;
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
                hintText.font = SuperMechUiSkin.DefaultFont;
                hintText.fontSize = 11;
                hintText.color = new Color(0.8f, 0.7f, 0.5f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = LocalizedTextManager.getText("sm_ui_cluster_hint");
                hintText.raycastTarget = false;
            }
        }
    }
}
