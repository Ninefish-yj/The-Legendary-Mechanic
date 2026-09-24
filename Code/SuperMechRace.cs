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

            // ===== 武道系（对应"神体"，原著ch675不灭之躯/ch268刚韧之躯/蛇辫岩石皮肤/哈达威防御强化）=====
            new RaceTalentDef { id="sm_rt_indestructible", name="不灭之躯", classId=SuperMechTraits.ClassMartial,
                desc="ch675原著韩萧天灾级名号：肉身不灭，恢复力极强。生命+30%，护甲+20。",
                intel=5, dmgMul=0.15f, hpMul=0.30f, spdMul=0f, armor=20 },
            new RaceTalentDef { id="sm_rt_resilient_body", name="刚韧之躯", classId=SuperMechTraits.ClassMartial,
                desc="ch268原著：肉身刚韧，物理伤害-15%，生命+20%。",
                intel=3, dmgMul=0.10f, hpMul=0.20f, spdMul=0f, armor=30 },
            new RaceTalentDef { id="sm_rt_divine_body", name="神体强化", classId=SuperMechTraits.ClassMartial,
                desc="武道系对应'神体'：肉身神化，全属性+15%，战斗中持续恢复。",
                intel=8, dmgMul=0.15f, hpMul=0.20f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_qi_burst", name="气力爆发", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：气力消耗技能威力+40%，气力恢复+20%。",
                intel=5, dmgMul=0.30f, hpMul=0.10f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_rock_skin", name="岩石皮肤", classId=SuperMechTraits.ClassMartial,
                desc="ch329原著蛇辫种族天赋：皮肤岩石化，非常耐打，护甲+40，物理减伤25%。",
                intel=2, dmgMul=0.05f, hpMul=0.25f, spdMul=0f, armor=40 },
            new RaceTalentDef { id="sm_rt_combat_instinct", name="战斗本能", classId=SuperMechTraits.ClassMartial,
                desc="武道系种族天赋：战斗算法优化，近战伤害+30%，攻速+15%，闪避+10%。",
                intel=6, dmgMul=0.30f, hpMul=0f, spdMul=0.15f },

            // ===== 异能系（对应"神通"，原著ch919纯净血脉/基因链核心/西斯科进化方块返祖血脉）=====
            new RaceTalentDef { id="sm_rt_pure_blood", name="纯净血脉", classId=SuperMechTraits.ClassPsi,
                desc="ch919原著：基因链纯净，异能威力+25%。",
                intel=12, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_power", name="神通觉醒", classId=SuperMechTraits.ClassPsi,
                desc="异能系对应'神通'：异能神通觉醒，所有异能技能等级+2，范围+20%。",
                intel=18, dmgMul=0.20f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_gene_overflow", name="基因溢流", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：基因链阶位提升速度+30%，异能冷却-15%。",
                intel=15, dmgMul=0.20f, hpMul=0.05f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_ancestor_blood", name="返祖血脉", classId=SuperMechTraits.ClassPsi,
                desc="ch740原著西斯科用进化方块激活返祖血脉：远古基因觉醒，元素异能伤害+35%，抗性+20%。",
                intel=10, dmgMul=0.35f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_controllable_mutation", name="可控突变", classId=SuperMechTraits.ClassPsi,
                desc="异能系种族天赋：基因突变率+50%，突变方向可控，无负面突变。",
                intel=12, dmgMul=0.20f, hpMul=0.15f, spdMul=0.10f },

            // ===== 魔法系（对应"神权"，原著法神白格尔/魔网/符文体系）=====
            new RaceTalentDef { id="sm_rt_mana_source", name="魔力源泉", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：魔力池浩瀚，魔法威力+25%，魔力+200。",
                intel=12, dmgMul=0.25f, hpMul=0f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_authority", name="神权掌握", classId=SuperMechTraits.ClassMage,
                desc="魔法系对应'神权'：掌控世界规则的一角，施法速度+20%，魔法穿透+15%。",
                intel=18, dmgMul=0.20f, hpMul=0f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_weave_master", name="魔网编织", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：魔网解析度提升，可叠加两个法术模型，复合法术威力+50%。",
                intel=20, dmgMul=0.30f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_rune_decode", name="符文解析", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：符文编码效率+30%，魔法冷却-20%，魔力消耗-15%。",
                intel=15, dmgMul=0.15f, hpMul=0.10f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mana_surge", name="魔力涌动", classId=SuperMechTraits.ClassMage,
                desc="魔法系种族天赋：魔力恢复+50%，战斗中持续回能。",
                intel=10, dmgMul=0.15f, hpMul=0.15f, spdMul=0.10f },

            // ===== 念力系（对应"神魂"，原著克苏耶虚空种族天赋/精神力核心）=====
            new RaceTalentDef { id="sm_rt_spirit_field", name="精神力场", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神力场浩瀚，念力威力+25%，智力+15。",
                intel=15, dmgMul=0.25f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_divine_soul", name="神魂强化", classId=SuperMechTraits.ClassMind,
                desc="念力系对应'神魂'：神魂凝练，精神攻击+30%，心灵抗性+40%。",
                intel=20, dmgMul=0.30f, hpMul=0.10f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_consciousness_control", name="意识操控", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神控制成功率+30%，控制时长+50%，可入侵敌方神经。",
                intel=20, dmgMul=0.20f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mental_burn", name="精神灼烧", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神攻击伤害+40%，可灼烧敌方意识造成持续伤害。",
                intel=18, dmgMul=0.40f, hpMul=0.05f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_reality_warp", name="现实扭曲", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：念力可短暂扭曲物理法则，全属性+15%。",
                intel=22, dmgMul=0.15f, hpMul=0.15f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_consciousness_projection", name="意识投射", classId=SuperMechTraits.ClassMind,
                desc="念力系种族天赋：精神体可脱离肉身行动，感知范围+100%，可远程入侵。",
                intel=16, dmgMul=0.20f, hpMul=0.10f, spdMul=0.20f },

            // ===== 原著虚空进化路线种族天赋（韩萧ch586/ch684/ch770，通用系，任何职业走虚空进化可获得）=====
            new RaceTalentDef { id="sm_rt_void_echo", name="虚空神灵回响", classId="通用",
                desc="ch770原著（混沌使徒）：连接虚空神灵获得神力，万用型——负面/禁锢/防御/攻击四选一，冷却5分钟。",
                intel=15, dmgMul=0.25f, hpMul=0.15f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_supreme_piety", name="至高虔诚", classId="通用",
                desc="ch770原著（虚空传教士）：心灵抗性+90%，免疫迷惑类精神攻击，承受精神攻击20%几率反弹。",
                intel=18, dmgMul=0.10f, hpMul=0.20f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_chaos_release", name="混沌体释放", classId="通用",
                desc="ch586原著（混沌观察者）：化身混沌体，失去实体变灰雾，物理攻击无效，持续消耗气力。",
                intel=12, dmgMul=0.15f, hpMul=0.25f, spdMul=0.20f },
            new RaceTalentDef { id="sm_rt_adaptive_swarm", name="适应性群体", classId="通用",
                desc="ch770原著（宇宙人族）：物理/异常状态/心灵抗性统统+10%，万金油天赋。",
                intel=8, dmgMul=0.10f, hpMul=0.15f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_void_sight", name="虚空视界·观察者", classId="通用",
                desc="ch586原著（混沌观察者）：感知范围大幅提升，可看穿虚空伪装。",
                intel=20, dmgMul=0.10f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_mark_observation", name="观察标记", classId="通用",
                desc="ch586原著（混沌观察者）：标记敌人，被标记目标受到伤害+15%。",
                intel=10, dmgMul=0.15f, hpMul=0f, spdMul=0.05f },
            new RaceTalentDef { id="sm_rt_void_metamorphosis", name="虚空蜕化", classId="通用",
                desc="ch684原著（虚空扭曲者）：半截身体伸进虚空维度，可扭曲空间揉搓敌人。",
                intel=14, dmgMul=0.30f, hpMul=0.10f, spdMul=0.15f },
            new RaceTalentDef { id="sm_rt_void_shatter", name="虚空波纹", classId="通用",
                desc="ch685原著：释放虚空波纹，范围伤害+击退。",
                intel=10, dmgMul=0.25f, hpMul=0f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_warp_void", name="扭曲虚空", classId="通用",
                desc="ch685原著：扭曲周围空间，敌人移动速度-30%，攻击有几率落空。",
                intel=16, dmgMul=0.15f, hpMul=0.20f, spdMul=0f },
            new RaceTalentDef { id="sm_rt_void_blink", name="高等虚空穿梭", classId="通用",
                desc="ch586原著：短距空间跳跃，冷却短，可穿越障碍。",
                intel=12, dmgMul=0.10f, hpMul=0.10f, spdMul=0.30f },

            // ===== 原著其他种族天赋 =====
            new RaceTalentDef { id="sm_rt_resilient_body", name="刚韧之躯", classId=SuperMechTraits.ClassMartial,
                desc="ch268原著：肉身刚韧，受到物理伤害-15%，生命+20%。",
                intel=3, dmgMul=0.10f, hpMul=0.20f, spdMul=0f, armor=30 },
            new RaceTalentDef { id="sm_rt_mech_god_body", name="机械神体", classId=SuperMechTraits.ClassMech,
                desc="ch1401原著（神性蜕变·机械）：肉身机械神化，全属性+20%，可与机械合体。",
                intel=15, dmgMul=0.20f, hpMul=0.20f, spdMul=0.10f },
            new RaceTalentDef { id="sm_rt_void_god_body", name="虚空神体", classId="通用",
                desc="ch1401原著（神性蜕变·虚空）：肉身虚空神化，可在虚空维度自由行动，全属性+15%。",
                intel=18, dmgMul=0.15f, hpMul=0.20f, spdMul=0.20f },
            new RaceTalentDef { id="sm_rt_primary_mech_sense", name="初级械感", classId=SuperMechTraits.ClassMech,
                desc="ch478原著：机械亲和+8%，机械制造速度+10%。",
                intel=5, dmgMul=0.08f, hpMul=0f, spdMul=0.05f },
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
                // 原著ch770有13个种族天赋（10过去+2下一阶段+1自创），这里简化给3个：1职业系+2通用虚空系
                var talentIds = new List<string>();
                talentIds.Add(PickClassTalent(a));       // 职业系专属
                talentIds.Add(PickUniversalTalent());     // 通用虚空系1
                talentIds.Add(PickUniversalTalent());     // 通用虚空系2
                DetachSubspecies(a, raceName, talentIds.ToArray());
                Debug.Log($"[超神机械师] {a.Name} 获得名号【{title}】，物种蜕变 → {raceName}（种族天赋：{string.Join(",", talentIds)}）");
            }
        }

        /// <summary>从职业系天赋池roll（模拟3次更换选最好）。</summary>
        private static string PickClassTalent(Actor a)
        {
            string cls = SuperMechUnitWindow.GetClass(a);
            var candidates = TalentPool.FindAll(t => t.classId == cls);
            if (candidates.Count == 0) candidates = TalentPool;
            return RollBest(candidates);
        }

        /// <summary>从通用虚空系天赋池roll（模拟3次更换选最好）。</summary>
        private static string PickUniversalTalent()
        {
            var candidates = TalentPool.FindAll(t => t.classId == "通用");
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

        /// <summary>清空种族数据（世界切换用）。</summary>
        public static void Clear()
        {
            _titles.Clear();
        }
    }
}
