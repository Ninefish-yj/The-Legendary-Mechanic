namespace SuperMech.Code
{
    /// <summary>
    /// 可注册系统接口：所有需要在模组加载时初始化的系统实现此接口
    /// 遵循单一职责：每个系统只负责自己的注册逻辑
    /// </summary>
    public interface IRegisterable
    {
        /// <summary>注册系统资源（特质/属性/本地化文本等）</summary>
        void Register();
    }

    /// <summary>
    /// 可更新系统接口：所有需要每帧/定时更新的系统实现此接口
    /// 遵循开闭原则：新增系统不需要修改UnifiedTick
    /// </summary>
    public interface ITickable
    {
        /// <summary>定时更新（由UnifiedTick统一调度）</summary>
        void Tick();
    }

    /// <summary>
    /// 数据存储接口：统一按单位ID存储数据的模式
    /// 遵循依赖倒置：业务逻辑依赖接口，不依赖具体Dictionary实现
    /// </summary>
    public interface IDataStore<T>
    {
        /// <summary>获取单位数据，不存在返回默认值</summary>
        T Get(long actorId);

        /// <summary>设置单位数据</summary>
        void Set(long actorId, T value);

        /// <summary>检查单位是否有数据</summary>
        bool Has(long actorId);

        /// <summary>移除单位数据（单位死亡时调用）</summary>
        void Remove(long actorId);

        /// <summary>清空所有数据（世界切换时调用）</summary>
        void Clear();
    }

    /// <summary>
    /// 可序列化接口：支持存档/读档的系统实现此接口
    /// 遵循接口隔离：只有需要存档的系统才实现
    /// </summary>
    public interface ISerializable<TData>
    {
        /// <summary>序列化为存档数据</summary>
        TData Serialize();

        /// <summary>从存档数据反序列化</summary>
        void Deserialize(TData data);
    }
}
