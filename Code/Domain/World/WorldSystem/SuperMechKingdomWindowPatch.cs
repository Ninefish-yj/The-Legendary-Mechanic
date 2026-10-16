using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.205.2 修复王国面板注入：showStatRow返回的KeyValueField默认SetActive(false)，
    /// Postfix追加的行赶不上showRows协程，必须手动SetActive(true)。
    /// on_click_value/on_hover_value是public字段不是属性，用GetField。
    /// Harmony patch KingdomWindow.showStatsRows，在原版统计行后追加模组信息+按钮入口
    /// </summary>
    [HarmonyPatch(typeof(KingdomWindow), "showStatsRows")]
    public static class SuperMechKingdomWindowPatch
    {
        private static MethodInfo _showStatRow;
        private static FieldInfo _onClickField;
        private static FieldInfo _onHoverField;
        private static PropertyInfo _gameObjectProp;

        static void Postfix(KingdomWindow __instance)
        {
            try
            {
                Kingdom kingdom = __instance.meta_object;
                if (kingdom == null || kingdom.wild || !kingdom.isCiv()) return;

                // 反射缓存：showStatRow(string, object, MetaType, long, string)
                if (_showStatRow == null)
                {
                    _showStatRow = AccessTools.Method(typeof(KingdomWindow), "showStatRow",
                        new[] { typeof(string), typeof(object), typeof(MetaType), typeof(long), typeof(string) });
                }
                if (_showStatRow == null) return;

                // 反射缓存：KeyValueField的字段和属性
                if (_onClickField == null)
                {
                    _onClickField = typeof(KeyValueField).GetField("on_click_value");
                    _onHoverField = typeof(KeyValueField).GetField("on_hover_value");
                    _gameObjectProp = typeof(Component).GetProperty("gameObject");
                }

                // === 文明与科技 ===
                var civLevel = SuperMechCivilization.GetCivLevelFromKingdom(kingdom);
                int techLevel = SuperMechCivilization.GetTechLevelFromKingdom(kingdom);
                float techPoints = SuperMechCivilization.GetTechPointsFromKingdom(kingdom);

                AddRow(__instance, LocalizedTextManager.getText("sm_ui_civ_level"),
                    SuperMechCivilization.GetLevelName(civLevel), "iconKingdom");

                AddRow(__instance, LocalizedTextManager.getText("sm_ui_tech_level"),
                    $"Lv{techLevel} ({techPoints:F0})", "iconAge");

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
                AddRow(__instance, LocalizedTextManager.getText("sm_ui_super_count"), superCount, "iconSword");

                if (guardian != null)
                {
                    AddRow(__instance, LocalizedTextManager.getText("sm_ui_civ_guardian"),
                        guardian.data.name, "iconKings");
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
                    AddRow(__instance, LocalizedTextManager.getText("sm_ui_colony_count"),
                        colonyCount.ToString(), "iconCamp");
                }

                // === 文化力与战略（AI文明）===
                long kid = kingdom.data.id;
                float culturePower = SuperMechCultureAI.GetCulturePower(kid);
                if (culturePower > 0)
                {
                    AddRow(__instance, LocalizedTextManager.getText("sm_ui_culture_power"),
                        culturePower.ToString("F1"), "iconBook");
                }

                string strategy = SuperMechCivStrategyAI.GetStrategyName(kid);
                if (!string.IsNullOrEmpty(strategy) && strategy != "None")
                {
                    AddRow(__instance, LocalizedTextManager.getText("sm_ui_civ_strategy"),
                        strategy, "iconEye");
                }

                // === 按钮入口：打开星海总览 ===
                var btnRow = AddRow(__instance, LocalizedTextManager.getText("sm_ui_kingdom_open_overview"),
                    "▸ " + LocalizedTextManager.getText("sm_ui_open"), "iconPlanet");
                if (btnRow != null)
                {
                    _onClickField?.SetValue(btnRow, (System.Action)(() => SuperMechWindowManager.OpenOverview()));
                    _onHoverField?.SetValue(btnRow, (System.Action)(() => SuperMechWindowManager.OpenOverview()));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 王国界面注入异常: " + e.Message);
            }
        }

        /// <summary>
        /// 调用showStatRow并手动激活（Postfix中showRows协程已启动，新行不会被自动激活）
        /// </summary>
        private static KeyValueField AddRow(KingdomWindow window, string label, object value, string icon)
        {
            var row = _showStatRow.Invoke(window, new object[] {
                label, value, MetaType.None, -1L, icon
            }) as KeyValueField;
            if (row != null)
            {
                row.gameObject.SetActive(true);
            }
            return row;
        }
    }
}
