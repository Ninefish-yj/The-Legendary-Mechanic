using System;
using System.Collections.Generic;
using System.Reflection;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechRace
    {
        public const string TraitSuperARace = "sm_race_super_a";
        public const string TraitDivineRace = "sm_race_divine";

        public const string SubspeciesDivineGene = "sm_subspecies_divine_gene";
        public const string SubspeciesBornElite  = "sm_subspecies_born_elite";

        public class RaceTalentDef
        {
            public string id;
            public string name;
            public string desc;
            public string classId;
            public int intel;
            public float dmgMul, hpMul, spdMul, armor;
        }

        public static readonly List<RaceTalentDef> TalentPool = new List<RaceTalentDef>
        {
            new RaceTalentDef { id="sm_rt_mech_genius", name="sm_race_262", classId=SuperMechTraits.ClassMech,
                desc="sm_race_263",
                intel=10, dmgMul=0.25f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mech_sense", name="sm_race_264", classId=SuperMechTraits.ClassMech,
                desc="sm_race_265",
                intel=8, dmgMul=0.20f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_mech_source", name="sm_race_266", classId=SuperMechTraits.ClassMech,
                desc="sm_race_267",
                intel=5, dmgMul=0.15f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mech_overload", name="sm_race_268", classId=SuperMechTraits.ClassMech,
                desc="sm_race_269",
                intel=8, dmgMul=0.30f, hpMul=0f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_mech_swarm", name="sm_race_270", classId=SuperMechTraits.ClassMech,
                desc="sm_race_271",
                intel=12, dmgMul=0.20f, hpMul=0.15f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mech_forge", name="sm_race_272", classId=SuperMechTraits.ClassMech,
                desc="sm_race_273",
                intel=15, dmgMul=0.15f, hpMul=0.10f, spdMul=0f },

            new RaceTalentDef { id="sm_rt_indestructible", name="sm_race_274", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_275",
                intel=5, dmgMul=0.15f, hpMul=0.30f, spdMul=0f, armor=20 },
            new RaceTalentDef { id="sm_rt_resilient_body", name="sm_race_276", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_277",
                intel=3, dmgMul=0.10f, hpMul=0.20f, spdMul=0f, armor=30 },
            new RaceTalentDef { id="sm_rt_divine_body", name="sm_race_278", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_279",
                intel=8, dmgMul=0.15f, hpMul=0.20f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_qi_burst", name="sm_race_280", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_281",
                intel=5, dmgMul=0.30f, hpMul=0.10f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_rock_skin", name="sm_race_282", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_283",
                intel=2, dmgMul=0.05f, hpMul=0.25f, spdMul=0f, armor=40 },
            new RaceTalentDef { id="sm_rt_combat_instinct", name="sm_race_284", classId=SuperMechTraits.ClassMartial,
                desc="sm_race_285",
                intel=6, dmgMul=0.30f, hpMul=0f, spdMul=0.15f },

            new RaceTalentDef { id="sm_rt_pure_blood", name="sm_race_286", classId=SuperMechTraits.ClassPsi,
                desc="sm_race_287",
                intel=12, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_power", name="sm_race_288", classId=SuperMechTraits.ClassPsi,
                desc="sm_race_289",
                intel=18, dmgMul=0.20f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_gene_overflow", name="sm_race_290", classId=SuperMechTraits.ClassPsi,
                desc="sm_race_291",
                intel=15, dmgMul=0.20f, hpMul=0.05f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_ancestor_blood", name="sm_race_292", classId=SuperMechTraits.ClassPsi,
                desc="sm_race_293",
                intel=10, dmgMul=0.35f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_controllable_mutation", name="sm_race_294", classId=SuperMechTraits.ClassPsi,
                desc="sm_race_295",
                intel=12, dmgMul=0.20f, hpMul=0.15f, spdMul=0.10f },

            new RaceTalentDef { id="sm_rt_mana_source", name="sm_race_296", classId=SuperMechTraits.ClassMage,
                desc="sm_race_297",
                intel=12, dmgMul=0.25f, hpMul=0f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_authority", name="sm_race_298", classId=SuperMechTraits.ClassMage,
                desc="sm_race_299",
                intel=18, dmgMul=0.20f, hpMul=0f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_weave_master", name="sm_race_300", classId=SuperMechTraits.ClassMage,
                desc="sm_race_301",
                intel=20, dmgMul=0.30f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_rune_decode", name="sm_race_302", classId=SuperMechTraits.ClassMage,
                desc="sm_race_303",
                intel=15, dmgMul=0.15f, hpMul=0.10f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mana_surge", name="sm_race_304", classId=SuperMechTraits.ClassMage,
                desc="sm_race_305",
                intel=10, dmgMul=0.15f, hpMul=0.15f, spdMul=0.10f },

            new RaceTalentDef { id="sm_rt_spirit_field", name="sm_race_306", classId=SuperMechTraits.ClassMind,
                desc="sm_race_307",
                intel=15, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_soul", name="sm_race_308", classId=SuperMechTraits.ClassMind,
                desc="sm_race_309",
                intel=20, dmgMul=0.30f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_consciousness_control", name="sm_race_310", classId=SuperMechTraits.ClassMind,
                desc="sm_race_311",
                intel=20, dmgMul=0.20f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mental_burn", name="sm_race_312", classId=SuperMechTraits.ClassMind,
                desc="sm_race_313",
                intel=18, dmgMul=0.40f, hpMul=0.05f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_reality_warp", name="sm_race_314", classId=SuperMechTraits.ClassMind,
                desc="sm_race_315",
                intel=22, dmgMul=0.15f, hpMul=0.15f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_consciousness_projection", name="sm_race_316", classId=SuperMechTraits.ClassMind,
                desc="sm_race_317",
                intel=16, dmgMul=0.20f, hpMul=0.10f, spdMul=0.20f },

            new RaceTalentDef { id="sm_rt_void_echo", name="sm_race_318", classId="sm_race_319",
                desc="sm_race_320",
                intel=15, dmgMul=0.25f, hpMul=0.15f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_supreme_piety", name="sm_race_321", classId="sm_race_319",
                desc="sm_race_322",
                intel=18, dmgMul=0.10f, hpMul=0.20f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_chaos_release", name="sm_race_323", classId="sm_race_319",
                desc="sm_race_324",
                intel=12, dmgMul=0.15f, hpMul=0.25f, spdMul=0.20f },
            new RaceTalentDef { id="sm_rt_adaptive_swarm", name="sm_race_325", classId="sm_race_319",
                desc="sm_race_326",
                intel=8, dmgMul=0.10f, hpMul=0.15f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_void_sight", name="sm_race_327", classId="sm_race_319",
                desc="sm_race_328",
                intel=20, dmgMul=0.10f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mark_observation", name="sm_race_329", classId="sm_race_319",
                desc="sm_race_330",
                intel=10, dmgMul=0.15f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_void_metamorphosis", name="sm_race_331", classId="sm_race_319",
                desc="sm_race_332",
                intel=14, dmgMul=0.30f, hpMul=0.10f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_void_shatter", name="sm_race_333", classId="sm_race_319",
                desc="sm_race_334",
                intel=10, dmgMul=0.25f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_warp_void", name="sm_race_335", classId="sm_race_319",
                desc="sm_race_336",
                intel=16, dmgMul=0.15f, hpMul=0.20f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_void_blink", name="sm_race_337", classId="sm_race_319",
                desc="sm_race_338",
                intel=12, dmgMul=0.10f, hpMul=0.10f, spdMul=0.30f },

            new RaceTalentDef { id="sm_rt_mech_god_body", name="sm_race_339", classId=SuperMechTraits.ClassMech,
                desc="sm_race_340",
                intel=15, dmgMul=0.20f, hpMul=0.20f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_void_god_body", name="sm_race_341", classId="sm_race_319",
                desc="sm_race_342",
                intel=18, dmgMul=0.15f, hpMul=0.20f, spdMul=0.20f },
            new RaceTalentDef { id="sm_rt_primary_mech_sense", name="sm_race_343", classId=SuperMechTraits.ClassMech,
                desc="sm_race_344",
                intel=5, dmgMul=0.08f, hpMul=0f, spdMul=0.05f },
        };

        private static bool _registered = false;
        private static readonly System.Random _rng = new System.Random();

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            if (AssetManager.subspecies_trait_groups.get("sm_race_talents") == null)
            {
                var group = new SubspeciesTraitGroupAsset
                {
                    id = "sm_race_talents",
                    name = "trait_group_sm_race_talents",
                    color = "#9932CC"
                };
                AssetManager.subspecies_trait_groups.add(group);
                LocalizedTextManager.add("trait_group_sm_race_talents", LocalizedTextManager.getText("sm_race_345"), pReplace: true);
            }

            AddMarkerTrait(TraitSuperARace, "sm_race_346", "sm_race_347", 0.40f, 0.40f, 15);
            AddMarkerTrait(TraitDivineRace, "sm_race_348", "sm_race_349", 1.00f, 1.00f, 30);

            foreach (var t in TalentPool)
                AddSubspeciesTrait(t.id, t.name, t.desc, t.intel, t.dmgMul, t.hpMul, t.spdMul, t.armor);

            AddSubspeciesTrait(SubspeciesDivineGene, "sm_race_350",
                "sm_race_351", 0, 0.46f, 0f, 0f);
            AddSubspeciesTrait(SubspeciesBornElite, "sm_race_352",
                "sm_race_353", 5, 0.10f, 0.10f, 0f);

            Debug.Log("[超神机械师] 种族系统注册完成：2标记特质 + 5专属种族天赋 + 2神化遗传天赋");
        }

        public static void AutoEvolve(Actor a, int rankIndex)
        {
            if (a == null) return;

            if (rankIndex >= 13)
            {
                if (!a.hasTrait(TraitDivineRace))
                {
                    a.addTrait(TraitDivineRace);
                    string title = GetTitle(a);
                    string divineName = $"sm_race_354";
                    if (a.subspecies != null && a.hasTrait(TraitSuperARace))
                    {
                        SetSubspeciesName(a.subspecies, divineName);
                        AddSubspeciesTraitToSpecies(a.subspecies, SubspeciesDivineGene);
                        AddSubspeciesTraitToSpecies(a.subspecies, SubspeciesBornElite);
                    }
                    else
                    {
                        DetachSubspecies(a, divineName,
                            new[] { SubspeciesDivineGene, SubspeciesBornElite });
                    }
                    Debug.Log($"[超神机械师] {a.name}({title}) 物种神化 → {divineName}");
                }
                return;
            }

            if (rankIndex >= 10 && !a.hasTrait(TraitSuperARace))
            {
                a.addTrait(TraitSuperARace);
                string title = GetTitle(a);
                string raceName = $"sm_race_355";
                var talentIds = new List<string>();
                talentIds.Add(PickClassTalent(a));
                talentIds.Add(PickUniversalTalent());
                talentIds.Add(PickUniversalTalent());
                DetachSubspecies(a, raceName, talentIds.ToArray());
                Debug.Log($"[超神机械师] {a.name} 获得名号【{title}】，物种蜕变 → {raceName}（种族天赋：{string.Join(",", talentIds)}）");
            }
        }

        private static string PickClassTalent(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            var candidates = TalentPool.FindAll(t => t.classId == cls);
            if (candidates.Count == 0) candidates = TalentPool;
            return RollBest(candidates);
        }

        private static string PickUniversalTalent()
        {
            var candidates = TalentPool.FindAll(t => t.classId == "sm_race_319");
            if (candidates.Count == 0) candidates = TalentPool;
            return RollBest(candidates);
        }

        private static string RollBest(List<RaceTalentDef> candidates)
        {
            RaceTalentDef best = null;
            float bestScore = -1;
            for (int i = 0; i < 3; i++)
            {
                var t = candidates[_rng.Next(candidates.Count)];
                float score = t.intel + t.dmgMul * 100 + t.hpMul * 100 + t.spdMul * 100 + t.armor;
                if (score > bestScore) { bestScore = score; best = t; }
            }
            return best != null ? best.id : candidates[0].id;
        }

        private static readonly Dictionary<string, string> _titles = new Dictionary<string, string>();

        public static string GetTitle(Actor a)
        {
            if (a == null) return "";
            string key = a.data.id.ToString();
            if (!_titles.ContainsKey(key))
            {
                string cls = SuperMechBranch.GetClass(a);
                _titles[key] = SuperMechTitleGenerator.Generate(cls);
            }
            return _titles[key];
        }

        private static void AddMarkerTrait(string id, string name, string desc,
            float dmg, float hp, int intel)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconGiant",
                group_id = "sm_race", can_be_removed = false, can_be_given = false,
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intel;
            t.base_stats["multiplier_damage"] = 1f + dmg;
            t.base_stats["multiplier_health"] = 1f + hp;
            t.addCombatAction("combat_dash");
            t.addCombatAction("combat_block");
            t.addCombatAction("combat_dodge");
            if (id == TraitDivineRace)
            {
                t.addCombatAction("combat_backstep");
                t.addCombatAction("combat_instincts");
                t.addCombatAction("combat_deflect_projectile");
                t.addCombatAction("combat_attack_range");
                t.addCombatAction("combat_cast_spell");
            }
            AssetManager.traits.add(t);
        }

        private static void AddSubspeciesTrait(string id, string name, string desc,
            int intel, float dmgMul, float hpMul, float spdMul, float armor = 0f)
        {
            LocalizedTextManager.add("subspecies_trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("subspecies_trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);

            SubspeciesTrait st = AssetManager.subspecies_traits.clone(id, "$adaptation$");
            if (st == null)
            {
                st = new SubspeciesTrait
                {
                    id = id, group_id = "sm_race_talents",
                    path_icon = "ui/Icons/actor_traits/iconGiant",
                    needs_to_be_explored = false, base_stats_meta = new BaseStats()
                };
                AssetManager.subspecies_traits.add(st);
            }
            else
            {
                st.group_id = "sm_race_talents";
                st.path_icon = "ui/Icons/actor_traits/iconGiant";
                st.needs_to_be_explored = false;
                if (st.base_stats_meta == null) st.base_stats_meta = new BaseStats();
            }

            if (intel > 0) st.base_stats_meta["intelligence"] = intel;
            if (dmgMul > 0) st.base_stats_meta["multiplier_damage"] = 1f + dmgMul;
            if (hpMul > 0) st.base_stats_meta["multiplier_health"] = 1f + hpMul;
            if (spdMul > 0) st.base_stats_meta["multiplier_speed"] = 1f + spdMul;
            if (armor > 0) st.base_stats_meta["armor"] = armor;
        }

        private static void AddSubspeciesTraitToSpecies(Subspecies species, string traitId)
        {
            SubspeciesTrait trait = AssetManager.subspecies_traits.get(traitId);
            if (trait != null) species.addTrait(trait);
        }

        private static void DetachSubspecies(Actor actor, string subspeciesName, string[] traitIds)
        {
            try
            {
                Subspecies oldSpecies = actor.subspecies;
                Subspecies newSpecies = World.world.subspecies.newSpecies(actor.asset, actor.current_tile);
                if (newSpecies == null)
                {
                    Debug.LogWarning($"[超神机械师] {actor.name} 创建独立亚种失败");
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

        public static string GetRaceName(Actor a)
        {
            if (a == null) return LocalizedTextManager.getText("sm_race_356");
            if (a.subspecies != null && !string.IsNullOrEmpty(a.subspecies.name))
                return a.subspecies.name;
            string title = GetTitle(a);
            if (a.hasTrait(TraitDivineRace)) return $"sm_race_354";
            if (a.hasTrait(TraitSuperARace)) return $"sm_race_355";
            return LocalizedTextManager.getText("sm_race_356");
        }

        public static void Clear()
        {
            _titles.Clear();
        }
    }
}
