using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMKnowledgeWindowImgui : MonoBehaviour
    {
        private static SMKnowledgeWindowImgui _instance;
        private const int WINDOW_ID = 730501;

        private bool _visible;
        private Vector2 _scrollPos;
        private Vector2 _detailScroll;
        private Rect _windowRect = new Rect(100, 50, 900, 650);
        private static Actor _target;
        private static long _targetId = -1L;
        private int _selectedTier = -1;
        private string _selectedKnowledgeId = "";
        private string _message = "";

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMKnowledgeWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMKnowledgeWindowImgui>();
        }

        public static void Open(Actor actor)
        {
            Ensure();
            _target = actor;
            _targetId = actor != null ? actor.data.id : -1L;
            _instance._visible = true;
            _instance._selectedTier = -1;
            _instance._selectedKnowledgeId = "";
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
            SMImguiTheme.DrawRect(_windowRect, SMImguiTheme.Background);
            SMImguiTheme.DrawBorder(_windowRect, SMImguiTheme.Border, 2f);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    LocalizedTextManager.getText("sm_ui_knowledge_title"), SMImguiTheme.WindowStyle);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 知识窗口绘制异常: " + e.Message);
                _visible = false;
            }
        }

        private void NormalizeRect()
        {
            float maxW = Mathf.Max(600f, Screen.width - 30f);
            float maxH = Mathf.Max(400f, Screen.height - 30f);
            _windowRect.width = Mathf.Min(Mathf.Max(700f, Screen.width * 0.7f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(500f, Screen.height * 0.75f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            try
            {
                if (_target == null || _target.data == null || _target.data.id != _targetId)
                {
                    GUILayout.Label(LocalizedTextManager.getText("sm_ui_need_target"), SMImguiTheme.Muted);
                    GUI.DragWindow();
                    return;
                }

                DrawTopBar();
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                DrawTierList();
                GUILayout.Space(8);
                DrawKnowledgeList();
                GUILayout.Space(8);
                DrawDetailPanel();
                GUILayout.EndHorizontal();

                GUI.DragWindow(new Rect(0, 0, 10000, 28));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 知识窗口内容异常: " + e.Message);
            }
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();
            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(_target));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var all = SuperMechKnowledge.GetAllByPrefix(prefix);
            int unlocked = all.FindAll(k => SuperMechKnowledge.IsUnlocked(_target, k.id)).Count;
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_progress")}: {unlocked}/{all.Count}", SMImguiTheme.Small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", SMImguiTheme.Button, GUILayout.Width(28), GUILayout.Height(22)))
            {
                _visible = false;
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Label(_message, SMImguiTheme.Small);
            }
        }

        private void DrawTierList()
        {
            GUILayout.BeginVertical(SMImguiTheme.RaisedPanelStyle, GUILayout.Width(SMImguiTheme.Px(140)));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_tier"), SMImguiTheme.Section);
            GUILayout.Space(4);

            if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_all"),
                _selectedTier < 0 ? SMImguiTheme.TabActive : SMImguiTheme.Tab, GUILayout.Height(26)))
            {
                _selectedTier = -1;
                _selectedKnowledgeId = "";
            }

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(_target));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var all = SuperMechKnowledge.GetAllByPrefix(prefix);
            var tiers = new HashSet<int>();
            foreach (var k in all) tiers.Add(k.tier);

            Color[] tierColors = {
                new Color(0.3f, 0.71f, 0.67f),
                new Color(0.15f, 0.65f, 0.6f),
                new Color(0f, 0.54f, 0.48f),
                new Color(0f, 0.47f, 0.42f),
                new Color(0f, 0.3f, 0.25f)
            };

            for (int t = 0; t <= 4; t++)
            {
                if (!tiers.Contains(t)) continue;
                int tierCount = all.FindAll(k => k.tier == t).Count;
                int tierUnlocked = all.FindAll(k => k.tier == t && SuperMechKnowledge.IsUnlocked(_target, k.id)).Count;
                string label = $"{LocalizedTextManager.getText("sm_ui_tier")} {t + 1} ({tierUnlocked}/{tierCount})";
                var oldColor = GUI.color;
                GUI.color = tierColors[t];
                bool clicked = GUILayout.Button(label,
                    _selectedTier == t ? SMImguiTheme.TabActive : SMImguiTheme.Tab, GUILayout.Height(26));
                GUI.color = oldColor;
                if (clicked)
                {
                    _selectedTier = t;
                    _selectedKnowledgeId = "";
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void DrawKnowledgeList()
        {
            GUILayout.BeginVertical(SMImguiTheme.PanelStyle, GUILayout.Width(SMImguiTheme.Px(260)));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_knowledge_list"), SMImguiTheme.Section);
            GUILayout.Space(4);

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(_target));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var all = SuperMechKnowledge.GetAllByPrefix(prefix);
            var filtered = _selectedTier >= 0 ? all.FindAll(k => k.tier == _selectedTier) : all;

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            foreach (var k in filtered)
            {
                bool unlocked = SuperMechKnowledge.IsUnlocked(_target, k.id);
                bool selected = _selectedKnowledgeId == k.id;
                string label = (unlocked ? "✓ " : "○ ") + k.id;
                var style = selected ? SMImguiTheme.PrimaryButton : (unlocked ? SMImguiTheme.Button : SMImguiTheme.Tab);
                if (GUILayout.Button(label, style, GUILayout.Height(24)))
                {
                    _selectedKnowledgeId = k.id;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawDetailPanel()
        {
            GUILayout.BeginVertical(SMImguiTheme.RaisedPanelStyle);
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_detail"), SMImguiTheme.Section);
            GUILayout.Space(4);

            if (string.IsNullOrEmpty(_selectedKnowledgeId))
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_select_knowledge"), SMImguiTheme.Muted);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(_target));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var all = SuperMechKnowledge.GetAllByPrefix(prefix);
            var def = all.Find(k => k.id == _selectedKnowledgeId);
            if (def == null)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_not_found"), SMImguiTheme.Muted);
                GUILayout.EndVertical();
                return;
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Label(def.id, SMImguiTheme.Title);
            GUILayout.Space(4);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_tier")}: {def.tier + 1}", SMImguiTheme.Label);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_unlocked")}: {(SuperMechKnowledge.IsUnlocked(_target, def.id) ? "✓" : "✗")}", SMImguiTheme.Label);
            GUILayout.Space(6);
            GUILayout.Label(def.desc, SMImguiTheme.WrappedLabel);
            GUILayout.Space(8);

            if (def.effects != null && def.effects.Count > 0)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_effects"), SMImguiTheme.Section);
                foreach (var eff in def.effects)
                {
                    GUILayout.Label($"  • {eff}", SMImguiTheme.Small);
                }
                GUILayout.Space(6);
            }

            if (!SuperMechKnowledge.IsUnlocked(_target, def.id))
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unlock"), SMImguiTheme.PrimaryButton, GUILayout.Height(30)))
                {
                    bool success = SuperMechKnowledge.Unlock(_target, def.id);
                    _message = success ?
                        $"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_unlock_success")}</color>" :
                        $"<color=#E35D6A>{LocalizedTextManager.getText("sm_ui_unlock_fail")}</color>";
                }
            }
            else
            {
                GUILayout.Label($"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_already_unlocked")}</color>", SMImguiTheme.Label);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
