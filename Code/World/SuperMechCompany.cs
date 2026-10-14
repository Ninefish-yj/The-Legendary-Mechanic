using System;
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
            public bool isGodCompany = false;  // 是否为上帝创建的公司
            public int level = 1;
            public long treasury = 0;    // 公司金库（储备金，伊纳尔）
            public long totalIssued = 0; // 已发行货币总量（M2）
            public float reserveRatio = 0.2f;  // 准备金率（默认20%，部分准备金制度）
            public float baseInterestRate = 0.05f;  // 基准利率（5%）
            public long totalLoans = 0;  // 贷款总额
            public long revenue = 0;     // 累计营收
            public long expenses = 0;    // 累计支出
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

        /// <summary>获取所有公司列表（v0.140.0 供CompanyAI使用）</summary>
        public static List<CompanyDef> GetAllCompanies()
        {
            return new List<CompanyDef>(_companies.Values);
        }

        /// <summary>公司等级决定每tick伊纳尔发行量</summary>
        public static int GetIssueAmount(CompanyDef c)
        {
            if (c == null) return 0;
            // 基础发行量 = 等级 × 100，黑星公司额外+50%（机械制造利润高）
            int baseAmount = c.level * 100;
            if (c.id == BlackStarCorp) baseAmount = (int)(baseAmount * 1.5f);
            return baseAmount;
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

        // ══════════════════════════════════════════════
        // 上帝创建公司（玩家作为央行发行货币）
        // ══════════════════════════════════════════════

        /// <summary>上帝创建公司，作为货币发行机构</summary>
        public static CompanyDef CreateGodCompany(string nameKey, long initialReserve)
        {
            string id = "god_company_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var c = new CompanyDef
            {
                id = id,
                nameKey = nameKey,
                founderId = "god",
                isGodCompany = true,
                level = 1,
                treasury = initialReserve,
                reserveRatio = 0.2f,
                baseInterestRate = 0.05f
            };
            _companies[id] = c;
            Debug.Log($"[超神机械师] 上帝创建公司: {id}, 初始储备金: {initialReserve}");
            return c;
        }

        /// <summary>获取上帝创建的公司列表</summary>
        public static List<CompanyDef> GetGodCompanies()
        {
            var list = new List<CompanyDef>();
            foreach (var c in _companies.Values)
                if (c.isGodCompany) list.Add(c);
            return list;
        }

        // ══════════════════════════════════════════════
        // 部分准备金货币发行制度
        // ══════════════════════════════════════════════

        /// <summary>计算最大可发行货币量 = 储备金 / 准备金率（上帝公司拥有无限发行权）</summary>
        public static long GetMaxIssueable(CompanyDef c)
        {
            if (c == null) return 0;
            if (c.isGodCompany) return long.MaxValue;  // 上帝公司：无限发行权
            if (c.reserveRatio <= 0) return 0;
            return (long)(c.treasury / c.reserveRatio);
        }

        /// <summary>计算剩余可发行量（上帝公司无限）</summary>
        public static long GetRemainingIssueable(CompanyDef c)
        {
            if (c == null) return 0;
            if (c.isGodCompany) return long.MaxValue;  // 上帝公司：无限发行权
            return System.Math.Max(0, GetMaxIssueable(c) - c.totalIssued);
        }

        /// <summary>发行货币：从公司金库发行到市场流通（上帝公司不受准备金约束）</summary>
        public static bool IssueCurrency(CompanyDef c, long amount)
        {
            if (c == null || amount <= 0) return false;
            if (!c.isGodCompany)
            {
                long remaining = GetRemainingIssueable(c);
                if (amount > remaining)
                {
                    Debug.LogWarning($"[超神机械师] 货币发行超限: 尝试{amount}, 剩余可发行{remaining}");
                    return false;
                }
            }
            c.totalIssued += amount;
            c.revenue += amount;  // 发行收入计入营收
            return true;
        }

        /// <summary>回笼货币：从市场回收，减少流通量</summary>
        public static bool RetireCurrency(CompanyDef c, long amount)
        {
            if (c == null || amount <= 0) return false;
            if (amount > c.totalIssued) return false;
            c.totalIssued -= amount;
            c.expenses += amount;
            return true;
        }

        // ══════════════════════════════════════════════
        // 利率与通胀
        // ══════════════════════════════════════════════

        /// <summary>设定基准利率（影响存贷款和经济活动）</summary>
        public static void SetInterestRate(CompanyDef c, float rate)
        {
            if (c == null) return;
            c.baseInterestRate = Mathf.Clamp(rate, 0f, 0.5f);
        }

        /// <summary>计算全市场通胀率 = (货币供应量增长 - 经济增长) / 基期</summary>
        public static float CalculateInflationRate()
        {
            long totalMoney = 0;
            long totalReserve = 0;
            foreach (var c in _companies.Values)
            {
                totalMoney += c.totalIssued;
                totalReserve += c.treasury;
            }
            if (totalReserve <= 0) return 0f;
            // 简化模型：通胀率 = (M2 - 储备金) / 储备金 × 利率系数
            float inflation = (float)(totalMoney - totalReserve) / totalReserve * 0.1f;
            return Mathf.Clamp(inflation, -0.1f, 1.0f);
        }

        /// <summary>获取全市场货币供应量（M2）</summary>
        public static long GetTotalMoneySupply()
        {
            long total = 0;
            foreach (var c in _companies.Values)
                total += c.totalIssued;
            return total;
        }

        /// <summary>获取全市场储备金总量</summary>
        public static long GetTotalReserves()
        {
            long total = 0;
            foreach (var c in _companies.Values)
                total += c.treasury;
            return total;
        }

        /// <summary>计算公司净利润 = 营收 - 支出</summary>
        public static long GetNetProfit(CompanyDef c)
        {
            if (c == null) return 0;
            return c.revenue - c.expenses;
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
                    nameKey = c.nameKey,
                    isGodCompany = c.isGodCompany,
                    level = c.level,
                    treasury = c.treasury,
                    totalIssued = c.totalIssued,
                    reserveRatio = c.reserveRatio,
                    baseInterestRate = c.baseInterestRate,
                    totalLoans = c.totalLoans,
                    revenue = c.revenue,
                    expenses = c.expenses,
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
                    c.totalIssued = s.totalIssued;
                    c.reserveRatio = s.reserveRatio > 0 ? s.reserveRatio : 0.2f;
                    c.baseInterestRate = s.baseInterestRate;
                    c.totalLoans = s.totalLoans;
                    c.revenue = s.revenue;
                    c.expenses = s.expenses;
                    c.reputation = s.reputation;
                }
                else if (s.isGodCompany)
                {
                    // 恢复上帝创建的公司
                    var gc = new CompanyDef
                    {
                        id = s.id,
                        nameKey = s.nameKey ?? "sm_company_god_default",
                        founderId = "god",
                        isGodCompany = true,
                        level = s.level,
                        treasury = s.treasury,
                        totalIssued = s.totalIssued,
                        reserveRatio = s.reserveRatio > 0 ? s.reserveRatio : 0.2f,
                        baseInterestRate = s.baseInterestRate,
                        totalLoans = s.totalLoans,
                        revenue = s.revenue,
                        expenses = s.expenses,
                        reputation = s.reputation
                    };
                    _companies[s.id] = gc;
                }
            }
        }

        [Serializable]
        public class CompanySaveData
        {
            public string id;
            public string nameKey;
            public bool isGodCompany;
            public int level;
            public long treasury;
            public long totalIssued;
            public float reserveRatio;
            public float baseInterestRate;
            public long totalLoans;
            public long revenue;
            public long expenses;
            public int reputation;
        }
    }
}
