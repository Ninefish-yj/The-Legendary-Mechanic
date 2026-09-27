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
            Add("sm_ui_knowledge", "知识");

        // === 知识节点名（249个）===
        Add("sm_know_000", "基础组装");
        Add("sm_know_001", "基础机械工程学");
        Add("sm_know_002", "基础仿生学");
        Add("sm_know_003", "基础武器学");
        Add("sm_know_004", "基础材料大全");
        Add("sm_know_005", "基础电磁原理");
        Add("sm_know_006", "基础声学");
        Add("sm_know_007", "基础虚拟电子技术");
        Add("sm_know_008", "基础力学原理");
        Add("sm_know_009", "基础广域感应");
        Add("sm_know_010", "基础生化");
        Add("sm_know_011", "基础能源理论");
        Add("sm_know_012", "基础光学");
        Add("sm_know_013", "基础热力学");
        Add("sm_know_014", "基础能量转化");
        Add("sm_know_015", "高密物质压缩技术");
        Add("sm_know_016", "进阶材料合成");
        Add("sm_know_017", "重装机械改造");
        Add("sm_know_018", "微型机械改装");
        Add("sm_know_019", "进阶机械动力学");
        Add("sm_know_020", "神经链接");
        Add("sm_know_021", "初级空间技术");
        Add("sm_know_022", "进阶电磁学");
        Add("sm_know_023", "进阶探测技术");
        Add("sm_know_024", "进阶智能技术");
        Add("sm_know_025", "秒级拆分重组");
        Add("sm_know_026", "进阶能量理论");
        Add("sm_know_027", "巨型复式机械技术");
        Add("sm_know_028", "高端材料科技");
        Add("sm_know_029", "高级虚拟智能科技");
        Add("sm_know_030", "星际航行技术");
        Add("sm_know_031", "量子纠缠高级应用");
        Add("sm_know_032", "高端电磁力场");
        Add("sm_know_033", "纳米动力");
        Add("sm_know_034", "高能掌握");
        Add("sm_know_035", "高能武器化应用");
        Add("sm_know_036", "可控湮灭武器");
        Add("sm_know_037", "尖端材料学");
        Add("sm_know_038", "超复合型机械架构技术");
        Add("sm_know_039", "高阶空间应用");
        Add("sm_know_040", "量子矩阵思维场");
        Add("sm_know_041", "时空维度勘探");
        Add("sm_know_042", "异态能量");
        Add("sm_know_043", "超视距能量传输");
        Add("sm_know_044", "活性化理论");
        Add("sm_know_045", "无尽材料学");
        Add("sm_know_046", "终极机械工程学");
        Add("sm_know_047", "虚拟造物主");
        Add("sm_know_048", "超时空理论");
        Add("sm_know_049", "机械生命火种");
        Add("sm_know_050", "永恒能源");
        Add("sm_know_051", "骨密度增生");
        Add("sm_know_052", "肌肉组织缔合");
        Add("sm_know_053", "初级自愈");
        Add("sm_know_054", "活性分裂");
        Add("sm_know_055", "神经反射优化");
        Add("sm_know_056", "体术入门");
        Add("sm_know_057", "型态许可战术");
        Add("sm_know_058", "行动循环");
        Add("sm_know_059", "兵器武学大全");
        Add("sm_know_060", "气力聚能");
        Add("sm_know_061", "气力冲动");
        Add("sm_know_062", "气力循环");
        Add("sm_know_063", "气力翻涌");
        Add("sm_know_064", "气力跳跃");
        Add("sm_know_065", "细胞脱分化愈合");
        Add("sm_know_066", "特种骨骼发育");
        Add("sm_know_067", "肌能膨胀压缩");
        Add("sm_know_068", "机体自洁调控");
        Add("sm_know_069", "结缔循环回构");
        Add("sm_know_070", "进阶体术");
        Add("sm_know_071", "形态优势武学");
        Add("sm_know_072", "隔山打牛");
        Add("sm_know_073", "功能创伤武学");
        Add("sm_know_074", "离体波动");
        Add("sm_know_075", "气力压缩");
        Add("sm_know_076", "实体化气焰");
        Add("sm_know_077", "爆气");
        Add("sm_know_078", "闪气");
        Add("sm_know_079", "代谢系统格式优化");
        Add("sm_know_080", "细胞供能物质更新");
        Add("sm_know_081", "肌能纤维马达");
        Add("sm_know_082", "高阶武学");
        Add("sm_know_083", "高等形态综合");
        Add("sm_know_084", "特异性弱点突击战术");
        Add("sm_know_085", "高频气能坍缩");
        Add("sm_know_086", "毁灭性震荡");
        Add("sm_know_087", "高密气能压缩突进");
        Add("sm_know_088", "细胞基元革命");
        Add("sm_know_089", "肌动系统递归重组");
        Add("sm_know_090", "复合型超抵抗细胞");
        Add("sm_know_091", "诸般武道全综");
        Add("sm_know_092", "特种形态突破");
        Add("sm_know_093", "微观战争艺术");
        Add("sm_know_094", "湮灭性气能因子");
        Add("sm_know_095", "结构基石暴力解离");
        Add("sm_know_096", "不可逆性瓦解回归");
        Add("sm_know_097", "血肉神化");
        Add("sm_know_098", "神化生命蓝图");
        Add("sm_know_099", "武道穷举法");
        Add("sm_know_100", "无尽形态模型");
        Add("sm_know_101", "混沌演化");
        Add("sm_know_102", "究极终焉回归");
        Add("sm_know_103", "初级元素理论");
        Add("sm_know_104", "元素合成入门");
        Add("sm_know_105", "自然元素沟通");
        Add("sm_know_106", "常见元素塑形术");
        Add("sm_know_107", "初级元素附着");
        Add("sm_know_108", "基础奥术魔法");
        Add("sm_know_109", "魔导回路入门");
        Add("sm_know_110", "咒术入门");
        Add("sm_know_111", "占卜入门");
        Add("sm_know_112", "初级炼金术");
        Add("sm_know_113", "初级法阵");
        Add("sm_know_114", "初级铭文镌刻");
        Add("sm_know_115", "初级药剂合成");
        Add("sm_know_116", "中等元素课程");
        Add("sm_know_117", "进阶元素合成");
        Add("sm_know_118", "进阶元素塑能");
        Add("sm_know_119", "进阶元素附魔");
        Add("sm_know_120", "进阶奥术魔法");
        Add("sm_know_121", "进阶魔导回路优化");
        Add("sm_know_122", "进阶咒术");
        Add("sm_know_123", "相位魔法");
        Add("sm_know_124", "中等炼金术");
        Add("sm_know_125", "中等阵法课");
        Add("sm_know_126", "中等铭文学");
        Add("sm_know_127", "中等药剂学课程");
        Add("sm_know_128", "高等元素论");
        Add("sm_know_129", "高密元素实体提纯压缩");
        Add("sm_know_130", "活体元素附魔");
        Add("sm_know_131", "高阶奥术魔法");
        Add("sm_know_132", "高阶魔导回路重塑");
        Add("sm_know_133", "高阶咒法");
        Add("sm_know_134", "生死界限突破");
        Add("sm_know_135", "高等炼金术");
        Add("sm_know_136", "高等魔法造物综合");
        Add("sm_know_137", "古典生命炼金学");
        Add("sm_know_138", "元素解剖示意模型");
        Add("sm_know_139", "高等元素生命学");
        Add("sm_know_140", "超复合元素合成转换");
        Add("sm_know_141", "奥术理论突破");
        Add("sm_know_142", "魔导回路格式革新");
        Add("sm_know_143", "预言术");
        Add("sm_know_144", "神话炼金术");
        Add("sm_know_145", "创生魔法");
        Add("sm_know_146", "超能显圣奇观筑造");
        Add("sm_know_147", "寰宇基石单元");
        Add("sm_know_148", "元素恒星反应堆");
        Add("sm_know_149", "奇迹显化");
        Add("sm_know_150", "既定命运");
        Add("sm_know_151", "创世法则");
        Add("sm_know_152", "神话起源图腾");
        Add("sm_know_153", "基础心灵单元");
        Add("sm_know_154", "基础心灵感应");
        Add("sm_know_155", "基础催眠");
        Add("sm_know_156", "基础幻觉");
        Add("sm_know_157", "基础通灵");
        Add("sm_know_158", "低级唯心干涉");
        Add("sm_know_159", "低级信息模拟");
        Add("sm_know_160", "低级主观空间感应");
        Add("sm_know_161", "低级时间观扭曲");
        Add("sm_know_162", "低阶意念动能");
        Add("sm_know_163", "低阶虚空作用力");
        Add("sm_know_164", "低阶虚构实体");
        Add("sm_know_165", "小型相互作用取消");
        Add("sm_know_166", "进阶心灵单元");
        Add("sm_know_167", "进阶心灵创伤打击");
        Add("sm_know_168", "进阶幻术");
        Add("sm_know_169", "纯灵生物感应沟通");
        Add("sm_know_170", "人格修改");
        Add("sm_know_171", "中级唯心干涉");
        Add("sm_know_172", "中级拟似信息");
        Add("sm_know_173", "中级主观时空扭曲");
        Add("sm_know_174", "进阶意念动能");
        Add("sm_know_175", "进阶虚空作用力");
        Add("sm_know_176", "进阶虚构实体");
        Add("sm_know_177", "波性频率异常");
        Add("sm_know_178", "高阶心灵单元");
        Add("sm_know_179", "高阶心灵侵略");
        Add("sm_know_180", "高阶幻境制造");
        Add("sm_know_181", "灵魂解剖学");
        Add("sm_know_182", "高级唯心干涉");
        Add("sm_know_183", "高级拟造信息");
        Add("sm_know_184", "高级主观时空伤害化应用");
        Add("sm_know_185", "高等意念动能");
        Add("sm_know_186", "高等虚空作用力");
        Add("sm_know_187", "高等虚构实体");
        Add("sm_know_188", "大型相互作用力修改");
        Add("sm_know_189", "万维心灵网络");
        Add("sm_know_190", "混沌精神回响");
        Add("sm_know_191", "灵魂思维能量场");
        Add("sm_know_192", "规律传递介质");
        Add("sm_know_193", "混沌概率云");
        Add("sm_know_194", "因果修改");
        Add("sm_know_195", "现实扭曲");
        Add("sm_know_196", "物理规律打击");
        Add("sm_know_197", "人造异常维度");
        Add("sm_know_198", "终极灵魂生命");
        Add("sm_know_199", "无尽幻想维度神宫");
        Add("sm_know_200", "芝诺的乌龟·无穷微分");
        Add("sm_know_201", "薛定谔的猫·混沌因果");
        Add("sm_know_202", "拉普拉斯轨道纠缠方程组");
        Add("sm_know_203", "麦克斯韦隧洞更迭不等式");
        Add("sm_know_204", "一阶基因链·能级强化");
        Add("sm_know_205", "一阶基因链·效率提升");
        Add("sm_know_206", "一阶基因链·潜力发掘");
        Add("sm_know_207", "一阶基因链·持久力强化");
        Add("sm_know_208", "一阶基因链·高速冷却");
        Add("sm_know_209", "一阶基因链·损耗缩减");
        Add("sm_know_210", "一阶基因链·范围扩展");
        Add("sm_know_211", "一阶基因链·新功能分化");
        Add("sm_know_212", "一阶基因链·操控强化");
        Add("sm_know_213", "二阶基因链·能级强化");
        Add("sm_know_214", "二阶基因链·效率提升");
        Add("sm_know_215", "二阶基因链·潜力发掘");
        Add("sm_know_216", "二阶基因链·持久力强化");
        Add("sm_know_217", "二阶基因链·高速冷却");
        Add("sm_know_218", "二阶基因链·损耗缩减");
        Add("sm_know_219", "二阶基因链·范围扩展");
        Add("sm_know_220", "二阶基因链·新功能分化");
        Add("sm_know_221", "二阶基因链·操控强化");
        Add("sm_know_222", "三阶基因链·能级强化");
        Add("sm_know_223", "三阶基因链·效率提升");
        Add("sm_know_224", "三阶基因链·潜力发掘");
        Add("sm_know_225", "三阶基因链·持久力强化");
        Add("sm_know_226", "三阶基因链·高速冷却");
        Add("sm_know_227", "三阶基因链·损耗缩减");
        Add("sm_know_228", "三阶基因链·范围扩展");
        Add("sm_know_229", "三阶基因链·新功能分化");
        Add("sm_know_230", "三阶基因链·操控强化");
        Add("sm_know_231", "四阶基因链·能级强化");
        Add("sm_know_232", "四阶基因链·效率提升");
        Add("sm_know_233", "四阶基因链·潜力发掘");
        Add("sm_know_234", "四阶基因链·持久力强化");
        Add("sm_know_235", "四阶基因链·高速冷却");
        Add("sm_know_236", "四阶基因链·损耗缩减");
        Add("sm_know_237", "四阶基因链·范围扩展");
        Add("sm_know_238", "四阶基因链·新功能分化");
        Add("sm_know_239", "四阶基因链·操控强化");
        Add("sm_know_240", "五阶基因链·能级强化");
        Add("sm_know_241", "五阶基因链·效率提升");
        Add("sm_know_242", "五阶基因链·潜力发掘");
        Add("sm_know_243", "五阶基因链·持久力强化");
        Add("sm_know_244", "五阶基因链·高速冷却");
        Add("sm_know_245", "五阶基因链·损耗缩减");
        Add("sm_know_246", "五阶基因链·范围扩展");
        Add("sm_know_247", "五阶基因链·新功能分化");
        Add("sm_know_248", "五阶基因链·操控强化");
        Add("sm_know_249", "枪炮师");
        Add("sm_know_250", "机械师");
        Add("sm_know_251", "械武者");
        Add("sm_know_252", "敏捷");
        Add("sm_know_253", "力量");
        Add("sm_know_254", "防御");
        Add("sm_know_255", "专精法师");
        Add("sm_know_256", "魔网法师");
        Add("sm_know_257", "元素");
        Add("sm_know_258", "灵魂");
        Add("sm_know_259", "法则");
        Add("sm_know_260", "现实");
        Add("sm_know_261", "能级");
        Add("sm_know_262", "操控");
        Add("sm_know_263", "持久力");

            Debug.Log("[超神机械师] 数据本地化注册完成（基础+知识节点）");
        }

        private static void Add(string key, string value)
        {
            LocalizedTextManager.add(key, value, pReplace: true);
        }
    }
}
