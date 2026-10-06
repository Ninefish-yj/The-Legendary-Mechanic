using NeoModLoader.api;
using NeoModLoader.services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HarmonyLib;

namespace SuperMech.Code
{


    public static partial class SuperMechSanctuary
    {
        public static readonly string[] SanctuaryNames = {
            "sm_sanctuary_958", "sm_sanctuary_959", "sm_sanctuary_960",
            "sm_sanctuary_961", "sm_sanctuary_962", "sm_sanctuary_963"
        };
        public static readonly string[] SanctuaryClasses = {
            "sm_sanctuary_964", "sm_sanctuary_965", "sm_sanctuary_966", "sm_sanctuary_967", "sm_sanctuary_968", "sm_sanctuary_969"
        };

        public const int TotalSanctuaries = 6;
        public const float DivinityOnarThreshold = 78000f;
        public const int DivinityQiLevel = 21;
        public const int FragmentsToUnlock = 3;
        /// <summary>圣所能量上限（原著：圣所是不灭的信息态存在，自行汲取能量维持运转，不会枯竭）</summary>
        public const float MaxSanctuaryEnergy = 10000f;

        /// <summary>圣所类型（原著：第一圣所=虚拟技术，第三圣所=苏醒计划，第六圣所=信息态；其余按五职业对应）</summary>
        public enum SanctuaryType
        {
            Mechanical = 0,    // 第一圣所：虚拟技术（原著：虚拟技术类技能效果+50%）
            Psi = 1,           // 第二圣所：念力师
            Biological = 2,    // 第三圣所：苏醒计划/生物基因（原著：苏醒计划）
            Martial = 3,       // 第四圣所：武道家
            Mage = 4,          // 第五圣所：魔法师
            Information = 5    // 第六圣所：信息态（原著：最孤傲，进度最慢）
        }

        /// <summary>各圣所类型（索引0~5对应第一~第六圣所）</summary>
        public static readonly SanctuaryType[] SanctuaryTypes =
        {
            SanctuaryType.Mechanical,    // 第一圣所：虚拟技术
            SanctuaryType.Psi,           // 第二圣所：念力师
            SanctuaryType.Biological,    // 第三圣所：苏醒计划
            SanctuaryType.Martial,       // 第四圣所：武道家
            SanctuaryType.Mage,          // 第五圣所：魔法师
            SanctuaryType.Information    // 第六圣所：信息态
        };

        /// <summary>圣所专属知识分支（访问时产出对应知识）
        /// 原著：第一圣所=虚拟技术，第三圣所=苏醒计划，第六圣所=信息态；其余按五职业对应
        /// </summary>
        public static readonly string[][] SanctuaryKnowledgeBranches =
        {
            new[] { "mech_weapon", "mech_energy", "mech_control" },  // 第一圣所：虚拟技术（机械系分支）
            new[] { "psi_mind", "psi_kinesis", "psi_sense" },        // 第二圣所：念力师
            new[] { "bio_gene", "bio_ability", "bio_evolution" },    // 第三圣所：苏醒计划/生物基因
            new[] { "martial_breath", "martial_flurry", "martial_body" }, // 第四圣所：武道家
            new[] { "spell_element", "spell_arcane", "spell_summon" }, // 第五圣所：魔法师
            new[] { "info_state", "info_virtual", "info_resurrect" } // 第六圣所：信息态
        };

        /// <summary>圣所类型名称本地化key</summary>
        public static readonly string[] SanctuaryTypeNames =
        {
            "sm_sanctuary_type_mech",
            "sm_sanctuary_type_psi",
            "sm_sanctuary_type_bio",
            "sm_sanctuary_type_martial",
            "sm_sanctuary_type_mage",
            "sm_sanctuary_type_info"
        };

        private static readonly string DataPath =
            Path.Combine(Application.dataPath, "sm_sanctuary_970");

        public static SanctuaryData Data = new SanctuaryData();
        private static readonly HashSet<long> _divinityTriggered = new HashSet<long>();

