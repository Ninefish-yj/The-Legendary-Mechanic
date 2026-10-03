using System;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 事件总线：解耦系统间直接调用。发布者只发事件，订阅者按需监听。
    /// 用法：EventBus.Subscribe("ActorDied", data => { ... });
    ///       EventBus.Publish("ActorDied", new ActorDiedEvent { actor = a });
    /// </summary>
    public static class SuperMechEventBus
    {
        private static readonly Dictionary<string, List<Delegate>> _handlers = new Dictionary<string, List<Delegate>>();

        /// <summary>订阅事件，返回订阅ID用于取消</summary>
        public static int Subscribe<T>(string eventName, Action<T> handler)
        {
            if (!_handlers.TryGetValue(eventName, out var list))
            {
                list = new List<Delegate>();
                _handlers[eventName] = list;
            }
            list.Add(handler);
            return handler.GetHashCode();
        }

        /// <summary>订阅无参数事件</summary>
        public static int Subscribe(string eventName, Action handler)
        {
            if (!_handlers.TryGetValue(eventName, out var list))
            {
                list = new List<Delegate>();
                _handlers[eventName] = list;
            }
            list.Add(handler);
            return handler.GetHashCode();
        }

        /// <summary>发布事件</summary>
        public static void Publish<T>(string eventName, T data)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (list[i] is Action<T> typed) typed.Invoke(data);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[超神机械师] 事件总线异常 [{eventName}]: {e.Message}");
                }
            }
        }

        /// <summary>发布无参数事件</summary>
        public static void Publish(string eventName)
        {
            if (!_handlers.TryGetValue(eventName, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (list[i] is Action typed) typed.Invoke();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[超神机械师] 事件总线异常 [{eventName}]: {e.Message}");
                }
            }
        }

        /// <summary>清空所有订阅（世界重置时调用）</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    // === 常用事件数据结构 ===
    public struct ActorRankChangedEvent
    {
        public Actor actor;
        public int oldRank;
        public int newRank;
    }

    public struct ActorDiedEvent
    {
        public Actor actor;
        public AttackType cause;
    }

    public struct SanctuaryEnteredEvent
    {
        public Actor actor;
        public int sanctuaryIndex;
    }

    public struct FactionCreatedEvent
    {
        public Actor leader;
        public string factionName;
    }
}
