using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>阶位专长系统：晋升阶位(OnRankUp)时获得的加成专长；区别于 Specialty=通用专长</summary>
    public static class SuperMechRankSpecialty
    {
        public const string TranscendentPower = "sm_rs_transcendent";
        public const string SupremePower     = "sm_rs_supreme";
        public const string EternalBody      = "sm_rs_eternal";
        public const string CellReactor      = "sm_rs_cellreactor";
        public const string DivineMajesty    = "sm_rs_divinemajesty";
        public const string CosmicBody       = "sm_rs_cosmicbody";
        public const string QiFoundation     = "sm_rs_qifoundation";
        public const string ConceptImmortal  = "sm_rs_conceptimmortal";
        public const string MechGodVirtual   = "sm_rs_mechgod_virtual";
        public const string MechGodArmed     = "sm_rs_mechgod_armed";
        public const string MechGodEnergy    = "sm_rs_mechgod_energy";
        public const string LifeVirtual      = "sm_rs_lifevirtual";
        public const string LifeMech         = "sm_rs_lifemech";
        public const string BeyondArtifact   = "sm_rs_beyondartifact";
        public const string SpeciesDivine    = "sm_rs_speciesdivine";
        public const string DivineGene       = "sm_rs_divinegene";
        public const string BornElite        = "sm_rs_bornelite";

        public static void Register()
        {
            AddRankSpec(TranscendentPower, "sm_rankspecialty_226", "sm_rankspecialty_227",
                dmg: 0.15f, hp: 0.15f, intel: 5);
            AddRankSpec(SupremePower, "sm_rankspecialty_228", "sm_rankspecialty_229",
                dmg: 0.30f, intel: 10);
            AddRankSpec(EternalBody, "sm_rankspecialty_230", "sm_rankspecialty_231",
                hp: 0.40f, stamina: 20f);
            AddRankSpec(CellReactor, "sm_rankspecialty_232", "sm_rankspecialty_233",
                stamina: 30f, hp: 0.10f);
            AddRankSpec(DivineMajesty, "sm_rankspecialty_234", "sm_rankspecialty_235",
                dmg: 0.50f, intel: 20);
            AddRankSpec(CosmicBody, "sm_rankspecialty_236", "sm_rankspecialty_237",
                hp: 0.60f, stamina: 50f, lifespan: 99999f);
            AddRankSpec(QiFoundation, "sm_rankspecialty_238", "sm_rankspecialty_239",
                dmg: 0.50f, stamina: 100f, intel: 30);
            AddRankSpec(ConceptImmortal, "sm_rankspecialty_240", "sm_rankspecialty_241",
                hp: 0.30f, intel: 15);
            AddRankSpec(MechGodVirtual, "sm_rankspecialty_242", "sm_rankspecialty_243",
                dmg: 0.80f, hp: 0.50f, intel: 50);
            AddRankSpec(MechGodArmed, "sm_rankspecialty_244", "sm_rankspecialty_245",
                dmg: 1.00f, hp: 0.40f, armor: 15f);
            AddRankSpec(MechGodEnergy, "sm_rankspecialty_246", "sm_rankspecialty_247",
                dmg: 0.70f, hp: 0.60f, armor: 20f);
            AddRankSpec(LifeVirtual, "sm_rankspecialty_250", "sm_rankspecialty_251",
                hp: 0.20f, intel: 10);
            AddRankSpec(LifeMech, "sm_rankspecialty_252", "sm_rankspecialty_253",
                dmg: 0.20f, hp: 0.20f, intel: 10);
            AddRankSpec(BeyondArtifact, "sm_rankspecialty_254", "sm_rankspecialty_255",
                dmg: 0.25f, armor: 10f);
            AddRankSpec(SpeciesDivine, "sm_rankspecialty_256", "sm_rankspecialty_257",
                dmg: 0.15f, hp: 0.15f, intel: 10);
            AddRankSpec(DivineGene, "sm_rankspecialty_258", "sm_rankspecialty_259",
                dmg: 0.10f, intel: 5);
            AddRankSpec(BornElite, "sm_rankspecialty_260", "sm_rankspecialty_261",
                dmg: 0.10f, hp: 0.10f, intel: 10, exp: 1.5f);

        }

        private static void AddRankSpec(string id, string name, string desc,
            float dmg = 0f, float hp = 0f, int intel = 0, float stamina = 0f,
            float armor = 0f, float lifespan = 0f, float exp = 0f)
        {
            LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(name), pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", LocalizedTextManager.getText(desc), pReplace: true);
            var t = new ActorTrait
            {
                id = id,
                path_icon = "ui/Icons/actor_traits/iconGiant",
                group_id = "sm_rank_specialty",
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            if (dmg > 0) t.base_stats["multiplier_damage"] = 1f + dmg;
            if (hp > 0) t.base_stats["multiplier_health"] = 1f + hp;
            if (intel > 0) t.base_stats["intelligence"] = intel;
            if (stamina > 0) t.base_stats["stamina"] = stamina;
            if (armor > 0) t.base_stats["armor"] = armor;
            if (lifespan > 0) t.base_stats["lifespan"] = lifespan;
            if (exp > 0) t.base_stats["experience"] = exp;
            AssetManager.traits.add(t);
        }

        public static void OnRankUp(Actor a, int newRankIndex)
        {
            if (a == null) return;
            if (newRankIndex >= 4 && !a.hasTrait(TranscendentPower))
                a.addTrait(TranscendentPower);
            if (newRankIndex >= 8)
            {
                if (!a.hasTrait(SupremePower)) a.addTrait(SupremePower);
                if (!a.hasTrait(EternalBody)) a.addTrait(EternalBody);
            }
            if (newRankIndex >= 10 && !a.hasTrait(CellReactor))
                a.addTrait(CellReactor);
            if (newRankIndex >= 13)
            {
                string[] divineSpecs = {
                    DivineMajesty, CosmicBody, QiFoundation, ConceptImmortal,
                    LifeVirtual, LifeMech,
                    BeyondArtifact, SpeciesDivine, DivineGene, BornElite
                };
                foreach (string spec in divineSpecs)
                {
                    if (!a.hasTrait(spec)) a.addTrait(spec);
                }
                string branch = SuperMechBranch.GetBranchTrait(a);
                string finalForm = MechGodVirtual;
                if (branch == SuperMechBranch.BranchGunner) finalForm = MechGodArmed;
                else if (branch == SuperMechBranch.BranchMartial) finalForm = MechGodEnergy;
                if (!a.hasTrait(finalForm)) a.addTrait(finalForm);
                Debug.Log($"[超神机械师] {a.name} 达到X阶（超神级），最终形态：{finalForm}");
            }
        }
    }
}
