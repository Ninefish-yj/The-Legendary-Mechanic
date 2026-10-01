using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// UGUI窗口基类 - 用Canvas+RectTransform实现可拖动窗口
    /// 精灵图作为Image背景，支持9-slice缩放
    /// </summary>
    public abstract class SMUguiWindow
    {
        protected static Canvas _canvas;
        protected static Dictionary<int, SMUguiWindow> _openWindows = new Dictionary<int, SMUguiWindow>();

        protected GameObject _root;
        protected RectTransform _rootRect;
        protected Image _background;
        protected Text _titleText;
        protected Button _closeButton;
        protected RectTransform _contentRect;
        protected bool _dragging;
        protected Vector2 _dragOffset;

        public abstract int WindowId { get; }
        public abstract string TitleKey { get; }
        protected virtual int Width => 800;
        protected virtual int Height => 600;

        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// 确保Canvas存在
        /// </summary>
        protected static void EnsureCanvas()
        {
            if (_canvas != null) return;
            var canvasObj = new GameObject("SM_UiCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasObj);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 打开窗口
        /// </summary>
        public virtual void Open()
        {
            if (IsOpen) { Close(); return; }
            EnsureCanvas();
            CreateWindow();
            _openWindows[WindowId] = this;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public virtual void Close()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _openWindows.Remove(WindowId);
        }

        /// <summary>
        /// 创建窗口结构
        /// </summary>
        protected virtual void CreateWindow()
        {
            // 根节点
            _root = new GameObject("SM_Window_" + WindowId);
            _root.transform.SetParent(_canvas.transform, false);
            _rootRect = _root.AddComponent<RectTransform>();
            _rootRect.sizeDelta = new Vector2(Width, Height);
            _rootRect.anchoredPosition = new Vector2(
                (Screen.width - Width) / 2f,
                (Screen.height - Height) / 2f);

            // 背景（精灵图，9-slice）
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_root.transform, false);
            _background = bgObj.AddComponent<Image>();
            _background.type = Image.Type.Sliced;
            var bgRect = _background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            if (SMImguiTheme.UiPanel != null)
            {
                _background.sprite = Sprite.Create(SMImguiTheme.UiPanel,
                    new Rect(0, 0, SMImguiTheme.UiPanel.width, SMImguiTheme.UiPanel.height),
                    new Vector2(0.5f, 0.5f));
            }

            // 标题栏（可拖动区域）
            var titleObj = new GameObject("TitleBar");
            titleObj.transform.SetParent(_root.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 50);
            titleRect.anchoredPosition = new Vector2(0, -10);
            var titleTrigger = titleObj.AddComponent<SMDragHandler>();
            titleTrigger.Window = this;

            // 标题文字
            var titleTextObj = new GameObject("Title");
            titleTextObj.transform.SetParent(titleObj.transform, false);
            _titleText = titleTextObj.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 24;
            _titleText.color = new Color(0.1f, 0.15f, 0.25f);
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.text = LocalizedTextManager.getText(TitleKey);
            var titleTextRect = _titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(50, 0);
            titleTextRect.offsetMax = new Vector2(-60, 0);

            // 关闭按钮
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(titleObj.transform, false);
            _closeButton = closeObj.AddComponent<Button>();
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(32, 32);
            closeRect.anchoredPosition = new Vector2(-15, 0);
            _closeButton.onClick.AddListener(Close);
            var closeTextObj = new GameObject("X");
            closeTextObj.transform.SetParent(closeObj.transform, false);
            var closeText = closeTextObj.AddComponent<Text>();
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeText.fontSize = 24;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = "X";
            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            // 内容区域
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(_root.transform, false);
            _contentRect = contentObj.AddComponent<RectTransform>();
            _contentRect.anchorMin = Vector2.zero;
            _contentRect.anchorMax = Vector2.one;
            _contentRect.offsetMin = new Vector2(42, 50);
            _contentRect.offsetMax = new Vector2(-42, -60);

            BuildContent(_contentRect);
        }

        /// <summary>
        /// 子类实现：构建内容区域
        /// </summary>
        protected abstract void BuildContent(RectTransform content);

        /// <summary>
        /// 拖动开始
        /// </summary>
        public void OnDragStart(Vector2 mousePos)
        {
            _dragging = true;
            _dragOffset = new Vector2(mousePos.x - _rootRect.anchoredPosition.x,
                                       mousePos.y - _rootRect.anchoredPosition.y);
        }

        /// <summary>
        /// 拖动中
        /// </summary>
        public void OnDrag(Vector2 mousePos)
        {
            if (!_dragging) return;
            _rootRect.anchoredPosition = new Vector2(
                mousePos.x - _dragOffset.x,
                mousePos.y - _dragOffset.y);
        }

        /// <summary>
        /// 拖动结束
        /// </summary>
        public void OnDragEnd()
        {
            _dragging = false;
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public static void CloseAll()
        {
            var ids = new List<int>(_openWindows.Keys);
            foreach (var id in ids)
            {
                _openWindows[id].Close();
            }
        }

        /// <summary>
        /// 创建一个Text组件
        /// </summary>
        protected Text CreateText(Transform parent, string text, int fontSize = 16, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = new Color(0.1f, 0.15f, 0.25f);
            t.alignment = anchor;
            t.text = text;
            return t;
        }

        /// <summary>
        /// 创建一个Button
        /// </summary>
        protected Button CreateButton(Transform parent, string textKey, Action onClick)
        {
            var obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);
            var btn = obj.AddComponent<Button>();
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.5f, 0.8f, 0.8f);
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = LocalizedTextManager.getText(textKey);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }

    /// <summary>
    /// 拖动事件处理
    /// </summary>
    public class SMDragHandler : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler,
        UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
    {
        public SMUguiWindow Window;

        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Window?.OnDragStart(eventData.position);
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Window?.OnDrag(eventData.position);
        }

        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            Window?.OnDragEnd();
        }
    }
}
