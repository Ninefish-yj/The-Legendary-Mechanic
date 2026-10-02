using System.Collections.Generic;
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

        /// <summary>构建折叠标题行
        /// </summary>
        /// <param name="window">StatsWindow实例，用于调用showStatRow</param>
        /// <param name="actorId">单位ID</param>
        /// <param name="sectionId">区块ID</param>
        /// <param name="titleKey">标题本地化key</param>
        /// <param name="rebuildAction">点击后重建面板的回调</param>
        public static void BuildHeader(StatsWindow window, long actorId, string sectionId,
            string titleKey, System.Action rebuildAction)
        {
            bool expanded = IsExpanded(actorId, sectionId);
            string arrow = expanded ? "▼" : "▶";
            string title = $"{arrow} {LocalizedTextManager.getText(titleKey)}";

            var row = window.showStatRow(title, "", null, MetaType.None, -1L,
                pColorText: false, pIconPath: null, pTooltipId: null, pTooltipData: null, pLocalize: false);

            if (row != null)
            {
                row.on_click_value = () =>
                {
                    Toggle(actorId, sectionId);
                    rebuildAction?.Invoke();
                };
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
