using System;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMSanctuaryWindowImgui : MonoBehaviour
    {
        private static SMSanctuaryWindowImgui _instance;
        private const int WINDOW_ID = 730505;

        private bool _visible;
        private Vector2 _scroll;
        private Rect _windowRect = new Rect(200, 80, 620, 700);
        private string _message = "";

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMSanctuaryWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMSanctuaryWindowImgui>();
        }

        public static void Toggle()
        {
            Ensure();
            _instance._visible = !_instance._visible;
            if (_instance._visible) _instance._message = "";
        }

        public static void Show()
        {
            Ensure();
            _instance._visible = true;
            _instance._message = "";
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
            SMImguiTheme.DrawWindowBackground(_windowRect);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    "", SMImguiTheme.WindowStyle);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 圣所窗口异常: " + e.Message);
                _visible = false;
            }
        }

        private void NormalizeRect()
        {
            float maxW = Mathf.Max(400f, Screen.width - 30f);
            float maxH = Mathf.Max(400f, Screen.height - 30f);
            _windowRect.width = Mathf.Min(Mathf.Max(550f, Screen.width * 0.45f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(550f, Screen.height * 0.8f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            try
            {
                DrawTitleBar();
                GUILayout.Space(8);

                Rect contentRect = GUILayoutUtility.GetRect(_windowRect.width - 24, _windowRect.height - 90f);
                SMImguiTheme.DrawGlowBorder(contentRect, new Color(0.35f, 0.62f, 0.88f, 0.5f), 3f);

                GUILayout.BeginArea(new Rect(contentRect.x + 12, contentRect.y + 12, contentRect.width - 24, contentRect.height - 24));
                _scroll = GUILayout.BeginScrollView(_scroll);
                DrawSanctuaryList();
                GUILayout.Space(10);
                DrawDimensionList();
                GUILayout.EndScrollView();
                GUILayout.EndArea();

                GUI.DragWindow(new Rect(0, 0, 10000, 36));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 圣所内容异常: " + e.Message);
            }
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(8);
            GUILayout.Label("◀", SMImguiTheme.Label, GUILayout.Width(24));
            GUILayout.Label(LocalizedTextManager.getText("sm_sanctuary_title"), SMImguiTheme.Title, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Label(_message, SMImguiTheme.Small, GUILayout.Height(30));
            }
            GUILayout.Space(10);
            if (GUILayout.Button("✕", SMImguiTheme.Button, GUILayout.Width(30), GUILayout.Height(26)))
            {
                _visible = false;
            }
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            Rect titleLine = new Rect(8, 36, _windowRect.width - 16, 1f);
            SMImguiTheme.DrawRect(titleLine, new Color(0.35f, 0.62f, 0.88f, 0.4f));
        }

        private void DrawSanctuaryList()
        {
            GUILayout.Label(LocalizedTextManager.getText("sm_sanctuary_subtitle"), SMImguiTheme.Section);
            GUILayout.Space(6);

            string[] nameKeys = { "sm_sanctuary_1", "sm_sanctuary_2", "sm_sanctuary_3", "sm_sanctuary_4", "sm_sanctuary_5", "sm_sanctuary_6" };
            string[] names = Array.ConvertAll(nameKeys, k => LocalizedTextManager.getText(k));
            string[] descKeys = { "sm_sanctuary_desc_1", "sm_sanctuary_desc_2", "sm_sanctuary_desc_3", "sm_sanctuary_desc_4", "sm_sanctuary_desc_5", "sm_sanctuary_desc_6" };
            string[] descs = Array.ConvertAll(descKeys, k => LocalizedTextManager.getText(k));

            var data = SuperMechSanctuary.Data;
            for (int i = 0; i < 6; i++)
            {
                int sanctuaryIndex = i;
                bool unlocked = (data.unlocked_sanctuaries & (1 << i)) != 0;
                int fragments = data.sanctuary_fragments[i];

                Rect itemRect = GUILayoutUtility.GetRect(_windowRect.width - 50, 50);
                Color bg = unlocked ? new Color(0.78f, 0.9f, 0.82f, 0.85f) : new Color(0.85f, 0.88f, 0.92f, 0.85f);
                SMImguiTheme.DrawRect(itemRect, bg);
                SMImguiTheme.DrawBorder(itemRect, unlocked ? SMImguiTheme.Success : SMImguiTheme.Border, 1f);

                GUI.Label(new Rect(itemRect.x + 8, itemRect.y + 4, itemRect.width - 100, 20),
                    names[i], SMImguiTheme.Label);
                GUI.Label(new Rect(itemRect.x + 8, itemRect.y + 24, itemRect.width - 100, 16),
                    descs[i], SMImguiTheme.Small);

                string status = unlocked ? LocalizedTextManager.getText("sm_sanctuary_unlocked") : $"{fragments}/3";
                GUI.Label(new Rect(itemRect.xMax - 80, itemRect.y + 6, 70, 18),
                    status, unlocked ? SMImguiTheme.DataValue : SMImguiTheme.Small);

                if (GUI.Button(new Rect(itemRect.xMax - 80, itemRect.y + 26, 70, 20),
                    unlocked ? LocalizedTextManager.getText("sm_sanctuary_enter") : LocalizedTextManager.getText("sm_sanctuary_locked"),
                    unlocked ? SMImguiTheme.PrimaryButton : SMImguiTheme.Button))
                {
                    if (!unlocked)
                    {
                        _message = string.Format(LocalizedTextManager.getText("sm_sanctuary_msg_locked"), names[sanctuaryIndex]);
                        return;
                    }
                    Actor selected = World.world.getActorNearCursor();
                    if (selected == null)
                    {
                        _message = LocalizedTextManager.getText("sm_sanctuary_msg_nounit");
                        return;
                    }
                    if (SuperMechSanctuary.EnterSanctuary(selected, sanctuaryIndex))
                    {
                        _message = string.Format(LocalizedTextManager.getText("sm_sanctuary_msg_enter"), selected.name, names[sanctuaryIndex]);
                    }
                }
                GUILayout.Space(4);
            }

            GUILayout.Space(8);
            int total = 0;
            foreach (int f in data.sanctuary_fragments) total += f;
            int unlockedCount = 0;
            for (int i = 0; i < 6; i++) if ((data.unlocked_sanctuaries & (1 << i)) != 0) unlockedCount++;
            GUILayout.Label($"{LocalizedTextManager.getText("sm_sanctuary_fragments")}: {total}  |  {LocalizedTextManager.getText("sm_sanctuary_unlocked")}: {unlockedCount}/6  |  {LocalizedTextManager.getText("sm_sanctuary_keys")}: {data.key_fragments}",
                SMImguiTheme.Small);
        }

        private void DrawDimensionList()
        {
            Rect divider = GUILayoutUtility.GetRect(0, 1f, GUILayout.ExpandWidth(true));
            SMImguiTheme.DrawRect(divider, new Color(0.35f, 0.62f, 0.88f, 0.3f));
            GUILayout.Space(8);

            GUILayout.Label(LocalizedTextManager.getText("sm_sanctuary_dimension_title"), SMImguiTheme.Section);
            GUILayout.Space(6);

            Actor selected = MoveCamera.getFocusUnit();
            var dims = SuperMechDimension.GetAvailableDimensions(selected);
            if (dims.Count == 0)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_sanctuary_dimension_hint"), SMImguiTheme.Small);
                return;
            }

            foreach (var dim in dims)
            {
                bool canEnter = SuperMechDimension.CanEnter(selected, dim);
                Rect itemRect = GUILayoutUtility.GetRect(_windowRect.width - 50, 36);
                Color bg = canEnter ? new Color(0.78f, 0.85f, 0.92f, 0.9f) : new Color(0.82f, 0.82f, 0.85f, 0.8f);
                SMImguiTheme.DrawRect(itemRect, bg);
                SMImguiTheme.DrawBorder(itemRect, canEnter ? SMImguiTheme.BorderGlow : SMImguiTheme.Border, 1f);

                GUI.Label(new Rect(itemRect.x + 8, itemRect.y + 4, itemRect.width - 90, 18),
                    dim.name, SMImguiTheme.Label);
                GUI.Label(new Rect(itemRect.x + 8, itemRect.y + 20, itemRect.width - 90, 14),
                    dim.desc, SMImguiTheme.Small);

                if (GUI.Button(new Rect(itemRect.xMax - 80, itemRect.y + 6, 70, 24),
                    LocalizedTextManager.getText("sm_sanctuary_enter"),
                    canEnter ? SMImguiTheme.PrimaryButton : SMImguiTheme.Button))
                {
                    if (SuperMechDimension.Enter(selected, dim))
                    {
                        _message = string.Format(LocalizedTextManager.getText("sm_sanctuary_msg_enter_dim"), dim.name);
                    }
                }
                GUILayout.Space(4);
            }
        }
    }
}
