using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// </summary>
    public class SuperMechRankView : MonoBehaviour
    {
        private RectTransform _listContent;
        private RectTransform _scrollViewRect;
        private Text _countText;
        private Text _emptyText;
        private Text _statsText;
        private Dropdown _sortDropdown;
        private Dropdown _rankDropdown;
        private List<GameObject> _systemButtons = new List<GameObject>();

        private static List<Actor> _candidatePool = new List<Actor>(512);
        private static List<Actor> _sortedList = new List<Actor>(512);
        private static readonly Dictionary<long, float> _scoreCache = new Dictionary<long, float>(512);
        private static readonly List<GameObject> _cardInstances = new List<GameObject>();
        private static readonly Dictionary<int, GameObject> _cardByIndex = new Dictionary<int, GameObject>();

        private static int _currentSystemFilter = -1; // -1=全部
        private static int _currentRankFilter = -1;  // -1=全部
        private static int _currentSortType = 0;     // 0=能级,1=气力,2=阶位,3=知识数,4=职业等级

        /// <summary>v0.75.35: 换存档时清空排行榜缓存/卡片引用/筛选状态（防悬垂引用与状态残留）</summary>
        public static void Clear()
        {
            _scoreCache.Clear();
            _cardByIndex.Clear();
            _currentSystemFilter = -1;
            _currentRankFilter = -1;
            _currentSortType = 0;
        }
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
            LocalizedTextManager.getText("sm_ui_filter_mind")
        };
        private static string[] _rankNames;
        private static string[] RankNames => _rankNames ??= new[] {
            LocalizedTextManager.getText("sm_ui_filter_all"), "F", "E", "D", "C", "B", "A", "S", "X"
        };
        private static string[] _sortNames;
        private static string[] SortNames => _sortNames ??= new[] {
            LocalizedTextManager.getText("sm_ui_sort_energy"),
            LocalizedTextManager.getText("sm_ui_sort_qi"),
            LocalizedTextManager.getText("sm_ui_sort_rank"),
            LocalizedTextManager.getText("sm_ui_sort_knowledge"),
            LocalizedTextManager.getText("sm_ui_sort_class")
        };

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

        private void BuildLayout()
        {
            // === 顶部工具栏（高度56）===
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 56);
            topRect.offsetMin = new Vector2(4, 0);
            topRect.offsetMax = new Vector2(-4, 0);
            var topBg = topBar.AddComponent<Image>();
            topBg.color = new Color(0, 0, 0, 0.2f);

            // 第一行：体系筛选按钮（横向）
            var sysRow = new GameObject("SysRow");
            sysRow.transform.SetParent(topBar.transform, false);
            var sysRowRect = sysRow.AddComponent<RectTransform>();
            sysRowRect.anchorMin = new Vector2(0, 1);
            sysRowRect.anchorMax = new Vector2(1, 1);
            sysRowRect.pivot = new Vector2(0.5f, 1);
            sysRowRect.sizeDelta = new Vector2(0, 24);
            sysRowRect.anchoredPosition = new Vector2(0, -4);
            var sysLayout = sysRow.AddComponent<HorizontalLayoutGroup>();
            sysLayout.spacing = 3;
            sysLayout.padding = new RectOffset(4, 4, 0, 0);
            sysLayout.childControlHeight = true;
            sysLayout.childControlWidth = true;
            sysLayout.childForceExpandWidth = true;

            _systemButtons = new List<GameObject>();
            for (int i = 0; i < SystemNames.Length; i++)
            {
                int idx = i - 1;
                var btn = SuperMechUiSkin.MakeButton(sysRow.transform, SystemNames[i], 9, () =>
                {
                    _currentSystemFilter = idx;
                    UpdateSystemButtonColors();
                    RefreshList();
                });
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = _currentSystemFilter == idx ? new Color(0.3f, 0.5f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                _systemButtons.Add(btn.gameObject);
            }

            // 第二行：排序下拉 + 阶位筛选 + 统计 + 清除筛选
            var ctrlRow = new GameObject("CtrlRow");
            ctrlRow.transform.SetParent(topBar.transform, false);
            var ctrlRect = ctrlRow.AddComponent<RectTransform>();
            ctrlRect.anchorMin = new Vector2(0, 1);
            ctrlRect.anchorMax = new Vector2(1, 1);
            ctrlRect.pivot = new Vector2(0.5f, 1);
            ctrlRect.sizeDelta = new Vector2(0, 24);
            ctrlRect.anchoredPosition = new Vector2(0, -30);

            // 排序下拉（左）
            _sortDropdown = CreateSimpleDropdown(ctrlRow.transform, SortNames, OnSortChanged);
            var sortRect = _sortDropdown.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0, 0.5f);
            sortRect.anchorMax = new Vector2(0, 0.5f);
            sortRect.pivot = new Vector2(0, 0.5f);
            sortRect.sizeDelta = new Vector2(90, 20);
            sortRect.anchoredPosition = new Vector2(4, 0);

            // 阶位筛选下拉（左，排序右侧）
            _rankDropdown = CreateSimpleDropdown(ctrlRow.transform, RankNames, OnRankChanged);
            var rankDropRect = _rankDropdown.GetComponent<RectTransform>();
            rankDropRect.anchorMin = new Vector2(0, 0.5f);
            rankDropRect.anchorMax = new Vector2(0, 0.5f);
            rankDropRect.pivot = new Vector2(0, 0.5f);
            rankDropRect.sizeDelta = new Vector2(60, 20);
            rankDropRect.anchoredPosition = new Vector2(98, 0);

            // 清除筛选按钮（左）
            var clearBtn = SuperMechUiSkin.MakeButton(ctrlRow.transform, LocalizedTextManager.getText("sm_ui_rank_clear"), 9, ClearAllFilters);
            var clearBtnRect = clearBtn.GetComponent<RectTransform>();
            clearBtnRect.anchorMin = new Vector2(0, 0.5f);
            clearBtnRect.anchorMax = new Vector2(0, 0.5f);
            clearBtnRect.pivot = new Vector2(0, 0.5f);
            clearBtnRect.sizeDelta = new Vector2(60, 20);
            clearBtnRect.anchoredPosition = new Vector2(162, 0);
            var clearBtnImg = clearBtn.GetComponent<Image>();
            if (clearBtnImg != null) clearBtnImg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);

            // 统计文字（右）
            _countText = SuperMechUiSkin.MakeText(ctrlRow.transform, LocalizedTextManager.getText("sm_ui_legion_count_zero"), 10, TextAnchor.MiddleRight);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 0.5f);
            countRect.anchorMax = new Vector2(1, 0.5f);
            countRect.pivot = new Vector2(1, 0.5f);
            countRect.sizeDelta = new Vector2(100, 20);
            countRect.anchoredPosition = new Vector2(-4, 0);

            // 阶位分布统计（右，统计文字左侧）
            _statsText = SuperMechUiSkin.MakeText(ctrlRow.transform, "", 9, TextAnchor.MiddleRight);
            var statsRect = _statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(1, 0.5f);
            statsRect.anchorMax = new Vector2(1, 0.5f);
            statsRect.pivot = new Vector2(1, 0.5f);
            statsRect.sizeDelta = new Vector2(200, 20);
            statsRect.anchoredPosition = new Vector2(-106, 0);
            _statsText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // === 中间：全宽滚动列表 ===
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            _scrollViewRect = scrollGo.AddComponent<RectTransform>();
            _scrollViewRect.anchorMin = Vector2.zero;
            _scrollViewRect.anchorMax = Vector2.one;
            _scrollViewRect.offsetMin = new Vector2(4, 36);
            _scrollViewRect.offsetMax = new Vector2(-4, -60);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.15f);
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

            // === 底部导航（HorizontalLayoutGroup自适应，避免窄分辨率越界）===
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(transform, false);
            var bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(0, 24);
            bottomRect.anchoredPosition = new Vector2(0, 6);

            var bottomLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.padding = new RectOffset(8, 8, 2, 2);
            bottomLayout.spacing = 8;
            bottomLayout.childAlignment = TextAnchor.LowerCenter;
            bottomLayout.childControlHeight = true;
            bottomLayout.childControlWidth = false;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.childForceExpandHeight = false;

            var topBtn = SuperMechUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_top"), 9, ToTop);
            var topBtnRect = topBtn.GetComponent<RectTransform>();
            topBtnRect.sizeDelta = new Vector2(70, 20);

            var bottomBtn = SuperMechUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_bottom"), 9, ToBottom);
            var bottomBtnRect = bottomBtn.GetComponent<RectTransform>();
            bottomBtnRect.sizeDelta = new Vector2(70, 20);

            _emptyText = SuperMechUiSkin.MakeText(transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.sizeDelta = new Vector2(200, 40);
            _emptyText.gameObject.SetActive(false);
        }

        private static Dropdown CreateSimpleDropdown(Transform parent, string[] options, System.Action<int> onChanged)
        {
            var go = new GameObject("Dropdown");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            var dropdown = go.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData>();
            foreach (var opt in options)
                dropdown.options.Add(new Dropdown.OptionData(opt));
            dropdown.value = 0;
            dropdown.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<int>(onChanged));
            return dropdown;
        }

        private void OnSortChanged(int index)
        {
            _currentSortType = index;
            RefreshList();
        }

        private void OnRankChanged(int index)
        {
            _currentRankFilter = index - 1; // -1=全部, 0-7=F~X
            RefreshList();
        }

        private void UpdateSystemButtonColors()
        {
            if (_systemButtons == null) return;
            for (int i = 0; i < _systemButtons.Count; i++)
            {
                var btn = _systemButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                int idx = i - 1;
                img.color = _currentSystemFilter == idx
                    ? new Color(0.3f, 0.5f, 0.8f, 0.8f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
        }

        private void ClearAllFilters()
        {
            _currentSystemFilter = -1;
            _currentRankFilter = -1;
            if (_rankDropdown != null) _rankDropdown.value = 0;
            UpdateSystemButtonColors();
            RefreshList();
        }

        private void RefreshList()
        {
            ClearCards();
            if (_listContent != null) _listContent.anchoredPosition = Vector2.zero;

            // 扫描候选池
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

            // 排序
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

            // 更新UI
            if (_listContent != null)
                _listContent.sizeDelta = new Vector2(0, (_sortedList.Count + 1) * 34f);
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(_sortedList.Count == 0);
            if (_countText != null)
                _countText.text = string.Format(LocalizedTextManager.getText("sm_ui_rank_count"), _sortedList.Count);

            UpdateStats();
            _lastViewStart = 9999;
            _lastViewEnd = -1;
            RenderVisibleCards();
        }

        private bool PassesFilter(Actor a)
        {
            // 体系筛选
            if (_currentSystemFilter >= 0)
            {
                int sysIdx = GetSystemIndex(a);
                if (sysIdx != _currentSystemFilter) return false;
            }
            // 阶位筛选
            if (_currentRankFilter >= 0)
            {
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                // 简化：按阶位大档筛选
                int rankTier = rankIdx switch
                {
                    0 => 1, // F
                    1 or 2 => 2, // E/E+
                    3 or 4 => 3, // D/D+
                    5 or 6 => 4, // C/C+
                    7 => 5, // B
                    8 or 9 => 6, // A/A+
                    10 or 11 or 12 => 7, // S/S+/SS
                    13 => 8, // X
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
            return sortType switch
            {
                0 => SuperMechAdvancement.CalcOnar(a), // 能级
                1 => SuperMechQi.GetQiMax(a), // 气力
                2 => SuperMechAdvancement.GetExactRankIndex(a), // 阶位
                3 => SuperMechKnowledge.GetUnlockedCount(a, "mech"), // 知识数
                4 => SuperMechStage.GetStage(a), // 职业阶段
                _ => 0
            };
        }

        private void UpdateStats()
        {
            if (_statsText == null) return;

            // 阶位分布（单行摘要，适配顶部工具栏）
            int[] rankCount = new int[8]; // F,E,D,C,B,A,S,X
            foreach (var a in _candidatePool)
            {
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                int tier = rankIdx switch
                {
                    0 => 0, 1 or 2 => 1, 3 or 4 => 2, 5 or 6 => 3,
                    7 => 4, 8 or 9 => 5, 10 or 11 or 12 => 6, 13 => 7,
                    _ => -1
                };
                if (tier >= 0) rankCount[tier]++;
            }

            string[] rankLabels = { "F", "E", "D", "C", "B", "A", "S", "X" };
            string stats = "";
            for (int i = 0; i < 8; i++)
            {
                if (rankCount[i] > 0)
                    stats += $"{rankLabels[i]}:{rankCount[i]} ";
            }
            _statsText.text = stats;
        }

        private void OnScrollUpdate()
        {
            if (_sortedList.Count == 0 || _listContent == null || _scrollViewRect == null) return;

            float scrollY = _listContent.anchoredPosition.y;
            float viewHeight = _scrollViewRect.rect.height > 0 ? _scrollViewRect.rect.height : 300f;
            int startIdx = Mathf.Max(0, Mathf.FloorToInt(scrollY / 34f) - 1);
            int endIdx = Mathf.Min(Mathf.CeilToInt((scrollY + viewHeight) / 34f) + 1, _sortedList.Count - 1);

            if (startIdx == _lastViewStart && endIdx == _lastViewEnd) return;

            // 移除不可见卡片
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

            // 创建可见卡片
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
            int visibleCount = Mathf.CeilToInt(viewHeight / 34f) + 2;
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
            cardRect.sizeDelta = new Vector2(0, 34);
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

        private void ToTop()
        {
            if (_listContent != null) _listContent.anchoredPosition = Vector2.zero;
        }

        private void ToBottom()
        {
            if (_listContent == null || _scrollViewRect == null) return;
            float maxY = _listContent.sizeDelta.y - _scrollViewRect.rect.height;
            _listContent.anchoredPosition = new Vector2(0, Mathf.Max(0, maxY));
        }

        private static string GetRankClassName(Actor a)
        {
            if (a == null) return "?";
            if (a.hasTrait(SuperMechTraits.ClassMech)) return LocalizedTextManager.getText("sm_class_mech");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_class_mage");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_class_power");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_class_wudan");
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
    }

    /// <summary>
    /// v0.53.0 势力列表窗口
    /// 显示所有超能者势力：名称/等级/成员数/领袖/战力，点击展开成员
    /// </summary>
}
