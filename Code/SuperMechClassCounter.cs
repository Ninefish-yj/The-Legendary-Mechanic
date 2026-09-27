using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechClassCounter
    {
        private const float CounterDamageBonus = 0.25f;
        private const float CounterDamagePenalty = -0.15f;

        private static readonly Dictionary<string, string[]> CounterTable = new Dictionary<string, string[]>
        {
            { SuperMechTraits.ClassMech, new[] { SuperMechTraits.ClassMind } },   // 机械克念力
            { SuperMechTraits.ClassMind, new[] { SuperMechTraits.ClassMech } },   // 念力克机械
        };

        public static float GetDamageMultiplier(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return 1f;
            string atkClass = GetClass(attacker);
            string tgtClass = GetClass(target);
            if (string.IsNullOrEmpty(atkClass) || string.IsNullOrEmpty(tgtClass)) return 1f;

            if (CounterTable.TryGetValue(atkClass, out var counters))
            {
                foreach (var c in counters)
                {
                    if (c == tgtClass) return 1f + CounterDamageBonus;
                }
            }
            if (CounterTable.TryGetValue(tgtClass, out var tgtCounters))
            {
                foreach (var c in tgtCounters)
                {
                    if (c == atkClass) return 1f + CounterDamagePenalty;
                }
            }
            return 1f;
        }

        private static string GetClass(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return SuperMechTraits.ClassMech;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return SuperMechTraits.ClassMind;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return SuperMechTraits.ClassMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return SuperMechTraits.ClassPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return SuperMechTraits.ClassMage;
            return null;
        }

        public static bool HasCounterRelation(Actor a, Actor b)
        {
            return GetDamageMultiplier(a, b) != 1f;
        }

        public static string GetCounterDesc(Actor attacker, Actor target)
        {
            float mult = GetDamageMultiplier(attacker, target);
            if (mult > 1f) return $"sm_classcounter_545";
            if (mult < 1f) return $"sm_classcounter_546";
            return "";
        }
    }
}
