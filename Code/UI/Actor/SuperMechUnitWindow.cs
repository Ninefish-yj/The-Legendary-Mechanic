using System;
using HarmonyLib;
using NeoModLoader.services;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindow
    {
        // v0.218 防重复注入：记录当前正在注入的窗口，防止RebuildPanel触发多次showStatsRows导致集合枚举冲突
        private static UnitWindow _injectingWindow;

        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                if (__instance == null) return;
                Actor actor = __instance.actor;
                if (actor == null || actor.isRekt() || actor.data == null) return;

                // v0.218 防止同一窗口重复启动注入协程（点击折叠按钮时RebuildPanel会触发多次showStatsRows）
                if (_injectingWindow == __instance) return;
                _injectingWindow = __instance;

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
            // v0.218 修复Collection was modified：等待原版showRows协程完全完成后再注入
            // showRows()每帧激活一行，行多时需多帧，期间修改stats_rows会触发异常
            var container = window.GetComponentInChildren<StatsRowsContainer>();
            if (container != null)
            {
                int timeout = 60; // 最多等60帧，防止死等
                while (timeout-- > 0)
                {
                    bool allActive = true;
                    foreach (Transform child in container.transform)
                    {
                        if (!child.gameObject.activeSelf) { allActive = false; break; }
                    }
                    if (allActive) break;
                    yield return null;
                }
                // v0.218 额外等待2帧：确保showRows协程的MoveNext完全执行完毕，枚举器已释放
                yield return null;
                yield return null;
            }
            else
            {
                // 找不到container时退化为固定延迟
                yield return null;
                yield return null;
                yield return null;
            }

            // v0.218 注入前二次校验：窗口可能已关闭或切换了单位
            if (window == null || actor == null || !actor.isAlive())
            {
                _injectingWindow = null;
                yield break;
            }
            if (window.actor != actor)
            {
                // 窗口已切换到其他单位，本次注入作废
                _injectingWindow = null;
                yield break;
            }

            try
            {
                // v0.232 超能者信息整体可折叠：点击标题行切换展开/收起
                long actorId = actor.data.id;
                bool expanded = SuperMechCollapsibleSection.IsExpanded(actorId, "super_info");
                string arrow = expanded ? "▼" : "▶";
                string title = $"{arrow} {LocalizedTextManager.getText("sm_ui_super_info")}";
                string valueText = expanded ? LocalizedTextManager.getText("sm_ui_collapse") : LocalizedTextManager.getText("sm_ui_expand");

                var headerRow = ShowRow(window, title, valueText, null, SuperMechUiSkin.CyanColor);
                if (headerRow != null)
                {
                    UnityEngine.Events.UnityAction toggleAction = () =>
                    {
                        SuperMechCollapsibleSection.Toggle(actorId, "super_info");
                        window.showStatsRows();
                    };
                    headerRow.on_click_value = toggleAction;
                    headerRow.on_hover_value = () =>
                    {
                        if (!InputHelpers.mouseSupported) toggleAction();
                    };
                }

                if (expanded)
                {
                    ShowMainInfo(window, actor);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 单位面板延迟注入异常: " + e);
            }
            finally
            {
                _injectingWindow = null;
            }
        }

        private static readonly Color InfoColor = SuperMechUiSkin.CyanColor;

        private static void ShowMainInfo(UnitWindow window, Actor a)
        {
            bool hasTalent = SuperMechTalent.HasTalent(a);

            // v0.228 气力条优先插入到原版UnitBarsElement区域（和饥饿/体力/魔力条排在一起）
            // 如果插入成功，就不在统计行重复显示
            if (!TryInjectQiBarToBarsArea(window, a))
            {
                ShowQiBar(window, a);
            }

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

        /// <summary>v0.228 尝试把气力条插入到原版UnitBarsElement区域（和饥饿/体力/魔力条排在一起）</summary>
        private static bool TryInjectQiBarToBarsArea(UnitWindow window, Actor a)
        {
            try
            {
                // 找到原版进度条容器（包含饥饿/快乐/体力/魔力条）
                var barsElement = window.GetComponentInChildren<UnitBarsElement>();
                if (barsElement == null) return false;

                // 找到一个现有的StatBar作为模板（魔力条最相似，都是蓝色系）
                var existingBars = barsElement.GetComponentsInChildren<StatBar>(true);
                if (existingBars == null || existingBars.Length == 0) return false;

                // 检查是否已经插入过气力条（防止重复）
                var existingQi = barsElement.transform.Find("SM_QiBar");
                if (existingQi != null)
                {
                    UpdateQiBar(existingQi.GetComponent<StatBar>(), a);
                    return true;
                }

                // 复制模板StatBar
                var template = existingBars[0];
                var qiBarGo = UnityEngine.Object.Instantiate(template.gameObject, barsElement.transform);
                qiBarGo.name = "SM_QiBar";

                // 修改颜色为青色（模组主题色）
                var barImg = qiBarGo.transform.Find("bar")?.GetComponent<Image>();
                if (barImg != null)
                {
                    barImg.color = new Color(0.35f, 0.75f, 0.85f, 1f);
                }

                // 设置气力值
                var statBar = qiBarGo.GetComponent<StatBar>();
                UpdateQiBar(statBar, a);

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 气力条插入Bars区域失败: " + e.Message);
                return false;
            }
        }

        /// <summary>v0.228 更新气力条数值</summary>
        private static void UpdateQiBar(StatBar bar, Actor a)
        {
            if (bar == null) return;
            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");

            if (qiMax > 0)
            {
                bar.setBar(qi, qiMax, $"/{qiMax:F0} {qiLvText}");
            }
            else
            {
                bar.setBar(0, 1, qiLvText);
            }
        }

        /// <summary>v0.226 气力条重做：模仿原版消耗条——纯进度条+等级标签，不显示数字</summary>
        private static void ShowQiBar(UnitWindow window, Actor a)
        {
            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);

            // v0.226 只显示名称+等级，数值由进度条表达（像原版生命条）
            var qiRow = ShowRow(window, qiName, qiLvText, null, InfoColor);
            if (qiRow != null)
            {
                if (qiRow.icon != null && SuperMechUiSkin.QiIcon != null)
                {
                    qiRow.icon.sprite = SuperMechUiSkin.QiIcon;
                    qiRow.icon.gameObject.SetActive(true);
                }
                if (qiMax > 0)
                {
                    AddQiProgressBar(qiRow.gameObject, qi, qiMax);
                }
            }
        }

        /// <summary>v0.205 UI重设计：单位面板6按钮（知识/背包/超能详情/装备改装/机械军团/维度创世）</summary>
        private static void ShowQuickButtons(UnitWindow window, Actor a)
        {
            // 1. 知识（固定）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_knowledge"), "iconBooks",
                SuperMechUiSkin.SciPurple, () => SuperMechWindowManager.OpenKnowledge(a));
            // 2. 背包（固定）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_bag"), "iconBox",
                SuperMechUiSkin.CyanColor, () => SuperMechWindowManager.OpenBag(a));
            // 3. 超能详情（固定：合并职业/法术/气力/天赋/专精）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_actor_detail"), "iconChosenOne",
                SuperMechUiSkin.CyanColor, () => SuperMechActorDetailView.Toggle(a));
            // 4. 装备改装（固定：合并锻造）
            ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_craft_entry"), "iconBuildings",
                SuperMechUiSkin.SuccessGreen, () => SuperMechWindowManager.OpenCraft(a));
            // 5. 机械军团（机械系显示）
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_mech_legion"), "iconSword",
                    new Color(0.7f, 0.8f, 1.0f), () => SuperMechWindowManager.OpenMechLegion(a));
            }
            // 6. 维度/虚拟创世（高阶位或有维度特质显示）
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            if (rank >= 8 || a.hasTrait("sm_dimension_traveler"))
            {
                ShowEntryButton(window, LocalizedTextManager.getText("sm_ui_dim_title"), "iconEye",
                    new Color(0.8f, 0.6f, 0.9f), () => SuperMechWindowManager.OpenDimension(a));
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

            // 4. 气力已移到ShowQiBar（所有单位都显示，v0.218）

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
            // v0.226 原著阶位纯靠欧纳判断，不显示初期/中期/巅峰细分（欧纳数值在能级行显示）
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

        /// <summary>
        /// v0.203 在KeyValueField行上叠加气力可视化进度条
        /// 参考诡秘之主模组的自定义条实现：在行物体底部添加背景+填充两个Image
        /// </summary>
        /// <summary>v0.226 气力进度条：纯色Image实现，不依赖运行时Sprite，模仿原版消耗条样式</summary>
        private static void AddQiProgressBar(GameObject rowGo, float current, float max)
        {
            try
            {
                if (rowGo == null || max <= 0) return;
                float ratio = Mathf.Clamp01(current / max);

                // 进度条背景（整行宽度，在行底部）
                var bgGo = new GameObject("QiBarBg");
                bgGo.transform.SetParent(rowGo.transform, false);
                var bgRect = bgGo.AddComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(0, 0);
                bgRect.anchorMax = new Vector2(1, 0);
                bgRect.pivot = new Vector2(0.5f, 0);
                bgRect.sizeDelta = new Vector2(-16, 10);
                bgRect.anchoredPosition = new Vector2(0, 2);
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
                bgImg.raycastTarget = false;

                // 进度条填充（青色，从左到右）
                var fillGo = new GameObject("QiBarFill");
                fillGo.transform.SetParent(bgGo.transform, false);
                var fillRect = fillGo.AddComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(ratio, 1);
                fillRect.pivot = new Vector2(0, 0.5f);
                fillRect.sizeDelta = Vector2.zero;
                fillRect.anchoredPosition = Vector2.zero;
                var fillImg = fillGo.AddComponent<Image>();
                fillImg.color = new Color(0.35f, 0.75f, 0.85f, 0.9f);
                fillImg.raycastTarget = false;

                // 顶部高光（增加质感）
                var hlGo = new GameObject("QiBarHL");
                hlGo.transform.SetParent(fillGo.transform, false);
                var hlRect = hlGo.AddComponent<RectTransform>();
                hlRect.anchorMin = new Vector2(0, 0.6f);
                hlRect.anchorMax = new Vector2(1, 1);
                hlRect.sizeDelta = Vector2.zero;
                var hlImg = hlGo.AddComponent<Image>();
                hlImg.color = new Color(1, 1, 1, 0.2f);
                hlImg.raycastTarget = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 气力进度条创建失败: " + e.Message);
            }
        }
    }
}
