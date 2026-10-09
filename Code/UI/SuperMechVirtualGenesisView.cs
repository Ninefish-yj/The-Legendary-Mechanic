using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>虚拟创世窗口（原著：韩萧融合世界树后的标志性能力）</summary>
    public class SuperMechVirtualGenesisView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _content;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] VirtualGenesisView初始化失败: " + e); }
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

            var contentGo = new GameObject("VGContent");
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

            var state = SuperMechVirtualGenesis.GetState(a);
            if (state == null || state.level == SuperMechVirtualGenesis.GenesisLevel.None)
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_vg_none");
                var txt = SuperMechUiSkin.MakeText(_content, LocalizedTextManager.getText("sm_ui_vg_desc_pseudo"), 12, TextAnchor.UpperLeft);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 1);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(4, -8);
                tr.offsetMax = new Vector2(-4, -60);
                return;
            }

            string levelName = state.level == SuperMechVirtualGenesis.GenesisLevel.True ?
                LocalizedTextManager.getText("sm_ui_vg_true") : LocalizedTextManager.getText("sm_ui_vg_pseudo");
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_dim_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_vg_title"), levelName);

            float y = 0;
            // 空间等级
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_level"),
                string.Format("{0}/10", state.spaceLevel));
            // 创世能量
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_energy"),
                string.Format("{0:F0}/10000", state.energy));
            // 修炼加成
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_cultivation_bonus"),
                string.Format("{0:F1}x", SuperMechVirtualGenesis.GetCultivationBonus(a)));
            // 经验加成
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_exp_bonus"),
                string.Format("{0:F1}x", SuperMechVirtualGenesis.GetExpBonus(a)));

            // 升级按钮
            y += 10;
            float cost = SuperMechVirtualGenesis.EnergyPerSpaceLevel * state.spaceLevel;
            bool canUpgrade = state.spaceLevel < 10 && state.energy >= cost;
            var btn = SuperMechUiSkin.MakeButton(_content,
                string.Format("{0} ({1:F0})", LocalizedTextManager.getText("sm_ui_vg_upgrade"), cost), 12,
                () => { SuperMechVirtualGenesis.UpgradeSpace(a); RefreshAll(); });
            var br = btn.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 1);
            br.anchorMax = new Vector2(0.5f, 1);
            br.sizeDelta = new Vector2(200, 30);
            br.anchoredPosition = new Vector2(0, -y);
            if (!canUpgrade) btn.interactable = false;

            // 世界观描述
            y += 45;
            var lore = SuperMechUiSkin.MakeText(_content, LocalizedTextManager.getText("sm_ui_vg_lore"), 10, TextAnchor.UpperLeft);
            var lr = lore.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 1);
            lr.anchorMax = new Vector2(1, 1);
            lr.offsetMin = new Vector2(4, -y);
            lr.offsetMax = new Vector2(-4, -y - 50);
        }

        private void AddStatRow(ref float y, string label, string value)
        {
            var rowGo = new GameObject("StatRow");
            rowGo.transform.SetParent(_content, false);
            var row = rowGo.AddComponent<RectTransform>();
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(0, 28);
            row.anchoredPosition = new Vector2(0, -y);

            var labelTxt = SuperMechUiSkin.MakeText(row, label, 12, TextAnchor.MiddleLeft);
            var lr = labelTxt.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 0);
            lr.anchorMax = new Vector2(0.5f, 1);
            lr.offsetMin = new Vector2(8, 0);
            lr.offsetMax = new Vector2(0, 0);

            var valueTxt = SuperMechUiSkin.MakeText(row, value, 12, TextAnchor.MiddleRight);
            var vr = valueTxt.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0.5f, 0);
            vr.anchorMax = new Vector2(1, 1);
            vr.offsetMin = new Vector2(0, 0);
            vr.offsetMax = new Vector2(-8, 0);

            y += 30;
        }
    }
}
