using System.Collections.Generic;
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
        private List<SMCubeNodeConnection> connections = new List<SMCubeNodeConnection>();

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
            int index = 0;
            foreach (var def in allKnowledge)
            {
                string kid = def.id;
                bool unlocked = SuperMechKnowledge.IsUnlocked(_actor, kid);
                int tier = def.tier;

                SMCubeNode node = _pool_nodes.getNext();
                Sprite icon = null;
                try { icon = SpriteTextureLoader.getSprite(def.icon); } catch { }
                node.setupNode(kid, tier, unlocked, icon);
                node.logical_pos = GetLogicalPos(index, tier, allKnowledge.Count);
                node.setColor(GetNodeColor(tier, unlocked));
                _nodes.Add(node);
                _nodes_by_index.Add(node);
                index++;
            }

            GenerateConnections();
            UpdateVisual();
        }

        private Vector4 GetLogicalPos(int index, int tier, int total)
        {
            float angle = (index / (float)total) * Mathf.PI * 2f;
            float radius = 0.3f + tier * 0.12f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            float z = Mathf.Sin(angle * 2f) * 0.2f;
            return new Vector4(x, y, z, 1f);
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
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    SMCubeNode a = _nodes[i];
                    SMCubeNode b = _nodes[j];
                    float dist = Vector4.Distance(a.logical_pos, b.logical_pos);
                    if (dist < 0.35f)
                    {
                        SMCubeNodeConnection conn = _pool_connections.getNext();
                        conn.node_1 = a;
                        conn.node_2 = b;
                        conn.setConnection(a.tier == b.tier);
                        a.addConnection(b, conn);
                        b.addConnection(a, conn);
                    }
                }
            }
        }

        private void Update()
        {
            if (!_initialized || _nodes.Count == 0) return;

            if (!_is_dragging)
            {
                _offset_target_x += Time.deltaTime * 0.05f;
            }
            _offset_x = Mathf.Lerp(_offset_x, _offset_target_x, Time.deltaTime * 2f);
            _offset_y = Mathf.Lerp(_offset_y, _offset_target_y, Time.deltaTime * 2f);

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            float cosX = Mathf.Cos(_offset_x);
            float sinX = Mathf.Sin(_offset_x);
            float cosY = Mathf.Cos(_offset_y);
            float sinY = Mathf.Sin(_offset_y);

            foreach (SMCubeNode node in _nodes)
            {
                Vector4 p = node.logical_pos;
                float x = p.x * cosX - p.z * sinX;
                float z = p.x * sinX + p.z * cosX;
                float y = p.y * cosY - z * sinY;
                float depth = z;

                node.render_depth = depth;
                float scale = Mathf.Lerp(0.6f, 1.3f, (depth + 1f) / 2f) * node.scale_mod_spawn;
                node.transform.localScale = new Vector3(scale, scale, 1f);

                RectTransform nodeRt = node.GetComponent<RectTransform>();
                float centerX = (GetComponent<RectTransform>().rect.width / 2f);
                float centerY = (GetComponent<RectTransform>().rect.height / 2f);
                nodeRt.anchoredPosition = new Vector2(centerX + x * 200f, centerY + y * 200f);
                nodeRt.SetSiblingIndex((int)((depth + 1f) * 1000f));
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
