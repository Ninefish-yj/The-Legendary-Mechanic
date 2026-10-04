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
        public static void OpenFaction() { ToggleWindow("faction", "sm_ui_faction_window_title", 620, 500, DrawFactionContent); }
        public static void OpenTrade() { ToggleWindow("trade", "sm_ui_trade_title", 640, 520, DrawTradeContent); }
        public static void OpenGenetics() { ToggleWindow("genetics", "sm_ui_genetics_title", 640, 520, DrawGeneticsContent); }
        public static void OpenStarGate() { ToggleWindow("stargate", "sm_ui_stargate_title", 640, 520, DrawStarGateContent); }
        public static void OpenIntel() { ToggleWindow("intel", "sm_ui_intel_title", 640, 520, DrawIntelContent); }
        public static void OpenCosmicBeast() { ToggleWindow("beast", "sm_ui_beast_title", 640, 520, DrawCosmicBeastContent); }
        public static void OpenCombatEnhance() { ToggleWindow("combat", "sm_ui_combat_title", 640, 520, DrawCombatEnhanceContent); }
        public static void OpenPlayer() { ToggleWindow("player", "sm_ui_player_title", 640, 520, DrawPlayerContent); }
        public static void OpenLegion() { ToggleWindow("legion", "sm_ui_legion_title", 640, 520, DrawLegionContent); }
        public static void OpenWorldTree() { ToggleWindow("worldtree", "sm_ui_tree_title", 640, 520, DrawWorldTreeContent); }
        public static void OpenEsGod() { ToggleWindow("esgod", "sm_ui_esgod_title", 640, 520, DrawEsGodContent); }
        public static void OpenForge() { ToggleWindow("forge", "sm_ui_forge_title", 640, 520, DrawForgeContent); }
        public static void OpenSanctuary() { SuperMechSanctuaryView.Toggle(); }

        public static void OpenOverview()
        {
            ToggleWindow("overview", "sm_ui_ov_title", 660, 580, DrawOverviewContent);
        }

        // v0.75.13: 神权栏收敛——18个窗口入口收纳为总览窗口分组列表
        private static float _ovY = -8f;
        private static int _ovCol = 0;

        private static void DrawOverviewContent(RectTransform content)
        {
            _ovY = -8f; _ovCol = 0;
            float colW = (content.rect.width - 40f) / 2f;
            if (colW < 200f) colW = 200f;

            void GroupTitle(string key)
            {
                var t = SuperMechUiSkin.MakeText(content, LocalizedTextManager.getText(key), 13, TextAnchor.MiddleLeft);
                var tr = t.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
                tr.pivot = new Vector2(0.5f, 1);
                tr.anchoredPosition = new Vector2(12, _ovY);
                tr.sizeDelta = new Vector2(0, 22);
                _ovY -= 28f;
            }

            void Entry(string nameKey, System.Action open)
            {
                int col = _ovCol;
                var b = SuperMechUiSkin.MakeButton(content, LocalizedTextManager.getText(nameKey), 12, open);
                var br = b.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(0, 1);
                br.pivot = new Vector2(0, 1);
                br.anchoredPosition = new Vector2(12 + col * (colW + 10), _ovY);
                br.sizeDelta = new Vector2(colW, 30);
                _ovCol ^= 1;
                if (_ovCol == 0) _ovY -= 38f;
            }

            GroupTitle("sm_ui_ov_growth");
            Entry("sm_ui_knowledge", () => OpenKnowledge());
            Entry("sm_ui_bag", () => OpenBag());
            Entry("sm_ui_spell_entry", () => OpenSpell());
            Entry("sm_ui_craft", () => OpenCraft());
            Entry("sm_ui_rank_window_title", () => OpenRank());
            if (_ovCol == 1) { _ovCol = 0; _ovY -= 38f; }

            GroupTitle("sm_ui_ov_war");
            Entry("sm_ui_combat_title", () => OpenCombatEnhance());
            Entry("sm_ui_beast_title", () => OpenCosmicBeast());
            if (_ovCol == 1) { _ovCol = 0; _ovY -= 38f; }

            GroupTitle("sm_ui_ov_world");
            Entry("sm_ui_trade_title", () => OpenTrade());
            Entry("sm_ui_genetics_title", () => OpenGenetics());
            Entry("sm_ui_stargate_title", () => OpenStarGate());
            Entry("sm_ui_intel_title", () => OpenIntel());
            Entry("sm_ui_faction_window_title", () => OpenFaction());
            if (_ovCol == 1) { _ovCol = 0; _ovY -= 38f; }

            GroupTitle("sm_ui_ov_finale");
            Entry("sm_ui_sanctuary", () => OpenSanctuary());
            Entry("sm_ui_player_title", () => OpenPlayer());
            Entry("sm_ui_legion_title", () => OpenLegion());
            Entry("sm_ui_tree_title", () => OpenWorldTree());
            Entry("sm_ui_esgod_title", () => OpenEsGod());
            Entry("sm_ui_forge_title", () => OpenForge());
        }

        private static void ToggleWindow(string id, string titleKey, float w, float h, System.Action<RectTransform> drawContent)
        {
            if (_windows.TryGetValue(id, out var win) && win != null && win.IsOpen)
            {
                win.Close();
                _windows.Remove(id);
                return;
            }
            var newWin = SuperMechUguiWindow.Create(id, titleKey, w, h);
            if (newWin == null) return;
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

        private static void DrawFactionContent(RectTransform content)
        {
            var go = new GameObject("FactionView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechFactionView>();
        }

        private static void DrawTradeContent(RectTransform content)
        {
            var go = new GameObject("TradeView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechTradeView>();
        }

        private static void DrawGeneticsContent(RectTransform content)
        {
            var go = new GameObject("GeneticsView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechGeneticsView>();
        }

        private static void DrawStarGateContent(RectTransform content)
        {
            var go = new GameObject("StarGateView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechStarGateView>();
        }

        private static void DrawIntelContent(RectTransform content)
        {
            var go = new GameObject("IntelView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechIntelView>();
        }

        private static void DrawCosmicBeastContent(RectTransform content)
        {
            var go = new GameObject("CosmicBeastView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechCosmicBeastView>();
        }

        private static void DrawCombatEnhanceContent(RectTransform content)
        {
            var go = new GameObject("CombatEnhanceView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechCombatEnhanceView>();
        }

        private static void DrawPlayerContent(RectTransform content)
        {
            var go = new GameObject("PlayerView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechPlayerView>();
        }

        private static void DrawLegionContent(RectTransform content)
        {
            var go = new GameObject("LegionView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechLegionView>();
        }

        private static void DrawWorldTreeContent(RectTransform content)
        {
            var go = new GameObject("WorldTreeView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechWorldTreeView>();
        }

        private static void DrawEsGodContent(RectTransform content)
        {
            var go = new GameObject("EsGodView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechEsGodView>();
        }

        private static void DrawForgeContent(RectTransform content)
        {
            var go = new GameObject("ForgeView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechForgeView>();
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
