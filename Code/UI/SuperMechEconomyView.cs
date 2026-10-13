using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

namespace SuperMech.Code
{
    /// <summary>伊纳尔经济系统窗口（原著：主宇宙通用货币，超A俸禄/佣金/悬赏）
    /// 显示超A俸禄排行榜、公司金库、经济统计</summary>
    public class SuperMechEconomyView : MonoBehaviour
    {
        private Text _statsText;
        private Text _rankText;
        private Text _companyText;

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
            rpRect.anchorMax = new Vector2(0.6f, 1);
            rpRect.pivot = new Vector2(0, 1);
            rpRect.offsetMin = new Vector2(0, -40);
            rpRect.offsetMax = new Vector2(0, -4);
            _rankText = SuperMechUiBuilder.AddText(rankPanel, "", 11, TextAnchor.UpperLeft);
            var rtRect = _rankText.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = new Vector2(8, 4);
            rtRect.offsetMax = new Vector2(-8, -4);

            // 右侧：公司金库
            var companyPanel = SuperMechUiBuilder.CreatePanel(transform, "CompanyPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var cpRect = companyPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.6f, 0);
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
            if (_statsText == null || _rankText == null || _companyText == null) return;
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

            // 顶部统计
            var sb = new StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_economy_total")).Append(" ").Append(totalInal)
              .Append("  |  ").Append(LocalizedTextManager.getText("sm_ui_economy_supera")).Append(" ").Append(superACount)
              .Append("  |  ").Append(LocalizedTextManager.getText("sm_ui_economy_salary_interval")).Append(" 32tick");
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
                    string kingdomName = item.actor.kingdom != null && item.actor.kingdom.kingdom_name != null
                        ? item.actor.kingdom.kingdom_name
                        : LocalizedTextManager.getText("sm_ui_economy_no_kingdom");
                    sb.Append("<color=").Append(rankColor).Append(">").Append(i + 1).Append(".</color> ")
                      .Append(item.actor.name != null ? item.actor.name : "?")
                      .Append("  <color=#93A4BC>").Append(kingdomName).Append("</color>")
                      .Append("\n  ").Append(LocalizedTextManager.getText("sm_ui_economy_balance")).Append(" ").Append(item.balance)
                      .Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_salary")).Append(" ").Append(item.salary)
                      .Append("/32tick\n");
                }
                if (superAList.Count > 15)
                {
                    sb.Append("... ").Append(superAList.Count - 15).Append(" ").Append(LocalizedTextManager.getText("sm_ui_economy_more")).Append("\n");
                }
            }
            _rankText.text = sb.ToString();

            // 公司金库
            sb.Clear();
            sb.Append("<b>").Append(LocalizedTextManager.getText("sm_ui_economy_company")).Append("</b>\n");
            var bs = SuperMechCompany.GetCompany(SuperMechCompany.BlackStarCorp);
            var ec = SuperMechCompany.GetCompany(SuperMechCompany.EarthChamber);
            if (bs != null)
            {
                sb.Append("<color=#4fc3f7>").Append(LocalizedTextManager.getText(bs.nameKey)).Append("</color>\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_treasury")).Append(" ").Append(bs.treasury).Append("\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_level")).Append(" ").Append(bs.level).Append("\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reputation")).Append(" ").Append(bs.reputation).Append("\n\n");
            }
            if (ec != null)
            {
                sb.Append("<color=#81c784>").Append(LocalizedTextManager.getText(ec.nameKey)).Append("</color>\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_treasury")).Append(" ").Append(ec.treasury).Append("\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_level")).Append(" ").Append(ec.level).Append("\n");
                sb.Append("  ").Append(LocalizedTextManager.getText("sm_ui_economy_reputation")).Append(" ").Append(ec.reputation).Append("\n");
            }
            if (bs == null && ec == null)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_economy_no_company")).Append("\n");
            }
            _companyText.text = sb.ToString();
        }
    }
}
