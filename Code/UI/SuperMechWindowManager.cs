using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    internal static class SuperMechWindowManager
    {
        private static readonly Dictionary<string, SuperMechUguiWindow> _windows = new Dictionary<string, SuperMechUguiWindow>();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public static void OpenKnowledge() { SuperMechKnowledgeView.OverrideActor = null; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenKnowledge(Actor a) { SuperMechKnowledgeView.OverrideActor = a; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenBag() { SuperMechBagView.OverrideActor = null; ToggleWindow("bag", "sm_ui_bag", 680, 500, DrawBagContent); }
        public static void OpenBag(Actor a) { SuperMechBagView.OverrideActor = a; ToggleWindow("bag", "sm_ui_bag", 680, 500, DrawBagContent); }
        public static void OpenFusion() { SuperMechKnowledgeView.OverrideActor = null; SuperMechFusionView.OverrideActor = null; SuperMechKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenFusion(Actor a) { SuperMechKnowledgeView.OverrideActor = a; SuperMechFusionView.OverrideActor = a; SuperMechKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 500, DrawKnowledgeContent); }
        public static void OpenSpell() { SuperMechSpellView.OverrideActor = null; ToggleWindow("spell", "sm_ui_spell_entry", 680, 500, DrawSpellContent); }
        public static void OpenSpell(Actor a) { SuperMechSpellView.OverrideActor = a; ToggleWindow("spell", "sm_ui_spell_entry", 680, 500, DrawSpellContent); }
        public static void OpenCraft() { ToggleWindow("craft", "sm_ui_craft", 560, 420, DrawCraftContent); }
        public static void OpenRank() { ToggleWindow("rank", "sm_ui_rank_window_title", 620, 460, DrawRankContent); }
        public static void OpenSanctuary() { SuperMechSanctuaryView.Toggle(); }

        private static void ToggleWindow(string id, string titleKey, float w, float h, System.Action<RectTransform> drawContent)
        {
            if (_windows.TryGetValue(id, out var win) && win != null && win.IsOpen)
            {
                win.Close();
                _windows.Remove(id);
                return;
            }
            var newWin = SuperMechUguiWindow.Create(id, titleKey, w, h);
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
            go.AddComponent<SuperMechKnowledgeView>();
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
            go.AddComponent<SuperMechBagView>();
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
            go.AddComponent<SuperMechSpellView>();
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
            go.AddComponent<SuperMechCraftView>();
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
            go.AddComponent<SuperMechRankView>();
        }

        public static void CloseAll()
        {
            foreach (var kv in _windows)
            {
                if (kv.Value != null) kv.Value.Close();
            }
            _windows.Clear();
        }
    }
}
