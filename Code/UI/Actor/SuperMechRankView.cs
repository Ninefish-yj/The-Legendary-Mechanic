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
            // === v0.230 简化顶部工具栏：一行体系筛选 + 右侧统计 ===
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 36);
            topRect.offsetMin = new Vector2(4, 0);
            topRect.offsetMax = new Vector2(-4, 0);
            var topBg = topBar.AddComponent<Image>();
            topBg.raycastTarget = true;
            topBg.color = new Color(0.08f, 0.10f, 0.16f, 0.6f);

            // 体系筛选按钮（横向，左侧70%）
            var sysRow = new GameObject("SysRow");
            sysRow.transform.SetParent(topBar.transform, false);
            var sysRowRect = sysRow.AddComponent<RectTransform>();
            sysRowRect.anchorMin = Vector2.zero;
            sysRowRect.anchorMax = new Vector2(0.7f, 1);
            sysRowRect.pivot = new Vector2(0, 0.5f);
            sysRowRect.offsetMin = new Vector2(6, 4);
            sysRowRect.offsetMax = new Vector2(-4, -4);
            var sysLayout = sysRow.AddComponent<HorizontalLayoutGroup>();
            sysLayout.spacing = 4;
            sysLayout.childControlHeight = true;
            sysLayout.childControlWidth = true;
            sysLayout.childForceExpandWidth = true;

            _systemButtons = new List<GameObject>();
            for (int i = 0; i < SystemNames.Length; i++)
            {
                int idx = i - 1;
                var btn = SuperMechUiSkin.MakeButton(sysRow.transform, SystemNames[i], 10, () =>
                {
                    _currentSystemFilter = idx;
                    UpdateSystemButtonColors();
                    RefreshList();
                });
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.sprite = _currentSystemFilter == idx ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = Color.white;
                }
                _systemButtons.Add(btn.gameObject);
            }

            // 统计文字（右侧30%）
            _countText = SuperMechUiSkin.MakeText(topBar.transform, LocalizedTextManager.getText("sm_ui_legion_count_zero"), 11, TextAnchor.MiddleRight);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.7f, 0);
            countRect.anchorMax = Vector2.one;
            countRect.pivot = new Vector2(1, 0.5f);
            countRect.offsetMin = new Vector2(4, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            // 阶位分布统计（统计文字下方小字）
            _statsText = SuperMechUiSkin.MakeText(topBar.transform, "", 9, TextAnchor.LowerRight);
            var statsRect = _statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.7f, 0);
            statsRect.anchorMax = Vector2.one;
            statsRect.pivot = new Vector2(1, 0);
            statsRect.offsetMin = new Vector2(4, 2);
            statsRect.offsetMax = new Vector2(-8, 16);
            _statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statsText.verticalOverflow = VerticalWrapMode.Truncate;

            // === 中间：全宽滚动列表 ===
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            _scrollViewRect = scrollGo.AddComponent<RectTransform>();
            _scrollViewRect.anchorMin = Vector2.zero;
            _scrollViewRect.anchorMax = Vector2.one;
            _scrollViewRect.offsetMin = new Vector2(4, 42);
            _scrollViewRect.offsetMax = new Vector2(-4, -40);

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
            topBtnRect.sizeDelta = new Vector2(70, 40);

            var bottomBtn = SuperMechUiSkin.MakeButton(bottomBar.transform, LocalizedTextManager.getText("sm_ui_rank_to_bottom"), 9, ToBottom);
            var bottomBtnRect = bottomBtn.GetComponent<RectTransform>();
            bottomBtnRect.sizeDelta = new Vector2(70, 40);

            _emptyText = SuperMechUiSkin.MakeText(transform, LocalizedTextManager.getText("sm_ui_no_data"), 14, TextAnchor.MiddleCenter);
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRect.sizeDelta = new Vector2(200, 40);
            _emptyText.gameObject.SetActive(false);
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
                img.sprite = _currentSystemFilter == idx ? SuperMechUiSkin.TabActive : SuperMechUiSkin.TabNormal;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
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
            if (_statsText == null) return;
            _statsText.text = RankSorter.CalculateRankStats(_candidatePool);
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
    }

    /// <summary>
    /// v0.53.0 势力列表窗口
    /// 显示所有超能者势力：名称/等级/成员数/领袖/战力，点击展开成员
    /// </summary>
}
