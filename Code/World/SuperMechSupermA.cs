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
    /// </summary>
    public static class SuperMechSupermA
    {
        public const string CouncilTrait = "sm_supera_council"; // 超A级协会成员标记
        public const string PurgeTrait = "sm_supera_purge";     // 清算标记（文明联合压制）

        public enum Attitude { Tolerate, Purge } // 默许 / 清算

        // 数值（原著尺度等比缩放）
        private const int SuperAThreshold = 3;      // 超A≥3 触发清算（个体伟力泛滥的担忧）
        private const int CouncilThreshold = 2;     // 超A≥2 协会成立（个体伟力联合）
        private const float CouncilDamageBonus = 0.10f; // 协会联合加成：超A个体伤害+10%
        private const float PurgeDamageTakenMult = 1.30f; // 清算期超A个体受击伤害×1.3（文明联合压制）
        private const int RecalcInterval = 8;       // 每8 tick 重算态度/协会

        private static int _tick = 0;
        private static Attitude _attitude = Attitude.Tolerate;
        private static bool _councilFormed = false;

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

        /// <summary>协会联合加成：超A个体伤害+10%（原著：超A级联合提升地位）</summary>
        public static float GetCouncilDamageBonus(Actor a)
        {
            if (!_councilFormed || !IsSuperA(a)) return 1f;
            return 1f + CouncilDamageBonus;
        }

        /// <summary>清算压制：清算期超A个体受击伤害×1.3（文明联合压制个体伟力，原著巅峰之殇式清算）</summary>
        public static float GetPurgeDamageTakenMult(Actor a)
        {
            if (_attitude != Attitude.Purge || !IsSuperA(a)) return 1f;
            return PurgeDamageTakenMult;
        }

        // ============ 主循环 ============

        public static void Tick(float delta)
        {
            if (!SuperMechConfig.SuperAEnabled) return;
            if (World.world == null || World.world.units == null) return;

            _tick++;
            if (_tick % RecalcInterval != 0) return;

            int count = CountSuperA();
            _councilFormed = count >= CouncilThreshold;  // 原著后期：超A级协会由超A级个体联合创建
            if (count >= SuperAThreshold && !_councilFormed)
            {
                _attitude = Attitude.Purge;             // 个体伟力泛滥→文明清算（ch1018）
                ApplyPurgeTrait(true);                  // 清算不分敌我：所有超A个体（含自己人）打清算标记
            }
            else
            {
                _attitude = Attitude.Tolerate;          // 默许/拉拢（协会成立后文明转默许）
                ApplyPurgeTrait(false);
            }
        }

        /// <summary>清算标记应用：清算不分阵营（原著巅峰之殇ch1002：三大文明连超星团级盟友的超A级一起清算，
        /// 怕的是"个体伟力泛滥"本身，ch1019：超A是"有自由思想、不受控制风险的独立个体"）</summary>
        private static void ApplyPurgeTrait(bool purge)
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsSuperA(a)) continue;
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
        }
    }
}
