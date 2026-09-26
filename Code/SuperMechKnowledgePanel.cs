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
        private const float PanelHeight = 480f;
        private const float HeaderHeight = 56f;
        private const float GraphHeight = 240f;
        private const float LibraryHeight = 180f;

        private static GameObject _container;
        private static Transform _headerLayer;   // 系别切换层
        private static Transform _graphLayer;    // 知识图谱层
        private static SMKnowledgeGraph3D _graph3D; // 3D知识图谱组件
        private static Transform _libraryLayer;  // 知识库内容层
        private static Text _libraryTitle;       // 知识库标题
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

        private static Text _headerClassName;
        private static Text _headerStage;
        private static Text _headerBranch;
        private static Text _headerPotential;
        private static Text _headerProgress;

        private static void CreateHeaderLayer()
        {
            GameObject header = new GameObject("HeaderLayer", typeof(RectTransform));
            header.transform.SetParent(_container.transform, false);
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.minHeight = HeaderHeight;
            le.preferredHeight = HeaderHeight;
            le.flexibleHeight = 0f;

            // 背景
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
            bg.raycastTarget = false;

            _headerLayer = header.transform;

            // 左侧：系别图标+名称
            GameObject leftGo = new GameObject("Left", typeof(RectTransform));
            leftGo.transform.SetParent(header.transform, false);
            HorizontalLayoutGroup leftHlg = leftGo.AddComponent<HorizontalLayoutGroup>();
            leftHlg.childAlignment = TextAnchor.MiddleLeft;
            leftHlg.childControlWidth = true;
            leftHlg.childControlHeight = true;
            leftHlg.childForceExpandWidth = false;
            leftHlg.childForceExpandHeight = true;
            leftHlg.spacing = 6f;
            leftHlg.padding = new RectOffset(6, 0, 4, 4);
            RectTransform leftRt = leftGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.4f, 1);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;

            // 系别图标
            GameObject iconGo = new GameObject("ClassIcon", typeof(RectTransform));
            iconGo.transform.SetParent(leftGo.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(32, 32);
            _headerClassIcon = iconImg;

            // 系别名称+知识树名
            GameObject nameGo = new GameObject("ClassName", typeof(RectTransform));
            nameGo.transform.SetParent(leftGo.transform, false);
            Text nameText = nameGo.AddComponent<Text>();
            nameText.fontSize = 13;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(1f, 0.84f, 0f);
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (nameText.font == null) nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _headerClassName = nameText;

            // 中间：职业阶段+分支
            GameObject midGo = new GameObject("Middle", typeof(RectTransform));
            midGo.transform.SetParent(header.transform, false);
            VerticalLayoutGroup midVlg = midGo.AddComponent<VerticalLayoutGroup>();
            midVlg.childAlignment = TextAnchor.MiddleLeft;
            midVlg.childControlWidth = true;
            midVlg.childControlHeight = true;
            midVlg.childForceExpandWidth = true;
            midVlg.childForceExpandHeight = false;
            midVlg.spacing = 1f;
            midVlg.padding = new RectOffset(4, 4, 2, 2);
            RectTransform midRt = midGo.GetComponent<RectTransform>();
            midRt.anchorMin = new Vector2(0.4f, 0);
            midRt.anchorMax = new Vector2(0.7f, 1);
            midRt.offsetMin = Vector2.zero;
            midRt.offsetMax = Vector2.zero;

            Text stageText = midGo.AddComponent<Text>();
            stageText.fontSize = 11;
            stageText.color = new Color(0.8f, 0.9f, 1f);
            stageText.alignment = TextAnchor.MiddleLeft;
            stageText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (stageText.font == null) stageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _headerStage = stageText;

            GameObject branchGo = new GameObject("Branch", typeof(RectTransform));
            branchGo.transform.SetParent(midGo.transform, false);
            Text branchText = branchGo.AddComponent<Text>();
            branchText.fontSize = 10;
            branchText.color = new Color(0.6f, 0.7f, 0.8f);
            branchText.alignment = TextAnchor.MiddleLeft;
            branchText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (branchText.font == null) branchText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _headerBranch = branchText;

            // 右侧：潜能点+进度
            GameObject rightGo = new GameObject("Right", typeof(RectTransform));
            rightGo.transform.SetParent(header.transform, false);
            VerticalLayoutGroup rightVlg = rightGo.AddComponent<VerticalLayoutGroup>();
            rightVlg.childAlignment = TextAnchor.MiddleRight;
            rightVlg.childControlWidth = true;
            rightVlg.childControlHeight = true;
            rightVlg.childForceExpandWidth = true;
            rightVlg.childForceExpandHeight = false;
            rightVlg.spacing = 1f;
            rightVlg.padding = new RectOffset(4, 6, 2, 2);
            RectTransform rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.7f, 0);
            rightRt.anchorMax = new Vector2(1f, 1);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;

            Text potText = rightGo.AddComponent<Text>();
            potText.fontSize = 11;
            potText.color = new Color(0.5f, 1f, 0.6f);
            potText.alignment = TextAnchor.MiddleRight;
            potText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (potText.font == null) potText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _headerPotential = potText;

            GameObject progGo = new GameObject("Progress", typeof(RectTransform));
            progGo.transform.SetParent(rightGo.transform, false);
            Text progText = progGo.AddComponent<Text>();
            progText.fontSize = 10;
            progText.color = new Color(0.7f, 0.7f, 0.7f);
            progText.alignment = TextAnchor.MiddleRight;
            progText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (progText.font == null) progText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _headerProgress = progText;

            // 底部：系别切换小图标
            GameObject switcherGo = new GameObject("ClassSwitcher", typeof(RectTransform));
            switcherGo.transform.SetParent(header.transform, false);
            HorizontalLayoutGroup swHlg = switcherGo.AddComponent<HorizontalLayoutGroup>();
            swHlg.childAlignment = TextAnchor.MiddleCenter;
            swHlg.childControlWidth = true;
            swHlg.childControlHeight = true;
            swHlg.childForceExpandWidth = true;
            swHlg.childForceExpandHeight = true;
            swHlg.spacing = 2f;
            swHlg.padding = new RectOffset(2, 2, 0, 0);
            RectTransform swRt = switcherGo.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0, 0);
            swRt.anchorMax = new Vector2(1, 0);
            swRt.pivot = new Vector2(0.5f, 0f);
            swRt.sizeDelta = new Vector2(0, 16f);
            _classSwitcher = switcherGo.transform;

            // 5个系别切换按钮
            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] names = { "机械系", "武道系", "异能系", "魔法系", "念力系" };
            string[] icons = {
                "ui/Icons/actor_traits/iconStrong",
                "ui/Icons/actor_traits/iconAgile",
                "ui/Icons/actor_traits/iconLightning",
                "ui/Icons/actor_traits/iconFireBlood",
                "ui/Icons/actor_traits/iconStrongMinded"
            };
            for (int i = 0; i < prefixes.Length; i++)
            {
                CreateClassSwitchButton(prefixes[i], names[i], icons[i]);
            }
        }

        private static Image _headerClassIcon;
        private static Transform _classSwitcher;

        private static void CreateClassSwitchButton(string prefix, string name, string iconPath)
        {
            GameObject btnObj = new GameObject("SwitchBtn_" + prefix, typeof(RectTransform));
            btnObj.transform.SetParent(_classSwitcher, false);
            Button btn = btnObj.AddComponent<Button>();
            Image img = btnObj.AddComponent<Image>();
            img.color = prefix == _currentPrefix
                ? new Color(0.25f, 0.45f, 0.75f, 0.9f)
                : new Color(0.15f, 0.15f, 0.2f, 0.6f);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(1, 1);
            iconRt.offsetMax = new Vector2(-1, -1);
            Image iconImg = iconObj.AddComponent<Image>();
            try { iconImg.sprite = SpriteTextureLoader.getSprite(iconPath); } catch { }
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            TipButton tip = btnObj.AddComponent<TipButton>();
            tip.textOnClick = name;

            string p = prefix;
            btn.onClick.AddListener(() =>
            {
                _currentPrefix = p;
                UpdateSwitcherColors();
                Refresh();
            });
        }

        private static void UpdateSwitcherColors()
        {
            if (_classSwitcher == null) return;
            for (int i = 0; i < _classSwitcher.childCount; i++)
            {
                Transform child = _classSwitcher.GetChild(i);
                Image img = child.GetComponent<Image>();
                if (img == null) continue;
                string prefix = child.name.Replace("SwitchBtn_", "");
                img.color = prefix == _currentPrefix
                    ? new Color(0.25f, 0.45f, 0.75f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.6f);
            }
        }

        /// <summary>刷新头部信息栏。</summary>
        private static void RefreshHeader()
        {
            if (_currentActor == null) return;

            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] names = { "机械系", "武道系", "异能系", "魔法系", "念力系" };
            string[] treeNames = { "机械知识树", "御气技巧树", "基因树", "魔法知识树", "精神修炼树" };
            string[] icons = {
                "ui/Icons/actor_traits/iconStrong",
                "ui/Icons/actor_traits/iconAgile",
                "ui/Icons/actor_traits/iconLightning",
                "ui/Icons/actor_traits/iconFireBlood",
                "ui/Icons/actor_traits/iconStrongMinded"
            };

            int idx = System.Array.IndexOf(prefixes, _currentPrefix);
            if (idx < 0) idx = 0;

            // 系别图标
            if (_headerClassIcon != null)
            {
                try { _headerClassIcon.sprite = SpriteTextureLoader.getSprite(icons[idx]); } catch { }
            }

            // 系别名称+知识树名
            if (_headerClassName != null)
                _headerClassName.text = $"{names[idx]} · {treeNames[idx]}";

            // 职业阶段
            if (_headerStage != null)
            {
                string stage = SuperMechStage.GetStageName(_currentActor);
                _headerStage.text = $"职业: {stage}";
            }

            // 分支
            if (_headerBranch != null)
            {
                string branch = SuperMechBranch.GetBranchName(_currentActor);
                _headerBranch.text = string.IsNullOrEmpty(branch) ? "分支: 未选择" : $"分支: {branch}";
            }

            // 潜能点
            if (_headerPotential != null)
            {
                int pot = SuperMechPotential.GetPotential(_currentActor);
                _headerPotential.text = $"潜能点: {pot}";
            }

            // 进度
            if (_headerProgress != null)
            {
                var allDefs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
                int total = allDefs != null ? allDefs.Count : 0;
                int unlocked = SuperMechKnowledge.GetUnlockedCount(_currentActor, _currentPrefix);
                _headerProgress.text = $"进度: {unlocked}/{total}";
            }

            UpdateSwitcherColors();
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
            bg.color = new Color(0.03f, 0.04f, 0.08f, 0.95f);

            // 3D知识图谱组件（球面分布+轴突+神经冲动+拖拽旋转）
            _graph3D = graph.AddComponent<SMKnowledgeGraph3D>();

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
            bg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
            bg.raycastTarget = false;

            // 标题栏
            GameObject titleGo = new GameObject("LibTitle", typeof(RectTransform));
            titleGo.transform.SetParent(lib.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 20f);
            Image titleBg = titleGo.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);
            titleBg.raycastTarget = false;
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.fontSize = 11;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.8f, 0.85f, 0.9f);
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectOffset titlePadding = new RectOffset(8, 8, 0, 0);
            _libraryTitle = titleTxt;

            // 滚动区域
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
            vpRt.offsetMax = new Vector2(-2, 22); // 顶部留出标题栏空间
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.2f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0, LibraryHeight - 24f);

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

            RefreshHeader();
            RefreshGraph();
            RefreshLibrary();
        }

        private static void RefreshGraph()
        {
            if (_graph3D == null || _graphLayer == null) return;

            // 使用3D知识图谱（球面分布+轴突+神经冲动+拖拽旋转）
            _graph3D.Init(_currentActor, _currentPrefix, _graphLayer);
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

            // 更新标题
            if (_libraryTitle != null && _currentActor != null)
            {
                var allDefs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
                int total = allDefs != null ? allDefs.Count : 0;
                int unlocked = SuperMechKnowledge.GetUnlockedCount(_currentActor, _currentPrefix);
                _libraryTitle.text = $"◆ 知识库  {unlocked}/{total}  （点击图标解锁知识）";
            }

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
