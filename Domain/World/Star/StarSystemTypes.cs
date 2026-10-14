using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>行星类型（岩石/气态/冰/熔岩/海洋/沙漠/森林/恒星）</summary>
    public enum PlanetType { Rocky, Gas, Ice, Lava, Ocean, Desert, Forest, Star }

    /// <summary>恒星光谱类型（天体物理学分类）</summary>
    public enum StarType
    {
        RedDwarf,    // 红矮星 M型：温度低，寿命极长，最常见
        YellowDwarf, // 黄矮星 G型：类似太阳，温度适中
        BlueGiant,   // 蓝巨星 O/B型：温度极高，质量大，寿命短
        RedGiant,    // 红巨星：晚年恒星，体积巨大，温度低
        WhiteDwarf,  // 白矮星：恒星残骸，密度极高
        NeutronStar, // 中子星：超新星残骸，自转极快，强磁场
        BlackHole    // 黑洞：引力极强，光无法逃逸
    }

    /// <summary>星团类型（疏散星团/球状星团）</summary>
    public enum ClusterType { Open, Globular }

    /// <summary>Actor快照：单位离开主星球时保存关键数据，到达时恢复</summary>
    [Serializable]
    public class ActorSnapshot
    {
        public long id;
        public string assetId; // 种族asset ID
        public string name;
        public string kingdomName;
        public int age;
        public int rank = -1; // 阶位索引（0=F~13=X），超A(>=10)可肉身横渡
        public string assignment = "idle"; // 任务分配：idle/mining/research/defense
    }

    /// <summary>星球数据（行星/恒星）</summary>
    [Serializable]
    public class PlanetData
    {
        public string id;
        public string name;
        public PlanetType type;
        public float x;
        public float y;
        public float resource;
        public float maxResource;
        public string ownerKingdom;
        public List<ActorSnapshot> units = new List<ActorSnapshot>(); // 驻留单位快照
        public int colonyLevel;
        public bool discovered;
        public bool autoTransport = true; // 戴森球式：自动运输开关（资源超阈值自动运回主星球）
        public string parentStar; // 所属恒星ID（行星才有）
        public float orbitRadius; // 公转轨道半长轴
        public float orbitAngle; // 初始轨道角度
        public float orbitSpeed; // 公转速度（开普勒第三定律：越远越慢）
        public float eccentricity; // 轨道偏心率（0=正圆，0.9=极扁椭圆）
        public float inclination; // 轨道倾角（度）
        public bool hasRing; // 是否有行星环
        public StarType starType; // 恒星类型（仅恒星有）
        public float temperature; // 行星表面温度（K）
        public bool inHabitableZone; // 是否在宜居带内

        // ========== 行星物理参数（现实天体物理） ==========
        public float mass; // 质量（地球质量=1）
        public float radius; // 半径（地球半径=1）
        public float gravity; // 表面重力（g）
        public float density; // 密度（g/cm³）
        public float rotationPeriod; // 自转周期（小时）
        public float axialTilt; // 黄赤交角（度，0=无季节，23.4=地球）
        public int moonCount; // 卫星数量
        public bool tidallyLocked; // 是否潮汐锁定（一面永远朝向恒星）
        public float atmosphere; // 大气层厚度（0=无，1=地球）
        public float magneticField; // 磁场强度（0=无，1=地球）
        public float starMass; // 恒星质量（太阳质量=1，仅恒星有）
        public float starRadius; // 恒星半径（太阳半径=1，仅恒星有）
        public float starLuminosity; // 恒星光度（太阳=1，仅恒星有）
        public float starAge; // 恒星年龄（亿年，仅恒星有）

        // ========== 殖民地与事件（v0.178.0玩法深化） ==========
        public List<ColonyBuilding> buildings = new List<ColonyBuilding>(); // 殖民地建筑
        public float lastEventTime; // 上次事件时间（冷却用）
        public StarEventResult pendingEvent; // 待处理的事件结果
        public float eventCooldown = 60f; // 事件冷却时间（秒）
    }

    /// <summary>航行数据（单位在星际航行中的状态）</summary>
    [Serializable]
    public class VoyageData
    {
        public ActorSnapshot snapshot; // 航行中的单位快照
        public string fromPlanet;
        public string toPlanet;
        public float progress;
        public float speed;
        public int totalTicks; // 总航行时间（tick）
    }

    /// <summary>星团数据（疏散星团/球状星团）</summary>
    [Serializable]
    public class StarCluster
    {
        public string id;
        public string name;
        public ClusterType type;
        public float x;
        public float y;
        public float radius;
        public int starCount;
        public List<string> planetIds = new List<string>();
    }

    /// <summary>星际系统存档数据</summary>
    [Serializable]
    public class StarSystemSaveData
    {
        public List<PlanetSaveData> planets = new List<PlanetSaveData>();
        public List<VoyageSaveData> voyages = new List<VoyageSaveData>();
        public int worldSeed;
    }

    /// <summary>星球存档数据</summary>
    [Serializable]
    public class PlanetSaveData
    {
        public string id;
        public string name;
        public PlanetType type;
        public float x, y;
        public float resource;
        public float maxResource;
        public string ownerKingdom;
        public int colonyLevel;
        public bool discovered;
        public string parentStar;
        public float orbitRadius, orbitAngle, orbitSpeed;
        public float eccentricity, inclination;
        public bool hasRing;
        public StarType starType;
        public float temperature;
        public bool inHabitableZone;
        public float mass, radius, gravity, density;
        public float rotationPeriod, axialTilt;
        public int moonCount;
        public bool tidallyLocked;
        public float atmosphere, magneticField;
        public float starMass, starRadius, starLuminosity, starAge;
        public List<ActorSnapshot> units = new List<ActorSnapshot>();
    }

    /// <summary>航行存档数据</summary>
    [Serializable]
    public class VoyageSaveData
    {
        public ActorSnapshot snapshot;
        public string fromPlanet;
        public string toPlanet;
        public float progress;
        public float speed;
        public int totalTicks;
    }
}
