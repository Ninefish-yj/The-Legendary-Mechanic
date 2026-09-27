using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechProfession
    {
        public enum ProfessionType
        {
            None,        // 未选定（野生超能者）
            Mechanical,  // 机械系
            Martial,     // 武道系
            Psi,         // 异能系
            Mage,        // 魔法系
            Mind         // 念力系
        }

        private static readonly Dictionary<long, ProfessionType> _profession = new Dictionary<long, ProfessionType>();

        private static readonly Dictionary<long, int> _switchCount = new Dictionary<long, int>();

        public static string GetProfessionName(ProfessionType type)
        {
            switch (type)
            {
                case ProfessionType.Mechanical: return LocalizedTextManager.getText("sm_profession_931");
                case ProfessionType.Martial: return LocalizedTextManager.getText("sm_profession_932");
                case ProfessionType.Psi: return LocalizedTextManager.getText("sm_profession_933");
                case ProfessionType.Mage: return LocalizedTextManager.getText("sm_profession_934");
                case ProfessionType.Mind: return LocalizedTextManager.getText("sm_profession_935");
                default: return LocalizedTextManager.getText("sm_profession_936");
            }
        }

        public static ProfessionType GetProfession(Actor a)
        {
            if (a == null) return ProfessionType.None;
            if (_profession.TryGetValue(a.id, out var p)) return p;
            return ProfessionType.None;
        }

        public static string GetClass(Actor a)
        {
            var p = GetProfession(a);
            if (p == ProfessionType.None) return null;
            return GetProfessionName(p);
        }

        public static bool HasProfession(Actor a)
        {
            return GetProfession(a) != ProfessionType.None;
        }

        public static bool IsWild(Actor a)
        {
            return SuperMechTalent.HasTalent(a) && !HasProfession(a);
        }

        public static void SetProfession(Actor a, ProfessionType type)
        {
            if (a == null || type == ProfessionType.None) return;

            if (HasProfession(a) && GetProfession(a) != type)
            {
                if (!_switchCount.ContainsKey(a.id)) _switchCount[a.id] = 0;
                _switchCount[a.id]++;
            }

            _profession[a.id] = type;

            SyncClassTrait(a, type);
        }

        private static void SyncClassTrait(Actor a, ProfessionType type)
        {
            a.removeTrait(SuperMechTraits.ClassMech);
            a.removeTrait(SuperMechTraits.ClassMartial);
            a.removeTrait(SuperMechTraits.ClassPsi);
            a.removeTrait(SuperMechTraits.ClassMage);
            a.removeTrait(SuperMechTraits.ClassMind);

            switch (type)
            {
                case ProfessionType.Mechanical:
                    a.addTrait(SuperMechTraits.ClassMech);
                    SuperMechStage.SetStage(a, 1);
                    break;
                case ProfessionType.Martial:
                    a.addTrait(SuperMechTraits.ClassMartial);
                    SuperMechStage.SetStage(a, 1);
                    break;
                case ProfessionType.Psi:
                    a.addTrait(SuperMechTraits.ClassPsi);
                    SuperMechStage.SetStage(a, 1);
                    break;
                case ProfessionType.Mage:
                    a.addTrait(SuperMechTraits.ClassMage);
                    SuperMechStage.SetStage(a, 1);
                    break;
                case ProfessionType.Mind:
                    a.addTrait(SuperMechTraits.ClassMind);
                    SuperMechStage.SetStage(a, 1);
                    break;
            }
        }

        public static int GetSwitchCount(Actor a)
        {
            if (a == null) return 0;
            if (_switchCount.TryGetValue(a.id, out int c)) return c;
            return 0;
        }

        public static float GetSwitchPenalty(Actor a)
        {
            int count = GetSwitchCount(a);
            return Mathf.Max(1f - count * 0.1f, 0.5f);
        }

        public static void CleanupDead(List<long> aliveIds)
        {
            var toRemove = new List<long>();
            foreach (var id in _profession.Keys)
            {
                if (!aliveIds.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove)
            {
                _profession.Remove(id);
                _switchCount.Remove(id);
            }
        }

        public static Dictionary<string, object> GetSaveData()
        {
            var data = new Dictionary<string, object>();
            var prof = new Dictionary<string, object>();
            var sw = new Dictionary<string, object>();
            foreach (var kv in _profession) prof[kv.Key.ToString()] = (int)kv.Value;
            foreach (var kv in _switchCount) sw[kv.Key.ToString()] = kv.Value;
            data["profession"] = prof;
            data["switchCount"] = sw;
            return data;
        }

        public static void LoadSaveData(Dictionary<string, object> data)
        {
            _profession.Clear();
            _switchCount.Clear();
            if (data == null) return;

            if (data.ContainsKey("profession"))
            {
                var dict = data["profession"] as Dictionary<string, object>;
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _profession[long.Parse(kv.Key)] = (ProfessionType)System.Convert.ToInt32(kv.Value);
                }
            }
            if (data.ContainsKey("switchCount"))
            {
                var dict = data["switchCount"] as Dictionary<string, object>;
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _switchCount[long.Parse(kv.Key)] = System.Convert.ToInt32(kv.Value);
                }
            }
        }
    }
}
