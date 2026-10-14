using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业专精系统（对照原著）
    /// 原著：机械师有武装/能量/虚拟三分支（知识树分类，按学习倾向性自动判定主要分支）
    /// 模组扩展：枪炮师/械武者也各有三专精，按属性倾向性自动判定
    /// 专精在职业阶段达到第7阶段（战争堡垒/机甲操控师）时自动判定
    /// 存储方式：Dictionary（非特质），属性加成通过Actor.updateStats Postfix应用
    /// </summary>
    public static class SuperMechSpecialty
    {
        // === 机械师三专精（原著明确：武装/能量/虚拟）===
        public const string SpecMechArmed = "sm_spec_mech_armed";       // 武装分支
        public const string SpecMechEnergy = "sm_spec_mech_energy";     // 能量分支
        public const string SpecMechVirtual = "sm_spec_mech_virtual";   // 虚拟分支

        // === 枪炮师三专精（模组扩展，基于原著"距离决定实力"推演）===
        public const string SpecGunEagle = "sm_spec_gun_eagle";         // 鹰眼射手（超远程狙击）
        public const string SpecGunFire = "sm_spec_gun_fire";           // 火力手（范围轰炸）
        public const string SpecGunDancer = "sm_spec_gun_dancer";       // 枪斗士（中近距离机动）

        // === 械武者三专精（模组扩展，基于原著"武技+机械"推演）===
        public const string SpecMartialWeapon = "sm_spec_mech_martial_weapon";   // 武器大师（多武器切换）
        public const string SpecMartialFight = "sm_spec_mech_martial_fight";     // 格斗技师（近身搏击）
        public const string SpecMartialHeavy = "sm_spec_mech_martial_heavy";     // 重装斗士（重甲防御）

        public struct SpecialtyDef
        {
            public string id;
            public string name;
            public string desc;
            public string profession;      // 所属职业方向：mech/gun/martial
            public string classTrait;     // 所属体系特质
            public System.Action<BaseStats> applyBonus;
        }

        public static readonly List<SpecialtyDef> AllSpecialties = new List<SpecialtyDef>();

        // 单位专精存储（actor id -> spec id）
        private static readonly Dictionary<long, string> _unitSpecialty = new Dictionary<long, string>();

        public static void Register()
        {
            // === 机械师三专精（原著：武装/能量/虚拟）===
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMechArmed, name = "sm_spec_mech_armed_name", desc = "sm_spec_mech_armed_desc",
                profession = "mech", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.20f; s["attack_speed"] = s["attack_speed"] + 0.10f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMechEnergy, name = "sm_spec_mech_energy_name", desc = "sm_spec_mech_energy_desc",
                profession = "mech", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["stamina"] = s["stamina"] + 50f; s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.10f; s["range"] = s["range"] + 3f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMechVirtual, name = "sm_spec_mech_virtual_name", desc = "sm_spec_mech_virtual_desc",
                profession = "mech", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["intelligence"] = s["intelligence"] + 20; s["experience"] = (s["experience"] == 0f ? 1f : s["experience"]) * 1.20f; }
            });

            // === 枪炮师三专精（模组扩展）===
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecGunEagle, name = "sm_spec_gun_eagle_name", desc = "sm_spec_gun_eagle_desc",
                profession = "gun", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["range"] = s["range"] + 8f; s["critical_chance"] = s["critical_chance"] + 0.15f; s["accuracy"] = s["accuracy"] + 15f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecGunFire, name = "sm_spec_gun_fire_name", desc = "sm_spec_gun_fire_desc",
                profession = "gun", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.25f; s["attack_speed"] = s["attack_speed"] - 0.05f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecGunDancer, name = "sm_spec_gun_dancer_name", desc = "sm_spec_gun_dancer_desc",
                profession = "gun", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["attack_speed"] = s["attack_speed"] + 0.20f; s["multiplier_speed"] = (s["multiplier_speed"] == 0f ? 1f : s["multiplier_speed"]) * 1.10f; s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.10f; }
            });

            // === 械武者三专精（模组扩展）===
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMartialWeapon, name = "sm_spec_martial_weapon_name", desc = "sm_spec_martial_weapon_desc",
                profession = "martial", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["damage"] = s["damage"] + 15f; s["attack_speed"] = s["attack_speed"] + 0.15f; s["armor"] = s["armor"] + 5f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMartialFight, name = "sm_spec_martial_fight_name", desc = "sm_spec_martial_fight_desc",
                profession = "martial", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["damage"] = s["damage"] + 10f; s["multiplier_speed"] = (s["multiplier_speed"] == 0f ? 1f : s["multiplier_speed"]) * 1.15f; s["attack_speed"] = s["attack_speed"] + 0.10f; }
            });
            AllSpecialties.Add(new SpecialtyDef {
                id = SpecMartialHeavy, name = "sm_spec_martial_heavy_name", desc = "sm_spec_martial_heavy_desc",
                profession = "martial", classTrait = SuperMechTraits.ClassMech,
                applyBonus = s => { s["armor"] = s["armor"] + 15f; s["multiplier_health"] = (s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]) * 1.30f; s["damage"] = s["damage"] + 8f; }
            });

            // 不再注册为ActorTrait，专精是纯数据存储
            // 本地化文本仍需注册（用于UI显示）
            foreach (var spec in AllSpecialties)
            {
                LocalizedTextManager.add("trait_" + spec.id, LocalizedTextManager.getText(spec.name), pReplace: true);
                LocalizedTextManager.add("trait_" + spec.id + "_info", LocalizedTextManager.getText(spec.desc), pReplace: true);
            }
        }

        /// <summary>获取单位的专精</summary>
        public static string GetSpecialty(Actor a)
        {
            if (a == null) return null;
            string spec;
            _unitSpecialty.TryGetValue(a.id, out spec);
            return spec;
        }

        /// <summary>设置单位专精</summary>
        public static void SetSpecialty(Actor a, string specId)
        {
            if (a == null) return;
            if (string.IsNullOrEmpty(specId))
                _unitSpecialty.Remove(a.id);
            else
                _unitSpecialty[a.id] = specId;
        }

        /// <summary>获取某职业方向的专精列表</summary>
        public static List<SpecialtyDef> GetSpecialtiesForProfession(string profession)
        {
            var list = new List<SpecialtyDef>();
            foreach (var spec in AllSpecialties)
            {
                if (spec.profession == profession) list.Add(spec);
            }
            return list;
        }

        /// <summary>单位是否已有专精</summary>
        public static bool HasSpecialty(Actor a)
        {
            return GetSpecialty(a) != null;
        }

        /// <summary>应用专精属性加成（在Actor.updateStats Postfix中调用）</summary>
        public static void ApplySpecialtyBonus(Actor a, BaseStats stats)
        {
            if (a == null || stats == null) return;
            string specId = GetSpecialty(a);
            if (string.IsNullOrEmpty(specId)) return;
            foreach (var spec in AllSpecialties)
            {
                if (spec.id == specId && spec.applyBonus != null)
                {
                    spec.applyBonus(stats);
                    return;
                }
            }
        }

        /// <summary>
        /// 基于知识学习倾向性自动判定专精（对照原著：机械师分支按知识学习倾向性判定）
        /// 统计单位已学mech知识树中武装/能量/虚拟各分支的数量，哪个多就是哪个专精
        /// 机械师：武装→武装分支，能量→能量分支，虚拟→虚拟分支
        /// 枪炮师：武装→火力手，能量→鹰眼射手，虚拟→枪斗士
        /// 械武者：武装→重装斗士，能量→格斗技师，虚拟→武器大师
        /// </summary>
        public static void AutoAssignSpecialty(Actor a, string profession)
        {
            if (a == null || HasSpecialty(a)) return;
            var specs = GetSpecialtiesForProfession(profession);
            if (specs.Count == 0) return;

            // 统计mech知识树各分支学习量（原著：按知识学习倾向性判定主要分支）
            int[] counts = SuperMechKnowledge.GetBranchKnowledgeCounts(a, "mech");
            int armed = counts[0], energy = counts[1], control = counts[2];
            int total = armed + energy + control;

            string chosenId = null;

            // 如果有学习知识，按知识倾向性判定
            if (total > 0)
            {
                switch (profession)
                {
                    case "mech":
                        // 机械师：直接映射武装/能量/虚拟
                        if (armed >= energy && armed >= control) chosenId = SpecMechArmed;
                        else if (energy >= armed && energy >= control) chosenId = SpecMechEnergy;
                        else chosenId = SpecMechVirtual;
                        break;
                    case "gun":
                        // 枪炮师：武装→火力手（重武器），能量→鹰眼射手（能量狙击），虚拟→枪斗士（灵活操控）
                        if (armed >= energy && armed >= control) chosenId = SpecGunFire;
                        else if (energy >= armed && energy >= control) chosenId = SpecGunEagle;
                        else chosenId = SpecGunDancer;
                        break;
                    case "martial":
                        // 械武者：武装→重装斗士（重武器），能量→格斗技师（能量格斗），虚拟→武器大师（多武器操控）
                        if (armed >= energy && armed >= control) chosenId = SpecMartialHeavy;
                        else if (energy >= armed && energy >= control) chosenId = SpecMartialFight;
                        else chosenId = SpecMartialWeapon;
                        break;
                }
                Debug.Log($"[超神机械师] 专精自动判定(知识倾向): {a.name} ({profession}) 武装{armed}/能量{energy}/虚拟{control} → {GetSpecialtyNameById(chosenId)}");
            }
            else
            {
                // 没有学习知识时，按属性倾向性作为fallback
                float intel = a.stats["intelligence"];
                float str = a.stats["strength"];
                float agi = a.stats["agility"];
                float end = a.stats["endurance"];

                switch (profession)
                {
                    case "mech":
                        if (intel >= str && intel >= end) chosenId = SpecMechVirtual;
                        else if (str >= intel && str >= end) chosenId = SpecMechArmed;
                        else chosenId = SpecMechEnergy;
                        break;
                    case "gun":
                        if (agi >= str && agi >= end) chosenId = SpecGunEagle;
                        else if (str >= agi && str >= end) chosenId = SpecGunFire;
                        else chosenId = SpecGunDancer;
                        break;
                    case "martial":
                        if (agi >= str && agi >= end) chosenId = SpecMartialWeapon;
                        else if (str >= agi && str >= end) chosenId = SpecMartialFight;
                        else chosenId = SpecMartialHeavy;
                        break;
                    default:
                        chosenId = specs[Random.Range(0, specs.Count)].id;
                        break;
                }
                Debug.Log($"[超神机械师] 专精自动判定(属性倾向): {a.name} ({profession}) 智{intel:F0}/力{str:F0}/敏{agi:F0}/耐{end:F0} → {GetSpecialtyNameById(chosenId)}");
            }

            if (chosenId != null)
            {
                SetSpecialty(a, chosenId);
            }
        }

        /// <summary>根据专精ID获取名称</summary>
        private static string GetSpecialtyNameById(string specId)
        {
            if (specId == null) return "无";
            foreach (var spec in AllSpecialties)
            {
                if (spec.id == specId) return LocalizedTextManager.getText(spec.name);
            }
            return specId;
        }

        /// <summary>获取专精名称</summary>
        public static string GetSpecialtyName(Actor a)
        {
            var specId = GetSpecialty(a);
            if (specId == null) return LocalizedTextManager.getText("sm_spec_none");
            foreach (var spec in AllSpecialties)
            {
                if (spec.id == specId) return LocalizedTextManager.getText(spec.name);
            }
            return "";
        }

        public static void Clear() { _unitSpecialty.Clear(); }

        public static void Clear(Actor a)
        {
            if (a != null) _unitSpecialty.Remove(a.id);
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_unitSpecialty, alive);
        }
    }
}
