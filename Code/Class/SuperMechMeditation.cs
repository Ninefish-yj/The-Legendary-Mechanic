using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.20: 冥想属性修炼（原 SuperMechRefinement 中提炼法部分已并入 SuperMechQiRefine 气力修炼法，
    /// 本类仅承载冥想系属性修炼：念力/法师/心念师通过冥想提升智力与法力——贴原著冥想设定）
    /// </summary>
    public static class SuperMechMeditation
    {
        public const string PsiResonance = "sm_pcult_resonance";
        public const string ManaMeditation = "sm_pcult_meditation";
        public const string MindTrain = "sm_pcult_mind_train";

        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            try
            {
                var t1 = new ActorTrait
                {
                    id = PsiResonance, path_icon = "actor_traits/iconMind", group_id = "sm_cultivation",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                AssetManager.traits.add(t1);

                var t2 = new ActorTrait
                {
                    id = ManaMeditation, path_icon = "actor_traits/iconMana", group_id = "sm_cultivation",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                AssetManager.traits.add(t2);

                var t3 = new ActorTrait
                {
                    id = MindTrain, path_icon = "actor_traits/iconMind", group_id = "sm_cultivation",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                AssetManager.traits.add(t3);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 冥想trait注册失败: " + e.Message);
            }
        }

        /// <summary>冥想修炼tick：念力/法师/心念师持续提升智力与法力（属性修炼，不直接涨气力）</summary>
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
    }
}
