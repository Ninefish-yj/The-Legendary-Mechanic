using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识树面板：参考天人武道神藏Tab，在单位面板添加"知识"Tab。
    /// 显示单位已解锁的知识节点（按系/分支/阶位分组），不再注册为特质。
    /// </summary>
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechKnowledgeTab
    {
        private const string TabName = "SuperMechKnowledgeTab";
        private const string ContainerName = "SuperMechKnowledgeContent";

        private static bool _callbacksRegistered;
        private static WindowMetaTab _knowTab;
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
            if (!SuperMechAdvancement.IsSuperMechUnit(actor)) return;

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

            RenderContent(actor);
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                FieldInfo fi = typeof(UnitWindow).GetField("_actor", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null) return fi.GetValue(window) as Actor;
                PropertyInfo pi = typeof(UnitWindow).GetProperty("actor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null) return pi.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        private static WindowMetaTab FindOrCreateTab(ScrollWindow scroll, UnitWindow window)
        {
            if (_knowTab != null && _boundWindow == window) return _knowTab;

            foreach (WindowMetaTab t in scroll.tabs.GetComponentsInChildren<WindowMetaTab>(true))
            {
                if (t.name == TabName) { _knowTab = t; _boundWindow = window; return t; }
            }

            WindowMetaTab source = FindCloneSource(scroll.tabs);
            if (source == null) return null;

            GameObject tabObj = Object.Instantiate(source.gameObject, scroll.tabs.transform);
            tabObj.name = TabName;
            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            newTab.id = "knowledge";
            newTab.gameObject.SetActive(true);
            LocalizedTextManager.add("sm_knowledge_tab", "知识", pReplace: true);
            LocalizedTextManager.add("sm_knowledge_tab_desc", "已解锁的职业知识节点", pReplace: true);

            try
            {
                FieldInfo tf = typeof(WindowMetaTab).GetField("_worldtip_text", BindingFlags.Instance | BindingFlags.NonPublic);
                if (tf != null)
                {
                    Text tipText = tf.GetValue(newTab) as Text;
                    if (tipText != null) tipText.text = "知识";
                }
            }
            catch { }

            _knowTab = newTab;
            _boundWindow = window;
            return newTab;
        }

        private static WindowMetaTab FindCloneSource(WindowMetaTabButtonsContainer container)
        {
            WindowMetaTab[] all = container.GetComponentsInChildren<WindowMetaTab>(true);
            foreach (WindowMetaTab t in all)
            {
                if (t.name != TabName && t.gameObject.activeSelf) return t;
            }
            return all.Length > 0 ? all[0] : null;
        }

        private static void EnsureContainer(ScrollWindow scroll)
        {
            if (_container != null) return;
            _container = new GameObject(ContainerName, typeof(RectTransform));
            _container.transform.SetParent(scroll.content.transform, false);
            RectTransform rt = _container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 600);
            _container.SetActive(false);

            var layout = _container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
        }

        private static void WireTab(WindowMetaTab tab, ScrollWindow scroll)
        {
            if (_container == null) return;
            tab.toggle = tab.GetComponent<Toggle>();
            if (tab.toggle != null)
            {
                tab.toggle.onValueChanged.RemoveAllListeners();
                tab.toggle.onValueChanged.AddListener(on =>
                {
                    if (_container != null) _container.SetActive(on);
                    if (on) { scroll.content.gameObject.SetActive(false); }
                    else { scroll.content.gameObject.SetActive(true); }
                });
            }
        }

        private static void RegisterCallbacks(ScrollWindow scroll)
        {
            if (_callbacksRegistered) return;
            _callbacksRegistered = true;
        }

        private static void RenderContent(Actor actor)
        {
            if (_container == null) return;
            foreach (Transform child in _container.transform) Object.Destroy(child.gameObject);

            string cls = SuperMechBranch.GetClass(actor);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            string treeName = GetTreeName(prefix);

            // 标题
            AddHeader(_container.transform, $"{treeName}（{cls}）");

            // 潜能点/觉醒点
            int pot = SuperMechPotential.GetPotential(actor);
            int awk = SuperMechPotential.GetAwakening(actor);
            AddInfoRow(_container.transform, $"潜能点: {pot}", $"觉醒点: {awk}");

            // 按阶位分组显示已解锁知识
            string[] tierNames = { "基础", "进阶", "高端", "尖端", "终极" };
            string[] branchNames = GetBranchNames(prefix);

            int totalUnlocked = 0;
            for (int tier = 0; tier < 5; tier++)
            {
                int tierCount = SuperMechKnowledge.GetTierKnowledgeCount(actor, prefix, tier);
                if (tierCount == 0) continue;
                totalUnlocked += tierCount;

                AddSectionHeader(_container.transform, $"{tierNames[tier]}知识（{tierCount}项）");

                var unlocked = SuperMechKnowledge.GetUnlockedList(actor, prefix);
                foreach (var def in unlocked)
                {
                    if (def.tier != tier) continue;
                    string branchName = def.branch < branchNames.Length ? branchNames[def.branch] : "?";
                    AddKnowledgeRow(_container.transform, def.name, branchName, def.cost);
                }
            }

            if (totalUnlocked == 0)
            {
                AddInfoRow(_container.transform, "尚未解锁任何知识", "用神权解锁知识·武装/能量/虚拟系");
            }

            // 总计
            AddHeader(_container.transform, $"已解锁: {totalUnlocked} / 249");
        }

        private static string GetTreeName(string prefix)
        {
            switch (prefix)
            {
                case "mech": return "机械知识树";
                case "martial": return "御气技巧树";
                case "mage": return "魔法知识树";
                case "mind": return "精神修炼树";
                case "psi": return "基因树";
                default: return "知识树";
            }
        }

        private static string[] GetBranchNames(string prefix)
        {
            switch (prefix)
            {
                case "mech": return new[] { "枪炮师", "机械师", "械武者" };
                case "martial": return new[] { "敏捷", "力量", "防御" };
                case "mage": return new[] { "专精法师", "魔网法师", "元素" };
                case "mind": return new[] { "灵魂", "法则", "现实" };
                case "psi": return new[] { "能级", "操控", "持久力" };
                default: return new[] { "?", "?", "?" };
            }
        }

        private static void AddHeader(Transform parent, string text)
        {
            GameObject go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.84f, 0f);
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28);
        }

        private static void AddSectionHeader(Transform parent, string text)
        {
            GameObject go = new GameObject("Section", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.6f, 0.8f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 22);
        }

        private static void AddInfoRow(Transform parent, string left, string right)
        {
            GameObject go = new GameObject("InfoRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;

            GameObject leftGo = new GameObject("Left", typeof(RectTransform));
            leftGo.transform.SetParent(go.transform, false);
            Text lt = leftGo.AddComponent<Text>();
            lt.text = left;
            lt.fontSize = 12;
            lt.color = Color.white;
            if (lt.font == null) lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rightGo = new GameObject("Right", typeof(RectTransform));
            rightGo.transform.SetParent(go.transform, false);
            Text rt2 = rightGo.AddComponent<Text>();
            rt2.text = right;
            rt2.fontSize = 12;
            rt2.color = new Color(0.7f, 0.7f, 0.7f);
            rt2.alignment = TextAnchor.MiddleRight;
            if (rt2.font == null) rt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
        }

        private static void AddKnowledgeRow(Transform parent, string name, string branch, int cost)
        {
            GameObject go = new GameObject("Knowledge", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childForceExpandWidth = true;

            GameObject nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(go.transform, false);
            Text nt = nameGo.AddComponent<Text>();
            nt.text = name;
            nt.fontSize = 11;
            nt.color = new Color(0.9f, 0.9f, 0.9f);
            if (nt.font == null) nt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject branchGo = new GameObject("Branch", typeof(RectTransform));
            branchGo.transform.SetParent(go.transform, false);
            Text bt = branchGo.AddComponent<Text>();
            bt.text = branch;
            bt.fontSize = 10;
            bt.color = new Color(0.5f, 0.7f, 1f);
            bt.alignment = TextAnchor.MiddleRight;
            if (bt.font == null) bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 18);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.6f);
        }
    }
}
