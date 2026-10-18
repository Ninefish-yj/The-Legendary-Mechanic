using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.39 方案A重写：超能详情窗口完全用原版UI组件。
    /// 窗口框架：WindowCreator.CreateEmptyWindow（原版ScrollWindow）
    /// 内容组件：KeyValueField（原版属性行，Resources.Load "ui/KeyValueFieldStats"）
    /// 不再自绘Text/Image/背景，零布局问题。
    /// </summary>
    public static class SuperMechActorDetailWindow
    {
        private static SuperMechUguiWindow _window;
        private static KeyValueField _rowPrefab;
        private static readonly List<KeyValueField> _rows = new List<KeyValueField>();

        public static void Open(Actor actor)
        {
            if (actor == null || actor.isRekt()) return;

            // 关闭已打开的窗口
            if (_window != null && _window.IsOpen)
            {
                _window.Close();
                _window = null;
            }

            // 加载行预制体（原版组件）
            if (_rowPrefab == null)
            {
                _rowPrefab = Resources.Load<KeyValueField>("ui/KeyValueFieldStats");
                if (_rowPrefab == null)
                {
                    Debug.LogError("[超神机械师] 加载KeyValueFieldStats预制体失败");
                    return;
                }
            }

            // 创建窗口（原版框架）
            _window = SuperMechUguiWindow.Create("sm_actor_detail",
                LocalizedTextManager.getText("sm_ui_actor_detail_title"), 480, 420);
            if (_window == null) return;

            BuildContent(_window.Content, actor);
        }

        private static void BuildContent(RectTransform content, Actor actor)
        {
            _rows.Clear();

            // 滚动区（原版ScrollRect模式）
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(content, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(4, 4); sr.offsetMax = new Vector2(-4, -4);

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewportGo.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 600);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 添加属性行（原版KeyValueField组件）
            AddRow(contentRect.transform, "sm_ui_actor_name", actor.getName(), "#FFD700");
            AddRow(contentRect.transform, "sm_ui_actor_species",
                LocalizedTextManager.getText(actor.asset.getLocaleID()), "#FFFFFF");

            // 分隔
            AddRow(contentRect.transform, "sm_ui_super_mech_divider", "─── 超能 ───", "#8B7355");

            // 职业
            var prof = SuperMechProfession.GetProfession(actor);
            AddRow(contentRect.transform, "sm_ui_profession",
                prof != SuperMechProfession.ProfessionType.None ? SuperMechProfession.GetProfessionName(prof) : "-",
                "#FFD700");

            // 阶位
            int rankIdx = SuperMechAdvancement.GetRankIndex(actor);
            AddRow(contentRect.transform, "sm_ui_rank",
                rankIdx > 0 ? SuperMechRanks.GetRankNameWithSub(actor) : "-",
                "#FF6B6B");

            // 觉醒等级
            int awakenedLv = SuperMechAwakened.GetLevel(actor);
            AddRow(contentRect.transform, "sm_ui_awakened_level",
                awakenedLv > 0 ? SuperMechAwakened.GetLevelText(actor) : "-",
                "#4ECDC4");

            // 知识数量
            int knowledgeCount = SuperMechKnowledge.GetUnlockedCount(actor, "");
            AddRow(contentRect.transform, "sm_ui_knowledge_count",
                knowledgeCount > 0 ? knowledgeCount.ToString() : "-",
                "#95E1D3");

            // 装备数量
            var bag = SuperMechEquipBag.GetBag(actor);
            int equipCount = bag != null ? bag.Count : 0;
            AddRow(contentRect.transform, "sm_ui_equipment_count",
                equipCount > 0 ? equipCount.ToString() : "-",
                "#F38181");

            // 法术数量
            int spellCount = SuperMechSpell.GetLearnedCount(actor);
            AddRow(contentRect.transform, "sm_ui_spell_count",
                spellCount > 0 ? spellCount.ToString() : "-",
                "#AA96DA");

            // 动态计算内容高度
            float totalHeight = 4 * 2 + _rows.Count * 32f;
            contentRect.sizeDelta = new Vector2(0, Mathf.Max(totalHeight, 200));
        }

        private static void AddRow(Transform parent, string nameKey, string valueText, string valueColor)
        {
            if (_rowPrefab == null) return;
            var row = Object.Instantiate(_rowPrefab, parent, false);
            row.name_text.text = LocalizedTextManager.getText(nameKey);
            row.value.text = Toolbox.coloredString(valueText, valueColor);
            row.icon.gameObject.SetActive(false);
            row.icon_secondary.gameObject.SetActive(false);
            _rows.Add(row);
        }
    }
}
