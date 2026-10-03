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
    public class SMSanctuaryView : MonoBehaviour
    {
        private static SMSanctuaryView _instance;
        public static bool IsOpen => _instance != null;

        private RectTransform _rootRect;
        private Text _statusText;

        // 两层视图
        private GameObject _selectLayer;    // 圣所选择层
        private GameObject _interiorLayer;  // 圣所内部层
        private int _currentSanctuary = -1; // 当前进入的圣所

        // 内部层光球
        private readonly List<GameObject> _lightOrbs = new();
        private RectTransform _orbContainer;
        private Text _interiorTitle;

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
            var infoText = SMUiSkin.MakeText(infoGo.transform,
                LocalizedTextManager.getText("sm_ui_san_info_library"), 13, TextAnchor.MiddleCenter);
            infoText.color = Color.white;
            var infoTextRect = infoText.GetComponent<RectTransform>();
            infoTextRect.anchorMin = Vector2.zero;
            infoTextRect.anchorMax = Vector2.one;
            infoTextRect.offsetMin = Vector2.zero;
            infoTextRect.offsetMax = Vector2.zero;
            infoBtn.onClick.AddListener(() => SMWindowManager.OpenResurrection());
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
            var hintText = SMUiSkin.MakeText(_selectLayer.transform,
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
                var nameText = SMUiSkin.MakeText(entryGo.transform,
                    $"<color={colors[i]}>{name}</color>", 15, TextAnchor.UpperCenter);
                nameText.fontStyle = FontStyle.Bold;
                var nameRect = nameText.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 1);
                nameRect.anchorMax = new Vector2(1, 1);
                nameRect.pivot = new Vector2(0.5f, 1);
                nameRect.sizeDelta = new Vector2(0, 25);
                nameRect.anchoredPosition = new Vector2(0, -10);

                // 知识方向
                var classText = SMUiSkin.MakeText(entryGo.transform, cls, 11, TextAnchor.UpperCenter);
                classText.color = new Color(0.8f, 0.8f, 0.8f);
                var classRect = classText.GetComponent<RectTransform>();
                classRect.anchorMin = new Vector2(0, 1);
                classRect.anchorMax = new Vector2(1, 1);
                classRect.pivot = new Vector2(0.5f, 1);
                classRect.sizeDelta = new Vector2(0, 20);
                classRect.anchoredPosition = new Vector2(0, -38);

                // 类型
                var typeText = SMUiSkin.MakeText(entryGo.transform, typeName, 10, TextAnchor.UpperCenter);
                typeText.color = new Color(0.6f, 0.6f, 0.6f);
                var typeRect = typeText.GetComponent<RectTransform>();
                typeRect.anchorMin = new Vector2(0, 1);
                typeRect.anchorMax = new Vector2(1, 1);
                typeRect.pivot = new Vector2(0.5f, 1);
                typeRect.sizeDelta = new Vector2(0, 18);
                typeRect.anchoredPosition = new Vector2(0, -58);

                // 进入按钮文字
                var enterText = SMUiSkin.MakeText(entryGo.transform,
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
            _interiorTitle = SMUiSkin.MakeText(_interiorLayer.transform, "", 16, TextAnchor.UpperCenter);
            _interiorTitle.color = new Color(0.2f, 0.3f, 0.5f);
            _interiorTitle.fontStyle = FontStyle.Bold;
            var titleRect = _interiorTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.anchoredPosition = new Vector2(0, -15);

            // 光球容器
            var orbContainerGo = new GameObject("OrbContainer");
            orbContainerGo.transform.SetParent(_interiorLayer.transform, false);
            _orbContainer = orbContainerGo.AddComponent<RectTransform>();
            _orbContainer.anchorMin = Vector2.zero;
            _orbContainer.anchorMax = Vector2.one;
            _orbContainer.offsetMin = new Vector2(20, 60);
            _orbContainer.offsetMax = new Vector2(-20, -20);

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
            var gateText = SMUiSkin.MakeText(gateGo.transform,
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

            // 生成12-20个随机大小的白色光球
            int orbCount = Random.Range(12, 20);
            for (int i = 0; i < orbCount; i++)
            {
                var orbGo = new GameObject($"LightOrb_{i}");
                orbGo.transform.SetParent(_orbContainer, false);
                var orbRect = orbGo.AddComponent<RectTransform>();
                orbRect.anchorMin = new Vector2(0.5f, 0.5f);
                orbRect.anchorMax = new Vector2(0.5f, 0.5f);
                orbRect.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(30, 70);
                orbRect.sizeDelta = new Vector2(size, size);
                // 随机位置
                float x = Random.Range(-0.4f, 0.4f) * _orbContainer.rect.width;
                float y = Random.Range(-0.35f, 0.35f) * _orbContainer.rect.height;
                orbRect.anchoredPosition = new Vector2(x, y);

                // 光球（白色半透明渐变效果用多层Image模拟）
                var orbImg = orbGo.AddComponent<Image>();
                orbImg.color = new Color(1f, 1f, 1f, Random.Range(0.3f, 0.7f));
                orbImg.raycastTarget = true;

                // 光晕
                var glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(orbGo.transform, false);
                var glowRect = glowGo.AddComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = new Vector2(-size * 0.3f, -size * 0.3f);
                glowRect.offsetMax = new Vector2(size * 0.3f, size * 0.3f);
                var glowImg = glowGo.AddComponent<Image>();
                glowImg.color = new Color(1f, 1f, 1f, 0.15f);
                glowImg.raycastTarget = false;

                // 点击光球读取信息
                var orbBtn = orbGo.AddComponent<Button>();
                orbBtn.targetGraphic = orbImg;
                int orbIdx = i;
                int sanIdx = sanctuaryIndex;
                orbBtn.onClick.AddListener(() => OnOrbClick(orbIdx, sanIdx));

                _lightOrbs.Add(orbGo);
            }
        }

        private void OnOrbClick(int orbIndex, int sanctuaryIndex)
        {
            // 触碰光球→获得该圣所领域的随机知识
            Actor selected = SelectedUnit.unit;
            if (selected != null && selected.isAlive())
            {
                // 给选中单位添加随机知识
                string[] branches = SuperMechSanctuary.GetSanctuaryKnowledgeBranches(sanctuaryIndex);
                if (branches.Length > 0)
                {
                    string branch = branches[Random.Range(0, branches.Length)];
                    // 记录日志
                    Debug.Log($"[超神机械师] {selected.name} 触碰圣所光球，获得{branch}相关知识");
                }
            }

            // 光球被触碰后变淡（模拟信息被读取）
            if (orbIndex < _lightOrbs.Count && _lightOrbs[orbIndex] != null)
            {
                var img = _lightOrbs[orbIndex].GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, 0.1f);
            }
        }

        private void ShowSelectLayer()
        {
            _currentSanctuary = -1;
            _selectLayer.SetActive(true);
            _interiorLayer.SetActive(false);
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

            _statusText = SMUiSkin.MakeText(barGo.transform, "", 11, TextAnchor.MiddleCenter);
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
    }
}
