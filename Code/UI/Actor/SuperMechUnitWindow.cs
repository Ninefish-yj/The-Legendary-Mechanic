using System;
using HarmonyLib;
using NeoModLoader.services;
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
                if (SuperMechConfig.ShowRankInPanel)
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), LocalizedTextManager.getText("sm_ui_mortal"), null, InfoColor);
                return;
            }

            long actorId = a.data.id;

            // === 核心档案（默认展开）：身份+核心属性，玩家第一眼看到 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "core", "sm_ui_section_core");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "core"))
            {
                ShowCoreInfo(window, a);
            }

            // === 操作入口（默认展开）：知识/背包/制造等功能按钮 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "entry", "sm_ui_section_entry");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "entry"))
            {
                ShowEntryButtons(window, a);
            }

            // === 战斗数值（默认折叠）：气力层次/潜力等详细数据 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "combat", "sm_ui_section_combat");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "combat"))
            {
                ShowCombatInfo(window, a);
            }

            // === 社会关系（默认折叠）：势力/身份/经济 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "social", "sm_ui_section_social");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "social"))
            {
                ShowSocialInfo(window, a);
            }

            // === 特殊信息（默认折叠）：神性/圣所/种族/机械/专属特质 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "special", "sm_ui_section_special");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "special"))
            {
                ShowSpecialInfo(window, a);
            }
        }

        /// <summary>
        /// 核心档案：阶位→职业→能级→气力→天赋→专精
        /// 符合大众阅读习惯：先身份，再战力，再能力
        /// </summary>
        private static void ShowCoreInfo(UnitWindow window, Actor a)
        {
            // 1. 阶位（最核心，最显眼）
            if (SuperMechConfig.ShowRankInPanel)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), GetRank(a), null, InfoColor);

            // 2. 职业+阶段
            bool hasProfession = SuperMechProfession.HasProfession(a);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(a);
                string clsAspect = GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), clsText, null, InfoColor);

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

                // 专精（机械系）
                if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech))
                {
                    string specName = SuperMechSpecialty.GetSpecialtyName(a);
                    if (specName != LocalizedTextManager.getText("sm_spec_none"))
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_specialization"), specName, null, InfoColor);
                }

                // 法师类型（魔法系）
                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    string mageType = SuperMechMageType.GetMageTypeName(a);
                    if (!string.IsNullOrEmpty(mageType))
                    {
                        string mageSpec = SuperMechMageType.GetSpecName(a);
                        string displayText = string.IsNullOrEmpty(mageSpec) ? mageType : $"{mageType}（{mageSpec}）";
                        ShowRow(window, LocalizedTextManager.getText("sm_ui_mage_type"), displayText, null, InfoColor);
                    }
                }
            }
            else
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), null, InfoColor);
            }

            // 3. 能级（欧纳）—— 战力指标，核心属性
            ShowRow(window, LocalizedTextManager.getText("sm_ui_onar"), SuperMechEnergyLevel.GetShortText(a), null, InfoColor);

            // 4. 气力 —— 核心资源
            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);
            string qiFmt = "F" + SuperMechConfig.QiDisplayDecimals;
            string qiBar = qiMax > 0 ? qi.ToString(qiFmt) + "/" + qiMax.ToString(qiFmt) : qi.ToString(qiFmt);
            ShowRow(window, qiName, $"{qiBar}（{qiLvText}）", null, InfoColor);

            // 5. 天赋倾向
            string talentText = GetTalentText(a);
            if (!string.IsNullOrEmpty(talentText))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_talent_tendency"), talentText, null, InfoColor);
        }

        /// <summary>
        /// 战斗数值：气力层次/潜力等详细战斗数据
        /// </summary>
        private static void ShowCombatInfo(UnitWindow window, Actor a)
        {
            int qiLayer = SuperMechQiLayer.GetLayer(a);
            if (qiLayer > 0)
            {
                string layerText = SuperMechQiLayer.GetLayerText(a);
                ShowRow(window, LocalizedTextManager.getText("sm_ui_qi_layer"), layerText, null, InfoColor);
            }

            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), null, InfoColor);
        }

        /// <summary>
        /// 社会关系：势力/身份/经济
        /// </summary>
        private static void ShowSocialInfo(UnitWindow window, Actor a)
        {
            // 势力
            if (SuperMechConfig.FactionEnabled)
            {
                var faction = SuperMechFaction.GetFaction(a);
                if (faction != null)
                {
                    bool isLeader = faction.leaderId == a.id;
                    string factionText = isLeader
                        ? string.Format(LocalizedTextManager.getText("sm_ui_faction_leader"), faction.name, faction.level)
                        : string.Format(LocalizedTextManager.getText("sm_ui_faction_member"), faction.name, faction.level);
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_faction_label"), factionText, null, InfoColor);
                }
            }

            // 身份（觉醒者/降临者）
            if (SuperMechAwakened.IsAwakened(a))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), null, InfoColor);

            // 伊纳尔余额
            int inal = SuperMechInal.GetInal(a);
            if (inal > 0 || SuperMechSupermA.IsSuperA(a))
                ShowRow(window, LocalizedTextManager.getText("sm_inal_balance"), inal.ToString("N0"), null, InfoColor);

            // 降临者公司
            var company = SuperMechCompany.GetCompanyByActor(a);
            if (company != null)
                ShowRow(window, LocalizedTextManager.getText("sm_company_title"), SuperMechCompany.GetDisplayText(company), null, InfoColor);
        }

        /// <summary>
        /// 特殊信息：神性/圣所/种族/机械军团/专属特质
        /// </summary>
        private static void ShowSpecialInfo(UnitWindow window, Actor a)
        {
            int divLayers = SuperMechDivinity.GetTotalLayers(a);
            string sanctuaryInfo = GetSanctuaryInfo(a);

            if (divLayers > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_divinity"), $"{divLayers}{LocalizedTextManager.getText("sm_ui_divinity_layer")}", null, InfoColor);
            if (!string.IsNullOrEmpty(sanctuaryInfo))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_sanctuary"), sanctuaryInfo, null, InfoColor);

            // 种族
            string speciesId = SuperMechSpeciesEvolution.GetSpeciesId(a.data.id);
            if (speciesId != "human")
            {
                string speciesName = SuperMechSpeciesEvolution.GetSpeciesName(a.data.id);
                ShowRow(window, LocalizedTextManager.getText("sm_ui_species"), speciesName, null, InfoColor);
                string talent = SuperMechSpeciesEvolution.GetSpeciesTalent(a.data.id);
                if (!string.IsNullOrEmpty(talent))
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_species_talent"), talent, null, InfoColor);
            }

            // 机械军团单位
            string mechType = SuperMechMechLegion.GetUnitType(a);
            if (!string.IsNullOrEmpty(mechType))
            {
                string mechName = SuperMechMechLegion.GetUnitName(mechType);
                ShowRow(window, LocalizedTextManager.getText("sm_ui_mech_type"), mechName, null, InfoColor);
                string summoner = SuperMechMechLegion.GetSummonerName(a);
                if (!string.IsNullOrEmpty(summoner))
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_mech_summoner"), summoner, null, InfoColor);
                int lifeLeft = SuperMechMechLegion.GetLifeLeft(a);
                if (lifeLeft > 0)
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_mech_life"), $"{lifeLeft}", null, InfoColor);
            }

            // 专属特质/模板
            string specText = SuperMechExclusiveTrait.GetDisplay(a);
            if (!string.IsNullOrEmpty(specText))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_specialties"), specText, null, InfoColor);
            string tplText = SuperMechExclusiveTrait.GetTemplateDisplay(a);
            if (tplText != null)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_templates"), tplText, null, InfoColor);
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
                    new Color(0.6f, 0.8f, 0.4f), () => SuperMechWindowManager.OpenCraft(a));
            }

            // v0.76.48 宇宙宝物：选中单位装备/切换宇宙宝物（创世神赐予 trait）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_cr_entry"), "iconChosenOne",
                new Color(0.95f, 0.75f, 0.3f), () => SuperMechWindowManager.OpenCosmicRelic(a));

            // v0.76.48 次级维度：高阶超能者进入维度获得增益（原著：次级维度修炼）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_dim_entry"), "iconDivineLight",
                new Color(0.75f, 0.55f, 0.95f), () => SuperMechWindowManager.OpenDimension(a));

            // v0.86.1 虚拟创世：机械师虚拟分支转职超神机械师时领悟（原著：虚实转化）
            if (SuperMechVirtualGenesis.HasGenesis(a))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_virtual_genesis"), "iconChosenOne",
                    new Color(0.5f, 0.9f, 0.8f), () => SuperMechWindowManager.OpenVirtualGenesis(a));
            }

            // v0.95.0 机械军团：机械师分支可制造机械单位（原著：机械师核心特色）
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_mech_legion"), "iconWeapon",
                    new Color(0.7f, 0.8f, 1.0f), () => SuperMechWindowManager.OpenMechLegion(a));
            }

            // v0.89.1 种族进化：非人类种族且有可进化目标时显示
            var evolvable = SuperMechSpeciesEvolution.GetEvolvableSpecies(a.data.id);
            if (evolvable.Count > 0)
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_species_evolve"), "iconDivineLight",
                    new Color(0.8f, 0.9f, 0.6f), () => SuperMechSpeciesEvolution.AutoEvolve(a.data.id));
            }

            // v0.183 伊纳尔服务：付费复活/传送/洗点等
            ShowEntryButton(window, LocalizedTextManager.getText("sm_econ_service_panel"), "iconBox",
                new Color(0.4f, 0.8f, 0.6f), () => SuperMechEconomyServiceView.Toggle(a));

            // v0.183 战斗统计：击杀/伤害/评级
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_combat_stats"), "iconWeapon",
                new Color(0.9f, 0.5f, 0.4f), () => SuperMechCombatStatsView.Toggle(a));

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
