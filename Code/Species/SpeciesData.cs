using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族数据模型 - 定义一个自定义种族的所有属性
    /// 美术资源通过ISpeciesRenderer接口抽象，当前用人类占位
    /// </summary>
    public class SpeciesData
    {
        /// <summary>种族唯一ID（如human_cosmic）</summary>
        public string Id;

        /// <summary>种族显示名（中文，通过LocalizedTextManager.add注册）</summary>
        public string Name;

        /// <summary>种族描述</summary>
        public string Description;

        /// <summary>野生王国ID（对应ActorAsset.kingdom_id_wild）</summary>
        public string WildKingdomId;

        /// <summary>文明王国ID（对应ActorAsset.kingdom_id_civilization）</summary>
        public string CivKingdomId;

        /// <summary>是否为特殊生物（不自然生成，需事件投放）</summary>
        public bool IsSpecial;

        /// <summary>体型（对应ActorSize枚举，默认S13_Human）</summary>
        public string ActorSize = "S13_Human";

        /// <summary>基础属性加成（key=属性名，value=加成值）</summary>
        public Dictionary<string, float> BaseStats = new Dictionary<string, float>();

        /// <summary>初始特质列表</summary>
        public List<string> Traits = new List<string>();

        /// <summary>可进化的目标种族ID列表</summary>
        public List<string> EvolutionTargets = new List<string>();

        /// <summary>进化所需条件描述</summary>
        public string EvolutionCondition;

        /// <summary>种族天赋（专属能力描述）</summary>
        public string RacialTalent;

        /// <summary>优先级（P0/P1/P2/P3）</summary>
        public string Priority;

        /// <summary>原著依据章节</summary>
        public string NovelReference;

        /// <summary>是否已注册到WB的ActorAssetLibrary</summary>
        public bool IsRegistered;

        public SpeciesData(string id, string name, string priority = "P0")
        {
            Id = id;
            Name = name;
            Priority = priority;
        }
    }
}
