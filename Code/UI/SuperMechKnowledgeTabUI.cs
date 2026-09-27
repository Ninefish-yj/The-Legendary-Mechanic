using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public static partial class SuperMechKnowledgeTab
    {
        private static void AddInfoRow(Transform parent, string left, string right)
        {
            left = LocalizedTextManager.getText(left);
            right = LocalizedTextManager.getText(right);
            GameObject go = new GameObject("InfoRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            GameObject leftGo = new GameObject("Left", typeof(RectTransform));
            leftGo.transform.SetParent(go.transform, false);
            Text lt = leftGo.AddComponent<Text>();
            lt.text = left;
            lt.fontSize = 12;
            lt.color = Color.white;
            lt.horizontalOverflow = HorizontalWrapMode.Overflow;
            lt.verticalOverflow = VerticalWrapMode.Truncate;
            if (lt.font == null) lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var leftLE = leftGo.AddComponent<LayoutElement>();
            leftLE.minWidth = 80;
            leftLE.flexibleWidth = 1;

            GameObject rightGo = new GameObject("Right", typeof(RectTransform));
            rightGo.transform.SetParent(go.transform, false);
            Text rt2 = rightGo.AddComponent<Text>();
            rt2.text = right;
            rt2.fontSize = 12;
            rt2.color = new Color(0.7f, 0.7f, 0.7f);
            rt2.alignment = TextAnchor.MiddleRight;
            rt2.horizontalOverflow = HorizontalWrapMode.Overflow;
            rt2.verticalOverflow = VerticalWrapMode.Truncate;
            if (rt2.font == null) rt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rightLE = rightGo.AddComponent<LayoutElement>();
            rightLE.minWidth = 80;
            rightLE.flexibleWidth = 2;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
        }

        private static void AddHeader(Transform parent, string text)
        {
            GameObject go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(1f, 0.85f, 0.4f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (LocalizedTextManager.current_font != null) txt.font = LocalizedTextManager.current_font;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28);
        }

        private static void AddSectionHeader(Transform parent, string text)
        {
            AddSectionHeader(parent, text, new Color(0.6f, 0.8f, 1f));
        }

        private static void AddSectionHeader(Transform parent, string text, Color color)
        {
            GameObject go = new GameObject("SectionHeader", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = LocalizedTextManager.getText(text);
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (LocalizedTextManager.current_font != null) txt.font = LocalizedTextManager.current_font;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 22);
        }

        private static void AddActionButton(Transform parent, string text, System.Action onClick, Color color)
        {
            GameObject go = new GameObject("ActionButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(color.r, color.g, color.b, 0.2f);
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (LocalizedTextManager.current_font != null) txt.font = LocalizedTextManager.current_font;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 26);
        }

        private static void AddKnowledgeIcon(Transform parent, Actor actor, SuperMechKnowledge.KnowledgeDef def, string branch, bool unlocked, bool canUnlock, int actualCost = 0)
        {
            SuperMechKnowledgeButton btn = SuperMechKnowledgeButton.Create(parent);
            btn.gameObject.SetActive(true);

            string iconPath = def.icon ?? GetTierIcon(def.tier);
            Sprite sprite = SpriteTextureLoader.getSprite(iconPath);
            btn.Setup(def.id, sprite, unlocked, canUnlock, def.tier);

            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            if (canUnlock)
            {
                btn.button.onClick.AddListener(() =>
                {
                    if (SuperMechPotential.UnlockNode(actor, def.id, def.cost))
                    {
                        RenderContent(actor);
                    }
                });
            }

            var tip = btn.gameObject.AddComponent<TipButton>();
            bool crossClass = SuperMechPotential.IsCrossClass(actor, def.id);
            bool hasSynergy = SuperMechPotential.HasPowerSynergy(actor, SuperMechPotential.GetKnowledgePrefix(def.id));
            float intel = actor.stats["intelligence"];
            string costText = $"sm_knowledgetab_868";
            if (crossClass)
            {
                string intelText = intel < 10f ? "sm_knowledgetab_869" : intel >= 20f ? "sm_knowledgetab_870" : "sm_knowledgetab_871";
                string synergyText = hasSynergy ? "sm_knowledgetab_872" : "";
                costText = $"sm_knowledgetab_873";
            }
            else if (hasSynergy)
            {
                costText = $"sm_knowledgetab_874";
            }
            tip.textOnClick = LocalizedTextManager.getText("sm_knowledgetab_875");
        }

        private static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.6f, 0.6f, 0.6f);
                case 1: return new Color(0.3f, 0.8f, 0.3f);
                case 2: return new Color(0.3f, 0.5f, 1f);
                case 3: return new Color(0.7f, 0.4f, 1f);
                case 4: return new Color(1f, 0.84f, 0f);
                default: return Color.white;
            }
        }

        private static string GetTierIcon(int tier)
        {
            switch (tier)
            {
                case 0: return "ui/Icons/skills/iconSkillBlock";
                case 1: return "ui/Icons/skills/iconSkillDash";
                case 2: return "ui/Icons/skills/iconSkillDodge";
                case 3: return "ui/Icons/skills/iconSkillBackstep";
                case 4: return "ui/Icons/skills/iconSkillDeflectProjectile";
                default: return "ui/Icons/skills/iconSkillBlock";
            }
        }

        private static GameObject AddIconRow(Transform parent)
        {
            GameObject go = new GameObject("IconRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 42);
            return go;
        }
    }
}
