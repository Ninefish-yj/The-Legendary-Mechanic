using System.Collections.Generic;

namespace SuperMech.Code
{
    public static class SuperMechCleanup
    {
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

        public static int CleanNestedDict<T>(Dictionary<long, T> dict, HashSet<long> alive) where T : class
        {
            return CleanDict(dict, alive);
        }

        /// <summary>清理UI静态状态（世界切换时调用）</summary>
        public static void ClearUiStaticState()
        {
            // 当前无UI静态状态需清理；保留入口供未来UI系统扩展
        }
    }
}
