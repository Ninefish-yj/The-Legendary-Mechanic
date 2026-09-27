using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static class SMUnitInfoWindow
    {
        private const float WinW = 520f;
        private const float WinH = 600f;
        private static RectTransform _root;
        private static RectTransform _content;
        private static Actor _actor;
        private static int _currentTab = 0;
        private static readonly Button[] _tabButtons = new Button[2];
        private static readonly Image[] _tabImages = new Image[2];
        private static GameObject _knowledgePanel;
        private static GameObject _bagPanel;

        public static void Show(Actor actor)
        {
            if (actor == null) return;
            if (_root == null) Init();
            if (_root == null) return;
            _actor = actor;
            _currentTab = 0;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Refresh();
        }

        public static void Hide()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
            _actor = null;
        }

        private static void Init()
        {
            try
            {
                Canvas canvas = GameObject.Find("Main Canvas")?.GetComponent<Canvas>();
                if (canvas == null)
                {
                    Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                    foreach (Canvas c in canvases)
                    {
                        if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            canvas = c;
                            break;
                        }
                    }
                }
                if (canvas == null) return;

                GameObject rootGo = new GameObject("SMUnitInfoWindow", typeof(RectTransform));
                rootGo.transform.SetParent(canvas.transform, false);
                _root = rootGo.GetComponent<RectTransform>();
                _root.anchorMin = new Vector2(0.5f, 0.5f);
                _root.anchorMax = new Vector2(0.5f, 0.5f);
                _root.pivot = new Vector2(0.5f, 0.5f);
                _root.sizeDelta = new Vector2(WinW, WinH);
                _root.anchoredPosition = new Vector2(100f, 0f);

                Image bg = rootGo.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.1f, 0.12f, 0.97f);
                bg.raycastTarget = true;

                AddBorder(_root, new Color(0.3f, 0.35f, 0.4f, 0.8f));

                GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform));
                titleBar.transform.SetParent(_root, false);
                RectTransform titleRt = titleBar.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 1);
                titleRt.anchorMax = new Vector2(1, 1);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.sizeDelta = new Vector2(0, 32f);
                Image titleBg = titleBar.AddComponent<Image>();
                titleBg.color = new Color(0.12f, 0.15f, 0.2f, 1f);
                titleBg.raycastTarget = true;

                SMDraggableWindow drag = titleBar.AddComponent<SMDraggableWindow>();
                drag.WindowRect = _root;

                Text titleText = SuperMechUtils.CreateText(titleBar.transform, LocalizedTextManager.getText("sm_ui_unit_info"), 14, TextAnchor.MiddleCenter, Color.white);
                RectTransform textRt = titleText.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(40f, 0f);
                textRt.offsetMax = new Vector2(-40f, 0f);
                titleText.fontStyle = FontStyle.Bold;

                GameObject closeBtn = new GameObject("CloseBtn", typeof(RectTransform));
                closeBtn.transform.SetParent(titleBar.transform, false);
                RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(1, 0.5f);
                closeRt.anchorMax = new Vector2(1, 0.5f);
                closeRt.pivot = new Vector2(1, 0.5f);
                closeRt.sizeDelta = new Vector2(28f, 28f);
                closeRt.anchoredPosition = new Vector2(-6f, 0f);
                Image closeImg = closeBtn.AddComponent<Image>();
                closeImg.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
                Button closeButton = closeBtn.AddComponent<Button>();
                closeButton.onClick.AddListener(Hide);
                Text closeText = SuperMechUtils.CreateText(closeBtn.transform, "X", 14, TextAnchor.MiddleCenter, Color.white);
                closeText.fontStyle = FontStyle.Bold;

                GameObject tabBar = new GameObject("TabBar", typeof(RectTransform));
                tabBar.transform.SetParent(_root, false);
                RectTransform tabRt = tabBar.GetComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0, 1);
                tabRt.anchorMax = new Vector2(1, 1);
                tabRt.pivot = new Vector2(0.5f, 1f);
                tabRt.sizeDelta = new Vector2(0, 28f);
                tabRt.anchoredPosition = new Vector2(0, -32f);
                HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 4f;
                hlg.padding = new RectOffset(4, 4, 2, 2);

                string[] tabNames = { LocalizedTextManager.getText("sm_ui_knowledge"), LocalizedTextManager.getText("sm_ui_bag") };
                for (int i = 0; i < 2; i++)
                {
                    int idx = i;
                    GameObject tabGo = new GameObject("Tab" + i, typeof(RectTransform));
                    tabGo.transform.SetParent(tabBar.transform, false);
                    RectTransform tr = tabGo.GetComponent<RectTransform>();
                    tr.sizeDelta = new Vector2(120f, 24f);
                    Image tabImg = tabGo.AddComponent<Image>();
                    tabImg.color = i == 0 ? new Color(0.2f, 0.3f, 0.4f, 1f) : new Color(0.15f, 0.18f, 0.22f, 1f);
                    Button tabBtn = tabGo.AddComponent<Button>();
                    tabBtn.onClick.AddListener(() => SwitchTab(idx));
                    Text tabText = SuperMechUtils.CreateText(tabGo.transform, tabNames[i], 12, TextAnchor.MiddleCenter, Color.white);
                    _tabButtons[i] = tabBtn;
                    _tabImages[i] = tabImg;
                }

                GameObject contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(_root, false);
                _content = contentGo.GetComponent<RectTransform>();
                _content.anchorMin = Vector2.zero;
                _content.anchorMax = Vector2.one;
                _content.pivot = new Vector2(0.5f, 0.5f);
                _content.offsetMin = new Vector2(6f, 6f);
                _content.offsetMax = new Vector2(-6f, -66f);

                _root.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 独立窗口初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void AddBorder(RectTransform parent, Color color)
        {
            GameObject top = new GameObject("BorderTop", typeof(RectTransform));
            top.transform.SetParent(parent, false);
            Image topImg = top.AddComponent<Image>();
            topImg.color = color;
            topImg.raycastTarget = false;
            RectTransform tr = top.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(0, 2f);

            GameObject bottom = new GameObject("BorderBottom", typeof(RectTransform));
            bottom.transform.SetParent(parent, false);
            Image botImg = bottom.AddComponent<Image>();
            botImg.color = color;
            botImg.raycastTarget = false;
            RectTransform br = bottom.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0, 0);
            br.anchorMax = new Vector2(1, 0);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0, 2f);

            GameObject left = new GameObject("BorderLeft", typeof(RectTransform));
            left.transform.SetParent(parent, false);
            Image leftImg = left.AddComponent<Image>();
            leftImg.color = color;
            leftImg.raycastTarget = false;
            RectTransform lr = left.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 0);
            lr.anchorMax = new Vector2(0, 1);
            lr.pivot = new Vector2(0, 0.5f);
            lr.sizeDelta = new Vector2(2f, 0);

            GameObject right = new GameObject("BorderRight", typeof(RectTransform));
            right.transform.SetParent(parent, false);
            Image rightImg = right.AddComponent<Image>();
            rightImg.color = color;
            rightImg.raycastTarget = false;
            RectTransform rr = right.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(1, 0);
            rr.anchorMax = new Vector2(1, 1);
            rr.pivot = new Vector2(1, 0.5f);
            rr.sizeDelta = new Vector2(2f, 0);
        }

        private static void SwitchTab(int idx)
        {
            _currentTab = idx;
            for (int i = 0; i < 2; i++)
            {
                if (_tabImages[i] != null)
                {
                    _tabImages[i].color = i == idx ? new Color(0.2f, 0.3f, 0.4f, 1f) : new Color(0.15f, 0.18f, 0.22f, 1f);
                }
            }
            Refresh();
        }

        private static void Refresh()
        {
            if (_content == null || _actor == null) return;

            foreach (Transform child in _content)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            if (_currentTab == 0)
            {
                SuperMechKnowledgePanel.Ensure(_content, _actor);
            }
            else
            {
                RenderBag();
            }
        }

        private static void RenderBag()
        {
            if (_actor == null || _content == null) return;

            GameObject scrollGo = new GameObject("BagScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(_content, false);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGo, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);

            GameObject contentGo = new GameObject("BagContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 800f);

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text title = SuperMechUtils.CreateText(contentGo.transform, LocalizedTextManager.getText("sm_ui_equipment"), 13, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
            title.fontStyle = FontStyle.Bold;

            GridLayoutGroup grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(44, 44);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.childAlignment = TextAnchor.UpperLeft;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(_actor);
            if (currentIdx >= 0)
            {
                CreateEquipIcon(grid.transform, SuperMechRelic.Equipments[currentIdx], true);
            }

            var bag = SuperMechEquipBag.GetBag(_actor);
            if (bag != null)
            {
                foreach (string equipId in bag)
                {
                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx >= 0 && idx != currentIdx)
                    {
                        CreateEquipIcon(grid.transform, SuperMechRelic.Equipments[idx], false);
                    }
                }
            }

            Text resTitle = SuperMechUtils.CreateText(contentGo.transform, LocalizedTextManager.getText("sm_ui_resources"), 13, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
            resTitle.fontStyle = FontStyle.Bold;

            GridLayoutGroup resGrid = contentGo.AddComponent<GridLayoutGroup>();
            resGrid.cellSize = new Vector2(44, 44);
            resGrid.spacing = new Vector2(6, 6);
            resGrid.constraint = GridLayoutGroup.Constraint.Flexible;
            resGrid.childAlignment = TextAnchor.UpperLeft;

            if (_actor.inventory != null)
            {
                var resources = new List<(ResourceAsset asset, int amount)>();
                foreach (var kv in _actor.inventory.dict)
                {
                    ResourceAsset res = AssetManager.resources.get(kv.Key);
                    if (res != null && kv.Value.amount > 0)
                    {
                        resources.Add((res, kv.Value.amount));
                    }
                }
                resources.Sort((a, b) => a.asset.order.CompareTo(b.asset.order));
                foreach (var r in resources)
                {
                    CreateResourceIcon(resGrid.transform, r.asset, r.amount);
                }
            }
        }

        private static void CreateEquipIcon(Transform parent, SuperMechRelic.EquipDef def, bool equipped)
        {
            GameObject go = new GameObject("Equip_" + def.id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            Image bg = go.AddComponent<Image>();
            bg.color = equipped ? new Color(1f, 0.84f, 0f, 0.3f) : new Color(0.15f, 0.18f, 0.22f, 0.8f);

            Image icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(go.transform, false);
            icon.raycastTarget = false;
            try
            {
                Sprite sprite = SpriteTextureLoader.getSprite(def.icon);
                if (sprite != null) icon.sprite = sprite;
            }
            catch { }
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);

            Color qColor = GetQualityColor(def.qualityLevel);
            Image border = new GameObject("Border", typeof(RectTransform)).AddComponent<Image>();
            border.transform.SetParent(go.transform, false);
            border.color = qColor;
            border.raycastTarget = false;
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (equipped)
                {
                    SuperMechRelic.Unequip(_actor);
                }
                else
                {
                    SuperMechRelic.Equip(_actor, def.id);
                }
                Refresh();
            });
        }

        private static void CreateResourceIcon(Transform parent, ResourceAsset res, int amount)
        {
            GameObject go = new GameObject("Res_" + res.id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.18f, 0.8f);

            Image icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(go.transform, false);
            icon.raycastTarget = false;
            try
            {
                Sprite sprite = SpriteTextureLoader.getSprite(res.path_icon);
                if (sprite != null) icon.sprite = sprite;
            }
            catch { }
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);

            Text amountTxt = SuperMechUtils.CreateText(go.transform, amount.ToString(), 10, TextAnchor.LowerRight, Color.white);
            RectTransform amtRt = amountTxt.rectTransform;
            amtRt.anchorMin = new Vector2(0, 0);
            amtRt.anchorMax = new Vector2(1, 0.3f);
            amtRt.offsetMin = new Vector2(0, 0);
            amtRt.offsetMax = new Vector2(-2f, 0);

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (res.type == ResType.Food && amount > 0)
                {
                    _actor.consumeFoodResource(res);
                    _actor.inventory.remove(res.id, 1);
                    Refresh();
                }
            });
        }
        private static Color GetQualityColor(int q)
        {
            Color[] colors = {
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.3f, 0.8f, 0.3f),
                new Color(0.3f, 0.5f, 1f),
                new Color(0.7f, 0.4f, 1f),
                new Color(0.8f, 0.2f, 0.8f),
                new Color(1f, 0.4f, 0.7f),
                new Color(1f, 0.6f, 0.2f),
                new Color(0.8f, 0.8f, 0.9f),
                new Color(1f, 0.84f, 0f)
            };
            if (q >= 0 && q < colors.Length) return colors[q];
            return Color.white;
        }
    }
}
