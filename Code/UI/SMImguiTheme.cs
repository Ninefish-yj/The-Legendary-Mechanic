using UnityEngine;

namespace SuperMech.Code
{
    internal static class SMImguiTheme
    {
        private static bool _ready;
        private static Texture2D _pixel;
        private static Texture2D _scanline;
        private static Texture2D _starfield;
        private static Texture2D _glowCircle;
        private static Texture2D _gradientPanel;
        private static Texture2D _windowBg;
        private static Texture2D _panelTex;

        public static readonly RectOffset ContentPadding = new RectOffset(42, 42, 56, 50);

        public static readonly Color Background = new Color(0.88f, 0.91f, 0.95f, 0.88f);
        public static readonly Color Panel = new Color(0.93f, 0.95f, 0.98f, 0.75f);
        public static readonly Color PanelRaised = new Color(0.97f, 0.98f, 1f, 0.85f);
        public static readonly Color PanelDark = new Color(0.78f, 0.82f, 0.88f, 0.6f);
        public static readonly Color Silver = new Color(0.6f, 0.65f, 0.72f, 1f);
        public static readonly Color SilverDim = new Color(0.45f, 0.5f, 0.58f, 1f);
        public static readonly Color Border = new Color(0.6f, 0.65f, 0.72f, 0.8f);
        public static readonly Color BorderGlow = new Color(0.4f, 0.65f, 0.9f, 1f);
        public static readonly Color Gold = new Color(0.8f, 0.65f, 0.3f, 1f);
        public static readonly Color Cyan = new Color(0.3f, 0.6f, 0.85f, 1f);
        public static readonly Color Teal = new Color(0.35f, 0.55f, 0.7f, 1f);
        public static readonly Color Blue = new Color(0.25f, 0.4f, 0.65f, 1f);
        public static readonly Color Purple = new Color(0.55f, 0.35f, 0.75f, 1f);
        public static readonly Color Success = new Color(0.15f, 0.55f, 0.35f, 1f);
        public static readonly Color Warning = new Color(0.75f, 0.5f, 0.15f, 1f);
        public static readonly Color Danger = new Color(0.75f, 0.25f, 0.3f, 1f);
        public static readonly Color Text = new Color(0.1f, 0.15f, 0.25f, 1f);
        public static readonly Color TextDim = new Color(0.3f, 0.35f, 0.45f, 1f);
        public static readonly Color Muted = new Color(0.45f, 0.5f, 0.6f, 1f);

        public static GUIStyle Label { get; private set; }
        public static GUIStyle WrappedLabel { get; private set; }
        public static GUIStyle Small { get; private set; }
        public static GUIStyle Title { get; private set; }
        public static GUIStyle Section { get; private set; }
        public static GUIStyle PanelStyle { get; private set; }
        public static GUIStyle RaisedPanelStyle { get; private set; }
        public static GUIStyle WindowStyle { get; private set; }
        public static GUIStyle Button { get; private set; }
        public static GUIStyle PrimaryButton { get; private set; }
        public static GUIStyle Tab { get; private set; }
        public static GUIStyle TabActive { get; private set; }
        public static GUIStyle Input { get; private set; }
        public static GUIStyle DataLabel { get; private set; }
        public static GUIStyle DataValue { get; private set; }
        public static GUISkin RootSkin { get; private set; }

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            EnsurePixel();
            EnsureScanline();
            EnsureStarfield();
            EnsureGlowCircle();
            EnsureGradientPanel();

