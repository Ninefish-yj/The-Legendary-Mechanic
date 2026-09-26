using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 天赋倾向系统（原著：每人有1-3个天赋倾向，带评级F-S，影响修炼速度）。
    /// 天赋是潜在的，不直接决定职业方向。踏入超能时获得天赋倾向，后天选定主职业。
    /// </summary>
    public static class SuperMechTalent
    {
        public enum TalentType
        {
            Mechanical,  // 机械天赋（械感）
            Martial,     // 武道天赋（体魄）
            Psi,         // 异能天赋（异能潜力）
            Mage,        // 魔法天赋（魔感）
            Mind         // 念力天赋（精神）
        }

        public class TalentInfo
        {
            public TalentType type;
            public int rating;  // 0=F, 1=E, 2=D, 3=C, 4=B, 5=A, 6=S
            public string specificPower;  // 具体异能类型（电磁/火焰/念动力等）
        }

        // 具体异能类型库（原著+同人补全）
        private static readonly string[][] SpecificPowers = {
            new[] { "电磁操控", "能量亲和", "虚拟意识", "机械心灵", "纳米操控", "量子计算" },  // 机械系
            new[] { "体魄强化", "气血澎湃", "战本能", "气劲外放", "金刚不坏", "血脉觉醒" },  // 武道系
            new[] { "元素操控", "念动力", "空间异能", "时间感知", "物质转化", "心灵感应" },  // 异能系
            new[] { "元素魔法", "变化术", "造物术", "召唤术", "结界术", "符文魔法" },        // 魔法系
            new[] { "灵魂感知", "法则之眼", "现实扭曲", "精神冲击", "记忆操控", "预知未来" }   // 念力系
        };

        // unit.id -> 天赋倾向列表
        private static readonly Dictionary<long, List<TalentInfo>> _talents = new Dictionary<long, List<TalentInfo>>();

        // 评级名称
        public static readonly string[] RatingNames = { "F", "E", "D", "C", "B", "A", "S" };

        // 天赋类型名称
        public static string GetTalentName(TalentType type)
        {
            switch (type)
            {
                case TalentType.Mechanical: return "械感";
                case TalentType.Martial: return "体魄";
                case TalentType.Psi: return "异能潜力";
                case TalentType.Mage: return "魔感";
                case TalentType.Mind: return "精神";
                default: return "未知";
            }
        }

        // 天赋类型对应体系
        public static string GetTalentClass(TalentType type)
        {
            switch (type)
            {
                case TalentType.Mechanical: return "机械系";
                case TalentType.Martial: return "武道系";
                case TalentType.Psi: return "异能系";
                case TalentType.Mage: return "魔法系";
                case TalentType.Mind: return "念力系";
                default: return "未知";
            }
        }

        /// <summary>踏入超能：随机获得1-3个天赋倾向，带评级。</summary>
        public static List<TalentInfo> GenerateTalents()
        {
            var talents = new List<TalentInfo>();
            int count = Random.Range(1, 4);  // 1-3个

            var allTypes = new List<TalentType> { TalentType.Mechanical, TalentType.Martial, TalentType.Psi, TalentType.Mage, TalentType.Mind };
            for (int i = 0; i < count; i++)
            {
                if (allTypes.Count == 0) break;
                int idx = Random.Range(0, allTypes.Count);
                var type = allTypes[idx];
                allTypes.RemoveAt(idx);

                // 评级：F(40%) E(25%) D(15%) C(10%) B(6%) A(3%) S(1%)
                float roll = Random.value;
                int rating = 0;
                if (roll < 0.40f) rating = 0;        // F
                else if (roll < 0.65f) rating = 1;   // E
                else if (roll < 0.80f) rating = 2;   // D
                else if (roll < 0.90f) rating = 3;   // C
                else if (roll < 0.96f) rating = 4;   // B
                else if (roll < 0.99f) rating = 5;   // A
                else rating = 6;                      // S

                talents.Add(new TalentInfo
                {
                    type = type,
                    rating = rating,
                    specificPower = SpecificPowers[(int)type][Random.Range(0, SpecificPowers[(int)type].Length)]
                });
            }
            return talents;
        }

        /// <summary>给单位赋予天赋倾向（踏入超能）。</summary>
        public static void GrantTalents(Actor a)
        {
            if (a == null) return;
            if (_talents.ContainsKey(a.id)) return;  // 已有天赋
            _talents[a.id] = GenerateTalents();
        }

        /// <summary>获取单位的天赋倾向。</summary>
        public static List<TalentInfo> GetTalents(Actor a)
        {
            if (a == null) return new List<TalentInfo>();
            if (_talents.TryGetValue(a.id, out var list)) return list;
            return new List<TalentInfo>();
        }

        /// <summary>单位是否已踏入超能（有天赋倾向）。</summary>
        public static bool HasTalent(Actor a)
        {
            if (a == null) return false;
            return _talents.ContainsKey(a.id) && _talents[a.id].Count > 0;
        }

        /// <summary>获取单位某系的天赋评级（没有该系天赋返回-1）。</summary>
        public static int GetTalentRating(Actor a, TalentType type)
        {
            var talents = GetTalents(a);
            foreach (var t in talents)
            {
                if (t.type == type) return t.rating;
            }
            return -1;
        }

        /// <summary>天赋修炼速度加成（评级越高越快）。</summary>
        public static float GetTrainingSpeed(Actor a, TalentType type)
        {
            int rating = GetTalentRating(a, type);
            if (rating < 0) return 0.5f;  // 无天赋，跨系修炼慢
            return 1f + rating * 0.15f;   // F=1.0x, E=1.15x, ... S=1.9x
        }

        /// <summary>天赋阶位上限（评级决定最终能达到的阶位）。</summary>
        public static int GetMaxRank(Actor a)
        {
            var talents = GetTalents(a);
            int maxRating = -1;
            foreach (var t in talents)
            {
                if (t.rating > maxRating) maxRating = t.rating;
            }
            // F=E阶上限, E=D, D=C, C=B, B=A, A=S, S=SS
            if (maxRating < 0) return 1;  // 无天赋，E阶上限
            return Mathf.Min(maxRating + 1, 12);  // 最高SS阶，X阶需要特殊条件
        }

        /// <summary>清理死亡单位数据。</summary>
        public static void CleanupDead(List<long> aliveIds)
        {
            var toRemove = new List<long>();
            foreach (var id in _talents.Keys)
            {
                if (!aliveIds.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove) _talents.Remove(id);
        }

        /// <summary>获取存档数据。</summary>
        public static Dictionary<string, object> GetSaveData()
        {
            var data = new Dictionary<string, object>();
            var dict = new Dictionary<string, object>();
            foreach (var kv in _talents)
            {
                var list = new List<object>();
                foreach (var t in kv.Value)
                {
                    list.Add(new Dictionary<string, object> { { "type", (int)t.type }, { "rating", t.rating } });
                }
                dict[kv.Key.ToString()] = list;
            }
            data["talents"] = dict;
            return data;
        }

        /// <summary>加载存档数据。</summary>
        public static void LoadSaveData(Dictionary<string, object> data)
        {
            _talents.Clear();
            if (data == null || !data.ContainsKey("talents")) return;
            var dict = data["talents"] as Dictionary<string, object>;
            if (dict == null) return;
            foreach (var kv in dict)
            {
                long id = long.Parse(kv.Key);
                var list = new List<TalentInfo>();
                var arr = kv.Value as List<object>;
                if (arr != null)
                {
                    foreach (var item in arr)
                    {
                        var d = item as Dictionary<string, object>;
                        if (d != null)
                        {
                            list.Add(new TalentInfo
                            {
                                type = (TalentType)System.Convert.ToInt32(d["type"]),
                                rating = System.Convert.ToInt32(d["rating"])
                            });
                        }
                    }
                }
                _talents[id] = list;
            }
        }
    }
}
