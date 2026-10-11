using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口：显示星系星球，支持航行和殖民</summary>
    public class SuperMechStarView : MonoBehaviour
    {
        private RectTransform _mapContent;
        private Text _infoText;
        private string _selectedPlanet;
        private readonly Dictionary<string, GameObject> _planetNodes = new Dictionary<string, GameObject>();

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
            mapImg.color = new Color(0.02f, 0.03f, 0.08f, 0.95f); // 深空背景

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
            _mapContent.sizeDelta = new Vector2(800, 800);
            var contentHit = contentGo.AddComponent<Image>();
            contentHit.color = new Color(0f, 0f, 0f, 0f);
            contentHit.raycastTarget = true;
            scrollRect.content = _mapContent;
            scrollRect.viewport = vpRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 右侧信息面板
            var infoGo = new GameObject("InfoPanel");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.7f, 0);
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);

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
        }

        /// <summary>刷新星图，绘制星球节点</summary>
        public void RefreshMap()
        {
            if (_mapContent == null) return;
            foreach (Transform child in _mapContent) Destroy(child.gameObject);
            _planetNodes.Clear();

            var planets = SuperMechStarSystem.GetDiscoveredPlanets();
            float centerX = 400, centerY = 400;

            // 绘制连线（主星球到其他星球）
            var main = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (main != null)
            {
                foreach (var p in planets)
                {
                    if (p.id == SuperMechStarSystem.MAIN_PLANET) continue;
                    CreateLine(centerX + main.x, centerY + main.y, centerX + p.x, centerY + p.y);
                }
            }

            // 绘制星球节点
            foreach (var p in planets)
            {
                CreatePlanetNode(p, centerX, centerY);
            }
        }

        private void CreatePlanetNode(SuperMechStarSystem.PlanetData p, float cx, float cy)
        {
            var nodeGo = new GameObject($"Planet_{p.id}");
            nodeGo.transform.SetParent(_mapContent, false);
            var nodeRect = nodeGo.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            float size = p.id == SuperMechStarSystem.MAIN_PLANET ? 40 : 25;
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = new Vector2(cx + p.x, cy + p.y);

            var btn = nodeGo.AddComponent<Button>();
            var img = nodeGo.AddComponent<Image>();
            // 星球颜色按类型
            Color planetColor = p.type switch
            {
                SuperMechStarSystem.PlanetType.Rocky => new Color(0.6f, 0.5f, 0.4f),
                SuperMechStarSystem.PlanetType.Gas => new Color(0.8f, 0.7f, 0.4f),
                SuperMechStarSystem.PlanetType.Ice => new Color(0.6f, 0.8f, 1f),
                SuperMechStarSystem.PlanetType.Lava => new Color(1f, 0.4f, 0.2f),
                SuperMechStarSystem.PlanetType.Ocean => new Color(0.3f, 0.5f, 0.9f),
                SuperMechStarSystem.PlanetType.Desert => new Color(0.9f, 0.8f, 0.5f),
                SuperMechStarSystem.PlanetType.Forest => new Color(0.3f, 0.7f, 0.4f),
                _ => Color.gray
            };
            img.color = p.colonyLevel > 0 ? planetColor : new Color(planetColor.r * 0.5f, planetColor.g * 0.5f, planetColor.b * 0.5f, 0.7f);
            _planetNodes[p.id] = nodeGo;

            // 星球名称
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(nodeGo.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0);
            nameRect.anchorMax = new Vector2(0.5f, 0);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.sizeDelta = new Vector2(80, 16);
            nameRect.anchoredPosition = new Vector2(0, -2);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 10;
            nameText.color = p.id == SuperMechStarSystem.MAIN_PLANET ? new Color(1f, 0.9f, 0.5f) : new Color(0.7f, 0.8f, 0.9f);
            nameText.alignment = TextAnchor.UpperCenter;
            nameText.text = p.name;

            string pid = p.id;
            btn.onClick.AddListener(() => SelectPlanet(pid));
        }

        private void CreateLine(float x1, float y1, float x2, float y2)
        {
            var lineGo = new GameObject("Line");
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
            lineImg.color = new Color(0.3f, 0.4f, 0.6f, 0.3f);
        }

        private void SelectPlanet(string id)
        {
            _selectedPlanet = id;
            var p = SuperMechStarSystem.GetPlanet(id);
            if (p == null) return;

            string typeName = p.type switch
            {
                SuperMechStarSystem.PlanetType.Rocky => LocalizedTextManager.getText("sm_ui_planet_rocky"),
                SuperMechStarSystem.PlanetType.Gas => LocalizedTextManager.getText("sm_ui_planet_gas"),
                SuperMechStarSystem.PlanetType.Ice => LocalizedTextManager.getText("sm_ui_planet_ice"),
                SuperMechStarSystem.PlanetType.Lava => LocalizedTextManager.getText("sm_ui_planet_lava"),
                SuperMechStarSystem.PlanetType.Ocean => LocalizedTextManager.getText("sm_ui_planet_ocean"),
                SuperMechStarSystem.PlanetType.Desert => LocalizedTextManager.getText("sm_ui_planet_desert"),
                SuperMechStarSystem.PlanetType.Forest => LocalizedTextManager.getText("sm_ui_planet_forest"),
                _ => LocalizedTextManager.getText("sm_ui_unknown")
            };

            _infoText.text = $"<b><size=15><color=#ffd700>{p.name}</color></size></b>\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_type")}:</color> {typeName}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_resource")}:</color> {p.resource:F0}/{p.maxResource:F0}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_colony")}:</color> {p.colonyLevel}\n" +
                $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_units")}:</color> {p.units.Count}\n" +
                (p.colonyLevel > 0 ? $"<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_owner")}:</color> {p.ownerKingdom}\n" : "\n") +
                $"\n<color=#8fa8c8>{LocalizedTextManager.getText("sm_ui_planet_coord")}:</color> ({p.x:F0}, {p.y:F0})\n";

            if (p.id != SuperMechStarSystem.MAIN_PLANET && p.colonyLevel == 0)
            {
                _infoText.text += "\n<color=#ffcc66>" + LocalizedTextManager.getText("sm_ui_planet_uncolonized") + "</color>";
            }

            // 高亮选中
            foreach (var kv in _planetNodes)
            {
                var img = kv.Value.GetComponent<Image>();
                if (img != null)
                {
                    bool selected = kv.Key == id;
                    img.color = new Color(img.color.r, img.color.g, img.color.b, selected ? 1f : 0.7f);
                }
            }
        }
    }
}
