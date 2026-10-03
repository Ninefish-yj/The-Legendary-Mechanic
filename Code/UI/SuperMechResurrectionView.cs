using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所复活窗口：显示可复活的死亡单位列表，点击复活
    /// </summary>
    public class SuperMechResurrectionView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _infoText;

        void Awake()
        {
            try { BuildLayout(); RefreshList(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] 复活窗口初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 顶部信息栏
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = Vector2.one;
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 50);
            topRect.anchoredPosition = Vector2.zero;
            var topImg = topGo.AddComponent<Image>();
            topImg.color = new Color(0.1f, 0.15f, 0.22f, 0.9f);

            _infoText = SuperMechUiSkin.MakeText(topGo.transform, "", 12, TextAnchor.UpperLeft);
            var infoRect = _infoText.GetComponent<RectTransform>();
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(10, 5);
            infoRect.offsetMax = new Vector2(-10, -5);
            _infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoText.supportRichText = true;

            // 列表面板
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.offsetMin = new Vector2(0, 0);
            listRect.offsetMax = new Vector2(0, -50);

            var (scroll, content) = SuperMechUiSkin.CreateScrollArea(listGo.transform, "Scroll");
            _listContent = content;
        }

        private void RefreshList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var deadStates = SuperMechInformationState.GetDeadStates();
            int resurrectable = 0;

            // 更新信息栏（原著设定：圣所能量媒介，信息完整度决定复苏质量）
            string tEnergy = LocalizedTextManager.getText("sm_ui_res_energy");
            string tCond = LocalizedTextManager.getText("sm_ui_res_cond");
            string tDeadRecord = LocalizedTextManager.getText("sm_ui_res_dead_record");
            string tResurrectable = LocalizedTextManager.getText("sm_ui_res_resurrectable");
            _infoText.text = $"<color=#6ab7ff>{tEnergy}:</color> {SuperMechSanctuary.Data.sanctuary_energy:F0}  " +
                $"<color=#6ab7ff>{tCond}:</color> S+ 20%\n" +
                $"<color=#8fa8c8>{tDeadRecord}: {deadStates.Count} | {tResurrectable}: {SuperMechResurrection.GetResurrectableStates().Count}</color>";

            if (deadStates.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(_listContent, false);
                emptyGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
                var emptyText = SuperMechUiSkin.MakeText(emptyGo.transform, LocalizedTextManager.getText("sm_ui_no_dead"), 13, TextAnchor.MiddleCenter);
                emptyText.color = SuperMechUiSkin.TextDim;
                return;
            }

            foreach (var state in deadStates)
            {
                bool canRes = SuperMechResurrection.CanResurrect(state);
                if (canRes) resurrectable++;

                var itemGo = new GameObject($"Item_{state.actorId}");
                itemGo.transform.SetParent(_listContent, false);
                itemGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = canRes ? SuperMechUiSkin.RowEven : SuperMechUiSkin.RowOdd;

                // 信息文字
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(itemGo.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(8, 0);
                textRect.offsetMax = new Vector2(-100, 0);
                var text = textGo.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 12;
                text.color = canRes ? SuperMechUiSkin.TextColor : SuperMechUiSkin.TextDim;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.supportRichText = true;
                string rankName = (state.rankIndex >= 0 && state.rankIndex < SuperMechRanks.All.Count)
                    ? LocalizedTextManager.getText(SuperMechRanks.All[state.rankIndex].name)
                    : "?";
                string reason = canRes ? "" : GetCannotReason(state);
                float integrity = SuperMechResurrection.CalculateInformationIntegrity(state);
                string tQi = LocalizedTextManager.getText("sm_ui_res_qi");
                string tIntegrity = LocalizedTextManager.getText("sm_ui_res_integrity");
                string tReviveCount = LocalizedTextManager.getText("sm_ui_res_revive_count");
                text.text = $"<b>{state.name}</b>  <color=#8fa8c8>{rankName}</color>\n" +
                    $"<color=#8fa8c8>{tQi}:{state.qi:F0} {tIntegrity}:{integrity:F0%} {tReviveCount}:{state.reviveCount}</color>" +
                    (string.IsNullOrEmpty(reason) ? "" : $"\n<color=#ff9966>{reason}</color>");

                // 复活按钮
                var btnGo = new GameObject("ResurrectBtn");
                btnGo.transform.SetParent(itemGo.transform, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(1, 0.5f);
                btnRect.anchorMax = new Vector2(1, 0.5f);
                btnRect.pivot = new Vector2(1, 0.5f);
                btnRect.sizeDelta = new Vector2(80, 30);
                btnRect.anchoredPosition = new Vector2(-10, 0);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = canRes ? new Color(0.2f, 0.5f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                var btn = btnGo.AddComponent<Button>();
                btn.interactable = canRes;
                var btnText = SuperMechUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText("sm_ui_resurrect"), 12, TextAnchor.MiddleCenter);
                btnText.color = canRes ? Color.white : SuperMechUiSkin.TextDim;
                var btnTextRect = btnText.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;

                var capturedState = state;
                btn.onClick.AddListener(() =>
                {
                    var resurrected = SuperMechResurrection.Resurrect(capturedState);
                    if (resurrected != null)
                    {
                        Debug.Log($"[超神机械师] 复活成功: {capturedState.name}");
                    }
                    RefreshList();
                });
            }
        }

        private string GetCannotReason(SuperMechInformationState.InformationStateRecord state)
        {
            if (state.rankIndex < SuperMechResurrection.MinRankForResurrect)
                return LocalizedTextManager.getText("sm_ui_resurrect_rank_low");
            float integrity = SuperMechResurrection.CalculateInformationIntegrity(state);
            if (integrity < SuperMechResurrection.MinInformationIntegrity)
                return LocalizedTextManager.getText("sm_ui_res_integrity_low");
            if (state.iterationId != SuperMechCosmicIteration.CurrentIteration)
                return LocalizedTextManager.getText("sm_ui_res_cross_iteration");
            float cost = SuperMechResurrection.GetResurrectCost(state);
            if (SuperMechSanctuary.Data.sanctuary_energy < cost)
            {
                string tEnergyLow = LocalizedTextManager.getText("sm_ui_res_energy_low");
                return $"{tEnergyLow}（{cost:F0}）";
            }
            return "";
        }
    }
}
