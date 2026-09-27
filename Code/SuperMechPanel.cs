using System;
using System.Linq;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 超能者面板窗口：整合所有单位操作（觉醒/分支/制造/知识/修炼/查看）。
    /// 整合所有单位操作的窗口界面。
    /// 自动读取 MoveCamera.getFocusUnit() 当前选中单位。
    /// </summary>
    public static class SuperMechPanel
    {
        private static SMWindowFrame _frame;
        private static Actor _currentUnit;
        private static float _refreshTimer;

        // 五系定义
        private static readonly (string trait, string name, Color color)[] Classes =
        {
            (SuperMechTraits.ClassMech, "sm_panel_1200", new Color(0.3f, 0.5f, 0.9f)),
            (SuperMechTraits.ClassMartial, "sm_panel_1201", new Color(0.9f, 0.3f, 0.3f)),
            (SuperMechTraits.ClassPsi, "sm_panel_1202", new Color(0.3f, 0.9f, 0.5f)),
            (SuperMechTraits.ClassMage, "sm_panel_1203", new Color(0.7f, 0.3f, 0.9f)),
            (SuperMechTraits.ClassMind, "sm_panel_1204", new Color(0.9f, 0.7f, 0.3f)),
        };

        public static void Show()
        {
            if (_frame == null) Init();
            if (_frame == null) return;
            _frame.Show();
            Refresh();
        }

        private static void Init()
        {
            try
            {
                _frame = SMWindowFrame.Create("sm_panel_1205", 720f, 640f);
                if (_frame == null) return;
                Debug.Log("[超神机械师] 超能者面板窗口创建成功");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 超能者面板初始化失败: " + e.Message);
            }
        }

        /// <summary>每帧检测单位变化，自动刷新。</summary>
        public static void Tick()
        {
            if (_frame == null || !_frame.IsVisible) return;
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < 0.5f) return;
            _refreshTimer = 0f;

            Actor focus = MoveCamera.getFocusUnit();
            if (focus != _currentUnit)
            {
                _currentUnit = focus;
                Refresh();
            }
        }

        private static void Refresh()
        {
            if (_frame == null) return;
            _frame.ClearContent();
            Actor a = MoveCamera.getFocusUnit();
            _currentUnit = a;

            float y = -8f;
            const float leftX = 12f;
            const float colW = 340f;

            if (a == null || !a.isAlive())
            {
                _frame.AddLabel("sm_panel_1206", leftX, y, 680f, 30f, 16, TextAnchor.MiddleCenter);
                return;
            }

            // —— 单位基本信息 ——
            string className = GetClassName(a);
            _frame.AddLabel($"【{a.name}】 {className}", leftX, y, 680f, 28f, 18, TextAnchor.MiddleLeft);
            y -= 32f;

            // 阶位/职业阶段/气力/欧纳
            string rank = SuperMechRanks.GetRankName(a);
            string stage = SuperMechStage.GetStageName(a);
            int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQi(a));
            float qiVal = SuperMechQi.GetQi(a);
            float onar = SuperMechAdvancement.CalcOnar(a);
            string branch = SuperMechBranch.GetBranchName(a);

            _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_rank")}: {rank}    {LocalizedTextManager.getText("sm_panel_stage")}: {stage}", leftX, y, colW, 22f, 13);
            y -= 24f;
            string qiAttr = SuperMechQiAttribute.GetAttribute(a);
            if (qiAttr != SuperMechQiAttribute.AttrNone)
                _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_qi_attr")}: {qiAttr}", leftX, y, colW, 22f, 13);
            _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_qi")}: Lv{qiLv} ({qiVal:F0})    {LocalizedTextManager.getText("sm_panel_onar")}: {onar:F0}", leftX + colW, y, colW, 22f, 13);
            y -= 26f;

            _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_branch")}: {branch}    {LocalizedTextManager.getText("sm_panel_potential")}: {SuperMechPotential.GetPotential(a)}", leftX, y, colW, 22f, 13);
            string subText = SuperMechSubClass.GetSubLevelText(a);
            _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_subclass")}: {(string.IsNullOrEmpty(subText)?LocalizedTextManager.getText("sm_panel_none"):subText)}", leftX + colW, y, colW, 22f, 13);
            y -= 26f;

            string relic = SuperMechRelic.GetCurrentEquipName(a);
            bool divine = SuperMechDivinity.IsDivineAwakened(a);
            _frame.AddLabel($"{LocalizedTextManager.getText("sm_panel_relic")}: {relic}    {LocalizedTextManager.getText("sm_panel_divinity")}: {(divine?LocalizedTextManager.getText("sm_panel_divine_yes"):LocalizedTextManager.getText("sm_panel_divine_no"))}", leftX, y, 680f, 22f, 13);
            y -= 34f;

            // —— 分割线 ——
            _frame.AddLabel("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", leftX, y, 680f, 16f, 10);
            y -= 22f;

            // —— 觉醒区（未觉醒时显示5系按钮）——
            if (!SuperMechAdvancement.IsSuperMechUnit(a))
            {
                _frame.AddLabel("sm_panel_1207", leftX, y, 200f, 22f, 14);
                y -= 28f;
                for (int i = 0; i < Classes.Length; i++)
                {
                    int idx = i;
                    float bx = leftX + (i % 3) * 115f;
                    if (i >= 3) bx = leftX + (i - 3) * 115f;
                    _frame.AddButton(Classes[idx].name, bx, y, 105f, 30f, () =>
                    {
                        a.addTrait(Classes[idx].trait);
                        SuperMechSpecialty.AssignRandomSpecialty(a);
                        SuperMechQiAttribute.AutoAssign(a);
                        // 异能系觉醒时随机潜力评级（原著ch48：EDCBAS）
                        if (Classes[idx].trait == SuperMechTraits.ClassPsi)
                        {
                            SuperMechPotentialRating.RollRating(a);
                        }
                        Debug.Log($"[超神机械师] {a.name} 觉醒 {Classes[idx].name}");
                        Refresh();
                    }, Classes[idx].color);
                }
                y -= 40f;
            }
            else
            {
                // —— 分支选择（已觉醒未选分支时）——
                if (branch == "sm_panel_1208" && CanSelectBranch(a))
                {
                    _frame.AddLabel("sm_panel_1209", leftX, y, 200f, 22f, 14);
                    y -= 28f;
                    var branches = SuperMechBranch.GetBranchesForClass(GetClassTrait(a));
                    for (int i = 0; i < branches.Count; i++)
                    {
                        int idx = i;
                        _frame.AddButton(branches[idx].name, leftX + i * 115f, y, 105f, 30f, () =>
                        {
                            a.addTrait(branches[idx].traitId);
                            // 属性加成已在特质base_stats中，addTrait时自动生效
                            // 机械系：选完分支自动晋升磁环
                            if (a.hasTrait(SuperMechTraits.ClassMech) && SuperMechStage.GetStage(a) == 3)
                            {
                                SuperMechStage.SetStage(a, 4);
                            }
                            SuperMechQiAttribute.AutoAssign(a);
                            // 选择分支后获得对应的特色能力（原著：其他四系的特色是后天学习的职业技能）
                            SuperMechSpecialty.GrantBranchSpecialty(a, branches[idx].traitId);
                            Debug.Log($"[超神机械师] {a.name} 转职 {branches[idx].name}");
                            Refresh();
                        });
                    }
                    y -= 40f;
                }

                // —— 机械系制造 ——
                if (a.hasTrait(SuperMechTraits.ClassMech))
                {
                    _frame.AddLabel("sm_panel_1210", leftX, y, 200f, 22f, 14);
                    y -= 28f;
                    string[] crafts = { "sm_panel_1211", "sm_panel_1212", "sm_panel_1213", "sm_panel_1214", "sm_panel_1215" };
                    string[] craftIds = { "sm_craft_ranger", "sm_craft_drone", "sm_craft_mech", "sm_craft_fortress", "sm_craft_virtual" };
                    for (int i = 0; i < crafts.Length; i++)
                    {
                        int idx = i;
                        _frame.AddButton(crafts[idx], leftX + (idx % 3) * 115f, y - (idx / 3) * 34f, 105f, 28f, () =>
                        {
                            var p = AssetManager.powers.get(craftIds[idx]);
                            if (p != null) p.click_action?.Invoke(a.current_tile, craftIds[idx]);
                            Refresh();
                        });
                    }
                    y -= (crafts.Length > 3 ? 68f : 34f);
                    y -= 10f;
                }

                // —— 知识解锁 ——
                _frame.AddLabel("sm_panel_1216", leftX, y, 280f, 22f, 14);
                y -= 28f;
                string[] knowNames = { "sm_panel_1217", "sm_panel_1218", "sm_panel_1219" };
                string[] knowIds = { SuperMechPowers.UnlockArmed, SuperMechPowers.UnlockEnergy, SuperMechPowers.UnlockVirtual };
                for (int i = 0; i < knowNames.Length; i++)
                {
                    int idx = i;
                    _frame.AddButton(knowNames[idx], leftX + idx * 115f, y, 105f, 28f, () =>
                    {
                        var p = AssetManager.powers.get(knowIds[idx]);
                        if (p != null) p.click_action?.Invoke(a.current_tile, knowIds[idx]);
                        Refresh();
                    });
                }
                y -= 38f;

                // —— 提炼法（统一入口，按系别自动传授对应变种）——
                _frame.AddLabel("sm_panel_1220", leftX, y, 200f, 22f, 14);
                y -= 28f;
                _frame.AddButton("sm_panel_1221", leftX, y, 200f, 28f, () =>
                {
                    var p = AssetManager.powers.get("sm_give_refinement");
                    if (p != null) p.click_action?.Invoke(a.current_tile, "sm_give_refinement");
                    Refresh();
                });
                y -= 38f;

                // —— 其他操作 ——
                _frame.AddLabel("sm_panel_1222", leftX, y, 200f, 22f, 14);
                y -= 28f;
                _frame.AddButton("sm_panel_1223", leftX, y, 145f, 28f, () =>
                {
                    var p = AssetManager.powers.get(SuperMechPowers.CheckPotential);
                    if (p != null) p.click_action?.Invoke(a.current_tile, SuperMechPowers.CheckPotential);
                });
                _frame.AddButton("sm_panel_1224", leftX + 155f, y, 105f, 28f, () =>
                {
                    var p = AssetManager.powers.get("sm_enter_sanctuary");
                    if (p != null) p.click_action?.Invoke(a.current_tile, "sm_enter_sanctuary");
                    Refresh();
                });
            }
        }

        private static string GetClassName(Actor a)
        {
            foreach (var c in Classes)
            {
                if (a.hasTrait(c.trait)) return c.name;
            }
            return "sm_panel_1225";
        }

        private static string GetClassTrait(Actor a)
        {
            foreach (var c in Classes)
            {
                if (a.hasTrait(c.trait)) return c.trait;
            }
            return null;
        }

        private static bool CanSelectBranch(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech))
                return SuperMechStage.GetStage(a) >= 3;  // 见习机械师
            return SuperMechAdvancement.GetRankIndex(a) >= 2;  // D阶
        }

    }
}
