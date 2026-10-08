using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 系统注册表：统一管理所有IRegisterable和ITickable系统
    /// 遵循开闭原则：新增系统只需注册，不需要修改Main.cs
    /// 替代原来Main.cs中32个Register()和48个Tick()的手动调用
    /// </summary>
    public static class SystemRegistry
    {
        private static readonly List<IRegisterable> _registerables = new List<IRegisterable>();
        private static readonly List<ITickable> _tickables = new List<ITickable>();
        private static bool _registered = false;

        /// <summary>注册系统（同时加入Register和Tick列表）</summary>
        public static void Register<T>(T system) where T : IRegisterable
        {
            if (system == null) return;
            if (!_registerables.Contains(system))
            {
                _registerables.Add(system);
            }
            if (system is ITickable tickable && !_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
            }
        }

        /// <summary>只注册Tickable（不需要Register的系统）</summary>
        public static void RegisterTickable(ITickable tickable)
        {
            if (tickable != null && !_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
            }
        }

        /// <summary>执行所有系统的Register（模组加载时调用一次）</summary>
        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            foreach (var r in _registerables)
            {
                try { r.Register(); }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"[超神机械师] 系统注册失败({r.GetType().Name}): {e.Message}");
                }
            }
        }

        /// <summary>执行所有系统的Tick（统一调度）</summary>
        public static void TickAll()
        {
            foreach (var t in _tickables)
            {
                try { t.Tick(); }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"[超神机械师] 系统Tick异常({t.GetType().Name}): {e.Message}");
                }
            }
        }

        /// <summary>清空所有注册（世界切换时调用）</summary>
        public static void Clear()
        {
            // 不清空_registerables（系统是静态单例，跨世界保留）
            // 只清空数据存储，由各系统自己的Clear()处理
        }
    }
}
