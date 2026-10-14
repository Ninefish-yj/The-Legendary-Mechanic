using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>势力外交UI面板（v0.183外交系统UI）
    /// 玩家可宣战/结盟/停战/进贡</summary>
    public class SuperMechDiplomacyView : MonoBehaviour
    {
        private static SuperMechDiplomacyView _instance;
        public static bool IsOpen => _instance != null;

        private GameObject _panel;
        private string _playerFactionId;

        public static void Toggle()
        {
            if (_instance != null) { _instance.Close(); return; }
            Open();
        }

        public static void Open()
        {
            if (_instance != null) return;
            var go = new GameObject("SMDiplomacyView");
            _instance = go.AddComponent<SuperMechDiplomacyView>();
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
            panelRect.sizeDelta = new Vector2(450, 500);
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
            titleText.color = new Color(0.9f, 0.7f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = LocalizedTextManager.getText("sm_faction_diplomacy");
            titleText.raycastTarget = false;

            // 势力列表
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.88f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 600);

            // 获取玩家势力
            _playerFactionId = GetPlayerFactionId();
            var allFactions = SuperMechFaction.GetAllFactions();

            float y = 0f;
            foreach (var faction in allFactions)
            {
                if (faction.id == _playerFactionId) continue;
                CreateFactionRow(contentGo.transform, faction, ref y);
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

        private string GetPlayerFactionId()
        {
            // 找到玩家单位所属的势力
            if (World.world?.units != null)
            {
                foreach (var a in World.world.units)
                {
                    if (a != null && a.isAlive() && a.hasTrait(SuperMechPlayer.PlayerTrait))
                    {
                        return SuperMechFaction.GetActorFactionId(a);
                    }
                }
            }
            return null;
        }

        private void CreateFactionRow(Transform parent, SuperMechFaction.FactionData faction, ref float y)
        {
            var relation = string.IsNullOrEmpty(_playerFactionId)
                ? SuperMechFaction.FactionRelation.Neutral
                : SuperMechFaction.GetRelation(_playerFactionId, faction.id);
            string relationText = SuperMechFaction.GetRelationDescription(relation);
            Color relationColor = relation == SuperMechFaction.FactionRelation.Allied
                ? new Color(0.4f, 0.8f, 0.4f)
                : relation == SuperMechFaction.FactionRelation.Hostile
                    ? new Color(0.9f, 0.4f, 0.4f)
                    : new Color(0.7f, 0.7f, 0.7f);

            var rowGo = new GameObject("Faction_" + faction.id);
            rowGo.transform.SetParent(parent, false);
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.sizeDelta = new Vector2(0, 80);
            rowRect.anchoredPosition = new Vector2(0, -y);
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = new Color(0.12f, 0.15f, 0.2f, 0.9f);

            // 势力名称
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0.6f);
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 13;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.LowerLeft;
            nameText.fontStyle = FontStyle.Bold;
            nameText.text = faction.name;
            nameText.raycastTarget = false;

            // 关系
            var relGo = new GameObject("Relation");
            relGo.transform.SetParent(rowGo.transform, false);
            var relRect = relGo.AddComponent<RectTransform>();
            relRect.anchorMin = new Vector2(0.02f, 0.3f);
            relRect.anchorMax = new Vector2(0.6f, 0.6f);
            relRect.offsetMin = Vector2.zero;
            relRect.offsetMax = Vector2.zero;
            var relText = relGo.AddComponent<Text>();
            relText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            relText.fontSize = 11;
            relText.color = relationColor;
            relText.alignment = TextAnchor.UpperLeft;
            relText.text = relationText;
            relText.raycastTarget = false;

            // 操作按钮区域
            float btnX = 0.62f;
            float btnWidth = 0.18f;

            // 宣战/停战
            if (relation == SuperMechFaction.FactionRelation.Hostile)
            {
                CreateDiplomacyButton(rowGo.transform, "sm_faction_make_peace", new Color(0.3f, 0.5f, 0.7f, 0.9f), btnX, btnWidth, () =>
                {
                    if (!string.IsNullOrEmpty(_playerFactionId))
                    {
                        SuperMechFaction.MakePeace(_playerFactionId, faction.id);
                        Close(); Open();
                    }
                });
            }
            else
            {
                CreateDiplomacyButton(rowGo.transform, "sm_faction_declare_war", new Color(0.8f, 0.3f, 0.3f, 0.9f), btnX, btnWidth, () =>
                {
                    if (!string.IsNullOrEmpty(_playerFactionId))
                    {
                        SuperMechFaction.DeclareWar(_playerFactionId, faction.id);
                        Close(); Open();
                    }
                });
            }

            // 结盟/断盟
            if (relation == SuperMechFaction.FactionRelation.Allied)
            {
                CreateDiplomacyButton(rowGo.transform, "sm_faction_break_alliance", new Color(0.6f, 0.5f, 0.3f, 0.9f), btnX + btnWidth + 0.01f, btnWidth, () =>
                {
                    if (!string.IsNullOrEmpty(_playerFactionId))
                    {
                        SuperMechFaction.BreakAlliance(_playerFactionId, faction.id);
                        Close(); Open();
                    }
                });
            }
            else if (relation == SuperMechFaction.FactionRelation.Neutral)
            {
                CreateDiplomacyButton(rowGo.transform, "sm_faction_form_alliance", new Color(0.3f, 0.7f, 0.4f, 0.9f), btnX + btnWidth + 0.01f, btnWidth, () =>
                {
                    if (!string.IsNullOrEmpty(_playerFactionId))
                    {
                        SuperMechFaction.FormAlliance(_playerFactionId, faction.id);
                        Close(); Open();
                    }
                });
            }

            // 进贡
            CreateDiplomacyButton(rowGo.transform, "sm_faction_send_gift", new Color(0.7f, 0.6f, 0.3f, 0.9f), btnX + (btnWidth + 0.01f) * 2, btnWidth, () =>
            {
                if (!string.IsNullOrEmpty(_playerFactionId))
                {
                    SuperMechFaction.SendGift(_playerFactionId, faction.id, 100);
                    Close(); Open();
                }
            });

            y += 85f;
        }

        private void CreateDiplomacyButton(Transform parent, string textKey, Color color, float xRatio, float widthRatio, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(xRatio, 0.1f);
            btnRect.anchorMax = new Vector2(xRatio + widthRatio, 0.45f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = color;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 10;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = LocalizedTextManager.getText(textKey);
            btnText.raycastTarget = false;
        }
    }
}
