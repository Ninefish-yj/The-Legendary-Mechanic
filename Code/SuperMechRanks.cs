using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 阶位数据（原文 ch3/ch377/ch1040/ch1402）。
    /// 14阶：F→E→D→D+→C→C+→B→B+→A→A+→S→S+→SS→X。
    /// E阶无E+（原著设定）。
    /// 欧纳=战斗力函数评价值（非加减），此处为门槛参考值。
    /// </summary>
    public static class SuperMechRanks
    {
        public class RankDef
        {
            public string id;
            public string name;
            public double onarFloor;     // 进入该阶位的欧纳门槛（参考标定）
            public float damageMul;      // 伤害倍率
            public float healthMul;     // 生命倍率
            public string nextRank;     // 下一阶位 id
        }

        public static readonly List<RankDef> All = new List<RankDef>
        {
            new RankDef { id="sm_rank_f",       name="阶位·F（凡人）",     onarFloor=0,         damageMul=1.00f, healthMul=1.00f },
            new RankDef { id="sm_rank_e",       name="阶位·E",            onarFloor=100,       damageMul=1.50f, healthMul=1.20f },
            new RankDef { id="sm_rank_d",       name="阶位·D",            onarFloor=400,       damageMul=3.00f, healthMul=1.80f },
            new RankDef { id="sm_rank_d_plus",  name="阶位·D+",           onarFloor=800,       damageMul=4.50f, healthMul=2.30f },
            new RankDef { id="sm_rank_c",       name="阶位·C",            onarFloor=1500,      damageMul=7.00f, healthMul=3.00f },
            new RankDef { id="sm_rank_c_plus",  name="阶位·C+",           onarFloor=3000,      damageMul=10.0f, healthMul=4.00f },
            new RankDef { id="sm_rank_b",       name="阶位·B",            onarFloor=6000,      damageMul=15.0f, healthMul=6.00f },
            new RankDef { id="sm_rank_b_plus",  name="阶位·B+",           onarFloor=12000,     damageMul=22.0f, healthMul=8.00f },
            new RankDef { id="sm_rank_a",       name="阶位·A（天灾级）",    onarFloor=25000,     damageMul=35.0f, healthMul=12.0f },
            new RankDef { id="sm_rank_a_plus",  name="阶位·A+",           onarFloor=50000,     damageMul=55.0f, healthMul=18.0f },
            new RankDef { id="sm_rank_s",       name="阶位·S（超A级）",    onarFloor=100000,    damageMul=90.0f, healthMul=30.0f },
            new RankDef { id="sm_rank_s_plus",  name="阶位·S+",           onarFloor=250000,    damageMul=150.0f, healthMul=50.0f },
            new RankDef { id="sm_rank_ss",      name="阶位·SS（巅峰超A）", onarFloor=600000,    damageMul=300.0f, healthMul=100.0f },
            new RankDef { id="sm_rank_x",       name="阶位·X（神化·你即是宇宙）",     onarFloor=2000000,   damageMul=1000.0f, healthMul=500.0f },
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

        /// <summary>判断索引是否是+位（D+/C+/B+/A+/S+）。</summary>
        public static bool IsPlusRank(int index)
        {
            if (index < 0 || index >= All.Count) return false;
            return All[index].id.Contains("_plus");
        }

        /// <summary>获取+位对应的主阶位索引（D+→D, C+→C, etc.）。</summary>
        public static int GetMainRankIndex(int index)
        {
            if (!IsPlusRank(index)) return index;
            // +位的前一个就是主阶位
            return index - 1;
        }

        /// <summary>获取主阶位特质ID（+位返回对应主阶位的ID）。</summary>
        public static string GetMainRankTraitId(int index)
        {
            int mainIdx = GetMainRankIndex(index);
            if (mainIdx >= 0 && mainIdx < All.Count) return All[mainIdx].id;
            return null;
        }

        /// <summary>获取单位当前阶位名称（含+位，从精确阶位字典读取）。</summary>
        public static string GetRankName(Actor a)
        {
            int exact = SuperMechAdvancement.GetExactRankIndex(a);
            if (exact >= 0 && exact < All.Count) return All[exact].name;
            return "凡人";
        }
    }
}
