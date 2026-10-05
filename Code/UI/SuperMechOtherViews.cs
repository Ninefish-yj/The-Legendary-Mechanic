using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>机械制造（v0.76.48 壳子→真实实现）：配方列表+材料+冷却+制造按钮</summary>
    public class SuperMechCraftView : MonoBehaviour
    {
        public static Actor OverrideActor; // 制造者（单位面板入口传入）
        private RectTransform _listContent;
        private Text _headText;

        private static readonly Dictionary<string, string> MaterialNames = new Dictionary<string, string>
        {
            { "common_metals", "金属" }, { "gems", "宝石" }, { "stone", "石头" }, { "wood", "木材" },
            { "gold", "金" }, { "adamantine", "精金" }, { "dragon_scales", "龙鳞" }
        };

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] View初始化失败: " + e); }
        }

        private Actor Maker
        {
            get
            {
                if (OverrideActor != null && OverrideActor.isAlive()) return OverrideActor;
                return null;
            }
        }

        private void BuildLayout()
        {
            _headText = SuperMechUiSkin.MakeText(transform, "", 14, TextAnchor.UpperLeft);
            var hr = _headText.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(12, -60);
            hr.offsetMax = new Vector2(-12, -8);

            var listGo = new GameObject("CraftList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -66);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            Actor maker = Maker;
            if (maker == null || !maker.hasTrait(SuperMechTraits.ClassMech))
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_craft_none");
                SuperMechUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_craft_none"), 14, TextAnchor.UpperLeft);
                return;
            }

            var recipes = SuperMechCrafting.GetAvailableRecipes(maker);
            int minionCount = SuperMechCrafting.GetMinionCount(maker);
            _headText.text = string.Format("{0}: {1}    {2}: {3}/{4}",
                LocalizedTextManager.getText("sm_ui_craft_maker"), maker.getName(),
                LocalizedTextManager.getText("sm_ui_craft_minion"), minionCount, SuperMechConfig.MaxSummonedUnits);

            if (recipes.Count == 0)
            {
                SuperMechUiSkin.MakeText(_listContent, LocalizedTextManager.getText("sm_ui_craft_none"), 14, TextAnchor.UpperLeft);
                return;
            }

            foreach (var r in recipes)
            {
                var rowGo = new GameObject("Row");
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 44);

                string costStr = BuildCostText(r.cost);
                float cd = SuperMechCrafting.GetCraftCooldown(maker);
                string cdStr = cd > 0 ? string.Format(LocalizedTextManager.getText("sm_ui_craft_cd_left"), (int)cd) : "";
                string line = string.Format("{0}（{1}）\n{2}    {3}",
                    LocalizedTextManager.getText(r.name), LocalizedTextManager.getText(r.desc),
                    costStr, cdStr);
                var txt = SuperMechUiSkin.MakeText(row, line, 12, TextAnchor.UpperLeft);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(0, 0);
                tr.offsetMax = new Vector2(-120, 0);

                bool canCraft = cd <= 0 && SuperMechCrafting.HasMaterials(maker, r.cost);
                var btn = SuperMechUiSkin.MakeButton(row,
                    LocalizedTextManager.getText("sm_ui_craft_button"), 12,
                    () => { TryCraftAndRefresh(maker, r.id); });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(100, 30);
                br.anchoredPosition = new Vector2(-50, 0);
                if (!canCraft) btn.interactable = false;
            }
        }

        private string BuildCostText(Dictionary<string, int> cost)
        {
            if (cost == null || cost.Count == 0) return LocalizedTextManager.getText("sm_ui_craft_no_cost");
            var parts = new List<string>();
            foreach (var kv in cost)
            {
                string matName = MaterialNames.TryGetValue(kv.Key, out string mn) ? mn : kv.Key;
                parts.Add(string.Format("{0}×{1}", matName, kv.Value));
            }
            return string.Format("{0}: {1}", LocalizedTextManager.getText("sm_ui_craft_cost"), string.Join(" ", parts));
        }

        private void TryCraftAndRefresh(Actor maker, string recipeId)
        {
            if (maker == null || !maker.isAlive()) return;
            WorldTile tile = null;
            try { tile = maker.current_tile; } catch { }
            if (tile == null) return;
            bool ok = SuperMechCrafting.TryCraft(maker, recipeId, tile);
            if (ok)
            {
                RefreshAll();
            }
        }
    }

    /// <summary>超能者排行榜（v0.33.0重构：三栏布局+筛选+排序+统计）
    /// 左栏：体系/阶位筛选；中栏：排序列表+卡片；右栏：阶位/体系统计
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
            float onar = SuperMechEnergyLevel.Calculate(actor);
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
    public class SuperMechFactionView : MonoBehaviour
    {
        private RectTransform _listContent;
        private Text _countText;
        private Text _emptyText;
        private Text _detailText;
        private static string _selectedFactionId;

        void Awake() { try { BuildLayout(); } catch (System.Exception e) { Debug.LogError("[超神机械师] FactionView初始化失败: " + e); } }
        void OnEnable() { try { RefreshList(); } catch (System.Exception e) { Debug.LogError("[超神机械师] FactionView刷新失败: " + e); } }

        private void BuildLayout()
        {
            // 顶部统计栏
            var topBar = SuperMechUiBuilder.CreatePanel(transform, "TopBar", new Color(0, 0, 0, 0.2f), 4, 4, 0, 0);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 28);
            _countText = SuperMechUiBuilder.AddText(topBar, LocalizedTextManager.getText("sm_ui_faction_count_zero"), 13, TextAnchor.MiddleLeft);
            var countRect = _countText.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(8, 0);
            countRect.offsetMax = new Vector2(-8, 0);

            // 滚动列表
            var scrollGo = new GameObject("FactionScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.35f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.pivot = new Vector2(0.5f, 1);
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-4, -32);
            var scroll = scrollGo.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<UnityEngine.UI.Mask>();
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _listContent = content.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 100);
            scroll.viewport = vpRect;
            scroll.content = _listContent;
            scroll.vertical = true;
            scroll.horizontal = false;

            _emptyText = SuperMechUiBuilder.AddText(content, "暂无势力（超能者达到B阶后会自动创建势力）", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
            var emptyRect = _emptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0.5f);
            emptyRect.anchorMax = new Vector2(1, 0.5f);
            emptyRect.sizeDelta = new Vector2(0, 30);

            // 底部成员详情
            var detailPanel = SuperMechUiBuilder.CreatePanel(transform, "DetailPanel", new Color(0, 0, 0, 0.15f), 4, 4, 0, 0);
            var dRect = detailPanel.GetComponent<RectTransform>();
            dRect.anchorMin = Vector2.zero;
            dRect.anchorMax = new Vector2(1, 0.35f);
            dRect.pivot = new Vector2(0.5f, 0);
            dRect.offsetMin = new Vector2(4, 4);
            dRect.offsetMax = new Vector2(-4, 0);
            _detailText = SuperMechUiBuilder.AddText(detailPanel, "点击势力查看成员", 11, TextAnchor.UpperLeft);
            var dtRect = _detailText.GetComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(8, 8);
            dtRect.offsetMax = new Vector2(-8, -8);
            _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshList()
        {
            if (_listContent == null || _countText == null || _emptyText == null) return; // v0.75.11: 布局未构建时防御
            // 清空旧条目
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i);
                if (child.name != "EmptyText") Destroy(child.gameObject);
            }

            var factions = SuperMechFaction.GetAllFactions();
            _countText.text = $"势力总数: {factions.Count}";
            _emptyText.gameObject.SetActive(factions.Count == 0);

            float y = 0f;
            foreach (var f in factions)
            {
                var row = CreateFactionRow(f, ref y);
                if (f.id == _selectedFactionId) ShowMembers(f);
            }
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(y, 100f));
        }

        private GameObject CreateFactionRow(SuperMechFaction.FactionData f, ref float y)
        {
            var row = new GameObject($"Faction_{f.id}");
            row.transform.SetParent(_listContent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 36);
            rect.anchoredPosition = new Vector2(0, -y);
            y += 38f;

            var bg = row.AddComponent<UnityEngine.UI.Image>();
            bg.color = f.id == _selectedFactionId ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : new Color(1, 1, 1, 0.05f);

            var leaderName = "?";
            var units = World.world.units?.units_only_alive;
            if (units != null)
            {
                foreach (var a in units) { if (a != null && a.id == f.leaderId) { leaderName = a.name; break; } }
            }

            var label = SuperMechUiBuilder.AddText(row,
                $"[{f.level}级] {f.name}  |  成员:{f.memberIds.Count}  |  领袖:{leaderName}  |  战力:{f.totalPower:F0}",
                11, TextAnchor.MiddleLeft);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, 0);

            var btn = row.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => {
                _selectedFactionId = (_selectedFactionId == f.id) ? null : f.id;
                RefreshList();
            });
            return row;
        }

        private void ShowMembers(SuperMechFaction.FactionData f)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {f.name} 成员列表 ===");
            var units = World.world.units?.units_only_alive;
            int idx = 1;
            foreach (var mid in f.memberIds)
            {
                if (units == null) break;
                foreach (var a in units)
                {
                    if (a == null || a.id != mid) continue;
                    int rank = SuperMechAdvancement.GetExactRankIndex(a);
                    string rankName = (rank >= 0 && rank < SuperMechRanks.All.Count)
                        ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name)
                        : "?";
                    string role = (a.id == f.leaderId) ? "领袖" : "成员";
                    sb.AppendLine($"{idx}. {a.name} [{rankName}] {role}");
                    idx++;
                    break;
                }
            }
            if (idx == 1) sb.AppendLine("（无存活成员）");
            _detailText.text = sb.ToString();
        }
    }

    /// <summary>宇宙宝物装备窗口（v0.76.48 接线：图鉴列表+装备按钮）</summary>
    public class SuperMechCosmicRelicView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _listContent;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] CosmicRelicView初始化失败: " + e); }
        }

        private Actor Target
        {
            get
            {
                if (OverrideActor != null && OverrideActor.isAlive()) return OverrideActor;
                return null;
            }
        }

        private void BuildLayout()
        {
            _headText = SuperMechUiSkin.MakeText(transform, "", 14, TextAnchor.UpperLeft);
            var hr = _headText.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(12, -36);
            hr.offsetMax = new Vector2(-12, -8);

            var listGo = new GameObject("RelicList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -42);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            Actor a = Target;
            if (a == null)
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_need_target");
                return;
            }

            string equipped = SuperMechCosmicRelic.GetEquippedName(a);
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_cr_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_cr_equipped"),
                string.IsNullOrEmpty(equipped) ? LocalizedTextManager.getText("sm_ui_cr_none") : LocalizedTextManager.getText(equipped));

            foreach (var r in SuperMechCosmicRelic.Relics)
            {
                var rowGo = new GameObject("Relic_"+r.id);
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 40);

                string wonderMark = r.isWonder ? "★ " : "";
                string line = string.Format("{0}{1}\n<color=#8fa8c8>{2}</color>",
                    wonderMark, LocalizedTextManager.getText(r.name), LocalizedTextManager.getText(r.desc));
                var txt = SuperMechUiSkin.MakeText(row, line, 12, TextAnchor.UpperLeft);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(0, 0);
                tr.offsetMax = new Vector2(-130, 0);

                bool isCurrent = a.hasTrait(r.id);
                var btn = SuperMechUiSkin.MakeButton(row,
                    isCurrent ? LocalizedTextManager.getText("sm_ui_cr_equipped_short") : LocalizedTextManager.getText("sm_ui_cr_equip"), 12,
                    () => { SuperMechCosmicRelic.EquipCosmicRelic(a, r.id); RefreshAll(); });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(110, 26);
                br.anchoredPosition = new Vector2(-55, 0);
            }
        }
    }


    /// <summary>次级维度窗口（v0.76.48 接线：维度列表+进入/退出按钮）</summary>
    public class SuperMechDimensionView : MonoBehaviour
    {
        public static Actor OverrideActor;
        private RectTransform _listContent;
        private Text _headText;

        void Awake()
        {
            try { BuildLayout(); }
            catch (System.Exception e) { Debug.LogError("[超神机械师] DimensionView初始化失败: " + e); }
        }

        private Actor Target
        {
            get
            {
                if (OverrideActor != null && OverrideActor.isAlive()) return OverrideActor;
                return null;
            }
        }

        private void BuildLayout()
        {
            _headText = SuperMechUiSkin.MakeText(transform, "", 14, TextAnchor.UpperLeft);
            var hr = _headText.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1);
            hr.offsetMin = new Vector2(12, -36);
            hr.offsetMax = new Vector2(-12, -8);

            var listGo = new GameObject("DimList");
            listGo.transform.SetParent(transform, false);
            _listContent = listGo.AddComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 0);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.offsetMin = new Vector2(12, 12);
            _listContent.offsetMax = new Vector2(-12, -42);

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headText == null || _listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            Actor a = Target;
            if (a == null)
            {
                _headText.text = LocalizedTextManager.getText("sm_ui_need_target");
                return;
            }

            string active = SuperMechDimension.GetActiveDimension(a);
            _headText.text = string.Format("{0}: {1}    {2}: {3}",
                LocalizedTextManager.getText("sm_ui_dim_target"), a.getName(),
                LocalizedTextManager.getText("sm_ui_dim_active"),
                string.IsNullOrEmpty(active) ? LocalizedTextManager.getText("sm_ui_dim_none") : LocalizedTextManager.getText(SuperMechDimension.GetDef(active).name));

            foreach (var dim in SuperMechDimension.Dimensions)
            {
                var rowGo = new GameObject("Dim_"+dim.id);
                rowGo.transform.SetParent(_listContent, false);
                var row = rowGo.AddComponent<RectTransform>();
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, 42);

                bool canEnter = SuperMechDimension.CanEnter(a, dim);
                string lockedMark = canEnter ? "" : "（" + LocalizedTextManager.getText("sm_ui_dim_locked") + "）";
                string line = string.Format("{0}{1}\n<color=#8fa8c8>{2}</color>",
                    LocalizedTextManager.getText(dim.name), lockedMark, LocalizedTextManager.getText(dim.desc));
                var txt = SuperMechUiSkin.MakeText(row, line, 12, TextAnchor.UpperLeft);
                var tr = txt.GetComponent<RectTransform>();
                tr.anchorMin = new Vector2(0, 0);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = new Vector2(0, 0);
                tr.offsetMax = new Vector2(-130, 0);

                bool isActive = active == dim.id;
                var btn = SuperMechUiSkin.MakeButton(row,
                    isActive ? LocalizedTextManager.getText("sm_ui_dim_inside") : LocalizedTextManager.getText("sm_ui_dim_enter"), 12,
                    () => { SuperMechDimension.Enter(a, dim); RefreshAll(); });
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(1, 0.5f);
                br.anchorMax = new Vector2(1, 0.5f);
                br.sizeDelta = new Vector2(110, 26);
                br.anchoredPosition = new Vector2(-55, 0);
                if (!canEnter || isActive) btn.interactable = false;
            }
        }
    }

}
