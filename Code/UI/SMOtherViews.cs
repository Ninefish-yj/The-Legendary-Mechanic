using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMCraftView : MonoBehaviour
    {
        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            var (scroll, content) = SMUiSkin.CreateScrollArea(transform, "Scroll");
            var text = SMUiSkin.MakeText(content, LocalizedTextManager.getText("sm_ui_craft_info"), 14, TextAnchor.UpperLeft);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    public class SMRankView : MonoBehaviour
    {
        private RectTransform _content;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] RankView初始化失败: " + e); }
        }

        void OnEnable()
        {
            try { RefreshRank(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] RankView刷新失败: " + e); }
        }

        private void BuildLayout()
        {
            var (scroll, content) = SMUiSkin.CreateScrollArea(transform, "Scroll");
            _content = content;
        }

        private void RefreshRank()
        {
            if (_content == null) return;

            // 清空旧内容
            for (int i = _content.childCount - 1; i >= 0; i--)
                Object.Destroy(_content.GetChild(i).gameObject);

            var units = World.world?.units?.units_only_alive;
            if (units == null)
            {
                Debug.Log("[超神机械师] 排行榜: units为null");
                ShowEmpty();
                return;
            }

            Debug.Log($"[超神机械师] 排行榜: 遍历{units.Count}个单位");
            var rankings = new System.Collections.Generic.List<(string name, string rankKey, float onar)>();
            int qiCount = 0, crossCount = 0, zeroCount = 0;
            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                float onar = SuperMechAdvancement.CalcOnar(a);
                if (onar <= 0) zeroCount++;
                if (SuperMechQi.GetQiMax(a) > 0) qiCount++; else if (onar > 0) crossCount++;
                string rankKey = SuperMechRanks.GetRankName(a);
                rankings.Add((a.name, rankKey, onar));
            }
            Debug.Log($"[超神机械师] 排行榜: 有气力{qiCount}个, 跨模组{crossCount}个, 零能级{zeroCount}个, 入榜{rankings.Count}个");
            rankings.Sort((x, y) => y.onar.CompareTo(x.onar));
            if (rankings.Count > 20) rankings.RemoveRange(20, rankings.Count - 20);

            if (rankings.Count == 0)
            {
                ShowEmpty();
                return;
            }

            int rank = 1;
            foreach (var r in rankings)
            {
                var rowGo = new GameObject($"Row_{rank}");
                rowGo.transform.SetParent(_content, false);
                var layout = rowGo.AddComponent<LayoutElement>();
                layout.preferredHeight = 30;
                layout.minHeight = 30;
                var rowImg = rowGo.AddComponent<Image>();
                if (rank <= 3)
                    rowImg.color = new Color(SMUiSkin.AccentColor.r, SMUiSkin.AccentColor.g, SMUiSkin.AccentColor.b, 0.2f);
                else
                    rowImg.color = rank % 2 == 0 ? new Color(1, 1, 1, 0.04f) : new Color(1, 1, 1, 0.02f);

                var text = SMUiSkin.MakeText(rowGo.transform, "", 13, TextAnchor.MiddleLeft);
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                if (rank <= 3) text.color = SMUiSkin.AccentColor;
                string onarLabel = LocalizedTextManager.getText("sm_ui_onar_col");
                text.text = $"#{rank}  {r.name}  -  {r.rankKey}  ({onarLabel}:{r.onar:F0})";
                rank++;
            }
        }

        private void ShowEmpty()
        {
            var emptyGo = new GameObject("Empty");
            emptyGo.transform.SetParent(_content, false);
            var layout = emptyGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;
            var emptyText = SMUiSkin.MakeText(emptyGo.transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var textRect = emptyText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
    }

    public class SMSanctuaryView : MonoBehaviour
    {
        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            var (scroll, content) = SMUiSkin.CreateScrollArea(transform, "Scroll");
            var text = SMUiSkin.MakeText(content, LocalizedTextManager.getText("sm_ui_sanctuary_info"), 14, TextAnchor.UpperLeft);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 4);
            rect.offsetMax = new Vector2(-8, -4);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
