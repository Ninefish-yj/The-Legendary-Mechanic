using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

namespace SuperMech.Code
{
    /// <summary>伊纳尔经济系统窗口（原著：主宇宙通用货币，超A俸禄/佣金/悬赏）
    /// v0.242.20: 移除付费服务标签（功能未完成，普通单位无复活需求，降临者自动复活待实现）</summary>
    public class SuperMechEconomyView : MonoBehaviour
    {
        private Text _statsText;
        private Text _rankText;
        private Text _companyText;
        private Text _macroText;
        private Text _marketText;
        private GameObject _overviewPanel;
        private GameObject _marketPanel;
        private int _currentTab = 0; // 0=总览, 1=市场
        private float _nextRefreshTime;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView刷新失败: " + e); } }
        void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime) return;
            _nextRefreshTime = Time.unscaledTime + 0.5f;
            try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView更新失败: " + e); }
        }

        private void BuildLayout()
        {
            // 页签栏
            var tabBar = SuperMechUiBuilder.CreatePanel(transform, "TabBar", new Color(0.04f, 0.06f, 0.10f, 0.9f), 4, 4, 0, 0);
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0, 1);
            tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.sizeDelta = new Vector2(0, 32);

            string[] tabLabels = {
                LocalizedTextManager.getText("sm_ui_economy_overview"),
                LocalizedTextManager.getText("sm_ui_market_title")
            };
            float tabW = 1f / 2f;
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                var tabGo = SuperMechUiBuilder.CreateObject("Tab" + i, tabBar.transform, typeof(RectTransform), typeof(Image), typeof(Button));
                var tabRt = SuperMechUiBuilder.AddRectTransform(tabGo);
                tabRt.anchorMin = new Vector2(idx * tabW, 0);
                tabRt.anchorMax = new Vector2((idx + 1) * tabW, 1);
                tabRt.offsetMin = new Vector2(2, 2);
                tabRt.offsetMax = new Vector2(-2, -2);
                var tabImg = tabGo.GetComponent<Image>();
                tabImg.sprite = idx == 0 ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                tabImg.type = Image.Type.Sliced;
                tabImg.color = Color.white;
                var tabBtn = tabGo.GetComponent<Button>();
                SuperMechUiBuilder.AddText(tabGo, tabLabels[i], 12, TextAnchor.MiddleCenter,
                    idx == 0 ? new Color(0.95f, 0.85f, 0.5f, 1f) : new Color(0.7f, 0.75f, 0.85f, 1f));
                tabBtn.onClick.AddListener(() => SwitchTab(idx));
            }

            // === 总览页签：三栏布局 ===
            _overviewPanel = SuperMechUiBuilder.CreateObject("Overview", transform, typeof(RectTransform));
            var ovRect = SuperMechUiBuilder.AddRectTransform(_overviewPanel);
            ovRect.anchorMin = Vector2.zero;
            ovRect.anchorMax = Vector2.one;
            ovRect.offsetMin = new Vector2(4, 4);
            ovRect.offsetMax = new Vector2(-4, -36);

            // 顶部统计栏
            var statsBar = SuperMechUiBuilder.CreatePanel(_overviewPanel.transform, "StatsBar", new Color(0, 0, 0, 0.25f), 0, 0, 0, 0);
            var sbRect = statsBar.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0, 1);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(0.5f, 1);
            sbRect.sizeDelta = new Vector2(0, 30);
            _statsText = SuperMechUiBuilder.AddText(statsBar, "", 11, TextAnchor.MiddleLeft);
            var stRect = _statsText.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero; stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(8, 0); stRect.offsetMax = new Vector2(-8, 0);

            // 左侧：超A俸禄排行榜
            var rankPanel = SuperMechUiBuilder.CreatePanel(_overviewPanel.transform, "RankPanel", new Color(0, 0, 0, 0.12f), 0, 0, 0, 0);
            var rpRect = rankPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0, 0);
            rpRect.anchorMax = new Vector2(0.4f, 1);
            rpRect.pivot = new Vector2(0, 1);
            rpRect.offsetMin = new Vector2(0, -34);
            rpRect.offsetMax = new Vector2(0, -4);
            _rankText = SuperMechUiBuilder.AddText(rankPanel, "", 10, TextAnchor.UpperLeft);
            var rtRect = _rankText.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero; rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = new Vector2(6, 4); rtRect.offsetMax = new Vector2(-6, -4);

            // 中间：宏观经济指标
            var macroPanel = SuperMechUiBuilder.CreatePanel(_overviewPanel.transform, "MacroPanel", new Color(0, 0, 0, 0.12f), 0, 0, 0, 0);
            var mpRect = macroPanel.GetComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.4f, 0);
            mpRect.anchorMax = new Vector2(0.7f, 1);
            mpRect.pivot = new Vector2(0, 1);
            mpRect.offsetMin = new Vector2(0, -34);
            mpRect.offsetMax = new Vector2(0, -4);
            _macroText = SuperMechUiBuilder.AddText(macroPanel, "", 10, TextAnchor.UpperLeft);
            var mtRect = _macroText.GetComponent<RectTransform>();
            mtRect.anchorMin = Vector2.zero; mtRect.anchorMax = Vector2.one;
            mtRect.offsetMin = new Vector2(6, 4); mtRect.offsetMax = new Vector2(-6, -4);

            // 右侧：公司金库
            var companyPanel = SuperMechUiBuilder.CreatePanel(_overviewPanel.transform, "CompanyPanel", new Color(0, 0, 0, 0.12f), 0, 0, 0, 0);
            var cpRect = companyPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.7f, 0);
            cpRect.anchorMax = new Vector2(1, 1);
            cpRect.pivot = new Vector2(1, 1);
            cpRect.offsetMin = new Vector2(0, -34);
            cpRect.offsetMax = new Vector2(0, -4);
            _companyText = SuperMechUiBuilder.AddText(companyPanel, "", 10, TextAnchor.UpperLeft);
            var ctRect = _companyText.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero; ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = new Vector2(6, 4); ctRect.offsetMax = new Vector2(-6, -4);

            // === 市场页签 ===
            _marketPanel = SuperMechUiBuilder.CreateObject("Market", transform, typeof(RectTransform));
            var mkRect = SuperMechUiBuilder.AddRectTransform(_marketPanel);
            mkRect.anchorMin = Vector2.zero; mkRect.anchorMax = Vector2.one;
            mkRect.offsetMin = new Vector2(4, 4); mkRect.offsetMax = new Vector2(-4, -36);
            _marketPanel.SetActive(false);

            var mkScroll = SuperMechUiBuilder.CreateObject("Scroll", _marketPanel.transform, typeof(RectTransform), typeof(ScrollRect), typeof(Mask), typeof(Image));
            var mkScrollRt = SuperMechUiBuilder.AddRectTransform(mkScroll);
            mkScrollRt.anchorMin = Vector2.zero; mkScrollRt.anchorMax = Vector2.one;
            mkScrollRt.offsetMin = Vector2.zero; mkScrollRt.offsetMax = Vector2.zero;
            mkScroll.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            mkScroll.GetComponent<Mask>().showMaskGraphic = false;
            var mkSr = mkScroll.GetComponent<ScrollRect>();
            mkSr.horizontal = false; mkSr.vertical = true;

            var mkContent = SuperMechUiBuilder.CreateObject("Content", mkScroll.transform, typeof(RectTransform));
            var mkContentRt = SuperMechUiBuilder.AddRectTransform(mkContent);
            mkContentRt.anchorMin = new Vector2(0, 1); mkContentRt.anchorMax = new Vector2(1, 1);
            mkContentRt.pivot = new Vector2(0.5f, 1);
            mkContentRt.sizeDelta = new Vector2(0, 600);
            mkSr.content = mkContentRt;
            mkContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _marketText = mkContent.AddComponent<Text>();
            _marketText.font = SuperMechUiSkin.DefaultFont;
            _marketText.fontSize = 11;
            _marketText.color = new Color(0.8f, 0.9f, 1f);
            _marketText.alignment = TextAnchor.UpperLeft;
            _marketText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _marketText.verticalOverflow = VerticalWrapMode.Truncate;
            var mkTextRt = _marketText.GetComponent<RectTransform>();
            mkTextRt.anchorMin = Vector2.zero; mkTextRt.anchorMax = Vector2.one;
            mkTextRt.offsetMin = new Vector2(8, 8); mkTextRt.offsetMax = new Vector2(-8, -8);

        private void SwitchTab(int idx)
        {
            _currentTab = idx;
            _overviewPanel.SetActive(idx == 0);
            _marketPanel.SetActive(idx == 1);
            // 更新页签按钮样式
            foreach (Transform t in transform.Find("TabBar"))
            {
                var img = t.GetComponent<Image>();
                var txt = t.GetComponentInChildren<Text>();
                bool active = t.GetSiblingIndex() == idx;
                if (img != null)
                {
                    img.sprite = active ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                if (txt != null) txt.color = active ? SuperMechUiSkin.CyanColor : SuperMechUiSkin.TextDim;
            }
            if (idx == 1) RefreshMarket();
        }

        private void RefreshAll()
        {
            if (_statsText == null || _rankText == null || _companyText == null || _macroText == null) return;
            if (World.world == null || World.world.units == null) return;

            // v0.206 获取当前选中的王国（跨标签页联动）
            Kingdom selectedKingdom = null;
            long selId = SuperMechWindowManager.SelectedKingdomId;
            if (selId >= 0 && World.world.kingdoms != null)
            {
                foreach (var k in World.world.kingdoms.list)
                    if (k != null && k.getID() == selId) { selectedKingdom = k; break; }
            }
            string kingdomLabel = selectedKingdom != null
                ? selectedKingdom.data.name
                : LocalizedTextManager.getText("sm_ui_economy_all_kingdoms");

            // 收集超A经济数据（按选中王国过滤）
            var superAList = new List<(Actor actor, int balance, int salary)>();
            long totalInal = 0;
            int superACount = 0;

            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                // 王国过滤：选中王国时只显示该国超A
                if (selectedKingdom != null && a.kingdom != selectedKingdom) continue;
                int balance = SuperMechInal.GetInal(a);
                int salary = a.kingdom != null ? SuperMechInal.CalcSalary(a, a.kingdom) : 0;
                superAList.Add((a, balance, salary));
                totalInal += balance;
                superACount++;
            }
            superAList.Sort((x, y) => y.balance.CompareTo(x.balance));

            // 宏观经济指标（全局，不过滤）
            long m2 = SuperMechCompany.GetTotalMoneySupply();
            long reserves = SuperMechCompany.GetTotalReserves();
            float inflation = SuperMechCompany.CalculateInflationRate();
            var godCompanies = SuperMechCompany.GetGodCompanies();
            float avgRate = 0f;
            if (godCompanies.Count > 0)
            {
                foreach (var gc in godCompanies) avgRate += gc.baseInterestRate;
                avgRate /= godCompanies.Count;
            }

            // 顶部统计（显示当前王国）
            var sb = new StringBuilder();
            sb.Append("<color=#ffd700>[").Append(kingdomLabel).Append("]</color>  ")
              .Append(LocalizedTextManager.getText("sm_ui_economy_m2")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(m2.ToString("N0"))
              .Append("  |  ").Append(LocalizedTextManager.getText("sm_ui_economy_inflation")).Append(" ").Append(inflation.ToString("P1"))
              .Append("  |  ").Append(LocalizedTextManager.getText("sm_ui_economy_rate")).Append(" ").Append(avgRate.ToString("P1"));
            _statsText.text = sb.ToString();

            // 俸禄排行榜
            sb.Clear();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_economy_rank")).Append("</b>\n");
            if (superAList.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_economy_no_supera")).Append("\n");
            }
            else
            {
                int show = Mathf.Min(superAList.Count, 15);
                for (int i = 0; i < show; i++)
                {
                    var item = superAList[i];
                    string rankColor = i < 3 ? "#ffd700" : "#ffffff";
                    string kingdomName = item.actor.kingdom != null && item.actor.kingdom.name != null
                        ? item.actor.kingdom.name
                        : LocalizedTextManager.getText("sm_ui_economy_no_kingdom");
                    sb.Append("<color=").Append(rankColor).Append(">").Append(i + 1).Append(".</color> ")
                      .Append(item.actor.name != null ? item.actor.name : "?")
                      .Append("  <color=#93A4BC>").Append(kingdomName).Append("</color>")
                      .Append("\n  ").Append(LocalizedTextManager.getText("sm_ui_economy_balance")).Append(" ").Append(item.balance)
                      .Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_salary")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(item.salary)
                      .Append("/32tick\n");
                }
                if (superAList.Count > 15)
                {
                    sb.Append("... ").Append(superAList.Count - 15).Append(" ").Append(LocalizedTextManager.getText("sm_ui_economy_more")).Append("\n");
                }
            }
            _rankText.text = sb.ToString();

            // 宏观经济指标面板
            sb.Clear();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_economy_macro")).Append("</b>\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_m2")).Append(": <color=#ffd700>").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(m2.ToString("N0")).Append("</color>\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_reserves")).Append(": <color=#4fc3f7>").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(reserves.ToString("N0")).Append("</color>\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_inflation")).Append(": <color=").Append(inflation > 0.05f ? "#ff6b6b" : "#81c784").Append(">")
              .Append(inflation.ToString("P1")).Append("</color>\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_rate")).Append(": <color=#ba68c8>").Append(avgRate.ToString("P1")).Append("</color>\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_supera_total")).Append(": ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(totalInal.ToString("N0")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_god_companies")).Append(": ").Append(godCompanies.Count).Append("\n");

            // 通胀说明
            if (inflation > 0.1f)
                sb.Append("\n<color=#ff6b6b>").Append(LocalizedTextManager.getText("sm_ui_economy_inflation_high")).Append("</color>\n");
            else if (inflation < 0)
                sb.Append("\n<color=#4fc3f7>").Append(LocalizedTextManager.getText("sm_ui_economy_deflation")).Append("</color>\n");
            _macroText.text = sb.ToString();

            // 公司金库+货币发行
            sb.Clear();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_economy_company")).Append("</b>\n");
            var allCompanies = SuperMechCompany.GetAllCompanies();
            if (allCompanies.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_economy_no_company")).Append("\n");
            }
            else
            {
                foreach (var c in allCompanies)
                {
                    if (c == null) continue;
                    string color = c.isGodCompany ? "#ffd700" : (c.id == SuperMechCompany.BlackStarCorp ? "#4fc3f7" : "#81c784");
                    string tag = c.isGodCompany ? "[" + LocalizedTextManager.getText("sm_ui_economy_god_tag") + "]" : "";
                    string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₡" : c.currencySymbol;
                    string cname = string.IsNullOrEmpty(c.currencyName) ? LocalizedTextManager.getText("sm_ui_economy_inar") : c.currencyName;
                    sb.Append("<color=").Append(color).Append(">").Append(c.GetDisplayName()).Append(" ").Append(tag).Append("</color>\n");
                    sb.Append("  <color=#93A4BC>").Append(cname).Append("(").Append(sym).Append(")</color>\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_treasury")).Append(" ").Append(sym).Append(c.treasury.ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_issued")).Append(" ").Append(sym).Append(c.totalIssued.ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reserve_ratio")).Append(" ")
                      .Append(c.isGodCompany ? LocalizedTextManager.getText("sm_ui_economy_unlimited") : c.reserveRatio.ToString("P0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_max_issue")).Append(" ")
                      .Append(c.isGodCompany ? LocalizedTextManager.getText("sm_ui_economy_unlimited") : sym + SuperMechCompany.GetMaxIssueable(c).ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_profit")).Append(" ").Append(sym).Append(SuperMechCompany.GetNetProfit(c).ToString("N0")).Append("\n");
                    sb.Append("  Lv.").Append(c.level).Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reputation")).Append(" ").Append(c.reputation).Append("\n\n");
                }
            }
            _companyText.text = sb.ToString();
        }

        /// <summary>v0.195 市场页签：显示货币供应/通胀/公司列表</summary>
        private void RefreshMarket()
        {
            if (_marketText == null) return;
            var sb = new StringBuilder();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_market_title")).Append("</b>\n\n");

            long totalM2 = SuperMechCompany.GetTotalMoneySupply();
            float inflation = SuperMechCompany.CalculateInflationRate();
            var allCompanies = SuperMechCompany.GetAllCompanies();

            sb.Append("<color=#ffd700>").Append(LocalizedTextManager.getText("sm_ui_money_supply")).Append(":</color> ").Append(totalM2.ToString("N0")).Append(" ₡\n");
            sb.Append("<color=#4fc3f7>").Append(LocalizedTextManager.getText("sm_ui_economy_inflation")).Append(":</color> ").Append(inflation.ToString("P1")).Append("\n");
            sb.Append("<color=#81c784>").Append(LocalizedTextManager.getText("sm_ui_economy_god_companies")).Append(":</color> ").Append(allCompanies.Count).Append("\n\n");

            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_economy_company")).Append("</b>\n");
            if (allCompanies.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_economy_no_company")).Append("\n");
            }
            else
            {
                foreach (var c in allCompanies)
                {
                    if (c == null) continue;
                    string color = c.isGodCompany ? "#ffd700" : "#81c784";
                    sb.Append("<color=").Append(color).Append(">").Append(c.GetDisplayName()).Append("</color>\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_treasury")).Append(": ").Append(c.treasury.ToString("N0")).Append(" ₡\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_issued")).Append(": ").Append(c.totalIssued.ToString("N0")).Append(" ₡\n\n");
                }
            }
            _marketText.text = sb.ToString();
        }
    }
}
