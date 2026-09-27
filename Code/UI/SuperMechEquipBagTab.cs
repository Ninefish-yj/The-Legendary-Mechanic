using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]


    public static partial class SuperMechEquipBagTab
    {
        private const string TabName = "SuperMechEquipBagTab";
        private const string ContainerName = "SuperMechEquipBagContent";

        private static bool _callbacksRegistered;
        private static WindowMetaTab _bagTab;
        private static GameObject _container;
        private static UnitWindow _boundWindow;
        private static readonly FieldInfo TabsListField =
            AccessTools.Field(typeof(WindowMetaTabButtonsContainer), "_tabs");

        private static EquipmentButton _equipButtonPrefab;
        private static readonly List<EquipmentButton> _activeButtons = new List<EquipmentButton>();
        private static readonly Queue<EquipmentButton> _buttonPool = new Queue<EquipmentButton>();

        public static void Postfix(UnitWindow __instance)
        {
            try { Refresh(__instance); } catch { }
        }

        private static void Refresh(UnitWindow window)
        {
            if (window == null) return;
            Actor actor = SuperMechUtils.GetActor(window);
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
                try { scroll.tabs.GetType().GetMethod("refillTabsWithContent",
                    BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(scroll.tabs, null); } catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 背包Tab反射刷新失败: {e.Message}"); }
            }

            bool onBag = scroll.tabs != null && scroll.tabs.isActiveTab(tab);
            if (onBag) RenderBag(actor);
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

            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetAsLastSibling();

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            DragOrderElement dragElem = newTab.GetComponent<DragOrderElement>();
            if (dragElem != null) Object.DestroyImmediate(dragElem);

            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

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

            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ =>
            {
                scroll.tabs.showTab(newTab);
                if (_boundWindow != null)
                {
                    try { Refresh(_boundWindow); } catch { }
                }
            });

            newTab.gameObject.SetActive(true);

            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = LocalizedTextManager.getText("sm_tab_bag_tip");
                tip.textOnClickDescription = string.Empty;
                tip.text_description_2 = string.Empty;
            }

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
            Actor actor = SuperMechUtils.GetActor(_boundWindow);
            if (actor != null) RenderBag(actor);
        }

        private static void OnTabHide()
        {
        }
    }
}
