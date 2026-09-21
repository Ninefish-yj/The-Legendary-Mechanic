using NeoModLoader.api;

namespace SuperMech
{
    /// <summary>
    /// 《超神机械师》WorldBox 模组入口（作者：阿鱼要吃书 · 原著：齐佩甲）
    /// Phase 0：项目骨架 + 编译链验证
    /// </summary>
    public class Main : BasicMod<Main>
    {
        public new static Main Instance { get; private set; }

        protected override void OnModLoad()
        {
            Instance = this;
            LogInfo("[超神机械师] 模组加载成功（Phase 0 骨架）");
            SuperMechSystems.Bootstrap();
            LogInfo("[超神机械师] 系统引导完成，等待 Phase 1 内容接入");
        }
    }

    /// <summary>
    /// 模组系统引导层：后续各 Phase 的系统（职业/特性/神权/事件/圣所/轮回）在此注册。
    /// </summary>
    public static class SuperMechSystems
    {
        public static void Bootstrap()
        {
            // Phase 1 接入点：力量体系（五系职业 + 阶位特质 + 专长/技能 + 机械工厂）
            // Phase 2 接入点：降临者（异人单位）+ 版本 1.0-3.0 灾难链 + 势力王国
            // Phase 3 接入点：4.0-5.5（闪耀世界 / 圣所复苏 / 全境战争 / 超A级协会）
            // Phase 4 接入点：无尽轮回 + 圣所跨存档持久层
        }
    }
}