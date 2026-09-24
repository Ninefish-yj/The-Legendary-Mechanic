using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位收藏（星标）助手。
    /// 原版 Actor.favorite 字段可能是 private/internal，用反射安全访问。
    /// 达到指定阶位的单位自动被游戏收藏，方便玩家追踪强力单位。
    /// </summary>
    public static class SuperMechFavorite
    {
        private static FieldInfo _favoriteField;
        private static bool _fieldResolved;

        /// <summary>通过反射获取 Actor.favorite 字段（尝试多种命名）。</summary>
        private static FieldInfo GetFavoriteField()
        {
            if (_fieldResolved) return _favoriteField;
            _fieldResolved = true;
            string[] candidates = { "favorite", "isFavorite", "_favorite", "starred", "isStarred", "_isFavorite" };
            foreach (var name in candidates)
            {
                var f = typeof(Actor).GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(bool))
                {
                    _favoriteField = f;
                    Debug.Log($"[超神机械师] 收藏字段解析成功: Actor.{name}");
                    return _favoriteField;
                }
            }
            Debug.LogWarning("[超神机械师] 未找到 Actor.favorite 字段，自动收藏功能不可用");
            return null;
        }

        /// <summary>设置单位收藏状态。返回是否成功。</summary>
        public static bool SetFavorite(Actor a, bool favorite)
        {
            if (a == null) return false;
            var f = GetFavoriteField();
            if (f == null) return false;
            try
            {
                f.SetValue(a, favorite);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 设置收藏失败: {e.Message}");
                return false;
            }
        }

        /// <summary>读取单位收藏状态。</summary>
        public static bool IsFavorite(Actor a)
        {
            if (a == null) return false;
            var f = GetFavoriteField();
            if (f == null) return false;
            try { return (bool)f.GetValue(a); }
            catch { return false; }
        }

        /// <summary>
        /// 遍历全场单位，达到配置阶位阈值的自动收藏。
        /// 在 TickPromotions 之后调用，确保阶位已更新。
        /// </summary>
        public static void TickAutoFavorite()
        {
            if (!SuperMechConfig.AutoFavoriteEnabled) return;
            int threshold = SuperMechConfig.AutoFavoriteRank;
            if (threshold <= 0) return;  // 阈值0=F，等于全部收藏，无意义，跳过

            var list = World.world.units.units_only_alive;
            if (list == null) return;

            int favorited = 0;
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (IsFavorite(a)) continue;  // 已收藏的跳过

                // 检查当前阶位是否达到阈值
                int currentRankIdx = -1;
                for (int i = SuperMechRanks.All.Count - 1; i >= 0; i--)
                {
                    if (a.hasTrait(SuperMechRanks.All[i].id)) { currentRankIdx = i; break; }
                }
                if (currentRankIdx >= threshold)
                {
                    if (SetFavorite(a, true)) favorited++;
                }
            }
            if (favorited > 0 && SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] 自动收藏 {favorited} 个单位（阈值阶位索引={threshold}）");
        }
    }
}
