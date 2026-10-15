using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.196 势力战争战报系统
    /// 记录每次战争的伤亡、缴获、指挥官表现，可回看历史战报</summary>
    public static class SuperMechBattleReport
    {
        public struct BattleReportData
        {
            public string id;
            public string factionA;
            public string factionB;
            public int year;
            public int casualtiesA;
            public int casualtiesB;
            public int superACasualtiesA;
            public int superACasualtiesB;
            public string result; // "A胜"/"B胜"/"僵持"
            public string description;
            public BattleReportData(string i, string a, string b, int y)
            {
                id = i; factionA = a; factionB = b; year = y;
                casualtiesA = 0; casualtiesB = 0;
                superACasualtiesA = 0; superACasualtiesB = 0;
                result = "进行中"; description = "";
            }
        }

        private static readonly List<BattleReportData> _reports = new List<BattleReportData>();
        private static BattleReportData _currentWar;
        private static bool _inWar = false;

        /// <summary>战争开始时调用</summary>
        public static void OnWarDeclared(string factionA, string factionB)
        {
            if (_inWar) EndCurrentWar("僵持");
            _currentWar = new BattleReportData(
                System.Guid.NewGuid().ToString("N").Substring(0, 8),
                factionA, factionB, World.world == null ? 1 : World.world.year);
            _inWar = true;
            Debug.Log($"[超神机械师] 战报：{factionA} vs {factionB} 战争开始");
        }

        /// <summary>战斗中单位死亡时记录伤亡</summary>
        public static void OnUnitKilled(Actor killer, Actor target)
        {
            if (!_inWar || target == null) return;
            var factionData = SuperMechFaction.GetFaction(target);
            if (factionData == null || string.IsNullOrEmpty(factionData.name)) return;
            string targetFaction = factionData.name;

            bool isSuperA = SuperMechSupermA.IsSuperA(target);

            if (targetFaction == _currentWar.factionA)
            {
                _currentWar.casualtiesA++;
                if (isSuperA) _currentWar.superACasualtiesA++;
            }
            else if (targetFaction == _currentWar.factionB)
            {
                _currentWar.casualtiesB++;
                if (isSuperA) _currentWar.superACasualtiesB++;
            }
        }

        /// <summary>战争结束时调用</summary>
        public static void EndCurrentWar(string result)
        {
            if (!_inWar) return;
            _currentWar.result = result;
            _currentWar.description = GenerateDescription(_currentWar);
            _reports.Add(_currentWar);
            if (_reports.Count > 50) _reports.RemoveAt(0); // 最多保留50条
            _inWar = false;
            Debug.Log($"[超神机械师] 战报：{_currentWar.factionA} vs {_currentWar.factionB} 结束，结果：{result}");
        }

        private static string GenerateDescription(BattleReportData r)
        {
            string desc = $"{r.factionA}与{r.factionB}的战争";
            desc += $"，{r.factionA}伤亡{r.casualtiesA}人（超A{r.superACasualtiesA}人）";
            desc += $"，{r.factionB}伤亡{r.casualtiesB}人（超A{r.superACasualtiesB}人）";
            return desc;
        }

        /// <summary>获取所有战报</summary>
        public static List<BattleReportData> GetAllReports()
        {
            return _reports;
        }

        /// <summary>获取当前进行中的战争</summary>
        public static BattleReportData? GetCurrentWar()
        {
            return _inWar ? _currentWar : (BattleReportData?)null;
        }

        /// <summary>是否有进行中的战争</summary>
        public static bool IsInWar() { return _inWar; }
    }
}
