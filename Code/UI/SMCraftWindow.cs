using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMCraftWindow : MonoBehaviour
    {
        private static SMCraftWindow _instance;
        private const int WindowId = 730503;

        private bool _visible;
        private Rect _windowRect = new Rect(100, 100, 420, 500);
        private Vector2 _scrollPos;
        private static Actor _target;
        private string _selectedRecipe;

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
            if (!_visible) return;
            if (_target == null || !_target.isAlive()) { _visible = false; return; }
            SMImguiTheme.Ensure();
            SMImguiTheme.DrawRect(_windowRect, SMImguiTheme.Background);
            SMImguiTheme.DrawBorder(_windowRect, SMImguiTheme.Border, 2f);
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow,
                LocalizedTextManager.getText("sm_ui_craft_title"), SMImguiTheme.WindowStyle);
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 24));

            float y = 32;
            float w = _windowRect.width - 20;
            Actor a = _target;

            int stage = SuperMechStage.GetStage(a);
            string stageName = SuperMechStage.GetStageName(a);

            GUI.Label(new Rect(10, y, w, 20),
                $"{LocalizedTextManager.getText("sm_ui_craft_maker")}: {a.name}", SMImguiTheme.DataLabel);
            y += 22;
            GUI.Label(new Rect(10, y, w, 20),
                $"{LocalizedTextManager.getText("sm_ui_stage")}: {stageName} (Lv{stage})", SMImguiTheme.DataValue);
            y += 26;

            float cd = SuperMechCrafting.GetCraftCooldown(a);
            if (cd > 0)
            {
                GUI.Label(new Rect(10, y, w, 20),
                    $"{LocalizedTextManager.getText("sm_ui_craft_cooldown")}: {cd:F1}s", SMImguiTheme.DataValue);
                y += 24;
            }

            var recipes = SuperMechCrafting.GetAvailableRecipes(a);
            if (recipes.Count == 0)
            {
                GUI.Label(new Rect(10, y, w, 20),
                    LocalizedTextManager.getText("sm_ui_craft_none"), SMImguiTheme.Label);
                return;
            }

            _scrollPos = GUI.BeginScrollView(new Rect(10, y, w, _windowRect.height - y - 50),
                _scrollPos, new Rect(0, 0, w - 20, recipes.Count * 70));

            float ry = 0;
            foreach (var r in recipes)
            {
                bool selected = _selectedRecipe == r.id;
                GUI.color = selected ? SMImguiTheme.Panel : SMImguiTheme.Background;
                GUI.Box(new Rect(0, ry, w - 20, 64), "");
                GUI.color = Color.white;

                GUI.Label(new Rect(8, ry + 4, w - 40, 20),
                    LocalizedTextManager.getText(r.name), SMImguiTheme.Section);
                GUI.Label(new Rect(8, ry + 24, w - 40, 16),
                    LocalizedTextManager.getText(r.desc), SMImguiTheme.Label);

                string costStr = "";
                if (r.cost != null)
                    foreach (var kv in r.cost) costStr += $"{kv.Key}x{kv.Value} ";
                GUI.Label(new Rect(8, ry + 42, w - 100, 16),
                    $"{LocalizedTextManager.getText("sm_ui_craft_cost")}: {costStr}", SMImguiTheme.Label);

                if (GUI.Button(new Rect(w - 90, ry + 20, 70, 28),
                    LocalizedTextManager.getText("sm_ui_craft_button"),
                    selected ? SMImguiTheme.PrimaryButton : SMImguiTheme.Button))
                {
                    _selectedRecipe = r.id;
                    WorldTile tile = a.current_tile;
                    if (tile != null) SuperMechCrafting.TryCraft(a, r.id, tile);
                }
                ry += 70;
            }
            GUI.EndScrollView();

            if (GUI.Button(new Rect(10, _windowRect.height - 34, 80, 26),
                LocalizedTextManager.getText("sm_ui_close"), SMImguiTheme.Button))
                _visible = false;
        }

        public static void ClearStaticState()
        {
            if (_instance != null) _instance._visible = false;
            _target = null;
        }
    }
}
