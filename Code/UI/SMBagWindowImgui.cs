using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    internal class SMBagWindowImgui : MonoBehaviour
    {
        private static SMBagWindowImgui _instance;
        private const int WINDOW_ID = 730502;

        private bool _visible;
        private Vector2 _scroll;
        private Rect _windowRect = new Rect(120, 80, 800, 600);
        private static Actor _target;
        private static long _targetId = -1L;
        private int _activeTab = 0;
        private string _message = "";

        private static readonly string[] _tabKeys = { "sm_ui_vanilla_equipment", "sm_ui_equipment", "sm_ui_resources" };
        private static readonly List<ResourceContainer> _resourceCache = new List<ResourceContainer>();
        private static readonly Color[] _qualityColors = {
            new Color(0.55f, 0.55f, 0.6f),
            new Color(0.35f, 0.85f, 0.4f),
            new Color(0.35f, 0.55f, 1f),
            new Color(0.75f, 0.45f, 1f),
            new Color(0.85f, 0.25f, 0.85f),
            new Color(1f, 0.45f, 0.75f),
            new Color(1f, 0.65f, 0.25f),
            new Color(0.85f, 0.85f, 0.95f),
            new Color(1f, 0.88f, 0.1f)
        };

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMBagWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMBagWindowImgui>();
        }

        public static void Toggle(Actor actor)
        {
            Ensure();
            _instance._visible = !_instance._visible;
            if (_instance._visible)
            {
                _target = actor;
                _targetId = actor != null ? actor.data.id : -1L;
            }
        }

        public static void Open(Actor actor)
        {
            Ensure();
            _target = actor;
            _targetId = actor != null ? actor.data.id : -1L;
            _instance._visible = true;
        }

        public static void Close()
        {
            if (_instance != null) _instance._visible = false;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                SMWindowBlocker.Get(WINDOW_ID).Hide();
                return;
            }
            SMImguiTheme.Ensure();
            NormalizeRect();
            SMWindowBlocker.Get(WINDOW_ID).Sync(_windowRect);
            SMImguiTheme.DrawWindowBackground(_windowRect);
            SMImguiTheme.DrawGlowBorder(_windowRect, SMImguiTheme.BorderGlow, 8f);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    "", SMImguiTheme.WindowStyle);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 背包窗口绘制异常: " + e.Message);
                _visible = false;
            }
        }

        private void NormalizeRect()
        {
            float maxW = Mathf.Max(500f, Screen.width - 30f);
            float maxH = Mathf.Max(350f, Screen.height - 30f);
            _windowRect.width = Mathf.Min(Mathf.Max(700f, Screen.width * 0.6f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(500f, Screen.height * 0.7f), maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Screen.width - _windowRect.width - 10f);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Screen.height - _windowRect.height - 10f);
        }

        private void DrawWindow(int id)
        {
            try
            {
                DrawTitleBar();

                if (_target == null || _target.data == null || _target.data.id != _targetId)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizedTextManager.getText("sm_ui_need_target"), SMImguiTheme.Title, GUILayout.Height(40));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    GUI.DragWindow(new Rect(0, 0, 10000, 36));
                    return;
                }

                DrawTabs();
                GUILayout.Space(8);

                Rect contentRect = GUILayoutUtility.GetRect(_windowRect.width - 24, _windowRect.height - 110f);

                GUILayout.BeginArea(new Rect(contentRect.x + 12, contentRect.y + 12, contentRect.width - 24, contentRect.height - 24));
                if (_activeTab == 0) DrawVanillaEquipment();
                else if (_activeTab == 1) DrawModEquipment();
                else DrawResources();
                GUILayout.EndArea();

                GUI.DragWindow(new Rect(0, 0, 10000, 36));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 背包窗口内容异常: " + e.Message);
            }
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(8);
            GUILayout.Label("◀", SMImguiTheme.Label, GUILayout.Width(24));
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_bag_title"), SMImguiTheme.Title, GUILayout.Height(30));
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

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            for (int i = 0; i < _tabKeys.Length; i++)
            {
                bool active = _activeTab == i;
                string name = LocalizedTextManager.getText(_tabKeys[i]);
                var style = active ? SMImguiTheme.TabActive : SMImguiTheme.Tab;
                if (GUILayout.Button(name, style, GUILayout.Height(30), GUILayout.MinWidth(80)))
                {
                    _activeTab = i;
                    _message = "";
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawVanillaEquipment()
        {
            if (_target.equipment == null || !_target.equipment.hasItems())
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Label);
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (ActorEquipmentSlot slot in _target.equipment)
            {
                if (slot.isEmpty()) continue;
                Item item = slot.getItem();
                if (item == null) continue;
                EquipmentAsset asset = item.getAsset();

                GUILayout.BeginHorizontal(SMImguiTheme.RaisedPanelStyle);
                string equipName = asset != null ? LocalizedTextManager.getText(asset.getLocaleID()) : item.data.asset_id;
                GUILayout.Label($"[{slot.type}] {equipName}", SMImguiTheme.Label, GUILayout.Width(220));
                if (asset != null)
                {
                    GUILayout.Label($"ATK:{asset.base_stats.get("damage")} DEF:{asset.base_stats.get("defense")}",
                        SMImguiTheme.Small);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unequip"),
                    SMImguiTheme.Button, GUILayout.Width(80), GUILayout.Height(26)))
                {
                    slot.takeAwayItem();
                    _message = $"<color=#FFB347>{LocalizedTextManager.getText("sm_ui_unequipped")}</color>";
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
            GUILayout.EndScrollView();
        }

        private void DrawModEquipment()
        {
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(_target);
            var bag = SuperMechEquipBag.GetBag(_target);

            if (currentIdx < 0 && (bag == null || bag.Count == 0))
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Label);
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll);

            if (currentIdx >= 0)
            {
                var equip = SuperMechRelic.Equipments[currentIdx];
                DrawModEquipRow(equip, true);
            }

            if (bag != null)
            {
                foreach (string equipId in bag)
                {
                    int idx = SuperMechRelic.GetEquipIndex(equipId);
                    if (idx >= 0 && idx != currentIdx)
                    {
                        DrawModEquipRow(SuperMechRelic.Equipments[idx], false);
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawModEquipRow(SuperMechRelic.EquipDef equip, bool isCurrent)
        {
            GUILayout.BeginHorizontal(SMImguiTheme.RaisedPanelStyle);
            Color qColor = GetQualityColor(equip.qualityLevel);
            var oldColor = GUI.color;
            GUI.color = qColor;
            GUILayout.Label(LocalizedTextManager.getText(equip.name), SMImguiTheme.Label, GUILayout.Width(200));
            GUI.color = oldColor;
            GUILayout.Label($"DMG:{equip.dmgMul} HP:{equip.hpMul}", SMImguiTheme.Small);
            GUILayout.FlexibleSpace();
            if (isCurrent)
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unequip"),
                    SMImguiTheme.Button, GUILayout.Width(80), GUILayout.Height(26)))
                {
                    SuperMechEquipBag.UnequipToBag(_target);
                    _message = $"<color=#FFB347>{LocalizedTextManager.getText("sm_ui_unequipped")}</color>";
                }
            }
            else
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_equip"),
                    SMImguiTheme.PrimaryButton, GUILayout.Width(80), GUILayout.Height(26)))
                {
                    SuperMechEquipBag.EquipFromBag(_target, equip.id);
                    _message = $"<color=#66E096>{LocalizedTextManager.getText("sm_ui_equipped")}</color>";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawResources()
        {
            if (_target.inventory == null || _target.inventory.dict == null || _target.inventory.dict.Count == 0)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Label);
                return;
            }

            _resourceCache.Clear();
            foreach (var kv in _target.inventory.dict)
            {
                if (kv.Value.amount > 0) _resourceCache.Add(kv.Value);
            }
            _resourceCache.Sort((a, b) => a.asset.order.CompareTo(b.asset.order));

            _scroll = GUILayout.BeginScrollView(_scroll);
            int cols = Mathf.Max(4, (int)(_windowRect.width - 100) / 75);
            int count = 0;
            GUILayout.BeginHorizontal();
            foreach (var res in _resourceCache)
            {
                if (count >= cols)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    count = 0;
                }

                GUILayout.BeginVertical(SMImguiTheme.RaisedPanelStyle, GUILayout.Width(68));
                GUILayout.Label(LocalizedTextManager.getText(res.asset.getLocaleID()), SMImguiTheme.Small, GUILayout.Width(60));
                GUILayout.Label($"x{res.amount}", SMImguiTheme.Label, GUILayout.Width(60));
                if (res.asset.type == ResType.Food)
                {
                    if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_eat"),
                        SMImguiTheme.Button, GUILayout.Width(60), GUILayout.Height(22)))
                    {
                        _target.consumeFoodResource(res.asset);
                        _target.inventory.remove(res.asset.id, 1);
                        _message = LocalizedTextManager.getText("sm_ui_ate_msg");
                    }
                }
                GUILayout.EndVertical();
                count++;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private Color GetQualityColor(int q)
        {
            if (q >= 0 && q < _qualityColors.Length) return _qualityColors[q];
            return Color.white;
        }
    }
}
