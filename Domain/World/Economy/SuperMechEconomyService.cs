using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>伊纳尔消费服务系统（v0.182经济闭环）
    /// 提供复活/传送/洗点/建筑加速/科技加速等付费服务
    /// 原著依据：伊纳尔是星际通用货币，高级服务都需要消耗</summary>
    public static class SuperMechEconomyService
    {
        /// <summary>服务类型</summary>
        public enum ServiceType
        {
            Resurrect,      // 复活
            Teleport,       // 传送
            ResetRank,      // 重置阶位选择
            ResetProfession,// 重置职业
            BuildingBoost,  // 建筑加速
            TechBoost,      // 科技加速
            GeneRepair      // 基因修复（去除负面特质）
        }

        /// <summary>服务基础价格</summary>
        private static readonly Dictionary<ServiceType, int> _baseCost = new Dictionary<ServiceType, int>
        {
            { ServiceType.Resurrect, 500 },
            { ServiceType.Teleport, 100 },
            { ServiceType.ResetRank, 2000 },
            { ServiceType.ResetProfession, 1000 },
            { ServiceType.BuildingBoost, 200 },
            { ServiceType.TechBoost, 300 },
            { ServiceType.GeneRepair, 800 }
        };

        /// <summary>获取服务价格（可随次数递增，v0.185应用声望折扣）</summary>
        public static int GetCost(ServiceType type, Actor a = null)
        {
            int baseCost = _baseCost[type];
            // 阶位越高，服务越贵（高阶单位复活成本更高）
            if (a != null && (type == ServiceType.Resurrect || type == ServiceType.ResetRank))
            {
                int rankIdx = SuperMechActorContextRegistry.GetRank(a);
                baseCost = baseCost * (1 + rankIdx / 2);
            }
            // v0.185 声望折扣：取所有势力中最高声望对应的折扣
            if (a != null)
            {
                float bestMultiplier = 1.0f;
                var factions = SuperMechFaction.GetAllFactions();
                foreach (var f in factions)
                {
                    float mult = SuperMechQuestSystem.GetReputationPriceMultiplier(a, f.id);
                    if (mult < bestMultiplier) bestMultiplier = mult;
                }
                baseCost = Mathf.RoundToInt(baseCost * bestMultiplier);
            }
            return baseCost;
        }

        /// <summary>获取服务名称（本地化key）</summary>
        public static string GetNameKey(ServiceType type)
        {
            return type switch
            {
                ServiceType.Resurrect => "sm_econ_resurrect",
                ServiceType.Teleport => "sm_econ_teleport",
                ServiceType.ResetRank => "sm_econ_reset_rank",
                ServiceType.ResetProfession => "sm_econ_reset_prof",
                ServiceType.BuildingBoost => "sm_econ_building_boost",
                ServiceType.TechBoost => "sm_econ_tech_boost",
                ServiceType.GeneRepair => "sm_econ_gene_repair",
                _ => "?"
            };
        }

        /// <summary>获取服务描述（本地化key）</summary>
        public static string GetDescKey(ServiceType type)
        {
            return type switch
            {
                ServiceType.Resurrect => "sm_econ_resurrect_desc",
                ServiceType.Teleport => "sm_econ_teleport_desc",
                ServiceType.ResetRank => "sm_econ_reset_rank_desc",
                ServiceType.ResetProfession => "sm_econ_reset_prof_desc",
                ServiceType.BuildingBoost => "sm_econ_building_boost_desc",
                ServiceType.TechBoost => "sm_econ_tech_boost_desc",
                ServiceType.GeneRepair => "sm_econ_gene_repair_desc",
                _ => "?"
            };
        }

        /// <summary>购买服务（扣除伊纳尔并执行）</summary>
        public static bool Purchase(ServiceType type, Actor a, object param = null)
        {
            if (a == null) return false;
            int cost = GetCost(type, a);
            if (!SuperMechInal.SpendInal(a, cost))
            {
                Debug.Log($"[超神机械师] 伊纳尔不足，无法购买{type}（需要{cost}）");
                return false;
            }

            switch (type)
            {
                case ServiceType.Resurrect:
                    return DoResurrect(a);
                case ServiceType.Teleport:
                    return DoTeleport(a, param as City);
                case ServiceType.ResetRank:
                    return DoResetRank(a);
                case ServiceType.ResetProfession:
                    return DoResetProfession(a);
                case ServiceType.BuildingBoost:
                    return DoBuildingBoost(a, param as string);
                case ServiceType.TechBoost:
                    return DoTechBoost(a, param as string);
                case ServiceType.GeneRepair:
                    return DoGeneRepair(a);
                default:
                    return false;
            }
        }

        /// <summary>复活服务（回满血量，死亡单位尝试复活）</summary>
        private static bool DoResurrect(Actor a)
        {
            if (!a.isAlive())
            {
                // 死亡单位：尝试通过圣所系统复活
                try
                {
                    var record = SuperMechInfoStateRecord.GetDeadState(a.id);
                    if (record != null && SuperMechResurrection.CanResurrect(record))
                    {
                        SuperMechResurrection.Resurrect(record);
                        Debug.Log($"[超神机械师] {a.data.name} 通过付费服务复活");
                        return true;
                    }
                }
                catch { /* 复活系统不可用时回退 */ }
                return false;
            }
            // 存活单位：回满血量
            a.data.health = a.getMaxHealth();
            return true;
        }

        /// <summary>传送服务（传送到指定世界坐标）</summary>
        private static bool DoTeleport(Actor a, City targetCity)
        {
            if (targetCity == null || !a.isAlive()) return false;
            // City没有center字段，用城市第一个建筑的位置
            WorldTile tile = null;
            if (targetCity.buildings != null && targetCity.buildings.Count > 0)
            {
                tile = targetCity.buildings[0].current_tile;
            }
            if (tile != null)
            {
                a.current_tile = tile;
                Debug.Log($"[超神机械师] {a.data.name} 传送到 {targetCity.data.name}");
                return true;
            }
            return false;
        }

        /// <summary>重置阶位选择（回退到上一阶位）</summary>
        private static bool DoResetRank(Actor a)
        {
            int currentRank = SuperMechAdvancement.GetExactRankIndex(a);
            if (currentRank <= 0) return false;
            SuperMechAdvancement.SetExactRank(a, currentRank - 1);
            Debug.Log($"[超神机械师] {a.data.name} 重置阶位到 {currentRank - 1}");
            return true;
        }

        /// <summary>重置职业选择（清除职业和子类）</summary>
        private static bool DoResetProfession(Actor a)
        {
            SuperMechProfession.SetProfession(a, SuperMechProfession.ProfessionType.None);
            // 清除所有子类专精
            foreach (var subId in SuperMechSubClass.AllSubClasses)
            {
                if (SuperMechSubClass.HasSubClass(a, subId))
                    SuperMechSubClass.RemoveSubClass(a, subId);
            }
            Debug.Log($"[超神机械师] {a.data.name} 重置职业");
            return true;
        }

        /// <summary>建筑加速（立即完成殖民地建筑建设）</summary>
        private static bool DoBuildingBoost(Actor a, string planetId)
        {
            if (string.IsNullOrEmpty(planetId)) return false;
            var planet = SuperMechStarSystem.GetPlanet(planetId);
            if (planet == null || planet.buildings == null) return false;
            bool boosted = false;
            foreach (var b in planet.buildings)
            {
                if (b.isBuilding)
                {
                    b.isBuilding = false;
                    b.buildProgress = 1f;
                    b.level++;
                    boosted = true;
                }
            }
            return boosted;
        }

        /// <summary>科技加速（立即完成研究）</summary>
        private static bool DoTechBoost(Actor a, string techId)
        {
            if (string.IsNullOrEmpty(techId) || a.kingdom == null) return false;
            long kid = a.kingdom.data.id;
            if (SuperMechTech.IsResearched(kid, techId)) return false;
            // 直接研究（跳过科技点消耗，但需要伊纳尔）
            var def = SuperMechTech.GetDef(techId);
            if (def == null) return false;
            // 检查前置
            if (!string.IsNullOrEmpty(def.prerequisite) && !SuperMechTech.IsResearched(kid, def.prerequisite))
                return false;
            SuperMechTech.Research(kid, techId);
            return true;
        }

        /// <summary>基因修复（去除负面特质）</summary>
        private static bool DoGeneRepair(Actor a)
        {
            // 移除所有负面特质
            bool removed = false;
            var negativeTraits = new List<string>();
            if (a.data.saved_traits != null)
            {
                foreach (var t in a.data.saved_traits)
                {
                    if (IsNegativeTrait(t)) negativeTraits.Add(t);
                }
            }
            foreach (var t in negativeTraits)
            {
                a.removeTrait(t);
                removed = true;
            }
            return removed;
        }

        /// <summary>判断是否为负面特质</summary>
        private static bool IsNegativeTrait(string traitId)
        {
            string[] negative = { "ugly", "weak", "dumb", "coward", "greedy", "gluttonous", "lazy", "sickly", "infertile" };
            foreach (var n in negative)
            {
                if (traitId.ToLower().Contains(n)) return true;
            }
            return false;
        }

        /// <summary>获取所有可用服务列表（用于UI展示）</summary>
        public static List<ServiceType> GetAvailableServices(Actor a)
        {
            var list = new List<ServiceType>();
            if (a == null) return list;
            list.Add(ServiceType.Resurrect);
            list.Add(ServiceType.Teleport);
            list.Add(ServiceType.GeneRepair);
            if (SuperMechActorContextRegistry.GetRank(a) >= 2)
                list.Add(ServiceType.ResetRank);
            list.Add(ServiceType.ResetProfession);
            return list;
        }
    }
}
