using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>宇宙宝物装备窗口（v0.76.48 接线：图鉴列表+装备按钮）</summary>
    public class SuperMechCosmicRelicView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _listContent;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] CosmicRelicView初始化失败: " + e); }
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

            var listGo = new GameObject("RelicList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -42);

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

            string equipped = SuperMechCosmicRelic.GetEquippedName(a);
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_cr_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_cr_equipped"),
                string.IsNullOrEmpty(equipped) ? LocalizedTextManager.getText("sm_ui_cr_none") : LocalizedTextManager.getText(equipped));

            foreach (var r in SuperMechCosmicRelic.Relics)
            {
                var rowGo = new GameObject("Relic_"+r.id);
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 56);

                string wonderMark = r.isWonder ? LocalizedTextManager.getText("sm_ui_cr_wonder") + " " : "";
                string line = string.Format("{0}{1}\n{2}",
                    wonderMark, LocalizedTextManager.getText(r.name), LocalizedTextManager.getText(r.desc));
                var txt = SuperMechUiSkin.MakeText(row, line, 12, TextAnchor.UpperLeft);
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(4, 2);
                tr.offsetMax = new Vector2(-130, -2);

                bool isCurrent = a.hasTrait(r.id);
                var btn = SuperMechUiSkin.MakeButton(row,
                    isCurrent ? LocalizedTextManager.getText("sm_ui_cr_equipped_short") : LocalizedTextManager.getText("sm_ui_cr_equip"), 12,
                    () => { SuperMechCosmicRelic.EquipCosmicRelic(a, r.id); RefreshAll(); });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(110, 26);
                br.anchoredPosition = new Vector2(-55, 0);
            }
        }
    }


    /// <summary>次级维度窗口（v0.76.48 接线：维度列表+进入/退出按钮）</summary>
}
