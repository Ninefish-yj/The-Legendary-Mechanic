using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal static class SuperMechInfoCard
    {
        private const string CardName = "SuperMechInfoCard";
        private const float CardWidth = 180f;

        private static readonly Color TitleColor = new Color(1f, 0.85f, 0.4f);
        private static readonly Color LabelColor = new Color(0.78f, 0.84f, 0.9f);
        private static readonly Color ValueColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color HighlightColor = new Color(0.4f, 0.85f, 0.5f);

        private static readonly Dictionary<UnitWindow, GameObject> Cards = new Dictionary<UnitWindow, GameObject>();

        internal static void Show(UnitWindow window, Actor actor)
        {
            if (window == null || window.transform == null || actor == null) return;

            Transform bg = window.transform.Find("Background");
            if (bg == null) return;

            Font font = LocalizedTextManager.current_font;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (!Cards.TryGetValue(window, out GameObject card) || card == null)
            {
                Transform existed = bg.Find(CardName);
                if (existed != null)
                    Object.Destroy(existed.gameObject);

                card = new GameObject(CardName,
                    typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                card.transform.SetParent(bg, false);
                card.transform.localScale = Vector3.one;

                RectTransform rect = card.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(CardWidth, 0f);
                rect.anchoredPosition = new Vector2(70f, -24f);

                Image img = card.GetComponent<Image>();
                img.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);
                img.raycastTarget = false;

                Outline outline = card.GetComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.7f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = true;

                VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(4, 4, 4, 4);
                vlg.spacing = 0f;
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                ContentSizeFitter csf = card.GetComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Cards[window] = card;
            }

            for (int i = card.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(card.transform.GetChild(i).gameObject);

            bool hasTalent = SuperMechTalent.HasTalent(actor);
            if (!hasTalent)
            {
                CreateTitle(card.transform, font, LocalizedTextManager.getText("sm_ui_mortal_title"));
                CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_hint"), LocalizedTextManager.getText("sm_ui_hint_awaken"), LabelColor, ValueColor);
                card.SetActive(true);
                RefreshLayout(card);
                return;
            }

            CreateTitle(card.transform, font, LocalizedTextManager.getText("sm_ui_super_info"));

            string rank = SuperMechUnitWindow.GetRank(actor);
            CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_rank"), rank, LabelColor, HighlightColor);

            bool hasProfession = SuperMechProfession.HasProfession(actor);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(actor);
                string clsAspect = SuperMechUnitWindow.GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_class"), clsText, LabelColor, ValueColor);

                string stage = SuperMechStage.GetStageName(actor);
                if (stage != "—" && stage != "sm_knowledgetab_829")
                    CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_class_stage"), stage, LabelColor, ValueColor);
            }
            else
            {
                CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), LabelColor, ValueColor);
            }

            float onar = SuperMechAdvancement.CalcOnar(actor);
            CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_onar"), $"{onar:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}", LabelColor, ValueColor);

            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = SuperMechUnitWindow.GetQiDisplayName(actor);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            CreateLine(card.transform, font, qiName, $"{qiBar}（{qiLvText}）", LabelColor, ValueColor);

            int pot = SuperMechPotential.GetPotential(actor);
            if (pot > 0)
                CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), LabelColor, new Color(0.8f, 0.7f, 1f));

            if (SuperMechAwakened.IsAwakened(actor))
                CreateLine(card.transform, font, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), LabelColor, HighlightColor);

            card.SetActive(true);
            RefreshLayout(card);
        }

        internal static void Hide(UnitWindow window)
        {
            if (window != null && Cards.TryGetValue(window, out GameObject card) && card != null)
            {
                Object.Destroy(card);
                Cards.Remove(window);
                return;
            }

            try
            {
                Transform bg = window?.transform?.Find("Background");
                Transform existed = bg?.Find(CardName);
                if (existed != null)
                    Object.Destroy(existed.gameObject);
            }
            catch { }

            if (window != null)
                Cards.Remove(window);
        }

        internal static void HideAll()
        {
            foreach (var kv in Cards)
            {
                if (kv.Value != null)
                    Object.Destroy(kv.Value);
            }
            Cards.Clear();
        }

        private static void CreateTitle(Transform parent, Font font, string text)
        {
            GameObject obj = new GameObject("CardTitle", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Text), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);

            Image titleBg = obj.GetComponent<Image>();
            titleBg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
            titleBg.raycastTarget = false;

            Outline titleOutline = obj.GetComponent<Outline>();
            titleOutline.effectColor = new Color(0.4f, 0.5f, 0.7f, 0.5f);
            titleOutline.effectDistance = new Vector2(1f, -1f);
            titleOutline.useGraphicAlpha = true;

            Text t = obj.GetComponent<Text>();
            t.font = font;
            t.fontSize = 10;
            t.fontStyle = FontStyle.Bold;
            t.color = TitleColor;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;

            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.minHeight = 18f;
            le.preferredHeight = 18f;
        }

        private static void CreateLine(Transform parent, Font font, string label, string value, Color labelColor, Color valueColor)
        {
            GameObject obj = new GameObject("CardLine", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = obj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(6, 6, 2, 2);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(obj.transform, false);
            Text labelTxt = labelObj.GetComponent<Text>();
            labelTxt.font = font;
            labelTxt.fontSize = 9;
            labelTxt.color = labelColor;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            labelTxt.raycastTarget = false;
            labelTxt.text = label;
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(50f, 12f);

            GameObject valueObj = new GameObject("Value", typeof(RectTransform), typeof(Text));
            valueObj.transform.SetParent(obj.transform, false);
            Text valueTxt = valueObj.GetComponent<Text>();
            valueTxt.font = font;
            valueTxt.fontSize = 9;
            valueTxt.color = valueColor;
            valueTxt.alignment = TextAnchor.MiddleLeft;
            valueTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueTxt.verticalOverflow = VerticalWrapMode.Overflow;
            valueTxt.raycastTarget = false;
            valueTxt.text = value;
            RectTransform valueRt = valueObj.GetComponent<RectTransform>();
            valueRt.sizeDelta = new Vector2(120f, 12f);

            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.minHeight = 12f;
            le.preferredHeight = 12f;
        }

        private static void RefreshLayout(GameObject card)
        {
            RectTransform cardRect = card.GetComponent<RectTransform>();
            VerticalLayoutGroup vlgCard = card.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter csfCard = card.GetComponent<ContentSizeFitter>();
            if (vlgCard != null) { vlgCard.enabled = false; vlgCard.enabled = true; }
            if (csfCard != null) { csfCard.enabled = false; csfCard.enabled = true; }
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            Canvas.ForceUpdateCanvases();
        }
    }
}
