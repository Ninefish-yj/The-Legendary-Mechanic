using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.10 个体伟力·集体伟力体系（原著后期主线重写——按原著现实考量，非自造）
    /// 原著机制链（全部有原文）：
    ///  1. 起点：超A级在高级文明眼中="战略威慑和自相残杀的工具"（ch1016）；
    ///  2. 巅峰之殇（ch1002）：三大文明超A互相伤害损伤惨重→担心失去个人伟力战略优势→
    ///     编莫须有罪名，把超星团级文明的超A盟友屠干净——清算对象=他文明（超星团级文明）的超A资产，
    ///     不是自由散人；且是"绝户计"：超A担心人身安全不敢投靠超星团级→三大文明把超A都抓到自己手里；
    ///  3. 清算逻辑（ch1018）：超A总数只会一直增加→"个体伟力将失去掌控"→"清算超A级是必须的，
    ///     不过我不打算在我的任期内做"——文明未雨绸缪+政治烫手山芋，非"超A压倒文明"；
    ///  4. 阶级觉醒（ch1016）：麦尼逊为超A级所代表的"阶级"谋福利，联合="拧成一股…高级文明更加忌惮"；
    ///     但协会会被打压（ch1459"当年三大文明对超A级协会的打压"）；
    ///  5. 终局=超能圣域（ch1459）：韩萧建圣域→"与三大文明的势力达到同一层次"→"把全宇宙的
    ///     个体伟力一网打尽"（收编自然觉醒的民间超能者）→其他文明只剩嫡系培养的超能者→
    ///     变相帮文明巩固统治基础——超A阶级自治实体与三大文明对等共存（终局，非压倒）。
    /// 状态机：默许→警惕（高阶超能者群体收编压力）→清算风暴（清超星团级文明超A+绝户计）→
    /// 协会（超A阶级联合，会被打压）→超能圣域（政治地位承认，对等共存终局）。
    /// 删除：v0.76.0~v0.76.9 的"超A时代=个体压倒文明"状态（原著无此状态，系自造）。
    /// </summary>
    public static class SuperMechSupermA
    {
        public const string CouncilTrait = "sm_supera_council"; // 超A级协会成员标记
        public const string PurgeTrait = "sm_supera_purge";     // 清算标记（文明联合压制）

        public enum Attitude { Tolerate, Vigilant, Purge, Council, Sanctum }
        // 默许 / 警惕（高阶超能者群体收编压力） / 清算风暴（巅峰之殇：清超星团级文明超A） /
        // 协会（超A阶级联合） / 超能圣域（与三大文明同层次，对等共存终局）

        /// <summary>超A政治倾向（原著：秩序派投靠文明=安全部干部，中立派保持独立，混乱派=自由散人）</summary>
        public enum PoliticalAlignment { Order = 0, Neutral = 1, Chaos = 2 }
        private static readonly Dictionary<long, PoliticalAlignment> _alignment = new Dictionary<long, PoliticalAlignment>();
        private const int DefectionInterval = 16;  // 每16 tick检查一次投靠
        private const float DefectionChance = 0.15f; // 秩序派投靠概率（每次检查）

        // 力量对比制（威胁=力量比值，非单位数量）：力量指数=onar/1000（SS巅峰超A≈83，X超神级≈149）；
        // 文明力量=科技点+Lv×100；超A级=S/S+/SS（ch1040韩萧82600=SS），X=超神级（ch1402超A之上）
        private const float VigilantPowerRatio = 0.20f;  // 非霸主高阶觉醒者力量≥霸主文明20%→警惕（收编压力，ch1018超能者总数只会一直增加）
        private const float PurgePowerRatio = 0.40f;     // 超星团级文明超A力量≥霸主文明40%→清算风暴（ch1002巅峰之殇：清他文明超A）
        private const float CouncilPowerRatio = 0.60f;   // 超A阶级总力量≥霸主文明60%→协会成立（ch1016麦尼逊阶级联合，文明忌惮）
        private const float SanctumPowerRatio = 0.80f;   // 协会已立且阶级力量≥霸主文明80%→超能圣域（ch1459与三大文明同层次，对等共存）
        private const float VigilantSuppressMult = 0.95f;  // 警惕期非霸主高阶超能者伤害×0.95（收编压力）
        private const int PurgeDuration = 120;      // 清算风暴时长（周期事件，非无尽战争）
        private const int AftermathDuration = 120;  // 绝户计残留时长（清算后超A不敢投靠超星团级→流失向霸主文明，ch1002）
        private const float AftermathPowerMult = 0.80f; // 绝户计：超星团级文明超A力量×0.8（流失/不敢投靠）
        private const float CouncilDamageBonus = 0.10f; // 协会联合加成：超A个体伤害+10%（阶级联合威慑）
        private const float SanctumDamageBonus = 0.15f; // 超能圣域加成：超A个体伤害+15%（圣域体制，对等共存）
        private const float PurgeDamageTakenMult = 1.30f; // 清算期超星团级/民间超A受击伤害×1.3（文明联合压制）
        private const int RecalcInterval = 8;       // 每8 tick 重算态度/协会/圣域
        // v0.76.11 社会形态分叉（原著：主宇宙=半自由三大文明 vs 诸星联=超A全面体制化，ch1207/1210）
        private const float BureaucratBonus = 0.05f;    // 体制化文明：超A纳入编制+5%（诸星联式：超A=安全部干部，受控不作乱）
        private const float AllianceBonus = 0.05f;      // 超星团同盟联合加成+5%（ch1459"战后就会与超A级协会、超星团同盟形成新的制衡格局"）
        private const float AlliancePowerRatio = 0.25f; // 超星团级文明超A力量≥霸主文明25%且被清算过→抱团成立同盟
        private const float AlliancePurgeScale = 1.25f; // 同盟成立后清算触发阈值×1.25（抱团制衡，清算难度上升）

        private static int _tick = 0;
        private static Attitude _attitude = Attitude.Tolerate;
        private static bool _councilFormed = false;
        private static bool _sanctumFormed = false;
        private static int _purgeTicksLeft = 0;     // 清算风暴剩余时长
        private static int _aftermathTicksLeft = 0; // 绝户计残留剩余时长
        private static bool _orderEstablished = false; // 秩序确立（探索历→星海历：霸主文明≥超星团级且力量压过超A阶级）
        private static bool _bureaucratized = false;   // 霸主文明体制化（宇宙级=诸星联式：超A全部纳入编制）
        private static bool _allianceFormed = false;   // 超星团同盟（超星团级文明被清算后抱团制衡）
        private static int _purgeCount = 0;            // 清算风暴发生次数

        // 力量缓存：一次遍历计算4个值，避免重复遍历全图（性能优化）
        private static float _cachedThreatPower = 0f;
        private static float _cachedClassPower = 0f;
        private static float _cachedAwakenedPower = 0f;
        private static float _cachedCivPower = 0f;
        private static bool _powersDirty = true;

        // ============ 状态查询 ============

        public static Attitude CurrentAttitude => _attitude;
        public static bool CouncilFormed => _councilFormed;
        public static bool SanctumFormed => _sanctumFormed;
        public static bool IsSanctum => _attitude == Attitude.Sanctum;
        public static bool IsVigilant => _attitude == Attitude.Vigilant;
        public static bool OrderEstablished => _orderEstablished;
        public static bool Bureaucratized => _bureaucratized;
        public static bool AllianceFormed => _allianceFormed;

        /// <summary>超A级判定：觉醒超能者中能级S级及以上（原著ch1040：韩萧能级82600=阶位SS=超A级，
        /// S级=超A级入门档；X阶=超神级=超A之上，原著ch1402韩萧命名"超A级之上就叫做超神级"；
        /// 普通人即使战力堆高也不属超A级语境——不进威胁池、不被清算波及）</summary>
        public static bool IsSuperA(Actor a)
        {
            return a != null && SuperMechAwakened.IsAwakened(a) && SuperMechAdvancement.GetExactRankIndex(a) >= 10;
        }

        /// <summary>当前存活超A级个体数（含异神）</summary>
        public static int CountSuperA()
        {
            if (World.world == null || World.world.units == null) return 0;
            int count = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (IsSuperA(a)) count++;
            }
            return count;
        }

        /// <summary>嫡系判定：超A个体属于霸主文明（科技最强3王国）之一=战略威慑工具（原著：
        /// 三大文明把超A级收编到自己手里，ch1002"以此把更多超A级都抓到了自己手里"），清算豁免</summary>
        public static bool IsDynasty(Actor a)
        {
            if (a == null || a.kingdom == null) return false;
            foreach (var k in GetTopCivilizations(3))
            {
                if (k != null && k.id == a.kingdom.id) return true;
            }
            return false;
        }

        /// <summary>超星团级文明判定（原著清算对象=超星团级文明的超A盟友资产，ch1002：
        /// 把包括摩多文明在内的所有超星团级文明的超A级盟友屠了个干净）：文明等级≥超星团级(SuperCluster)
        /// 且非霸主文明3王国</summary>
        public static bool IsMidCivilization(Kingdom k)
        {
            if (k == null) return false;
            foreach (var top in GetTopCivilizations(3))
            {
                if (top != null && top.id == k.id) return false; // 霸主文明不是清算对象
            }
            return (int)SuperMechCivilization.GetCivLevelFromKingdom(k) >= 3; // SuperCluster+
        }

        /// <summary>民间自由超A判定：自然觉醒、未依附超星团级/霸主文明的散人超A（原著ch1459
        /// "自然觉醒的民间超能者"——超能圣域收编对象；低层文明/无文明的超A）</summary>
        public static bool IsFreeSuperA(Actor a)
        {
            if (a == null || !IsSuperA(a) || IsDynasty(a)) return false;
            if (a.kingdom != null && (int)SuperMechCivilization.GetCivLevelFromKingdom(a.kingdom) >= 3) return false;
            return true;
        }

        /// <summary>非霸主文明嫡系的自由超A级个体数（显示辅助，判定用力量）</summary>
        public static int CountForeignSuperA()
        {
            if (World.world == null || World.world.units == null) return 0;
            int count = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (IsSuperA(a) && !IsDynasty(a)) count++;
            }
            return count;
        }

        /// <summary>力量指数：能级onar/1000（SS巅峰超A≈83，X超神级≈149，普通超能者≈0.x~10）</summary>
        public static float GetPowerIndex(Actor a)
        {
            if (a == null) return 0f;
            return SuperMechAdvancement.CalcOnar(a) / 1000f;
        }

        /// <summary>超星团级文明超A力量总和（清算对象池——原著巅峰之殇清的就是他文明/盟友的超A资产；
        /// 绝户计残留期×0.8：超A担心人身安全不敢投靠超星团级→流失向霸主文明，ch1002）</summary>
        public static float GetThreatPower()
        {
            if (!_powersDirty) return _cachedThreatPower;
            RecalculatePowers();
            return _cachedThreatPower;
        }

        /// <summary>超A阶级总力量（全体超A，含霸主文明嫡系——原著ch1016麦尼逊"为超A级所代表的阶级
        /// 谋求福利"，阶级=所有超A级个体，不分阵营）</summary>
        public static float GetClassPower()
        {
            if (!_powersDirty) return _cachedClassPower;
            RecalculatePowers();
            return _cachedClassPower;
        }

        /// <summary>民间自由超A力量（圣域收编对象：低层文明/无文明的自然觉醒散人）</summary>
        public static float GetFreeSuperAPower()
        {
            if (World.world == null || World.world.units == null) return 0f;
            float power = 0f;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (IsFreeSuperA(a)) power += GetPowerIndex(a);
            }
            return power;
        }

        /// <summary>非霸主文明嫡系高阶觉醒者（超能者，能级A级及以上）力量总和。
        /// 觉醒者=检测出超能基因并觉醒成超能者（原著ch1004"检测出了超能基因，觉醒成超能者"）；
        /// 低阶超能者（F~B+）对文明无威胁（原著ch1004：凡人埃文斯弄死E级阶位恶徒），威胁只看高阶</summary>
        public static float GetAwakenedPower()
        {
            if (!_powersDirty) return _cachedAwakenedPower;
            RecalculatePowers();
            return _cachedAwakenedPower;
        }

        /// <summary>霸主文明力量：科技点总和+科技Lv×100（集体伟力；科技点0~1000+随发展持续增长）</summary>
        public static float GetCivPower()
        {
            if (!_powersDirty) return _cachedCivPower;
            RecalculatePowers();
            return _cachedCivPower;
        }

        /// <summary>一次遍历计算4个力量值（性能优化：避免4次重复遍历全图）</summary>
        private static void RecalculatePowers()
        {
            _powersDirty = false;
            float threat = 0f, classP = 0f, awakened = 0f, civ = 0f;
            if (World.world != null && World.world.units != null)
            {
                foreach (Actor a in World.world.units.units_only_alive)
                {
                    if (a == null) continue;
                    bool isSuperA = IsSuperA(a);
                    bool isDynasty = IsDynasty(a);
                    float powerIdx = isSuperA ? GetPowerIndex(a) : 0f;
                    if (isSuperA)
                    {
                        classP += powerIdx;
                        if (!isDynasty)
                        {
                            if (a.kingdom != null && (int)SuperMechCivilization.GetCivLevelFromKingdom(a.kingdom) >= 3)
                                threat += powerIdx; // 超星团级文明超A=清算对象
                        }
                    }
                    if (SuperMechAwakened.IsAwakened(a) && !isDynasty && SuperMechAdvancement.GetExactRankIndex(a) >= 8)
                        awakened += GetPowerIndex(a);
                }
            }
            _cachedThreatPower = _aftermathTicksLeft > 0 ? threat * AftermathPowerMult : threat;
            _cachedClassPower = classP;
            _cachedAwakenedPower = awakened;
            foreach (var k in GetTopCivilizations(3))
            {
                if (k == null) continue;
                civ += SuperMechCivilization.GetTechPointsFromKingdom(k) + SuperMechCivilization.GetTechLevelFromKingdom(k) * 100f;
            }
            _cachedCivPower = Mathf.Max(50f, civ);
        }

        /// <summary>标记力量缓存失效（单位死亡/阶位变化/文明变化时调用）</summary>
        public static void MarkPowersDirty() { _powersDirty = true; }

        /// <summary>三大文明：科技最强的3个王国（原著三大超级文明，集体伟力代表）</summary>
        public static List<Kingdom> GetTopCivilizations(int limit = 3)
        {
            var result = new List<Kingdom>();
            if (World.world == null || World.world.kingdoms == null || World.world.kingdoms.list == null) return result;
            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null) continue;
                result.Add(k);
            }
            result.Sort((a, b) => SuperMechCivilization.GetTechLevelFromKingdom(b).CompareTo(SuperMechCivilization.GetTechLevelFromKingdom(a)));
            if (result.Count > limit) result.RemoveRange(limit, result.Count - limit);
            return result;
        }

        /// <summary>超A个体战力加成（叠加）：协会+10%（ch1016阶级联合）；超能圣域+15%（ch1459对等共存）；
        /// 体制化文明编制超A+5%（诸星联式：超A=安全部干部，受控不作乱，ch1207/1210）；
        /// 超星团同盟超A+5%（ch1459三方制衡）</summary>
        public static float GetCouncilDamageBonus(Actor a)
        {
            if (a == null || !IsSuperA(a)) return 1f;
            float bonus = 0f;
            if (_councilFormed) bonus += _sanctumFormed ? SanctumDamageBonus : CouncilDamageBonus;
            if (_bureaucratized && IsDynasty(a)) bonus += BureaucratBonus; // 体制化霸主收编的超A=编制干部
            if (_allianceFormed && a.kingdom != null && IsMidCivilization(a.kingdom)) bonus += AllianceBonus;
            return 1f + bonus;
        }

        /// <summary>清算压制：清算期超星团级/民间超A个体受击伤害×1.3（霸主文明联合压制他文明超A，
        /// 原著巅峰之殇式清算）；霸主文明嫡系超A（战略威慑工具）豁免</summary>
        public static float GetPurgeDamageTakenMult(Actor a)
        {
            if (_attitude != Attitude.Purge || !IsSuperA(a) || IsDynasty(a)) return 1f;
            return PurgeDamageTakenMult;
        }

        /// <summary>收编压力：警惕期非霸主文明嫡系高阶超能者伤害×0.95（原著：文明靠收编拉拢超能者，
        /// 不收编的自由超能者受压；暴力清算只针对超星团级文明的超A资产）</summary>
        public static float GetVigilantSuppressMult(Actor a)
        {
            if (_attitude != Attitude.Vigilant || a == null || !SuperMechAwakened.IsAwakened(a) || IsDynasty(a)) return 1f;
            return VigilantSuppressMult;
        }

        // ============ 主循环 ============

        public static void Tick(float delta)
        {
            if (!SuperMechConfig.SuperAEnabled) return;
            if (World.world == null || World.world.units == null) return;

            _tick++;
            if (_tick % DefectionInterval == 0) TickDefections();
            if (_tick % RecalcInterval != 0) return;

            _powersDirty = true; // 每8tick重算力量（一次遍历算4个值）
            float threatPower = GetThreatPower();    // 超星团级文明超A力量（清算对象=他文明资产）
            float classPower = GetClassPower();      // 超A阶级总力量（含霸主文明嫡系）
            float awakenedPower = GetAwakenedPower();// 非霸主高阶超能者群体力量
            float civPower = GetCivPower();          // 霸主文明集体伟力

            // ①秩序确立（探索历→星海历）：霸主文明≥超星团级 且 文明力量压过超A阶级——
            //   权威结构确立（谁的力量大谁定规则，ch1002/1018）。确立前=探索历（无秩序：
            //   超A自由沉浮、文明无力约束，清算/收编/协会全不生效）
            _orderEstablished = civPower >= classPower && CanPurge();

            // v0.76.23 高维留言：世界达成任一重大里程碑→三大文明（超脱者）向圣所写入成功心得
            // （一次性·跨迭代保留；原著接力：终极文明把经验留给后人，ch1211）
            if (_orderEstablished || _bureaucratized || _sanctumFormed || _allianceFormed)
                SuperMechSanctuary.TryGiveBeyondMessage();

            if (!_orderEstablished)
            {
                if (_attitude == Attitude.Purge)
                {
                    _attitude = Attitude.Tolerate;
                    ApplyPurgeTrait(false);
                }
                else
                {
                    _attitude = Attitude.Tolerate;
                }
                return;
            }

            // ②霸主文明体制化（诸星联式分叉）：霸主文明达宇宙级→超A全面纳入编制（ch1207/1210：
            //   诸星联近万圣体级全是安全部干部、乖宝宝）——编制消化威胁：清算永不触发、
            //   态度恒为默许（文明吞并超A阶级=与圣域并列的另一条稳定化路径）
            _bureaucratized = IsBureaucratCiv();
            if (_bureaucratized)
            {
                if (_attitude == Attitude.Purge)
                {
                    _attitude = Attitude.Tolerate;
                    ApplyPurgeTrait(false);
                }
                else if (_attitude != Attitude.Sanctum)
                {
                    _attitude = Attitude.Tolerate;
                }
                return;
            }

            // ③超星团同盟：超星团级文明被清算过（巅峰之殇历史）且力量达霸主25%→抱团制衡
            //   （ch1459"战后就会与超A级协会、超星团同盟形成新的制衡格局"）
            if (_purgeCount > 0 && !_allianceFormed && threatPower >= civPower * AlliancePowerRatio)
            {
                _allianceFormed = true;
            }

            // ④超能圣域（终局）：协会已立 + 超A阶级力量达霸主文明80% → 政治地位被承认（ch1459
            //   "超能圣域和三大文明的势力达到了同一层次"）——对等共存，清算永久解除，终局不回退
            if (_councilFormed && classPower >= civPower * SanctumPowerRatio)
            {
                if (!_sanctumFormed)
                {
                    _sanctumFormed = true;
                    _attitude = Attitude.Sanctum;
                    _purgeTicksLeft = 0;
                    _aftermathTicksLeft = 0;
                    ApplyPurgeTrait(false);
                }
                return;
            }

            if (_attitude == Attitude.Sanctum) return; // 终局态保持

            // ②清算风暴进行中：倒计时；结束后进入"绝户计"残留（超A担心人身安全不敢投靠超星团级
            //   →流失向霸主文明，ch1002"三大文明以此把更多超A级都抓到了自己手里"）
            if (_attitude == Attitude.Purge)
            {
                _purgeTicksLeft--;
                if (_purgeTicksLeft <= 0)
                {
                    _attitude = Attitude.Tolerate;
                    _aftermathTicksLeft = AftermathDuration;
                    ApplyPurgeTrait(false);
                }
                return;
            }

            // ③绝户计残留期：倒计时后回默许/警惕
            if (_aftermathTicksLeft > 0)
            {
                _aftermathTicksLeft--;
                if (_aftermathTicksLeft <= 0)
                {
                    _attitude = awakenedPower >= civPower * VigilantPowerRatio ? Attitude.Vigilant : Attitude.Tolerate;
                }
                return;
            }

            // ④清算触发：超星团级文明超A力量达霸主文明40%（他文明个人伟力资产膨胀=失控风险，
            //   ch1018"个体伟力将失去掌控"）+ 霸主文明有能力清算（时序：文明未成形无力清算）+
            //   圣域未成立（协会不免疫清算——ch1459"当年三大文明对超A级协会的打压"，直到圣域）
            float purgeRatio = PurgePowerRatio * (_allianceFormed ? AlliancePurgeScale : 1f); // 同盟抱团→清算难度上升
            if (threatPower >= civPower * purgeRatio)
            {
                _attitude = Attitude.Purge;   // 清算风暴（原著巅峰之殇：清超星团级盟友的超A，绝户计）
                _purgeTicksLeft = PurgeDuration;
                _purgeCount++;
                ApplyPurgeTrait(true);
                SuperMechInal.ConfiscateDuringPurge(); // 没收超星团级非嫡系超A的伊纳尔存款（经济动机）
                return;
            }

            // ⑤超A级协会（阶级联合）：超A阶级总力量达霸主文明60%（ch1016麦尼逊阶级觉醒，
            //   联合="拧成一股…高级文明更加忌惮"）——提供联合加成，但不免疫清算（会被打压）
            bool newCouncil = classPower >= civPower * CouncilPowerRatio;
            if (newCouncil != _councilFormed)
            {
                _councilFormed = newCouncil;
                ApplyCouncilTrait(newCouncil);
            }

            // ⑥高阶超能者群体威胁：非霸主高阶觉醒者力量达霸主文明20%→警惕（收编压力）
            if (awakenedPower >= civPower * VigilantPowerRatio)
            {
                _attitude = Attitude.Vigilant;
            }
            else
            {
                _attitude = Attitude.Tolerate;
            }
        }

        /// <summary>清算能力：霸主文明（科技最强3王国）中最强者至少达到超星团级（原著：三大文明=宇宙级
        /// 集体伟力，清算需文明成形；先发展出超A力量而文明未成形时，文明无力清算只能默许拉拢）</summary>
        public static bool CanPurge()
        {
            var top = GetTopCivilizations(3);
            foreach (var k in top)
            {
                if (k == null) continue;
                if ((int)SuperMechCivilization.GetCivLevelFromKingdom(k) >= 3) return true; // SuperCluster+
            }
            return false;
        }

        /// <summary>清算标记应用：只作用于超星团级/民间超A个体（原著巅峰之殇ch1002：清超星团级盟友的
        /// 超A，三大文明嫡系不清算；清算目的是把超A收编为嫡系战略武器）</summary>
        /// <summary>协会成立/解散时给自由超A添加/移除成员标记</summary>
        private static void ApplyCouncilTrait(bool council)
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsSuperA(a) || IsDynasty(a)) continue;
                if (council)
                {
                    if (!a.hasTrait(CouncilTrait)) a.addTrait(CouncilTrait);
                }
                else
                {
                    if (a.hasTrait(CouncilTrait)) a.removeTrait(CouncilTrait);
                }
            }
        }

        /// <summary>清算标记应用：清算风暴时给自由超A添加压制标记</summary>
        private static void ApplyPurgeTrait(bool purge)
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsSuperA(a) || IsDynasty(a)) continue;
                if (purge)
                {
                    if (!a.hasTrait(PurgeTrait)) a.addTrait(PurgeTrait);
                }
                else
                {
                    if (a.hasTrait(PurgeTrait)) a.removeTrait(PurgeTrait);
                }
            }
        }

        /// <summary>获取超A政治倾向（首次访问时随机决定：50%秩序/30%中立/20%混乱）</summary>
        public static PoliticalAlignment GetAlignment(Actor a)
        {
            if (a == null) return PoliticalAlignment.Neutral;
            if (!_alignment.TryGetValue(a.id, out var align))
            {
                float r = Random.value;
                align = r < 0.5f ? PoliticalAlignment.Order : r < 0.8f ? PoliticalAlignment.Neutral : PoliticalAlignment.Chaos;
                _alignment[a.id] = align;
            }
            return align;
        }

        /// <summary>超A投靠文明：秩序派超A主动投靠强大文明获得庇护（原著：超A选择投靠星际财团/虚灵教派/光辉联邦）</summary>
        private static void TickDefections()
        {
            if (World.world == null || World.world.units == null || World.world.kingdoms == null) return;
            var topCivs = GetTopCivilizations(3);
            if (topCivs.Count == 0) return;

            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsSuperA(a)) continue;
                if (GetAlignment(a) != PoliticalAlignment.Order) continue; // 只有秩序派主动投靠
                if (a.kingdom != null && (int)SuperMechCivilization.GetCivLevelFromKingdom(a.kingdom) >= 2) continue; // 已在星团级+文明

                // 绝户计残留期：超A不敢投靠超星团级文明（ch1002）
                bool avoidSuperCluster = _aftermathTicksLeft > 0;

                // 找待遇最好的强大文明（星团级+，绝户计期间避开超星团级）
                // 原著：超A选择投靠待遇最好的文明（俸禄+资源+庇护），不是最近的
                Kingdom target = null;
                int bestSalary = 0;
                foreach (var k in World.world.kingdoms.kingdoms)
                {
                    if (k == null || k.id == a.kingdom?.id) continue;
                    int lvl = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
                    if (lvl < 2) continue; // 至少星团级
                    if (avoidSuperCluster && lvl >= 3) continue; // 绝户计期间避开超星团级
                    int salary = SuperMechInal.CalcSalary(a, k);
                    if (salary > bestSalary) { bestSalary = salary; target = k; }
                }

                if (target != null && Random.value < DefectionChance)
                {
                    a.joinKingdom(target);
                    Debug.Log($"[超神机械师] 超A投靠：{a.name} → {target.name}（秩序派）");
                }
            }
        }

        /// <summary>混乱时代历法名（玩家可在配置选择/修改历法风格，v0.76.13）：
        /// 0=原著历法"探索历"；1=文明历法"混沌时代"；2=时代历法"混沌历"</summary>
        public static string GetEraChaosName()
        {
            switch (SuperMechConfig.EraCalendar)
            {
                case 1: return "混沌时代";
                case 2: return "混沌历";
                default: return "探索历";
            }
        }

        /// <summary>秩序时代历法名：0=原著历法"星海历"；1=文明历法（以霸主文明最强者命名"XX历"）；
        /// 2=时代历法"秩序历"</summary>
        public static string GetEraOrderName()
        {
            switch (SuperMechConfig.EraCalendar)
            {
                case 1:
                    var top = GetTopCivilizations(3);
                    if (top.Count > 0 && top[0] != null)
                    {
                        string n = top[0].name;
                        if (n.EndsWith("历")) return n;
                        return n + "历";
                    }
                    return "星海历";
                case 2: return "秩序历";
                default: return "星海历";
            }
        }

        /// <summary>霸主文明体制化判定：霸主文明（科技最强3王国）中最强达宇宙级（Universal，
        /// 诸星联=宇宙级文明+近万超A编制化，ch1207）</summary>
        public static bool IsBureaucratCiv()
        {
            var top = GetTopCivilizations(3);
            foreach (var k in top)
            {
                if (k == null) continue;
                if ((int)SuperMechCivilization.GetCivLevelFromKingdom(k) >= 4) return true; // Universal+
            }
            return false;
        }

        public static void Clear()
        {
            _tick = 0;
            _attitude = Attitude.Tolerate;
            _councilFormed = false;
            _sanctumFormed = false;
            _purgeTicksLeft = 0;
            _aftermathTicksLeft = 0;
            _orderEstablished = false;
            _bureaucratized = false;
            _allianceFormed = false;
            _purgeCount = 0;
            _alignment.Clear();
        }
    }
}
