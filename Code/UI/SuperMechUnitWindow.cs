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
                Debug.LogError("[超神机械师] 单位面板信息注入异常: " + e);
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

            long actorId = a.data.id;

            // === 核心档案组（默认展开）===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "core", "sm_ui_section_core");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "core"))
            {
                ShowCoreInfo(window, a);
            }

            // === 战斗数值组（默认折叠）===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "combat", "sm_ui_section_combat");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "combat"))
            {
                ShowCombatInfo(window, a);
            }

            // === 特殊信息与入口组（默认折叠）===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "special", "sm_ui_section_special");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "special"))
            {
                ShowSpecialInfo(window, a);
                ShowEntryButtons(window, a);
            }
        }

        private static void ShowCoreInfo(UnitWindow window, Actor a)
        {
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

                // v0.75.33: 职业阶段仅对觉醒者显示（独立转职链，原著玩家转职）；
                // 普通单位的职业阶段是阶位镜像（RankToStage），与"能级阶位"重复，不再展示
                if (SuperMechAwakened.IsAwakened(a))
                {
                    string stage = SuperMechStage.GetStageName(a);
                    if (stage != "—" && stage != "sm_knowledgetab_829")
                    {
                        string progress = SuperMechStage.GetStageProgressText(a);
                        string stageText = string.IsNullOrEmpty(progress) ? stage : $"{stage}（{progress}）";
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_class_stage"), stageText, null, InfoColor);
                    }
                }

                // v0.50.0 势力显示
                if (SuperMechConfig.FactionEnabled)
                {
                    var faction = SuperMechFaction.GetFaction(a);
                    if (faction != null)
                    {
                        bool isLeader = faction.leaderId == a.id;
                        string factionText = isLeader ? $"{faction.name}（领袖·Lv{faction.level}）" : $"{faction.name}（成员·Lv{faction.level}）";
                        ShowRow(window, "势力", factionText, null, new Color(1f, 0.85f, 0.5f));
                    }
                }

                if (a.hasTrait(SuperMechBranch.BranchMech))
                {
                    string specName = SuperMechSpecialization.GetSpecName(a);
                    if (specName != LocalizedTextManager.getText("sm_spec_none"))
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_specialization"), specName, null, new Color(0.6f, 0.9f, 1f));
                }

                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    string mageType = SuperMechMageType.GetMageTypeName(a);
                    if (!string.IsNullOrEmpty(mageType))
                    {
                        string mageSpec = SuperMechMageType.GetSpecName(a);
                        string displayText = string.IsNullOrEmpty(mageSpec) ? mageType : $"{mageType}（{mageSpec}）";
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_mage_type"), displayText, null, new Color(0.8f, 0.6f, 1f));
                    }
                }
            }
            else
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), null, InfoColor);
            }
        }

        private static void ShowCombatInfo(UnitWindow window, Actor a)
        {
            ShowRow(window, LocalizedTextManager.getText("sm_ui_onar"), SuperMechEnergyLevel.GetShortText(a), null, InfoColor);

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            ShowRow(window, qiName, $"{qiBar}（{qiLvText}）", null, InfoColor);

            int qiLayer = SuperMechQiLayer.GetLayer(a);
            if (qiLayer > 0)
            {
                string layerText = SuperMechQiLayer.GetLayerText(a);
                ShowRow(window, LocalizedTextManager.getText("sm_ui_qi_layer"), layerText, null, new Color(0.7f, 0.85f, 1f));
            }

            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), null, InfoColor);
        }

        private static void ShowSpecialInfo(UnitWindow window, Actor a)
        {
            int divLayers = SuperMechDivinity.GetTotalLayers(a);
            string sanctuaryInfo = GetSanctuaryInfo(a);
            bool isAwakened = SuperMechAwakened.IsAwakened(a);

            if (divLayers > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_divinity"), $"{divLayers}{LocalizedTextManager.getText("sm_ui_divinity_layer")}", null, new Color(1f, 0.6f, 0.9f));
            if (!string.IsNullOrEmpty(sanctuaryInfo))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_sanctuary"), sanctuaryInfo, null, new Color(0.7f, 0.9f, 1f));
            if (isAwakened)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), null, InfoColor);
        }

        private static void ShowEntryButtons(UnitWindow window, Actor a)
        {
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_knowledge"), "iconBooks",
                new Color(0.5f, 0.35f, 0.7f), () => SuperMechWindowManager.OpenKnowledge(a));
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_bag"), "iconBox",
                new Color(0.9f, 0.7f, 0.3f), () => SuperMechWindowManager.OpenBag(a));

            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_spell_entry"), "iconPurpleBook",
                    new Color(0.6f, 0.4f, 1f), () => SuperMechWindowManager.OpenSpell(a));
            }

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_craft_entry"), "iconBuildings",
                    new Color(0.6f, 0.8f, 0.4f), () => SuperMechWindowManager.OpenCraft());
            }

            // v0.75.30: 阶位排行榜从星海总览迁至单位面板入口（查看全图强者格局）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_rank_window_title"), "iconPlanet",
                new Color(1f, 0.62f, 0.04f), () => SuperMechWindowManager.OpenRank());
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
