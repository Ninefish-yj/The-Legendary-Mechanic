using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 伊纳尔经济系统（原著：主宇宙通用货币，宇宙级文明制订，超A俸禄/佣金/悬赏用伊纳尔）
    /// 双轨制：普通单位用WorldBox金币（地表层），超A/星际文明用伊纳尔（星际层）
    /// </summary>
    public static class SuperMechInal
    {
        private static readonly Dictionary<long, int> _balance = new Dictionary<long, int>();
        private const int SalaryInterval = 32;       // 每32 tick发一次俸禄
        private const int BaseSalary = 50;           // 基础俸禄（E阶）
        private static int _salaryTick = 0;

        /// <summary>伊纳尔余额</summary>
        public static int GetInal(Actor a)
        {
            if (a == null) return 0;
            return _balance.TryGetValue(a.id, out var v) ? v : 0;
        }

        public static void AddInal(Actor a, int amount)
        {
            if (a == null || amount <= 0) return;
            _balance[a.id] = Mathf.Clamp(GetInal(a) + amount, 0, 99999999);
        }

        public static bool SpendInal(Actor a, int amount)
        {
            if (a == null || amount <= 0) return false;
            int cur = GetInal(a);
            if (cur < amount) return false;
            _balance[a.id] = cur - amount;
            return true;
        }

        /// <summary>计算超A在某文明的期望俸禄（文明等级×阶位系数）</summary>
        public static int CalcSalary(Actor superA, Kingdom k)
        {
            if (superA == null || k == null) return 0;
            int civLevel = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
            int rank = SuperMechAdvancement.GetExactRankIndex(superA);
            // 俸禄 = 基础 × 文明等级系数 × 阶位系数
            return BaseSalary * (civLevel + 1) * (rank + 1);
        }

        /// <summary>俸禄发放Tick：投靠文明的超A获得伊纳尔俸禄</summary>
        public static void TickSalary()
        {
            _salaryTick++;
            if (_salaryTick % SalaryInterval != 0) return;
            if (World.world == null || World.world.units == null) return;

            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                if (a.kingdom == null) continue;
                int salary = CalcSalary(a, a.kingdom);
                if (salary > 0)
                {
                    AddInal(a, salary);
                }
            }
        }

        /// <summary>清算风暴：文明没收境内超A的伊纳尔存款（经济动机，原著巅峰之殇）</summary>
        public static void ConfiscateDuringPurge()
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !SuperMechSupermA.IsSuperA(a)) continue;
                if (a.kingdom == null) continue;
                // 清算对象：超星团级文明的非嫡系超A
                if (SuperMechSupermA.IsMidCivilization(a.kingdom) && !SuperMechSupermA.IsDynasty(a))
                {
                    int seized = GetInal(a);
                    if (seized > 0)
                    {
                        _balance[a.id] = 0;
                        Debug.Log($"[超神机械师] 清算没收：{a.name} 被没收 {seized} 伊纳尔");
                    }
                }
            }
        }

        public static void Clear()
        {
            _balance.Clear();
            _salaryTick = 0;
        }
    }
}
