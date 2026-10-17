using System;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.234 科技系统重设计：数据结构定义
    /// 参考帝国盒子架构，独立实现。JSON驱动，5分支×9时代。
    /// </summary>
    public static class TechData
    {
        /// <summary>科技分支</summary>
        public enum Branch
        {
            Base,    // 基础生存
            Mech,    // 机械工程
            Power,   // 超能觉醒
            Space,   // 星际航行
            Info     // 信息生命
        }

        /// <summary>加成效果类型</summary>
        public enum BoostType
        {
            Resource,          // 资源产出加成
            BuildingType,      // 建筑类型效率
            Population,        // 人口上限
            Cities,            // 城市数上限
            Research,          // 研究点产出
            SuperAwakening,    // 超能觉醒率
            WarKnows,          // 战争知识（战斗加成）
            ContactKnows,      // 接触知识（外交加成）
            InstitutionFeature // 制度特征
        }

        /// <summary>研究点产出配置</summary>
        [Serializable]
        public class ResearchConfig
        {
            public float base_per_city = 0.2f;
            public float per_population = 0.005f;
            public float per_library = 1.0f;
            public float per_temple = 0.3f;
            public float per_university = 2.0f;
            public float per_factory = 0.5f;
            public float per_sanctuary = 3.0f;
            public float war_penalty = 0.25f;
            public float cost_multiplier = 12.0f;
            public float gold_per_point = 0f;
            public float stagnation_threshold = 1.0f;
        }

        /// <summary>时代定义</summary>
        [Serializable]
        public class EraDef
        {
            public int tier;
            public string key = "";
        }

        /// <summary>材料所需资源</summary>
        [Serializable]
        public class MaterialResource
        {
            public string id = "";
            public int amount = 1;
        }

        /// <summary>材料定义</summary>
        [Serializable]
        public class MaterialDef
        {
            public string id = "";
            public int tier;
            public List<MaterialResource> resources = new List<MaterialResource>();
            public List<string> requires_techs = new List<string>();
        }

        /// <summary>科技加成效果</summary>
        [Serializable]
        public class TechBoost
        {
            public string type = "";
            public string value = "";
            public float amount = 1f;
        }

        /// <summary>科技定义</summary>
        [Serializable]
        public class TechDef
        {
            public string id = "";
            public string branch = "";
            public int tier;
            public int cost = 10;
            public List<string> requires_techs = new List<string>();
            public List<string> requires_materials = new List<string>();
            public List<string> unlock_buildings = new List<string>();
            public List<string> unlock_items = new List<string>();
            public List<string> unlock_knowledge = new List<string>();
            public List<string> unlock_professions = new List<string>();
            public List<string> unlock_units = new List<string>();
            public List<TechBoost> boosts = new List<TechBoost>();
        }

        /// <summary>科技树完整数据（对应JSON根对象）</summary>
        [Serializable]
        public class TechTreeData
        {
            public int schema_version = 1;
            public ResearchConfig research = new ResearchConfig();
            public List<EraDef> eras = new List<EraDef>();
            public List<string> branches = new List<string>();
            public List<MaterialDef> materials = new List<MaterialDef>();
            public List<TechDef> techs = new List<TechDef>();
        }

        /// <summary>分支ID转枚举</summary>
        public static Branch ParseBranch(string id)
        {
            switch (id?.ToLower())
            {
                case "base": return Branch.Base;
                case "mech": return Branch.Mech;
                case "power": return Branch.Power;
                case "space": return Branch.Space;
                case "info": return Branch.Info;
                default: return Branch.Base;
            }
        }

        /// <summary>分支枚举转ID</summary>
        public static string BranchId(Branch b)
        {
            switch (b)
            {
                case Branch.Base: return "base";
                case Branch.Mech: return "mech";
                case Branch.Power: return "power";
                case Branch.Space: return "space";
                case Branch.Info: return "info";
                default: return "base";
            }
        }

        /// <summary>加成类型ID转枚举</summary>
        public static BoostType ParseBoostType(string id)
        {
            switch (id?.ToLower())
            {
                case "resource": return BoostType.Resource;
                case "building_type": return BoostType.BuildingType;
                case "population": return BoostType.Population;
                case "cities": return BoostType.Cities;
                case "research": return BoostType.Research;
                case "super_awakening": return BoostType.SuperAwakening;
                case "war_knows": return BoostType.WarKnows;
                case "contact_knows": return BoostType.ContactKnows;
                case "institution_feature": return BoostType.InstitutionFeature;
                default: return BoostType.Resource;
            }
        }
    }
}
