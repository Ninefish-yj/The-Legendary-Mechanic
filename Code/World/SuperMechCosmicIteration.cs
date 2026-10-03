using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 宇宙迭代模拟系统：记录宇宙迭代次数，大重启时保留圣所遗产
    /// 原著中宇宙会经历多次迭代，圣所记录每轮文明的知识和信息态
    /// 包含暗面宇宙子迭代、信息态剥离计划、世界树事件等原著设定
    /// </summary>
    public static class SuperMechCosmicIteration
    {
        /// <summary>迭代阶段（原著宇宙演化阶段）</summary>
        public enum IterationPhase
        {
            Initial = 0,           // 最初时空（终极文明将毁灭改为大重启）
            Savior = 1,            // 救世主文明阶段（某个迭代的终极文明留下圣所）
            ThreeCivilizations = 2, // 三大文明阶段（继承重启计划）
            DarkUniverse = 3,      // 暗面宇宙阶段（信息态剥离计划，三次小重启）
            PlayerUniverse = 4     // 玩家宇宙（当前迭代）
        }

        /// <summary>当前宇宙迭代次数</summary>
        public static int CurrentIteration { get; private set; } = 0;

        /// <summary>当前迭代阶段</summary>
        public static IterationPhase CurrentPhase { get; private set; } = IterationPhase.PlayerUniverse;

        /// <summary>暗面宇宙迭代次数（原著：三大文明在暗面经历三次迭代）</summary>
        public static int DarkUniverseIteration { get; private set; } = 0;

        /// <summary>暗面宇宙最大迭代次数</summary>
        public const int MaxDarkUniverseIteration = 3;

        /// <summary>信息态剥离计划是否已开启（原著：三大文明开启该计划进入暗面宇宙）</summary>
        public static bool InformationStrippingPlanStarted { get; private set; } = false;

        /// <summary>世界树是否已诞生（原著：暗面宇宙信息态技术创造的宇宙宝物）</summary>
        public static bool WorldTreeCreated { get; private set; } = false;

        /// <summary>世界树成长等级（0~10）</summary>
        public static int WorldTreeLevel { get; private set; } = 0;

        /// <summary>遗产保留率基础值（0~1），实际保留率随机波动并受圣所权限影响</summary>
        public const float BaseHeritageRetentionRate = 0.3f;

        /// <summary>遗产保留率最大波动范围（±20%）</summary>
        public const float HeritageRetentionFluctuation = 0.2f;

        /// <summary>圣所权限对保留率的加成（每100点权限+5%）</summary>
        public const float AuthorityRetentionBonus = 0.0005f;

        /// <summary>是否已初始化</summary>
        private static bool _initialized = false;

        /// <summary>上一轮的遗产（跨迭代继承的数据）</summary>
        public class HeritageData
        {
            public int iteration;              // 来自第几轮
            public int retainedFragments;      // 保留的圣所钥匙
            public int retainedMaterials;      // 保留的钥匙材料（v0.46.0）
            public int retainedAuthority;      // 保留的权限
            public List<string> retainedKnowledge; // 保留的知识ID
            public string summary;             // 上一轮文明总结
            public bool fromDarkUniverse;      // 是否来自暗面宇宙
        }

        /// <summary>暗面宇宙文明记录</summary>
        public class DarkUniverseCivilization
        {
            public int iteration;              // 暗面迭代次数（1~3）
            public string civilizationName;    // 文明名称
            public int totalAwakened;          // 觉醒数
            public int maxRankReached;         // 最高阶位
            public string summary;             // 文明总结
            public bool worldTreeCreated;      // 本轮是否创造世界树
        }

        private static HeritageData _pendingHeritage;
        private static readonly List<DarkUniverseCivilization> _darkUniverseHistory = new List<DarkUniverseCivilization>();

        /// <summary>初始化宇宙迭代系统（加载世界时调用）</summary>
        public static void Initialize(int savedIteration, string savedCivilizationJson)
        {
            if (_initialized) return;
            _initialized = true;

            CurrentIteration = savedIteration;
            SuperMechCivilizationData.Deserialize(savedCivilizationJson);

            // 开始新一轮文明记录
            SuperMechCivilizationData.StartIteration(CurrentIteration);

            // 应用上一轮遗产
            ApplyHeritage();

            Debug.Log($"[超神机械师] 宇宙迭代系统初始化: 第{CurrentIteration}轮，历史记录{SuperMechCivilizationData.GetHistory().Count}轮");
        }

        /// <summary>开启信息态剥离计划（原著：三大文明开启该计划进入暗面宇宙）
        /// 触发条件：圣所权限达到阈值，且当前阶段为三大文明阶段
        /// </summary>
        public static bool StartInformationStrippingPlan()
        {
            if (InformationStrippingPlanStarted) return false;
            if (SuperMechSanctuary.Data.total_permission < 10000)
            {
                Debug.Log("[超神机械师] 信息态剥离计划启动失败：圣所权限不足（需10000）");
                return false;
            }

            InformationStrippingPlanStarted = true;
            CurrentPhase = IterationPhase.DarkUniverse;
            DarkUniverseIteration = 1;

            // 记录暗面宇宙第一轮文明
            _darkUniverseHistory.Add(new DarkUniverseCivilization
            {
                iteration = 1,
                civilizationName = "三大文明暗面第一迭代",
                totalAwakened = 0,
                maxRankReached = 0,
                summary = "三大文明开启信息态剥离计划，进入暗面宇宙",
                worldTreeCreated = false
            });

            Debug.Log("[超神机械师] 信息态剥离计划启动！三大文明进入暗面宇宙，开始第一次暗面迭代");
            return true;
        }

        /// <summary>触发暗面宇宙小重启（原著：暗面宇宙经历三次迭代）
        /// 每次暗面重启有机会创造世界树
        /// </summary>
        public static bool TriggerDarkUniverseRestart()
        {
            if (!InformationStrippingPlanStarted) return false;
            if (DarkUniverseIteration >= MaxDarkUniverseIteration)
            {
                Debug.Log("[超神机械师] 暗面宇宙已完成三次迭代，无法继续重启");
                return false;
            }

            // 结束当前暗面文明记录
            if (_darkUniverseHistory.Count > 0)
            {
                var last = _darkUniverseHistory[_darkUniverseHistory.Count - 1];
                last.totalAwakened = SuperMechCivilizationData.GetHistory().Count > 0
                    ? SuperMechCivilizationData.GetHistory()[SuperMechCivilizationData.GetHistory().Count - 1].totalAwakened
                    : 0;
                last.maxRankReached = SuperMechCivilizationData.GetHistory().Count > 0
                    ? SuperMechCivilizationData.GetHistory()[SuperMechCivilizationData.GetHistory().Count - 1].maxRankReached
                    : 0;
            }

            DarkUniverseIteration++;

            // 第二次暗面迭代有30%概率创造世界树
            if (DarkUniverseIteration == 2 && !WorldTreeCreated && Random.value < 0.3f)
            {
                CreateWorldTree();
            }

            // 记录新一轮暗面文明
            _darkUniverseHistory.Add(new DarkUniverseCivilization
            {
                iteration = DarkUniverseIteration,
                civilizationName = $"三大文明暗面第{DarkUniverseIteration}迭代",
                totalAwakened = 0,
                maxRankReached = 0,
                summary = DarkUniverseIteration == MaxDarkUniverseIteration
                    ? "暗面宇宙最终迭代，三大文明准备回归真实宇宙"
                    : $"暗面宇宙第{DarkUniverseIteration}次迭代",
                worldTreeCreated = WorldTreeCreated
            });

            Debug.Log($"[超神机械师] 暗面宇宙小重启！进入第{DarkUniverseIteration}次暗面迭代");
            return true;
        }

        /// <summary>创造世界树（原著：暗面宇宙信息态技术创造的宇宙宝物）
        /// 世界树可以加速信息态恢复，提升遗产保留率
        /// </summary>
        public static bool CreateWorldTree()
        {
            if (WorldTreeCreated) return false;
            if (!InformationStrippingPlanStarted) return false;

            WorldTreeCreated = true;
            WorldTreeLevel = 1;

            Debug.Log("[超神机械师] 世界树诞生！暗面宇宙信息态技术结晶，遗产保留率+10%");
            return true;
        }

        /// <summary>世界树成长（消耗圣所权限，提升遗产保留率加成）</summary>
        public static bool GrowWorldTree()
        {
            if (!WorldTreeCreated) return false;
            if (WorldTreeLevel >= 10) return false;
            if (SuperMechSanctuary.Data.total_permission < 5000 * WorldTreeLevel) return false;

            SuperMechSanctuary.Data.total_permission -= 5000 * WorldTreeLevel;
            WorldTreeLevel++;

            Debug.Log($"[超神机械师] 世界树成长至Lv{WorldTreeLevel}，遗产保留率+{WorldTreeLevel * 2}%");
            return true;
        }

        /// <summary>获取世界树对遗产保留率的加成</summary>
        public static float GetWorldTreeRetentionBonus()
        {
            if (!WorldTreeCreated) return 0f;
            return WorldTreeLevel * 0.02f; // 每级+2%
        }

        /// <summary>获取暗面宇宙历史记录</summary>
        public static List<DarkUniverseCivilization> GetDarkUniverseHistory()
        {
            return _darkUniverseHistory;
        }

        /// <summary>完成暗面宇宙迭代，回归真实宇宙（原著：暗面三次迭代后真实宇宙进入新迭代）</summary>
        public static bool CompleteDarkUniverseCycle()
        {
            if (!InformationStrippingPlanStarted) return false;
            if (DarkUniverseIteration < MaxDarkUniverseIteration) return false;

            CurrentPhase = IterationPhase.PlayerUniverse;
            Debug.Log("[超神机械师] 暗面宇宙三次迭代完成，三大文明回归，真实宇宙进入新迭代");
            return true;
        }

        /// <summary>触发大重启（世界重置时调用）</summary>
        public static void TriggerGreatRestart()
        {
            if (!_initialized) return;

            // 结束当前文明记录
            SuperMechCivilizationData.EndIteration();

            // 计算遗产
            _pendingHeritage = CalculateHeritage();

            // 迭代次数+1
            CurrentIteration++;

            Debug.Log($"[超神机械师] 宇宙大重启! 进入第{CurrentIteration}轮迭代，遗产碎片{_pendingHeritage.retainedFragments}");

            // 推送事件日志
            float retentionRate = CalculateRetentionRate();
            string civSummary = "";
            var history = SuperMechCivilizationData.GetHistory();
            if (history.Count > 0)
            {
                var last = history[history.Count - 1];
                string rankName = (last.maxRankReached >= 0 && last.maxRankReached < SuperMechRanks.All.Count)
                    ? LocalizedTextManager.getText(SuperMechRanks.All[last.maxRankReached].name)
                    : LocalizedTextManager.getText("sm_civ_none");
                civSummary = string.Format(LocalizedTextManager.getText("sm_event_civ_reset"),
                    last.iteration, last.totalAwakened, rankName);
            }
            SuperMechEventLogger.LogIteration(CurrentIteration, retentionRate, civSummary);

            // 清除世界单位（保留降临者）
            ResetWorldForNewIteration();

            // 应用遗产到新世界
            ApplyHeritage();

            // 开始新一轮文明记录
            SuperMechCivilizationData.StartIteration(CurrentIteration);
        }

        /// <summary>大重启时重置世界：清除非降临者单位，降临者重置阶位/知识/气力</summary>
        private static void ResetWorldForNewIteration()
        {
            if (World.world == null || World.world.units == null) return;

            int killed = 0, reset = 0;
            var toKill = new System.Collections.Generic.List<Actor>();

            foreach (var a in World.world.units)
            {
                if (a == null || !a.isAlive()) continue;
                if (a.hasTrait(SuperMechTraits.Descendant))
                {
                    // 降临者保留，但重置成长
                    SuperMechAdvancement.SetExactRank(a, 0);
                    SuperMechQi.SetQi(a, 100f);
                    SuperMechQi.SetQiMax(a, 100f);
                    SuperMechKnowledge.ClearActor(a);
                    reset++;
                }
                else
                {
                    toKill.Add(a);
                }
            }

            foreach (var a in toKill)
            {
                try { a.die(pDestroy: true, AttackType.None, pCountDeath: false); } catch { }
                killed++;
            }

            // 清理传承数据（旧单位ID失效）
            SuperMechHeritage.Clear();

            Debug.Log($"[超神机械师] 大重启世界重置: 清除{killed}个单位，重置{reset}个降临者");
        }

        /// <summary>计算实际遗产保留率（随机波动+圣所权限加成）
        /// 原著：遗产保留不是固定比例，而是随机抽取部分信息融入，与圣所权限、迭代深度、文明等级相关
        /// </summary>
        private static float CalculateRetentionRate()
        {
            // 基础随机波动：±20%
            float randomFactor = Random.Range(-HeritageRetentionFluctuation, HeritageRetentionFluctuation);
            // 圣所权限加成：每100点权限+5%
            float authorityBonus = SuperMechSanctuary.Data.total_permission * AuthorityRetentionBonus;
            // 迭代深度加成：迭代越深，圣所积累越多，保留率越高
            float iterationBonus = CurrentIteration * 0.01f;
            // 世界树加成：暗面宇宙信息态技术产物，每级+2%
            float worldTreeBonus = GetWorldTreeRetentionBonus();
            return Mathf.Clamp01(BaseHeritageRetentionRate + randomFactor + authorityBonus + iterationBonus + worldTreeBonus);
        }

        /// <summary>计算本轮遗产（大重启时保留的资源）</summary>
        private static HeritageData CalculateHeritage()
        {
            float retentionRate = CalculateRetentionRate();

            var heritage = new HeritageData
            {
                iteration = CurrentIteration,
                retainedFragments = Mathf.FloorToInt(SuperMechSanctuary.Data.key_fragments * retentionRate),
                retainedMaterials = Mathf.FloorToInt(SuperMechSanctuary.Data.key_materials * retentionRate),
                retainedAuthority = Mathf.FloorToInt(SuperMechSanctuary.Data.total_permission * retentionRate),
                retainedKnowledge = new List<string>(),
                summary = SuperMechCivilizationData.GetHistory().Count > 0
                    ? SuperMechCivilizationData.GetHistory()[SuperMechCivilizationData.GetHistory().Count - 1].summary
                    : "",
            };

            // 随机保留部分知识（最多10条，随机抽取而非按比例）
            if (World.world != null && World.world.units != null)
            {
                var allKnowledge = new HashSet<string>();
                foreach (var a in World.world.units)
                {
                    if (a == null) continue;
                    var unlocked = SuperMechKnowledge.GetUnlockedList(a, "mech");
                    if (unlocked != null)
                    {
                        foreach (var k in unlocked) allKnowledge.Add(k.id);
                    }
                }
                int retainCount = Mathf.Min(10, Mathf.FloorToInt(allKnowledge.Count * retentionRate));
                var list = new List<string>(allKnowledge);
                // Fisher-Yates随机打乱
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    string temp = list[i]; list[i] = list[j]; list[j] = temp;
                }
                for (int i = 0; i < retainCount && i < list.Count; i++)
                {
                    heritage.retainedKnowledge.Add(list[i]);
                }
            }

            Debug.Log($"[超神机械师] 遗产保留率: {retentionRate:F0%} (随机+权限加成)");
            return heritage;
        }

        /// <summary>应用上一轮遗产到新世界</summary>
        private static void ApplyHeritage()
        {
            if (_pendingHeritage == null) return;

            // 保留钥匙和材料
            SuperMechSanctuary.Data.key_fragments += _pendingHeritage.retainedFragments;
            SuperMechSanctuary.Data.key_materials += _pendingHeritage.retainedMaterials;

            Debug.Log($"[超神机械师] 遗产继承: 来自第{_pendingHeritage.iteration}轮，钥匙+{_pendingHeritage.retainedFragments}，材料+{_pendingHeritage.retainedMaterials}，知识+{_pendingHeritage.retainedKnowledge.Count}条");
            _pendingHeritage = null;
        }

        /// <summary>获取待应用的遗产（用于UI显示）</summary>
        public static HeritageData GetPendingHeritage()
        {
            return _pendingHeritage;
        }

        /// <summary>获取迭代统计信息</summary>
        public static string GetStatsText()
        {
            var history = SuperMechCivilizationData.GetHistory();
            int totalAwakened = 0;
            int maxRank = 0;
            foreach (var h in history)
            {
                totalAwakened += h.totalAwakened;
                if (h.maxRankReached > maxRank) maxRank = h.maxRankReached;
            }

            string phaseText = CurrentPhase switch
            {
                IterationPhase.Initial => LocalizedTextManager.getText("sm_iter_phase_initial"),
                IterationPhase.Savior => LocalizedTextManager.getText("sm_iter_phase_savior"),
                IterationPhase.ThreeCivilizations => LocalizedTextManager.getText("sm_iter_phase_three"),
                IterationPhase.DarkUniverse => LocalizedTextManager.getText("sm_iter_phase_dark"),
                IterationPhase.PlayerUniverse => LocalizedTextManager.getText("sm_iter_phase_player"),
                _ => LocalizedTextManager.getText("sm_iter_phase_unknown")
            };

            string tCurIter = LocalizedTextManager.getText("sm_iter_cur_iteration");
            string tCurPhase = LocalizedTextManager.getText("sm_iter_cur_phase");
            string tHistory = LocalizedTextManager.getText("sm_iter_history");
            string tTotalAwakened = LocalizedTextManager.getText("sm_iter_total_awakened");
            string tMaxRank = LocalizedTextManager.getText("sm_iter_max_rank");
            string tNone = LocalizedTextManager.getText("sm_iter_none");
            string tHeritage = LocalizedTextManager.getText("sm_iter_heritage");
            string tWorldTree = LocalizedTextManager.getText("sm_iter_world_tree");

            string stats = $"{tCurIter}: {CurrentIteration}\n" +
                $"{tCurPhase}: {phaseText}\n" +
                $"{tHistory}: {history.Count}\n" +
                $"{tTotalAwakened}: {totalAwakened}\n" +
                $"{tMaxRank}: {(maxRank >= 0 && maxRank < SuperMechRanks.All.Count ? LocalizedTextManager.getText(SuperMechRanks.All[maxRank].name) : tNone)}\n" +
                $"{tHeritage}: {BaseHeritageRetentionRate * 100:F0}%{(WorldTreeCreated ? $"+{tWorldTree}{GetWorldTreeRetentionBonus() * 100:F0}%" : "")}";

            if (InformationStrippingPlanStarted)
            {
                string tStripping = LocalizedTextManager.getText("sm_iter_stripping");
                string tDarkIter = LocalizedTextManager.getText("sm_iter_dark_iteration");
                string tDarkHistory = LocalizedTextManager.getText("sm_iter_dark_history");
                stats += $"\n{tStripping}\n" +
                    $"{tDarkIter}: {DarkUniverseIteration}/{MaxDarkUniverseIteration}\n" +
                    $"{tDarkHistory}: {_darkUniverseHistory.Count}";
            }
            if (WorldTreeCreated)
            {
                string tTreeLevel = LocalizedTextManager.getText("sm_iter_tree_level");
                stats += $"\n{tTreeLevel}: Lv{WorldTreeLevel}/10";
            }

            return stats;
        }

        /// <summary>重置系统（新世界创建时）</summary>
        public static void Reset()
        {
            _initialized = false;
            CurrentIteration = 0;
            CurrentPhase = IterationPhase.PlayerUniverse;
            DarkUniverseIteration = 0;
            InformationStrippingPlanStarted = false;
            WorldTreeCreated = false;
            WorldTreeLevel = 0;
            _pendingHeritage = null;
            _darkUniverseHistory.Clear();
            SuperMechCivilizationData.Clear();
        }

        /// <summary>定期更新文明统计（每60秒调用一次）</summary>
        public static void TickUpdate()
        {
            if (!_initialized) return;
            SuperMechCivilizationData.UpdateStats();
        }
    }
}
