using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.66.0 基因科研中心窗口：文明科研进度 + 基因数据库（基因优化/升华）
    /// 原著：基因链是力量基础，基因优化/升华消耗潜能点，契合度决定升华结果
    /// </summary>
    public class SuperMechGeneticsView : MonoBehaviour
    {
        private RectTransform _civContent;
        private RectTransform _dbContent;
        private Text _countText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] GeneticsView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] GeneticsView刷新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部统计
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, "", 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            // 左：文明科研进度
            var civPanel = SuperMechUiBuilder.CreatePanel(transform, "CivPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var cpRect = civPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0, 0.45f);
            cpRect.anchorMax = new Vector2(1, 1);
            cpRect.pivot = new Vector2(0.5f, 1);
            cpRect.offsetMin = new Vector2(4, 4);
            cpRect.offsetMax = new Vector2(-4, -32);
            var cpTitle = SuperMechUiBuilder.AddText(civPanel, LocalizedTextManager.getText("sm_ui_genetics_civ_research"), 12, TextAnchor.UpperCenter);
            var cpt = cpTitle.GetComponent<RectTransform>();
            cpt.anchorMin = new Vector2(0, 1);
            cpt.anchorMax = new Vector2(1, 1);
            cpt.sizeDelta = new Vector2(0, 22);

            var civScroll = new GameObject("CivScroll");
            civScroll.transform.SetParent(civPanel.transform, false);
            var csRect = civScroll.AddComponent<RectTransform>();
            csRect.anchorMin = Vector2.zero;
            csRect.anchorMax = Vector2.one;
            csRect.offsetMin = new Vector2(4, 4);
            csRect.offsetMax = new Vector2(-4, -26);
            var csScroll = civScroll.AddComponent<ScrollRect>();
            var csVp = new GameObject("Viewport");
            csVp.transform.SetParent(civScroll.transform, false);
            var csVpRect = csVp.AddComponent<RectTransform>();
            csVpRect.anchorMin = Vector2.zero;
            csVpRect.anchorMax = Vector2.one;
            csVpRect.offsetMin = Vector2.zero;
            csVpRect.offsetMax = Vector2.zero;
            csVp.AddComponent<Mask>();
            var csContent = new GameObject("Content");
            csContent.transform.SetParent(csVp.transform, false);
            _civContent = csContent.AddComponent<RectTransform>();
            _civContent.anchorMin = new Vector2(0, 1);
            _civContent.anchorMax = new Vector2(1, 1);
            _civContent.pivot = new Vector2(0.5f, 1);
            _civContent.sizeDelta = new Vector2(0, 100);
            csScroll.viewport = csVpRect;
            csScroll.content = _civContent;
            csScroll.vertical = true;
            csScroll.horizontal = false;

            // 右：基因数据库
            var dbPanel = SuperMechUiBuilder.CreatePanel(transform, "DbPanel", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var dpRect = dbPanel.GetComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0, 0);
            dpRect.anchorMax = new Vector2(1, 0.45f);
            dpRect.pivot = new Vector2(0.5f, 0);
            dpRect.offsetMin = new Vector2(4, 4);
            dpRect.offsetMax = new Vector2(-4, 0);
            var dbTitle = SuperMechUiBuilder.AddText(dbPanel, LocalizedTextManager.getText("sm_ui_genetics_database"), 12, TextAnchor.UpperCenter);
            var dbt = dbTitle.GetComponent<RectTransform>();
            dbt.anchorMin = new Vector2(0, 1);
            dbt.anchorMax = new Vector2(1, 1);
            dbt.sizeDelta = new Vector2(0, 22);

            var dbScroll = new GameObject("DbScroll");
            dbScroll.transform.SetParent(dbPanel.transform, false);
            var dsRect = dbScroll.AddComponent<RectTransform>();
            dsRect.anchorMin = Vector2.zero;
            dsRect.anchorMax = Vector2.one;
            dsRect.offsetMin = new Vector2(4, 4);
            dsRect.offsetMax = new Vector2(-4, -26);
            var dsScroll = dbScroll.AddComponent<ScrollRect>();
            var dsVp = new GameObject("Viewport");
            dsVp.transform.SetParent(dbScroll.transform, false);
            var dsVpRect = dsVp.AddComponent<RectTransform>();
            dsVpRect.anchorMin = Vector2.zero;
            dsVpRect.anchorMax = Vector2.one;
            dsVpRect.offsetMin = Vector2.zero;
            dsVpRect.offsetMax = Vector2.zero;
            dsVp.AddComponent<Mask>();
            var dsContent = new GameObject("Content");
            dsContent.transform.SetParent(dsVp.transform, false);
            _dbContent = dsContent.AddComponent<RectTransform>();
            _dbContent.anchorMin = new Vector2(0, 1);
            _dbContent.anchorMax = new Vector2(1, 1);
            _dbContent.pivot = new Vector2(0.5f, 1);
            _dbContent.sizeDelta = new Vector2(0, 100);
            dsScroll.viewport = dsVpRect;
            dsScroll.content = _dbContent;
            dsScroll.vertical = true;
            dsScroll.horizontal = false;
        }

        private void RefreshAll()
        {
            RefreshCivList();
            RefreshDatabase();
        }

        private void RefreshCivList()
        {
            if (_civContent == null) return;
            for (int i = _civContent.childCount - 1; i >= 0; i--)
                Destroy(_civContent.GetChild(i).gameObject);

            var factions = SuperMechFaction.GetAllFactions();
            if (factions.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_civContent.gameObject, LocalizedTextManager.getText("sm_ui_genetics_no_faction"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var f in factions)
            {
                var row = new GameObject($"Civ_{f.id}");
                row.transform.SetParent(_civContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 26);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 28f;

                var bg = row.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.05f);

                float progress = SuperMechGenetics.GetCivResearchProgress(f.id);
                var label = SuperMechUiBuilder.AddText(row,
                    $"{f.name}  |  {LocalizedTextManager.getText("sm_ui_genetics_research")}: {progress * 100:F0}%",
                    10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 0);
                labelRect.offsetMax = new Vector2(-8, 0);
            }
            _civContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void RefreshDatabase()
        {
            if (_dbContent == null) return;
            for (int i = _dbContent.childCount - 1; i >= 0; i--)
                Destroy(_dbContent.GetChild(i).gameObject);

            var ranking = SuperMechGenetics.GetGeneRanking(20);
            _countText.text = $"{LocalizedTextManager.getText("sm_ui_genetics_database")}: {ranking.Count}  |  " +
                $"{LocalizedTextManager.getText("sm_ui_genetics_cost")}: 10+层数×5 / 升华100+50×次数";

            if (ranking.Count == 0)
            {
                var empty = SuperMechUiBuilder.AddText(_dbContent.gameObject, LocalizedTextManager.getText("sm_ui_genetics_empty"), 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0, 0.5f);
                er.anchorMax = new Vector2(1, 0.5f);
                er.sizeDelta = new Vector2(0, 30);
                return;
            }

            float y = 0f;
            foreach (var kv in ranking)
            {
                var a = kv.Key;
                var g = kv.Value;
                var row = new GameObject($"Gene_{a.id}");
                row.transform.SetParent(_dbContent, false);
                var rect = row.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 30);
                rect.anchoredPosition = new Vector2(0, -y);
                y += 32f;

                var bg = row.AddComponent<Image>();
                bg.color = g.crashed ? new Color(0.5f, 0.2f, 0.2f, 0.25f) : new Color(1, 1, 1, 0.05f);

                int potential = SuperMechPotential.GetPotential(a);
                var label = SuperMechUiBuilder.AddText(row,
                    $"{a.name}  |  {LocalizedTextManager.getText("sm_ui_genetics_chain")}:{g.chainLevel}  {LocalizedTextManager.getText("sm_ui_genetics_sublimate_short")}:{g.sublimations}  " +
                    $"{LocalizedTextManager.getText("sm_ui_genetics_potential")}:{potential}",
                    10, TextAnchor.MiddleLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = new Vector2(0.62f, 1);
                labelRect.offsetMin = new Vector2(6, 0);
                labelRect.offsetMax = new Vector2(0, 0);

                // 优化按钮
                CreateSmallButton(row, new Vector2(-58, 0), 52, 24,
                    LocalizedTextManager.getText("sm_ui_genetics_optimize"),
                    () => { SuperMechGenetics.TryGeneOptimize(a); RefreshDatabase(); });
                // 升华按钮（层数≥8可用）
                if (g.chainLevel >= SuperMechGenetics.SublimateMinLevel)
                {
                    CreateSmallButton(row, new Vector2(-116, 0), 52, 24,
                        LocalizedTextManager.getText("sm_ui_genetics_sublimate"),
                        () => { SuperMechGenetics.TryGeneSublimate(a); RefreshDatabase(); });
                }
            }
            _dbContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private void CreateSmallButton(GameObject parent, Vector2 pos, float w, float h, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(parent.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.sizeDelta = new Vector2(w, h);
            btnRect.anchoredPosition = pos;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.45f, 0.3f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            var btnText = SuperMechUiBuilder.AddText(btnGo, text, 9, TextAnchor.MiddleCenter);
            var btr = btnText.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = new Vector2(2, 0);
            btr.offsetMax = new Vector2(-2, 0);
            btn.onClick.AddListener(onClick);
        }
    }
}
