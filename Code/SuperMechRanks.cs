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
            // 原著确认门槛：E=100(ch3), D=800(ch116), D+=1600(ch116), C=2000(ch116),
            // B=5000(ch368), A=10000(ch368), 普通超A上限=52000(ch1087/ch1206),
            // 巅峰超A=70000(ch1209), 神性蜕变=78000(ch1039), X≈148800(ch1402)
            // C+/B+/A+/S+门槛为插值推断（原著未明确给出具体数值）
            // F阶仅降临者（玩家）lv1-20专属（ch476），星海人刚觉醒直接E级（ch1051）
            // F阶门槛=10（避免0能级被算成F阶，普通人能级不适用）
            new RankDef { id="sm_rank_00_f",       name="sm_rank_name_00",            onarFloor=10,        damageMul=1.00f, healthMul=1.00f },
            new RankDef { id="sm_rank_01_e",       name="sm_rank_name_01",            onarFloor=100,       damageMul=1.50f, healthMul=1.20f },
            new RankDef { id="sm_rank_02_d",       name="sm_rank_name_02",            onarFloor=800,       damageMul=3.00f, healthMul=1.80f },
            new RankDef { id="sm_rank_03_d_plus",  name="sm_rank_name_03",           onarFloor=1600,      damageMul=4.50f, healthMul=2.30f },
            new RankDef { id="sm_rank_04_c",       name="sm_rank_name_04",            onarFloor=2000,      damageMul=7.00f, healthMul=3.00f },
            new RankDef { id="sm_rank_05_c_plus",  name="sm_rank_name_05",           onarFloor=3500,      damageMul=10.0f, healthMul=4.00f },
            new RankDef { id="sm_rank_06_b",       name="sm_rank_name_06",            onarFloor=5000,      damageMul=15.0f, healthMul=6.00f },
            new RankDef { id="sm_rank_07_b_plus",  name="sm_rank_name_07",           onarFloor=7500,      damageMul=22.0f, healthMul=8.00f },
            new RankDef { id="sm_rank_08_a",       name="sm_rank_name_08",    onarFloor=10000,     damageMul=35.0f, healthMul=12.00f },
            new RankDef { id="sm_rank_09_a_plus",  name="sm_rank_name_09",           onarFloor=30000,     damageMul=55.0f, healthMul=18.00f },
            new RankDef { id="sm_rank_10_s",       name="sm_rank_name_10",    onarFloor=52000,     damageMul=90.0f, healthMul=30.00f },
            new RankDef { id="sm_rank_11_s_plus",  name="sm_rank_name_11",           onarFloor=60000,     damageMul=130.0f, healthMul=45.00f },
            new RankDef { id="sm_rank_12_ss",      name="sm_rank_name_12", onarFloor=70000,     damageMul=200.0f, healthMul=70.00f },
            new RankDef { id="sm_rank_13_x",       name="sm_rank_name_13",     onarFloor=140000,  damageMul=500.0f, healthMul=200.0f },
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
            return LocalizedTextManager.getText("sm_ranks_957");
        }

        /// <summary>按阶位索引获取阶位名称。</summary>
        public static string GetRankName(int index)
        {
            if (index >= 0 && index < All.Count) return All[index].name;
            return LocalizedTextManager.getText("sm_ranks_957");
        }
    }
}
