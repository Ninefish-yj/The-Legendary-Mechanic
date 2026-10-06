using NeoModLoader.services;
using UnityEngine;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 阶位专长系统（对照原著）：
    /// - 原著：进阶超A级时从7个选项中选择两项（威能加成必选，原著ch~）
    /// - 7个选项：物理抗性/心灵抗性/威能加成/生命值/爆发力/气力值/状态增幅
    /// - 概念永生：X阶位专长（原著1402-1403：资讯唯一，死亡后自发信息态扰动重生）
    /// - 模组实现：AI单位随机选择两项（威能加成必选），玩家单位同样随机
    /// </summary>
    public static class SuperMechRankTrait
    {
        // X阶位专长：资讯唯一·概念永生
        public const string ConceptImmortal = "sm_rs_conceptimmortal";

        // 超A级阶位专长（7个选项，原著明确）
        public const string PhysicalResist = "sm_rs_physical_resist";   // 物理抗性
        public const string MentalResist   = "sm_rs_mental_resist";     // 心灵抗性
        public const string BoundlessPower = "sm_rs_boundless_power";   // 无上威能（威能加成，必选）
        public const string EternalBody    = "sm_rs_eternal_body";      // 永恒之躯（生命值）
        public const string ExplosivePower = "sm_rs_explosive_power";   // 爆发力
        public const string CellReactor    = "sm_rs_cell_reactor";      // 细胞反应炉（气力值）
        public const string StatusBoost    = "sm_rs_status_boost";      // 状态增幅

        // 所有超A阶位专长列表
        private static readonly string[] AllRankSpecs = {
            PhysicalResist, MentalResist, BoundlessPower,
            EternalBody, ExplosivePower, CellReactor, StatusBoost
        };

        public static void Register()
        {
            // 超A级阶位专长（7个选项）
            AddRankSpec(PhysicalResist, "sm_rankspecialty_260", "sm_rankspecialty_261", armor: 20f);
            AddRankSpec(MentalResist,   "sm_rankspecialty_262", "sm_rankspecialty_263", intel: 10);
            AddRankSpec(BoundlessPower, "sm_rankspecialty_250", "sm_rankspecialty_251", dmg: 0.25f);
            AddRankSpec(EternalBody,    "sm_rankspecialty_264", "sm_rankspecialty_265", hp: 0.40f, stamina: 30f);
            AddRankSpec(ExplosivePower, "sm_rankspecialty_266", "sm_rankspecialty_267", dmg: 0.15f, crit: 0.10f);
            AddRankSpec(CellReactor,    "sm_rankspecialty_252", "sm_rankspecialty_253", stamina: 80f);
            AddRankSpec(StatusBoost,    "sm_rankspecialty_268", "sm_rankspecialty_269", exp: 0.20f);

            // X阶位专长：概念永生
            AddRankSpec(ConceptImmortal, "sm_rankspecialty_240", "sm_rankspecialty_241", hp: 0.30f, intel: 15);
        }

        private static void AddRankSpec(string id, string name, string desc,
            float dmg = 0f, float hp = 0f, int intel = 0, float stamina = 0f,
            float armor = 0f, float lifespan = 0f, float exp = 0f, float crit = 0f)
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
            if (exp > 0) t.base_stats["experience"] = 1f + exp;
            if (crit > 0) t.base_stats["critical_chance"] = crit;
            AssetManager.traits.add(t);
        }

        /// <summary>
        /// 进阶超A时选择两项阶位专长（原著：威能加成必选，第二项随机）
        /// </summary>
        public static void OnRankUp(Actor a, int newRankIndex)
        {
            if (a == null) return;
            // S阶（超A级）选择两项阶位专长
            if (newRankIndex >= 10 && !a.data.GetBool("sm_rank_spec_granted", false))
            {
                // 威能加成（无上威能）必选
                if (!a.hasTrait(BoundlessPower)) a.addTrait(BoundlessPower);
                // 第二项从剩余6个中随机选
                var candidates = new List<string>(AllRankSpecs);
                candidates.Remove(BoundlessPower);
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                string second = candidates[idx];
                if (!a.hasTrait(second)) a.addTrait(second);
                a.data.SetBool("sm_rank_spec_granted", true);
                Debug.Log($"[超神机械师] {a.data.name} 进阶超A，选择阶位专长：无上威能 + {LocalizedTextManager.getText("trait_" + second)}");
            }
            // X阶获得概念永生
            if (newRankIndex >= 13 && !a.hasTrait(ConceptImmortal))
                a.addTrait(ConceptImmortal);
            // X阶按专精解锁对应超神级技能
            if (newRankIndex >= 13 && a.hasTrait(SuperMechTraits.ClassMech))
            {
                string spec = SuperMechSpecialty.GetSpecialty(a);
                if (spec == SuperMechSpecialty.SpecMechArmed)
                    SuperMechSkills.LearnSkill(a, "sm_skill_mech_armed_god");
                else if (spec == SuperMechSpecialty.SpecMechEnergy)
                    SuperMechSkills.LearnSkill(a, "sm_skill_mech_energy_god");
                else if (spec == SuperMechSpecialty.SpecMechVirtual)
                    SuperMechSkills.LearnSkill(a, "sm_skill_mech_virtual_god");
                else if (spec == SuperMechSpecialty.SpecGunEagle)
                    SuperMechSkills.LearnSkill(a, "sm_skill_gun_eagle_god");
                else if (spec == SuperMechSpecialty.SpecGunFire)
                    SuperMechSkills.LearnSkill(a, "sm_skill_gun_fire_god");
                else if (spec == SuperMechSpecialty.SpecGunDancer)
                    SuperMechSkills.LearnSkill(a, "sm_skill_gun_dancer_god");
                else if (spec == SuperMechSpecialty.SpecMartialWeapon)
                    SuperMechSkills.LearnSkill(a, "sm_skill_martial_weapon_god");
                else if (spec == SuperMechSpecialty.SpecMartialFight)
                    SuperMechSkills.LearnSkill(a, "sm_skill_martial_fight_god");
                else if (spec == SuperMechSpecialty.SpecMartialHeavy)
                    SuperMechSkills.LearnSkill(a, "sm_skill_martial_heavy_god");
            }
        }
    }
}
