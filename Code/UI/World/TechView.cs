using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.235 科技树UI窗口
    /// 四标签页：概况/材料/科技树/详情
    /// 用原版WindowCreator创建窗口，SuperMechUiBuilder构建内容。
    /// </summary>
    public static class TechView
    {
        private const string WindowId = "sm_tech_window";
        private const float WindowWidth = 520f;
        private const float WindowHeight = 420f;
        private const float NodeWidth = 80f;
        private const float NodeHeight = 28f;
        private const float NodeHPadding = 6f;
        private const float NodeVPadding = 4f;

        private static SuperMechUguiWindow _window;
        private static long _currentKingdomId = -1;
        private static string _selectedTechId = "";
        private static readonly string[] TabLabels = { "概况", "材料", "科技树", "详情" };

        // 分支颜色
        private static readonly Color BranchBase = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color BranchMech = new Color(0.4f, 0.7f, 0.9f);
        private static readonly Color BranchPower = new Color(0.9f, 0.5f, 0.5f);
        private static readonly Color BranchSpace = new Color(0.5f, 0.8f, 0.6f);
        private static readonly Color BranchInfo = new Color(0.7f, 0.6f, 0.9f);

        // 状态颜色
        private static readonly Color StatusResearched = new Color(0.3f, 0.7f, 0.3f);
        private static readonly Color StatusResearching = new Color(0.9f, 0.8f, 0.3f);
        private static readonly Color StatusAvailable = new Color(0.4f, 0.6f, 0.9f);
        private static readonly Color StatusLocked = new Color(0.4f, 0.4f, 0.4f);

        /// <summary>打开科技树窗口（指定王国）</summary>
        public static void Open(long kingdomId)
        {
            _currentKingdomId = kingdomId;
            _selectedTechId = "";
            if (_window == null)
            {
                _window = SuperMechUguiWindow.Create(WindowId, "sm_tech_title", WindowWidth, WindowHeight);
            }
            _window.Show();
            Rebuild();
        }

        /// <summary>关闭窗口</summary>
        public static void Close()
        {
            _window?.Close();
        }

        /// <summary>重建内容</summary>
        private static void Rebuild()
        {
            if (_window == null) return;
            var content = _window.Content;
            foreach (Transform c in content) UnityEngine.Object.Destroy(c.gameObject);

            var panel = SuperMechUiBuilder.CreateTabbedPanel(content, TabLabels, 0, OnDrawTab);
        }

        /// <summary>绘制标签页</summary>
        private static void OnDrawTab(int tabIndex, RectTransform content)
        {
            switch (tabIndex)
            {
                case 0: DrawOverview(content); break;
                case 1: DrawMaterials(content); break;
                case 2: DrawTechTree(content); break;
                case 3: DrawDetail(content); break;
            }
        }

        // ═══════════════════════════════════════════
        // 概况页
        // ═══════════════════════════════════════════
        private static void DrawOverview(RectTransform content)
        {
            var state = TechSystem.TryGetState(_currentKingdomId);
            if (state == null)
            {
                AddText(content, "sm_tech_no_data", 14, TextAnchor.MiddleCenter, 0, 0, 0, 0);
                return;
            }

            float y = -8f;

            // 时代标签
            string eraKey = TechSystem.GetEraKey(state.CurrentEra);
            string eraText = LocalizedTextManager.getText(eraKey);
            AddText(content, $"<color=#F3C34A><b>{eraText}</b></color>", 16, TextAnchor.UpperCenter, 8f, y, -8f, y - 24f);
            y -= 30f;

            // 统计行
            int totalTech = TechDataLoader.AllTechs.Count;
            int totalMat = TechDataLoader.AllMaterials.Count;
            string stats = LocalizedTextManager.getText("sm_tech_stats")
                .Replace("{0}", state.Researched.Count.ToString())
                .Replace("{1}", totalTech.ToString())
                .Replace("{2}", state.DiscoveredMaterials.Count.ToString())
                .Replace("{3}", totalMat.ToString());
            AddText(content, stats, 11, TextAnchor.UpperCenter, 8f, y, -8f, y - 18f);
            y -= 24f;

            // 研究点储备
            string bankText = LocalizedTextManager.getText("sm_tech_bank")
                .Replace("{0}", state.ResearchPoints.ToString("0.0"))
                .Replace("{1}", state.LastYearOutput.ToString("0.0"));
            AddText(content, bankText, 11, TextAnchor.UpperCenter, 8f, y, -8f, y - 18f);
            y -= 24f;

            // 当前研究+进度条
            if (!string.IsNullOrEmpty(state.CurrentResearch))
            {
                var tech = TechDataLoader.GetTech(state.CurrentResearch);
                if (tech != null)
                {
                    float cost = tech.cost * (TechDataLoader.Data?.research?.cost_multiplier ?? 12f);
                    float progress = Mathf.Clamp01(state.ResearchProgress / cost);
                    string techName = GetTechDisplayName(tech.id);

                    string researchLabel = LocalizedTextManager.getText("sm_tech_researching")
                        .Replace("{0}", techName);
                    AddText(content, researchLabel, 11, TextAnchor.UpperCenter, 8f, y, -8f, y - 18f);
                    y -= 20f;

                    // 进度条
                    CreateProgressBar(content, 8f, y, content.rect.width - 16f, 10f, progress,
                        new Color(0.4f, 0.84f, 0.77f));
                    y -= 16f;

                    string eta = state.LastYearOutput > 0.01f
                        ? Mathf.CeilToInt(Mathf.Max(0f, cost - state.ResearchProgress) / state.LastYearOutput).ToString()
                        : "?";
                    string progressText = $"{state.ResearchProgress:0}/{cost:0}  ({LocalizedTextManager.getText("sm_tech_eta")}: {eta})";
                    AddText(content, progressText, 10, TextAnchor.UpperCenter, 8f, y, -8f, y - 16f);
                    y -= 20f;
                }
            }
            else
            {
                AddText(content, "sm_tech_no_research", 11, TextAnchor.UpperCenter, 8f, y, -8f, y - 18f);
                y -= 24f;
            }

            // 操作按钮
            y -= 4f;
            var btnRow = CreateHorizontalRow(content, 8f, y, content.rect.width - 16f, 24f, 4f);

            CreateButton(btnRow, "sm_tech_auto", () =>
            {
                // AI自动选一个研究
                var st = TechSystem.GetState(_currentKingdomId);
                if (string.IsNullOrEmpty(st.CurrentResearch))
                {
                    // 触发一次tick来选研究
                }
                Rebuild();
            }, 100f, 22f, 11);

            CreateButton(btnRow, "sm_tech_clear", () =>
            {
                var st = TechSystem.GetState(_currentKingdomId);
                st.CurrentResearch = "";
                st.ResearchProgress = 0f;
                Rebuild();
            }, 100f, 22f, 11);
        }

        // ═══════════════════════════════════════════
        // 材料页
        // ═══════════════════════════════════════════
        private static void DrawMaterials(RectTransform content)
        {
            var state = TechSystem.TryGetState(_currentKingdomId);
            var materials = TechDataLoader.AllMaterials.OrderBy(m => m.tier).ToList();

            var scroll = SuperMechUiBuilder.AddScrollView(content.gameObject, out var scrollContent);
            scroll.GetComponent<RectTransform>().offsetMin = new Vector2(4, 4);
            scroll.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -4);

            float y = -8f;
            int perRow = 4;
            float chipW = (content.rect.width - 24f) / perRow - 4f;

            for (int i = 0; i < materials.Count; i += perRow)
            {
                var row = CreateHorizontalRow(scrollContent, 8f, y, scrollContent.rect.width - 16f, 22f, 4f);
                for (int j = 0; j < perRow && i + j < materials.Count; j++)
                {
                    var mat = materials[i + j];
                    bool found = state != null && state.DiscoveredMaterials.Contains(mat.id);
                    string matName = GetMaterialDisplayName(mat.id);
                    Color c = found ? new Color(1f, 0.82f, 0.45f) : new Color(0.5f, 0.55f, 0.6f);

                    var chip = SuperMechUiBuilder.CreatePanel(row, "MatChip",
                        new Color(0.1f, 0.12f, 0.16f, 0.9f));
                    chip.GetComponent<RectTransform>().sizeDelta = new Vector2(chipW, 20f);
                    AddText(chip.GetComponent<RectTransform>(),
                        $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{matName}</color>",
                        9, TextAnchor.MiddleCenter, 2f, 0, -2f, 0);
                }
                y -= 26f;
            }

            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, Mathf.Abs(y) + 16f);
        }

        // ═══════════════════════════════════════════
        // 科技树页（节点图）
        // ═══════════════════════════════════════════
        private static void DrawTechTree(RectTransform content)
        {
            var state = TechSystem.TryGetState(_currentKingdomId);
            var techs = TechDataLoader.AllTechs.OrderBy(t => t.tier).ThenBy(t => t.branch).ToList();

            // 滚动区域
            var scroll = SuperMechUiBuilder.AddScrollView(content.gameObject, out var scrollContent);
            scroll.GetComponent<RectTransform>().offsetMin = new Vector2(2, 2);
            scroll.GetComponent<RectTransform>().offsetMax = new Vector2(-2, -2);

            // 按tier分组
            var byTier = new Dictionary<int, List<TechData.TechDef>>();
            foreach (var t in techs)
            {
                if (!byTier.ContainsKey(t.tier)) byTier[t.tier] = new List<TechData.TechDef>();
                byTier[t.tier].Add(t);
            }

            float y = -8f;
            float totalW = content.rect.width - 16f;

            foreach (var tier in byTier.Keys.OrderBy(k => k))
            {
                var tierTechs = byTier[tier];
                string eraKey = TechSystem.GetEraKey(tier);
                string eraText = LocalizedTextManager.getText(eraKey);

                // 时代标签
                AddText(scrollContent, $"<color=#7FD8EA><b>◆ {eraText} (T{tier})</b></color>",
                    11, TextAnchor.UpperLeft, 8f, y, -8f, y - 18f);
                y -= 22f;

                // 节点行（按分支排序）
                var ordered = tierTechs.OrderBy(t => BranchOrder(t.branch)).ToList();
                int cols = Mathf.Min(ordered.Count, 5);
                float nodeW = (totalW - (cols - 1) * NodeHPadding) / cols;
                nodeW = Mathf.Min(nodeW, NodeWidth + 20f);

                var row = CreateHorizontalRow(scrollContent, 8f, y, totalW, NodeHeight, NodeHPadding);

                foreach (var tech in ordered)
                {
                    bool researched = state != null && state.Researched.Contains(tech.id);
                    bool researching = state != null && state.CurrentResearch == tech.id;
                    bool available = TechSystem.CanResearch(_currentKingdomId, tech.id);
                    Color statusColor = researched ? StatusResearched : researching ? StatusResearching : available ? StatusAvailable : StatusLocked;
                    Color branchColor = GetBranchColor(tech.branch);

                    var nodeGo = SuperMechUiBuilder.CreateObject("TechNode", row, typeof(RectTransform), typeof(Image), typeof(Button));
                    var nodeRt = nodeGo.GetComponent<RectTransform>();
                    nodeRt.sizeDelta = new Vector2(nodeW, NodeHeight);
                    var nodeImg = nodeGo.GetComponent<Image>();
                    nodeImg.color = new Color(statusColor.r * 0.3f, statusColor.g * 0.3f, statusColor.b * 0.3f, 0.9f);

                    // 分支色左边条
                    var barGo = SuperMechUiBuilder.CreateObject("BranchBar", nodeGo.transform, typeof(RectTransform), typeof(Image));
                    var barRt = barGo.GetComponent<RectTransform>();
                    barRt.anchorMin = new Vector2(0, 0);
                    barRt.anchorMax = new Vector2(0, 1);
                    barRt.sizeDelta = new Vector2(3f, 0);
                    barGo.GetComponent<Image>().color = branchColor;

                    string techName = GetTechDisplayName(tech.id);
                    AddText(nodeRt, techName, 8, TextAnchor.MiddleCenter, 5f, 0, -2f, 0);

                    // 进度条（研究中）
                    if (researching && state != null)
                    {
                        float cost = tech.cost * (TechDataLoader.Data?.research?.cost_multiplier ?? 12f);
                        float prog = Mathf.Clamp01(state.ResearchProgress / cost);
                        var progBar = SuperMechUiBuilder.CreateObject("Progress", nodeGo.transform, typeof(RectTransform), typeof(Image));
                        var progRt = progBar.GetComponent<RectTransform>();
                        progRt.anchorMin = new Vector2(0, 0);
                        progRt.anchorMax = new Vector2(prog, 0);
                        progRt.sizeDelta = new Vector2(0, 2f);
                        progBar.GetComponent<Image>().color = StatusResearching;
                    }

                    // 选中高亮
                    if (_selectedTechId == tech.id)
                    {
                        nodeImg.color = new Color(statusColor.r * 0.5f, statusColor.g * 0.5f, statusColor.b * 0.5f, 1f);
                    }

                    string tid = tech.id;
                    nodeGo.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        _selectedTechId = tid;
                        Rebuild();
                    });
                }

                y -= NodeHeight + NodeVPadding + 4f;
            }

            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, Mathf.Abs(y) + 16f);
        }

        // ═══════════════════════════════════════════
        // 详情页
        // ═══════════════════════════════════════════
        private static void DrawDetail(RectTransform content)
        {
            if (string.IsNullOrEmpty(_selectedTechId))
            {
                AddText(content, "sm_tech_select_hint", 12, TextAnchor.MiddleCenter, 0, 0, 0, 0);
                return;
            }

            var tech = TechDataLoader.GetTech(_selectedTechId);
            if (tech == null)
            {
                AddText(content, "sm_tech_not_found", 12, TextAnchor.MiddleCenter, 0, 0, 0, 0);
                return;
            }

            var state = TechSystem.TryGetState(_currentKingdomId);
            bool researched = state != null && state.Researched.Contains(tech.id);
            bool researching = state != null && state.CurrentResearch == tech.id;
            bool available = TechSystem.CanResearch(_currentKingdomId, tech.id);

            float y = -8f;

            // 标题
            string techName = GetTechDisplayName(tech.id);
            Color branchColor = GetBranchColor(tech.branch);
            AddText(content, $"<color=#{ColorUtility.ToHtmlStringRGB(branchColor)}><b>{techName}</b></color>",
                15, TextAnchor.UpperCenter, 8f, y, -8f, y - 22f);
            y -= 28f;

            // 状态行
            string statusText;
            Color statusColor;
            if (researched) { statusText = LocalizedTextManager.getText("sm_tech_status_researched"); statusColor = StatusResearched; }
            else if (researching) { statusText = LocalizedTextManager.getText("sm_tech_status_researching"); statusColor = StatusResearching; }
            else if (available) { statusText = LocalizedTextManager.getText("sm_tech_status_available"); statusColor = StatusAvailable; }
            else { statusText = LocalizedTextManager.getText("sm_tech_status_locked"); statusColor = StatusLocked; }

            string branchName = GetBranchDisplayName(tech.branch);
            AddText(content, $"{branchName} · T{tech.tier} · <color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>{statusText}</color>",
                10, TextAnchor.UpperCenter, 8f, y, -8f, y - 16f);
            y -= 20f;

            // 消耗
            float cost = tech.cost * (TechDataLoader.Data?.research?.cost_multiplier ?? 12f);
            AddText(content, $"{LocalizedTextManager.getText("sm_tech_cost")}: {cost:0}", 10, TextAnchor.UpperLeft, 12f, y, -12f, y - 16f);
            y -= 20f;

            // 前置科技
            if (tech.requires_techs != null && tech.requires_techs.Count > 0)
            {
                string preText = LocalizedTextManager.getText("sm_tech_requires") + ": ";
                foreach (var pre in tech.requires_techs)
                {
                    bool has = state != null && state.Researched.Contains(pre);
                    string preName = GetTechDisplayName(pre);
                    preText += has ? $"<color=#9EF29E>✔{preName}</color> " : $"<color=#FF8A7A>✘{preName}</color> ";
                }
                AddText(content, preText, 9, TextAnchor.UpperLeft, 12f, y, -12f, y - 18f);
                y -= 22f;
            }

            // 前置材料
            if (tech.requires_materials != null && tech.requires_materials.Count > 0)
            {
                string matText = LocalizedTextManager.getText("sm_tech_requires_mat") + ": ";
                foreach (var mat in tech.requires_materials)
                {
                    bool has = state != null && state.DiscoveredMaterials.Contains(mat);
                    string matName = GetMaterialDisplayName(mat);
                    matText += has ? $"<color=#9EF29E>✔{matName}</color> " : $"<color=#FF8A7A>✘{matName}</color> ";
                }
                AddText(content, matText, 9, TextAnchor.UpperLeft, 12f, y, -12f, y - 18f);
                y -= 22f;
            }

            // 解锁效果
            var unlocks = new List<string>();
            if (tech.unlock_buildings != null && tech.unlock_buildings.Count > 0)
                unlocks.Add(LocalizedTextManager.getText("sm_tech_unlock_building") + ": " + string.Join(", ", tech.unlock_buildings));
            if (tech.unlock_knowledge != null && tech.unlock_knowledge.Count > 0)
                unlocks.Add(LocalizedTextManager.getText("sm_tech_unlock_knowledge") + ": " + tech.unlock_knowledge.Count + " " + LocalizedTextManager.getText("sm_tech_items"));
            if (tech.unlock_professions != null && tech.unlock_professions.Count > 0)
                unlocks.Add(LocalizedTextManager.getText("sm_tech_unlock_profession") + ": " + string.Join(", ", tech.unlock_professions));
            if (tech.boosts != null && tech.boosts.Count > 0)
                unlocks.Add(LocalizedTextManager.getText("sm_tech_boosts") + ": " + tech.boosts.Count + " " + LocalizedTextManager.getText("sm_tech_items"));

            if (unlocks.Count > 0)
            {
                AddText(content, LocalizedTextManager.getText("sm_tech_unlocks") + ":", 10, TextAnchor.UpperLeft, 12f, y, -12f, y - 16f);
                y -= 18f;
                foreach (var u in unlocks)
                {
                    AddText(content, "  • " + u, 9, TextAnchor.UpperLeft, 16f, y, -16f, y - 16f);
                    y -= 18f;
                }
            }

            // 操作按钮
            y -= 8f;
            var btnRow = CreateHorizontalRow(content, 8f, y, content.rect.width - 16f, 26f, 8f);

            if (!researched && !researching && available)
            {
                CreateButton(btnRow, "sm_tech_start", () =>
                {
                    TechSystem.StartResearch(_currentKingdomId, _selectedTechId);
                    Rebuild();
                }, 120f, 24f, 12);
            }
            else if (researching)
            {
                CreateButton(btnRow, "sm_tech_cancel", () =>
                {
                    var st = TechSystem.GetState(_currentKingdomId);
                    st.CurrentResearch = "";
                    st.ResearchProgress = 0f;
                    Rebuild();
                }, 120f, 24f, 12);
            }
            else if (researched)
            {
                AddText(btnRow, "sm_tech_already_done", 11, TextAnchor.MiddleCenter, 0, 0, 0, 0);
            }
            else
            {
                AddText(btnRow, "sm_tech_locked_hint", 10, TextAnchor.MiddleCenter, 0, 0, 0, 0);
            }
        }

        // ═══════════════════════════════════════════
        // 辅助方法
        // ═══════════════════════════════════════════

        private static int BranchOrder(string branch)
        {
            return branch switch
            {
                "base" => 0,
                "mech" => 1,
                "power" => 2,
                "space" => 3,
                "info" => 4,
                _ => 99
            };
        }

        private static Color GetBranchColor(string branch)
        {
            return branch switch
            {
                "base" => BranchBase,
                "mech" => BranchMech,
                "power" => BranchPower,
                "space" => BranchSpace,
                "info" => BranchInfo,
                _ => Color.gray
            };
        }

        private static string GetBranchDisplayName(string branch)
        {
            return branch switch
            {
                "base" => LocalizedTextManager.getText("sm_branch_base"),
                "mech" => LocalizedTextManager.getText("sm_branch_mech"),
                "power" => LocalizedTextManager.getText("sm_branch_power"),
                "space" => LocalizedTextManager.getText("sm_branch_space"),
                "info" => LocalizedTextManager.getText("sm_branch_info"),
                _ => branch
            };
        }

        private static string GetTechDisplayName(string techId)
        {
            // 优先从本地化取，没有则用ID
            string key = "sm_tech_" + techId;
            string localized = LocalizedTextManager.getText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key) return localized;
            return techId;
        }

        private static string GetMaterialDisplayName(string matId)
        {
            string key = "sm_mat_" + matId;
            string localized = LocalizedTextManager.getText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key) return localized;
            return matId;
        }

        private static void AddText(RectTransform parent, string text, int fontSize, TextAnchor anchor,
            float left, float top, float right, float bottom)
        {
            var go = SuperMechUiBuilder.CreateObject("Text", parent, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
            SuperMechUiBuilder.AddText(go, text, fontSize, anchor, new Color(0.9f, 0.9f, 0.9f));
        }

        private static RectTransform CreateHorizontalRow(RectTransform parent, float x, float y, float width, float height, float spacing)
        {
            var go = SuperMechUiBuilder.CreateObject("HoriRow", parent, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            return rt;
        }

        private static void CreateButton(RectTransform parent, string textKey, Action onClick, float width, float height, int fontSize)
        {
            string text = LocalizedTextManager.getText(textKey);
            var btn = SuperMechUiBuilder.CreateButton(parent, "Btn", text, onClick, width, height, fontSize);
        }

        private static void CreateProgressBar(RectTransform parent, float x, float y, float width, float height, float progress, Color fillColor)
        {
            var trackGo = SuperMechUiBuilder.CreateObject("ProgressTrack", parent, typeof(RectTransform), typeof(Image));
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0, 1);
            trackRt.anchorMax = new Vector2(0, 1);
            trackRt.pivot = new Vector2(0, 1);
            trackRt.anchoredPosition = new Vector2(x, y);
            trackRt.sizeDelta = new Vector2(width, height);
            trackGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var fillGo = SuperMechUiBuilder.CreateObject("Fill", trackGo.transform, typeof(RectTransform), typeof(Image));
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = fillColor;
        }
    }
}
