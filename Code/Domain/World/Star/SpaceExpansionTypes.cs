using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    // ═══════════════════════════════════════════════════════════
    // v0.231 宇宙深化：统一数据结构
    // 参考：无人深空(星球探索)、EVE(贸易)、X4(太空站)、战锤40K(科技树)
    // ═══════════════════════════════════════════════════════════

    #region 星球深化（无人深空式）

    /// <summary>星球气候类型</summary>
    public enum PlanetClimate
    {
        Temperate,    // 温带（宜居）
        Desert,       // 沙漠
        Ice,          // 冰冻
        Volcanic,     // 火山
        Ocean,        // 海洋
        Gas,          // 气态
        Toxic,        // 剧毒
        Radiated,     // 辐射
        Storm,        // 风暴
        Paradise      // 天堂（稀有）
    }

    /// <summary>星球资源类型（决定特产）</summary>
    public enum PlanetResourceType
    {
        Mineral,      // 矿物
        Energy,       // 能源
        Food,         // 食物
        Tech,         // 科技材料
        Luxury,       // 奢侈品
        Biological    // 生物材料
    }

    /// <summary>生物群系（决定地表特征和生物）</summary>
    public enum PlanetBiome
    {
        Forest,       // 森林
        Grassland,    // 草原
        Mountain,     // 山地
        Swamp,        // 沼泽
        Wasteland,    // 荒地
        Coral,        // 珊瑚礁
        Crystal,      // 晶体洞
        Lava,         // 熔岩
        Tundra,       // 冻原
        Floating      // 浮空岛
    }

    /// <summary>探索事件类型</summary>
    public enum ExplorationEventType
    {
        Ruins,        // 发现古代遗迹
        RareResource, // 稀有资源矿脉
        AlienLife,    // 外星生物
        Danger,       // 危险事件
        Treasure,     // 宝藏
        Signal,       // 神秘信号
        Anomaly,      // 空间异常
        Nothing       // 一无所获
    }

    /// <summary>探索事件结果</summary>
    [Serializable]
    public class ExplorationEvent
    {
        public ExplorationEventType type;
        public string title;
        public string description;
        public int resourceReward;
        public int expReward;
        public bool isDangerous;
        public int dangerDamage;
    }

    #endregion

    #region 星际贸易（EVE/精英式）

    /// <summary>贸易商品类型</summary>
    public enum TradeGoodType
    {
        Ore,          // 矿石
        Fuel,         // 燃料
        Food,         // 食物
        TechParts,    // 科技零件
        Luxury,       // 奢侈品
        Medicine,     // 药品
        Weapons,      // 武器
        Slaves        // 奴隶（黑市）
    }

    /// <summary>商品数据</summary>
    [Serializable]
    public class TradeGood
    {
        public TradeGoodType type;
        public string name;
        public int basePrice;
        public int volume; // 每单位体积
    }

    /// <summary>星球市场数据</summary>
    [Serializable]
    public class PlanetMarket
    {
        public string planetId;
        public Dictionary<TradeGoodType, int> buyPrice = new Dictionary<TradeGoodType, int>();  // 买入价（玩家卖给星球）
        public Dictionary<TradeGoodType, int> sellPrice = new Dictionary<TradeGoodType, int>(); // 卖出价（玩家从星球买）
        public Dictionary<TradeGoodType, int> stock = new Dictionary<TradeGoodType, int>();     // 库存
        public Dictionary<TradeGoodType, int> demand = new Dictionary<TradeGoodType, int>();    // 需求度 0-100
    }

    /// <summary>贸易路线</summary>
    [Serializable]
    public class TradeRoute
    {
        public string id;
        public string fromPlanet;
        public string toPlanet;
        public TradeGoodType goodType;
        public int profitPerUnit;
        public int distance;
        public bool isActive;
    }

    #endregion

    #region 太空站（X4式）

    /// <summary>太空站类型</summary>
    public enum StationType
    {
        Mining,       // 采矿站（自动采集矿物）
        Trading,      // 贸易站（降低贸易成本）
        Shipyard,     // 造船厂（加速舰船建造）
        Defense,      // 防御站（轨道防御）
        Research,     // 研究站（加速科技研究）
        Habitat       // 居住站（增加人口上限）
    }

    /// <summary>太空站数据</summary>
    [Serializable]
    public class SpaceStation
    {
        public string id;
        public string name;
        public StationType type;
        public string orbitPlanet;
        public int level; // 1-5
        public int hp;
        public int maxHp;
        public float productionRate; // 产出倍率
        public bool isPlayerOwned;
    }

    #endregion

    #region 阵营科技树（战锤式）

    /// <summary>科技领域</summary>
    public enum TechField
    {
        Energy,       // 能源（+资源产出）
        Weapons,      // 武器（+舰队攻击）
        Shields,      // 护盾（+舰队防御）
        Engines,      // 引擎（+航行速度）
        Biology,      // 生物（+殖民地成长）
        Industry      // 工业（+建造速度）
    }

    /// <summary>科技节点</summary>
    [Serializable]
    public class TechNode
    {
        public string id;
        public string name;
        public string description;
        public TechField field;
        public int tier; // 1-5
        public int cost;
        public int researchTime; // 秒
        public List<string> prerequisites = new List<string>();
        public bool isResearched;
        public Dictionary<string, float> effects = new Dictionary<string, float>();
    }

    #endregion

    #region 扩展星球数据

    /// <summary>v0.231 星球扩展数据（挂在原有PlanetData上）</summary>
    [Serializable]
    public class PlanetExpansionData
    {
        public string planetId;
        public PlanetClimate climate;
        public PlanetResourceType resourceType;
        public PlanetBiome biome;
        public int dangerLevel; // 0-10
        public bool isExplored; // 是否已登陆探索
        public int explorationCount;
        public PlanetMarket market;
        public List<SpaceStation> stations = new List<SpaceStation>();
    }

    #endregion
}
