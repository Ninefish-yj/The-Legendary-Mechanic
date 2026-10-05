using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>阶位专长系统：晋升阶位(OnRankUp)时获得的加成专长；区别于 Specialty=通用专长</summary>
    public static class SuperMechRankSpecialty
    {
        // v0.76.72 更正：X阶位专长【资讯唯一·概念永生】（原著1402-1403）——达到X阶后信息态巨变，
        // 死亡后无需圣所复苏、自发信息态扰动重生；与"圣所复苏"（圣所备份复活）是两个不同机制，不可混用
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
            // 资讯唯一·概念永生：X阶位专长（原著：达到X阶获得，死亡自发信息态重生，不需要圣所复苏）
            if (newRankIndex >= 13 && !a.hasTrait(ConceptImmortal))
                a.addTrait(ConceptImmortal);
        }
    }
}
