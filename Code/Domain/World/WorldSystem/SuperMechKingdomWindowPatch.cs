using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.187.2 重写王国界面注入：移除普通王国跳过逻辑，添加超能者统计/殖民地/科技点/文化力/战略
    /// Harmony patch KingdomWindow.showStatsRows，在原版统计行后追加模组信息
    /// </summary>
    [HarmonyPatch(typeof(KingdomWindow), "showStatsRows")]
    public static class SuperMechKingdomWindowPatch
    {
        private static MethodInfo _showStatRow;

        static void Postfix(KingdomWindow __instance)
        {
            try
            {
                Kingdom kingdom = __instance.meta_object;
                if (kingdom == null || kingdom.wild || !kingdom.isCiv()) return;

                // 反射调用 showStatRow(string, object, MetaType, long, string)
                if (_showStatRow == null)
                {
                    _showStatRow = AccessTools.Method(typeof(KingdomWindow), "showStatRow",
                        new[] { typeof(string), typeof(object), typeof(MetaType), typeof(long), typeof(string) });
                }
                if (_showStatRow == null) return;

                // === 文明与科技 ===
                var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(kingdom);
                int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(kingdom);
                float techPoints = SuperMechCivilization.GetTechPointsFromKingdom(kingdom);

                _showStatRow.Invoke(__instance, new object[] {
                    LocalizedTextManager.getText("sm_ui_civ_level"),
                    SuperMechCivilization.GetLevelName(civLevel), MetaType.None, -1L, "iconKingdom"
                });

                _showStatRow.Invoke(__instance, new object[] {
                    LocalizedTextManager.getText("sm_ui_tech_level"),
                    $"Lv{techLevel} ({techPoints:F0})", MetaType.None, -1L, "iconAge"
                });

                // === 超能者统计 ===
                int countA = 0, countS = 0, countSS = 0, countX = 0;
                Actor guardian = null;
                int bestRank = -1;
                var units = World.world.units?.units_only_alive;
                if (units != null)
                {
                    foreach (var a in units)
                    {
                        if (a == null || !a.isAlive() || a.kingdom != kingdom) continue;
                        int rank = SuperMechAdvancement.GetExactRankIndex(a);
                        if (rank >= 13) countX++;
                        else if (rank >= 12) countSS++;
                        else if (rank >= 10) countS++;
                        else if (rank >= 8) countA++;
                        if (rank >= 8 && rank > bestRank) { bestRank = rank; guardian = a; }
                    }
                }

                string superCount = $"A:{countA} S:{countS} SS:{countSS} X:{countX}";
                _showStatRow.Invoke(__instance, new object[] {
                    LocalizedTextManager.getText("sm_ui_super_count"),
                    superCount, MetaType.None, -1L, "iconSword"
                });

                if (guardian != null)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        LocalizedTextManager.getText("sm_ui_civ_guardian"),
                        guardian.data.name, MetaType.None, -1L, "iconKings"
                    });
                }

                // === 星际殖民地 ===
                int colonyCount = 0;
                foreach (var p in SuperMechStarSystem.GetAllPlanets())
                {
                    if (p != null && p.colonyLevel > 0 && p.id != SuperMechStarSystem.MAIN_PLANET
                        && p.ownerKingdom == kingdom.data.name)
                    {
                        colonyCount++;
                    }
                }
                if (colonyCount > 0)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        LocalizedTextManager.getText("sm_ui_colony_count"),
                        colonyCount.ToString(), MetaType.None, -1L, "iconCamp"
                    });
                }

                // === 文化力与战略（AI文明）===
                long kid = kingdom.data.id;
                float culturePower = SuperMechCultureAI.GetCulturePower(kid);
                if (culturePower > 0)
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        LocalizedTextManager.getText("sm_ui_culture_power"),
                        culturePower.ToString("F1"), MetaType.None, -1L, "iconBook"
                    });
                }

                string strategy = SuperMechCivStrategyAI.GetStrategyName(kid);
                if (!string.IsNullOrEmpty(strategy) && strategy != "None")
                {
                    _showStatRow.Invoke(__instance, new object[] {
                        LocalizedTextManager.getText("sm_ui_civ_strategy"),
                        strategy, MetaType.None, -1L, "iconEye"
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 王国界面注入异常: " + e.Message);
            }
        }
    }
}
