using System;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    internal class SuperMechKnowledgeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image icon;
        public Image lockedBg;
        public Image outline;
        public Button button;
        public string knowledgeId;
        public bool unlocked;
        public bool canUnlock;

        private static GameObject _prefab;

        public static SuperMechKnowledgeButton Create(Transform parent)
        {
            if (_prefab == null)
                _prefab = CreatePrefab();

            GameObject obj = Instantiate(_prefab, parent);
            return obj.GetComponent<SuperMechKnowledgeButton>();
        }

        private static GameObject CreatePrefab()
        {
            GameObject prefab = new GameObject("KnowledgeButtonPrefab",
                typeof(RectTransform), typeof(Button), typeof(SuperMechKnowledgeButton));
            prefab.SetActive(false);

            GameObject tilt = new GameObject("TiltEffect", typeof(RectTransform));
            tilt.transform.SetParent(prefab.transform, false);
            RectTransform tiltRt = tilt.GetComponent<RectTransform>();
            tiltRt.anchorMin = Vector2.zero;
            tiltRt.anchorMax = Vector2.one;
            tiltRt.offsetMin = Vector2.zero;
            tiltRt.offsetMax = Vector2.zero;

            GameObject iconObj = new GameObject("icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(tilt.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.raycastTarget = false;

            GameObject lockedObj = new GameObject("locked_bg", typeof(RectTransform), typeof(Image));
            lockedObj.transform.SetParent(tilt.transform, false);
            RectTransform lockedRt = lockedObj.GetComponent<RectTransform>();
            lockedRt.anchorMin = Vector2.zero;
            lockedRt.anchorMax = Vector2.one;
            lockedRt.offsetMin = Vector2.zero;
            lockedRt.offsetMax = Vector2.zero;
            Image lockedImg = lockedObj.GetComponent<Image>();
            lockedImg.color = new Color(0f, 0f, 0f, 0.6f);
            lockedImg.raycastTarget = false;
            lockedObj.SetActive(false);

            GameObject outlineObj = new GameObject("outline", typeof(RectTransform), typeof(Image));
            outlineObj.transform.SetParent(tilt.transform, false);
            RectTransform outlineRt = outlineObj.GetComponent<RectTransform>();
            outlineRt.anchorMin = Vector2.zero;
            outlineRt.anchorMax = Vector2.one;
            outlineRt.offsetMin = new Vector2(-2f, -2f);
            outlineRt.offsetMax = new Vector2(2f, 2f);
            Image outlineImg = outlineObj.GetComponent<Image>();
            outlineImg.color = new Color(1f, 0.85f, 0.3f, 0f);
            outlineImg.raycastTarget = false;

            SuperMechKnowledgeButton btn = prefab.GetComponent<SuperMechKnowledgeButton>();
            btn.icon = iconImg;
            btn.lockedBg = lockedImg;
            btn.outline = outlineImg;
            btn.button = prefab.GetComponent<Button>();

            return prefab;
        }

        public void Setup(string id, Sprite sprite, bool isUnlocked, bool isCanUnlock, int tier)
        {
            knowledgeId = id;
            unlocked = isUnlocked;
            canUnlock = isCanUnlock;

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = isUnlocked ? Color.white : (isCanUnlock ? new Color(0.8f, 0.8f, 0.6f) : new Color(0.4f, 0.4f, 0.4f));
            }

            if (lockedBg != null)
                lockedBg.gameObject.SetActive(!isUnlocked);

            if (outline != null)
            {
                if (tier >= 3)
                {
                    outline.color = tier >= 4 ? new Color(1f, 0.85f, 0.3f, 0.8f) : new Color(0.6f, 0.4f, 0.8f, 0.6f);
                }
                else
                {
                    outline.color = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (icon != null)
                icon.transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (icon != null)
                icon.transform.localScale = Vector3.one;
        }
    }
}
