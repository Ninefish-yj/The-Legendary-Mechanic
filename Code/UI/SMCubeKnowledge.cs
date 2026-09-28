using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMCubeNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        private const float SCALE_HIGHLIGHTED = 1.6f;
        private const float SCALE_NORMAL = 1f;

        public Vector4 logical_pos;
        internal List<SMCubeNode> connected_nodes = new List<SMCubeNode>();
        internal List<SMCubeNodeConnection> connections = new List<SMCubeNodeConnection>();

        private Image _image;
        private Text _text;
        private SMCubeKnowledge _cubeOverview;
        internal float render_depth;
        internal float scale_mod_spawn = 1f;
        internal float bonus_scale = 1f;
        internal bool highlighted;
        private float _timer_change;
        internal string knowledgeId;
        internal int tier;
        internal bool unlocked;

        private void Start()
        {
            _cubeOverview = gameObject.GetComponentInParent<SMCubeKnowledge>();
            _image = GetComponent<Image>();
            initClick();
        }

        public void update()
        {
            _timer_change -= Time.deltaTime;
        }

        public void clear()
        {
            connected_nodes.Clear();
            connections.Clear();
            _timer_change = 0f;
        }

        protected void initClick()
        {
            if (TryGetComponent<Button>(out var component))
            {
                component.onClick.AddListener(setPressed);
            }
        }

        public void setupNode(string pKnowledgeId, int pTier, bool pUnlocked, Sprite pIcon)
        {
            if (!(_timer_change > 0f))
            {
                _timer_change = 2f;
                knowledgeId = pKnowledgeId;
                tier = pTier;
                unlocked = pUnlocked;
                if (_image == null) _image = GetComponent<Image>();
                if (pIcon != null) _image.sprite = pIcon;
            }
        }

        public void setHighlighted()
        {
            if (!highlighted)
            {
                highlighted = true;
                scale_mod_spawn = SCALE_HIGHLIGHTED;
            }
        }

        public void setPressed()
        {
            _cubeOverview?.OnNodeClicked(this);
        }

        public void setColor(Color pColor)
        {
            if (_image == null) _image = GetComponent<Image>();
            _image.color = pColor;
        }

        public void addConnection(SMCubeNode pNode, SMCubeNodeConnection pConnection)
        {
            connected_nodes.Add(pNode);
            connections.Add(pConnection);
        }

        public void OnInitializePotentialDrag(PointerEventData pEventData)
        {
            _cubeOverview?.OnInitializePotentialDrag(pEventData);
        }

        public void OnBeginDrag(PointerEventData pEventData)
        {
            _cubeOverview?.OnBeginDrag(pEventData);
        }

        public void OnDrag(PointerEventData pEventData)
        {
            _cubeOverview?.OnDrag(pEventData);
        }

        public void OnEndDrag(PointerEventData pEventData)
        {
            _cubeOverview?.OnEndDrag(pEventData);
        }
    }

    public class SMCubeNodeConnection : MonoBehaviour
    {
        public Image image;
        internal SMCubeNode node_1;
        internal SMCubeNode node_2;
        internal bool inner_cube;
        internal float mod_light = 1f;

        public void update()
        {
            mod_light -= Time.deltaTime * 2f;
            mod_light = Mathf.Max(0f, mod_light);
        }

        public void setConnection(bool pInner)
        {
            inner_cube = pInner;
        }

        public void clear()
        {
            node_1 = null;
            node_2 = null;
            inner_cube = false;
        }
    }

    public class SMCubeKnowledge : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DRAG_ROTATE_SPEED = 0.005f;
        private const float NODE_SCALE_MIN = 0.4f;
        private const float NODE_SCALE_MAX = 1.2f;
        private const float PERSPECTIVE_STRENGTH_MAIN = 3f;
        private const float SPEED_MOD_INNER = 0.2f;
        private const float SPEED_MOD_OUTER = 0.2f;
        private const float SPEED_MOD_4D = 0.3f;

        private static readonly Vector4[] _hypercube_positions = new Vector4[16]
        {
            new Vector4(-1f, -1f, -1f, -1f),
            new Vector4(1f, -1f, -1f, -1f),
            new Vector4(-1f, 1f, -1f, -1f),
            new Vector4(1f, 1f, -1f, -1f),
            new Vector4(-1f, -1f, 1f, -1f),
            new Vector4(1f, -1f, 1f, -1f),
            new Vector4(-1f, 1f, 1f, -1f),
            new Vector4(1f, 1f, 1f, -1f),
            new Vector4(-1f, -1f, -1f, 1f),
            new Vector4(1f, -1f, -1f, 1f),
            new Vector4(-1f, 1f, -1f, 1f),
            new Vector4(1f, 1f, -1f, 1f),
            new Vector4(-1f, -1f, 1f, 1f),
            new Vector4(1f, -1f, 1f, 1f),
            new Vector4(-1f, 1f, 1f, 1f),
            new Vector4(1f, 1f, 1f, 1f)
        };

        private static readonly int[,] _hypercube_connections = new int[32, 2]
        {
            { 0, 1 }, { 0, 2 }, { 0, 4 }, { 0, 8 },
            { 1, 3 }, { 1, 5 }, { 1, 9 },
            { 2, 3 }, { 2, 6 }, { 2, 10 },
            { 3, 7 }, { 3, 11 },
            { 4, 5 }, { 4, 6 }, { 4, 12 },
            { 5, 7 }, { 5, 13 },
            { 6, 7 }, { 6, 14 },
            { 7, 15 },
            { 8, 9 }, { 8, 10 }, { 8, 12 },
            { 9, 11 }, { 9, 13 },
            { 10, 11 }, { 10, 14 },
            { 11, 15 },
            { 12, 13 }, { 12, 14 },
            { 13, 15 },
            { 14, 15 }
        };

        private SMCubeNode _active_node;
        private SMCubeNode _prefab_node;
        private SMCubeNodeConnection _prefab_connection;
        private RectTransform _parent_connections;
        private RectTransform _parent_nodes;
        private GameObject _object_main;

        private float _offset_target_x = -0.015f;
        private float _offset_target_y = 0.07f;
        private bool _is_dragging;
        private Vector2 _last_mouse_delta;
        private float _offset_x;
        private float _offset_y;

        private float _angle_4d;
        private Quaternion _rotation_q = Quaternion.identity;
        private Quaternion _rotation_q_2 = Quaternion.identity;
        private float _perspective_strength_main = PERSPECTIVE_STRENGTH_MAIN;
        public float spacing = 25f;

        private List<SMCubeNode> _nodes_by_index = new List<SMCubeNode>();
        private List<SMCubeNode> _nodes = new List<SMCubeNode>();
        private ObjectPoolGenericMono<SMCubeNode> _pool_nodes;
        private ObjectPoolGenericMono<SMCubeNodeConnection> _pool_connections;

        private Actor _actor;
        private bool _initialized;

        public void Init(Actor actor)
        {
            _actor = actor;
            if (!_initialized)
            {
                CreateStructure();
                _initialized = true;
            }
            GenerateNodes();

            if (_object_main != null)
            {
                _object_main.transform.DOKill();
                _object_main.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                _object_main.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack);
            }
        }

        private void CreateStructure()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            _object_main = new GameObject("CubeMain", typeof(RectTransform));
            _object_main.transform.SetParent(transform, false);
            RectTransform mainRt = _object_main.GetComponent<RectTransform>();
            mainRt.anchorMin = Vector2.zero;
            mainRt.anchorMax = Vector2.one;
            mainRt.offsetMin = Vector2.zero;
            mainRt.offsetMax = Vector2.zero;

            _parent_connections = new GameObject("Connections", typeof(RectTransform)).GetComponent<RectTransform>();
            _parent_connections.transform.SetParent(_object_main.transform, false);
            _parent_connections.anchorMin = Vector2.zero;
            _parent_connections.anchorMax = Vector2.one;
            _parent_connections.offsetMin = Vector2.zero;
            _parent_connections.offsetMax = Vector2.zero;

            _parent_nodes = new GameObject("Nodes", typeof(RectTransform)).GetComponent<RectTransform>();
            _parent_nodes.transform.SetParent(_object_main.transform, false);
            _parent_nodes.anchorMin = Vector2.zero;
            _parent_nodes.anchorMax = Vector2.one;
            _parent_nodes.offsetMin = Vector2.zero;
            _parent_nodes.offsetMax = Vector2.zero;

            _prefab_node = CreateNodePrefab();
            _prefab_connection = CreateConnectionPrefab();
            _pool_nodes = new ObjectPoolGenericMono<SMCubeNode>(_prefab_node, _parent_nodes);
            _pool_connections = new ObjectPoolGenericMono<SMCubeNodeConnection>(_prefab_connection, _parent_connections);
        }

        private SMCubeNode CreateNodePrefab()
        {
            GameObject prefab = new GameObject("SMCubeNodePrefab", typeof(RectTransform));
            prefab.SetActive(false);
            Image img = prefab.AddComponent<Image>();
            img.color = Color.white;
            Button btn = prefab.AddComponent<Button>();
            SMCubeNode node = prefab.AddComponent<SMCubeNode>();
            return node;
        }

        private SMCubeNodeConnection CreateConnectionPrefab()
        {
            GameObject prefab = new GameObject("SMCubeConnectionPrefab", typeof(RectTransform));
            prefab.SetActive(false);
            Image img = prefab.AddComponent<Image>();
            img.color = new Color(0.23f, 1f, 0.96f, 0.4f);
            SMCubeNodeConnection conn = prefab.AddComponent<SMCubeNodeConnection>();
            conn.image = img;
            return conn;
        }

        private void GenerateNodes()
        {
            _pool_nodes.clear();
            _pool_connections.clear();
            _nodes.Clear();
            _nodes_by_index.Clear();

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(_actor));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var allKnowledge = SuperMechKnowledge.GetAllByPrefix(prefix);

            var selected = new List<SuperMechKnowledge.KnowledgeDef>();
            if (allKnowledge.Count <= 16)
            {
                selected.AddRange(allKnowledge);
            }
            else
            {
                var shuffled = new List<SuperMechKnowledge.KnowledgeDef>(allKnowledge);
                shuffled.Shuffle();
                selected.AddRange(shuffled.GetRange(0, 16));
            }

            for (int i = 0; i < _hypercube_positions.Length; i++)
            {
                if (i >= selected.Count) break;

                var def = selected[i];
                string kid = def.id;
                bool unlocked = SuperMechKnowledge.IsUnlocked(_actor, kid);
                int tier = def.tier;

                SMCubeNode node = _pool_nodes.getNext();
                Sprite icon = null;
                try { icon = SpriteTextureLoader.getSprite(def.icon); } catch { }
                node.setupNode(kid, tier, unlocked, icon);
                node.logical_pos = _hypercube_positions[i];
                node.setColor(GetNodeColor(tier, unlocked));
                node.gameObject.name = i.ToString();
                _nodes.Add(node);
                _nodes_by_index.Add(node);
            }

            GenerateConnections();
            UpdateVisual();
        }

        private Color GetNodeColor(int tier, bool unlocked)
        {
            Color[] tierColors = {
                new Color(0.3f, 0.71f, 0.67f),
                new Color(0.15f, 0.65f, 0.6f),
                new Color(0f, 0.54f, 0.48f),
                new Color(0f, 0.47f, 0.42f),
                new Color(0f, 0.3f, 0.25f)
            };
            Color c = tierColors[Mathf.Clamp(tier, 0, tierColors.Length - 1)];
            if (!unlocked) c = new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 0.5f);
            return c;
        }

        private void GenerateConnections()
        {
            for (int i = 0; i < _hypercube_connections.GetLength(0); i++)
            {
                int index1 = _hypercube_connections[i, 0];
                int index2 = _hypercube_connections[i, 1];
                if (index1 >= _nodes_by_index.Count || index2 >= _nodes_by_index.Count) continue;

                SMCubeNode a = _nodes_by_index[index1];
                SMCubeNode b = _nodes_by_index[index2];
                if (a == null || b == null) continue;

                SMCubeNodeConnection conn = _pool_connections.getNext();
                conn.node_1 = a;
                conn.node_2 = b;
                bool isInner = a.logical_pos.w < 0f && b.logical_pos.w < 0f;
                conn.setConnection(isInner);
                a.addConnection(b, conn);
                b.addConnection(a, conn);
                conn.gameObject.name = "connection " + a.gameObject.name + "-" + b.gameObject.name;
            }
        }

        private void Update()
        {
            if (!_initialized || _nodes.Count == 0) return;

            UpdateRotationAndSpeeds();
            UpdateVisual();
        }

        private void UpdateRotationAndSpeeds()
        {
            if (!_is_dragging)
            {
                _angle_4d += Time.deltaTime * SPEED_MOD_4D;
                _offset_target_x += Time.deltaTime * 0.05f;
            }

            if (Input.GetMouseButton(0))
            {
                _perspective_strength_main = Mathf.Lerp(_perspective_strength_main, 4f, 0.1f);
            }
            else
            {
                _perspective_strength_main = Mathf.Lerp(_perspective_strength_main, PERSPECTIVE_STRENGTH_MAIN, 0.1f);
            }

            _offset_x = Mathf.Lerp(_offset_x, _offset_target_x, Time.deltaTime * 2f);
            _offset_y = Mathf.Lerp(_offset_y, _offset_target_y, Time.deltaTime * 2f);

            float num = 0f - _offset_x;
            float num2 = 0f - _offset_y;
            float num3 = _offset_y;
            float num4 = _offset_y;
            if (!_is_dragging)
            {
                num += SPEED_MOD_INNER;
                num2 += SPEED_MOD_INNER;
                num3 += SPEED_MOD_OUTER;
                num4 += SPEED_MOD_OUTER;
            }
            Quaternion quaternion = Quaternion.Euler(num, num2, 0f);
            _rotation_q = quaternion * _rotation_q;
            Quaternion quaternion2 = Quaternion.Euler(num3, num4, 0f);
            _rotation_q_2 = quaternion2 * _rotation_q_2;
        }

        private Vector4 Rotate4D(Vector4 pPoint, float pAngle)
        {
            float num = Mathf.Cos(pAngle);
            float num2 = Mathf.Sin(pAngle);
            float x = pPoint.x * num - pPoint.w * num2;
            float w = pPoint.x * num2 + pPoint.w * num;
            float y = pPoint.y * num - pPoint.z * num2;
            float z = pPoint.y * num2 + pPoint.z * num;
            return new Vector4(x, y, z, w);
        }

        private Vector3 Project4Dto3D(Vector4 p)
        {
            float num = _perspective_strength_main;
            float num5 = num - p.w;
            if (Mathf.Abs(num5) < 0.01f)
            {
                num5 = 0.01f * Mathf.Sign(num5);
            }
            float num6 = ((num5 == 0f) ? 0f : (num / num5));
            return new Vector3(p.x * num6, p.y * num6, p.z * num6);
        }

        private void UpdateVisual()
        {
            float angle_4d = _angle_4d;
            float centerX = GetComponent<RectTransform>().rect.width / 2f;
            float centerY = GetComponent<RectTransform>().rect.height / 2f;

            foreach (SMCubeNode node in _nodes)
            {
                bool isInner = node.logical_pos.w < 0f;
                Vector4 p = Rotate4D(node.logical_pos, angle_4d);
                Vector3 vector = Project4Dto3D(p) * spacing;
                Vector3 localPosition = (isInner ? _rotation_q : _rotation_q_2) * vector;

                node.render_depth = localPosition.z;
                float depthNorm = (localPosition.z + 1f) / 2f;
                float scale = Mathf.Lerp(NODE_SCALE_MIN, NODE_SCALE_MAX, depthNorm) * node.scale_mod_spawn;
                node.transform.localScale = new Vector3(scale, scale, 1f);

                RectTransform nodeRt = node.GetComponent<RectTransform>();
                nodeRt.anchoredPosition = new Vector2(centerX + localPosition.x, centerY + localPosition.y);
                nodeRt.SetSiblingIndex((int)((depthNorm) * 1000f));

                Color baseColor = GetNodeColor(node.tier, node.unlocked);
                float colorLerp = Mathf.Clamp01(depthNorm);
                node.setColor(Color.Lerp(new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.3f, baseColor.a), baseColor, colorLerp));
            }

            foreach (SMCubeNodeConnection conn in _pool_connections.getListTotal())
            {
                if (conn.node_1 == null || conn.node_2 == null) continue;
                if (!conn.gameObject.activeSelf) continue;
                UpdateConnection(conn);
            }
        }

        private void UpdateConnection(SMCubeNodeConnection conn)
        {
            RectTransform rt1 = conn.node_1.GetComponent<RectTransform>();
            RectTransform rt2 = conn.node_2.GetComponent<RectTransform>();
            Vector2 pos1 = rt1.anchoredPosition;
            Vector2 pos2 = rt2.anchoredPosition;
            Vector2 diff = pos2 - pos1;
            float dist = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            RectTransform connRt = conn.GetComponent<RectTransform>();
            connRt.anchoredPosition = (pos1 + pos2) / 2f;
            connRt.sizeDelta = new Vector2(dist, 2f);
            connRt.localRotation = Quaternion.Euler(0, 0, angle);

            bool bothUnlocked = conn.node_1.unlocked && conn.node_2.unlocked;
            conn.image.color = bothUnlocked
                ? new Color(0.23f, 1f, 0.96f, 0.4f + conn.mod_light * 0.3f)
                : new Color(0.11f, 0.48f, 0.45f, 0.15f);
        }

        public void OnNodeClicked(SMCubeNode node)
        {
            if (!node.unlocked && _actor != null)
            {
                bool success = SuperMechKnowledge.Unlock(_actor, node.knowledgeId);
                if (success)
                {
                    node.unlocked = true;
                    node.setColor(GetNodeColor(node.tier, true));
                    node.setHighlighted();
                    foreach (var conn in node.connected_nodes)
                    {
                        if (conn.unlocked)
                        {
                            SMCubeNodeConnection c = node.connections.Find(x => x.node_1 == conn || x.node_2 == conn);
                            if (c != null) c.mod_light = 1f;
                        }
                    }
                }
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            _last_mouse_delta = Vector2.zero;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _is_dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _last_mouse_delta = eventData.delta;
            _offset_target_x += eventData.delta.x * 0.005f;
            _offset_target_y += eventData.delta.y * 0.005f;
            _offset_target_y = Mathf.Clamp(_offset_target_y, -0.5f, 0.5f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _is_dragging = false;
        }
    }
}
