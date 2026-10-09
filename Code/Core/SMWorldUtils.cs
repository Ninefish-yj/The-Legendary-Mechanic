using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>世界相关工具方法（查找边缘地块、最强文明等）</summary>
    public static class SMWorldUtils
    {
        /// <summary>在地图边缘找一个可生成的地块（排除深海和山脉）</summary>
        public static WorldTile FindEdgeTile()
        {
            if (World.world == null) return null;
            int w = MapBox.width, h = MapBox.height;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Random.value < 0.5f ? Random.Range(0, Mathf.Max(1, w / 8)) : Random.Range(w - w / 8, w);
                int y = Random.Range(0, h);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                WorldTile t = World.world.GetTile(x, y);
                if (t != null && t.Type != TileLibrary.deep_ocean && t.Type != TileLibrary.close_ocean && t.Type != TileLibrary.mountains)
                    return t;
            }
            return null;
        }

        /// <summary>查找当前世界文明等级最高的王国</summary>
        public static Kingdom FindStrongestKingdom()
        {
            try
            {
                if (World.world == null || World.world.kingdoms == null || World.world.kingdoms.list == null) return null;
                Kingdom best = null;
                int bestLevel = -1;
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.wild || !k.isCiv()) continue;
                    int lv = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
                    if (lv > bestLevel) { bestLevel = lv; best = k; }
                }
                return best;
            }
            catch { return null; }
        }
    }
}
