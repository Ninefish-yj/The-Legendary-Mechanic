using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星际交易市场界面：查看货币/公司/交易/物价指数</summary>
    public class SuperMechMarketView : MonoBehaviour
    {
        private static SuperMechMarketView _instance;
        public static SuperMechMarketView Instance => _instance;

        private GameObject _panel;
        private Text _marketText;
        private Button _refreshBtn;

        void Awake() { _instance = this; }
        void OnDestroy() { if (_instance == this) _instance = null; }

        public static void Toggle()
        {
            if (_instance != null && _instance._panel != null && _instance._panel.activeSelf)
            {
                _instance.Close();
                return;
            }
            if (_instance == null)
            {
                var go = new GameObject(typeof(SuperMechMarketView).Name);
                go.transform.SetParent(SuperMechUguiWindow.GetCanvas().transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _instance = go.AddComponent<SuperMechMarketView>();
            }
            _instance.Open();
        }

        private void Open()
        {
            if (_panel == null) BuildPanel();
            _panel.SetActive(true);
            RefreshMarket();
        }

        private void Close() { if (_panel != null) _panel.SetActive(false); }

        private void BuildPanel()
        {
            var overlayGo = new GameObject("MarketOverlay");
            overlayGo.transform.SetParent(SuperMechUguiWindow.GetCanvas().transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.75f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(Close);
            _panel = overlayGo;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 500);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_market_title");

            // v0.193 滚动文本区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(10, 55);
            scrollRt.offsetMax = new Vector2(-10, -55);
            var sr = scrollGo.AddComponent<ScrollRect>();
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var scrollImg = scrollGo.AddComponent<Image>(); scrollImg.color = new Color(0, 0, 0, 0); scrollImg.raycastTarget = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 400);
            sr.content = contentRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            _marketText = contentGo.AddComponent<Text>();
            _marketText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _marketText.fontSize = 12;
            _marketText.color = new Color(0.8f, 0.9f, 1f);
            _marketText.alignment = TextAnchor.UpperLeft;
            _marketText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _marketText.verticalOverflow = VerticalWrapMode.Truncate;
            var marketTextRt = _marketText.GetComponent<RectTransform>();
            marketTextRt.anchorMin = Vector2.zero;
            marketTextRt.anchorMax = Vector2.one;
            marketTextRt.offsetMin = Vector2.zero;
            marketTextRt.offsetMax = Vector2.zero;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var refreshBtnGo = new GameObject("RefreshBtn");
            refreshBtnGo.transform.SetParent(panelGo.transform, false);
            var refreshBtnRect = refreshBtnGo.AddComponent<RectTransform>();
            refreshBtnRect.anchorMin = new Vector2(0.5f, 0);
            refreshBtnRect.anchorMax = new Vector2(0.5f, 0);
            refreshBtnRect.pivot = new Vector2(0.5f, 0);
            refreshBtnRect.sizeDelta = new Vector2(150, 44);
            refreshBtnRect.anchoredPosition = new Vector2(0, 10);
            var refreshBtnImg = refreshBtnGo.AddComponent<Image>();
            refreshBtnImg.color = new Color(0.3f, 0.6f, 0.4f, 0.9f);
            _refreshBtn = refreshBtnGo.AddComponent<Button>();
            _refreshBtn.onClick.AddListener(RefreshMarket);
            var refreshTextGo = new GameObject("Text");
            refreshTextGo.transform.SetParent(refreshBtnGo.transform, false);
            var refreshTextRect = refreshTextGo.AddComponent<RectTransform>();
            refreshTextRect.anchorMin = Vector2.zero;
            refreshTextRect.anchorMax = Vector2.one;
            refreshTextRect.offsetMin = Vector2.zero;
            refreshTextRect.offsetMax = Vector2.zero;
            var refreshText = refreshTextGo.AddComponent<Text>();
            refreshText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            refreshText.fontSize = 12;
            refreshText.color = Color.white;
            refreshText.alignment = TextAnchor.MiddleCenter;
            refreshText.text = LocalizedTextManager.getText("sm_ui_refresh");
            refreshText.raycastTarget = false;
        }

        private void RefreshMarket()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>{LocalizedTextManager.getText("sm_ui_economy_overview")}</b>\n\n");

            // 货币供应
            long totalM2 = 0; float avgRate = 0f;
            var allCompanies = SuperMechCompany.GetAllCompanies();
            if (allCompanies != null) { foreach (var c in allCompanies) { if (c != null) { totalM2 += c.totalIssued; avgRate += c.baseInterestRate; } } if (allCompanies.Count > 0) avgRate /= allCompanies.Count; }
            float inflation = SuperMechCompany.CalculateInflationRate();
            sb.Append($"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_money_supply")}:</color> {totalM2:F0} ₡\n");
            sb.Append($"<color=#ff9966>{LocalizedTextManager.getText("sm_ui_inflation")}:</color> {inflation * 100:F1}%\n");
            sb.Append($"<color=#66ccff>{LocalizedTextManager.getText("sm_ui_interest_rate")}:</color> {avgRate * 100:F1}%\n\n");

            // 公司列表
            sb.Append($"<b>{LocalizedTextManager.getText("sm_ui_companies")}:</b>\n");
            var companies = SuperMechCompany.GetAllCompanies();
            if (companies != null && companies.Count > 0)
            {
                foreach (var c in companies)
                {
                    if (c == null) continue;
                    string godMark = c.isGodCompany ? " [GOD]" : "";
                    sb.Append($"  • {c.GetDisplayName()}{godMark}\n");
                    sb.Append($"    金库:{c.treasury:F0}₡ | 已发行:{c.totalIssued:F0} | 利率:{c.baseInterestRate * 100:F0}%\n");
                }
            }
            else sb.Append($"  <color=#888888>{LocalizedTextManager.getText("sm_ui_no_company")}</color>\n");

            // 物价指数（简化）
            sb.Append($"\n<b>{LocalizedTextManager.getText("sm_ui_price_index")}:</b>\n");
            sb.Append($"  能源: {(100 + inflation * 50):F0}\n");
            sb.Append($"  矿产: {(100 + inflation * 30):F0}\n");
            sb.Append($"  粮食: {(100 + inflation * 20):F0}\n");
            sb.Append($"  装备: {(100 + inflation * 40):F0}\n");

            _marketText.text = sb.ToString();
        }
    }
}
