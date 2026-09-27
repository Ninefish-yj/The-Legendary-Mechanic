using System.Collections.Generic;
using NeoModLoader.services;

namespace SuperMech.Code
{
    /// <summary>
    /// 集中注册所有数据定义的本地化key和中文value。
    /// 多语言架构：代码中只用key，中文value在这里注册。
    /// 未来加英文/日文只需翻译此文件或创建对应语言JSON。
    /// </summary>
    public static class SuperMechDataLocale
    {
        private static bool _inited;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;

            // === 五系名称 ===
            Add("sm_class_mech", "机械系");
            Add("sm_class_martial", "武道系");
            Add("sm_class_psi", "异能系");
            Add("sm_class_mage", "魔法系");
            Add("sm_class_mind", "念力系");

            // === 五系方面（原著：武道=神体，念力=神魂，魔法=神权，异能=神通，机械=神器）===
            Add("sm_aspect_mech", "神器");
            Add("sm_aspect_martial", "神体");
            Add("sm_aspect_psi", "神通");
            Add("sm_aspect_mage", "神权");
            Add("sm_aspect_mind", "神魂");

            // === 阶位名 ===
            Add("sm_rank_f", "F");
            Add("sm_rank_e", "E");
            Add("sm_rank_d", "D");
            Add("sm_rank_d_plus", "D+");
            Add("sm_rank_c", "C");
            Add("sm_rank_c_plus", "C+");
            Add("sm_rank_b", "B");
            Add("sm_rank_b_plus", "B+");
            Add("sm_rank_a", "A");
            Add("sm_rank_a_plus", "A+");
            Add("sm_rank_s", "S");
            Add("sm_rank_s_plus", "S+");
            Add("sm_rank_ss", "SS");
            Add("sm_rank_x", "X");

            // === 知识阶名 ===
            Add("sm_tier_basic", "基础");
            Add("sm_tier_advanced", "进阶");
            Add("sm_tier_high", "高端");
            Add("sm_tier_cutting", "尖端");
            Add("sm_tier_ultimate", "终极");

            // === 气力名称 ===
            Add("sm_qi_qi", "气力");
            Add("sm_qi_mech", "械力");
            Add("sm_qi_mage", "魔力");
            Add("sm_qi_mind", "精神力");

            // === 知识树名 ===
            Add("sm_tree_mech", "机械知识树");
            Add("sm_tree_martial", "御气技巧树");
            Add("sm_tree_psi", "基因树");
            Add("sm_tree_mage", "魔法知识树");
            Add("sm_tree_mind", "精神修炼树");
            Add("sm_tree_generic", "知识树");

            // === 装备品质 ===
            Add("sm_quality_0", "普通");
            Add("sm_quality_1", "精良");
            Add("sm_quality_2", "稀有");
            Add("sm_quality_3", "史诗");
            Add("sm_quality_4", "传说");
            Add("sm_quality_5", "珍稀");
            Add("sm_quality_6", "神器");
            Add("sm_quality_7", "使徒兵器");
            Add("sm_quality_8", "宇宙宝物");

            Debug.Log("[超神机械师] 数据本地化注册完成（基础部分）");
        }

        private static void Add(string key, string value)
        {
            LocalizedTextManager.add(key, value, pReplace: true);
        }
    }
}
