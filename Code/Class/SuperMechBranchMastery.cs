using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 机械师专精系统：大型机械/微型机械/虚拟技术
    /// 专精由知识树学习倾向决定，影响高阶形态与战斗风格
    /// 职业=身份/阶位，专精=知识树+路线，两者叠加构成build
    /// </summary>
    public static class SuperMechBranchMastery
    {
        public const string SpecLarge = "sm_spec_large";     // 大型机械 - 武装分支 - 宇宙帝皇
        public const string SpecMicro = "sm_spec_micro";     // 微型机械 - 能量分支 - 起源神君
        public const string SpecVirtual = "sm_spec_virtual"; // 虚拟技术 - 操控分支 - 至高天尊

        public struct SpecDef
        {
            public string id;
            public string nameKey;
            public string descKey;
            public string knowledgeBranch; // 关联的知识树分支
            public string highFormName;    // 超神阶形态名
            public string throneFormName;  // 神座阶形态名
            public System.Action<BaseStats> applyBonus;
        }

        public static readonly List<SpecDef> AllSpecs = new List<SpecDef>();

        // 单位专精缓存（actor id -> spec id）
        private static readonly Dictionary<long, string> _unitSpec = new Dictionary<long, string>();

        public static void Register()
        {
            AllSpecs.Add(new SpecDef
            {
                id = SpecLarge,
                nameKey = "sm_spec_name_large",
                descKey = "sm_spec_desc_large",
                knowledgeBranch = "armed",
                highFormName = "sm_spec_form_emperor",
                throneFormName = "sm_spec_throne_emperor",
                applyBonus = s => {
                    s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.25f;
                    s["multiplier_health"] = (s["multiplier_health"] == 0f ? 1f : s["multiplier_health"]) * 1.2f;
                    s["armor"] = s["armor"] + 10f;
                }
            });

            AllSpecs.Add(new SpecDef
            {
                id = SpecMicro,
                nameKey = "sm_spec_name_micro",
                descKey = "sm_spec_desc_micro",
                knowledgeBranch = "energy",
                highFormName = "sm_spec_form_origin",
                throneFormName = "sm_spec_throne_origin",
                applyBonus = s => {
                    s["attack_speed"] = s["attack_speed"] + 0.3f;
                    s["critical_chance"] = s["critical_chance"] + 0.1f;
                    s["multiplier_damage"] = (s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"]) * 1.15f;
                }
            });

            AllSpecs.Add(new SpecDef
            {
                id = SpecVirtual,
                nameKey = "sm_spec_name_virtual",
                descKey = "sm_spec_desc_virtual",
                knowledgeBranch = "control",
                highFormName = "sm_spec_form_sovereign",
                throneFormName = "sm_spec_throne_sovereign",
                applyBonus = s => {
                    s["intelligence"] = s["intelligence"] + 20f;
                    s["experience"] = (s["experience"] == 0f ? 1f : s["experience"]) * 1.3f;
                    s["range"] = s["range"] + 3f;
                }
            });

            foreach (var spec in AllSpecs)
            {
                var bs = new BaseStats();
                spec.applyBonus(bs);
                var t = new ActorTrait
                {
                    id = spec.id,
                    path_icon = "ui/Icons/actor_traits/iconArcaneReflexes",
                    group_id = "sm_specialization",
                    needs_to_be_explored = false,
                    base_stats = bs
                };
                AssetManager.traits.add(t);
                LocalizedTextManager.add("trait_" + spec.id, LocalizedTextManager.getText(spec.nameKey), pReplace: true);
                LocalizedTextManager.add("trait_" + spec.id + "_info", LocalizedTextManager.getText(spec.descKey), pReplace: true);
            }
        }

        /// <summary>
        /// 根据知识树学习情况自动判定专精
        /// 学习某分支知识达到3个以上解锁对应专精
        /// </summary>
        public static string DetermineSpec(Actor a)
        {
            if (a == null) return null;
            if (!a.hasTrait(SuperMechBranch.BranchMech)) return null; // 只有机械师有专精

            int armedCount = 0, energyCount = 0, controlCount = 0;
            var knowledge = SuperMechKnowledge.GetLearnedKnowledge(a);
            foreach (var kid in knowledge)
            {
                string branch = SuperMechKnowledge.GetKnowledgeBranch(kid);
                if (branch == "armed") armedCount++;
                else if (branch == "energy") energyCount++;
                else if (branch == "control") controlCount++;
            }

            // 至少学3个同分支知识才解锁专精
            if (armedCount >= 3 && armedCount >= energyCount && armedCount >= controlCount)
                return SpecLarge;
            if (energyCount >= 3 && energyCount >= armedCount && energyCount >= controlCount)
                return SpecMicro;
            if (controlCount >= 3 && controlCount >= armedCount && controlCount >= energyCount)
                return SpecVirtual;

            return null;
        }

        /// <summary>
        /// 检查并更新单位专精（在知识学习后调用）
        /// </summary>
        public static void UpdateSpec(Actor a)
        {
            if (a == null) return;
            string newSpec = DetermineSpec(a);
            string oldSpec = GetSpec(a);

            if (newSpec != oldSpec)
            {
                if (oldSpec != null && a.hasTrait(oldSpec))
                    a.removeTrait(oldSpec);
                if (newSpec != null && !a.hasTrait(newSpec))
                {
                    a.addTrait(newSpec);
                    _unitSpec[a.data.id] = newSpec;
                    LogInfo($"[超神机械师] {a.data.name} 解锁专精：{LocalizedTextManager.getText(GetSpecDef(newSpec).nameKey)}");
                }
            }
        }

        public static string GetSpec(Actor a)
        {
            if (a == null) return null;
            foreach (var spec in AllSpecs)
            {
                if (a.hasTrait(spec.id)) return spec.id;
            }
            return null;
        }

        public static SpecDef GetSpecDef(string specId)
        {
            foreach (var spec in AllSpecs)
            {
                if (spec.id == specId) return spec;
            }
            return default(SpecDef);
        }

        public static string GetSpecName(Actor a)
        {
            string spec = GetSpec(a);
            if (spec == null) return LocalizedTextManager.getText("sm_spec_none");
            return LocalizedTextManager.getText(GetSpecDef(spec).nameKey);
        }

        /// <summary>
        /// 获取高阶形态名称（超神阶）
        /// </summary>
        public static string GetHighFormName(Actor a)
        {
            string spec = GetSpec(a);
            if (spec == null) return LocalizedTextManager.getText("sm_stage_029"); // 默认超神机械师
            return LocalizedTextManager.getText(GetSpecDef(spec).highFormName);
        }

        /// <summary>
        /// 获取神座阶形态名称
        /// </summary>
        public static string GetThroneFormName(Actor a)
        {
            string spec = GetSpec(a);
            if (spec == null) return LocalizedTextManager.getText("sm_stage_028"); // 默认神座机械师
            return LocalizedTextManager.getText(GetSpecDef(spec).throneFormName);
        }

        private static void LogInfo(string msg)
        {
            Debug.Log(msg);
        }

        public static void Clear() { _unitSpec.Clear(); }

        public static void Clear(Actor a)
        {
            if (a != null) _unitSpec.Remove(a.data.id);
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_unitSpec, alive);
        }
    }
}
