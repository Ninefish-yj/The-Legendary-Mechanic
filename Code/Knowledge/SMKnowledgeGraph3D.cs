using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMKnowledgeGraph3D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float Radius = 70f;
        private const float NodeSize = 28f;
        private const float DragSpeed = 0.46f;
        private const float DragRotateSpeed = 0.005f;
        private const float RotationBounds = 0.7f;

        public class KnowledgeNode
        {
            public string id;
            public string name;
            public string icon;
            public int tier;
            public bool unlocked;
            public Vector3 spherePos;
            public GameObject gameObject;
            public Image image;
            public float renderDepth;
        }

        private class Axon
        {
            public KnowledgeNode from;
            public KnowledgeNode to;
            public GameObject lineObj;
            public Image lineImage;
        }

        private List<KnowledgeNode> _nodes = new List<KnowledgeNode>();
        private List<Axon> _axons = new List<Axon>();

        private GameObject _nodesParent;
        private GameObject _axonsParent;

        private bool _isDragging;
        private float _offsetX;
        private float _offsetY;
        private float _targetOffsetX = -0.015f;
        private float _targetOffsetY = 0.07f;

        private Actor _actor;
        private string _prefix;

        public void Init(Actor actor, string prefix, Transform parent)
        {
            _actor = actor;
            _prefix = prefix;

            Clear();

            _nodesParent = new GameObject("Nodes", typeof(RectTransform));
            _nodesParent.transform.SetParent(parent, false);
            RectTransform nrt = _nodesParent.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.offsetMin = Vector2.zero;
            nrt.offsetMax = Vector2.zero;

            _axonsParent = new GameObject("Axons", typeof(RectTransform));
            _axonsParent.transform.SetParent(parent, false);
            RectTransform art = _axonsParent.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = Vector2.zero;
            art.offsetMax = Vector2.zero;
            _axonsParent.transform.SetAsFirstSibling();

            GenerateNodes();
            GenerateAxons();
            UpdateVisual();

            Debug.Log($"[超神机械师] 3D图谱初始化: prefix={_prefix}, 节点数={_nodes.Count}, 轴突数={_axons.Count}");
            if (_nodes.Count > 0)
            {
                Debug.Log($"[超神机械师] 首节点: id={_nodes[0].id}, pos={_nodes[0].spherePos}, go={_nodes[0].gameObject != null}");
            }
        }

        private void GenerateNodes()
        {
            var allDefs = SuperMechKnowledge.GetAllByPrefix(_prefix);
            if (allDefs == null || allDefs.Count == 0) return;

            int total = allDefs.Count;
            for (int i = 0; i < total; i++)
            {
                var def = allDefs[i];
                bool unlocked = SuperMechKnowledge.IsUnlocked(_actor, def.id);
                KnowledgeNode node = new KnowledgeNode
                {
                    id = def.id,
                    name = def.name,
                    icon = def.icon,
                    tier = def.tier,
                    unlocked = unlocked,
                    spherePos = GetPositionOnSphere(i, total)
                };
                CreateNodeGameObject(node);
                _nodes.Add(node);
            }
        }

        private Vector3 GetPositionOnSphere(int index, int total)
        {
            float f = Mathf.Acos(1f - (float)(2 * (index + 1)) / (float)total);
            float f2 = Mathf.PI * (1f + Mathf.Sqrt(5f)) * (float)index;
            float x = Radius * Mathf.Cos(f2) * Mathf.Sin(f);
            float y = Radius * Mathf.Sin(f2) * Mathf.Sin(f);
            float z = Radius * Mathf.Cos(f);
            return new Vector3(x, y, z);
        }

        private void CreateNodeGameObject(KnowledgeNode node)
        {
            GameObject go = new GameObject("Node_" + node.id, typeof(RectTransform));
            go.transform.SetParent(_nodesParent.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(NodeSize, NodeSize);
            rt.localPosition = node.spherePos;

            Image bg = go.AddComponent<Image>();
            bg.sprite = SpriteTextureLoader.getSprite("ui/special/special_circle");
            Color tierColor = GetTierColor(node.tier);

            var def = SuperMechKnowledge.GetDef(node.id);
            int cost = def != null ? def.cost : 1;
            int pot = SuperMechPotential.GetPotential(_actor);
            bool tierUnlocked = node.tier == 0 || SuperMechKnowledge.GetTierKnowledgeCount(_actor, _prefix, node.tier - 1) > 0;
            bool canUnlock = !node.unlocked && tierUnlocked && pot >= cost;

            if (node.unlocked)
            {
                bg.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.95f);
            }
            else if (canUnlock)
            {
                bg.color = new Color(tierColor.r * 0.7f, tierColor.g * 0.7f, tierColor.b * 0.7f, 0.6f);
            }
            else
            {
                bg.color = new Color(0.2f, 0.2f, 0.25f, 0.4f);
            }
            bg.raycastTarget = true;

            if (!string.IsNullOrEmpty(node.icon))
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(go.transform, false);
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(4, 4);
                iconRt.offsetMax = new Vector2(-4, -4);
                Image iconImg = iconGo.AddComponent<Image>();
                try { iconImg.sprite = SpriteTextureLoader.getSprite(node.icon); } catch { }
                iconImg.color = node.unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
                iconImg.raycastTarget = false;
            }

            Button btn = go.AddComponent<Button>();
            if (canUnlock)
            {
                btn.onClick.AddListener(() =>
                {
                    SuperMechPotential.UnlockNode(_actor, node.id, cost);
                });
            }

            TipButton tip = go.AddComponent<TipButton>();
            tip.textOnClick = LocalizedTextManager.getText(node.name);
            tip.textOnClickDescription = SuperMechKnowledge.GetDef(node.id)?.desc ?? "";

            node.gameObject = go;
            node.image = bg;
        }

        private Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.5f, 0.7f, 0.9f);
                case 1: return new Color(0.4f, 0.8f, 0.5f);
                case 2: return new Color(0.7f, 0.5f, 0.9f);
                case 3: return new Color(0.9f, 0.6f, 0.3f);
                case 4: return new Color(0.95f, 0.8f, 0.3f);
                default: return new Color(0.9f, 0.9f, 0.9f);
            }
        }

        private void GenerateAxons()
        {
            int count = _nodes.Count;
            if (count < 2) return;

            float maxDist = 250f / Mathf.Sqrt(count) * 1.5f;
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    float dist = Vector3.Distance(_nodes[i].spherePos, _nodes[j].spherePos);
                    if (dist <= maxDist)
                    {
                        CreateAxon(_nodes[i], _nodes[j]);
                    }
                }
            }
        }

        private void CreateAxon(KnowledgeNode from, KnowledgeNode to)
        {
            GameObject go = new GameObject("Axon", typeof(RectTransform));
            go.transform.SetParent(_axonsParent.transform, false);

            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f);
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Axon axon = new Axon { from = from, to = to, lineObj = go, lineImage = img };
            _axons.Add(axon);
        }

        private void UpdateVisual()
        {
            Quaternion rot = Quaternion.Euler(_offsetX, _offsetY, 0f);

            foreach (var node in _nodes)
            {
                if (node.gameObject == null) continue;

                Vector3 rotated = rot * node.spherePos;
                node.gameObject.transform.localPosition = rotated;

                float depth = Mathf.InverseLerp(-Radius, Radius, rotated.z);
                node.renderDepth = depth;

                float scale = Mathf.Lerp(0.7f, 1.3f, depth);
                node.gameObject.transform.localScale = new Vector3(scale, scale, 1f);

                Color c = node.image.color;
                c.a = Mathf.Lerp(0.25f, node.unlocked ? 0.95f : 0.6f, depth);
                node.image.color = c;
            }

            _nodes.Sort((a, b) => a.renderDepth.CompareTo(b.renderDepth));
            foreach (var node in _nodes)
            {
                if (node.gameObject != null) node.gameObject.transform.SetAsLastSibling();
            }

            foreach (var axon in _axons)
            {
                if (axon.lineObj == null || axon.from.gameObject == null || axon.to.gameObject == null) continue;

                Vector2 p1 = axon.from.gameObject.transform.localPosition;
                Vector2 p2 = axon.to.gameObject.transform.localPosition;
                Vector2 mid = (p1 + p2) / 2f;
                axon.lineObj.transform.localPosition = mid;

                float dist = Vector3.Distance(p1, p2);
                axon.lineObj.transform.localScale = new Vector3(dist, 1f, 1f);

                Vector3 diff = p2 - p1;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                axon.lineObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _offsetX = 0f;
            _offsetY = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _offsetX = (0f - eventData.delta.y) * DragSpeed * 0.01f;
            _offsetY = eventData.delta.x * DragSpeed * 0.01f;
            UpdateVisual();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _targetOffsetX += (0f - eventData.delta.y) * DragRotateSpeed;
            _targetOffsetY += eventData.delta.x * DragRotateSpeed;
            _targetOffsetX = Mathf.Clamp(_targetOffsetX, -RotationBounds, RotationBounds);
            _targetOffsetY = Mathf.Clamp(_targetOffsetY, -RotationBounds, RotationBounds);
        }

        private int _checkFrameCounter;

        void Update()
        {
            if (!_isDragging)
            {
                _offsetX = Mathf.Lerp(_offsetX, _targetOffsetX, 0.1f);
                _offsetY = Mathf.Lerp(_offsetY, _targetOffsetY, 0.1f);
                _targetOffsetY += Time.deltaTime * 0.1f;
            }

            _checkFrameCounter++;
            if (_checkFrameCounter >= 30)
            {
                _checkFrameCounter = 0;
                CheckAndAddNewNodes();
            }

            UpdateVisual();
        }

        private void CheckAndAddNewNodes()
        {
            if (_actor == null || string.IsNullOrEmpty(_prefix)) return;

            var allDefs = SuperMechKnowledge.GetAllByPrefix(_prefix);
            if (allDefs == null) return;

            int unlockedCount = 0;
            foreach (var def in allDefs)
            {
                if (SuperMechKnowledge.IsUnlocked(_actor, def.id)) unlockedCount++;
            }

            if (unlockedCount != _nodes.Count)
            {
                Debug.Log($"[超神机械师] 3D图谱检测到新解锁知识: {_nodes.Count}→{unlockedCount}，重新生成节点");
                ClearNodesOnly();
                GenerateNodes();
                GenerateAxons();
            }
        }

        private void ClearNodesOnly()
        {
            foreach (var node in _nodes)
            {
                if (node.gameObject != null) Destroy(node.gameObject);
            }
            foreach (var axon in _axons)
            {
                if (axon.lineObj != null) Destroy(axon.lineObj);
            }
            _nodes.Clear();
            _axons.Clear();
        }

        private void Clear()
        {
            if (_nodesParent != null) Destroy(_nodesParent);
            if (_axonsParent != null) Destroy(_axonsParent);
            _nodes.Clear();
            _axons.Clear();
        }
    }
}
