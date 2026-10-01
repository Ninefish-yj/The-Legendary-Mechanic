using System;
using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMRankWindowImgui : MonoBehaviour
    {
        private static SMRankWindowImgui _instance;
        private const int WINDOW_ID = 730504;

        private bool _visible;
        private Vector2 _scroll;
        private Rect _windowRect = new Rect(150, 60, 720, 650);
        private int _sortMode;

        private static readonly Color[] RankColors = {
            new Color(1f, 0.85f, 0.4f, 0.9f),
            new Color(0.85f, 0.9f, 0.95f, 0.8f),
            new Color(0.9f, 0.6f, 0.4f, 0.75f),
            new Color(0.8f, 0.85f, 0.9f, 0.6f)
        };

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMRankWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMRankWindowImgui>();
        }

        public static void Toggle()
        {
            Ensure();
            _instance._visible = !_instance._visible;
        }

        public static void Show()
        {
            Ensure();
            _instance._visible = true;
        }

        public static void Close()
        {
            if (_instance != null) _instance._visible = false;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            SMImguiTheme.Ensure();
            NormalizeRect();
            SMImguiTheme.DrawWindowBackground(_windowRect, SMImguiTheme.UiRank);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    "", SMImguiTheme.WindowStyle);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 排行榜窗口异常: " + e.Message);
                _visible = false;
            }
        }

        private void NormalizeRect()
        {
            float maxW = Mathf.Max(500f, Screen.width - 30f);
            float maxH = Mathf.Max(400f, Screen.height - 30f);
            _windowRect.width = Mathf.Min(Mathf.Max(650f, Screen.width * 0.55f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(500f, Screen.height * 0.75f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            try
            {
                DrawTitleBar();
                DrawSortButtons();
                GUILayout.Space(8);

                Rect contentRect = GUILayoutUtility.GetRect(_windowRect.width - 24, _windowRect.height - 120f);
                SMImguiTheme.DrawGlowBorder(contentRect, new Color(0.35f, 0.62f, 0.88f, 0.5f), 3f);

                GUILayout.BeginArea(new Rect(contentRect.x + 12, contentRect.y + 12, contentRect.width - 24, contentRect.height - 24));
                DrawRankList();
                GUILayout.EndArea();

                GUI.DragWindow(new Rect(0, 0, 10000, 36));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 排行榜内容异常: " + e.Message);
            }
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(8);
            GUILayout.Label("◀", SMImguiTheme.Label, GUILayout.Width(24));
            GUILayout.Label(LocalizedTextManager.getText("sm_rank_title"), SMImguiTheme.Title, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", SMImguiTheme.Button, GUILayout.Width(30), GUILayout.Height(26)))
            {
                _visible = false;
            }
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            Rect titleLine = new Rect(8, 36, _windowRect.width - 16, 1f);
            SMImguiTheme.DrawRect(titleLine, new Color(0.35f, 0.62f, 0.88f, 0.4f));
        }

        private void DrawSortButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            string[] sortKeys = { "sm_rank_sort_onar", "sm_rank_sort_qi", "sm_rank_sort_rank" };
            for (int i = 0; i < 3; i++)
            {
                bool active = _sortMode == i;
                string name = LocalizedTextManager.getText(sortKeys[i]);
                var style = active ? SMImguiTheme.TabActive : SMImguiTheme.Tab;
                if (GUILayout.Button(name, style, GUILayout.Height(28), GUILayout.MinWidth(90)))
                {
                    _sortMode = i;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawRankList()
        {
            string header = string.Format("{0,-4} {1,-16} {2,-6} {3,-6} {4,-5} {5,-8}",
                "#",
                LocalizedTextManager.getText("sm_ui_name"),
                LocalizedTextManager.getText("sm_ui_system"),
                LocalizedTextManager.getText("sm_ui_rank_col"),
                LocalizedTextManager.getText("sm_ui_qi_col"),
                LocalizedTextManager.getText("sm_ui_onar_col"));
            GUILayout.Label(header, SMImguiTheme.Small);
            GUILayout.Space(4);

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

            _scroll = GUILayout.BeginScrollView(_scroll);
            int count = Math.Min(list.Count, 50);
            for (int i = 0; i < count; i++)
            {
                var entry = list[i];
                string cls = GetClassShort(entry.a);
                string rank = SuperMechRanks.GetRankName(entry.a);
                int qiLv = SuperMechQi.GetLevel(SuperMechQi.GetQi(entry.a));

                Color rowColor = i < 3 ? RankColors[i] : RankColors[3];
                string rankPrefix = i < 3 ? GetRankIcon(i) : (i + 1).ToString().PadLeft(2) + ".";
                string nameTrunc = Truncate(entry.a.name, 14).PadRight(14);
                string line = rankPrefix + " " + nameTrunc + " " + cls.PadRight(5) + " " + rank.PadRight(5) + " Lv" + qiLv.ToString().PadRight(3) + " " + entry.onar.ToString("F0").PadLeft(8);

                var oldColor = GUI.color;
                GUI.color = rowColor;
                if (GUILayout.Button(line, SMImguiTheme.Button, GUILayout.Height(28)))
                {
                    MoveCamera.setFocusUnit(entry.a);
                }
                GUI.color = oldColor;
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            if (count == 0)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_rank_empty"), SMImguiTheme.Label);
            }
            else
            {
                GUILayout.Label($"{LocalizedTextManager.getText("sm_rank_total")} {list.Count} {LocalizedTextManager.getText("sm_rank_showing")} {Math.Min(count, list.Count)}",
                    SMImguiTheme.Small);
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
