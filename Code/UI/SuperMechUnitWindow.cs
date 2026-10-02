using System;
using NeoModLoader.services;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindow
    {
        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                if (__instance == null) return;
                Actor actor = __instance.actor;
                if (actor == null || actor.isRekt() || actor.data == null) return;

                ShowRow(__instance, LocalizedTextManager.getText("sm_ui_super_info"), "", null, new Color(1f, 0.85f, 0.4f));
                ShowMainInfo(__instance, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 单位面板信息注入异常: {e.Message}");
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

            // === 核心身份组 ===
            ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), GetRank(a), null, InfoColor);

            string talentText = GetTalentText(a);
            if (!string.IsNullOrEmpty(talentText))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_talent_tendency"), talentText, null, InfoColor);

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

                // 机械师专精显示（只有机械师分支且解锁了专精才显示）
                if (a.hasTrait(SuperMechBranch.BranchMech))
                {
                    string specName = SuperMechSpecialization.GetSpecName(a);
                    if (specName != LocalizedTextManager.getText("sm_spec_none"))
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_specialization"), specName, null, new Color(0.6f, 0.9f, 1f));
                }
            }
            else
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), null, InfoColor);
            }

            // === 数值组 ===
            ShowRow(window, LocalizedTextManager.getText("sm_ui_onar"), SuperMechEnergyLevel.GetEvaluationText(a), null, InfoColor);

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            ShowRow(window, qiName, $"{qiBar}（{qiLvText}）", null, InfoColor);

            // 气力层次
            int qiLayer = SuperMechQiLayer.GetLayer(a);
            if (qiLayer > 0)
            {
                string layerText = SuperMechQiLayer.GetLayerText(a);
                ShowRow(window, LocalizedTextManager.getText("sm_ui_qi_layer"), layerText, null, new Color(0.7f, 0.85f, 1f));
            }

            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), null, InfoColor);

            // === 特殊组 ===
            bool hasSpecial = false;
            int divLayers = SuperMechDivinity.GetTotalLayers(a);
            if (divLayers > 0) hasSpecial = true;
            string sanctuaryInfo = GetSanctuaryInfo(a);
            if (!string.IsNullOrEmpty(sanctuaryInfo)) hasSpecial = true;
            if (SuperMechAwakened.IsAwakened(a)) hasSpecial = true;

            if (hasSpecial)
            {
                if (divLayers > 0)
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_divinity"), $"{divLayers}{LocalizedTextManager.getText("sm_ui_divinity_layer")}", null, new Color(1f, 0.6f, 0.9f));
                if (!string.IsNullOrEmpty(sanctuaryInfo))
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_sanctuary"), sanctuaryInfo, null, new Color(0.7f, 0.9f, 1f));
                if (SuperMechAwakened.IsAwakened(a))
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), null, InfoColor);
            }

            // === 入口按钮组 ===
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_knowledge"), "iconBooks",
                new Color(0.5f, 0.35f, 0.7f), () => SMWindowManager.OpenKnowledge(a));
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_bag"), "iconBox",
                new Color(0.9f, 0.7f, 0.3f), () => SMWindowManager.OpenBag(a));
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_fusion"), "iconSkills",
                new Color(0.3f, 0.7f, 0.9f), () => SMWindowManager.OpenFusion(a));

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_craft_entry"), "iconBuildings",
                    new Color(0.6f, 0.8f, 0.4f), () => SMWindowManager.OpenCraft());
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

        private static string GetTalentText(Actor a)
        {
            var talents = SuperMechTalent.GetTalents(a);
            if (talents == null || talents.Count == 0) return "";
            var parts = new System.Collections.Generic.List<string>();
            foreach (var t in talents)
            {
                string name = SuperMechTalent.GetTalentName(t.type);
                string rating = SuperMechTalent.RatingNames[Mathf.Clamp(t.rating, 0, SuperMechTalent.RatingNames.Length - 1)];
                parts.Add($"{name}({rating})");
            }
            return string.Join(" ", parts);
        }

        private static string GetSanctuaryInfo(Actor a)
        {
            var parts = new System.Collections.Generic.List<string>();
            string[] names = { "sm_sanctuary_name_1", "sm_sanctuary_name_2", "sm_sanctuary_name_3",
                               "sm_sanctuary_name_4", "sm_sanctuary_name_5", "sm_sanctuary_name_6" };
            for (int i = 0; i < 6; i++)
            {
                float auth = SuperMechSanctuary.GetAuthority(a, i);
                if (auth > 0)
                {
                    parts.Add($"{LocalizedTextManager.getText(names[i])}:{auth:F0}");
                }
            }
            return string.Join(" ", parts);
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
