using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperMech.Code
{
    /// <summary>单位面板折叠区块组件（v0.40.0修复：用IPointerClickHandler替代Button.onClick）
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
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 重建面板失败: {e.Message}");
            }
        }

        /// <summary>构建折叠标题行
        /// 用自定义IPointerClickHandler组件处理点击，不依赖Button.onClick（对象池复用会导致监听器累积）
        /// </summary>
        public static void BuildHeader(StatsWindow window, long actorId, string sectionId, string titleKey)
        {
            bool expanded = IsExpanded(actorId, sectionId);
            string arrow = expanded ? "▼" : "▶";
            string title = $"{arrow} {LocalizedTextManager.getText(titleKey)}";

            var row = window.showStatRow(title, "", null, MetaType.None, -1L,
                pColorText: false, pIconPath: null, pTooltipId: null, pTooltipData: null, pLocalize: false);

            if (row != null && row.value != null)
            {
                // 用自定义组件处理点击，挂在row.value上（那里有Graphic能接收点击）
                var header = row.value.GetComponent<SMCollapsibleHeader>();
                if (header == null)
                    header = row.value.gameObject.AddComponent<SMCollapsibleHeader>();
                header.Init(actorId, sectionId, window);
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

    /// <summary>折叠标题行点击组件（挂在row.value上，实现IPointerClickHandler）
    /// 不依赖Button.onClick，避免对象池复用导致监听器累积和双重触发
    /// </summary>
    public class SMCollapsibleHeader : MonoBehaviour, IPointerClickHandler
    {
        private long _actorId;
        private string _sectionId;
        private StatsWindow _window;

        public void Init(long actorId, string sectionId, StatsWindow window)
        {
            _actorId = actorId;
            _sectionId = sectionId;
            _window = window;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SMCollapsibleSection.Toggle(_actorId, _sectionId);
            SMCollapsibleSection.RebuildPanel(_window);
        }
    }

    /// <summary>通用行点击组件（用于入口按钮等，挂在row.value上）
    /// 不依赖Button.onClick，避免对象池复用导致监听器累积
    /// </summary>
    public class SMRowClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private System.Action _onClick;

        public void Init(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke();
        }
    }
}
