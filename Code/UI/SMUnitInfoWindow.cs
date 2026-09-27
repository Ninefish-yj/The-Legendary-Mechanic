using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static class SMUnitInfoWindow
    {
        private const float WinW = 540f;
        private const float WinH = 620f;

        private static readonly Color BgColor = new Color(0.02f, 0.05f, 0.08f, 0.92f);
        private static readonly Color PanelBg = new Color(0.04f, 0.08f, 0.12f, 0.85f);
        private static readonly Color BorderColor = new Color(0.1f, 0.7f, 0.8f, 0.9f);
        private static readonly Color BorderDim = new Color(0.1f, 0.5f, 0.6f, 0.5f);
        private static readonly Color AccentColor = new Color(0.2f, 0.9f, 1f, 1f);
        private static readonly Color TextPrimary = new Color(0.85f, 0.95f, 1f, 1f);
        private static readonly Color TextSecondary = new Color(0.5f, 0.7f, 0.8f, 1f);
        private static readonly Color TextDim = new Color(0.35f, 0.5f, 0.6f, 1f);
        private static readonly Color TabActive = new Color(0.08f, 0.25f, 0.35f, 0.95f);
        private static readonly Color TabInactive = new Color(0.04f, 0.08f, 0.12f, 0.7f);
        private static readonly Color BarBg = new Color(0.05f, 0.1f, 0.15f, 0.8f);
        private static readonly Color BarFill = new Color(0.15f, 0.7f, 0.85f, 0.9f);

        private static RectTransform _root;
        private static RectTransform _content;
        private static Actor _actor;
        private static int _currentTab = 0;
        private static readonly Button[] _tabButtons = new Button[2];
        private static readonly Image[] _tabImages = new Image[2];

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
                _root.anchoredPosition = new Vector2(120f, 0f);

                Image bg = rootGo.AddComponent<Image>();
                bg.color = BgColor;
                bg.raycastTarget = false;

                AddSciFiBorder(_root);
                AddScanLines(_root);

                GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform));
                titleBar.transform.SetParent(_root, false);
                RectTransform titleRt = titleBar.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 1);
                titleRt.anchorMax = new Vector2(1, 1);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.sizeDelta = new Vector2(0, 36f);
                Image titleBg = titleBar.AddComponent<Image>();
                titleBg.color = new Color(0.05f, 0.12f, 0.18f, 0.95f);
                titleBg.raycastTarget = true;

                Image titleGlow = titleBar.AddComponent<Image>();
                titleGlow.color = new Color(0.1f, 0.6f, 0.7f, 0.15f);
                titleGlow.raycastTarget = false;
                RectTransform glowRt = titleGlow.GetComponent<RectTransform>();
                glowRt.anchorMin = Vector2.zero;
                glowRt.anchorMax = Vector2.one;
                glowRt.offsetMin = new Vector2(0, 28f);
                glowRt.offsetMax = Vector2.zero;

                SMDraggableWindow drag = titleBar.AddComponent<SMDraggableWindow>();
                drag.WindowRect = _root;

                Text titleText = SuperMechUtils.CreateText(titleBar.transform, LocalizedTextManager.getText("sm_ui_unit_info"), 15, TextAnchor.MiddleCenter, AccentColor);
                RectTransform textRt = titleText.rectTransform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(50f, 0f);
                textRt.offsetMax = new Vector2(-50f, 0f);
                titleText.fontStyle = FontStyle.Bold;
                titleText.fontSize = 14;

                Image titleLine = new GameObject("TitleLine", typeof(RectTransform)).AddComponent<Image>();
                titleLine.transform.SetParent(titleBar.transform, false);
                titleLine.color = BorderColor;
                titleLine.raycastTarget = false;
                RectTransform lineRt = titleLine.GetComponent<RectTransform>();
                lineRt.anchorMin = new Vector2(0, 0);
                lineRt.anchorMax = new Vector2(1, 0);
                lineRt.pivot = new Vector2(0.5f, 0f);
                lineRt.sizeDelta = new Vector2(0, 1f);

                GameObject closeBtn = new GameObject("CloseBtn", typeof(RectTransform));
                closeBtn.transform.SetParent(titleBar.transform, false);
                RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(1, 0.5f);
                closeRt.anchorMax = new Vector2(1, 0.5f);
                closeRt.pivot = new Vector2(1, 0.5f);
                closeRt.sizeDelta = new Vector2(28f, 28f);
                closeRt.anchoredPosition = new Vector2(-8f, 0f);
                Image closeImg = closeBtn.AddComponent<Image>();
                closeImg.color = new Color(0.6f, 0.15f, 0.15f, 0.8f);
                closeImg.raycastTarget = true;
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
                tabRt.sizeDelta = new Vector2(0, 30f);
                tabRt.anchoredPosition = new Vector2(0, -36f);
                HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 2f;
                hlg.padding = new RectOffset(6, 6, 2, 2);

                string[] tabNames = { LocalizedTextManager.getText("sm_ui_knowledge"), LocalizedTextManager.getText("sm_ui_bag") };
                for (int i = 0; i < 2; i++)
                {
                    int idx = i;
                    GameObject tabGo = new GameObject("Tab" + i, typeof(RectTransform));
                    tabGo.transform.SetParent(tabBar.transform, false);
                    RectTransform tr = tabGo.GetComponent<RectTransform>();
                    tr.sizeDelta = new Vector2(130f, 26f);
                    Image tabImg = tabGo.AddComponent<Image>();
                    tabImg.color = i == 0 ? TabActive : TabInactive;
                    tabImg.raycastTarget = true;
                    Button tabBtn = tabGo.AddComponent<Button>();
                    tabBtn.onClick.AddListener(() => SwitchTab(idx));
                    Text tabText = SuperMechUtils.CreateText(tabGo.transform, tabNames[i], 12, TextAnchor.MiddleCenter, i == 0 ? AccentColor : TextSecondary);
                    tabText.fontStyle = FontStyle.Bold;
                    _tabButtons[i] = tabBtn;
                    _tabImages[i] = tabImg;
                }

                Image tabLine = new GameObject("TabLine", typeof(RectTransform)).AddComponent<Image>();
                tabLine.transform.SetParent(tabBar.transform, false);
                tabLine.color = BorderDim;
                tabLine.raycastTarget = false;
                RectTransform tabLineRt = tabLine.GetComponent<RectTransform>();
                tabLineRt.anchorMin = new Vector2(0, 0);
                tabLineRt.anchorMax = new Vector2(1, 0);
                tabLineRt.pivot = new Vector2(0.5f, 0f);
                tabLineRt.sizeDelta = new Vector2(0, 1f);

                GameObject contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(_root, false);
                _content = contentGo.GetComponent<RectTransform>();
                _content.anchorMin = Vector2.zero;
                _content.anchorMax = Vector2.one;
                _content.pivot = new Vector2(0.5f, 0.5f);
                _content.offsetMin = new Vector2(8f, 8f);
                _content.offsetMax = new Vector2(-8f, -74f);

                _root.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 独立窗口初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void AddSciFiBorder(RectTransform parent)
        {
            int cornerSize = 12;
            int lineWidth = 1;

            GameObject tl = new GameObject("CornerTL", typeof(RectTransform));
            tl.transform.SetParent(parent, false);
            Image tlImg = tl.AddComponent<Image>();
            tlImg.color = BorderColor;
            tlImg.raycastTarget = false;
            RectTransform tlRt = tl.GetComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0, 1);
            tlRt.anchorMax = new Vector2(0, 1);
            tlRt.pivot = new Vector2(0, 1);
            tlRt.sizeDelta = new Vector2(cornerSize, lineWidth);
            tlRt.anchoredPosition = Vector2.zero;

            GameObject tl2 = new GameObject("CornerTL2", typeof(RectTransform));
            tl2.transform.SetParent(parent, false);
            Image tl2Img = tl2.AddComponent<Image>();
            tl2Img.color = BorderColor;
            tl2Img.raycastTarget = false;
            RectTransform tl2Rt = tl2.GetComponent<RectTransform>();
            tl2Rt.anchorMin = new Vector2(0, 1);
            tl2Rt.anchorMax = new Vector2(0, 1);
            tl2Rt.pivot = new Vector2(0, 1);
            tl2Rt.sizeDelta = new Vector2(lineWidth, cornerSize);
            tl2Rt.anchoredPosition = Vector2.zero;

            GameObject tr = new GameObject("CornerTR", typeof(RectTransform));
            tr.transform.SetParent(parent, false);
            Image trImg = tr.AddComponent<Image>();
            trImg.color = BorderColor;
            trImg.raycastTarget = false;
            RectTransform trRt = tr.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(1, 1);
            trRt.anchorMax = new Vector2(1, 1);
            trRt.pivot = new Vector2(1, 1);
            trRt.sizeDelta = new Vector2(cornerSize, lineWidth);
            trRt.anchoredPosition = Vector2.zero;

            GameObject tr2 = new GameObject("CornerTR2", typeof(RectTransform));
            tr2.transform.SetParent(parent, false);
            Image tr2Img = tr2.AddComponent<Image>();
            tr2Img.color = BorderColor;
            tr2Img.raycastTarget = false;
            RectTransform tr2Rt = tr2.GetComponent<RectTransform>();
            tr2Rt.anchorMin = new Vector2(1, 1);
            tr2Rt.anchorMax = new Vector2(1, 1);
            tr2Rt.pivot = new Vector2(1, 1);
            tr2Rt.sizeDelta = new Vector2(lineWidth, cornerSize);
            tr2Rt.anchoredPosition = Vector2.zero;

            GameObject bl = new GameObject("CornerBL", typeof(RectTransform));
            bl.transform.SetParent(parent, false);
            Image blImg = bl.AddComponent<Image>();
            blImg.color = BorderColor;
            blImg.raycastTarget = false;
            RectTransform blRt = bl.GetComponent<RectTransform>();
            blRt.anchorMin = new Vector2(0, 0);
            blRt.anchorMax = new Vector2(0, 0);
            blRt.pivot = new Vector2(0, 0);
            blRt.sizeDelta = new Vector2(cornerSize, lineWidth);
            blRt.anchoredPosition = Vector2.zero;

            GameObject bl2 = new GameObject("CornerBL2", typeof(RectTransform));
            bl2.transform.SetParent(parent, false);
            Image bl2Img = bl2.AddComponent<Image>();
            bl2Img.color = BorderColor;
            bl2Img.raycastTarget = false;
            RectTransform bl2Rt = bl2.GetComponent<RectTransform>();
            bl2Rt.anchorMin = new Vector2(0, 0);
            bl2Rt.anchorMax = new Vector2(0, 0);
            bl2Rt.pivot = new Vector2(0, 0);
            bl2Rt.sizeDelta = new Vector2(lineWidth, cornerSize);
            bl2Rt.anchoredPosition = Vector2.zero;

            GameObject br = new GameObject("CornerBR", typeof(RectTransform));
            br.transform.SetParent(parent, false);
            Image brImg = br.AddComponent<Image>();
            brImg.color = BorderColor;
            brImg.raycastTarget = false;
            RectTransform brRt = br.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(1, 0);
            brRt.anchorMax = new Vector2(1, 0);
            brRt.pivot = new Vector2(1, 0);
            brRt.sizeDelta = new Vector2(cornerSize, lineWidth);
            brRt.anchoredPosition = Vector2.zero;

            GameObject br2 = new GameObject("CornerBR2", typeof(RectTransform));
            br2.transform.SetParent(parent, false);
            Image br2Img = br2.AddComponent<Image>();
            br2Img.color = BorderColor;
            br2Img.raycastTarget = false;
            RectTransform br2Rt = br2.GetComponent<RectTransform>();
            br2Rt.anchorMin = new Vector2(1, 0);
            br2Rt.anchorMax = new Vector2(1, 0);
            br2Rt.pivot = new Vector2(1, 0);
            br2Rt.sizeDelta = new Vector2(lineWidth, cornerSize);
            br2Rt.anchoredPosition = Vector2.zero;
        }

        private static void AddScanLines(RectTransform parent)
        {
            GameObject scanGo = new GameObject("ScanLines", typeof(RectTransform));
            scanGo.transform.SetParent(parent, false);
            Image scanImg = scanGo.AddComponent<Image>();
            scanImg.color = new Color(0.1f, 0.4f, 0.5f, 0.03f);
            scanImg.raycastTarget = false;
            RectTransform scanRt = scanImg.GetComponent<RectTransform>();
            scanRt.anchorMin = Vector2.zero;
            scanRt.anchorMax = Vector2.one;
            scanRt.offsetMin = Vector2.zero;
            scanRt.offsetMax = Vector2.zero;
        }

        private static void SwitchTab(int idx)
        {
            _currentTab = idx;
            for (int i = 0; i < 2; i++)
            {
                if (_tabImages[i] != null)
                {
                    _tabImages[i].color = i == idx ? TabActive : TabInactive;
                }
                Text txt = _tabButtons[i]?.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = i == idx ? AccentColor : TextSecondary;
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
            viewport.transform.SetParent(scrollGo.transform, false);
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
            contentRt.sizeDelta = new Vector2(0, 900f);

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddSectionTitle(contentGo.transform, LocalizedTextManager.getText("sm_ui_equipment"));

            GridLayoutGroup grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(48, 48);
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

            AddSectionTitle(contentGo.transform, LocalizedTextManager.getText("sm_ui_resources"));

            GameObject resContainer = new GameObject("ResourceContainer", typeof(RectTransform));
            resContainer.transform.SetParent(contentGo.transform, false);
            RectTransform resRt = resContainer.GetComponent<RectTransform>();
            resRt.sizeDelta = new Vector2(0, 200f);

            GridLayoutGroup resGrid = resContainer.AddComponent<GridLayoutGroup>();
            resGrid.cellSize = new Vector2(48, 48);
            resGrid.spacing = new Vector2(6, 6);
            resGrid.constraint = GridLayoutGroup.Constraint.Flexible;
            resGrid.childAlignment = TextAnchor.UpperLeft;

            SMUnitBag.Render(resContainer.transform, _actor);
        }

        private static void AddSectionTitle(Transform parent, string title)
        {
            GameObject go = new GameObject("SectionTitle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 24f);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.12f, 0.18f, 0.6f);
            bg.raycastTarget = false;

            Image line = new GameObject("Line", typeof(RectTransform)).AddComponent<Image>();
            line.transform.SetParent(go.transform, false);
            line.color = BorderColor;
            line.raycastTarget = false;
            RectTransform lineRt = line.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0);
            lineRt.anchorMax = new Vector2(1, 0);
            lineRt.pivot = new Vector2(0.5f, 0f);
            lineRt.sizeDelta = new Vector2(0, 1f);

            Text txt = SuperMechUtils.CreateText(go.transform, title, 12, TextAnchor.MiddleLeft, AccentColor);
            txt.fontStyle = FontStyle.Bold;
            RectTransform txtRt = txt.rectTransform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 0f);
            txtRt.offsetMax = new Vector2(-8f, 0f);
        }

        private static void CreateEquipIcon(Transform parent, SuperMechRelic.EquipDef def, bool equipped)
        {
            GameObject go = new GameObject("Equip_" + def.id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);

            Image bg = go.AddComponent<Image>();
            bg.color = equipped ? new Color(0.1f, 0.3f, 0.4f, 0.6f) : PanelBg;
            bg.raycastTarget = true;

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

            if (equipped)
            {
                Image mark = new GameObject("EquippedMark", typeof(RectTransform)).AddComponent<Image>();
                mark.transform.SetParent(go.transform, false);
                mark.color = new Color(1f, 0.84f, 0f, 0.3f);
                mark.raycastTarget = false;
                RectTransform markRt = mark.GetComponent<RectTransform>();
                markRt.anchorMin = Vector2.zero;
                markRt.anchorMax = Vector2.one;
                markRt.offsetMin = Vector2.zero;
                markRt.offsetMax = Vector2.zero;
            }

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (equipped)
                {
                    SuperMechEquipBag.UnequipToBag(_actor);
                }
                else
                {
                    SuperMechEquipBag.EquipFromBag(_actor, def.id);
                }
                Refresh();
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
