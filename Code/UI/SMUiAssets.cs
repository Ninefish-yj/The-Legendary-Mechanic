using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// UI资源加载：面板背景精灵
    /// NML自动加载GameResources目录到Unity Resources系统，用Resources.Load访问
    /// 9-slice border=42,50,42,50（Resources.Load的Sprite没有border，需要重新创建）
    /// </summary>
    internal static class SMUiAssets
    {
        private static Sprite _panelSprite;
        private static bool _triedLoad;

        // 9-slice border：左42, 下50, 右42, 上50（对应Sprite.Create的Vector4(x,y,z,w)）
        private static readonly Vector4 PanelBorder = new Vector4(42f, 50f, 42f, 50f);

        /// <summary>
        /// 获取面板背景精灵（9-slice，border=42,50,42,50）
        /// </summary>
        public static Sprite GetPanelSprite()
        {
            if (_triedLoad) return _panelSprite;
            _triedLoad = true;

            // 标准方式：NML自动加载GameResources到Resources系统
            var loaded = Resources.Load<Sprite>("ui_panel");
            if (loaded != null)
            {
                // Resources.Load的Sprite没有9-slice border，重新创建带border的Sprite
                _panelSprite = Sprite.Create(
                    loaded.texture,
                    new Rect(0, 0, loaded.texture.width, loaded.texture.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    PanelBorder);
                return _panelSprite;
            }

            Debug.LogWarning("[超神机械师] Resources.Load<Sprite>(ui_panel)失败，尝试文件IO兜底");
            _panelSprite = LoadFromFileFallback();
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
                    PanelBorder);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 文件IO加载UI面板失败: " + e.Message);
                return null;
            }
        }
    }
}
