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

    /// <summary>超能者排行榜（v0.33.0重构：三栏布局+筛选+排序+统计）
    /// 左栏：体系/阶位筛选；中栏：排序列表+卡片；右栏：阶位/体系统计
    /// </summary>
    public class SMRankView : MonoBehaviour
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
        private static int _lastViewStart = 9999;
        private static int _lastViewEnd = -1;
        private static bool _sessionPopulated;

        private static readonly string[] SystemNames = { "全部", "机械系", "魔法系", "异能系", "武道系", "念力系" };
        private static readonly string[] RankNames = { "全部", "F", "E", "D", "C", "B", "A", "S", "X" };
        private static readonly string[] SortNames = { "能级排序", "气力排序", "阶位排序", "知识排序", "职业排序" };

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
            catch { }
        }

        private void BuildLayout()
        {
            // 窗口背景
            var bg = transform.Find("Background");
            if (bg != null)
            {
                var bgRect = bg.GetComponent<RectTransform>();
                if (bgRect != null) bgRect.sizeDelta = new Vector2(580, 420);
            }

            // === 左栏：筛选（宽度120）===
            var leftGo = new GameObject("LeftPanel");
            leftGo.transform.SetParent(transform, false);
            var leftRect = leftGo.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0, 1);
            leftRect.pivot = new Vector2(0, 0.5f);
            leftRect.sizeDelta = new Vector2(120, 0);
            leftRect.offsetMin = new Vector2(4, 30);
            leftRect.offsetMax = new Vector2(0, -8);

            var leftBg = leftGo.AddComponent<Image>();
            leftBg.color = new Color(0, 0, 0, 0.15f);

            SMUiSkin.MakeText(leftGo.transform, LocalizedTextManager.getText("sm_ui_rank_filter"), 12, TextAnchor.UpperCenter)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(110, 20);

            // 体系筛选按钮
            var sysContainer = new GameObject("SysFilter");
            sysContainer.transform.SetParent(leftGo.transform, false);
            var sysRect = sysContainer.AddComponent<RectTransform>();
            sysRect.anchorMin = new Vector2(0, 1);
            sysRect.anchorMax = new Vector2(1, 1);
            sysRect.pivot = new Vector2(0.5f, 1);
            sysRect.sizeDelta = new Vector2(0, 130);
            sysRect.anchoredPosition = new Vector2(0, -24);
            var sysLayout = sysContainer.AddComponent<VerticalLayoutGroup>();
            sysLayout.spacing = 2;
            sysLayout.padding = new RectOffset(4, 4, 0, 0);
            sysLayout.childControlHeight = true;
            sysLayout.childControlWidth = true;

            _systemButtons = new List<GameObject>();
            for (int i = 0; i < SystemNames.Length; i++)
            {
                int idx = i - 1; // -1=全部, 0-4=体系
                var btn = SMUiSkin.MakeButton(sysContainer.transform, SystemNames[i], 10, () =>
                {
                    _currentSystemFilter = idx;
                    UpdateSystemButtonColors();
                    RefreshList();
                });
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = _currentSystemFilter == idx ? new Color(0.3f, 0.5f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                var btnRect = btn.GetComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(0, 18);
                _systemButtons.Add(btn.gameObject);
            }

            // 阶位筛选下拉
            var rankLabel = SMUiSkin.MakeText(leftGo.transform, LocalizedTextManager.getText("sm_ui_rank_rank_filter"), 11, TextAnchor.MiddleLeft);
            var rankLabelRect = rankLabel.GetComponent<RectTransform>();
            rankLabelRect.anchorMin = new Vector2(0, 1);
            rankLabelRect.anchorMax = new Vector2(1, 1);
            rankLabelRect.pivot = new Vector2(0.5f, 1);
            rankLabelRect.sizeDelta = new Vector2(0, 18);
            rankLabelRect.anchoredPosition = new Vector2(0, -158);

            _rankDropdown = CreateSimpleDropdown(leftGo.transform, RankNames, OnRankChanged);
            var rankDropRect = _rankDropdown.GetComponent<RectTransform>();
            rankDropRect.anchorMin = new Vector2(0, 1);
            rankDropRect.anchorMax = new Vector2(1, 1);
            rankDropRect.pivot = new Vector2(0.5f, 1);
            rankDropRect.sizeDelta = new Vector2(0, 22);
            rankDropRect.anchoredPosition = new Vector2(0, -178);

            // 清除筛选按钮
            var clearBtn = SMUiSkin.MakeButton(leftGo.transform, LocalizedTextManager.getText("sm_ui_rank_clear"), 10, ClearAllFilters);
            var clearBtnRect = clearBtn.GetComponent<RectTransform>();
            clearBtnRect.anchorMin = new Vector2(0.5f, 0);
            clearBtnRect.anchorMax = new Vector2(0.5f, 0);
            clearBtnRect.pivot = new Vector2(0.5f, 0);
            clearBtnRect.sizeDelta = new Vector2(80, 20);
            clearBtnRect.anchoredPosition = new Vector2(0, 8);
            var clearBtnImg = clearBtn.GetComponent<Image>();
            if (clearBtnImg != null) clearBtnImg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);

            // === 中栏：列表（宽度340）===
            var centerGo = new GameObject("CenterPanel");
            centerGo.transform.SetParent(transform, false);
            var centerRect = centerGo.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0, 0);
            centerRect.anchorMax = new Vector2(1, 1);
            centerRect.offsetMin = new Vector2(128, 30);
            centerRect.offsetMax = new Vector2(-130, -8);

            // 顶部排序栏
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(centerGo.transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 26);

            _sortDropdown = CreateSimpleDropdown(topBar.transform, SortNames, OnSortChanged);
            var sortRect = _sortDropdown.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0, 0.5f);
            sortRect.anchorMax = new Vector2(0, 0.5f);
            sortRect.pivot = new Vector2(0, 0.5f);
            sortRect.sizeDelta = new Vector2(120, 22);
            sortRect.anchoredPosition = new Vector2(4, 0);

            _countText = SMUiSkin.MakeText(topBar.transform, "共 0 人", 10, TextAnchor.MiddleRight);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 0.5f);
            countRect.anchorMax = new Vector2(1, 0.5f);
            countRect.pivot = new Vector2(1, 0.5f);
            countRect.sizeDelta = new Vector2(80, 20);
            countRect.anchoredPosition = new Vector2(-4, 0);

            // 滚动列表
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(centerGo.transform, false);
            _scrollViewRect = scrollGo.AddComponent<RectTransform>();
            _scrollViewRect.anchorMin = Vector2.zero;
            _scrollViewRect.anchorMax = Vector2.one;
            _scrollViewRect.offsetMin = new Vector2(0, 28);
            _scrollViewRect.offsetMax = new Vector2(0, 28);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.2f);
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

            // 底部导航
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(centerGo.transform, false);
            var bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.sizeDelta = new Vector2(0, 24);

            var topBtn = SMUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_top"), 10, ToTop);
            var topBtnRect = topBtn.GetComponent<RectTransform>();
            topBtnRect.anchorMin = new Vector2(0, 0.5f);
            topBtnRect.anchorMax = new Vector2(0, 0.5f);
            topBtnRect.sizeDelta = new Vector2(60, 20);
            topBtnRect.anchoredPosition = new Vector2(4, 0);

            var bottomBtn = SMUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_bottom"), 10, ToBottom);
            var bottomBtnRect = bottomBtn.GetComponent<RectTransform>();
            bottomBtnRect.anchorMin = new Vector2(1, 0.5f);
            bottomBtnRect.anchorMax = new Vector2(1, 0.5f);
            bottomBtnRect.sizeDelta = new Vector2(60, 20);
            bottomBtnRect.anchoredPosition = new Vector2(-4, 0);

            _emptyText = SMUiSkin.MakeText(centerGo.transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.sizeDelta = new Vector2(200, 40);
            _emptyText.gameObject.SetActive(false);

            // === 右栏：统计（宽度120）===
            var rightGo = new GameObject("RightPanel");
            rightGo.transform.SetParent(transform, false);
            var rightRect = rightGo.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1, 0);
            rightRect.anchorMax = new Vector2(1, 1);
            rightRect.pivot = new Vector2(1, 0.5f);
            rightRect.sizeDelta = new Vector2(120, 0);
            rightRect.offsetMin = new Vector2(0, 30);
            rightRect.offsetMax = new Vector2(-4, -8);

            var rightBg = rightGo.AddComponent<Image>();
            rightBg.color = new Color(0, 0, 0, 0.15f);

            SMUiSkin.MakeText(rightGo.transform, LocalizedTextManager.getText("sm_ui_rank_stats"), 12, TextAnchor.UpperCenter)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(110, 20);

            _statsText = SMUiSkin.MakeText(rightGo.transform, "", 9, TextAnchor.UpperLeft);
            var statsRect = _statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.offsetMin = new Vector2(6, 28);
            statsRect.offsetMax = new Vector2(-6, -6);
            _statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statsText.verticalOverflow = VerticalWrapMode.Overflow;
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
                catch { }
            }

            _sessionPopulated = true;

            // 更新UI
            if (_listContent != null)
                _listContent.sizeDelta = new Vector2(0, (_sortedList.Count + 1) * 44f);
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

            // 阶位分布
            int[] rankCount = new int[9]; // F,E,D,C,B,A,S,X,其他
            int[] sysCount = new int[5];  // 机械,魔法,异能,武道,念力
            int awakened = 0;

            foreach (var a in _candidatePool)
            {
                awakened++;
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                int tier = rankIdx switch
                {
                    0 => 0, 1 or 2 => 1, 3 or 4 => 2, 5 or 6 => 3,
                    7 => 4, 8 or 9 => 5, 10 or 11 or 12 => 6, 13 => 7,
                    _ => 8
                };
                rankCount[tier]++;

                int sysIdx = GetSystemIndex(a);
                if (sysIdx >= 0) sysCount[sysIdx]++;
            }

            string tRankDist = LocalizedTextManager.getText("sm_ui_rank_distribution");
            string tSysDist = LocalizedTextManager.getText("sm_ui_sys_distribution");
            string tAwakened = LocalizedTextManager.getText("sm_ui_awakened_count");

            string stats = $"<b>{tRankDist}</b>\n";
            string[] rankLabels = { "F", "E", "D", "C", "B", "A", "S", "X" };
            for (int i = 0; i < 8; i++)
            {
                if (rankCount[i] > 0)
                    stats += $"{rankLabels[i]}: {rankCount[i]}  ";
            }
            stats += $"\n\n<b>{tSysDist}</b>\n";
            for (int i = 0; i < 5; i++)
            {
                if (sysCount[i] > 0)
                    stats += $"{SystemNames[i + 1]}: {sysCount[i]}\n";
            }
            stats += $"\n<b>{tAwakened}</b>: {awakened}";

            _statsText.text = stats;
        }

        private void OnScrollUpdate()
        {
            if (_sortedList.Count == 0 || _listContent == null || _scrollViewRect == null) return;

            float scrollY = _listContent.anchoredPosition.y;
            float viewHeight = _scrollViewRect.rect.height > 0 ? _scrollViewRect.rect.height : 300f;
            int startIdx = Mathf.Max(0, Mathf.FloorToInt(scrollY / 44f) - 1);
            int endIdx = Mathf.Min(Mathf.CeilToInt((scrollY + viewHeight) / 44f) + 1, _sortedList.Count - 1);

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
            int visibleCount = Mathf.CeilToInt(viewHeight / 44f) + 2;
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
            cardRect.sizeDelta = new Vector2(0, 40);
            cardRect.anchoredPosition = new Vector2(0, -index * 44f - 2);

            var card = cardGo.AddComponent<SMRankCard>();
            float score = _scoreCache.TryGetValue(actor.getID(), out float s) ? s : 0;
            string sortLabel = SortNames[_currentSortType].Replace("排序", "");
            card.Build(actor, index + 1, score, sortLabel);

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

        public static void InvalidateCache()
        {
            _sessionPopulated = false;
            _candidatePool.Clear();
            _sortedList.Clear();
            _scoreCache.Clear();
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
            string tUnlocked = LocalizedTextManager.getText("sm_ui_san_unlocked");
            string tFragments = LocalizedTextManager.getText("sm_ui_san_fragments");
            string tTotalAuth = LocalizedTextManager.getText("sm_ui_san_total_auth");
            string tTotalVisits = LocalizedTextManager.getText("sm_ui_san_total_visits");
            string tDivinity = LocalizedTextManager.getText("sm_ui_san_divinity");
            string tReviveCount = LocalizedTextManager.getText("sm_ui_san_revive_count");
            string tIteration = LocalizedTextManager.getText("sm_ui_san_iteration");
            string tHistory = LocalizedTextManager.getText("sm_ui_san_history");
            statsText.text = $"<color=#6ab7ff>{tUnlocked}:</color> {data.unlocked_sanctuaries}/6  " +
                $"<color=#6ab7ff>{tFragments}:</color> {data.key_fragments}  " +
                $"<color=#6ab7ff>{tTotalAuth}:</color> {data.total_permission}\n" +
                $"<color=#6ab7ff>{tTotalVisits}:</color> {data.total_visits}  " +
                $"<color=#6ab7ff>{tDivinity}:</color> {data.total_divinity_ascensions}  " +
                $"<color=#6ab7ff>{tReviveCount}:</color> {data.total_resurrections}\n" +
                $"<color=#ffd700>{tIteration}:</color> {SuperMechCosmicIteration.CurrentIteration}  " +
                $"<color=#ffd700>{tHistory}:</color> {SuperMechCivilizationData.GetHistory().Count}";

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
            string tClass = LocalizedTextManager.getText("sm_ui_san_class");
            string tFragProgress = LocalizedTextManager.getText("sm_ui_san_frag_progress");
            string tUnlocked = LocalizedTextManager.getText("sm_ui_san_unlocked_tag");
            string tLocked = LocalizedTextManager.getText("sm_ui_san_locked_tag");
            AddDetailLine($"{tClass}: {cls}", 12);
            AddDetailLine($"{tFragProgress}: {frags}/{SuperMechSanctuary.FragmentsToUnlock} {(unlocked ? $"<color=#4f4>[{tUnlocked}]</color>" : $"<color=#f84>[{tLocked}]</color>")}", 12);
            AddDetailLine("", 4);

            // 钥匙系统说明（v0.31.0：原著设定补充）
            AddDetailLine($"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_sanctuary_key_title")}</color>", 12);
            AddDetailLine($"<size=10><color=#aaa>{LocalizedTextManager.getText("sm_ui_sanctuary_key_desc")}</color></size>", 10);
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

            string tAuthStats = LocalizedTextManager.getText("sm_ui_san_auth_stats");
            string tAuthUnits = LocalizedTextManager.getText("sm_ui_san_auth_units");
            string tAuthRecorded = LocalizedTextManager.getText("sm_ui_san_auth_recorded");
            string tAuthNone = LocalizedTextManager.getText("sm_ui_san_auth_none");
            string tMaxAuth = LocalizedTextManager.getText("sm_ui_san_max_auth");
            string tUnlockCond = LocalizedTextManager.getText("sm_ui_san_unlock_cond");
            string tCollectFrags = LocalizedTextManager.getText("sm_ui_san_collect_frags");
            string tFragSource = LocalizedTextManager.getText("sm_ui_san_frag_source");

            AddDetailLine($"<color=#6ab7ff>{tAuthStats}</color>", 13);
            AddDetailLine($"{tAuthUnits}: {(totalAuth > 0 ? tAuthRecorded : tAuthNone)}", 12);
            AddDetailLine($"{tMaxAuth}: {maxAuth}", 12);
            AddDetailLine("", 8);
            AddDetailLine($"<color=#6ab7ff>{tUnlockCond}</color>", 13);
            AddDetailLine($"{tCollectFrags} {SuperMechSanctuary.FragmentsToUnlock}", 12);
            AddDetailLine(tFragSource, 12);
            AddDetailLine("", 8);

            // 访问圣所按钮（v0.30.0新增：选中单位后可访问圣所获得知识和权限）
            var visitBtnGo = new GameObject("VisitBtn");
            visitBtnGo.transform.SetParent(_detailContent, false);
            var visitBtnRect = visitBtnGo.AddComponent<RectTransform>();
            visitBtnRect.anchorMin = new Vector2(0, 1);
            visitBtnRect.anchorMax = new Vector2(1, 1);
            visitBtnRect.pivot = new Vector2(0, 1);
            visitBtnRect.sizeDelta = new Vector2(0, 32);
            var visitBtnImg = visitBtnGo.AddComponent<Image>();
            visitBtnImg.color = unlocked ? new Color(0.2f, 0.5f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.6f);
            var visitBtn = visitBtnGo.AddComponent<Button>();
            var visitBtnText = SMUiSkin.MakeText(visitBtnGo.transform,
                unlocked ? LocalizedTextManager.getText("sm_ui_visit_sanctuary") : LocalizedTextManager.getText("sm_ui_sanctuary_locked"),
                13, TextAnchor.MiddleCenter);
            visitBtnText.color = Color.white;
            visitBtnText.fontStyle = FontStyle.Bold;
            var visitBtnTextRect = visitBtnText.GetComponent<RectTransform>();
            visitBtnTextRect.anchorMin = Vector2.zero;
            visitBtnTextRect.anchorMax = Vector2.one;
            visitBtnTextRect.offsetMin = Vector2.zero;
            visitBtnTextRect.offsetMax = Vector2.zero;
            visitBtn.interactable = unlocked;
            visitBtn.onClick.AddListener(() =>
            {
                Actor visitor = SelectedUnit.unit;
                if (visitor == null || !visitor.isAlive())
                {
                    Debug.LogWarning("[超神机械师] 访问圣所失败：请先选中一个存活单位");
                    return;
                }
                if (SuperMechSanctuary.VisitSanctuary(visitor, _selectedIndex))
                {
                    RefreshDetail(); // 刷新权限统计
                }
            });

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
