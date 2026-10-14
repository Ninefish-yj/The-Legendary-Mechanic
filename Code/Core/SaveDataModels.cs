using System;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>模组存档数据结构（从SuperMechSaveData拆分，纯数据定义）</summary>
        [Serializable]
        public class SaveData
        {
            public int version = CurrentSaveVersion;
            public Dictionary<string, ActorSaveData> actors = new Dictionary<string, ActorSaveData>();
            public SanctuarySaveData sanctuary = new SanctuarySaveData();
            public SuperMechFaction.FactionSaveData faction = new SuperMechFaction.FactionSaveData();
            public SuperMechPlayer.PlayerSaveData player = new SuperMechPlayer.PlayerSaveData();
            public SuperMechWorldTree.WorldTreeSaveData worldTree = new SuperMechWorldTree.WorldTreeSaveData();
            public SuperMechWorldBeyond.WorldBeyondSaveData worldBeyond = new SuperMechWorldBeyond.WorldBeyondSaveData();
            public List<SuperMechVirtualGenesis.GenesisSaveData> virtualGenesis = new List<SuperMechVirtualGenesis.GenesisSaveData>();
            public SuperMechMechLegion.MechLegionSaveData mechLegion = new SuperMechMechLegion.MechLegionSaveData();
            public string worldSeed = "";
            public long savedAt = 0;
            // === v0.31.0 预留字段 ===
            public int cosmicIteration = 0;       // 宇宙迭代次数
            public string civilizationData = "";  // 文明数据（JSON序列化）
            public List<string> unlockedRecipes = new List<string>(); // 已解锁融合配方
            // === v0.37.0 超神遗力还原 ===
            public string worldLegacyPool = "";   // 世界游离遗力池（JSON序列化）
            // === v0.39.7 首位突破记录 ===
            public List<string> firstBreakthroughRanks = new List<string>(); // 已首次突破的阶位（全图只记一次）
            public string companyData = "";  // 公司数据（JSON序列化，v0.87.0）
            // === v0.102.0 星际+科技存档 ===
            public string starSystemData = "";  // 星际系统数据（JSON序列化）
            public string techData = "";        // 科技系统数据（JSON序列化）
            // === v0.152.8 文明科技点存档 ===
            public string civilizationTechData = "";  // 文明科技点+科技等级（JSON序列化）
            // === v0.185 任务/成就/声望存档 ===
            public string questData = "";  // 任务进度+成就+声望（JSON序列化）
        }

        [Serializable]
        public class ActorSaveData
        {
            public string name;
            public int qiLevel;
            public float qiCurrent;
            public float qiMax;
            public int stage;
            public int exactRank;
            public int potential;
            public int awakeningPoints;
            public int divinityPoints;
            public int divinityProfLayers;
            public int divinitySpeciesLayers;
            public bool divinityTriggered;
            public int legacyPower;
            public string legacySources = "";  // v0.37.0 遗力来源记录（JSON序列化）
            public bool transcended;
            public float advancementProgress;
            public bool advancementTaskDone;
            public int infoStateLevel;
            public int awakenedLevel;
            public float awakenedXp;
            public float heritage;
            public int reviveCount;
            public string qiAttribute;
            public int legend;
            public string lastDeed;
            public string destinyName;
            public float destinyProgress;
            public bool destinyCompleted;
            public string branch;
            public string subclass;
            public string species = "human";  // 种族ID（v0.89.1 自定义生物系统）
            public int subclassLevel;
            public List<string> knowledge;
            public List<string> equipBag;
            public string currentEquip;
            public List<string> fusionRecipes;
            public int mechFusionLevel;
            public string fusedEquip;
            public int[] sanctuaryAuthority;
            public string talents;
            public int profession;
            public int switchCount;
            public bool fiveSystemGenius;
            public string equippedAffixes;
            public int modifyLevel;  // 装备改装等级（v0.86.3）
            public string durability;
            public int refineCount;
            public int emRefineCount;
            public float refineQiBonus;
            public float emRefineQiBonus;
            public List<string> activeSynergies;
            public List<string> specialties;   // 专属专长（超A级个体独有，v0.76.57）
            public int geneStage;
            public int manaStage;
            public int mindStage;
            public int geneProgress;
            public int manaProgress;
            public int mindProgress;
            public string activeDimension;
            public float insightProgress;
            public string potentialRating;
            public List<string> learnedSkills;
            public int towerLevel;
            public string subXpData;
            public int inal = 0;  // 伊纳尔余额（v0.87.0）
            public string subLevelData;
            public int craftCount;
            // === v0.31.0 预留字段 ===
            public int qiLayer;              // 气力层次（Lv1~Lv10）
            public float energyPeak;         // 能级峰值记录
            public List<string> fusionHistory; // 知识融合历史
            public string informationState;  // 信息态记录（预留）
            // === v0.185 知识等级+装备强化存档 ===
            public string knowledgeLevels = "";  // 知识等级（JSON序列化：kid->level）
            public int enhanceLevel = 0;  // 装备强化等级
        }

        [Serializable]
        public class SanctuarySaveData
        {
            public int unlockedSanctuaries;
            public int keyFragments;
            public int keyMaterials; // v0.46.0
            public int[] sanctuaryFragments;
            public int totalPermission;
            public int totalVisits;
            public bool messageBoardUnlocked;
            public int totalDivinityAscensions;
            public int totalResurrections;
        }
}
