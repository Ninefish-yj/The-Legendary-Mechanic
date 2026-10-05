using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.0 个体伟力·集体伟力体系（原著后期主线，用户指定方向）
    /// 原著依据：
    ///  1. 超A级=个体伟力：掌握个人伟力的超A级已是宇宙大人物，但在高级文明眼中更多是战略威慑（ch1016）；
    ///  2. 三大文明默许/清算：三大文明担忧"个体伟力泛滥"，历史上以"巅峰之殇"清算超A级盟友
    ///     （ch1002），后期讨论"新的清算"（ch1018）；
    ///  3. 超A级协会（原著后期创建）：超A级个体联合起来提升宇宙地位、对抗文明压制（ch1016 麦尼逊推动）；
    ///  4. 异神=顶层超A之一（ch712"异能之神"），能存在是三大文明的默许——受集体伟力框架约束。
    /// 机制：超A级个体（能级X阶）计数驱动文明态度（默许/清算）；超A级协会（个体伟力联合）
    /// 在超A数量达标时成立，抵消清算、提供联合加成；异神为顶层超A个体之一。
    /// 清算分对象（原著巅峰之殇ch1002）：清的是超星团级盟友/非嫡系的自由超A个体，
    /// 三大文明嫡系超A（战略威慑工具）不清算——清算目的反而是把超A收编为嫡系；
    /// 清算非必然且有条件（原著ch1018：政治考量，巴德尔"不当任期内做"；文明间在维护统治权威上联合，
    /// 平时互相竞争用超A当战略威慑工具）：
    ///  ①清算能力=霸主文明至少超星团级（文明未成形无力清算——时序：先有超A则只有默许拉拢）；
    ///  ②清算=周期事件（120tick清算风暴后回默许收编，非无尽战争）；
    ///  ③协会成立（超A≥5）抵消清算；
    ///  ④超A≥8=个体伟力压倒集体伟力（原著ch1018"个体伟力将失去掌控"），超A时代：清算永久停、协会主导。
    /// </summary>
    public static class SuperMechSupermA
    {
        public const string CouncilTrait = "sm_supera_council"; // 超A级协会成员标记
        public const string PurgeTrait = "sm_supera_purge";     // 清算标记（文明联合压制）

        public enum Attitude { Tolerate, Vigilant, Purge, Dominance } // 默许 / 警惕（超能者群体收编压力） / 清算风暴 / 超A时代

        // 数值（原著尺度等比缩放）
        private const int SuperAThreshold = 3;      // 非嫡系超A≥3 触发清算（自由个体伟力泛滥的担忧，原著巅峰之殇导火索）
        private const int CouncilThreshold = 5;     // 超A≥5 协会成立（原著：协会为超A级规模庞大后由麦尼逊等推动创建，ch1016"超A总数只会一直增加"）
        private const int DominanceThreshold = 8;   // 超A≥8=个体伟力压倒集体伟力（原著ch1018"个体伟力将失去掌控"）
        private const int AwakenedVigilantThreshold = 20; // 非嫡系超能者≥20→霸主文明警惕（超能者群体泛滥，原著ch1018超A/超能者总数只会一直增加）
        private const float VigilantSuppressMult = 0.95f;  // 警惕期非嫡系超能者伤害×0.95（收编压力：逼自由超能者投靠文明）
        private const int PurgeDuration = 120;      // 清算风暴时长（周期事件，非无尽战争；风暴后回默许=霸主文明收编/拉拢）
        private const float CouncilDamageBonus = 0.10f; // 协会联合加成：超A个体伤害+10%
        private const float DominanceDamageBonus = 0.15f; // 超A时代协会主导：伤害+15%
        private const float PurgeDamageTakenMult = 1.30f; // 清算期超A个体受击伤害×1.3（文明联合压制）
        private const int RecalcInterval = 8;       // 每8 tick 重算态度/协会

        private static int _tick = 0;
        private static Attitude _attitude = Attitude.Tolerate;
        private static bool _councilFormed = false;
        private static int _purgeTicksLeft = 0;     // 清算风暴剩余时长

        // ============ 状态查询 ============

        public static Attitude CurrentAttitude => _attitude;
        public static bool CouncilFormed => _councilFormed;

        /// <summary>超A级判定：能级X阶（原著：超A级=超脱A级的顶级强者，模组X阶为能级顶点）</summary>
        public static bool IsSuperA(Actor a)
        {
            return a != null && SuperMechAdvancement.GetExactRankIndex(a) >= 13;
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

        /// <summary>嫡系判定：超A个体属于霸主文明（科技最强3王国）之一=战略威慑工具（原著：三大文明
        /// 把超A级收编到自己手里），清算豁免；非嫡系=有自由思想、不受控制风险的独立个体（ch1019），
        /// 才是清算对象（原著巅峰之殇：清超星团级盟友的超A，不碰自己人）</summary>
        public static bool IsDynasty(Actor a)
        {
            if (a == null || a.kingdom == null) return false;
            foreach (var k in GetTopCivilizations(3))
            {
                if (k != null && k.id == a.kingdom.id) return true;
            }
            return false;
        }

        /// <summary>非霸主文明嫡系的觉醒者（超能者）数量：超能者群体=个体伟力底座，泛滥=威胁文明秩序
        /// （原著：文明自己的超能者是资产，不受控的自由超能者是隐患）</summary>
        public static int CountForeignAwakened()
        {
            if (World.world == null || World.world.units == null) return 0;
            int count = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechAwakened.IsAwakened(a)) continue;
                if (!IsDynasty(a)) count++;
            }
            return count;
        }

        /// <summary>收编压力：警惕期非霸主文明嫡系超能者伤害×0.95（原著：文明靠收编拉拢超能者，
        /// 不收编的自由超能者受压；暴力清算只针对超A级顶端泛滥）</summary>
        public static float GetVigilantSuppressMult(Actor a)
        {
            if (_attitude != Attitude.Vigilant || a == null || !SuperMechAwakened.IsAwakened(a) || IsDynasty(a)) return 1f;
            return VigilantSuppressMult;
        }

        /// <summary>非霸主文明嫡系的自由超A级个体数（清算对象池）</summary>
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

        /// <summary>协会联合加成：超A个体伤害+10%；超A时代（个体伟力压倒集体伟力）协会主导+15%（原著ch1018）</summary>
        public static float GetCouncilDamageBonus(Actor a)
        {
            if (!_councilFormed || !IsSuperA(a)) return 1f;
            return 1f + (_attitude == Attitude.Dominance ? DominanceDamageBonus : CouncilDamageBonus);
        }

        /// <summary>清算压制：清算期非嫡系超A个体受击伤害×1.3（霸主文明联合压制自由个体伟力，
        /// 原著巅峰之殇式清算）；嫡系超A（战略威慑工具）豁免</summary>
        public static float GetPurgeDamageTakenMult(Actor a)
        {
            if (_attitude != Attitude.Purge || !IsSuperA(a) || IsDynasty(a)) return 1f;
            return PurgeDamageTakenMult;
        }

        public static bool IsDominance => _attitude == Attitude.Dominance;

        // ============ 主循环 ============

        public static void Tick(float delta)
        {
            if (!SuperMechConfig.SuperAEnabled) return;
            if (World.world == null || World.world.units == null) return;

            _tick++;
            if (_tick % RecalcInterval != 0) return;

            int count = CountSuperA();
            int foreign = CountForeignSuperA();          // 清算对象池：非霸主文明嫡系的自由超A
            _councilFormed = count >= CouncilThreshold;  // 原著后期：超A级协会由超A级个体大规模联合创建

            // ①超A时代：个体伟力压倒集体伟力（原著ch1018"个体伟力将失去掌控"）→ 清算永久停、协会主导
            if (count >= DominanceThreshold)
            {
                _attitude = Attitude.Dominance;
                _purgeTicksLeft = 0;
                ApplyPurgeTrait(false);
                return;
            }

            // ②超能者群体威胁：非嫡系超能者泛滥→霸主文明警惕（收编压力）；超A清算风暴优先
            if (_attitude == Attitude.Purge || _attitude == Attitude.Dominance)
            {
                // 清算/超A时代优先，不覆盖
            }
            else if (CountForeignAwakened() >= AwakenedVigilantThreshold)
            {
                _attitude = Attitude.Vigilant;
            }
            else
            {
                _attitude = Attitude.Tolerate;
            }

            // ③清算风暴进行中：倒计时，结束后回默许（霸主文明收编/拉拢，非无尽战争）
            if (_attitude == Attitude.Purge)
            {
                _purgeTicksLeft--;
                if (_purgeTicksLeft <= 0)
                {
                    _attitude = Attitude.Tolerate;
                    ApplyPurgeTrait(false);
                }
                return;
            }

            // ④清算触发：非嫡系自由超A泛滥 + 协会未成立 + 霸主文明有能力清算（时序：文明未成形无力清算，
            //    先发展出超A力量时只有默许拉拢——原著巅峰之殇是三大文明成形后的历史事件）
            if (foreign >= SuperAThreshold && !_councilFormed && CanPurge())
            {
                _attitude = Attitude.Purge;             // 非嫡系自由超A泛滥→霸主文明清算风暴（原著巅峰之殇：清盟友收编嫡系）
                _purgeTicksLeft = PurgeDuration;
                ApplyPurgeTrait(true);
                return;
            }

            // ⑤回退：清算风暴结束或未触发时，按超能者威胁重估（警惕/默许）
            if (_attitude == Attitude.Purge)
            {
                _attitude = CountForeignAwakened() >= AwakenedVigilantThreshold ? Attitude.Vigilant : Attitude.Tolerate;
            }
        }

        /// <summary>清算能力：霸主文明（科技最强3王国）中最强者至少达到超星团级（原著：三大文明=宇宙级
        /// 集体伟力，清算需文明成形；先发展出超A力量而文明未成形时，文明无力清算只能默许）</summary>
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

        /// <summary>清算标记应用：只作用于非霸主文明嫡系的自由超A个体（原著巅峰之殇ch1002：
        /// 清超星团级盟友的超A，三大文明嫡系不清算；清算目的是把超A收编为嫡系战略武器）</summary>
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

        public static void Clear()
        {
            _tick = 0;
            _attitude = Attitude.Tolerate;
            _councilFormed = false;
            _purgeTicksLeft = 0;
        }

        public static bool IsVigilant => _attitude == Attitude.Vigilant;
    }
}
