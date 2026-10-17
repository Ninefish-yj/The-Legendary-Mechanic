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
                Debug.Log($"[超神机械师] KingdomWindowPatch执行: kingdom={kingdom?.data.name}, wild={kingdom?.wild}, isCiv={kingdom?.isCiv()}");
                if (kingdom == null || kingdom.wild || !kingdom.isCiv()) return;

                // 反射缓存：showStatRow完整7参数签名（StatsWindow定义，KingdomWindow继承）
                if (_showStatRow == null)
                {
                    _showStatRow = AccessTools.Method(typeof(StatsWindow), "showStatRow",
                        new[] { typeof(string), typeof(object), typeof(MetaType), typeof(long), typeof(string), typeof(string), typeof(TooltipDataGetter) });
                    Debug.Log($"[超神机械师] showStatRow反射结果: {_showStatRow != null}");
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
                        if (rank >= 8) countX++;
                        else if (rank >= 7) countSS++;
                        else if (rank >= 6) countS++;
                        else if (rank >= 5) countA++;
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

                // === 模组功能按钮区（和单位面板操作逻辑一致：点按钮开对应窗口）===
                // 科技树
                AddFuncButton(__instance, LocalizedTextManager.getText("sm_ui_civ_tech_tree"),
                    () => { SuperMechWindowManager.SelectedKingdomId = kid; SuperMechWindowManager.OpenTech(); });

                // 经济管理
                AddFuncButton(__instance, LocalizedTextManager.getText("sm_ui_economy_title"),
                    () => { SuperMechWindowManager.SelectedKingdomId = kid; SuperMechWindowManager.OpenEconomy(); });

                // 建筑管理
                AddFuncButton(__instance, LocalizedTextManager.getText("sm_ui_building_title"),
                    () => { SuperMechWindowManager.SelectedKingdomId = kid; SuperMechWindowManager.OpenBuilding(); });

                // 势力外交
                AddFuncButton(__instance, LocalizedTextManager.getText("sm_ui_faction_window_title"),
                    () => { SuperMechWindowManager.SelectedKingdomId = kid; SuperMechWindowManager.OpenFaction(); });

                // 注：星海总览是全局面板，不在单个王国面板中提供入口（神权栏已有入口）
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
                label, value, MetaType.None, -1L, icon, null, null
            }) as KeyValueField;
            if (row != null)
            {
                row.gameObject.SetActive(true);
            }
            return row;
        }

        /// <summary>
        /// 添加功能按钮行（标签=功能名，值=▸，点击执行action）
        /// </summary>
        private static void AddFuncButton(KingdomWindow window, string label, System.Action action)
        {
            var row = AddRow(window, label, "▸", "iconPlanet");
            if (row != null)
            {
                _onClickField?.SetValue(row, action);
                _onHoverField?.SetValue(row, action);
            }
        }
    }
}
