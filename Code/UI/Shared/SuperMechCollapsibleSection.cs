using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>单位面板折叠区块组件
    /// 提供折叠状态管理和标题行构建，点击标题行切换展开/折叠
    /// </summary>
    public static class SuperMechCollapsibleSection
    {
        /// <summary>折叠状态缓存：actorId -> sectionId -> isExpanded</summary>
        private static readonly Dictionary<long, Dictionary<string, bool>> _expandState =
            new Dictionary<long, Dictionary<string, bool>>();

        /// <summary>默认展开的sectionId集合</summary>
        private static readonly HashSet<string> _defaultExpanded = new HashSet<string>
        {
            "core",   // 核心信息默认展开
        };

        /// <summary>检查某个区块是否展开</summary>
        public static bool IsExpanded(long actorId, string sectionId)
        {
            if (_expandState.TryGetValue(actorId, out var sections))
            {
                if (sections.TryGetValue(sectionId, out bool expanded))
                    return expanded;
            }
            return _defaultExpanded.Contains(sectionId);
        }

        /// <summary>切换折叠状态</summary>
        public static void Toggle(long actorId, string sectionId)
        {
            if (!_expandState.TryGetValue(actorId, out var sections))
            {
                sections = new Dictionary<string, bool>();
                _expandState[actorId] = sections;
            }
            bool current = IsExpanded(actorId, sectionId);
            sections[sectionId] = !current;
        }

        /// <summary>重建面板
        /// 正确方式：调用StatsWindow.updateStatsRows()（internal方法），
        /// 它会禁用再启用StatsRowsContainer，触发OnDisable->OnEnable->showStatsRows()->协程激活行。
        /// 不能直接调用showStatsRows()，因为它只创建行但不激活行（SetActive(false)），
        /// 新行不会显示在面板上。
        /// </summary>
        public static void RebuildPanel(StatsWindow window)
        {
            if (window == null) return;
            try
            {
                // 方式1：反射调用 updateStatsRows() 方法
                var method = window.GetType().GetMethod("updateStatsRows",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(window, null);
                    return;
                }
                // 方式2：通过属性获取 stats_rows_container（它是property不是field）
                var prop = window.GetType().GetProperty("stats_rows_container",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (prop != null && prop.GetValue(window) is StatsRowsContainer container)
                {
                    container.gameObject.SetActive(false);
                    container.gameObject.SetActive(true);
                    return;
                }
                // 方式3：通过Transform查找
                var contentStats = window.transform.FindRecursive("content_stats");
                if (contentStats != null)
                {
                    contentStats.gameObject.SetActive(false);
                    contentStats.gameObject.SetActive(true);
                    return;
                }
                // 方式4：直接禁用再启用窗口GameObject（最暴力但最可靠）
                window.gameObject.SetActive(false);
                window.gameObject.SetActive(true);
                Debug.Log("[超神机械师] 重建面板: 使用方式4(禁用再启用窗口)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 重建面板异常: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>构建折叠标题行
        /// 手机端点击修复：
        /// 1. value必须非空——KeyValueField的Button绑定在value文本上，空字符串=零宽度=点不到
        /// 2. 手机端原生行为：第一次点击触发on_hover_value（选中），第二次点击触发on_click_value
        ///    所以同时给on_hover_value绑定Toggle（仅手机端生效），实现一次点击即切换
        /// 3. PC端on_hover_value是鼠标悬停，不触发Toggle（通过InputHelpers.mouseSupported判断）
        /// </summary>
        public static void BuildHeader(StatsWindow window, long actorId, string sectionId, string titleKey)
        {
            try
            {
                bool expanded = IsExpanded(actorId, sectionId);
                string arrow = expanded ? "▼" : "▶";
                string title = $"{arrow} {LocalizedTextManager.getText(titleKey)}";
                // value非空确保点击区域有宽度（手机端关键）
                string valueText = expanded ? LocalizedTextManager.getText("sm_ui_collapse") : LocalizedTextManager.getText("sm_ui_expand");

                var row = window.showStatRow(title, valueText, "#ffd700", MetaType.None, -1L,
                    pColorText: false, pIconPath: null, pTooltipId: null, pTooltipData: null, pLocalize: false);

                if (row != null)
                {
                    UnityEngine.Events.UnityAction toggleAction = () =>
                    {
                        Toggle(actorId, sectionId);
                        RebuildPanel(window);
                    };
                    // PC端点击 / 手机端第二次点击
                    row.on_click_value = toggleAction;
                    // 手机端第一次点击触发hover——仅在非鼠标平台执行toggle，PC端悬停不触发
                    row.on_hover_value = () =>
                    {
                        if (!InputHelpers.mouseSupported)
                            toggleAction();
                    };
                }
                else
                {
                    Debug.LogWarning($"[超神机械师] 折叠标题行创建失败: section={sectionId}, actor={actorId}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] BuildHeader异常(section={sectionId}): {e.Message}");
            }
        }

        /// <summary>清理指定单位的折叠状态（单位死亡时调用）</summary>
        public static void ClearActor(long actorId)
        {
            _expandState.Remove(actorId);
        }

        /// <summary>清理所有折叠状态（切换世界时调用）</summary>
        public static void ClearAll()
        {
            _expandState.Clear();
        }
    }
}
