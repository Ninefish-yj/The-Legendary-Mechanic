using System.Collections.Generic;
using HarmonyLib;
using System;
using UnityEngine.UI;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showMainInfo")]
    public static class SuperMechStatsIconPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                Actor actor = SuperMechUtils.GetActor(__instance);
                if (actor == null || !actor.isAlive()) return;
                SuperMechStatsIcon.Initialize(__instance);
                SuperMechStatsIcon.UpdateValues(__instance, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 自定义属性图标失败: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    public static class SuperMechStatsIcon
    {
        private static bool _initialized = false;
        private static Transform _contentTransform;
        private static readonly List<StatsIconData> IconDatas = new List<StatsIconData>
        {
            new StatsIconData("sm_mech_affinity", "ui/Icons/iconMechAffinity", true),
            new StatsIconData("sm_magic_affinity", "ui/Icons/iconMagicAffinity", true),
            new StatsIconData("sm_mystery", "ui/Icons/iconMystery", true),
        };

        public static void Initialize(UnitWindow window)
        {
            if (_initialized) return;
            _initialized = true;

            _contentTransform = window.gameObject.transform.Find(
                "Background/Scroll View/Viewport/Content/content_more_icons");

            if (_contentTransform == null)
            {
                Debug.LogError("[超神机械师] 找不到content_more_icons容器");
                return;
            }

            Transform originalGroup = null;
            for (int i = 0; i < _contentTransform.childCount; i++)
            {
                Transform child = _contentTransform.GetChild(i);
                if (child.Find("i_kills") != null)
                {
                    originalGroup = child;
                    break;
                }
            }

            if (originalGroup == null && _contentTransform.childCount > 0)
            {
                originalGroup = _contentTransform.GetChild(_contentTransform.childCount - 1);
            }

            if (originalGroup == null)
            {
                Debug.LogError("[超神机械师] 找不到图标组模板");
                return;
            }

            Transform iKills = originalGroup.Find("i_kills");
            if (iKills == null)
            {
                foreach (Transform child in originalGroup)
                {
                    if (child.GetComponent<StatsIcon>() != null)
                    {
                        iKills = child;
                        break;
                    }
                }
            }

            if (iKills == null)
            {
                Debug.LogError("[超神机械师] 找不到i_kills图标模板");
                return;
            }

            GameObject newGroup = UnityEngine.Object.Instantiate(originalGroup.gameObject);
            for (int i = newGroup.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = newGroup.transform.GetChild(i);
                if (child.name != "i_kills")
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }

            Transform template = newGroup.transform.Find("i_kills");
            int index = 0;
            foreach (StatsIconData data in IconDatas)
            {
                if (!data.isShow) continue;
                if (index >= 25) break;

                GameObject newIcon = UnityEngine.Object.Instantiate(template.gameObject, newGroup.transform);
                newIcon.name = data.name;
                StatsIcon icon = newIcon.GetComponent<StatsIcon>();
                TipButton tip = newIcon.GetComponent<TipButton>();
                if (tip != null)
                {
                    tip.textOnClick = "sm_icon_" + data.name;
                }
                Image iconImage = icon.getIcon();
                if (iconImage != null)
                {
                    Sprite sprite = SpriteTextureLoader.getSprite(data.iconPath);
                    if (sprite != null)
                    {
                        iconImage.sprite = sprite;
                    }
                }
                index++;
            }

            UnityEngine.Object.DestroyImmediate(template.gameObject);
            newGroup.name = "sm_custom_icons";
            newGroup.transform.SetParent(_contentTransform, false);
            newGroup.transform.localScale = Vector3.one;
        }

        private static void CallSetIconValue(UnitWindow window, string name, float value)
        {
            try
            {
                Transform t = window.transform.FindRecursive(name);
                if (t == null) return;
                StatsIcon icon = t.GetComponent<StatsIcon>();
                if (icon == null) return;
                icon.gameObject.SetActive(true);
                icon.setValue(value, null, "", false, "", '/');
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] setIconValue调用失败: " + e.Message);
            }
        }

        public static void UpdateValues(UnitWindow window, Actor actor)
        {
            if (!_initialized || _contentTransform == null) return;

            float mechAff = SuperMechCustomStats.GetStat(actor, SuperMechCustomStats.StatMechAffinity);
            float mageAff = SuperMechCustomStats.GetStat(actor, SuperMechCustomStats.StatMageAffinity);
            float mystery = SuperMechCustomStats.GetStat(actor, SuperMechCustomStats.StatMystery);

            CallSetIconValue(window, "sm_mech_affinity", mechAff);
            CallSetIconValue(window, "sm_magic_affinity", mageAff);
            CallSetIconValue(window, "sm_mystery", mystery);
        }

        public static void ClearStaticState()
        {
            _initialized = false;
            _contentTransform = null;
        }
    }

    public class StatsIconData
    {
        public string name;
        public string iconPath;
        public bool isShow;

        public StatsIconData(string name, string iconPath, bool isShow = false)
        {
            this.name = name;
            this.iconPath = iconPath;
            this.isShow = isShow;
        }
    }
}
