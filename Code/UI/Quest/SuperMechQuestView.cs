using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>任务/成就/声望UI面板（v0.184）
    /// 显示可接取任务、进行中任务、成就进度、声望等级</summary>
    public class SuperMechQuestView : MonoBehaviour
    {
        private static SuperMechQuestView _instance;
        public static bool IsOpen => _instance != null;

        private GameObject _panel;
        private Actor _selectedActor;
        private Text _contentText;
        private int _currentTab = 0; // 0=任务 1=成就 2=声望

        public static void Toggle(Actor actor = null)
        {
            if (_instance != null) { _instance.Close(); return; }
            Open(actor);
        }

        public static void Open(Actor actor = null)
        {
            if (_instance != null) return;
            var go = new GameObject("SMQuestView");
            _instance = go.AddComponent<SuperMechQuestView>();
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
            panelRect.sizeDelta = new Vector2(600, 500);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.sprite = SuperMechUiSkin.Panel;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;
            // v0.202 装饰角标

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -20);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.text = "任务 / 成就 / 声望";

            // 关闭按钮
            var closeBtn = SuperMechUiSkin.MakeButton(_panel.transform, "X", 14, () => Close());
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(30, 30);
            closeRect.anchoredPosition = new Vector2(-15, -20);

            // 页签按钮
            string[] tabs = { "任务", "成就", "声望" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int tabIdx = i;
                var tabBtn = SuperMechUiSkin.MakeButton(_panel.transform, tabs[i], 13, () => { _currentTab = tabIdx; Refresh(); });
                var tabRect = tabBtn.GetComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(0, 1);
                tabRect.anchorMax = new Vector2(0, 1);
                tabRect.sizeDelta = new Vector2(80, 30);
                tabRect.anchoredPosition = new Vector2(50 + i * 90, -55);
            }

            // 内容区域（v0.193 真正的滚动文本：ScrollRect + Mask + 自适应高度Text）
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRectRt = scrollGo.AddComponent<RectTransform>();
            scrollRectRt.anchorMin = new Vector2(0, 0);
            scrollRectRt.anchorMax = new Vector2(1, 1);
            scrollRectRt.offsetMin = new Vector2(15, 55);
            scrollRectRt.offsetMax = new Vector2(-15, -75);
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
            contentRect.sizeDelta = new Vector2(0, 400);
            sr.content = contentRect;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            _contentText = contentGo.AddComponent<Text>();
            _contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _contentText.fontSize = 13;
            _contentText.alignment = TextAnchor.UpperLeft;
            _contentText.color = SuperMechUiSkin.TextColor;
            _contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _contentText.verticalOverflow = VerticalWrapMode.Truncate;
            var textRt = _contentText.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            // v0.193 内容高度自适应
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // v0.185 底部操作按钮：一键接取/交付
            var acceptBtn = SuperMechUiSkin.MakeButton(_panel.transform, "一键接取", 12, () => AcceptAllQuests());
            var acceptRect = acceptBtn.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(0, 0);
            acceptRect.anchorMax = new Vector2(0, 0);
            acceptRect.sizeDelta = new Vector2(120, 35);
            acceptRect.anchoredPosition = new Vector2(80, 20);

            var turnInBtn = SuperMechUiSkin.MakeButton(_panel.transform, "一键交付", 12, () => TurnInAllQuests());
            var turnInRect = turnInBtn.GetComponent<RectTransform>();
            turnInRect.anchorMin = new Vector2(1, 0);
            turnInRect.anchorMax = new Vector2(1, 0);
            turnInRect.sizeDelta = new Vector2(120, 35);
            turnInRect.anchoredPosition = new Vector2(-80, 20);

            Refresh();
        }

        private void Refresh()
        {
            if (_contentText == null) return;
            if (_selectedActor == null)
            {
                _contentText.text = "请先选择一个单位";
                return;
            }

            if (_currentTab == 0) RefreshQuests();
            else if (_currentTab == 1) RefreshAchievements();
            else RefreshReputation();
        }

        private void RefreshQuests()
        {
            var sb = new System.Text.StringBuilder();
            var available = SuperMechQuestSystem.GetAvailableQuests(_selectedActor);
            sb.AppendLine($"<color=#E8C36A><b>可接取任务 ({available.Count})</b></color>");
            foreach (var q in available)
            {
                string qname = LocalizedTextManager.getText(q.nameKey);
                string qdesc = LocalizedTextManager.getText(q.descKey);
                sb.AppendLine($"  • {qname}");
                sb.AppendLine($"    <color=#93A4BC>{qdesc}</color>");
                sb.AppendLine($"    奖励: {q.inalReward}₡ 经验:{q.xpReward}");
            }

            // 进行中任务
            var progress = SuperMechQuestSystem.GetProgress(_selectedActor);
            int activeCount = 0;
            foreach (var p in progress.Values)
            {
                if (p.status == SuperMechQuestSystem.QuestStatus.Active) activeCount++;
            }
            sb.AppendLine($"\n<color=#66ff99><b>进行中 ({activeCount})</b></color>");
            foreach (var p in progress.Values)
            {
                if (p.status != SuperMechQuestSystem.QuestStatus.Active) continue;
                var def = SuperMechQuestSystem.GetQuestDef(p.questId);
                if (def == null) continue;
                string qname = LocalizedTextManager.getText(def.nameKey);
                sb.AppendLine($"  • {qname} [{p.progress}/{def.objectiveCount}]");
            }

            // 已完成待交付
            int completedCount = 0;
            foreach (var p in progress.Values)
                if (p.status == SuperMechQuestSystem.QuestStatus.Completed) completedCount++;
            sb.AppendLine($"\n<color=#FFD700>待交付: {completedCount}</color>");

            // 已完成
            int doneCount = 0;
            foreach (var p in progress.Values)
                if (p.status == SuperMechQuestSystem.QuestStatus.TurnedIn) doneCount++;
            sb.AppendLine($"<color=#93A4BC>已交付: {doneCount}</color>");

            _contentText.text = sb.ToString();
        }

        private void RefreshAchievements()
        {
            var sb = new System.Text.StringBuilder();
            int unlocked = SuperMechQuestSystem.GetUnlockedCount(_selectedActor);
            int total = SuperMechQuestSystem.TotalAchievements;
            sb.AppendLine($"<color=#E8C36A><b>成就进度: {unlocked}/{total}</b></color>\n");

            var allAch = SuperMechQuestSystem.GetAllAchievements();
            foreach (var ach in allAch)
            {
                bool isUnlocked = SuperMechQuestSystem.IsAchievementUnlocked(_selectedActor, ach.id);
                string aname = LocalizedTextManager.getText(ach.nameKey);
                string adesc = LocalizedTextManager.getText(ach.descKey);
                string color = isUnlocked ? "#FFD700" : "#666666";
                string status = isUnlocked ? "✓" : "✗";
                sb.AppendLine($"<color={color}>{status} {aname}</color>");
                sb.AppendLine($"  <color=#93A4BC>{adesc}</color>");
                sb.AppendLine($"  奖励: {ach.inalReward}₡");
            }
            _contentText.text = sb.ToString();
        }

        private void RefreshReputation()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#E8C36A><b>势力声望</b></color>\n");

            var factions = SuperMechFaction.GetAllFactions();
            foreach (var f in factions)
            {
                int rep = SuperMechQuestSystem.GetReputation(_selectedActor, f.id);
                var level = SuperMechQuestSystem.GetReputationLevel(_selectedActor, f.id);
                string levelKey = SuperMechQuestSystem.GetReputationLevelKey(level);
                string levelName = LocalizedTextManager.getText(levelKey);
                string color = rep >= 0 ? "#66ff99" : "#ff6666";
                sb.AppendLine($"<color={color}>{f.name}: {rep} ({levelName})</color>");
            }
            _contentText.text = sb.ToString();
        }

        /// <summary>v0.185 一键接取所有可接取任务</summary>
        private void AcceptAllQuests()
        {
            if (_selectedActor == null) return;
            var available = SuperMechQuestSystem.GetAvailableQuests(_selectedActor);
            int count = 0;
            foreach (var q in available)
            {
                if (SuperMechQuestSystem.AcceptQuest(_selectedActor, q.id)) count++;
            }
            Debug.Log($"[超神机械师] 一键接取{count}个任务");
            Refresh();
        }

        /// <summary>v0.185 一键交付所有完成任务</summary>
        private void TurnInAllQuests()
        {
            if (_selectedActor == null) return;
            var progress = SuperMechQuestSystem.GetProgress(_selectedActor);
            int count = 0;
            foreach (var p in progress.Values)
            {
                if (p.status == SuperMechQuestSystem.QuestStatus.Completed)
                {
                    if (SuperMechQuestSystem.TurnInQuest(_selectedActor, p.questId)) count++;
                }
            }
            Debug.Log($"[超神机械师] 一键交付{count}个任务");
            Refresh();
        }
    }
}
