using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>v0.198 超A协会排名面板
    /// MMO方向：按战力指数排名显示所有超A级个体，原著超A协会风格</summary>
    public class SuperMechSuperARankingView : MonoBehaviour
    {
        private static SuperMechSuperARankingView _instance;
        private GameObject _panel;
        private RectTransform _content;

        public static void Toggle()
        {
            if (_instance == null)
            {
                var go = new GameObject("SuperARankingView");
                _instance = go.AddComponent<SuperMechSuperARankingView>();
                _instance.BuildPanel();
            }
            else
            {
                _instance._panel.SetActive(!_instance._panel.activeSelf);
                if (_instance._panel.activeSelf) _instance.Refresh();
            }
        }

        private void BuildPanel()
        {
            // 面板背景
            _panel = new GameObject("RankingPanel");
            _panel.transform.SetParent(transform, false);
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 500);
            panelRect.anchoredPosition = Vector2.zero;

            // 标题栏
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panel.transform, false);
            var titleImg = titleBar.AddComponent<Image>();
            titleImg.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            var titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(0, 36);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = SuperMechUtils.CreateText(titleBar.transform,
                LocalizedTextManager.getText("sm_ui_supera_ranking"), 16, TextAnchor.MiddleCenter);
            titleText.rectTransform.anchorMin = Vector2.zero;
            titleText.rectTransform.anchorMax = Vector2.one;
            titleText.rectTransform.offsetMin = new Vector2(40, 0);
            titleText.rectTransform.offsetMax = new Vector2(-40, 0);
            titleText.color = new Color(1f, 0.85f, 0.4f, 1f);

            // 关闭按钮
            var closeBtn = SuperMechUiSkin.MakeButton(_panel.transform, "X", 14, Close);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(28, 28);
            closeRect.anchoredPosition = new Vector2(-16, -18);

            // 滚动区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8, 44);
            scrollRect.offsetMax = new Vector2(-8, 8);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0.1f, 0.12f, 0.15f, 0.5f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = vpRect;

            _content = new GameObject("Content").AddComponent<RectTransform>();
            _content.transform.SetParent(viewport.transform, false);
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = new Vector2(0, 0);
            scroll.content = _content;

            Refresh();
        }

        private void Refresh()
        {
            // 清除旧内容
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            // 收集所有超A并按战力排名
            var superAList = new List<(Actor actor, float power)>();
            foreach (var kv in World.world.units)
            {
                Actor a = kv.Value;
                if (a != null && a.isAlive() && SuperMechSupermA.IsSuperA(a))
                {
                    superAList.Add((a, SuperMechSupermA.GetPowerIndex(a)));
                }
            }
            superAList.Sort((x, y) => y.power.CompareTo(x.power));

            if (superAList.Count == 0)
            {
                var emptyText = SuperMechUtils.CreateText(_content,
                    LocalizedTextManager.getText("sm_ui_supera_empty"), 14, TextAnchor.MiddleCenter);
                emptyText.rectTransform.anchorMin = new Vector2(0, 1);
                emptyText.rectTransform.anchorMax = new Vector2(1, 1);
                emptyText.rectTransform.sizeDelta = new Vector2(0, 40);
                emptyText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                _content.sizeDelta = new Vector2(0, 60);
                return;
            }

            float y = 0;
            int rank = 1;
            foreach (var (actor, power) in superAList)
            {
                var row = CreateRankRow(rank, actor, power);
                row.anchoredPosition = new Vector2(0, -y);
                y += 52;
                rank++;
            }
            _content.sizeDelta = new Vector2(0, y + 10);
        }

        private RectTransform CreateRankRow(int rank, Actor actor, float power)
        {
            var rowGo = new GameObject($"Rank_{rank}");
            rowGo.transform.SetParent(_content, false);
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.sizeDelta = new Vector2(0, 48);
            rowRect.pivot = new Vector2(0.5f, 1);

            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = rank <= 3
                ? new Color(0.3f, 0.25f, 0.1f, 0.6f)
                : new Color(0.15f, 0.18f, 0.22f, 0.6f);

            // 排名数字
            string rankStr = rank == 1 ? "①" : rank == 2 ? "②" : rank == 3 ? "③" : rank.ToString();
            var rankText = SuperMechUtils.CreateText(rowGo.transform, rankStr, 18, TextAnchor.MiddleCenter);
            rankText.rectTransform.anchorMin = new Vector2(0, 0);
            rankText.rectTransform.anchorMax = new Vector2(0, 1);
            rankText.rectTransform.sizeDelta = new Vector2(44, 0);
            rankText.color = rank <= 3 ? new Color(1f, 0.85f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);

            // 单位名
            string name = actor.getName();
            string className = GetClassName(actor);
            string rankName = SuperMechRanks.GetRankName(actor);
            var nameText = SuperMechUtils.CreateText(rowGo.transform,
                $"{name}\n<size=11>{className} · {rankName}</size>", 13, TextAnchor.MiddleLeft);
            nameText.rectTransform.anchorMin = new Vector2(0, 0);
            nameText.rectTransform.anchorMax = new Vector2(1, 1);
            nameText.rectTransform.offsetMin = new Vector2(48, 0);
            nameText.rectTransform.offsetMax = new Vector2(-80, 0);
            nameText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            // 战力指数
            var powerText = SuperMechUtils.CreateText(rowGo.transform,
                power.ToString("F0"), 14, TextAnchor.MiddleRight);
            powerText.rectTransform.anchorMin = new Vector2(1, 0);
            powerText.rectTransform.anchorMax = new Vector2(1, 1);
            powerText.rectTransform.sizeDelta = new Vector2(72, 0);
            powerText.color = new Color(0.4f, 0.8f, 1f, 1f);

            return rowRect;
        }

        private string GetClassName(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMechanic)) return LocalizedTextManager.getText("trait_sm_class_mech");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("trait_sm_class_martial");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("trait_sm_class_mage");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("trait_sm_class_psi");
            if (a.hasTrait(SuperMechTraits.ClassPower)) return LocalizedTextManager.getText("trait_sm_class_power");
            return "?";
        }

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
