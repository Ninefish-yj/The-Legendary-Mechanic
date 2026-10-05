using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 星海总览·历法页签（v0.76.14）。
    /// 历法=同一宇宙（同一迭代）内的纪元命名，由秩序确立驱动（原著：探索历→星海历）；
    /// 与宇宙迭代（大重启=代际）是不同维度，不混为一谈。
    /// 玩家可在本页签就地选择历法风格（原著历法/文明历法/时代历法），无需进配置面板。
    /// </summary>
    public class SuperMechEraView : MonoBehaviour
    {
        private static bool _warnedRefresh;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 历法页签初始化失败: " + e); } }
        void OnEnable() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 历法页签刷新失败: " + e); } }
        void Update()
        {
            // 秩序状态可能由博弈系统实时翻转，周期刷新状态行
            try { RefreshStatus(); } catch (System.Exception e) { if (!_warnedRefresh) { _warnedRefresh = true; Debug.LogError("[超神机械师] 历法页签刷新异常(仅首次): " + e.Message); } }
        }

        private Text _statusText;

        private void BuildLayout()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);

            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _statusText = SuperMechUiBuilder.AddText(topBar, "", 13, TextAnchor.MiddleLeft);
            var sRect = _statusText.GetComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(8, 0);
            sRect.offsetMax = new Vector2(-8, 0);

            var bodyPanel = SuperMechUiBuilder.CreatePanel(transform, "Body", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var bRect = bodyPanel.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.offsetMin = new Vector2(0, -32);
            bRect.offsetMax = new Vector2(0, -4);

            var scroll = SuperMechUiBuilder.AddScrollView(bodyPanel, out var content);
            var scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            float y = -4f;

            // ── 当前纪元卡 ──
            bool order = SuperMechSupermA.OrderEstablished;
            string eraName = order ? SuperMechSupermA.GetEraOrderName() : SuperMechSupermA.GetEraChaosName();
            string eraKind = order ? LocalizedTextManager.getText("sm_ui_era_ordered") : LocalizedTextManager.getText("sm_ui_era_chaotic");
            y = AddCard(content, y, LocalizedTextManager.getText("sm_ui_era_now"),
                $"<size=20><color=#ffd966>{eraName}</color></size>  <color=#8fa8c8>{eraKind}</color>", 66);

            // ── 历法风格就地切换 ──
            y = AddCard(content, y, LocalizedTextManager.getText("sm_ui_era_style"),
                LocalizedTextManager.getText("sm_ui_era_style_desc"), 44);
            string[] styles = {
                LocalizedTextManager.getText("sm_ui_era_style_0"),
                LocalizedTextManager.getText("sm_ui_era_style_1"),
                LocalizedTextManager.getText("sm_ui_era_style_2")
            };
            float bx = 10f;
            for (int i = 0; i < 3; i++)
            {
                int sel = i;
                bool active = SuperMechConfig.EraCalendar == i;
                var btn = SuperMechUiBuilder.CreateButton(content, "Style" + i, styles[i], () =>
                {
                    SuperMechConfig.EraCalendar = sel;
                    BuildLayout();
                }, width: 200, height: 30, fontSize: 12);
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0, 1);
                br.anchorMax = new Vector2(0, 1);
                br.pivot = new Vector2(0, 1);
                br.anchoredPosition = new Vector2(bx, y - 36);
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = active ? new Color(0.20f, 0.45f, 0.30f, 0.95f) : new Color(0.10f, 0.14f, 0.22f, 0.9f);
                bx += 204f;
            }
            y -= 72f;

            // ── 纪元演进规则 ──
            y = AddCard(content, y, LocalizedTextManager.getText("sm_ui_era_rule"),
                LocalizedTextManager.getText("sm_ui_era_rule_desc"), 64);

            // ── 概念区分：历法≠迭代 ──
            y = AddCard(content, y, LocalizedTextManager.getText("sm_ui_era_scope"),
                LocalizedTextManager.getText("sm_ui_era_scope_desc"), 96);

            var cr = content.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(0, -y + 10f);
        }

        private float AddCard(Transform parent, float y, string title, string body, float height)
        {
            var card = SuperMechUiBuilder.CreatePanel(parent, "Card", new Color(0.05f, 0.09f, 0.15f, 0.9f), 6, 6, 4, 4);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(1, 1);
            cardRect.pivot = new Vector2(0.5f, 1);
            cardRect.anchoredPosition = new Vector2(0, y);
            cardRect.sizeDelta = new Vector2(-12, -height);

            var t = SuperMechUiBuilder.AddText(card, "", 12, TextAnchor.UpperLeft);
            var tRect = t.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(10, 4);
            tRect.offsetMax = new Vector2(-10, -4);
            t.text = $"<color=#6ab7ff>{title}</color>\n{body}";
            return y - height - 6f;
        }

        private void RefreshStatus()
        {
            if (_statusText == null) return;
            bool order = SuperMechSupermA.OrderEstablished;
            string eraName = order ? SuperMechSupermA.GetEraOrderName() : SuperMechSupermA.GetEraChaosName();
            string iter = LocalizedTextManager.getText("sm_ui_era_iteration");
            _statusText.text = $"{LocalizedTextManager.getText("sm_ui_era_title")} · {eraName} · {iter} {SuperMechCosmicIteration.CurrentIteration}";
        }
    }
}
