using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    internal static class SMWindowManager
    {
        private static readonly Dictionary<string, SMUguiWindow> _windows = new Dictionary<string, SMUguiWindow>();
        private static readonly Dictionary<string, SMDraggableButton> _buttons = new Dictionary<string, SMDraggableButton>();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            CreateEntryButtons();
        }

        private static void CreateEntryButtons()
        {
            float startX = 80;
            float startY = Screen.height - 120;
            float spacing = 74;

            var btn1 = SMDraggableButton.Create("knowledge", "sm_ui_knowledge", "ui/Icons/iconBook", startX, startY);
            btn1.OnClick = () => ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent);
            _buttons["knowledge"] = btn1;

            var btn2 = SMDraggableButton.Create("bag", "sm_ui_bag", "ui/Icons/iconBackpack", startX + spacing, startY);
            btn2.OnClick = () => ToggleWindow("bag", "sm_ui_bag", 680, 500, DrawBagContent);
            _buttons["bag"] = btn2;

            var btn3 = SMDraggableButton.Create("craft", "sm_ui_craft", "ui/Icons/iconHammer", startX + spacing * 2, startY);
            btn3.OnClick = () => ToggleWindow("craft", "sm_ui_craft", 560, 420, DrawCraftContent);
            _buttons["craft"] = btn3;

            var btn4 = SMDraggableButton.Create("rank", "sm_ui_rank", "ui/Icons/iconCrown", startX + spacing * 3, startY);
            btn4.OnClick = () => ToggleWindow("rank", "sm_ui_rank_window_title", 620, 460, DrawRankContent);
            _buttons["rank"] = btn4;

            var btn5 = SMDraggableButton.Create("sanctuary", "sm_ui_sanctuary", "ui/Icons/iconBlessing", startX + spacing * 4, startY);
            btn5.OnClick = () => SMSanctuaryView.Toggle();
            _buttons["sanctuary"] = btn5;
        }

        public static void OpenKnowledge() { SMKnowledgeView.OverrideActor = null; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenKnowledge(Actor a) { SMKnowledgeView.OverrideActor = a; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenBag() { SMBagView.OverrideActor = null; ToggleWindow("bag", "sm_ui_bag", 680, 500, DrawBagContent); }
        public static void OpenBag(Actor a) { SMBagView.OverrideActor = a; ToggleWindow("bag", "sm_ui_bag", 680, 500, DrawBagContent); }
        public static void OpenFusion() { SMKnowledgeView.OverrideActor = null; SMFusionView.OverrideActor = null; SMKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenFusion(Actor a) { SMKnowledgeView.OverrideActor = a; SMFusionView.OverrideActor = a; SMKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenSpell() { SMSpellView.OverrideActor = null; ToggleWindow("spell", "sm_ui_spell_entry", 680, 500, DrawSpellContent); }
        public static void OpenSpell(Actor a) { SMSpellView.OverrideActor = a; ToggleWindow("spell", "sm_ui_spell_entry", 680, 500, DrawSpellContent); }
        public static void OpenCraft() { ToggleWindow("craft", "sm_ui_craft", 560, 420, DrawCraftContent); }
        public static void OpenRank() { ToggleWindow("rank", "sm_ui_rank_window_title", 620, 460, DrawRankContent); }
        public static void OpenSanctuary() { SMSanctuaryView.Toggle(); }
        public static void OpenResurrection() { ToggleWindow("resurrection", "sm_ui_resurrection", 620, 460, DrawResurrectionContent); }

        private static void ToggleWindow(string id, string titleKey, float w, float h, System.Action<RectTransform> drawContent)
        {
            if (_windows.TryGetValue(id, out var win) && win != null && win.IsOpen)
            {
                win.Close();
                _windows.Remove(id);
                return;
            }
            var newWin = SMUguiWindow.Create(id, titleKey, w, h);
            _windows[id] = newWin;
            drawContent?.Invoke(newWin.Content);
        }

        private static void DrawKnowledgeContent(RectTransform content)
        {
            var go = new GameObject("KnowledgeView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMKnowledgeView>();
        }

        private static void DrawBagContent(RectTransform content)
        {
            var go = new GameObject("BagView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMBagView>();
        }

        private static void DrawSpellContent(RectTransform content)
        {
            var go = new GameObject("SpellView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMSpellView>();
        }

        private static void DrawCraftContent(RectTransform content)
        {
            var go = new GameObject("CraftView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMCraftView>();
        }

        private static void DrawRankContent(RectTransform content)
        {
            var go = new GameObject("RankView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMRankView>();
        }

        private static void DrawResurrectionContent(RectTransform content)
        {
            var go = new GameObject("ResurrectionView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SMResurrectionView>();
        }

        public static void CloseAll()
        {
            foreach (var kv in _windows)
            {
                if (kv.Value != null) kv.Value.Close();
            }
            _windows.Clear();
        }

        public static void DestroyButtons()
        {
            foreach (var kv in _buttons)
            {
                if (kv.Value != null) Object.Destroy(kv.Value.gameObject);
            }
            _buttons.Clear();
            _initialized = false;
        }
    }
}
