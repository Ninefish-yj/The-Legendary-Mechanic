using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.240.0 排行榜重构：去掉独立三栏窗口，改为内嵌星海总览的单栏列表。
    /// 只参考天人武道天榜中间栏的卡片美术（原版切片背景+UiUnitAvatarElement+金银铜排名），
    /// 不参考其三栏结构。筛选用顶部原版风格下拉，不自绘大色块。
    /// </summary>
    public class SuperMechRankView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _scrollViewRect;
        private Text _countText;
        private Text _emptyText;
        private UnityEngine.UI.Dropdown _rankDropdown;
        private UnityEngine.UI.Dropdown _systemDropdown;
        private UnityEngine.UI.Dropdown _sortDropdown;

        private static List<Actor> _candidatePool = new List<Actor>(512);
        private static List<Actor> _sortedList = new List<Actor>(512);
        private static readonly Dictionary<long, float> _scoreCache = new Dictionary<long, float>(512);
        private static readonly List<GameObject> _cardInstances = new List<GameObject>();
        private static readonly Dictionary<int, GameObject> _cardByIndex = new Dictionary<int, GameObject>();

        private static int _currentSystemFilter = -1; // -1=全部
        private static int _currentRankFilter = -1;  // -1=全部
        private static int _currentSortType = 0;     // 0=能级,1=气力,2=阶位,3=知识数,4=职业等级

        private static int _lastViewStart = 9999;
        private static int _lastViewEnd = -1;
        private static bool _sessionPopulated;

        private static string[] _systemNames;
        private static string[] SystemNames => _systemNames ??= new[] {
            LocalizedTextManager.getText("sm_ui_filter_all"),
            LocalizedTextManager.getText("sm_ui_filter_mech"),
            LocalizedTextManager.getText("sm_ui_filter_mage"),
            LocalizedTextManager.getText("sm_ui_filter_psi"),
            LocalizedTextManager.getText("sm_ui_filter_martial"),
            LocalizedTextManager.getText("sm_ui_filter_mind"),
        };

        public static void Clear()
        {
            _candidatePool.Clear();
            _sortedList.Clear();
            _scoreCache.Clear();
            _cardInstances.Clear();
            _cardByIndex.Clear();
            _sessionPopulated = false;
            _lastViewStart = 9999;
            _lastViewEnd = -1;
        }

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] RankView初始化失败: " + e); }
        }

        void OnEnable()
        {
            try { RefreshList(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] RankView刷新失败: " + e); }
        }

        void Update()
        {
            try { OnScrollUpdate(); }
            catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 排行榜滚动更新异常: {e.Message}"); }
        }

        // ═══════════════ 单栏布局：顶部筛选条 + 滚动列表 ═══════════════
        private void BuildLayout()
        {
            // ── 顶部筛选条（阶位+体系+排序下拉 + 计数）──
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.offsetMin = new Vector2(4, -30);
            topRect.offsetMax = new Vector2(-4, 0);

            string[] rankOptions = { LocalizedTextManager.getText("sm_ui_filter_all"), "F", "E", "D", "C", "B", "A", "S", "X" };
            _rankDropdown = SuperMechUiDropdown.Create("RankDropdown", topBar.transform, rankOptions, OnRankChanged);
            var rankRect = _rankDropdown.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f);
            rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.pivot = new Vector2(0, 0.5f);
            rankRect.sizeDelta = new Vector2(80, 22);
            rankRect.anchoredPosition = new Vector2(0, 0);

            _systemDropdown = SuperMechUiDropdown.Create("SystemDropdown", topBar.transform, SystemNames, OnSystemChanged);
            var sysRect = _systemDropdown.GetComponent<RectTransform>();
            sysRect.anchorMin = new Vector2(0, 0.5f);
            sysRect.anchorMax = new Vector2(0, 0.5f);
            sysRect.pivot = new Vector2(0, 0.5f);
            sysRect.sizeDelta = new Vector2(80, 22);
            sysRect.anchoredPosition = new Vector2(86, 0);

            _sortDropdown = SuperMechUiDropdown.Create("SortDropdown", topBar.transform, RankSorter.SortNames, OnSortChanged);
            var sortRect = _sortDropdown.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0, 0.5f);
            sortRect.anchorMax = new Vector2(0, 0.5f);
            sortRect.pivot = new Vector2(0, 0.5f);
            sortRect.sizeDelta = new Vector2(80, 22);
            sortRect.anchoredPosition = new Vector2(172, 0);

            _countText = SuperMechUiSkin.MakeText(topBar.transform, "", 10, TextAnchor.MiddleRight);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(260, 0);
            countRect.offsetMax = Vector2.zero;
            _countText.color = new Color(0.75f, 0.70f, 0.60f, 1f);

            // ── 滚动列表（原版ScrollRect模式，不自绘背景色块）──
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            _scrollViewRect = scrollGo.AddComponent<RectTransform>();
            _scrollViewRect.anchorMin = Vector2.zero;
            _scrollViewRect.anchorMax = Vector2.one;
            _scrollViewRect.offsetMin = new Vector2(4, 4);
            _scrollViewRect.offsetMax = new Vector2(-4, -34);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.15f); // 轻微暗化透出原版窗口纹理
            scrollGo.AddComponent<Mask>().showMaskGraphic = true;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _listContent = contentGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = Vector2.zero;
            scroll.viewport = viewportRect;
            scroll.content = _listContent;

            // ── 空状态 ──
            _emptyText = SuperMechUiSkin.MakeText(transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.sizeDelta = new Vector2(200, 40);
            emptyRect.anchoredPosition = Vector2.zero;
            _emptyText.gameObject.SetActive(false);
        }

        private void OnRankChanged(int idx)
        {
            _currentRankFilter = idx - 1; // 0=全部
            RefreshList();
        }

        private void OnSystemChanged(int idx)
        {
            _currentSystemFilter = idx - 1; // 0=全部
            RefreshList();
        }

        private void OnSortChanged(int idx)
        {
            _currentSortType = idx;
            RefreshList();
        }

        // ═══════════════ 核心数据逻辑（排序/筛选/虚拟滚动）═══════════════
        private void RefreshList()
        {
            ClearCards();
            if (_listContent != null) _listContent.anchoredPosition = Vector2.zero;

            _candidatePool.Clear();
            var units = World.world?.units?.units_only_alive;
            if (units != null)
            {
                foreach (Actor a in units)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (!IsAwakenedUnit(a)) continue;
                    if (!PassesFilter(a)) continue;
                    _candidatePool.Add(a);
                }
            }

            _sortedList.Clear();
            _sortedList.AddRange(_candidatePool);
            _scoreCache.Clear();
            foreach (var a in _sortedList)
                _scoreCache[a.getID()] = GetScore(a, _currentSortType);

            if (_sortedList.Count > 1)
            {
                try { _sortedList.Sort((a, b) => GetScore(b, _currentSortType).CompareTo(GetScore(a, _currentSortType))); }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 排行榜排序异常: {e.Message}"); }
            }

            _sessionPopulated = true;

            if (_listContent != null)
                _listContent.sizeDelta = new Vector2(0, (_sortedList.Count + 1) * 38f);
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(_sortedList.Count == 0);
            if (_countText != null)
                _countText.text = string.Format(LocalizedTextManager.getText("sm_ui_rank_count"), _sortedList.Count);

            _lastViewStart = 9999;
            _lastViewEnd = -1;
            RenderVisibleCards();
        }

        private bool PassesFilter(Actor a)
        {
            if (_currentSystemFilter >= 0)
            {
                int sysIdx = GetSystemIndex(a);
                if (sysIdx != _currentSystemFilter) return false;
            }
            if (_currentRankFilter >= 0)
            {
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                int rankTier = rankIdx switch
                {
                    0 => 1,
                    1 or 2 => 2,
                    3 or 4 => 3,
                    5 or 6 => 4,
                    7 => 5,
                    8 or 9 => 6,
                    10 or 11 or 12 => 7,
                    13 => 8,
                    _ => 0
                };
                if (rankTier != _currentRankFilter) return false;
            }
            return true;
        }

        private static bool IsAwakenedUnit(Actor a)
        {
            return a.hasTrait(SuperMechTraits.ClassMech)
                || a.hasTrait(SuperMechTraits.ClassMage)
                || a.hasTrait(SuperMechTraits.ClassMind)
                || a.hasTrait(SuperMechTraits.ClassMartial)
                || a.hasTrait(SuperMechTraits.ClassPsi);
        }

        private static int GetSystemIndex(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return 0;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return 1;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return 2;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return 3;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return 4;
            return -1;
        }

        private static float GetScore(Actor a, int sortType)
        {
            return RankSorter.GetScore(a, sortType);
        }

        private void OnScrollUpdate()
        {
            if (_sortedList.Count == 0 || _listContent == null || _scrollViewRect == null) return;

            float scrollY = _listContent.anchoredPosition.y;
            float viewHeight = _scrollViewRect.rect.height > 0 ? _scrollViewRect.rect.height : 300f;
            int startIdx = Mathf.Max(0, Mathf.FloorToInt(scrollY / 38f) - 1);
            int endIdx = Mathf.Min(Mathf.CeilToInt((scrollY + viewHeight) / 38f) + 1, _sortedList.Count - 1);

            if (startIdx == _lastViewStart && endIdx == _lastViewEnd) return;

            var toRemove = new List<int>();
            foreach (var kv in _cardByIndex)
            {
                if (kv.Key < startIdx || kv.Key > endIdx)
                    toRemove.Add(kv.Key);
            }
            foreach (int idx in toRemove)
            {
                if (_cardByIndex[idx] != null)
                    Object.Destroy(_cardByIndex[idx]);
                _cardByIndex.Remove(idx);
            }
            _cardInstances.RemoveAll(c => c == null);

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (!_cardByIndex.ContainsKey(i))
                    CreateCard(i);
            }

            _lastViewStart = startIdx;
            _lastViewEnd = endIdx;
        }

        private void RenderVisibleCards()
        {
            if (_sortedList.Count <= 0) return;
            float viewHeight = _scrollViewRect != null && _scrollViewRect.rect.height > 0 ? _scrollViewRect.rect.height : 300f;
            int visibleCount = Mathf.CeilToInt(viewHeight / 38f) + 2;
            int endIdx = Mathf.Min(visibleCount - 1, _sortedList.Count - 1);
            for (int i = 0; i <= endIdx; i++)
                CreateCard(i);
            _lastViewStart = 0;
            _lastViewEnd = endIdx;
        }

        private void CreateCard(int index)
        {
            if (index < 0 || index >= _sortedList.Count || _cardByIndex.ContainsKey(index)) return;

            Actor actor = _sortedList[index];
            if (actor == null || _listContent == null) return;

            var cardGo = new GameObject($"Card_{index}");
            cardGo.transform.SetParent(_listContent, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(1, 1);
            cardRect.pivot = new Vector2(0.5f, 1);
            cardRect.sizeDelta = new Vector2(0, 38);
            cardRect.anchoredPosition = new Vector2(0, -index * 38f - 2);

            var card = cardGo.AddComponent<SuperMechLeaderboardCard>();
            float onar = SuperMechAdvancement.CalcOnar(actor);
            string className = GetRankClassName(actor);
            card.Build(actor, index + 1, onar, className);

            _cardInstances.Add(cardGo);
            _cardByIndex[index] = cardGo;
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cardInstances.Count; i++)
            {
                if (_cardInstances[i] != null)
                    Object.Destroy(_cardInstances[i]);
            }
            _cardInstances.Clear();
            _cardByIndex.Clear();
        }

        private static string GetRankClassName(Actor a)
        {
            if (a == null) return "?";
            if (a.hasTrait(SuperMechTraits.ClassMech)) return LocalizedTextManager.getText("sm_class_mech");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_class_mage");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_class_mind");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_class_martial");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_class_psi");
            return LocalizedTextManager.getText("sm_ui_wild");
        }

        public static void InvalidateCache()
        {
            _sessionPopulated = false;
            _candidatePool.Clear();
            _sortedList.Clear();
            _scoreCache.Clear();
        }

        private static readonly Color[] RankColorTable = {
            new Color(0.53f, 0.53f, 0.53f),
            new Color(0.80f, 0.80f, 0.80f),
            new Color(0.31f, 0.80f, 0.44f),
            new Color(0.23f, 0.62f, 1.00f),
            new Color(0.66f, 0.33f, 0.97f),
            new Color(0.96f, 0.62f, 0.04f),
            new Color(1.00f, 0.42f, 0.21f),
            new Color(1.00f, 0.20f, 0.20f),
        };

        private static Color GetRankColorFor(Actor a)
        {
            if (a == null) return Color.white;
            int rankIdx = SuperMechAdvancement.GetRankIndex(a);
            if (rankIdx >= 0 && rankIdx < RankColorTable.Length) return RankColorTable[rankIdx];
            return Color.white;
        }
    }
}
