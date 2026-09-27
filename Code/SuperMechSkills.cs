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
            public string requiredClass; // 需要的职业系（"sm_skills_1365"/"sm_skills_1366"/"sm_skills_1367"/"sm_skills_1368"/"sm_skills_1369"/""=全系）
            public int requiredStage;     // 需要的职业阶段（0=入门者，3=磁环...）
            public string requiredKnowledge; // 需要学会的知识节点ID（""=无前置知识）
            public int intelligence;      // 智力加成
            public float dmgMul;          // 伤害倍率
            public float hpMul;           // 生命倍率
            public float speedMul;        // 攻速倍率
        }

        public static readonly List<SkillDef> AllSkills = new List<SkillDef>
        {
            new SkillDef {
                id = "sm_skill_qimod", name = "sm_skills_986",
                desc = "sm_skills_987",
                icon = "ui/Icons/actor_traits/iconBattleReflexes",
                requiredClass = "sm_skills_1365", requiredStage = 3, requiredKnowledge = "sm_know_mech_1_0_0", // 电磁理论进阶知识
                intelligence = 5, dmgMul = 1.1f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_virtual_purify", name = "sm_skills_988",
                desc = "sm_skills_989",
                icon = "ui/Icons/actor_traits/iconChosenOne",
                requiredClass = "sm_skills_1365", requiredStage = 6, requiredKnowledge = "sm_know_mech_2_0_0", // 虚拟技术高端知识
                intelligence = 3, dmgMul = 1.05f, hpMul = 1.05f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_dimension_march", name = "sm_skills_990",
                desc = "sm_skills_991",
                icon = "ui/Icons/actor_traits/iconBlessing",
                requiredClass = "sm_skills_1365", requiredStage = 7, requiredKnowledge = "sm_know_mech_3_0_0", // 维度技术尖端知识
                intelligence = 8, dmgMul = 1.2f, hpMul = 1.1f, speedMul = 1.1f
            },
            new SkillDef {
                id = "sm_skill_qi_burst", name = "sm_skills_992",
                desc = "sm_skills_993",
                icon = "ui/Icons/actor_traits/iconRage",
                requiredClass = "sm_skills_1366", requiredStage = 4, requiredKnowledge = "sm_know_martial_1_2_0", // 超能分支进阶知识（气劲外放）
                intelligence = 0, dmgMul = 1.3f, hpMul = 1f, speedMul = 1.2f
            },
            new SkillDef {
                id = "sm_skill_gene_awaken", name = "sm_skills_994",
                desc = "sm_skills_995",
                icon = "ui/Icons/actor_traits/iconGenius",
                requiredClass = "sm_skills_1367", requiredStage = 4, requiredKnowledge = "sm_know_psi_1_0_0", // 攻效分支进阶知识（基因链理论）
                intelligence = 3, dmgMul = 1.25f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_element_mastery", name = "sm_skills_996",
                desc = "sm_skills_997",
                icon = "ui/Icons/actor_traits/iconFire",
                requiredClass = "sm_skills_1368", requiredStage = 4, requiredKnowledge = "sm_know_mage_1_0_0", // 元素魔法进阶知识
                intelligence = 4, dmgMul = 1.3f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_soul_shock", name = "sm_skills_998",
                desc = "sm_skills_999",
                icon = "ui/Icons/actor_traits/iconMind",
                requiredClass = "sm_skills_1369", requiredStage = 4, requiredKnowledge = "sm_know_mind_1_0_0", // 灵魂分支进阶知识
                intelligence = 5, dmgMul = 1.35f, hpMul = 1f, speedMul = 1.1f
            }
        };

        private static readonly Dictionary<long, HashSet<string>> _learned = new Dictionary<long, HashSet<string>>();

        private static readonly Dictionary<long, Dictionary<string, float>> _cooldown = new Dictionary<long, Dictionary<string, float>>();

        public static void Register()
        {
            Debug.Log($"[超神机械师] 独立技能系统注册：{AllSkills.Count}个职业技能");
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
            Debug.Log($"[超神机械师] {a.name} 学会技能：{def.name}");
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
            if (cls == "sm_skills_1000") prefix = "mech";
            else if (cls == "sm_skills_1001") prefix = "martial";
            else if (cls == "sm_skills_1002") prefix = "psi";
            else if (cls == "sm_skills_1003") prefix = "mage";
            else if (cls == "sm_skills_1004") prefix = "mind";
            int knowCount = !string.IsNullOrEmpty(prefix) ? SuperMechKnowledge.GetUnlockedCount(a, prefix) : 0;
            float knowMul = 1f + knowCount * 0.02f; // 每学会1个同系知识，技能伤害+2%
            foreach (var def in AllSkills)
            {
                if (!set.Contains(def.id)) continue;
                bonus.intelligence += def.intelligence;
                bonus.dmgMul *= def.dmgMul * knowMul; // 知识提升技能威力
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
