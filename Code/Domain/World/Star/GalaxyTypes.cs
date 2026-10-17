using System;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>星系类型（参考真实宇宙结构）</summary>
    public enum GalaxyType
    {
        Spiral,      // 旋涡星系（如银河系）
        Elliptical,  // 椭圆星系
        Irregular,   // 不规则星系
        Dwarf        // 矮星系
    }

    /// <summary>星系（恒星系统）类型</summary>
    public enum StarSystemType
    {
        Single,      // 单星系统
        Binary,      // 双星系统
        Trinary,     // 三星系统
        Nebula,      // 星云（恒星形成区）
        Remnant      // 残骸星系（中子星/黑洞）
    }

    /// <summary>舰队状态</summary>
    public enum FleetStatus
    {
        Idle,        // 驻扎
        Traveling,   // 航行中
        Orbiting,    // 轨道驻扎
        Bombarding,  // 轨道轰炸
        Invading,    // 入侵中
        Retreating   // 撤退中
    }

    /// <summary>舰船类型</summary>
    public enum ShipType
    {
        Scout,       // 侦察舰
        Corvette,    // 护卫舰
        Frigate,     // 驱逐舰
        Cruiser,     // 巡洋舰
        Battleship,  // 战列舰
        Carrier,     // 航空母舰
        Transport,   // 运输舰
        ColonyShip   // 殖民舰
    }

    /// <summary>外交关系类型</summary>
    public enum DiplomacyState
    {
        Unknown,     // 未接触
        Neutral,     // 中立
        Friendly,    // 友好
        Allied,      // 同盟
        Trade,       // 贸易协定
        Cold,        // 冷战
        Hostile,     // 敌对
        War          // 战争
    }

    /// <summary>外星文明科技等级（参考卡尔达肖夫指数）</summary>
    public enum CivTechLevel
    {
        PreIndustrial,  // 前工业（0.7级以下）
        Industrial,     // 工业时代（0.7级）
        Atomic,         // 原子能时代（0.8级）
        Space,          // 太空时代（0.9级）
        Interplanetary, // 行星际文明（1.0级）
        Interstellar,   // 星际文明（1.5级）
        Galactic        // 银河文明（2.0级+）
    }

    /// <summary>舰船数据</summary>
    [Serializable]
    public class ShipData
    {
        public string id;
        public string name;
        public ShipType type;
        public float hp;
        public float maxHp;
        public float attack;
        public float defense;
        public float speed;
        public int crewCapacity; // 载员容量
        public List<ActorSnapshot> crew = new List<ActorSnapshot>(); // 舰载单位
    }

    /// <summary>舰队数据</summary>
    [Serializable]
    public class FleetData
    {
        public string id;
        public string name;
        public string ownerKingdom; // 所属王国/文明
        public FleetStatus status;
        public string currentSystem; // 当前所在星系
        public string targetSystem;  // 目标星系
        public float travelProgress; // 航行进度0-1
        public List<ShipData> ships = new List<ShipData>();
        public float fuel;
        public float maxFuel;
        public float morale;

        public float GetTotalAttack()
        {
            float total = 0;
            foreach (var s in ships) total += s.attack;
            return total;
        }

        public float GetTotalDefense()
        {
            float total = 0;
            foreach (var s in ships) total += s.defense;
            return total;
        }

        public int GetTotalCrew()
        {
            int total = 0;
            foreach (var s in ships) total += s.crew.Count;
            return total;
        }
    }

    /// <summary>星系（恒星系统）数据</summary>
    [Serializable]
    public class StarSystemData
    {
        public string id;
        public string name;
        public StarSystemType type;
        public float x; // 银河坐标x
        public float y; // 银河坐标y
        public bool discovered;
        public bool visited;
        public string ownerCiv; // 所属外星文明（null=无主）
        public List<string> planetIds = new List<string>(); // 星系内星球ID
        public List<string> fleetIds = new List<string>(); // 驻扎舰队ID
        public float resourceBonus; // 星系资源加成
        public int threatLevel; // 威胁等级（0-5）
    }

    /// <summary>外交关系数据</summary>
    [Serializable]
    public class DiplomacyRelation
    {
        public string civA;
        public string civB;
        public DiplomacyState state;
        public int opinion; // -100到100
        public float tradeVolume; // 贸易额
        public bool hasTradeAgreement;
        public bool hasAlliance;
        public bool hasNonAggression;
        public int warScore; // 战争分数
    }

    /// <summary>外星文明数据</summary>
    [Serializable]
    public class AlienCivilization
    {
        public string id;
        public string name;
        public string speciesName; // 种族名
        public CivTechLevel techLevel;
        public Color skinColor; // 种族代表色
        public List<string> controlledSystems = new List<string>(); // 控制的星系
        public List<string> knownCivs = new List<string>(); // 已知文明
        public float totalPower; // 综合国力
        public float economy;
        public float military;
        public float research;
        public int population;
        public string governmentType; // 政体
        public string ideology; // 意识形态
        public bool isPlayer; // 是否是玩家文明
    }

    /// <summary>银河数据（整个星系地图）</summary>
    [Serializable]
    public class GalaxyData
    {
        public string id;
        public string name;
        public GalaxyType type;
        public int seed;
        public float width;  // 银河宽度（光年）
        public float height; // 银河高度（光年）
        public List<StarSystemData> systems = new List<StarSystemData>();
        public List<AlienCivilization> civilizations = new List<AlienCivilization>();
        public List<DiplomacyRelation> relations = new List<DiplomacyRelation>();
        public List<FleetData> fleets = new List<FleetData>();
        public string playerHomeSystem; // 玩家母星系ID
        public int turnCount; // 回合数（用于模拟）
    }

    /// <summary>星际战斗结果</summary>
    [Serializable]
    public class SpaceBattleResult
    {
        public string attackerFleet;
        public string defenderFleet;
        public string winner; // attacker/ defender / draw
        public float attackerLosses;
        public float defenderLosses;
        public List<string> destroyedShips = new List<string>();
        public string description;
    }

    /// <summary>银河存档数据</summary>
    [Serializable]
    public class GalaxySaveData
    {
        public GalaxyData galaxy;
        public List<FleetData> fleets = new List<FleetData>();
        public List<AlienCivilization> civs = new List<AlienCivilization>();
        public List<DiplomacyRelation> relations = new List<DiplomacyRelation>();
    }
}
