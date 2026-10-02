using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechQiAttribute
    {
        public const string AttrMagnetic = "sm_qiattribute_938";
        public const string AttrSpirit   = "sm_qiattribute_939";
        public const string AttrFire     = "sm_qiattribute_940";
        public const string AttrWind     = "sm_qiattribute_941";
        public const string AttrIron     = "sm_qiattribute_942";
        public const string AttrWater    = "sm_qiattribute_943";
        public const string AttrLightning = "sm_qiattribute_944";
        public const string AttrDark     = "sm_qiattribute_945";
        public const string AttrLight    = "sm_qiattribute_946";
        public const string AttrNone     = "sm_qiattribute_947";

        /// <summary>
        /// 原著五属性克制环：金→磁→念→灵→暗→金
        /// 克制方伤害+30%，被克制方伤害-20%
        /// 机械系磁属性，精神系（念力/异能）念/灵属性
        /// </summary>
        private static readonly Dictionary<string, string> CounterTable = new Dictionary<string, string>
        {
            { AttrIron, AttrMagnetic },       // 金克磁（金属屏蔽磁场）
            { AttrMagnetic, AttrSpirit },     // 磁克念（磁场干扰精神）
            { AttrSpirit, AttrLight },        // 念克灵（念力压制灵魂）
            { AttrLight, AttrDark },          // 灵克暗（光明驱散黑暗）
            { AttrDark, AttrIron },           // 暗克金（暗蚀金属）
            // 元素属性扩展克制
            { AttrFire, AttrIron },           // 火克金
            { AttrWater, AttrFire },          // 水克火
            { AttrLightning, AttrWater },     // 雷克水
            { AttrWind, AttrLightning },      // 风克雷
        };

        /// <summary>克制伤害加成（原著：属性克制时威力显著提升）</summary>
        public const float CounterDamageBonus = 0.30f;
        /// <summary>被克制伤害减免</summary>
        public const float CounterDamagePenalty = 0.20f;

        /// <summary>
        /// 获取气力属性克制伤害倍率。
        /// 攻击者属性克制防御者时+30%，被克制时-20%，无克制关系1.0。
        /// 第六级分水岭后克制效果增强（+40%/-25%）。
        /// </summary>
        public static float GetCounterMultiplier(Actor attacker, Actor defender)
        {
            if (attacker == null || defender == null) return 1f;
            string atkAttr = GetAttribute(attacker);
            string defAttr = GetAttribute(defender);
            if (atkAttr == AttrNone || defAttr == AttrNone) return 1f;
            if (atkAttr == defAttr) return 1f;

            bool atkCounters = CounterTable.TryGetValue(atkAttr, out string atkCountersAttr) && atkCountersAttr == defAttr;
            bool defCounters = CounterTable.TryGetValue(defAttr, out string defCountersAttr) && defCountersAttr == atkAttr;

            // 第六级分水岭后克制效果增强
            bool tier6Boost = SuperMechQiLayer.IsBreakthrough(attacker);
            float bonus = tier6Boost ? 0.40f : CounterDamageBonus;
            float penalty = tier6Boost ? 0.25f : CounterDamagePenalty;

            if (atkCounters) return 1f + bonus;
            if (defCounters) return 1f - penalty;
            return 1f;
        }

        /// <summary>职业间克制：机械系对精神系（念力/异能）的攻防修正</summary>
        public static float GetClassCounterMultiplier(Actor attacker, Actor defender)
        {
            if (attacker == null || defender == null) return 1f;
            bool atkMech = attacker.hasTrait(SuperMechTraits.ClassMech);
            bool defMech = defender.hasTrait(SuperMechTraits.ClassMech);
            bool atkPsi = attacker.hasTrait(SuperMechTraits.ClassPsi) || attacker.hasTrait(SuperMechTraits.ClassMind);
            bool defPsi = defender.hasTrait(SuperMechTraits.ClassPsi) || defender.hasTrait(SuperMechTraits.ClassMind);

            // 原著：机械没有灵魂免疫精神伤害，但机械师智力较高幻术影响大幅减弱
            // 机械系攻击精神系：+15%（机械对精神体的物理压制）
            // 精神系攻击机械系：-25%（机械无灵魂，精神攻击效果衰减但非免疫）
            if (atkMech && defPsi) return 1.15f;
            if (atkPsi && defMech) return 0.75f;
            return 1f;
        }

        public static readonly Dictionary<string, string> AttrDesc = new Dictionary<string, string>
        {
            { AttrMagnetic, "sm_qiattribute_948" },
            { AttrSpirit,   "sm_qiattribute_949" },
            { AttrFire,     "sm_qiattribute_950" },
            { AttrWind,     "sm_qiattribute_951" },
            { AttrIron,     "sm_qiattribute_952" },
            { AttrWater,    "sm_qiattribute_953" },
            { AttrLightning, "sm_qiattribute_954" },
            { AttrDark,     "sm_qiattribute_955" },
            { AttrLight,    "sm_qiattribute_956" },
        };

        private static readonly Dictionary<long, string> _attr = new Dictionary<long, string>();

        public static string GetAttribute(Actor a)
        {
            if (a == null) return AttrNone;
            if (_attr.TryGetValue(a.data.id, out string v)) return v;
            // 未分配时按体系默认属性（用于战斗克制计算）
            if (a.hasTrait(SuperMechTraits.ClassMech)) return AttrMagnetic;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return AttrSpirit;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return AttrSpirit;
            return AttrNone;
        }

        public static void SetAttribute(Actor a, string attr)
        {
            if (a == null) return;
            _attr[a.data.id] = attr;
            ApplyBonus(a);
        }

        public static void AutoAssign(Actor a)
        {
            if (a == null) return;
            string attr = AttrNone;

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                // 原著：机械师气力属性为磁，从觉醒开始即有
                // 磁属性可用于制造和使用机械，宛如肢体延伸
                attr = AttrMagnetic;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                attr = AttrSpirit;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMartial))
            {
                // 武道系无固定分支，随机分配气力属性
                string[] martialAttrs = { AttrIron, AttrFire, AttrWind };
                attr = martialAttrs[Mathf.Abs(a.data.id.GetHashCode()) % martialAttrs.Length];
            }
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                // 念力系按四大分支分配属性
                if (a.hasTrait(SuperMechBranch.BranchPsiMind)) attr = AttrSpirit;
                else if (a.hasTrait(SuperMechBranch.BranchPsiKinesis)) attr = AttrWind;
                else if (a.hasTrait(SuperMechBranch.BranchPsiSense)) attr = AttrLight;
                else if (a.hasTrait(SuperMechBranch.BranchPsiPotential)) attr = AttrFire;
                else attr = AttrSpirit;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                // 魔法系无固定分支（法术可兼修），随机分配元素属性
                string[] mageAttrs = { AttrFire, AttrWater, AttrLightning, AttrLight, AttrDark };
                attr = mageAttrs[Mathf.Abs(a.data.id.GetHashCode()) % mageAttrs.Length];
            }

            if (attr != AttrNone) SetAttribute(a, attr);
        }

        /// <summary>
        /// 原著：气力属性并非一成不变，武道系通过修行不同流派可改变属性。
        /// 火的爆裂、风的速度、铁的坚韧。
        /// </summary>
        private static readonly string[] MartialStyles = { AttrIron, AttrFire, AttrWind, AttrWater, AttrLightning };

        /// <summary>武道系流派改变属性的概率（每次升级时检查）</summary>
        public const float MartialStyleChangeChance = 0.15f;

        /// <summary>
        /// 尝试改变武道系单位的气力属性（模拟修行不同流派）。
        /// 原著：武道系通过修行不同流派，可改变自身气力属性。
        /// </summary>
        public static void TryMartialStyleChange(Actor a)
        {
            if (a == null) return;
            if (!a.hasTrait(SuperMechTraits.ClassMartial)) return;
            if (Random.value > MartialStyleChangeChance) return;

            string currentAttr = GetAttribute(a);
            string newAttr = MartialStyles[Mathf.Abs(a.data.id.GetHashCode() + (int)Time.time) % MartialStyles.Length];
            if (newAttr != currentAttr && newAttr != AttrNone)
            {
                SetAttribute(a, newAttr);
                if (SuperMechConfig.LogVerbose)
                {
                    Debug.Log($"[超神机械师] {a.data.name} 武道系修行新流派，气力属性变更");
                }
            }
        }

        /// <summary>
        /// 主动改变单位气力属性（用于技能/道具/事件）。
        /// </summary>
        public static void ChangeAttribute(Actor a, string newAttr)
        {
            if (a == null || string.IsNullOrEmpty(newAttr)) return;
            if (newAttr == AttrNone) return;
            SetAttribute(a, newAttr);
        }

        private static void ApplyBonus(Actor a)
        {
            string attr = GetAttribute(a);
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            switch (attr)
            {
                case AttrMagnetic:
                    stats["intelligence"] = (stats["intelligence"]) + 5f;
                    stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) * 1.1f;
                    break;
                case AttrSpirit:
                    stats["intelligence"] = (stats["intelligence"]) + 8f;
                    stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) * 1.1f;
                    break;
                case AttrFire:
                    stats["damage"] = (stats["damage"]) + 5f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.1f;
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.05f;
                    break;
                case AttrWind:
                    stats["speed"] = (stats["speed"]) + 0.5f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.15f;
                    break;
                case AttrIron:
                    stats["armor"] = (stats["armor"]) + 5f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.15f;
                    break;
                case AttrWater:
                    stats["stamina"] = (stats["stamina"]) + 20f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.05f;
                    break;
                case AttrLightning:
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.1f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.2f;
                    break;
                case AttrDark:
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.08f;
                    stats["damage"] = (stats["damage"]) + 3f;
                    break;
                case AttrLight:
                    stats["intelligence"] = (stats["intelligence"]) + 3f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.08f;
                    break;
            }
        }

        public static void Clear() { _attr.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_attr, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null) _attr.Remove(a.data.id);
        }

        public static void TickAutoAssign()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (GetAttribute(a) == AttrNone) AutoAssign(a);
            }
        }
    }
}
