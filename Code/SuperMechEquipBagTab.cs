using System.Collections.Generic;
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

            tab.gameObject.SetActive(true);
            try { tab.toggleActive(true); } catch { }
            EnsureContainer(scroll);
            WireTab(tab, scroll);
            RegisterCallbacks(scroll);

            if (_container != null && !tab.tab_elements.Contains(_container.transform))
            {
                tab.tab_elements.Clear();
                tab.tab_elements.Add(_container.transform);
            }

            bool onBag = scroll.tabs != null && scroll.tabs.isActiveTab(tab);
            if (!onBag && _container != null) _container.SetActive(false);

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

            // 参考天人武道：克隆到source的父对象，设置兄弟索引
            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            // 清空而不是new（参考天人武道）
            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

            // 重置tab_action
            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ => scroll.tabs.showTab(newTab));

            newTab.gameObject.SetActive(true);

            // 用TipButton设置文本（参考天人武道，不用反射）
            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = "装备背包";
                tip.textOnClickDescription = "查看与管理单位的装备背包";
                tip.text_description_2 = string.Empty;
            }

            // 设置图标
            Image icon = tabObj.GetComponentInChildren<Image>();
            if (icon != null)
            {
                try { icon.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconArmor"); } catch { }
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
            if (tab != _bagTab) { if (_container != null) _container.SetActive(false); return; }
            if (_container != null)
            {
                _container.SetActive(true);
                Actor actor = GetActor(_boundWindow);
                if (actor != null) RenderBag(actor);
            }
        }

        private static void OnTabHide()
        {
            if (_container != null) _container.SetActive(false);
        }

        /// <summary>渲染背包内容。</summary>
        private static void RenderBag(Actor actor)
        {
            if (_container == null || actor == null) return;

            // 清空旧内容
            foreach (Transform child in _container.transform)
            {
                if (child.name != "LayoutGroup") Object.Destroy(child.gameObject);
            }

            // 标题
            AddText(_container.transform, $"装备背包（{SuperMechEquipBag.GetBag(actor).Count}/{SuperMechEquipBag.MaxBagSize}）", 12, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.55f));

            // 当前装备（带品质颜色和详情）
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                var cur = SuperMechRelic.Equipments[currentIdx];
                Color qColor = GetQualityColor(cur.qualityLevel);
                AddText(_container.transform, $"当前装备：{cur.name}", 11, TextAnchor.MiddleLeft, qColor);
                AddText(_container.transform, $"  伤害×{cur.dmgMul}  生命×{cur.hpMul}  品质：{GetQualityName(cur.qualityLevel)}", 9, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

                // 装备词条（随机属性）
                var affixes = SuperMechEquipAffix.GetAffixes(actor);
                if (affixes.Count > 0)
                {
                    AddText(_container.transform, $"  词条（{affixes.Count}）：", 9, TextAnchor.MiddleLeft, new Color(0.85f, 0.75f, 0.4f));
                    foreach (var affix in affixes)
                    {
                        string valText = affix.isMultiplier ? $"+{(affix.value * 100):0}%" : $"+{affix.value:0.##}";
                        AddText(_container.transform, $"    · {affix.name} {valText}", 8, TextAnchor.MiddleLeft, new Color(0.75f, 0.7f, 0.55f));
                    }
                }

                AddButton(_container.transform, "卸下当前装备", () =>
                {
                    SuperMechEquipBag.UnequipToBag(actor);
                    RenderBag(actor);
                });
            }
            else
            {
                AddText(_container.transform, "当前装备：无", 10, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));
            }

            // 分隔线
            AddText(_container.transform, "—— 背包 ——", 10, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));

            // 背包物品列表（带品质颜色、详情、丢弃按钮）
            var bag = SuperMechEquipBag.GetBag(actor);
            if (bag.Count == 0)
            {
                AddText(_container.transform, "（空）", 10, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
            }
            else
            {
                foreach (string equipId in bag)
                {
                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx < 0) continue;
                    var def = SuperMechRelic.Equipments[idx];
                    Color qColor = GetQualityColor(def.qualityLevel);

                    // 装备行：图标+名称
                    var equipRow = new GameObject("EquipRow", typeof(RectTransform));
                    equipRow.transform.SetParent(_container.transform, false);
                    var eqLayout = equipRow.AddComponent<HorizontalLayoutGroup>();
                    eqLayout.spacing = 6;
                    eqLayout.childForceExpandWidth = true;
                    RectTransform ert = equipRow.GetComponent<RectTransform>();
                    ert.sizeDelta = new Vector2(0, 24);

                    // 图标
                    AddIcon(eqLayout.transform, def.icon, qColor);
                    // 名称
                    AddTextTo(eqLayout.transform, def.name, 11, TextAnchor.MiddleLeft, qColor);

                    // 详情
                    AddText(_container.transform, $"  伤害×{def.dmgMul}  生命×{def.hpMul}  {GetQualityName(def.qualityLevel)}", 9, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));

                    // 装备/丢弃按钮行
                    var btnRow = new GameObject("BtnRow", typeof(RectTransform));
                    btnRow.transform.SetParent(_container.transform, false);
                    var hLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
                    hLayout.spacing = 4;
                    hLayout.childForceExpandWidth = true;
                    RectTransform brt = btnRow.GetComponent<RectTransform>();
                    brt.sizeDelta = new Vector2(0, 22);

                    AddButton(btnRow.transform, "装备", () =>
                    {
                        SuperMechEquipBag.EquipFromBag(actor, equipId);
                        RenderBag(actor);
                    });
                    AddButton(btnRow.transform, "丢弃", () =>
                    {
                        SuperMechEquipBag.RemoveFromBag(actor, equipId);
                        RenderBag(actor);
                    });
                }
            }
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
        private static void AddIcon(Transform parent, string iconPath, Color tint)
        {
            GameObject obj = new GameObject("Icon", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.minWidth = 20;
            le.preferredWidth = 20;
            le.minHeight = 20;
            le.preferredHeight = 20;

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
