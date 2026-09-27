using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal static partial class SuperMechKnowledgePanel
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

        private static Transform _classSwitcher;

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

            Text nameTxt = SuperMechUtils.CreateText(header.transform, GetTierName(tier), 11, TextAnchor.MiddleLeft);
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

            Text progressTxt = SuperMechUtils.CreateText(header.transform, $"{unlocked}/{defs.Count}", 9, TextAnchor.MiddleRight);
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