        public class SanctuaryData
        {
            public int unlocked_sanctuaries = 0;
            public int key_fragments = 0;       // v0.46.0：完整圣所钥匙（进入消耗）
            public int key_materials = 0;       // v0.46.0：钥匙材料（击杀高阶单位掉落，合成钥匙）
            public int[] sanctuary_fragments = new int[6];
            public int total_permission = 0;
            public int total_visits = 0;
            public bool message_board_unlocked = true; // v0.75.21: 默认解锁（原著无门槛），内容分级看圣所权限
            public int total_divinity_ascensions = 0;
            public int total_resurrections = 0;
            public float sanctuary_energy = 5000f;  // 圣所能量（复活媒介消耗）
            public List<IterationArchive> iteration_archives = new List<IterationArchive>(); // v0.76.12 跨迭代传承档案
            public bool beyond_message_given = false; // v0.76.23 高维留言是否已写入（一次性·跨迭代保留）
        }

        /// <summary>跨迭代档案（v0.76.12 圣所跨迭代传承：原著ch1211圣所=上一迭代遗产融入新生宇宙，
        /// ch1214圣所记录圣体级/超A级信息=另一种形式的不灭）</summary>
        public class IterationArchive
        {
            public int iteration;               // 第几轮宇宙迭代（0=前代/原著上代文明）
            public int superACount;             // 该迭代超A级（圣体级）数量（type=1记事档案不计传承）
            public int[] classCounts = new int[5]; // 体系分布：机械/念力/武道/异能/魔法
            public int maxRankIdx;              // 最高阶位
            public string label;                // 档案名（本地化键；null=玩家世界第N轮记录）
            public string note;                 // 档案说明（本地化键；null=无）
            public int type;                    // 0=文明档案（计入传承）/1=记事档案（纯记录，ch1267留言/情报）
        }

        public class DeadUnitRecord
        {
            public string name;
            public string classTrait;
            public string branchTrait;
            public int stage;
            public int rankIndex;
            public float qi;
            public string qiAttribute;
            public long diedAt;
            public int reviveCount;
        }

        private static readonly Dictionary<long, int> _reviveCount = new Dictionary<long, int>();
        private static readonly HashSet<long> _transcendenceFailed = new HashSet<long>();
        private static readonly Dictionary<long, int[]> _unitAuthority = new Dictionary<long, int[]>();

        /// <summary>记录当前迭代的超A级（圣体级）信息到圣所档案（大重启前调用；
        /// 原著ch1214：圣所关注记录超A级个体=有价值的信息；ch1211：圣所把上一迭代信息融入新生宇宙）</summary>
        public static void RecordIterationArchive()
        {
            int superA = 0;
            int[] cls = new int[5];
            int maxRank = 0;
            if (World.world != null && World.world.units != null)
            {
                foreach (var a in World.world.units.units_only_alive)
                {
                    if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                    superA++;
                    int ri = SuperMechAdvancement.GetExactRankIndex(a);
                    if (ri > maxRank) maxRank = ri;
                    int ci = 0;
                    if (a.hasTrait(SuperMechTraits.ClassMech)) ci = 0;
                    else if (a.hasTrait(SuperMechTraits.ClassPsi)) ci = 1;
                    else if (a.hasTrait(SuperMechTraits.ClassMartial)) ci = 2;
                    else if (a.hasTrait(SuperMechTraits.ClassMind)) ci = 3;
                    else if (a.hasTrait(SuperMechTraits.ClassMage)) ci = 4;
                    cls[ci]++;
                }
            }
            Data.iteration_archives.Add(new IterationArchive
            {
                iteration = SuperMechCosmicIteration.CurrentIteration,
                superACount = superA,
                classCounts = cls,
                maxRankIdx = maxRank
            });
            Save();
            Debug.Log($"[超神机械师] 圣所迭代档案: 第{SuperMechCosmicIteration.CurrentIteration}轮 超A×{superA} 最高阶rank{maxRank}");
        }

        /// <summary>高维留言：世界达成重大里程碑（秩序确立/体制化/圣域/同盟）时，
        /// 三大文明（已脱离迭代的超脱者）向圣所写入成功心得——原著接力精神（ch1211"即便失败了
        /// 也会留下心得让后人研究"；成功经验是接力最珍贵的一棒）。一次性，写入后跨迭代永久保留。</summary>
        public static bool TryGiveBeyondMessage()
        {
            if (Data.beyond_message_given) return false;
            Data.beyond_message_given = true;
            Data.iteration_archives.Add(new IterationArchive
            {
                iteration = 0, type = 1, label = "sm_san_arch_beyond", note = "sm_san_arch_beyond_n",
                superACount = 0, maxRankIdx = 0
            });
            Save();
            Debug.Log("[超神机械师] 高维留言：三大文明向圣所写入成功心得（世界达成里程碑）");
            return true;
        }

