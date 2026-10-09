using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>次级维度窗口（原著：泡泡模型+空间壁跳跃+锚点坐标导航）</summary>
    public class SuperMechDimensionView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _listContent;
        private Text _headText;
        private Text _infoText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] DimensionView初始化失败: " + e); }
        }

        private Actor Target
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
            hr.offsetMin = new Vector2(12, -36);
            hr.offsetMax = new Vector2(-12, -8);

            _infoText = SuperMechUiSkin.MakeText(transform, "", 11, TextAnchor.UpperLeft);
            var ir = _infoText.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0, 1);
            ir.anchorMax = new Vector2(1, 1);
            ir.pivot = new Vector2(0.5f, 1);
            ir.offsetMin = new Vector2(12, -56);
            ir.offsetMax = new Vector2(-12, -38);

            var listGo = new GameObject("DimList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -72);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            Actor a = Target;
            if (a == null)
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_need_target");
                return;
            }

            string active = SuperMechDimension.GetActiveDimension(a);
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_dim_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_dim_active"),
                string.IsNullOrEmpty(active) ? LocalizedTextManager.getText("sm_ui_dim_none") : LocalizedTextManager.getText(SuperMechDimension.GetDef(active).name));

            // 维度跳跃负荷（原著：连续跳跃承受空间乱流撕扯）
            int jumps = SuperMechDimension._jumpCount.ContainsKey(a.id) ? SuperMechDimension._jumpCount[a.id] : 0;
            _infoText.text = string.Format("{0}: {1}/{2}    {3}",
                LocalizedTextManager.getText("sm_ui_dim_jump_load"), jumps, SuperMechDimension.MaxJumpsBeforeRest,
                LocalizedTextManager.getText("sm_ui_dim_world_model"));

            float yOffset = 0;
            foreach (var dim in SuperMechDimension.Dimensions)
            {
                var rowGo = new GameObject("Dim_"+dim.id);
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 78);
                row.anchoredPosition = new Vector2(0, -yOffset);
                yOffset += 82;

                bool canEnter = SuperMechDimension.CanEnter(a, dim);
                bool isActive = active == dim.id;
                string typeName = SuperMechDimension.GetDimensionTypeName(dim.type);
                string lockedMark = canEnter ? "" : "（" + LocalizedTextManager.getText("sm_ui_dim_locked") + "）";

                // 维度名称+类型+锚点
                string title = string.Format("{0}  [{1}]  {2}: {3}{4}",
                    LocalizedTextManager.getText(dim.name), typeName,
                    LocalizedTextManager.getText("sm_ui_dim_anchor"), dim.anchor, lockedMark);
                // 世界观描述
                string lore = LocalizedTextManager.getText(dim.lore);
                string line = string.Format("{0}\n{1}", title, lore);

                var txt = SuperMechUiSkin.MakeText(row, line, 11, TextAnchor.UpperLeft);
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(4, 2);
                tr.offsetMax = new Vector2(-130, -2);

                string btnLabel = isActive ? LocalizedTextManager.getText("sm_ui_dim_exit") :
                                  (canEnter ? LocalizedTextManager.getText("sm_ui_dim_enter") : LocalizedTextManager.getText("sm_ui_dim_locked"));
                var btn = SuperMechUiSkin.MakeButton(row, btnLabel, 11,
                    () => {
                        if (isActive) { SuperMechDimension.Exit(a); }
                        else { SuperMechDimension.Enter(a, dim); }
                        RefreshAll();
                    });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(110, 26);
                br.anchoredPosition = new Vector2(-55, 0);
                if (!canEnter && !isActive) btn.interactable = false;
            }
        }
    }
}
