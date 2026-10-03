using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechSkills
    {
        public class SkillDef
        {
            public string id;
            public string name;
            public string desc;
            public string icon;
            public string requiredClass;
            public int requiredStage;
            public string requiredKnowledge;
            public int intelligence;
            public float dmgMul;
            public float hpMul;
            public float speedMul;
            // v0.44.0 技能释放机制
            public SkillType type = SkillType.Passive;
            public float cooldown = 0f;
            public float qiCost = 0f;
            public SkillEffectType effectType = SkillEffectType.None;
            public float effectValue = 0f;
            public float effectDuration = 0f;
        }

        public enum SkillType { Passive, Active }
        public enum SkillEffectType { None, Damage, Buff, Debuff, Summon, Heal, Teleport }

        public static readonly List<SkillDef> AllSkills = new List<SkillDef>
        {
            new SkillDef {
                id = "sm_skill_qimod", name = "sm_skills_986",
                desc = "sm_skills_987",
                icon = "ui/Icons/actor_traits/iconBattleReflexes",
                requiredClass = "sm_skills_1365", requiredStage = 3, requiredKnowledge = "sm_know_mech_1_0_0",
                intelligence = 5, dmgMul = 1.1f, hpMul = 1f, speedMul = 1f,
                type = SkillType.Active, cooldown = 15f, qiCost = 50f,
                effectType = SkillEffectType.Damage, effectValue = 2f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_virtual_purify", name = "sm_skills_988",
                desc = "sm_skills_989",
                icon = "ui/Icons/actor_traits/iconChosenOne",
                requiredClass = "sm_skills_1365", requiredStage = 6, requiredKnowledge = "sm_know_mech_2_0_0",
                intelligence = 3, dmgMul = 1.05f, hpMul = 1.05f, speedMul = 1f,
                type = SkillType.Active, cooldown = 20f, qiCost = 80f,
                effectType = SkillEffectType.Debuff, effectValue = 0.5f, effectDuration = 5f
            },
            new SkillDef {
                id = "sm_skill_dimension_march", name = "sm_skills_990",
                desc = "sm_skills_991",
                icon = "ui/Icons/actor_traits/iconBlessing",
                requiredClass = "sm_skills_1365", requiredStage = 7, requiredKnowledge = "sm_know_mech_3_0_0",
                intelligence = 8, dmgMul = 1.2f, hpMul = 1.1f, speedMul = 1.1f,
                type = SkillType.Active, cooldown = 25f, qiCost = 100f,
                effectType = SkillEffectType.Buff, effectValue = 1.5f, effectDuration = 8f
            },
            new SkillDef {
                id = "sm_skill_qi_burst", name = "sm_skills_992",
                desc = "sm_skills_993",
                icon = "ui/Icons/actor_traits/iconRage",
                requiredClass = "sm_skills_1366", requiredStage = 4, requiredKnowledge = "sm_know_martial_1_2_0",
                intelligence = 0, dmgMul = 1.3f, hpMul = 1f, speedMul = 1.2f,
                type = SkillType.Active, cooldown = 12f, qiCost = 60f,
                effectType = SkillEffectType.Damage, effectValue = 2.5f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_gene_awaken", name = "sm_skills_994",
                desc = "sm_skills_995",
                icon = "ui/Icons/actor_traits/iconGenius",
                requiredClass = "sm_skills_1367", requiredStage = 4, requiredKnowledge = "sm_know_psi_1_0_0",
                intelligence = 3, dmgMul = 1.25f, hpMul = 1f, speedMul = 1f,
                type = SkillType.Active, cooldown = 18f, qiCost = 70f,
                effectType = SkillEffectType.Buff, effectValue = 1.3f, effectDuration = 6f
            },
            new SkillDef {
                id = "sm_skill_element_mastery", name = "sm_skills_996",
                desc = "sm_skills_997",
                icon = "ui/Icons/actor_traits/iconFire",
                requiredClass = "sm_skills_1368", requiredStage = 4, requiredKnowledge = "sm_know_mage_1_0_0",
                intelligence = 4, dmgMul = 1.3f, hpMul = 1f, speedMul = 1f,
                type = SkillType.Active, cooldown = 14f, qiCost = 65f,
                effectType = SkillEffectType.Damage, effectValue = 2.2f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_soul_shock", name = "sm_skills_998",
                desc = "sm_skills_999",
                icon = "ui/Icons/actor_traits/iconMind",
                requiredClass = "sm_skills_1369", requiredStage = 4, requiredKnowledge = "sm_know_mind_1_0_0",
                intelligence = 5, dmgMul = 1.35f, hpMul = 1f, speedMul = 1.1f,
                type = SkillType.Active, cooldown = 16f, qiCost = 75f,
                effectType = SkillEffectType.Debuff, effectValue = 0.4f, effectDuration = 4f
            },
            new SkillDef {
                id = "sm_skill_apostle_summon", name = "sm_skills_1100",
                desc = "sm_skills_1101",
                icon = "ui/Icons/actor_traits/iconStrong",
                requiredClass = "sm_skills_1365", requiredStage = 9, requiredKnowledge = "sm_know_mech_3_0_0",
                intelligence = 10, dmgMul = 1.4f, hpMul = 1.2f, speedMul = 1.1f,
                type = SkillType.Active, cooldown = 30f, qiCost = 150f,
                effectType = SkillEffectType.Summon, effectValue = 3f, effectDuration = 15f
            },
            new SkillDef {
                id = "sm_skill_emperor_dominion", name = "sm_skills_1102",
                desc = "sm_skills_1103",
                icon = "ui/Icons/actor_traits/iconLeader",
                requiredClass = "sm_skills_1365", requiredStage = 11, requiredKnowledge = "sm_know_mech_4_0_0",
                intelligence = 15, dmgMul = 1.5f, hpMul = 1.3f, speedMul = 1.2f,
                type = SkillType.Active, cooldown = 35f, qiCost = 200f,
                effectType = SkillEffectType.Buff, effectValue = 2f, effectDuration = 10f
            },
            new SkillDef {
                id = "sm_skill_vajra_body", name = "sm_skills_1104",
                desc = "sm_skills_1105",
                icon = "ui/Icons/actor_traits/iconTough",
                requiredClass = "sm_skills_1366", requiredStage = 6, requiredKnowledge = "sm_know_martial_2_0_0",
                intelligence = 0, dmgMul = 1.1f, hpMul = 1.4f, speedMul = 1f,
                type = SkillType.Active, cooldown = 22f, qiCost = 90f,
                effectType = SkillEffectType.Buff, effectValue = 2f, effectDuration = 6f
            },
            new SkillDef {
                id = "sm_skill_war_god", name = "sm_skills_1106",
                desc = "sm_skills_1107",
                icon = "ui/Icons/actor_traits/iconRage",
                requiredClass = "sm_skills_1366", requiredStage = 8, requiredKnowledge = "sm_know_martial_3_0_0",
                intelligence = 2, dmgMul = 1.5f, hpMul = 1.2f, speedMul = 1.3f,
                type = SkillType.Active, cooldown = 28f, qiCost = 120f,
                effectType = SkillEffectType.Damage, effectValue = 3f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_element_control", name = "sm_skills_1108",
                desc = "sm_skills_1109",
                icon = "ui/Icons/actor_traits/iconFire",
                requiredClass = "sm_skills_1367", requiredStage = 6, requiredKnowledge = "sm_know_psi_2_0_0",
                intelligence = 5, dmgMul = 1.4f, hpMul = 1f, speedMul = 1.1f,
                type = SkillType.Active, cooldown = 18f, qiCost = 85f,
                effectType = SkillEffectType.Damage, effectValue = 2.5f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_space_fold", name = "sm_skills_1110",
                desc = "sm_skills_1111",
                icon = "ui/Icons/actor_traits/iconFast",
                requiredClass = "sm_skills_1367", requiredStage = 8, requiredKnowledge = "sm_know_psi_3_0_0",
                intelligence = 8, dmgMul = 1.3f, hpMul = 1f, speedMul = 1.5f,
                type = SkillType.Active, cooldown = 25f, qiCost = 110f,
                effectType = SkillEffectType.Teleport, effectValue = 1f, effectDuration = 2f
            },
            new SkillDef {
                id = "sm_skill_magic_shield", name = "sm_skills_1112",
                desc = "sm_skills_1113",
                icon = "ui/Icons/actor_traits/iconBlessing",
                requiredClass = "sm_skills_1368", requiredStage = 6, requiredKnowledge = "sm_know_mage_2_0_0",
                intelligence = 6, dmgMul = 1.1f, hpMul = 1.4f, speedMul = 1f,
                type = SkillType.Active, cooldown = 20f, qiCost = 95f,
                effectType = SkillEffectType.Buff, effectValue = 3f, effectDuration = 5f
            },
            new SkillDef {
                id = "sm_skill_forbidden_spell", name = "sm_skills_1114",
                desc = "sm_skills_1115",
                icon = "ui/Icons/actor_traits/iconGenius",
                requiredClass = "sm_skills_1368", requiredStage = 8, requiredKnowledge = "sm_know_mage_3_0_0",
                intelligence = 12, dmgMul = 1.6f, hpMul = 1.1f, speedMul = 1.1f,
                type = SkillType.Active, cooldown = 30f, qiCost = 180f,
                effectType = SkillEffectType.Damage, effectValue = 4f, effectDuration = 0f
            },
            new SkillDef {
                id = "sm_skill_mental_barrier", name = "sm_skills_1116",
                desc = "sm_skills_1117",
                icon = "ui/Icons/actor_traits/iconMind",
                requiredClass = "sm_skills_1369", requiredStage = 6, requiredKnowledge = "sm_know_mind_2_0_0",
                intelligence = 8, dmgMul = 1.1f, hpMul = 1.4f, speedMul = 1f,
                type = SkillType.Active, cooldown = 22f, qiCost = 100f,
                effectType = SkillEffectType.Buff, effectValue = 2.5f, effectDuration = 7f
            },
            new SkillDef {
                id = "sm_skill_law_eye", name = "sm_skills_1118",
                desc = "sm_skills_1119",
                icon = "ui/Icons/actor_traits/iconEagleEye",
                requiredClass = "sm_skills_1369", requiredStage = 8, requiredKnowledge = "sm_know_mind_3_0_0",
                intelligence = 10, dmgMul = 1.4f, hpMul = 1.1f, speedMul = 1.2f,
                type = SkillType.Active, cooldown = 24f, qiCost = 130f,
                effectType = SkillEffectType.Debuff, effectValue = 0.3f, effectDuration = 6f
            }
        };

        public static readonly Dictionary<long, HashSet<string>> _learned = new Dictionary<long, HashSet<string>>();

        private static readonly Dictionary<long, Dictionary<string, float>> _cooldown = new Dictionary<long, Dictionary<string, float>>();

        public static void Register()
        {
        }

        public static bool HasSkill(Actor a, string skillId)
        {
            if (a == null) return false;
            return _learned.TryGetValue(a.id, out var set) && set.Contains(skillId);
        }

        public static bool LearnSkill(Actor a, string skillId)
        {
            if (a == null) return false;
            var def = GetDef(skillId);
            if (def == null) return false;

            if (!string.IsNullOrEmpty(def.requiredKnowledge) && !SuperMechKnowledge.IsUnlocked(a, def.requiredKnowledge))
                return false;

            if (!_learned.TryGetValue(a.id, out var set))
            {
                set = new HashSet<string>();
                _learned[a.id] = set;
            }
            if (set.Contains(skillId)) return false;
            set.Add(skillId);
            return true;
        }

        public static SkillDef GetDef(string skillId)
        {
            foreach (var def in AllSkills)
                if (def.id == skillId) return def;
            return null;
        }

        public static List<SkillDef> GetLearned(Actor a)
        {
            var list = new List<SkillDef>();
            if (a == null || !_learned.TryGetValue(a.id, out var set)) return list;
            foreach (var def in AllSkills)
                if (set.Contains(def.id)) list.Add(def);
            return list;
        }

        public static List<SkillDef> GetAvailable(Actor a)
        {
            var list = new List<SkillDef>();
            if (a == null) return list;
            string cls = SuperMechBranch.GetClass(a);
            int stage = SuperMechStage.GetStage(a);
            foreach (var def in AllSkills)
            {
                if (HasSkill(a, def.id)) continue;
                if (!string.IsNullOrEmpty(def.requiredClass) && (cls == null || !cls.Contains(def.requiredClass))) continue;
                if (stage < def.requiredStage) continue;
                if (!string.IsNullOrEmpty(def.requiredKnowledge) && !SuperMechKnowledge.IsUnlocked(a, def.requiredKnowledge)) continue;
                list.Add(def);
            }
            return list;
        }

        public static void TickAutoLearn(Actor a)
        {
            if (a == null) return;
            var available = GetAvailable(a);
            foreach (var def in available)
            {
                LearnSkill(a, def.id);
            }
        }

        public static void TickAutoLearnAll()
        {
            if (World.world == null || World.world.units == null) return;
            foreach (var a in World.world.units)
            {
                if (a == null || !a.isAlive()) continue;
                TickAutoLearn(a);
            }
        }

        public static SkillBonus GetBonus(Actor a)
        {
            var bonus = new SkillBonus();
            if (a == null || !_learned.TryGetValue(a.id, out var set)) return bonus;
            string cls = SuperMechBranch.GetClass(a);
            string prefix = "";
            if (cls == "sm_skills_1100") prefix = "mech";
            else if (cls == "sm_skills_1101") prefix = "martial";
            else if (cls == "sm_skills_1102") prefix = "psi";
            else if (cls == "sm_skills_1103") prefix = "mage";
            else if (cls == "sm_skills_1104") prefix = "mind";
            int knowCount = !string.IsNullOrEmpty(prefix) ? SuperMechKnowledge.GetUnlockedCount(a, prefix) : 0;
            float knowMul = 1f + knowCount * 0.02f;
            foreach (var def in AllSkills)
            {
                if (!set.Contains(def.id)) continue;
                bonus.intelligence += def.intelligence;
                bonus.dmgMul *= def.dmgMul * knowMul;
                bonus.hpMul *= def.hpMul;
                bonus.speedMul *= def.speedMul;
            }
            return bonus;
        }

        public class SkillBonus
        {
            public int intelligence = 0;
            public float dmgMul = 1f;
            public float hpMul = 1f;
            public float speedMul = 1f;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_learned, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }

        public static void Clear() { _learned.Clear(); _cooldown.Clear(); }
    }
}
