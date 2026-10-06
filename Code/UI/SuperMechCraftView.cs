using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>机械制造（v0.76.48 壳子→真实实现）：配方列表+材料+冷却+制造按钮</summary>
    public class SuperMechCraftView : MonoBehaviour
    {
        public static Actor OverrideActor; // 制造者（单位面板入口传入）
        private RectTransform _listContent;
        private Text _headText;

        private static readonly Dictionary<string, string> MaterialNames = new Dictionary<string, string>
        {
            { "common_metals", "金属" }, { "gems", "宝石" }, { "stone", "石头" }, { "wood", "木材" },
            { "gold", "金" }, { "adamantine", "精金" }, { "dragon_scales", "龙鳞" }
        };

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private Actor Maker
        {
            get
            {
                if (OverrideActor != null && OverrideActor.isAlive()) return OverrideActor;
                return null;
            }
        }

        private void BuildLayout()
        {
            _headText = SuperMechUiSkin.MakeText(transform, "", 14, TextAnchor.UpperLeft);
            var hr = _headText.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(12, -60);
            hr.offsetMax = new Vector2(-12, -8);

            var listGo = new GameObject("CraftList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -66);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            Actor maker = Maker;
            if (maker == null || !maker.hasTrait(SuperMechTraits.ClassMech))
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_craft_none");
                SuperMechUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_craft_none"), 14, TextAnchor.UpperLeft);
                return;
            }

            var recipes = SuperMechCrafting.GetAvailableRecipes(maker);
            int minionCount = SuperMechCrafting.GetMinionCount(maker);
            _headText.text = string.Format("{0}: {1}    {2}: {3}/{4}",
                LocalizedTextManager.getText("sm_ui_craft_maker"), maker.getName(),
                LocalizedTextManager.getText("sm_ui_craft_minion"), minionCount, SuperMechConfig.MaxSummonedUnits);

            if (recipes.Count == 0)
            {
                SuperMechUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_craft_none"), 14, TextAnchor.UpperLeft);
                return;
            }

            foreach (var r in recipes)
            {
                var rowGo = new GameObject("Row");
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 44);

                string costStr = BuildCostText(r.cost);
                float cd = SuperMechCrafting.GetCraftCooldown(maker);
                string cdStr = cd > 0 ? string.Format(LocalizedTextManager.getText("sm_ui_craft_cd_left"), (int)cd) : "";
                string line = string.Format("{0}（{1}）\n{2}    {3}",
                    LocalizedTextManager.getText(r.name), LocalizedTextManager.getText(r.desc),
                    costStr, cdStr);
                var txt = SuperMechUiSkin.MakeText(row, line, 12, TextAnchor.UpperLeft);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(0, 0);
                tr.offsetMax = new Vector2(-120, 0);

                bool canCraft = cd <= 0 && SuperMechCrafting.HasMaterials(maker, r.cost);
                var btn = SuperMechUiSkin.MakeButton(row,
                    LocalizedTextManager.getText("sm_ui_craft_button"), 12,
                    () => { TryCraftAndRefresh(maker, r.id); });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(100, 30);
                br.anchoredPosition = new Vector2(-50, 0);
                if (!canCraft) btn.interactable = false;
            }
        }

        private string BuildCostText(Dictionary<string, int> cost)
        {
            if (cost == null || cost.Count == 0) return LocalizedTextManager.getText("sm_ui_craft_no_cost");
            var parts = new List<string>();
            foreach (var kv in cost)
            {
                string matName = MaterialNames.TryGetValue(kv.Key, out string mn) ? mn : kv.Key;
                parts.Add(string.Format("{0}×{1}", matName, kv.Value));
            }
            return string.Format("{0}: {1}", LocalizedTextManager.getText("sm_ui_craft_cost"), string.Join(" ", parts));
        }

        private void TryCraftAndRefresh(Actor maker, string recipeId)
        {
            if (maker == null || !maker.isAlive()) return;
            WorldTile tile = null;
            try { tile = maker.current_tile; } catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 获取制造者位置失败: {e.Message}"); }
            if (tile == null) return;
            bool ok = SuperMechCrafting.TryCraft(maker, recipeId, tile);
            if (ok)
            {
                RefreshAll();
            }
        }
    }

    /// <summary>超能者排行榜（v0.33.0重构：三栏布局+筛选+排序+统计）
    /// 左栏：体系/阶位筛选；中栏：排序列表+卡片；右栏：阶位/体系统计
    /// </summary>
}
