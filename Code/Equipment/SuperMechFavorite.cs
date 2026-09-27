using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechFavorite
    {
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

        public static bool IsFavorite(Actor a)
        {
            if (a == null) return false;
            try { return a.isFavorite(); }
            catch { return false; }
        }

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
                if (IsFavorite(a)) continue;

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
