using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMUnitBagButton : MonoBehaviour
    {
        public Text textAmount;
        public ResourceAsset asset;
        public Actor owner;
        public static float scaleTime = 0.1f;

        private void Start()
        {
            Button component = GetComponent<Button>();
            component.onClick.AddListener(OnClick);
            component.OnHover(showHoverTooltip);
            component.OnHoverOut(Tooltip.hideTooltip);
        }

        internal void load(ResourceAsset pAsset, int pAmount, Actor pOwner)
        {
            asset = pAsset;
            owner = pOwner;
            if (asset != null)
            {
                GetComponent<Image>().sprite = pAsset.getSpriteIcon();
                textAmount.text = pAmount.ToString() ?? "";
            }
        }

        private void OnClick()
        {
            if (owner != null && asset != null && asset.type == ResType.Food)
            {
                owner.consumeFoodResource(asset);
                owner.inventory.remove(asset.id, 1);
            }
            showTooltip();
        }

        private void showHoverTooltip()
        {
            if (Config.tooltips_active)
            {
                showTooltip();
            }
        }

        private void showTooltip()
        {
            string tooltip = asset.tooltip;
            Tooltip.show(this, tooltip, new TooltipData
            {
                resource = asset
            });
            transform.localScale = new Vector3(1f, 1f, 1f);
            transform.DOKill();
            transform.DOScale(0.8f, scaleTime).SetEase(Ease.InBack);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }

    public static class SMUnitBag
    {
        private static GameObject _prefabResource;
        private static ObjectPoolGenericMono<SMUnitBagButton> _poolResources;
        private static Dictionary<ResourceContainer, SMUnitBagButton> _loadedSlots = new Dictionary<ResourceContainer, SMUnitBagButton>();

        public static void Render(Transform parent, Actor actor)
        {
            if (parent == null || actor == null) return;
            if (actor.inventory == null) return;

            EnsurePool(parent);

            _loadedSlots.Clear();
            _poolResources.clear();

            var resources = new List<ResourceContainer>();
            foreach (var kv in actor.inventory.dict)
            {
                if (kv.Value.amount > 0)
                {
                    resources.Add(kv.Value);
                }
            }

            resources.Sort((a, b) => a.asset.order.CompareTo(b.asset.order));

            foreach (var slot in resources)
            {
                LoadResource(slot, actor);
            }
        }

        private static void EnsurePool(Transform parent)
        {
            if (_poolResources != null) return;

            if (_prefabResource == null)
            {
                _prefabResource = CreateResourcePrefab();
            }

            _poolResources = new ObjectPoolGenericMono<SMUnitBagButton>(_prefabResource.GetComponent<SMUnitBagButton>(), parent);
        }

        private static GameObject CreateResourcePrefab()
        {
            GameObject prefab = new GameObject("SMResourcePrefab", typeof(RectTransform));
            prefab.SetActive(false);

            Image img = prefab.AddComponent<Image>();
            img.color = Color.white;

            Button btn = prefab.AddComponent<Button>();

            Text amount = SuperMechUtils.CreateText(prefab.transform, "", 10, TextAnchor.LowerRight, Color.white);
            RectTransform amtRt = amount.rectTransform;
            amtRt.anchorMin = new Vector2(0, 0);
            amtRt.anchorMax = new Vector2(1, 0.3f);
            amtRt.offsetMin = new Vector2(0, 0);
            amtRt.offsetMax = new Vector2(-2f, 0);

            SMUnitBagButton button = prefab.AddComponent<SMUnitBagButton>();
            button.textAmount = amount;

            return prefab;
        }

        private static void LoadResource(ResourceContainer pSlot, Actor actor)
        {
            SMUnitBagButton next = _poolResources.getNext();
            next.load(pSlot.asset, pSlot.amount, actor);
            _loadedSlots[pSlot] = next;
        }

        public static void Clear()
        {
            _loadedSlots.Clear();
            if (_poolResources != null)
            {
                _poolResources.clear();
            }
        }
    }
}
