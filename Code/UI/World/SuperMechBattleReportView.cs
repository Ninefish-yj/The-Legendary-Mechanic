using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>v0.196 势力战争战报面板
    /// 显示历史战报和当前进行中的战争</summary>
    public static class SuperMechBattleReportView
    {
        private static GameObject _panel;

        public static void Toggle()
        {
            if (_panel != null) { Close(); return; }
            Open();
        }

        public static void Open()
        {
            if (_panel != null) return;
            try { Build(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] 战报面板初始化失败: " + e); }
        }

        public static void Close()
        {
            if (_panel != null) { Object.Destroy(_panel); _panel = null; }
        }

        private static void Build()
        {
            var canvas = SuperMechUguiWindow.GetCanvas();
            if (canvas == null) return;

            // 遮罩
            var overlayGo = new GameObject("ReportOverlay");
            overlayGo.transform.SetParent(canvas.transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero; overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.75f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(Close);
            _panel = overlayGo;

            // 主面板
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560, 480);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = SuperMechUiSkin.Panel;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 18;
            titleTxt.color = SuperMechUiSkin.CyanColor;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = LocalizedTextManager.getText("sm_ui_battle_report_title");

            // 关闭按钮
            var closeBtn = SuperMechUiSkin.MakeButton(panelGo.transform, "X", 16, Close);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1); closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(32, 32);
            closeRect.anchoredPosition = new Vector2(-8, -8);

            // 滚动区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(12, 12); scrollRt.offsetMax = new Vector2(-12, -52);
            var sr = scrollGo.AddComponent<ScrollRect>();
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.2f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 800);
            sr.content = contentRt;
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float y = -10f;

            // 当前战争
            var currentWar = SuperMechBattleReport.GetCurrentWar();
            if (currentWar.HasValue)
            {
                var war = currentWar.Value;
                y = DrawReportCard(contentRt, y, war, true);
            }

            // 历史战报
            var reports = SuperMechBattleReport.GetAllReports();
            if (reports.Count == 0 && !currentWar.HasValue)
            {
                var emptyGo = new GameObject("Empty", typeof(RectTransform));
                emptyGo.transform.SetParent(contentRt, false);
                var er = emptyGo.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 1); er.anchorMax = new Vector2(1, 1);
                er.pivot = new Vector2(0.5f, 1);
                er.anchoredPosition = new Vector2(0, y);
                er.sizeDelta = new Vector2(0, 60);
                var emptyTxt = SuperMechUiSkin.MakeText(emptyGo.transform,
                    LocalizedTextManager.getText("sm_ui_battle_report_empty"), 14, TextAnchor.MiddleCenter);
                emptyTxt.color = SuperMechUiSkin.TextDim;
                y -= 70f;
            }
            else
            {
                // 历史标题
                if (reports.Count > 0)
                {
                    var histGo = new GameObject("HistTitle", typeof(RectTransform));
                    histGo.transform.SetParent(contentRt, false);
                    var hr = histGo.GetComponent<RectTransform>();
                    hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
                    hr.pivot = new Vector2(0.5f, 1);
                    hr.anchoredPosition = new Vector2(0, y);
                    hr.sizeDelta = new Vector2(0, 24);
                    var histTxt = SuperMechUiSkin.MakeText(histGo.transform,
                        LocalizedTextManager.getText("sm_ui_battle_report_history"), 12, TextAnchor.MiddleLeft);
                    histTxt.color = SuperMechUiSkin.TextColor;
                    y -= 30f;
                }

                for (int i = reports.Count - 1; i >= 0; i--)
                {
                    y = DrawReportCard(contentRt, y, reports[i], false);
                }
            }

            contentRt.sizeDelta = new Vector2(0, -y + 20);
        }

        private static float DrawReportCard(RectTransform parent, float y, SuperMechBattleReport.BattleReportData r, bool isCurrent)
        {
            var cardGo = new GameObject("ReportCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            var cr = cardGo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.anchoredPosition = new Vector2(0, y);
            cr.sizeDelta = new Vector2(0, 90);
            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = isCurrent
                ? new Color(0.2f, 0.15f, 0.05f, 0.9f)
                : new Color(0.08f, 0.1f, 0.16f, 0.9f);

            // 标题行：双方 vs 结果
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(cardGo.transform, false);
            var tr = titleGo.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0.5f, 1);
            tr.anchoredPosition = new Vector2(8, -6);
            tr.sizeDelta = new Vector2(-16, 22);
            string resultColor = r.result == LocalizedTextManager.getText("sm_ui_battle_a_win") ? "#81c784" : r.result == LocalizedTextManager.getText("sm_ui_battle_b_win") ? "#e57373" : "#ffd54f";
            string title = $"<b>{r.factionA}</b>  <color=#93A4BC>VS</color>  <b>{r.factionB}</b>  <color={resultColor}>[{r.result}]</color>";
            if (isCurrent) title += "  <color=#ffd700>(" + LocalizedTextManager.getText("sm_ui_battle_report_ongoing") + ")</color>";
            var titleTxt = SuperMechUiSkin.MakeText(titleGo.transform, title, 13, TextAnchor.MiddleLeft);
            titleTxt.color = SuperMechUiSkin.TextColor;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 伤亡行
            var casualtyGo = new GameObject("Casualty", typeof(RectTransform));
            casualtyGo.transform.SetParent(cardGo.transform, false);
            var car = casualtyGo.GetComponent<RectTransform>();
            car.anchorMin = new Vector2(0, 0); car.anchorMax = new Vector2(1, 0);
            car.pivot = new Vector2(0.5f, 0);
            car.anchoredPosition = new Vector2(8, 8);
            car.sizeDelta = new Vector2(-16, 50);
            string casualty = $"{r.factionA}: <color=#e57373>{r.casualtiesA}</color>";
            if (r.superACasualtiesA > 0) casualty += $" (<color=#ffd700>超A{r.superACasualtiesA}</color>)";
            casualty += $"    {r.factionB}: <color=#e57373>{r.casualtiesB}</color>";
            if (r.superACasualtiesB > 0) casualty += $" (<color=#ffd700>超A{r.superACasualtiesB}</color>)";
            casualty += $"\n<color=#93A4BC>第{r.year}年</color>";
            var casualtyTxt = SuperMechUiSkin.MakeText(casualtyGo.transform, casualty, 11, TextAnchor.UpperLeft);
            casualtyTxt.color = SuperMechUiSkin.TextDim;
            casualtyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var cRect = casualtyTxt.GetComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero; cRect.anchorMax = Vector2.one;
            cRect.offsetMin = Vector2.zero; cRect.offsetMax = Vector2.zero;

            return y - 100f;
        }
    }
}
