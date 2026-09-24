using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系职业知识树（原著知识节点 + 同人二创整理）。
    /// 每系三分支 × 五阶（基础/进阶/高端/尖端/终极）。
    /// 对应设定全录：潜能点点知识树（ch5）。
    /// </summary>
    public static class SuperMechKnowledge
    {
        // 知识阶名
        private static readonly string[] Tiers = { "基础", "进阶", "高端", "尖端", "终极" };

        // 机械系：武装 / 虚拟(操控) / 能量
        private static readonly string[][] Mech = {
            new[] { "基础组装","基础机械工程学","基础仿生学","基础武器学","基础材料大全" },
            new[] { "基础电磁原理","基础声学","基础虚拟电子技术","基础力学原理","基础广域感应" },
            new[] { "基础生化","基础能源理论","基础光学","基础热力学","基础能量转化" },
        };
        private static readonly string[][] MechAdv = {
            new[] { "高密物质压缩技术","进阶材料合成","重装机械改造","微型机械改装","进阶机械动力学" },
            new[] { "神经链接","初级空间技术","进阶电磁学","进阶探测技术","进阶智能技术" },
            new[] { "秒级拆分重组","进阶能量理论","","","" },
        };
        private static readonly string[][] MechHigh = {
            new[] { "巨型复式机械技术","高端材料科技","","","" },
            new[] { "高级虚拟智能科技","星际航行技术","量子纠缠高级应用","高端电磁力场","" },
            new[] { "纳米动力","高能掌握","高能武器化应用","","" },
        };
        private static readonly string[][] MechTop = {
            new[] { "可控湮灭武器","尖端材料学","超复合型机械架构技术","","" },
            new[] { "高阶空间应用","量子矩阵思维场","时空维度勘探","","" },
            new[] { "异态能量","超视距能量传输","活性化理论","","" },
        };
        private static readonly string[][] MechUlt = {
            new[] { "无尽材料学","终极机械工程学","","","" },
            new[] { "虚拟造物主","超时空理论","","","" },
            new[] { "机械生命火种","永恒能源","","","" },
        };

        // 武道系：体魄 / 战术 / 超能
        private static readonly string[][] Martial = {
            new[] { "骨密度增生","肌肉组织缔合","初级自愈","活性分裂","神经反射优化" },
            new[] { "体术入门","型态许可战术","行动循环","兵器武学大全","" },
            new[] { "气力聚能","气力冲动","气力循环","气力翻涌","气力跳跃" },
        };
        private static readonly string[][] MartialAdv = {
            new[] { "细胞脱分化愈合","特种骨骼发育","肌能膨胀压缩","机体自洁调控","结缔循环回构" },
            new[] { "进阶体术","形态优势武学","隔山打牛","功能创伤武学","" },
            new[] { "离体波动","气力压缩","实体化气焰","爆气","闪气" },
        };
        private static readonly string[][] MartialHigh = {
            new[] { "代谢系统格式优化","细胞供能物质更新","肌能纤维马达","","" },
            new[] { "高阶武学","高等形态综合","特异性弱点突击战术","","" },
            new[] { "高频气能坍缩","毁灭性震荡","高密气能压缩突进","","" },
        };
        private static readonly string[][] MartialTop = {
            new[] { "细胞基元革命","肌动系统递归重组","复合型超抵抗细胞","","" },
            new[] { "诸般武道全综","特种形态突破","微观战争艺术","","" },
            new[] { "湮灭性气能因子","结构基石暴力解离","不可逆性瓦解回归","","" },
        };
        private static readonly string[][] MartialUlt = {
            new[] { "血肉飞升","神化生命蓝图","","","" },
            new[] { "武道穷举法","无尽形态模型","","","" },
            new[] { "混沌演化","究极终焉回归","","","" },
        };

        // 魔法系：元素 / 变化 / 造物
        private static readonly string[][] Mage = {
            new[] { "初级元素理论","元素合成入门","自然元素沟通","常见元素塑形术","初级元素附着" },
            new[] { "基础奥术魔法","魔导回路入门","咒术入门","占卜入门","" },
            new[] { "初级炼金术","初级法阵","初级铭文镌刻","初级药剂合成","" },
        };
        private static readonly string[][] MageAdv = {
            new[] { "中等元素课程","进阶元素合成","进阶元素塑能","进阶元素附魔","" },
            new[] { "进阶奥术魔法","进阶魔导回路优化","进阶咒术","相位魔法","" },
            new[] { "中等炼金术","中等阵法课","中等铭文学","中等药剂学课程","" },
        };
        private static readonly string[][] MageHigh = {
            new[] { "高等元素论","高密元素实体提纯压缩","活体元素附魔","","" },
            new[] { "高阶奥术魔法","高阶魔导回路重塑","高阶咒法","生死界限突破","" },
            new[] { "高等炼金术","高等魔法造物综合","古典生命炼金学","","" },
        };
        private static readonly string[][] MageTop = {
            new[] { "元素解剖示意模型","高等元素生命学","超复合元素合成转换","","" },
            new[] { "奥术理论突破","魔导回路格式革新","预言术","","" },
            new[] { "神话炼金术","创生魔法","超能显圣奇观筑造","","" },
        };
        private static readonly string[][] MageUlt = {
            new[] { "寰宇基石单元","元素恒星反应堆","","","" },
            new[] { "奇迹显化","既定命运","","","" },
            new[] { "创世法则","神话起源图腾","","","" },
        };

        // 念力系：灵魂 / 法则 / 现实
        private static readonly string[][] Mind = {
            new[] { "基础心灵单元","基础心灵感应","基础催眠","基础幻觉","基础通灵" },
            new[] { "低级唯心干涉","低级信息模拟","低级主观空间感应","低级时间观扭曲","" },
            new[] { "低阶意念动能","低阶虚空作用力","低阶虚构实体","小型相互作用取消","" },
        };
        private static readonly string[][] MindAdv = {
            new[] { "进阶心灵单元","进阶心灵创伤打击","进阶幻术","纯灵生物感应沟通","人格修改" },
            new[] { "中级唯心干涉","中级拟似信息","中级主观时空扭曲","","" },
            new[] { "进阶意念动能","进阶虚空作用力","进阶虚构实体","波性频率异常","" },
        };
        private static readonly string[][] MindHigh = {
            new[] { "高阶心灵单元","高阶心灵侵略","高阶幻境制造","灵魂解剖学","" },
            new[] { "高级唯心干涉","高级拟造信息","高级主观时空伤害化应用","","" },
            new[] { "高等意念动能","高等虚空作用力","高等虚构实体","大型相互作用力修改","" },
        };
        private static readonly string[][] MindTop = {
            new[] { "万维心灵网络","混沌精神回响","灵魂思维能量场","","" },
            new[] { "规律传递介质","混沌概率云","因果修改","","" },
            new[] { "现实扭曲","物理规律打击","人造异常维度","","" },
        };
        private static readonly string[][] MindUlt = {
            new[] { "终极灵魂生命","无尽幻想维度神宫","","","" },
            new[] { "芝诺的乌龟·无穷微分","薛定谔的猫·混沌因果","","","" },
            new[] { "拉普拉斯轨道纠缠方程组","麦克斯韦隧洞更迭不等式","","","" },
        };

        // 异能系：攻效 / 循环 / 功能（一~五阶基因链）
        private static readonly string[][] Psi = {
            new[] { "一阶基因链·能级强化","一阶基因链·效率提升","一阶基因链·潜力发掘","","" },
            new[] { "一阶基因链·持久力强化","一阶基因链·高速冷却","一阶基因链·损耗缩减","","" },
            new[] { "一阶基因链·范围扩展","一阶基因链·新功能分化","一阶基因链·操控强化","","" },
        };
        private static readonly string[][] PsiAdv = {
            new[] { "二阶基因链·能级强化","二阶基因链·效率提升","二阶基因链·潜力发掘","","" },
            new[] { "二阶基因链·持久力强化","二阶基因链·高速冷却","二阶基因链·损耗缩减","","" },
            new[] { "二阶基因链·范围扩展","二阶基因链·新功能分化","二阶基因链·操控强化","","" },
        };
        private static readonly string[][] PsiHigh = {
            new[] { "三阶基因链·能级强化","三阶基因链·效率提升","三阶基因链·潜力发掘","","" },
            new[] { "三阶基因链·持久力强化","三阶基因链·高速冷却","三阶基因链·损耗缩减","","" },
            new[] { "三阶基因链·范围扩展","三阶基因链·新功能分化","三阶基因链·操控强化","","" },
        };
        private static readonly string[][] PsiTop = {
            new[] { "四阶基因链·能级强化","四阶基因链·效率提升","四阶基因链·潜力发掘","","" },
            new[] { "四阶基因链·持久力强化","四阶基因链·高速冷却","四阶基因链·损耗缩减","","" },
            new[] { "四阶基因链·范围扩展","四阶基因链·新功能分化","四阶基因链·操控强化","","" },
        };
        private static readonly string[][] PsiUlt = {
            new[] { "五阶基因链·能级强化","五阶基因链·效率提升","五阶基因链·潜力发掘","","" },
            new[] { "五阶基因链·持久力强化","五阶基因链·高速冷却","五阶基因链·损耗缩减","","" },
            new[] { "五阶基因链·范围扩展","五阶基因链·新功能分化","五阶基因链·操控强化","","" },
        };

        public static void Register()
        {
            int count = 0;
            // 百度百科：每系职业树名各不相同
            count += RegisterTree("mech", "机械知识树",
                new[] { "枪炮师", "机械师", "械武者" },
                new[] { Mech, MechAdv, MechHigh, MechTop, MechUlt });
            count += RegisterTree("martial", "御气技巧树",
                new[] { "敏捷", "力量", "防御" },
                new[] { Martial, MartialAdv, MartialHigh, MartialTop, MartialUlt });
            count += RegisterTree("mage", "魔法知识树",
                new[] { "专精法师", "魔网法师", "元素" },
                new[] { Mage, MageAdv, MageHigh, MageTop, MageUlt });
            count += RegisterTree("mind", "精神修炼树",
                new[] { "灵魂", "法则", "现实" },
                new[] { Mind, MindAdv, MindHigh, MindTop, MindUlt });
            count += RegisterTree("psi", "基因树",
                new[] { "能级", "操控", "持久力" },
                new[] { Psi, PsiAdv, PsiHigh, PsiTop, PsiUlt });

            Debug.Log($"[超神机械师] 五系知识树注册完成，共 {count} 个知识特质");
        }

        private static int RegisterTree(string prefix, string treeName, string[] branchNames, string[][][] tiers)
        {
            int n = 0;
            for (int ti = 0; ti < tiers.Length; ti++)
            {
                for (int bi = 0; bi < tiers[ti].Length; bi++)
                {
                    for (int ki = 0; ki < tiers[ti][bi].Length; ki++)
                    {
                        string kn = tiers[ti][bi][ki];
                        if (string.IsNullOrEmpty(kn)) continue;
                        string id = $"sm_know_{prefix}_{ti}_{bi}_{ki}";
                        LocalizedTextManager.add("trait_" + id, kn, pReplace: true);
                        LocalizedTextManager.add("trait_" + id + "_info", $"{treeName}·{branchNames[bi]}·{Tiers[ti]}知识", pReplace: true);
                        var t = new ActorTrait
                        {
                            id = id,
                            path_icon = "ui/Icons/actor_traits/iconHardSkin",
                            group_id = $"sm_know_{prefix}",
                            needs_to_be_explored = false,
                            base_stats = new BaseStats()
                        };
                        // 知识阶越高，智力加成越大
                        t.base_stats["intelligence"] = (ti + 1) * 2f;
                        AssetManager.traits.add(t);
                        n++;
                    }
                }
            }
            return n;
        }

        /// <summary>统计单位已解锁的知识节点数（按系前缀）。</summary>
        public static int GetUnlockedCount(Actor a, string prefix)
        {
            if (a == null || a.traits == null) return 0;
            int count = 0;
            string key = "sm_know_" + prefix + "_";
            foreach (var t in a.traits)
            {
                if (t.id != null && t.id.StartsWith(key)) count++;
            }
            return count;
        }

        /// <summary>获取系对应的知识树前缀。</summary>
        public static string GetPrefixForClass(string cls)
        {
            switch (cls)
            {
                case "武道系": return "martial";
                case "异能系": return "psi";
                case "魔法系": return "mage";
                case "念力系": return "mind";
                default: return "mech";
            }
        }
    }
}
