using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMKnowledgeWindowImgui : MonoBehaviour
    {
        private static SMKnowledgeWindowImgui _instance;
        private const int WINDOW_ID = 730501;

        private bool _visible;
        private Vector2 _detailScroll;
        private Rect _windowRect = new Rect(100, 50, 1000, 700);
        private static Actor _target;
        private static long _targetId = -1L;
        private string _currentPrefix = "mech";
        private string _message = "";
        private SMKnowledgeGraph _graph;
        private bool _graphBuilt;
        private int _selectedTab = 0;

        private static readonly string[] _prefixes = { "mech", "martial", "mage", "mind", "psi" };
        private static readonly string[] _prefixNames = { "sm_tree_mech", "sm_tree_martial", "sm_tree_mage", "sm_tree_mind", "sm_tree_psi" };
        private static readonly string[] _tabNames = { "sm_ui_knowledge_tree", "sm_ui_branch", "sm_ui_condition" };

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMKnowledgeWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMKnowledgeWindowImgui>();
        }

        public static void Toggle(Actor actor)
        {
            Ensure();
            _instance._visible = !_instance._visible;
            if (_instance._visible) _target = actor;
        }

        public static void Open(Actor actor)
        {
            Ensure();
            _target = actor;
            _targetId = actor != null ? actor.data.id : -1L;
            _instance._visible = true;
            _instance._message = "";
            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(actor));
            if (!string.IsNullOrEmpty(prefix)) _instance._currentPrefix = prefix;
            _instance._graphBuilt = false;
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
            SMImguiTheme.DrawWindowBackground(_windowRect, SMImguiTheme.UiKnowledge);
            SMImguiTheme.DrawGlowBorder(_windowRect, SMImguiTheme.BorderGlow, 8f);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    "", SMImguiTheme.WindowStyle);
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
            _windowRect.width = Mathf.Min(Mathf.Max(900f, Screen.width * 0.78f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(550f, Screen.height * 0.82f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            try
            {
                if (_target == null || _target.data == null || _target.data.id != _targetId)
                {
                    GUILayout.Label(LocalizedTextManager.getText("sm_ui_need_target"), SMImguiTheme.Label);
                    GUI.DragWindow();
                    return;
                }

                if (_graph == null) _graph = new SMKnowledgeGraph();
                if (!_graphBuilt)
                {
                    _graph.Build(_currentPrefix, _target);
                    _graphBuilt = true;
                }

                DrawTitleBar();
                DrawClassTabs();
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                DrawLeftPanel();
                GUILayout.Space(10);
                DrawRightPanel();
                GUILayout.EndHorizontal();

                GUI.DragWindow(new Rect(0, 0, 10000, 36));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 知识窗口内容异常: " + e.Message);
            }
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(8);
            GUILayout.Label("◀", SMImguiTheme.Label, GUILayout.Width(24));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_knowledge_title"), SMImguiTheme.Title, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            var all = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            int unlocked = all.FindAll(k => SuperMechKnowledge.IsUnlocked(_target, k.id)).Count;
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_progress")}: {unlocked}/{all.Count}", SMImguiTheme.Small, GUILayout.Height(30));
            GUILayout.Space(10);
            if (GUILayout.Button("✕", SMImguiTheme.Button, GUILayout.Width(30), GUILayout.Height(26)))
            {
                _visible = false;
            }
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            Rect titleLine = new Rect(8, 36, _windowRect.width - 16, 1f);
            SMImguiTheme.DrawRect(titleLine, new Color(0.25f, 0.7f, 0.95f, 0.4f));

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Label(_message, SMImguiTheme.Small);
            }
        }

        private void DrawClassTabs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            for (int i = 0; i < _prefixes.Length; i++)
            {
                bool active = _currentPrefix == _prefixes[i];
                string name = LocalizedTextManager.getText(_prefixNames[i]);
                var style = active ? SMImguiTheme.TabActive : SMImguiTheme.Tab;
                if (GUILayout.Button(name, style, GUILayout.Height(30), GUILayout.MinWidth(70)))
                {
                    _currentPrefix = _prefixes[i];
                    _graphBuilt = false;
                    _message = "";
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            float panelW = _windowRect.width * 0.55f;
            GUILayout.BeginVertical(GUILayout.Width(panelW));

            Rect panelRect = GUILayoutUtility.GetRect(panelW - 8, _windowRect.height - 130f);

            GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
            float graphHeight = panelRect.height - 40f;
            _graph.Draw(_target, graphHeight);
            GUILayout.EndArea();

            GUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            float panelW = _windowRect.width * 0.40f;
            GUILayout.BeginVertical(GUILayout.Width(panelW));

            Rect panelRect = GUILayoutUtility.GetRect(panelW - 8, _windowRect.height - 130f);

            GUILayout.BeginArea(new Rect(panelRect.x + 12, panelRect.y + 12, panelRect.width - 24, panelRect.height - 24));
            DrawDetailContent();
            GUILayout.EndArea();

            GUILayout.EndVertical();
        }

        private void DrawDetailContent()
        {
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_detail"), SMImguiTheme.Section);
            GUILayout.Space(4);

            string selectedId = _graph != null ? _graph.Selected : null;
            if (string.IsNullOrEmpty(selectedId))
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_select_knowledge"), SMImguiTheme.Label);
                return;
            }

            var def = SuperMechKnowledge.GetDef(selectedId);
            if (def == null)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_not_found"), SMImguiTheme.Label);
                return;
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);

            bool unlocked = SuperMechKnowledge.IsUnlocked(_target, def.id);
            Color titleColor = unlocked ? SMImguiTheme.Success : SMImguiTheme.Cyan;
            GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(titleColor)}>{LocalizedTextManager.getText(def.name)}</color>", SMImguiTheme.Title);
            GUILayout.Space(4);

            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_tier")}: {def.tier + 1}  |  {LocalizedTextManager.getText("sm_ui_cost")}: {def.cost} {LocalizedTextManager.getText("sm_ui_potential")}", SMImguiTheme.Small);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_unlocked")}: {(unlocked ? "✓" : "✗")}", SMImguiTheme.Small);
            GUILayout.Space(6);

            Rect divider = GUILayoutUtility.GetRect(0, 1f, GUILayout.ExpandWidth(true));
            SMImguiTheme.DrawRect(divider, new Color(0.25f, 0.7f, 0.95f, 0.3f));
            GUILayout.Space(6);

            GUILayout.Label(def.desc, SMImguiTheme.WrappedLabel);
            GUILayout.Space(10);

            GUILayout.Label(LocalizedTextManager.getText("sm_ui_condition"), SMImguiTheme.Section);
            GUILayout.Space(4);
            DrawConditionList(def);
            GUILayout.Space(10);

            if (!unlocked)
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unlock"), SMImguiTheme.PrimaryButton, GUILayout.Height(34)))
                {
                    bool success = SuperMechKnowledge.Unlock(_target, def.id);
                    _message = success ?
                        $"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_unlock_success")}</color>" :
                        $"<color=#E35D6A>{LocalizedTextManager.getText("sm_ui_unlock_fail")}</color>";
                    _graphBuilt = false;
                }
            }
            else
            {
                GUILayout.Label($"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_already_unlocked")}</color>", SMImguiTheme.Label);
            }

            GUILayout.EndScrollView();
        }

        private void DrawConditionList(SuperMechKnowledge.KnowledgeDef def)
        {
            var all = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
            int conditions = 0;
            foreach (var k in all)
            {
                if (k.tier == def.tier - 1 && k.branch == def.branch)
                {
                    bool condUnlocked = SuperMechKnowledge.IsUnlocked(_target, k.id);
                    string mark = condUnlocked ? "✓" : "○";
                    string color = condUnlocked ? "#66D18F" : "#8899AA";
                    GUILayout.Label($"  <color={color}>{mark}</color>  {LocalizedTextManager.getText(k.name)}", SMImguiTheme.Small);
                    conditions++;
                }
            }
            if (conditions == 0)
            {
                GUILayout.Label($"  <color=#8899AA>—</color>  {LocalizedTextManager.getText("sm_ui_no_prerequisite")}", SMImguiTheme.Small);
            }
        }
    }
}
