using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "OnEnable")]
    public static class SuperMechCustomTabs
    {
        private const string KnowledgeTabId = "SMKnowledgeTab";
        private const string BagTabId = "SMBagTab";
        private const string KnowledgeContentName = "content_sm_knowledge";
        private const string BagContentName = "content_sm_bag";
        private const string KnowledgeIconPath = "ui/iconBook";
        private const string BagIconPath = "ui/iconBackpack";

        private static bool _initialized = false;

        [HarmonyPrefix]
        private static void Prefix(UnitWindow __instance)
        {
            try
            {
                Actor actor = SuperMechUtils.GetActor(__instance);
                if (actor == null || !actor.isAlive()) return;

                if (_initialized) return;
                _initialized = true;

                Transform tabsRoot = __instance.transform.Find("Background/Tabs");
                if (tabsRoot == null) return;

                WindowMetaTab genealogyTab = tabsRoot.Find("Genealogy")?.GetComponent<WindowMetaTab>();
                if (genealogyTab == null) return;

                int genealogyIndex = -1;
                for (int i = 0; i < tabsRoot.childCount; i++)
                {
                    if (tabsRoot.GetChild(i).name.ToLower().Contains("genealogy"))
                    {
                        genealogyIndex = i;
                        break;
                    }
                }
                if (genealogyIndex < 0) genealogyIndex = tabsRoot.childCount;

                CreateKnowledgeTab(__instance, tabsRoot, genealogyTab, genealogyIndex);
                CreateBagTab(__instance, tabsRoot, genealogyTab, genealogyIndex + 1);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 自定义Tab注册失败: " + e.Message);
            }
        }

        private static void CreateKnowledgeTab(UnitWindow window, Transform tabsRoot, WindowMetaTab sourceTab, int index)
        {
            WindowMetaTab customTab = UnityEngine.Object.Instantiate(sourceTab, tabsRoot);
            customTab.name = KnowledgeTabId;
            customTab.tab_action = new WindowMetaTabEvent();
            customTab.tab_action.AddListener(delegate (WindowMetaTab tab)
            {
                window.scroll_window.tabs.showTab(tab);
            });

            Sprite sprite = SpriteTextureLoader.getSprite(KnowledgeIconPath);
            if (sprite != null)
            {
                foreach (Image img in customTab.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name.ToLower().Contains("icon")
                        || (img.transform.parent != null && img.transform.parent.GetComponent<Button>() != null))
                    {
                        img.sprite = sprite;
                        break;
                    }
                }
            }

            TipButton tipBtn = customTab.GetComponentInChildren<TipButton>();
            if (tipBtn != null)
            {
                tipBtn.textOnClick = "sm_ui_knowledge_tab";
                tipBtn.textOnClickDescription = "sm_ui_knowledge_tab_desc";
            }

            customTab.transform.SetSiblingIndex(index);

            customTab.container = window.scroll_window.tabs;
            customTab.tab_elements.RemoveAll((Transform t) => t.name.ToLower().StartsWith("content_"));

            var tabsField = typeof(WindowMetaTabButtonsContainer).GetField("_tabs",
                BindingFlags.NonPublic | BindingFlags.Instance);
            List<WindowMetaTab> tabsList = tabsField?.GetValue(window.scroll_window.tabs) as List<WindowMetaTab>;
            if (tabsList != null && !tabsList.Contains(customTab))
            {
                tabsList.Add(customTab);
            }

            GameObject contentObj = null;

            KnowledgeWindow knowledgeWindow = UnityEngine.Object.FindObjectOfType<KnowledgeWindow>();
            if (knowledgeWindow != null)
            {
                var prefabField = typeof(KnowledgeWindow).GetField("_element_prefab",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                KnowledgeElement elementPrefab = prefabField?.GetValue(knowledgeWindow) as KnowledgeElement;
                if (elementPrefab != null)
                {
                    contentObj = UnityEngine.Object.Instantiate(
                        elementPrefab.gameObject,
                        window.transform.Find("Background/Scroll View/Viewport/Content"));
                    contentObj.name = KnowledgeContentName;

                    KnowledgeElement keComp = contentObj.GetComponent<KnowledgeElement>();
                    if (keComp != null)
                        UnityEngine.Object.DestroyImmediate(keComp);
                }
            }

            if (contentObj == null)
            {
                UnitGenealogyElement genealogyElement = window.transform.GetComponentInChildren<UnitGenealogyElement>(true);
                if (genealogyElement == null) return;

                contentObj = UnityEngine.Object.Instantiate(
                    genealogyElement.gameObject,
                    window.transform.Find("Background/Scroll View/Viewport/Content"));
                contentObj.name = KnowledgeContentName;

                UnitGenealogyElement genealogyComp = contentObj.GetComponent<UnitGenealogyElement>();
                if (genealogyComp != null)
                    UnityEngine.Object.DestroyImmediate(genealogyComp);
            }

            foreach (var old in contentObj.GetComponents<LayoutGroup>())
                UnityEngine.Object.DestroyImmediate(old);
            foreach (var old in contentObj.GetComponents<ContentSizeFitter>())
                UnityEngine.Object.DestroyImmediate(old);

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 800f);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SMKnowledgeWindow.Create(contentObj.transform, SuperMechUtils.GetActor(window));

            window.scroll_window.tabs.addTabContent(customTab, contentObj.transform);
            window.scroll_window.tabs.refillTabsWithContent();
        }

        private static void CreateBagTab(UnitWindow window, Transform tabsRoot, WindowMetaTab sourceTab, int index)
        {
            WindowMetaTab customTab = UnityEngine.Object.Instantiate(sourceTab, tabsRoot);
            customTab.name = BagTabId;
            customTab.tab_action = new WindowMetaTabEvent();
            customTab.tab_action.AddListener(delegate (WindowMetaTab tab)
            {
                window.scroll_window.tabs.showTab(tab);
            });

            Sprite sprite = SpriteTextureLoader.getSprite(BagIconPath);
            if (sprite == null)
            {
                sprite = SpriteTextureLoader.getSprite("ui/Icons/iconBackpack");
            }
            if (sprite == null)
            {
                sprite = SpriteTextureLoader.getSprite("ui/iconBox");
            }
            if (sprite != null)
            {
                foreach (Image img in customTab.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name.ToLower().Contains("icon")
                        || (img.transform.parent != null && img.transform.parent.GetComponent<Button>() != null))
                    {
                        img.sprite = sprite;
                        break;
                    }
                }
            }

            TipButton tipBtn = customTab.GetComponentInChildren<TipButton>();
            if (tipBtn != null)
            {
                tipBtn.textOnClick = "sm_ui_bag_tab";
                tipBtn.textOnClickDescription = "sm_ui_bag_tab_desc";
            }

            customTab.transform.SetSiblingIndex(index);

            customTab.container = window.scroll_window.tabs;
            customTab.tab_elements.RemoveAll((Transform t) => t.name.ToLower().StartsWith("content_"));

            var tabsField = typeof(WindowMetaTabButtonsContainer).GetField("_tabs",
                BindingFlags.NonPublic | BindingFlags.Instance);
            List<WindowMetaTab> tabsList = tabsField?.GetValue(window.scroll_window.tabs) as List<WindowMetaTab>;
            if (tabsList != null && !tabsList.Contains(customTab))
            {
                tabsList.Add(customTab);
            }

            UnitGenealogyElement genealogyElement2 = window.transform.GetComponentInChildren<UnitGenealogyElement>(true);
            if (genealogyElement2 == null) return;

            GameObject contentObj = UnityEngine.Object.Instantiate(
                genealogyElement2.gameObject,
                window.transform.Find("Background/Scroll View/Viewport/Content"));
            contentObj.name = BagContentName;

            UnitGenealogyElement genealogyComp2 = contentObj.GetComponent<UnitGenealogyElement>();
            if (genealogyComp2 != null)
            {
                UnityEngine.Object.DestroyImmediate(genealogyComp2);
            }

            foreach (var old in contentObj.GetComponents<LayoutGroup>())
                UnityEngine.Object.DestroyImmediate(old);
            foreach (var old in contentObj.GetComponents<ContentSizeFitter>())
                UnityEngine.Object.DestroyImmediate(old);

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, 600f);

            RenderBagContent(contentObj.transform, SuperMechUtils.GetActor(window));

            window.scroll_window.tabs.addTabContent(customTab, contentObj.transform);
            window.scroll_window.tabs.refillTabsWithContent();
        }

        private static void RenderBagContent(Transform parent, Actor actor)
        {
            if (parent == null || actor == null) return;

            VerticalLayoutGroup vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = parent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject equipTitle = new GameObject("EquipTitle", typeof(RectTransform));
            equipTitle.transform.SetParent(parent, false);
            Text equipTxt = SuperMechUtils.CreateText(equipTitle.transform, LocalizedTextManager.getText("sm_ui_equipment"), 12, TextAnchor.MiddleLeft, new Color(0.3f, 0.85f, 1f));
            equipTxt.fontStyle = FontStyle.Bold;
            RectTransform equipRt = equipTitle.GetComponent<RectTransform>();
            equipRt.sizeDelta = new Vector2(0, 24f);

            GameObject equipGrid = new GameObject("EquipGrid", typeof(RectTransform));
            equipGrid.transform.SetParent(parent, false);
            GridLayoutGroup equipGlg = equipGrid.AddComponent<GridLayoutGroup>();
            equipGlg.cellSize = new Vector2(48, 48);
            equipGlg.spacing = new Vector2(6, 6);
            equipGlg.constraint = GridLayoutGroup.Constraint.Flexible;
            equipGlg.childAlignment = TextAnchor.UpperLeft;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(actor);
            if (currentIdx >= 0)
            {
                CreateEquipIcon(equipGrid.transform, SuperMechRelic.Equipments[currentIdx], true, actor);
            }

            var bag = SuperMechEquipBag.GetBag(actor);
            if (bag != null)
            {
                foreach (string equipId in bag)
                {
                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx >= 0 && idx != currentIdx)
                    {
                        CreateEquipIcon(equipGrid.transform, SuperMechRelic.Equipments[idx], false, actor);
                    }
                }
            }

            GameObject resTitle = new GameObject("ResTitle", typeof(RectTransform));
            resTitle.transform.SetParent(parent, false);
            Text resTxt = SuperMechUtils.CreateText(resTitle.transform, LocalizedTextManager.getText("sm_ui_resources"), 12, TextAnchor.MiddleLeft, new Color(0.3f, 0.85f, 1f));
            resTxt.fontStyle = FontStyle.Bold;
            RectTransform resRt = resTitle.GetComponent<RectTransform>();
            resRt.sizeDelta = new Vector2(0, 24f);

            GameObject resGrid = new GameObject("ResGrid", typeof(RectTransform));
            resGrid.transform.SetParent(parent, false);
            GridLayoutGroup resGlg = resGrid.AddComponent<GridLayoutGroup>();
            resGlg.cellSize = new Vector2(48, 48);
            resGlg.spacing = new Vector2(6, 6);
            resGlg.constraint = GridLayoutGroup.Constraint.Flexible;
            resGlg.childAlignment = TextAnchor.UpperLeft;

            SMUnitBag.Render(resGrid.transform, actor);
        }

        private static void CreateEquipIcon(Transform parent, SuperMechRelic.EquipDef equip, bool isCurrent, Actor actor)
        {
            GameObject go = new GameObject("Equip_" + equip.id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);

            Image bg = go.AddComponent<Image>();
            bg.color = isCurrent ? new Color(1f, 0.84f, 0f, 0.3f) : new Color(0.05f, 0.12f, 0.18f, 0.6f);
            bg.raycastTarget = true;

            Image icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(go.transform, false);
            icon.raycastTarget = false;
            try
            {
                Sprite sprite = SpriteTextureLoader.getSprite(equip.icon);
                if (sprite != null) icon.sprite = sprite;
            }
            catch { }
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);

            Image border = new GameObject("Border", typeof(RectTransform)).AddComponent<Image>();
            border.transform.SetParent(go.transform, false);
            border.raycastTarget = false;
            border.color = GetQualityColor(equip.qualityLevel);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1f, -1f);
            borderRt.offsetMax = new Vector2(1f, 1f);

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (isCurrent)
                {
                    SuperMechEquipBag.UnequipToBag(actor);
                }
                else
                {
                    SuperMechEquipBag.EquipFromBag(actor, equip.id);
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
