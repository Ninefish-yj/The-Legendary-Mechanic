using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系三分支选择系统。
    /// 机械系分支来自原著ch50（枪炮师/机械师/械武者）。
    /// 其他四系分支参考B站同人二创《五大职业全套知识二创补全》：
    ///   武道=体魄/战术/超能，异能=攻效/循环/功能，
    ///   魔法=元素/变化/造物，念力=灵魂/法则/现实。
    /// 其中武道【离体波动/闪气/暴气】、异能【二阶基因链能级/操控/持久力强化】为原著明确出现。
    /// 转职后其他分支知识潜能点费用×3（ch611）。
    /// </summary>
    public static class SuperMechBranch
    {
        // —— 机械系三分支（原著ch50）——
        public const string BranchGunner = "sm_branch_gunner";     // 枪炮师（武装）
        public const string BranchMech = "sm_branch_mech";         // 机械师（能量）
        public const string BranchMartial = "sm_branch_martial";   // 械武者（操控）

        // —— 武道系三分支（同人二创，含原著知识）——
        public const string BranchMartialBody = "sm_branch_martial_body";     // 体魄
        public const string BranchMartialTactic = "sm_branch_martial_tactic"; // 战术
        public const string BranchMartialPower = "sm_branch_martial_power";   // 超能（离体波动/闪气/暴气）

        // —— 异能系三分支（同人二创，含原著知识）——
        public const string BranchPsiAttack = "sm_branch_psi_attack";   // 攻效（能级强化）
        public const string BranchPsiCycle = "sm_branch_psi_cycle";     // 循环（持久力强化）
        public const string BranchPsiFunc = "sm_branch_psi_func";       // 功能（操控强化）

        // —— 魔法系三分支（同人二创）——
        public const string BranchMageElement = "sm_branch_mage_element";   // 元素
        public const string BranchMageChange = "sm_branch_mage_change";     // 变化
        public const string BranchMageCreate = "sm_branch_mage_create";     // 造物

        // —— 念力系三分支（同人二创）——
        public const string BranchMindSoul = "sm_branch_mind_soul";     // 灵魂
        public const string BranchMindLaw = "sm_branch_mind_law";       // 法则
        public const string BranchMindReality = "sm_branch_mind_reality"; // 现实

        // 分支定义表：(特质ID, 显示名, 所属系特质, 描述, 属性加成委托)
        private struct BranchDef
        {
            public string traitId;
            public string name;
            public string classTrait;
            public string desc;
            public System.Action<BaseStats> applyBonus;
        }

        private static readonly List<BranchDef> AllBranches = new List<BranchDef>();

        public static void Register()
        {
            // 机械系（需磁环tier4）
            AllBranches.Add(new BranchDef {
                traitId = BranchGunner, name = "枪炮师", classTrait = SuperMechTraits.ClassMech,
                desc = "远程重型枪械，高级职业【战争堡垒】，伤害+30%攻速+20%射程+2",
                applyBonus = s => { s["multiplier_damage"] = (s["multiplier_damage"]??1f)*1.3f; s["attack_speed"]=(s["attack_speed"]??0f)+0.2f; s["range"]=(s["range"]??0f)+2f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMech, name = "机械师", classTrait = SuperMechTraits.ClassMech,
                desc = "制造路线，80%图纸前置，制造+50%智力+10",
                applyBonus = s => { s["experience"] = (s["experience"]??1f)*1.5f; s["intelligence"]=(s["intelligence"]??0f)+10f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMartial, name = "械武者", classTrait = SuperMechTraits.ClassMech,
                desc = "近战机械武器+纳米殖装，生命+40%护甲+5伤害+10",
                applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.4f; s["armor"]=(s["armor"]??0f)+5f; s["damage"]=(s["damage"]??0f)+10f; }
            });

            // 武道系（需D阶以上）
            AllBranches.Add(new BranchDef {
                traitId = BranchMartialBody, name = "体魄", classTrait = SuperMechTraits.ClassMartial,
                desc = "肉体强化路线，生命+50%耐力+10护甲+3",
                applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.5f; s["stamina"]=(s["stamina"]??0f)+20f; s["armor"]=(s["armor"]??0f)+3f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMartialTactic, name = "战术", classTrait = SuperMechTraits.ClassMartial,
                desc = "战斗技巧路线，攻速+25%暴击+10%伤害+15%",
                applyBonus = s => { s["attack_speed"]=(s["attack_speed"]??0f)+0.25f; s["critical_chance"]=(s["critical_chance"]??0f)+0.1f; s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.15f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMartialPower, name = "超能", classTrait = SuperMechTraits.ClassMartial,
                desc = "气劲外放路线（离体波动/闪气/暴气），伤害+40%射程+3",
                applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.4f; s["range"]=(s["range"]??0f)+3f; s["damage"]=(s["damage"]??0f)+8f; }
            });

            // 异能系（需D阶以上）
            AllBranches.Add(new BranchDef {
                traitId = BranchPsiAttack, name = "攻效", classTrait = SuperMechTraits.ClassPsi,
                desc = "能级强化路线，伤害+50%暴击+5%",
                applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.5f; s["critical_chance"]=(s["critical_chance"]??0f)+0.05f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchPsiCycle, name = "循环", classTrait = SuperMechTraits.ClassPsi,
                desc = "持久力强化路线，生命+30%气力恢复+100%耐力+10",
                applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.3f; s["stamina"]=(s["stamina"]??0f)+25f; s["multiplier_stamina"]=(s["multiplier_stamina"]??1f)*1.2f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchPsiFunc, name = "功能", classTrait = SuperMechTraits.ClassPsi,
                desc = "操控强化路线，智力+15攻速+15%范围+2",
                applyBonus = s => { s["intelligence"]=(s["intelligence"]??0f)+15f; s["attack_speed"]=(s["attack_speed"]??0f)+0.15f; s["range"]=(s["range"]??0f)+2f; }
            });

            // 魔法系（需D阶以上）
            AllBranches.Add(new BranchDef {
                traitId = BranchMageElement, name = "元素", classTrait = SuperMechTraits.ClassMage,
                desc = "元素魔法路线，伤害+45%暴击+8%",
                applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.45f; s["critical_chance"]=(s["critical_chance"]??0f)+0.08f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMageChange, name = "变化", classTrait = SuperMechTraits.ClassMage,
                desc = "变化/奥术路线，攻速+30%移速+20%智力+10",
                applyBonus = s => { s["attack_speed"]=(s["attack_speed"]??0f)+0.3f; s["speed"]=(s["speed"]??0f)+0.5f; s["intelligence"]=(s["intelligence"]??0f)+10f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMageCreate, name = "造物", classTrait = SuperMechTraits.ClassMage,
                desc = "炼金/造物路线，生命+35%护甲+5经验+30%",
                applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.35f; s["armor"]=(s["armor"]??0f)+5f; s["experience"]=(s["experience"]??1f)*1.3f; }
            });

            // 念力系（需D阶以上）
            AllBranches.Add(new BranchDef {
                traitId = BranchMindSoul, name = "灵魂", classTrait = SuperMechTraits.ClassMind,
                desc = "心灵/灵魂路线，智力+20暴击+12%",
                applyBonus = s => { s["intelligence"]=(s["intelligence"]??0f)+20f; s["critical_chance"]=(s["critical_chance"]??0f)+0.12f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMindLaw, name = "法则", classTrait = SuperMechTraits.ClassMind,
                desc = "法则/因果路线，伤害+35%全属性+5",
                applyBonus = s => { s["multiplier_damage"]=(s["multiplier_damage"]??1f)*1.35f; s["damage"]=(s["damage"]??0f)+5f; s["health"]=(s["health"]??0f)+15f; s["intelligence"]=(s["intelligence"]??0f)+5f; }
            });
            AllBranches.Add(new BranchDef {
                traitId = BranchMindReality, name = "现实", classTrait = SuperMechTraits.ClassMind,
                desc = "现实扭曲路线，生命+45%护甲+8伤害+12",
                applyBonus = s => { s["multiplier_health"]=(s["multiplier_health"]??1f)*1.45f; s["armor"]=(s["armor"]??0f)+8f; s["damage"]=(s["damage"]??0f)+12f; }
            });

            // 注册所有分支特质和神权
            foreach (var b in AllBranches)
            {
                RegisterBranchTrait(b);
                AddBranchPower(b);
            }

            Debug.Log($"[超神机械师] 五系分支系统注册完成：{AllBranches.Count}个分支（机械3+武道3+异能3+魔法3+念力3）");
        }

        private static void RegisterBranchTrait(BranchDef b)
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

        private static void AddBranchPower(BranchDef b)
        {
            string powerId = "sm_select_" + b.traitId.Replace("sm_branch_", "");
            var p = new GodPower
            {
                id = powerId,
                name = "选择分支·" + b.name,
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                bool applied = false;
                tile.doUnits(u =>
                {
                    if (u == null || applied) return;
                    if (!u.hasTrait(b.classTrait)) return;

                    // 机械系需磁环tier4，其他系需D阶以上
                    if (b.classTrait == SuperMechTraits.ClassMech)
                    {
                        if (GetMechStageTier(u) < 4) return;
                    }
                    else
                    {
                        if (SuperMechAdvancement.GetRankIndex(u) < 2) return;  // D阶=index2
                    }

                    // 已有同系分支则不能重选
                    if (HasAnyBranch(u, b.classTrait)) return;

                    u.addTrait(b.traitId);
                    var stats = SuperMechStats.Of(u);
                    if (stats != null) b.applyBonus(stats);
                    applied = true;
                    Debug.Log($"[超神机械师] {u.Name} 选择分支：{b.name}");
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }

        /// <summary>检查单位是否已有某系的任何分支。</summary>
        private static bool HasAnyBranch(Actor a, string classTrait)
        {
            foreach (var b in AllBranches)
            {
                if (b.classTrait == classTrait && a.hasTrait(b.traitId)) return true;
            }
            return false;
        }

        /// <summary>获取机械师阶段tier（1-14）。</summary>
        private static int GetMechStageTier(Actor a)
        {
            string[] stages =
            {
                SuperMechTraits.MechInitiate, SuperMechTraits.MechApprentice,
                SuperMechTraits.MechTrainee, SuperMechTraits.MechMagnet,
                SuperMechTraits.MechData, SuperMechTraits.MechWar,
                SuperMechTraits.MechVirtual, SuperMechTraits.MechStarsea,
                SuperMechTraits.MechTruth, SuperMechTraits.MechApostle,
                SuperMechTraits.MechEmperor, SuperMechTraits.MechLord,
                SuperMechTraits.MechGod, SuperMechTraits.MechSupreme,
            };
            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(stages[i])) return i + 1;
            }
            return 0;
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
    }
}
