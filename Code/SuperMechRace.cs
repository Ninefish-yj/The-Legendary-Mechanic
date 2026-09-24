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

        // S阶专属种族天赋池（按职业系，模拟原著ch770"捏天赋"随机roll）
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
            // ===== 机械系（原著【机械天才】为首，其余为同倾向自创）=====
            new RaceTalentDef { id="sm_rt_mech_genius", name="机械天才", classId=SuperMechTraits.ClassMech,
                desc="ch770原著：机械总亲和1.25x，机械造物性能+40%，机械系技能等级+1。",
                intel=10, dmgMul=0.25f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mech_sense", name="天生械感", classId=SuperMechTraits.ClassMech,
                desc="ch770原著roll项：机械亲和+180%，所有机械类技能LV+2。",
                intel=8, dmgMul=0.20f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_mech_source", name="械力之源", classId=SuperMechTraits.ClassMech,
                desc="ch770原著roll项：气力属性为械力时，每级额外获得50点气力值。",
                intel=5, dmgMul=0.15f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mech_overload", name="过载核心", classId=SuperMechTraits.ClassMech,
                desc="机械系种族天赋：短时过载输出，伤害+30%，但消耗额外气力。",
                intel=8, dmgMul=0.30f, hpMul=0f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_mech_swarm", name="机群意志", classId=SuperMechTraits.ClassMech,
                desc="机械系种族天赋：操控机械军团时，每台机械额外+10%属性。",
                intel=12, dmgMul=0.20f, hpMul=0.15f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mech_forge", name="造物者之心", classId=SuperMechTraits.ClassMech,
                desc="机械系种族天赋：制造完美度+20%，冷却-15%。",
                intel=15, dmgMul=0.15f, hpMul=0.10f, spdMul=0f },

            // ===== 武道系 =====
            new RaceTalentDef { id="sm_rt_indestructible", name="不灭之躯", classId=SuperMechTraits.ClassMartial,
                desc="ch675原著韩萧天灾级名号：肉身不灭，恢复力极强。生命+30%，护甲+20。",
                intel=5, dmgMul=0.15f, hpMul=0.30f, spdMul=0f, armor=20 },
            new RaceTalentDef { id="sm_rt_battle_body", name="战神之躯", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：战斗中全属性随时间递增，最高+25%。",
                intel=8, dmgMul=0.25f, hpMul=0.20f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_qi_burst", name="气力爆发", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：气力消耗技能威力+40%，气力恢复+20%。",
                intel=5, dmgMul=0.30f, hpMul=0.10f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_iron_body", name="金刚不坏", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：护甲+50，受到物理伤害-20%。",
                intel=3, dmgMul=0.10f, hpMul=0.25f, spdMul=0f, armor=50 },
            new RaceTalentDef { id="sm_rt_fist_god", name="拳神", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：近战伤害+40%，攻击速度+15%。",
                intel=5, dmgMul=0.40f, hpMul=0f, spdMul=0.15f },

            // ===== 异能系 =====
            new RaceTalentDef { id="sm_rt_pure_blood", name="纯净血脉", classId=SuperMechTraits.ClassPsi,
                desc="ch919原著：基因链纯净，异能威力+25%。",
                intel=12, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_gene_overflow", name="基因溢流", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：基因链阶位提升速度+30%，异能冷却-15%。",
                intel=15, dmgMul=0.20f, hpMul=0.05f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_ability_master", name="异能精通", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：所有异能技能等级+2，异能范围+20%。",
                intel=18, dmgMul=0.25f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_element_affinity", name="元素亲和", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：元素类异能伤害+35%，元素抗性+20%。",
                intel=10, dmgMul=0.35f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mutation_king", name="突变王者", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：基因突变率+50%，突变方向可控。",
                intel=12, dmgMul=0.20f, hpMul=0.15f, spdMul=0.10f },

            // ===== 魔法系 =====
            new RaceTalentDef { id="sm_rt_mana_source", name="魔力源泉", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：魔力池浩瀚，魔法威力+25%，魔力+200。",
                intel=12, dmgMul=0.25f, hpMul=0f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_arcane_master", name="奥术精通", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：所有魔法技能等级+2，施法速度+20%。",
                intel=18, dmgMul=0.20f, hpMul=0f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_element_lord", name="元素领主", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：元素魔法伤害+35%，可同时操控两种元素。",
                intel=15, dmgMul=0.35f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mana_regen", name="魔力涌动", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：魔力恢复+50%，战斗中持续回蓝。",
                intel=10, dmgMul=0.15f, hpMul=0.15f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_spell_weave", name="法术编织", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：可叠加两个法术，复合法术威力+50%。",
                intel=20, dmgMul=0.30f, hpMul=0f, spdMul=0.05f },

            // ===== 念力系 =====
            new RaceTalentDef { id="sm_rt_spirit_ocean", name="精神海洋", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神力浩瀚，念力威力+25%，智力+15。",
                intel=15, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_mind_domination", name="心灵支配", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神控制成功率+30%，控制时长+50%。",
                intel=20, dmgMul=0.20f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_soul_fire", name="灵魂之火", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：灵魂攻击伤害+40%，可灼烧敌方精神。",
                intel=18, dmgMul=0.40f, hpMul=0.05f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_reality_warp", name="现实扭曲", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：念力可短暂扭曲物理法则，全属性+15%。",
                intel=22, dmgMul=0.15f, hpMul=0.15f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_astral_projection", name="星界投射", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神体可脱离肉身行动，范围+100%。",
                intel=16, dmgMul=0.20f, hpMul=0.10f, spdMul=0.20f },
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
                AddSubspeciesTrait(t.id, t.name, t.desc, t.intel, t.dmgMul, t.hpMul, t.spdMul, t.armor);

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
                    string title = GetTitle(a);
                    string divineName = $"{title}神系·王族血脉";
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
                    Debug.Log($"[超神机械师] {a.Name}({title}) 物种神化 → {divineName}");
                }
                return;
            }

            // S阶：物种蜕变，创建以名号命名的独立亚种
            if (rankIndex >= 10 && !a.hasTrait(TraitSuperARace))
            {
                a.addTrait(TraitSuperARace);
                string title = GetTitle(a);
                string raceName = $"{title}族";
                string talentId = PickRaceTalent(a);
                DetachSubspecies(a, raceName, new[] { talentId });
                Debug.Log($"[超神机械师] {a.Name} 获得名号【{title}】，物种蜕变 → {raceName}（专属天赋：{talentId}）");
            }
        }

        /// <summary>按职业系从天赋池随机roll专属种族天赋（模拟原著ch770"3次更换机会选最好"）。</summary>
        private static string PickRaceTalent(Actor a)
        {
            string cls = SuperMechUnitWindow.GetClass(a);
            var candidates = TalentPool.FindAll(t => t.classId == cls);
            if (candidates.Count == 0) candidates = TalentPool;
            // 模拟3次roll取总属性最高的
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

        /// <summary>名号字典：actorID → 名号（S阶物种蜕变时生成）。</summary>
        private static readonly Dictionary<string, string> _titles = new Dictionary<string, string>();

        /// <summary>获取或生成单位的超A名号。</summary>
        public static string GetTitle(Actor a)
        {
            if (a == null) return "";
            string key = a.data?.id ?? a.GetInstanceID().ToString();
            if (!_titles.ContainsKey(key))
            {
                string cls = SuperMechUnitWindow.GetClass(a);
                _titles[key] = SuperMechTitleGenerator.Generate(cls);
            }
            return _titles[key];
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
            int intel, float dmgMul, float hpMul, float spdMul, float armor = 0f)
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
            if (armor > 0) st.base_stats_meta["armor"] = armor;
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
            string title = GetTitle(a);
            if (a.hasTrait(TraitDivineRace)) return $"{title}神系·王族血脉";
            if (a.hasTrait(TraitSuperARace)) return $"{title}族";
            return "碳基人类（黄）";
        }
    }
}
