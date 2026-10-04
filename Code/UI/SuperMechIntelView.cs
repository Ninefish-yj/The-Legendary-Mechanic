using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.68.0 情报中心窗口：势力情报概览 + 情报行动（侦察/渗透/破坏）
    /// 原著：十三局/风眼/暗网——情报汇聚处理，渗透与破坏是势力博弈常规手段
    /// </summary>
    public class SuperMechIntelView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _countText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] IntelView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] IntelView刷新失败: " + e); } }

        private void BuildLayout()
        {
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, "", 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            var panel = SuperMechUiBuilder.CreatePanel(transform, "IntelPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var pRect = panel.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0, 0);
            pRect.anchorMax = new Vector2(1, 1);
            pRect.pivot = new Vector2(0.5f, 1);
            pRect.offsetMin = new Vector2(4, 4);
            pRect.offsetMax = new Vector2(-4, -32);
            var pTitle = SuperMechUiBuilder.AddText(panel, LocalizedTextManager.getText("sm_ui_intel_factions"), 12, TextAnchor.UpperCenter);
            var pt = pTitle.GetComponent<RectTransform>();
            pt.anchorMin = new Vector2(0, 1);
            pt.anchorMax = new Vector2(1, 1);
            pt.sizeDelta = new Vector2(0, 22);

            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
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
            _listContent = sContent.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 100);
            sScroll.viewport = sVpRect;
            sScroll.content = _listContent;
            sScroll.vertical = true;
            sScroll.horizontal = false;
        }

        private void RefreshAll()
        {
            if (_listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            var factions = SuperMechFaction.GetAllFactions();
            _countText.text = LocalizedTextManager.getText("sm_ui_intel_count") + ": " + factions.Count;

            if (factions.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_listContent.gameObject, LocalizedTextManager.getText("sm_ui_intel_empty"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var f in factions)
            {
                var data = SuperMechIntel.GetIntel(f.id);
                var row = new GameObject($"Intel_{f.id}");
                row.transform.SetParent(_listContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 46);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 48f;

                var bg = row.AddComponent<Image>();
                bg.color = data.disrupted ? new Color(0.5f, 0.25f, 0.2f, 0.3f) : new Color(1, 1, 1, 0.05f);

                string threatLine = data.reconLevel > 0 ? data.threatRating : "<color=#888>" + LocalizedTextManager.getText("sm_ui_intel_no_recon") + "</color>";
                string disruptLine = data.disrupted ? "  |  <color=#ff9966>" + LocalizedTextManager.getText("sm_ui_intel_disrupted") + "</color>" : "";
                var label = SuperMechUiBuilder.AddText(row,
                    $"{f.name}\n{LocalizedTextManager.getText("sm_ui_intel_points")}:{data.points:F0}  {LocalizedTextManager.getText("sm_ui_intel_infiltrate_short")}:{data.infiltrations}{disruptLine}\n{threatLine}",
                    9, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = new Vector2(0.58f, 1);
                labelRect.offsetMin = new Vector2(6, 0);
                labelRect.offsetMax = new Vector2(0, 0);

                string fid = f.id;
                // 行动按钮：侦察/渗透/破坏
                CreateSmallButton(row, new Vector2(-6, 8), 48, 20, LocalizedTextManager.getText("sm_ui_intel_recon"),
                    () => { SuperMechIntel.TryRecon(fid); RefreshAll(); });
                CreateSmallButton(row, new Vector2(-58, 8), 48, 20, LocalizedTextManager.getText("sm_ui_intel_infiltrate"),
                    () => { SuperMechIntel.TryInfiltrate(fid); RefreshAll(); });
                CreateSmallButton(row, new Vector2(-6, -10), 48, 20, LocalizedTextManager.getText("sm_ui_intel_disrupt"),
                    () => { SuperMechIntel.TryDisrupt(fid); RefreshAll(); });
                if (data.disrupted)
                {
                    CreateSmallButton(row, new Vector2(-58, -10), 48, 20, LocalizedTextManager.getText("sm_ui_intel_restore"),
                        () => { SuperMechIntel.ClearDisrupted(fid); RefreshAll(); });
                }
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void CreateSmallButton(GameObject parent, Vector2 pos, float w, float h, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(parent.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.sizeDelta = new Vector2(w, h);
            btnRect.anchoredPosition = pos;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.4f, 0.35f, 0.25f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            var btnText = SuperMechUiBuilder.AddText(btnGo, text, 8, TextAnchor.MiddleCenter);
            var btr = btnText.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = new Vector2(1, 0);
            btr.offsetMax = new Vector2(-1, 0);
            btn.onClick.AddListener(onClick);
        }
    }
}