        /// <summary>历代传承系数：圣所逐代累积的超A记录→新迭代复苏更完整（原著ch1211
        /// "一次次对宇宙进行微小的改变，不断让下一迭代的宇宙变得更加丰富"；每200超A记录+1%，上限+10%）。
        /// 只统计文明档案（type=0）；记事档案（留言/情报）不计入传承。</summary>
        public static float GetInheritanceBonus()
        {
            int total = 0;
            foreach (var ar in Data.iteration_archives)
                if (ar != null && ar.type == 0) total += ar.superACount;
            return 1f + Mathf.Min(0.10f, total * 0.0005f);
        }

        /// <summary>历代迭代档案（UI显示用）</summary>
        public static List<IterationArchive> GetIterationArchives() => Data.iteration_archives;

        public static int GetReviveCount(Actor a)
        {
            if (a == null) return 0;
            var ctx = SuperMechActorContextRegistry.TryGet(a.id);
            if (ctx != null && ctx.reviveCount > 0) return ctx.reviveCount;
            int v; _reviveCount.TryGetValue(a.id, out v); return v;
        }

        public static void SetReviveCount(Actor a, int count)
        {
            if (a == null) return;
            _reviveCount[a.id] = count;
            var ctx = SuperMechActorContextRegistry.Get(a);
            if (ctx != null) ctx.reviveCount = count;
        }

