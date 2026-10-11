using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>文明科技树系统（v0.99.5重构：6系文明科技，对照原著+现实科技路线）
    /// 能源/航天/信息/机械/生物/军事，每系5级，共30科技
    /// 超能者5系（机械/武道/魔法/异能/念力）为个人修炼体系，不在此科技树中
    /// 原著依据：幽能核心(ch663)、跃迁引擎、虫洞星门、歼星级武器(ch707)、机械军团、虚拟世界等</summary>
    public static class SuperMechTech
    {
        public enum TechBranch { Energy, Space, Info, Mech, Bio, Military }

        public class TechDef
        {
            public string id;
            public string name;
            public string desc;
            public TechBranch branch;
            public int tier;
            public int cost;
            public string prerequisite;
            public float dmgBonus;
            public float hpBonus;
            public float spdBonus;
            public string unlockBuilding;
            public string unlockUnit;
        }

        private static readonly Dictionary<string, TechDef> _defs = new Dictionary<string, TechDef>();
        private static readonly Dictionary<long, HashSet<string>> _researched = new Dictionary<long, HashSet<string>>();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterAll();
        }

        private static void Add(string id, string name, string desc, TechBranch branch, int tier, int cost, string pre, float dmg = 0, float hp = 0, float spd = 0, string building = null, string unit = null)
        {
            _defs[id] = new TechDef { id = id, name = name, desc = desc, branch = branch, tier = tier, cost = cost, prerequisite = pre, dmgBonus = dmg, hpBonus = hp, spdBonus = spd, unlockBuilding = building, unlockUnit = unit };
        }

        private static void RegisterAll()
        {
            // === 能源科技 ===
            Add("energy_t1", "化石能源", "煤炭石油天然气，工业革命的起点", TechBranch.Energy, 1, 50, null, 0.02f, 0.01f);
            Add("energy_t2", "核能利用", "核裂变与核聚变，清洁能源与强大动力", TechBranch.Energy, 2, 100, "energy_t1", 0.04f, 0.03f);
            Add("energy_t3", "暗能量引擎", "汲取宇宙暗能量，远超核能的动力源（原著暗能汲取技能）", TechBranch.Energy, 3, 200, "energy_t2", 0.07f, 0.05f);
            Add("energy_t4", "幽能核心", "幽能转化装置，高级文明标配能源（原著第663章幽能核心）", TechBranch.Energy, 4, 400, "energy_t3", 0.12f, 0.08f);
            Add("energy_t5", "零点能提取", "从真空中提取零点能，无限能源，顶级文明标志", TechBranch.Energy, 5, 800, "energy_t4", 0.20f, 0.15f);

            // === 航天科技 ===
            Add("space_t1", "火箭与卫星", "进入太空的第一步，通信与侦察卫星", TechBranch.Space, 1, 50, null, 0.01f, 0.01f, 0.03f);
            Add("space_t2", "空间站与亚光速飞船", "轨道空间站，亚光速航行成为可能", TechBranch.Space, 2, 100, "space_t1", 0.02f, 0.03f, 0.05f);
            Add("space_t3", "跃迁引擎", "曲率驱动，超光速航行（原著跃迁42次）", TechBranch.Space, 3, 200, "space_t2", 0.04f, 0.05f, 0.10f);
            Add("space_t4", "星门与虫洞", "人工虫洞与星门网络，瞬时跨星系旅行（原著虫洞103次/星门38次）", TechBranch.Space, 4, 400, "space_t3", 0.06f, 0.08f, 0.15f);
            Add("space_t5", "星际堡垒与戴森球", "移动星际堡垒，恒星级能量收集（原著堡垒15次）", TechBranch.Space, 5, 800, "space_t4", 0.10f, 0.20f, 0.20f);

            // === 信息科技 ===
            Add("info_t1", "计算机与互联网", "电子计算机与全球网络，信息时代开端", TechBranch.Info, 1, 50, null, 0.02f, 0.01f);
            Add("info_t2", "量子网络", "量子通信与量子计算，绝对安全的网络（原著量子96次）", TechBranch.Info, 2, 100, "info_t1", 0.03f, 0.02f);
            Add("info_t3", "人工智能", "强人工智能，自动化决策与生产（原著AI19次）", TechBranch.Info, 3, 200, "info_t2", 0.05f, 0.04f, 0.05f);
            Add("info_t4", "虚拟世界", "完全沉浸式虚拟空间，意识数字化（原著虚拟世界2次）", TechBranch.Info, 4, 400, "info_t3", 0.08f, 0.06f, 0.08f);
            Add("info_t5", "意识上传与数据生命", "意识数字化永生，数据生命体，信息域主宰", TechBranch.Info, 5, 800, "info_t4", 0.12f, 0.10f, 0.12f);

            // === 机械科技 ===
            Add("mech_t1", "基础机械与自动化", "蒸汽机与自动化生产线，工业基础", TechBranch.Mech, 1, 50, null, 0.03f, 0.02f);
            Add("mech_t2", "机甲与机器人", "人形机甲与智能机器人（原著机甲79次/机器人55次）", TechBranch.Mech, 2, 100, "mech_t1", 0.05f, 0.04f, 0.03f);
            Add("mech_t3", "机械军团与量产", "标准化机械军团，流水线量产（原著机械军团67次）", TechBranch.Mech, 3, 200, "mech_t2", 0.09f, 0.07f, 0.05f);
            Add("mech_t4", "纳米机械与神之零件", "纳米级精密制造，神力级核心零件（原著纳米17次）", TechBranch.Mech, 4, 400, "mech_t3", 0.14f, 0.10f, 0.08f);
            Add("mech_t5", "神之机械与械国传承", "亿万械国麦尼逊传承，机械系巅峰，神级机械", TechBranch.Mech, 5, 800, "mech_t4", 0.25f, 0.15f, 0.10f);

            // === 生物科技 ===
            Add("bio_t1", "基因编辑与克隆", "基因编辑技术，克隆生物成为可能", TechBranch.Bio, 1, 50, null, 0.01f, 0.04f);
            Add("bio_t2", "基因药剂与进化", "基因强化药剂，人工诱导进化（原著基因药剂1次）", TechBranch.Bio, 2, 100, "bio_t1", 0.03f, 0.07f);
            Add("bio_t3", "种族优化与变异", "种族级基因优化，可控变异（原著变异35次）", TechBranch.Bio, 3, 200, "bio_t2", 0.05f, 0.12f);
            Add("bio_t4", "生化改造与器官移植", "生化电子改造，器官再生与强化", TechBranch.Bio, 4, 400, "bio_t3", 0.08f, 0.18f);
            Add("bio_t5", "意识转移与生物永生", "意识跨载体转移，生物级永生技术", TechBranch.Bio, 5, 800, "bio_t4", 0.12f, 0.25f);

            // === 军事科技 ===
            Add("mil_t1", "火药武器与导弹", "火枪火炮与导弹，现代战争基础（原著导弹32次）", TechBranch.Military, 1, 50, null, 0.04f, 0.01f);
            Add("mil_t2", "能量武器与激光", "激光与等离子武器，定向能武器（原著激光7次/等离子5次）", TechBranch.Military, 2, 100, "mil_t1", 0.07f, 0.02f);
            Add("mil_t3", "能量护盾与轨道轰炸", "能量护盾防御，天基轨道打击（原著能量护盾2次）", TechBranch.Military, 3, 200, "mil_t2", 0.10f, 0.06f);
            Add("mil_t4", "歼星级武器", "歼星级毁灭性武器（原著第707章歼星级武器与使徒化）", TechBranch.Military, 4, 400, "mil_t3", 0.18f, 0.08f);
            Add("mil_t5", "行星武器与规律武器", "行星级毁灭武器，宇宙规律级干涉", TechBranch.Military, 5, 800, "mil_t4", 0.30f, 0.12f);
        }

        public static TechDef GetDef(string id)
        {
            return _defs.TryGetValue(id, out var def) ? def : null;
        }

        public static List<TechDef> GetByBranch(TechBranch branch)
        {
            var list = new List<TechDef>();
            foreach (var def in _defs.Values)
                if (def.branch == branch) list.Add(def);
            list.Sort((a, b) => a.tier.CompareTo(b.tier));
            return list;
        }

        public static bool IsResearched(long kingdomId, string techId)
        {
            return _researched.TryGetValue(kingdomId, out var set) && set.Contains(techId);
        }

        public static bool CanResearch(long kingdomId, string techId)
        {
            if (!_defs.TryGetValue(techId, out var def)) return false;
            if (IsResearched(kingdomId, techId)) return false;
            if (!string.IsNullOrEmpty(def.prerequisite) && !IsResearched(kingdomId, def.prerequisite)) return false;
            return SuperMechCivilization.GetTechPointsFromKingdomById(kingdomId) >= def.cost;
        }

        public static bool Research(long kingdomId, string techId)
        {
            if (!CanResearch(kingdomId, techId)) return false;
            var def = _defs[techId];
            if (!SuperMechCivilization.SpendTechPoints(kingdomId, def.cost)) return false;
            if (!_researched.TryGetValue(kingdomId, out var set))
            {
                set = new HashSet<string>();
                _researched[kingdomId] = set;
            }
            set.Add(techId);
            return true;
        }

        public static (float dmg, float hp, float spd) GetBonuses(long kingdomId)
        {
            float dmg = 0, hp = 0, spd = 0;
            if (!_researched.TryGetValue(kingdomId, out var set)) return (0, 0, 0);
            foreach (var id in set)
            {
                if (_defs.TryGetValue(id, out var def))
                {
                    dmg += def.dmgBonus;
                    hp += def.hpBonus;
                    spd += def.spdBonus;
                }
            }
            return (dmg, hp, spd);
        }

        public static void ClearAll()
        {
            _researched.Clear();
        }
    }
}
