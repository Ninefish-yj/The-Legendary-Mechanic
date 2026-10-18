using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.25 修复 StatsRowsContainer.OnDisable 崩溃。
    /// 根因：原版OnDisable调用_stats_pool.clear()→sortElements()排序，
    /// 排序比较器访问a.transform时，若KeyValueField已被销毁但引用还在
    /// 对象池_elements_total列表里，就抛NullReferenceException。
    /// 模组折叠标题行在重建时可能被销毁但未从对象池移除，导致关闭
    /// 单位面板/王国面板时崩溃，连带超能详情页无法关闭。
    /// 修复：Prefix在clear()执行前清理对象池中的null/已销毁元素。
    /// </summary>
    [HarmonyPatch(typeof(StatsRowsContainer), "OnDisable")]
    public static class SuperMechStatsRowsOnDisablePatch
    {
        static void Prefix(StatsRowsContainer __instance)
        {
            try
            {
                // 获取 _stats_pool 字段（ObjectPoolGenericMono<KeyValueField>）
                var poolField = typeof(StatsRowsContainer).GetField("_stats_pool",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (poolField == null) return;
                object pool = poolField.GetValue(__instance);
                if (pool == null) return;

                // 获取 _elements_total 字段（List<KeyValueField>）
                var totalField = pool.GetType().GetField("_elements_total",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (totalField == null) return;
                var totalList = totalField.GetValue(pool) as System.Collections.IList;
                if (totalList == null) return;

                // 移除 null 或已销毁的元素（Unity的Component被销毁后==null为true）
                int removed = 0;
                for (int i = totalList.Count - 1; i >= 0; i--)
                {
                    object item = totalList[i];
                    if (item == null || (item is Object uo && uo == null))
                    {
                        totalList.RemoveAt(i);
                        removed++;
                    }
                }
                if (removed > 0)
                {
                    Debug.Log($"[超神机械师] OnDisable前清理Stats对象池null元素: {removed}个");
                }
            }
            catch (System.Exception e)
            {
                // 清理失败不影响原方法执行，静默忽略
                Debug.LogWarning("[超神机械师] StatsRowsContainer.OnDisable清理异常: " + e.Message);
            }
        }
    }
}
