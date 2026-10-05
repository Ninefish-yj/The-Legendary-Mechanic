using System.Collections.Generic;
using NeoModLoader.services;

namespace SuperMech.Code
{
    /// <summary>能级阶位定义（F~X 14 阶）：trait id=sm_rank_0X_x；只定义阶位，晋升逻辑在 SuperMechAdvancement；排行榜(排名)是 SuperMechLeaderboardCard，勿混</summary>
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
            // 原著能级依据：F=凡人, E=100(ch3), E+=600, D=800, D+=1600, C=2000(ch237),
            // B=6000+, A=10000~33000, A+=20000~33000, S=43000爆星(ch864),
            // S+=星系级, SS=82600星团级(ch1040), X=148800宇宙级(ch1402)
            // v0.36.0调整：阈值回归原著精确数值，A级区间陡升（10000~33000跨度2万+）
            // v0.39.8调整：A级到超A级是质的飞跃（原著第1044章：几十个天灾级联手只能抵挡2个超A一时半会）
            // A+准超A可碾压A级；S超A级可吊捶A+、碾压A级
            new RankDef { id="sm_rank_00_f",       name="sm_rank_name_00",            onarFloor=0,         damageMul=1.0f,  healthMul=1.0f,  aoe=0,    targets=0,   range=0 },
            new RankDef { id="sm_rank_01_e",       name="sm_rank_name_01",            onarFloor=100,       damageMul=1.3f,  healthMul=1.15f, aoe=0,    targets=0,   range=0 },
            new RankDef { id="sm_rank_02_d",       name="sm_rank_name_02",            onarFloor=800,       damageMul=1.6f,  healthMul=1.3f,  aoe=0,    targets=0,   range=0 },
            new RankDef { id="sm_rank_03_d_plus",  name="sm_rank_name_03",           onarFloor=1600,      damageMul=2.0f,  healthMul=1.5f,  aoe=0,    targets=0,   range=0 },
            new RankDef { id="sm_rank_04_c",       name="sm_rank_name_04",            onarFloor=2000,      damageMul=2.5f,  healthMul=1.8f,  aoe=0,    targets=0,   range=0 },
            new RankDef { id="sm_rank_05_c_plus",  name="sm_rank_name_05",           onarFloor=4000,      damageMul=3.2f,  healthMul=2.2f,  aoe=2,    targets=1,   range=2 },
            new RankDef { id="sm_rank_06_b",       name="sm_rank_name_06",            onarFloor=6000,      damageMul=4.0f,  healthMul=2.7f,  aoe=5,    targets=2,   range=5 },
            new RankDef { id="sm_rank_07_b_plus",  name="sm_rank_name_07",           onarFloor=10000,     damageMul=5.5f,  healthMul=3.5f,  aoe=10,   targets=3,   range=8 },
            new RankDef { id="sm_rank_08_a",       name="sm_rank_name_08",    onarFloor=20000,     damageMul=8.0f,  healthMul=5.0f,  aoe=20,   targets=5,   range=15 },
            new RankDef { id="sm_rank_09_a_plus",  name="sm_rank_name_09",           onarFloor=33000,     damageMul=13.0f, healthMul=8.0f,  aoe=30,   targets=10,  range=20 },
            new RankDef { id="sm_rank_10_s",       name="sm_rank_name_10",    onarFloor=43000,     damageMul=25.0f, healthMul=18.0f, aoe=50,   targets=20,  range=30 },
            new RankDef { id="sm_rank_11_s_plus",  name="sm_rank_name_11",           onarFloor=65000,     damageMul=35.0f, healthMul=25.0f, aoe=75,   targets=30,  range=40 },
            new RankDef { id="sm_rank_12_ss",      name="sm_rank_name_12", onarFloor=82600,     damageMul=50.0f, healthMul=35.0f, aoe=100,  targets=50,  range=50 },
            new RankDef { id="sm_rank_13_x",       name="sm_rank_name_13",     onarFloor=148800,    damageMul=100.0f,healthMul=70.0f, aoe=200,  targets=150, range=100 },
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

        public static bool IsPlusRank(int index)
        {
            if (index < 0 || index >= All.Count) return false;
            return All[index].id.Contains("_plus");
        }

        public static int GetMainRankIndex(int index)
        {
            if (!IsPlusRank(index)) return index;
            return index - 1;
        }

        public static string GetMainRankTraitId(int index)
        {
            int mainIdx = GetMainRankIndex(index);
            if (mainIdx >= 0 && mainIdx < All.Count) return All[mainIdx].id;
            return null;
        }

        public static string GetRankName(Actor a)
        {
            int exact = SuperMechAdvancement.GetExactRankIndex(a);
            if (exact >= 0 && exact < All.Count) return LocalizedTextManager.getText(All[exact].name);
            return LocalizedTextManager.getText("sm_ranks_957");
        }

        public static string GetRankName(int index)
        {
            if (index >= 0 && index < All.Count) return LocalizedTextManager.getText(All[index].name);
            return LocalizedTextManager.getText("sm_ranks_957");
        }
    }
}