        public static int GetAuthority(Actor a, int sanctuaryIndex)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= 6) return 0;
            if (!_unitAuthority.TryGetValue(a.id, out var arr)) return 0;
            return arr[sanctuaryIndex];
        }

        public static int GetTotalAuthority(Actor a)
        {
            if (a == null) return 0;
            if (!_unitAuthority.TryGetValue(a.id, out var arr)) return 0;
            int total = 0;
            foreach (int v in arr) total += v;
            return total;
        }

        public static void AddAuthority(Actor a, int sanctuaryIndex, int amount)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= 6 || amount <= 0) return;
            if (!_unitAuthority.TryGetValue(a.id, out var arr))
            {
                arr = new int[6];
                _unitAuthority[a.id] = arr;
            }
            arr[sanctuaryIndex] += amount;
        }

        public static void MarkTranscendenceFailed(Actor a)
        {
            if (a == null) return;
            _transcendenceFailed.Add(a.id);
        }

        private static readonly List<DeadUnitRecord> _deadUnits = new List<DeadUnitRecord>();
        private static readonly Dictionary<long, DeadUnitRecord> _aliveSnapshot = new Dictionary<long, DeadUnitRecord>();
        private static readonly Dictionary<long, DeadUnitRecord> _divineSnapshot = new Dictionary<long, DeadUnitRecord>();
        private static readonly HashSet<long> _divineCooldown = new HashSet<long>();
        public const int MaxDeadRecords = 20;
        public const int ResurrectionCost = 5;
        public const int DivineRankIndex = 13;

        /// <summary>圣所是否已解锁（原著第1267章：圣所碎片就是权限；集齐3碎片解锁对应圣所）</summary>
        public static bool IsSanctuaryUnlocked(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return false;
            return Data.sanctuary_fragments[sanctuaryIndex] >= FragmentsToUnlock;
        }

        /// <summary>从碎片同步解锁位掩码（v0.64.1 修复：此前位掩码从未被置位，导致圣所访问/进入逻辑永远失败）</summary>
        public static void RefreshUnlockedFromFragments()
        {
            for (int i = 0; i < TotalSanctuaries; i++)
            {
                if (Data.sanctuary_fragments[i] >= FragmentsToUnlock)
                    Data.unlocked_sanctuaries |= (1 << i);
            }
        }

        /// <summary>圣所能量缓慢恢复（原著：圣所不灭、自行运转；能量枯竭会永久禁用复苏，故随时间恢复）
        /// 恢复速率由配置 sanctuary_energy_regen 控制（每秒点数），上限 MaxSanctuaryEnergy</summary>
        public static void RegenerateEnergy(float delta)
        {
            if (!SuperMechConfig.SanctuaryEnabled) return;
            if (Data.sanctuary_energy >= MaxSanctuaryEnergy) return;
            float rate = Mathf.Max(0f, SuperMechConfig.SanctuaryEnergyRegen);
            if (rate <= 0f) return;
            Data.sanctuary_energy = Mathf.Min(MaxSanctuaryEnergy, Data.sanctuary_energy + rate * delta);
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(DataPath))
                {
                    string json = File.ReadAllText(DataPath);
                    Data = JsonConvert.DeserializeObject<SanctuaryData>(json) ?? new SanctuaryData();
                    if (Data.sanctuary_fragments == null || Data.sanctuary_fragments.Length < 6)
                        Data.sanctuary_fragments = new int[6];
                }
                // v0.64.1 修复：旧存档碎片已达标但位掩码缺失，加载时统一从碎片重算解锁状态
                RefreshUnlockedFromFragments();
                // 迭代档案兜底（旧存档缺字段→null）
                if (Data.iteration_archives == null)
                    Data.iteration_archives = new System.Collections.Generic.List<IterationArchive>();
                // v0.76.15/16 预置原著前代文明档案：圣所=上代文明遗产变异成的底层规律，记录一个迭代发生的
                // 所有信息（ch1211），大重启时随机抽取融入新生宇宙。前代记录=圣所传承的初始来源。
                // iteration=0 表示前代（原著上代文明）。
                if (Data.iteration_archives.Count == 0)
                {
                    // ① 救世主文明：最初改造宇宙、触动信息态异变、影响宇宙发展路线的文明（ch1211）
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 0, label = "sm_san_arch_savior", note = "sm_san_arch_savior_n",
                        superACount = 200, classCounts = new int[] { 50, 40, 40, 40, 30 }, maxRankIdx = 12
                    });
                    // ② 圣所文明（变革迭代）：信息态突破、查到大重启起源、用信息态延续文明失败、留下圣所（ch1211）
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 0, label = "sm_san_arch_sanciv", note = "sm_san_arch_sanciv_n",
                        superACount = 300, classCounts = new int[] { 70, 60, 60, 60, 50 }, maxRankIdx = 12
                    });
                    // ③ 诸星联（次级维度世界开辟者）："我们的迭代早于你们"（ch1211）——诸星联=前代迭代文明，
                    // 发起世界重启计划失败→宇宙末期截取为闭合循环时空（幻影·杰斯）→为后世开辟次级维度世界
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 0, label = "sm_san_arch_alliance", note = "sm_san_arch_alliance_n",
                        superACount = 500, classCounts = new int[] { 100, 100, 100, 100, 100 }, maxRankIdx = 12
                    });
                    // ④ 历代终极文明留言板：圣所记录每个迭代终极文明的经验留言，一代代传下去（ch1267）
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 1, label = "sm_san_arch_board", note = "sm_san_arch_board_n",
                        superACount = 0, maxRankIdx = 0
                    });
                    // ⑤ 暗面宇宙（信息态剥离计划）：二号迭代三大文明继承诸星联思路，剥离全宇宙信息态→
                    // 三号迭代形成封闭暗面宇宙（虚拟·小重启循环），4次暗面迭代（韩萧=第四），
                    // 世界树/枢蛇/玩家皆源于此（ch1464）；完全解锁权限才可见（ch1465）
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 1, label = "sm_san_arch_dark", note = "sm_san_arch_dark_n",
                        superACount = 0, maxRankIdx = 0
                    });
                    // ⑥ 高维留言·成功转化经验：按原著条件推演的"活过大重启"路径（用户要求"按原著条件
                    // 推演+只有三大文明能做到的非普世路径"）。原著：宇宙本无重启——大重启是救世主文明
                    // 为延续宇宙寿命改造的产物（终极命运时坍缩为奇点→创世爆炸→新生宇宙，ch1211"为宇宙
                    // 生灵赋予一线希望""他们开启了宇宙的迭代"）。历代尝试皆败（圣所文明信息态延续=只留
                    // 圣所不保命、诸星联世界重启=失败成循环幻影只暂停不重生）。三大文明独有=暗面宇宙
                    // （ch1464：继承诸星联思路、全宇宙信息态剥离进暗面，本体活在暗面经历4次小重启试错）
                    // ⑥ 高维留言·成功转化经验（v0.76.32重设，用户设定："新迭代中三大文明为了不在暗面宇宙
                    // 中经历重启，重新改造了暗面宇宙相关技术，然后在宇宙重启打包的时候出现了异变，那个
                    // 暗面宇宙成为了和玩家同层次的存在"）：
                    // 链（v0.76.38时序定案B + v0.76.39升维对象统一）：宇宙本无重启（终极命运=热寂/大坍缩/
                    // 大撕裂，ch1211）→救世主改造规则创造大重启（自身毁于其中，改造=一线希望）→三大文明
                    // 剥离全宇宙信息态=暗面（ch1464，第一次打包）→暗面小重启×4→暗面与真实宇宙重合=回真实
                    // 宇宙（ch1477，暗面独立态消失、融入真实宇宙）→回真实宇宙后临近大重启→三大文明为摆脱
                    // 重启循环，重新改造"暗面遗留的改造物/残留信息"→大重启（宇宙重启打包瞬间）改造异变→
                    // 【升维对象=暗面系整体：遗存+随暗面而来的住民（星海人）+登陆者（地球玩家），与降临者/
                    // 星海人层位设定一致】升维=超脱存在，与创世神（玩家）同层次、俯瞰迭代。
                    // 非普世性=迭代异变：异变不可复制、不可预测，手段可学、异变不可求
                    Data.iteration_archives.Add(new IterationArchive
                    {
                        iteration = 0, type = 1, label = "sm_san_arch_beyond", note = "sm_san_arch_beyond_n",
                        superACount = 0, maxRankIdx = 0
                    });
                    Save();
                    Debug.Log("[超神机械师] 圣所预置前代档案×6（救世主/圣所文明/诸星联/留言板/暗面宇宙/高维留言，传承+5%）");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 圣所数据读取失败: " + e.Message);
                Data = new SanctuaryData();
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
                File.WriteAllText(DataPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 圣所数据保存失败: " + e.Message);
            }
        }

        public static bool EnterSanctuary(Actor a, int sanctuaryIndex = -1)
        {
            if (!SuperMechConfig.SanctuaryEnabled) return false;
            int cost = GetEnterCost();
            if (Data.key_fragments < cost)
            {
                Debug.Log($"[超神机械师] 圣所钥匙碎片不足（需{cost}，当前{Data.key_fragments}）");
                return false;
            }
            if (sanctuaryIndex >= 0 && sanctuaryIndex < 6)
            {
                if (!IsSanctuaryUnlocked(sanctuaryIndex))
                {
                    return false;
                }
            }
            Data.key_fragments -= cost;
            Data.total_visits++;
            // v0.75.21: 留言板默认全解锁（原著chapter1267：韩萧首次进圣所即见光幕留言板，无访问次数门槛）
            // 内容分级按圣所权限（权限高→空缺少→留言全，原著"权限高了才能减少空缺"）在 ShowMessageBoard 内实现

            int authority = GetAuthority(a, sanctuaryIndex >= 0 ? sanctuaryIndex : 0);
            int knowledgeGain = Mathf.Clamp(authority + 1, 1, 20);
            SuperMechPotential.AddPotential(a, knowledgeGain);

            if (Random.value < 0.3f && sanctuaryIndex >= 0)
            {
                AddAuthority(a, sanctuaryIndex, 1);
            }

            float buff = 1f + Data.total_visits * 0.02f;
            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * buff;
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * buff;
            }

            ApplySanctuaryClassBonus(a, s, sanctuaryIndex);

            if (IsSanctuaryUnlocked(5))
            {
                SuperMechInfoState.Upgrade(a);
            }

            Save();

            return true;
        }

        private static void ApplySanctuaryClassBonus(Actor a, BaseStats s, int sanctuaryIndex)
        {
            if (a == null) return;
            int visits = Data.total_visits;

            if (sanctuaryIndex < 0 || sanctuaryIndex >= 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (IsSanctuaryUnlocked(i))
                    {
                        sanctuaryIndex = i;
                        break;
                    }
                }
                if (sanctuaryIndex < 0) return;
            }

            switch (sanctuaryIndex)
            {
                case 0:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.03f;
                        s["attack_speed"] = ((s["attack_speed"] == 0f ? 1f : s["attack_speed"])) * 1.05f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_964")
                        SuperMechQi.AddQiMax(a, 200f + visits * 20f);
                    break;

                case 1:
                    SuperMechQi.AddQiMax(a, 500f + visits * 50f);
                    if (s != null)
                    {
                        s["damage"] = (s["damage"]) + 3f + visits;
                        s["armor"] = (s["armor"]) + 3f + visits;
                    }
                    break;

                case 2:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.02f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_966")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 3:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 4f + visits;
                        s["mana"] = (s["mana"]) + 50f + visits * 5f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_967")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 4:
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 5f + visits;
                        s["mana"] = (s["mana"]) + 30f + visits * 3f;
                    }
                    if (SuperMechBranch.GetClass(a) == "sm_sanctuary_968")
                        SuperMechCorePower.AdvanceStage(a, 1);
                    break;

                case 5:
                    SuperMechInfoState.Upgrade(a);
                    if (s != null)
                    {
                        s["intelligence"] = (s["intelligence"]) + 10f + visits * 2;
                        s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.05f;
                        s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.05f;
                    }
                    break;
            }
        }

        private static int CountUnlocked()
        {
            int count = 0;
            for (int i = 0; i < 6; i++)
                if (IsSanctuaryUnlocked(i)) count++;
            return count;
        }

        public static void GrantKeyFragment()
        {
            Data.key_fragments++;
            Save();
        }

        // === v0.46.0 圣所钥匙机制（原著向）===
        // 钥匙（key_fragments）：进入圣所消耗，由钥匙材料合成
        // 钥匙材料（key_materials）：击杀S阶及以上超能者掉落
        // 权限/碎片（_unitAuthority / sanctuary_fragments）：进入圣所后积累，影响奖励，神性蜕变直接获得

        /// <summary>授予钥匙材料，达到合成阈值时自动合成钥匙（v0.46.0，原著：钥匙由稀有材料合成）</summary>
        public static void GrantKeyMaterials(int amount, string source)
        {
            if (!SuperMechConfig.SanctuaryKeyDropEnabled) return;
            if (amount <= 0) return;
            Data.key_materials += amount;
            int crafted = CraftKeys();
            Save();
            if (crafted > 0)
            {
                SuperMechEventLogger.LogKeyAcquired(null, crafted, source + "合成");
                Debug.Log($"[超神机械师] 获得{amount}钥匙材料（来源：{source}），自动合成{crafted}把圣所钥匙，材料剩余{Data.key_materials}");
            }
            else
            {
                Debug.Log($"[超神机械师] 获得{amount}钥匙材料（来源：{source}），当前{Data.key_materials}/{SuperMechConfig.KeyMaterialsPerKey}");
            }
        }

        /// <summary>自动合成钥匙：材料达到阈值时转换为钥匙，返回合成数量</summary>
        public static int CraftKeys()
        {
            int perKey = Mathf.Max(1, SuperMechConfig.KeyMaterialsPerKey);
            int crafted = Data.key_materials / perKey;
            if (crafted > 0)
            {
                Data.key_materials -= crafted * perKey;
                Data.key_fragments += crafted;
            }
            return crafted;
        }

        /// <summary>神性蜕变获得圣所碎片（权限），随机分配到已解锁圣所（v0.46.0，原著第1039章：神性蜕变给圣所技能碎片）</summary>
        public static void GrantAuthorityFromDivinity(Actor a)
        {
            if (a == null) return;
            // 找已解锁的圣所，随机选一个+1权限
            var unlocked = new List<int>();
            for (int i = 0; i < 6; i++)
                if (IsSanctuaryUnlocked(i))
                    unlocked.Add(i);
            int target = unlocked.Count > 0
                ? unlocked[UnityEngine.Random.Range(0, unlocked.Count)]
                : UnityEngine.Random.Range(0, 6);
            AddAuthority(a, target, 1);
            Data.sanctuary_fragments[target]++;
            Data.total_permission++;
            // v0.64.1 修复：碎片达到阈值后立即解锁对应圣所，并同步位掩码
            RefreshUnlockedFromFragments();
            Save();
            Debug.Log($"[超神机械师] {a.name} 神性蜕变获得圣所碎片，{SanctuaryNames[target]}权限+1");
        }

        /// <summary>获取每次进入圣所消耗的钥匙数（v0.46.0：可配置）</summary>
        public static int GetEnterCost()
        {
            return Mathf.Max(1, SuperMechConfig.SanctuaryKeyCostEnter);
        }

        /// <summary>获取圣所类型</summary>
        public static SanctuaryType GetSanctuaryType(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return SanctuaryType.Mechanical;
            return SanctuaryTypes[sanctuaryIndex];
        }

        /// <summary>获取圣所类型名称（本地化）</summary>
        public static string GetSanctuaryTypeName(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return "";
            return LocalizedTextManager.getText(SanctuaryTypeNames[sanctuaryIndex]);
        }

        /// <summary>获取圣所专属知识分支列表</summary>
        public static string[] GetSanctuaryKnowledgeBranches(int sanctuaryIndex)
        {
            if (sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return new string[0];
            return SanctuaryKnowledgeBranches[sanctuaryIndex];
        }

        /// <summary>访问圣所：根据圣所类型产出专属知识/权限（原著差异化）
        /// 第一圣所产出机械系知识，第三圣所产出生物基因知识，第六圣所产出信息态知识
        /// </summary>
        public static bool VisitSanctuary(Actor a, int sanctuaryIndex)
        {
            if (a == null || sanctuaryIndex < 0 || sanctuaryIndex >= TotalSanctuaries) return false;
            if (!IsSanctuaryUnlocked(sanctuaryIndex)) return false;

            // 增加访问次数和权限
            Data.total_visits++;
            AddAuthority(a, sanctuaryIndex, 10);

            // 根据圣所类型判断体系匹配（原著：五个超能职业分别对应一个圣所）
            var type = GetSanctuaryType(sanctuaryIndex);
            bool isMatchingClass = false;
            switch (type)
            {
                case SanctuaryType.Mechanical:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMech);
                    break;
                case SanctuaryType.Psi:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMind);
                    break;
                case SanctuaryType.Biological:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassPsi);
                    break;
                case SanctuaryType.Martial:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMartial);
                    break;
                case SanctuaryType.Mage:
                    isMatchingClass = a.hasTrait(SuperMechTraits.ClassMage);
                    break;
                case SanctuaryType.Information:
                    isMatchingClass = true; // 信息态圣所不限制职业
                    break;
            }

            // 匹配体系的单位访问获得额外权限加成
            if (isMatchingClass)
            {
                AddAuthority(a, sanctuaryIndex, 5);
                Debug.Log($"[超神机械师] {a.name} 访问第{sanctuaryIndex + 1}圣所（{GetSanctuaryTypeName(sanctuaryIndex)}），体系匹配，额外权限+5");
            }
            else
            {
                Debug.Log($"[超神机械师] {a.name} 访问第{sanctuaryIndex + 1}圣所（{GetSanctuaryTypeName(sanctuaryIndex)}）");
            }

            // v0.29.0 UI重构：推送圣所访问事件到原生事件日志
            string sanctuaryName = GetSanctuaryTypeName(sanctuaryIndex);
            SuperMechEventLogger.LogSanctuaryVisit(a, sanctuaryIndex, sanctuaryName);
            // 发布圣所进入事件
            SuperMechEventBus.Publish("SanctuaryEntered", new SanctuaryEnteredEvent
            {
                actor = a, sanctuaryIndex = sanctuaryIndex, sanctuaryName = sanctuaryName
            });

            Save();
            return true;
        }

        /// <summary>获取圣所时间比例（原著：权限越高，时间比例越趋近1:1）
        /// 基础比例1:10，每1000点权限提升10%，最高1:1
        /// </summary>
        public static float GetSanctuaryTimeRatio(Actor a, int sanctuaryIndex)
        {
            if (a == null) return 0.1f; // 基础1:10
            int authority = GetAuthority(a, sanctuaryIndex);
            float ratio = 0.1f + (authority / 1000f) * 0.1f;
            return Mathf.Clamp(ratio, 0.1f, 1.0f);
        }

        /// <summary>获取圣所时间比例描述（用于UI显示）</summary>
        public static string GetSanctuaryTimeRatioText(Actor a, int sanctuaryIndex)
        {
            float ratio = GetSanctuaryTimeRatio(a, sanctuaryIndex);
            int outsideTime = Mathf.RoundToInt(1f / ratio);
            string tTimeRatio = LocalizedTextManager.getText("sm_san_time_ratio");
            return string.Format(tTimeRatio, outsideTime);
        }

        public static void Register()
        {
            Load();

            var divinityTrait = new ActorTrait
            {
                id = "sm_divinity_ascended",
                path_icon = "ui/Icons/actor_traits/iconBlessing",
                group_id = "sm_awakened",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            divinityTrait.base_stats["multiplier_damage"] = 1.2f;
            divinityTrait.base_stats["multiplier_health"] = 1.2f;
            divinityTrait.base_stats["critical_chance"] = 0.05f;
            AssetManager.traits.add(divinityTrait);

            LocalizedTextManager.add("power_sm_enter_sanctuary", LocalizedTextManager.getText("sm_sanctuary_974"), pReplace: true);
            LocalizedTextManager.add("power_sm_enter_sanctuary_desc",
                LocalizedTextManager.getText("sm_sanctuary_975"), pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended", LocalizedTextManager.getText("sm_sanctuary_976"), pReplace: true);
            LocalizedTextManager.add("trait_sm_divinity_ascended_info", LocalizedTextManager.getText("sm_sanctuary_977"), pReplace: true);

        }

        public static void Clear()
        {
            _divinityTriggered.Clear();
            _reviveCount.Clear();
            _transcendenceFailed.Clear();
            _aliveSnapshot.Clear();
            _divineSnapshot.Clear();
            Data = new SanctuaryData();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_reviveCount, alive);
            removed += SuperMechCleanup.CleanDict(_aliveSnapshot, alive);
            return removed;
        }
    }

    /// <summary>v0.46.0：高阶超能者死亡掉落圣所钥匙材料（原著：钥匙由稀有材料合成）</summary>
    [HarmonyPatch]
    public static class SanctuaryKeyDropPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), "die")]
        public static void DiePostfix(Actor __instance)
        {
            if (__instance == null || __instance.isAlive()) return;
            SuperMechEventBus.Publish("ActorDied", new ActorDiedEvent { actor = __instance, cause = AttackType.None });
            // v0.52.0 文明守护者陨落处理
            SuperMechCivilization.OnGuardianDeath(__instance);
            if (!SuperMechConfig.SanctuaryKeyDropEnabled) return;

            try
            {
                int rank = SuperMechAdvancement.GetExactRankIndex(__instance);
                if (rank < SuperMechConfig.KeyDropMinRank) return;

                // 按阶位掉落材料：S=1, S+=1, SS=2, X=3
                int amount = 1;
                if (rank >= 13) amount = 3;
                else if (rank >= 12) amount = 2;
                else amount = 1;

                string rankName = rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name)
                    : "?";

                // v0.46.1：异能系/念力系单位额外掉落原始异能体碎片（原著：原始异能体是开启第三圣所的钥匙）
                bool isPsionic = __instance.hasTrait(SuperMechTraits.ClassPsi)
                              || __instance.hasTrait(SuperMechTraits.ClassMind);
                if (isPsionic)
                {
                    int bonus = SuperMechConfig.PsionicKeyMaterialBonus;
                    if (bonus > 0)
                    {
                        amount += bonus;
                        SuperMechSanctuary.GrantKeyMaterials(amount, "击杀" + rankName + "+原始异能体碎片");
                    }
                    else
                    {
                        SuperMechSanctuary.GrantKeyMaterials(amount, "击杀" + rankName);
                    }
                }
                else
                {
                    SuperMechSanctuary.GrantKeyMaterials(amount, "击杀" + rankName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 钥匙材料掉落异常: {e.Message}");
            }
        }
    }
}
