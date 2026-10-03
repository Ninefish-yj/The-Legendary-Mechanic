using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>单位面板折叠区块组件（v0.27.0 UI重构第二阶段）
    /// 提供折叠状态管理和标题行构建，点击标题行切换展开/折叠
    /// </summary>
    public static class SMCollapsibleSection
    {
        /// <summary>折叠状态缓存：actorId -> sectionId -> isExpanded</summary>
        private static readonly Dictionary<long, Dictionary<string, bool>> _expandState =
            new Dictionary<long, Dictionary<string, bool>>();

        /// <summary>默认展开的sectionId集合</summary>
        private static readonly HashSet<string> _defaultExpanded = new HashSet<string>
        {
            "core",   // 核心信息默认展开
        };

        /// <summary>showStatsRows方法缓存（反射调用）</summary>
        private static MethodInfo _showStatsRowsMethod;

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

        /// <summary>通过反射调用StatsWindow.showStatsRows()重建面板</summary>
        public static void RebuildPanel(StatsWindow window)
        {
            if (window == null) return;
            try
            {
                if (_showStatsRowsMethod == null)
                {
                    _showStatsRowsMethod = typeof(StatsWindow).GetMethod("showStatsRows",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                if (_showStatsRowsMethod != null)
                {
                    _showStatsRowsMethod.Invoke(window, null);
                }
                // showStatsRows是internal方法，反射必须能找到；找不到则无法重建
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 重建面板失败: {e.Message}");
            }
        }

        /// <summary>构建折叠标题行
        /// </summary>
        /// <param name="window">StatsWindow实例，用于调用showStatRow</param>
        /// <param name="actorId">单位ID</param>
        /// <param name="sectionId">区块ID</param>
        /// <param name="titleKey">标题本地化key</param>
        public static void BuildHeader(StatsWindow window, long actorId, string sectionId, string titleKey)
        {
            bool expanded = IsExpanded(actorId, sectionId);
            string arrow = expanded ? "▼" : "▶";
            string title = $"{arrow} {LocalizedTextManager.getText(titleKey)}";

            var row = window.showStatRow(title, "", null, MetaType.None, -1L,
                pColorText: false, pIconPath: null, pTooltipId: null, pTooltipData: null, pLocalize: false);

            if (row != null)
            {
                // 用on_click_value设置点击事件（原生支持，每次showStatsRows都会重新设置）
                row.on_click_value = () =>
                {
                    Toggle(actorId, sectionId);
                    RebuildPanel(window);
                };

                // 同时给Button组件添加持久监听器（on_click_value会被OnDisable清空，这是备份）
                var btn = row.value.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    // 先移除旧的监听器（避免重复添加），用私有字段无法直接移除，所以只添加一次
                    // 实际上每次showStatsRows都会创建新的row对象，所以不会重复
                    btn.onClick.AddListener(() =>
                    {
                        Toggle(actorId, sectionId);
                        RebuildPanel(window);
                    });
                }
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
