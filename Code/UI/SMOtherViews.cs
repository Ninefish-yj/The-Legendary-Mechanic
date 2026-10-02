using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMCraftView : MonoBehaviour
    {
        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            var (scroll, content) = SMUiSkin.CreateScrollArea(transform, "Scroll");
            var text = SMUiSkin.MakeText(content, LocalizedTextManager.getText("sm_ui_craft_info"), 14, TextAnchor.UpperLeft);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    public class SMRankView : MonoBehaviour
    {
        void Awake()
        {
            try { BuildLayout(); RefreshRank(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] RankView初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = Vector2.one;
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            // Mask裁剪
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 100);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;
            scrollRect.viewport = sr;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            // 滚动条样式
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
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
                string rankKey = LocalizedTextManager.getText(SuperMechRanks.GetRankName(a));
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
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.sizeDelta = new Vector2(0, 30);
                var layout = rowGo.AddComponent<LayoutElement>();
                layout.preferredHeight = 30;
                layout.minHeight = 30;
                var rowImg = rowGo.AddComponent<Image>();
                if (rank <= 3)
                    rowImg.color = new Color(SMUiSkin.AccentColor.r, SMUiSkin.AccentColor.g, SMUiSkin.AccentColor.b, 0.2f);
                else
                    rowImg.color = rank % 2 == 0 ? new Color(1, 1, 1, 0.04f) : new Color(1, 1, 1, 0.02f);

                var text = SMUiSkin.MakeText(rowGo.transform, "", 13, TextAnchor.MiddleLeft);
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                if (rank <= 3) text.color = SMUiSkin.AccentColor;
                string onarLabel = LocalizedTextManager.getText("sm_ui_onar_col");
                text.text = $"#{rank}  {r.name}  -  {r.rankKey}  ({onarLabel}:{r.onar:F0})";
                rank++;
            }

            // 没有数据时显示提示
            if (rankings.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(content, false);
                var emptyRect = emptyGo.AddComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0, 1);
                emptyRect.anchorMax = new Vector2(1, 1);
                emptyRect.pivot = new Vector2(0.5f, 1);
                emptyRect.sizeDelta = new Vector2(0, 40);
                var emptyLayout = emptyGo.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 40;
                var emptyText = SMUiSkin.MakeText(emptyGo.transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
                var emptyTextRect = emptyText.GetComponent<RectTransform>();
                emptyTextRect.anchorMin = Vector2.zero;
                emptyTextRect.anchorMax = Vector2.one;
                emptyTextRect.offsetMin = Vector2.zero;
                emptyTextRect.offsetMax = Vector2.zero;
            }
        }
    }

    public class SMSanctuaryView : MonoBehaviour
    {
        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            var (scroll, content) = SMUiSkin.CreateScrollArea(transform, "Scroll");
            var text = SMUiSkin.MakeText(content, LocalizedTextManager.getText("sm_ui_sanctuary_info"), 14, TextAnchor.UpperLeft);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
