using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位收藏（星标）助手。
    /// 原版 Actor 实现 IFavoriteable 接口，有公开方法 isFavorite() / switchFavorite()，
    /// data.favorite 是 BaseSystemData 的公开 bool 属性。
    /// 达到配置阶位的单位自动被游戏收藏，方便玩家追踪强力单位。
    /// </summary>
    public static class SuperMechFavorite
    {
        /// <summary>设置单位收藏状态。返回是否成功。</summary>
        public static bool SetFavorite(Actor a, bool favorite)
        {
            if (a == null || a.data == null) return false;
            try
            {
                if (a.isFavorite() != favorite)
                    a.switchFavorite();
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
            try { return a.isFavorite(); }
            catch { return false; }
        }

        /// <summary>
        /// 遍历全场单位，达到配置阶位的自动收藏。
        /// 每个主阶位有独立开关（参考凡人修仙传），+位跟随主阶位。
        /// </summary>
        public static void TickAutoFavorite()
        {
            if (!SuperMechConfig.AutoFavoriteEnabled) return;

            var list = World.world.units.units_only_alive;
            if (list == null) return;

            int favorited = 0;
            foreach (Actor a in list)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (IsFavorite(a)) continue;  // 已收藏的跳过

                // 检查当前阶位是否在收藏列表中
                int currentRankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                if (currentRankIdx >= 0 && SuperMechConfig.ShouldFavoriteRank(currentRankIdx))
                {
                    if (SetFavorite(a, true)) favorited++;
                }
            }
            if (favorited > 0 && SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] 自动收藏 {favorited} 个单位");
        }
    }
}
