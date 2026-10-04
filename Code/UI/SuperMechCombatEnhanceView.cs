using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.70.x 战斗核心面板：伤害分区/抗性/穿透/控制/Buff 状态与当前Buff列表
    /// 原著：物理/能量/精神分区抗性（上限90%）、抗性穿透、生理/精神眩晕、战斗增益
    /// </summary>
    public class SuperMechCombatEnhanceView : MonoBehaviour
    {
        private Text _infoText;
        private RectTransform _buffContent;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] CombatEnhanceView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] CombatEnhanceView刷新失败: " + e); } }

        private void BuildLayout()
        {
            var infoPanel = SuperMechUiBuilder.CreatePanel(transform, "InfoPanel", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var ipRect = infoPanel.GetComponent<RectTransform>();
            ipRect.anchorMin = new Vector2(0, 0.5f);
            ipRect.anchorMax = new Vector2(1, 1);
            ipRect.pivot = new Vector2(0.5f, 1);
            ipRect.offsetMin = new Vector2(4, 4);
            ipRect.offsetMax = new Vector2(-4, -32);
            _infoText = SuperMechUiBuilder.AddText(infoPanel, "", 10, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.85f));
            var itRect = _infoText.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(10, 10);
            itRect.offsetMax = new Vector2(-10, -10);
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.verticalOverflow = VerticalWrapMode.Truncate;

            var buffPanel = SuperMechUiBuilder.CreatePanel(transform, "BuffPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var bpRect = buffPanel.GetComponent<RectTransform>();
            bpRect.anchorMin = Vector2.zero;
            bpRect.anchorMax = new Vector2(1, 0.5f);
            bpRect.pivot = new Vector2(0.5f, 0);
            bpRect.offsetMin = new Vector2(4, 4);
            bpRect.offsetMax = new Vector2(-4, 0);
            var bpTitle = SuperMechUiBuilder.AddText(buffPanel, LocalizedTextManager.getText("sm_ui_combat_buffs"), 12, TextAnchor.UpperCenter);
            var bpt = bpTitle.GetComponent<RectTransform>();
            bpt.anchorMin = new Vector2(0, 1);
            bpt.anchorMax = new Vector2(1, 1);
            bpt.sizeDelta = new Vector2(0, 22);

            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(buffPanel.transform, false);
            var sRect = scroll.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(4, 4);
            sRect.offsetMax = new Vector2(-4, -26);
            var sScroll = scroll.AddComponent<ScrollRect>();
            var sVp = new GameObject("Viewport");
            sVp.transform.SetParent(scroll.transform, false);
            var sVpRect = sVp.AddComponent<RectTransform>();
            sVpRect.anchorMin = Vector2.zero;
            sVpRect.anchorMax = Vector2.one;
            sVpRect.offsetMin = Vector2.zero;
            sVpRect.offsetMax = Vector2.zero;
            sVp.AddComponent<Mask>();
            var sContent = new GameObject("Content");
            sContent.transform.SetParent(sVp.transform, false);
            _buffContent = sContent.AddComponent<RectTransform>();
            _buffContent.anchorMin = new Vector2(0, 1);
            _buffContent.anchorMax = new Vector2(1, 1);
            _buffContent.pivot = new Vector2(0.5f, 1);
            _buffContent.sizeDelta = new Vector2(0, 100);
            sScroll.viewport = sVpRect;
            sScroll.content = _buffContent;
            sScroll.vertical = true;
            sScroll.horizontal = false;
        }

        private void RefreshAll()
        {
            if (_infoText == null) return;
            _infoText.text =
                $"{LocalizedTextManager.getText("sm_ui_combat_info1")}\n" +
                $"{LocalizedTextManager.getText("sm_ui_combat_info2")}\n" +
                $"{LocalizedTextManager.getText("sm_ui_combat_info3")}\n" +
                $"{LocalizedTextManager.getText("sm_ui_combat_info4")}";

            if (_buffContent == null) return;
            for (int i = _buffContent.childCount - 1; i >= 0; i--)
                Destroy(_buffContent.GetChild(i).gameObject);

            var ranking = SuperMechCombatEnhance.GetBuffRanking(20);
            if (ranking.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_buffContent.gameObject, LocalizedTextManager.getText("sm_ui_combat_no_buff"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var kv in ranking)
            {
                var a = kv.Key;
                var row = new GameObject("BuffRow");
                row.transform.SetParent(_buffContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 26);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 28f;
                var bg = row.AddComponent<Image>();
                bg.color = new Color(0.25f, 0.4f, 0.3f, 0.3f);

                var sb = new System.Text.StringBuilder($"{a.name}: ");
                foreach (var b in kv.Value)
                    sb.Append($"[{GetBuffName(b.id)} {b.value * 100:F0}% ×{b.remaining}t] ");
                var label = SuperMechUiBuilder.AddText(row, sb.ToString(), 10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-8, 0);
            }
            _buffContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private static string GetBuffName(string buffId)
        {
            if (buffId == SuperMechCombatEnhance.BuffAtk) return LocalizedTextManager.getText("sm_ui_combat_buff_atk");
            if (buffId == SuperMechCombatEnhance.BuffDef) return LocalizedTextManager.getText("sm_ui_combat_buff_def");
            return buffId;
        }
    }
}
