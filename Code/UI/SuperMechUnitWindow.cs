using System;
using System.Collections;
using NeoModLoader.services;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "OnEnable")]
    public static class SuperMechUnitWindow
    {
        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            if (__instance.actor == null || !__instance.actor.isAlive()) return;
            __instance.StartCoroutine(DelayedShow(__instance));
        }

        private static IEnumerator DelayedShow(UnitWindow window)
        {
            yield return null;
            if (window.actor == null || !window.actor.isAlive()) yield break;

            try
            {
                Actor a = window.actor;
                ShowRow(window, LocalizedTextManager.getText("sm_ui_super_info"), "", null, new Color(1f, 0.85f, 0.4f));
                ShowMainInfo(window, a);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 单位面板信息失败: {e.Message}\n{e.StackTrace}");
            }
        }

        private static readonly Color InfoColor = new Color(1f, 0.9f, 0.6f);

        private static void ShowMainInfo(UnitWindow window, Actor a)
        {
            bool hasTalent = SuperMechTalent.HasTalent(a);
            if (!hasTalent)
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), LocalizedTextManager.getText("sm_ui_mortal"), null, InfoColor);
                return;
            }

            ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), GetRank(a), null, InfoColor);

            bool hasProfession = SuperMechProfession.HasProfession(a);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(a);
                string clsAspect = GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), clsText, null, InfoColor);

                string stage = SuperMechStage.GetStageName(a);
                if (stage != "—" && stage != "sm_knowledgetab_829")
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_class_stage"), stage, null, InfoColor);
            }
            else
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), null, InfoColor);
            }

            float onar = SuperMechAdvancement.CalcOnar(a);
            ShowRow(window, LocalizedTextManager.getText("sm_ui_onar"), $"{onar:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}", null, InfoColor);

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            ShowRow(window, qiName, $"{qiBar}（{qiLvText}）", null, InfoColor);

            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), null, InfoColor);

            if (SuperMechAwakened.IsAwakened(a))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), null, InfoColor);

            Actor actorRef = a;
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_knowledge"), "iconBooks",
                new Color(0.5f, 0.35f, 0.7f), () => SMKnowledgeWindowImgui.Toggle(actorRef));
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_bag"), "iconBox",
                new Color(0.9f, 0.7f, 0.3f), () => SMBagWindowImgui.Toggle(actorRef));

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_craft_entry"), "iconBuildings",
                    new Color(0.6f, 0.8f, 0.4f), () => SMCraftWindow.Toggle(actorRef));
            }
        }

        private static void ShowEntryButton(UnitWindow window, string label, string iconPath, Color color, System.Action onClick)
        {
            try
            {
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
                string valueText = "▸ " + LocalizedTextManager.getText("sm_ui_open");
                var row = window.showStatRow(label, valueText,
                    colorHex, MetaType.None, -1L, pColorText: false, iconPath, null, null, pLocalize: false);
                if (row != null)
                {
                    row.on_click_value = () => onClick?.Invoke();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 入口按钮创建失败: " + e.Message);
            }
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

        private static KeyValueField ShowRow(UnitWindow window, string label, object value, string iconPath = null, Color? color = null)
        {
            try
            {
                Color c = color ?? Color.white;
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(c);
                return window.showStatRow(label, value, colorHex, MetaType.None, -1L, pColorText: false, iconPath, null, null, pLocalize: false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
                return null;
            }
        }
    }
}
