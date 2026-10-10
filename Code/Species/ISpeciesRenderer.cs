using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族美术渲染接口 - 预留，后续补美术资源时实现
    /// 当前所有种族使用人类精灵图占位
    /// </summary>
    public interface ISpeciesRenderer
    {
        /// <summary>获取种族行走动画帧</summary>
        Sprite[] GetWalkFrames(string speciesId);

        /// <summary>获取种族游泳动画帧</summary>
        Sprite[] GetSwimFrames(string speciesId);

        /// <summary>获取种族头像预制体路径</summary>
        string GetAvatarPrefab(string speciesId);

        /// <summary>获取种族图标</summary>
        string GetIcon(string speciesId);

        /// <summary>获取种族体型（ActorSize枚举名）</summary>
        string GetActorSize(string speciesId);

        /// <summary>是否有自定义美术资源</summary>
        bool HasCustomArt(string speciesId);
    }

    /// <summary>
    /// 默认渲染器 - 使用人类精灵图占位
    /// 后续替换为具体美术资源时，新建SpeciesRenderer实现此接口
    /// </summary>
    public class DefaultSpeciesRenderer : ISpeciesRenderer
    {
        public Sprite[] GetWalkFrames(string speciesId) => null;
        public Sprite[] GetSwimFrames(string speciesId) => null;
        public string GetAvatarPrefab(string speciesId) => "";
        public string GetIcon(string speciesId) => "iconQuestionMark";
        public string GetActorSize(string speciesId) => "S13_Human";
        public bool HasCustomArt(string speciesId) => false;
    }
}
