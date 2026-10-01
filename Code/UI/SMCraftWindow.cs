using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMCraftWindow : MonoBehaviour
    {
        private static SMCraftWindow _instance;
        private const int WindowId = 730503;

        private bool _visible;
        private Rect _windowRect = new Rect(100, 100, 500, 600);
        private Vector2 _scrollPos;
        private static Actor _target;
        private string _selectedRecipe;
        private string _message = "";

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMCraftWindow");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMCraftWindow>();
        }

        public static void Toggle(Actor a)
        {
            Ensure();
            _target = a;
            _instance._visible = !_instance._visible;
            if (_instance._visible) _instance._selectedRecipe = null;
        }

        public static void Close()
        {
            if (_instance != null) _instance._visible = false;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                SMWindowBlocker.Get(WindowId).Hide();
                return;
            }
            if (_target == null || !_target.isAlive()) { _visible = false; return; }
            SMImguiTheme.Ensure();
            NormalizeRect();
            SMWindowBlocker.Get(WindowId).Sync(_windowRect);
            SMImguiTheme.DrawWindowBackground(_windowRect);
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow,
                "", SMImguiTheme.WindowStyle);
        }

        private void NormalizeRect()
        {
            float maxW = Mathf.Max(400f, Screen.width - 30f);
            float maxH = Mathf.Max(400f, Screen.height - 30f);
            _windowRect.width = Mathf.Min(Mathf.Max(450f, Screen.width * 0.4f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(500f, Screen.height * 0.7f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 36));

            DrawTitleBar();

            float y = 48;
            float w = _windowRect.width - 36;
            Actor a = _target;

            int stage = SuperMechStage.GetStage(a);
            string stageName = SuperMechStage.GetStageName(a);

            Rect infoRect = new Rect(14, y, w, 50);
            SMImguiTheme.DrawGlowBorder(infoRect, new Color(0.35f, 0.62f, 0.88f, 0.4f), 2f);

            GUI.Label(new Rect(22, y + 6, w - 16, 18),
                $"{LocalizedTextManager.getText("sm_ui_craft_maker")}: {a.name}", SMImguiTheme.DataLabel);
            GUI.Label(new Rect(22, y + 26, w - 16, 18),
                $"{LocalizedTextManager.getText("sm_ui_stage")}: {stageName} (Lv{stage})", SMImguiTheme.DataValue);
            y += 58;

            float cd = SuperMechCrafting.GetCraftCooldown(a);
            if (cd > 0)
            {
                GUI.Label(new Rect(14, y, w, 18),
                    $"{LocalizedTextManager.getText("sm_ui_craft_cooldown")}: {cd:F1}s", SMImguiTheme.DataValue);
                y += 24;
            }

            if (!string.IsNullOrEmpty(_message))
            {
                GUI.Label(new Rect(14, y, w, 18), _message, SMImguiTheme.Small);
                y += 22;
            }

            var recipes = SuperMechCrafting.GetAvailableRecipes(a);
            if (recipes.Count == 0)
            {
                GUI.Label(new Rect(14, y, w, 20),
                    LocalizedTextManager.getText("sm_ui_craft_none"), SMImguiTheme.Label);
                return;
            }

            Rect scrollRect = new Rect(14, y, w, _windowRect.height - y - 50);
            SMImguiTheme.DrawGlowBorder(scrollRect, new Color(0.35f, 0.62f, 0.88f, 0.4f), 2f);

            _scrollPos = GUI.BeginScrollView(new Rect(scrollRect.x + 4, scrollRect.y + 4, scrollRect.width - 8, scrollRect.height - 8),
                _scrollPos, new Rect(0, 0, scrollRect.width - 24, recipes.Count * 76));

            float ry = 0;
            foreach (var r in recipes)
            {
                bool selected = _selectedRecipe == r.id;
                Rect itemRect = new Rect(4, ry, scrollRect.width - 28, 68);

                Color itemBg = selected ? new Color(0.75f, 0.85f, 0.92f, 0.9f) : new Color(0.85f, 0.9f, 0.95f, 0.8f);
                SMImguiTheme.DrawRect(itemRect, itemBg);
                SMImguiTheme.DrawBorder(itemRect, selected ? SMImguiTheme.BorderGlow : SMImguiTheme.Border, 1f);

                GUI.Label(new Rect(12, ry + 6, itemRect.width - 100, 20),
                    LocalizedTextManager.getText(r.name), SMImguiTheme.Section);
                GUI.Label(new Rect(12, ry + 26, itemRect.width - 100, 16),
                    LocalizedTextManager.getText(r.desc), SMImguiTheme.Small);

                string costStr = "";
                if (r.cost != null)
                    foreach (var kv in r.cost) costStr += $"{kv.Key}x{kv.Value} ";
                GUI.Label(new Rect(12, ry + 44, itemRect.width - 100, 16),
                    $"{LocalizedTextManager.getText("sm_ui_craft_cost")}: {costStr}", SMImguiTheme.Small);

                if (GUI.Button(new Rect(itemRect.width - 80, ry + 20, 70, 30),
                    LocalizedTextManager.getText("sm_ui_craft_button"),
                    selected ? SMImguiTheme.PrimaryButton : SMImguiTheme.Button))
                {
                    _selectedRecipe = r.id;
                    WorldTile tile = a.current_tile;
                    if (tile != null)
                    {
                        bool success = SuperMechCrafting.TryCraft(a, r.id, tile);
                        _message = success ?
                            $"<color=#66E096>{LocalizedTextManager.getText("sm_ui_craft_success")}</color>" :
                            $"<color=#FF7070>{LocalizedTextManager.getText("sm_ui_craft_fail")}</color>";
                    }
                }
                ry += 76;
            }
            GUI.EndScrollView();
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(8);
            GUILayout.Label("◀", SMImguiTheme.Label, GUILayout.Width(24));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_craft_title"), SMImguiTheme.Title, GUILayout.Height(30));
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

        public static void ClearStaticState()
        {
            if (_instance != null) _instance._visible = false;
            _target = null;
        }
    }
}
