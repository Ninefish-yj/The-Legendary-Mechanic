using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal class SMWindowBlocker
    {
        private GameObject _go;
        private static readonly Dictionary<int, SMWindowBlocker> _instances = new Dictionary<int, SMWindowBlocker>();

        public static SMWindowBlocker Get(int windowId)
        {
            if (!_instances.TryGetValue(windowId, out var blocker))
            {
                blocker = new SMWindowBlocker();
                _instances[windowId] = blocker;
            }
            return blocker;
        }

        public void Sync(Rect windowRect)
        {
            if (_go == null)
            {
                try
                {
                    _go = new GameObject("SMWindowBlocker_" + windowRect.GetHashCode());
                    Canvas canvas = _go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 29980;
                    _go.AddComponent<GraphicRaycaster>();

                    Image img = _go.AddComponent<Image>();
                    img.color = new Color(0, 0, 0, 0f);
                    img.raycastTarget = true;
                }
                catch
                {
                    return;
                }
            }

            RectTransform rt = _go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(windowRect.width, windowRect.height);
            rt.anchoredPosition = new Vector2(windowRect.x, Screen.height - windowRect.y - windowRect.height);
            _go.SetActive(true);
        }

        public void Hide()
        {
            if (_go != null) _go.SetActive(false);
        }

        public static void HideAll()
        {
            foreach (var blocker in _instances.Values)
            {
                blocker.Hide();
            }
        }
    }
}
