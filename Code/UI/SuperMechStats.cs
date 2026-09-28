using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechStats
    {
        private static readonly FieldInfo StatsField =
            typeof(BaseSimObject).GetField("stats",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static BaseStats Of(Actor a)
        {
            if (a == null || StatsField == null) return null;
            try { return (BaseStats)StatsField.GetValue(a); }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 获取BaseStats反射失败: " + e.Message);
                return null;
            }
        }
    }
}
