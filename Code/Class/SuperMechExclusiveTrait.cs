using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 专长系统（对照原著）：
    /// - 模板专长（Template Feat）：NPC/BOSS/职业类别专长，原著第104章起明确出现
    /// - 专属专长（Exclusive Feat）：超A级个人独有，原著第1294章"首个专属专长"，玩家不存在这种能力
    /// 专长注册为真实特质（ActorTrait），战斗效果挂接攻击伤害乘区。
    /// </summary>
    public static class SuperMechExclusiveTrait
    {
        public class SpecialtyDef
        {
            public string id;
            public string nameKey;
            public string descKey;
            public string system;   // psi/martial/mech/mage/mind/any
            public float dmgMul;    // 攻击伤害加成
            public float lowHpMul;  // 低血额外加成
            public float hpMul;     // 生命加成
            public bool deathMark;  // 死亡侵蚀印记
            public float markBurst; // 引爆伤害
            public bool instantKill;// 即死判定
        }

        // === 模板专长（原著：NPC/BOSS/职业类别专长） ===
        // 强韧生命（ch104）、核子力·结构加固（真实伤害免疫）、传奇级战斗技巧（ch1066）、
        // 传奇固定伤害减免（ch1009）、完美械感（ch602 机械系顶尖模板）、
        // 高级生命强韧（低血爆发）、低强度攻击免疫（哈达威硬化防御）、刚韧之躯（韩萧模板）
        public static readonly List<SpecialtyDef> TemplateAll = new List<SpecialtyDef>
        {
            new SpecialtyDef { id = "sm_tpl_perfect_mech",  nameKey = "sm_tpl_perfect_mech",  descKey = "sm_tpl_perfect_mech_info",  system = "mech",    dmgMul = 0.30f },
            new SpecialtyDef { id = "sm_tpl_tough_life",    nameKey = "sm_tpl_tough_life",    descKey = "sm_tpl_tough_life_info",    system = "any",     hpMul = 0.25f },
            new SpecialtyDef { id = "sm_tpl_nuclear",       nameKey = "sm_tpl_nuclear",       descKey = "sm_tpl_nuclear_info",       system = "mech",    dmgMul = 0.15f, hpMul = 0.20f },
            new SpecialtyDef { id = "sm_tpl_combat_skill",  nameKey = "sm_tpl_combat_skill",  descKey = "sm_tpl_combat_skill_info",  system = "martial", dmgMul = 0.20f, lowHpMul = 0.20f },
            new SpecialtyDef { id = "sm_tpl_fixed_reduce",  nameKey = "sm_tpl_fixed_reduce",  descKey = "sm_tpl_fixed_reduce_info",  system = "any",     hpMul = 0.30f },
            new SpecialtyDef { id = "sm_tpl_high_life",     nameKey = "sm_tpl_high_life",     descKey = "sm_tpl_high_life_info",     system = "any",     hpMul = 0.30f, lowHpMul = 0.20f },
            new SpecialtyDef { id = "sm_tpl_low_immune",    nameKey = "sm_tpl_low_immune",    descKey = "sm_tpl_low_immune_info",    system = "any",     hpMul = 0.35f },
            new SpecialtyDef { id = "sm_tpl_tough_body",    nameKey = "sm_tpl_tough_body",    descKey = "sm_tpl_tough_body_info",    system = "any",     hpMul = 0.25f, lowHpMul = 0.15f },
        };

        // === 专属专长（原著：超A级个人独有，玩家不存在这种能力） ===
        // 死亡侵蚀（海拉 ch1012/1030）、亿万械国（麦尼逊 ch1294）、
        // 虚拟技术革新（麦尼逊 ch1294）、诡诈师（索罗金 ch936）
        public static readonly List<SpecialtyDef> LegendaryAll = new List<SpecialtyDef>
        {
            new SpecialtyDef { id = "sm_legend_death_erosion", nameKey = "sm_legend_death_erosion", descKey = "sm_legend_death_erosion_info", system = "psi",
                               dmgMul = 0.10f, deathMark = true, markBurst = 1.2f, instantKill = true },
            new SpecialtyDef { id = "sm_legend_mech_country", nameKey = "sm_legend_mech_country", descKey = "sm_legend_mech_country_info", system = "mech",
                               dmgMul = 0.20f, hpMul = 0.15f },
            new SpecialtyDef { id = "sm_legend_virtual_reform", nameKey = "sm_legend_virtual_reform", descKey = "sm_legend_virtual_reform_info", system = "mech",
                               dmgMul = 0.25f },
            new SpecialtyDef { id = "sm_legend_sorokin_trickster", nameKey = "sm_legend_sorokin_trickster", descKey = "sm_legend_sorokin_trickster_info", system = "psi",
                               dmgMul = 0.35f },
        };

        // 死亡侵蚀常量（原著ch1030：5层印记→15层引爆死神收割）
        public const int DeathMarkGain = 5;
        public const int DeathMarkMax = 15;
        public const float DeathMarkBurst = 1.0f;

        private static bool _markBurstJustHappened = false;

        // === 注册 ===
        public static void Register()
        {
            foreach (var d in TemplateAll)
            {
                LocalizedTextManager.add("trait_" + d.id, LocalizedTextManager.getText(d.nameKey), pReplace: true);
                LocalizedTextManager.add("trait_" + d.id + "_info", LocalizedTextManager.getText(d.descKey), pReplace: true);
                var t = new ActorTrait
                {
                    id = d.id,
                    path_icon = "ui/Icons/actor_traits/iconChosenOne",
                    group_id = "sm_templates",
                    needs_to_be_explored = false,
                    rarity = Rarity.R3_Legendary,
                    base_stats = new BaseStats()
                };
                if (d.hpMul > 0f) t.base_stats["multiplier_health"] = 1f + d.hpMul;
                AssetManager.traits.add(t);
            }
            foreach (var d in LegendaryAll)
            {
                LocalizedTextManager.add("trait_" + d.id, LocalizedTextManager.getText(d.nameKey), pReplace: true);
                LocalizedTextManager.add("trait_" + d.id + "_info", LocalizedTextManager.getText(d.descKey), pReplace: true);
                var t = new ActorTrait
                {
                    id = d.id,
                    path_icon = "ui/Icons/actor_traits/iconChosenOne",
                    group_id = "sm_specialties",
                    needs_to_be_explored = false,
                    rarity = Rarity.R3_Legendary,
                    base_stats = new BaseStats()
                };
                if (d.hpMul > 0f) t.base_stats["multiplier_health"] = 1f + d.hpMul;
                AssetManager.traits.add(t);
            }
        }

        // === 运行时存取 ===
        public static List<string> GetSpecialties(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            return ctx == null ? null : ctx.GetCustom<List<string>>("specialties");
        }

        private static List<string> EnsureList(Actor a)
        {
            var ctx = SuperMechActorContextRegistry.Get(a);
            var list = ctx.GetCustom<List<string>>("specialties");
            if (list == null) { list = new List<string>(); ctx.SetCustom("specialties", list); }
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

        // === 槽位（S=1 / SS=2 / X=3） ===
        public static int SlotCount(int rankIndex)
        {
            if (rankIndex >= 13) return 3;
            if (rankIndex >= 12) return 2;
            if (rankIndex >= 10) return 1;
            return 0;
        }

        // === 授予（超A级自动获得专长，玩家不获得专属专长） ===
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
                        string s = null;
                        bool isPlayer = a.hasTrait(SuperMechTraits.Descendant);
                        if (!isPlayer && list.Count == 0)
                            s = TryLegendaryGrant(a, list);  // 首槽：原著人物专属专长
                        if (s == null)
                            s = TryTemplateFormation(a, list);  // 后续：模板专长
                        if (s == null) break;
                        list.Add(s);
                        if (!a.hasTrait(s)) a.addTrait(s);
                    }
                }
                catch (Exception e) { Debug.LogWarning("[超神机械师] 专长授予失败: " + e.Message); }
            }
        }

        private static string TryTemplateFormation(Actor a, List<string> existing)
        {
            var pool = TemplateAll.FindAll(d => d.system == "any" || d.system == GetSystem(a));
            var cand = pool.FindAll(d => !existing.Contains(d.id));
            if (cand.Count == 0) return null;
            if (UnityEngine.Random.value > 0.2f) return null;
            return cand[UnityEngine.Random.Range(0, cand.Count)].id;
        }

        private static string TryLegendaryGrant(Actor a, List<string> existing)
        {
            if (a.hasTrait(SuperMechTraits.Descendant)) return null;  // 玩家不存在专属专长
            string sys = GetSystem(a);
            var pool = LegendaryAll.FindAll(d => d.system == sys);
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

        // === 即死判定 ===
        public static void TryInstantKill(Actor attacker, Actor target)
        {
            if (!_markBurstJustHappened) return;
            _markBurstJustHappened = false;
            if (attacker == null || target == null || !target.isAlive()) return;
            if (UnityEngine.Random.value < 0.05f) target.data.health = 0;
        }

        // === 战斗乘区 ===
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
                var td = TemplateAll.Find(x => x.id == id);
                if (td != null)
                {
                    bonus += td.dmgMul;
                    if (td.lowHpMul > 0f && lowHp) bonus += td.lowHpMul;
                    continue;
                }
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
                            if (ld.instantKill) _markBurstJustHappened = true;
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

        // === 击杀回调（能量虹吸原为通用专长，已移至技能系统；保留空方法兼容调用） ===
        public static void OnKill(Actor killer) { }
        public static string GetTemplateDisplay(Actor a)
        {
            var list = EnsureList(a);
            var tpl = list.FindAll(id => TemplateAll.Exists(t => t.id == id));
            if (tpl.Count == 0) return null;
            var names = new List<string>();
            foreach (var id in tpl) names.Add(LocalizedTextManager.getText(id));
            return string.Join("、", names.ToArray());
        }

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
