namespace SuperMech.Code
{
    /// <summary>
    /// 种族美术渲染接口（预留）
    /// 当前所有种族用人类精灵图占位，后续补美术时实现此接口
    /// 设计原则：种族数据与美术完全解耦，补美术时只改Renderer不碰逻辑
    /// </summary>
    public interface ISpeciesRenderer
    {
        /// <summary>种族ID</summary>
        string SpeciesId { get; }

        /// <summary>纹理资源根路径（如GameResources/actors/human_cosmic/）</summary>
        string TexturePath { get; }

        /// <summary>待机动画帧列表</summary>
        string[] IdleFrames { get; }

        /// <summary>行走动画帧列表</summary>
        string[] WalkFrames { get; }

        /// <summary>游泳动画帧列表</summary>
        string[] SwimFrames { get; }

        /// <summary>头像/图标Sprite</summary>
        string IconPath { get; }

        /// <summary>是否已加载美术资源（false=用人类占位）</summary>
        bool HasCustomArt { get; }
    }

    /// <summary>
    /// 默认渲染器：用人类精灵图占位
    /// 所有未实现自定义美术的种族都走这个
    /// </summary>
    public class DefaultSpeciesRenderer : ISpeciesRenderer
    {
        public string SpeciesId { get; private set; }
        public string TexturePath => "";  // 空路径=用人类默认纹理
        public string[] IdleFrames => null;
        public string[] WalkFrames => null;
        public string[] SwimFrames => null;
        public string IconPath => "";
        public bool HasCustomArt => false;

        public DefaultSpeciesRenderer(string speciesId)
        {
            SpeciesId = speciesId;
        }
    }
}
