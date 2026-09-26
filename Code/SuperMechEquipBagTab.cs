using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备背包Tab：参考天人武道神藏Tab的实现方式，在单位面板添加自定义Tab。
    /// 显示单位背包中的装备，点击可装备/卸下。
    /// </summary>
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechEquipBagTab
    {
        private const string TabName = "SuperMechEquipBagTab";
        private const string ContainerName = "SuperMechEquipBagContent";

        private static bool _callbacksRegistered;
        private static WindowMetaTab _bagTab;
        private static GameObject _container;
        private static UnitWindow _boundWindow;
        private static readonly FieldInfo TabsListField =
            AccessTools.Field(typeof(WindowMetaTabButtonsContainer), "_tabs");

        public static void Postfix(UnitWindow __instance)
        {
            try { Refresh(__instance); } catch { }
        }

        private static void Refresh(UnitWindow window)
        {
            if (window == null) return;
            Actor actor = GetActor(window);
            if (actor == null || !actor.isAlive()) return;

            ScrollWindow scroll = window.scroll_window;
            if (scroll == null)
            {
                Component host = (Component)(object)window;
                scroll = host.GetComponent<ScrollWindow>() ?? host.GetComponentInParent<ScrollWindow>();
            }
            if (scroll?.tabs == null) return;

            WindowMetaTab tab = FindOrCreateTab(scroll, window);
            if (tab == null) return;

            _bagTab = tab;
            _boundWindow = window;

            tab.gameObject.SetActive(true);
            try { tab.toggleActive(true); } catch { }
            EnsureContainer(scroll);
            WireTab(tab, scroll);
            RegisterCallbacks(scroll);

            if (_container != null && !tab.tab_elements.Contains(_container.transform))
            {
                tab.tab_elements.Add(_container.transform);
                // 更新_tabs_with_content列表
                try { scroll.tabs.GetType().GetMethod("refillTabsWithContent",
                    BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(scroll.tabs, null); } catch { }
            }

            bool onBag = scroll.tabs != null && scroll.tabs.isActiveTab(tab);
            if (onBag) RenderBag(actor);
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                var prop = typeof(UnitWindow).GetProperty("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.GetValue(window) as Actor;
                var field = typeof(UnitWindow).GetField("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        private static WindowMetaTab FindExistingTab(ScrollWindow scroll)
        {
            if (scroll?.tabs == null) return null;
            foreach (WindowMetaTab t in scroll.tabs.GetComponentsInChildren<WindowMetaTab>(true))
            {
                if (t != null && t.name == TabName) return t;
            }
            return null;
        }

        private static WindowMetaTab FindOrCreateTab(ScrollWindow scroll, UnitWindow window)
        {
            WindowMetaTab existing = FindExistingTab(scroll);
            if (existing != null) return existing;

            WindowMetaTab[] all = scroll.tabs.GetComponentsInChildren<WindowMetaTab>(true);
            if (all == null || all.Length == 0) return null;
            WindowMetaTab source = all[0];

            // 参考天人武道：克隆到source的父对象，放在最后面（不影响原版Tab的拖拽排序索引）
            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetAsLastSibling();

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            // 立即移除拖拽排序组件（用DestroyImmediate，避免延迟销毁期间被DragOrderContainer扫描到）
            DragOrderElement dragElem = newTab.GetComponent<DragOrderElement>();
            if (dragElem != null) Object.DestroyImmediate(dragElem);

            // 清空而不是new（参考天人武道）
            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

            // 关键：把克隆的Tab注册到_tabs列表，否则isActiveTab/showTab都不认识它
            try
            {
                var tabsField = typeof(WindowMetaTabButtonsContainer).GetField("_tabs",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (tabsField != null)
                {
                    var tabsList = tabsField.GetValue(scroll.tabs) as List<WindowMetaTab>;
                    if (tabsList != null && !tabsList.Contains(newTab))
                    {
                        tabsList.Add(newTab);
                        newTab.container = scroll.tabs;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 注册背包Tab到_tabs失败: " + e.Message);
            }

            // 重置tab_action
            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ =>
            {
                scroll.tabs.showTab(newTab);
                // Tab切换时触发内容刷新
                if (_boundWindow != null)
                {
                    try { Refresh(_boundWindow); } catch { }
                }
            });

            newTab.gameObject.SetActive(true);

            // 用TipButton设置文本（参考蛊真人：title+description合并到textOnClick）
            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = LocalizedTextManager.getText("sm_tab_bag_tip");
                tip.textOnClickDescription = string.Empty;
                tip.text_description_2 = string.Empty;
            }

            // 设置图标（精确找到图标Image，跳过背景Image）
            Image[] allImages = tabObj.GetComponentsInChildren<Image>(true);
            Image icon = null;
            foreach (Image img in allImages)
            {
                RectTransform rt = img.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.x < 50 && rt.sizeDelta.y < 50)
                {
                    icon = img;
                    break;
                }
            }
            if (icon == null && allImages.Length > 1) icon = allImages[1];
            if (icon != null)
            {
                try { icon.sprite = SpriteTextureLoader.getSprite("ui/Icons/iconArmor"); } catch { }
            }

            // CanvasGroup控制可见性
            CanvasGroup cg = newTab.GetComponent<CanvasGroup>();
            if (cg == null) cg = newTab.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            return newTab;
        }

        private static void EnsureContainer(ScrollWindow scroll)
        {
            Transform scrollContent = scroll.transform_content;
            if (scrollContent == null) return;

            Transform existing = scrollContent.Find(ContainerName);
            if (existing != null)
            {
                _container = existing.gameObject;
                return;
            }

            _container = new GameObject(ContainerName, typeof(RectTransform));
            _container.transform.SetParent(scrollContent, false);

            RectTransform rt = _container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ContentSizeFitter fitter = _container.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup layout = _container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            _container.SetActive(false);
        }

        private static void WireTab(WindowMetaTab tab, ScrollWindow scroll)
        {
            if (tab.tab_action == null)
            {
                tab.tab_action = new WindowMetaTabEvent();
                tab.tab_action.AddListener(_ => scroll.tabs.showTab(tab));
            }
        }

        private static void RegisterCallbacks(ScrollWindow scroll)
        {
            if (_callbacksRegistered || scroll.tabs == null) return;
            _callbacksRegistered = true;
            scroll.tabs.addTabShowCallback(OnTabShow);
            scroll.tabs.addTabHideCallback(OnTabHide);
        }

        private static void OnTabShow(WindowMetaTab tab)
        {
            if (tab != _bagTab) return;
            Actor actor = GetActor(_boundWindow);
            if (actor != null) RenderBag(actor);
        }

        private static void OnTabHide()
        {
            // tab_elements机制会自动隐藏内容，不需要手动SetActive
        }

        /// <summary>渲染背包内容（纯图标网格，参考原版城市资源仓库ButtonResource）。</summary>
        private static void RenderBag(Actor actor)
        {
            if (_container == null || actor == null) return;

            // 清空旧内容
            foreach (Transform child in _container.transform)
            {
                if (child.name != "LayoutGroup") Object.Destroy(child.gameObject);
            }

            // === 已装备区域（带框，参考原版装备槽）===
            var equippedBox = CreateCategoryBox(_container.transform, LocalizedTextManager.getText("sm_ui_equipped"), null);
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                var cur = SuperMechRelic.Equipments[currentIdx];
                Color qColor = GetQualityColor(cur.qualityLevel);

                var equipSlot = CreateItemIcon(equippedBox, cur.icon, qColor, 48, cur.name,
                    $"{LocalizedTextManager.getText(\"sm_ui_quality\")}: {GetQualityName(cur.qualityLevel)}\n{LocalizedTextManager.getText(\"sm_ui_damage\")}×{cur.dmgMul}  {LocalizedTextManager.getText(\"sm_ui_health\")}×{cur.hpMul}\n{LocalizedTextManager.getText(\"sm_ui_unequip\")}");
                var btn = equipSlot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        SuperMechEquipBag.UnequipToBag(actor);
                        RenderBag(actor);
                    });
                }
            }
            else
            {
                // 空槽位（参考原版空装备槽）
                CreateEmptySlot(equippedBox, 48, LocalizedTextManager.getText("sm_ui_unequipped"));
            }

            // 分隔
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(_container.transform, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 8);

            // === 背包物品：按品质分组（带框，参考原版物品栏分类）===
            var bag = SuperMechEquipBag.GetBag(actor);

            // 按品质分组
            var byQuality = new Dictionary<int, List<string>>();
            for (int q = 0; q <= 8; q++) byQuality[q] = new List<string>();
            foreach (string equipId in bag)
            {
                int idx = SuperMechRelic.GetEquipIndex(equipId);
                if (idx >= 0)
                {
                    int q = SuperMechRelic.Equipments[idx].qualityLevel;
                    byQuality[q].Add(equipId);
                }
            }

            // 从高到低显示每个品质组
            for (int q = 8; q >= 0; q--)
            {
                var items = byQuality[q];
                string catName = GetQualityName(q);
                Color qColor = GetQualityColor(q);

                // 创建带框的分类容器（带计数，参考原版特质分组框）
                var catBox = CreateCategoryBox(_container.transform, catName, qColor, items.Count);

                if (items.Count > 0)
                {
                    // 图标网格（每行5个）
                    GameObject currentRow = null;
                    int iconIndex = 0;
                    foreach (string equipId in items)
                    {
                        if (iconIndex % 5 == 0)
                        {
                            currentRow = new GameObject("BagRow", typeof(RectTransform));
                            currentRow.transform.SetParent(catBox, false);
                            var rowLayout = currentRow.AddComponent<HorizontalLayoutGroup>();
                            rowLayout.spacing = 6;
                            rowLayout.childAlignment = TextAnchor.UpperLeft;
                            currentRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
                        }

                        int idx = SuperMechRelic.GetEquipIndex(equipId);
                        if (idx < 0) continue;
                        var def = SuperMechRelic.Equipments[idx];

                        var iconGo = CreateItemIcon(currentRow.transform, def.icon, qColor, 34, def.name,
                            $"{LocalizedTextManager.getText(\"sm_ui_quality\")}: {GetQualityName(def.qualityLevel)}\n{LocalizedTextManager.getText(\"sm_ui_damage\")}×{def.dmgMul}  {LocalizedTextManager.getText(\"sm_ui_health\")}×{def.hpMul}\n{LocalizedTextManager.getText(\"sm_ui_equip\")}");
                        var btn = iconGo.GetComponent<Button>();
                        if (btn != null)
                        {
                            btn.onClick.AddListener(() =>
                            {
                                SuperMechEquipBag.EquipFromBag(actor, equipId);
                                RenderBag(actor);
                            });
                        }
                        iconIndex++;
                    }
                }
                else
                {
                    // 空槽位（参考原版空分类显示）
                    var emptyRow = new GameObject("EmptyRow", typeof(RectTransform));
                    emptyRow.transform.SetParent(catBox, false);
                    emptyRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
                    var emptyText = CreateText(emptyRow.transform, LocalizedTextManager.getText("sm_ui_none_dash"), 12, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f, 0.6f));
                    var emptyRt = emptyText.GetComponent<RectTransform>();
                    emptyRt.anchorMin = Vector2.zero;
                    emptyRt.anchorMax = Vector2.one;
                    emptyRt.offsetMin = Vector2.zero;
                    emptyRt.offsetMax = Vector2.zero;
                }

                // 分类间距
                var catSpacer = new GameObject("CatSpacer", typeof(RectTransform));
                catSpacer.transform.SetParent(_container.transform, false);
                catSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 6);
            }
        }

        /// <summary>创建带背景框的分类容器（参考原版物品栏分类框）。</summary>
        private static Transform CreateCategoryBox(Transform parent, string title, Color? titleColor, int count = -1)
        {
            // 外框容器
            GameObject box = new GameObject("CategoryBox", typeof(RectTransform));
            box.transform.SetParent(parent, false);
            LayoutElement boxLe = box.AddComponent<LayoutElement>();
            boxLe.minHeight = 60f;
            boxLe.flexibleHeight = 0f;

            // 背景（深色半透明，参考原版特质分组框）
            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(box.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 边框（用品品质颜色，2px，参考知识库卡片）
            Color borderColor = titleColor ?? new Color(0.4f, 0.45f, 0.5f);
            AddBoxBorder(box.transform, borderColor);

            // 标题栏（左对齐，参考原版特质分组框的title字段）
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(box.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(0, 18f);
            titleRt.offsetMin = new Vector2(8, -18);
            titleRt.offsetMax = new Vector2(-8, 0);

            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = title;
            titleTxt.fontSize = 11;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = titleColor ?? new Color(0.9f, 0.85f, 0.6f);
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleTxt.font == null) titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 计数（右上角，参考原版特质分组框的counter字段）
            if (count >= 0)
            {
                GameObject counterGo = new GameObject("Counter", typeof(RectTransform));
                counterGo.transform.SetParent(box.transform, false);
                RectTransform counterRt = counterGo.GetComponent<RectTransform>();
                counterRt.anchorMin = new Vector2(1, 1);
                counterRt.anchorMax = new Vector2(1, 1);
                counterRt.pivot = new Vector2(1f, 1f);
                counterRt.sizeDelta = new Vector2(60, 18f);
                counterRt.offsetMin = new Vector2(-68, -18);
                counterRt.offsetMax = new Vector2(-8, 0);

                Text counterTxt = counterGo.AddComponent<Text>();
                counterTxt.text = count.ToString();
                counterTxt.fontSize = 10;
                counterTxt.color = new Color(0.7f, 0.7f, 0.75f);
                counterTxt.alignment = TextAnchor.MiddleRight;
                counterTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (counterTxt.font == null) counterTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // 内容容器（标题下方）
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(box.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(6, 6);
            contentRt.offsetMax = new Vector2(-6, -24);

            VerticalLayoutGroup contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 4f;
            contentVlg.padding = new RectOffset(4, 4, 4, 4);

            ContentSizeFitter contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentGo.transform;
        }

        /// <summary>给容器添加2px边框（4个Image模拟，参考知识库卡片）。</summary>
        private static void AddBoxBorder(Transform parent, Color color)
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
            topRt.sizeDelta = new Vector2(0, 2f);

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
            bottomRt.sizeDelta = new Vector2(0, 2f);

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
            leftRt.sizeDelta = new Vector2(2f, 0);

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
            rightRt.sizeDelta = new Vector2(2f, 0);
        }

        /// <summary>创建空槽位（参考原版空装备槽）。</summary>
        private static GameObject CreateEmptySlot(Transform parent, int size, string label)
        {
            var slot = new GameObject("EmptySlot", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

            // 边框
            var border = slot.AddComponent<UnityEngine.UI.Image>();
            border.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // 文字
            var txt = CreateText(slot.transform, label, 10, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f, 0.7f));
            var txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return slot;
        }

        /// <summary>创建物品图标（参考原版ButtonResource：品质边框+内部图标+tooltip）。</summary>
        private static GameObject CreateItemIcon(Transform parent, string iconPath, Color qColor, int size, string name, string tooltip)
        {
            var iconGo = new GameObject("ItemIcon", typeof(RectTransform));
            iconGo.transform.SetParent(parent, false);

            // 品质边框（背景）
            var borderImg = iconGo.AddComponent<Image>();
            borderImg.color = qColor;

            // 内部图标区域（深色背景）
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            innerGo.transform.SetParent(iconGo.transform, false);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.08f, 0.08f);
            innerRt.anchorMax = new Vector2(0.92f, 0.92f);
            innerRt.offsetMin = Vector2.zero;
            innerRt.offsetMax = Vector2.zero;

            // 物品图标
            var itemIcon = new GameObject("Icon", typeof(RectTransform));
            itemIcon.transform.SetParent(innerGo.transform, false);
            var itemImg = itemIcon.AddComponent<Image>();
            Sprite iconSprite = SpriteTextureLoader.getSprite(iconPath);
            if (iconSprite != null) itemImg.sprite = iconSprite;
            itemImg.color = Color.white;
            RectTransform iconRt = itemIcon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(size, size);

            // 点击按钮
            var btn = iconGo.AddComponent<Button>();
            btn.targetGraphic = borderImg;

            // Tooltip
            var tip = iconGo.AddComponent<TipButton>();
            tip.textOnClick = tooltip;
            tip.textOnClickDescription = string.Empty;
            tip.text_description_2 = string.Empty;

            return iconGo;
        }

        /// <summary>原著9级品质颜色。</summary>
        private static Color GetQualityColor(int q)
        {
            switch (q)
            {
                case 0: return new Color(0.6f, 0.6f, 0.6f); // 灰
                case 1: return new Color(0.3f, 0.8f, 0.3f); // 绿
                case 2: return new Color(0.3f, 0.5f, 1f);   // 蓝
                case 3: return new Color(0.7f, 0.5f, 1f);   // 淡紫
                case 4: return new Color(0.6f, 0.2f, 0.9f); // 紫
                case 5: return new Color(1f, 0.4f, 0.7f);   // 粉(珍稀)
                case 6: return new Color(1f, 0.6f, 0f);     // 橙(传说)
                case 7: return new Color(0.8f, 0.8f, 0.9f); // 银橙(使徒兵器)
                case 8: return new Color(1f, 0.84f, 0f);    // 金(宇宙宝物级)
                default: return Color.white;
            }
        }

        /// <summary>原著9级品质名（本地化）。</summary>
        private static string GetQualityName(int q)
        {
            string[] keys = { "sm_quality_0", "sm_quality_1", "sm_quality_2", "sm_quality_3", "sm_quality_4",
                              "sm_quality_5", "sm_quality_6", "sm_quality_7", "sm_quality_8" };
            return q >= 0 && q < keys.Length ? LocalizedTextManager.getText(keys[q]) : "?";
        }

        private static Text CreateText(Transform parent, string text, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.font = LocalizedTextManager.current_font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = text;
            return t;
        }

        private static void AddButton(Transform parent, string text, System.Action onClick)
        {
            GameObject obj = new GameObject("Button", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minHeight = 22;
            le.preferredHeight = 22;

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            Text t = obj.AddComponent<Text>();
            t.font = LocalizedTextManager.current_font;
            t.fontSize = 9;
            t.color = new Color(0.85f, 0.85f, 0.9f);
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            t.transform.SetParent(obj.transform, false);
            RectTransform trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }
}
