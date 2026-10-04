using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.71.0 玩家降临·第四天灾（原著细还原）
    /// 原著依据：
    ///  1. 韩萧是唯一自带玩家面板的特殊NPC，可发布任务给真正玩家、成千上万玩家替他打工（今日头条简介/起点#57）；
    ///  2. 任务额度机制：单次任务奖励上限 + 每日总额度上限=最高额度的5倍、24小时刷新（起点第57章）；
    ///  3. 奖池任务：接取消耗经验、奖池按贡献度前五分配（原著第130章狩猎萌芽：奖池三万五经验、按贡献度前五分配）；
    ///  4. 任务类型与评级：讨伐/破坏类任务按完成目标数评级给额外奖励（原著第619章）；
    ///  5. 玩家不死不灭：降临者死亡后复活（第四天灾特性）。
    /// 真实系统适配：降临者单位从地图边缘周期生成、死亡后复活；任务由最强文明自动发布；贡献挂在战斗补丁击杀结算。
    /// </summary>
    public static class SuperMechPlayer
    {
        public const string PlayerTrait = "sm_player";      // 降临者标记（第四天灾玩家）

        // ==== 玩家面板（原著：生命/体力/六维属性/自由点/潜能/专长） ====
        public class PlayerPanelData
        {
            public int vitality;          // 生命值
            public int stamina;           // 体力值
            public int strength;          // 力量
            public int agility;           // 敏捷
            public int endurance;         // 耐力
            public int intelligence;      // 智力
            public int mystery;           // 神秘
            public int charm;             // 魅力
            public int luck;              // 幸运
            public int freePoints;        // 自由属性点
            public int potential;         // 潜能点
            public int kills;             // 累计击杀
            public int contribution;      // 累计任务贡献
        }

        // ==== 任务（原著：讨伐/破坏/采集；奖池+接取门槛+贡献榜） ====
        public enum TaskType { Hunt, Gather, Destroy }

        public class TaskData
        {
            public string id;
            public TaskType type;
            public string targetLabel;
            public int targetCount;
            public int progress;
            public float rewardPool;      // 奖池（经验）
            public float entryCost;       // 接取门槛（消耗经验，原著：接取任务需消耗经验）
            public int expiresTick;       // 到期tick
            public string publisherName;  // 发布者（最强文明）
            public Dictionary<long, int> contribution = new Dictionary<long, int>();
            public bool settled;
            public string grade;
        }

        public class PlayerSaveData
        {
            public Dictionary<string, PlayerPanelData> panels = new Dictionary<string, PlayerPanelData>();
            public List<TaskData> tasks = new List<TaskData>();
            public int nextTaskId = 1;
            public int dailyBudgetUsed;
            public int dailyResetTick;
            public int spawnCounter;
            public int nextSpawnInTicks = 60;
        }

        private static readonly PlayerSaveData _data = new PlayerSaveData();
        public static PlayerSaveData Data => _data;

        // 复活队列：actorId -> 剩余复活tick（第四天灾：玩家不死不灭）
        private static readonly Dictionary<long, int> _respawnTicks = new Dictionary<long, int>();
        private static int _tick = 0;

        // 数值（可配置）
        private const float PoolBase = 1000f;             // 奖池基准
        private const float PoolPerTech = 200f;           // 每科技等级+200奖池
        private const float EntryCostRatio = 0.05f;       // 接取门槛=奖池5%（原著：接取消耗经验）
        private const int DailyBudgetMultiplier = 5;      // 每日总额度=单任务上限×5（原著第57章）
        private const int RespawnTicks = 600;             // 复活冷却（tick）
        private const int SpawnBatchMin = 2;              // 每批降临者数量下限
        private const int SpawnBatchMax = 6;              // 每批降临者数量上限
        private const int ContributionTopN = 5;           // 贡献榜前5（原著第130章）
        private const float ContributionPerKill = 10f;    // 每击杀+10贡献
        private const int TaskTargetBase = 8;             // 讨伐目标数基准
        private const int DailyResetIntervalTicks = 1440; // 每日额度刷新间隔（原著：24小时刷新）
        private const float GradeExtraRatio = 0.2f;       // 评级额外奖励=奖池×20%

        // ============ 面板 ============

        public static bool IsPlayer(Actor a) => a != null && a.hasTrait(PlayerTrait);

        public static PlayerPanelData GetOrCreatePanel(Actor a)
        {
            if (a == null || a.id == null) return null;
            string key = a.id.ToString();
            if (!_data.panels.TryGetValue(key, out var p))
            {
                p = new PlayerPanelData
                {
                    vitality = 100, stamina = 100,
                    strength = 10, agility = 10, endurance = 10, intelligence = 10,
                    mystery = 5, charm = 5, luck = 5,
                    freePoints = 5, potential = 1
                };
                _data.panels[key] = p;
            }
            return p;
        }

        public static int PlayerCount
        {
            get
            {
                if (World.world == null || World.world.units == null) return 0;
                int n = 0;
                var units = World.world.units.units_only_alive;
                if (units == null) return 0;
                foreach (var a in units) if (a != null && a.isAlive() && a.hasTrait(PlayerTrait)) n++;
                return n;
            }
        }

        public static int RespawningCount => _respawnTicks.Count;

        // ============ 主循环 ============

        public static void TickPlayer(float delta)
        {
            if (!SuperMechConfig.PlayerEnabled) return;
            if (World.world == null || World.world.units == null) return;
            _tick++;
            if (_tick % 20 != 0) return; // 每20tick处理一次，控制开销

            // 1. 每日额度刷新（原著：24小时刷新一次）
            if (_tick >= _data.dailyResetTick)
            {
                _data.dailyBudgetUsed = 0;
                _data.dailyResetTick = _tick + DailyResetIntervalTicks;
            }

            // 2. 降临者生成（从地图边缘，第四天灾降临）
            _data.nextSpawnInTicks--;
            if (_data.nextSpawnInTicks <= 0)
            {
                _data.nextSpawnInTicks = Mathf.Max(60, SuperMechConfig.PlayerSpawnIntervalTicks);
                SpawnBatch();
            }

            // 3. 复活队列（玩家不死不灭）
            if (_respawnTicks.Count > 0)
            {
                var done = new List<long>();
                foreach (var kv in _respawnTicks)
                {
                    int left = kv.Value - 20;
                    if (left <= 0)
                    {
                        done.Add(kv.Key);
                        RespawnPlayer(kv.Key);
                    }
                    else _respawnTicks[kv.Key] = left;
                }
                foreach (var id in done) _respawnTicks.Remove(id);
            }

            // 4. 任务到期结算
            for (int i = _data.tasks.Count - 1; i >= 0; i--)
            {
                var t = _data.tasks[i];
                if (t.settled) { _data.tasks.RemoveAt(i); continue; }
                if (_tick >= t.expiresTick) SettleTask(t);
            }

            // 5. 自动发布新任务（无进行中任务时，由最强文明发布）
            bool hasActive = false;
            foreach (var t in _data.tasks) if (!t.settled) { hasActive = true; break; }
            if (!hasActive && _data.tasks.Count == 0) PublishAutoTask();
        }

        private static void SpawnBatch()
        {
            try
            {
                int count = Random.Range(SpawnBatchMin, SpawnBatchMax + 1);
                for (int i = 0; i < count; i++)
                {
                    WorldTile tile = FindEdgeTile();
                    if (tile == null) continue;
                    Actor p = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
                    if (p == null) continue;
                    if (!p.hasTrait(PlayerTrait)) p.addTrait(PlayerTrait);
                    GetOrCreatePanel(p);
                    _data.spawnCounter++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 玩家降临生成失败: {e.Message}");
            }
        }

        private static void RespawnPlayer(long actorId)
        {
            try
            {
                WorldTile tile = FindEdgeTile();
                if (tile == null) return;
                Actor p = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
                if (p == null) return;
                if (!p.hasTrait(PlayerTrait)) p.addTrait(PlayerTrait);
                GetOrCreatePanel(p);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 降临者复活失败: {e.Message}");
            }
        }

        /// <summary>地图边缘虚空生成点（参照宇宙异兽）</summary>
        private static WorldTile FindEdgeTile()
        {
            if (World.world == null) return null;
            int w = MapBox.width, h = MapBox.height;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Random.value < 0.5f ? Random.Range(0, Mathf.Max(1, w / 8)) : Random.Range(w - w / 8, w);
                int y = Random.Range(0, h);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                WorldTile t = World.world.GetTile(x, y);
                if (t != null && t.Type != TileLibrary.deep_ocean && t.Type != TileLibrary.close_ocean && t.Type != TileLibrary.mountains)
                    return t;
            }
            return null;
        }

        // ============ 任务系统 ============

        /// <summary>降临者击杀：计入讨伐任务贡献与面板（战斗补丁调用）</summary>
        public static void OnPlayerKill(Actor killer, Actor target)
        {
            if (killer == null || !killer.hasTrait(PlayerTrait)) return;
            var panel = GetOrCreatePanel(killer);
            if (panel != null) panel.kills++;

            foreach (var t in _data.tasks)
            {
                if (t.settled || t.type != TaskType.Hunt) continue;
                t.progress = Mathf.Min(t.targetCount, t.progress + 1);
                int c = t.contribution.TryGetValue(killer.id, out int cur) ? cur : 0;
                t.contribution[killer.id] = c + (int)ContributionPerKill;
                if (panel != null) panel.contribution += (int)ContributionPerKill;
            }
        }

        private static void PublishAutoTask()
        {
            try
            {
                // 最强文明作为发布者（原著：韩萧/黑星军团发布任务）
                string publisher = "星海公会";
                int tech = 0;
                Kingdom top = FindStrongestKingdom();
                if (top != null)
                {
                    publisher = top.name;
                    tech = SuperMechCivilization.GetTechLevelFromKingdom(top);
                }

                // 单任务奖励上限（原著：最高奖励额度）
                float singleMax = PoolBase + tech * PoolPerTech;
                // 每日总额度=单任务上限×5（原著第57章）
                float dailyMax = singleMax * DailyBudgetMultiplier;
                if (_data.dailyBudgetUsed >= (int)dailyMax) return;

                float pool = Mathf.Min(singleMax * (0.6f + Random.value * 0.4f), dailyMax - _data.dailyBudgetUsed);
                if (pool < 100f) return;
                _data.dailyBudgetUsed += (int)pool;

                int targetCount = Mathf.Max(4, TaskTargetBase + tech / 10);
                var task = new TaskData
                {
                    id = "task_" + _data.nextTaskId++,
                    type = TaskType.Hunt,
                    targetLabel = "讨伐宇宙异兽与敌对单位",
                    targetCount = targetCount,
                    rewardPool = pool,
                    entryCost = pool * EntryCostRatio,
                    expiresTick = _tick + 2400,
                    publisherName = publisher
                };
                _data.tasks.Add(task);
                SuperMechEventBus.Publish("PlayerTaskPublished", task.id);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 任务发布失败: {e.Message}");
            }
        }

        /// <summary>结算：按贡献榜前五分配奖池（原著第130章），评级给额外奖励（原著第619章）</summary>
        private static void SettleTask(TaskData t)
        {
            if (t.settled) return;
            t.settled = true;

            // 评级：按完成目标比例（原著：按完成目标数评级给额外奖励）
            float ratio = t.targetCount > 0 ? (float)t.progress / t.targetCount : 0f;
            if (ratio >= 1f) t.grade = "S";
            else if (ratio >= 0.8f) t.grade = "A";
            else if (ratio >= 0.6f) t.grade = "B";
            else if (ratio >= 0.4f) t.grade = "C";
            else t.grade = "D";

            // 贡献榜前5（原著：按比例分配给贡献度前五的玩家/小队）
            var top = GetTopContributors(t, ContributionTopN);
            if (top.Count == 0) return;

            int totalContrib = 0;
            foreach (var kv in top) totalContrib += kv.Value;

            // 评级额外奖励=奖池×20%×评级系数
            float gradeCoeff = t.grade == "S" ? 1f : t.grade == "A" ? 0.8f : t.grade == "B" ? 0.6f : t.grade == "C" ? 0.4f : 0.2f;
            float extra = t.rewardPool * GradeExtraRatio * gradeCoeff;
            float poolTotal = t.rewardPool + extra;

            foreach (var kv in top)
            {
                if (totalContrib <= 0) break;
                float share = poolTotal * kv.Value / totalContrib;
                var a = FindActorById(kv.Key);
                if (a == null || !a.isAlive()) continue;
                // 经验奖励：转化为潜能（模组无全局经验池，按原著经验折现为潜能点）
                int potentialGain = Mathf.Max(1, (int)(share / 500f));
                SuperMechPotential.AddPotential(a, potentialGain);
            }

            Debug.Log($"[超神机械师] 任务[{t.id}]结算：评级{t.grade}，奖池{t.rewardPool:0}，贡献者{top.Count}人");
        }

        public static List<KeyValuePair<long, int>> GetTopContributors(TaskData t, int limit)
        {
            var list = new List<KeyValuePair<long, int>>();
            if (t == null) return list;
            foreach (var kv in t.contribution) list.Add(kv);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (limit > 0 && list.Count > limit) list.RemoveRange(limit, list.Count - limit);
            return list;
        }

        public static TaskData GetActiveTask()
        {
            foreach (var t in _data.tasks) if (!t.settled) return t;
            return null;
        }

        public static List<KeyValuePair<Actor, PlayerPanelData>> GetPanelRanking(int limit)
        {
            var result = new List<KeyValuePair<Actor, PlayerPanelData>>();
            foreach (var kv in _data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                var a = FindActorById(id);
                if (a == null || !a.isAlive()) continue;
                result.Add(new KeyValuePair<Actor, PlayerPanelData>(a, kv.Value));
            }
            result.Sort((x, y) => y.Value.kills.CompareTo(x.Value.kills));
            if (limit > 0 && result.Count > limit) result.RemoveRange(limit, result.Count - limit);
            return result;
        }

        private static Kingdom FindStrongestKingdom()
        {
            try
            {
                if (World.world == null || World.world.kingdoms == null || World.world.kingdoms.list == null) return null;
                Kingdom best = null;
                int bestLevel = -1;
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.wild || !k.isCiv()) continue;
                    int lv = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
                    if (lv > bestLevel) { bestLevel = lv; best = k; }
                }
                return best;
            }
            catch { return null; }
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units) if (a != null && a.id == id) return a;
            return null;
        }

        // ============ 死亡挂接（第四天灾：不死不灭） ============

        /// <summary>战斗补丁在单位死亡时调用：玩家进入复活队列</summary>
        public static void OnPlayerDeath(Actor dead)
        {
            if (dead == null || !dead.hasTrait(PlayerTrait)) return;
            _respawnTicks[dead.id] = RespawnTicks;
        }

        // ============ 存档 ============

        public static PlayerSaveData Save() => _data;

        public static void Load(PlayerSaveData data)
        {
            _data.panels.Clear();
            _data.tasks.Clear();
            if (data == null) return;
            if (data.panels != null)
                foreach (var kv in data.panels) _data.panels[kv.Key] = kv.Value;
            if (data.tasks != null)
                foreach (var t in data.tasks) _data.tasks.Add(t);
            _data.nextTaskId = data.nextTaskId;
            _data.dailyBudgetUsed = data.dailyBudgetUsed;
            _data.dailyResetTick = data.dailyResetTick;
            _data.spawnCounter = data.spawnCounter;
            _data.nextSpawnInTicks = data.nextSpawnInTicks;
        }

        public static void Clear()
        {
            _data.panels.Clear();
            _data.tasks.Clear();
            _respawnTicks.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<string>();
            foreach (var kv in _data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                if (!alive.Contains(id)) dead.Add(kv.Key);
            }
            foreach (var key in dead)
            {
                _data.panels.Remove(key);
                removed++;
            }
            return removed;
        }
    }
}
