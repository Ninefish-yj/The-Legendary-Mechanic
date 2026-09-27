using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechAwakened
    {
        public const string AwakenedTrait = "sm_awakened";

        private static readonly Dictionary<long, int> _level = new Dictionary<long, int>();
        private static readonly Dictionary<long, float> _xp = new Dictionary<long, float>();

        public static readonly int[] StageLevelCaps = {
            20,   // 1.入门者
            40,   // 2.学徒
            15,   // 3.见习
            60,   // 4.磁环
            60,   // 5.数据
            60,   // 6.战争
            60,   // 7.虚拟
            60,   // 8.星海
            60,   // 9.真理
            60,   // 10.使徒
            60,   // 11.帝皇
            60,   // 12.主宰
            60,   // 13.神座
            999   // 14.超神（无上限）
        };

        public static readonly float[] StageBaseXp = {
            200f,       // 1.入门者（ch3原文）
            50000f,     // 2.学徒（ch50原文）
            200000f,    // 3.见习（推测，学徒2.5倍）
            800000f,    // 4.磁环（推测）
            2000000f,   // 5.数据（推测）
            5000000f,   // 6.战争（推测）
            10000000f,  // 7.虚拟（推测）
            20000000f,  // 8.星海（推测）
            40000000f,  // 9.真理（推测）
            80000000f,  // 10.使徒（推测）
            150000000f, // 11.帝皇（推测）
            300000000f, // 12.主宰（推测）
            500000000f, // 13.神座（推测）
            1000000000f // 14.超神（推测）
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
            t.base_stats["experience"] = 2.0f; // 降临者经验获取×2
            t.base_stats["intelligence"] = 5;
            AssetManager.traits.add(t);
            LocalizedTextManager.add("trait_" + AwakenedTrait, LocalizedTextManager.getText("sm_awakened_505"), pReplace: true);
            LocalizedTextManager.add("trait_" + AwakenedTrait + "_info", LocalizedTextManager.getText("sm_awakened_506"), pReplace: true);
            Debug.Log("[超神机械师] 降临者体系注册完成");
        }

        public static bool IsAwakened(Actor a)
        {
            return a != null && a.hasTrait(AwakenedTrait);
        }

        public static int GetLevel(Actor a)
        {
            if (a == null || !IsAwakened(a)) return 0;
            if (_level.TryGetValue(a.data.id, out int lv)) return lv;
            return 1; // 降临者初始1级
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
                SuperMechQi.AddQiMax(a, SuperMechStage.LevelQiBonus[idx]); // 升级提升气力上限
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a)); // 升级后气力充盈
                SuperMechPotential.AddPotential(a, LevelPotentialPoints[idx]);
                Debug.Log($"[超神机械师] {a.name} 升级到Lv{lv}（阶段{stage}，气力+{SuperMechStage.LevelQiBonus[idx]:F0}）");
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
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name} 转职条件未满足：{SuperMechAdvancementTask.GetReqText(a)}");
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
            Debug.Log($"[超神机械师] {a.name} 转职到阶段{newStage}：{SuperMechStage.GetStageName(a)}");
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
            Debug.Log($"[超神机械师] {a.name} 自动选择分支：{b.name}");
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
