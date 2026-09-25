using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 通用字典清理工具。
    /// 每60秒从所有系统字典中移除已死亡/移除单位的数据，防止内存泄漏。
    /// </summary>
    public static class SuperMechCleanup
    {
        /// <summary>从字典中移除不在alive集合中的键，返回移除数量。</summary>
        public static int CleanDict<T>(Dictionary<long, T> dict, HashSet<long> alive)
        {
            if (dict == null || dict.Count == 0) return 0;
            var toRemove = new List<long>();
            foreach (var kv in dict)
            {
                if (!alive.Contains(kv.Key)) toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove) dict.Remove(id);
            return toRemove.Count;
        }

        /// <summary>从嵌套字典中移除不在alive集合中的键。</summary>
        public static int CleanNestedDict<T>(Dictionary<long, T> dict, HashSet<long> alive) where T : class
        {
            return CleanDict(dict, alive);
        }
    }
}
