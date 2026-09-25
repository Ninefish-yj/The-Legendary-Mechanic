using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 职业阶段追踪系统（内部数据，不做特质，只在单位面板显示）。
    ///
    /// 【原著设定】
    /// - 每个分支有独立的职业阶段名（ch175：枪炮师学徒/见习枪炮师，不是通用的"磁环机械师"）
    /// - 机械系原著完整14阶段（磁环→数据→战争→虚拟→星海→真理→使徒→帝皇→主宰→神座→超神）
    /// - 枪炮师高级职业叫【战争堡垒】（ch50），械武者可解锁【机甲操控师】（ch50）
    /// - 其他四系原著未逐一列出，按"每系同样结构"原则同人补全
    /// - 转职=选分支后自动晋升阶段，阶位(F→X)是能级境界，两套独立系统
    /// </summary>
    public static class SuperMechStage
    {
        // ===== 阶段前缀（14个，机械系来自原著，其他系同人补全）=====
        // 索引0-2：入门者/学徒/见习（前3阶段，各分支命名不同）
        // 索引3-13：高阶阶段前缀
        private static readonly string[] MechPrefixes =
        {
            "入门者", "学徒", "见习", "磁环", "数据", "战争", "虚拟", "星海", "真理", "使徒", "帝皇", "主宰", "神座", "超神"
        };
        private static readonly string[] MartialPrefixes =
        {
            "入门者", "学徒", "见习", "炼体", "格斗", "战技", "气劲", "宗师", "王者", "使徒", "帝皇", "主宰", "神座", "超神"
        };
        private static readonly string[] PsiPrefixes =
        {
            "入门者", "学徒", "见习", "基因链", "能力", "神通", "掌控", "宗师", "王者", "使徒", "帝皇", "主宰", "神座", "超神"
        };
        private static readonly string[] MagePrefixes =
        {
            "入门者", "学徒", "见习", "魔网", "符文", "魔导", "神权", "宗师", "王者", "使徒", "帝皇", "主宰", "神座", "超神"
        };
        private static readonly string[] MindPrefixes =
        {
            "入门者", "学徒", "见习", "精神", "念动", "神魂", "凝念", "宗师", "王者", "使徒", "帝皇", "主宰", "神座", "超神"
        };

        // ===== 分支后缀（选分支后，阶段名=前缀+后缀）=====
        // 机械系（原著ch50三分支）
        private const string MechSuffix_Gunner = "枪炮师";
        private const string MechSuffix_Mech = "机械师";
        private const string MechSuffix_Martial = "械武者";
        // 武道系（同人三分支）
        private const string MartialSuffix_Body = "武道家";
        private const string MartialSuffix_Tactic = "战道家";
        private const string MartialSuffix_Power = "气劲师";
        // 异能系（同人三分支）
        private const string PsiSuffix_Attack = "异能者";
        private const string PsiSuffix_Cycle = "控能者";
        private const string PsiSuffix_Func = "超能者";
        // 魔法系（原著两类：专精法师/魔网法师）
        private const string MageSuffix_Specialist = "专精法师";
        private const string MageSuffix_Weave = "魔网法师";
        // 念力系（同人三分支）
        private const string MindSuffix_Soul = "念魂师";
        private const string MindSuffix_Law = "念法师";
        private const string MindSuffix_Reality = "现实扭曲者";

        // 每阶段转职的气力奖励（原著原文确认）
        // ch3入门+10, ch50学徒+30, ch107见习+50, ch237磁环+70, ch362数据+100,
        // ch477战争+120, ch539虚拟+150, ch626星海+180, ch670真理+210, ch716使徒+240,
        // ch762帝皇+300, 主宰推断+360, 神座推断+450, ch1402超神+700
        public static readonly float[] StageQiBonus = { 10, 30, 50, 70, 100, 120, 150, 180, 210, 240, 300, 360, 450, 700 };

        // 每阶段每次升级的气力奖励（原著原文确认）
        public static readonly float[] LevelQiBonus = { 10, 20, 50, 80, 120, 120, 150, 180, 200, 220, 300, 360, 450, 700 };

        private static readonly Dictionary<long, int> _stage = new Dictionary<long, int>();

        /// <summary>获取单位所属系的阶段前缀数组。</summary>
        private static string[] GetPrefixArray(Actor a)
        {
            if (a == null) return MechPrefixes;
            if (a.hasTrait(SuperMechTraits.ClassMech)) return MechPrefixes;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return MartialPrefixes;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return PsiPrefixes;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return MagePrefixes;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return MindPrefixes;
            return MechPrefixes;
        }

        /// <summary>获取单位的分支后缀（未选分支返回系通用名）。</summary>
        private static string GetBranchSuffix(Actor a)
        {
            if (a == null) return "超能者";
            // 机械系
            if (a.hasTrait(SuperMechBranch.BranchGunner)) return MechSuffix_Gunner;
            if (a.hasTrait(SuperMechBranch.BranchMech)) return MechSuffix_Mech;
            if (a.hasTrait(SuperMechBranch.BranchMartial)) return MechSuffix_Martial;
            // 武道系
            if (a.hasTrait(SuperMechBranch.BranchMartialBody)) return MartialSuffix_Body;
            if (a.hasTrait(SuperMechBranch.BranchMartialTactic)) return MartialSuffix_Tactic;
            if (a.hasTrait(SuperMechBranch.BranchMartialPower)) return MartialSuffix_Power;
            // 异能系
            if (a.hasTrait(SuperMechBranch.BranchPsiAttack)) return PsiSuffix_Attack;
            if (a.hasTrait(SuperMechBranch.BranchPsiCycle)) return PsiSuffix_Cycle;
            if (a.hasTrait(SuperMechBranch.BranchPsiFunc)) return PsiSuffix_Func;
            // 魔法系
            if (a.hasTrait(SuperMechBranch.BranchMageSpecialist)) return MageSuffix_Specialist;
            if (a.hasTrait(SuperMechBranch.BranchMageWeave)) return MageSuffix_Weave;
            // 念力系
            if (a.hasTrait(SuperMechBranch.BranchMindSoul)) return MindSuffix_Soul;
            if (a.hasTrait(SuperMechBranch.BranchMindLaw)) return MindSuffix_Law;
            if (a.hasTrait(SuperMechBranch.BranchMindReality)) return MindSuffix_Reality;
            // 未选分支：返回系通用名
            if (a.hasTrait(SuperMechTraits.ClassMech)) return "机械师";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "武道家";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "异能者";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "法师";
            if (a.hasTrait(SuperMechTraits.ClassMind)) return "念力师";
            return "超能者";
        }

        /// <summary>获取单位职业阶段（0=未入门，1-14）。</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            if (_stage.TryGetValue(a.data.id, out int s)) return s;
            return 0;
        }

        /// <summary>获取单位职业阶段名称（按分支显示，原著ch175：枪炮师学徒/见习枪炮师）。</summary>
        public static string GetStageName(Actor a)
        {
            if (a == null) return "—";
            int s = GetStage(a);
            if (s <= 0) return "未入门";
            string[] prefixes = GetPrefixArray(a);
            string suffix = GetBranchSuffix(a);
            int idx = Mathf.Clamp(s - 1, 0, prefixes.Length - 1);

            // 前3阶段（入门者/学徒/见习）
            if (idx <= 2)
            {
                // 入门者：机械系入门者/武道系入门者...
                if (idx == 0)
                {
                    if (a.hasTrait(SuperMechTraits.ClassMech)) return "机械系入门者";
                    if (a.hasTrait(SuperMechTraits.ClassMartial)) return "武道系入门者";
                    if (a.hasTrait(SuperMechTraits.ClassPsi)) return "异能系入门者";
                    if (a.hasTrait(SuperMechTraits.ClassMage)) return "魔法系入门者";
                    if (a.hasTrait(SuperMechTraits.ClassMind)) return "念力系入门者";
                    return "超能系入门者";
                }
                // 学徒：枪炮师学徒/机械师学徒（ch175原著）
                if (idx == 1) return suffix + "学徒";
                // 见习：见习枪炮师/见习机械师（ch175原著）
                if (idx == 2) return "见习" + suffix;
            }

            // 高阶：前缀+后缀，如"磁环枪炮师""数据机械师"
            return prefixes[idx] + suffix;
        }

        /// <summary>设置单位职业阶段（转职时调用）。</summary>
        public static void SetStage(Actor a, int stage)
        {
            if (a == null) return;
            _stage[a.data.id] = Mathf.Clamp(stage, 0, 14);
        }

        /// <summary>晋升到下一阶段，返回是否成功。仅降临者（有面板）可转职。</summary>
        public static bool Advance(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechAwakened.IsAwakened(a)) return false; // 土著无职业阶段
            int cur = GetStage(a);
            if (cur >= 14) return false;
            string oldName = cur <= 0 ? "未入门" : GetStageName(a);
            SetStage(a, cur + 1);
            string newName = GetStageName(a);
            // 转职气力奖励（原著：转职后气力上限提升，当前值补满，ch50显示160/160）
            if (cur + 1 <= StageQiBonus.Length && StageQiBonus[cur] > 0)
            {
                SuperMechQi.AddQiMax(a, StageQiBonus[cur]);
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a)); // 转职后气力充盈
            }
            // 第4阶段觉醒气力属性（各系不同）
            if (cur + 1 == 4)
            {
                if (a.hasTrait(SuperMechTraits.ClassMech))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrMagnetic); // 械力
                else if (a.hasTrait(SuperMechTraits.ClassMartial))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrIron); // 铁属性
                else if (a.hasTrait(SuperMechTraits.ClassPsi))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrSpirit); // 精神属性
                else if (a.hasTrait(SuperMechTraits.ClassMage))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrLight); // 光属性
                else if (a.hasTrait(SuperMechTraits.ClassMind))
                    SuperMechQiAttribute.SetAttribute(a, SuperMechQiAttribute.AttrDark); // 暗属性
            }
            Debug.Log($"[超神机械师] {a.name} 职业晋升：{oldName} → {newName}");
            return true;
        }

        /// <summary>是否达到指定阶段。</summary>
        public static bool IsAtLeast(Actor a, int stage)
        {
            return GetStage(a) >= stage;
        }

        /// <summary>清除单位阶段数据（单位死亡时调用）。</summary>
        public static void Clear() { _stage.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) _stage.Remove(a.data.id);
        }
    }
}
