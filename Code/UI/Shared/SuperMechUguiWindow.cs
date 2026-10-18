using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NeoModLoader.General;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.233.1 改用原版 WindowCreator 创建窗口，自带原版石质背景、标题栏、关闭按钮、拖拽和窗口层级管理。
    /// 公共接口（WindowId/Rect/Content/IsOpen/Create/Close）保持不变，外部调用无需修改。
    /// </summary>
    public class SuperMechUguiWindow : MonoBehaviour
    {
        public string WindowId { get; private set; }
        public RectTransform Rect { get; private set; }
        public RectTransform Content { get; private set; }
        public bool IsOpen { get; private set; }

        private ScrollWindow _scrollWindow;

        private const float MobileScreenRatio = 0.92f;
        private static bool IsMobile => Application.isMobilePlatform;

        private static Canvas _canvas;

        /// <summary>
        /// 供全屏UI（星图/银河地图/圣所/战报等）使用的高层级Canvas。
        /// 弹窗窗口不再使用此Canvas，改用原版 WindowCreator.CreateEmptyWindow。
        /// </summary>
        public static Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            var go = new GameObject("SM_Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            go.AddComponent<GraphicRaycaster>();
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            DontDestroyOnLoad(go);
            return _canvas;
        }

        /// <summary>已创建窗口的缓存（原版窗口设计为创建一次、show/hide复用，不反复销毁）</summary>
        private static readonly Dictionary<string, SuperMechUguiWindow> _windowCache = new Dictionary<string, SuperMechUguiWindow>();

        public static SuperMechUguiWindow Create(string id, string titleKey, float width, float height)
        {
            // 手机端适配：窗口尺寸不超过屏幕的92%
            if (IsMobile)
            {
                float maxW = Screen.width * MobileScreenRatio;
                float maxH = Screen.height * MobileScreenRatio;
                width = Mathf.Min(width, maxW);
                height = Mathf.Min(height, maxH);
            }

            // 复用已创建的窗口
            if (_windowCache.TryGetValue(id, out var existing) && existing != null && existing._scrollWindow != null)
            {
                // 清除旧内容
                if (existing.Content != null)
                {
                    foreach (Transform child in existing.Content)
                        Destroy(child.gameObject);
                }
                // v0.242.14 修复：复用窗口时必须重设anchor/pivot再设sizeDelta。
                // 旧版本缓存的窗口anchor可能是拉伸模式(0,0)-(1,1)，此时sizeDelta只改偏移量
                // 不改实际大小，导致窗口保持全屏。和新建窗口逻辑一致：先anchor居中，再sizeDelta。
                if (existing.Rect != null)
                {
                    existing.Rect.anchorMin = new Vector2(0.5f, 0.5f);
                    existing.Rect.anchorMax = new Vector2(0.5f, 0.5f);
                    existing.Rect.pivot = new Vector2(0.5f, 0.5f);
                    existing.Rect.sizeDelta = new Vector2(width, height);
                    existing.Rect.anchoredPosition = Vector2.zero;
                }
                // v0.239.0 缓存复用窗口也执行红叉归位（旧版创建的窗口红叉停在标题栏中间，复用时不重建不会重跑Create里的FixCloseButton）
                if (existing.Rect != null)
                    SuperMechResizeHandle.FixCloseButton(existing.Rect);
                // v0.240.3 缓存复用也执行标题居中（旧版窗口标题偏右）
                try
                {
                    Transform titleTf = ((Component)existing._scrollWindow).transform.Find("Background/Title");
                    if (titleTf != null)
                    {
                        RectTransform titleRect = titleTf.GetComponent<RectTransform>();
                        if (titleRect != null)
                        {
                            titleRect.anchorMin = new Vector2(0f, 1f);
                            titleRect.anchorMax = new Vector2(1f, 1f);
                            titleRect.pivot = new Vector2(0.5f, 1f);
                            titleRect.anchoredPosition = Vector2.zero;
                            titleRect.offsetMin = new Vector2(40f, -26f);  // v0.242.0 标题往上移
                            titleRect.offsetMax = new Vector2(-40f, -2f);
                        }
                        Text titleText = titleTf.GetComponent<Text>();
                        if (titleText != null) titleText.alignment = TextAnchor.MiddleCenter;
                    }
                }
                catch (System.Exception)
                {
                    // 缓存复用窗口时标题栏调整失败不影响功能，静默忽略
                }
                existing.IsOpen = true;
                ScrollWindow.showWindow(id);
                return existing;
            }

            // 用原版窗口创建器（自带原版背景纹理、标题栏、关闭按钮、拖拽）
            ScrollWindow sw = WindowCreator.CreateEmptyWindow(id, titleKey, "");
            if (sw == null)
            {
                Debug.LogError("[超神机械师] 原版窗口创建失败: " + id);
                return null;
            }

            Transform background = ((Component)sw).transform.Find("Background");
            if (background == null)
            {
                Debug.LogError("[超神机械师] 原版窗口缺少Background子物体: " + id);
                return null;
            }

            RectTransform bgRect = background.GetComponent<RectTransform>();

            // v0.242.6 关键修复：必须先设anchor/pivot，再设sizeDelta。
            // 原版CreateEmptyWindow的Background默认是拉伸锚点(0,0)-(1,1)，
            // 如果先设sizeDelta再改anchor为居中(0.5,0.5)，Unity会自动重算sizeDelta
            // 保持视觉大小不变，导致我们设的578x425被覆盖成全屏大小。
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(width, height);
            bgRect.anchoredPosition = Vector2.zero;

            // 创建内容区（在Background下，标题栏下方，四周留边距）
            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(background, false);
            RectTransform contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(8f, 8f);
            contentRect.offsetMax = new Vector2(-8f, -36f); // 顶部留出原版标题栏

            // 把包装组件挂在原版窗口上，Close时Destroy会一起销毁
            GameObject swGo = ((Component)sw).gameObject;
            SuperMechUguiWindow wrapper = swGo.AddComponent<SuperMechUguiWindow>();
            wrapper.WindowId = id;
            wrapper.Rect = bgRect;
            wrapper.Content = contentRect;
            wrapper._scrollWindow = sw;
            wrapper.IsOpen = true;

            _windowCache[id] = wrapper;

            // v0.237.7 PC端加右下角拖拽缩放手柄（手机端屏幕小、已有92%适配，不启用）
            if (!Application.isMobilePlatform)
                SuperMechResizeHandle.Add(bgRect);

            // v0.237.9 所有平台：窗口创建后立即把关闭按钮钉到右上角
            // （原版空窗口预制体的关闭按钮锚点不在右上角，窗口一宽它就漂在标题栏中间——用户截图确认）
            SuperMechResizeHandle.FixCloseButton(bgRect);

            // v0.240.3 标题居中对齐帝国盒子：原版空窗口标题默认偏右（紧挨关闭按钮），
            // 帝国盒子标题是顶部居中。找到Background/Title强制居中。
            try
            {
                Transform titleTf = ((Component)sw).transform.Find("Background/Title");
                if (titleTf != null)
                {
                    RectTransform titleRect = titleTf.GetComponent<RectTransform>();
                    if (titleRect != null)
                    {
                        // v0.242.0 标题往上移进入标题栏专门区域（顶部2~26px），对齐帝国盒子
                        titleRect.anchorMin = new Vector2(0f, 1f);
                        titleRect.anchorMax = new Vector2(1f, 1f);
                        titleRect.pivot = new Vector2(0.5f, 1f);
                        titleRect.anchoredPosition = Vector2.zero;
                        titleRect.offsetMin = new Vector2(40f, -26f);
                        titleRect.offsetMax = new Vector2(-40f, -2f);
                    }
                    Text titleText = titleTf.GetComponent<Text>();
                    if (titleText != null)
                    {
                        titleText.alignment = TextAnchor.MiddleCenter;
                    }
                }
            }
            catch (System.Exception)
            {
                // 标题调整失败不影响窗口功能
            }

            // 标准化：新创建的窗口默认active，先关闭再用showWindow走原版开启动画
            if (swGo.activeSelf) swGo.SetActive(false);
            ScrollWindow.showWindow(id);

            return wrapper;
        }

        /// <summary>关闭窗口（用原版showWindow toggle隐藏，不销毁，下次打开复用）</summary>
        public void Close()
        {
            IsOpen = false;
            if (_scrollWindow != null && !string.IsNullOrEmpty(WindowId))
            {
                ScrollWindow.showWindow(WindowId);
            }
        }

        /// <summary>显示窗口（用原版showWindow toggle显示）</summary>
        public void Show()
        {
            if (_scrollWindow != null && !string.IsNullOrEmpty(WindowId))
            {
                ScrollWindow.showWindow(WindowId);
                IsOpen = true;
            }
        }

        /// <summary>调整窗口大小</summary>
        public void Resize(Vector2 delta)
        {
            if (Rect == null) return;
            Rect.sizeDelta += delta;
        }

        /// <summary>运行时改标题（原版窗口标题由本地化key驱动，一般不需要调用）</summary>
        public void SetTitle(string text)
        {
            if (_scrollWindow == null) return;
            Transform titleTf = ((Component)_scrollWindow).transform.Find("Background/Title");
            if (titleTf != null)
            {
                Text t = titleTf.GetComponent<Text>();
                if (t != null) t.text = text;
            }
        }
    }
}
