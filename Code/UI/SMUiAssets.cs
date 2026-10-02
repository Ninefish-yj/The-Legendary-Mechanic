using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// UI资源加载：面板背景纹理和9-slice精灵
    /// 从GameResources/ui_panel.png加载，供UGUI窗口和按钮使用
    /// </summary>
    internal static class SMUiAssets
    {
        private static Texture2D _panelTex;
        private static Sprite _panelSprite;
        private static bool _triedLoad;

        /// <summary>
        /// 获取面板背景精灵（9-slice，border=42,50,42,50）
        /// </summary>
        public static Sprite GetPanelSprite()
        {
            EnsurePanelTexture();
            if (_panelSprite == null && _panelTex != null)
            {
                _panelSprite = Sprite.Create(_panelTex,
                    new Rect(0, 0, _panelTex.width, _panelTex.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(42, 50, 42, 50));
            }
            return _panelSprite;
        }

        private static void EnsurePanelTexture()
        {
            if (_triedLoad) return;
            _triedLoad = true;

            try
            {
                string path = System.IO.Path.Combine(Main.ModPath, "GameResources", "ui_panel.png");
                if (System.IO.File.Exists(path))
                {
                    byte[] data = System.IO.File.ReadAllBytes(path);
                    _panelTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _panelTex.LoadImage(data);
                    _panelTex.wrapMode = TextureWrapMode.Clamp;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 加载UI面板纹理失败: " + e.Message);
            }
        }
    }
}
