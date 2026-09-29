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

        private static readonly string[] _prefixes = { "mech", "martial", "mage", "mind", "psi" };
        private static readonly string[] _prefixNames = { "sm_tree_mech", "sm_tree_martial", "sm_tree_mage", "sm_tree_mind", "sm_tree_psi" };

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
            _windowRect.width = Mathf.Min(Mathf.Max(800f, Screen.width * 0.75f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(500f, Screen.height * 0.8f), maxH);
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

                if (_graph == null) _graph = new SMKnowledgeGraph();
                if (!_graphBuilt)
                {
                    _graph.Build(_currentPrefix, _target);
                    _graphBuilt = true;
                }

                DrawTopBar();
                GUILayout.Space(4);
                DrawClassTabs();
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                DrawGraph();
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
            var all = SuperMechKnowledge.GetAllByPrefix(_currentPrefix);
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

        private void DrawClassTabs()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _prefixes.Length; i++)
            {
                bool active = _currentPrefix == _prefixes[i];
                string name = LocalizedTextManager.getText(_prefixNames[i]);
                if (GUILayout.Button(name, active ? SMImguiTheme.TabActive : SMImguiTheme.Tab, GUILayout.Height(28)))
                {
                    _currentPrefix = _prefixes[i];
                    _graphBuilt = false;
                    _message = "";
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawGraph()
        {
            GUILayout.BeginVertical(SMImguiTheme.PanelStyle, GUILayout.ExpandWidth(true));
            float graphHeight = _windowRect.height - 140f;
            _graph.Draw(_target, graphHeight);
            GUILayout.EndVertical();
        }

        private void DrawDetailPanel()
        {
            GUILayout.BeginVertical(SMImguiTheme.RaisedPanelStyle, GUILayout.Width(SMImguiTheme.Px(260)));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_detail"), SMImguiTheme.Section);
            GUILayout.Space(4);

            string selectedId = _graph != null ? _graph.Selected : null;
            if (string.IsNullOrEmpty(selectedId))
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_select_knowledge"), SMImguiTheme.Muted);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            var def = SuperMechKnowledge.GetDef(selectedId);
            if (def == null)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_not_found"), SMImguiTheme.Muted);
                GUILayout.EndVertical();
                return;
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Label(LocalizedTextManager.getText(def.name), SMImguiTheme.Title);
            GUILayout.Space(4);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_tier")}: {def.tier + 1}", SMImguiTheme.Label);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_unlocked")}: {(SuperMechKnowledge.IsUnlocked(_target, def.id) ? "✓" : "✗")}", SMImguiTheme.Label);
            GUILayout.Label($"{LocalizedTextManager.getText("sm_ui_cost")}: {def.cost} {LocalizedTextManager.getText("sm_ui_potential")}", SMImguiTheme.Label);
            GUILayout.Space(6);
            GUILayout.Label(def.desc, SMImguiTheme.WrappedLabel);
            GUILayout.Space(8);

            if (!SuperMechKnowledge.IsUnlocked(_target, def.id))
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unlock"), SMImguiTheme.PrimaryButton, GUILayout.Height(30)))
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
            GUILayout.EndVertical();
        }
    }
}
