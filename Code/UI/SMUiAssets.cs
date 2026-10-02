using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// UI资源加载：面板背景精灵
    /// NML自动加载GameResources目录到Unity Resources系统，用Resources.Load访问
    /// 9-slice border=42,50,42,50
    /// </summary>
    internal static class SMUiAssets
    {
        private static Sprite _panelSprite;
        private static bool _triedLoad;

        /// <summary>
        /// 获取面板背景精灵（9-slice，border=42,50,42,50）
        /// </summary>
        public static Sprite GetPanelSprite()
        {
            if (_triedLoad) return _panelSprite;
            _triedLoad = true;

            // 标准方式：NML自动加载GameResources到Resources系统
            _panelSprite = Resources.Load<Sprite>("ui_panel");

            if (_panelSprite == null)
            {
                Debug.LogWarning("[超神机械师] Resources.Load<Sprite>(ui_panel)失败，尝试文件IO兜底");
                _panelSprite = LoadFromFileFallback();
            }

            return _panelSprite;
        }

        /// <summary>
        /// 文件IO兜底加载（Resources.Load失败时使用）
        /// </summary>
        private static Sprite LoadFromFileFallback()
        {
            try
            {
                string path = System.IO.Path.Combine(Main.ModPath, "GameResources", "ui_panel.png");
                if (!System.IO.File.Exists(path)) return null;

                byte[] data = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);
                tex.wrapMode = TextureWrapMode.Clamp;

                return Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(42, 50, 42, 50));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 文件IO加载UI面板失败: " + e.Message);
                return null;
            }
        }
    }
}
