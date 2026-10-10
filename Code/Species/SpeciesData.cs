using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族数据模型：定义自定义生物的基础属性
    /// 对照原著：Ⅰ型宇宙人族/卢翰兽人/精灵/矮人/虫族/银灵人/机械族/虚空龙族/星游兽/黑星族
    /// 美术资源通过ISpeciesRenderer接口抽象，当前用人类精灵图占位，后续替换
    /// </summary>
    public class SpeciesDef
    {
        public string id;                  // 种族ID（如sm_species_human_cosmic）
        public string nameKey;             // 本地化key（名称）
        public string descKey;             // 本地化key（描述）
        public string templateId;          // WB克隆模板（human/animal/mob）
        public string texturePath;         // 美术资源路径（预留，当前为空用人类占位）
        public ActorSize actorSize;        // 体型
        public float scale;                // 缩放（普通人形约0.1）
        public bool isCivilized;           // 是否可建文明
        public bool canReproduce;          // 是否可繁殖
        public bool hasSoul;               // 是否有灵魂
        public bool flying;                // 是否飞行
        public string colorHex;            // 主题色
        public Dictionary<string, float> baseStats;  // 基础属性覆盖
        public List<string> defaultTraits; // 默认特质
        public List<string> defaultSkills; // 默认技能
        public int rarity;                 // 稀有度（0普通/1稀有/2史诗/3传说）
        public string evolutionFrom;       // 进化来源种族ID（null=基础种族）
        public string evolutionTo;         // 进化目标种族ID（null=最终形态）
    }

    /// <summary>体型枚举（与WB ActorSize对齐）</summary>
    public enum ActorSize
    {
        S0_Tiny = 0,
        S5_Small = 5,
        S10_Medium = 10,
        S13_Human = 13,
        S15_Large = 15,
        S17_Dragon = 17,
        S20_Giant = 20
    }
}
