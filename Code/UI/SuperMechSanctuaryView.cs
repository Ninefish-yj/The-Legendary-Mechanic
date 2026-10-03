using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>圣所独立空间视图（v0.39.0 原著还原版）
    /// 圣所不在地图上，是独立于宇宙的信息态空间。
    /// 两层结构：圣所选择层（6个入口）→ 圣所内部层（白茫茫空间+飘浮光球）
    /// 玩家是上帝视角，可直接查看所有圣所；游戏内单位需要超A级+钥匙才能进入
    /// </summary>
    public class SuperMechSanctuaryView : MonoBehaviour
    {
        private static SuperMechSanctuaryView _instance;
        public static bool IsOpen => _instance != null;

        private RectTransform _rootRect;
        private Text _statusText;

        // 两层视图
        private GameObject _selectLayer;    // 圣所选择层
        private GameObject _interiorLayer;  // 圣所内部层
        private GameObject _resurrectionLayer; // 复活面板层（内嵌信息态库）
        private int _currentSanctuary = -1; // 当前进入的圣所

        // 内部层光球
        private readonly List<GameObject> _lightOrbs = new();
        private readonly List<OrbInfo> _orbInfos = new();
        private RectTransform _orbContainer;
        private Text _interiorTitle;
        private GameObject _orbDetailPanel;
        private Text _orbDetailText;

        // 选择层圣所入口按钮
        private readonly List<Button> _sanctuaryButtons = new();

        public static void Toggle()
        {
            if (_instance != null) _instance.Close();
            else Open();
        }

        public static void Open()
        {
            if (_instance != null) return;
            var canvas = SuperMechUguiWindow.GetCanvas();
            var go = new GameObject("SMSanctuarySpaceView");
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _instance = go.AddComponent<SuperMechSanctuaryView>();
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

            // 深空背景（选择层用）
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.06f, 0.96f);
            bgImg.raycastTarget = true;

            // 顶部标题栏
            BuildTopBar();

            // 圣所选择层
            BuildSelectLayer();

            // 圣所内部层（默认隐藏）
            BuildInteriorLayer();

            // 复活面板层（默认隐藏，信息态库内嵌）
            BuildResurrectionLayer();

            // 底部状态栏
            BuildStatusBar();

            ShowSelectLayer();
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
            var backText = SuperMechUiSkin.MakeText(backGo.transform,
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
            var titleText = SuperMechUiSkin.MakeText(topGo.transform,
                LocalizedTextManager.getText("sm_ui_san_space_title"), 18, TextAnchor.MiddleCenter);
            titleText.color = new Color(0.4f, 0.7f, 1.0f);
            titleText.fontStyle = FontStyle.Bold;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(140, 0);
            titleRect.offsetMax = new Vector2(-140, 0);

            // 信息态库按钮
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
            var infoText = SuperMechUiSkin.MakeText(infoGo.transform,
                LocalizedTextManager.getText("sm_ui_san_info_library"), 13, TextAnchor.MiddleCenter);
            infoText.color = Color.white;
            var infoTextRect = infoText.GetComponent<RectTransform>();
            infoTextRect.anchorMin = Vector2.zero;
            infoTextRect.anchorMax = Vector2.one;
            infoTextRect.offsetMin = Vector2.zero;
            infoTextRect.offsetMax = Vector2.zero;
            infoBtn.onClick.AddListener(ShowResurrectionLayer);
        }

        private void BuildSelectLayer()
        {
            _selectLayer = new GameObject("SelectLayer");
            _selectLayer.transform.SetParent(transform, false);
            var rect = _selectLayer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 60);
            rect.offsetMax = new Vector2(0, -60);

            // 提示文字
            var hintText = SuperMechUiSkin.MakeText(_selectLayer.transform,
                LocalizedTextManager.getText("sm_ui_san_select_hint"), 14, TextAnchor.UpperCenter);
            hintText.color = new Color(0.6f, 0.8f, 1.0f);
            var hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 1);
            hintRect.anchorMax = new Vector2(1, 1);
            hintRect.pivot = new Vector2(0.5f, 1);
            hintRect.sizeDelta = new Vector2(0, 30);
            hintRect.anchoredPosition = new Vector2(0, -20);

            // 6个圣所入口（2行3列）
            string[] colors = { "#4db8ff", "#ff9933", "#4dff88", "#b366ff", "#ff66b3", "#33e6e6" };
            int[] positions = { -1, 0, 1, -1, 0, 1 }; // x位置
            int[] rows = { 0, 0, 0, 1, 1, 1 }; // y行

            for (int i = 0; i < SuperMechSanctuary.TotalSanctuaries; i++)
            {
                int idx = i;
                string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[i]);
                string cls = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryClasses[i]);
                string typeName = SuperMechSanctuary.GetSanctuaryTypeName(i);

                var entryGo = new GameObject($"SanctuaryEntry_{i}");
                entryGo.transform.SetParent(_selectLayer.transform, false);
                var entryRect = entryGo.AddComponent<RectTransform>();
                entryRect.anchorMin = new Vector2(0.5f, 0.5f);
                entryRect.anchorMax = new Vector2(0.5f, 0.5f);
                entryRect.pivot = new Vector2(0.5f, 0.5f);
                entryRect.sizeDelta = new Vector2(220, 130);
                float x = positions[i] * 250;
                float y = (rows[i] == 0 ? 60 : -100);
                entryRect.anchoredPosition = new Vector2(x, y);

                // 背景
                var entryBg = entryGo.AddComponent<Image>();
                entryBg.color = new Color(0.08f, 0.1f, 0.18f, 0.9f);
                entryBg.raycastTarget = true;

                // 边框（用稍大的背景模拟）
                var borderGo = new GameObject("Border");
                borderGo.transform.SetParent(entryGo.transform, false);
                borderGo.transform.SetAsFirstSibling();
                var borderRect = borderGo.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = new Vector2(-2, -2);
                borderRect.offsetMax = new Vector2(2, 2);
                var borderImg = borderGo.AddComponent<Image>();
                Color borderColor = ParseColor(colors[i]);
                borderImg.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.6f);
                borderImg.raycastTarget = false;

                // 按钮
                var btn = entryGo.AddComponent<Button>();
                btn.targetGraphic = entryBg;
                int sanctuaryIdx = idx;
                btn.onClick.AddListener(() => EnterSanctuary(sanctuaryIdx));
                _sanctuaryButtons.Add(btn);

                // 圣所名称
                var nameText = SuperMechUiSkin.MakeText(entryGo.transform,
                    $"<color={colors[i]}>{name}</color>", 15, TextAnchor.UpperCenter);
                nameText.fontStyle = FontStyle.Bold;
                var nameRect = nameText.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 1);
                nameRect.anchorMax = new Vector2(1, 1);
                nameRect.pivot = new Vector2(0.5f, 1);
                nameRect.sizeDelta = new Vector2(0, 25);
                nameRect.anchoredPosition = new Vector2(0, -10);

                // 知识方向
                var classText = SuperMechUiSkin.MakeText(entryGo.transform, cls, 11, TextAnchor.UpperCenter);
                classText.color = new Color(0.8f, 0.8f, 0.8f);
                var classRect = classText.GetComponent<RectTransform>();
                classRect.anchorMin = new Vector2(0, 1);
                classRect.anchorMax = new Vector2(1, 1);
                classRect.pivot = new Vector2(0.5f, 1);
                classRect.sizeDelta = new Vector2(0, 20);
                classRect.anchoredPosition = new Vector2(0, -38);

                // 类型
                var typeText = SuperMechUiSkin.MakeText(entryGo.transform, typeName, 10, TextAnchor.UpperCenter);
                typeText.color = new Color(0.6f, 0.6f, 0.6f);
                var typeRect = typeText.GetComponent<RectTransform>();
                typeRect.anchorMin = new Vector2(0, 1);
                typeRect.anchorMax = new Vector2(1, 1);
                typeRect.pivot = new Vector2(0.5f, 1);
                typeRect.sizeDelta = new Vector2(0, 18);
                typeRect.anchoredPosition = new Vector2(0, -58);

                // 进入按钮文字
                var enterText = SuperMechUiSkin.MakeText(entryGo.transform,
                    LocalizedTextManager.getText("sm_ui_san_enter"), 12, TextAnchor.LowerCenter);
                enterText.color = new Color(0.4f, 0.7f, 1.0f);
                enterText.fontStyle = FontStyle.Bold;
                var enterRect = enterText.GetComponent<RectTransform>();
                enterRect.anchorMin = new Vector2(0, 0);
                enterRect.anchorMax = new Vector2(1, 0);
                enterRect.pivot = new Vector2(0.5f, 0);
                enterRect.sizeDelta = new Vector2(0, 25);
                enterRect.anchoredPosition = new Vector2(0, 8);
            }

            // v0.56.0 留言板按钮（原著经典留言）
            var msgBtnGo = new GameObject("MessageBoardBtn");
            msgBtnGo.transform.SetParent(_selectLayer.transform, false);
            var msgBtnRect = msgBtnGo.AddComponent<RectTransform>();
            msgBtnRect.anchorMin = new Vector2(0.5f, 0);
            msgBtnRect.anchorMax = new Vector2(0.5f, 0);
            msgBtnRect.pivot = new Vector2(0.5f, 0);
            msgBtnRect.sizeDelta = new Vector2(140, 36);
            msgBtnRect.anchoredPosition = new Vector2(-80, 50);
            var msgBtnBg = msgBtnGo.AddComponent<Image>();
            msgBtnBg.color = new Color(0.15f, 0.1f, 0.25f, 0.9f);
            var msgBtn = msgBtnGo.AddComponent<Button>();
            msgBtn.targetGraphic = msgBtnBg;
            msgBtn.onClick.AddListener(ShowMessageBoard);
            var msgBtnText = SuperMechUiSkin.MakeText(msgBtnGo.transform, "留言板", 13, TextAnchor.MiddleCenter);
            msgBtnText.color = new Color(0.8f, 0.6f, 1f);
            msgBtnText.fontStyle = FontStyle.Bold;

            // v0.56.0 文明名录按钮（原著文明记录）
            var civBtnGo = new GameObject("CivListBtn");
            civBtnGo.transform.SetParent(_selectLayer.transform, false);
            var civBtnRect = civBtnGo.AddComponent<RectTransform>();
            civBtnRect.anchorMin = new Vector2(0.5f, 0);
            civBtnRect.anchorMax = new Vector2(0.5f, 0);
            civBtnRect.pivot = new Vector2(0.5f, 0);
            civBtnRect.sizeDelta = new Vector2(140, 36);
            civBtnRect.anchoredPosition = new Vector2(80, 50);
            var civBtnBg = civBtnGo.AddComponent<Image>();
            civBtnBg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            var civBtn = civBtnGo.AddComponent<Button>();
            civBtn.targetGraphic = civBtnBg;
            civBtn.onClick.AddListener(ShowCivRegistry);
            var civBtnText = SuperMechUiSkin.MakeText(civBtnGo.transform, "文明名录", 13, TextAnchor.MiddleCenter);
            civBtnText.color = new Color(0.6f, 0.8f, 1f);
            civBtnText.fontStyle = FontStyle.Bold;
        }

        private void BuildInteriorLayer()
        {
            _interiorLayer = new GameObject("InteriorLayer");
            _interiorLayer.transform.SetParent(transform, false);
            var rect = _interiorLayer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 60);
            rect.offsetMax = new Vector2(0, -60);

            // 白茫茫背景（原著：无边无际的白茫茫世界）
            var interiorBg = _interiorLayer.AddComponent<Image>();
            interiorBg.color = new Color(0.9f, 0.92f, 0.95f, 0.97f);
            interiorBg.raycastTarget = true;

            // 标题
            _interiorTitle = SuperMechUiSkin.MakeText(_interiorLayer.transform, "", 16, TextAnchor.UpperCenter);
            _interiorTitle.color = new Color(0.2f, 0.3f, 0.5f);
            _interiorTitle.fontStyle = FontStyle.Bold;
            var titleRect = _interiorTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.anchoredPosition = new Vector2(0, -15);

            // 光球容器（左侧70%区域）
            var orbContainerGo = new GameObject("OrbContainer");
            orbContainerGo.transform.SetParent(_interiorLayer.transform, false);
            _orbContainer = orbContainerGo.AddComponent<RectTransform>();
            _orbContainer.anchorMin = Vector2.zero;
            _orbContainer.anchorMax = new Vector2(0.7f, 1);
            _orbContainer.offsetMin = new Vector2(20, 60);
            _orbContainer.offsetMax = new Vector2(-10, -20);

            // 光球详情面板（右侧30%区域）
            _orbDetailPanel = new GameObject("OrbDetailPanel");
            _orbDetailPanel.transform.SetParent(_interiorLayer.transform, false);
            var detailRect = _orbDetailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.7f, 0);
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(10, 60);
            detailRect.offsetMax = new Vector2(-20, -20);
            var detailBg = _orbDetailPanel.AddComponent<Image>();
            detailBg.color = new Color(0.95f, 0.96f, 0.98f, 0.9f);
            detailBg.raycastTarget = true;

            // 详情面板边框
            var detailBorderGo = new GameObject("Border");
            detailBorderGo.transform.SetParent(_orbDetailPanel.transform, false);
            detailBorderGo.transform.SetAsFirstSibling();
            var detailBorderRect = detailBorderGo.AddComponent<RectTransform>();
            detailBorderRect.anchorMin = Vector2.zero;
            detailBorderRect.anchorMax = Vector2.one;
            detailBorderRect.offsetMin = new Vector2(-1, -1);
            detailBorderRect.offsetMax = new Vector2(1, 1);
            var detailBorderImg = detailBorderGo.AddComponent<Image>();
            detailBorderImg.color = new Color(0.3f, 0.4f, 0.6f, 0.5f);
            detailBorderImg.raycastTarget = false;

            // 详情文本
            _orbDetailText = SuperMechUiSkin.MakeText(_orbDetailPanel.transform,
                LocalizedTextManager.getText("sm_ui_san_orb_hint"), 12, TextAnchor.UpperLeft);
            _orbDetailText.supportRichText = true;
            _orbDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _orbDetailText.verticalOverflow = VerticalWrapMode.Truncate;
            _orbDetailText.color = new Color(0.2f, 0.25f, 0.35f);
            var detailTextRect = _orbDetailText.GetComponent<RectTransform>();
            detailTextRect.anchorMin = Vector2.zero;
            detailTextRect.anchorMax = Vector2.one;
            detailTextRect.offsetMin = new Vector2(12, 12);
            detailTextRect.offsetMax = new Vector2(-12, -12);

            // 返回选择层按钮（光门通道）
            var gateGo = new GameObject("GateBtn");
            gateGo.transform.SetParent(_interiorLayer.transform, false);
            var gateRect = gateGo.AddComponent<RectTransform>();
            gateRect.anchorMin = new Vector2(1, 0);
            gateRect.anchorMax = new Vector2(1, 0);
            gateRect.pivot = new Vector2(1, 0);
            gateRect.sizeDelta = new Vector2(160, 40);
            gateRect.anchoredPosition = new Vector2(-20, 15);
            var gateImg = gateGo.AddComponent<Image>();
            gateImg.color = new Color(0.3f, 0.4f, 0.6f, 0.9f);
            var gateBtn = gateGo.AddComponent<Button>();
            var gateText = SuperMechUiSkin.MakeText(gateGo.transform,
                LocalizedTextManager.getText("sm_ui_san_gate_return"), 13, TextAnchor.MiddleCenter);
            gateText.color = Color.white;
            gateText.fontStyle = FontStyle.Bold;
            var gateTextRect = gateText.GetComponent<RectTransform>();
            gateTextRect.anchorMin = Vector2.zero;
            gateTextRect.anchorMax = Vector2.one;
            gateTextRect.offsetMin = Vector2.zero;
            gateTextRect.offsetMax = Vector2.zero;
            gateBtn.onClick.AddListener(ShowSelectLayer);

            _interiorLayer.SetActive(false);
        }

        private void EnterSanctuary(int index)
        {
            _currentSanctuary = index;
            string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[index]);
            _interiorTitle.text = name;

            // 生成光球（模拟各迭代文明的信息态集合体）
            GenerateLightOrbs(index);

            _selectLayer.SetActive(false);
            _interiorLayer.SetActive(true);
        }

        private void GenerateLightOrbs(int sanctuaryIndex)
        {
            // 清除旧光球
            foreach (var orb in _lightOrbs) if (orb != null) Destroy(orb);
            _lightOrbs.Clear();
            _orbInfos.Clear();

            // 基于真实迭代历史生成光球——迭代几轮就有几个记录
            var history = SuperMechCivilizationData.GetHistory();
            int orbCount = Mathf.Max(history.Count, 1);

            for (int i = 0; i < orbCount; i++)
            {
                var snap = (i < history.Count) ? history[i] : SuperMechCivilizationData.GetCurrent();
                int iter = (snap != null) ? snap.iteration : SuperMechCosmicIteration.CurrentIteration;
                bool isCurrent = (i >= history.Count);

                // 文明名称：真实记录用著名单位或迭代编号
                string civName;
                if (snap != null && snap.notableUnits != null && snap.notableUnits.Count > 0)
                    civName = snap.notableUnits[0] + LocalizedTextManager.getText("sm_san_civ_era");
                else if (snap != null)
                    civName = string.Format(LocalizedTextManager.getText("sm_san_civ_iteration"), iter);
                else
                    civName = LocalizedTextManager.getText("sm_san_civ_current");

                // 成就：基于真实数据
                string achievement;
                if (snap != null)
                {
                    string rankName = (snap.maxRankReached >= 0 && snap.maxRankReached < SuperMechRanks.All.Count)
                        ? LocalizedTextManager.getText(SuperMechRanks.All[snap.maxRankReached].name)
                        : LocalizedTextManager.getText("sm_civ_none");
                    achievement = string.Format(LocalizedTextManager.getText("sm_san_civ_achievement"),
                        snap.totalAwakened, rankName, snap.totalKnowledgeUnlocked);
                }
                else
                {
                    achievement = LocalizedTextManager.getText("sm_san_civ_ongoing");
                }

                string destruction = isCurrent
                    ? LocalizedTextManager.getText("sm_san_civ_ongoing")
                    : LocalizedTextManager.getText("sm_san_civ_reset");

                var info = new OrbInfo
                {
                    civilizationName = civName,
                    domain = SuperMechSanctuary.GetSanctuaryTypeName(sanctuaryIndex),
                    iteration = iter,
                    achievement = achievement,
                    destructionCause = destruction,
                    snapshot = snap
                };
                _orbInfos.Add(info);

                var orbGo = new GameObject($"LightOrb_{i}");
                orbGo.transform.SetParent(_orbContainer, false);
                var orbRect = orbGo.AddComponent<RectTransform>();
                orbRect.anchorMin = new Vector2(0.5f, 0.5f);
                orbRect.anchorMax = new Vector2(0.5f, 0.5f);
                orbRect.pivot = new Vector2(0.5f, 0.5f);
                // 越古老的迭代光球越小越暗，越近的越大越亮
                float ageFactor = 1f - (float)i / Mathf.Max(1, orbCount);
                float size = 30f + ageFactor * 40f;
                orbRect.sizeDelta = new Vector2(size, size);
                // 环形分布避免重叠
                float angle = (i / (float)orbCount) * Mathf.PI * 2f;
                float radius = 0.15f + ageFactor * 0.25f;
                float x = Mathf.Cos(angle) * radius * _orbContainer.rect.width;
                float y = Mathf.Sin(angle) * radius * _orbContainer.rect.height;
                orbRect.anchoredPosition = new Vector2(x, y);

                var orbImg = orbGo.AddComponent<Image>();
                orbImg.color = new Color(1f, 1f, 1f, 0.3f + ageFactor * 0.4f);
                orbImg.raycastTarget = true;

                var glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(orbGo.transform, false);
                var glowRect = glowGo.AddComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = new Vector2(-size * 0.3f, -size * 0.3f);
                glowRect.offsetMax = new Vector2(size * 0.3f, size * 0.3f);
                var glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(1f, 1f, 1f, 0.1f + ageFactor * 0.15f);
                glowImg.raycastTarget = false;

                var orbBtn = orbGo.AddComponent<Button>();
                orbBtn.targetGraphic = orbImg;
                int orbIdx = i;
                orbBtn.onClick.AddListener(() => OnOrbClick(orbIdx));

                _lightOrbs.Add(orbGo);
            }
        }

        private void OnOrbClick(int orbIndex)
        {
            // 玩家观察模式：点击光球查看该文明的信息态记录
            if (orbIndex >= _orbInfos.Count) return;
            var info = _orbInfos[orbIndex];

            // 显示详情面板
            if (_orbDetailPanel != null) _orbDetailPanel.SetActive(true);
            _orbDetailText.text =
                $"【文明记录】\n\n" +
                $"<color=#3366cc>名称：</color>{info.civilizationName}\n" +
                $"<color=#3366cc>领域：</color>{info.domain}\n" +
                $"<color=#3366cc>迭代：</color>第{info.iteration}轮\n" +
                $"<color=#3366cc>成就：</color>{info.achievement}\n" +
                $"<color=#cc3333>毁灭：</color>{info.destructionCause}\n\n" +
                $"<color=#888><size=10>信息态记录 · 触碰光球可读取</size></color>";

            // 光球被查看后变淡（模拟信息已读取）
            if (orbIndex < _lightOrbs.Count && _lightOrbs[orbIndex] != null)
            {
                var img = _lightOrbs[orbIndex].GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, 0.15f);
            }
        }

        private void ShowSelectLayer()
        {
            _currentSanctuary = -1;
            _selectLayer.SetActive(true);
            _interiorLayer.SetActive(false);
            if (_resurrectionLayer != null) _resurrectionLayer.SetActive(false);
            RefreshStatus();
        }

        private void BuildResurrectionLayer()
        {
            _resurrectionLayer = new GameObject("ResurrectionLayer");
            _resurrectionLayer.transform.SetParent(transform, false);
            var rect = _resurrectionLayer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 50);
            rect.offsetMax = new Vector2(0, -50);

            // 半透明背景
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(_resurrectionLayer.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.03f, 0.08f, 0.95f);
            bgImg.raycastTarget = true;

            // 标题
            var titleText = SuperMechUiSkin.MakeText(_resurrectionLayer.transform,
                LocalizedTextManager.getText("sm_ui_san_info_library"), 16, TextAnchor.UpperCenter);
            titleText.color = new Color(0.6f, 0.8f, 1.0f);
            titleText.fontStyle = FontStyle.Bold;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.anchoredPosition = new Vector2(0, -10);

            // 返回按钮
            var backGo = new GameObject("BackBtn");
            backGo.transform.SetParent(_resurrectionLayer.transform, false);
            var backRect = backGo.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.sizeDelta = new Vector2(100, 30);
            backRect.anchoredPosition = new Vector2(12, -8);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);
            var backBtn = backGo.AddComponent<Button>();
            var backText = SuperMechUiSkin.MakeText(backGo.transform,
                LocalizedTextManager.getText("sm_ui_san_back"), 12, TextAnchor.MiddleCenter);
            backText.color = Color.white;
            var backTextRect = backText.GetComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;
            backBtn.onClick.AddListener(ShowSelectLayer);

            // 内嵌复活列表
            var listGo = new GameObject("ResurrectionList");
            listGo.transform.SetParent(_resurrectionLayer.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(20, 20);
            listRect.offsetMax = new Vector2(-20, -50);
            listGo.AddComponent<SuperMechResurrectionView>();

            _resurrectionLayer.SetActive(false);
        }

        private void ShowResurrectionLayer()
        {
            _selectLayer.SetActive(false);
            _interiorLayer.SetActive(false);
            _resurrectionLayer.SetActive(true);
            RefreshStatus();
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

            _statusText = SuperMechUiSkin.MakeText(barGo.transform, "", 11, TextAnchor.MiddleCenter);
            _statusText.supportRichText = true;
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(16, 4);
            statusRect.offsetMax = new Vector2(-16, -4);

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            var data = SuperMechSanctuary.Data;
            int unlocked = 0;
            for (int i = 0; i < SuperMechSanctuary.TotalSanctuaries; i++)
            {
                if (data.sanctuary_fragments[i] >= SuperMechSanctuary.FragmentsToUnlock) unlocked++;
            }

            _statusText.text =
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_unlocked")}:</color> {unlocked}/6  " +
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_fragments")}:</color> {data.key_fragments}  " +
                $"<color=#88cc88>{LocalizedTextManager.getText("sm_ui_san_materials")}:</color> {data.key_materials}/{SuperMechConfig.KeyMaterialsPerKey}  " +
                $"<color=#6ab7ff>{LocalizedTextManager.getText("sm_ui_san_total_visits")}:</color> {data.total_visits}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_iteration")}:</color> {SuperMechCosmicIteration.CurrentIteration}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_divinity")}:</color> {data.total_divinity_ascensions}  " +
                $"<color=#ffd700>{LocalizedTextManager.getText("sm_ui_san_revive_count")}:</color> {data.total_resurrections}";
        }

        private static Color ParseColor(string hex)
        {
            Color c = new Color(1, 1, 1);
            if (hex.StartsWith("#") && hex.Length >= 7)
            {
                float r = System.Convert.ToInt32(hex.Substring(1, 2), 16) / 255f;
                float g = System.Convert.ToInt32(hex.Substring(3, 2), 16) / 255f;
                float b = System.Convert.ToInt32(hex.Substring(5, 2), 16) / 255f;
                c = new Color(r, g, b);
            }
            return c;
        }

        /// <summary>v0.56.0 显示留言板（原著经典留言）</summary>
        private void ShowMessageBoard()
        {
            var panel = CreateInfoPanel("留言板", new Color(0.3f, 0.2f, 0.5f, 0.95f));
            var text = panel.transform.Find("ContentText").GetComponent<Text>();
            text.text = "【原著经典留言】\n\n" +
                "1.「对于高级文明来说，超A级只是争斗工具，文明本身才是宇宙的主角」\n\n" +
                "2.「机械师总有提高实力的途径」\n\n" +
                "3.「魔法师对于各类法术的渴望，就像机械师对科技的好奇一样」\n\n" +
                "4.「异能者向来是奇迹的创造者，多少特殊技术都是异能者带来的」\n\n" +
                "5.「进化方块是进化者文明举族之力打造的技术核心」\n\n" +
                "6.「时空剪切技术将会是一个研究时空理论的新方向，能够衍生出极强的应用技术」\n\n" +
                "7.「黑星能活很久，我们没有那么多时间等待，原始异能体是开启第三圣所的钥匙」\n\n" +
                "<color=#888><size=10>—— 信息态空间残留的历史留言</size></color>";
        }

        /// <summary>v0.56.0 显示文明名录（原著出现过的文明）</summary>
        private void ShowCivRegistry()
        {
            var panel = CreateInfoPanel("文明名录", new Color(0.2f, 0.3f, 0.5f, 0.95f));
            var text = panel.transform.Find("ContentText").GetComponent<Text>();
            text.text = "【原著文明名录】\n\n" +
                "<color=#ffd700>【宇宙级文明】</color>\n" +
                "• 赤色帝国 — 三大文明之一，掌控帝国科学院，研究时空剪切技术\n" +
                "• 光辉联邦 — 三大文明之一，拥有高维天启传送器等战略级技术\n" +
                "• 虚灵教派 — 三大文明之一，神秘主义文明\n\n" +
                "<color=#87ceeb>【超星团级文明】</color>\n" +
                "• 摩多文明 — 超星团级，昆德族事件幕后策划者\n" +
                "• 银影文明 — 变异宇宙宝物文明，防御力极强\n\n" +
                "<color=#90ee90>【星团级/星际文明】</color>\n" +
                "• 机械文明 — 韩萧帮助发展的机械生命文明\n" +
                "• 昆德族 — 土著文明，拥有时空剪切技术\n\n" +
                "<color=#ff6b6b>【已灭亡文明】</color>\n" +
                "• 进化者文明 — 举族之力打造进化方块，已灭亡\n\n" +
                "<color=#888><size=10>—— 信息态空间记录的已知文明</size></color>";
        }

        /// <summary>创建信息面板（留言板/文明名录共用）</summary>
        private GameObject CreateInfoPanel(string title, Color bgColor)
        {
            var panel = new GameObject($"InfoPanel_{title}");
            panel.transform.SetParent(transform, false);
            panel.transform.SetAsLastSibling();
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520, 480);
            var bg = panel.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = true;

            // 标题
            var titleText = SuperMechUiSkin.MakeText(panel.transform, title, 18, TextAnchor.UpperCenter);
            titleText.color = Color.white;
            titleText.fontStyle = FontStyle.Bold;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -10);

            // 内容文本
            var contentGo = new GameObject("ContentText");
            contentGo.transform.SetParent(panel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20, 50);
            contentRect.offsetMax = new Vector2(-20, -50);
            var contentText = contentGo.AddComponent<Text>();
            contentText.font = titleText.font;
            contentText.fontSize = 13;
            contentText.color = new Color(0.9f, 0.9f, 0.9f);
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Truncate;

            // 关闭按钮
            var closeBtnGo = new GameObject("CloseBtn");
            closeBtnGo.transform.SetParent(panel.transform, false);
            var closeRect = closeBtnGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(100, 32);
            closeRect.anchoredPosition = new Vector2(0, 12);
            var closeBg = closeBtnGo.AddComponent<Image>();
            closeBg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            var closeBtn = closeBtnGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(() => Destroy(panel));
            var closeText = SuperMechUiSkin.MakeText(closeBtnGo.transform, "关闭", 13, TextAnchor.MiddleCenter);
            closeText.color = Color.white;

            return panel;
        }

        /// <summary>光球记录的文明信息</summary>
        private class OrbInfo
        {
            public string civilizationName;
            public string domain;
            public int iteration;
            public string achievement;
            public string destructionCause;
            public SuperMechCivilizationData.CivilizationSnapshot snapshot; // 真实迭代快照
        }
    }
}
