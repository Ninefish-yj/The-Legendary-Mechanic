using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系三分支转职系统（数据层）。
    /// 核心逻辑：选分支=转职，选完才是那个职业。
    /// 机械系分支来自原著ch50；其他四系参考B站同人二创。
    /// 分支选择UI由超能者面板窗口(SuperMechPanel)处理，不再用神权。
    /// </summary>
    public static class SuperMechBranch
    {
        // —— 机械系三分支（原著ch50）——
        public const string BranchGunner = "sm_branch_gunner";
        public const string BranchMech = "sm_branch_mech";
        public const string BranchMartial = "sm_branch_martial";

        // —— 武道系三分支（同人二创）——
        public const string BranchMartialBody = "sm_branch_martial_body";
        public const string BranchMartialTactic = "sm_branch_martial_tactic";
        public const string BranchMartialPower = "sm_branch_martial_power";

        // —— 异能系三分支（同人二创）——
        public const string BranchPsiAttack = "sm_branch_psi_attack";
        public const string BranchPsiCycle = "sm_branch_psi_cycle";
        public const string BranchPsiFunc = "sm_branch_psi_func";

        // —— 魔法系三分支（同人二创）——
        // —— 魔法系：原著明确两类（专精法师/魔网法师），元素/变化/造物是知识树方向不是职业分支 ——
        public const string BranchMageSpecialist = "sm_branch_mage_specialist";
        public const string BranchMageWeave = "sm_branch_mage_weave";

        // —— 念力系三分支（同人二创）——
        public const string BranchMindSoul = "sm_branch_mind_soul";
        public const string BranchMindLaw = "sm_branch_mind_law";
        public const string BranchMindReality = "sm_branch_mind_reality";

        public struct BranchDef
        {
            public string traitId;
            public string name;
            public string classTrait;
            public string desc;
            public System.Action<BaseStats> applyBonus;
        }

        public static readonly List<BranchDef> AllBranches = new List<BranchDef>();

        public static void Register()
        {
            // 机械系
            AllBranches.Add(new BranchDef { traitId = BranchGunner, name = "枪炮师", classTrait = SuperMechTraits.ClassMech, desc = "远程重型枪械，伤害+30%攻速+20%射程+2", applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.3f; s["attack_speed"]=(s["attack_speed"]??0f)+0.2f; s["range"]=(s["range"]??0f)+2f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMech, name = "机械师", classTrait = SuperMechTraits.ClassMech, desc = "制造路线，制造+50%智力+10", applyBonus = s => { s["experience"]=(s["experience"]??1f)*1.5f; s["intelligence"]=(s["intelligence"]??0f)+10f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartial, name = "械武者", classTrait = SuperMechTraits.ClassMech, desc = "近战机械武器+纳米殖装，生命+40%护甲+5伤害+10", applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.4f; s["armor"]=(s["armor"]??0f)+5f; s["damage"]=(s["damage"]??0f)+10f; } });

            // 武道系
            AllBranches.Add(new BranchDef { traitId = BranchMartialBody, name = "体魄", classTrait = SuperMechTraits.ClassMartial, desc = "肉体强化，生命+50%耐力+10护甲+3", applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.5f; s["stamina"]=(s["stamina"]??0f)+20f; s["armor"]=(s["armor"]??0f)+3f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartialTactic, name = "战术", classTrait = SuperMechTraits.ClassMartial, desc = "战斗技巧，攻速+25%暴击+10%伤害+15%", applyBonus = s => { s["attack_speed"]=(s["attack_speed"]??0f)+0.25f; s["critical_chance"]=(s["critical_chance"]??0f)+0.1f; s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.15f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMartialPower, name = "超能", classTrait = SuperMechTraits.ClassMartial, desc = "气劲外放（离体波动/闪气/暴气），伤害+40%射程+3", applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.4f; s["range"]=(s["range"]??0f)+3f; s["damage"]=(s["damage"]??0f)+8f; } });

            // 异能系
            AllBranches.Add(new BranchDef { traitId = BranchPsiAttack, name = "攻效", classTrait = SuperMechTraits.ClassPsi, desc = "能级强化，伤害+50%暴击+5%", applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.5f; s["critical_chance"]=(s["critical_chance"]??0f)+0.05f; } });
            AllBranches.Add(new BranchDef { traitId = BranchPsiCycle, name = "循环", classTrait = SuperMechTraits.ClassPsi, desc = "持久力强化，生命+30%耐力+10", applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.3f; s["stamina"]=(s["stamina"]??0f)+25f; s["multiplier_stamina"]=(s["multiplier_stamina"]??1f)*1.2f; } });
            AllBranches.Add(new BranchDef { traitId = BranchPsiFunc, name = "功能", classTrait = SuperMechTraits.ClassPsi, desc = "操控强化，智力+15攻速+15%范围+2", applyBonus = s => { s["intelligence"]=(s["intelligence"]??0f)+15f; s["attack_speed"]=(s["attack_speed"]??0f)+0.15f; s["range"]=(s["range"]??0f)+2f; } });

            // 魔法系
            // 魔法系：专精法师（专注一种魔法+魔法回路，高威力）/魔网法师（契约借法，无消耗但有次数上限）
            AllBranches.Add(new BranchDef { traitId = BranchMageSpecialist, name = "专精法师", classTrait = SuperMechTraits.ClassMage, desc = "专注一种类型魔法，可植入魔法回路，伤害+50%智力+15", applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.5f; s["intelligence"]=(s["intelligence"]??0f)+15f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMageWeave, name = "魔网法师", classTrait = SuperMechTraits.ClassMage, desc = "与魔法实体契约借法，法力充沛，生命+30%经验+20%", applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.3f; s["experience"]=(s["experience"]??1f)*1.2f; s["mana"]=(s["mana"]??0f)+50f; } });

            // 念力系
            AllBranches.Add(new BranchDef { traitId = BranchMindSoul, name = "灵魂", classTrait = SuperMechTraits.ClassMind, desc = "心灵/灵魂，智力+20暴击+12%", applyBonus = s => { s["intelligence"]=(s["intelligence"]??0f)+20f; s["critical_chance"]=(s["critical_chance"]??0f)+0.12f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMindLaw, name = "法则", classTrait = SuperMechTraits.ClassMind, desc = "法则/因果，伤害+35%全属性+5", applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.35f; s["damage"]=(s["damage"]??0f)+5f; s["health"]=(s["health"]??0f)+15f; s["intelligence"]=(s["intelligence"]??0f)+5f; } });
            AllBranches.Add(new BranchDef { traitId = BranchMindReality, name = "现实", classTrait = SuperMechTraits.ClassMind, desc = "现实扭曲，生命+45%护甲+8伤害+12", applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.45f; s["armor"]=(s["armor"]??0f)+8f; s["damage"]=(s["damage"]??0f)+12f; } });

            // 只注册特质，不注册神权（分支选择由面板窗口处理）
            foreach (var b in AllBranches)
            {
                var t = new ActorTrait
                {
                    id = b.traitId,
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = "sm_branches",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                AssetManager.traits.add(t);
                LocalizedTextManager.add("trait_" + b.traitId, b.name, pReplace: true);
                LocalizedTextManager.add("trait_" + b.traitId + "_info", b.desc, pReplace: true);
            }

            Debug.Log($"[超神机械师] 五系分支特质注册完成：{AllBranches.Count}个");
        }

        /// <summary>获取单位所属系（中文名称）。</summary>
        public static string GetClass(Actor a)
        {
            if (a == null) return null;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return "机械系";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "武道系";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "异能系";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "魔法系";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "念力系";
            return null;
        }

        /// <summary>检查单位是否已有某系的任何分支。</summary>
        public static bool HasAnyBranch(Actor a, string classTrait)
        {
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait && a.hasTrait(b.traitId)) return true;
            }
            return false;
        }

        /// <summary>获取某系的所有分支定义。</summary>
        public static List<BranchDef> GetBranchesForClass(string classTrait)
        {
            var list = new List<BranchDef>();
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait) list.Add(b);
            }
            return list;
        }

        /// <summary>获取单位分支名（所有系）。</summary>
        public static string GetBranchName(Actor a)
        {
            if (a == null) return "";
            foreach (var b in AllBranches)
            {
                if (a.hasTrait(b.traitId)) return b.name;
            }
            return "未选择";
        }

        /// <summary>获取单位分支特质id（复活用）。</summary>
        public static string GetBranchTrait(Actor a)
        {
            if (a == null) return null;
            foreach (var b in AllBranches)
            {
                if (a.hasTrait(b.traitId)) return b.traitId;
            }
            return null;
        }
    }
}
