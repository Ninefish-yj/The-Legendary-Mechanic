using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal static class SuperMechKnowledgePanel
    {
        private const string ContainerName = "SMKnowledgePanel";
        private const float PanelHeight = 480f;
        private const float HeaderHeight = 36f;
        private const float GraphHeight = 160f;
        private const float LibraryHeight = 280f;

        private static GameObject _container;
        private static Transform _headerLayer;
        private static Transform _graphLayer;
        private static RectTransform _graphContent;
        private static Text _graphTitle;
        private static SMKnowledgeGraph3D _graph3D;
        private static Transform _libraryLayer;
        private static Text _libraryTitle;
        private static Text _libraryProgress;
        private static Actor _currentActor;
        private static string _currentPrefix = "mech";
        private static readonly Color[] TierColors =
        {
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.3f, 0.7f, 0.3f),
            new Color(0.3f, 0.5f, 0.9f),
            new Color(0.7f, 0.3f, 0.8f),
            new Color(0.95f, 0.7f, 0.2f)
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

            ContentSizeFitter containerFitter = _container.AddComponent<ContentSizeFitter>();
            containerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CreateHeaderLayer()
        {
            GameObject header = new GameObject("HeaderLayer", typeof(RectTransform));
            header.transform.SetParent(_container.transform, false);
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.minHeight = HeaderHeight;
            le.preferredHeight = HeaderHeight;
            le.flexibleHeight = 0f;

            AddBoxFrame(header, new Color(0.06f, 0.08f, 0.12f, 0.9f), new Color(0.25f, 0.3f, 0.4f, 0.8f));

            _headerLayer = header.transform;

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

            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] names = { "sm_knowledgepanel_778", "sm_knowledgepanel_779", "sm_knowledgepanel_780", "sm_knowledgepanel_781", "sm_knowledgepanel_782" };
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
            tip.textOnClick = LocalizedTextManager.getText(name);
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

        private static void RefreshHeader()
        {
            if (_currentActor == null) return;

            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] names = { "sm_knowledgepanel_778", "sm_knowledgepanel_779", "sm_knowledgepanel_780", "sm_knowledgepanel_781", "sm_knowledgepanel_782" };
            string[] treeNames = { "sm_knowledgepanel_783", "sm_knowledgepanel_784", "sm_knowledgepanel_785", "sm_knowledgepanel_786", "sm_knowledgepanel_787" };
            string[] icons = {
                "ui/Icons/actor_traits/iconStrong",
                "ui/Icons/actor_traits/iconAgile",
                "ui/Icons/actor_traits/iconLightning",
                "ui/Icons/actor_traits/iconFireBlood",
                "ui/Icons/actor_traits/iconStrongMinded"
            };

            int idx = System.Array.IndexOf(prefixes, _currentPrefix);
            if (idx < 0) idx = 0;

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

            AddBoxFrame(graph, new Color(0.03f, 0.04f, 0.08f, 0.95f), new Color(0.2f, 0.25f, 0.35f, 0.8f));

            GameObject titleGo = new GameObject("GraphTitle", typeof(RectTransform));
            titleGo.transform.SetParent(graph.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = LocalizedTextManager.getText("sm_knowledgepanel_788");
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

            GameObject graphContent = new GameObject("GraphContent", typeof(RectTransform));
            graphContent.transform.SetParent(graph.transform, false);
            RectTransform gcRt = graphContent.GetComponent<RectTransform>();
            gcRt.anchorMin = Vector2.zero;
            gcRt.anchorMax = new Vector2(1, 1);
            gcRt.pivot = new Vector2(0.5f, 0.5f);
            gcRt.offsetMin = new Vector2(0, 0);
            gcRt.offsetMax = new Vector2(0, -22);

            _graph3D = graphContent.AddComponent<SMKnowledgeGraph3D>();

            _graphLayer = graph.transform;
            _graphContent = gcRt;
        }

        private static void CreateLibraryLayer()
        {
            if (_container == null) return;
            GameObject lib = new GameObject("LibraryLayer", typeof(RectTransform));
            lib.transform.SetParent(_container.transform, false);
            LayoutElement le = lib.AddComponent<LayoutElement>();
            le.minHeight = 0f;
            le.preferredHeight = -1f;
            le.flexibleHeight = 0f;

            AddBoxFrame(lib, new Color(0.06f, 0.07f, 0.1f, 0.9f), new Color(0.25f, 0.27f, 0.32f, 0.8f));

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
            titleTxt.text = LocalizedTextManager.getText("sm_knowledgepanel_789");
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.85f, 0.88f, 0.92f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _libraryTitle = titleTxt;

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

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(lib.transform, false);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 0);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = new Vector2(4, 4);
            cRt.offsetMax = new Vector2(-4, -26);

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(2, 2, 2, 2);

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

            if (_libraryTitle != null && _currentActor != null)
            {
                _libraryTitle.text = LocalizedTextManager.getText("sm_knowledgepanel_789");
            }
            if (_libraryProgress != null && _currentActor != null)
            {
                var allDefs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
                int total = allDefs != null ? allDefs.Count : 0;
                int unlocked = SuperMechKnowledge.GetUnlockedCount(_currentActor, _currentPrefix);
                _libraryProgress.text = $"{unlocked}/{total}";
            }

            for (int i = _libraryLayer.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_libraryLayer.GetChild(i).gameObject);

            if (_currentActor == null) return;

            List<SuperMechKnowledge.KnowledgeDef> defs = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            if (defs == null) return;

            for (int tier = 0; tier <= 4; tier++)
            {
                var tierDefs = defs.FindAll(d => d.tier == tier);
                if (tierDefs.Count == 0) continue;

                int unlocked = tierDefs.FindAll(d => SuperMechKnowledge.IsUnlocked(_currentActor, d.id)).Count;
                CreateTierCard(_libraryLayer, tier, tierDefs, unlocked);
            }
        }

        private static void CreateTierCard(Transform parent, int tier, List<SuperMechKnowledge.KnowledgeDef> defs, int unlocked)
        {
            Color tierColor = TierColors[tier];

            GameObject card = new GameObject($"TierCard_{tier}", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            LayoutElement cardLe = card.AddComponent<LayoutElement>();
            cardLe.minHeight = 36f;
            cardLe.preferredHeight = 36f;
            cardLe.flexibleWidth = 1f;

            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
            cardBg.raycastTarget = false;

            AddCardBorder(card.transform, tierColor);

            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(card.transform, false);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0, 28f);

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

            Text nameTxt = CreateText(header.transform, GetTierName(tier), 11, TextAnchor.MiddleLeft);
            nameTxt.color = tierColor;
            nameTxt.fontStyle = FontStyle.Bold;
            RectTransform nameRt = nameTxt.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0, 1);
            nameRt.pivot = new Vector2(0, 0.5f);
            nameRt.offsetMin = new Vector2(28f, 0);
            nameRt.offsetMax = new Vector2(100f, 0);

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

            Text progressTxt = CreateText(header.transform, $"{unlocked}/{defs.Count}", 9, TextAnchor.MiddleRight);
            progressTxt.color = new Color(0.7f, 0.75f, 0.8f);
            RectTransform progRt = progressTxt.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(1, 0);
            progRt.anchorMax = new Vector2(1, 1);
            progRt.pivot = new Vector2(1, 0.5f);
            progRt.offsetMin = new Vector2(-46f, 0);
            progRt.offsetMax = new Vector2(-6f, 0);

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

            cardLe.preferredHeight = 28f + gridRt.sizeDelta.y;
        }

        private static void AddCardBorder(Transform parent, Color color)
        {
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
            tip.textOnClickDescription = def.desc + $"sm_knowledgepanel_790";
            tip.text_description_2 = string.Empty;
        }

        private static void AddBoxFrame(GameObject go, Color bgColor, Color borderColor)
        {
            Image bg = go.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

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
                0 => "sm_knowledgepanel_791",
                1 => "sm_knowledgepanel_792",
                2 => "sm_knowledgepanel_793",
                3 => "sm_knowledgepanel_794",
                4 => "sm_knowledgepanel_795",
                _ => "sm_knowledgepanel_796"
            };
        }

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
