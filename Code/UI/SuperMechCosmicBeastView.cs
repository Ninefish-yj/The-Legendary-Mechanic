using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.69.0 宇宙异兽观测站窗口：异兽波次状态 + 击杀记录
    /// 原著：评级标准之外的宇宙威胁（虚空生物/宇宙生命），击杀收获丰厚
    /// </summary>
    public class SuperMechCosmicBeastView : MonoBehaviour
    {
        private Text _statusText;
        private RectTransform _logContent;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] CosmicBeastView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] CosmicBeastView刷新失败: " + e); } }

        private void BuildLayout()
        {
            var statusPanel = SuperMechUiBuilder.CreatePanel(transform, "StatusPanel", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var spRect = statusPanel.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0, 0.55f);
            spRect.anchorMax = new Vector2(1, 1);
            spRect.pivot = new Vector2(0.5f, 1);
            spRect.offsetMin = new Vector2(4, 4);
            spRect.offsetMax = new Vector2(-4, -32);
            _statusText = SuperMechUiBuilder.AddText(statusPanel, "", 12, TextAnchor.UpperLeft);
            var stRect = _statusText.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(10, 10);
            stRect.offsetMax = new Vector2(-10, -10);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Truncate;

            var logPanel = SuperMechUiBuilder.CreatePanel(transform, "LogPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var lpRect = logPanel.GetComponent<RectTransform>();
            lpRect.anchorMin = Vector2.zero;
            lpRect.anchorMax = new Vector2(1, 0.55f);
            lpRect.pivot = new Vector2(0.5f, 0);
            lpRect.offsetMin = new Vector2(4, 4);
            lpRect.offsetMax = new Vector2(-4, 0);
            var lpTitle = SuperMechUiBuilder.AddText(logPanel, LocalizedTextManager.getText("sm_ui_beast_log"), 12, TextAnchor.UpperCenter);
            var lpt = lpTitle.GetComponent<RectTransform>();
            lpt.anchorMin = new Vector2(0, 1);
            lpt.anchorMax = new Vector2(1, 1);
            lpt.sizeDelta = new Vector2(0, 22);

            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(logPanel.transform, false);
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
            _logContent = sContent.AddComponent<RectTransform>();
            _logContent.anchorMin = new Vector2(0, 1);
            _logContent.anchorMax = new Vector2(1, 1);
            _logContent.pivot = new Vector2(0.5f, 1);
            _logContent.sizeDelta = new Vector2(0, 100);
            sScroll.viewport = sVpRect;
            sScroll.content = _logContent;
            sScroll.vertical = true;
            sScroll.horizontal = false;
        }

        private void RefreshAll()
        {
            if (_statusText == null) return;
            int active = SuperMechCosmicBeast.ActiveCount;
            int killed = SuperMechCosmicBeast.KilledCount;
            int next = SuperMechCosmicBeast.NextSpawnInTicks;
            _statusText.text =
                $"{LocalizedTextManager.getText("sm_ui_beast_active")}: {active}\n" +
                $"{LocalizedTextManager.getText("sm_ui_beast_killed")}: {killed}\n" +
                $"{LocalizedTextManager.getText("sm_ui_beast_next")}: {next}\n" +
                $"<size=10><color=#aaa>{LocalizedTextManager.getText("sm_ui_beast_hint")}</color></size>";

            if (_logContent == null) return;
            for (int i = _logContent.childCount - 1; i >= 0; i--)
                Destroy(_logContent.GetChild(i).gameObject);

            var units = World.world?.units?.units_only_alive;
            if (units == null || units.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_logContent.gameObject, LocalizedTextManager.getText("sm_ui_beast_none"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || !a.hasTrait(SuperMechCosmicBeast.BeastTrait)) continue;
                var row = new GameObject("BeastRow");
                row.transform.SetParent(_logContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 24);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 26f;
                var bg = row.AddComponent<Image>();
                bg.color = new Color(0.4f, 0.15f, 0.2f, 0.3f);
                var label = SuperMechUiBuilder.AddText(row,
                    $"{a.name}  |  {LocalizedTextManager.getText("sm_ui_beast_health")}:{a.data.health}  |  ({a.current_tile?.x ?? 0}, {a.current_tile?.y ?? 0})",
                    10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-8, 0);
            }
            if (y == 0f)
            {
                var empty = SuperMechUiBuilder.AddText(_logContent.gameObject, LocalizedTextManager.getText("sm_ui_beast_none"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
            }
            else
            {
                _logContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
            }
        }
    }
}
