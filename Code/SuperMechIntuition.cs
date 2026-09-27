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
            new[] { "sm_intuition_381", "sm_intuition_382", "rule", "100" },
            new[] { "sm_intuition_383", "sm_intuition_384", "create", "100" },
            new[] { "sm_intuition_385", "sm_intuition_386", "rule", "100" },
            new[] { "sm_intuition_387", "sm_intuition_388", "explore", "100" },
            new[] { "sm_intuition_389", "sm_intuition_390", "transcend", "100" },
        };

        private static readonly string[][] MartialDestinies = {
            new[] { "sm_intuition_391", "sm_intuition_392", "create", "100" },
            new[] { "sm_intuition_393", "sm_intuition_394", "combat", "100" },
            new[] { "sm_intuition_395", "sm_intuition_396", "transcend", "100" },
            new[] { "sm_intuition_397", "sm_intuition_398", "rule", "100" },
            new[] { "sm_intuition_399", "sm_intuition_400", "transcend", "100" },
        };

        private static readonly string[][] PsiDestinies = {
            new[] { "sm_intuition_401", "sm_intuition_402", "transcend", "100" },
            new[] { "sm_intuition_403", "sm_intuition_404", "create", "100" },
            new[] { "sm_intuition_405", "sm_intuition_406", "combat", "100" },
            new[] { "sm_intuition_407", "sm_intuition_408", "explore", "100" },
            new[] { "sm_intuition_409", "sm_intuition_410", "rule", "100" },
        };

        private static readonly string[][] MageDestinies = {
            new[] { "sm_intuition_411", "sm_intuition_412", "create", "100" },
            new[] { "sm_intuition_413", "sm_intuition_414", "transcend", "100" },
            new[] { "sm_intuition_415", "sm_intuition_416", "create", "100" },
            new[] { "sm_intuition_417", "sm_intuition_418", "explore", "100" },
            new[] { "sm_intuition_419", "sm_intuition_420", "rule", "100" },
        };

        private static readonly string[][] MindDestinies = {
            new[] { "sm_intuition_421", "sm_intuition_422", "transcend", "100" },
            new[] { "sm_intuition_423", "sm_intuition_424", "transcend", "100" },
            new[] { "sm_intuition_425", "sm_intuition_426", "rule", "100" },
            new[] { "sm_intuition_427", "sm_intuition_428", "explore", "100" },
            new[] { "sm_intuition_429", "sm_intuition_430", "transcend", "100" },
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
                case "sm_intuition_431": pool = MartialDestinies; break;
                case "sm_intuition_432": pool = PsiDestinies; break;
                case "sm_intuition_433": pool = MageDestinies; break;
                case "sm_intuition_434": pool = MindDestinies; break;
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
