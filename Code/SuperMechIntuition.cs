using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 冥冥感应系统（原著ch1204/ch1393/ch1441）。
    ///
    /// 原著设定：
    /// - 神性蜕变高段（S阶以上）产生冥冥中的直觉感应，察觉到蜕变的契机
    /// - 感应到一种类似使命的东西，达成使命就能稳步提升神性蜕变
    /// - 用玄乎的话解释，就像是证求自己的"道"
    /// - 克苏耶：成为虚空维度最强大的霸主
    /// - 麦尼逊：成为机械神教主神
    /// - 每个人的道路都是独一无二的
    /// - 面板解释：高阶位突破不再是属性硬指标，而是"做任务"类型
    ///
    /// 模组实现：
    /// - S阶以上星海人单位自动获得一个独属使命
    /// - 使命按所属系/分支生成，每个人不同
    /// - 使命有进度条，完成后给神性蜕变点/突破加成
    /// - 降临者（玩家）的使命显示为"进阶任务"
    /// </summary>
    public static class SuperMechIntuition
    {
        // 使命数据（unit.id -> Destiny）
        private static readonly Dictionary<long, Destiny> _destinies = new Dictionary<long, Destiny>();

        public class Destiny
        {
            public string name;        // 使命名称
            public string description; // 使命描述
            public string type;        // 类型：combat/create/explore/rule/transcend
            public float progress;     // 进度0-100
            public float target;       // 目标值
            public bool completed;     // 是否完成
            public int rewardDivinity; // 奖励神性蜕变点
        }

        // 各系使命模板（按系+分支生成，每个人随机选一个）
        private static readonly string[][] MechDestinies = {
            new[] { "机械神王", "建立覆盖星海的机械神教，成为信徒心中的主神", "rule", "100" },
            new[] { "万机之主", "创造一件使徒兵器级的宇宙宝物", "create", "100" },
            new[] { "军团统帅", "组建一支横扫星域的机械军团", "rule", "100" },
            new[] { "维度行者", "探索并征服一个次级维度", "explore", "100" },
            new[] { "械源觉醒", "将械力提升到宇宙本源层次", "transcend", "100" },
        };

        private static readonly string[][] MartialDestinies = {
            new[] { "武道神话", "开创一个流传万古的武道流派", "create", "100" },
            new[] { "不败战神", "在决斗中击败一百名同阶强者", "combat", "100" },
            new[] { "肉身成圣", "将体魄锤炼到超越种族极限", "transcend", "100" },
            new[] { "极道霸主", "成为某一星域的武道霸主", "rule", "100" },
            new[] { "气吞星海", "将气力修炼到前无古人的层次", "transcend", "100" },
        };

        private static readonly string[][] PsiDestinies = {
            new[] { "异能之源", "将异能进化到操控法则的层次", "transcend", "100" },
            new[] { "基因之主", "破解生命基因密码，创造新物种", "create", "100" },
            new[] { "天灾化身", "以异能之力毁灭一个文明", "combat", "100" },
            new[] { "维度感应", "感应并沟通一个未知维度", "explore", "100" },
            new[] { "精神霸主", "以精神力统治一个星域", "rule", "100" },
        };

        private static readonly string[][] MageDestinies = {
            new[] { "秘法之主", "建造一座秘法之殿级的法师塔", "create", "100" },
            new[] { "元素神王", "掌握一种元素的本源力量", "transcend", "100" },
            new[] { "魔网编织者", "构建覆盖星域的魔网系统", "create", "100" },
            new[] { "虚空法师", "探索虚空维度的魔法奥秘", "explore", "100" },
            new[] { "奥术皇帝", "建立以魔法为根基的帝国", "rule", "100" },
        };

        private static readonly string[][] MindDestinies = {
            new[] { "灵魂之主", "将灵魂修炼到不生不灭的层次", "transcend", "100" },
            new[] { "念动乾坤", "以念力扭曲现实法则", "transcend", "100" },
            new[] { "心灵霸主", "以心灵力量统治一个文明", "rule", "100" },
            new[] { "维度沟通者", "与高维存在建立沟通", "explore", "100" },
            new[] { "法则编织者", "掌握一条宇宙法则的权柄", "transcend", "100" },
        };

        /// <summary>Tick：S阶以上单位自动获得使命，推进使命进度。</summary>
        public static void TickIntuition()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f; // 分组4：每4次UnifiedTick调用才跑一次本系统

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 只有S阶以上（index>=10）才有冥冥感应
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                if (rankIdx < 10) continue;

                // 还没有使命的，生成一个
                if (!_destinies.ContainsKey(a.id))
                {
                    GenerateDestiny(a);
                }

                // 推进使命进度
                var d = _destinies[a.id];
                if (d.completed) continue;

                float progressRate = 0.5f * tickInterval; // 基础进度
                // 阶位越高，感应越清晰，进度越快
                progressRate *= (1f + (rankIdx - 10) * 0.3f);
                // 战斗中推进更快（实战感悟）
                if (SuperMechQi.IsInCombat(a)) progressRate *= 2f;
                // 有提炼法的推进更快
                if (a.hasTrait("sm_refinement")) progressRate *= 1.3f;

                d.progress += progressRate;
                if (d.progress >= d.target)
                {
                    d.progress = d.target;
                    d.completed = true;
                    OnDestinyCompleted(a, d);
                }
            }
        }

        /// <summary>为单位生成独属使命（按系+分支随机选择）。</summary>
        private static void GenerateDestiny(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            string[][] pool = MechDestinies;
            switch (cls)
            {
                case "武道系": pool = MartialDestinies; break;
                case "异能系": pool = PsiDestinies; break;
                case "魔法系": pool = MageDestinies; break;
                case "念力系": pool = MindDestinies; break;
                default: pool = MechDestinies; break;
            }

            // 随机选一个使命（每个人的道路独一无二）
            int idx = Random.Range(0, pool.Length);
            var t = pool[idx];
            var d = new Destiny
            {
                name = t[0],
                description = t[1],
                type = t[2],
                target = float.Parse(t[3]),
                progress = 0,
                completed = false,
                rewardDivinity = 3 + Random.Range(0, 3) // 奖励3-5神性蜕变点
            };
            _destinies[a.id] = d;
            Debug.Log($"[超神机械师] {a.name}（{cls}）感应到冥冥中的使命：{d.name}——{d.description}");
        }

        /// <summary>使命完成：奖励神性蜕变点，大幅提升突破概率。</summary>
        private static void OnDestinyCompleted(Actor a, Destiny d)
        {
            Debug.Log($"[超神机械师] {a.name} 完成使命【{d.name}】！获得{d.rewardDivinity}神性蜕变点");
            // 使命完成后大幅提升气力上限和属性（证道成功）
            SuperMechQi.AddQiMax(a, 50000f);
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                stats["intelligence"] = (stats["intelligence"]) + 20f;
                stats["damage"] = (stats["damage"]) + 30f;
                stats["health"] = (stats["health"]) + 500f;
            }
            // 生成新的更高层次使命（证道之后还有更高的道）
            _destinies.Remove(a.id);
        }

        /// <summary>获取单位的使命（面板显示用）。</summary>
        public static Destiny GetDestiny(Actor a)
        {
            if (a == null) return null;
            Destiny d;
            _destinies.TryGetValue(a.id, out d);
            return d;
        }

        /// <summary>清除数据。</summary>
        public static void Clear() { _destinies.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_destinies, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _destinies.Remove(a.id);
        }
    }
}
