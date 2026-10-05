using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 专属专长系统：原著超A级强者各具独特的专属能力
    /// （ch1012 死亡能量专属被动——攻击叠加特殊状态层数；ch1017 专属能力）。
    /// 每个超A级个体晋升时按职业系【领悟】专属专长（S=1 / SS=2 / X=3 个，个体独有组合，
    /// 原著：专属专长是超A级自创/领悟的个人流派，名字由单位自己取——
    /// ch1009 领悟技能、ch1090 领悟新技能新专长、ch1384 自创念力攻击技能完善自身流派），
    /// 专长注册为真实特质（特质系统，组 sm_specialties），单位面板「专属专长」栏显示个体名；
    /// 战斗效果挂接攻击伤害乘区。运行时数据存 ActorContext.custom，持久化走 ActorSaveData.specialties。
    /// </summary>
    public static class SuperMechSpecialties
    {
        public class SpecialtyDef
        {
            public string id;
            public string nameKey;  // 特质名本地化键
            public string descKey;  // 特质描述本地化键
            public string system;   // psi/martial/mech/mage/mind/any
            public float dmgMul;    // 攻击伤害加成（0.10 = +10%）
            public float lowHpMul;  // 自身生命<40% 时额外加成（0.25 = 再+25%）
            public float hpMul;     // 生命加成（走 trait base_stats 累加，安全）
            public bool deathMark;  // 印记层数引爆（死亡侵蚀体系）
            public float markBurst; // 引爆伤害（0 = 用默认 DeathMarkBurst）
            public bool instantKill;// 满层引爆时进行即死判定（原著：死亡侵蚀满层引爆即死判定）
            public bool siphon;     // 击杀回复 30% 气力上限
            public bool alive = true; // 个人专属=归属原著人物存活（存活才赋予）；模板专长=false（形成获得）
        }

        public static readonly List<SpecialtyDef> All = new List<SpecialtyDef>
        {
            new SpecialtyDef { id = "sm_spec_death_mark", nameKey = "sm_spec_death_mark", descKey = "sm_spec_death_mark_info", system = "psi",     dmgMul = 0.10f, deathMark = true },
            new SpecialtyDef { id = "sm_spec_gravity",    nameKey = "sm_spec_gravity",    descKey = "sm_spec_gravity_info",    system = "psi",     dmgMul = 0.08f },
            new SpecialtyDef { id = "sm_spec_qi_blood",   nameKey = "sm_spec_qi_blood",   descKey = "sm_spec_qi_blood_info",   system = "martial", dmgMul = 0.10f, lowHpMul = 0.25f },
            new SpecialtyDef { id = "sm_spec_overclock",  nameKey = "sm_spec_overclock",  descKey = "sm_spec_overclock_info",  system = "mech",    dmgMul = 0.15f },
            new SpecialtyDef { id = "sm_spec_elemental",  nameKey = "sm_spec_elemental",  descKey = "sm_spec_elemental_info",  system = "mage",    dmgMul = 0.15f },
            new SpecialtyDef { id = "sm_spec_mind",       nameKey = "sm_spec_mind",       descKey = "sm_spec_mind_info",       system = "mind",    dmgMul = 0.10f },
            new SpecialtyDef { id = "sm_spec_immortal",   nameKey = "sm_spec_immortal",   descKey = "sm_spec_immortal_info",   system = "any",     dmgMul = 0.10f },
            new SpecialtyDef { id = "sm_spec_siphon",     nameKey = "sm_spec_siphon",     descKey = "sm_spec_siphon_info",     system = "any",     dmgMul = 0f,    siphon = true },
        };

        // === 原著专属专长（原著明确出现过的能力，由对应原著人物（高维存在）赋予，人物已死则不赋予） ===
        // 归属人物不进入世界、不显示姓名（高维层），仅作赋予源与存活判定（原著核实）：
        // 虚拟创世=韩萧（机械系·ch1402 虚拟创世（伪）·活至终局）、
        // 死神收割=海拉（死亡系异能超A·ch1030 海拉引爆15层印记·活跃 ch1401+）、
        // 冲锋·无尽蓄势=韩萧（ch1009 韩萧领悟·机械系近战冲锋）、
        // 真名解放=红魔·托莱恩（恶魔族超A·ch1010 诞生于亮银旋臂·ch1056 出场）
        public static readonly List<SpecialtyDef> LegendaryAll = new List<SpecialtyDef>
        {
            // === 个人专属专长（原著标明：超A级强者独有，由归属原著人物赋予，人物存活才可赋予） ===
            // 死亡侵蚀=海拉（ch1012：超A级专属被动——死亡能量击中叠加15层，满层引爆即死判定；海拉活跃ch1401+）
            new SpecialtyDef { id = "sm_legend_death_erosion", nameKey = "sm_legend_death_erosion", descKey = "sm_legend_death_erosion_info", system = "psi",
                               dmgMul = 0.10f, deathMark = true, markBurst = 1.2f, instantKill = true, alive = true },
            // === 模板专长（原著标明：NPC/BOSS专属、效果极其强劲、稀有——单位形成获得，非人物赋予） ===
            new SpecialtyDef { id = "sm_legend_tough_life",   nameKey = "sm_legend_tough_life",   descKey = "sm_legend_tough_life_info",   system = "any",     hpMul = 0.25f, alive = false },
            new SpecialtyDef { id = "sm_legend_nuclear",      nameKey = "sm_legend_nuclear",      descKey = "sm_legend_nuclear_info",      system = "mech",    dmgMul = 0.15f, hpMul = 0.20f, alive = false },
            new SpecialtyDef { id = "sm_legend_combat_skill", nameKey = "sm_legend_combat_skill", descKey = "sm_legend_combat_skill_info", system = "martial", dmgMul = 0.20f, lowHpMul = 0.20f, alive = false },
            new SpecialtyDef { id = "sm_legend_fixed_reduce", nameKey = "sm_legend_fixed_reduce", descKey = "sm_legend_fixed_reduce_info", system = "any",     hpMul = 0.30f, alive = false },
        };

        // === 组合自创（原著：专属专长无限自创、名字单位自己取——可生成模组未预设的独特专长） ===
        // 生成规则：按 (actorId, slot) 做确定性种子 → 同一单位重启后生成相同专长（存档可重建）
        private static readonly string[] GenPrefixes = { "死亡", "虚空", "湮灭", "无尽", "命运", "星海", "永夜", "苍穹", "深渊", "神域", "混沌", "天启", "寂灭", "轮回" };
        private static readonly string[] GenCores = { "收割", "印记", "领域", "共鸣", "侵蚀", "汲取", "爆发", "超频", "穿透", "压制", "觉醒", "重铸", "凝视", "吞噬" };
        private static readonly string[] GenSuffixes = { "", "之域", "之力", "之瞳", "之门", "之环" };

        public class GenSpec
        {
            public float dmg;
            public bool mark; public int markMax; public float burst;
            public bool lowHp; public float lowHpBonus;
            public bool siphon;
        }

        private static readonly Dictionary<string, GenSpec> _genParams = new Dictionary<string, GenSpec>();
        private static bool _markBurstJustHappened = false;   // 死亡侵蚀满层引爆标记（用于即死判定）

        /// <summary>死亡侵蚀满层引爆后的即死判定（原著：引爆后立刻进行一次即死判定；模组：5% 概率处决）</summary>
        public static void TryInstantKill(Actor attacker, Actor target)
        {
            if (!_markBurstJustHappened) return;
            _markBurstJustHappened = false;
            if (attacker == null || target == null || !target.isAlive()) return;
            if (UnityEngine.Random.value < 0.05f) target.data.health = 0;  // 即死判定：5% 概率直接处决
        }

        /// <summary>为超A级单位按槽位组合自创一个独特专属专长（模组未预设）</summary>
        public static string GenerateSpecialty(long actorId, int slot)
        {
            var rng = new System.Random(unchecked((int)(actorId * 7 + slot * 13 + 0x5EED)));
            string name = GenPrefixes[rng.Next(GenPrefixes.Length)]
                        + GenCores[rng.Next(GenCores.Length)]
                        + GenSuffixes[rng.Next(GenSuffixes.Length)];
            if (name.Length < 3) name = GenPrefixes[rng.Next(GenPrefixes.Length)] + name;

            float dmg = 0.08f + (float)rng.NextDouble() * 0.17f;   // +8%~25%
            bool mark = rng.Next(100) < 45;
            bool lowHp = !mark && rng.Next(100) < 40;
            bool siphon = !mark && !lowHp && rng.Next(100) < 40;
            int markMax = 5 + rng.Next(6);                          // 5~10 层
            float burst = 0.3f + (float)rng.NextDouble() * 0.7f;    // 引爆 +30%~100%
            float lowHpBonus = 0.2f + (float)rng.NextDouble() * 0.2f;

            string id = "sm_spec_gen_" + actorId + "_" + slot;
            RegisterGenerated(id, name, dmg, mark, markMax, burst, lowHp, lowHpBonus, siphon);
            return id;
        }

        /// <summary>动态注册组合专长为真实特质（重名/重复注册幂等）</summary>
        private static void RegisterGenerated(string id, string name, float dmg, bool mark,
            int markMax, float burst, bool lowHp, float lowHpBonus, bool siphon)
        {
            if (AssetManager.traits.has(id)) { _genParams[id] = new GenSpec { dmg = dmg, mark = mark, markMax = markMax, burst = burst, lowHp = lowHp, lowHpBonus = lowHpBonus, siphon = siphon }; return; }
            LocalizedTextManager.add(id, name, pReplace: true);
            LocalizedTextManager.add(id + "_info", name + "（领悟自创的专属专长，效果与名字为该单位独有）", pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconChosenOne",
                group_id = "sm_specialties", needs_to_be_explored = false,
                rarity = (mark || dmg >= 0.18f) ? Rarity.R3_Legendary : Rarity.R2_Epic,
                base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);
            _genParams[id] = new GenSpec { dmg = dmg, mark = mark, markMax = markMax, burst = burst, lowHp = lowHp, lowHpBonus = lowHpBonus, siphon = siphon };
        }

        /// <summary>存档恢复时重建组合专长（同单位同槽位 → 同种子 → 同效果）</summary>
        public static void RebuildGenerated(string id)
        {
            string[] parts = id.Split('_');
            if (parts.Length < 4 || !id.StartsWith("sm_spec_gen_")) return;
            if (long.TryParse(parts[3], out long actorId) && int.TryParse(parts[4], out int slot))
            {
                var rng = new System.Random(unchecked((int)(actorId * 7 + slot * 13 + 0x5EED)));
                string name = GenPrefixes[rng.Next(GenPrefixes.Length)] + GenCores[rng.Next(GenCores.Length)] + GenSuffixes[rng.Next(GenSuffixes.Length)];
                float dmg = 0.08f + (float)rng.NextDouble() * 0.17f;
                bool mark = rng.Next(100) < 45;
                bool lowHp = !mark && rng.Next(100) < 40;
                bool siphon = !mark && !lowHp && rng.Next(100) < 40;
                int markMax = 5 + rng.Next(6);
                float burst = 0.3f + (float)rng.NextDouble() * 0.7f;
                float lowHpBonus = 0.2f + (float)rng.NextDouble() * 0.2f;
                RegisterGenerated(id, name, dmg, mark, markMax, burst, lowHp, lowHpBonus, siphon);
            }
        }

        // === 注册（特质系统：专属专长=真实特质，有名字/图标/稀有度/描述） ===
        public static void Register()
        {
            foreach (var d in All)
            {
                LocalizedTextManager.add(d.id, LocalizedTextManager.getText(d.nameKey), pReplace: true);
                LocalizedTextManager.add(d.id + "_info", LocalizedTextManager.getText(d.descKey), pReplace: true);
                var t = new ActorTrait
                {
                    id = d.id,
                    path_icon = "ui/Icons/actor_traits/iconGenius",
                    group_id = "sm_specialties",
                    needs_to_be_explored = false,
                    rarity = (d.id == "sm_spec_death_mark" || d.id == "sm_spec_siphon")
                        ? Rarity.R3_Legendary : Rarity.R2_Epic,
                    base_stats = new BaseStats()
                };
                AssetManager.traits.add(t);
            }
            // 原著专属专长：由高维原著人物赋予，均为传奇稀有度
            foreach (var d in LegendaryAll)
            {
                LocalizedTextManager.add(d.id, LocalizedTextManager.getText(d.nameKey), pReplace: true);
                LocalizedTextManager.add(d.id + "_info", LocalizedTextManager.getText(d.descKey), pReplace: true);
                var t = new ActorTrait
                {
                    id = d.id,
                    path_icon = "ui/Icons/actor_traits/iconChosenOne",
                    group_id = "sm_specialties",
                    needs_to_be_explored = false,
                    rarity = Rarity.R3_Legendary,
                    base_stats = new BaseStats()
                };
                if (d.hpMul > 0f) t.base_stats["multiplier_health"] = 1f + d.hpMul;  // 模板专长生命加成（trait 层累加）
                AssetManager.traits.add(t);
            }
        }

        // 原著机制（ch1030）：印记赋予目标 5 层【死亡侵蚀】，引爆 15 层【死神收割】
        public const int DeathMarkGain = 5;        // 每次攻击赋予的层数（原著：5 层死亡侵蚀）
        public const int DeathMarkMax = 15;        // 层数上限（原著：15 层引爆）
        public const float DeathMarkBurst = 1.0f;  // 引爆伤害（死神收割）

        // === 运行时存取（ActorContext.custom） ===
        public static List<string> GetSpecialties(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            return ctx == null ? null : ctx.GetCustom<List<string>>("specialties");
        }

        private static List<string> EnsureList(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            var list = ctx.GetCustom<List<string>>("specialties");
            if (list == null)
            {
                list = new List<string>();
                ctx.SetCustom("specialties", list);
            }
            return list;
        }

        public static int GetStacks(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            return ctx == null ? 0 : ctx.GetCustom<int>("death_mark_stacks", 0);
        }

        public static void ClearStacks(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.SetCustom("death_mark_stacks", 0);
        }

        // === 槽位（按阶位：S=1 / SS=2 / X=3） ===
        public static int SlotCount(int rankIndex)
        {
            if (rankIndex >= 13) return 3;
            if (rankIndex >= 12) return 2;
            if (rankIndex >= 10) return 1;
            return 0;
        }

        // === 授予（每 tick 补齐，只处理超A级） ===
        public static void TickGrant()
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                try
                {
                    if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                    int slots = SlotCount(SuperMechAdvancement.GetExactRankIndex(a));
                    if (slots <= 0) continue;
                    var list = EnsureList(a);
                    int guard = 0;
                    while (list.Count < slots && guard++ < 8)
                    {
                        // 原著：专属专长由单位领悟/自创，或由高维原著人物赋予（人物存活才赋予）
                        string s = null;
                        int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                        if (rankIdx >= 13 && !a.hasTrait(SuperMechTraits.Descendant))
                        {
                            // 原著：模板专长=NPC/BOSS专属（玩家弄不到，ch107）；个人专属=超A级强者独有（前世玩家不存在这种能力，ch1093）
                            if (list.Count == 0) s = TryLegendaryGrant(a, list);      // 首槽：原著人物（存活）赋予个人专属
                            else s = TryTemplateFormation(a, list);                       // 后续槽：概率形成模板专长
                        }
                        if (s == null)
                        {
                            // 一半概率组合自创（模组未预设的独特专长），否则从预设池领悟
                            if (UnityEngine.Random.value < 0.5f)
                            {
                                s = GenerateSpecialty(a.data.id, list.Count);
                                if (list.Contains(s)) { s = RollSpecialty(a, list); if (s == null) break; }
                            }
                            else
                            {
                                s = RollSpecialty(a, list);
                                if (s == null) break;
                            }
                        }
                        list.Add(s);
                        if (!a.hasTrait(s)) a.addTrait(s);   // 专属专长=真实特质，挂到单位特质系统
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[超神机械师] 专属专长授予失败: " + e.Message);
                }
            }
        }

        /// <summary>原著赋予：X阶超A匹配职业系的原著人物（存活）赋予其专属专长</summary>
        /// <summary>原著赋予：X阶超A匹配职业系时，由存活的原著人物赋予个人专属专长</summary>
        private static string TryLegendaryGrant(Actor a, List<string> existing)
        {
            if (a.hasTrait(SuperMechTraits.Descendant)) return null;   // 原著：玩家不存在专属专长（ch1093）
            string sys = GetSystem(a);
            var pool = LegendaryAll.FindAll(d => d.alive && d.system == sys);
            if (pool.Count == 0) return null;
            var cand = pool.FindAll(d => !existing.Contains(d.id));
            if (cand.Count == 0) return null;
            return cand[UnityEngine.Random.Range(0, cand.Count)].id;
        }

        /// <summary>模板专长形成：X阶超A（NPC/BOSS 定位）按形成几率获得模板专长（原著：专属专长形成几率）</summary>
        private static string TryTemplateFormation(Actor a, List<string> existing)
        {
            if (a.hasTrait(SuperMechTraits.Descendant)) return null;   // 原著：玩家弄不到模板专长（ch107）
            var pool = LegendaryAll.FindAll(d => !d.alive);
            if (pool.Count == 0) return null;
            var cand = pool.FindAll(d => !existing.Contains(d.id));
            if (cand.Count == 0) return null;
            // 形成几率 20%（原著：专属专长形成几率——模组固定低概率）
            if (UnityEngine.Random.value > 0.2f) return null;
            return cand[UnityEngine.Random.Range(0, cand.Count)].id;
        }

        private static string RollSpecialty(Actor a, List<string> existing)
        {
            string sys = GetSystem(a);
            var pool = All.FindAll(d => d.system == sys);
            if (pool.Count == 0) pool = All.FindAll(d => d.system == "any");
            if (pool.Count == 0) pool = All;
            var cand = pool.FindAll(d => !existing.Contains(d.id));
            if (cand.Count == 0) return null;
            return cand[UnityEngine.Random.Range(0, cand.Count)].id;
        }

        private static string GetSystem(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "mage";
            if (a.hasTrait(SuperMechTraits.ClassMech)) return "mech";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "mind";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "martial";
            return "psi";
        }

        public static bool Has(Actor a, string id)
        {
            var list = GetSpecialties(a);
            return list != null && list.Contains(id);
        }

        // === 战斗乘区（攻击侧，Actor_GetHit_Prefix 调用；返回伤害加成系数） ===
        public static float GetAttackBonus(Actor a)
        {
            if (a == null) return 0f;
            var list = GetSpecialties(a);
            if (list == null || list.Count == 0) return 0f;

            float bonus = 0f;
            bool lowHp = a.data != null && a.getMaxHealth() > 0f &&
                         a.data.health <= a.getMaxHealth() * 0.4f;
            foreach (string id in list)
            {
                var d = All.Find(x => x.id == id);
                if (d != null)
                {
                    bonus += d.dmgMul;
                    if (d.lowHpMul > 0f && lowHp) bonus += d.lowHpMul;
                    if (d.deathMark)
                    {
                        int stacks = GetStacks(a);
                        if (stacks + DeathMarkGain >= DeathMarkMax)
                        {
                            ClearStacks(a);
                            bonus += DeathMarkBurst;   // 引爆【死神收割】
                        }
                        else
                        {
                            var ctx = SuperMechActorContextRegistry.Get(a);
                            if (ctx != null) ctx.SetCustom("death_mark_stacks", stacks + DeathMarkGain);
                        }
                    }
                    continue;
                }
                // 原著专属专长（高维原著人物赋予）
                var ld = LegendaryAll.Find(x => x.id == id);
                if (ld != null)
                {
                    bonus += ld.dmgMul;
                    if (ld.lowHpMul > 0f && lowHp) bonus += ld.lowHpMul;
                    if (ld.deathMark)
                    {
                        int stacks = GetStacks(a);
                        float burst = ld.markBurst > 0f ? ld.markBurst : DeathMarkBurst;
                        if (stacks + DeathMarkGain >= DeathMarkMax)
                        {
                            ClearStacks(a);
                            bonus += burst;
                            if (ld.instantKill) _markBurstJustHappened = true;  // 死亡侵蚀满层引爆→即死判定
                        }
                        else
                        {
                            var ctx = SuperMechActorContextRegistry.Get(a);
                            if (ctx != null) ctx.SetCustom("death_mark_stacks", stacks + DeathMarkGain);
                        }
                    }
                    continue;
                }
                // 组合自创专长（模组未预设，效果参数存 _genParams）
                if (_genParams.TryGetValue(id, out var g))
                {
                    bonus += g.dmg;
                    if (g.lowHp && lowHp) bonus += g.lowHpBonus;
                    if (g.mark)
                    {
                        int stacks = GetStacks(a);
                        if (stacks + DeathMarkGain >= g.markMax)
                        {
                            ClearStacks(a);
                            bonus += g.burst;
                        }
                        else
                        {
                            var ctx = SuperMechActorContextRegistry.Get(a);
                            if (ctx != null) ctx.SetCustom("death_mark_stacks", stacks + DeathMarkGain);
                        }
                    }
                }
            }
            return bonus;
        }

        // === 击杀回气力（能量虹吸，击杀补丁调用） ===
        public static void OnKill(Actor killer)
        {
            if (killer == null) return;
            bool siphon = Has(killer, "sm_spec_siphon");
            if (!siphon)
            {
                var list = GetSpecialties(killer);
                if (list != null)
                    foreach (string id in list)
                        if (_genParams.TryGetValue(id, out var g) && g.siphon) { siphon = true; break; }
            }
            if (!siphon) return;
            float max = SuperMechQi.GetQiMax(killer);
            if (max > 0f) SuperMechQi.AddQi(killer, max * 0.25f);   // 原著ch1009：恢复15%~25%气力
        }

        // === UI 显示名（原著：专属专长的名字是单位自己取的——显示 {单位名}·{专长名}） ===
        public static string GetDisplay(Actor a)
        {
            var list = GetSpecialties(a);
            if (list == null || list.Count == 0) return null;
            string unitName = a.data != null ? a.data.name : null;
            var names = new List<string>();
            foreach (string id in list)
            {
                string t = LocalizedTextManager.getText(id);
                string baseName = string.IsNullOrEmpty(t) ? id : t;
                names.Add(string.IsNullOrEmpty(unitName) ? baseName : unitName + "·" + baseName);
            }
            return string.Join("、", names.ToArray());
        }
    }
}
