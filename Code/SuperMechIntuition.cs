using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechIntuition
    {
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

        public static void TickIntuition()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f; // 分组4：每4次UnifiedTick调用才跑一次本系统

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                if (rankIdx < 10) continue;

                if (!_destinies.ContainsKey(a.id))
                {
                    GenerateDestiny(a);
                }

                var d = _destinies[a.id];
                if (d.completed) continue;

                float progressRate = 0.5f * tickInterval; // 基础进度
                progressRate *= (1f + (rankIdx - 10) * 0.3f);
                if (SuperMechQi.IsInCombat(a)) progressRate *= 2f;
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

        private static void OnDestinyCompleted(Actor a, Destiny d)
        {
            Debug.Log($"[超神机械师] {a.name} 完成使命【{d.name}】！获得{d.rewardDivinity}神性蜕变点");
            SuperMechQi.AddQiMax(a, 50000f);
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                stats["intelligence"] = (stats["intelligence"]) + 20f;
                stats["damage"] = (stats["damage"]) + 30f;
                stats["health"] = (stats["health"]) + 500f;
            }
            _destinies.Remove(a.id);
        }

        public static Destiny GetDestiny(Actor a)
        {
            if (a == null) return null;
            Destiny d;
            _destinies.TryGetValue(a.id, out d);
            return d;
        }

        public static void Clear() { _destinies.Clear(); }

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
