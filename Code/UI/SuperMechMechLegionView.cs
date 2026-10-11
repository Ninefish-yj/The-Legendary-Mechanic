using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>机械军团制造窗口（机械师分支核心特色）
    /// 显示可制造的机械单位列表，点击制造</summary>
    public class SuperMechMechLegionView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _content;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] MechLegionView初始化失败: " + e); }
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

            var contentGo = new GameObject("MechLegionContent");
            contentGo.transform.SetParent(transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 0);
            _content.anchorMax = new Vector2(1, 1);
            _content.offsetMin = new Vector2(12, 12);
            _content.offsetMax = new Vector2(-12, -52);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _content == null) return;
            foreach (Transform child in _content) Destroy(child.gameObject);

            Actor a = Target;
            if (a == null)
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_need_target");
                return;
            }

            int potential = SuperMechPotential.GetPotential(a);
            _headText.text = $"{a.getName()}  |  {LocalizedTextManager.getText("sm_ui_potential")}: {potential}";

            var available = SuperMechMechLegion.GetAvailableTypes(a);
            if (available == null || available.Count == 0)
            {
                var txt = SuperMechUiSkin.MakeText(_content, LocalizedTextManager.getText("sm_ui_mech_none"),
                    13, TextAnchor.UpperCenter);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 1);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(4, -20);
                tr.offsetMax = new Vector2(-4, -60);
                return;
            }

            float y = -8f;
            foreach (var def in available)
            {
                string name = SuperMechMechLegion.GetUnitName(def.id);
                string desc = LocalizedTextManager.getText(def.desc);
                bool canAfford = potential >= def.cost;
                string costText = $"{def.cost} {LocalizedTextManager.getText("sm_ui_potential")}";

                var btnGo = new GameObject($"MechBtn_{def.id}");
                btnGo.transform.SetParent(_content, false);
                var btnRect = btnGo.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0, 1);
                btnRect.anchorMax = new Vector2(1, 1);
                btnRect.pivot = new Vector2(0.5f, 1);
                btnRect.offsetMin = new Vector2(4, y - 52);
                btnRect.offsetMax = new Vector2(-4, y);

                var btn = btnGo.AddComponent<Button>();
                var img = btnGo.AddComponent<Image>();
                img.color = canAfford ? new Color(0.3f, 0.5f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.6f);

                var label = SuperMechUiSkin.MakeText(btnGo.transform,
                    $"{name}  [{costText}]\n{desc}", 12, TextAnchor.UpperLeft);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 4);
                labelRect.offsetMax = new Vector2(-8, -4);

                if (canAfford)
                {
                    string typeId = def.id;
                    btn.onClick.AddListener(() =>
                    {
                        SuperMechMechLegion.SpawnMechUnit(a, typeId, a.currentTile);
                        RefreshAll();
                    });
                }

                y -= 58f;
            }
        }
    }
}
