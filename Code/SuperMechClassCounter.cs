using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 系间克制系统（原著：机械系↔念力系互为克星）。
    /// 机械系可屏蔽精神攻击，念力系可入侵机械系统——双方互相克制。
    /// 战斗中检测敌我系别，施加伤害倍率。
    /// </summary>
    public static class SuperMechClassCounter
    {
        // 克制倍率：克制方伤害+25%，被克制方伤害-15%
        private const float CounterDamageBonus = 0.25f;
        private const float CounterDamagePenalty = -0.15f;

        // 克制关系表：key=攻击方系, value=被克制的系列表
        private static readonly Dictionary<string, string[]> CounterTable = new Dictionary<string, string[]>
        {
            { SuperMechTraits.ClassMech, new[] { SuperMechTraits.ClassMind } },   // 机械克念力
            { SuperMechTraits.ClassMind, new[] { SuperMechTraits.ClassMech } },   // 念力克机械
        };

        /// <summary>获取攻击方对目标的伤害倍率（系间克制）。</summary>
        public static float GetDamageMultiplier(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return 1f;
            string atkClass = GetClass(attacker);
            string tgtClass = GetClass(target);
            if (string.IsNullOrEmpty(atkClass) || string.IsNullOrEmpty(tgtClass)) return 1f;

            // 检查攻击方是否克制目标
            if (CounterTable.TryGetValue(atkClass, out var counters))
            {
                foreach (var c in counters)
                {
                    if (c == tgtClass) return 1f + CounterDamageBonus;
                }
            }
            // 检查目标是否克制攻击方（攻击方被克制）
            if (CounterTable.TryGetValue(tgtClass, out var tgtCounters))
            {
                foreach (var c in tgtCounters)
                {
                    if (c == atkClass) return 1f + CounterDamagePenalty;
                }
            }
            return 1f;
        }

        /// <summary>获取单位的系别特质ID。</summary>
        private static string GetClass(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return SuperMechTraits.ClassMech;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return SuperMechTraits.ClassMind;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return SuperMechTraits.ClassMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return SuperMechTraits.ClassPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return SuperMechTraits.ClassMage;
            return null;
        }

        /// <summary>检查两单位是否存在克制关系。</summary>
        public static bool HasCounterRelation(Actor a, Actor b)
        {
            return GetDamageMultiplier(a, b) != 1f;
        }

        /// <summary>获取克制关系描述。</summary>
        public static string GetCounterDesc(Actor attacker, Actor target)
        {
            float mult = GetDamageMultiplier(attacker, target);
            if (mult > 1f) return $"sm_classcounter_545";
            if (mult < 1f) return $"sm_classcounter_546";
            return "";
        }
    }
}
