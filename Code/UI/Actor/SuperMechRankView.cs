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
        private Text _statsPanelText;
        private UnityEngine.UI.Dropdown _realmDropdown;
        private UnityEngine.UI.Dropdown _sortDropdown;
        private List<GameObject> _systemButtons = new List<GameObject>();
        private List<GameObject> _rankButtons = new List<GameObject>();

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
            // ═════════════ v0.238.0 三栏布局（参考天人武道天榜）═════════════
            // 左：筛选（阶位+体系+清除）｜中：下拉筛选/排序+卡片列表｜右：统计面板
            const float topY = 40f;    // 顶栏留白（原版窗口标题区）
            const float botY = 38f;    // 底部留白

            BuildLeftPanel(topY, botY);
            BuildCenterPanel(topY, botY);
            BuildRightPanel(topY, botY);
        }

        // ═══════════════ 左栏：筛选 ═══════════════
        private void BuildLeftPanel(float topY, float botY)
        {
            var leftGo = new GameObject("LeftPanel");
            leftGo.transform.SetParent(transform, false);
            var leftRect = leftGo.AddComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = new Vector2(8, botY);
            leftRect.offsetMax = new Vector2(-548, -topY);
            var leftBg = leftGo.AddComponent<Image>();
            leftBg.color = SuperMechUiSkin.PanelVanilla; // v0.238.5 三栏背景统一暖棕常量（此前硬编码旧色，改常量不同步）

            // 筛选标题
            var title = SuperMechUiSkin.MakeText(leftGo.transform, LocalizedTextManager.getText("sm_ui_rank_filter_title"), 11, TextAnchor.MiddleLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 20);
            titleRect.anchoredPosition = new Vector2(0, -4);
            title.color = new Color(0.85f, 0.72f, 0.50f, 1f);

            // 可滚动筛选区
            var scrollGo = new GameObject("FilterScroll");
            scrollGo.transform.SetParent(leftGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(4, 26);
            scrollRect.offsetMax = new Vector2(-4, -28);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.12f);
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
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            // —— 阶位组 ——
            var rankTitle = SuperMechUiSkin.MakeText(contentGo.transform, LocalizedTextManager.getText("sm_ui_rank_rank_title"), 10, TextAnchor.MiddleLeft);
            var rankTitleRect = rankTitle.GetComponent<RectTransform>();
            rankTitleRect.anchorMin = new Vector2(0, 1);
            rankTitleRect.anchorMax = new Vector2(1, 1);
            rankTitleRect.pivot = new Vector2(0.5f, 1);
            rankTitleRect.sizeDelta = new Vector2(0, 18);
            rankTitleRect.anchoredPosition = new Vector2(0, -2);
            rankTitle.color = new Color(0.72f, 0.64f, 0.50f, 1f);

            var rankGridGo = new GameObject("RankGrid");
            rankGridGo.transform.SetParent(contentGo.transform, false);
            var rankGridRect = rankGridGo.AddComponent<RectTransform>();
            rankGridRect.anchorMin = new Vector2(0, 1);
            rankGridRect.anchorMax = new Vector2(1, 1);
            rankGridRect.pivot = new Vector2(0.5f, 1);
            rankGridRect.sizeDelta = new Vector2(0, 78);
            rankGridRect.anchoredPosition = new Vector2(0, -22);
            var rankGrid = rankGridGo.AddComponent<GridLayoutGroup>();
            rankGrid.cellSize = new Vector2(44, 22);
            rankGrid.spacing = new Vector2(3, 3);
            rankGrid.padding = new RectOffset(0, 0, 0, 0);

            string[] rankLabels = { LocalizedTextManager.getText("sm_ui_filter_all"), "F", "E", "D", "C", "B", "A", "S", "X" };
            _rankButtons = new List<GameObject>();
            for (int i = 0; i < rankLabels.Length; i++)
            {
                int idx = i;
                var btn = SuperMechUiSkin.MakeButton(rankGridGo.transform, rankLabels[i], 9, () =>
                {
                    _currentRankFilter = idx - 1; // 0=全部
                    UpdateRankButtonColors();
                    RefreshList();
                });
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = _currentRankFilter == idx - 1 ? new Color(0.55f, 0.40f, 0.18f, 0.95f) : new Color(0.16f, 0.12f, 0.08f, 0.85f);
                }
                _rankButtons.Add(btn.gameObject);
            }

            // —— 体系组 ——
            var sysTitle = SuperMechUiSkin.MakeText(contentGo.transform, LocalizedTextManager.getText("sm_ui_rank_system_title"), 10, TextAnchor.MiddleLeft);
            var sysTitleRect = sysTitle.GetComponent<RectTransform>();
            sysTitleRect.anchorMin = new Vector2(0, 1);
            sysTitleRect.anchorMax = new Vector2(1, 1);
            sysTitleRect.pivot = new Vector2(0.5f, 1);
            sysTitleRect.sizeDelta = new Vector2(0, 18);
            sysTitleRect.anchoredPosition = new Vector2(0, -104);
            sysTitle.color = new Color(0.72f, 0.64f, 0.50f, 1f);

            var sysGridGo = new GameObject("SysGrid");
            sysGridGo.transform.SetParent(contentGo.transform, false);
            var sysGridRect = sysGridGo.AddComponent<RectTransform>();
            sysGridRect.anchorMin = new Vector2(0, 1);
            sysGridRect.anchorMax = new Vector2(1, 1);
            sysGridRect.pivot = new Vector2(0.5f, 1);
            sysGridRect.sizeDelta = new Vector2(0, 52);
            sysGridRect.anchoredPosition = new Vector2(0, -124);
            var sysGrid = sysGridGo.AddComponent<GridLayoutGroup>();
            sysGrid.cellSize = new Vector2(46, 22);
            sysGrid.spacing = new Vector2(3, 3);
            sysGrid.padding = new RectOffset(0, 0, 0, 0);

            _systemButtons = new List<GameObject>();
            for (int i = 0; i < SystemNames.Length; i++)
            {
                int idx = i - 1;
                var btn = SuperMechUiSkin.MakeButton(sysGridGo.transform, SystemNames[i], 9, () =>
                {
                    _currentSystemFilter = idx;
                    UpdateSystemButtonColors();
                    RefreshList();
                });
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = _currentSystemFilter == idx ? new Color(0.55f, 0.40f, 0.18f, 0.95f) : new Color(0.16f, 0.12f, 0.08f, 0.85f);
                }
                _systemButtons.Add(btn.gameObject);
            }

            // 内容高度
            contentRect.sizeDelta = new Vector2(0, 260);

            // —— 清除筛选（左栏底部）——
            var clearBtn = SuperMechUiSkin.MakeButton(leftGo.transform, LocalizedTextManager.getText("sm_ui_rank_clear"), 9, ClearAllFilters);
            var clearRect = clearBtn.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.5f, 0);
            clearRect.anchorMax = new Vector2(0.5f, 0);
            clearRect.pivot = new Vector2(0.5f, 0);
            clearRect.sizeDelta = new Vector2(88, 20);
            clearRect.anchoredPosition = new Vector2(0, 6);
        }

        private void ClearAllFilters()
        {
            _currentSystemFilter = -1;
            _currentRankFilter = -1;
            UpdateSystemButtonColors();
            UpdateRankButtonColors();
            RefreshList();
        }

        // ═══════════════ 中栏：下拉 + 卡片列表 ═══════════════
        private void BuildCenterPanel(float topY, float botY)
        {
            // 顶栏：阶位下拉 + 排序下拉 + 计数（贴中栏顶部，anchor 顶部拉伸）
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.offsetMin = new Vector2(166, -(topY + 26));
            topRect.offsetMax = new Vector2(-196, -topY);

            _realmDropdown = SuperMechUiDropdown.Create("RealmDropdown", topBar.transform, new[] { LocalizedTextManager.getText("sm_ui_filter_all"), "F", "E", "D", "C", "B", "A", "S", "X" }, OnRealmChanged);
            var realmRect = _realmDropdown.GetComponent<RectTransform>();
            realmRect.anchorMin = new Vector2(0, 0.5f);
            realmRect.anchorMax = new Vector2(0, 0.5f);
            realmRect.pivot = new Vector2(0, 0.5f);
            realmRect.sizeDelta = new Vector2(100, 24);
            realmRect.anchoredPosition = Vector2.zero;

            _sortDropdown = SuperMechUiDropdown.Create("SortDropdown", topBar.transform, RankSorter.SortNames, OnSortChanged);
            var sortRect = _sortDropdown.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0, 0.5f);
            sortRect.anchorMax = new Vector2(0, 0.5f);
            sortRect.pivot = new Vector2(0, 0.5f);
            sortRect.anchoredPosition = new Vector2(106, 0);
            sortRect.sizeDelta = new Vector2(100, 24);

            // 计数（顶栏右侧）
            _countText = SuperMechUiSkin.MakeText(topBar.transform, "", 10, TextAnchor.MiddleRight);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(212, 0);
            countRect.offsetMax = Vector2.zero;
            _countText.color = new Color(0.75f, 0.70f, 0.60f, 1f);

            // 列表（中栏，TopBar 与 BottomBar 之间，双向拉伸）
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            _scrollViewRect = scrollGo.AddComponent<RectTransform>();
            _scrollViewRect.anchorMin = Vector2.zero;
            _scrollViewRect.anchorMax = Vector2.one;
            _scrollViewRect.offsetMin = new Vector2(166, botY + 30);
            _scrollViewRect.offsetMax = new Vector2(-196, -(topY + 30));
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

            // 底部：到顶/到底（贴中栏底部，anchor 底部拉伸）
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(transform, false);
            var bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.offsetMin = new Vector2(166, botY);
            bottomRect.offsetMax = new Vector2(-196, botY + 26);

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
            topBtnRect.sizeDelta = new Vector2(64, 40);

            var bottomBtn = SuperMechUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_bottom"), 9, ToBottom);
            var bottomBtnRect = bottomBtn.GetComponent<RectTransform>();
            bottomBtnRect.sizeDelta = new Vector2(64, 40);

            _emptyText = SuperMechUiSkin.MakeText(transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.sizeDelta = new Vector2(200, 40);
            emptyRect.anchoredPosition = new Vector2(25, 0);
            _emptyText.gameObject.SetActive(false);
        }

        // ═══════════════ 右栏：统计 ═══════════════
        private void BuildRightPanel(float topY, float botY)
        {
            var rightGo = new GameObject("RightPanel");
            rightGo.transform.SetParent(transform, false);
            var rightRect = rightGo.AddComponent<RectTransform>();
            // 右栏：锚定右边，offset 用负数（相对右边缘向内）
            rightRect.anchorMin = new Vector2(1, 0);
            rightRect.anchorMax = new Vector2(1, 1);
            rightRect.pivot = new Vector2(1, 0.5f);
            rightRect.offsetMin = new Vector2(-196, botY);
            rightRect.offsetMax = new Vector2(-8, -topY);
            var rightBg = rightGo.AddComponent<Image>();
            rightBg.color = SuperMechUiSkin.PanelVanilla; // v0.238.5 三栏背景统一暖棕常量

            var title = SuperMechUiSkin.MakeText(rightGo.transform, LocalizedTextManager.getText("sm_ui_rank_stats_title"), 11, TextAnchor.MiddleLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 20);
            titleRect.anchoredPosition = new Vector2(0, -4);
            title.color = new Color(0.85f, 0.72f, 0.50f, 1f);

            var scrollGo = new GameObject("StatsScroll");
            scrollGo.transform.SetParent(rightGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(6, 6);
            scrollRect.offsetMax = new Vector2(-6, -26);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.12f);
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
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            _statsPanelText = SuperMechUiSkin.MakeText(contentGo.transform, "", 9, TextAnchor.UpperLeft);
            var statsRect = _statsPanelText.GetComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero;
            statsRect.anchorMax = Vector2.one;
            statsRect.offsetMin = new Vector2(6, 4);
            statsRect.offsetMax = Vector2.zero;
            _statsPanelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statsPanelText.verticalOverflow = VerticalWrapMode.Truncate;
            _statsPanelText.lineSpacing = 1.15f;
            _statsPanelText.color = new Color(0.88f, 0.84f, 0.74f, 1f);
            var fitter = _statsPanelText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private void OnRealmChanged(int idx)
        {
            _currentRankFilter = idx - 1; // 0=全部
            RefreshList();
        }

        private void OnSortChanged(int idx)
        {
            _currentSortType = idx;
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
                img.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
                img.type = Image.Type.Sliced;
                img.color = _currentSystemFilter == idx ? new Color(0.55f, 0.40f, 0.18f, 0.95f) : new Color(0.16f, 0.12f, 0.08f, 0.85f);
            }
        }

        private void UpdateRankButtonColors()
        {
            if (_rankButtons == null) return;
            for (int i = 0; i < _rankButtons.Count; i++)
            {
                var btn = _rankButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                int idx = i - 1;
                img.sprite = SuperMechUiSkin.GetWhiteSpriteSafe();
                img.type = Image.Type.Sliced;
                img.color = _currentRankFilter == idx ? new Color(0.55f, 0.40f, 0.18f, 0.95f) : new Color(0.16f, 0.12f, 0.08f, 0.85f);
            }
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
            return RankSorter.GetScore(a, sortType);
        }

        private void UpdateStats()
        {
            if (_statsText != null)
                _statsText.text = RankSorter.CalculateRankStats(_candidatePool);
            if (_statsPanelText == null) return;

            // ── 阶位分布 ──
            string[] rankLabels = { "F", "E", "D", "C", "B", "A", "S", "X" };
            int[] rankCount = new int[8];
            foreach (var a in _candidatePool)
            {
                if (a == null) continue;
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                int tier = rankIdx switch
                {
                    0 => 0, 1 or 2 => 1, 3 or 4 => 2, 5 or 6 => 3,
                    7 => 4, 8 or 9 => 5, 10 or 11 or 12 => 6, 13 => 7,
                    _ => -1
                };
                if (tier >= 0) rankCount[tier]++;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(LocalizedTextManager.getText("sm_ui_rank_stats_rank") + ":");
            for (int i = 0; i < 8; i++)
                sb.Append($"<color=#CFC5B0>{rankLabels[i]}</color>:<color=#F2E8D5>{rankCount[i]}</color>  ");
            sb.AppendLine();
            sb.AppendLine();

            // ── 体系分布 ──
            sb.AppendLine(LocalizedTextManager.getText("sm_ui_rank_stats_system") + ":");
            string[] sysKeys = { "sm_ui_filter_mech", "sm_ui_filter_mage", "sm_ui_filter_psi", "sm_ui_filter_martial", "sm_ui_filter_mind" };
            for (int s = 0; s < 5; s++)
            {
                int count = 0;
                foreach (var a in _candidatePool)
                {
                    if (a == null) continue;
                    if (GetSystemIndex(a) == s) count++;
                }
                sb.Append($"<color=#CFC5B0>{LocalizedTextManager.getText(sysKeys[s])}</color>:<color=#F2E8D5>{count}</color>  ");
            }
            sb.AppendLine();
            sb.AppendLine();

            // ── 前三强 ──
            sb.AppendLine(LocalizedTextManager.getText("sm_ui_rank_stats_top") + ":");
            int topN = Mathf.Min(3, _sortedList.Count);
            for (int i = 0; i < topN; i++)
            {
                Actor a = _sortedList[i];
                if (a == null) continue;
                string name = a.name ?? "?";
                if (name.Length > 6) name = name.Substring(0, 6) + "..";
                string rn = SuperMechRanks.GetRankName(a);
                float onar = SuperMechAdvancement.CalcOnar(a);
                string val = onar >= 10000 ? $"{onar / 10000f:F1}w" : $"{onar:F0}";
                Color rc = GetRankColorFor(a);
                string hex = ColorUtility.ToHtmlStringRGB(rc);
                sb.Append($"<color=#D8B45A>#{i + 1}</color> <color=#F2E8D5>{name}</color> <color=#{hex}>{rn}</color> <color=#C98A5A>{val}</color>");
                if (i < topN - 1) sb.AppendLine();
            }
            _statsPanelText.text = sb.ToString();
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
            new Color(0.53f, 0.53f, 0.53f), // F 灰
            new Color(0.80f, 0.80f, 0.80f), // E 银白
            new Color(0.31f, 0.80f, 0.44f), // D 绿
            new Color(0.23f, 0.62f, 1.00f), // C 蓝
            new Color(0.66f, 0.33f, 0.97f), // B 紫
            new Color(0.96f, 0.62f, 0.04f), // A 橙
            new Color(1.00f, 0.42f, 0.21f), // S 橙红
            new Color(1.00f, 0.20f, 0.20f), // X 红
        };

        private static Color GetRankColorFor(Actor a)
        {
            if (a == null) return Color.white;
            int rankIdx = SuperMechAdvancement.GetRankIndex(a);
            if (rankIdx >= 0 && rankIdx < RankColorTable.Length) return RankColorTable[rankIdx];
            return Color.white;
        }
    }

    /// <summary>
    /// v0.53.0 势力列表窗口
    /// 显示所有超能者势力：名称/等级/成员数/领袖/战力，点击展开成员
    /// </summary>
}
