using System;
using System.Collections.Generic;
using System.Reflection;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族进化系统（原著 ch770/ch1402）。
    /// ch770原文："成为超A级相当于进化成了独一无二的新物种"。
    /// 每个S阶（超A）单位物种蜕变后创建以自己名字命名的独立亚种，并获得专属种族天赋。
    /// X阶（超神级）物种神化，亚种升级为"{名}神系·王族血脉"，再获得两个遗传性天赋。
    /// 参考 DivineAscension 登神长阶的 TranscendentSpeciesSystem 独立亚种实现。
    /// </summary>
    public static class SuperMechRace
    {
        // 单位标记特质
        public const string TraitSuperARace = "sm_race_super_a";    // S阶物种蜕变标记
        public const string TraitDivineRace = "sm_race_divine";     // X阶物种神化标记

        // X阶两个遗传性天赋（ch1402韩萧选的，作为神化标配）
        public const string SubspeciesDivineGene = "sm_subspecies_divine_gene";  // 【神力基因】
        public const string SubspeciesBornElite  = "sm_subspecies_born_elite";   // 【天生精英】

        // S阶专属种族天赋池（按职业系）
        public class RaceTalentDef
        {
            public string id;
            public string name;
            public string desc;
            public string classId;  // 对应职业系
            public int intel;
            public float dmgMul, hpMul, spdMul;
        }

        public static readonly List<RaceTalentDef> TalentPool = new List<RaceTalentDef>
        {
            // 机械系（原著ch770韩萧选的【机械天才】）
            new RaceTalentDef { id="sm_rt_mech_genius", name="机械天才", classId=SuperMechTraits.ClassMech,
                desc="ch770：机械总亲和1.25x，机械造物性能+40%，机械系技能等级+1。",
                intel=10, dmgMul=0.25f, hpMul=0f, spdMul=0.10f },
            // 武道系
            new RaceTalentDef { id="sm_rt_indestructible", name="不灭之躯", classId=SuperMechTraits.ClassMartial,
                desc="ch927：武道系超A种族天赋，肉身不灭，恢复力极强。生命+30%，护甲+20。",
                intel=5, dmgMul=0.15f, hpMul=0.30f, spdMul=0f },
            // 异能系
            new RaceTalentDef { id="sm_rt_pure_blood", name="纯净血脉", classId=SuperMechTraits.ClassPsi,
                desc="ch919：异能系超A种族天赋，基因链纯净，异能威力+25%。",
                intel=12, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            // 魔法系
            new RaceTalentDef { id="sm_rt_mana_source", name="魔力源泉", classId=SuperMechTraits.ClassMage,
                desc="魔法系超A种族天赋，魔力池浩瀚，魔法威力+25%，魔力+200。",
                intel=12, dmgMul=0.25f, hpMul=0f, spdMul=0f },
            // 念力系
            new RaceTalentDef { id="sm_rt_spirit_ocean", name="精神海洋", classId=SuperMechTraits.ClassMind,
                desc="念力系超A种族天赋，精神力浩瀚，念力威力+25%，智力+15。",
                intel=15, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
        };

        private static bool _registered = false;
        private static readonly System.Random _rng = new System.Random();

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            // 单位标记特质
            AddMarkerTrait(TraitSuperARace, "物种蜕变", "ch770：超A级物种蜕变，成为独一无二的新物种。全属性+40%。", 0.40f, 0.40f, 15);
            AddMarkerTrait(TraitDivineRace, "物种神化", "ch1402：X阶物种神化，神系王族血脉。全属性+100%。", 1.00f, 1.00f, 30);

            // S阶专属种族天赋池
            foreach (var t in TalentPool)
                AddSubspeciesTrait(t.id, t.name, t.desc, t.intel, t.dmgMul, t.hpMul, t.spdMul);

            // X阶两个遗传性天赋
            AddSubspeciesTrait(SubspeciesDivineGene, "神力基因",
                "ch1402：能力强度+10%，每次进阶+2%（X阶累计46%）。遗传性天赋。", 0, 0.46f, 0f, 0f);
            AddSubspeciesTrait(SubspeciesBornElite, "天生精英",
                "ch1402：全属性+10%，升级额外获得自由属性点（累计3360点）。遗传性天赋。", 5, 0.10f, 0.10f, 0f);

            Debug.Log("[超神机械师] 种族系统注册完成：2标记特质 + 5专属种族天赋 + 2神化遗传天赋");
        }

        /// <summary>根据阶位自动进化种族。S阶→以单位名创建独立亚种+专属天赋；X阶→神化升级。</summary>
        public static void AutoEvolve(Actor a, int rankIndex)
        {
            if (a == null) return;

            // X阶：物种神化
            if (rankIndex >= 13)
            {
                if (!a.hasTrait(TraitDivineRace))
                {
                    a.addTrait(TraitDivineRace);
                    // 如果已有S阶亚种，改名为神系王族血脉；否则创建
                    string divineName = $"{a.Name}神系·王族血脉";
                    if (a.subspecies != null && a.hasTrait(TraitSuperARace))
                    {
                        SetSubspeciesName(a.subspecies, divineName);
                        // 添加两个神化遗传天赋
                        AddSubspeciesTraitToSpecies(a.subspecies, SubspeciesDivineGene);
                        AddSubspeciesTraitToSpecies(a.subspecies, SubspeciesBornElite);
                    }
                    else
                    {
                        DetachSubspecies(a, divineName,
                            new[] { SubspeciesDivineGene, SubspeciesBornElite });
                    }
                    Debug.Log($"[超神机械师] {a.Name} 物种神化 → {divineName}");
                }
                return;
            }

            // S阶：物种蜕变，创建以自己名字命名的独立亚种
            if (rankIndex >= 10 && !a.hasTrait(TraitSuperARace))
            {
                a.addTrait(TraitSuperARace);
                string raceName = $"{a.Name}族";
                // 按职业系选专属种族天赋
                string talentId = PickRaceTalent(a);
                DetachSubspecies(a, raceName, new[] { talentId });
                Debug.Log($"[超神机械师] {a.Name} 物种蜕变 → {raceName}（专属天赋：{talentId}）");
            }
        }

        /// <summary>按职业系从天赋池选专属种族天赋。</summary>
        private static string PickRaceTalent(Actor a)
        {
            string cls = SuperMechUnitWindow.GetClass(a);
            foreach (var t in TalentPool)
                if (t.classId == cls) return t.id;
            // 无职业系时随机
            return TalentPool[_rng.Next(TalentPool.Count)].id;
        }

        private static void AddMarkerTrait(string id, string name, string desc,
            float dmg, float hp, int intel)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin",
                group_id = "sm_race", can_be_removed = false, can_be_given = false,
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intel;
            t.base_stats["multiplier_damage"] = 1f + dmg;
            t.base_stats["multiplier_health"] = 1f + hp;
            AssetManager.traits.add(t);
        }

        private static void AddSubspeciesTrait(string id, string name, string desc,
            int intel, float dmgMul, float hpMul, float spdMul)
        {
            LocalizedTextManager.add("subspecies_trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("subspecies_trait_" + id + "_info", desc, pReplace: true);
            var st = new SubspeciesTrait
            {
                id = id, can_be_given = false, can_be_removed = false,
                needs_to_be_explored = false, base_stats_meta = new BaseStats()
            };
            if (intel > 0) st.base_stats_meta["intelligence"] = intel;
            if (dmgMul > 0) st.base_stats_meta["multiplier_damage"] = 1f + dmgMul;
            if (hpMul > 0) st.base_stats_meta["multiplier_health"] = 1f + hpMul;
            if (spdMul > 0) st.base_stats_meta["multiplier_speed"] = 1f + spdMul;
            AssetManager.subspecies_traits.add(st);
        }

        private static void AddSubspeciesTraitToSpecies(Subspecies species, string traitId)
        {
            SubspeciesTrait trait = AssetManager.subspecies_traits.get(traitId);
            if (trait != null) species.addTrait(trait);
        }

        /// <summary>脱离原亚种，创建独立亚种并改名。参考 DivineAscension。</summary>
        private static void DetachSubspecies(Actor actor, string subspeciesName, string[] traitIds)
        {
            try
            {
                Subspecies oldSpecies = actor.subspecies;
                Subspecies newSpecies = World.world.subspecies.newSpecies(actor.asset, actor.current_tile);
                if (newSpecies == null)
                {
                    Debug.LogWarning($"[超神机械师] {actor.Name} 创建独立亚种失败");
                    return;
                }

                if (oldSpecies != null)
                {
                    foreach (SubspeciesTrait trait in oldSpecies.getTraits())
                        newSpecies.addTrait(trait);
                    newSpecies.nucleus.cloneFrom(oldSpecies.nucleus);
                    var newBirth = newSpecies.getActorBirthTraits();
                    var oldBirth = oldSpecies.getActorBirthTraits();
                    newBirth.reset();
                    foreach (ActorTrait bTrait in oldBirth.getTraits())
                        newBirth.addTrait(bTrait);
                    oldSpecies.units.Remove(actor);
                }

                foreach (var tid in traitIds)
                    AddSubspeciesTraitToSpecies(newSpecies, tid);

                SetSubspeciesName(newSpecies, subspeciesName);
                actor.setSubspecies(newSpecies);
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 创建独立亚种失败: {e.Message}");
            }
        }

        private static void SetSubspeciesName(Subspecies species, string name)
        {
            try
            {
                var nameField = species.GetType().GetField("name",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null) nameField.SetValue(species, name);
                else
                {
                    var setName = species.GetType().GetMethod("set_name",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    setName?.Invoke(species, new object[] { name });
                }
                var nameLocField = species.GetType().GetField("name_localized",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameLocField != null)
                {
                    var locText = nameLocField.GetValue(species);
                    if (locText != null)
                    {
                        var textField = locText.GetType().GetField("text",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        textField?.SetValue(locText, name);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 设置亚种名失败: {e.Message}");
            }
        }

        /// <summary>获取单位当前种族名。</summary>
        public static string GetRaceName(Actor a)
        {
            if (a == null) return "碳基人类（黄）";
            if (a.subspecies != null && !string.IsNullOrEmpty(a.subspecies.name))
                return a.subspecies.name;
            if (a.hasTrait(TraitDivineRace)) return $"{a.Name}神系·王族血脉";
            if (a.hasTrait(TraitSuperARace)) return $"{a.Name}族";
            return "碳基人类（黄）";
        }
    }
}
