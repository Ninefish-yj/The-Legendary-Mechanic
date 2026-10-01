using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechKnowledgeSynergy
    {
        public class SynergyDef
        {
            public string id;
            public string name;
            public string desc;
            public string prefix;
            public string[] requiredKnowledge;
            public float dmgMul;
            public float hpMul;
            public float speedMul;
            public float qiBonus;
            public int potentialBonus;
        }

        public static readonly List<SynergyDef> _synergies = new List<SynergyDef>();

        public static readonly Dictionary<long, HashSet<string>> _active = new Dictionary<long, HashSet<string>>();

        public static readonly Dictionary<long, SynergyBonus> _bonusCache = new Dictionary<long, SynergyBonus>();

        public class SynergyBonus
        {
            public float dmgMul = 1f;
            public float hpMul = 1f;
            public float speedMul = 1f;
            public float qiBonus = 0f;
            public int potentialBonus = 0;
        }

        public static void Register()
        {
            Add("syn_mech_assembly", "sm_knowledgesynergy_797", "sm_knowledgesynergy_798", "mech",
                new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                dmgMul: 1.1f, speedMul: 1.1f);

            Add("syn_mech_energy", "sm_knowledgesynergy_799", "sm_knowledgesynergy_800", "mech",
                new[] { "sm_know_mech_1_1_0", "sm_know_mech_1_1_1" },
                hpMul: 1.15f, qiBonus: 500f);

            Add("syn_mech_virtual", "sm_knowledgesynergy_801", "sm_knowledgesynergy_802", "mech",
                new[] { "sm_know_mech_2_2_0", "sm_know_mech_2_2_1" },
                dmgMul: 1.2f, speedMul: 1.15f);

            Add("syn_mech_god", "sm_knowledgesynergy_803", "sm_knowledgesynergy_804", "mech",
                new[] { "sm_know_mech_3_1_0", "sm_know_mech_4_1_0" },
                dmgMul: 1.5f, hpMul: 1.3f, potentialBonus: 5);

            Add("syn_martial_body", "sm_knowledgesynergy_805", "sm_knowledgesynergy_806", "martial",
                new[] { "sm_know_martial_0_1_0", "sm_know_martial_0_1_1" },
                hpMul: 1.15f, dmgMul: 1.05f);

            Add("syn_martial_speed", "sm_knowledgesynergy_807", "sm_knowledgesynergy_808", "martial",
                new[] { "sm_know_martial_0_0_0", "sm_know_martial_1_0_0" },
                speedMul: 1.2f);

            Add("syn_martial_qi", "sm_knowledgesynergy_809", "sm_knowledgesynergy_810", "martial",
                new[] { "sm_know_martial_1_2_0", "sm_know_martial_2_2_0" },
                dmgMul: 1.15f, qiBonus: 800f);

            Add("syn_psi_gene", "sm_knowledgesynergy_811", "sm_knowledgesynergy_812", "psi",
                new[] { "sm_know_psi_0_0_0", "sm_know_psi_1_0_0" },
                dmgMul: 1.15f);

            Add("syn_psi_control", "sm_knowledgesynergy_813", "sm_knowledgesynergy_814", "psi",
                new[] { "sm_know_psi_1_1_0", "sm_know_psi_2_1_0" },
                dmgMul: 1.1f, speedMul: 1.1f);

            Add("syn_mage_element", "sm_knowledgesynergy_815", "sm_knowledgesynergy_816", "mage",
                new[] { "sm_know_mage_0_2_0", "sm_know_mage_1_2_0" },
                dmgMul: 1.2f);

            Add("syn_mage_arcane", "sm_knowledgesynergy_817", "sm_knowledgesynergy_818", "mage",
                new[] { "sm_know_mage_1_0_0", "sm_know_mage_2_0_0" },
                hpMul: 1.1f, qiBonus: 600f);

            Add("syn_mind_soul", "sm_knowledgesynergy_819", "sm_knowledgesynergy_820", "mind",
                new[] { "sm_know_mind_0_0_0", "sm_know_mind_1_0_0" },
                dmgMul: 1.15f, speedMul: 1.05f);

            Add("syn_mind_reality", "sm_knowledgesynergy_821", "sm_knowledgesynergy_822", "mind",
                new[] { "sm_know_mind_2_1_0", "sm_know_mind_3_1_0" },
                dmgMul: 1.3f, hpMul: 1.2f, potentialBonus: 3);

            Debug.Log($"[超神机械师] 知识协同效应注册完成：{_synergies.Count}个协同组合");
        }

        private static void Add(string id, string name, string desc, string prefix,
            string[] required, float dmgMul = 1f, float hpMul = 1f,
            float speedMul = 1f, float qiBonus = 0f, int potentialBonus = 0)
        {
            _synergies.Add(new SynergyDef
            {
                id = id, name = name, desc = desc, prefix = prefix,
                requiredKnowledge = required,
                dmgMul = dmgMul, hpMul = hpMul, speedMul = speedMul,
                qiBonus = qiBonus, potentialBonus = potentialBonus
            });
        }

        public static void TickSynergy()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechBranch.GetClass(a));
                var unlocked = SuperMechKnowledge.GetUnlockedList(a, prefix);
                var unlockedIds = new HashSet<string>();
                foreach (var def in unlocked) unlockedIds.Add(def.id);

                if (!_active.TryGetValue(a.id, out var activeSet))
                {
                    activeSet = new HashSet<string>();
                    _active[a.id] = activeSet;
                }
                activeSet.Clear();

                var bonus = new SynergyBonus();
                foreach (var syn in _synergies)
                {
                    if (syn.prefix != prefix) continue;
                    bool allMet = true;
                    foreach (string req in syn.requiredKnowledge)
                    {
                        if (!unlockedIds.Contains(req)) { allMet = false; break; }
                    }
                    if (allMet)
                    {
                        activeSet.Add(syn.id);
                        bonus.dmgMul *= syn.dmgMul;
                        bonus.hpMul *= syn.hpMul;
                        bonus.speedMul *= syn.speedMul;
                        bonus.qiBonus += syn.qiBonus;
                        bonus.potentialBonus += syn.potentialBonus;
                    }
                }
                _bonusCache[a.id] = bonus;
            }
        }

        public static List<SynergyDef> GetActiveSynergies(Actor a)
        {
            var list = new List<SynergyDef>();
            if (a == null || !_active.TryGetValue(a.id, out var set)) return list;
            foreach (var syn in _synergies)
            {
                if (set.Contains(syn.id)) list.Add(syn);
            }
            return list;
        }

        public static SynergyBonus GetBonus(Actor a)
        {
            if (a != null && _bonusCache.TryGetValue(a.id, out var bonus)) return bonus;
            return new SynergyBonus();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_active, alive);
            removed += SuperMechCleanup.CleanDict(_bonusCache, alive);
            return removed;
        }

        public static void Clear() { _active.Clear(); _bonusCache.Clear(); }
    }
}
