using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.71.0 玩家降临·第四天灾（v0.75.5 清理剧情）
    /// v0.76.83 修正设定：所有降临者=超脱者化身，只有韩萧和地球玩家可投放
    ///   - 韩萧化身：唯一，机械系，更高初始属性，前世记忆
    ///   - 地球玩家化身：多个，随机五系职业，普通玩家水平
    /// 机制：降临者单位从地图边缘周期生成、死亡后复活（不死不灭）；最强文明自动发布讨伐任务；
    /// 任务额度=单次奖励上限+每日总额度=最高额度5倍、24h刷新（#57）；奖池任务=接取消耗经验、按贡献度前五分配（#130）；
    /// 按完成目标数评级给额外奖励（#619）；击杀结算经验/贡献挂在战斗补丁。
    /// </summary>
    public static class SuperMechPlayer
    {
        public const string PlayerTrait = "sm_player";      // 降临者标记（第四天灾玩家）
        public const string HanXiaoTrait = "sm_player_hanxiao"; // 韩萧化身标记（唯一）
        private static bool _hanxiaoSpawned = false;        // 韩萧化身是否已生成（唯一）

        /// <summary>原著风格固定玩家ID（高优先级，参考【肉包打狗】【狂刀怒剑】）</summary>
        private static readonly string[] EarthPlayerFixedIDs = {
            "肉包打狗", "狂刀怒剑", "极乐迪斯科", "飞云之下", "慕辰",
            "夜雨听风", "孤星逐日", "星海漫游者", "虚空行者", "量子幽灵",
            "次元旅者", "混沌钓叟", "极光之翼", "暗影刺客", "银河摆渡人",
            "时间拾荒者", "梦境编织者", "符文大师", "灵能学徒", "钛合金直男",
            "咸鱼翻身", "摸鱼真君", "社畜本畜", "卷王之王", "键盘侠",
            "嘴强王者", "理论大师", "实践矮子", "云玩家", "内测大佬",
            "公测萌新", "肝帝", "仓鼠党", "外观党", "成就猎人",
            "风景党", "剧情党", "PVP狂人", "PVE咸鱼", "副本刷子",
            "战场收割者", "公会会长", "散人玩家", "独行侠", "氪金大佬",
            "零充豹子头", "欧皇附体", "非酋本酋", "脸黑如碳", "手残党",
            "意识流", "走位风骚", "输出全靠吼", "躺赢专家", "下饭操作",
            "菜鸡互啄", "泉水指挥官", "团战祭品", "人头狗", "赛博朋克",
            "蒸汽朋克", "机械公敌", "黑客帝国", "盗梦空间", "星际穿越",
            "火星救援", "银翼杀手", "星球大战", "星际迷航", "铁血战士",
            "变形金刚", "环太平洋", "AI觉醒", "机甲驾驶员", "星舰指挥官",
            "清风徐来", "明月几时有", "把酒问青天", "落花人独立", "微雨燕双飞",
            "人生若只如初见", "何事秋风悲画扇", "一蓑烟雨任平生", "大漠孤烟直", "长河落日圆",
            "星垂平野阔", "月涌大江流", "天地一沙鸥", "会当凌绝顶", "一览众山小",
            "行到水穷处", "坐看云起时", "空山新雨后", "明月松间照", "清泉石上流",
            "麻辣烫", "小龙虾", "烧烤", "火锅", "奶茶",
            "肥宅快乐水", "薯片", "泡面", "炸鸡", "可乐",
            "橘猫", "哈士奇", "柴犬", "柯基", "布偶猫",
            "熊猫", "企鹅", "水獭", "海獭", "仓鼠",
            "卖女孩的小火柴", "采姑娘的小蘑菇", "偷井盖的贼", "抢银行的劫匪", "碰瓷的老大爷",
            "广场舞大妈", "小区保安", "外卖小哥", "快递员", "网约车司机",
            "程序猿", "产品经理", "设计师", "运营狗", "市场部",
            "甲方爸爸", "乙方孙子", "需求又变了", "这个需求很简单", "怎么实现我不管",
            "明天上线", "今晚加班", "bug修不完", "玩家007", "玩家404",
            "玩家520", "玩家666", "PlayerUnknown", "NoobMaster", "ProGamer",
        };

        /// <summary>已使用的网名（避免重复）</summary>
        private static readonly HashSet<string> _usedPlayerIDs = new HashSet<string>();

        // ==== 玩家面板（原著：生命/体力/六维属性/自由点/潜能/经验/专长） ====
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
            public int experience;        // 经验值（原著：接取任务消耗经验）
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
            public HashSet<long> accepted = new HashSet<long>(); // 已接取降临者（原著第130章：接取消耗经验）
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
            public HashSet<string> usedPlayerIDs = new HashSet<string>();   // 已使用的网名（防重复）
            public Dictionary<long, string> playerIDs = new Dictionary<long, string>(); // actorId -> 网名
            public bool hanxiaoSpawned;  // 韩萧化身是否已生成（唯一）
        }

        private static readonly PlayerSaveData _data = new PlayerSaveData();
        public static PlayerSaveData Data => _data;

        // 复活队列：actorId -> 剩余复活tick（第四天灾：玩家不死不灭）
        private static readonly Dictionary<long, int> _respawnTicks = new Dictionary<long, int>();
        // 玩家ID记录：actorId -> 玩家网名（复活时恢复）
        private static readonly Dictionary<long, string> _playerIDs = new Dictionary<long, string>();
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
                    freePoints = 5, potential = 1, experience = 1000
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

            // 4. 任务到期结算 + 降临者接取任务（原著第130章：接取消耗经验，奖池按贡献度前五分配）
            for (int i = _data.tasks.Count - 1; i >= 0; i--)
            {
                var t = _data.tasks[i];
                if (t.settled) { _data.tasks.RemoveAt(i); continue; }
                if (_tick >= t.expiresTick) SettleTask(t);
                else AcceptTaskByPlayers(t); // 未到期的任务：在册降临者自动接取（接取消耗经验）
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
                // 韩萧化身：唯一，首次生成时创建
                if (!_hanxiaoSpawned)
                {
                    _hanxiaoSpawned = true;
                    SpawnHanXiaoAvatar();
                }

                int count = Random.Range(SpawnBatchMin, SpawnBatchMax + 1);
                for (int i = 0; i < count; i++)
                {
                    SpawnEarthPlayerAvatar();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 玩家降临生成失败: {e.Message}");
            }
        }

        /// <summary>韩萧化身：唯一，机械系，更高初始属性，前世记忆</summary>
        private static void SpawnHanXiaoAvatar()
        {
            WorldTile tile = FindEdgeTile();
            if (tile == null) return;
            Actor p = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
            if (p == null) return;
            // 第一步：设置名字（降临者直接就是玩家身份，不是夺舍）
            p.data.name = "黑星";
            _playerIDs[p.id] = "黑星";
            _usedPlayerIDs.Add("黑星");
            // 然后添加特质
            if (!p.hasTrait(PlayerTrait)) p.addTrait(PlayerTrait);
            if (!p.hasTrait(HanXiaoTrait)) p.addTrait(HanXiaoTrait);
            if (!p.hasTrait("sm_awakened")) p.addTrait("sm_awakened");
            if (!p.data.favorite) p.switchFavorite();
            if (!p.hasTrait("infertile")) p.addTrait("infertile");
            if (!p.hasTrait("sm_rank_00_f")) p.addTrait("sm_rank_00_f");
            SuperMechAdvancement.SetExactRank(p, 0);
            SuperMechProfession.SetProfession(p, SuperMechProfession.ProfessionType.Mechanical);
            SuperMechPotential.AddPotential(p, 10);
            // 初始化气力（韩萧前世记忆，初始气力更高）
            if (SuperMechQi.GetQiMax(p) <= 0f)
            {
                SuperMechQi.SetQiMax(p, 200f);
                SuperMechQi.SetQi(p, 200f);
            }
            GetOrCreatePanel(p);
            var panel = GetOrCreatePanel(p);
            if (panel != null)
            {
                panel.strength = 15; panel.agility = 15; panel.endurance = 15;
                panel.intelligence = 20; panel.mystery = 10; panel.charm = 10; panel.luck = 15;
                panel.freePoints = 10; panel.potential = 5; panel.experience = 5000;
            }
            _data.spawnCounter++;
            Debug.Log("[超神机械师] 韩萧化身已降临");
        }

        /// <summary>地球玩家化身：多个，随机五系职业，普通玩家水平</summary>
        private static void SpawnEarthPlayerAvatar()
        {
            WorldTile tile = FindEdgeTile();
            if (tile == null) return;
            // 先选好玩家ID（生成单位后立即设置，避免WorldBox默认名字闪烁）
            string playerID = PickEarthPlayerID();
            Actor p = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
            if (p == null) return;
            // 第一步：设置名字（降临者直接就是玩家身份，不是夺舍）
            p.data.name = playerID;
            _playerIDs[p.id] = playerID;
            _usedPlayerIDs.Add(playerID);
            // 然后添加特质
            if (!p.hasTrait(PlayerTrait)) p.addTrait(PlayerTrait);
            if (!p.hasTrait("sm_awakened")) p.addTrait("sm_awakened");
            if (!p.data.favorite) p.switchFavorite();
            if (!p.hasTrait("infertile")) p.addTrait("infertile");
            if (!p.hasTrait("sm_rank_00_f")) p.addTrait("sm_rank_00_f");
            SuperMechAdvancement.SetExactRank(p, 0);
            AssignRandomClass(p);
            // 初始化气力（F阶标准100点，避免CalcOnar返回0导致排行榜垫底）
            if (SuperMechQi.GetQiMax(p) <= 0f)
            {
                SuperMechQi.SetQiMax(p, 100f);
                SuperMechQi.SetQi(p, 100f);
            }
            GetOrCreatePanel(p);
            _data.spawnCounter++;
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] 地球玩家降临：{playerID}");
        }

        /// <summary>选取地球玩家网名（正经人谁实名上网，原著风格ID）</summary>
        private static string PickEarthPlayerID()
        {
            string playerID = null;
            var available = new List<string>();
            foreach (var id in EarthPlayerFixedIDs)
            {
                if (!_usedPlayerIDs.Contains(id)) available.Add(id);
            }
            if (available.Count > 0)
            {
                playerID = available[Random.Range(0, available.Count)];
            }
            else
            {
                string baseID = EarthPlayerFixedIDs[Random.Range(0, EarthPlayerFixedIDs.Length)];
                playerID = baseID + Random.Range(1, 9999);
            }
            return playerID;
        }

        /// <summary>随机分配五系职业</summary>
        private static void AssignRandomClass(Actor p)
        {
            var classes = new[] {
                SuperMechProfession.ProfessionType.Mechanical,
                SuperMechProfession.ProfessionType.Mind,
                SuperMechProfession.ProfessionType.Psi,
                SuperMechProfession.ProfessionType.Martial,
                SuperMechProfession.ProfessionType.Mage
            };
            var cls = classes[Random.Range(0, classes.Length)];
            SuperMechProfession.SetProfession(p, cls);
        }

        private static void RespawnPlayer(long actorId)
        {
            try
            {
                WorldTile tile = FindEdgeTile();
                if (tile == null) return;
                Actor p = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
                if (p == null) return;
                // 复用原单位内部ID（原著：玩家复活是同一个角色，同一个账号）
                p.data.id = actorId;
                // 第一步：恢复名字（降临者直接就是玩家身份，不是夺舍）
                if (_playerIDs.TryGetValue(actorId, out string playerID))
                {
                    p.data.name = playerID;
                }
                if (!p.hasTrait(PlayerTrait)) p.addTrait(PlayerTrait);
                if (!p.hasTrait("sm_awakened")) p.addTrait("sm_awakened");
                // 金色名字 + 不育
                if (!p.data.favorite) p.switchFavorite();
                if (!p.hasTrait("infertile")) p.addTrait("infertile");
                // 恢复阶位特质（复用ID后_exactRank自动关联，但特质需要重新添加）
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(p);
                if (rankIdx >= 0 && rankIdx < SuperMechRanks.All.Count)
                {
                    string rankId = SuperMechRanks.All[rankIdx].id;
                    if (!p.hasTrait(rankId)) p.addTrait(rankId);
                }
                // 恢复体系特质（复用ID后_profession字典自动关联，但特质需要重新添加）
                var prof = SuperMechProfession.GetProfession(p);
                if (prof != SuperMechProfession.ProfessionType.None)
                {
                    switch (prof)
                    {
                        case SuperMechProfession.ProfessionType.Mechanical:
                            if (!p.hasTrait(SuperMechTraits.ClassMech)) p.addTrait(SuperMechTraits.ClassMech);
                            break;
                        case SuperMechProfession.ProfessionType.Martial:
                            if (!p.hasTrait(SuperMechTraits.ClassMartial)) p.addTrait(SuperMechTraits.ClassMartial);
                            break;
                        case SuperMechProfession.ProfessionType.Psi:
                            if (!p.hasTrait(SuperMechTraits.ClassPsi)) p.addTrait(SuperMechTraits.ClassPsi);
                            break;
                        case SuperMechProfession.ProfessionType.Mage:
                            if (!p.hasTrait(SuperMechTraits.ClassMage)) p.addTrait(SuperMechTraits.ClassMage);
                            break;
                        case SuperMechProfession.ProfessionType.Mind:
                            if (!p.hasTrait(SuperMechTraits.ClassMind)) p.addTrait(SuperMechTraits.ClassMind);
                            break;
                    }
                }
                // 初始化气力（如果为0）
                if (SuperMechQi.GetQiMax(p) <= 0f)
                {
                    SuperMechQi.SetQiMax(p, 100f);
                    SuperMechQi.SetQi(p, 100f);
                }
                // 面板数据因为复用ID自动关联
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

        /// <summary>在册降临者自动接取进行中任务（原著第130章：接取消耗经验；经验不足则扣至0）</summary>
        private static void AcceptTaskByPlayers(TaskData t)
        {
            if (t == null || t.settled) return;
            foreach (var kv in _data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                if (t.accepted.Contains(id)) continue;
                var a = FindActorById(id);
                if (a == null || !a.isAlive()) continue;

                var panel = kv.Value;
                int cost = Mathf.Max(1, (int)t.entryCost);
                int actual = Mathf.Min(cost, panel.experience);
                panel.experience -= actual;
                t.accepted.Add(id);
            }
        }

        /// <summary>降临者击杀：计入讨伐任务贡献与面板（战斗补丁调用）</summary>
        public static void OnPlayerKill(Actor killer, Actor target)
        {
            if (killer == null || !killer.hasTrait(PlayerTrait)) return;
            var panel = GetOrCreatePanel(killer);
            if (panel != null) { panel.kills++; panel.experience += 10; } // 击杀经验（攒经验→接取任务消耗，闭环）

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
                // 最强文明作为任务发布者
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
                    targetLabel = "讨伐敌对单位",
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

        public static PlayerSaveData Save()
        {
            _data.usedPlayerIDs = new HashSet<string>(_usedPlayerIDs);
            _data.playerIDs = new Dictionary<long, string>(_playerIDs);
            _data.hanxiaoSpawned = _hanxiaoSpawned;
            return _data;
        }

        public static void Load(PlayerSaveData data)
        {
            _data.panels.Clear();
            _data.tasks.Clear();
            _usedPlayerIDs.Clear();
            _playerIDs.Clear();
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
            if (data.usedPlayerIDs != null)
                foreach (var id in data.usedPlayerIDs) _usedPlayerIDs.Add(id);
            if (data.playerIDs != null)
                foreach (var kv in data.playerIDs) _playerIDs[kv.Key] = kv.Value;
            _hanxiaoSpawned = data.hanxiaoSpawned;
        }

        public static void Clear()
        {
            _data.panels.Clear();
            _data.tasks.Clear();
            _respawnTicks.Clear();
            _usedPlayerIDs.Clear();
            _playerIDs.Clear();
            _hanxiaoSpawned = false;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<string>();
            foreach (var kv in _data.panels)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                // 跳过正在复活队列中的玩家（不死不灭，面板保留）
                if (_respawnTicks.ContainsKey(id)) continue;
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
