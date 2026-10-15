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

                // 延迟一帧执行：避免同帧添加的子物体被原生布局覆盖（手机版折叠功能修复）
                __instance.StartCoroutine(InjectNextFrame(__instance, actor));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 单位面板信息注入异常: " + e);
            }
        }

        private static System.Collections.IEnumerator InjectNextFrame(UnitWindow window, Actor actor)
        {
            // 等待多帧：原版StatsRowsContainer.showRows是协程，可能持续多帧遍历
            // 必须等它完全完成后再添加行，否则触发Collection was modified错误
            yield return null;
            yield return null;
            yield return null;
            if (window == null || actor == null || !actor.isAlive()) yield break;
            try
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_super_info"), "", null, SuperMechUiSkin.AccentColor);
                ShowMainInfo(window, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 单位面板延迟注入异常: " + e);
            }
        }

        private static readonly Color InfoColor = SuperMechUiSkin.AccentColor;

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

            // === 核心档案（默认展开）：身份+核心属性 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "core", "sm_ui_section_core");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "core"))
            {
                ShowCoreInfo(window, a);
            }

            // === 快捷操作（默认展开）：最常用的3个功能+详情入口 ===
            SuperMechCollapsibleSection.BuildHeader(window, actorId, "entry", "sm_ui_section_entry");
            if (SuperMechCollapsibleSection.IsExpanded(actorId, "entry"))
            {
                ShowQuickButtons(window, a);
            }
        }

        /// <summary>v0.195 精简：只保留最常用的3个功能按钮+超能详情入口</summary>
        private static void ShowQuickButtons(UnitWindow window, Actor a)
        {
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_knowledge"), "iconBooks",
                SuperMechUiSkin.SciPurple, () => SuperMechWindowManager.OpenKnowledge(a));
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_bag"), "iconBox",
                SuperMechUiSkin.AccentColor, () => SuperMechWindowManager.OpenBag(a));

            if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_spell_entry"), "iconPurpleBook",
                    SuperMechUiSkin.SciPurple, () => SuperMechWindowManager.OpenSpell(a));
            }
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_craft_entry"), "iconBuildings",
                    SuperMechUiSkin.SuccessGreen, () => SuperMechWindowManager.OpenCraft(a));
            }

            // v0.195 超能详情入口：其他详细信息和功能按钮都在详情面板中
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_actor_detail"), "iconChosenOne",
                SuperMechUiSkin.CyanColor, () => SuperMechActorDetailView.Toggle(a));
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

        private static void ShowEntryButton(UnitWindow window, string label, string iconPath, Color color, System.Action onClick)
        {
            try
            {
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
                string valueText = "▸ " + LocalizedTextManager.getText("sm_ui_open");
                var row = window.showStatRow(label, valueText,
                    colorHex, MetaType.None, -1L, pColorText: false, iconPath, null, null, pLocalize: false);
                // v0.194 修复：延迟添加的行需手动激活
                if (row != null) row.gameObject.SetActive(true);
                if (row != null)
                {
                    // v0.201.7 修复手机端：手机端第一次点击触发on_hover_value，第二次才触发on_click_value
                    // 同时绑定两个事件，确保手机端和PC端都能正常打开
                    row.on_click_value = () => onClick?.Invoke();
                    row.on_hover_value = () => onClick?.Invoke();
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
                case "sm_unitwindow_1150": // 机械师
                    int stage = SuperMechStage.GetStage(a);
                    return stage >= 3 ? LocalizedTextManager.getText("sm_qi_mech") : LocalizedTextManager.getText("sm_qi_qi");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_qi_mage"); // 法师→魔力
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_qi_mind"); // 异能→精神力
                case "sm_unitwindow_1149": return LocalizedTextManager.getText("sm_qi_psi");  // 念力师→念力
                case "sm_unitwindow_1146": return LocalizedTextManager.getText("sm_qi_qi");   // 武道家→气力
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
                var row = window.showStatRow(label, value, colorHex, MetaType.None, -1L, pColorText: false, iconPath, null, null, pLocalize: false);
                // v0.194 修复：延迟一帧添加的行错过了showRows()协程的激活，需手动激活
                if (row != null) row.gameObject.SetActive(true);
                return row;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
                return null;
            }
        }
    }
}
