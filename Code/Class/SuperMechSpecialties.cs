using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 专属专长系统：原著超A级强者各具独特的专属能力
    /// （ch1012 死亡能量专属被动——攻击叠加特殊状态层数；ch1017 专属能力）。
    /// 每个超A级个体晋升后按职业系抽取专属专长（S=1 / SS=2 / X=3 个，个体独有组合），
    /// 显示于单位面板「专属专长」栏；战斗效果挂接攻击伤害乘区。
    /// 运行时数据存 ActorContext.custom，持久化走 ActorSaveData.specialties。
    /// </summary>
    public static class SuperMechSpecialties
    {
        public class SpecialtyDef
        {
            public string id;
            public string system;   // psi/martial/mech/mage/mind/any
            public float dmgMul;    // 攻击伤害加成（0.10 = +10%）
            public float lowHpMul;  // 自身生命<40% 时额外加成（0.25 = 再+25%）
            public bool deathMark;  // 死亡印记：攻击叠层，5 层引爆
            public bool siphon;     // 能量虹吸：击杀回复 30% 气力上限
        }

        public static readonly List<SpecialtyDef> All = new List<SpecialtyDef>
        {
            new SpecialtyDef { id = "sm_spec_death_mark", system = "psi",     dmgMul = 0.10f, deathMark = true },
            new SpecialtyDef { id = "sm_spec_gravity",    system = "psi",     dmgMul = 0.08f },
            new SpecialtyDef { id = "sm_spec_qi_blood",   system = "martial", dmgMul = 0.10f, lowHpMul = 0.25f },
            new SpecialtyDef { id = "sm_spec_overclock",  system = "mech",    dmgMul = 0.15f },
            new SpecialtyDef { id = "sm_spec_elemental",  system = "mage",    dmgMul = 0.15f },
            new SpecialtyDef { id = "sm_spec_mind",       system = "mind",    dmgMul = 0.10f },
            new SpecialtyDef { id = "sm_spec_immortal",   system = "any",     dmgMul = 0.10f },
            new SpecialtyDef { id = "sm_spec_siphon",     system = "any",     dmgMul = 0f,    siphon = true },
        };

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
                        string s = RollSpecialty(a, list);
                        if (s == null) break;
                        list.Add(s);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[超神机械师] 专属专长授予失败: " + e.Message);
                }
            }
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
                if (d == null) continue;
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
            }
            return bonus;
        }

        // === 击杀回气力（能量虹吸，击杀补丁调用） ===
        public static void OnKill(Actor killer)
        {
            if (killer == null || !Has(killer, "sm_spec_siphon")) return;
            float max = SuperMechQi.GetQiMax(killer);
            if (max > 0f) SuperMechQi.AddQi(killer, max * 0.25f);   // 原著ch1009：恢复15%~25%气力
        }

        // === UI 显示名 ===
        public static string GetDisplay(Actor a)
        {
            var list = GetSpecialties(a);
            if (list == null || list.Count == 0) return null;
            var names = new List<string>();
            foreach (string id in list)
            {
                string t = LocalizedTextManager.getText(id);
                names.Add(string.IsNullOrEmpty(t) ? id : t);
            }
            return string.Join("、", names.ToArray());
        }
    }
}
