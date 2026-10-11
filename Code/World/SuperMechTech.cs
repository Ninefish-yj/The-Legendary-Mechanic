using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>文明科技树系统（v0.99.4重构：多分支结构，参考文明/钢铁雄心）
    /// 5系×3分支×5级=75科技，第1级共同基础，第2-5级分支专属
    /// 对照原著：三大文明科技路线，玩家选择分支发展方向</summary>
    public static class SuperMechTech
    {
        public enum TechBranch { Mech, Martial, Mage, Psi, Mind }
        public enum TechSubBranch { Common, A, B, C }

        public class TechDef
        {
            public string id;
            public string name;
            public string desc;
            public TechBranch branch;
            public TechSubBranch subBranch;
            public int tier;
            public int cost;
            public string prerequisite;
            public float dmgBonus;
            public float hpBonus;
            public float spdBonus;
            public string unlockBuilding;
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

        private static void Add(string id, string name, string desc, TechBranch branch, TechSubBranch sub, int tier, int cost, string pre, float dmg = 0, float hp = 0, float spd = 0, string building = null)
        {
            _defs[id] = new TechDef { id = id, name = name, desc = desc, branch = branch, subBranch = sub, tier = tier, cost = cost, prerequisite = pre, dmgBonus = dmg, hpBonus = hp, spdBonus = spd, unlockBuilding = building };
        }

        private static void RegisterAll()
        {
            // === 机械系 ===
            Add("mech_t1", "基础机械学", "机械文明的起点，掌握基础机械原理", TechBranch.Mech, TechSubBranch.Common, 1, 50, null, 0.03f, 0.02f);
            // 军团分支
            Add("mech_legion_t2", "动力核心", "高能量动力源，为机械军团提供动力", TechBranch.Mech, TechSubBranch.A, 2, 100, "mech_t1", 0.05f, 0.03f);
            Add("mech_legion_t3", "军团编制", "标准化机械军团编制，量产能力提升", TechBranch.Mech, TechSubBranch.A, 3, 200, "mech_legion_t2", 0.08f, 0.05f);
            Add("mech_legion_t4", "量产技术", "流水线量产，械兵生产成本降低", TechBranch.Mech, TechSubBranch.A, 4, 400, "mech_legion_t3", 0.12f, 0.08f);
            Add("mech_legion_t5", "机械洪流", "无穷无尽的机械军团，碾压一切", TechBranch.Mech, TechSubBranch.A, 5, 800, "mech_legion_t4", 0.20f, 0.15f);
            // 虚拟分支
            Add("mech_virtual_t2", "虚拟技术", "虚拟现实与数据空间技术", TechBranch.Mech, TechSubBranch.B, 2, 100, "mech_t1", 0.03f, 0.05f);
            Add("mech_virtual_t3", "数据解析", "解析敌方数据，弱点分析", TechBranch.Mech, TechSubBranch.B, 3, 200, "mech_virtual_t2", 0.05f, 0.08f);
            Add("mech_virtual_t4", "虚拟创世", "在虚拟空间中预演战斗，研究加速", TechBranch.Mech, TechSubBranch.B, 4, 400, "mech_virtual_t3", 0.08f, 0.12f);
            Add("mech_virtual_t5", "信息主宰", "掌控信息域，全知全能", TechBranch.Mech, TechSubBranch.B, 5, 800, "mech_virtual_t4", 0.15f, 0.20f);
            // 神械分支
            Add("mech_divine_t2", "精密制造", "微米级精密制造工艺", TechBranch.Mech, TechSubBranch.C, 2, 100, "mech_t1", 0.06f, 0.02f);
            Add("mech_divine_t3", "神之零件", "使用神力级材料制造核心零件", TechBranch.Mech, TechSubBranch.C, 3, 200, "mech_divine_t2", 0.10f, 0.04f);
            Add("mech_divine_t4", "神之机械", "超越凡俗的神级机械", TechBranch.Mech, TechSubBranch.C, 4, 400, "mech_divine_t3", 0.15f, 0.06f);
            Add("mech_divine_t5", "械国传承", "亿万械国麦尼逊的传承，机械系巅峰", TechBranch.Mech, TechSubBranch.C, 5, 800, "mech_divine_t4", 0.25f, 0.10f);

            // === 武道系 ===
            Add("martial_t1", "基础炼体", "武道的起点，锤炼肉身", TechBranch.Martial, TechSubBranch.Common, 1, 50, null, 0.02f, 0.03f);
            // 炼体分支
            Add("martial_body_t2", "气血搬运", "搬运气血，强化肉身", TechBranch.Martial, TechSubBranch.A, 2, 100, "martial_t1", 0.03f, 0.06f);
            Add("martial_body_t3", "铜皮铁骨", "肉身坚如钢铁，刀枪不入", TechBranch.Martial, TechSubBranch.A, 3, 200, "martial_body_t2", 0.05f, 0.10f);
            Add("martial_body_t4", "破碎虚空", "肉身强横到可以破碎空间", TechBranch.Martial, TechSubBranch.A, 4, 400, "martial_body_t3", 0.08f, 0.15f);
            Add("martial_body_t5", "武祖传承", "武道始祖的传承，肉身成圣", TechBranch.Martial, TechSubBranch.A, 5, 800, "martial_body_t4", 0.12f, 0.25f);
            // 技巧分支
            Add("martial_skill_t2", "身法训练", "提升移动速度和反应", TechBranch.Martial, TechSubBranch.B, 2, 100, "martial_t1", 0.04f, 0.02f, 0.05f);
            Add("martial_skill_t3", "战斗技巧", "精妙的战斗技巧，弱点打击", TechBranch.Martial, TechSubBranch.B, 3, 200, "martial_skill_t2", 0.08f, 0.03f, 0.08f);
            Add("martial_skill_t4", "一击必杀", "寻找破绽，一击毙命", TechBranch.Martial, TechSubBranch.B, 4, 400, "martial_skill_t3", 0.15f, 0.04f, 0.10f);
            Add("martial_skill_t5", "武道宗师", "武道巅峰，万法归一", TechBranch.Martial, TechSubBranch.B, 5, 800, "martial_skill_t4", 0.25f, 0.05f, 0.15f);
            // 宗门分支
            Add("martial_sect_t2", "宗门建立", "建立武道宗门，传承武学", TechBranch.Martial, TechSubBranch.C, 2, 100, "martial_t1", 0.03f, 0.04f);
            Add("martial_sect_t3", "功法传承", "系统化功法传承，弟子实力提升", TechBranch.Martial, TechSubBranch.C, 3, 200, "martial_sect_t2", 0.05f, 0.06f);
            Add("martial_sect_t4", "武道联盟", "联合各宗门，形成武道联盟", TechBranch.Martial, TechSubBranch.C, 4, 400, "martial_sect_t3", 0.08f, 0.10f);
            Add("martial_sect_t5", "武林盟主", "武道界至尊，号令天下武者", TechBranch.Martial, TechSubBranch.C, 5, 800, "martial_sect_t4", 0.15f, 0.18f);

            // === 魔法系 ===
            Add("mage_t1", "奥术基础", "魔法的起点，掌握奥术能量", TechBranch.Mage, TechSubBranch.Common, 1, 50, null, 0.03f, 0.02f);
            // 元素分支
            Add("mage_element_t2", "元素掌控", "掌控火水风土四大元素", TechBranch.Mage, TechSubBranch.A, 2, 100, "mage_t1", 0.06f, 0.02f);
            Add("mage_element_t3", "元素融合", "融合多种元素，产生复合魔法", TechBranch.Mage, TechSubBranch.A, 3, 200, "mage_element_t2", 0.10f, 0.03f);
            Add("mage_element_t4", "元素风暴", "召唤毁天灭地的元素风暴", TechBranch.Mage, TechSubBranch.A, 4, 400, "mage_element_t3", 0.15f, 0.05f);
            Add("mage_element_t5", "元素主神", "元素法则的化身，掌控一切元素", TechBranch.Mage, TechSubBranch.A, 5, 800, "mage_element_t4", 0.25f, 0.08f);
            // 禁咒分支
            Add("mage_forbidden_t2", "法师塔", "建造法师塔，汇聚魔力", TechBranch.Mage, TechSubBranch.B, 2, 100, "mage_t1", 0.04f, 0.04f);
            Add("mage_forbidden_t3", "禁咒研究", "研究禁忌魔法，威力巨大", TechBranch.Mage, TechSubBranch.B, 3, 200, "mage_forbidden_t2", 0.08f, 0.06f);
            Add("mage_forbidden_t4", "位面封锁", "封锁空间，限制敌人移动", TechBranch.Mage, TechSubBranch.B, 4, 400, "mage_forbidden_t3", 0.12f, 0.10f);
            Add("mage_forbidden_t5", "魔法神域", "开辟魔法神域，法则由我定义", TechBranch.Mage, TechSubBranch.B, 5, 800, "mage_forbidden_t4", 0.20f, 0.15f);
            // 辅助分支
            Add("mage_support_t2", "附魔技术", "为装备附魔，提升属性", TechBranch.Mage, TechSubBranch.C, 2, 100, "mage_t1", 0.03f, 0.05f);
            Add("mage_support_t3", "传送门", "建立传送门，快速机动", TechBranch.Mage, TechSubBranch.C, 3, 200, "mage_support_t2", 0.04f, 0.08f, 0.10f);
            Add("mage_support_t4", "时空魔法", "操控时间与空间", TechBranch.Mage, TechSubBranch.C, 4, 400, "mage_support_t3", 0.06f, 0.12f, 0.15f);
            Add("mage_support_t5", "魔法议会", "联合法师，建立魔法议会", TechBranch.Mage, TechSubBranch.C, 5, 800, "mage_support_t4", 0.10f, 0.20f, 0.20f);

            // === 异能系 ===
            Add("psi_t1", "异能觉醒", "异能的起点，觉醒超能力", TechBranch.Psi, TechSubBranch.Common, 1, 50, null, 0.03f, 0.02f);
            // 精神分支
            Add("psi_mind_t2", "精神力训练", "强化精神力，提升异能强度", TechBranch.Psi, TechSubBranch.A, 2, 100, "psi_t1", 0.05f, 0.03f);
            Add("psi_mind_t3", "精神冲击", "直接攻击敌人精神", TechBranch.Psi, TechSubBranch.A, 3, 200, "psi_mind_t2", 0.08f, 0.05f);
            Add("psi_mind_t4", "精神控制", "控制敌人精神，使其倒戈", TechBranch.Psi, TechSubBranch.A, 4, 400, "psi_mind_t3", 0.12f, 0.08f);
            Add("psi_mind_t5", "心灵主宰", "精神域的主宰，掌控一切心灵", TechBranch.Psi, TechSubBranch.A, 5, 800, "psi_mind_t4", 0.20f, 0.12f);
            // 物质分支
            Add("psi_matter_t2", "物质操控", "操控物质，改变形态", TechBranch.Psi, TechSubBranch.B, 2, 100, "psi_t1", 0.05f, 0.04f);
            Add("psi_matter_t3", "元素异能", "操控元素，释放能量攻击", TechBranch.Psi, TechSubBranch.B, 3, 200, "psi_matter_t2", 0.09f, 0.06f);
            Add("psi_matter_t4", "领域展开", "展开个人领域，法则由我定义", TechBranch.Psi, TechSubBranch.B, 4, 400, "psi_matter_t3", 0.14f, 0.10f);
            Add("psi_matter_t5", "概念干涉", "干涉概念法则，改写现实", TechBranch.Psi, TechSubBranch.B, 5, 800, "psi_matter_t4", 0.22f, 0.15f);
            // 时空分支
            Add("psi_time_t2", "时空感知", "感知时空流动，预知危险", TechBranch.Psi, TechSubBranch.C, 2, 100, "psi_t1", 0.03f, 0.05f, 0.05f);
            Add("psi_time_t3", "短距传送", "短距离空间跳跃", TechBranch.Psi, TechSubBranch.C, 3, 200, "psi_time_t2", 0.05f, 0.08f, 0.10f);
            Add("psi_time_t4", "时间减速", "减速周围时间，敌人变慢", TechBranch.Psi, TechSubBranch.C, 4, 400, "psi_time_t3", 0.08f, 0.12f, 0.15f);
            Add("psi_time_t5", "时空扭曲", "扭曲时空，创造绝对领域", TechBranch.Psi, TechSubBranch.C, 5, 800, "psi_time_t4", 0.15f, 0.18f, 0.20f);

            // === 念力系 ===
            Add("mind_t1", "念力感知", "念力的起点，感知周围环境", TechBranch.Mind, TechSubBranch.Common, 1, 50, null, 0.02f, 0.03f);
            // 感知分支
            Add("mind_sense_t2", "远程感知", "超远距离感知，侦查敌情", TechBranch.Mind, TechSubBranch.A, 2, 100, "mind_t1", 0.02f, 0.05f);
            Add("mind_sense_t3", "预知未来", "短暂预知未来，规避危险", TechBranch.Mind, TechSubBranch.A, 3, 200, "mind_sense_t2", 0.04f, 0.08f);
            Add("mind_sense_t4", "全知视角", "上帝视角，洞察一切", TechBranch.Mind, TechSubBranch.A, 4, 400, "mind_sense_t3", 0.06f, 0.12f);
            Add("mind_sense_t5", "思维殿堂", "构建思维殿堂，记忆永不遗忘", TechBranch.Mind, TechSubBranch.A, 5, 800, "mind_sense_t4", 0.10f, 0.20f);
            // 控制分支
            Add("mind_control_t2", "心灵链接", "与他人建立心灵链接", TechBranch.Mind, TechSubBranch.B, 2, 100, "mind_t1", 0.03f, 0.04f);
            Add("mind_control_t3", "精神控制", "控制他人精神", TechBranch.Mind, TechSubBranch.B, 3, 200, "mind_control_t2", 0.06f, 0.06f);
            Add("mind_control_t4", "集体意识", "连接群体意识，协同作战", TechBranch.Mind, TechSubBranch.B, 4, 400, "mind_control_t3", 0.10f, 0.10f);
            Add("mind_control_t5", "思维主宰", "思维域的主宰，掌控集体意识", TechBranch.Mind, TechSubBranch.B, 5, 800, "mind_control_t4", 0.18f, 0.15f);
            // 攻击分支
            Add("mind_attack_t2", "念力冲击", "用念力直接攻击敌人", TechBranch.Mind, TechSubBranch.C, 2, 100, "mind_t1", 0.05f, 0.02f);
            Add("mind_attack_t3", "精神风暴", "释放精神风暴，范围伤害", TechBranch.Mind, TechSubBranch.C, 3, 200, "mind_attack_t2", 0.09f, 0.04f);
            Add("mind_attack_t4", "意识湮灭", "湮灭敌人意识，直接秒杀", TechBranch.Mind, TechSubBranch.C, 4, 400, "mind_attack_t3", 0.15f, 0.06f);
            Add("mind_attack_t5", "思维风暴", "思维风暴席卷战场，无差别毁灭", TechBranch.Mind, TechSubBranch.C, 5, 800, "mind_attack_t4", 0.25f, 0.10f);
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
            list.Sort((a, b) => a.tier != b.tier ? a.tier.CompareTo(b.tier) : a.subBranch.CompareTo(b.subBranch));
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
