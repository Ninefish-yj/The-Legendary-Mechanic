using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 性能监控：记录各子系统Tick耗时，帮助定位性能瓶颈
    /// 轻量级，只在开启配置时记录，默认关闭不影响性能
    /// </summary>
    public static class SuperMechPerformanceMonitor
    {
        private static readonly Dictionary<string, System.TimeSpan> _totalTime = new Dictionary<string, System.TimeSpan>();
        private static readonly Dictionary<string, int> _callCount = new Dictionary<string, int>();
        private static readonly Stopwatch _sw = new Stopwatch();
        private static string _currentName;
        private static float _lastLogTime = 0f;

        /// <summary>开始计时（配合End使用）</summary>
        public static void Begin(string name)
        {
            if (!SuperMechConfig.EnableProfiler) return;
            _currentName = name;
            _sw.Restart();
        }

        /// <summary>结束计时并记录</summary>
        public static void End()
        {
            if (!SuperMechConfig.EnableProfiler || _currentName == null) return;
            _sw.Stop();
            if (!_totalTime.ContainsKey(_currentName))
            {
                _totalTime[_currentName] = System.TimeSpan.Zero;
                _callCount[_currentName] = 0;
            }
            _totalTime[_currentName] += _sw.Elapsed;
            _callCount[_currentName]++;
            _currentName = null;
        }

        /// <summary>每60秒输出一次性能报告</summary>
        public static void Tick()
        {
            if (!SuperMechConfig.EnableProfiler) return;
            float now = Time.time;
            if (now - _lastLogTime < 60f) return;
            _lastLogTime = now;
            LogReport();
        }

        /// <summary>输出性能报告（按总耗时降序）</summary>
        public static void LogReport()
        {
            if (_totalTime.Count == 0) return;
            var list = new List<(string name, System.TimeSpan total, int count)>(_totalTime.Count);
            foreach (var kv in _totalTime)
                list.Add((kv.Key, kv.Value, _callCount[kv.Key]));
            list.Sort((a, b) => b.total.CompareTo(a.total));

            Debug.Log("[超神机械师] 性能报告（60秒累计）:");
            foreach (var (name, total, count) in list)
            {
                double avgMs = total.TotalMilliseconds / count;
                Debug.Log($"  {name,-30} 总耗时:{total.TotalMilliseconds,8:F1}ms  次数:{count,5}  平均:{avgMs,6:F2}ms");
            }
            _totalTime.Clear();
            _callCount.Clear();
        }

        /// <summary>获取性能报告文本（用于UI显示）</summary>
        public static string GetReportText()
        {
            if (_totalTime.Count == 0) return "无数据";
            var list = new List<(string name, System.TimeSpan total, int count)>(_totalTime.Count);
            foreach (var kv in _totalTime)
                list.Add((kv.Key, kv.Value, _callCount[kv.Key]));
            list.Sort((a, b) => b.total.CompareTo(a.total));
            var sb = new System.Text.StringBuilder();
            foreach (var (name, total, count) in list)
            {
                double avgMs = total.TotalMilliseconds / count;
                sb.AppendLine($"{name}: {avgMs:F2}ms/次 ({count}次)");
            }
            return sb.ToString();
        }
    }
}
