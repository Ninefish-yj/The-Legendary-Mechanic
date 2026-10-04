using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.18: 气力锻炼法（原著细还原——第780章《赫伯尔恒星熔炉锻炼法》、chapter1005/1013/1039/1189）
    /// 原著机制：超能者修行气力锻炼法，用长久的锻炼慢慢提升气力上限；超A级标配多本，越多气力上限越高。
    /// 实现：持有锻炼法的单位，非战斗状态持续微量提升气力上限；效率 = 层数 × (1+智力×0.02)。
    /// 属性→修炼效率→气力→能级的间接链（原著设定）由此接通。
    /// </summary>
    public static class SuperMechQiRefine
    {
        public const string RefineMethodTrait = "sm_qi_refine";
        public const int MaxRefineLevel = 10;

        private static readonly Dictionary<long, int> _refineLevel = new Dictionary<long, int>();
        private static bool _traitRegistered;

        public static int GetRefineLevel(Actor a)
        {
            if (a == null) return 0;
            int v;
            if (_refineLevel.TryGetValue(a.data.id, out v)) return v;
            if (a.hasTrait(RefineMethodTrait)) return 1;
            return 0;
        }

        public static void AddRefineLevel(Actor a, int add)
        {
            if (a == null || add <= 0) return;
            int cur = GetRefineLevel(a);
            int next = Mathf.Min(MaxRefineLevel, cur + add);
            if (next == cur) return;
            _refineLevel[a.data.id] = next;
            if (!a.hasTrait(RefineMethodTrait))
                a.addTrait(RefineMethodTrait);
            Debug.Log($"[超神机械师] {a.name} 习得气力锻炼法 Lv{next}，气力上限将随长久修行缓慢提升");
        }

        public static void Register()
        {
            if (_traitRegistered) return;
            _traitRegistered = true;
            try
            {
                LocalizedTextManager.add("trait_" + RefineMethodTrait, LocalizedTextManager.getText("sm_qi_refine_080"), pReplace: true);
                LocalizedTextManager.add("trait_" + RefineMethodTrait + "_info", LocalizedTextManager.getText("sm_qi_refine_081"), pReplace: true);
                var t = new ActorTrait
                {
                    id = RefineMethodTrait,
                    path_icon = "actor_traits/iconSpellbook",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                AssetManager.traits.add(t);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 锻炼法trait注册失败: " + e.Message);
            }
        }

        /// <summary>修行tick：非战斗单位按层数持续提升气力上限（原著"长久锻炼慢慢提升"）</summary>
        public static void TickCultivation()
        {
            float tickInterval = 1.25f; // 与同组修炼tick一致（每4tick执行一次）
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;
                int level = GetRefineLevel(a);
                if (level <= 0) continue;
                if (SuperMechQi.IsInCombat(a)) continue; // 战斗中靠战斗成长，和平期靠锻炼法

                float intel = 5f;
                var stats = SuperMechStats.Of(a);
                if (stats != null) intel = stats["intelligence"];

                // 效率：每层0.5/秒 × 智力系数；1层约0.5~0.8/秒（约为战斗成长1.9的1/3），10层约5~8/秒
                float gain = 0.5f * level * (1f + intel * 0.02f) * tickInterval;
                SuperMechQi.AddQiMax(a, gain);
            }
        }
    }
}
