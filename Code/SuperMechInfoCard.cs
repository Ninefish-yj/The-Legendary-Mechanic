using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal static class SuperMechInfoCard
    {
        private const string CardName = "SuperMechInfoCard";
        private const float CardWidth = 200f;

        private static KeyValueField _rowPrefab;

        private static readonly Dictionary<UnitWindow, GameObject> Cards = new Dictionary<UnitWindow, GameObject>();

        internal static void Show(UnitWindow window, Actor actor)
        {
            if (window == null || window.transform == null || actor == null) return;

            Transform bg = window.transform.Find("Background");
            if (bg == null) return;

            if (_rowPrefab == null)
                _rowPrefab = Resources.Load<KeyValueField>("ui/KeyValueFieldStats");

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
                rect.anchoredPosition = new Vector2(70f, -100f);

                Image img = card.GetComponent<Image>();
                img.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);
                img.raycastTarget = false;

                Outline outline = card.GetComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.7f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = true;

                VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(2, 2, 2, 2);
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

            CreateTitle(card.transform, LocalizedTextManager.getText("sm_ui_super_info"));

            bool hasTalent = SuperMechTalent.HasTalent(actor);
            if (!hasTalent)
            {
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_rank"), LocalizedTextManager.getText("sm_ui_mortal"), 0);
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_hint"), LocalizedTextManager.getText("sm_ui_hint_awaken"), 1);
                card.SetActive(true);
                RefreshLayout(card);
                return;
            }

            int rowIdx = 0;
            AddRow(card.transform, LocalizedTextManager.getText("sm_ui_rank"), SuperMechUnitWindow.GetRank(actor), rowIdx++);

            bool hasProfession = SuperMechProfession.HasProfession(actor);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(actor);
                string clsAspect = SuperMechUnitWindow.GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_class"), clsText, rowIdx++);

                string stage = SuperMechStage.GetStageName(actor);
                if (stage != "—" && stage != "sm_knowledgetab_829")
                    AddRow(card.transform, LocalizedTextManager.getText("sm_ui_class_stage"), stage, rowIdx++);
            }
            else
            {
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), rowIdx++);
            }

            float onar = SuperMechAdvancement.CalcOnar(actor);
            AddRow(card.transform, LocalizedTextManager.getText("sm_ui_onar"), $"{onar:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}", rowIdx++);

            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = SuperMechUnitWindow.GetQiDisplayName(actor);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            AddRow(card.transform, qiName, $"{qiBar}（{qiLvText}）", rowIdx++);

            int pot = SuperMechPotential.GetPotential(actor);
            if (pot > 0)
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), rowIdx++);

            if (SuperMechAwakened.IsAwakened(actor))
                AddRow(card.transform, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), rowIdx++);

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

        private static void AddRow(Transform parent, string name, string value, int index)
        {
            if (_rowPrefab == null) return;

            KeyValueField row = Object.Instantiate(_rowPrefab, parent);
            row.gameObject.name = "InfoRow_" + index;
            row.auto_odd_even_coloring = true;

            if (row.name_text != null)
            {
                row.name_text.text = name;
                row.name_text.fontSize = 9;
            }
            if (row.value != null)
            {
                row.value.text = value;
                row.value.fontSize = 9;
            }

            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 16f;
            le.preferredHeight = 16f;
            le.flexibleHeight = 0f;
        }

        private static void CreateTitle(Transform parent, string text)
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
            t.font = LocalizedTextManager.current_font;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 10;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.85f, 0.4f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = text;

            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.minHeight = 18f;
            le.preferredHeight = 18f;
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
