using System;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public class SMKnowledgeGraph
    {
        private struct NodePos
        {
            public string id;
            public int tier;
            public int branch;
            public int index;
            public Rect local;
        }

        private struct Edge
        {
            public string from;
            public string to;
        }

        private NodePos[] _nodes;
        private Edge[] _edges;
        private Dictionary<string, NodePos> _posMap;
        private Rect _bounds;
        private Vector2 _pan;
        private float _zoom = 1f;
        private bool _fit = true;
        private string _selected;
        private string _hovered;
        private bool _dragging;
        private Vector2 _dragStart;
        private Vector2 _panStart;
        private int _controlId;
        private GUIStyle _nodeNameStyle;
        private GUIStyle _nodeDescStyle;
        private Texture2D _bgTex;
        private Texture2D _whiteTex;

        public string Selected => _selected;

        private void EnsureWhiteTex()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
        }

        private void DrawColoredRect(Rect rect, Color color)
        {
            EnsureWhiteTex();
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = old;
        }

        public void Reset()
        {
            _pan = Vector2.zero;
            _zoom = 1f;
            _fit = true;
            _selected = null;
            _hovered = null;
        }

        public void Focus(string id)
        {
            _selected = id;
            if (_nodes != null)
            {
                foreach (var n in _nodes)
                {
                    if (n.id == id)
                    {
                        _pan = -n.local.center;
                        _zoom = 0.8f;
                        break;
                    }
                }
            }
        }

        public void Build(string prefix, Actor a)
        {
            var all = SuperMechKnowledge.GetAllByPrefix(prefix);
            var list = new List<NodePos>();
            var edgeList = new List<Edge>();

            float nodeW = 160f, nodeH = 56f;
            float layerPitch = 90f;
            float branchPitch = 200f;
            float indexPitch = 175f;

            Dictionary<string, NodePos> posMap = new Dictionary<string, NodePos>();

            foreach (var def in all)
            {
                if (def.prefix != prefix) continue;
                float x = def.branch * branchPitch + def.index * indexPitch;
                float y = (4 - def.tier) * layerPitch;
                var np = new NodePos
                {
                    id = def.id,
                    tier = def.tier,
                    branch = def.branch,
                    index = def.index,
                    local = new Rect(x - nodeW / 2, y - nodeH / 2, nodeW, nodeH)
                };
                list.Add(np);
                posMap[def.id] = np;
            }

            foreach (var def in all)
            {
                if (def.prefix != prefix) continue;
                if (def.tier > 0)
                {
                    string parentId = $"sm_know_{prefix}_{def.tier - 1}_{def.branch}_{def.index}";
                    if (posMap.ContainsKey(parentId))
                    {
                        edgeList.Add(new Edge { from = parentId, to = def.id });
                    }
                    else
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            string altId = $"sm_know_{prefix}_{def.tier - 1}_{def.branch}_{i}";
                            if (posMap.ContainsKey(altId))
                            {
                                edgeList.Add(new Edge { from = altId, to = def.id });
                                break;
                            }
                        }
                    }
                }
            }

            _nodes = list.ToArray();
            _edges = edgeList.ToArray();
            _posMap = new Dictionary<string, NodePos>();
            foreach (var n in _nodes) _posMap[n.id] = n;

            if (_nodes.Length > 0)
            {
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var n in _nodes)
                {
                    x0 = Math.Min(x0, n.local.xMin);
                    y0 = Math.Min(y0, n.local.yMin);
                    x1 = Math.Max(x1, n.local.xMax);
                    y1 = Math.Max(y1, n.local.yMax);
                }
                _bounds = Rect.MinMaxRect(x0 - 40, y0 - 40, x1 + 40, y1 + 40);
            }
            else
            {
                _bounds = new Rect(0, 0, 400, 300);
            }

            _fit = true;
        }

        public void Draw(Actor a, float height)
        {
            EnsureTextures();
            EnsureStyles();

            Rect area = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Layout) return;

            int control = GUIUtility.GetControlID(FocusType.Passive);
            Vector2 mouse = Event.current.mousePosition - area.position;

            if (_fit && _nodes != null && _nodes.Length > 0)
            {
                _zoom = Mathf.Clamp(Math.Min(
                    (area.width - 40) / Math.Max(1, _bounds.width),
                    (area.height - 40) / Math.Max(1, _bounds.height)), 0.15f, 1f);
                _pan = area.size * 0.5f - _bounds.center * _zoom;
                _fit = false;
            }

            HandleInput(area, mouse, control);

            GUI.BeginGroup(area);
            try
            {
                if (Event.current.type != EventType.Repaint) return;

                Rect local = new Rect(0, 0, area.width, area.height);
                DrawBackground(local);

                if (_nodes == null || _nodes.Length == 0)
                {
                    GUI.Label(new Rect(local.width / 2 - 100, local.height / 2 - 10, 200, 20),
                        LocalizedTextManager.getText("sm_ui_no_knowledge"), _nodeNameStyle);
                    return;
                }

                DrawEdges(local, a);
                DrawNodes(local, a);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void HandleInput(Rect area, Vector2 mouse, int control)
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (area.Contains(e.mousePosition) && e.button == 0)
                    {
                        _controlId = control;
                        GUIUtility.hotControl = control;
                        _dragging = true;
                        _dragStart = mouse;
                        _panStart = _pan;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (_dragging && GUIUtility.hotControl == control)
                    {
                        _pan = _panStart + (mouse - _dragStart);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (_dragging && GUIUtility.hotControl == control)
                    {
                        _dragging = false;
                        GUIUtility.hotControl = 0;
                        if ((mouse - _dragStart).magnitude < 5f)
                        {
                            TrySelectNode(mouse);
                        }
                        e.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (area.Contains(e.mousePosition))
                    {
                        float factor = e.delta.y > 0 ? 0.9f : 1.1f;
                        float before = _zoom;
                        _zoom = Mathf.Clamp(_zoom * factor, 0.15f, 2f);
                        _pan = mouse + (_pan - mouse) * (_zoom / before);
                        e.Use();
                    }
                    break;
            }
        }

        private void TrySelectNode(Vector2 mouse)
        {
            if (_nodes == null) return;
            foreach (var n in _nodes)
            {
                Rect r = ScreenRect(n.local);
                if (r.Contains(mouse))
                {
                    _selected = n.id;
                    return;
                }
            }
            _selected = null;
        }

        private void DrawBackground(Rect local)
        {
            EnsureWhiteTex();
            Color old = GUI.color;
            GUI.color = new Color(0.94f, 0.96f, 0.99f, 0.95f);
            GUI.DrawTexture(local, _whiteTex);
            float pitch = 32f * Mathf.Max(0.5f, _zoom);
            GUI.color = new Color(0.75f, 0.82f, 0.9f, 0.35f);
            for (float x = _pan.x % pitch; x < local.width; x += pitch)
                GUI.DrawTexture(new Rect(x, 0, 1, local.height), _whiteTex);
            for (float y = _pan.y % pitch; y < local.height; y += pitch)
                GUI.DrawTexture(new Rect(0, y, local.width, 1), _whiteTex);
            GUI.color = old;
        }

        private void DrawEdges(Rect local, Actor a)
        {
            if (_edges == null || _posMap == null) return;

            foreach (var edge in _edges)
            {
                if (!_posMap.ContainsKey(edge.from) || !_posMap.ContainsKey(edge.to)) continue;
                NodePos from = _posMap[edge.from];
                NodePos to = _posMap[edge.to];

                bool fromUnlocked = SuperMechKnowledge.IsUnlocked(a, edge.from);
                bool toUnlocked = SuperMechKnowledge.IsUnlocked(a, edge.to);
                Color color = toUnlocked ? new Color(0.3f, 0.85f, 0.5f) :
                              fromUnlocked ? new Color(0.9f, 0.75f, 0.3f) :
                              new Color(0.3f, 0.35f, 0.45f);

                Vector2 p1 = ScreenPoint(new Vector2(from.local.center.x, from.local.yMax));
                Vector2 p2 = ScreenPoint(new Vector2(to.local.center.x, to.local.yMin));
                float midY = (p1.y + p2.y) * 0.5f;
                float lineWidth = 2f;

                DrawLine(p1, new Vector2(p1.x, midY), color, lineWidth);
                DrawLine(new Vector2(p1.x, midY), new Vector2(p2.x, midY), color, lineWidth);
                DrawLine(new Vector2(p2.x, midY), p2, color, lineWidth);
            }
        }

        private void DrawNodes(Rect local, Actor a)
        {
            if (_nodes == null) return;
            _hovered = null;

            foreach (var n in _nodes)
            {
                Rect r = ScreenRect(n.local);
                if (!local.Overlaps(r)) continue;

                bool unlocked = SuperMechKnowledge.IsUnlocked(a, n.id);
                bool isSelected = n.id == _selected;
                var def = SuperMechKnowledge.GetDef(n.id);
                if (def == null) continue;

                Color bgColor = unlocked ? new Color(0.78f, 0.9f, 0.82f) :
                                isSelected ? new Color(0.75f, 0.8f, 0.88f) :
                                new Color(0.82f, 0.85f, 0.9f);
                Color borderColor = unlocked ? new Color(0.3f, 0.85f, 0.5f) :
                                    isSelected ? new Color(0.9f, 0.75f, 0.3f) :
                                    new Color(0.65f, 0.7f, 0.78f);

                DrawColoredRect(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(0, 0, 0, 0.4f));
                DrawColoredRect(ExpandRect(r, isSelected ? 2 : 1), borderColor);
                DrawColoredRect(r, bgColor);
                DrawColoredRect(new Rect(r.x, r.y, 3, r.height), borderColor);

                if (_zoom >= 0.4f)
                {
                    string name = LocalizedTextManager.getText(def.name);
                    if (name.Length > 8) name = name.Substring(0, 8) + "..";
                    GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, 20), name, _nodeNameStyle);

                    if (_zoom >= 0.7f)
                    {
                        string status = unlocked
                            ? LocalizedTextManager.getText("sm_ui_unlocked")
                            : string.Format(LocalizedTextManager.getText("sm_ui_cost_potential"), def.cost);
                        GUI.Label(new Rect(r.x + 8, r.y + 28, r.width - 16, 16), status, _nodeDescStyle);
                    }
                }
            }
        }

        private Rect ScreenRect(Rect local)
        {
            return new Rect(
                local.x * _zoom + _pan.x,
                local.y * _zoom + _pan.y,
                local.width * _zoom,
                local.height * _zoom);
        }

        private Vector2 ScreenPoint(Vector2 local)
        {
            return new Vector2(local.x * _zoom + _pan.x, local.y * _zoom + _pan.y);
        }

        private Rect ExpandRect(Rect r, float amount)
        {
            return new Rect(r.x - amount, r.y - amount, r.width + amount * 2, r.height + amount * 2);
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 d = to - from;
            float len = d.magnitude;
            if (len < 0.1f) return;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, from);
            DrawColoredRect(new Rect(from.x, from.y - width / 2, len, width), color);
            GUI.matrix = matrix;
        }

        private void EnsureTextures()
        {
            EnsureWhiteTex();
        }

        private void EnsureStyles()
        {
            if (_nodeNameStyle == null)
            {
                _nodeNameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.9f, 0.92f, 0.95f) },
                    alignment = TextAnchor.UpperLeft
                };
            }
            if (_nodeDescStyle == null)
            {
                _nodeDescStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    normal = { textColor = new Color(0.6f, 0.65f, 0.7f) },
                    alignment = TextAnchor.UpperLeft
                };
            }
        }
    }
}
