using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 阶位专长系统（原著 ch586/ch770/ch1017/ch1402）。
    /// 每个阶位晋升时获得对应的阶位专长，阶位越高专长越强。
    /// - 低阶位：【超凡之力】
    /// - 中阶位：【无上威能】、【永恒之躯】、【细胞反应炉】
    /// - X阶（超神级）：【浩荡神威】、【宇宙神躯】+ 超神专属能力
    /// </summary>
    public static class SuperMechRankSpecialty
    {
        // —— 各阶位专长 ID ——
        public const string TranscendentPower = "sm_rs_transcendent";    // 超凡之力（低阶位）
        public const string SupremePower     = "sm_rs_supreme";          // 无上威能（中阶位，异能强度）
        public const string EternalBody      = "sm_rs_eternal";          // 永恒之躯（中阶位，生命）
        public const string CellReactor      = "sm_rs_cellreactor";      // 细胞反应炉（气力值/续航）
        // —— X阶（超神级）阶位专长 ——
        public const string DivineMajesty    = "sm_rs_divinemajesty";    // 浩荡神威
        public const string CosmicBody       = "sm_rs_cosmicbody";       // 宇宙神躯
        // —— 超神级专属能力（机械系，ch1402）——
        public const string QiFoundation     = "sm_rs_qifoundation";     // 气力之基·万机之神
        public const string ConceptImmortal  = "sm_rs_conceptimmortal";  // 资讯唯一·概念永生
        // 三种超神机械师最终形态（百度百科：按分支不同）
        public const string MechGodVirtual   = "sm_rs_mechgod_virtual";  // 机械神灵·至高天尊（虚拟分支）
        public const string MechGodArmed     = "sm_rs_mechgod_armed";    // 机械神灵·宇宙帝皇（武装分支/枪炮师）
        public const string MechGodEnergy    = "sm_rs_mechgod_energy";   // 机械神灵·起源神君（能量分支/械武者）
        public const string VirtualCreation  = "sm_rs_virtualcreation";  // 虚拟创世（伪）
        public const string LifeVirtual      = "sm_rs_lifevirtual";      // 生命转变·虚拟
        public const string LifeMech         = "sm_rs_lifemech";         // 生命转变·万机
        public const string BeyondArtifact   = "sm_rs_beyondartifact";   // 超越神器
        // —— 物种神化天赋 ——
        public const string SpeciesDivine    = "sm_rs_speciesdivine";    // 物种神化
        public const string DivineGene       = "sm_rs_divinegene";       // 神力基因
        public const string BornElite        = "sm_rs_bornelite";        // 天生精英

        public static void Register()
        {
            // 低阶位专长
            AddRankSpec(TranscendentPower, "sm_rankspecialty_226", "sm_rankspecialty_227",
                dmg: 0.15f, hp: 0.15f, intel: 5);
            // 中阶位专长
            AddRankSpec(SupremePower, "sm_rankspecialty_228", "sm_rankspecialty_229",
                dmg: 0.30f, intel: 10);
            AddRankSpec(EternalBody, "sm_rankspecialty_230", "sm_rankspecialty_231",
                hp: 0.40f, stamina: 20f);
            AddRankSpec(CellReactor, "sm_rankspecialty_232", "sm_rankspecialty_233",
                stamina: 30f, hp: 0.10f);
            // X阶（超神级）阶位专长
            AddRankSpec(DivineMajesty, "sm_rankspecialty_234", "sm_rankspecialty_235",
                dmg: 0.50f, intel: 20);
            AddRankSpec(CosmicBody, "sm_rankspecialty_236", "sm_rankspecialty_237",
                hp: 0.60f, stamina: 50f, lifespan: 99999f);
            // 超神级专属能力
            AddRankSpec(QiFoundation, "sm_rankspecialty_238", "sm_rankspecialty_239",
                dmg: 0.50f, stamina: 100f, intel: 30);
            AddRankSpec(ConceptImmortal, "sm_rankspecialty_240", "sm_rankspecialty_241",
                hp: 0.30f, intel: 15);
            // 三种超神机械师最终形态（百度百科：转职超神机械师后，根据分支不同得到不同最终形态）
            AddRankSpec(MechGodVirtual, "sm_rankspecialty_242", "sm_rankspecialty_243",
                dmg: 0.80f, hp: 0.50f, intel: 50);
            AddRankSpec(MechGodArmed, "sm_rankspecialty_244", "sm_rankspecialty_245",
                dmg: 1.00f, hp: 0.40f, armor: 15f);
            AddRankSpec(MechGodEnergy, "sm_rankspecialty_246", "sm_rankspecialty_247",
                dmg: 0.70f, hp: 0.60f, armor: 20f);
            AddRankSpec(VirtualCreation, "sm_rankspecialty_248", "sm_rankspecialty_249",
                intel: 40, exp: 2.0f);
            AddRankSpec(LifeVirtual, "sm_rankspecialty_250", "sm_rankspecialty_251",
                hp: 0.20f, intel: 10);
            AddRankSpec(LifeMech, "sm_rankspecialty_252", "sm_rankspecialty_253",
                dmg: 0.20f, hp: 0.20f, intel: 10);
            AddRankSpec(BeyondArtifact, "sm_rankspecialty_254", "sm_rankspecialty_255",
                dmg: 0.25f, armor: 10f);
            // 物种神化天赋
            AddRankSpec(SpeciesDivine, "sm_rankspecialty_256", "sm_rankspecialty_257",
                dmg: 0.15f, hp: 0.15f, intel: 10);
            AddRankSpec(DivineGene, "sm_rankspecialty_258", "sm_rankspecialty_259",
                dmg: 0.10f, intel: 5);
            AddRankSpec(BornElite, "sm_rankspecialty_260", "sm_rankspecialty_261",
                dmg: 0.10f, hp: 0.10f, intel: 10, exp: 1.5f);

            Debug.Log("[超神机械师] 阶位专长注册完成：16个专长（含X阶超神级7个）");
        }

        private static void AddRankSpec(string id, string name, string desc,
            float dmg = 0f, float hp = 0f, int intel = 0, float stamina = 0f,
            float armor = 0f, float lifespan = 0f, float exp = 0f)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
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

        /// <summary>晋升时自动赋予对应阶位专长。</summary>
        public static void OnRankUp(Actor a, int newRankIndex)
        {
            if (a == null) return;
            // 低阶位（C阶以上给超凡之力）
            if (newRankIndex >= 4 && !a.hasTrait(TranscendentPower))
                a.addTrait(TranscendentPower);
            // 中阶位（A阶以上给无上威能+永恒之躯）
            if (newRankIndex >= 8)
            {
                if (!a.hasTrait(SupremePower)) a.addTrait(SupremePower);
                if (!a.hasTrait(EternalBody)) a.addTrait(EternalBody);
            }
            // S阶给细胞反应炉
            if (newRankIndex >= 10 && !a.hasTrait(CellReactor))
                a.addTrait(CellReactor);
            // X阶（超神级）：全部超神专长 + 按分支赋予最终形态
            if (newRankIndex >= 13)
            {
                string[] divineSpecs = {
                    DivineMajesty, CosmicBody, QiFoundation, ConceptImmortal,
                    VirtualCreation, LifeVirtual, LifeMech,
                    BeyondArtifact, SpeciesDivine, DivineGene, BornElite
                };
                foreach (string spec in divineSpecs)
                {
                    if (!a.hasTrait(spec)) a.addTrait(spec);
                }
                // 按分支赋予超神机械师最终形态（百度百科）
                string branch = SuperMechBranch.GetBranchTrait(a);
                string finalForm = MechGodVirtual; // 默认虚拟分支=至高天尊
                if (branch == SuperMechBranch.BranchGunner) finalForm = MechGodArmed;     // 枪炮师=宇宙帝皇
                else if (branch == SuperMechBranch.BranchMartial) finalForm = MechGodEnergy; // 械武者=起源神君
                if (!a.hasTrait(finalForm)) a.addTrait(finalForm);
                Debug.Log($"[超神机械师] {a.name} 达到X阶（超神级），最终形态：{finalForm}");
            }
        }
    }
}
