using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>虚拟创世窗口（原著：机械师虚拟分支超神级专属能力）
    /// 核心功能：虚实转化——将虚拟设计图直接转化为实物，省略制造过程
    /// </summary>
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
                tr.offsetMax = new Vector2(-4, -80);
                return;
            }

            string levelName = state.level == SuperMechVirtualGenesis.GenesisLevel.True ?
                LocalizedTextManager.getText("sm_ui_vg_true") : LocalizedTextManager.getText("sm_ui_vg_pseudo");
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_dim_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_vg_title"), levelName);

            float y = 0;
            // 制造加速倍率（虚实转化：省略制造过程）
            float craftMult = SuperMechVirtualGenesis.GetCraftSpeedMultiplier(a);
            string craftText = SuperMechVirtualGenesis.CanInstantCraft(a) ?
                LocalizedTextManager.getText("sm_ui_vg_instant") : string.Format("{0:F0}x", craftMult);
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_craft_speed"), craftText);

            // 召唤消耗
            int summonCost = state.level == SuperMechVirtualGenesis.GenesisLevel.True ?
                SuperMechVirtualGenesis.SummonCostTrue : SuperMechVirtualGenesis.SummonCostBasic;
            AddStatRow(ref y, LocalizedTextManager.getText("sm_ui_vg_summon_cost"),
                string.Format("{0} {1}", summonCost, LocalizedTextManager.getText("sm_ui_vg_potential")));

            // 召唤机械单位按钮
            y += 10;
            int potential = SuperMechPotential.GetPotential(a);
            bool canSummon = potential >= summonCost;
            string summonLabel = state.level == SuperMechVirtualGenesis.GenesisLevel.True ?
                LocalizedTextManager.getText("sm_ui_vg_summon_true") : LocalizedTextManager.getText("sm_ui_vg_summon_basic");
            var btn = SuperMechUiSkin.MakeButton(_content, summonLabel, 12,
                () => { SuperMechVirtualGenesis.SummonMechUnit(a); RefreshAll(); });
            var br = btn.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 1);
            br.anchorMax = new Vector2(0.5f, 1);
            br.sizeDelta = new Vector2(220, 32);
            br.anchoredPosition = new Vector2(0, -y);
            if (!canSummon) btn.interactable = false;

            // 世界观描述（原著：虚实转化）
            y += 48;
            var lore = SuperMechUiSkin.MakeText(_content, LocalizedTextManager.getText("sm_ui_vg_lore"), 10, TextAnchor.UpperLeft);
            var lr = lore.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 1);
            lr.anchorMax = new Vector2(1, 1);
            lr.offsetMin = new Vector2(4, -y);
            lr.offsetMax = new Vector2(-4, -y - 70);
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
