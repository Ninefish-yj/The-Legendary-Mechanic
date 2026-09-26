using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 3D知识图谱：参考原版NeuronsOverview实现
    /// 球面分布知识节点 + 轴突连接线 + 神经冲动流动 + 拖拽旋转 + 节点发光
    /// </summary>
    public class SMKnowledgeGraph3D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // 球面分布参数
        private const float Radius = 90f;
        private const float NodeScaleMin = 0.7f;
        private const float NodeScaleMax = 1.3f;
        private const float DragSpeed = 0.3f;

        // 颜色
        private static readonly Color ColorLocked = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        private static readonly Color ColorUnlockable = new Color(0.3f, 0.5f, 0.9f, 0.9f);
        private static readonly Color ColorUnlocked = new Color(0.9f, 0.75f, 0.2f, 1f);
        private static readonly Color ColorAxonDefault = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color ColorAxonActive = new Color(0.3f, 0.8f, 1f, 0.4f);
        private static readonly Color ColorImpulse = new Color(0.5f, 0.9f, 1f, 1f);

        // 节点数据
        public class KnowledgeNode
        {
            public string id;
            public string name;
            public string icon;
            public int tier; // 0=基础,1=进阶,2=高端,3=尖端,4=终极
            public bool unlocked;
            public bool unlockable;
            public int cost;
            public Vector3 spherePos;
            public GameObject gameObject;
            public Image image;
            public Button button;
        }

        // 轴突数据
        private class Axon
        {
            public KnowledgeNode from;
            public KnowledgeNode to;
            public GameObject lineObj;
            public Image lineImage;
            public bool active;
        }

        // 神经冲动
        private class NerveImpulse
        {
            public Axon axon;
            public GameObject obj;
            public Image image;
            public float progress;
            public float speed;
        }

        private List<KnowledgeNode> _nodes = new List<KnowledgeNode>();
        private List<Axon> _axons = new List<Axon>();
        private List<NerveImpulse> _impulses = new List<NerveImpulse>();

        private GameObject _graphContainer; // 旋转的容器
        private GameObject _nodesParent;
        private GameObject _axonsParent;
        private GameObject _impulsesParent;

        private bool _isDragging;
        private Vector2 _lastDragPos;
        private float _rotationY;
        private float _rotationX;
        private float _targetRotationY;
        private float _targetRotationX;

        private Actor _actor;
        private string _prefix;

        /// <summary>初始化3D图谱。</summary>
        public void Init(Actor actor, string prefix, Transform parent)
        {
            _actor = actor;
            _prefix = prefix;

            // 清理旧的
            Clear();

            // 创建容器
            _graphContainer = new GameObject("Graph3D", typeof(RectTransform));
            _graphContainer.transform.SetParent(parent, false);
            RectTransform grt = _graphContainer.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.localPosition = Vector3.zero;
            grt.sizeDelta = new Vector2(parent.GetComponent<RectTransform>().rect.width, parent.GetComponent<RectTransform>().rect.height);

            // 背景层（按系别主题色渐变+光点装饰）
            CreateBackground(_graphContainer.transform);

            // 三层父对象：轴突在最底层，节点在中间，冲动在最上层
            _axonsParent = new GameObject("Axons", typeof(RectTransform));
            _axonsParent.transform.SetParent(_graphContainer.transform, false);
            _nodesParent = new GameObject("Nodes", typeof(RectTransform));
            _nodesParent.transform.SetParent(_graphContainer.transform, false);
            _impulsesParent = new GameObject("Impulses", typeof(RectTransform));
            _impulsesParent.transform.SetParent(_graphContainer.transform, false);

            foreach (Transform t in new[] { _axonsParent.transform, _nodesParent.transform, _impulsesParent.transform })
            {
                RectTransform rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localPosition = Vector3.zero;
                rt.sizeDelta = _graphContainer.GetComponent<RectTransform>().sizeDelta;
            }

            // 生成节点
            GenerateNodes();
            // 生成轴突
            GenerateAxons();
            // 初始旋转
            _rotationY = 30f;
            _rotationX = 15f;
            _targetRotationY = _rotationY;
            _targetRotationX = _rotationX;
            // 立即更新节点位置（否则所有节点重叠在中心看不见）
            UpdateNodes();
            UpdateAxons();
            UpdateGraphTransform();
        }

        /// <summary>创建背景层（星云+天体+轨道环+光点装饰）。</summary>
        private void CreateBackground(Transform parent)
        {
            // 系别主题色
            Color themeColor = GetThemeColor(_prefix);
            System.Random rng = new System.Random(_prefix.GetHashCode() + 42);

            // 深空背景
            GameObject bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(parent, false);
            bgGo.transform.SetAsFirstSibling();
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.01f, 0.015f, 0.04f, 0.97f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // === 多层星云 ===
            // 星云1：主题色，左上
            CreateNebula(bgGo.transform, new Vector2(-120, 60), 220, themeColor, 0.08f);
            // 星云2：互补色，右下
            Color complement = new Color(1f - themeColor.r, 1f - themeColor.g, 1f - themeColor.b);
            CreateNebula(bgGo.transform, new Vector2(100, -50), 180, complement, 0.06f);
            // 星云3：白色，中心
            CreateNebula(bgGo.transform, new Vector2(0, 0), 260, Color.white, 0.04f);

            // === 轨道环（同心圆，模拟星系）===
            for (int i = 0; i < 3; i++)
            {
                GameObject ringGo = new GameObject("OrbitRing_" + i, typeof(RectTransform));
                ringGo.transform.SetParent(bgGo.transform, false);
                Image ringImg = ringGo.AddComponent<Image>();
                Color ringColor = themeColor;
                ringColor.a = 0.06f + i * 0.02f;
                ringImg.color = ringColor;
                ringImg.raycastTarget = false;
                RectTransform ringRt = ringGo.GetComponent<RectTransform>();
                ringRt.anchorMin = new Vector2(0.5f, 0.5f);
                ringRt.anchorMax = new Vector2(0.5f, 0.5f);
                ringRt.pivot = new Vector2(0.5f, 0.5f);
                float ringSize = 120f + i * 60f;
                ringRt.sizeDelta = new Vector2(ringSize, ringSize);
                // 用大尺寸+小alpha模拟圆环（实际是实心圆，靠中心光晕覆盖中心部分）
            }

            // === 天体装饰 ===
            // 主天体（大发光球，右上角）
            CreateCelestialBody(bgGo.transform, new Vector2(140, 70), 36, themeColor, 0.25f, true);
            // 副天体（小球，左下角）
            CreateCelestialBody(bgGo.transform, new Vector2(-130, -60), 20, complement, 0.2f, false);
            // 微型天体（随机位置）
            for (int i = 0; i < 3; i++)
            {
                float x = (float)rng.NextDouble() * 300f - 150f;
                float y = (float)rng.NextDouble() * 140f - 70f;
                float size = 6f + (float)rng.NextDouble() * 8f;
                Color c = (rng.Next(0, 2) == 0) ? themeColor : Color.white;
                CreateCelestialBody(bgGo.transform, new Vector2(x, y), size, c, 0.15f, false);
            }

            // === 光点装饰（模拟星空，不同大小和颜色）===
            int starCount = 80;
            for (int i = 0; i < starCount; i++)
            {
                GameObject star = new GameObject("Star_" + i, typeof(RectTransform));
                star.transform.SetParent(bgGo.transform, false);
                Image starImg = star.AddComponent<Image>();
                // 70%白色，20%主题色，10%互补色
                int colorRoll = rng.Next(0, 10);
                Color starColor;
                if (colorRoll < 7) starColor = Color.white;
                else if (colorRoll < 9) starColor = themeColor;
                else starColor = complement;
                starColor.a = 0.15f + (float)rng.NextDouble() * 0.5f;
                starImg.color = starColor;
                starImg.raycastTarget = false;
                RectTransform starRt = star.GetComponent<RectTransform>();
                starRt.anchorMin = new Vector2(0f, 0f);
                starRt.anchorMax = new Vector2(0f, 0f);
                starRt.pivot = new Vector2(0.5f, 0.5f);
                float x = (float)rng.NextDouble() * 420f - 210f;
                float y = (float)rng.NextDouble() * 220f - 110f;
                starRt.anchoredPosition = new Vector2(x, y);
                float size = 0.8f + (float)rng.NextDouble() * 2.5f;
                starRt.sizeDelta = new Vector2(size, size);
            }

            // === 流星/彗星装饰（2-3条斜线）===
            for (int i = 0; i < 2; i++)
            {
                GameObject meteorGo = new GameObject("Meteor_" + i, typeof(RectTransform));
                meteorGo.transform.SetParent(bgGo.transform, false);
                Image meteorImg = meteorGo.AddComponent<Image>();
                Color meteorColor = themeColor;
                meteorColor.a = 0.12f;
                meteorImg.color = meteorColor;
                meteorImg.raycastTarget = false;
                RectTransform meteorRt = meteorGo.GetComponent<RectTransform>();
                meteorRt.anchorMin = new Vector2(0f, 0f);
                meteorRt.anchorMax = new Vector2(0f, 0f);
                meteorRt.pivot = new Vector2(0.5f, 0.5f);
                float mx = (float)rng.NextDouble() * 300f - 150f;
                float my = (float)rng.NextDouble() * 140f - 70f;
                meteorRt.anchoredPosition = new Vector2(mx, my);
                meteorRt.sizeDelta = new Vector2(60f, 1.5f);
                meteorRt.localRotation = Quaternion.Euler(0, 0, -30f - i * 15f);
            }

            // === 系别标识（左上角）===
            GameObject labelGo = new GameObject("ThemeLabel", typeof(RectTransform));
            labelGo.transform.SetParent(bgGo.transform, false);
            // 标识背景
            Image labelBg = labelGo.AddComponent<Image>();
            labelBg.color = new Color(0f, 0f, 0f, 0.4f);
            labelBg.raycastTarget = false;
            Text labelText = labelGo.AddComponent<Text>();
            labelText.text = GetThemeName(_prefix);
            labelText.fontSize = 11;
            labelText.color = themeColor;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.fontStyle = FontStyle.Bold;
            if (labelText.font == null) labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.anchoredPosition = new Vector2(6f, -4f);
            labelRt.sizeDelta = new Vector2(110, 18);
        }

        /// <summary>创建星云（大尺寸半透明渐变圆）。</summary>
        private void CreateNebula(Transform parent, Vector2 pos, float size, Color color, float alpha)
        {
            GameObject nebulaGo = new GameObject("Nebula", typeof(RectTransform));
            nebulaGo.transform.SetParent(parent, false);
            Image nebulaImg = nebulaGo.AddComponent<Image>();
            Color c = color;
            c.a = alpha;
            nebulaImg.color = c;
            nebulaImg.raycastTarget = false;
            RectTransform nebulaRt = nebulaGo.GetComponent<RectTransform>();
            nebulaRt.anchorMin = new Vector2(0.5f, 0.5f);
            nebulaRt.anchorMax = new Vector2(0.5f, 0.5f);
            nebulaRt.pivot = new Vector2(0.5f, 0.5f);
            nebulaRt.anchoredPosition = pos;
            nebulaRt.sizeDelta = new Vector2(size, size);
        }

        /// <summary>创建天体（发光球体+光晕）。</summary>
        private void CreateCelestialBody(Transform parent, Vector2 pos, float size, Color color, float alpha, bool hasGlow)
        {
            GameObject bodyGo = new GameObject("CelestialBody", typeof(RectTransform));
            bodyGo.transform.SetParent(parent, false);
            Image bodyImg = bodyGo.AddComponent<Image>();
            Color c = color;
            c.a = alpha;
            bodyImg.color = c;
            bodyImg.raycastTarget = false;
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = pos;
            bodyRt.sizeDelta = new Vector2(size, size);

            if (hasGlow)
            {
                // 外层光晕
                GameObject glowGo = new GameObject("Glow", typeof(RectTransform));
                glowGo.transform.SetParent(bodyGo.transform, false);
                Image glowImg = glowGo.AddComponent<Image>();
                Color glowColor = color;
                glowColor.a = alpha * 0.3f;
                glowImg.color = glowColor;
                glowImg.raycastTarget = false;
                RectTransform glowRt = glowGo.GetComponent<RectTransform>();
                glowRt.anchorMin = new Vector2(0.5f, 0.5f);
                glowRt.anchorMax = new Vector2(0.5f, 0.5f);
                glowRt.pivot = new Vector2(0.5f, 0.5f);
                glowRt.sizeDelta = new Vector2(size * 2.5f, size * 2.5f);
            }
        }

        /// <summary>获取系别主题色。</summary>
        private Color GetThemeColor(string prefix)
        {
            switch (prefix)
            {
                case "mech": return new Color(0.4f, 0.7f, 1f);    // 机械系-蓝
                case "martial": return new Color(1f, 0.5f, 0.3f);  // 武道系-橙
                case "psi": return new Color(0.8f, 0.4f, 1f);      // 异能系-紫
                case "mage": return new Color(0.4f, 1f, 0.6f);     // 魔法系-绿
                case "mind": return new Color(1f, 0.8f, 0.3f);     // 念力系-金
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        /// <summary>获取阶位颜色。</summary>
        private Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.6f, 0.6f, 0.65f);  // 基础-灰
                case 1: return new Color(0.4f, 0.8f, 0.5f);   // 进阶-绿
                case 2: return new Color(0.4f, 0.6f, 1f);    // 高端-蓝
                case 3: return new Color(0.8f, 0.4f, 1f);    // 尖端-紫
                case 4: return new Color(1f, 0.8f, 0.3f);    // 终极-金
                default: return new Color(0.6f, 0.6f, 0.65f);
            }
        }

        /// <summary>获取系别中文名。</summary>
        private string GetThemeName(string prefix)
        {
            switch (prefix)
            {
                case "mech": return "机械知识树";
                case "martial": return "御气技巧树";                case "psi": return "基因树";
                case "mage": return "魔法知识树";
                case "mind": return "精神修炼树";
                default: return "知识树";
            }
        }

        /// <summary>生成知识节点（球面分布）。</summary>
        private void GenerateNodes()
        {
            List<SuperMechKnowledge.KnowledgeDef> allKnowledge = SuperMechKnowledge.GetAllByPrefix(_prefix);
            if (allKnowledge == null || allKnowledge.Count == 0) return;

            int total = allKnowledge.Count;
            for (int i = 0; i < total; i++)
            {
                var def = allKnowledge[i];
                KnowledgeNode node = new KnowledgeNode
                {
                    id = def.id,
                    name = def.name,
                    icon = def.icon,
                    tier = def.tier,
                    unlocked = SuperMechKnowledge.IsUnlocked(_actor, def.id),
                    cost = def.cost
                };
                int actualCost = SuperMechPotential.GetActualCost(_actor, def.id, def.cost);
                node.unlockable = !node.unlocked && SuperMechPotential.GetPotential(_actor) >= actualCost;

                // 斐波那契球面分布（按阶位调整半径）
                float tierRadius = Radius * (0.7f + node.tier * 0.08f);
                node.spherePos = GetPositionOnSphere(i, total, tierRadius);

                // 创建节点GameObject
                CreateNodeGameObject(node);
                _nodes.Add(node);
            }
        }

        /// <summary>斐波那契球面分布。</summary>
        private Vector3 GetPositionOnSphere(int index, int total, float radius)
        {
            float phi = Mathf.Acos(1f - (float)(2 * (index + 1)) / (float)total);
            float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * (float)index;
            float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = radius * Mathf.Cos(phi);
            return new Vector3(x, y, z);
        }

        /// <summary>创建节点GameObject。</summary>
        private void CreateNodeGameObject(KnowledgeNode node)
        {
            GameObject go = new GameObject("Node_" + node.id, typeof(RectTransform));
            go.transform.SetParent(_nodesParent.transform, false);

            // 节点大小按阶位区分（高阶位更大）
            float nodeSize = 32f + node.tier * 4f;

            // 外层光晕（已解锁节点有发光效果）
            if (node.unlocked)
            {
                GameObject glowGo = new GameObject("Glow", typeof(RectTransform));
                glowGo.transform.SetParent(go.transform, false);
                Image glowImg = glowGo.AddComponent<Image>();
                Color glowColor = GetTierColor(node.tier);
                glowColor.a = 0.25f;
                glowImg.color = glowColor;
                glowImg.raycastTarget = false;
                RectTransform glowRt = glowGo.GetComponent<RectTransform>();
                glowRt.anchorMin = new Vector2(0.5f, 0.5f);
                glowRt.anchorMax = new Vector2(0.5f, 0.5f);
                glowRt.pivot = new Vector2(0.5f, 0.5f);
                glowRt.sizeDelta = new Vector2(nodeSize * 1.8f, nodeSize * 1.8f);
            }

            // 外边框（按状态着色）
            GameObject borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(go.transform, false);
            Image borderImg = borderGo.AddComponent<Image>();
            Color borderColor;
            if (node.unlocked) borderColor = GetTierColor(node.tier);
            else if (node.unlockable) borderColor = new Color(0.4f, 0.7f, 1f);
            else borderColor = new Color(0.3f, 0.3f, 0.35f);
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0.5f, 0.5f);
            borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.pivot = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(nodeSize + 4, nodeSize + 4);

            // 背景（深色底）
            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.1f, 0.9f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(nodeSize, nodeSize);

            // 图标
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            if (!string.IsNullOrEmpty(node.icon))
            {
                try { iconImg.sprite = SpriteTextureLoader.getSprite(node.icon); } catch { }
            }
            if (iconImg.sprite == null)
            {
                iconImg.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconStrong");
            }
            iconImg.color = node.unlocked ? Color.white : new Color(1f, 1f, 1f, node.unlockable ? 0.85f : 0.35f);
            iconImg.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(nodeSize * 0.65f, nodeSize * 0.65f);

            // 按钮（透明，覆盖整个节点）
            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            btn.colors = cb;
            btn.targetGraphic = bgImg;

            string nodeId = node.id;
            btn.onClick.AddListener(() =>
            {
                if (SuperMechKnowledge.Unlock(_actor, nodeId))
                {
                    // 刷新节点状态
                    foreach (var n in _nodes)
                    {
                        if (n.id == nodeId)
                        {
                            n.unlocked = true;
                            n.unlockable = false;
                            UpdateNodeVisual(n);
                        }
                        else
                        {
                            n.unlocked = SuperMechKnowledge.IsUnlocked(_actor, n.id);
                            int aCost = SuperMechPotential.GetActualCost(_actor, n.id, n.cost);
                            n.unlockable = !n.unlocked && SuperMechPotential.GetPotential(_actor) >= aCost;
                            UpdateNodeVisual(n);
                        }
                    }
                    // 刷新轴突
                    UpdateAxonsVisual();
                }
            });

            // Tooltip
            TipButton tip = go.AddComponent<TipButton>();
            tip.textOnClick = $"{node.name}\n阶位: {GetTierName(node.tier)}\n消耗: {node.cost}潜能点\n{(node.unlocked ? "已解锁" : (node.unlockable ? "点击解锁" : "未满足条件"))}";

            node.gameObject = go;
            node.image = iconImg;
            node.button = btn;
        }

        /// <summary>更新节点视觉。</summary>
        private void UpdateNodeVisual(KnowledgeNode node)
        {
            if (node.gameObject == null) return;

            // 更新边框颜色
            Image border = node.gameObject.transform.Find("Border")?.GetComponent<Image>();
            if (border != null)
            {
                Color borderColor;
                if (node.unlocked) borderColor = GetTierColor(node.tier);
                else if (node.unlockable) borderColor = new Color(0.4f, 0.7f, 1f);
                else borderColor = new Color(0.3f, 0.3f, 0.35f);
                border.color = borderColor;
            }

            // 更新图标透明度
            if (node.image != null)
            {
                node.image.color = node.unlocked ? Color.white : new Color(1f, 1f, 1f, node.unlockable ? 0.85f : 0.35f);
            }

            // 已解锁节点显示光晕，未解锁隐藏
            Transform glow = node.gameObject.transform.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(node.unlocked);
        }

        /// <summary>生成轴突连接线（基于空间距离+同阶位相邻）。</summary>
        private void GenerateAxons()
        {
            if (_nodes.Count < 2) return;

            // 连接策略：每个节点连接最近的2-3个节点
            float maxDist = Radius * 1.2f;
            for (int i = 0; i < _nodes.Count; i++)
            {
                // 找最近的3个节点
                List<(int idx, float dist)> nearest = new List<(int, float)>();
                for (int j = 0; j < _nodes.Count; j++)
                {
                    if (i == j) continue;
                    float d = Vector3.Distance(_nodes[i].spherePos, _nodes[j].spherePos);
                    if (d < maxDist)
                    {
                        nearest.Add((j, d));
                    }
                }
                nearest.Sort((a, b) => a.dist.CompareTo(b.dist));

                int connectCount = Mathf.Min(2, nearest.Count);
                for (int k = 0; k < connectCount; k++)
                {
                    int j = nearest[k].idx;
                    // 避免重复连接
                    if (_axons.Exists(a =>
                        (a.from == _nodes[i] && a.to == _nodes[j]) ||
                        (a.from == _nodes[j] && a.to == _nodes[i])))
                        continue;

                    Axon axon = new Axon
                    {
                        from = _nodes[i],
                        to = _nodes[j],
                        active = _nodes[i].unlocked && _nodes[j].unlocked
                    };
                    CreateAxonGameObject(axon);
                    _axons.Add(axon);
                }
            }
        }

        /// <summary>创建轴突GameObject（用Image拉伸模拟线）。</summary>
        private void CreateAxonGameObject(Axon axon)
        {
            GameObject go = new GameObject("Axon", typeof(RectTransform));
            go.transform.SetParent(_axonsParent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = axon.active ? ColorAxonActive : ColorAxonDefault;
            img.raycastTarget = false;
            axon.lineObj = go;
            axon.lineImage = img;
        }

        /// <summary>更新轴突位置（每帧根据节点位置）。</summary>
        private void UpdateAxons()
        {
            foreach (var axon in _axons)
            {
                if (axon.lineObj == null || axon.from.gameObject == null || axon.to.gameObject == null) continue;

                Vector3 fromPos = axon.from.gameObject.transform.localPosition;
                Vector3 toPos = axon.to.gameObject.transform.localPosition;
                Vector3 mid = (fromPos + toPos) / 2f;
                float dist = Vector3.Distance(fromPos, toPos);
                float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;

                RectTransform rt = axon.lineObj.GetComponent<RectTransform>();
                rt.localPosition = mid;
                rt.sizeDelta = new Vector2(dist, 2f);
                rt.localRotation = Quaternion.Euler(0, 0, angle);

                // 根据深度调整透明度
                float avgZ = (fromPos.z + toPos.z) / 2f;
                float alpha = Mathf.InverseLerp(-Radius, Radius, avgZ);
                Color c = axon.active ? ColorAxonActive : ColorAxonDefault;
                c.a *= 0.3f + alpha * 0.7f;
                axon.lineImage.color = c;
            }
        }

        /// <summary>更新轴突视觉状态。</summary>
        private void UpdateAxonsVisual()
        {
            foreach (var axon in _axons)
            {
                axon.active = axon.from.unlocked && axon.to.unlocked;
                // 已解锁的轴突上生成神经冲动
                if (axon.active && Random.value < 0.3f)
                {
                    SpawnImpulse(axon);
                }
            }
        }

        /// <summary>生成神经冲动。</summary>
        private void SpawnImpulse(Axon axon)
        {
            if (_impulses.Count > 30) return; // 限制数量

            GameObject go = new GameObject("Impulse", typeof(RectTransform));
            go.transform.SetParent(_impulsesParent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = ColorImpulse;
            img.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(6, 6);

            NerveImpulse impulse = new NerveImpulse
            {
                axon = axon,
                obj = go,
                image = img,
                progress = 0f,
                speed = Random.Range(0.5f, 1.5f)
            };
            _impulses.Add(impulse);
        }

        /// <summary>更新神经冲动。</summary>
        private void UpdateImpulses()
        {
            for (int i = _impulses.Count - 1; i >= 0; i--)
            {
                var imp = _impulses[i];
                if (imp.axon.lineObj == null || imp.axon.from.gameObject == null || imp.axon.to.gameObject == null)
                {
                    Destroy(imp.obj);
                    _impulses.RemoveAt(i);
                    continue;
                }

                imp.progress += Time.deltaTime * imp.speed;
                if (imp.progress >= 1f)
                {
                    Destroy(imp.obj);
                    _impulses.RemoveAt(i);
                    continue;
                }

                Vector3 fromPos = imp.axon.from.gameObject.transform.localPosition;
                Vector3 toPos = imp.axon.to.gameObject.transform.localPosition;
                Vector3 pos = Vector3.Lerp(fromPos, toPos, imp.progress);
                imp.obj.transform.localPosition = pos;

                // 闪烁效果
                float pulse = 0.5f + Mathf.Sin(Time.time * 10f) * 0.5f;
                Color c = ColorImpulse;
                c.a *= pulse;
                imp.image.color = c;
            }
        }

        /// <summary>每帧更新节点位置（根据旋转）和深度效果。</summary>
        private void UpdateNodes()
        {
            foreach (var node in _nodes)
            {
                if (node.gameObject == null) continue;

                // 应用旋转到球面坐标
                Vector3 rotated = ApplyRotation(node.spherePos);
                node.gameObject.transform.localPosition = rotated;

                // 根据z深度调整大小和透明度（模拟3D）
                float depth = Mathf.InverseLerp(-Radius, Radius, rotated.z);
                float scale = NodeScaleMin + depth * (NodeScaleMax - NodeScaleMin);
                node.gameObject.transform.localScale = Vector3.one * scale;

                // 透明度
                CanvasGroup cg = node.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = node.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.4f + depth * 0.6f;

                // 排序：z大的在前面
                node.gameObject.transform.SetSiblingIndex(Mathf.RoundToInt(depth * 100));
            }
        }

        /// <summary>应用旋转到球面坐标。</summary>
        private Vector3 ApplyRotation(Vector3 pos)
        {
            // Y轴旋转
            float cosY = Mathf.Cos(_rotationY * Mathf.Deg2Rad);
            float sinY = Mathf.Sin(_rotationY * Mathf.Deg2Rad);
            float x1 = pos.x * cosY - pos.z * sinY;
            float z1 = pos.x * sinY + pos.z * cosY;

            // X轴旋转
            float cosX = Mathf.Cos(_rotationX * Mathf.Deg2Rad);
            float sinX = Mathf.Sin(_rotationX * Mathf.Deg2Rad);
            float y2 = pos.y * cosX - z1 * sinX;
            float z2 = pos.y * sinX + z1 * cosX;

            return new Vector3(x1, y2, z2);
        }

        /// <summary>更新图谱容器旋转。</summary>
        private void UpdateGraphTransform()
        {
            // 不旋转容器，只通过UpdateNodes旋转节点位置，避免双重旋转
        }

        void Update()
        {
            // 平滑旋转
            if (!_isDragging)
            {
                _rotationY = Mathf.Lerp(_rotationY, _targetRotationY, Time.deltaTime * 5f);
                _rotationX = Mathf.Lerp(_rotationX, _targetRotationX, Time.deltaTime * 5f);
                // 自动缓慢旋转
                _targetRotationY += Time.deltaTime * 2f;
            }

            UpdateNodes();
            UpdateAxons();
            UpdateImpulses();
        }

        // 拖拽接口
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _lastDragPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            Vector2 delta = eventData.position - _lastDragPos;
            _targetRotationY += delta.x * DragSpeed;
            _targetRotationX -= delta.y * DragSpeed;
            _targetRotationX = Mathf.Clamp(_targetRotationX, -60f, 60f);
            _lastDragPos = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        /// <summary>获取阶位名称。</summary>
        private string GetTierName(int tier)
        {
            string[] names = { "基础", "进阶", "高端", "尖端", "终极" };
            return tier >= 0 && tier < names.Length ? names[tier] : "未知";
        }

        /// <summary>清理。</summary>
        public void Clear()
        {
            if (_graphContainer != null)
            {
                Destroy(_graphContainer);
                _graphContainer = null;
            }
            _nodes.Clear();
            _axons.Clear();
            _impulses.Clear();
        }
    }
}
