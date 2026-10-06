using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.51.1 注入文明等级和科技等级到原版王国信息面板
    /// Harmony patch KingdomWindow.showStatsRows，在原版统计行后追加文明信息
    /// </summary>
    [HarmonyPatch(typeof(KingdomWindow), "showStatsRows")]
    public static class SuperMechKingdomWindowPatch
    {
        private static MethodInfo _showStatRow;

        static void Postfix(KingdomWindow __instance)
        {
            if (!SuperMechConfig.FactionEnabled) return; // 用势力总控开关
            Kingdom kingdom = __instance.meta_object;
            if (kingdom == null || kingdom.wild || !kingdom.isCiv()) return;

            // 获取文明等级（通过王国内任意单位查询，或直接计算）
            var civLevel = GetKingdomCivLevel(kingdom);
            var techLevel = SuperMechCivilization.GetTechLevelFromKingdom(kingdom);

            if (civLevel == SuperMechCivilization.CivLevel.Normal && techLevel <= 0) return;

            // 反射调用 showStatRow(string, object, MetaType, long, string)
            if (_showStatRow == null)
            {
                _showStatRow = AccessTools.Method(typeof(KingdomWindow), "showStatRow",
                    new[] { typeof(string), typeof(object), typeof(MetaType), typeof(long), typeof(string) });
            }

            if (_showStatRow != null)
            {
                if (civLevel != SuperMechCivilization.CivLevel.Normal)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        "文明等级", SuperMechCivilization.GetLevelName(civLevel), MetaType.None, -1L, "iconKingdom"
                    });
                }
                if (techLevel > 0)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        "科技等级", $"Lv{techLevel}", MetaType.None, -1L, "iconAge"
                    });
                }
                // v0.52.0 文明守护者
                var guardian = GetGuardian(kingdom);
                if (guardian != null)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        "文明守护者", guardian.name, MetaType.None, -1L, "iconKings"
                    });
                }
            }
        }

        /// <summary>获取王国守护者</summary>
        private static Actor GetGuardian(Kingdom kingdom)
        {
            var units = World.world.units?.units_only_alive;
            if (units == null) return null;
            Actor best = null;
            int bestRank = -1;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.kingdom != kingdom) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank >= 8 && rank > bestRank)
                {
                    bestRank = rank;
                    best = a;
                }
            }
            return best;
        }

        /// <summary>从王国直接计算文明等级（不依赖单位查询）</summary>
        private static SuperMechCivilization.CivLevel GetKingdomCivLevel(Kingdom kingdom)
        {
            int countA = 0, countS = 0, countSS = 0, countX = 0;
            var units = World.world.units?.units_only_alive;
            if (units == null) return SuperMechCivilization.CivLevel.Normal;

            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.kingdom != kingdom) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank >= 13) countX++;
                else if (rank >= 12) countSS++;
                else if (rank >= 10) countS++;
                else if (rank >= 8) countA++;
            }

            if (countX > 0) return SuperMechCivilization.CivLevel.Universal;
            if (countSS > 0) return SuperMechCivilization.CivLevel.SuperCluster;
            if (countS > 0 || countA >= 3) return SuperMechCivilization.CivLevel.Cluster;
            if (countA > 0) return SuperMechCivilization.CivLevel.Stellar;
            return SuperMechCivilization.CivLevel.Normal;
        }
    }
}
