using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.0 机械权限体系窗口：机械系权限榜 + 职业称号 + 资源阈值说明
    /// 机制：机械权限体系（权限等级1~13匹配资源阈值）、机械系职业链13阶（入门者→…→神座）
    /// </summary>
    public class SuperMechForgeView : MonoBehaviour
    {
        private static bool _warnedRefresh; // 一次性异常警告
        private Text _countText;
        private Text _detailText;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] ForgeView初始化失败: " + e); } }
        void OnEnable() { try { RefreshAll(); } catch (System.Exception e) { Debug.LogError("[超神机械师] ForgeView刷新失败: " + e); } }
        void Update() { try { RefreshAll(); } catch (System.Exception e) { if (!_warnedRefresh) { _warnedRefresh = true; Debug.LogWarning($"[超神机械师] 权限体系窗口刷新异常(仅首次): " + e.Message); } } }

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

            _countText.text = LocalizedTextManager.getText("sm_ui_forge_title") + " | "
                + LocalizedTextManager.getText("sm_ui_forge_mechs") + " " + MechanicCount();

            var sb = new System.Text.StringBuilder();
            sb.Append(LocalizedTextManager.getText("sm_ui_forge_rank_title")).Append("\n");
            var top = GetTopPermission(10);
            if (top.Count == 0)
            {
                sb.Append(LocalizedTextManager.getText("sm_ui_forge_no_mech")).Append("\n");
            }
            else
            {
                foreach (var kv in top)
                {
                    var a = FindActor(kv.Key);
                    string title = SuperMechForge.GetTitle(kv.Value);
                    sb.Append("· ").Append(a != null && a.name != null ? a.name : ("#" + kv.Key))
                      .Append("  [权限").Append(kv.Value).Append("] ").Append(title)
                      .Append("  资源").Append(SuperMechForge.GetResource(a)).Append("\n");
                }
            }
            sb.Append(LocalizedTextManager.getText("sm_ui_forge_desc")).Append("\n");
            _detailText.text = sb.ToString();
        }

        private static int MechanicCount()
        {
            if (World.world == null || World.world.units == null) return 0;
            var units = World.world.units.units_only_alive;
            if (units == null) return 0;
            int n = 0;
            foreach (var a in units)
                if (a != null && a.isAlive() && SuperMechForge.IsMechanic(a)) n++;
            return n;
        }

        private static List<KeyValuePair<long, int>> GetTopPermission(int limit)
        {
            var list = new List<KeyValuePair<long, int>>();
            foreach (var kv in SuperMechForge.Data.permission)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                var a = FindActor(id);
                if (a == null || !a.isAlive()) continue;
                list.Add(new KeyValuePair<long, int>(id, kv.Value));
            }
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (limit > 0 && list.Count > limit) list.RemoveRange(limit, list.Count - limit);
            return list;
        }

        private static Actor FindActor(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units) if (a != null && a.id == id) return a;
            return null;
        }
    }
}
