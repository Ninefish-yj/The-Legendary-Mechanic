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

        // 数值（力量对比制：威胁=力量比值，非单位数量——用户点破：3个巅峰超A与10个菜鸟超A威胁不同）
        // 力量指数：onar/1000（X阶超A≈149，普通超能者≈0.x~10）；文明力量：科技点+Lv×100
        private const float VigilantPowerRatio = 0.20f;  // 非嫡系超能者力量≥文明力量20%→警惕（收编压力，原著ch1018超能者总数只会一直增加）
        private const float PurgePowerRatio = 0.40f;     // 非嫡系超A力量≥文明力量40%→清算风暴（原著巅峰之殇：清盟友收编嫡系）
        private const float CouncilPowerRatio = 0.60f;   // 超A力量≥文明力量60%→协会成立（对等威慑，原著后期麦尼逊推动）
        private const float DominancePowerRatio = 1.00f; // 超A力量≥文明力量100%→超A时代（个体伟力压倒集体伟力，原著ch1018"个体伟力将失去掌控"）
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

        /// <summary>超A级判定：觉醒超能者中的能级X阶（原著：超A级=超能者体系的顶级强者，
        /// 韩萧等都是超能者；普通人即使战力堆高也不属超A级语境——普通单位不算威胁池、不被清算波及，
        /// 避免"有能级就被清算"的大屠杀）</summary>
        public static bool IsSuperA(Actor a)
        {
            return a != null && SuperMechAwakened.IsAwakened(a) && SuperMechAdvancement.GetExactRankIndex(a) >= 13;
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

        /// <summary>力量指数：能级onar/1000（X阶超A≈149，普通超能者≈0.x~10）</summary>
        public static float GetPowerIndex(Actor a)
        {
            if (a == null) return 0f;
            return SuperMechAdvancement.CalcOnar(a) / 1000f;
        }

        /// <summary>非霸主文明嫡系超A力量总和（清算对象力量；威胁=力量比值非单位数量）</summary>
        public static float GetSuperAPower()
        {
            if (World.world == null || World.world.units == null) return 0f;
            float power = 0f;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsSuperA(a) || IsDynasty(a)) continue;
                power += GetPowerIndex(a);
            }
            return power;
        }

        /// <summary>非霸主文明嫡系觉醒者（超能者）力量总和（超能者群体=个体伟力底座）</summary>
        public static float GetAwakenedPower()
        {
            if (World.world == null || World.world.units == null) return 0f;
            float power = 0f;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechAwakened.IsAwakened(a) || IsDynasty(a)) continue;
                power += GetPowerIndex(a);
            }
            return power;
        }

        /// <summary>霸主文明力量：科技点总和+科技Lv×100（集体伟力；科技点0~1000+随发展持续增长）</summary>
        public static float GetCivPower()
        {
            float power = 0f;
            foreach (var k in GetTopCivilizations(3))
            {
                if (k == null) continue;
                power += SuperMechCivilization.GetTechPointsFromKingdom(k) + SuperMechCivilization.GetTechLevelFromKingdom(k) * 100f;
            }
            return Mathf.Max(50f, power); // 最低保底，避免空世界误触发
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

            float superAPower = GetSuperAPower();      // 非嫡系超A力量（清算对象）
            float awakenedPower = GetAwakenedPower();   // 非嫡系超能者群体力量
            float civPower = GetCivPower();             // 霸主文明集体伟力
            _councilFormed = superAPower >= civPower * CouncilPowerRatio; // 超A力量达文明60%→协会成立（对等威慑）

            // ①超A时代：超A力量压倒文明力量（原著ch1018"个体伟力将失去掌控"）→ 清算永久停、协会主导
            if (superAPower >= civPower * DominancePowerRatio)
            {
                _attitude = Attitude.Dominance;
                _purgeTicksLeft = 0;
                ApplyPurgeTrait(false);
                return;
            }

            // ②清算风暴进行中：倒计时，结束后回默许/警惕（霸主文明收编/拉拢，非无尽战争）
            if (_attitude == Attitude.Purge)
            {
                _purgeTicksLeft--;
                if (_purgeTicksLeft <= 0)
                {
                    _attitude = awakenedPower >= civPower * VigilantPowerRatio ? Attitude.Vigilant : Attitude.Tolerate;
                    ApplyPurgeTrait(false);
                }
                return;
            }

            // ③超能者群体威胁：非嫡系超能者力量达文明20%→警惕（收编压力）
            if (awakenedPower >= civPower * VigilantPowerRatio)
            {
                _attitude = Attitude.Vigilant;
            }
            else if (_attitude == Attitude.Vigilant)
            {
                _attitude = Attitude.Tolerate;
            }

            // ④清算触发：非嫡系超A力量达文明40% + 协会未成立 + 霸主文明有能力清算（时序：文明未成形无力清算，
            //    先发展出超A力量时只有默许拉拢——原著巅峰之殇是三大文明成形后的历史事件）
            if (superAPower >= civPower * PurgePowerRatio && !_councilFormed && CanPurge())
            {
                _attitude = Attitude.Purge;             // 非嫡系超A力量泛滥→霸主文明清算风暴（原著巅峰之殇：清盟友收编嫡系）
                _purgeTicksLeft = PurgeDuration;
                ApplyPurgeTrait(true);
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
