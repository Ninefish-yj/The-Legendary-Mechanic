using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
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

        private static readonly string[][] SpecificPowers = {
            new[] { "sm_talent_1370", "sm_talent_1371", "sm_talent_1372", "sm_talent_1373", "sm_talent_1374", "sm_talent_1375" },  // 机械系（神器·机械知识树）
            new[] { "sm_talent_1376", "sm_talent_1377", "sm_talent_1378", "sm_talent_1379", "sm_talent_1380", "sm_talent_1381" },  // 武道系（神体·御气技巧树）
            new[] { "sm_talent_1382", "sm_talent_1383", "sm_talent_1384", "sm_talent_1385", "sm_talent_1386", "sm_talent_1387" },  // 异能系（神通·基因树，基因变异类）
            new[] { "sm_talent_1388", "sm_talent_1389", "sm_talent_1390", "sm_talent_1391", "sm_talent_1392", "sm_talent_1393" },        // 魔法系（神权·魔法知识树）
            new[] { "sm_talent_1394", "sm_talent_1395", "sm_talent_1396", "sm_talent_1397", "sm_talent_1398", "sm_talent_1399" }   // 念力系（神魂·精神修炼树，精神力类）
        };

        private static readonly Dictionary<long, List<TalentInfo>> _talents = new Dictionary<long, List<TalentInfo>>();

        public static readonly string[] RatingNames = { "F", "E", "D", "C", "B", "A", "S" };

        public static string GetTalentName(TalentType type)
        {
            switch (type)
            {
                case TalentType.Mechanical: return LocalizedTextManager.getText("sm_talent_1005");
                case TalentType.Martial: return LocalizedTextManager.getText("sm_talent_1006");
                case TalentType.Psi: return LocalizedTextManager.getText("sm_talent_1007");
                case TalentType.Mage: return LocalizedTextManager.getText("sm_talent_1008");
                case TalentType.Mind: return LocalizedTextManager.getText("sm_talent_1009");
                default: return LocalizedTextManager.getText("sm_talent_1010");
            }
        }

        public static string GetTalentClass(TalentType type)
        {
            switch (type)
            {
                case TalentType.Mechanical: return LocalizedTextManager.getText("sm_talent_1011");
                case TalentType.Martial: return LocalizedTextManager.getText("sm_talent_1012");
                case TalentType.Psi: return LocalizedTextManager.getText("sm_talent_1013");
                case TalentType.Mage: return LocalizedTextManager.getText("sm_talent_1014");
                case TalentType.Mind: return LocalizedTextManager.getText("sm_talent_1015");
                default: return LocalizedTextManager.getText("sm_talent_1010");
            }
        }

        private static readonly HashSet<long> _fiveSystemGenius = new HashSet<long>();

        public static bool IsFiveSystemGenius(Actor a)
        {
            if (a == null) return false;
            return _fiveSystemGenius.Contains(a.id);
        }

        public static List<TalentInfo> GenerateTalents()
        {
            var talents = new List<TalentInfo>();

            if (Random.value < 0.0001f)
            {
                var allTypes = new[] { TalentType.Mechanical, TalentType.Martial, TalentType.Psi, TalentType.Mage, TalentType.Mind };
                foreach (var type in allTypes)
                {
                    int rating = Random.Range(4, 7);  // B/A/S
                    talents.Add(new TalentInfo
                    {
                        type = type,
                        rating = rating,
                        specificPower = SpecificPowers[(int)type][Random.Range(0, SpecificPowers[(int)type].Length)]
                    });
                }
                return talents;
            }

            int count = Random.Range(1, 4);  // 1-3个

            var allTypesList = new List<TalentType> { TalentType.Mechanical, TalentType.Martial, TalentType.Psi, TalentType.Mage, TalentType.Mind };
            for (int i = 0; i < count; i++)
            {
                if (allTypesList.Count == 0) break;
                int idx = Random.Range(0, allTypesList.Count);
                var type = allTypesList[idx];
                allTypesList.RemoveAt(idx);

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

        public static void GrantTalents(Actor a)
        {
            if (a == null) return;
            if (_talents.ContainsKey(a.id)) return;  // 已有天赋
            var talents = GenerateTalents();
            _talents[a.id] = talents;
            if (talents.Count >= 5)
            {
                _fiveSystemGenius.Add(a.id);
                Debug.Log($"[超神机械师] ★五系天才诞生：{a.name}（同时拥有五系天赋，跨系兼修不惩罚）");
            }
        }

        public static List<TalentInfo> GetTalents(Actor a)
        {
            if (a == null) return new List<TalentInfo>();
            if (_talents.TryGetValue(a.id, out var list)) return list;
            return new List<TalentInfo>();
        }

        public static bool HasTalent(Actor a)
        {
            if (a == null) return false;
            return _talents.ContainsKey(a.id) && _talents[a.id].Count > 0;
        }

        public static int GetTalentRating(Actor a, TalentType type)
        {
            var talents = GetTalents(a);
            foreach (var t in talents)
            {
                if (t.type == type) return t.rating;
            }
            return -1;
        }

        public static float GetTrainingSpeed(Actor a, TalentType type)
        {
            int rating = GetTalentRating(a, type);
            if (rating < 0) return 0.5f;  // 无天赋，跨系修炼慢
            return 1f + rating * 0.15f;   // F=1.0x, E=1.15x, ... S=1.9x
        }

        public static int GetMaxRank(Actor a)
        {
            var talents = GetTalents(a);
            int maxRating = -1;
            foreach (var t in talents)
            {
                if (t.rating > maxRating) maxRating = t.rating;
            }
            if (maxRating < 0) return 1;  // 无天赋，E阶上限
            return Mathf.Min(maxRating + 1, 12);  // 最高SS阶，X阶需要特殊条件
        }

        public static void CleanupDead(List<long> aliveIds)
        {
            var toRemove = new List<long>();
            foreach (var id in _talents.Keys)
            {
                if (!aliveIds.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove) _talents.Remove(id);
        }

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
