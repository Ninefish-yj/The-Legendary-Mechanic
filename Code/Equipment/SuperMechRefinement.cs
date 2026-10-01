using System.Collections.Generic;
using NeoModLoader.services;
using NeoModLoader.api;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechRefinement
    {
        public const string RefinementTrait = "sm_refinement";
        public const string EmRefinementTrait = "sm_em_refinement";
        public const string PsiResonance  = "sm_pcult_resonance";
        public const string ManaMeditation = "sm_pcult_meditation";
        public const string MindTrain    = "sm_pcult_mind_train";

        public static readonly Dictionary<long, int> _refineCount = new Dictionary<long, int>();
        public static readonly Dictionary<long, int> _emRefineCount = new Dictionary<long, int>();
        public static readonly Dictionary<long, float> _refineQiBonus = new Dictionary<long, float>();
        public static readonly Dictionary<long, float> _emRefineQiBonus = new Dictionary<long, float>();

        public const int MaxRefineCount = 80;
        public const int RefineXpCost = 800;
        public const int RefineStaminaCost = 500;
        public const float RefineBaseQi = 10f;
        public const int MaxEmRefineCount = 100;

        public static void Register()
        {
            LocalizedTextManager.add("trait_" + RefinementTrait, LocalizedTextManager.getText("sm_refinement_080"), pReplace: true);
            LocalizedTextManager.add("trait_" + RefinementTrait + "_info", LocalizedTextManager.getText("sm_refinement_081"), pReplace: true);
            var t = new ActorTrait
            {
                id = RefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);

            LocalizedTextManager.add("trait_" + EmRefinementTrait, LocalizedTextManager.getText("sm_refinement_082"), pReplace: true);
            LocalizedTextManager.add("trait_" + EmRefinementTrait + "_info", LocalizedTextManager.getText("sm_refinement_083"), pReplace: true);
            var t2 = new ActorTrait
            {
                id = EmRefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t2);

            AddVariantTrait(PsiResonance, "sm_refinement_085", "sm_refinement_086");
            AddVariantTrait(ManaMeditation, "sm_refinement_087", "sm_refinement_088");
            AddVariantTrait(MindTrain, "sm_refinement_089", "sm_refinement_090");

            Debug.Log("[超神机械师] 提炼法系统注册完成（气力提炼法全系通用+电磁因子提炼法机械专属+三系变种）");
        }

        private static void AddVariantTrait(string id, string name, string desc)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);
        }

        public static void TickCultivation()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                var s = SuperMechStats.Of(a);
                if (s == null) continue;

                if (a.hasTrait(PsiResonance) && a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
                if (a.hasTrait(ManaMeditation) && a.hasTrait(SuperMechTraits.ClassMage))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.6f;
                    float mana = s["mana"];
                    if (mana < 1000) s["mana"] = mana + 3f;
                }
                if (a.hasTrait(MindTrain) && a.hasTrait(SuperMechTraits.ClassMind))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
            }
        }

        private static float CalcPerfection(Actor a)
        {
            float mainStat = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMartial)) mainStat = (stats["warfare"] + stats["stamina"]) / 2f;
                else if (a.hasTrait(SuperMechTraits.ClassPsi)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMage)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMind)) mainStat = stats["intelligence"];
            }
            float perfection = 50f + mainStat * 2f;
            return Mathf.Clamp(perfection, 10f, 95f);
        }

        private static int CalcQiGain(float perfection)
        {
            if (perfection >= 80f) return 3;
            if (perfection >= 40f) return 2;
            return 1;
        }

        public static bool TryRefine(Actor a)
        {
            if (a == null || !a.hasTrait(RefinementTrait)) return false;
            long id = a.data.id;
            int count = GetRefineCount(a);
            if (count >= MaxRefineCount) return false;

            if (SuperMechAwakened.IsAwakened(a))
            {
                float xp = SuperMechAwakened.GetXp(a);
                if (xp < RefineXpCost) return false;
                SuperMechAwakened.AddXp(a, -RefineXpCost);
            }

            float perfection = CalcPerfection(a);
            int qiGain = CalcQiGain(perfection);

            _refineCount[id] = count + 1;
            if (!_refineQiBonus.ContainsKey(id)) _refineQiBonus[id] = RefineBaseQi;
            _refineQiBonus[id] += qiGain;

            SuperMechQi.AddQiMax(a, qiGain);
            SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 提炼法锻炼：完美度{perfection:F0}%，气力+{qiGain}（{count + 1}/{MaxRefineCount}）");

            return true;
        }

        public static bool TryEmRefine(Actor a)
        {
            if (a == null || !a.hasTrait(EmRefinementTrait)) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMech)) return false;
            long id = a.data.id;
            int count = GetEmRefineCount(a);
            if (count >= MaxEmRefineCount) return false;

            _emRefineCount[id] = count + 1;

            float intel = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null) intel = stats["intelligence"];
            float qiGain = 0.5f * (1f + intel * 0.02f);
            if (!_emRefineQiBonus.ContainsKey(id)) _emRefineQiBonus[id] = 0;
            _emRefineQiBonus[id] += qiGain;
            SuperMechQi.AddQiMax(a, qiGain);

            if (count + 1 >= MaxEmRefineCount && SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 电磁因子提炼法圆满！");

            return true;
        }

        public static void TickRefinement()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                bool hasRefine = a.hasTrait(RefinementTrait);
                bool hasEmRefine = a.hasTrait(EmRefinementTrait);
                if (!hasRefine && !hasEmRefine) continue;

                if (!SuperMechAwakened.IsAwakened(a))
                {
                    if (hasRefine && Random.value < 0.1f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.05f) TryEmRefine(a);
                }
                else
                {
                    if (hasRefine && Random.value < 0.02f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.01f) TryEmRefine(a);
                }
            }
        }

        public static int GetRefineCount(Actor a)
        {
            if (a == null) return 0;
            _refineCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        public static int GetEmRefineCount(Actor a)
        {
            if (a == null) return 0;
            _emRefineCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        public static float GetRefineQiBonus(Actor a)
        {
            if (a == null) return 0;
            _refineQiBonus.TryGetValue(a.data.id, out float v);
            return v;
        }

        public static string GetStatusText(Actor a)
        {
            if (a == null) return null;
            bool hasRefine = a.hasTrait(RefinementTrait);
            bool hasEm = a.hasTrait(EmRefinementTrait);
            if (!hasRefine && !hasEm) return null;

            string text = "";
            if (hasRefine)
            {
                int c = GetRefineCount(a);
                float bonus = GetRefineQiBonus(a);
                text += $"sm_refinement_091";
                if (c >= MaxRefineCount) text += "sm_refinement_092";
            }
            if (hasEm)
            {
                int c = GetEmRefineCount(a);
                if (text.Length > 0) text += " | ";
                text += $"sm_refinement_093";
                if (c >= MaxEmRefineCount) text += "sm_refinement_092";
            }
            return text;
        }

        public static void Clear() { _refineCount.Clear(); _emRefineCount.Clear(); _refineQiBonus.Clear(); _emRefineQiBonus.Clear(); }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_refineCount, alive);
            removed += SuperMechCleanup.CleanDict(_emRefineCount, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null)
            {
                _refineCount.Remove(a.data.id);
                _emRefineCount.Remove(a.data.id);
                _refineQiBonus.Remove(a.data.id);
                _emRefineQiBonus.Remove(a.data.id);
            }
        }
    }
}
