using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.67.0 星际航道·星门控制台窗口：势力星门列表（建造/匿踪切换）+ 航道说明
    /// 原著：星门是技术门槛（超星团级文明可建造，宇宙级/三大文明零偏差），秘密星门可匿踪
    /// </summary>
    public class SuperMechStarGateView : MonoBehaviour
    {
        private RectTransform _gateContent;
        private Text _countText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] StarGateView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] StarGateView刷新失败: " + e); } }

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

            // 星门列表
            var gatePanel = SuperMechUiBuilder.CreatePanel(transform, "GatePanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var gpRect = gatePanel.GetComponent<RectTransform>();
            gpRect.anchorMin = new Vector2(0, 0.3f);
            gpRect.anchorMax = new Vector2(1, 1);
            gpRect.pivot = new Vector2(0.5f, 1);
            gpRect.offsetMin = new Vector2(4, 4);
            gpRect.offsetMax = new Vector2(-4, -32);
            var gpTitle = SuperMechUiBuilder.AddText(gatePanel, LocalizedTextManager.getText("sm_ui_stargate_list"), 12, TextAnchor.UpperCenter);
            var gpt = gpTitle.GetComponent<RectTransform>();
            gpt.anchorMin = new Vector2(0, 1);
            gpt.anchorMax = new Vector2(1, 1);
            gpt.sizeDelta = new Vector2(0, 22);

            var gateScroll = new GameObject("GateScroll");
            gateScroll.transform.SetParent(gatePanel.transform, false);
            var gsRect = gateScroll.AddComponent<RectTransform>();
            gsRect.anchorMin = Vector2.zero;
            gsRect.anchorMax = Vector2.one;
            gsRect.offsetMin = new Vector2(4, 4);
            gsRect.offsetMax = new Vector2(-4, -26);
            var gsScroll = gateScroll.AddComponent<ScrollRect>();
            var gsVp = new GameObject("Viewport");
            gsVp.transform.SetParent(gateScroll.transform, false);
            var gsVpRect = gsVp.AddComponent<RectTransform>();
            gsVpRect.anchorMin = Vector2.zero;
            gsVpRect.anchorMax = Vector2.one;
            gsVpRect.offsetMin = Vector2.zero;
            gsVpRect.offsetMax = Vector2.zero;
            gsVp.AddComponent<Mask>();
            var gsContent = new GameObject("Content");
            gsContent.transform.SetParent(gsVp.transform, false);
            _gateContent = gsContent.AddComponent<RectTransform>();
            _gateContent.anchorMin = new Vector2(0, 1);
            _gateContent.anchorMax = new Vector2(1, 1);
            _gateContent.pivot = new Vector2(0.5f, 1);
            _gateContent.sizeDelta = new Vector2(0, 100);
            gsScroll.viewport = gsVpRect;
            gsScroll.content = _gateContent;
            gsScroll.vertical = true;
            gsScroll.horizontal = false;

            // 说明
            var infoPanel = SuperMechUiBuilder.CreatePanel(transform, "InfoPanel", new Color(0, 0, 0, 0.15f), 4, 4, 0, 0);
            var iRect = infoPanel.GetComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = new Vector2(1, 0.3f);
            iRect.pivot = new Vector2(0.5f, 0);
            iRect.offsetMin = new Vector2(4, 4);
            iRect.offsetMax = new Vector2(-4, 0);
            var infoText = SuperMechUiBuilder.AddText(infoPanel, LocalizedTextManager.getText("sm_ui_stargate_info"), 10, TextAnchor.UpperLeft, new Color(0.75f, 0.75f, 0.75f));
            var itRect = infoText.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.offsetMin = new Vector2(8, 8);
            itRect.offsetMax = new Vector2(-8, -8);
            infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshAll()
        {
            if (_gateContent == null) return;
            for (int i = _gateContent.childCount - 1; i >= 0; i--)
                Destroy(_gateContent.GetChild(i).gameObject);

            var factions = SuperMechFaction.GetAllFactions();
            int builtCount = 0;
            foreach (var f in factions) if (SuperMechStarGate.HasGate(f.id)) builtCount++;
            _countText.text = $"{LocalizedTextManager.getText("sm_ui_stargate_list")}: {factions.Count}  |  " +
                $"{LocalizedTextManager.getText("sm_ui_stargate_built")}: {builtCount}";

            if (factions.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_gateContent.gameObject, LocalizedTextManager.getText("sm_ui_stargate_no_faction"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var f in factions)
            {
                var row = new GameObject($"Gate_{f.id}");
                row.transform.SetParent(_gateContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 34);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 36f;

                var bg = row.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.05f);

                var kingdom = SuperMechFaction.GetFactionCivilization(f);
                string civName = kingdom == null ? "?" : SuperMechCivilization.GetLevelName(SuperMechCivilization.GetCivLevelFromKingdom(kingdom));
                bool hasGate = SuperMechStarGate.HasGate(f.id);
                bool hidden = hasGate && (SuperMechStarGate.GetGate(f.id)?.hidden ?? false);
                bool canBuild = SuperMechStarGate.CanBuild(f.id);

                string state = !hasGate ? (canBuild
                    ? LocalizedTextManager.getText("sm_ui_stargate_ready")
                    : "<color=#999>" + LocalizedTextManager.getText("sm_ui_stargate_need_tech") + "</color>")
                    : (hidden ? "<color=#66ccff>" + LocalizedTextManager.getText("sm_ui_stargate_hidden") + "</color>"
                              : "<color=#66ff66>" + LocalizedTextManager.getText("sm_ui_stargate_active") + "</color>");

                var label = SuperMechUiBuilder.AddText(row,
                    $"{f.name}  |  {civName}  |  {state}",
                    10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = new Vector2(0.62f, 1);
                labelRect.offsetMin = new Vector2(6, 0);
                labelRect.offsetMax = new Vector2(0, 0);

                string fid = f.id;
                if (!hasGate && canBuild)
                {
                    CreateSmallButton(row, new Vector2(-6, 0), 52, 24, LocalizedTextManager.getText("sm_ui_stargate_build"),
                        () => { SuperMechStarGate.TryBuildGate(fid); RefreshAll(); });
                }
                else if (hasGate)
                {
                    CreateSmallButton(row, new Vector2(-6, 0), 52, 24,
                        hidden ? LocalizedTextManager.getText("sm_ui_stargate_show") : LocalizedTextManager.getText("sm_ui_stargate_hide"),
                        () => { SuperMechStarGate.ToggleHidden(fid); RefreshAll(); });
                }
            }
            _gateContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
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
            btnImg.color = new Color(0.3f, 0.4f, 0.55f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            var btnText = SuperMechUiBuilder.AddText(btnGo, text, 9, TextAnchor.MiddleCenter);
            var btr = btnText.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = new Vector2(2, 0);
            btr.offsetMax = new Vector2(-2, 0);
            btn.onClick.AddListener(onClick);
        }
    }
}
