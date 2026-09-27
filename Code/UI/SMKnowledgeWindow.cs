using System;
using System.Collections.Generic;
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

            VerticalLayoutGroup vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = parent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(actor));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var allKnowledge = SuperMechKnowledge.GetAllByPrefix(prefix);
            int unlockedCount = SuperMechKnowledge.GetUnlockedCount(actor, prefix);

            CreateProgressBar(parent, unlockedCount, allKnowledge.Count);

            GameObject graphObj = new GameObject("CubeOverview", typeof(RectTransform));
            graphObj.transform.SetParent(parent, false);
            RectTransform graphRt = graphObj.GetComponent<RectTransform>();
            graphRt.sizeDelta = new Vector2(0, 320f);
            SMCubeKnowledge graph = graphObj.AddComponent<SMCubeKnowledge>();
            graph.Init(actor);

            for (int tier = 0; tier <= 4; tier++)
            {
                var tierKnowledge = allKnowledge.FindAll(k => k.tier == tier);
                if (tierKnowledge.Count == 0) continue;

                CreateKnowledgeElement(parent, actor, tier, tierKnowledge, prefix);
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
            Mask mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(maskObj.transform, false);
            Image fill = fillObj.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 1f, 0.8f);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            float ratio = total > 0 ? (float)unlocked / total : 0;
            fillRt.sizeDelta = new Vector2(maskRt.rect.width * ratio, 0);

            Text text = SuperMechUtils.CreateText(barObj.transform,
                $"{unlocked}/{total}", 12, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private static void CreateKnowledgeElement(Transform parent, Actor actor, int tier, List<SuperMechKnowledge.KnowledgeDef> knowledge, string prefix)
        {
            GameObject elementObj = new GameObject($"KnowledgeElement_Tier{tier}", typeof(RectTransform));
            elementObj.transform.SetParent(parent, false);
            RectTransform elementRt = elementObj.GetComponent<RectTransform>();
            elementRt.sizeDelta = new Vector2(0, 120f);

            Image bg = elementObj.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.1f, 0.7f);

            VerticalLayoutGroup vlg = elementObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.childAlignment = TextAnchor.UpperLeft;

            GameObject headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(elementObj.transform, false);
            HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = false;
            hlg.childControlWidth = false;
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(0, 28f);

            Image leftIcon = new GameObject("IconLeft", typeof(RectTransform)).AddComponent<Image>();
            leftIcon.transform.SetParent(headerObj.transform, false);
            leftIcon.color = TierColors[tier];
            RectTransform leftIconRt = leftIcon.GetComponent<RectTransform>();
            leftIconRt.sizeDelta = new Vector2(24, 24);

            int tierUnlocked = knowledge.FindAll(k => SuperMechKnowledge.IsUnlocked(actor, k.id)).Count;
            Text title = SuperMechUtils.CreateText(headerObj.transform,
                $"{LocalizedTextManager.getText("sm_ui_tier")} {tier + 1}",
                13, TextAnchor.MiddleLeft, TierColors[tier]);
            title.fontStyle = FontStyle.Bold;
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(80, 24);

            GameObject miniBarObj = new GameObject("MiniProgress", typeof(RectTransform));
            miniBarObj.transform.SetParent(headerObj.transform, false);
            RectTransform miniBarRt = miniBarObj.GetComponent<RectTransform>();
            miniBarRt.sizeDelta = new Vector2(120, 16);
            Image miniBg = miniBarObj.AddComponent<Image>();
            miniBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            GameObject miniFillObj = new GameObject("Fill", typeof(RectTransform));
            miniFillObj.transform.SetParent(miniBarObj.transform, false);
            Image miniFill = miniFillObj.AddComponent<Image>();
            miniFill.color = TierColors[tier];
            RectTransform miniFillRt = miniFillObj.GetComponent<RectTransform>();
            miniFillRt.anchorMin = new Vector2(0, 0);
            miniFillRt.anchorMax = new Vector2(knowledge.Count > 0 ? (float)tierUnlocked / knowledge.Count : 0, 1);
            miniFillRt.offsetMin = Vector2.zero;
            miniFillRt.offsetMax = Vector2.zero;
            Text miniText = SuperMechUtils.CreateText(miniBarObj.transform,
                $"{tierUnlocked}/{knowledge.Count}", 10, TextAnchor.MiddleCenter, Color.white);
            RectTransform miniTextRt = miniText.GetComponent<RectTransform>();
            miniTextRt.anchorMin = Vector2.zero;
            miniTextRt.anchorMax = Vector2.one;
            miniTextRt.offsetMin = Vector2.zero;
            miniTextRt.offsetMax = Vector2.zero;

            Image rightIcon = new GameObject("IconRight", typeof(RectTransform)).AddComponent<Image>();
            rightIcon.transform.SetParent(headerObj.transform, false);
            rightIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            RectTransform rightIconRt = rightIcon.GetComponent<RectTransform>();
            rightIconRt.sizeDelta = new Vector2(20, 20);

            GameObject gridObj = new GameObject("RunningIcons", typeof(RectTransform));
            gridObj.transform.SetParent(elementObj.transform, false);
            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(28, 28);
            glg.spacing = new Vector2(4, 4);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;
            glg.childAlignment = TextAnchor.UpperLeft;
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(0, 70f);

            foreach (var def in knowledge)
            {
                bool unlocked = SuperMechKnowledge.IsUnlocked(actor, def.id);
                GameObject iconObj = new GameObject(def.id, typeof(RectTransform));
                iconObj.transform.SetParent(gridObj.transform, false);
                Image img = iconObj.AddComponent<Image>();
                img.color = unlocked ? TierColors[tier] : new Color(0.25f, 0.25f, 0.25f, 0.6f);
                try
                {
                    Sprite sprite = SpriteTextureLoader.getSprite(def.icon);
                    if (sprite != null) img.sprite = sprite;
                }
                catch { }
                Button btn = iconObj.AddComponent<Button>();
                string kid = def.id;
                btn.onClick.AddListener(() =>
                {
                    if (!unlocked && actor != null)
                    {
                        SuperMechKnowledge.Unlock(actor, kid);
                    }
                });
            }
        }
    }
}
