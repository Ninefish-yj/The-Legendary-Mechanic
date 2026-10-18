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

        // v0.242.12 统一调大窗口尺寸：原578x425在1080p只占30%宽，内容拥挤布局错乱。
        // 统一参考帝国盒子比例，宽高各增约20%，内容区更舒展。
        public static void OpenKnowledge() { SuperMechKnowledgeView.OverrideActor = null; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 380, DrawKnowledgeContent); }
        public static void OpenKnowledge(Actor a) { SuperMechKnowledgeView.OverrideActor = a; ToggleWindow("knowledge", "sm_ui_knowledge", 680, 380, DrawKnowledgeContent); }
        public static void OpenBag() { SuperMechBagView.OverrideActor = null; ToggleWindow("bag", "sm_ui_bag", 680, 380, DrawBagContent); }
        public static void OpenBag(Actor a) { SuperMechBagView.OverrideActor = a; ToggleWindow("bag", "sm_ui_bag", 680, 380, DrawBagContent); }
        public static void OpenFusion() { SuperMechKnowledgeView.OverrideActor = null; SuperMechFusionView.OverrideActor = null; SuperMechKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 380, DrawKnowledgeContent); }
        public static void OpenFusion(Actor a) { SuperMechKnowledgeView.OverrideActor = a; SuperMechFusionView.OverrideActor = a; SuperMechKnowledgeView.OpenFusionTab(); ToggleWindow("knowledge", "sm_ui_knowledge", 680, 380, DrawKnowledgeContent); }
        public static void OpenSpell() { SuperMechSpellView.OverrideActor = null; ToggleWindow("spell", "sm_ui_spell_entry", 680, 380, DrawSpellContent); }
        public static void OpenSpell(Actor a) { SuperMechSpellView.OverrideActor = a; ToggleWindow("spell", "sm_ui_spell_entry", 680, 380, DrawSpellContent); }
        public static void OpenCraft(Actor a) { SuperMechCraftView.OverrideActor = a; ToggleWindow("craft", "sm_ui_craft", 640, 400, DrawCraftContent); }
        public static void OpenCosmicRelic(Actor a) { SuperMechCosmicRelicView.OverrideActor = a; ToggleWindow("relic", "sm_ui_cr_title", 480, 440, DrawCosmicRelicContent); }
        public static void OpenDimension(Actor a) { SuperMechDimensionView.OverrideActor = a; ToggleWindow("dimension", "sm_ui_dim_title", 510, 425, DrawDimensionContent); }
        public static void OpenVirtualGenesis(Actor a) { SuperMechVirtualGenesisView.OverrideActor = a; ToggleWindow("virtualgenesis", "sm_ui_vg_title", 460, 420, DrawVirtualGenesisContent); }
        public static void OpenMechLegion(Actor a) { SuperMechMechLegionView.OverrideActor = a; ToggleWindow("mechlegion", "sm_ui_mech_legion_title", 540, 400, DrawMechLegionContent); }
        public static void OpenBuilding() { ToggleWindow("building", "sm_ui_building_title", 540, 380, DrawBuildingContent); }
        public static void OpenRank() { ToggleWindow("rank", "sm_ui_rank_window_title", 720, 460, DrawRankContent); }
        public static void OpenFaction() { ToggleWindow("faction", "sm_ui_faction_window_title", 640, 380, DrawFactionContent); }
        public static void OpenPlayer() { ToggleWindow("player", "sm_ui_player_title", 640, 460, DrawPlayerContent); }
        public static void OpenWorldTree() { ToggleWindow("worldtree", "sm_ui_tree_title", 640, 460, DrawWorldTreeContent); }
        public static void OpenWorldBeyond() { ToggleWindow("worldbeyond", "sm_ui_worldbeyond_title", 680, 480, DrawWorldBeyondContent); }
        public static void OpenEconomy() { ToggleWindow("economy", "sm_ui_economy_title", 680, 380, DrawEconomyContent); }
        public static void OpenTech() { ToggleWindow("tech", "sm_ui_tech", 680, 520, DrawTechContent); }
        public static void OpenStar() { ToggleWindow("star", "sm_ui_star", 720, 560, DrawStarContent); }
        public static void OpenSanctuary() { SuperMechSanctuaryView.Toggle(); }

        /// <summary>v0.208 设置从星海总览移出，改为独立窗口</summary>
        public static void OpenSettings()
        {
            ToggleWindow("settings", "sm_ui_settings_title", 580, 420, DrawSettingsContent);
        }
        public static void OpenOverview() { ToggleWindow("overview", "sm_tab_panel_title", 720, 380, SuperMechTab.DrawOverviewContent); }
        /// <summary>v0.206 从王国面板打开总览，自动选中该王国</summary>
        public static void OpenOverview(Kingdom kingdom)
        {
            if (kingdom != null) SelectedKingdomId = kingdom.getID();
            ToggleWindow("overview", "sm_tab_panel_title", 720, 380, SuperMechTab.DrawOverviewContent);
        }

        /// <summary>v0.242.16 移除宇宙页签（子页签无法切换，用户要求删除）
        /// v0.242.21 添加降临者页签（第四天灾玩家列表+任务详情）
        /// 文明(全球对比) / 超能者排行榜 / 组织(全球势力) / 降临者(第四天灾) / 设置</summary>
        internal static readonly (string key, System.Action<RectTransform> draw)[] OverviewTabs = new (string, System.Action<RectTransform>)[]
        {
            ("sm_ui_kingdom_window_title", DrawKingdomContent),      // 文明（全球对比表）
            ("sm_ui_rank_window_title", DrawRankContent),            // 超能者排行榜（全球超能者）
            ("sm_ui_faction_window_title", DrawFactionContent),      // 组织（全球势力）
            ("sm_ui_overview_player_tab", DrawPlayerContent),       // 降临者（第四天灾玩家）
            ("sm_ui_settings_title", DrawSettingsContent),           // 设置
        };

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

        /// <summary>v0.242.21 降临者页签：第四天灾玩家列表+击杀榜+任务详情
        /// v0.242.34 固定高度，顶部锚定，不依赖父物体拉伸（避免负高度翻转）
        /// v0.242.35 高度460→320，适配窗口可见区域
        /// v0.242.39 高度320→280，面板236→200，避免底部BottomBar被裁到窗口外</summary>
        internal static void DrawPlayerContent(RectTransform content)
        {
            // 降临者页签内容不多，把ScrollContent高度降到280，不需要滚动
            content.sizeDelta = new Vector2(0, 280);

            var go = new GameObject("PlayerView");
            go.transform.SetParent(content, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 280);
            go.AddComponent<SuperMechPlayerView>();
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
