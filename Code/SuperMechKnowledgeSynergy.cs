using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识协同效应：特定知识组合触发额外加成。
    /// 原著：知识树不是孤立的，不同知识之间有联动效果（如机械系的"基础组装+能量护盾"联动）。
    /// 参考天人武道神藏系统的"先天协同"设计。
    /// </summary>
    public static class SuperMechKnowledgeSynergy
    {
        /// <summary>协同组合定义。</summary>
        public class SynergyDef
        {
            public string id;
            public string name;
            public string desc;
            public string prefix;          // 所属系
            public string[] requiredKnowledge; // 需要的知识ID（全部满足才触发）
            public float dmgMul;          // 伤害倍率
            public float hpMul;           // 生命倍率
            public float speedMul;        // 攻速倍率
            public float qiBonus;         // 气力加成
            public int potentialBonus;    // 潜能点加成
        }

        /// <summary>所有协同组合。</summary>
        private static readonly List<SynergyDef> _synergies = new List<SynergyDef>();

        /// <summary>单位已激活的协同（actorId → HashSet<synergyId>）。</summary>
        private static readonly Dictionary<long, HashSet<string>> _active = new Dictionary<long, HashSet<string>>();

        /// <summary>协同加成缓存（actorId → 总加成）。</summary>
        private static readonly Dictionary<long, SynergyBonus> _bonusCache = new Dictionary<long, SynergyBonus>();

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
            // 机械系协同
            Add("syn_mech_assembly", "机械精通", "基础组装+武器改造，机械制造效率提升", "mech",
                new[] { "sm_know_mech_0_0_0", "sm_know_mech_0_0_1" },
                dmgMul: 1.1f, speedMul: 1.1f);

            Add("syn_mech_energy", "能量工程", "能量护盾+能量核心，能量类装备效果提升", "mech",
                new[] { "sm_know_mech_1_1_0", "sm_know_mech_1_1_1" },
                hpMul: 1.15f, qiBonus: 500f);

            Add("syn_mech_virtual", "虚拟网络", "虚拟入侵+数据解析，虚拟系能力联动", "mech",
                new[] { "sm_know_mech_2_2_0", "sm_know_mech_2_2_1" },
                dmgMul: 1.2f, speedMul: 1.15f);

            Add("syn_mech_god", "神级机械", "星海工程+真理造物，接近古神机械师水平", "mech",
                new[] { "sm_know_mech_3_1_0", "sm_know_mech_4_1_0" },
                dmgMul: 1.5f, hpMul: 1.3f, potentialBonus: 5);

            // 武道系协同
            Add("syn_martial_body", "炼体大成", "基础炼体+力量训练，体魄强度提升", "martial",
                new[] { "sm_know_martial_0_1_0", "sm_know_martial_0_1_1" },
                hpMul: 1.15f, dmgMul: 1.05f);

            Add("syn_martial_speed", "疾风步", "敏捷训练+步法精通，移动和攻速提升", "martial",
                new[] { "sm_know_martial_0_0_0", "sm_know_martial_1_0_0" },
                speedMul: 1.2f);

            Add("syn_martial_qi", "气劲外放", "气劲掌握+暴气技巧，气力转化效率提升", "martial",
                new[] { "sm_know_martial_1_2_0", "sm_know_martial_2_2_0" },
                dmgMul: 1.15f, qiBonus: 800f);

            // 异能系协同
            Add("syn_psi_gene", "基因觉醒", "一阶基因链+二阶基因链，基因能力联动", "psi",
                new[] { "sm_know_psi_0_0_0", "sm_know_psi_1_0_0" },
                dmgMul: 1.15f);

            Add("syn_psi_control", "精细操控", "操控强化+能级强化，异能精度提升", "psi",
                new[] { "sm_know_psi_1_1_0", "sm_know_psi_2_1_0" },
                dmgMul: 1.1f, speedMul: 1.1f);

            // 魔法系协同
            Add("syn_mage_element", "元素共鸣", "元素掌握+元素精通，元素魔法效果提升", "mage",
                new[] { "sm_know_mage_0_2_0", "sm_know_mage_1_2_0" },
                dmgMul: 1.2f);

            Add("syn_mage_arcane", "奥术精通", "专精魔法+魔网链接，魔力效率提升", "mage",
                new[] { "sm_know_mage_1_0_0", "sm_know_mage_2_0_0" },
                hpMul: 1.1f, qiBonus: 600f);

            // 念力系协同
            Add("syn_mind_soul", "灵魂链接", "灵魂感知+灵魂操控，精神力联动", "mind",
                new[] { "sm_know_mind_0_0_0", "sm_know_mind_1_0_0" },
                dmgMul: 1.15f, speedMul: 1.05f);

            Add("syn_mind_reality", "现实扭曲", "法则掌握+现实干涉，接近规则级能力", "mind",
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

        /// <summary>检查单位已解锁知识，更新激活的协同。</summary>
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

        /// <summary>获取单位已激活的协同列表。</summary>
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

        /// <summary>获取单位协同总加成。</summary>
        public static SynergyBonus GetBonus(Actor a)
        {
            if (a != null && _bonusCache.TryGetValue(a.id, out var bonus)) return bonus;
            return new SynergyBonus();
        }

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_active, alive);
            removed += SuperMechCleanup.CleanDict(_bonusCache, alive);
            return removed;
        }

        /// <summary>清空。</summary>
        public static void Clear() { _active.Clear(); _bonusCache.Clear(); }
    }
}
