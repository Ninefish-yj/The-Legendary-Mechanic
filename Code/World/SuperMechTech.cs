using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>文明科技树系统（v0.99.6重构：6系×8级=48科技，对照原著完整科技路线）
    /// 能源/航天/信息/机械/生物/军事，每系8级
    /// 原著依据：械国865次、次级维度808次、力场330次、母舰227次、使徒兵器222次、暗能量89次等</summary>
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

        private static void Add(string id, string name, string desc, TechBranch branch, int tier, int cost, string pre, float dmg = 0, float hp = 0, float spd = 0)
        {
            _defs[id] = new TechDef { id = id, name = name, desc = desc, branch = branch, tier = tier, cost = cost, prerequisite = pre, dmgBonus = dmg, hpBonus = hp, spdBonus = spd };
        }

        private static void RegisterAll()
        {
            // === 能源科技（8级）===
            Add("energy_t1", "化石能源", "煤炭石油天然气，工业革命的起点", TechBranch.Energy, 1, 30, null, 0.01f, 0.01f);
            Add("energy_t2", "核裂变", "核裂变反应堆，强大的能源来源（原著核裂变4次）", TechBranch.Energy, 2, 60, "energy_t1", 0.02f, 0.02f);
            Add("energy_t3", "核聚变", "可控核聚变，清洁能源与强大动力（原著核聚变8次）", TechBranch.Energy, 3, 100, "energy_t2", 0.04f, 0.03f);
            Add("energy_t4", "反物质引擎", "物质反物质湮灭，近100%质能转换（原著反物质7次）", TechBranch.Energy, 4, 180, "energy_t3", 0.06f, 0.05f);
            Add("energy_t5", "暗能量引擎", "汲取宇宙暗能量，远超核能的动力源（原著暗能量89次）", TechBranch.Energy, 5, 300, "energy_t4", 0.09f, 0.07f);
            Add("energy_t6", "幽能核心", "幽能转化装置，高级文明标配能源（原著幽能48次/ch663）", TechBranch.Energy, 6, 500, "energy_t5", 0.13f, 0.10f);
            Add("energy_t7", "零点能提取", "从真空中提取零点能，近乎无限能源", TechBranch.Energy, 7, 800, "energy_t6", 0.18f, 0.14f);
            Add("energy_t8", "恒星能量与戴森球", "恒星级能量收集，戴森球结构（原著恒星能量11次）", TechBranch.Energy, 8, 1200, "energy_t7", 0.25f, 0.20f);

            // === 航天科技（8级）===
            Add("space_t1", "火箭与卫星", "进入太空的第一步，通信与侦察卫星", TechBranch.Space, 1, 30, null, 0.01f, 0.01f, 0.02f);
            Add("space_t2", "空间站与太空电梯", "轨道空间站，天地往返系统（原著太空电梯1次）", TechBranch.Space, 2, 60, "space_t1", 0.02f, 0.02f, 0.03f);
            Add("space_t3", "亚光速飞船", "亚光速航行，星际探索成为可能（原著光速2次）", TechBranch.Space, 3, 100, "space_t2", 0.03f, 0.03f, 0.06f);
            Add("space_t4", "曲速引擎", "曲率驱动，超光速航行（原著曲速29次/曲率8次）", TechBranch.Space, 4, 180, "space_t3", 0.05f, 0.05f, 0.10f);
            Add("space_t5", "跃迁引擎", "空间跃迁，瞬时跨星系移动（原著跃迁42次）", TechBranch.Space, 5, 300, "space_t4", 0.07f, 0.07f, 0.14f);
            Add("space_t6", "星门与虫洞", "人工虫洞与星门网络（原著虫洞103次/星门38次/虫洞发生器2次）", TechBranch.Space, 6, 500, "space_t5", 0.10f, 0.10f, 0.18f);
            Add("space_t7", "星际堡垒", "移动星际堡垒，移动要塞（原著堡垒15次）", TechBranch.Space, 7, 800, "space_t6", 0.14f, 0.18f, 0.20f);
            Add("space_t8", "母舰舰队", "母舰战列舰巡洋舰驱逐舰护卫舰完整舰队（原著母舰227次/护卫舰63次/战列舰3次/驱逐舰3次）", TechBranch.Space, 8, 1200, "space_t7", 0.20f, 0.25f, 0.25f);

            // === 信息科技（8级）===
            Add("info_t1", "计算机与互联网", "电子计算机与全球网络，信息时代开端", TechBranch.Info, 1, 30, null, 0.01f, 0.01f);
            Add("info_t2", "量子通信", "量子纠缠通信，绝对安全（原著量子纠缠5次/量子96次）", TechBranch.Info, 2, 60, "info_t1", 0.02f, 0.01f);
            Add("info_t3", "人工智能", "强人工智能，自动化决策（原著AI19次）", TechBranch.Info, 3, 100, "info_t2", 0.03f, 0.02f, 0.03f);
            Add("info_t4", "量子计算", "量子计算机，超高速运算", TechBranch.Info, 4, 180, "info_t3", 0.05f, 0.03f, 0.05f);
            Add("info_t5", "虚拟世界", "完全沉浸式虚拟空间（原著虚拟世界2次）", TechBranch.Info, 5, 300, "info_t4", 0.07f, 0.05f, 0.07f);
            Add("info_t6", "数据解析", "解析敌方数据，弱点分析与情报", TechBranch.Info, 6, 500, "info_t5", 0.10f, 0.07f, 0.09f);
            Add("info_t7", "意识数字化", "意识数字化存储与传输", TechBranch.Info, 7, 800, "info_t6", 0.14f, 0.10f, 0.12f);
            Add("info_t8", "数据生命与信息域主宰", "数据生命体，信息域全知全能", TechBranch.Info, 8, 1200, "info_t7", 0.20f, 0.15f, 0.18f);

            // === 机械科技（8级）===
            Add("mech_t1", "基础机械与自动化", "蒸汽机与自动化生产线，工业基础", TechBranch.Mech, 1, 30, null, 0.02f, 0.01f);
            Add("mech_t2", "机甲与机器人", "人形机甲与智能机器人（原著机甲79次/机器人55次）", TechBranch.Mech, 2, 60, "mech_t1", 0.03f, 0.02f, 0.02f);
            Add("mech_t3", "械兵与械兽", "标准化械兵与机械兽军团（原著械兵49次/械兽56次）", TechBranch.Mech, 3, 100, "mech_t2", 0.05f, 0.04f, 0.03f);
            Add("mech_t4", "机械军团与量产", "流水线量产，军团协同作战（原著机械军团67次）", TechBranch.Mech, 4, 180, "mech_t3", 0.08f, 0.06f, 0.05f);
            Add("mech_t5", "纳米机械", "纳米级精密制造与纳米机器人（原著纳米17次）", TechBranch.Mech, 5, 300, "mech_t4", 0.12f, 0.09f, 0.07f);
            Add("mech_t6", "神之零件", "神力级核心零件，超越凡俗的制造工艺", TechBranch.Mech, 6, 500, "mech_t5", 0.16f, 0.12f, 0.09f);
            Add("mech_t7", "使徒兵器", "超A级机械师终极兵器，拥有自主意识（原著使徒兵器222次）", TechBranch.Mech, 7, 800, "mech_t6", 0.22f, 0.16f, 0.12f);
            Add("mech_t8", "神之机械与械国传承", "亿万械国麦尼逊传承，机械系巅峰（原著械国865次）", TechBranch.Mech, 8, 1200, "mech_t7", 0.30f, 0.22f, 0.15f);

            // === 生物科技（8级）===
            Add("bio_t1", "基因编辑", "基因编辑技术，改造生物性状", TechBranch.Bio, 1, 30, null, 0.01f, 0.03f);
            Add("bio_t2", "克隆技术", "克隆生物，批量复制优良性状", TechBranch.Bio, 2, 60, "bio_t1", 0.02f, 0.05f);
            Add("bio_t3", "基因药剂", "基因强化药剂，人工诱导进化（原著基因药剂1次）", TechBranch.Bio, 3, 100, "bio_t2", 0.03f, 0.08f);
            Add("bio_t4", "生化改造", "生化电子改造，机械与生物融合（原著生化9次）", TechBranch.Bio, 4, 180, "bio_t3", 0.05f, 0.12f);
            Add("bio_t5", "种族优化与变异", "种族级基因优化，可控变异（原著变异35次）", TechBranch.Bio, 5, 300, "bio_t4", 0.07f, 0.16f);
            Add("bio_t6", "器官再生", "器官再生与强化，断肢重生", TechBranch.Bio, 6, 500, "bio_t5", 0.10f, 0.20f);
            Add("bio_t7", "意识转移", "意识跨载体转移，更换身体（原著灵魂转移12次）", TechBranch.Bio, 7, 800, "bio_t6", 0.14f, 0.25f);
            Add("bio_t8", "生物永生", "生物级永生技术，不老不死", TechBranch.Bio, 8, 1200, "bio_t7", 0.20f, 0.35f);

            // === 军事科技（8级）===
            Add("mil_t1", "火药武器", "火枪火炮，现代战争基础", TechBranch.Military, 1, 30, null, 0.03f, 0.01f);
            Add("mil_t2", "导弹", "制导导弹，精确打击（原著导弹32次）", TechBranch.Military, 2, 60, "mil_t1", 0.05f, 0.01f);
            Add("mil_t3", "激光与电磁炮", "激光武器与电磁轨道炮（原著激光7次/电磁炮5次/轨道炮17次）", TechBranch.Military, 3, 100, "mil_t2", 0.08f, 0.02f);
            Add("mil_t4", "等离子与能量武器", "等离子武器与定向能武器（原著等离子5次）", TechBranch.Military, 4, 180, "mil_t3", 0.12f, 0.03f);
            Add("mil_t5", "能量护盾与力场", "能量护盾与力场防御（原著能量护盾18次/力场330次/力场护盾1次）", TechBranch.Military, 5, 300, "mil_t4", 0.16f, 0.08f);
            Add("mil_t6", "反重力与惯性抑制", "反重力推进与惯性补偿（原著反重力35次）", TechBranch.Military, 6, 500, "mil_t5", 0.20f, 0.12f, 0.10f);
            Add("mil_t7", "轨道轰炸", "天基轨道打击，从太空轰击地面", TechBranch.Military, 7, 800, "mil_t6", 0.26f, 0.15f);
            Add("mil_t8", "歼星级武器", "歼星级毁灭性武器（原著歼星8次/ch707歼星级武器与使徒化）", TechBranch.Military, 8, 1200, "mil_t7", 0.35f, 0.20f);
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
            // 研究航天科技后发现新星球
            if (techId.StartsWith("space_t"))
            {
                int tier = int.Parse(techId.Split('_')[1].Substring(1));
                SuperMechStarSystem.DiscoverPlanets(tier);
            }
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
