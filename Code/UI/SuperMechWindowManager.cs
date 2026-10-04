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
        public static void OpenOverview() { ToggleWindow("overview", "sm_tab_panel_title", 720, 680, SuperMechTab.DrawOverviewContent); }

        /// <summary>星海总览工作台页签映射：key=本地化键，draw=内嵌内容绘制；draw==null 表示点击开独立窗口（圣所）</summary>
        internal static readonly (string key, System.Action<RectTransform> draw)[] OverviewTabs = new (string, System.Action<RectTransform>)[]
        {
            ("sm_ui_knowledge", DrawKnowledgeContent),
            ("sm_ui_bag", DrawBagContent),
            ("sm_ui_spell_entry", DrawSpellContent),
            ("sm_ui_craft", DrawCraftContent),
            ("sm_ui_rank_window_title", DrawRankContent),
            ("sm_ui_combat_title", DrawCombatEnhanceContent),
            ("sm_ui_beast_title", DrawCosmicBeastContent),
            ("sm_ui_trade_title", DrawTradeContent),
            ("sm_ui_genetics_title", DrawGeneticsContent),
            ("sm_ui_stargate_title", DrawStarGateContent),
            ("sm_ui_intel_title", DrawIntelContent),
            ("sm_ui_faction_window_title", DrawFactionContent),
            ("sm_ui_sanctuary", null),
            ("sm_ui_player_title", DrawPlayerContent),
            ("sm_ui_legion_title", DrawLegionContent),
            ("sm_ui_tree_title", DrawWorldTreeContent),
            ("sm_ui_esgod_title", DrawEsGodContent),
        };

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

        internal static void DrawKnowledgeContent(RectTransform content)
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

        internal static void DrawBagContent(RectTransform content)
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

        internal static void DrawSpellContent(RectTransform content)
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

        internal static void DrawCraftContent(RectTransform content)
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

        internal static void DrawRankContent(RectTransform content)
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

        internal static void DrawFactionContent(RectTransform content)
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

        internal static void DrawTradeContent(RectTransform content)
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

        internal static void DrawGeneticsContent(RectTransform content)
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

        internal static void DrawStarGateContent(RectTransform content)
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

        internal static void DrawIntelContent(RectTransform content)
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

        internal static void DrawCosmicBeastContent(RectTransform content)
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

        internal static void DrawCombatEnhanceContent(RectTransform content)
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

        internal static void DrawPlayerContent(RectTransform content)
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

        internal static void DrawLegionContent(RectTransform content)
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

        internal static void DrawWorldTreeContent(RectTransform content)
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

        internal static void DrawEsGodContent(RectTransform content)
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

        internal static void DrawForgeContent(RectTransform content)
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
