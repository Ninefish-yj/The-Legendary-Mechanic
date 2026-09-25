using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业技能系统（独立系统，参考西幻世界SkillMeta设计，不注册为特质）。
    /// 原著：机械师职业技能（气力改装/虚拟净化/维度行军等），学会后永久掌握，有独立技能栏。
    /// </summary>
    public static class SuperMechSkills
    {
        /// <summary>技能定义。</summary>
        public class SkillDef
        {
            public string id;
            public string name;
            public string desc;
            public string icon;
            public string requiredClass; // 需要的职业系（"机械"/"武道"/"异能"/"魔法"/"念力"/""=全系）
            public int requiredStage;     // 需要的职业阶段（0=入门者，3=磁环...）
            public int intelligence;      // 智力加成
            public float dmgMul;          // 伤害倍率
            public float hpMul;           // 生命倍率
            public float speedMul;        // 攻速倍率
        }

        /// <summary>所有技能定义。</summary>
        public static readonly List<SkillDef> AllSkills = new List<SkillDef>
        {
            new SkillDef {
                id = "sm_skill_qimod", name = "气力改装·LVMAX",
                desc = "气力数值按比例增加制造机械的效率与品质（原著ch237：电磁因子提炼法）",
                icon = "ui/Icons/actor_traits/iconBattleReflexes",
                requiredClass = "机械", requiredStage = 3, // 磁环阶段
                intelligence = 5, dmgMul = 1.1f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_virtual_purify", name = "虚拟净化复原·LVMAX",
                desc = "净化病毒感染的智能目标（原著ch539：虚拟生命净化技术）",
                icon = "ui/Icons/actor_traits/iconChosenOne",
                requiredClass = "机械", requiredStage = 6, // 虚拟阶段
                intelligence = 3, dmgMul = 1.05f, hpMul = 1.05f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_dimension_march", name = "次级维度行军·LVMAX",
                desc = "打开黑色传送门进行维度行军（原著ch626：星海阶段维度技术）",
                icon = "ui/Icons/actor_traits/iconBlessing",
                requiredClass = "机械", requiredStage = 7, // 星海阶段
                intelligence = 8, dmgMul = 1.2f, hpMul = 1.1f, speedMul = 1.1f
            },
            new SkillDef {
                id = "sm_skill_qi_burst", name = "暴气·LVMAX",
                desc = "气劲外放，短时间内大幅提升攻击力（原著ch50：武道超能离体波动）",
                icon = "ui/Icons/actor_traits/iconRage",
                requiredClass = "武道", requiredStage = 4, // D阶以上
                intelligence = 0, dmgMul = 1.3f, hpMul = 1f, speedMul = 1.2f
            },
            new SkillDef {
                id = "sm_skill_gene_awaken", name = "基因觉醒·LVMAX",
                desc = "激发基因链潜力，临时提升异能强度（原著ch50：二阶基因链）",
                icon = "ui/Icons/actor_traits/iconGenius",
                requiredClass = "异能", requiredStage = 4,
                intelligence = 3, dmgMul = 1.25f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_element_mastery", name = "元素掌控·LVMAX",
                desc = "掌控元素能量，提升魔法伤害与范围（原著ch50：魔法元素亲和）",
                icon = "ui/Icons/actor_traits/iconFire",
                requiredClass = "魔法", requiredStage = 4,
                intelligence = 4, dmgMul = 1.3f, hpMul = 1f, speedMul = 1f
            },
            new SkillDef {
                id = "sm_skill_soul_shock", name = "灵魂冲击·LVMAX",
                desc = "精神力直接攻击对方灵魂，造成精神伤害（原著ch50：念力灵魂攻击）",
                icon = "ui/Icons/actor_traits/iconMind",
                requiredClass = "念力", requiredStage = 4,
                intelligence = 5, dmgMul = 1.35f, hpMul = 1f, speedMul = 1.1f
            }
        };

        /// <summary>单位已学会的技能（actorId → HashSet<skillId>）。</summary>
        private static readonly Dictionary<long, HashSet<string>> _learned = new Dictionary<long, HashSet<string>>();

        /// <summary>技能冷却（actorId → skillId → 下次可用时间）。</summary>
        private static readonly Dictionary<long, Dictionary<string, float>> _cooldown = new Dictionary<long, Dictionary<string, float>>();

        /// <summary>注册（初始化技能列表，无实际操作，保留接口）。</summary>
        public static void Register()
        {
            Debug.Log($"[超神机械师] 独立技能系统注册：{AllSkills.Count}个职业技能");
        }

        /// <summary>单位是否学会了某技能。</summary>
        public static bool HasSkill(Actor a, string skillId)
        {
            if (a == null) return false;
            return _learned.TryGetValue(a.id, out var set) && set.Contains(skillId);
        }

        /// <summary>学会技能（满足条件时自动学会，或通过神权赋予）。</summary>
        public static bool LearnSkill(Actor a, string skillId)
        {
            if (a == null) return false;
            var def = GetDef(skillId);
            if (def == null) return false;

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

        /// <summary>获取技能定义。</summary>
        public static SkillDef GetDef(string skillId)
        {
            foreach (var def in AllSkills)
                if (def.id == skillId) return def;
            return null;
        }

        /// <summary>获取单位已学会的所有技能。</summary>
        public static List<SkillDef> GetLearned(Actor a)
        {
            var list = new List<SkillDef>();
            if (a == null || !_learned.TryGetValue(a.id, out var set)) return list;
            foreach (var def in AllSkills)
                if (set.Contains(def.id)) list.Add(def);
            return list;
        }

        /// <summary>获取单位可学习的技能（满足职业/阶段条件但还没学会的）。</summary>
        public static List<SkillDef> GetAvailable(Actor a)
        {
            var list = new List<SkillDef>();
            if (a == null) return list;
            string cls = SuperMechBranch.GetClass(a);
            int stage = SuperMechStage.GetStage(a);
            foreach (var def in AllSkills)
            {
                if (HasSkill(a, def.id)) continue;
                if (!string.IsNullOrEmpty(def.requiredClass) && !cls.Contains(def.requiredClass)) continue;
                if (stage < def.requiredStage) continue;
                list.Add(def);
            }
            return list;
        }

        /// <summary>自动学习：单位达到阶段时自动学会对应技能。</summary>
        public static void TickAutoLearn(Actor a)
        {
            if (a == null) return;
            var available = GetAvailable(a);
            foreach (var def in available)
            {
                LearnSkill(a, def.id);
            }
        }

        /// <summary>遍历所有单位自动学习。</summary>
        public static void TickAutoLearnAll()
        {
            if (World.world == null || World.world.units == null) return;
            foreach (var a in World.world.units)
            {
                if (a == null || !a.isAlive()) continue;
                TickAutoLearn(a);
            }
        }

        /// <summary>获取所有已学会技能的总属性加成。</summary>
        public static SkillBonus GetBonus(Actor a)
        {
            var bonus = new SkillBonus();
            if (a == null || !_learned.TryGetValue(a.id, out var set)) return bonus;
            foreach (var def in AllSkills)
            {
                if (!set.Contains(def.id)) continue;
                bonus.intelligence += def.intelligence;
                bonus.dmgMul *= def.dmgMul;
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

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_learned, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }

        /// <summary>清空。</summary>
        public static void Clear() { _learned.Clear(); _cooldown.Clear(); }
    }
}
