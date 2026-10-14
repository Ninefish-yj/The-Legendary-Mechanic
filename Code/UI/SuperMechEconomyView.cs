using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

namespace SuperMech.Code
{
    /// <summary>伊纳尔经济系统窗口（原著：主宇宙通用货币，超A俸禄/佣金/悬赏）
    /// v0.173.0: 上帝创建公司发行货币，部分准备金制度，利率与通胀</summary>
    public class SuperMechEconomyView : MonoBehaviour
    {
        private Text _statsText;
        private Text _rankText;
        private Text _companyText;
        private Text _macroText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EconomyView更新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部统计栏
            var statsBar = SuperMechUiBuilder.CreatePanel(transform, "StatsBar", new Color(0, 0, 0, 0.25f), 4, 4, 0, 0);
            var sbRect = statsBar.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0, 1);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(0.5f, 1);
            sbRect.sizeDelta = new Vector2(0, 36);
            _statsText = SuperMechUiBuilder.AddText(statsBar, "", 12, TextAnchor.MiddleLeft);
            var stRect = _statsText.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(8, 0);
            stRect.offsetMax = new Vector2(-8, 0);

            // 左侧：超A俸禄排行榜
            var rankPanel = SuperMechUiBuilder.CreatePanel(transform, "RankPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var rpRect = rankPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0, 0);
            rpRect.anchorMax = new Vector2(0.4f, 1);
            rpRect.pivot = new Vector2(0, 1);
            rpRect.offsetMin = new Vector2(0, -40);
            rpRect.offsetMax = new Vector2(0, -4);
            _rankText = SuperMechUiBuilder.AddText(rankPanel, "", 11, TextAnchor.UpperLeft);
            var rtRect = _rankText.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = new Vector2(8, 4);
            rtRect.offsetMax = new Vector2(-8, -4);

            // 中间：宏观经济指标
            var macroPanel = SuperMechUiBuilder.CreatePanel(transform, "MacroPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var mpRect = macroPanel.GetComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.4f, 0);
            mpRect.anchorMax = new Vector2(0.7f, 1);
            mpRect.pivot = new Vector2(0, 1);
            mpRect.offsetMin = new Vector2(0, -40);
            mpRect.offsetMax = new Vector2(0, -4);
            _macroText = SuperMechUiBuilder.AddText(macroPanel, "", 11, TextAnchor.UpperLeft);
            var mtRect = _macroText.GetComponent<RectTransform>();
            mtRect.anchorMin = Vector2.zero;
            mtRect.anchorMax = Vector2.one;
            mtRect.offsetMin = new Vector2(8, 4);
            mtRect.offsetMax = new Vector2(-8, -4);

            // 右侧：公司金库+货币发行
            var companyPanel = SuperMechUiBuilder.CreatePanel(transform, "CompanyPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var cpRect = companyPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.7f, 0);
            cpRect.anchorMax = new Vector2(1, 1);
            cpRect.pivot = new Vector2(1, 1);
            cpRect.offsetMin = new Vector2(0, -40);
            cpRect.offsetMax = new Vector2(0, -4);
            _companyText = SuperMechUiBuilder.AddText(companyPanel, "", 11, TextAnchor.UpperLeft);
            var ctRect = _companyText.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = new Vector2(8, 4);
            ctRect.offsetMax = new Vector2(-8, -4);
        }

        private void RefreshAll()
        {
            if (_statsText == null || _rankText == null || _companyText == null || _macroText == null) return;
            if (World.world == null || World.world.units == null) return;

            // 收集超A经济数据
            var superAList = new List<(Actor actor, int balance, int salary)>();
            long totalInal = 0;
            int superACount = 0;

            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                int balance = SuperMechInal.GetInal(a);
                int salary = a.kingdom != null ? SuperMechInal.CalcSalary(a, a.kingdom) : 0;
                superAList.Add((a, balance, salary));
                totalInal += balance;
                superACount++;
            }
            superAList.Sort((x, y) => y.balance.CompareTo(x.balance));

            // 宏观经济指标
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

            // 顶部统计
            var sb = new StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_m2")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(m2.ToString("N0"))
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
                    sb.Append("<color=").Append(color).Append(">").Append(LocalizedTextManager.getText(c.nameKey)).Append(" ").Append(tag).Append("</color>\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_treasury")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(c.treasury.ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_issued")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(c.totalIssued.ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reserve_ratio")).Append(" ")
                      .Append(c.isGodCompany ? LocalizedTextManager.getText("sm_ui_economy_unlimited") : c.reserveRatio.ToString("P0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_max_issue")).Append(" ")
                      .Append(c.isGodCompany ? LocalizedTextManager.getText("sm_ui_economy_unlimited") : SuperMechCompany.GetMaxIssueable(c).ToString("N0")).Append("\n");
                    sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_profit")).Append(" ").Append(LocalizedTextManager.getText("sm_inal_symbol")).Append(SuperMechCompany.GetNetProfit(c).ToString("N0")).Append("\n");
                    sb.Append("  Lv.").Append(c.level).Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reputation")).Append(" ").Append(c.reputation).Append("\n\n");
                }
            }
            _companyText.text = sb.ToString();
        }
    }
}
