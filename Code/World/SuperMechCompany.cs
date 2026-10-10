using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 降临者公司系统（新设定：三大文明升维后，伊纳尔由降临者公司发行）
    /// 黑星公司（韩萧化身·机械系）+ 地球商会（地球人群体·贸易系）
    /// 公司发行伊纳尔作为星际层通用货币，超A俸禄从公司金库支出
    /// </summary>
    public static class SuperMechCompany
    {
        public const string BlackStarCorp = "black_star_corp";
        public const string EarthChamber = "earth_chamber";

        public class CompanyDef
        {
            public string id;
            public string nameKey;       // 本地化key
            public string founderId;     // 创立者特质ID
            public int level = 1;
            public long treasury = 0;    // 公司金库（伊纳尔）
            public int reputation = 0;   // 声望
            public int tickCounter = 0;
        }

        private static readonly Dictionary<string, CompanyDef> _companies = new Dictionary<string, CompanyDef>();
        private const int IssueInterval = 64;  // 每64tick发行一次

        public static void Register()
        {
            _companies[BlackStarCorp] = new CompanyDef
            {
                id = BlackStarCorp,
                nameKey = "sm_company_blackstar",
                founderId = "sm_player_hanxiao",
                treasury = 10000  // 初始启动资金
            };
            _companies[EarthChamber] = new CompanyDef
            {
                id = EarthChamber,
                nameKey = "sm_company_earth",
                founderId = SuperMechTraits.Descendant,
                treasury = 5000
            };
        }

        public static CompanyDef GetCompany(string id)
        {
            return _companies.TryGetValue(id, out var c) ? c : null;
        }

        public static CompanyDef GetCompanyByActor(Actor a)
        {
            if (a == null) return null;
            if (a.hasTrait("sm_player_hanxiao")) return GetCompany(BlackStarCorp);
            if (a.hasTrait(SuperMechTraits.Descendant)) return GetCompany(EarthChamber);
            return null;
        }

        /// <summary>公司等级决定每tick伊纳尔发行量</summary>
        public static int GetIssueAmount(CompanyDef c)
        {
            if (c == null) return 0;
            // 基础发行量 = 等级 × 100，黑星公司额外+50%（机械制造利润高）
            int base = c.level * 100;
            if (c.id == BlackStarCorp) base = (int)(base * 1.5f);
            return base;
        }

        /// <summary>发行Tick：公司产生伊纳尔存入金库</summary>
        public static void TickIssue()
        {
            foreach (var c in _companies.Values)
            {
                c.tickCounter++;
                if (c.tickCounter % IssueInterval != 0) continue;
                int issue = GetIssueAmount(c);
                c.treasury += issue;
                c.reputation += c.level;
            }
        }

        /// <summary>从公司金库支出俸禄，金库不足时返回false</summary>
        public static bool PaySalary(CompanyDef c, int amount)
        {
            if (c == null || amount <= 0) return false;
            if (c.treasury < amount) return false;
            c.treasury -= amount;
            return true;
        }

        /// <summary>公司升级：消耗伊纳尔和声望</summary>
        public static bool Upgrade(CompanyDef c)
        {
            if (c == null || c.level >= 10) return false;
            int cost = c.level * 5000;
            int repCost = c.level * 100;
            if (c.treasury < cost || c.reputation < repCost) return false;
            c.treasury -= cost;
            c.reputation -= repCost;
            c.level++;
            return true;
        }

        /// <summary>获取公司显示文本</summary>
        public static string GetDisplayText(CompanyDef c)
        {
            if (c == null) return "";
            string name = LocalizedTextManager.getText(c.nameKey);
            return $"{name} Lv.{c.level} | {LocalizedTextManager.getText("sm_inal_balance")}: {c.treasury:N0}";
        }

        public static void Clear()
        {
            _companies.Clear();
        }

        /// <summary>序列化所有公司数据用于存档</summary>
        public static List<CompanySaveData> GetAllForSave()
        {
            var list = new List<CompanySaveData>();
            foreach (var c in _companies.Values)
            {
                list.Add(new CompanySaveData
                {
                    id = c.id,
                    level = c.level,
                    treasury = c.treasury,
                    reputation = c.reputation
                });
            }
            return list;
        }

        /// <summary>从存档恢复公司数据</summary>
        public static void LoadFromSave(List<CompanySaveData> list)
        {
            if (list == null) return;
            foreach (var s in list)
            {
                if (_companies.TryGetValue(s.id, out var c))
                {
                    c.level = s.level;
                    c.treasury = s.treasury;
                    c.reputation = s.reputation;
                }
            }
        }

        [Serializable]
        public class CompanySaveData
        {
            public string id;
            public int level;
            public long treasury;
            public int reputation;
        }
    }
}
