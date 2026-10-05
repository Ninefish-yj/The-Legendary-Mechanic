using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>阶位专长系统：晋升阶位(OnRankUp)时获得的加成专长；区别于 Specialty=通用专长</summary>
    public static class SuperMechRankSpecialty
    {
        // v0.76.69 收敛+70 改名：超A级进阶系统（原著依据=超A级=超能者巅峰/圣体级/圣所复苏资格/机械神明）——原17个自研Power收敛为6个
        public const string DivinePower      = "sm_rs5_power";       // 超凡神力（超凡之力+至高超能+神圣威严）
        public const string EternalBody      = "sm_rs5_body";        // 永恒之躯（永恒之体+细胞反应堆+宇宙之躯+生命转变）
        public const string QiFoundation     = "sm_rs_qifoundation"; // 气力根基（保留原 id）
        public const string ConceptImmortal  = "sm_rs_conceptimmortal"; // 概念不朽（独立复活机制系统依赖，保留原 id）
        public const string SacredBlood      = "sm_rs5_sacred";      // 神圣血脉（超越神器+种族神圣+神圣基因+天生精英）
        public const string MechGod          = "sm_rs5_mechgod";     // 机械神座（机械神虚拟/武装/能量三态合一=最终形态）

        public static void Register()
        {
            AddRankSpec(DivinePower,     "sm_rankspecialty_270", "sm_rankspecialty_271",
                dmg: 0.40f, hp: 0.15f, intel: 20);
            AddRankSpec(EternalBody,     "sm_rankspecialty_272", "sm_rankspecialty_273",
                hp: 0.60f, stamina: 50f, lifespan: 99999f);
            AddRankSpec(QiFoundation,    "sm_rankspecialty_238", "sm_rankspecialty_239",
                dmg: 0.50f, stamina: 100f, intel: 30);
            AddRankSpec(ConceptImmortal, "sm_rankspecialty_240", "sm_rankspecialty_241",
                hp: 0.30f, intel: 15);
            AddRankSpec(SacredBlood,     "sm_rankspecialty_274", "sm_rankspecialty_275",
                dmg: 0.30f, hp: 0.15f, armor: 10f, intel: 10);
            AddRankSpec(MechGod,         "sm_rankspecialty_276", "sm_rankspecialty_277",
                dmg: 0.60f, hp: 0.35f, armor: 15f, intel: 30);
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
            if (newRankIndex >= 4 && !a.hasTrait(DivinePower))
                a.addTrait(DivinePower);
            if (newRankIndex >= 8 && !a.hasTrait(EternalBody))
                a.addTrait(EternalBody);
            if (newRankIndex >= 13)
            {
                if (!a.hasTrait(QiFoundation)) a.addTrait(QiFoundation);
                if (!a.hasTrait(ConceptImmortal)) a.addTrait(ConceptImmortal);
                if (!a.hasTrait(SacredBlood)) a.addTrait(SacredBlood);
                if (!a.hasTrait(MechGod)) a.addTrait(MechGod);
                Debug.Log($"[超神机械师] {a.name} 达到X阶（超神级），获得最终形态：机械神座");
            }
        }
    }
}
