using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 世界之外（暗面宇宙）系统：信息态剥离计划、暗面迭代、世界树诞生
    /// 原著设定：三大文明为规避大重启，剥离全宇宙信息态开辟暗面宇宙，经历三次小重启
    /// 暗面宇宙与真实宇宙重合时发生异变，暗面系升维为超脱存在
    /// </summary>
    public static class SuperMechWorldBeyond
    {
        /// <summary>暗面宇宙迭代阶段</summary>
        public enum BeyondPhase
        {
            None = 0,               // 未进入暗面宇宙
            InformationStripping = 1, // 信息态剥离中
            DarkIteration = 2,      // 暗面迭代中
            Merging = 3,            // 与真实宇宙重合中
            Transcended = 4         // 已升维（超脱存在）
        }

        /// <summary>当前暗面宇宙阶段</summary>
        public static BeyondPhase CurrentPhase { get; private set; } = BeyondPhase.None;

        /// <summary>暗面宇宙迭代次数（原著：三大文明在暗面经历三次迭代）</summary>
        public static int DarkIteration { get; private set; } = 0;

        /// <summary>暗面宇宙最大迭代次数</summary>
        public const int MaxDarkIteration = 3;

        /// <summary>信息态剥离计划是否已开启</summary>
        public static bool IsStrippingStarted { get; private set; } = false;

        /// <summary>世界树是否已诞生（原著：暗面宇宙信息态技术创造的宇宙宝物）</summary>
        public static bool WorldTreeBorn { get; private set; } = false;

        /// <summary>世界树成长等级（0~10）</summary>
        public static int WorldTreeLevel { get; private set; } = 0;

        /// <summary>暗面宇宙文明记录</summary>
        public class DarkCivilization
        {
            public int iteration;              // 暗面迭代次数（1~3）
            public string civilizationName;    // 文明名称
            public int totalAwakened;          // 觉醒数
            public int maxRankReached;         // 最高阶位
            public string summary;             // 文明总结
            public bool worldTreeCreated;      // 本轮是否创造世界树
        }

        private static readonly List<DarkCivilization> _darkHistory = new List<DarkCivilization>();
        private static bool _initialized = false;

        /// <summary>初始化世界之外系统（预置前代暗面宇宙历史）</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 预置前代暗面宇宙历史（三大文明前代路线=背景设定，玩家世界不驱动暗面链）：
            // 信息态剥离→暗面迭代×3→世界树（第二次暗面迭代30%诞生）→与真实宇宙重合
            if (_darkHistory.Count == 0)
            {
                _darkHistory.Add(new DarkCivilization
                { iteration = 1, civilizationName = "三大文明·信息态剥离", totalAwakened = 0, maxRankReached = 0,
                  summary = "三大文明为规避大重启，剥离全宇宙信息态，开辟暗面宇宙", worldTreeCreated = false });
                _darkHistory.Add(new DarkCivilization
                { iteration = 2, civilizationName = "三大文明·暗面第二迭代", totalAwakened = 0, maxRankReached = 0,
                  summary = "暗面宇宙第二次迭代，30%概率诞生世界树（信息态技术结晶）", worldTreeCreated = true });
                _darkHistory.Add(new DarkCivilization
                { iteration = 3, civilizationName = "三大文明·暗面第三迭代", totalAwakened = 0, maxRankReached = 0,
                  summary = "暗面宇宙最终迭代，暗面系准备回归真实宇宙", worldTreeCreated = true });
                _darkHistory.Add(new DarkCivilization
                { iteration = 4, civilizationName = "三大文明·重合异变", totalAwakened = 0, maxRankReached = 0,
                  summary = "暗面与真实宇宙重合瞬间异变，暗面系升维为超脱存在（手段可学、异变不可求）", worldTreeCreated = true });
                WorldTreeBorn = true;
                WorldTreeLevel = 3;
                DarkIteration = 3;
                CurrentPhase = BeyondPhase.Transcended;
            }

            Debug.Log("[超神机械师] 世界之外系统初始化: 暗面宇宙历史已预置");
        }

        /// <summary>开启信息态剥离计划
        /// 触发条件：圣所权限达到10000
        /// </summary>
        public static bool StartInformationStripping()
        {
            if (IsStrippingStarted) return false;
            if (SuperMechSanctuary.Data.total_permission < 10000)
            {
                Debug.Log("[超神机械师] 信息态剥离计划启动失败：圣所权限不足（需10000）");
                return false;
            }

            IsStrippingStarted = true;
            CurrentPhase = BeyondPhase.InformationStripping;
            DarkIteration = 1;

            _darkHistory.Add(new DarkCivilization
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
        public static bool TriggerDarkRestart()
        {
            if (!IsStrippingStarted) return false;
            if (DarkIteration >= MaxDarkIteration)
            {
                Debug.Log("[超神机械师] 暗面宇宙已完成三次迭代，无法继续重启");
                return false;
            }

            // 结束当前暗面文明记录
            if (_darkHistory.Count > 0)
            {
                var last = _darkHistory[_darkHistory.Count - 1];
                var civHistory = SuperMechCivilizationData.GetHistory();
                last.totalAwakened = civHistory.Count > 0 ? civHistory[civHistory.Count - 1].totalAwakened : 0;
                last.maxRankReached = civHistory.Count > 0 ? civHistory[civHistory.Count - 1].maxRankReached : 0;
            }

            DarkIteration++;
            CurrentPhase = BeyondPhase.DarkIteration;

            // 第二次暗面迭代有30%概率创造世界树
            if (DarkIteration == 2 && !WorldTreeBorn && Random.value < 0.3f)
            {
                CreateWorldTree();
            }

            _darkHistory.Add(new DarkCivilization
            {
                iteration = DarkIteration,
                civilizationName = $"三大文明暗面第{DarkIteration}迭代",
                totalAwakened = 0,
                maxRankReached = 0,
                summary = DarkIteration == MaxDarkIteration
                    ? "暗面宇宙最终迭代，三大文明准备回归真实宇宙"
                    : $"暗面宇宙第{DarkIteration}次迭代",
                worldTreeCreated = WorldTreeBorn
            });

            Debug.Log($"[超神机械师] 暗面宇宙小重启！进入第{DarkIteration}次暗面迭代");
            return true;
        }

        /// <summary>创造世界树（原著：暗面宇宙信息态技术创造的宇宙宝物）
        /// 世界树可以加速信息态恢复，提升遗产保留率
        /// </summary>
        public static bool CreateWorldTree()
        {
            if (WorldTreeBorn) return false;
            if (!IsStrippingStarted) return false;

            WorldTreeBorn = true;
            WorldTreeLevel = 1;

            Debug.Log("[超神机械师] 世界树诞生！暗面宇宙信息态技术结晶，遗产保留率+10%");
            return true;
        }

        /// <summary>世界树成长（消耗圣所权限，提升遗产保留率加成）</summary>
        public static bool GrowWorldTree()
        {
            if (!WorldTreeBorn) return false;
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
            if (!WorldTreeBorn) return 0f;
            return WorldTreeLevel * 0.02f; // 每级+2%
        }

        /// <summary>获取暗面宇宙历史记录</summary>
        public static List<DarkCivilization> GetDarkHistory()
        {
            return _darkHistory;
        }

        /// <summary>完成暗面宇宙迭代，回归真实宇宙（原著：暗面三次迭代后真实宇宙进入新迭代）</summary>
        public static bool CompleteDarkCycle()
        {
            if (!IsStrippingStarted) return false;
            if (DarkIteration < MaxDarkIteration) return false;

            CurrentPhase = BeyondPhase.Merging;
            Debug.Log("[超神机械师] 暗面宇宙三次迭代完成，三大文明回归，真实宇宙进入新迭代");
            return true;
        }

        /// <summary>暗面与真实宇宙重合异变（原著：重合瞬间暗面系升维）</summary>
        public static void TriggerTranscendence()
        {
            if (CurrentPhase != BeyondPhase.Merging) return;
            CurrentPhase = BeyondPhase.Transcended;
            Debug.Log("[超神机械师] 暗面与真实宇宙重合异变！暗面系升维为超脱存在");
        }

        /// <summary>存档数据</summary>
        public class WorldBeyondSaveData
        {
            public int currentPhase;
            public int darkIteration;
            public bool isStrippingStarted;
            public bool worldTreeBorn;
            public int worldTreeLevel;
            public List<DarkCivilization> darkHistory = new List<DarkCivilization>();
        }

        public static WorldBeyondSaveData Save()
        {
            return new WorldBeyondSaveData
            {
                currentPhase = (int)CurrentPhase,
                darkIteration = DarkIteration,
                isStrippingStarted = IsStrippingStarted,
                worldTreeBorn = WorldTreeBorn,
                worldTreeLevel = WorldTreeLevel,
                darkHistory = new List<DarkCivilization>(_darkHistory)
            };
        }

        public static void Load(WorldBeyondSaveData data)
        {
            if (data == null) return;
            CurrentPhase = (BeyondPhase)data.currentPhase;
            DarkIteration = data.darkIteration;
            IsStrippingStarted = data.isStrippingStarted;
            WorldTreeBorn = data.worldTreeBorn;
            WorldTreeLevel = data.worldTreeLevel;
            _darkHistory.Clear();
            if (data.darkHistory != null) _darkHistory.AddRange(data.darkHistory);
            _initialized = true;
        }

        public static void Clear()
        {
            CurrentPhase = BeyondPhase.None;
            DarkIteration = 0;
            IsStrippingStarted = false;
            WorldTreeBorn = false;
            WorldTreeLevel = 0;
            _darkHistory.Clear();
            _initialized = false;
        }
    }
}
