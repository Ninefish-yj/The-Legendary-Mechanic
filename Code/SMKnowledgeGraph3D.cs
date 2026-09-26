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
        private const float Radius = 120f;
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
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;

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
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
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
            UpdateGraphTransform();
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

            // 背景（发光效果）
            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = node.unlocked ? ColorUnlocked : (node.unlockable ? ColorUnlockable : ColorLocked);
            bgImg.type = Image.Type.Simple;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(40, 40);

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
                // 默认图标：按阶位选颜色
                iconImg.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconStrong");
            }
            iconImg.color = node.unlocked ? Color.white : new Color(1f, 1f, 1f, node.unlockable ? 0.9f : 0.4f);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(28, 28);

            // 按钮
            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.8f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;
            btn.targetGraphic = iconImg;

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
            Image bg = node.gameObject.transform.Find("Bg")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = node.unlocked ? ColorUnlocked : (node.unlockable ? ColorUnlockable : ColorLocked);
            }
            if (node.image != null)
            {
                node.image.color = node.unlocked ? Color.white : new Color(1f, 1f, 1f, node.unlockable ? 0.9f : 0.4f);
            }
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
            if (_graphContainer != null)
            {
                _graphContainer.transform.localRotation = Quaternion.Euler(_rotationX, _rotationY, 0);
            }
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
