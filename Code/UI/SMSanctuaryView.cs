using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>圣所独立空间视图（v0.35.0 UI重构）
    /// 圣所不在地图上，是独立于宇宙的信息态空间。
    /// 点击入口后整个UI切换为圣所视图：深空背景 + 星图节点 + 详情面板 + 信息态库
    /// </summary>
    public class SMSanctuaryView : MonoBehaviour
    {
        private static SMSanctuaryView _instance;
        public static bool IsOpen => _instance != null;

        private RectTransform _rootRect;
        private RectTransform _starMapContent;
        private RectTransform _detailContent;
        private RectTransform _infoStateContent;
        private Text _statusText;
        private Text _detailTitle;
        private readonly List<SMSanctuaryNode> _nodes = new();
        private readonly List<GameObject> _detailItems = new();
        private readonly List<GameObject> _infoItems = new();
        private int _selectedIndex = -1;
        private bool _showInfoState = false;

        // 星图节点位置（6个圣所按紧凑排列）
        private static readonly Vector2[] NodePositions =
        {
            new Vector2(-160, 100),   // 第一圣所：左上
            new Vector2(0, 130),      // 第二圣所：上中
            new Vector2(160, 100),    // 第三圣所：右上
            new Vector2(-160, -60),   // 第四圣所：左下
            new Vector2(0, -90),      // 第五圣所：下中
            new Vector2(160, -60),    // 第六圣所：右下
        };

        public static void Toggle()
        {
            if (_instance != null)
            {
                _instance.Close();
            }
            else
            {
                Open();
            }
        }

        public static void Open()
        {
            if (_instance != null) return;
            var canvas = SMUguiWindow.GetCanvas();
            var go = new GameObject("SMSanctuarySpaceView");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _instance = go.AddComponent<SMSanctuaryView>();
            _instance.Build();
        }

        public void Close()
        {
            if (_instance == this) _instance = null;
            Destroy(gameObject);
        }

        private void Build()
        {
            _rootRect = GetComponent<RectTransform>();

            // 深空背景
            var bgGo = new GameObject("SpaceBg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.06f, 0.96f);
            bgImg.raycastTarget = true;

            // 星点背景
            BuildStars(bgGo.transform);

            // 顶部标题栏
            BuildTopBar();

            // 中间星图区域
            BuildStarMap();

            // 右侧详情面板
            BuildDetailPanel();

            // 底部状态栏
            BuildStatusBar();

            RefreshAll();
        }

        private void BuildStars(Transform parent)
        {
            // 网格背景（参考道途树样式）
            var gridGo = new GameObject("GridBg");
            gridGo.transform.SetParent(parent, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            var gridImg = gridGo.AddComponent<Image>();
            gridImg.color = new Color(0.03f, 0.05f, 0.1f, 0.5f);
            gridImg.raycastTarget = false;

            // 横向网格线
            for (int y = -4; y <= 4; y++)
            {
                var lineGo = new GameObject($"HLine_{y}");
                lineGo.transform.SetParent(gridGo.transform, false);
                var lineRect = lineGo.AddComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0, 0.5f);
                lineRect.anchorMax = new Vector2(1, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.sizeDelta = new Vector2(0, 1);
                lineRect.anchoredPosition = new Vector2(0, y * 50);
                var lineImg = lineGo.AddComponent<Image>();
                lineImg.color = new Color(0.1f, 0.2f, 0.4f, 0.15f);
                lineImg.raycastTarget = false;
            }

            // 纵向网格线
            for (int x = -6; x <= 6; x++)
            {
                var lineGo = new GameObject($"VLine_{x}");
                lineGo.transform.SetParent(gridGo.transform, false);
                var lineRect = lineGo.AddComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0);
                lineRect.anchorMax = new Vector2(0.5f, 1);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.sizeDelta = new Vector2(1, 0);
                lineRect.anchoredPosition = new Vector2(x * 50, 0);
                var lineImg = lineGo.AddComponent<Image>();
                lineImg.color = new Color(0.1f, 0.2f, 0.4f, 0.15f);
                lineImg.raycastTarget = false;
            }

            // 随机星点
            for (int i = 0; i < 30; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(parent, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0, 0);
                starRect.anchorMax = new Vector2(1, 1);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float x = Random.Range(-0.45f, 0.45f);
                float y = Random.Range(-0.4f, 0.4f);
                starRect.anchoredPosition = new Vector2(x * Screen.width, y * Screen.height);
                float size = Random.Range(1, 2);
                starRect.sizeDelta = new Vector2(size, size);
                var starImg = starGo.AddComponent<Image>();
                starImg.color = new Color(1, 1, 1, Random.Range(0.1f, 0.4f));
                starImg.raycastTarget = false;
            }
        }

        private void BuildTopBar()
        {
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 50);
            topRect.anchoredPosition = Vector2.zero;
            var topImg = topGo.AddComponent<Image>();
            topImg.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);

            // 返回世界按钮
            var backGo = new GameObject("BackBtn");
            backGo.transform.SetParent(topGo.transform, false);
            var backRect = backGo.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.sizeDelta = new Vector2(120, 34);
            backRect.anchoredPosition = new Vector2(12, 0);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.2f, 0.3f, 0.5f, 0.9f);
            var backBtn = backGo.AddComponent<Button>();
            var backText = SMUiSkin.MakeText(backGo.transform,
                LocalizedTextManager.getText("sm_ui_san_back_world"), 13, TextAnchor.MiddleCenter);
            backText.color = Color.white;
            backText.fontStyle = FontStyle.Bold;
            var backTextRect = backText.GetComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;
            backBtn.onClick.AddListener(Close);

            // 标题
            var titleText = SMUiSkin.MakeText(topGo.transform,
                LocalizedTextManager.getText("sm_ui_san_space_title"), 18, TextAnchor.MiddleCenter);
            titleText.color = new Color(0.4f, 0.7f, 1.0f);
            titleText.fontStyle = FontStyle.Bold;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(140, 0);
            titleRect.offsetMax = new Vector2(-140, 0);

            // 信息态库切换按钮
            var infoGo = new GameObject("InfoStateBtn");
            infoGo.transform.SetParent(topGo.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 0.5f);
            infoRect.anchorMax = new Vector2(1, 0.5f);
            infoRect.pivot = new Vector2(1, 0.5f);
            infoRect.sizeDelta = new Vector2(120, 34);
            infoRect.anchoredPosition = new Vector2(-12, 0);
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.3f, 0.2f, 0.4f, 0.9f);
            var infoBtn = infoGo.AddComponent<Button>();
            var infoText = SMUiSkin.MakeText(infoGo.transform,
                LocalizedTextManager.getText("sm_ui_san_info_library"), 13, TextAnchor.MiddleCenter);
            infoText.color = Color.white;
            var infoTextRect = infoText.GetComponent<RectTransform>();
            infoTextRect.anchorMin = Vector2.zero;
            infoTextRect.anchorMax = Vector2.one;
            infoTextRect.offsetMin = Vector2.zero;
            infoTextRect.offsetMax = Vector2.zero;
            infoBtn.onClick.AddListener(ToggleInfoState);
        }

        private void BuildStarMap()
        {
            var mapGo = new GameObject("StarMap");
            mapGo.transform.SetParent(transform, false);
            var mapRect = mapGo.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = new Vector2(0.65f, 1);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.offsetMin = new Vector2(0, 60);
            mapRect.offsetMax = new Vector2(0, -50);

            _starMapContent = mapRect;

            // 圣所之间的信息流连线
            BuildConnectionLines(mapGo.transform);

            // 创建6个节点
            for (int i = 0; i < SuperMechSanctuary.TotalSanctuaries; i++)
            {
                int idx = i;
                var node = SMSanctuaryNode.Create(mapGo.transform, i, NodePositions[i], OnNodeClick);
                _nodes.Add(node);
            }
        }

        private void BuildConnectionLines(Transform parent)
        {
            // 用细线连接相邻节点（青色虚线效果用半透明Image模拟）
            var lines = new (int, int)[]
            {
                (0, 1), (1, 2), (0, 3), (1, 4), (2, 5), (3, 4), (4, 5), (0, 4), (2, 4)
            };

            foreach (var (a, b) in lines)
            {
                Vector2 pa = NodePositions[a];
                Vector2 pb = NodePositions[b];
                Vector2 mid = (pa + pb) * 0.5f;
                float length = Vector2.Distance(pa, pb);
                float angle = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg;

                var lineGo = new GameObject($"Line_{a}_{b}");
                lineGo.transform.SetParent(parent, false);
                var lineRect = lineGo.AddComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.sizeDelta = new Vector2(length, 2);
                lineRect.anchoredPosition = mid;
                lineRect.localRotation = Quaternion.Euler(0, 0, angle);
                var lineImg = lineGo.AddComponent<Image>();
                lineImg.color = new Color(0.2f, 0.5f, 0.9f, 0.4f);
                lineImg.raycastTarget = false;
            }
        }

        private void BuildDetailPanel()
        {
            var panelGo = new GameObject("DetailPanel");
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.65f, 0);
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = new Vector2(4, 60);
            panelRect.offsetMax = new Vector2(-4, -50);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);

            // 详情标题
            _detailTitle = SMUiSkin.MakeText(panelGo.transform,
                LocalizedTextManager.getText("sm_ui_select_sanctuary"), 15, TextAnchor.UpperCenter);
            _detailTitle.color = new Color(0.4f, 0.7f, 1.0f);
            _detailTitle.fontStyle = FontStyle.Bold;
            var titleRect = _detailTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 28);
            titleRect.anchoredPosition = new Vector2(0, -8);

            // 详情滚动区
            var scrollGo = new GameObject("DetailScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8, 8);
            scrollRect.offsetMax = new Vector2(-8, -40);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.01f);
            scrollImg.raycastTarget = false;
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var scroll = scrollGo.AddComponent<ScrollRect>();

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRect = vpGo.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            _detailContent = contentGo.AddComponent<RectTransform>();
            _detailContent.anchorMin = new Vector2(0, 1);
            _detailContent.anchorMax = new Vector2(1, 1);
            _detailContent.pivot = new Vector2(0, 1);
            _detailContent.sizeDelta = new Vector2(0, 0);
            var contentHit = contentGo.AddComponent<Image>();
            contentHit.color = new Color(0, 0, 0, 0);
            contentHit.raycastTarget = true;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = _detailContent;
            scroll.horizontal = false;
            scroll.vertical = true;

            // 信息态内容（默认隐藏）
            var infoGo = new GameObject("InfoStateContent");
            infoGo.transform.SetParent(panelGo.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(8, 8);
            infoRect.offsetMax = new Vector2(-8, -40);
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0, 0, 0, 0.01f);
            infoImg.raycastTarget = false;
            var infoMask = infoGo.AddComponent<Mask>();
            infoMask.showMaskGraphic = true;
            var infoScroll = infoGo.AddComponent<ScrollRect>();

            var ivpGo = new GameObject("Viewport");
            ivpGo.transform.SetParent(infoGo.transform, false);
            var ivpRect = ivpGo.AddComponent<RectTransform>();
            ivpRect.anchorMin = Vector2.zero;
            ivpRect.anchorMax = Vector2.one;
            ivpRect.offsetMin = Vector2.zero;
            ivpRect.offsetMax = Vector2.zero;

            var icGo = new GameObject("Content");
            icGo.transform.SetParent(ivpGo.transform, false);
            _infoStateContent = icGo.AddComponent<RectTransform>();
            _infoStateContent.anchorMin = new Vector2(0, 1);
            _infoStateContent.anchorMax = new Vector2(1, 1);
            _infoStateContent.pivot = new Vector2(0, 1);
            _infoStateContent.sizeDelta = new Vector2(0, 0);
            var icHit = icGo.AddComponent<Image>();
            icHit.color = new Color(0, 0, 0, 0);
            icHit.raycastTarget = true;
            var icVlg = icGo.AddComponent<VerticalLayoutGroup>();
            icVlg.spacing = 4;
            icVlg.padding = new RectOffset(8, 8, 8, 8);
            icVlg.childControlWidth = true;
            icVlg.childControlHeight = true;
            icVlg.childForceExpandWidth = true;
            var icFitter = icGo.AddComponent<ContentSizeFitter>();
            icFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            infoScroll.viewport = ivpRect;
            infoScroll.content = _infoStateContent;
            infoScroll.horizontal = false;
            infoScroll.vertical = true;

            infoGo.SetActive(false);
        }

        private void BuildStatusBar()
        {
            var barGo = new GameObject("StatusBar");
            barGo.transform.SetParent(transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.sizeDelta = new Vector2(0, 50);
            barRect.anchoredPosition = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);

            _statusText = SMUiSkin.MakeText(barGo.transform, "", 11, TextAnchor.MiddleCenter);
            _statusText.supportRichText = true;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(16, 4);
            statusRect.offsetMax = new Vector2(-16, -4);
        }

        private void OnNodeClick(int index)
        {
            _selectedIndex = index;
            _showInfoState = false;
            RefreshAll();
        }

        private void ToggleInfoState()
        {
            _showInfoState = !_showInfoState;
            RefreshAll();
        }

        private void RefreshAll()
        {
            // 更新节点选中状态
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].SetSelected(i == _selectedIndex && !_showInfoState);
            }

            // 切换详情/信息态面板
            if (_detailContent != null)
                _detailContent.parent.parent.gameObject.SetActive(!_showInfoState);
            if (_infoStateContent != null)
                _infoStateContent.parent.parent.gameObject.SetActive(_showInfoState);

            if (_showInfoState)
            {
                _detailTitle.text = LocalizedTextManager.getText("sm_ui_san_info_library");
                RefreshInfoState();
            }
            else
            {
                RefreshDetail();
            }

            RefreshStatus();
        }

        private void RefreshDetail()
        {
            foreach (var go in _detailItems) if (go != null) Destroy(go);
            _detailItems.Clear();

            if (_selectedIndex < 0 || _selectedIndex >= SuperMechSanctuary.TotalSanctuaries)
            {
                _detailTitle.text = LocalizedTextManager.getText("sm_ui_select_sanctuary");
                return;
            }

            int i = _selectedIndex;
            string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[i]);
            string cls = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryClasses[i]);
            string typeName = SuperMechSanctuary.GetSanctuaryTypeName(i);
            int frags = SuperMechSanctuary.Data.sanctuary_fragments[i];
            bool unlocked = frags >= SuperMechSanctuary.FragmentsToUnlock;

            _detailTitle.text = name;

            AddDetailLine($"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_class")}:</color> {cls}", 12);
            AddDetailLine($"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_type")}:</color> {typeName}", 12);
            AddDetailLine($"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_frag_progress")}:</color> {frags}/{SuperMechSanctuary.FragmentsToUnlock} " +
                $"<color={(unlocked ? "#4f4" : "#f84")}>[{(unlocked ? LocalizedTextManager.getText("sm_ui_san_unlocked_tag") : LocalizedTextManager.getText("sm_ui_san_locked_tag"))}]</color>", 12);

            // 时间比例
            Actor selected = SelectedUnit.unit;
            if (selected != null && selected.isAlive())
            {
                string ratioText = SuperMechSanctuary.GetSanctuaryTimeRatioText(selected, i);
                AddDetailLine($"<color=#ffd700>{LocalizedTextManager.getText("sm_san_time_ratio_label")}:</color> {ratioText}", 12);
            }
            AddDetailLine("", 6);

            // 专属知识分支
            AddDetailLine($"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_knowledge")}</color>", 13);
            string[] branches = SuperMechSanctuary.GetSanctuaryKnowledgeBranches(i);
            foreach (string branch in branches)
            {
                AddDetailLine($"  • {branch}", 11);
            }
            AddDetailLine("", 6);

            // 钥匙说明
            AddDetailLine($"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_sanctuary_key_title")}</color>", 12);
            AddDetailLine($"<size=10><color=#aaa>{LocalizedTextManager.getText("sm_ui_sanctuary_key_desc")}</color></size>", 10);
            AddDetailLine("", 8);

            // 访问圣所按钮
            AddButton(LocalizedTextManager.getText(unlocked ? "sm_ui_visit_sanctuary" : "sm_ui_sanctuary_locked"),
                unlocked ? new Color(0.2f, 0.5f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.6f),
                unlocked, () =>
                {
                    Actor visitor = SelectedUnit.unit;
                    if (visitor == null || !visitor.isAlive())
                    {
                        Debug.LogWarning("[超神机械师] 访问圣所失败：请先选中一个存活单位");
                        return;
                    }
                    if (SuperMechSanctuary.VisitSanctuary(visitor, _selectedIndex))
                    {
                        RefreshAll();
                    }
                });

            AddDetailLine("", 6);

            // 复活按钮
            AddButton(LocalizedTextManager.getText("sm_ui_open_resurrection"),
                new Color(0.2f, 0.4f, 0.6f, 0.9f), true,
                () => SMWindowManager.OpenResurrection());
        }

        private void RefreshInfoState()
        {
            foreach (var go in _infoItems) if (go != null) Destroy(go);
            _infoItems.Clear();

            // 获取死亡单位记录（通过反射访问私有字段）
            var deadField = typeof(SuperMechSanctuary).GetField("_deadUnits",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (deadField == null)
            {
                AddInfoLine(LocalizedTextManager.getText("sm_ui_san_info_empty"), 12);
                return;
            }

            var deadList = deadField.GetValue(null) as System.Collections.IList;
            if (deadList == null || deadList.Count == 0)
            {
                AddInfoLine(LocalizedTextManager.getText("sm_ui_san_info_empty"), 12);
                return;
            }

            AddInfoLine($"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_info_records")}: {deadList.Count}</color>", 13);
            AddInfoLine("", 4);

            foreach (var record in deadList)
            {
                var recType = record.GetType();
                string recName = (string)recType.GetField("name")?.GetValue(record) ?? "?";
                int recStage = (int)recType.GetField("stage")?.GetValue(record);
                int recRank = (int)recType.GetField("rankIndex")?.GetValue(record);
                float recQi = (float)recType.GetField("qi")?.GetValue(record);
                int recRevive = (int)recType.GetField("reviveCount")?.GetValue(record);
                string rankName = SuperMechRanks.GetRankName(recRank);

                AddInfoLine($"<color=#ffd700>{recName}</color>  {rankName}  Lv{recStage}", 12);
                AddInfoLine($"<size=10><color=#aaa>{LocalizedTextManager.getText("sm_ui_san_info_qi")}: {recQi:F0}  {LocalizedTextManager.getText("sm_ui_san_info_revive")}: {recRevive}</color></size>", 10);
                AddInfoLine("", 4);
            }
        }

        private void RefreshStatus()
        {
            var data = SuperMechSanctuary.Data;
            int unlocked = 0;
            for (int i = 0; i < 6; i++)
                if (data.sanctuary_fragments[i] >= SuperMechSanctuary.FragmentsToUnlock) unlocked++;

            _statusText.text =
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_unlocked")}:</color> {unlocked}/6  " +
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_fragments")}:</color> {data.key_fragments}  " +
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_total_visits")}:</color> {data.total_visits}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_iteration")}:</color> {SuperMechCosmicIteration.CurrentIteration}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_divinity")}:</color> {data.total_divinity_ascensions}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_revive_count")}:</color> {data.total_resurrections}";
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
            _detailItems.Add(t.gameObject);
        }

        private void AddInfoLine(string text, int size)
        {
            var t = SMUiSkin.MakeText(_infoStateContent, text, size, TextAnchor.UpperLeft);
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0, 1);
            tr.sizeDelta = new Vector2(0, size + 6);
            _infoItems.Add(t.gameObject);
        }

        private void AddButton(string text, Color color, bool interactable, System.Action onClick)
        {
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(_detailContent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0, 1);
            btnRect.sizeDelta = new Vector2(0, 30);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = color;
            var btn = btnGo.AddComponent<Button>();
            btn.interactable = interactable;
            var btnText = SMUiSkin.MakeText(btnGo.transform, text, 12, TextAnchor.MiddleCenter);
            btnText.color = Color.white;
            btnText.fontStyle = FontStyle.Bold;
            var btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() => onClick?.Invoke());
            _detailItems.Add(btnGo);
        }
    }
}
