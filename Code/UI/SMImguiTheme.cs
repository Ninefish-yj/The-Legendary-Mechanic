using UnityEngine;

namespace SuperMech.Code
{
    internal static class SMImguiTheme
    {
        private static bool _ready;
        private static Texture2D _pixel;

        public static readonly Color Background = new Color(0.03f, 0.04f, 0.07f, 0.95f);
        public static readonly Color Panel = new Color(0.07f, 0.09f, 0.15f, 1f);
        public static readonly Color PanelRaised = new Color(0.09f, 0.13f, 0.21f, 1f);
        public static readonly Color Border = new Color(0.2f, 0.26f, 0.35f, 1f);
        public static readonly Color Gold = new Color(0.91f, 0.76f, 0.42f, 1f);
        public static readonly Color Cyan = new Color(0.29f, 0.84f, 0.89f, 1f);
        public static readonly Color Teal = new Color(0.3f, 0.71f, 0.67f, 1f);
        public static readonly Color Success = new Color(0.4f, 0.82f, 0.56f, 1f);
        public static readonly Color Warning = new Color(0.91f, 0.64f, 0.29f, 1f);
        public static readonly Color Danger = new Color(0.89f, 0.36f, 0.42f, 1f);
        public static readonly Color Text = new Color(0.91f, 0.93f, 0.96f, 1f);
        public static readonly Color Muted = new Color(0.58f, 0.64f, 0.74f, 1f);

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
        public static GUISkin RootSkin { get; private set; }

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            EnsurePixel();

            Label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Text },
                richText = true
            };
            WrappedLabel = new GUIStyle(Label) { wordWrap = true };
            Small = new GUIStyle(Label) { fontSize = 11, normal = { textColor = Muted } };
            Title = new GUIStyle(Label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Gold } };
            Section = new GUIStyle(Label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Cyan } };

            PanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeFrame(Panel, Border) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 8, 8)
            };
            RaisedPanelStyle = new GUIStyle(PanelStyle)
            {
                normal = { background = MakeFrame(PanelRaised, Border) }
            };

            WindowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = MakeFrame(Background, Border) },
                active = { background = MakeFrame(Background, Gold) },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(10, 10, 32, 10),
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Gold },
                alignment = TextAnchor.UpperCenter
            };

            Button = new GUIStyle(GUI.skin.button)
            {
                normal = { background = MakeFrame(PanelRaised, Border), textColor = Text },
                hover = { background = MakeFrame(new Color(0.12f, 0.17f, 0.27f, 1f), Gold), textColor = Gold },
                active = { background = MakeFrame(new Color(0.15f, 0.2f, 0.32f, 1f), Cyan), textColor = Cyan },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(8, 8, 5, 5),
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            PrimaryButton = new GUIStyle(Button)
            {
                normal = { background = MakeFrame(new Color(0.15f, 0.3f, 0.45f, 1f), Cyan), textColor = Cyan },
                hover = { background = MakeFrame(new Color(0.2f, 0.4f, 0.55f, 1f), Gold), textColor = Gold }
            };

            Tab = new GUIStyle(Button)
            {
                fontSize = 12,
                padding = new RectOffset(6, 6, 4, 4)
            };
            TabActive = new GUIStyle(Tab)
            {
                normal = { background = MakeFrame(new Color(0.15f, 0.3f, 0.45f, 1f), Gold), textColor = Gold }
            };

            Input = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = MakeFrame(new Color(0.02f, 0.03f, 0.06f, 1f), Border), textColor = Text },
                focused = { background = MakeFrame(new Color(0.02f, 0.03f, 0.06f, 1f), Cyan), textColor = Text },
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
            RootSkin.verticalScrollbar.normal.background = MakeFrame(Background, Border);
            RootSkin.verticalScrollbar.border = new RectOffset(1, 1, 1, 1);
            RootSkin.verticalScrollbarThumb.normal.background = MakeFrame(Border, Muted);
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

        public static Texture2D MakeFrame(Color fill, Color edge)
        {
            var tex = new Texture2D(3, 3, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    tex.SetPixel(x, y, (x == 1 && y == 1) ? fill : edge);
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

        public static float Px(float value)
        {
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            return value * Mathf.Max(0.7f, Mathf.Min(1.3f, scale));
        }
    }
}
