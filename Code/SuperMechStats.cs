using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位属性访问助手。
    /// 原版 BaseSimObject.stats 是 internal 字段，外部模组在运行时无法直接访问
    /// （Assembly-CSharp-Publicized 只在编译期把它变公开，运行时仍是 internal）。
    /// 这里用反射缓存 FieldInfo 来取得 BaseStats 实例，之后通过其公开索引器读写。
    /// </summary>
    public static class SuperMechStats
    {
        private static readonly FieldInfo StatsField =
            typeof(BaseSimObject).GetField("stats",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        /// <summary>取得单位当前的 BaseStats（可通过公开索引器读写任意属性）。</summary>
        public static BaseStats Of(Actor a)
        {
            if (a == null) return null;
            return (BaseStats)StatsField.GetValue(a);
        }
    }
}
