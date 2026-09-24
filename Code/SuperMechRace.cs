using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族进化系统（原著 ch770/ch1402）。
    /// 低阶位（C~A+）的虚空潜影者/混沌观察者/虚空扭曲者/虚空逐星者
    /// 本质是旧种族的天赋变化，用原版虚空特质即可。
    /// 只有S阶（超A）的黑星族和X阶的黑星神系·王族血脉是真正的新种族。
    /// </summary>
    public static class SuperMechRace
    {
        public class RaceDef
        {
            public string id;
            public string name;
            public int minRankIndex;
            public int intell;
            public float dmgMul;
            public float hpMul;
            public string desc;
        }

        // 只有超A以上是真正的新种族（ch770黑星族/ch1402王族血脉）
        public static readonly List<RaceDef> EvolutionChain = new List<RaceDef>
        {
            new RaceDef { id="sm_race_blackstar",       name="黑星族",               minRankIndex=10, intell=15, dmgMul=1.40f, hpMul=1.40f, desc="ch770：超A级物种蜕变，以黑星为名的新种族。" },
            new RaceDef { id="sm_race_blackstar_royal", name="黑星神系·王族血脉",   minRankIndex=13, intell=30, dmgMul=2.00f, hpMul=2.00f, desc="ch1402：X阶物种神化，神系王族血脉。" },
        };

        // 低阶位用原版虚空特质（subspecies_trait_gift_of_void / mutation_skin_void）
        public const string OriginalVoidGift = "subspecies_trait_gift_of_void";
        public const string OriginalVoidForm = "subspecies_trait_mutation_skin_void";

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
            Debug.Log("[超神机械师] 种族进化系统注册完成：2个新种族（黑星族/王族血脉）+ 原版虚空特质");
        }

        /// <summary>根据阶位自动进化：C~A+给原版虚空特质，S阶以上给自定义新种族。</summary>
        public static void AutoEvolve(Actor a, int rankIndex)
        {
            if (a == null) return;

            // S阶以上：自定义新种族
            RaceDef target = null;
            for (int i = EvolutionChain.Count - 1; i >= 0; i--)
            {
                if (rankIndex >= EvolutionChain[i].minRankIndex) { target = EvolutionChain[i]; break; }
            }
            if (target != null)
            {
                if (!a.hasTrait(target.id))
                {
                    foreach (var r in EvolutionChain)
                        if (a.hasTrait(r.id) && r.id != target.id) a.removeTrait(r.id);
                    a.addTrait(target.id);
                    Debug.Log($"[超神机械师] {a.Name} 种族进化 → {target.name}");
                }
                return;
            }

            // C~A+阶：给原版虚空特质（低阶位用旧种族+虚空天赋）
            if (rankIndex >= 4 && !a.hasTrait(OriginalVoidGift))
            {
                a.addTrait(OriginalVoidGift);
                Debug.Log($"[超神机械师] {a.Name} 获得原版虚空天赋（虚无之礼）");
            }
            if (rankIndex >= 6 && !a.hasTrait(OriginalVoidForm))
            {
                a.addTrait(OriginalVoidForm);
                Debug.Log($"[超神机械师] {a.Name} 获得原版虚空形态（虚无形态）");
            }
        }

        /// <summary>获取单位当前种族名。</summary>
        public static string GetRaceName(Actor a)
        {
            if (a == null) return "碳基人类（黄）";
            for (int i = EvolutionChain.Count - 1; i >= 0; i--)
                if (a.hasTrait(EvolutionChain[i].id)) return EvolutionChain[i].name;
            if (a.hasTrait(OriginalVoidForm)) return "虚空形态（旧种族）";
            if (a.hasTrait(OriginalVoidGift)) return "虚空天赋（旧种族）";
            return "碳基人类（黄）";
        }
    }
}
