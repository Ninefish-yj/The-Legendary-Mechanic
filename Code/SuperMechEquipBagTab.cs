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
                tip.textOnClick = "背包\n9级品质装备·词条属性·装备掉落·按品质排序";
                tip.textOnClickDescription = string.Empty;
                tip.text_description_2 = string.Empty;
            }

            // 设置图标
            Image icon = tabObj.GetComponentInChildren<Image>();
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

            // 当前装备：大图标+品质边框（参考原版装备槽）
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                var cur = SuperMechRelic.Equipments[currentIdx];
                Color qColor = GetQualityColor(cur.qualityLevel);

                var equipSlot = CreateItemIcon(_container.transform, cur.icon, qColor, 48, cur.name,
                    $"品质: {GetQualityName(cur.qualityLevel)}\n伤害×{cur.dmgMul}  生命×{cur.hpMul}\n点击卸下");
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

            // 分隔：空行
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(_container.transform, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 8);

            // 背包物品：图标网格（每行5个，按品质从高到低排序）
            var bag = SuperMechEquipBag.GetBag(actor);
            if (bag.Count > 0)
            {
                var sortedBag = bag.OrderByDescending(id =>
                {
                    int idx = SuperMechRelic.GetEquipIndex(id);
                    return idx >= 0 ? SuperMechRelic.Equipments[idx].qualityLevel : -1;
                }).ToList();

                GameObject currentRow = null;
                int iconIndex = 0;
                foreach (string equipId in sortedBag)
                {
                    if (iconIndex % 5 == 0)
                    {
                        currentRow = new GameObject("BagRow", typeof(RectTransform));
                        currentRow.transform.SetParent(_container.transform, false);
                        var rowLayout = currentRow.AddComponent<HorizontalLayoutGroup>();
                        rowLayout.spacing = 4;
                        rowLayout.childAlignment = TextAnchor.UpperLeft;
                        currentRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
                    }

                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx < 0) continue;
                    var def = SuperMechRelic.Equipments[idx];
                    Color qColor = GetQualityColor(def.qualityLevel);

                    var iconGo = CreateItemIcon(currentRow.transform, def.icon, qColor, 34, def.name,
                        $"品质: {GetQualityName(def.qualityLevel)}\n伤害×{def.dmgMul}  生命×{def.hpMul}\n点击装备");
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

        /// <summary>品质简称（角落标签用）。</summary>
        private static string GetQualityShortName(int q)
        {
            switch (q)
            {
                case 0: return "灰";
                case 1: return "绿";
                case 2: return "蓝";
                case 3: return "淡紫";
                case 4: return "紫";
                case 5: return "粉";
                case 6: return "橙";
                case 7: return "银";
                case 8: return "金";
                default: return "?";
            }
        }

        /// <summary>原著9级品质名。</summary>
        private static string GetQualityName(int q)
        {
            string[] names = { "普通", "精良", "稀有", "史诗", "传说", "珍稀", "神器", "使徒兵器", "宇宙宝物" };
            return q >= 0 && q < names.Length ? names[q] : "?";
        }

        private static void AddText(Transform parent, string text, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 4;
            le.preferredHeight = fontSize + 4;
            Text t = obj.AddComponent<Text>();
            t.font = LocalizedTextManager.current_font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.text = text;
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

        /// <summary>添加图标（带品质颜色tint）。</summary>
        private static void AddIcon(Transform parent, string iconPath, Color tint, int size = 20)
        {
            GameObject obj = new GameObject("Icon", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minWidth = size;
            le.preferredWidth = size;
            le.minHeight = size;
            le.preferredHeight = size;

            Image img = obj.AddComponent<Image>();
            img.color = tint;
            try
            {
                Sprite sprite = SpriteTextureLoader.getSprite(iconPath);
                if (sprite != null) img.sprite = sprite;
            }
            catch { }
        }

        /// <summary>添加文字到指定父物体（不强制撑满宽度）。</summary>
        private static void AddTextTo(Transform parent, string text, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 4;
            le.preferredHeight = fontSize + 4;
            le.flexibleWidth = 1;
            Text t = obj.AddComponent<Text>();
            t.font = LocalizedTextManager.current_font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.text = text;
        }
    }
}
