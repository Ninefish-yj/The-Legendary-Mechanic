using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>建筑管理视图（神权面板）
    /// 显示所有自定义建筑的列表、等级、状态</summary>
    public class SuperMechBuildingView : MonoBehaviour
    {
        private RectTransform _content;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] BuildingView初始化失败: " + e); }
        }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] BuildingView刷新失败: " + e); } }

        private void BuildLayout()
        {
            _headText = SuperMechUiSkin.MakeText(transform,
                LocalizedTextManager.getText("sm_ui_building_title"), 16, TextAnchor.UpperCenter);
            var hr = _headText.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(12, -32);
            hr.offsetMax = new Vector2(-12, -8);

            var contentGo = new GameObject("BuildingContent");
            contentGo.transform.SetParent(transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 0);
            _content.anchorMax = new Vector2(1, 1);
            _content.offsetMin = new Vector2(12, 12);
            _content.offsetMax = new Vector2(-12, -48);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _content == null) return;
            foreach (Transform child in _content) Destroy(child.gameObject);

            if (World.world == null || World.world.buildings == null) return;

            // v0.206 获取当前选中的王国（跨标签页联动）
            Kingdom selectedKingdom = null;
            long selId = SuperMechWindowManager.SelectedKingdomId;
            if (selId >= 0 && World.world.kingdoms != null)
            {
                foreach (var k in World.world.kingdoms.list)
                    if (k != null && k.getID() == selId) { selectedKingdom = k; break; }
            }
            string kingdomLabel = selectedKingdom != null
                ? selectedKingdom.data.name
                : LocalizedTextManager.getText("sm_ui_economy_all_kingdoms");
            _headText.text = LocalizedTextManager.getText("sm_ui_building_title") + "  [" + kingdomLabel + "]";

            // 统计自定义建筑（按选中王国过滤）
            var buildingStats = new Dictionary<string, int>();
            var buildingLevels = new Dictionary<string, Dictionary<int, int>>();

            foreach (var b in World.world.buildings)
            {
                if (b == null || b.asset == null || b.isRuin()) continue;
                // 王国过滤
                if (selectedKingdom != null && (b.city == null || b.city.kingdom != selectedKingdom)) continue;
                string id = b.asset.id;
                if (!SuperMechBuilding.IsCustomBuilding(id)) continue;

                if (!buildingStats.ContainsKey(id)) buildingStats[id] = 0;
                buildingStats[id]++;

                int level = SuperMechBuildingUpgrade.GetLevel(b);
                if (!buildingLevels.ContainsKey(id)) buildingLevels[id] = new Dictionary<int, int>();
                if (!buildingLevels[id].ContainsKey(level)) buildingLevels[id][level] = 0;
                buildingLevels[id][level]++;
            }

            float y = -8f;
            int total = 0;
            foreach (var kv in buildingStats)
            {
                total += kv.Value;
                string name = LocalizedTextManager.getText(kv.Key);
                string levelInfo = "";
                if (buildingLevels.TryGetValue(kv.Key, out var levels))
                {
                    foreach (var lv in levels)
                    {
                        levelInfo += $"Lv{lv.Key}×{lv.Value} ";
                    }
                }

                var unit = LocalizedTextManager.getText("sm_ui_building_count");
                var row = SuperMechUiSkin.MakeText(_content,
                    $"{name}: {kv.Value}{unit}  ({levelInfo.Trim()})",
                    13, TextAnchor.UpperLeft);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.offsetMin = new Vector2(8, y - 24);
                rowRect.offsetMax = new Vector2(-8, y);
                y -= 30f;
            }

            _headText.text = $"{LocalizedTextManager.getText("sm_ui_building_title")}  {LocalizedTextManager.getText("sm_ui_building_total").Replace("{0}", total.ToString())}";

            if (total == 0)
            {
                var txt = SuperMechUiSkin.MakeText(_content,
                    LocalizedTextManager.getText("sm_ui_building_none"),
                    13, TextAnchor.UpperCenter);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 1);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(4, -20);
                tr.offsetMax = new Vector2(-4, -60);
            }
        }
    }
}
