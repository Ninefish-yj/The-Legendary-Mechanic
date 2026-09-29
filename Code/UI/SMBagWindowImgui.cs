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
        private Vector2 _equipScroll;
        private Vector2 _resScroll;
        private Rect _windowRect = new Rect(120, 80, 750, 550);
        private static Actor _target;
        private static long _targetId = -1L;
        private int _activeTab = 0;
        private string _message = "";

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("SMBagWindowImgui");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SMBagWindowImgui>();
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
            if (!_visible) return;
            SMImguiTheme.Ensure();
            NormalizeRect();
            SMImguiTheme.DrawRect(_windowRect, SMImguiTheme.Background);
            SMImguiTheme.DrawBorder(_windowRect, SMImguiTheme.Border, 2f);
            try
            {
                _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow,
                    LocalizedTextManager.getText("sm_ui_bag_title"), SMImguiTheme.WindowStyle);
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
            _windowRect.width = Mathf.Min(Mathf.Max(600f, Screen.width * 0.55f), maxW);
            _windowRect.height = Mathf.Min(Mathf.Max(450f, Screen.height * 0.65f), maxH);
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
                DrawTabs();
                GUILayout.Space(6);

                if (_activeTab == 0) DrawVanillaEquipment();
                else if (_activeTab == 1) DrawModEquipment();
                else DrawResources();

                GUI.DragWindow(new Rect(0, 0, 10000, 28));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 背包窗口内容异常: " + e.Message);
            }
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Label(_message, SMImguiTheme.Small);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", SMImguiTheme.Button, GUILayout.Width(28), GUILayout.Height(22)))
            {
                _visible = false;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            string[] tabs = {
                LocalizedTextManager.getText("sm_ui_vanilla_equipment"),
                LocalizedTextManager.getText("sm_ui_equipment"),
                LocalizedTextManager.getText("sm_ui_resources")
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button(tabs[i],
                    _activeTab == i ? SMImguiTheme.TabActive : SMImguiTheme.Tab,
                    GUILayout.Height(28)))
                {
                    _activeTab = i;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawVanillaEquipment()
        {
            GUILayout.BeginVertical(SMImguiTheme.PanelStyle);
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_vanilla_equipment"), SMImguiTheme.Section);
            GUILayout.Space(4);

            if (_target.equipment == null || !_target.equipment.hasItems())
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Muted);
                GUILayout.EndVertical();
                return;
            }

            _equipScroll = GUILayout.BeginScrollView(_equipScroll);
            foreach (ActorEquipmentSlot slot in _target.equipment)
            {
                if (slot.isEmpty()) continue;
                Item item = slot.getItem();
                if (item == null) continue;
                EquipmentAsset asset = item.getAsset();

                GUILayout.BeginHorizontal(SMImguiTheme.RaisedPanelStyle);
                GUILayout.Label($"[{slot.type}] {item.data.asset_id}", SMImguiTheme.Label, GUILayout.Width(200));
                if (asset != null)
                {
                    GUILayout.Label($"ATK:{asset.base_stats.get(StatType.Attack)} DEF:{asset.base_stats.get(StatType.Defense)}",
                        SMImguiTheme.Small);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unequip"),
                    SMImguiTheme.Button, GUILayout.Width(80), GUILayout.Height(24)))
                {
                    slot.takeAwayItem();
                    _message = $"<color=#E9A34A>{LocalizedTextManager.getText("sm_ui_unequipped")}</color>";
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawModEquipment()
        {
            GUILayout.BeginVertical(SMImguiTheme.PanelStyle);
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_equipment"), SMImguiTheme.Section);
            GUILayout.Space(4);

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(_target);
            var bag = SuperMechEquipBag.GetBag(_target);

            if (currentIdx < 0 && (bag == null || bag.Count == 0))
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Muted);
                GUILayout.EndVertical();
                return;
            }

            _equipScroll = GUILayout.BeginScrollView(_equipScroll);

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
            GUILayout.EndVertical();
        }

        private void DrawModEquipRow(SuperMechRelic.EquipDef equip, bool isCurrent)
        {
            GUILayout.BeginHorizontal(SMImguiTheme.RaisedPanelStyle);
            Color qColor = GetQualityColor(equip.qualityLevel);
            var oldColor = GUI.color;
            GUI.color = qColor;
            GUILayout.Label(equip.name, SMImguiTheme.Label, GUILayout.Width(180));
            GUI.color = oldColor;
            GUILayout.Label($"DMG:{equip.dmgMul} HP:{equip.hpMul}", SMImguiTheme.Small);
            GUILayout.FlexibleSpace();
            if (isCurrent)
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_unequip"),
                    SMImguiTheme.Button, GUILayout.Width(80), GUILayout.Height(24)))
                {
                    SuperMechEquipBag.UnequipToBag(_target);
                    _message = $"<color=#E9A34A>{LocalizedTextManager.getText("sm_ui_unequipped")}</color>";
                }
            }
            else
            {
                if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_equip"),
                    SMImguiTheme.PrimaryButton, GUILayout.Width(80), GUILayout.Height(24)))
                {
                    SuperMechEquipBag.EquipFromBag(_target, equip.id);
                    _message = $"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_equipped")}</color>";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        private void DrawResources()
        {
            GUILayout.BeginVertical(SMImguiTheme.PanelStyle);
            GUILayout.Label(LocalizedTextManager.getText("sm_ui_resources"), SMImguiTheme.Section);
            GUILayout.Space(4);

            if (_target.inventory == null || _target.inventory.dict == null || _target.inventory.dict.Count == 0)
            {
                GUILayout.Label(LocalizedTextManager.getText("sm_ui_empty"), SMImguiTheme.Muted);
                GUILayout.EndVertical();
                return;
            }

            var resources = new List<ResourceContainer>();
            foreach (var kv in _target.inventory.dict)
            {
                if (kv.Value.amount > 0) resources.Add(kv.Value);
            }
            resources.Sort((a, b) => a.asset.order.CompareTo(b.asset.order));

            _resScroll = GUILayout.BeginScrollView(_resScroll);
            int cols = Mathf.Max(4, (int)(_windowRect.width - 80) / 70);
            int count = 0;
            GUILayout.BeginHorizontal();
            foreach (var res in resources)
            {
                if (count >= cols)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    count = 0;
                }

                GUILayout.BeginVertical(SMImguiTheme.RaisedPanelStyle, GUILayout.Width(60));
                GUILayout.Label(res.asset.name, SMImguiTheme.Small, GUILayout.Width(56));
                GUILayout.Label($"x{res.amount}", SMImguiTheme.Label, GUILayout.Width(56));
                if (res.asset.type == ResType.Food)
                {
                    if (GUILayout.Button(LocalizedTextManager.getText("sm_ui_eat"),
                        SMImguiTheme.Button, GUILayout.Width(56), GUILayout.Height(20)))
                    {
                        _target.consumeFoodResource(res.asset);
                        _target.inventory.remove(res.asset.id, 1);
                        _message = $"<color=#66D18F>{LocalizedTextManager.getText("sm_ui_ate")}</color>";
                    }
                }
                GUILayout.EndVertical();
                count++;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private Color GetQualityColor(int q)
        {
            Color[] colors = {
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.3f, 0.8f, 0.3f),
                new Color(0.3f, 0.5f, 1f),
                new Color(0.7f, 0.4f, 1f),
                new Color(0.8f, 0.2f, 0.8f),
                new Color(1f, 0.4f, 0.7f),
                new Color(1f, 0.6f, 0.2f),
                new Color(0.8f, 0.8f, 0.9f),
                new Color(1f, 0.84f, 0f)
            };
            if (q >= 0 && q < colors.Length) return colors[q];
            return Color.white;
        }
    }
}
