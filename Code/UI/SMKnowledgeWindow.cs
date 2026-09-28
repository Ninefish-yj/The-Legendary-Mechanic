using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static class SMKnowledgeWindow
    {
        private static readonly Color[] TierColors = {
            new Color(0.3f, 0.71f, 0.67f),
            new Color(0.15f, 0.65f, 0.6f),
            new Color(0f, 0.54f, 0.48f),
            new Color(0f, 0.47f, 0.42f),
            new Color(0f, 0.3f, 0.25f)
        };

        public static void Create(Transform parent, Actor actor)
        {
            if (parent == null || actor == null) return;

            VerticalLayoutGroup vlg = parent.gameObject.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = parent.gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = parent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(actor));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var allKnowledge = SuperMechKnowledge.GetAllByPrefix(prefix);
            int unlockedCount = SuperMechKnowledge.GetUnlockedCount(actor, prefix);

            CreateProgressBar(parent, unlockedCount, allKnowledge.Count);

            GameObject graphObj = new GameObject("CubeOverview", typeof(RectTransform));
            graphObj.transform.SetParent(parent, false);
            RectTransform graphRt = graphObj.GetComponent<RectTransform>();
            graphRt.sizeDelta = new Vector2(0, 450f);
            SMCubeKnowledge graph = graphObj.AddComponent<SMCubeKnowledge>();
            graph.Init(actor);

            GameObject elementsParent = new GameObject("ElementsParent", typeof(RectTransform));
            elementsParent.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = elementsParent.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            ContentSizeFitter elementsCsf = elementsParent.AddComponent<ContentSizeFitter>();
            elementsCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            elementsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int tier = 0; tier <= 4; tier++)
            {
                var tierKnowledge = allKnowledge.FindAll(k => k.tier == tier);
                if (tierKnowledge.Count == 0) continue;

                CreateKnowledgeElement(elementsParent.transform, actor, tier, tierKnowledge, prefix);
            }
        }

        private static void CreateProgressBar(Transform parent, int unlocked, int total)
        {
            GameObject barObj = new GameObject("ProgressBar", typeof(RectTransform));
            barObj.transform.SetParent(parent, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0, 28f);

            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            GameObject maskObj = new GameObject("Mask", typeof(RectTransform));
            maskObj.transform.SetParent(barObj.transform, false);
            Image maskImg = maskObj.AddComponent<Image>();
            maskImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            RectTransform maskRt = maskObj.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = new Vector2(2, 2);
            maskRt.offsetMax = new Vector2(-2, -2);
            Mask maskComp = maskObj.AddComponent<Mask>();
            maskComp.showMaskGraphic = false;

            GameObject fillObj = new GameObject("Bar", typeof(RectTransform));
            fillObj.transform.SetParent(maskObj.transform, false);
            Image fill = fillObj.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 1f, 0.8f);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.sizeDelta = new Vector2(0, 0);

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(barObj.transform, false);
            Text text = SuperMechUtils.CreateText(textObj.transform,
                $"{unlocked}/{total}", 12, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            StatBar statBar = barObj.AddComponent<StatBar>();
            statBar.textField = text;
            statBar.mask = maskRt;
            statBar.bar = fillRt;
            statBar.setBar(unlocked, total, "/" + total);
        }

        private static void CreateKnowledgeElement(Transform parent, Actor actor, int tier, List<SuperMechKnowledge.KnowledgeDef> knowledge, string prefix)
        {
            try
            {
            GameObject elementObj = null;

            KnowledgeWindow knowledgeWindow = UnityEngine.Object.FindObjectOfType<KnowledgeWindow>();
            if (knowledgeWindow != null)
            {
                KnowledgeElement elementPrefab = SuperMechReflection.GetFieldValue<KnowledgeElement>(knowledgeWindow, "_element_prefab");
                if (elementPrefab != null)
                {
                    elementObj = UnityEngine.Object.Instantiate(elementPrefab.gameObject, parent, false);
                    elementObj.name = $"KnowledgeElement_Tier{tier}";
                    RectTransform elementRt = elementObj.GetComponent<RectTransform>();
                    elementRt.sizeDelta = new Vector2(160f, 0f);
                    LayoutElement le = elementObj.GetComponent<LayoutElement>();
                    if (le == null) le = elementObj.AddComponent<LayoutElement>();
                    le.preferredWidth = 160f;
                    le.flexibleWidth = 0f;

                    KnowledgeElement keComp = elementObj.GetComponent<KnowledgeElement>();
                    if (keComp != null)
                    {
                        LocalizedText locText = SuperMechReflection.GetFieldValue<LocalizedText>(keComp, "_localized_text");
                        Image iconLeft = SuperMechReflection.GetFieldValue<Image>(keComp, "_icon_left");
                        StatBar progressBar = SuperMechReflection.GetFieldValue<StatBar>(keComp, "_progress_bar");
                        RunningIcons runningIcons = SuperMechReflection.GetFieldValue<RunningIcons>(keComp, "_running_icons");

                        UnityEngine.Object.DestroyImmediate(keComp);

                        int tierUnlocked = knowledge.FindAll(k => SuperMechKnowledge.IsUnlocked(actor, k.id)).Count;

                        if (locText != null)
                        {
                            locText.gameObject.SetActive(true);
                            Text textComp = locText.GetComponent<Text>();
                            if (textComp != null)
                            {
                                textComp.text = $"{LocalizedTextManager.getText("sm_ui_tier")} {tier + 1}";
                                textComp.color = TierColors[tier];
                                textComp.fontStyle = FontStyle.Bold;
                            }
                        }

                        if (iconLeft != null)
                        {
                            iconLeft.gameObject.SetActive(true);
                            iconLeft.color = TierColors[tier];
                        }

                        if (progressBar != null)
                        {
                            progressBar.gameObject.SetActive(true);
                            progressBar.setBar(tierUnlocked, knowledge.Count, "/" + knowledge.Count);
                        }

                        if (runningIcons != null)
                        {
                            runningIcons.gameObject.SetActive(true);
                            foreach (Transform child in runningIcons.transform)
                            {
                                UnityEngine.Object.DestroyImmediate(child.gameObject);
                            }

                            HorizontalLayoutGroup hlgIcons = runningIcons.GetComponent<HorizontalLayoutGroup>();
                            if (hlgIcons == null) hlgIcons = runningIcons.gameObject.AddComponent<HorizontalLayoutGroup>();
                            hlgIcons.spacing = 3f;
                            hlgIcons.childAlignment = TextAnchor.MiddleLeft;
                            hlgIcons.childControlHeight = true;
                            hlgIcons.childControlWidth = false;
                            hlgIcons.childForceExpandHeight = false;
                            hlgIcons.childForceExpandWidth = false;
                            ContentSizeFitter iconsCsf = runningIcons.GetComponent<ContentSizeFitter>();
                            if (iconsCsf == null) iconsCsf = runningIcons.gameObject.AddComponent<ContentSizeFitter>();
                            iconsCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                            iconsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                            int displayCount = Mathf.Min(knowledge.Count, 6);
                            for (int ki = 0; ki < displayCount; ki++)
                            {
                                var k = knowledge[ki];
                                bool unlocked = SuperMechKnowledge.IsUnlocked(actor, k.id);
                                Color tierColor = TierColors[tier];
                                Color borderColor = unlocked ? tierColor : new Color(0.3f, 0.3f, 0.3f, 0.6f);
                                Color innerColor = unlocked ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.5f);

                                GameObject iconObj = new GameObject(k.id, typeof(RectTransform));
                                iconObj.transform.SetParent(runningIcons.transform, false);
                                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                                iconRt.sizeDelta = new Vector2(24, 24);

                                Image borderImg = iconObj.AddComponent<Image>();
                                borderImg.color = borderColor;
                                borderImg.sprite = SpriteTextureLoader.getSprite("ui/special/special_circle");
                                if (borderImg.sprite == null)
                                {
                                    borderImg.sprite = SpriteTextureLoader.getSprite("ui/special/special_square");
                                }
                                borderImg.type = Image.Type.Sliced;

                                GameObject innerObj = new GameObject("Inner", typeof(RectTransform));
                                innerObj.transform.SetParent(iconObj.transform, false);
                                RectTransform innerRt = innerObj.GetComponent<RectTransform>();
                                innerRt.anchorMin = new Vector2(0.15f, 0.15f);
                                innerRt.anchorMax = new Vector2(0.85f, 0.85f);
                                innerRt.offsetMin = Vector2.zero;
                                innerRt.offsetMax = Vector2.zero;
                                Image innerImg = innerObj.AddComponent<Image>();
                                innerImg.color = innerColor;
                                try
                                {
                                    Sprite sprite = SpriteTextureLoader.getSprite(k.icon);
                                    if (sprite != null)
                                    {
                                        innerImg.sprite = sprite;
                                        innerImg.color = Color.white;
                                    }
                                }
                                catch { }

                                if (unlocked)
                                {
                                    Outline outline = iconObj.AddComponent<Outline>();
                                    outline.effectColor = new Color(tierColor.r, tierColor.g, tierColor.b, 0.8f);
                                    outline.effectDistance = new Vector2(1, 1);
                                }

                                Button iconBtn = iconObj.AddComponent<Button>();
                                string kid = k.id;
                                iconBtn.onClick.AddListener(() =>
                                {
                                    if (!unlocked && actor != null)
                                    {
                                        SuperMechKnowledge.Unlock(actor, kid);
                                    }
                                });

                                TipButton tipBtn = iconObj.AddComponent<TipButton>();
                                tipBtn.textOnClick = k.id;
                                tipBtn.textOnClickDescription = k.desc;
                            }

                            if (knowledge.Count > 6)
                            {
                                GameObject moreObj = new GameObject("More", typeof(RectTransform));
                                moreObj.transform.SetParent(runningIcons.transform, false);
                                RectTransform moreRt = moreObj.GetComponent<RectTransform>();
                                moreRt.sizeDelta = new Vector2(24, 24);
                                Image moreImg = moreObj.AddComponent<Image>();
                                moreImg.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                                Text moreText = SuperMechUtils.CreateText(moreObj.transform,
                                    $"+{knowledge.Count - 6}", 10, TextAnchor.MiddleCenter, Color.white);
                                moreText.transform.SetParent(moreObj.transform, false);
                            }
                        }
                    }
                }
            }

            if (elementObj == null)
            {
                elementObj = new GameObject($"KnowledgeElement_Tier{tier}", typeof(RectTransform));
                elementObj.transform.SetParent(parent, false);
                Image bg = elementObj.AddComponent<Image>();
                bg.color = new Color(0.05f, 0.08f, 0.1f, 0.7f);
                VerticalLayoutGroup vlg = elementObj.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 6f;
                vlg.padding = new RectOffset(8, 8, 6, 6);
                int tierUnlocked = knowledge.FindAll(k => SuperMechKnowledge.IsUnlocked(actor, k.id)).Count;
                SuperMechUtils.CreateText(elementObj.transform,
                    $"{LocalizedTextManager.getText("sm_ui_tier")} {tier + 1}  {tierUnlocked}/{knowledge.Count}",
                    13, TextAnchor.MiddleLeft, TierColors[tier]);
                GridLayoutGroup glg = elementObj.AddComponent<GridLayoutGroup>();
                glg.cellSize = new Vector2(24, 24);
                glg.spacing = new Vector2(4, 4);
                foreach (var k in knowledge)
                {
                    bool unlocked = SuperMechKnowledge.IsUnlocked(actor, k.id);
                    GameObject iconObj = new GameObject(k.id, typeof(RectTransform));
                    iconObj.transform.SetParent(elementObj.transform, false);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.color = unlocked ? TierColors[tier] : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 创建知识分类元素失败: " + e.Message);
            }
        }
    }
}
