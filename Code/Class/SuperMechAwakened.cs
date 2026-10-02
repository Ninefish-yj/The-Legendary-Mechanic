using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechAwakened
    {
        public const string AwakenedTrait = "sm_awakened";

        private static readonly Dictionary<long, int> _level = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _xp = new Dictionary<long, float>();

        public static readonly int[] StageLevelCaps = {
            20,
            40,
            15,
            60,
            60,
            60,
            60,
            60,
            60,
            60,
            60,
            60,
            60,
            999
        };

        public static readonly float[] StageBaseXp = {
            200f,
            50000f,
            200000f,
            800000f,
            2000000f,
            5000000f,
            10000000f,
            20000000f,
            40000000f,
            80000000f,
            150000000f,
            300000000f,
            500000000f,
            1000000000f
        };

        public static float GetXpNeeded(int level, int stage)
        {
            int idx = Mathf.Clamp(stage - 1, 0, StageBaseXp.Length - 1);
            float baseXp = StageBaseXp[idx];
            return Mathf.Round(baseXp * Mathf.Pow(1.1f, level - 1));
        }

        public static readonly int[] LevelStatPoints = { 2, 3, 3, 4, 4, 5, 5, 8, 10, 15, 15, 20, 20, 20 };
        public static readonly int[] LevelPotentialPoints = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        public static void Register()
        {
            var t = new ActorTrait
            {
                id = AwakenedTrait,
                path_icon = "ui/Icons/actor_traits/iconClone",
                group_id = "sm_awakened",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            t.base_stats["experience"] = 2.0f;
            t.base_stats["intelligence"] = 5;
            AssetManager.traits.add(t);
            LocalizedTextManager.add("trait_" + AwakenedTrait, LocalizedTextManager.getText("sm_awakened_505"), pReplace: true);
            LocalizedTextManager.add("trait_" + AwakenedTrait + "_info", LocalizedTextManager.getText("sm_awakened_506"), pReplace: true);
        }

        public static bool IsAwakened(Actor a)
        {
            return a != null && a.hasTrait(AwakenedTrait);
        }

        public static int GetLevel(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            if (_level.TryGetValue(a.data.id, out int lv)) return lv;
            return 1;
        }

        public static int GetTotalLevel(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0) return GetLevel(a);
            int total = 0;
            for (int i = 0; i < stage - 1 && i < StageLevelCaps.Length; i++)
            {
                total += StageLevelCaps[i];
            }
            total += GetLevel(a);
            return total;
        }

        public static float GetXp(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            if (_xp.TryGetValue(a.data.id, out float xp)) return xp;
            return 0;
        }

        public static void SetLevel(Actor a, int level)
        {
            if (a == null || !IsAwakened(a)) return;
            _level[a.data.id] = Mathf.Max(1, level);
        }

        public static void SetXp(Actor a, float xp)
        {
            if (a == null || !IsAwakened(a)) return;
            _xp[a.data.id] = Mathf.Max(0, xp);
        }

        public static bool SpendXp(Actor a, float amount)
        {
            if (a == null || !IsAwakened(a) || amount <= 0) return false;
            float cur = GetXp(a);
            if (cur < amount) return false;
            _xp[a.data.id] = cur - amount;
            return true;
        }

        public static void AddXp(Actor a, float amount)
        {
            if (a == null || !IsAwakened(a) || amount <= 0) return;
            float curXp = GetXp(a) + amount;
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0) stage = 1;
            int cap = StageLevelCaps[stage - 1];

            while (lv < cap)
            {
                float needed = GetXpNeeded(lv, stage);
                if (curXp < needed) break;
                curXp -= needed;
                lv++;
                int idx = Mathf.Clamp(stage - 1, 0, SuperMechStage.LevelQiBonus.Length - 1);
                SuperMechQi.AddQiMax(a, SuperMechStage.LevelQiBonus[idx]);
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
                SuperMechPotential.AddPotential(a, LevelPotentialPoints[idx]);
            }

            if (lv >= cap)
            {
                curXp = Mathf.Min(curXp, GetXpNeeded(lv, stage));
            }

            _level[a.data.id] = lv;
            _xp[a.data.id] = curXp;
        }

        public static bool CanAdvanceStage(Actor a)
        {
            if (!IsAwakened(a)) return false;
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0 || stage >= 14) return false;
            return lv >= StageLevelCaps[stage - 1];
        }

        public static bool TryAdvanceStage(Actor a)
        {
            if (!CanAdvanceStage(a)) return false;
            int stage = SuperMechStage.GetStage(a);
            if (!SuperMechAdvancementTask.CheckReq(a, stage))
            {
                return false;
            }
            SuperMechStage.Advance(a);
            _level[a.data.id] = 1;
            _xp[a.data.id] = 0;
            int newStage = SuperMechStage.GetStage(a);
            if (newStage == 2)
            {
                AutoSelectBranch(a);
            }
            return true;
        }

        public static string GetLevelText(Actor a)
        {
            if (!IsAwakened(a)) return "";
            int lv = GetLevel(a);
            int stage = SuperMechStage.GetStage(a);
            if (stage <= 0) return $"Lv{lv}";
            int cap = StageLevelCaps[stage - 1];
            float xp = GetXp(a);
            float needed = GetXpNeeded(lv, stage);
            return $"Lv{lv}/{cap}（{xp:F0}/{needed:F0}）";
        }

        public static void Clear() { _level.Clear(); _xp.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_level, alive);
            removed += SuperMechCleanup.CleanDict(_xp, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _level.Remove(a.data.id);
            _xp.Remove(a.data.id);
        }

        public static void TickAutoPlay()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!IsAwakened(a)) continue;


                int pot = SuperMechPotential.GetPotential(a);
                if (pot >= 2)
                {
                    AutoUnlockKnowledge(a);
                }

                int divPoints = SuperMechDivinity.GetPoints(a);
                if (divPoints >= SuperMechDivinity.PointsPerLayer)
                {
                    int prof = SuperMechDivinity.GetProfLayers(a);
                    int spec = SuperMechDivinity.GetSpeciesLayers(a);
                    if (prof <= spec && prof < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "profession");
                    else if (spec < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "species");
                    else if (prof < SuperMechDivinity.MaxLayers)
                        SuperMechDivinity.SpendPoints(a, "profession");
                }

                if (SuperMechTranscendence.CanAttempt(a) && SuperMechTranscendence.AllConditionsMet(a))
                {
                    if (Random.value < 0.3f)
                    {
                        SuperMechTranscendence.AttemptTranscend(a);
                    }
                }
            }
        }

        private static void AutoUnlockKnowledge(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            int unlocked = SuperMechPotential.GetUnlockedCount(a);
            string nodeId = $"{prefix}_{unlocked + 1}";
            int cost = 2;
            SuperMechPotential.UnlockNode(a, nodeId, cost);
        }

        private static void AutoSelectBranch(Actor a)
        {
            string cls = SuperMechBranch.GetClass(a);
            var branches = SuperMechBranch.GetBranchesForClass(
                cls == "sm_awakened_507" ? SuperMechTraits.ClassMech :
                cls == "sm_awakened_508" ? SuperMechTraits.ClassMartial :
                cls == "sm_awakened_509" ? SuperMechTraits.ClassPsi :
                cls == "sm_awakened_510" ? SuperMechTraits.ClassMage :
                SuperMechTraits.ClassMind);
            if (branches == null || branches.Count == 0) return;
            var b = branches[Random.Range(0, branches.Count)];
            a.addTrait(b.traitId);
            SuperMechSpecialty.GrantBranchSpecialty(a, b.traitId);
        }

        public static void TickXp()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !IsAwakened(a)) continue;
                float xpGain = 20f;
                if (a.data != null && a.data.health < a.getMaxHealth() * 0.95f)
                    xpGain += 80f;
                AddXp(a, xpGain);
            }
        }
    }
}
