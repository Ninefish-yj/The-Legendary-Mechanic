using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.205 设置面板：大重启+常用配置开关+帮助
    /// 从星海总览"C组·高级功能"标签页进入
    /// </summary>
    public class SuperMechSettingsView : MonoBehaviour
    {
        private RectTransform _content;
        private float _curY;

        void Start()
        {
            var selfRect = GetComponent<RectTransform>();
            if (selfRect == null) return;

            // v0.207 ScrollRect：窗口缩放时内容可滚动
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(transform, false);
            var vpRect = viewportGo.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("SettingsContent");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = new Vector2(0, 600);

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = _content;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 20f;

            BuildPanel();

            // 动态设置内容高度
            _content.sizeDelta = new Vector2(0, Mathf.Max(Mathf.Abs(_curY) + 20f, 200f));
        }

        private void BuildPanel()
        {
            _curY = -10f;

            // === 标题 ===
            AddTitle(LocalizedTextManager.getText("sm_ui_settings_title"));
            _curY -= 36f;

            // === 危险操作区：大重启 ===
            AddSection(LocalizedTextManager.getText("sm_ui_settings_danger"));
            _curY -= 8f;
            AddButton(LocalizedTextManager.getText("sm_great_restart_title"),
                LocalizedTextManager.getText("sm_great_restart_desc"),
                () => SuperMechCosmicIteration.TriggerGreatRestart(),
                new Color(0.6f, 0.15f, 0.15f, 0.9f));
            _curY -= 52f;

            // === 常用配置 ===
            AddSection(LocalizedTextManager.getText("sm_ui_settings_config"));
            _curY -= 8f;

            AddToggle(LocalizedTextManager.getText("sm_cfg_qi_unlimited"),
                LocalizedTextManager.getText("sm_cfg_qi_unlimited_desc"),
                () => SuperMechConfig.QiUnlimited,
                v => SuperMechConfig.QiUnlimited = v);

            AddToggle(LocalizedTextManager.getText("sm_cfg_auto_awakening"),
                LocalizedTextManager.getText("sm_cfg_auto_awakening_desc"),
                () => SuperMechConfig.AutoAwakening,
                v => SuperMechConfig.AutoAwakening = v);

            AddToggle(LocalizedTextManager.getText("sm_cfg_auto_promotion"),
                LocalizedTextManager.getText("sm_cfg_auto_promotion_desc"),
                () => SuperMechConfig.AutoPromotion,
                v => SuperMechConfig.AutoPromotion = v);

            AddToggle(LocalizedTextManager.getText("sm_cfg_sanctuary_enabled"),
                LocalizedTextManager.getText("sm_cfg_sanctuary_enabled_desc"),
                () => SuperMechConfig.SanctuaryEnabled,
                v => SuperMechConfig.SanctuaryEnabled = v);

            AddToggle(LocalizedTextManager.getText("sm_cfg_combat_effects"),
                LocalizedTextManager.getText("sm_cfg_combat_effects_desc"),
                () => SuperMechConfig.CombatEffectsEnabled,
                v => SuperMechConfig.CombatEffectsEnabled = v);

            AddToggle(LocalizedTextManager.getText("sm_cfg_combat_ai"),
                LocalizedTextManager.getText("sm_cfg_combat_ai_desc"),
                () => SuperMechConfig.CombatAIEnabled,
                v => SuperMechConfig.CombatAIEnabled = v);

            _curY -= 12f;

            // === 其他功能入口（v0.207 从标签页收进设置）===
            AddSection(LocalizedTextManager.getText("sm_ui_settings_other"));
            _curY -= 8f;
            AddButton(LocalizedTextManager.getText("sm_ui_player_title"),
                LocalizedTextManager.getText("sm_ui_player_desc"),
                () => SuperMechWindowManager.OpenPlayer(),
                new Color(0.2f, 0.35f, 0.55f, 0.9f));
            _curY -= 48f;
            AddButton(LocalizedTextManager.getText("sm_ui_quest_title"),
                LocalizedTextManager.getText("sm_ui_quest_desc"),
                () => SuperMechQuestView.Toggle(),
                new Color(0.2f, 0.35f, 0.55f, 0.9f));
            _curY -= 48f;
            AddButton(LocalizedTextManager.getText("sm_ui_battle_report_title"),
                LocalizedTextManager.getText("sm_ui_battle_report_desc"),
                () => SuperMechBattleReportView.Toggle(),
                new Color(0.2f, 0.35f, 0.55f, 0.9f));
            _curY -= 52f;

            // === 帮助 ===
            AddSection(LocalizedTextManager.getText("sm_ui_settings_help"));
            _curY -= 8f;
            AddHelpText(LocalizedTextManager.getText("sm_ui_settings_help_text"));
        }

        private void AddTitle(string text)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_content, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = SuperMechUiSkin.DefaultFont;
            t.fontSize = 18;
            t.fontStyle = FontStyle.Bold;
            t.color = SuperMechUiSkin.SciGold;
            t.alignment = TextAnchor.UpperLeft;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(12, _curY);
            r.sizeDelta = new Vector2(-24, 24);
        }

        private void AddSection(string text)
        {
            var go = new GameObject("Section", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_content, false);
            var img = go.GetComponent<Image>();
            img.color = SuperMechUiSkin.SciCard;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(8, _curY);
            r.sizeDelta = new Vector2(-16, 22);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lt = labelGo.GetComponent<Text>();
            lt.text = text;
            lt.font = SuperMechUiSkin.GetFont();
            lt.fontSize = 13;
            lt.fontStyle = FontStyle.Bold;
            lt.color = SuperMechUiSkin.SciCyan;
            lt.alignment = TextAnchor.MiddleLeft;
            var lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(8, 0); lr.offsetMax = new Vector2(-8, 0);
        }

        private void AddButton(string name, string desc, System.Action onClick, Color bgColor)
        {
            var go = new GameObject("Btn_" + name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_content, false);
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(12, _curY);
            r.sizeDelta = new Vector2(-24, 40);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(go.transform, false);
            var nt = nameGo.GetComponent<Text>();
            nt.text = name;
            nt.font = SuperMechUiSkin.GetFont();
            nt.fontSize = 14;
            nt.fontStyle = FontStyle.Bold;
            nt.color = Color.white;
            nt.alignment = TextAnchor.MiddleLeft;
            var nr = nameGo.GetComponent<RectTransform>();
            nr.anchorMin = new Vector2(0, 0.5f); nr.anchorMax = new Vector2(0.4f, 0.5f);
            nr.offsetMin = new Vector2(10, -10); nr.offsetMax = new Vector2(0, 10);

            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(go.transform, false);
            var dt = descGo.GetComponent<Text>();
            dt.text = desc;
            dt.font = SuperMechUiSkin.GetFont();
            dt.fontSize = 11;
            dt.color = SuperMechUiSkin.SciMuted;
            dt.alignment = TextAnchor.MiddleLeft;
            var dr = descGo.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0.4f, 0.5f); dr.anchorMax = new Vector2(1, 0.5f);
            dr.offsetMin = new Vector2(8, -10); dr.offsetMax = new Vector2(-10, 10);

            _curY -= 46f;
        }

        private void AddToggle(string name, string desc, System.Func<bool> getter, System.Action<bool> setter)
        {
            var go = new GameObject("Toggle_" + name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_content, false);
            var img = go.GetComponent<Image>();
            img.color = SuperMechUiSkin.SciCard;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(12, _curY);
            r.sizeDelta = new Vector2(-24, 36);

            // 名称+描述
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(go.transform, false);
            var nt = nameGo.GetComponent<Text>();
            nt.text = name;
            nt.font = SuperMechUiSkin.GetFont();
            nt.fontSize = 13;
            nt.color = Color.white;
            nt.alignment = TextAnchor.MiddleLeft;
            var nr = nameGo.GetComponent<RectTransform>();
            nr.anchorMin = new Vector2(0, 0.5f); nr.anchorMax = new Vector2(0.55f, 0.5f);
            nr.offsetMin = new Vector2(10, -8); nr.offsetMax = new Vector2(0, 8);

            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(go.transform, false);
            var dt = descGo.GetComponent<Text>();
            dt.text = desc;
            dt.font = SuperMechUiSkin.GetFont();
            dt.fontSize = 10;
            dt.color = SuperMechUiSkin.SciMuted;
            dt.alignment = TextAnchor.MiddleLeft;
            var dr = descGo.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0, 0); dr.anchorMax = new Vector2(0.55f, 0.4f);
            dr.offsetMin = new Vector2(10, 2); dr.offsetMax = new Vector2(0, 0);

            // 开关按钮
            bool curVal = getter();
            var toggleGo = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            toggleGo.transform.SetParent(go.transform, false);
            var tImg = toggleGo.GetComponent<Image>();
            tImg.color = curVal ? SuperMechUiSkin.SciCyan : SuperMechUiSkin.SciCardHover;
            var tBtn = toggleGo.GetComponent<Button>();
            var tR = toggleGo.GetComponent<RectTransform>();
            tR.anchorMin = new Vector2(1, 0.5f); tR.anchorMax = new Vector2(1, 0.5f);
            tR.pivot = new Vector2(1, 0.5f);
            tR.anchoredPosition = new Vector2(-12, 0);
            tR.sizeDelta = new Vector2(50, 24);

            var toggleLabel = SuperMechUiSkin.MakeText(toggleGo.transform,
                curVal ? LocalizedTextManager.getText("sm_ui_on") : LocalizedTextManager.getText("sm_ui_off"),
                11, TextAnchor.MiddleCenter);
            toggleLabel.color = Color.white;

            tBtn.onClick.AddListener(() =>
            {
                bool newVal = !getter();
                setter(newVal);
                tImg.color = newVal ? SuperMechUiSkin.SciCyan : SuperMechUiSkin.SciCardHover;
                toggleLabel.text = newVal ? LocalizedTextManager.getText("sm_ui_on") : LocalizedTextManager.getText("sm_ui_off");
            });

            _curY -= 40f;
        }

        private void AddHelpText(string text)
        {
            var go = new GameObject("Help", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_content, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = SuperMechUiSkin.DefaultFont;
            t.fontSize = 11;
            t.color = SuperMechUiSkin.SciMuted;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(12, _curY);
            r.sizeDelta = new Vector2(-24, 80);
        }
    }
}
