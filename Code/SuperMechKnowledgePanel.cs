using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 三层知识面板：系别层 + 知识图谱层 + 知识库层
    /// 参考天人武道神藏系统的三栏结构，中间层用2D知识图谱（后续可升级为3D神经网络）
    /// </summary>
    internal static class SuperMechKnowledgePanel
    {
        private const string ContainerName = "SMKnowledgePanel";
        private const float PanelHeight = 420f;
        private const float HeaderHeight = 32f;
        private const float GraphHeight = 220f;
        private const float LibraryHeight = 160f;

        private static GameObject _container;
        private static Transform _headerLayer;   // 系别切换层
        private static Transform _graphLayer;    // 知识图谱层
        private static Transform _libraryLayer;  // 知识库层
        private static RectTransform _graphContent;
        private static Actor _currentActor;
        private static string _currentPrefix = "mech";
        private static bool _dragging;
        private static Vector2 _dragOffset;
        private static Vector2 _graphOffset = Vector2.zero;
        private static readonly Dictionary<string, GameObject> _nodeObjects = new Dictionary<string, GameObject>();
        private static readonly List<GameObject> _connectionLines = new List<GameObject>();
        private static readonly Color[] TierColors =
        {
            new Color(0.5f, 0.5f, 0.5f),  // 基础 - 灰
            new Color(0.3f, 0.7f, 0.3f),  // 进阶 - 绿
            new Color(0.3f, 0.5f, 0.9f),  // 高端 - 蓝
            new Color(0.7f, 0.3f, 0.8f),  // 尖端 - 紫
            new Color(0.95f, 0.7f, 0.2f)  // 终极 - 金
        };

        internal static void Ensure(Transform parent, Actor actor)
        {
            _currentActor = actor;
            if (_container != null && _container.transform.parent == parent)
            {
                Refresh();
                return;
            }

            if (_container != null)
                UnityEngine.Object.Destroy(_container);

            CreateContainer(parent);
            CreateHeaderLayer();
            CreateGraphLayer();
            CreateLibraryLayer();
            Refresh();
        }

        internal static void Hide()
        {
            if (_container != null) _container.SetActive(false);
        }

        internal static void Show()
        {
            if (_container != null) _container.SetActive(true);
        }

        private static void CreateContainer(Transform parent)
        {
            _container = new GameObject(ContainerName, typeof(RectTransform));
            _container.transform.SetParent(parent, false);
            RectTransform rt = _container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = _container.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(2, 2, 2, 2);

            ContentSizeFitter fitter = _container.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CreateHeaderLayer()
        {
            GameObject header = new GameObject("HeaderLayer", typeof(RectTransform));
            header.transform.SetParent(_container.transform, false);
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.minHeight = HeaderHeight;
            le.preferredHeight = HeaderHeight;
            le.flexibleHeight = 0f;

            HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4f;
            hlg.padding = new RectOffset(4, 4, 2, 2);

            _headerLayer = header.transform;

            // 5个系别按钮（图标+tooltip）
            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] names = { "机械系", "武道系", "异能系", "魔法系", "念力系" };
            string[] icons = {
                "ui/Icons/actor_traits/iconStrong",       // 机械系 - 力量/坚固
                "ui/Icons/actor_traits/iconAgile",        // 武道系 - 敏捷/战斗
                "ui/Icons/actor_traits/iconLightning",    // 异能系 - 闪电/超能力
                "ui/Icons/actor_traits/iconFireBlood",    // 魔法系 - 火焰/魔法
                "ui/Icons/actor_traits/iconStrongMinded"  // 念力系 - 精神/念力
            };
            for (int i = 0; i < prefixes.Length; i++)
            {
                CreateClassButton(prefixes[i], names[i], icons[i]);
            }
        }

        private static void CreateClassButton(string prefix, string name, string iconPath)
        {
            GameObject btnObj = new GameObject("ClassBtn_" + prefix, typeof(RectTransform));
            btnObj.transform.SetParent(_headerLayer, false);
            Button btn = btnObj.AddComponent<Button>();
            Image img = btnObj.AddComponent<Image>();
            img.color = prefix == _currentPrefix
                ? new Color(0.25f, 0.45f, 0.75f, 0.9f)
                : new Color(0.2f, 0.2f, 0.25f, 0.8f);

            // 图标
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4, 4);
            iconRt.offsetMax = new Vector2(-4, -4);
            Image iconImg = iconObj.AddComponent<Image>();
            try { iconImg.sprite = SpriteTextureLoader.getSprite(iconPath); } catch { }
            iconImg.color = Color.white;

            // Tooltip显示系别名
            TipButton tip = btnObj.AddComponent<TipButton>();
            tip.textOnClick = name;

            string p = prefix;
            btn.onClick.AddListener(() =>
            {
                _currentPrefix = p;
                _graphOffset = Vector2.zero;
                UpdateHeaderColors();
                Refresh();
            });
        }

        private static void UpdateHeaderColors()
        {
            for (int i = 0; i < _headerLayer.childCount; i++)
            {
                Transform child = _headerLayer.GetChild(i);
                Image img = child.GetComponent<Image>();
                if (img == null) continue;
                string prefix = child.name.Replace("ClassBtn_", "");
                img.color = prefix == _currentPrefix
                    ? new Color(0.25f, 0.45f, 0.75f, 0.9f)
                    : new Color(0.2f, 0.2f, 0.25f, 0.8f);
            }
        }

        private static void CreateGraphLayer()
        {
            GameObject graph = new GameObject("GraphLayer", typeof(RectTransform));
            graph.transform.SetParent(_container.transform, false);
            LayoutElement le = graph.AddComponent<LayoutElement>();
            le.minHeight = GraphHeight;
            le.preferredHeight = GraphHeight;
            le.flexibleHeight = 0f;

            Image bg = graph.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.1f, 0.9f);

            // 拖拽组件
            GraphDragHandler drag = graph.AddComponent<GraphDragHandler>();
            drag.OnDragDelta = OnGraphDrag;

            // 内容容器（可偏移）
            GameObject content = new GameObject("GraphContent", typeof(RectTransform));
            content.transform.SetParent(graph.transform, false);
            _graphContent = content.GetComponent<RectTransform>();
            _graphContent.anchorMin = Vector2.zero;
            _graphContent.anchorMax = Vector2.one;
            _graphContent.offsetMin = Vector2.zero;
            _graphContent.offsetMax = Vector2.zero;

            _graphLayer = graph.transform;
        }

        private static void OnGraphDrag(Vector2 delta)
        {
            _graphOffset += delta * 0.5f;
            _graphOffset.x = Mathf.Clamp(_graphOffset.x, -200f, 200f);
            _graphOffset.y = Mathf.Clamp(_graphOffset.y, -100f, 100f);
            if (_graphContent != null)
                _graphContent.anchoredPosition = _graphOffset;
        }

        private static void CreateLibraryLayer()
        {
            GameObject lib = new GameObject("LibraryLayer", typeof(RectTransform));
            lib.transform.SetParent(_container.transform, false);
            LayoutElement le = lib.AddComponent<LayoutElement>();
            le.minHeight = LibraryHeight;
            le.preferredHeight = LibraryHeight;
            le.flexibleHeight = 0f;

            Image bg = lib.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            ScrollRect scroll = lib.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 50f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(lib.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(2, 2);
            vpRt.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.3f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0, LibraryHeight);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = cRt;

            _libraryLayer = content.transform;
        }

        internal static void Refresh()
        {
            if (_currentActor == null || !_currentActor.isAlive()) return;

            RefreshGraph();
            RefreshLibrary();
        }

        private static void RefreshGraph()
        {
            // 清除旧节点和连线
            foreach (var kv in _nodeObjects)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            _nodeObjects.Clear();
            foreach (var line in _connectionLines)
                if (line != null) UnityEngine.Object.Destroy(line);
            _connectionLines.Clear();

            if (_graphContent == null) return;

            // 获取当前系别的知识定义
            List<SuperMechKnowledge.KnowledgeDef> defs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            if (defs == null || defs.Count == 0) return;

            // 按阶位分层，球面分布（模拟3D）
            Dictionary<int, List<SuperMechKnowledge.KnowledgeDef>> byTier = new Dictionary<int, List<SuperMechKnowledge.KnowledgeDef>>();
            foreach (var def in defs)
            {
                if (!byTier.ContainsKey(def.tier)) byTier[def.tier] = new List<SuperMechKnowledge.KnowledgeDef>();
                byTier[def.tier].Add(def);
            }

            float graphWidth = _graphContent.rect.width > 0 ? _graphContent.rect.width : 400f;
            float graphHeight = GraphHeight - 20f;
            float centerX = graphWidth / 2f;
            float centerY = graphHeight / 2f;

            // 每层半径不同（终极在中心，基础在外层）
            float[] tierRadius = { 90f, 70f, 50f, 30f, 10f };

            foreach (var tier in byTier.Keys)
            {
                var tierDefs = byTier[tier];
                float radius = tier < tierRadius.Length ? tierRadius[tier] : 20f;
                for (int i = 0; i < tierDefs.Count; i++)
                {
                    var def = tierDefs[i];
                    float angle = (float)i / tierDefs.Count * Mathf.PI * 2f + tier * 0.5f;
                    float x = centerX + Mathf.Cos(angle) * radius;
                    float y = centerY + Mathf.Sin(angle) * radius * 0.7f;
                    CreateKnowledgeNode(def, x, y, tier);
                }
            }

            // 创建阶位关系连线（同阶位相邻节点 + 低阶到高阶的中心连接）
            for (int tier = 0; tier <= 4; tier++)
            {
                var tierNodes = defs.FindAll(d => d.tier == tier);
                for (int i = 0; i < tierNodes.Count; i++)
                {
                    // 同阶位相邻节点连线
                    if (i + 1 < tierNodes.Count)
                    {
                        if (_nodeObjects.ContainsKey(tierNodes[i].id) && _nodeObjects.ContainsKey(tierNodes[i + 1].id))
                            CreateConnectionLine(_nodeObjects[tierNodes[i].id], _nodeObjects[tierNodes[i + 1].id]);
                    }
                    // 低阶到高阶连线（每个节点连接到上一阶位的第一个节点）
                    if (tier > 0)
                    {
                        var prevTierNodes = defs.FindAll(d => d.tier == tier - 1);
                        if (prevTierNodes.Count > 0 && _nodeObjects.ContainsKey(tierNodes[i].id) && _nodeObjects.ContainsKey(prevTierNodes[0].id))
                            CreateConnectionLine(_nodeObjects[prevTierNodes[0].id], _nodeObjects[tierNodes[i].id]);
                    }
                }
            }
        }

        private static void CreateKnowledgeNode(SuperMechKnowledge.KnowledgeDef def, float x, float y, int tier)
        {
            GameObject node = new GameObject("Node_" + def.id, typeof(RectTransform));
            node.transform.SetParent(_graphContent, false);
            RectTransform rt = node.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y - GraphHeight / 2f);
            rt.sizeDelta = new Vector2(28, 28);

            Image img = node.AddComponent<Image>();
            bool unlocked = SuperMechKnowledge.IsUnlocked(_currentActor, def.id);
            int pot = SuperMechPotential.GetPotential(_currentActor);
            int actualCost = SuperMechPotential.GetActualCost(_currentActor, def.id, def.cost);
            bool tierUnlocked = def.tier == 0 || SuperMechKnowledge.GetTierKnowledgeCount(_currentActor, _currentPrefix, def.tier - 1) > 0;
            bool canUnlock = !unlocked && tierUnlocked && pot >= actualCost;
            img.color = unlocked
                ? TierColors[tier]
                : canUnlock
                    ? new Color(TierColors[tier].r * 0.6f, TierColors[tier].g * 0.6f, TierColors[tier].b * 0.6f, 0.8f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // 图标
            if (!string.IsNullOrEmpty(def.icon))
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(node.transform, false);
                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(3, 3);
                iconRt.offsetMax = new Vector2(-3, -3);
                Image iconImg = iconObj.AddComponent<Image>();
                try { iconImg.sprite = SpriteTextureLoader.getSprite(def.icon); } catch { }
                iconImg.color = unlocked ? Color.white : new Color(1, 1, 1, 0.5f);
            }

            // 点击解锁
            Button btn = node.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (SuperMechPotential.UnlockNode(_currentActor, def.id, def.cost))
                {
                    Refresh();
                }
            });

            // Tooltip
            TipButton tip = node.AddComponent<TipButton>();
            tip.textOnClick = def.name;
            tip.textOnClickDescription = def.desc + $"\n消耗: {actualCost}潜能点\n阶位: {GetTierName(def.tier)}";

            _nodeObjects[def.id] = node;
        }

        private static void CreateConnectionLine(GameObject from, GameObject to)
        {
            GameObject line = new GameObject("Line", typeof(RectTransform));
            line.transform.SetParent(_graphContent, false);
            line.transform.SetAsFirstSibling();
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 fromPos = ((RectTransform)from.transform).anchoredPosition;
            Vector2 toPos = ((RectTransform)to.transform).anchoredPosition;
            Vector2 mid = (fromPos + toPos) / 2f;
            float dist = Vector2.Distance(fromPos, toPos);
            float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(dist, 2);
            rt.rotation = Quaternion.Euler(0, 0, angle);

            Image img = line.AddComponent<Image>();
            img.color = new Color(0.4f, 0.5f, 0.7f, 0.4f);

            _connectionLines.Add(line);
        }

        private static void RefreshLibrary()
        {
            if (_libraryLayer == null) return;

            // 清除旧内容
            for (int i = _libraryLayer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_libraryLayer.GetChild(i).gameObject);

            if (_currentActor == null) return;

            List<SuperMechKnowledge.KnowledgeDef> defs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            if (defs == null) return;

            // 按阶位分组
            for (int tier = 0; tier <= 4; tier++)
            {
                var tierDefs = defs.FindAll(d => d.tier == tier);
                if (tierDefs.Count == 0) continue;

                int unlocked = tierDefs.FindAll(d => SuperMechKnowledge.IsUnlocked(_currentActor, d.id)).Count;

                // 分组标题
                GameObject titleObj = new GameObject("TierTitle_" + tier, typeof(RectTransform));
                titleObj.transform.SetParent(_libraryLayer, false);
                LayoutElement titleLe = titleObj.AddComponent<LayoutElement>();
                titleLe.minHeight = 18f;
                titleLe.preferredHeight = 18f;
                Text titleTxt = CreateText(titleObj.transform,
                    $"◆ {GetTierName(tier)}  {unlocked}/{tierDefs.Count}",
                    10, TextAnchor.MiddleLeft);
                titleTxt.color = TierColors[tier];

                // 知识网格
                GameObject gridObj = new GameObject("TierGrid_" + tier, typeof(RectTransform));
                gridObj.transform.SetParent(_libraryLayer, false);
                LayoutElement gridLe = gridObj.AddComponent<LayoutElement>();
                gridLe.minHeight = 36f;
                gridLe.preferredHeight = 36f * Mathf.CeilToInt(tierDefs.Count / 6f);

                GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(32, 32);
                grid.spacing = new Vector2(4, 4);
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.padding = new RectOffset(4, 4, 2, 2);

                foreach (var def in tierDefs)
                {
                    CreateLibraryIcon(gridObj.transform, def);
                }
            }
        }

        private static void CreateLibraryIcon(Transform parent, SuperMechKnowledge.KnowledgeDef def)
        {
            GameObject iconObj = new GameObject("LibIcon_" + def.id, typeof(RectTransform));
            iconObj.transform.SetParent(parent, false);

            Image bg = iconObj.AddComponent<Image>();
            bool unlocked = SuperMechKnowledge.IsUnlocked(_currentActor, def.id);
            int pot = SuperMechPotential.GetPotential(_currentActor);
            int actualCost = SuperMechPotential.GetActualCost(_currentActor, def.id, def.cost);
            bool tierUnlocked = def.tier == 0 || SuperMechKnowledge.GetTierKnowledgeCount(_currentActor, _currentPrefix, def.tier - 1) > 0;
            bool canUnlock = !unlocked && tierUnlocked && pot >= actualCost;
            bg.color = unlocked
                ? TierColors[def.tier]
                : canUnlock
                    ? new Color(TierColors[def.tier].r * 0.5f, TierColors[def.tier].g * 0.5f, TierColors[def.tier].b * 0.5f, 0.7f)
                    : new Color(0.15f, 0.15f, 0.18f, 0.8f);

            if (!string.IsNullOrEmpty(def.icon))
            {
                GameObject inner = new GameObject("Icon", typeof(RectTransform));
                inner.transform.SetParent(iconObj.transform, false);
                RectTransform innerRt = inner.GetComponent<RectTransform>();
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.offsetMin = new Vector2(3, 3);
                innerRt.offsetMax = new Vector2(-3, -3);
                Image innerImg = inner.AddComponent<Image>();
                try { innerImg.sprite = SpriteTextureLoader.getSprite(def.icon); } catch { }
                innerImg.color = unlocked ? Color.white : new Color(1, 1, 1, 0.4f);
            }

            Button btn = iconObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (SuperMechPotential.UnlockNode(_currentActor, def.id, def.cost))
                    Refresh();
            });

            TipButton tip = iconObj.AddComponent<TipButton>();
            tip.textOnClick = def.name;
            tip.textOnClickDescription = def.desc + $"\n消耗: {actualCost}潜能点";
        }

        private static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = LocalizedTextManager.current_font;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.text = content;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static string GetTierName(int tier)
        {
            return tier switch
            {
                0 => "基础知识",
                1 => "进阶知识",
                2 => "高端知识",
                3 => "尖端知识",
                4 => "终极知识",
                _ => "未知"
            };
        }

        /// <summary>图谱拖拽处理组件</summary>
        private class GraphDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public Action<Vector2> OnDragDelta;
            private Vector2 _lastPos;

            public void OnBeginDrag(PointerEventData eventData)
            {
                _lastPos = eventData.position;
            }

            public void OnDrag(PointerEventData eventData)
            {
                Vector2 delta = eventData.position - _lastPos;
                _lastPos = eventData.position;
                OnDragDelta?.Invoke(delta);
            }

            public void OnEndDrag(PointerEventData eventData) { }
        }
    }
}
