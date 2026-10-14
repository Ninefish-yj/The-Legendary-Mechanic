using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>任务/成就/声望系统（v0.183完整世界）
    /// 任务系统：主线/支线/日常
    /// 成就系统：各种成就和奖励
    /// 声望系统：各势力声望等级
    /// 原著依据：机械师有任务系统、成就系统、势力声望</summary>
    public static class SuperMechQuestSystem
    {
        // ========== 任务系统 ==========

        /// <summary>任务类型</summary>
        public enum QuestType
        {
            Main,      // 主线
            Side,      // 支线
            Daily,     // 日常
            Faction    // 势力任务
        }

        /// <summary>任务状态</summary>
        public enum QuestStatus
        {
            Locked,     // 未解锁
            Available,  // 可接取
            Active,     // 进行中
            Completed,  // 已完成
            TurnedIn    // 已交付
        }

        /// <summary>任务目标类型</summary>
        public enum QuestObjectiveType
        {
            Kill,           // 击杀N个单位
            ReachRank,      // 达到某阶位
            LearnKnowledge, // 学习某知识
            BuildStructure, // 建造某建筑
            ExplorePlanet,  // 探索某星球
            EarnInal,       // 获得N伊纳尔
            WinBattle,      // 赢得N场战斗
            CraftItem       // 制作N件物品
        }

        /// <summary>任务定义</summary>
        public class QuestDef
        {
            public string id;
            public string nameKey;       // 名称本地化key
            public string descKey;       // 描述本地化key
            public QuestType type;
            public QuestObjectiveType objectiveType;
            public string objectiveTarget; // 目标ID（单位类型/知识ID/建筑ID等）
            public int objectiveCount;   // 目标数量
            public int inalReward;       // 伊纳尔奖励
            public int xpReward;         // 经验奖励
            public string prerequisite;  // 前置任务ID
            public int minLevel;         // 最低等级
        }

        /// <summary>任务进度</summary>
        public class QuestProgress
        {
            public string questId;
            public QuestStatus status;
            public int progress;         // 当前进度
            public float startTime;
        }

        private static readonly Dictionary<string, QuestDef> _questDefs = new Dictionary<string, QuestDef>();
        private static readonly Dictionary<long, Dictionary<string, QuestProgress>> _questProgress = new Dictionary<long, Dictionary<string, QuestProgress>>();
        private static bool _questsInitialized;

        /// <summary>初始化任务库</summary>
        public static void InitQuests()
        {
            if (_questsInitialized) return;
            _questsInitialized = true;

            // 主线任务
            AddQuest("main_awaken", "sm_quest_main_awaken", "sm_quest_main_awaken_desc",
                QuestType.Main, QuestObjectiveType.ReachRank, "rank_c", 1, 500, 100, null, 0);

            AddQuest("main_mech_path", "sm_quest_main_mech", "sm_quest_main_mech_desc",
                QuestType.Main, QuestObjectiveType.LearnKnowledge, "mech_theory", 1, 800, 200, "main_awaken", 2);

            AddQuest("main_first_battle", "sm_quest_main_battle", "sm_quest_main_battle_desc",
                QuestType.Main, QuestObjectiveType.Kill, "any", 10, 1000, 300, "main_awaken", 1);

            AddQuest("main_star_travel", "sm_quest_main_star", "sm_quest_main_star_desc",
                QuestType.Main, QuestObjectiveType.ExplorePlanet, "any", 1, 5000, 1000, "main_mech_path", 5);

            // 支线任务
            AddQuest("side_collect_inal", "sm_quest_side_inal", "sm_quest_side_inal_desc",
                QuestType.Side, QuestObjectiveType.EarnInal, "any", 10000, 2000, 500, null, 1);

            AddQuest("side_craft_master", "sm_quest_side_craft", "sm_quest_side_craft_desc",
                QuestType.Side, QuestObjectiveType.CraftItem, "any", 5, 1500, 400, null, 3);

            // 日常任务
            AddQuest("daily_kill_5", "sm_quest_daily_kill", "sm_quest_daily_kill_desc",
                QuestType.Daily, QuestObjectiveType.Kill, "any", 5, 200, 50, null, 0);

            AddQuest("daily_win_battle", "sm_quest_daily_battle", "sm_quest_daily_battle_desc",
                QuestType.Daily, QuestObjectiveType.WinBattle, "any", 3, 300, 80, null, 0);

            Debug.Log("[超神机械师] 任务系统初始化完成");
        }

        private static void AddQuest(string id, string nameKey, string descKey, QuestType type,
            QuestObjectiveType objType, string target, int count, int inal, int xp, string prereq, int minLvl)
        {
            _questDefs[id] = new QuestDef
            {
                id = id,
                nameKey = nameKey,
                descKey = descKey,
                type = type,
                objectiveType = objType,
                objectiveTarget = target,
                objectiveCount = count,
                inalReward = inal,
                xpReward = xp,
                prerequisite = prereq,
                minLevel = minLvl
            };
        }

        /// <summary>获取可接取任务</summary>
        public static List<QuestDef> GetAvailableQuests(Actor a)
        {
            InitQuests();
            var result = new List<QuestDef>();
            if (a == null) return result;
            var progress = GetProgressDict(a);
            int level = SuperMechInfoState.GetLevel(a);

            foreach (var quest in _questDefs.Values)
            {
                if (level < quest.minLevel) continue;
                if (!string.IsNullOrEmpty(quest.prerequisite) &&
                    (!progress.TryGetValue(quest.prerequisite, out var p) || p.status != QuestStatus.TurnedIn))
                    continue;
                if (progress.TryGetValue(quest.id, out var qp) && qp.status >= QuestStatus.Completed)
                    continue;
                result.Add(quest);
            }
            return result;
        }

        /// <summary>接取任务</summary>
        public static bool AcceptQuest(Actor a, string questId)
        {
            if (a == null || !_questDefs.ContainsKey(questId)) return false;
            var progress = GetProgressDict(a);
            if (progress.ContainsKey(questId) && progress[questId].status >= QuestStatus.Active) return false;
            progress[questId] = new QuestProgress { questId = questId, status = QuestStatus.Active, progress = 0, startTime = Time.time };
            Debug.Log($"[超神机械师] {a.data.name} 接取任务: {questId}");
            return true;
        }

        /// <summary>更新任务进度</summary>
        public static void UpdateQuestProgress(Actor a, QuestObjectiveType type, string target, int amount = 1)
        {
            if (a == null) return;
            var progress = GetProgressDict(a);
            foreach (var kv in progress)
            {
                var qp = kv.Value;
                if (qp.status != QuestStatus.Active) continue;
                if (!_questDefs.TryGetValue(qp.questId, out var def)) continue;
                if (def.objectiveType != type) continue;
                if (def.objectiveTarget != "any" && def.objectiveTarget != target) continue;

                qp.progress += amount;
                if (qp.progress >= def.objectiveCount)
                {
                    qp.status = QuestStatus.Completed;
                    Debug.Log($"[超神机械师] 任务完成: {qp.questId}");
                }
            }
        }

        /// <summary>交付任务（领取奖励）</summary>
        public static bool TurnInQuest(Actor a, string questId)
        {
            if (a == null) return false;
            var progress = GetProgressDict(a);
            if (!progress.TryGetValue(questId, out var qp) || qp.status != QuestStatus.Completed) return false;
            if (!_questDefs.TryGetValue(questId, out var def)) return false;

            SuperMechInal.AddInal(a, def.inalReward);
            SuperMechAwakened.AddXp(a, def.xpReward);
            qp.status = QuestStatus.TurnedIn;
            Debug.Log($"[超神机械师] {a.data.name} 交付任务: {questId}，获得{def.inalReward}₡ +{def.xpReward}经验");
            return true;
        }

        private static Dictionary<string, QuestProgress> GetProgressDict(Actor a)
        {
            if (!_questProgress.TryGetValue(a.id, out var dict))
            {
                dict = new Dictionary<string, QuestProgress>();
                _questProgress[a.id] = dict;
            }
            return dict;
        }

        // ========== 成就系统 ==========

        /// <summary>成就定义</summary>
        public class AchievementDef
        {
            public string id;
            public string nameKey;
            public string descKey;
            public string icon;
            public int inalReward;
            public Func<Actor, bool> checkCondition;
        }

        private static readonly List<AchievementDef> _achievements = new List<AchievementDef>();
        private static readonly HashSet<long> _unlockedAchievements = new HashSet<long>();
        private static bool _achievementsInitialized;

        /// <summary>初始化成就</summary>
        public static void InitAchievements()
        {
            if (_achievementsInitialized) return;
            _achievementsInitialized = true;

            _achievements.Add(new AchievementDef
            {
                id = "first_blood",
                nameKey = "sm_ach_first_blood",
                descKey = "sm_ach_first_blood_desc",
                inalReward = 100,
                checkCondition = a => SuperMechCombatStats.GetStats(a)?.totalKills >= 1
            });

            _achievements.Add(new AchievementDef
            {
                id = "kill_100",
                nameKey = "sm_ach_kill_100",
                descKey = "sm_ach_kill_100_desc",
                inalReward = 1000,
                checkCondition = a => SuperMechCombatStats.GetStats(a)?.totalKills >= 100
            });

            _achievements.Add(new AchievementDef
            {
                id = "kill_streak_10",
                nameKey = "sm_ach_streak_10",
                descKey = "sm_ach_streak_10_desc",
                inalReward = 500,
                checkCondition = a => SuperMechCombatStats.GetStats(a)?.maxKillStreak >= 10
            });

            _achievements.Add(new AchievementDef
            {
                id = "rich",
                nameKey = "sm_ach_rich",
                descKey = "sm_ach_rich_desc",
                inalReward = 500,
                checkCondition = a => SuperMechInal.GetInal(a) >= 10000
            });

            _achievements.Add(new AchievementDef
            {
                id = "rank_s",
                nameKey = "sm_ach_rank_s",
                descKey = "sm_ach_rank_s_desc",
                inalReward = 2000,
                checkCondition = a => SuperMechAdvancement.GetExactRankIndex(a) >= 6
            });

            _achievements.Add(new AchievementDef
            {
                id = "star_explorer",
                nameKey = "sm_ach_star_explorer",
                descKey = "sm_ach_star_explorer_desc",
                inalReward = 3000,
                checkCondition = a => SuperMechStarSystem.GetDiscoveredPlanets()?.Count >= 5
            });

            _achievements.Add(new AchievementDef
            {
                id = "knowledge_master",
                nameKey = "sm_ach_knowledge_master",
                descKey = "sm_ach_knowledge_master_desc",
                inalReward = 2000,
                checkCondition = a => SuperMechKnowledge.GetLearnedKnowledge(a)?.Count >= 10
            });

            _achievements.Add(new AchievementDef
            {
                id = "colony_builder",
                nameKey = "sm_ach_colony_builder",
                descKey = "sm_ach_colony_builder_desc",
                inalReward = 1500,
                checkCondition = a => CountTotalBuildings() >= 10
            });

            Debug.Log("[超神机械师] 成就系统初始化完成");
        }

        /// <summary>检查成就解锁</summary>
        public static void CheckAchievements(Actor a)
        {
            InitAchievements();
            if (a == null) return;
            foreach (var ach in _achievements)
            {
                long key = a.id ^ ach.id.GetHashCode();
                if (_unlockedAchievements.Contains(key)) continue;
                if (ach.checkCondition(a))
                {
                    _unlockedAchievements.Add(key);
                    SuperMechInal.AddInal(a, ach.inalReward);
                    Debug.Log($"[超神机械师] 成就解锁: {ach.id}，奖励{ach.inalReward}₡");
                }
            }
        }

        /// <summary>获取已解锁成就数</summary>
        public static int GetUnlockedCount(Actor a)
        {
            InitAchievements();
            int count = 0;
            foreach (var ach in _achievements)
            {
                long key = a.id ^ ach.id.GetHashCode();
                if (_unlockedAchievements.Contains(key)) count++;
            }
            return count;
        }

        /// <summary>获取成就总数</summary>
        public static int TotalAchievements
        {
            get { InitAchievements(); return _achievements.Count; }
        }

        /// <summary>计算所有殖民地建筑总数</summary>
        private static int CountTotalBuildings()
        {
            int count = 0;
            try
            {
                var planets = SuperMechStarSystem.GetAllPlanets();
                if (planets != null)
                {
                    foreach (var p in planets)
                    {
                        if (p.buildings != null)
                            count += p.buildings.Count;
                    }
                }
            }
            catch { /* 星际系统未初始化时返回0 */ }
            return count;
        }

        // ========== 声望系统 ==========

        /// <summary>声望等级</summary>
        public enum ReputationLevel
        {
            Hated = -2,      // 仇恨
            Hostile = -1,    // 敌对
            Neutral = 0,     // 中立
            Friendly = 1,    // 友好
            Honored = 2,     // 尊敬
            Revered = 3,     // 崇敬
            Exalted = 4      // 崇拜
        }

        private static readonly Dictionary<string, Dictionary<long, int>> _reputation = new Dictionary<string, Dictionary<long, int>>();

        /// <summary>获取声望值</summary>
        public static int GetReputation(Actor a, string factionId)
        {
            if (a == null || string.IsNullOrEmpty(factionId)) return 0;
            if (_reputation.TryGetValue(factionId, out var dict) && dict.TryGetValue(a.id, out var rep))
                return rep;
            return 0;
        }

        /// <summary>增加声望</summary>
        public static void AddReputation(Actor a, string factionId, int amount)
        {
            if (a == null || string.IsNullOrEmpty(factionId)) return;
            if (!_reputation.ContainsKey(factionId))
                _reputation[factionId] = new Dictionary<long, int>();
            if (!_reputation[factionId].ContainsKey(a.id))
                _reputation[factionId][a.id] = 0;
            _reputation[factionId][a.id] = Mathf.Clamp(_reputation[factionId][a.id] + amount, -10000, 10000);
        }

        /// <summary>获取声望等级</summary>
        public static ReputationLevel GetReputationLevel(Actor a, string factionId)
        {
            int rep = GetReputation(a, factionId);
            if (rep >= 5000) return ReputationLevel.Exalted;
            if (rep >= 3000) return ReputationLevel.Revered;
            if (rep >= 1500) return ReputationLevel.Honored;
            if (rep >= 500) return ReputationLevel.Friendly;
            if (rep >= -500) return ReputationLevel.Neutral;
            if (rep >= -2000) return ReputationLevel.Hostile;
            return ReputationLevel.Hated;
        }

        /// <summary>获取声望等级名称（本地化key）</summary>
        public static string GetReputationLevelKey(ReputationLevel level)
        {
            return level switch
            {
                ReputationLevel.Hated => "sm_rep_hated",
                ReputationLevel.Hostile => "sm_rep_hostile",
                ReputationLevel.Neutral => "sm_rep_neutral",
                ReputationLevel.Friendly => "sm_rep_friendly",
                ReputationLevel.Honored => "sm_rep_honored",
                ReputationLevel.Revered => "sm_rep_revered",
                ReputationLevel.Exalted => "sm_rep_exalted",
                _ => "?"
            };
        }

        // ========== 持久化 ==========

        /// <summary>存档数据结构</summary>
        [System.Serializable]
        public class QuestSaveData
        {
            public List<QuestProgressSave> questProgress = new List<QuestProgressSave>();
            public List<string> unlockedAchievements = new List<string>();
            public List<ReputationSave> reputation = new List<ReputationSave>();
        }

        [System.Serializable]
        public class QuestProgressSave
        {
            public long actorId;
            public string questId;
            public int status;
            public int progress;
        }

        [System.Serializable]
        public class ReputationSave
        {
            public string factionId;
            public long actorId;
            public int value;
        }

        /// <summary>保存数据</summary>
        public static QuestSaveData Save()
        {
            var data = new QuestSaveData();
            foreach (var kv in _questProgress)
            {
                foreach (var qp in kv.Value)
                {
                    data.questProgress.Add(new QuestProgressSave
                    {
                        actorId = kv.Key,
                        questId = qp.Value.questId,
                        status = (int)qp.Value.status,
                        progress = qp.Value.progress
                    });
                }
            }
            // 成就存储的是hash，需要转换
            // 简化：只保存数量，不保存具体成就（因为hash无法反推）
            data.unlockedAchievements.Add(_unlockedAchievements.Count.ToString());
            foreach (var kv in _reputation)
            {
                foreach (var rep in kv.Value)
                {
                    data.reputation.Add(new ReputationSave
                    {
                        factionId = kv.Key,
                        actorId = rep.Key,
                        value = rep.Value
                    });
                }
            }
            return data;
        }

        /// <summary>加载数据</summary>
        public static void Load(QuestSaveData data)
        {
            if (data == null) return;
            Clear();
            foreach (var qp in data.questProgress)
            {
                if (!_questProgress.ContainsKey(qp.actorId))
                    _questProgress[qp.actorId] = new Dictionary<string, QuestProgress>();
                _questProgress[qp.actorId][qp.questId] = new QuestProgress
                {
                    questId = qp.questId,
                    status = (QuestStatus)qp.status,
                    progress = qp.progress,
                    startTime = 0
                };
            }
            foreach (var rep in data.reputation)
            {
                if (!_reputation.ContainsKey(rep.factionId))
                    _reputation[rep.factionId] = new Dictionary<long, int>();
                _reputation[rep.factionId][rep.actorId] = rep.value;
            }
        }

        /// <summary>清空数据</summary>
        public static void Clear()
        {
            _questProgress.Clear();
            _unlockedAchievements.Clear();
            _reputation.Clear();
        }
    }
}
