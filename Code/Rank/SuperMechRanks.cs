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
            // 原著阶位表现(ch3/ch237/ch476/ch671/ch864/ch1040/ch1300+/ch1402)：
            // F=凡人(能级0~100), E=入门(100~500, 海拉E级打1000凡人),
            // C=硬扛小型军队, B=灭城/荡平飞船, A=天灾级毁星球地表,
            // S=超A秒整个星球生命, SS=巅峰超A恒星泡澡/掌控维度,
            // X=超神级宇宙级(打S像S打A一样轻松)
            // 关键差距：X/S ≈ S/A ≈ 2.5~3.5倍
            new RankDef { id="sm_rank_00_f",       name="sm_rank_name_00",            onarFloor=0,         damageMul=1.0f,  healthMul=1.0f },
            new RankDef { id="sm_rank_01_e",       name="sm_rank_name_01",            onarFloor=100,       damageMul=2.0f,  healthMul=1.3f },
            new RankDef { id="sm_rank_02_d",       name="sm_rank_name_02",            onarFloor=500,       damageMul=3.5f,  healthMul=1.7f },
            new RankDef { id="sm_rank_03_d_plus",  name="sm_rank_name_03",           onarFloor=1000,      damageMul=5.0f,  healthMul=2.2f },
            new RankDef { id="sm_rank_04_c",       name="sm_rank_name_04",            onarFloor=2000,      damageMul=7.5f,  healthMul=3.0f },
            new RankDef { id="sm_rank_05_c_plus",  name="sm_rank_name_05",           onarFloor=5000,      damageMul=11.0f, healthMul=4.0f },
            new RankDef { id="sm_rank_06_b",       name="sm_rank_name_06",            onarFloor=10000,     damageMul=16.0f, healthMul=5.5f },
            new RankDef { id="sm_rank_07_b_plus",  name="sm_rank_name_07",           onarFloor=15000,     damageMul=23.0f, healthMul=7.5f },
            new RankDef { id="sm_rank_08_a",       name="sm_rank_name_08",    onarFloor=17810,     damageMul=35.0f, healthMul=11.0f },
            new RankDef { id="sm_rank_09_a_plus",  name="sm_rank_name_09",           onarFloor=30000,     damageMul=50.0f, healthMul=15.0f },
            new RankDef { id="sm_rank_10_s",       name="sm_rank_name_10",    onarFloor=43000,     damageMul=80.0f, healthMul=22.0f },
            new RankDef { id="sm_rank_11_s_plus",  name="sm_rank_name_11",           onarFloor=65000,     damageMul=115.0f,healthMul=30.0f },
            new RankDef { id="sm_rank_12_ss",      name="sm_rank_name_12", onarFloor=82600,     damageMul=170.0f,healthMul=45.0f },
            new RankDef { id="sm_rank_13_x",       name="sm_rank_name_13",     onarFloor=148800,    damageMul=280.0f,healthMul=70.0f },
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
