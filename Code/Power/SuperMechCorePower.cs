using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;
using System.Collections.Generic;

namespace SuperMech.Code
{
    public static class SuperMechCorePower
    {
        public static readonly string[] GeneChainNames = {
            "sm_corepower_653", "sm_corepower_654", "sm_corepower_655", "sm_corepower_656", "sm_corepower_657"
        };
        public static readonly string[] ManaTierNames = {
            "sm_corepower_658", "sm_corepower_659", "sm_corepower_660", "sm_corepower_661", "sm_corepower_662"
        };
        public static readonly string[] MindTierNames = {
            "sm_corepower_663", "sm_corepower_664", "sm_corepower_665", "sm_corepower_666", "sm_corepower_667"
        };
        public static readonly int[] StageProgressReq = { 0, 100, 300, 600, 1000 };

        public static readonly Dictionary<long, int> _geneStage = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _manaStage = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _mindStage = new Dictionary<long, int>();

        public static readonly Dictionary<long, int> _geneProgress = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _manaProgress = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _mindProgress = new Dictionary<long, int>();

        public static void Register()
        {
        }

        public static void TickCorePowers()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                bool inCombat = SuperMechQi.IsInCombat(a);

                if (a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    TickCultivation(a, _geneStage, _geneProgress, "sm_corepower_668", inCombat ? 3 : 1);
                }

                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    TickCultivation(a, _manaStage, _manaProgress, "sm_corepower_669", inCombat ? 3 : 1);
                }

                if (a.hasTrait(SuperMechTraits.ClassMind))
                {
                    TickCultivation(a, _mindStage, _mindProgress, "sm_corepower_670", inCombat ? 3 : 1);
                }
            }
        }

        public static readonly int[] StageQiLevelReq = { 0, 1, 5, 10, 15, 21 };

        private static void TickCultivation(Actor a, Dictionary<long, int> stageDict, Dictionary<long, int> progDict, string name, int gain)
        {
            long id = a.id;
            if (!stageDict.TryGetValue(id, out int stage)) stage = 1;
            if (stage >= 5) return;

            int nextStage = stage + 1;
            int qiLvReq = StageQiLevelReq[nextStage];
            float qi = SuperMechQi.GetQi(a);
            int qiLv = SuperMechQi.GetLevel(qi);
            if (qiLv < qiLvReq) return;

            if (!progDict.TryGetValue(id, out int prog)) prog = 0;
            float qiMul = 1f + qiLv * 0.05f;
            prog += (int)(gain * qiMul);
            progDict[id] = prog;

            int req = StageProgressReq[stage];
            if (prog >= req)
            {
                progDict[id] = 0;
                stageDict[id] = stage + 1;
                ApplyStageEffects(a);
            }
        }

        private static string GetStageName(string type, int stage)
        {
            if (type == "sm_corepower_668") return GeneChainNames[stage - 1];
            if (type == "sm_corepower_669") return ManaTierNames[stage - 1];
            if (type == "sm_corepower_670") return MindTierNames[stage - 1];
            return "";
        }

        private static void ApplyStageEffects(Actor a)
        {
            if (a == null) return;
            var stats = a.stats;
            if (stats == null) return;

            if (a.hasTrait(SuperMechTraits.ClassPsi) && _geneStage.TryGetValue(a.id, out int geneLv))
            {
                float dmgBonus = 1f + geneLv * 0.10f;
                float spdBonus = geneLv * 0.05f;
                stats["multiplier_damage"] = dmgBonus;
                stats["attack_speed"] = spdBonus;
            }

            if (a.hasTrait(SuperMechTraits.ClassMage) && _manaStage.TryGetValue(a.id, out int manaLv))
            {
                float dmgBonus = 1f + manaLv * 0.12f;
                float manaBonus = manaLv * 20f;
                stats["multiplier_damage"] = dmgBonus;
                stats["mana"] = manaBonus;
            }

            if (a.hasTrait(SuperMechTraits.ClassMind) && _mindStage.TryGetValue(a.id, out int mindLv))
            {
                float dmgBonus = 1f + mindLv * 0.10f;
                float intBonus = mindLv * 3f;
                stats["multiplier_damage"] = dmgBonus;
                stats["intelligence"] = intBonus;
            }
        }

        public static string GetGeneStageName(Actor a)
        {
            if (a == null) return LocalizedTextManager.getText("sm_corepower_671");
            if (_geneStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= GeneChainNames.Length)
                return GeneChainNames[lv - 1];
            return LocalizedTextManager.getText("sm_corepower_653");
        }

        public static string GetManaStageName(Actor a)
        {
            if (a == null) return LocalizedTextManager.getText("sm_corepower_672");
            if (_manaStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= ManaTierNames.Length)
                return ManaTierNames[lv - 1];
            return LocalizedTextManager.getText("sm_corepower_658");
        }

        public static string GetMindStageName(Actor a)
        {
            if (a == null) return LocalizedTextManager.getText("sm_corepower_673");
            if (_mindStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= MindTierNames.Length)
                return MindTierNames[lv - 1];
            return LocalizedTextManager.getText("sm_corepower_663");
        }

        public static int GetGeneStage(Actor a)
        {
            if (a == null) return 1;
            return _geneStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        public static int GetManaStage(Actor a)
        {
            if (a == null) return 1;
            return _manaStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        public static int GetMindStage(Actor a)
        {
            if (a == null) return 1;
            return _mindStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        public static float GetGeneProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetGeneStage(a);
            if (stage >= 5) return 100f;
            if (!_geneProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        public static float GetManaProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetManaStage(a);
            if (stage >= 5) return 100f;
            if (!_manaProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        public static float GetMindProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetMindStage(a);
            if (stage >= 5) return 100f;
            if (!_mindProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        public static void AdvanceStage(Actor a, int amount = 1)
        {
            if (a == null) return;
            string cls = SuperMechBranch.GetClass(a);
            if (cls == "sm_corepower_674")
            {
                int cur = GetGeneStage(a);
                _geneStage[a.id] = Mathf.Min(cur + amount, GeneChainNames.Length);
            }
            else if (cls == "sm_corepower_675")
            {
                int cur = GetManaStage(a);
                _manaStage[a.id] = Mathf.Min(cur + amount, ManaTierNames.Length);
            }
            else if (cls == "sm_corepower_676")
            {
                int cur = GetMindStage(a);
                _mindStage[a.id] = Mathf.Min(cur + amount, MindTierNames.Length);
            }
        }

        public static void Clear()
        {
            _geneStage.Clear();
            _manaStage.Clear();
            _mindStage.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_geneStage, alive);
            removed += SuperMechCleanup.CleanDict(_manaStage, alive);
            removed += SuperMechCleanup.CleanDict(_mindStage, alive);
            return removed;
        }
    }
}
