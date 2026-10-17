using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    internal static class SuperMechWindowManager
    {
        private static readonly Dictionary<string, SuperMechUguiWindow> _windows = new Dictionary<string, SuperMechUguiWindow>();

        /// <summary>v0.206 星海总览当前选中的原版王国ID（-1表示未选中）</summary>
        public static long SelectedKingdomId = -1;

        /// <summary>v0.75.35: 换存档时清空窗口引用缓存（防悬垂引用）</summary>
        public static void Clear()
        {
            _windows.Clear();
            SelectedKingdomId = -1;
        }
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
        public static void OpenCraft(Actor a) { SuperMechCraftView.OverrideActor = a; ToggleWindow("craft", "sm_ui_craft", 640, 480, DrawCraftContent); }
        public static void OpenCosmicRelic(Actor a) { SuperMechCosmicRelicView.OverrideActor = a; ToggleWindow("relic", "sm_ui_cr_title", 460, 420, DrawCosmicRelicContent); }
        public static void OpenDimension(Actor a) { SuperMechDimensionView.OverrideActor = a; ToggleWindow("dimension", "sm_ui_dim_title", 480, 400, DrawDimensionContent); }
        public static void OpenVirtualGenesis(Actor a) { SuperMechVirtualGenesisView.OverrideActor = a; ToggleWindow("virtualgenesis", "sm_ui_vg_title", 420, 380, DrawVirtualGenesisContent); }
        public static void OpenMechLegion(Actor a) { SuperMechMechLegionView.OverrideActor = a; ToggleWindow("mechlegion", "sm_ui_mech_legion_title", 480, 520, DrawMechLegionContent); }
        public static void OpenBuilding() { ToggleWindow("building", "sm_ui_building_title", 520, 480, DrawBuildingContent); }
        public static void OpenRank() { ToggleWindow("rank", "sm_ui_rank_window_title", 620, 460, DrawRankContent); }
        public static void OpenFaction() { ToggleWindow("faction", "sm_ui_faction_window_title", 620, 500, DrawFactionContent); }
        public static void OpenPlayer() { ToggleWindow("player", "sm_ui_player_title", 640, 520, DrawPlayerContent); }
        public static void OpenWorldTree() { ToggleWindow("worldtree", "sm_ui_tree_title", 640, 520, DrawWorldTreeContent); }
        public static void OpenWorldBeyond() { ToggleWindow("worldbeyond", "sm_ui_worldbeyond_title", 680, 560, DrawWorldBeyondContent); }
        public static void OpenEconomy() { ToggleWindow("economy", "sm_ui_economy_title", 680, 520, DrawEconomyContent); }
        public static void OpenTech() { ToggleWindow("tech", "sm_ui_tech", 680, 520, DrawTechContent); }
        public static void OpenStar() { ToggleWindow("star", "sm_ui_star", 720, 560, DrawStarContent); }
        public static void OpenSanctuary() { SuperMechSanctuaryView.Toggle(); }

        /// <summary>v0.208 设置从星海总览移出，改为独立窗口</summary>
        public static void OpenSettings()
        {
            ToggleWindow("settings", "sm_ui_settings_title", 520, 640, DrawSettingsContent);
        }
        public static void OpenOverview() { ToggleWindow("overview", "sm_tab_panel_title", 720, 680, SuperMechTab.DrawOverviewContent); }
        /// <summary>v0.206 从王国面板打开总览，自动选中该王国</summary>
        public static void OpenOverview(Kingdom kingdom)
        {
            if (kingdom != null) SelectedKingdomId = kingdom.getID();
            ToggleWindow("overview", "sm_tab_panel_title", 720, 680, SuperMechTab.DrawOverviewContent);
        }

        /// <summary>v0.230 星海总览重构：标签重命名+合并冗余
        /// 文明(全球对比) / 强者榜(全球超能者) / 组织(全球势力) / 宇宙(星际+高维) / 设置
        /// 移除世界树独立标签，合并进宇宙标签</summary>
        internal static readonly (string key, System.Action<RectTransform> draw)[] OverviewTabs = new (string, System.Action<RectTransform>)[]
        {
            ("sm_ui_kingdom_window_title", DrawKingdomContent),      // 文明（全球对比表）
            ("sm_ui_rank_window_title", DrawRankContent),            // 强者榜（全球超能者）
            ("sm_ui_faction_window_title", DrawFactionContent),      // 组织（全球势力）
            ("sm_ui_worldbeyond_title", DrawUniverseContent),        // 宇宙（星际+高维存在）
            ("sm_ui_settings_title", DrawSettingsContent),           // 设置
        };

        /// <summary>v0.230 宇宙标签：合并世界之外+世界树</summary>
        internal static void DrawUniverseContent(RectTransform content)
        {
            // 上半部分：世界之外（高维存在）
            var upperGo = new GameObject("WorldBeyond");
            upperGo.transform.SetParent(content, false);
            var upperRect = upperGo.AddComponent<RectTransform>();
            upperRect.anchorMin = new Vector2(0, 0.5f);
            upperRect.anchorMax = Vector2.one;
            upperRect.offsetMin = Vector2.zero;
            upperRect.offsetMax = new Vector2(0, -2);
            SuperMechWorldBeyondView.Draw(upperRect);

            // 下半部分：世界树（入侵事件）
            var lowerGo = new GameObject("WorldTree");
            lowerGo.transform.SetParent(content, false);
            var lowerRect = lowerGo.AddComponent<RectTransform>();
            lowerRect.anchorMin = Vector2.zero;
            lowerRect.anchorMax = new Vector2(1, 0.5f);
            lowerRect.offsetMin = Vector2.zero;
            lowerRect.offsetMax = new Vector2(0, 2);
            var treeGo = new GameObject("WorldTreeView");
            treeGo.transform.SetParent(lowerRect, false);
            var treeRect = treeGo.AddComponent<RectTransform>();
            treeRect.anchorMin = Vector2.zero;
            treeRect.anchorMax = Vector2.one;
            treeRect.offsetMin = Vector2.zero;
            treeRect.offsetMax = Vector2.zero;
            treeGo.AddComponent<SuperMechWorldTreeView>();
        }

        /// <summary>v0.205 设置标签页：大重启+配置+帮助</summary>
        internal static void DrawSettingsContent(RectTransform content)
        {
            var go = new GameObject("SettingsView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechSettingsView>();
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

        internal static void DrawDimensionContent(RectTransform content)
        {
            var go = new GameObject("DimensionView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechDimensionView>();
        }

        internal static void DrawVirtualGenesisContent(RectTransform content)
        {
            var go = new GameObject("VirtualGenesisView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechVirtualGenesisView>();
        }

        internal static void DrawMechLegionContent(RectTransform content)
        {
            var go = new GameObject("MechLegionView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechMechLegionView>();
        }

        internal static void DrawBuildingContent(RectTransform content)
        {
            var go = new GameObject("BuildingView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechBuildingView>();
        }

        internal static void DrawCosmicRelicContent(RectTransform content)
        {
            var go = new GameObject("CosmicRelicView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechCosmicRelicView>();
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

        internal static void DrawKingdomContent(RectTransform content)
        {
            var go = new GameObject("KingdomView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechKingdomView>();
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


        internal static void DrawWorldBeyondContent(RectTransform content)
        {
            SuperMechWorldBeyondView.Draw(content);
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

        internal static void DrawEconomyContent(RectTransform content)
        {
            var go = new GameObject("EconomyView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechEconomyView>();
        }

        internal static void DrawTechContent(RectTransform content)
        {
            var go = new GameObject("TechView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechTechView>();
        }

        internal static void DrawStarContent(RectTransform content)
        {
            var go = new GameObject("StarView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<SuperMechStarView>();
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
