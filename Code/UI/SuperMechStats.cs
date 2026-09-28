using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechStats
    {
        public static BaseStats Of(Actor a)
        {
            if (a == null) return null;
            return SuperMechReflection.GetFieldValue<BaseStats>(a, "stats");
        }
    }
}
