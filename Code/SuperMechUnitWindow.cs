using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]


    public static class SuperMechUnitWindow
    {
        private static MethodInfo _showStatRow;

        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                Actor actor = GetActor(__instance);
                if (actor == null || !actor.isAlive())
                {
                    SuperMechInfoCard.Hide(__instance);
                    return;
                }

                SuperMechInfoCard.Show(__instance, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 单位面板浮动卡片失败: {e.Message}");
            }
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                var prop = typeof(UnitWindow).GetProperty("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.GetValue(window) as Actor;
                var field = typeof(UnitWindow).GetField("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        public static string GetClassAspect(string cls)
        {
            switch (cls)
            {
                case "sm_unitwindow_1146": return LocalizedTextManager.getText("sm_aspect_martial");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_aspect_mind");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_aspect_mage");
                case "sm_unitwindow_1149": return LocalizedTextManager.getText("sm_aspect_psi");
                case "sm_unitwindow_1150": return LocalizedTextManager.getText("sm_aspect_mech");
                default: return "";
            }
        }

        public static string GetRank(Actor a)
        {
            return SuperMechRanks.GetRankName(a);
        }

        public static string GetQiDisplayName(Actor a)
        {
            if (!SuperMechProfession.HasProfession(a)) return LocalizedTextManager.getText("sm_qi_qi");
            string cls = SuperMechProfession.GetClass(a);
            switch (cls)
            {
                case "sm_unitwindow_1150":
                    int stage = SuperMechStage.GetStage(a);
                    return stage >= 3 ? LocalizedTextManager.getText("sm_qi_mech") : LocalizedTextManager.getText("sm_qi_qi");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_qi_mage");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_qi_mind");
                default: return LocalizedTextManager.getText("sm_qi_qi");
            }
        }

        public static string GetKnowledgeTreeName(string cls)
        {
            switch (cls)
            {
                case "sm_unitwindow_1146": return LocalizedTextManager.getText("sm_tree_martial");
                case "sm_unitwindow_1149": return LocalizedTextManager.getText("sm_tree_psi");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_tree_mage");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_tree_mind");
                default: return LocalizedTextManager.getText("sm_tree_mech");
            }
        }

        private static void ShowRow(UnitWindow window, string label, object value)
        {
            try
            {
                if (_showStatRow == null)
                {
                    var methods = typeof(UnitWindow).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo best = null;
                    foreach (var m in methods)
                    {
                        if (m.Name == "showStatRow" && m.GetParameters().Length >= 2)
                        {
                            if (best == null || m.GetParameters().Length > best.GetParameters().Length)
                                best = m;
                        }
                    }
                    _showStatRow = best;
                    if (_showStatRow == null)
                    {
                        Debug.LogWarning("[超神机械师] 未找到UnitWindow.showStatRow方法，单位面板注入将跳过");
                        return;
                    }
                }

                ParameterInfo[] parms = _showStatRow.GetParameters();
                object[] args = new object[parms.Length];
                args[0] = label;
                args[1] = value;
                for (int i = 2; i < parms.Length; i++)
                {
                    Type pt = parms[i].ParameterType;
                    string pname = parms[i].Name;
                    if (pname == "pLocalize") { args[i] = false; continue; }
                    if (pt == typeof(string)) args[i] = null;
                    else if (pt == typeof(bool)) args[i] = false;
                    else if (pt == typeof(long)) args[i] = -1L;
                    else if (pt == typeof(MetaType)) args[i] = MetaType.None;
                    else if (pt.IsEnum) args[i] = System.Enum.ToObject(pt, 0);
                    else args[i] = null;
                }
                _showStatRow.Invoke(window, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
            }
        }

        private static void ShowCustomStats(UnitWindow window, Actor a)
        {
            try
            {
                float pp = a.stats[SuperMechCustomStats.StatPotentialPoints];
                if (pp > 0) ShowRow(window, "sm_unitwindow_1151", pp.ToString("F0"));

                float div = a.stats[SuperMechCustomStats.StatDivinityLayers];
                if (div > 0) ShowRow(window, "sm_unitwindow_1152", div.ToString("F0") + "sm_unitwindow_1153");

                int sanctuaryTotal = 0;
                string[] sanctuaryStats = {
                    SuperMechCustomStats.StatSanctuary1,
                    SuperMechCustomStats.StatSanctuary2,
                    SuperMechCustomStats.StatSanctuary3,
                    SuperMechCustomStats.StatSanctuary4,
                    SuperMechCustomStats.StatSanctuary5,
                    SuperMechCustomStats.StatSanctuary6
                };
                foreach (var s in sanctuaryStats) sanctuaryTotal += (int)a.stats[s];
                if (sanctuaryTotal > 0) ShowRow(window, "sm_unitwindow_1154", sanctuaryTotal + "sm_unitwindow_1155");


                float mechAff = a.stats[SuperMechCustomStats.StatMechAffinity];
                if (mechAff > 0) ShowRow(window, "sm_unitwindow_1156", mechAff.ToString("F0") + "%");

                float mageAff = a.stats[SuperMechCustomStats.StatMageAffinity];
                if (mageAff > 0) ShowRow(window, "sm_unitwindow_1157", mageAff.ToString("F0") + "%");

                float mystery = a.stats[SuperMechCustomStats.StatMystery];
                if (mystery > 0) ShowRow(window, "sm_unitwindow_1158", mystery.ToString("F0"));
                float charm = a.stats[SuperMechCustomStats.StatCharm];
                if (charm > 0) ShowRow(window, "sm_unitwindow_1159", charm.ToString("F0"));
                float luck = a.stats[SuperMechCustomStats.StatLuck];
                if (luck > 0) ShowRow(window, "sm_unitwindow_1160", luck.ToString("F0"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 自定义属性显示失败: " + e.Message);
            }
        }
    }
}
