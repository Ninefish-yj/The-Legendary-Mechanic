using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>阶位专长系统：晋升阶位(OnRankUp)时获得的加成专长；区别于 Specialty=通用专长</summary>
    public static class SuperMechRankSpecialty
    {
        // v0.76.71 按原著修剪：原著没有"超A级进阶/超凡神力/机械神座"等 Power 概念——自研 Power 全部删除
        // 仅保留原著依据明确的机制：圣所复苏（原著："达到超A级层次的超能者可以通过圣所复活""超A级层次理解为复活许可证"）
        public const string ConceptImmortal  = "sm_rs_conceptimmortal";

        public static void Register()
        {
            AddRankSpec(ConceptImmortal, "sm_rankspecialty_240", "sm_rankspecialty_241",
                hp: 0.30f, intel: 15);
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
            // 圣所复苏：超A级（X阶）=圣所复活资格（原著："超A级层次理解为复活许可证"）
            if (newRankIndex >= 13 && !a.hasTrait(ConceptImmortal))
                a.addTrait(ConceptImmortal);
        }
    }
}
