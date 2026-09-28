using System.Collections.Generic;

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
            new RankDef { id="sm_rank_00_f",       name="sm_rank_name_00",            onarFloor=0,         damageMul=1.00f, healthMul=1.00f },
            new RankDef { id="sm_rank_01_e",       name="sm_rank_name_01",            onarFloor=100,       damageMul=1.50f, healthMul=1.20f },
            new RankDef { id="sm_rank_02_d",       name="sm_rank_name_02",            onarFloor=500,       damageMul=3.00f, healthMul=1.80f },
            new RankDef { id="sm_rank_03_d_plus",  name="sm_rank_name_03",           onarFloor=1000,      damageMul=4.50f, healthMul=2.30f },
            new RankDef { id="sm_rank_04_c",       name="sm_rank_name_04",            onarFloor=2000,      damageMul=7.00f, healthMul=3.00f },
            new RankDef { id="sm_rank_05_c_plus",  name="sm_rank_name_05",           onarFloor=5000,      damageMul=10.0f, healthMul=4.00f },
            new RankDef { id="sm_rank_06_b",       name="sm_rank_name_06",            onarFloor=10000,     damageMul=15.0f, healthMul=6.00f },
            new RankDef { id="sm_rank_07_b_plus",  name="sm_rank_name_07",           onarFloor=25000,     damageMul=22.0f, healthMul=8.00f },
            new RankDef { id="sm_rank_08_a",       name="sm_rank_name_08",    onarFloor=50000,     damageMul=35.0f, healthMul=12.00f },
            new RankDef { id="sm_rank_09_a_plus",  name="sm_rank_name_09",           onarFloor=100000,    damageMul=55.0f, healthMul=18.00f },
            new RankDef { id="sm_rank_10_s",       name="sm_rank_name_10",    onarFloor=200000,    damageMul=90.0f, healthMul=30.00f },
            new RankDef { id="sm_rank_11_s_plus",  name="sm_rank_name_11",           onarFloor=500000,    damageMul=130.0f, healthMul=45.00f },
            new RankDef { id="sm_rank_12_ss",      name="sm_rank_name_12", onarFloor=1000000,   damageMul=200.0f, healthMul=70.00f },
            new RankDef { id="sm_rank_13_x",       name="sm_rank_name_13",     onarFloor=5000000,  damageMul=500.0f, healthMul=200.0f },
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
