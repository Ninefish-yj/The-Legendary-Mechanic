using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>付费服务UI面板（v0.183经济闭环UI）
    /// 玩家可购买复活/传送/洗点/加速等服务</summary>
    public class SuperMechEconomyServiceView : MonoBehaviour
    {
        private static SuperMechEconomyServiceView _instance;
        public static bool IsOpen => _instance != null;

        private GameObject _panel;
        private Actor _selectedActor;
        private readonly Dictionary<SuperMechEconomyService.ServiceType, GameObject> _serviceButtons = new Dictionary<SuperMechEconomyService.ServiceType, GameObject>();

        public static void Toggle(Actor actor = null)
        {
            if (_instance != null) { _instance.Close(); return; }
            Open(actor);
        }

        public static void Open(Actor actor = null)
        {
            if (_instance != null) return;
            var go = new GameObject("SMEconomyServiceView");
            _instance = go.AddComponent<SuperMechEconomyServiceView>();
            _instance._selectedActor = actor;
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
            // 半透明背景
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
            panelRect.sizeDelta = new Vector2(400, 500);
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
            titleText.color = new Color(0.9f, 0.8f, 0.5f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = LocalizedTextManager.getText("sm_econ_service_panel");
            titleText.raycastTarget = false;

            // 伊纳尔余额显示
            if (_selectedActor != null)
            {
                var balanceGo = new GameObject("Balance");
                balanceGo.transform.SetParent(_panel.transform, false);
                var balanceRect = balanceGo.AddComponent<RectTransform>();
                balanceRect.anchorMin = new Vector2(0, 1);
                balanceRect.anchorMax = new Vector2(1, 1);
                balanceRect.pivot = new Vector2(0.5f, 1);
                balanceRect.sizeDelta = new Vector2(0, 25);
                balanceRect.anchoredPosition = new Vector2(0, -48);
                var balanceText = balanceGo.AddComponent<Text>();
                balanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                balanceText.fontSize = 13;
                balanceText.color = new Color(0.6f, 0.9f, 0.6f);
                balanceText.alignment = TextAnchor.MiddleCenter;
                balanceText.text = $"{_selectedActor.data.name}: {SuperMechInal.GetInal(_selectedActor)}₡";
                balanceText.raycastTarget = false;
            }

            // 服务列表
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            // v0.193 真正的滚动
            var sr = scrollGo.AddComponent<ScrollRect>();
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var scrollImg = scrollGo.AddComponent<Image>(); scrollImg.color = new Color(0, 0, 0, 0); scrollImg.raycastTarget = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            sr.content = contentRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            float y = 0f;
            var services = SuperMechEconomyService.GetAvailableServices(_selectedActor);
            foreach (var service in services)
            {
                CreateServiceButton(contentGo.transform, service, ref y);
            }
            // v0.193 内容高度自适应
            contentRect.sizeDelta = new Vector2(0, Mathf.Max(300, y + 20));

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

        private void CreateServiceButton(Transform parent, SuperMechEconomyService.ServiceType service, ref float y)
        {
            int cost = SuperMechEconomyService.GetCost(service, _selectedActor);
            string name = LocalizedTextManager.getText(SuperMechEconomyService.GetNameKey(service));
            string desc = LocalizedTextManager.getText(SuperMechEconomyService.GetDescKey(service));
            bool canAfford = _selectedActor != null && SuperMechInal.GetInal(_selectedActor) >= cost;

            var btnGo = new GameObject("Service_" + service);
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.sizeDelta = new Vector2(0, 60);
            btnRect.anchoredPosition = new Vector2(0, -y);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = canAfford ? new Color(0.15f, 0.2f, 0.25f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.7f);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (_selectedActor != null)
                {
                    bool success = SuperMechEconomyService.Purchase(service, _selectedActor);
                    if (success)
                    {
                        Debug.Log($"[超神机械师] 购买服务成功: {service}");
                        Close();
                    }
                }
            });

            // 服务名称
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(btnGo.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0.5f);
            nameRect.anchorMax = new Vector2(0.7f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 12;
            nameText.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            nameText.alignment = TextAnchor.LowerLeft;
            nameText.text = name;
            nameText.raycastTarget = false;

            // 服务描述
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(btnGo.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.02f, 0);
            descRect.anchorMax = new Vector2(0.7f, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            var descText = descGo.AddComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 10;
            descText.color = new Color(0.7f, 0.7f, 0.7f);
            descText.alignment = TextAnchor.UpperLeft;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.text = desc;
            descText.raycastTarget = false;

            // 价格
            var priceGo = new GameObject("Price");
            priceGo.transform.SetParent(btnGo.transform, false);
            var priceRect = priceGo.AddComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0.7f, 0);
            priceRect.anchorMax = new Vector2(0.98f, 1);
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;
            var priceText = priceGo.AddComponent<Text>();
            priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceText.fontSize = 14;
            priceText.color = canAfford ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.9f, 0.5f, 0.5f);
            priceText.alignment = TextAnchor.MiddleRight;
            priceText.fontStyle = FontStyle.Bold;
            priceText.text = $"{cost}₡";
            priceText.raycastTarget = false;

            y += 65f;
        }
    }
}
