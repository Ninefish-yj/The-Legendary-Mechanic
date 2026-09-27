using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 超能者排行榜：按欧纳/气力/阶位排序，显示前20名。
    /// </summary>
    public static class SuperMechRankWindow
    {
        private static SMWindowFrame _frame;
        private static int _sortMode; // 0=欧纳 1=气力 2=阶位

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
                _frame = SMWindowFrame.Create(LocalizedTextManager.getText("sm_rank_title"), 640f, 700f);
                if (_frame == null) return;
                Debug.Log("[超神机械师] 排行榜窗口创建成功");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 排行榜初始化失败: " + e.Message);
            }
        }

        private static void Refresh()
        {
            if (_frame == null) return;
            _frame.ClearContent();
            float y = -8f;
            const float x = 12f;

            // 排序切换
            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_onar"), x, y, 90f, 26f, () => { _sortMode = 0; Refresh(); }, _sortMode == 0 ? new Color(0.2f, 0.4f, 0.7f) : (Color?)null);
            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_qi"), x + 95f, y, 90f, 26f, () => { _sortMode = 1; Refresh(); }, _sortMode == 1 ? new Color(0.2f, 0.4f, 0.7f) : (Color?)null);
            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_rank"), x + 190f, y, 90f, 26f, () => { _sortMode = 2; Refresh(); }, _sortMode == 2 ? new Color(0.2f, 0.4f, 0.7f) : (Color?)null);
            y -= 36f;

            _frame.AddLabel(LocalizedTextManager.getText("sm_rank_header"), x, y, 600f, 20f, 12);
            y -= 24f;

            // 收集所有觉醒单位
            var list = new List<(Actor a, float onar, float qi, int rank)>();
            if (World.world != null && World.world.units != null)
            {
                foreach (Actor a in World.world.units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                    list.Add((a, SuperMechAdvancement.CalcOnar(a), SuperMechQi.GetQi(a), SuperMechAdvancement.GetRankIndex(a)));
                }
            }

            // 排序
            switch (_sortMode)
            {
                case 0: list.Sort((a, b) => b.onar.CompareTo(a.onar)); break;
                case 1: list.Sort((a, b) => b.qi.CompareTo(a.qi)); break;
                case 2: list.Sort((a, b) => b.rank.CompareTo(a.rank)); break;
            }

            int count = Math.Min(list.Count, 20);
            for (int i = 0; i < count; i++)
            {
                var entry = list[i];
                string cls = GetClassShort(entry.a);
                string rank = SuperMechRanks.GetRankName(entry.a);
                int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQi(entry.a));

                Color rowColor = i < 3 ? new Color(0.15f, 0.12f, 0.05f, 0.6f) : new Color(0.08f, 0.08f, 0.12f, 0.5f);
                _frame.AddButton($"{i + 1,2}. {entry.a.name,-14} {cls,-4} {rank,-5} Lv{qiLv,-3} {entry.onar,8:F0}",
                    x, y, 600f, 26f, () => { MoveCamera.setFocusUnit(entry.a); }, rowColor);
                y -= 30f;
            }

            if (count == 0)
            {
                _frame.AddLabel(LocalizedTextManager.getText("sm_rank_empty"), x, y, 600f, 30f, 14, TextAnchor.MiddleCenter);
            }
            else
            {
                y -= 8f;
                _frame.AddLabel($"{LocalizedTextManager.getText(\"sm_rank_total\")} {list.Count} {LocalizedTextManager.getText(\"sm_rank_showing\")} {count}", x, y, 600f, 18f, 11);
            }
        }

        private static string GetClassShort(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return LocalizedTextManager.getText("sm_rank_class_mech");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_rank_class_martial");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_rank_class_psi");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_rank_class_mage");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_rank_class_mind");
            return "?";
        }
    }
}
