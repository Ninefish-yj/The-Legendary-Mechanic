using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>科技定义：5系科技树，每系5级，对照原著三大文明科技发展路线</summary>
    public static class SuperMechTech
    {
        public enum TechBranch { Mech, Martial, Mage, Psi, Mind }

        public class TechDef
        {
            public string id;
            public string name;
            public string desc;
            public TechBranch branch;
            public int tier;           // 0-4
            public int cost;           // 科技点消耗
            public string prerequisite; // 前置科技ID
            // 效果
            public float damageBonus;   // 全文明伤害加成
            public float healthBonus;   // 全文明生命加成
            public float speedBonus;    // 全文明速度加成
            public string unlockBuilding; // 解锁建筑ID
        }

        private static readonly Dictionary<string, TechDef> _allTech = new Dictionary<string, TechDef>();
        private static readonly Dictionary<long, HashSet<string>> _researched = new Dictionary<long, HashSet<string>>(); // kingdomId -> techIds
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            DefineTechs();
        }

        private static void DefineTechs()
        {
            // === 机械系（对照原著：光辉联邦机械科技路线）===
            Add("sm_tech_mech_0", "基础机械学", "掌握机械构造原理，机械系单位伤害+5%", TechBranch.Mech, 0, 10, null, 0.05f, 0, 0, null);
            Add("sm_tech_mech_1", "动力核心", "研发高效动力源，机械系单位伤害+10%，解锁机械工厂", TechBranch.Mech, 1, 25, "sm_tech_mech_0", 0.10f, 0, 0, "sm_mech_factory");
            Add("sm_tech_mech_2", "军团编制", "标准化机械军团，机械系单位生命+15%", TechBranch.Mech, 2, 50, "sm_tech_mech_1", 0, 0.15f, 0, null);
            Add("sm_tech_mech_3", "虚拟创世", "机械师虚拟分支巅峰，全文明伤害+15%", TechBranch.Mech, 3, 100, "sm_tech_mech_2", 0.15f, 0, 0, null);
            Add("sm_tech_mech_4", "神之机械", "机械系终极科技，全文明伤害+25%，生命+20%", TechBranch.Mech, 4, 200, "sm_tech_mech_3", 0.25f, 0.20f, 0, null);

            // === 武道系（对照原著：武道文明炼体路线）===
            Add("sm_tech_martial_0", "基础炼体", "强身健体，武道系单位生命+5%", TechBranch.Martial, 0, 10, null, 0, 0.05f, 0, null);
            Add("sm_tech_martial_1", "气血搬运", "内息运转，武道系单位生命+10%，速度+5%", TechBranch.Martial, 1, 25, "sm_tech_martial_0", 0, 0.10f, 0.05f, null);
            Add("sm_tech_martial_2", "武道宗门", "建立武道传承体系，武道系单位伤害+15%", TechBranch.Martial, 2, 50, "sm_tech_martial_1", 0.15f, 0, 0, null);
            Add("sm_tech_martial_3", "破碎虚空", "武道巅峰，全文明速度+15%", TechBranch.Martial, 3, 100, "sm_tech_martial_2", 0, 0, 0.15f, null);
            Add("sm_tech_martial_4", "武祖传承", "武道系终极科技，全文明生命+25%，伤害+15%", TechBranch.Martial, 4, 200, "sm_tech_martial_3", 0.15f, 0.25f, 0, null);

            // === 魔法系（对照原著：法师文明奥术路线）===
            Add("sm_tech_mage_0", "奥术基础", "掌握魔法能量，魔法系单位伤害+5%", TechBranch.Mage, 0, 10, null, 0.05f, 0, 0, null);
            Add("sm_tech_mage_1", "元素掌控", "操控元素之力，魔法系单位伤害+10%", TechBranch.Mage, 1, 25, "sm_tech_mage_0", 0.10f, 0, 0, null);
            Add("sm_tech_mage_2", "法师塔", "建立法师塔，魔法系单位生命+15%", TechBranch.Mage, 2, 50, "sm_tech_mage_1", 0, 0.15f, 0, "sm_mage_tower");
            Add("sm_tech_mage_3", "禁咒研究", "研究禁忌魔法，全文明伤害+15%", TechBranch.Mage, 3, 100, "sm_tech_mage_2", 0.15f, 0, 0, null);
            Add("sm_tech_mage_4", "魔法神域", "魔法系终极科技，全文明伤害+25%，速度+10%", TechBranch.Mage, 4, 200, "sm_tech_mage_3", 0.25f, 0, 0.10f, null);

            // === 异能系（对照原著：异能者觉醒路线）===
            Add("sm_tech_psi_0", "异能觉醒", "激发潜能，异能系单位伤害+5%", TechBranch.Psi, 0, 10, null, 0.05f, 0, 0, null);
            Add("sm_tech_psi_1", "精神力训练", "强化精神力，异能系单位伤害+10%", TechBranch.Psi, 1, 25, "sm_tech_psi_0", 0.10f, 0, 0, null);
            Add("sm_tech_psi_2", "异能分类", "系统化异能分支，异能系单位生命+15%", TechBranch.Psi, 2, 50, "sm_tech_psi_1", 0, 0.15f, 0, null);
            Add("sm_tech_psi_3", "领域展开", "异能者巅峰能力，全文明生命+15%", TechBranch.Psi, 3, 100, "sm_tech_psi_2", 0, 0.15f, 0, null);
            Add("sm_tech_psi_4", "概念干涉", "异能系终极科技，全文明伤害+20%，生命+20%", TechBranch.Psi, 4, 200, "sm_tech_psi_3", 0.20f, 0.20f, 0, null);

            // === 念力系（对照原著：念力师精神路线）===
            Add("sm_tech_mind_0", "念力感知", "基础精神感知，念力系单位速度+5%", TechBranch.Mind, 0, 10, null, 0, 0, 0.05f, null);
            Add("sm_tech_mind_1", "心灵链接", "建立精神网络，念力系单位速度+10%", TechBranch.Mind, 1, 25, "sm_tech_mind_0", 0, 0, 0.10f, null);
            Add("sm_tech_mind_2", "精神控制", "操控他人意志，念力系单位伤害+15%", TechBranch.Mind, 2, 50, "sm_tech_mind_1", 0.15f, 0, 0, null);
            Add("sm_tech_mind_3", "集体意识", "念力师巅峰，全文明速度+15%", TechBranch.Mind, 3, 100, "sm_tech_mind_2", 0, 0, 0.15f, null);
            Add("sm_tech_mind_4", "思维殿堂", "念力系终极科技，全文明速度+25%，生命+15%", TechBranch.Mind, 4, 200, "sm_tech_mind_3", 0, 0.15f, 0.25f, null);
        }

        private static void Add(string id, string name, string desc, TechBranch branch, int tier, int cost,
            string prereq, float dmg, float hp, float spd, string building)
        {
            _allTech[id] = new TechDef
            {
                id = id, name = name, desc = desc, branch = branch, tier = tier,
                cost = cost, prerequisite = prereq,
                damageBonus = dmg, healthBonus = hp, speedBonus = spd,
                unlockBuilding = building
            };
        }

        public static TechDef GetDef(string id)
        {
            return _allTech.TryGetValue(id, out var def) ? def : null;
        }

        public static List<TechDef> GetAll()
        {
            return new List<TechDef>(_allTech.Values);
        }

        public static List<TechDef> GetByBranch(TechBranch branch)
        {
            var list = new List<TechDef>();
            foreach (var t in _allTech.Values)
                if (t.branch == branch) list.Add(t);
            list.Sort((a, b) => a.tier.CompareTo(b.tier));
            return list;
        }

        public static bool IsResearched(long kingdomId, string techId)
        {
            return _researched.TryGetValue(kingdomId, out var set) && set.Contains(techId);
        }

        public static bool CanResearch(long kingdomId, string techId)
        {
            if (!_allTech.TryGetValue(techId, out var def)) return false;
            if (IsResearched(kingdomId, techId)) return false;
            if (!string.IsNullOrEmpty(def.prerequisite) && !IsResearched(kingdomId, def.prerequisite)) return false;
            return SuperMechCivilization.GetTechPointsFromKingdomById(kingdomId) >= def.cost;
        }

        public static bool Research(long kingdomId, string techId)
        {
            if (!CanResearch(kingdomId, techId)) return false;
            var def = _allTech[techId];
            if (!SuperMechCivilization.SpendTechPoints(kingdomId, def.cost)) return false;
            if (!_researched.TryGetValue(kingdomId, out var set))
            {
                set = new HashSet<string>();
                _researched[kingdomId] = set;
            }
            set.Add(techId);
            Debug.Log($"[超神机械师] 文明科技突破: {techId}");
            return true;
        }

        /// <summary>获取文明所有已研究科技的总加成</summary>
        public static (float dmg, float hp, float spd) GetBonuses(long kingdomId)
        {
            float dmg = 0, hp = 0, spd = 0;
            if (!_researched.TryGetValue(kingdomId, out var set)) return (0, 0, 0);
            foreach (var id in set)
            {
                if (!_allTech.TryGetValue(id, out var def)) continue;
                dmg += def.damageBonus;
                hp += def.healthBonus;
                spd += def.speedBonus;
            }
            return (dmg, hp, spd);
        }

        public static int GetResearchedCount(long kingdomId)
        {
            return _researched.TryGetValue(kingdomId, out var set) ? set.Count : 0;
        }

        public static void ClearAll()
        {
            _researched.Clear();
        }
    }
}
