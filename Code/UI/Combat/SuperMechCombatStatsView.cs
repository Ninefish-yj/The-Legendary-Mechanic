using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>战斗统计UI面板（v0.183战斗反馈UI）
    /// 显示单位的击杀/伤害/连杀/评级</summary>
    public class SuperMechCombatStatsView : MonoBehaviour
    {
        private static SuperMechCombatStatsView _instance;
        public static bool IsOpen => _instance != null;

        private GameObject _panel;
        private Actor _actor;

        public static void Toggle(Actor actor)
        {
            if (_instance != null) { _instance.Close(); return; }
            Open(actor);
        }

        public static void Open(Actor actor)
        {
            if (_instance != null || actor == null) return;
            var go = new GameObject("SMCombatStatsView");
            _instance = go.AddComponent<SuperMechCombatStatsView>();
            _instance._actor = actor;
            _instance.Build();
        }

        public void Close()
        {
            if (_panel != null) Destroy(_panel);
            if (_instance == this) _instance = null;
            Destroy(gameObject);
        }

        private void Build()
        {
            var stats = SuperMechCombatStats.GetStats(_actor);
            if (stats == null) return;

            // 背景
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);

            // 主面板
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(350, 400);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -8);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = new Color(0.9f, 0.5f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = _actor.data.name + " - 战斗统计";
            titleText.raycastTarget = false;

            // 评级（大字显示）
            var ratingGo = new GameObject("Rating");
            ratingGo.transform.SetParent(_panel.transform, false);
            var ratingRect = ratingGo.AddComponent<RectTransform>();
            ratingRect.anchorMin = new Vector2(0, 1);
            ratingRect.anchorMax = new Vector2(1, 1);
            ratingRect.pivot = new Vector2(0.5f, 1);
            ratingRect.sizeDelta = new Vector2(0, 50);
            ratingRect.anchoredPosition = new Vector2(0, -50);
            var ratingText = ratingGo.AddComponent<Text>();
            ratingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ratingText.fontSize = 36;
            ratingText.color = GetRatingColor(SuperMechCombatStats.GetCombatRating(_actor));
            ratingText.alignment = TextAnchor.MiddleCenter;
            ratingText.fontStyle = FontStyle.Bold;
            ratingText.text = SuperMechCombatStats.GetCombatRating(_actor);
            ratingText.raycastTarget = false;

            // 数据列表
            var data = new (string label, string value, Color color)[]
            {
                ("总击杀", stats.totalKills.ToString(), new Color(1f, 0.8f, 0.4f)),
                ("当前连杀", stats.killStreak.ToString(), new Color(1f, 0.5f, 0.5f)),
                ("最高连杀", stats.maxKillStreak.ToString(), new Color(1f, 0.7f, 0.3f)),
                ("造成伤害", stats.totalDamageDealt.ToString("F0"), new Color(0.8f, 0.4f, 0.4f)),
                ("承受伤害", stats.totalDamageTaken.ToString("F0"), new Color(0.4f, 0.6f, 0.9f)),
                ("KDA", SuperMechCombatStats.GetKDA(_actor), new Color(0.7f, 0.7f, 1f)),
            };

            float y = 110f;
            foreach (var (label, value, color) in data)
            {
                CreateStatRow(_panel.transform, label, value, color, ref y);
            }

            // 最近击杀记录
            if (stats.recentKills.Count > 0)
            {
                var killsTitleGo = new GameObject("KillsTitle");
                killsTitleGo.transform.SetParent(_panel.transform, false);
                var killsTitleRect = killsTitleGo.AddComponent<RectTransform>();
                killsTitleRect.anchorMin = new Vector2(0.05f, 1);
                killsTitleRect.anchorMax = new Vector2(0.95f, 1);
                killsTitleRect.pivot = new Vector2(0.5f, 1);
                killsTitleRect.sizeDelta = new Vector2(0, 20);
                killsTitleRect.anchoredPosition = new Vector2(0, -y);
                var killsTitleText = killsTitleGo.AddComponent<Text>();
                killsTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                killsTitleText.fontSize = 11;
                killsTitleText.color = new Color(0.7f, 0.7f, 0.7f);
                killsTitleText.alignment = TextAnchor.UpperLeft;
                killsTitleText.text = "最近击杀:";
                killsTitleText.raycastTarget = false;
                y += 22f;

                int showCount = Mathf.Min(5, stats.recentKills.Count);
                for (int i = 0; i < showCount; i++)
                {
                    var killGo = new GameObject("Kill_" + i);
                    killGo.transform.SetParent(_panel.transform, false);
                    var killRect = killGo.AddComponent<RectTransform>();
                    killRect.anchorMin = new Vector2(0.08f, 1);
                    killRect.anchorMax = new Vector2(0.92f, 1);
                    killRect.pivot = new Vector2(0.5f, 1);
                    killRect.sizeDelta = new Vector2(0, 16);
                    killRect.anchoredPosition = new Vector2(0, -y);
                    var killText = killGo.AddComponent<Text>();
                    killText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    killText.fontSize = 10;
                    killText.color = new Color(0.6f, 0.6f, 0.6f);
                    killText.alignment = TextAnchor.UpperLeft;
                    killText.text = "  • " + stats.recentKills[i];
                    killText.raycastTarget = false;
                    y += 18f;
                }
            }

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(_panel.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(100, 32);
            closeRect.anchoredPosition = new Vector2(0, 10);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.4f, 0.3f, 0.3f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 12;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = LocalizedTextManager.getText("sm_ui_close");
            closeText.raycastTarget = false;
        }

        private void CreateStatRow(Transform parent, string label, string value, Color color, ref float y)
        {
            var rowGo = new GameObject("Stat_" + label);
            rowGo.transform.SetParent(parent, false);
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.05f, 1);
            rowRect.anchorMax = new Vector2(0.95f, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.sizeDelta = new Vector2(0, 24);
            rowRect.anchoredPosition = new Vector2(0, -y);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;
            labelText.raycastTarget = false;

            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(rowGo.transform, false);
            var valueRect = valueGo.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            var valueText = valueGo.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = 13;
            valueText.color = color;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.fontStyle = FontStyle.Bold;
            valueText.text = value;
            valueText.raycastTarget = false;

            y += 28f;
        }

        private Color GetRatingColor(string rating)
        {
            return rating switch
            {
                "SSS" => new Color(1f, 0.3f, 0.3f),
                "SS" => new Color(1f, 0.5f, 0.3f),
                "S" => new Color(1f, 0.7f, 0.3f),
                "A" => new Color(0.8f, 0.8f, 0.3f),
                "B" => new Color(0.5f, 0.8f, 0.4f),
                _ => new Color(0.6f, 0.6f, 0.6f),
            };
        }
    }
}
