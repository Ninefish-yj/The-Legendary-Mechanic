using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechUI
    {
        private const string Layout = "tools";
        private static bool _inited;
        private static PowersTab _tab;

        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] CorePowers =
        {
            (SuperMechPowers.SummonAwakened,  "ui/Icons/actor_traits/iconChosenOne",      "sm_ui_1055", "sm_ui_1056"),
            (SuperMechPowers.DisasterAlien,   "iconDiscord",     "sm_ui_1057",     "sm_ui_1058"),
        };

        private static readonly (string name, string tip, System.Action action)[] WindowButtons =
        {
            ("sm_ui_1059",     "sm_ui_1060", () => SuperMechSanctuaryWindow.Show()),
            ("sm_ui_1061",     "sm_ui_1062", () => SuperMechRankWindow.Show()),
        };

        public static void Init()
        {
            if (_inited) return;
            try
            {
                _inited = true;

                LocalizedTextManager.add("supermech.tab", LocalizedTextManager.getText("sm_ui_1063"), pReplace: true);
                LocalizedTextManager.add("supermech.tab_desc", LocalizedTextManager.getText("sm_ui_1064"), pReplace: true);

                LocalizedTextManager.add("sm_ui_1065", LocalizedTextManager.getText("sm_ui_1065"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1066", LocalizedTextManager.getText("sm_ui_1066"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1067", LocalizedTextManager.getText("sm_ui_1068"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1069", LocalizedTextManager.getText("sm_ui_1070"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1071", LocalizedTextManager.getText("sm_ui_1072"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1073", LocalizedTextManager.getText("sm_ui_1074"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1075", LocalizedTextManager.getText("sm_ui_1076"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1077", LocalizedTextManager.getText("sm_ui_1078"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1079", LocalizedTextManager.getText("sm_ui_1080"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1081", LocalizedTextManager.getText("sm_ui_1082"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1083", LocalizedTextManager.getText("sm_ui_1084"), pReplace: true);
                LocalizedTextManager.add("sm_ui_1085", LocalizedTextManager.getText("sm_ui_1086"), pReplace: true);

                string[] panelLabels = {
                    "sm_ui_1087", "sm_ui_1088", "sm_ui_1089", "sm_ui_1090", "sm_ui_1091", "sm_ui_1092", "sm_ui_1093", "sm_ui_1094",
                    "sm_ui_1065", "sm_ui_1095", "sm_ui_1096", "sm_ui_1097", "sm_ui_1098", "sm_ui_1099", "sm_ui_1100",
                    "sm_ui_1101", "sm_ui_1102", "sm_ui_1103", "sm_ui_1104", "sm_ui_1105", "sm_ui_1106", "sm_ui_1107", "sm_ui_1108",
                    "sm_ui_1109", "sm_ui_1110", "sm_ui_1111", "sm_ui_1112", "sm_ui_1113", "sm_ui_1114",
                    "sm_ui_1115", "sm_ui_1116", "sm_ui_1117", "sm_ui_1118", "sm_ui_1119", "sm_ui_1120", "sm_ui_1121",
                    "sm_ui_1122", "sm_ui_1123", "sm_ui_1124", "sm_ui_1125", "sm_ui_1126", "sm_ui_1127", "sm_ui_1128",
                    "sm_ui_1129", "sm_ui_1066", "sm_ui_1130", "sm_ui_1131"
                };
                foreach (var label in panelLabels)
                    LocalizedTextManager.add(label, label, pReplace: true);

                string[] tooltipTexts = {
                    "sm_ui_1055", "sm_ui_1132", "sm_ui_1056",
                    "sm_ui_1057", "sm_ui_1133", "sm_ui_1058",
                    "sm_ui_1134", "sm_ui_1135", "sm_ui_1136",
                    "sm_ui_1137", "sm_ui_1138",
                    "sm_ui_1139", "sm_ui_1140", "sm_ui_1141",
                    "sm_ui_1059", "sm_ui_1060",
                    "sm_ui_1061", "sm_ui_1062",
                    "sm_ui_1142", "sm_ui_1143",
                    "sm_ui_1144", "sm_ui_1145"
                };
                foreach (var text in tooltipTexts)
                    LocalizedTextManager.add(text, text, pReplace: true);

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconChosenOne");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                foreach (var (id, iconPath, tipTitle, tipDesc) in CorePowers)
                {
                    try
                    {
                        GodPower power = AssetManager.powers.get(id);
                        if (power == null) { Debug.LogWarning("[超神机械师] 神权未注册: " + id); continue; }
                        Sprite icon = SpriteTextureLoader.getSprite(iconPath);
                        if (icon == null) icon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight"); // fallback
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(id, icon);
                        if (btn == null) continue;
                        btn.godPower = power;
                        if (btn.icon != null && btn.icon.sprite == null && icon != null)
                            btn.icon.sprite = icon;
                        SetupTooltip(btn, tipTitle, tipDesc);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 神权按钮失败 " + id + ": " + e.Message); }
                }


                foreach (var (name, tip, action) in WindowButtons)
                {
                    try
                    {
                        Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconUnity");
                        if (icon == null) icon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight");
                        PowerButton btn = PowerButtonCreator.CreateSimpleButton(name, () => { try { action?.Invoke(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗异常: " + e.Message); } }, icon);
                        if (btn == null) continue;
                        if (btn.icon != null && btn.icon.sprite == null && icon != null)
                            btn.icon.sprite = icon;
                        SetupTooltip(btn, name, tip);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗按钮失败 " + name + ": " + e.Message); }
                }

                _tab.UpdateLayout();
                Debug.Log($"[超神机械师] 专属Tab创建成功：{CorePowers.Length}神权+{WindowButtons.Length}窗口（五系觉醒/神之催化在单位面板）");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] Tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }

        private static void SetupTooltip(PowerButton btn, string title, string desc)
        {
            try
            {
                if (btn == null) return;
                var tipBtn = btn.GetComponent<TipButton>();
                if (tipBtn == null) tipBtn = btn.gameObject.AddComponent<TipButton>();
                tipBtn.textOnClick = title + "\n" + desc;
                tipBtn.textOnClickDescription = string.Empty;  // 清除，避免被本地化查找
                tipBtn.text_description_2 = string.Empty;
            }
            catch { }
        }
    }
}
