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
            // 不用ScrollArea，直接用Text填满窗口
            var text = SMUiSkin.MakeText(transform, LocalizedTextManager.getText("sm_ui_craft_info"), 14, TextAnchor.UpperLeft);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 12);
            rect.offsetMax = new Vector2(-12, -12);
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
        private RectTransform _listContent;
        private RectTransform _detailContent;
        private int _selectedIndex = -1;
        private readonly List<GameObject> _listItems = new();

        void Awake()
        {
            try { BuildLayout(); RefreshList(); RefreshDetail(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] 圣所窗口初始化失败: " + e); }
        }

        private void BuildLayout()
        {
            // 顶部统计栏（高度60px）
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = Vector2.one;
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 60);
            topRect.anchoredPosition = Vector2.zero;
            var topImg = topGo.AddComponent<Image>();
            topImg.color = new Color(0.1f, 0.15f, 0.22f, 0.9f);

            var statsText = SMUiSkin.MakeText(topGo.transform, "", 12, TextAnchor.UpperLeft);
            var statsRect = statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero;
            statsRect.anchorMax = Vector2.one;
            statsRect.offsetMin = new Vector2(10, 5);
            statsRect.offsetMax = new Vector2(-10, -5);
            statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statsText.supportRichText = true;
            var data = SuperMechSanctuary.Data;
            statsText.text = $"<color=#6ab7ff>已解锁圣所:</color> {data.unlocked_sanctuaries}/6  " +
                $"<color=#6ab7ff>钥匙碎片:</color> {data.key_fragments}  " +
                $"<color=#6ab7ff>总权限:</color> {data.total_permission}\n" +
                $"<color=#6ab7ff>总访问:</color> {data.total_visits}  " +
                $"<color=#6ab7ff>神性蜕变:</color> {data.total_divinity_ascensions}  " +
                $"<color=#6ab7ff>复活次数:</color> {data.total_resurrections}\n" +
                $"<color=#ffd700>宇宙迭代:</color> 第{SuperMechCosmicIteration.CurrentIteration}轮  " +
                $"<color=#ffd700>历史文明:</color> {SuperMechCivilizationData.GetHistory().Count}轮";

            // 左侧列表面板（宽度180px，从顶部60px开始）
            var listGo = new GameObject("ListPanel");
            listGo.transform.SetParent(transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 0.5f);
            listRect.sizeDelta = new Vector2(180, 0);
            listRect.offsetMin = new Vector2(0, 0);
            listRect.offsetMax = new Vector2(0, -60);

            var (scroll, content) = SMUiSkin.CreateScrollArea(listGo.transform, "Scroll");
            _listContent = content;

            // 右侧详情面板
            var detailGo = new GameObject("DetailPanel");
            detailGo.transform.SetParent(transform, false);
            var detailRect = detailGo.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.offsetMin = new Vector2(190, 0);
            detailRect.offsetMax = new Vector2(0, -60);

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(detailGo.transform, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.offsetMin = new Vector2(4, 4);
            cardRect.offsetMax = new Vector2(-4, -4);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = SMUiSkin.CardBg;

            // 详情滚动区
            var dsGo = new GameObject("DetailScroll");
            dsGo.transform.SetParent(cardGo.transform, false);
            var dsRect = dsGo.AddComponent<RectTransform>();
            dsRect.anchorMin = Vector2.zero; dsRect.anchorMax = Vector2.one;
            dsRect.offsetMin = new Vector2(8, 8); dsRect.offsetMax = new Vector2(-8, -8);
            var dsImg = dsGo.AddComponent<Image>();
            dsImg.color = new Color(0f, 0f, 0f, 0.01f);
            dsImg.raycastTarget = false;
            var dsMask = dsGo.AddComponent<Mask>();
            dsMask.showMaskGraphic = true;
            var dsScroll = dsGo.AddComponent<ScrollRect>();

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(dsGo.transform, false);
            var vpRect = vpGo.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;

            var dcGo = new GameObject("Content");
            dcGo.transform.SetParent(vpGo.transform, false);
            _detailContent = dcGo.AddComponent<RectTransform>();
            _detailContent.anchorMin = new Vector2(0, 1);
            _detailContent.anchorMax = new Vector2(0, 1);
            _detailContent.pivot = new Vector2(0, 1);
            _detailContent.sizeDelta = new Vector2(0, 0);
            var dcHit = dcGo.AddComponent<Image>();
            dcHit.color = new Color(0f, 0f, 0f, 0f);
            dcHit.raycastTarget = true;
            var dcVlg = dcGo.AddComponent<VerticalLayoutGroup>();
            dcVlg.spacing = 4;
            dcVlg.padding = new RectOffset(8, 8, 8, 8);
            dcVlg.childControlWidth = true;
            dcVlg.childControlHeight = true;
            dcVlg.childForceExpandWidth = true;
            var dcFitter = dcGo.AddComponent<ContentSizeFitter>();
            dcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            dsScroll.viewport = vpRect;
            dsScroll.content = _detailContent;
            dsScroll.horizontal = false;
            dsScroll.vertical = true;
        }

        private void RefreshList()
        {
            foreach (var go in _listItems) if (go != null) Destroy(go);
            _listItems.Clear();

            for (int i = 0; i < SuperMechSanctuary.TotalSanctuaries; i++)
            {
                int idx = i;
                string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[i]);
                string cls = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryClasses[i]);
                int frags = SuperMechSanctuary.Data.sanctuary_fragments[i];
                bool unlocked = frags >= SuperMechSanctuary.FragmentsToUnlock;

                var itemGo = new GameObject($"Item_{i}");
                itemGo.transform.SetParent(_listContent, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0, 1);
                itemRect.sizeDelta = new Vector2(0, 50);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = idx == _selectedIndex
                    ? new Color(0.2f, 0.4f, 0.7f, 0.8f)
                    : new Color(0.12f, 0.16f, 0.22f, 0.6f);
                itemImg.raycastTarget = true;

                var btn = itemGo.AddComponent<Button>();
                btn.targetGraphic = itemImg;
                btn.onClick.AddListener(() => { _selectedIndex = idx; RefreshList(); RefreshDetail(); });

                var txt = SMUiSkin.MakeText(itemGo.transform,
                    $"{name}\n<size=10><color=#888>{cls}</color> <color={(unlocked ? "#4f4" : "#f84")}>{frags}/3</color></size>",
                    12, TextAnchor.MiddleLeft);
                txt.supportRichText = true;
                var txtRect = txt.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = new Vector2(8, 0);
                txtRect.offsetMax = new Vector2(-8, 0);

                _listItems.Add(itemGo);
            }
        }

        private void RefreshDetail()
        {
            foreach (Transform child in _detailContent) Destroy(child.gameObject);

            if (_selectedIndex < 0 || _selectedIndex >= SuperMechSanctuary.TotalSanctuaries)
            {
                var hint = SMUiSkin.MakeText(_detailContent,
                    LocalizedTextManager.getText("sm_ui_select_sanctuary"), 14, TextAnchor.UpperCenter);
                hint.color = new Color(0.6f, 0.6f, 0.6f);
                return;
            }

            int i = _selectedIndex;
            string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[i]);
            string cls = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryClasses[i]);
            int frags = SuperMechSanctuary.Data.sanctuary_fragments[i];
            bool unlocked = frags >= SuperMechSanctuary.FragmentsToUnlock;

            AddDetailLine($"<size=16><color=#6ab7ff>{name}</color></size>", 16);
            AddDetailLine($"职业分类: {cls}", 12);
            AddDetailLine($"碎片进度: {frags}/{SuperMechSanctuary.FragmentsToUnlock} {(unlocked ? "<color=#4f4>[已解锁]</color>" : "<color=#f84>[未解锁]</color>")}", 12);
            AddDetailLine("", 8);

            // 统计该圣所的权限拥有者
            int totalAuth = 0;
            int maxAuth = 0;
            // 遍历_unitAuthority统计（通过反射访问私有字段）
            var authField = typeof(SuperMechSanctuary).GetField("_unitAuthority",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (authField != null)
            {
                var authDict = authField.GetValue(null) as System.Collections.IDictionary;
                if (authDict != null)
                {
                    foreach (System.Collections.DictionaryEntry entry in authDict)
                    {
                        var arr = entry.Value as int[];
                        if (arr != null && i < arr.Length)
                        {
                            totalAuth += arr[i];
                            if (arr[i] > maxAuth) maxAuth = arr[i];
                        }
                    }
                }
            }

            AddDetailLine($"<color=#6ab7ff>权限统计</color>", 13);
            AddDetailLine($"拥有权限单位数: {(totalAuth > 0 ? "已记录" : "暂无")}", 12);
            AddDetailLine($"最高权限等级: {maxAuth}", 12);
            AddDetailLine("", 8);
            AddDetailLine($"<color=#6ab7ff>解锁条件</color>", 13);
            AddDetailLine($"收集 {SuperMechSanctuary.FragmentsToUnlock} 个钥匙碎片", 12);
            AddDetailLine($"碎片可通过神性蜕变和高阶单位掉落获得", 12);
            AddDetailLine("", 8);

            // 复活按钮
            var btnGo = new GameObject("ResurrectBtn");
            btnGo.transform.SetParent(_detailContent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0, 1);
            btnRect.sizeDelta = new Vector2(0, 32);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            var btnText = SMUiSkin.MakeText(btnGo.transform, LocalizedTextManager.getText("sm_ui_open_resurrection"), 13, TextAnchor.MiddleCenter);
            btnText.color = Color.white;
            btnText.fontStyle = FontStyle.Bold;
            var btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() => SMWindowManager.OpenResurrection());
        }

        private void AddDetailLine(string text, int size)
        {
            var t = SMUiSkin.MakeText(_detailContent, text, size, TextAnchor.UpperLeft);
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0, 1);
            tr.sizeDelta = new Vector2(0, size + 6);
        }
    }
}
