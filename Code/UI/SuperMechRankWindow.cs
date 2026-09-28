using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechRankWindow
    {
        private static SMWindowFrame _frame;
        private static int _sortMode;

        private static readonly Color[] RankColors = {
            new Color(0.95f, 0.8f, 0.35f, 0.9f),
            new Color(0.8f, 0.85f, 0.9f, 0.8f),
            new Color(0.85f, 0.55f, 0.35f, 0.75f),
            new Color(0.1f, 0.15f, 0.25f, 0.6f)
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
                _frame = SMWindowFrame.Create(LocalizedTextManager.getText("sm_rank_title"), 680f, 720f);
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
            float width = 640f;

            _frame.AddLabel(LocalizedTextManager.getText("sm_rank_subtitle"), x, y, width, 24f, 13, TextAnchor.MiddleCenter);
            y -= 30f;

            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_onar"), x, y, 100f, 28f, () => { _sortMode = 0; Refresh(); }, _sortMode == 0 ? SMImguiTheme.Cyan : (Color?)null);
            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_qi"), x + 105f, y, 100f, 28f, () => { _sortMode = 1; Refresh(); }, _sortMode == 1 ? SMImguiTheme.Cyan : (Color?)null);
            _frame.AddButton(LocalizedTextManager.getText("sm_rank_sort_rank"), x + 210f, y, 100f, 28f, () => { _sortMode = 2; Refresh(); }, _sortMode == 2 ? SMImguiTheme.Cyan : (Color?)null);
            y -= 38f;

            string header = string.Format("{0,-4} {1,-16} {2,-6} {3,-6} {4,-5} {5,-8}",
                "#",
                LocalizedTextManager.getText("sm_ui_name"),
                LocalizedTextManager.getText("sm_ui_system"),
                LocalizedTextManager.getText("sm_ui_rank_col"),
                LocalizedTextManager.getText("sm_ui_qi_col"),
                LocalizedTextManager.getText("sm_ui_onar_col"));
            _frame.AddLabel(header, x, y, width, 22f, 12, TextAnchor.MiddleLeft, SMImguiTheme.Cyan);
            y -= 26f;

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

                Color rowColor = i < 3 ? RankColors[i] : RankColors[3];
                string rankPrefix = i < 3 ? GetRankIcon(i) : $"{i + 1,2}.";
                string line = $"{rankPrefix} {Truncate(entry.a.name, 14),-14} {cls,-5} {rank,-5} Lv{qiLv,-3} {entry.onar,8:F0}";

                _frame.AddButton(line, x, y, width, 28f, () => { MoveCamera.setFocusUnit(entry.a); }, rowColor);
                y -= 32f;
            }

            if (count == 0)
            {
                _frame.AddLabel(LocalizedTextManager.getText("sm_rank_empty"), x, y, width, 40f, 14, TextAnchor.MiddleCenter);
            }
            else
            {
                y -= 6f;
                _frame.AddLabel($"{LocalizedTextManager.getText("sm_rank_total")} {list.Count} {LocalizedTextManager.getText("sm_rank_showing")} {count}",
                    x, y, width, 18f, 11, TextAnchor.MiddleLeft, SMImguiTheme.TextDim);
            }
        }

        private static string GetRankIcon(int rank)
        {
            switch (rank)
            {
                case 0: return "①";
                case 1: return "②";
                case 2: return "③";
                default: return $"{rank + 1}.";
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > max ? s.Substring(0, max) + ".." : s;
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
