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
        private const float HeaderHeight = 36f;
        private const float GraphHeight = 260f;
        private const float LibraryHeight = 184f;

        private static GameObject _container;
        private static Transform _headerLayer;   // 系别切换层
        private static Transform _graphLayer;    // 知识图谱层
        private static RectTransform _graphContent;  // 图谱内容容器
        private static Text _graphTitle;         // 图谱标题
        private static SMKnowledgeGraph3D _graph3D; // 3D知识图谱组件
        private static Transform _libraryLayer;  // 知识库内容层
        private static Text _libraryTitle;       // 知识库标题
        private static Text _libraryProgress;    // 知识库进度（右下角）
        private static Actor _currentActor;
        private static string _currentPrefix = "mech";
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

        private static void CreateContainer(Transform parent)
        {
            _container = new GameObject(ContainerName, typeof(RectTransform));
            _container.transform.SetParent(parent, false);
            RectTransform rt = _container.GetComponent<RectTransform>();
            // 填满父元素（panelHost高度固定480），不用ContentSizeFitter避免布局冲突
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
            // 不添加ContentSizeFitter——容器已填满父元素，再加ContentSizeFitter会与anchorMax=1冲突导致高度为0
        }

        private static void CreateHeaderLayer()
        {
            GameObject header = new GameObject("HeaderLayer", typeof(RectTransform));
            header.transform.SetParent(_container.transform, false);
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.minHeight = HeaderHeight;
            le.preferredHeight = HeaderHeight;
            le.flexibleHeight = 0f;

            // 背景+边框
            AddBoxFrame(header, new Color(0.06f, 0.08f, 0.12f, 0.9f), new Color(0.25f, 0.3f, 0.4f, 0.8f));

            _headerLayer = header.transform;

            // 系别切换小图标（居中）
            GameObject switcherGo = new GameObject("ClassSwitcher", typeof(RectTransform));
            switcherGo.transform.SetParent(header.transform, false);
            HorizontalLayoutGroup swHlg = switcherGo.AddComponent<HorizontalLayoutGroup>();
            swHlg.childAlignment = TextAnchor.MiddleCenter;
            swHlg.childControlWidth = true;
            swHlg.childControlHeight = true;
            swHlg.childForceExpandWidth = false;
            swHlg.childForceExpandHeight = true;
            swHlg.spacing = 8f;
            swHlg.padding = new RectOffset(4, 4, 2, 2);
            RectTransform swRt = switcherGo.GetComponent<RectTransform>();
            swRt.anchorMin = Vector2.zero;
            swRt.anchorMax = Vector2.one;
            swRt.offsetMin = Vector2.zero;
            swRt.offsetMax = Vector2.zero;
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
            tip.textOnClickDescription = string.Empty;
            tip.text_description_2 = string.Empty;

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

            // 更新图谱标题（显示系别+职业阶段）
            if (_graphTitle != null && _currentActor != null)
            {
                string stage = SuperMechStage.GetStageName(_currentActor);
                _graphTitle.text = $"{names[idx]} · {stage}";
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

            // 背景+边框
            AddBoxFrame(graph, new Color(0.03f, 0.04f, 0.08f, 0.95f), new Color(0.2f, 0.25f, 0.35f, 0.8f));

            // 标题栏（"知识图谱"，居中大字，类似天人武道的"隐窍"）
            GameObject titleGo = new GameObject("GraphTitle", typeof(RectTransform));
            titleGo.transform.SetParent(graph.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "知识图谱";
            titleText.fontSize = 14;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.9f, 0.9f, 0.95f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(0, -22);
            titleRt.offsetMax = Vector2.zero;
            _graphTitle = titleText;

            // 图谱内容容器（标题下方）
            GameObject graphContent = new GameObject("GraphContent", typeof(RectTransform));
            graphContent.transform.SetParent(graph.transform, false);
            RectTransform gcRt = graphContent.GetComponent<RectTransform>();
            gcRt.anchorMin = Vector2.zero;
            gcRt.anchorMax = new Vector2(1, 1);
            gcRt.pivot = new Vector2(0.5f, 0.5f);
            gcRt.offsetMin = new Vector2(0, 0);
            gcRt.offsetMax = new Vector2(0, -22);

            // 3D知识图谱组件（球面分布+轴突+神经冲动+拖拽旋转）
            _graph3D = graphContent.AddComponent<SMKnowledgeGraph3D>();

            _graphLayer = graph.transform;
            _graphContent = gcRt;
        }

        private static void CreateLibraryLayer()
        {
            GameObject lib = new GameObject("LibraryLayer", typeof(RectTransform));
            lib.transform.SetParent(_container.transform, false);
            LayoutElement le = lib.AddComponent<LayoutElement>();
            le.minHeight = LibraryHeight;
            le.preferredHeight = LibraryHeight;
            le.flexibleHeight = 0f;

            // 背景+边框
            AddBoxFrame(lib, new Color(0.06f, 0.07f, 0.1f, 0.9f), new Color(0.25f, 0.27f, 0.32f, 0.8f));

            // 标题栏（居中大字，类似天人武道的"隐窍库"）
            GameObject titleGo = new GameObject("LibTitle", typeof(RectTransform));
            titleGo.transform.SetParent(lib.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 22f);
            Image titleBg = titleGo.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);
            titleBg.raycastTarget = false;
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = "知识库";
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.85f, 0.88f, 0.92f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _libraryTitle = titleTxt;

            // 右下角进度（类似天人武道的"41/41"）
            GameObject progressGo = new GameObject("LibProgress", typeof(RectTransform));
            progressGo.transform.SetParent(titleGo.transform, false);
            Text progressTxt = progressGo.AddComponent<Text>();
            progressTxt.fontSize = 10;
            progressTxt.color = new Color(0.6f, 0.7f, 0.8f);
            progressTxt.alignment = TextAnchor.MiddleRight;
            progressTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (progressTxt.font == null) progressTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform progRt = progressGo.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(1, 0);
            progRt.anchorMax = new Vector2(1, 1);
            progRt.pivot = new Vector2(1f, 0.5f);
            progRt.offsetMin = new Vector2(-80, 0);
            progRt.offsetMax = new Vector2(-6, 0);
            _libraryProgress = progressTxt;

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

            try { RefreshHeader(); } catch (System.Exception e) { Debug.LogError($"[超神机械师] 知识面板Header刷新失败: {e.Message}"); }
            try { RefreshGraph(); } catch (System.Exception e) { Debug.LogError($"[超神机械师] 知识面板Graph刷新失败: {e.Message}"); }
            try { RefreshLibrary(); } catch (System.Exception e) { Debug.LogError($"[超神机械师] 知识面板Library刷新失败: {e.Message}"); }
        }

        private static void RefreshGraph()
        {
            if (_graph3D == null || _graphContent == null) return;

            try
            {
                // 使用3D知识图谱（球面分布+轴突+神经冲动+拖拽旋转）
                _graph3D.Init(_currentActor, _currentPrefix, _graphContent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 3D知识图谱刷新失败: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void RefreshLibrary()
        {
            if (_libraryLayer == null) return;

            // 更新标题和进度
            if (_libraryTitle != null && _currentActor != null)
            {
                _libraryTitle.text = "知识库";
            }
            if (_libraryProgress != null && _currentActor != null)
            {
                var allDefs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
                int total = allDefs != null ? allDefs.Count : 0;
                int unlocked = SuperMechKnowledge.GetUnlockedCount(_currentActor, _currentPrefix);
                _libraryProgress.text = $"{unlocked}/{total}";
            }

            // 清除旧内容
            for (int i = _libraryLayer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_libraryLayer.GetChild(i).gameObject);

            if (_currentActor == null) return;

            List<SuperMechKnowledge.KnowledgeDef> defs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            if (defs == null) return;

            // 按阶位分组，每个阶位一个卡片（参考原版知识窗口KnowledgeElement）
            for (int tier = 0; tier <= 4; tier++)
            {
                var tierDefs = defs.FindAll(d => d.tier == tier);
                if (tierDefs.Count == 0) continue;

                int unlocked = tierDefs.FindAll(d => SuperMechKnowledge.IsUnlocked(_currentActor, d.id)).Count;
                CreateTierCard(_libraryLayer, tier, tierDefs, unlocked);
            }
        }

        /// <summary>创建阶位卡片（参考原版知识窗口KnowledgeElement：图标+名称+进度条+展开网格）。</summary>
        private static void CreateTierCard(Transform parent, int tier, List<SuperMechKnowledge.KnowledgeDef> defs, int unlocked)
        {
            Color tierColor = TierColors[tier];

            // 卡片容器
            GameObject card = new GameObject($"TierCard_{tier}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            LayoutElement cardLe = card.AddComponent<LayoutElement>();
            cardLe.minHeight = 36f;
            cardLe.preferredHeight = 36f;
            cardLe.flexibleWidth = 1f;

            // 卡片背景+边框（阶位颜色）
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
            cardBg.raycastTarget = false;

            // 边框（用4个Image模拟1px边框）
            AddCardBorder(card.transform, tierColor);

            // 卡片头部（图标+名称+进度条+展开按钮）
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(card.transform, false);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0, 28f);

            // 阶位图标（左侧）
            GameObject iconGo = new GameObject("TierIcon", typeof(RectTransform));
            iconGo.transform.SetParent(header.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.color = tierColor;
            iconImg.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(16f, 0f);
            iconRt.sizeDelta = new Vector2(14f, 14f);

            // 阶位名称
            Text nameTxt = CreateText(header.transform, GetTierName(tier), 11, TextAnchor.MiddleLeft);
            nameTxt.color = tierColor;
            nameTxt.fontStyle = FontStyle.Bold;
            RectTransform nameRt = nameTxt.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0, 1);
            nameRt.pivot = new Vector2(0, 0.5f);
            nameRt.offsetMin = new Vector2(28f, 0);
            nameRt.offsetMax = new Vector2(100f, 0);

            // 进度条背景
            GameObject barBgGo = new GameObject("ProgressBarBg", typeof(RectTransform));
            barBgGo.transform.SetParent(header.transform, false);
            Image barBg = barBgGo.AddComponent<Image>();
            barBg.color = new Color(0.1f, 0.12f, 0.18f, 0.8f);
            barBg.raycastTarget = false;
            RectTransform barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0, 0.5f);
            barBgRt.anchorMax = new Vector2(1, 0.5f);
            barBgRt.pivot = new Vector2(0.5f, 0.5f);
            barBgRt.offsetMin = new Vector2(100f, -4f);
            barBgRt.offsetMax = new Vector2(-50f, 4f);

            // 进度条填充
            GameObject barFillGo = new GameObject("ProgressBarFill", typeof(RectTransform));
            barFillGo.transform.SetParent(barBgGo.transform, false);
            Image barFill = barFillGo.AddComponent<Image>();
            barFill.color = tierColor;
            barFill.raycastTarget = false;
            RectTransform barFillRt = barFillGo.GetComponent<RectTransform>();
            barFillRt.anchorMin = new Vector2(0, 0);
            barFillRt.anchorMax = new Vector2(0, 1);
            barFillRt.pivot = new Vector2(0, 0.5f);
            float progress = defs.Count > 0 ? (float)unlocked / defs.Count : 0f;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = new Vector2(barBgRt.rect.width * progress, 0);

            // 进度文字（右侧）
            Text progressTxt = CreateText(header.transform, $"{unlocked}/{defs.Count}", 9, TextAnchor.MiddleRight);
            progressTxt.color = new Color(0.7f, 0.75f, 0.8f);
            RectTransform progRt = progressTxt.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(1, 0);
            progRt.anchorMax = new Vector2(1, 1);
            progRt.pivot = new Vector2(1, 0.5f);
            progRt.offsetMin = new Vector2(-46f, 0);
            progRt.offsetMax = new Vector2(-6f, 0);

            // 知识网格（默认展开）
            GameObject gridObj = new GameObject("KnowledgeGrid", typeof(RectTransform));
            gridObj.transform.SetParent(card.transform, false);
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0, 0);
            gridRt.anchorMax = new Vector2(1, 0);
            gridRt.pivot = new Vector2(0.5f, 0f);
            gridRt.sizeDelta = new Vector2(0, Mathf.CeilToInt(defs.Count / 6f) * 34f + 6f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(28, 28);
            grid.spacing = new Vector2(3, 3);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(6, 6, 4, 4);

            foreach (var def in defs)
            {
                CreateLibraryIcon(gridObj.transform, def);
            }

            // 更新卡片高度（头部+网格）
            cardLe.preferredHeight = 28f + gridRt.sizeDelta.y;
        }

        /// <summary>给卡片添加1px边框（4个Image模拟）。</summary>
        private static void AddCardBorder(Transform parent, Color color)
        {
            // 上
            GameObject top = new GameObject("BorderTop", typeof(RectTransform));
            top.transform.SetParent(parent, false);
            Image topImg = top.AddComponent<Image>();
            topImg.color = color;
            topImg.raycastTarget = false;
            RectTransform topRt = top.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0, 1f);

            // 下
            GameObject bottom = new GameObject("BorderBottom", typeof(RectTransform));
            bottom.transform.SetParent(parent, false);
            Image bottomImg = bottom.AddComponent<Image>();
            bottomImg.color = color;
            bottomImg.raycastTarget = false;
            RectTransform bottomRt = bottom.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(0, 1f);

            // 左
            GameObject left = new GameObject("BorderLeft", typeof(RectTransform));
            left.transform.SetParent(parent, false);
            Image leftImg = left.AddComponent<Image>();
            leftImg.color = color;
            leftImg.raycastTarget = false;
            RectTransform leftRt = left.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0f, 0.5f);
            leftRt.sizeDelta = new Vector2(1f, 0);

            // 右
            GameObject right = new GameObject("BorderRight", typeof(RectTransform));
            right.transform.SetParent(parent, false);
            Image rightImg = right.AddComponent<Image>();
            rightImg.color = color;
            rightImg.raycastTarget = false;
            RectTransform rightRt = right.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.sizeDelta = new Vector2(1f, 0);
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
            tip.textOnClick = LocalizedTextManager.getText(def.name);
            tip.textOnClickDescription = def.desc + $"\n消耗: {actualCost}潜能点";
            tip.text_description_2 = string.Empty;
        }

        /// <summary>给GameObject添加背景+1px边框（参考原版物品栏分类框样式）。</summary>
        private static void AddBoxFrame(GameObject go, Color bgColor, Color borderColor)
        {
            // 背景
            Image bg = go.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            // 边框（用4个1px的Image模拟）
            // 上边框
            GameObject top = new GameObject("BorderTop", typeof(RectTransform));
            top.transform.SetParent(go.transform, false);
            Image topImg = top.AddComponent<Image>();
            topImg.color = borderColor;
            topImg.raycastTarget = false;
            RectTransform topRt = top.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0, 1f);
            topRt.offsetMin = Vector2.zero;
            topRt.offsetMax = Vector2.zero;

            // 下边框
            GameObject bottom = new GameObject("BorderBottom", typeof(RectTransform));
            bottom.transform.SetParent(go.transform, false);
            Image bottomImg = bottom.AddComponent<Image>();
            bottomImg.color = borderColor;
            bottomImg.raycastTarget = false;
            RectTransform bottomRt = bottom.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.sizeDelta = new Vector2(0, 1f);
            bottomRt.offsetMin = Vector2.zero;
            bottomRt.offsetMax = Vector2.zero;

            // 左边框
            GameObject left = new GameObject("BorderLeft", typeof(RectTransform));
            left.transform.SetParent(go.transform, false);
            Image leftImg = left.AddComponent<Image>();
            leftImg.color = borderColor;
            leftImg.raycastTarget = false;
            RectTransform leftRt = left.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0f, 0.5f);
            leftRt.sizeDelta = new Vector2(1f, 0);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;

            // 右边框
            GameObject right = new GameObject("BorderRight", typeof(RectTransform));
            right.transform.SetParent(go.transform, false);
            Image rightImg = right.AddComponent<Image>();
            rightImg.color = borderColor;
            rightImg.raycastTarget = false;
            RectTransform rightRt = right.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.sizeDelta = new Vector2(1f, 0);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
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
