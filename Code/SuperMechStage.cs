using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质）。
    /// 机械系14阶段：爱好者→学徒→见习→磁环→数据→战争→虚拟→星海→真理→使徒→帝皇→主宰→神座→超神机械师。
    /// 其他四系暂用阶位承载，阶段名留空。
    /// 阶段只在单位面板显示，玩家通过知识树/转职来推进。
    /// </summary>
    public static class SuperMechStage
    {
        public static readonly string[] StageNames =
        {
            "机械爱好者", "机械师学徒", "见习机械师", "磁环", "数据", "战争",
            "虚拟", "星海", "真理", "使徒", "帝皇", "主宰", "神座", "超神机械师"
        };

        // 每阶段转职的气力/属性奖励（原著ch237/362/477/539/684/717/770/890/957/1039）
        public static readonly float[] StageQiBonus = { 0, 5, 10, 20, 50, 80, 120, 200, 300, 500, 800, 1200, 2000, 5000 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        /// <summary>获取单位职业阶段（0=未入门，1-14）。</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        /// <summary>获取单位职业阶段名称。</summary>
        public static string GetStageName(Actor a)
        {
            int s = GetStage(a);
            if (s <= 0) return "未入门";
            if (s > StageNames.Length) return StageNames[StageNames.Length - 1];
            return StageNames[s - 1];
        }

        /// <summary>设置单位职业阶段（转职时调用）。</summary>
        public static void SetStage(Actor a, int stage)
        {
            if (a == null) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, StageNames.Length);
        }

        /// <summary>晋升到下一阶段，返回是否成功。</summary>
        public static bool Advance(Actor a)
        {
            int cur = GetStage(a);
            if (cur >= StageNames.Length) return false;
            SetStage(a, cur + 1);
            // 转职气力奖励
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQi(a, StageQiBonus[cur]);
            }
            Debug.Log($"[超神机械师] {a.Name} 职业晋升：{StageNames[cur]} → {StageNames[cur + 1]}");
            return true;
        }

        /// <summary>机械系是否达到指定阶段。</summary>
        public static bool IsAtLeast(Actor a, int stage)
        {
            return GetStage(a) >= stage;
        }

        /// <summary>清除单位阶段数据（单位死亡时调用）。</summary>
        public static void Clear(Actor a)
        {
            if (a != null) _stage.Remove(a.data.id);
        }
    }
}
