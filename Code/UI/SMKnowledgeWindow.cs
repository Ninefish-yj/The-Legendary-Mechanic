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
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
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
            graphRt.sizeDelta = new Vector2(0, 300f);
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
            barRt.sizeDelta = new Vector2(0, 24f);

            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(barObj.transform, false);
            Image fill = fillObj.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 1f, 0.8f);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(total > 0 ? (float)unlocked / total : 0, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Text text = SuperMechUtils.CreateText(barObj.transform,
                $"{unlocked}/{total}", 11, TextAnchor.MiddleCenter, Color.white);
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
            elementRt.sizeDelta = new Vector2(0, 100f);

            Image bg = elementObj.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.1f, 0.6f);

            VerticalLayoutGroup vlg = elementObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            vlg.childAlignment = TextAnchor.UpperLeft;

            GameObject headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(elementObj.transform, false);
            HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = false;
            hlg.childControlWidth = false;
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(0, 20f);

            Image tierIcon = new GameObject("TierIcon", typeof(RectTransform)).AddComponent<Image>();
            tierIcon.transform.SetParent(headerObj.transform, false);
            tierIcon.color = TierColors[tier];
            RectTransform iconRt = tierIcon.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(16, 16);

            int tierUnlocked = knowledge.FindAll(k => SuperMechKnowledge.IsUnlocked(actor, k.id)).Count;
            Text title = SuperMechUtils.CreateText(headerObj.transform,
                $"{LocalizedTextManager.getText("sm_ui_tier")} {tier + 1}  {tierUnlocked}/{knowledge.Count}",
                11, TextAnchor.MiddleLeft, TierColors[tier]);
            title.fontStyle = FontStyle.Bold;

            GameObject gridObj = new GameObject("Icons", typeof(RectTransform));
            gridObj.transform.SetParent(elementObj.transform, false);
            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(24, 24);
            glg.spacing = new Vector2(3, 3);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;
            glg.childAlignment = TextAnchor.UpperLeft;
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(0, 60f);

            foreach (var def in knowledge)
            {
                bool unlocked = SuperMechKnowledge.IsUnlocked(actor, def.id);
                GameObject iconObj = new GameObject(def.id, typeof(RectTransform));
                iconObj.transform.SetParent(gridObj.transform, false);
                Image img = iconObj.AddComponent<Image>();
                img.color = unlocked ? TierColors[tier] : new Color(0.3f, 0.3f, 0.3f, 0.5f);
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
