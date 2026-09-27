using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal static partial class SuperMechKnowledgePanel
    {
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

            AddBoxFrame(graph, new Color(0.02f, 0.06f, 0.06f, 0.95f), new Color(0.11f, 0.48f, 0.45f, 0.6f));

            GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform));
            titleBar.transform.SetParent(graph.transform, false);
            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.05f, 0.15f, 0.14f, 0.95f);
            titleBg.raycastTarget = false;
            RectTransform titleBarRt = titleBar.GetComponent<RectTransform>();
            titleBarRt.anchorMin = new Vector2(0, 1);
            titleBarRt.anchorMax = new Vector2(1, 1);
            titleBarRt.pivot = new Vector2(0.5f, 1f);
            titleBarRt.sizeDelta = new Vector2(0, 20f);

            GameObject titleGo = new GameObject("GraphTitle", typeof(RectTransform));
            titleGo.transform.SetParent(titleBar.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = LocalizedTextManager.getText("sm_knowledgepanel_788");
            titleText.fontSize = 11;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.6f, 0.95f, 0.9f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            _graphTitle = titleText;

            GameObject graphContent = new GameObject("GraphContent", typeof(RectTransform));
            graphContent.transform.SetParent(graph.transform, false);
            RectTransform gcRt = graphContent.GetComponent<RectTransform>();
            gcRt.anchorMin = Vector2.zero;
            gcRt.anchorMax = new Vector2(1, 1);
            gcRt.pivot = new Vector2(0.5f, 0.5f);
            gcRt.offsetMin = new Vector2(2, 2);
            gcRt.offsetMax = new Vector2(-2, -22);

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
            titleTxt.fontSize = 11;
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
    }
}
