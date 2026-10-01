using System.Collections.Generic;
using NeoModLoader.services;

namespace SuperMech.Code
{
    public static class SuperMechRanks
    {
        public class RankDef
        {
            public string id;
            public string name;
            public double onarFloor;
            public float damageMul;
            public float healthMul;
            public string nextRank;
        }

        public static readonly List<RankDef> All = new List<RankDef>
        {
            // 原著能级依据：F=凡人, E=100(ch3), C=2000(ch237), B=10000(ch476),
            // A=17810(ch671), S≈43000(ch864), SS=82600(ch1040), X=148800(ch1402)
            new RankDef { id="sm_rank_00_f",       name="sm_rank_name_00",            onarFloor=0,         damageMul=1.0f,  healthMul=1.0f },
            new RankDef { id="sm_rank_01_e",       name="sm_rank_name_01",            onarFloor=100,       damageMul=1.2f,  healthMul=1.1f },
            new RankDef { id="sm_rank_02_d",       name="sm_rank_name_02",            onarFloor=500,       damageMul=1.4f,  healthMul=1.2f },
            new RankDef { id="sm_rank_03_d_plus",  name="sm_rank_name_03",           onarFloor=1000,      damageMul=1.6f,  healthMul=1.3f },
            new RankDef { id="sm_rank_04_c",       name="sm_rank_name_04",            onarFloor=2000,      damageMul=1.9f,  healthMul=1.5f },
            new RankDef { id="sm_rank_05_c_plus",  name="sm_rank_name_05",           onarFloor=5000,      damageMul=2.2f,  healthMul=1.7f },
            new RankDef { id="sm_rank_06_b",       name="sm_rank_name_06",            onarFloor=10000,     damageMul=2.6f,  healthMul=2.0f },
            new RankDef { id="sm_rank_07_b_plus",  name="sm_rank_name_07",           onarFloor=15000,     damageMul=3.1f,  healthMul=2.4f },
            new RankDef { id="sm_rank_08_a",       name="sm_rank_name_08",    onarFloor=17810,     damageMul=3.8f,  healthMul=2.9f },
            new RankDef { id="sm_rank_09_a_plus",  name="sm_rank_name_09",           onarFloor=30000,     damageMul=4.7f,  healthMul=3.5f },
            new RankDef { id="sm_rank_10_s",       name="sm_rank_name_10",    onarFloor=43000,     damageMul=6.0f,  healthMul=4.5f },
            new RankDef { id="sm_rank_11_s_plus",  name="sm_rank_name_11",           onarFloor=65000,     damageMul=7.5f,  healthMul=5.8f },
            new RankDef { id="sm_rank_12_ss",      name="sm_rank_name_12", onarFloor=82600,     damageMul=9.5f,  healthMul=7.5f },
            new RankDef { id="sm_rank_13_x",       name="sm_rank_name_13",     onarFloor=148800,    damageMul=13.0f, healthMul=10.0f },
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
