using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族进化系统（原著 ch478/ch586/ch685/ch745/ch770/ch1402）。
    /// 韩萧种族进化链：碳基人类→虚空潜影者→混沌观察者→虚空扭曲者→虚空逐星者→黑星族→黑星神系·王族血脉。
    /// 种族进化与阶位挂钩，达到对应阶位自动进化。
    /// </summary>
    public static class SuperMechRace
    {
        public class RaceDef
        {
            public string id;
            public string name;
            public int minRankIndex;  // 达到该阶位自动进化
            public int intell;
            public float dmgMul;
            public float hpMul;
            public string desc;
        }

        // 原著种族进化链（ch478虚空潜影者/ch586混沌观察者/ch685虚空扭曲者/ch745虚空逐星者/ch770黑星族/ch1402黑星神系王族）
        public static readonly List<RaceDef> EvolutionChain = new List<RaceDef>
        {
            new RaceDef { id="sm_race_void_shadow",   name="虚空潜影者（人形态）",   minRankIndex=4,  intell=3, dmgMul=1.10f, hpMul=1.10f, desc="ch478：首次种族蜕变，潜行与感知强化。" },
            new RaceDef { id="sm_race_chaos_observer", name="混沌观察者（人形态）",  minRankIndex=6,  intell=5, dmgMul=1.15f, hpMul=1.15f, desc="ch586：观察混沌，感知范围大幅提升。" },
            new RaceDef { id="sm_race_void_warp",     name="虚空扭曲者（人形态）",   minRankIndex=8,  intell=8, dmgMul=1.20f, hpMul=1.20f, desc="ch685：扭曲虚空，空间能力觉醒。" },
            new RaceDef { id="sm_race_void_star",     name="虚空逐星者（人形态）",   minRankIndex=9,  intell=10, dmgMul=1.25f, hpMul=1.25f, desc="ch745：逐星而行，星际航行能力。" },
            new RaceDef { id="sm_race_blackstar",     name="黑星族",                 minRankIndex=10, intell=15, dmgMul=1.40f, hpMul=1.40f, desc="ch770：超A级物种蜕变，以黑星为名的新种族。" },
            new RaceDef { id="sm_race_blackstar_royal", name="黑星神系·王族血脉",   minRankIndex=13, intell=30, dmgMul=2.00f, hpMul=2.00f, desc="ch1402：X阶物种神化，神系王族血脉。" },
        };

        public static void Register()
        {
            foreach (var r in EvolutionChain)
            {
                LocalizedTextManager.add("trait_" + r.id, r.name, pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info", r.desc, pReplace: true);
                var t = new ActorTrait
                {
                    id = r.id,
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = "sm_race",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                t.base_stats["intelligence"] = r.intell;
                if (r.dmgMul > 0) t.base_stats["multiplier_damage"] = r.dmgMul;
                if (r.hpMul > 0) t.base_stats["multiplier_health"] = r.hpMul;
                AssetManager.traits.add(t);
            }
            Debug.Log("[超神机械师] 种族进化系统注册完成：6个种族阶段");
        }

        /// <summary>根据阶位自动进化种族（达到对应阶位时替换旧种族特质）。</summary>
        public static void AutoEvolve(Actor a, int rankIndex)
        {
            if (a == null) return;
            RaceDef target = null;
            for (int i = EvolutionChain.Count - 1; i >= 0; i--)
            {
                if (rankIndex >= EvolutionChain[i].minRankIndex) { target = EvolutionChain[i]; break; }
            }
            if (target == null) return;
            if (a.hasTrait(target.id)) return;

            // 移除旧种族特质
            foreach (var r in EvolutionChain)
                if (a.hasTrait(r.id) && r.id != target.id) a.removeTrait(r.id);

            a.addTrait(target.id);
            Debug.Log($"[超神机械师] {a.Name} 种族进化 → {target.name}（阶位{SuperMechRanks.All[rankIndex].name}）");
        }

        /// <summary>获取单位当前种族名。</summary>
        public static string GetRaceName(Actor a)
        {
            if (a == null) return "碳基人类（黄）";
            for (int i = EvolutionChain.Count - 1; i >= 0; i--)
                if (a.hasTrait(EvolutionChain[i].id)) return EvolutionChain[i].name;
            return "碳基人类（黄）";
        }
    }
}