            Label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Text },
                richText = true
            };
            WrappedLabel = new GUIStyle(Label) { wordWrap = true };
            Small = new GUIStyle(Label) { fontSize = 11, normal = { textColor = TextDim } };
            Title = new GUIStyle(Label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Text },
                alignment = TextAnchor.MiddleCenter
            };
            Section = new GUIStyle(Label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Text }
            };
            DataLabel = new GUIStyle(Label)
            {
                fontSize = 12,
                normal = { textColor = TextDim },
                alignment = TextAnchor.MiddleLeft
            };
            DataValue = new GUIStyle(Label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Purple },
                alignment = TextAnchor.MiddleRight
            };

            PanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeFrame(Panel, Border) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(10, 10, 8, 8)
            };
            RaisedPanelStyle = new GUIStyle(PanelStyle)
            {
                normal = { background = MakeFrame(PanelRaised, BorderGlow) }
            };

            WindowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = null, textColor = Text },
                active = { background = null, textColor = Text },
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(42, 42, 0, 50),
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            Button = new GUIStyle(GUI.skin.button)
            {
                normal = { background = MakeFrame(PanelDark, Border), textColor = Text },
                hover = { background = MakeFrame(PanelRaised, BorderGlow), textColor = Text },
                active = { background = MakeFrame(new Color(0.85f, 0.82f, 0.9f, 1f), Purple), textColor = Text },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(8, 8, 5, 5),
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            PrimaryButton = new GUIStyle(Button)
            {
                normal = { background = MakeFrame(new Color(0.82f, 0.78f, 0.9f, 1f), BorderGlow), textColor = Text },
                hover = { background = MakeFrame(new Color(0.9f, 0.88f, 0.95f, 1f), Purple), textColor = Purple }
            };

            Tab = new GUIStyle(Button)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 5, 5),
                normal = { background = MakeFrame(PanelDark, Border), textColor = TextDim }
            };
            TabActive = new GUIStyle(Tab)
            {
                normal = { background = MakeFrame(new Color(0.85f, 0.82f, 0.9f, 1f), BorderGlow), textColor = Text }
            };

            Input = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = MakeFrame(PanelDark, Border), textColor = Text },
                focused = { background = MakeFrame(PanelDark, Cyan), textColor = Text },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(6, 6, 4, 4),
                fontSize = 13
            };
        }

        public static void EnsureRootSkin()
        {
            Ensure();
            if (RootSkin != null) return;
            RootSkin = Object.Instantiate(GUI.skin);
            RootSkin.hideFlags = HideFlags.HideAndDontSave;
            RootSkin.label = new GUIStyle(Label) { wordWrap = true };
            RootSkin.button = new GUIStyle(Button);
            RootSkin.box = new GUIStyle(PanelStyle);
            RootSkin.textField = new GUIStyle(Input);
            RootSkin.window = new GUIStyle(WindowStyle);
            RootSkin.verticalScrollbar.normal.background = MakeFrame(PanelDark, Border);
            RootSkin.verticalScrollbar.border = new RectOffset(1, 1, 1, 1);
            RootSkin.verticalScrollbarThumb.normal.background = MakeFrame(Border, Cyan);
            RootSkin.verticalScrollbarThumb.border = new RectOffset(1, 1, 1, 1);
        }

        private static void EnsurePixel()
        {
            if (_pixel != null) return;
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _pixel.hideFlags = HideFlags.HideAndDontSave;
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }

        private static void EnsureScanline()
        {
            if (_scanline != null) return;
            _scanline = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            _scanline.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < 4; y++)
            {
                _scanline.SetPixel(0, y, y == 0 ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0));
            }
            _scanline.Apply();
            _scanline.wrapMode = TextureWrapMode.Repeat;
        }

        private static void EnsureStarfield()
        {
            if (_starfield != null) return;
            _starfield = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            _starfield.hideFlags = HideFlags.HideAndDontSave;
            var rng = new System.Random(42);
            for (int x = 0; x < 256; x++)
                for (int y = 0; y < 256; y++)
                {
                    float v = (float)rng.NextDouble();
                    if (v > 0.985f) _starfield.SetPixel(x, y, new Color(1f, 1f, 1f, 0.8f));
                    else if (v > 0.97f) _starfield.SetPixel(x, y, new Color(0.7f, 0.85f, 1f, 0.5f));
                    else _starfield.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            _starfield.Apply();
            _starfield.wrapMode = TextureWrapMode.Repeat;
        }

        private static void EnsureGlowCircle()
        {
            if (_glowCircle != null) return;
            int size = 64;
            _glowCircle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _glowCircle.hideFlags = HideFlags.HideAndDontSave;
            float cx = size / 2f, cy = size / 2f;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    _glowCircle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            _glowCircle.Apply();
        }

        private static void EnsureGradientPanel()
        {
            if (_gradientPanel != null) return;
            int w = 4, h = 64;
            _gradientPanel = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _gradientPanel.hideFlags = HideFlags.HideAndDontSave;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float t = (float)y / h;
                    float r = Mathf.Lerp(0.95f, 0.82f, t);
                    float g = Mathf.Lerp(0.96f, 0.85f, t);
                    float b = Mathf.Lerp(0.98f, 0.90f, t);
                    _gradientPanel.SetPixel(x, y, new Color(r, g, b, 0.6f));
                }
            _gradientPanel.Apply();
            _gradientPanel.wrapMode = TextureWrapMode.Clamp;
        }

        public static Texture2D MakeFrame(Color fill, Color edge)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    bool isEdge = x == 0 || y == 0 || x == 3 || y == 3;
                    bool isCorner = (x == 0 || x == 3) && (y == 0 || y == 3);
                    tex.SetPixel(x, y, isCorner ? new Color(edge.r, edge.g, edge.b, edge.a * 0.5f) : isEdge ? edge : fill);
                }
            tex.Apply();
            return tex;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            EnsurePixel();
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = old;
        }

        public static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static void DrawCorners(Rect rect, Color color, float size = 8f)
        {
            DrawRect(new Rect(rect.x, rect.y, size, 2f), color);
            DrawRect(new Rect(rect.x, rect.y, 2f, size), color);
            DrawRect(new Rect(rect.xMax - size, rect.y, size, 2f), color);
            DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, size), color);
            DrawRect(new Rect(rect.x, rect.yMax - 2f, size, 2f), color);
            DrawRect(new Rect(rect.x, rect.yMax - size, 2f, size), color);
            DrawRect(new Rect(rect.xMax - size, rect.yMax - 2f, size, 2f), color);
            DrawRect(new Rect(rect.xMax - 2f, rect.yMax - size, 2f, size), color);
        }

        public static void DrawScanline(Rect rect)
        {
            if (_scanline == null) EnsureScanline();
            Color old = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTextureWithTexCoords(rect, _scanline, new Rect(0, 0, 1, rect.height / 4f));
            GUI.color = old;
        }

        public static void DrawDataRow(string label, string value, float width, ref float y, float x = 0f)
        {
            GUI.Label(new Rect(x + 8f, y, width * 0.5f, 20f), label, DataLabel);
            GUI.Label(new Rect(x + width * 0.5f, y, width * 0.5f - 8f, 20f), value, DataValue);
            y += 22f;
        }

        public static float Px(float value)
        {
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            return value * Mathf.Max(0.7f, Mathf.Min(1.3f, scale));
        }

        public static void DrawStarfield(Rect rect)
        {
            EnsureStarfield();
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.DrawTextureWithTexCoords(rect, _starfield, new Rect(0, 0, rect.width / 256f, rect.height / 256f));
            GUI.color = old;
        }

        public static void DrawGlowBorder(Rect rect, Color color, float glowSize = 6f)
        {
            EnsureGlowCircle();
            Color old = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * 0.3f);
            DrawBorder(new Rect(rect.x - glowSize, rect.y - glowSize, rect.width + glowSize * 2, rect.height + glowSize * 2), color, glowSize);
            GUI.color = new Color(color.r, color.g, color.b, color.a * 0.6f);
            DrawBorder(new Rect(rect.x - glowSize / 2, rect.y - glowSize / 2, rect.width + glowSize, rect.height + glowSize), color, glowSize / 2);
            GUI.color = color;
            DrawBorder(rect, color, 1.5f);
            GUI.color = old;
        }

        public static void DrawGlowCircle(Rect rect, Color color)
        {
            EnsureGlowCircle();
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _glowCircle);
            GUI.color = old;
        }

        public static void DrawGradientPanel(Rect rect)
        {
            EnsureGradientPanel();
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _gradientPanel);
            GUI.color = old;
        }

        private static void EnsureWindowBackground()
        {
            if (_windowBg != null) return;
            int w = 512, h = 512;
            _windowBg = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var rng = new System.Random(42);
            var pixels = new Color[w * h];
            int corner = 22;
            int borderOuter = 10;
            int borderMid = 7;
            int borderInner = 4;
            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float tx = (float)x / w;
                    int idx = y * w + x;
                    int dx = Mathf.Min(x, w - 1 - x);
                    int dy = Mathf.Min(y, h - 1 - y);
                    bool inCorner = false;
                    int cornerDist = 0;
                    if (x < corner && y < corner) { inCorner = true; cornerDist = (corner - x) + (corner - y); }
                    else if (x >= w - corner && y < corner) { inCorner = true; cornerDist = (x - (w - corner - 1)) + (corner - y); }
                    else if (x < corner && y >= h - corner) { inCorner = true; cornerDist = (corner - x) + (y - (h - corner - 1)); }
                    else if (x >= w - corner && y >= h - corner) { inCorner = true; cornerDist = (x - (w - corner - 1)) + (y - (h - corner - 1)); }
                    if (inCorner && cornerDist < corner) { pixels[idx] = new Color(0, 0, 0, 0); continue; }
                    float r = Mathf.Lerp(0.96f, 0.82f, ty);
                    float g = Mathf.Lerp(0.97f, 0.86f, ty);
                    float b = Mathf.Lerp(1.00f, 0.93f, ty);
                    float a = 0.92f;
                    int distToEdge = Mathf.Min(dx, dy);
                    if (inCorner) distToEdge = Mathf.Min(distToEdge, cornerDist - corner);
                    if (distToEdge < borderOuter)
                    {
                        float t = 1f - (float)distToEdge / borderOuter;
                        r = Mathf.Lerp(r, 0.50f, t * 0.5f);
                        g = Mathf.Lerp(g, 0.75f, t * 0.5f);
                        b = Mathf.Lerp(b, 0.98f, t * 0.5f);
                    }
                    if (distToEdge >= borderMid && distToEdge < borderMid + 2)
                    {
                        r = Mathf.Lerp(r, 0.85f, 0.6f);
                        g = Mathf.Lerp(g, 0.88f, 0.6f);
                        b = Mathf.Lerp(b, 0.95f, 0.6f);
                    }
                    if (distToEdge >= borderInner && distToEdge < borderInner + 1)
                    {
                        r = Mathf.Lerp(r, 0.40f, 0.7f);
                        g = Mathf.Lerp(g, 0.65f, 0.7f);
                        b = Mathf.Lerp(b, 0.95f, 0.7f);
                    }
                    float star = 0f;
                    if (distToEdge > 20 && rng.NextDouble() > 0.997f) star = (float)rng.NextDouble() * 0.5f;
                    float scan = (y % 5 == 0 && distToEdge > 15) ? 0.025f : 0f;
                    if ((x == 14 || x == w - 15) && y > 30 && y < h - 30 && (y % 12 < 3))
                    {
                        r = Mathf.Lerp(r, 0.5f, 0.4f);
                        g = Mathf.Lerp(g, 0.7f, 0.4f);
                        b = Mathf.Lerp(b, 0.9f, 0.4f);
                    }
                    if ((y == 14 || y == h - 15) && x > 30 && x < w - 30 && (x % 12 < 3))
                    {
                        r = Mathf.Lerp(r, 0.5f, 0.4f);
                        g = Mathf.Lerp(g, 0.7f, 0.4f);
                        b = Mathf.Lerp(b, 0.9f, 0.4f);
                    }
                    pixels[idx] = new Color(
                        Mathf.Clamp01(r + star),
                        Mathf.Clamp01(g + star),
                        Mathf.Clamp01(b + star),
                        Mathf.Clamp01(a - scan));
                }
            }
            _windowBg.SetPixels(pixels);
            _windowBg.Apply();
        }

        private static void EnsurePanelTexture()
        {
            if (_panelTex != null) return;
            try
            {
                string path = System.IO.Path.Combine(Main.ModPath, "GameResources", "ui_panel.png");
                if (System.IO.File.Exists(path))
                {
                    byte[] data = System.IO.File.ReadAllBytes(path);
                    _panelTex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        filterMode = FilterMode.Bilinear
                    };
                    _panelTex.LoadImage(data);
                }
            }
            catch { }
        }

        private static readonly float[] _sliceUv = { 0.047f, 0.955f, 0.068f, 0.939f };
        private static readonly int[] _slicePx = { 42, 42, 50, 50 };

        private static void DrawTexture9Slice(Rect rect, Texture2D tex)
        {
            float l = _slicePx[0], r = _slicePx[1], t = _slicePx[2], b = _slicePx[3];
            float u0 = 0, u1 = _sliceUv[0], u2 = _sliceUv[1], u3 = 1f;
            float v0 = 0, v1 = _sliceUv[2], v2 = _sliceUv[3], v3 = 1f;
            float x0 = rect.x, x1 = rect.x + l, x2 = rect.xMax - r, x3 = rect.xMax;
            float y0 = rect.y, y1 = rect.y + t, y2 = rect.yMax - b, y3 = rect.yMax;
            var cols = new[] { x0, x1, x2, x3 };
            var rows = new[] { y0, y1, y2, y3 };
            var uvs = new[] { u0, u1, u2, u3 };
            var vvs = new[] { v0, v1, v2, v3 };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    var dst = new Rect(cols[i], rows[j], cols[i + 1] - cols[i], rows[j + 1] - rows[j]);
                    if (dst.width <= 0 || dst.height <= 0) continue;
                    var src = new Rect(uvs[i], 1f - vvs[j + 1], uvs[i + 1] - uvs[i], vvs[j + 1] - vvs[j]);
                    GUI.DrawTextureWithTexCoords(dst, tex, src);
                }
        }

        public static void DrawWindowBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsurePanelTexture();
            if (_panelTex != null)
            {
                Color old = GUI.color;
                GUI.color = Color.white;
                DrawTexture9Slice(rect, _panelTex);
                GUI.color = old;
            }
            else
            {
                EnsureWindowBackground();
                Color old = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(rect, _windowBg);
                GUI.color = old;
            }
            DrawRect(new Rect(rect.x + 16, rect.y + 42, rect.width - 32, 1.5f), new Color(0.4f, 0.65f, 0.9f, 0.5f));
        }

        public static bool DrawBackdrop(Rect windowRect, float alpha = 0.45f)
        {
            EnsurePixel();
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _pixel);
            GUI.color = old;
            return GUI.Button(new Rect(0, 0, Screen.width, Screen.height), "", GUIStyle.none);
        }
    }
}
