using System.Collections.Generic;
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
        // 排行榜缓存：避免频繁打开关闭窗口时重复计算
        private static List<(string name, string rankKey, float onar)> _cachedRankings;
        private static float _cacheTime;
        private const float CacheDuration = 3f;

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
            // 直接用Content，不用ScrollArea（排行榜只显示前20名，不需要滚动）
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.offsetMin = new Vector2(8, 8);
            _content.offsetMax = new Vector2(-8, -8);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void RefreshRank()
        {
            if (_content == null) return;

            // 清空旧内容
            for (int i = _content.childCount - 1; i >= 0; i--)
                Object.Destroy(_content.GetChild(i).gameObject);

            // 用缓存（3秒内有效），避免重复计算
            var rankings = GetCachedRankings();
            if (rankings == null || rankings.Count == 0)
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
                layout.preferredHeight = 28;
                layout.minHeight = 28;
                var rowImg = rowGo.AddComponent<Image>();
                if (rank <= 3)
                    rowImg.color = new Color(SMUiSkin.AccentColor.r, SMUiSkin.AccentColor.g, SMUiSkin.AccentColor.b, 0.15f);
                else
                    rowImg.color = rank % 2 == 0 ? new Color(1, 1, 1, 0.03f) : new Color(1, 1, 1, 0.015f);

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

        private static List<(string name, string rankKey, float onar)> GetCachedRankings()
        {
            // 缓存有效，直接返回
            if (_cachedRankings != null && Time.realtimeSinceStartup - _cacheTime < CacheDuration)
                return _cachedRankings;

            // 重新计算
            var units = World.world?.units?.units_only_alive;
            if (units == null) return null;

            var rankings = new List<(string name, string rankKey, float onar)>();
            foreach (Actor a in units)
            {
                if (a == null || !a.isAlive()) continue;
                float onar = SuperMechAdvancement.CalcOnar(a);
                string rankKey = SuperMechRanks.GetRankName(a);
                rankings.Add((a.name, rankKey, onar));
            }
            rankings.Sort((x, y) => y.onar.CompareTo(x.onar));
            if (rankings.Count > 20) rankings.RemoveRange(20, rankings.Count - 20);

            _cachedRankings = rankings;
            _cacheTime = Time.realtimeSinceStartup;
            return rankings;
        }

        public static void InvalidateCache() { _cachedRankings = null; }

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
