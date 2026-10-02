using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechMechFusion
    {
        private static readonly Dictionary<long, int> _fusionLevel = new Dictionary<long, int>();

        private static readonly Dictionary<long, string> _fusedEquip = new Dictionary<long, string>();

        public const int MinStageForFusion = 3;

        public const int MinQualityForFusion = 2;

        public static bool TryFuse(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechBranch.GetClass(a).Contains("sm_mechfusion_884")) return false;

            int stage = SuperMechStage.GetStage(a);
            if (stage < MinStageForFusion) return false;

            int eqIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (eqIdx < MinQualityForFusion) return false;

            if (_fusionLevel.ContainsKey(a.id) && _fusionLevel[a.id] > 0) return false;

            int level = Mathf.Clamp(eqIdx - 1, 1, 5);
            _fusionLevel[a.id] = level;
            _fusedEquip[a.id] = SuperMechRelic.Equipments[eqIdx].id;

            return true;
        }

        public static bool Unfuse(Actor a)
        {
            if (a == null) return false;
            if (!_fusionLevel.ContainsKey(a.id) || _fusionLevel[a.id] == 0) return false;

            _fusionLevel.Remove(a.id);
            _fusedEquip.Remove(a.id);
            return true;
        }

        public static void TickFusion()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!_fusionLevel.TryGetValue(a.id, out int level) || level <= 0) continue;

                float qiCost = level * 2f;
                SuperMechQi.SpendQi(a, qiCost);

                if (SuperMechQi.GetQi(a) <= 0)
                {
                    Unfuse(a);
                }
            }
        }

        public static float GetFusionMultiplier(Actor a)
        {
            if (a == null || !_fusionLevel.TryGetValue(a.id, out int level) || level <= 0) return 1f;
            return 1f + level * 0.2f;
        }

        public static bool IsFused(Actor a)
        {
            return a != null && _fusionLevel.TryGetValue(a.id, out int level) && level > 0;
        }

        public static int GetFusionLevel(Actor a)
        {
            if (a != null && _fusionLevel.TryGetValue(a.id, out int level)) return level;
            return 0;
        }

        public static string GetFusedEquipId(Actor a)
        {
            if (a != null && _fusedEquip.TryGetValue(a.id, out string id)) return id;
            return null;
        }

        public static void RestoreFusion(Actor a, int level, string equipId)
        {
            if (a == null || level <= 0) return;
            _fusionLevel[a.id] = level;
            if (!string.IsNullOrEmpty(equipId)) _fusedEquip[a.id] = equipId;
        }

        public static bool IsEquipProtected(Actor a)
        {
            return IsFused(a);
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_fusionLevel, alive);
            removed += SuperMechCleanup.CleanDict(_fusedEquip, alive);
            return removed;
        }

        public static void Clear() { _fusionLevel.Clear(); _fusedEquip.Clear(); }
    }
}
