using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMCraftView : MonoBehaviour
    {
        void Awake()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            var textGo = new GameObject("Info");
            textGo.transform.SetParent(transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(15, 15);
            rect.offsetMax = new Vector2(-15, -15);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = new Color(0.1f, 0.15f, 0.25f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = LocalizedTextManager.getText("sm_ui_craft_info");
        }
    }

    public class SMRankView : MonoBehaviour
    {
        void Awake()
        {
            BuildLayout();
            RefreshRank();
        }

        private void BuildLayout()
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(10, 10);
            sr.offsetMax = new Vector2(-10, -10);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            scrollRect.content = contentRect;
            scrollRect.vertical = true;
        }

        private void RefreshRank()
        {
            var content = transform.Find("Scroll/Content");
            if (content == null) return;

            var units = World.world?.units?.units_only_alive;
            if (units == null) return;

            var rankings = new System.Collections.Generic.List<(string name, string rankKey, float onar)>();
            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                float onar = SuperMechAdvancement.CalcOnar(a);
                if (onar <= 0) continue;
                string rankKey = SuperMechRanks.GetRankName(a);
                rankings.Add((a.name, rankKey, onar));
            }
            rankings.Sort((x, y) => y.onar.CompareTo(x.onar));
            if (rankings.Count > 20) rankings.RemoveRange(20, rankings.Count - 20);

            int rank = 1;
            foreach (var r in rankings)
            {
                var rowGo = new GameObject($"Row_{rank}");
                rowGo.transform.SetParent(content, false);
                var rowRect = rowGo.AddComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0, 28);
                var rowImg = rowGo.AddComponent<Image>();
                rowImg.color = rank <= 3 ? new Color(0.8f, 0.6f, 0.2f, 0.3f) : new Color(0, 0, 0, 0.1f);
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(rowGo.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                var text = textGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 13;
                text.color = new Color(0.1f, 0.15f, 0.25f);
                text.alignment = TextAnchor.MiddleLeft;
                text.text = $"#{rank}  {r.name}  -  {r.rankKey}  (能级:{r.onar:F0})";
                rank++;
            }
        }
    }

    public class SMSanctuaryView : MonoBehaviour
    {
        void Awake()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            var textGo = new GameObject("Info");
            textGo.transform.SetParent(transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(15, 15);
            rect.offsetMax = new Vector2(-15, -15);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = new Color(0.1f, 0.15f, 0.25f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = LocalizedTextManager.getText("sm_ui_sanctuary_info");
        }
    }
}
