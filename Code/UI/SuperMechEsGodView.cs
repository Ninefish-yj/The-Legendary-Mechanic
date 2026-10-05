using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.10 超A格局窗口（原异神监测改造）：个体伟力·集体伟力体系总览
    /// 显示：超A级个体列表（含异神）/ 霸主文明态度（默许·警惕·清算·协会·圣域）/
    /// 超A级协会状态 / 超能圣域状态 / 异神状态
    /// 原著：异神=顶层超A之一（ch712），能存在是三大文明的默许（ch1002/1018），
    /// 超A级协会为原著后期创建（ch1016 麦尼逊推动），超能圣域=与三大文明同层次（ch1459）。
    /// </summary>
    public class SuperMechEsGodView : MonoBehaviour
    {
        private static bool _warnedRefresh; // 一次性异常警告
        private Text _countText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] EsGodView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { if (!_warnedRefresh) { _warnedRefresh = true; Debug.LogError($"[超神机械师] 超A格局窗口刷新异常(仅首次): " + e.Message); } } }

        private void BuildLayout()
        {
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

            var bodyPanel = SuperMechUiBuilder.CreatePanel(transform, "Body", new Color(0, 0, 0, 0.12f), 4, 4, 0, 0);
            var bRect = bodyPanel.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.offsetMin = new Vector2(0, -24);
            bRect.offsetMax = new Vector2(0, -4);
            _detailText = SuperMechUiBuilder.AddText(bodyPanel, "", 12, TextAnchor.UpperLeft);
            var dRect = _detailText.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = Vector2.one;
            dRect.offsetMin = new Vector2(8, 4);
            dRect.offsetMax = new Vector2(-8, -4);
        }

        private void RefreshAll()
        {
            if (_countText == null || _detailText == null) return;

            int superACount = SuperMechSupermA.CountSuperA();
            var attitude = SuperMechSupermA.CurrentAttitude;
            bool council = SuperMechSupermA.CouncilFormed;
            bool sanctum = SuperMechSupermA.SanctumFormed;
            bool order = SuperMechSupermA.OrderEstablished;
            bool bureau = SuperMechSupermA.Bureaucratized;
            bool alliance = SuperMechSupermA.AllianceFormed;
            float threatPower = SuperMechSupermA.GetThreatPower();   // 超星团级文明超A（清算对象）
            float classPower = SuperMechSupermA.GetClassPower();     // 超A阶级总力量
            float freePower = SuperMechSupermA.GetFreeSuperAPower(); // 民间自由超A（圣域收编对象）
            float civPower = SuperMechSupermA.GetCivPower();
            _countText.text = LocalizedTextManager.getText("sm_ui_esgod_title") + " | "
                + LocalizedTextManager.getText("sm_ui_supera_count") + " " + superACount + " | "
                + LocalizedTextManager.getText("sm_ui_supera_power") + " "
                + classPower.ToString("0") + "/" + civPower.ToString("0");

            var sb = new System.Text.StringBuilder();
            // 霸主文明态度（个体伟力 vs 集体伟力，原著五态：默许·警惕·清算·协会·圣域）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_attitude")).Append(": ")
              .Append(attitude == SuperMechSupermA.Attitude.Sanctum
                  ? LocalizedTextManager.getText("sm_ui_supera_sanctum")
                  : attitude == SuperMechSupermA.Attitude.Council
                      ? LocalizedTextManager.getText("sm_ui_supera_council_yes")
                      : attitude == SuperMechSupermA.Attitude.Purge
                          ? LocalizedTextManager.getText("sm_ui_supera_purge")
                          : attitude == SuperMechSupermA.Attitude.Vigilant
                              ? LocalizedTextManager.getText("sm_ui_supera_vigilant")
                              : LocalizedTextManager.getText("sm_ui_supera_tolerate")).Append("\n");
            // 超A级协会（阶级联合，会被打压直至圣域）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_council")).Append(": ")
              .Append(council
                  ? LocalizedTextManager.getText("sm_ui_supera_council_yes")
                  : LocalizedTextManager.getText("sm_ui_supera_council_no")).Append("\n");
            // 超能圣域（终局：与三大文明同层次，对等共存）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_sanctum")).Append(": ")
              .Append(sanctum
                  ? LocalizedTextManager.getText("sm_ui_supera_sanctum_yes")
                  : LocalizedTextManager.getText("sm_ui_supera_sanctum_no")).Append("\n");
            // 秩序确立（探索历→星海历）与社会形态分叉（诸星联式体制化）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_order")).Append(": ")
              .Append(order
                  ? SuperMechSupermA.GetEraOrderName() + LocalizedTextManager.getText("sm_ui_supera_order_yes")
                  : SuperMechSupermA.GetEraChaosName() + LocalizedTextManager.getText("sm_ui_supera_order_no")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_form")).Append(": ")
              .Append(bureau
                  ? LocalizedTextManager.getText("sm_ui_supera_form_bureau")
                  : LocalizedTextManager.getText("sm_ui_supera_form_semi")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_alliance")).Append(": ")
              .Append(alliance
                  ? LocalizedTextManager.getText("sm_ui_supera_alliance_yes")
                  : LocalizedTextManager.getText("sm_ui_supera_alliance_no")).Append("\n");
            // 力量分层（威胁=力量比值：清算对象=超星团级文明超A；民间自由超A=圣域收编对象）
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_layers")).Append(": ")
              .Append("威胁 ").Append(threatPower.ToString("0")).Append(" / 阶级 ")
              .Append(classPower.ToString("0")).Append(" / 民间 ").Append(freePower.ToString("0")).Append("\n");
            // 顶层超A格局（原著异神=顶层超A之一，不生成特殊单位；状态=世界顶层超A有无+遗产）
            int topCount = SuperMechEsGod.CountTopSuperA();
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_state")).Append(": ")
              .Append(topCount > 0
                  ? string.Format(LocalizedTextManager.getText("sm_ui_esgod_state_active"), topCount)
                  : LocalizedTextManager.getText("sm_ui_esgod_state_idle")).Append("\n")
              .Append(LocalizedTextManager.getText("sm_ui_esgod_heritage")).Append(": ")
              .Append(SuperMechEsGod.Data.heritageClaimed
                  ? LocalizedTextManager.getText("sm_ui_esgod_heritage_yes")
                  : LocalizedTextManager.getText("sm_ui_esgod_heritage_no")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_esgod_desc")).Append("\n");
            sb.Append(LocalizedTextManager.getText("sm_ui_supera_desc")).Append("\n");
            // v0.76.22 世界之外·高维存在层（原著文明已脱离迭代循环，与玩家平级的超脱存在）
            int archCount = SuperMechSanctuary.GetIterationArchives().Count;
            int archSuperA = 0;
            foreach (var ar in SuperMechSanctuary.GetIterationArchives())
                if (ar != null && ar.type == 0) archSuperA += ar.superACount;
            sb.Append("\n<color=#ffd966>══ ").Append(LocalizedTextManager.getText("sm_ui_supera_beyond")).Append(" ══</color>\n")
              .Append(LocalizedTextManager.getText("sm_ui_supera_beyond_desc")).Append("\n")
              .Append(LocalizedTextManager.getText("sm_ui_supera_beyond_arch")).Append(": ")
              .Append(string.Format(LocalizedTextManager.getText("sm_ui_supera_beyond_arch_info"),
                  archCount, archSuperA, (int)((SuperMechSanctuary.GetInheritanceBonus() - 1f) * 100f))).Append("\n")
              .Append(LocalizedTextManager.getText("sm_ui_supera_beyond_echo")).Append(": ")
              .Append(order || sanctum || bureau || alliance
                  ? LocalizedTextManager.getText("sm_ui_supera_beyond_watching")
                  : LocalizedTextManager.getText("sm_ui_supera_beyond_quiet")).Append("\n")
              .Append(LocalizedTextManager.getText("sm_ui_supera_beyond_msg")).Append(": ")
              .Append(SuperMechSanctuary.Data.beyond_message_given
                  ? LocalizedTextManager.getText("sm_ui_supera_beyond_msg_yes")
                  : LocalizedTextManager.getText("sm_ui_supera_beyond_msg_no")).Append("\n")
              .Append("\n<color=#ffd966>").Append(LocalizedTextManager.getText("sm_ui_supera_dialog")).Append("</color>\n")
              .Append(GetBeyondDialog(order, bureau, alliance, sanctum, council,
                  attitude == SuperMechSupermA.Attitude.Purge, SuperMechEsGod.CountTopSuperA()));
            _detailText.text = sb.ToString();
        }

        /// <summary>超脱者对话：世界之外的存在（韩萧/旧日登陆者）观察本世界并回应（按状态动态，非固定剧情）</summary>
        private static string GetBeyondDialog(bool order, bool bureau, bool alliance, bool sanctum, bool council, bool purge, int topCount)
        {
            if (sanctum) return LocalizedTextManager.getText("sm_ui_supera_dialog_sanctum");
            if (bureau) return LocalizedTextManager.getText("sm_ui_supera_dialog_bureau");
            if (alliance) return LocalizedTextManager.getText("sm_ui_supera_dialog_alliance");
            if (purge) return LocalizedTextManager.getText("sm_ui_supera_dialog_purge");
            if (council) return LocalizedTextManager.getText("sm_ui_supera_dialog_council");
            if (order) return LocalizedTextManager.getText("sm_ui_supera_dialog_order");
            if (topCount > 0) return LocalizedTextManager.getText("sm_ui_supera_dialog_top");
            return LocalizedTextManager.getText("sm_ui_supera_dialog_early");
        }
    }
}
