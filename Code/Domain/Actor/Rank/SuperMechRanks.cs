using System.Collections.Generic;
using NeoModLoader.services;

namespace SuperMech.Code
{
    /// <summary>能级阶位定义（F~X 9阶，原著设定）：trait id=sm_rank_0X_x；只定义阶位，晋升逻辑在 SuperMechAdvancement；排行榜(排名)是 SuperMechLeaderboardCard，勿混
    /// v0.221 去掉"巅峰"细分，改回原著9阶位；细分（初期/中期/巅峰）在信息面板通过能级百分比显示</summary>
    public static class SuperMechRanks
    {
        public class RankDef
        {
            public string id;
            public string name;
            public double onarFloor;
            public float damageMul;
            public float healthMul;
            public float aoe;        // 范围攻击
            public float targets;    // 最大目标数
            public float range;      // 攻击射程
            public string nextRank;
        }

        public static readonly List<RankDef> All = new List<RankDef>
        {
            // 原著能级依据：F=凡人, E=800, D=2000(ch237), C=6000+, B=20000,
            // A=43000爆星(ch864天灾级), 超A=65000星系级, 巅峰超A=82600星团级(ch1040), X=148800宇宙级(ch1402)
            new RankDef { id="sm_rank_00_f",   name="sm_rank_name_00", onarFloor=0,      damageMul=1.0f,   healthMul=1.0f,  aoe=0,   targets=0,   range=0 },
            new RankDef { id="sm_rank_01_e",   name="sm_rank_name_01", onarFloor=800,    damageMul=1.6f,   healthMul=1.3f,  aoe=0,   targets=0,   range=0 },
            new RankDef { id="sm_rank_02_d",   name="sm_rank_name_02", onarFloor=2000,   damageMul=2.5f,   healthMul=1.8f,  aoe=0,   targets=0,   range=0 },
            new RankDef { id="sm_rank_03_c",   name="sm_rank_name_03", onarFloor=6000,   damageMul=4.0f,   healthMul=2.7f,  aoe=5,   targets=2,   range=5 },
            new RankDef { id="sm_rank_04_b",   name="sm_rank_name_04", onarFloor=20000,  damageMul=8.0f,   healthMul=5.0f,  aoe=20,  targets=5,   range=15 },
            new RankDef { id="sm_rank_05_a",   name="sm_rank_name_05", onarFloor=43000,  damageMul=25.0f,  healthMul=18.0f, aoe=50,  targets=20,  range=30 },
            new RankDef { id="sm_rank_06_s",   name="sm_rank_name_06", onarFloor=65000,  damageMul=35.0f,  healthMul=25.0f, aoe=75,  targets=30,  range=40 },
            new RankDef { id="sm_rank_07_ss",  name="sm_rank_name_07", onarFloor=82600,  damageMul=50.0f,  healthMul=35.0f, aoe=100, targets=50,  range=50 },
            new RankDef { id="sm_rank_08_x",   name="sm_rank_name_08", onarFloor=148800, damageMul=100.0f, healthMul=70.0f, aoe=200, targets=150, range=100 },
        };

        static SuperMechRanks()
        {
            for (int i = 0; i < All.Count - 1; i++) All[i].nextRank = All[i + 1].id;
        }

        public static RankDef ById(string id)
        {
            foreach (var r in All) if (r.id == id) return r;
            return null;
        }

        /// <summary>v0.221 9阶位后无PlusRank，保留方法兼容旧代码，永远返回false</summary>
        public static bool IsPlusRank(int index)
        {
            return false;
        }

        public static int GetMainRankIndex(int index)
        {
            return index;
        }

        public static string GetMainRankTraitId(int index)
        {
            if (index >= 0 && index < All.Count) return All[index].id;
            return null;
        }

        /// <summary>v0.221 获取阶位细分（初期/中期/巅峰），基于能级在当前阶位中的百分比</summary>
        public static string GetRankSubLevel(Actor a)
        {
            int exact = SuperMechAdvancement.GetExactRankIndex(a);
            if (exact < 0 || exact >= All.Count) return "";
            double onar = SuperMechEnergyLevel.Calculate(a);
            double floor = All[exact].onarFloor;
            double nextFloor = exact + 1 < All.Count ? All[exact + 1].onarFloor : floor * 1.5;
            double range = nextFloor - floor;
            if (range <= 0) return "";
            double pct = (onar - floor) / range;
            if (pct < 0.33) return LocalizedTextManager.getText("sm_rank_sub_early");
            if (pct < 0.66) return LocalizedTextManager.getText("sm_rank_sub_mid");
            return LocalizedTextManager.getText("sm_rank_sub_peak");
        }

        public static string GetRankName(Actor a)
        {
            int exact = SuperMechAdvancement.GetExactRankIndex(a);
            if (exact >= 0 && exact < All.Count) return LocalizedTextManager.getText(All[exact].name);
            return LocalizedTextManager.getText("sm_ranks_957");
        }

        /// <summary>v0.221 带细分的阶位名，如"F级（初期）"</summary>
        public static string GetRankNameWithSub(Actor a)
        {
            string baseName = GetRankName(a);
            string sub = GetRankSubLevel(a);
            return string.IsNullOrEmpty(sub) ? baseName : $"{baseName}（{sub}）";
        }

        public static string GetRankName(int index)
        {
            if (index >= 0 && index < All.Count) return LocalizedTextManager.getText(All[index].name);
            return LocalizedTextManager.getText("sm_ranks_957");
        }
    }
}
