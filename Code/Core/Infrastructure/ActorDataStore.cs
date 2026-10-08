using System.Collections.Generic;

namespace SuperMech.Code.Core.Infrastructure
{
    /// <summary>
    /// 基于Actor的泛型数据存储基类
    /// 抽象128处重复的Dictionary<long, T>模式
    /// 遵循DRY原则：所有按单位ID存储数据的系统继承此类
    /// </summary>
    public abstract class ActorDataStore<T> : IDataStore<T>
    {
        protected readonly Dictionary<long, T> _data = new Dictionary<long, T>();

        /// <summary>获取单位数据，不存在返回默认值</summary>
        public virtual T Get(long actorId)
        {
            T value;
            _data.TryGetValue(actorId, out value);
            return value;
        }

        /// <summary>设置单位数据</summary>
        public virtual void Set(long actorId, T value)
        {
            _data[actorId] = value;
        }

        /// <summary>检查单位是否有数据</summary>
        public virtual bool Has(long actorId)
        {
            return _data.ContainsKey(actorId);
        }

        /// <summary>移除单位数据（单位死亡时调用）</summary>
        public virtual void Remove(long actorId)
        {
            _data.Remove(actorId);
        }

        /// <summary>清空所有数据（世界切换时调用）</summary>
        public virtual void Clear()
        {
            _data.Clear();
        }

        /// <summary>获取所有数据（用于存档序列化）</summary>
        public IReadOnlyDictionary<long, T> GetAll()
        {
            return _data;
        }

        /// <summary>批量设置数据（用于读档反序列化）</summary>
        public void LoadAll(Dictionary<long, T> data)
        {
            _data.Clear();
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    _data[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    /// <summary>
    /// 基于Actor的HashSet存储基类
    /// 用于只需要标记"有/无"的场景（如修炼状态、觉醒标记）
    /// </summary>
    public abstract class ActorFlagStore
    {
        protected readonly HashSet<long> _flags = new HashSet<long>();

        public virtual bool Has(long actorId) => _flags.Contains(actorId);
        public virtual void Add(long actorId) => _flags.Add(actorId);
        public virtual void Remove(long actorId) => _flags.Remove(actorId);
        public virtual void Clear() => _flags.Clear();
        public HashSet<long> GetAll() => new HashSet<long>(_flags);
        public void LoadAll(HashSet<long> flags)
        {
            _flags.Clear();
            if (flags != null) _flags.UnionWith(flags);
        }
    }
}
