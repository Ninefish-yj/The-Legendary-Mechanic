using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindow
    {
        [HarmonyPrefix]
        public static bool Prefix(UnitWindow __instance)
        {
            try
            {
                Actor actor = SuperMechUtils.GetActor(__instance);
                if (actor == null || !actor.isAlive()) return true;

                ShowRow(__instance, LocalizedTextManager.getText("sm_ui_super_info"), "", null, new Color(1f, 0.85f, 0.4f));
                ShowMainInfo(__instance, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 单位面板主要信息失败: {e.Message}\n{e.StackTrace}");
            }
            return true;
        }

        private static readonly Color InfoColor = new Color(1f, 0.9f, 0.6f);

        private static void ShowMainInfo(UnitWindow window, Actor a)
        {
            bool hasTalent = SuperMechTalent.HasTalent(a);
            if (!hasTalent)
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), LocalizedTextManager.getText("sm_ui_mortal"), null, InfoColor);
                return;
            }

            ShowRow(window, LocalizedTextManager.getText("sm_ui_rank"), GetRank(a), null, InfoColor);

            bool hasProfession = SuperMechProfession.HasProfession(a);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(a);
                string clsAspect = GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), clsText, null, InfoColor);

                string stage = SuperMechStage.GetStageName(a);
                if (stage != "—" && stage != "sm_knowledgetab_829")
                    ShowRow(window, LocalizedTextManager.getText("sm_ui_class_stage"), stage, null, InfoColor);
            }
            else
            {
                ShowRow(window, LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"), null, InfoColor);
            }

            float onar = SuperMechAdvancement.CalcOnar(a);
            ShowRow(window, LocalizedTextManager.getText("sm_ui_onar"), $"{onar:F0}{LocalizedTextManager.getText("sm_ui_onar_unit")}", null, InfoColor);

            float qi = SuperMechQi.GetQi(a);
            float qiMax = SuperMechQi.GetQiMax(a);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
            string qiName = GetQiDisplayName(a);
            string qiBar = qiMax > 0 ? $"{qi:F0}/{qiMax:F0}" : qi.ToString("F0");
            ShowRow(window, qiName, $"{qiBar}（{qiLvText}）", null, InfoColor);

            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                ShowRow(window, LocalizedTextManager.getText("sm_ui_potential"), pot.ToString(), null, InfoColor);

            if (SuperMechAwakened.IsAwakened(a))
                ShowRow(window, LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"), null, InfoColor);
        }

        public static string GetClassAspect(string cls)
        {
            switch (cls)
            {
                case "sm_unitwindow_1146": return LocalizedTextManager.getText("sm_aspect_martial");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_aspect_mind");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_aspect_mage");
                case "sm_unitwindow_1149": return LocalizedTextManager.getText("sm_aspect_psi");
                case "sm_unitwindow_1150": return LocalizedTextManager.getText("sm_aspect_mech");
                default: return "";
            }
        }

        public static string GetRank(Actor a)
        {
            return SuperMechRanks.GetRankName(a);
        }

        public static string GetQiDisplayName(Actor a)
        {
            if (!SuperMechProfession.HasProfession(a)) return LocalizedTextManager.getText("sm_qi_qi");
            string cls = SuperMechProfession.GetClass(a);
            switch (cls)
            {
                case "sm_unitwindow_1150":
                    int stage = SuperMechStage.GetStage(a);
                    return stage >= 3 ? LocalizedTextManager.getText("sm_qi_mech") : LocalizedTextManager.getText("sm_qi_qi");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_qi_mage");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_qi_mind");
                default: return LocalizedTextManager.getText("sm_qi_qi");
            }
        }

        public static string GetKnowledgeTreeName(string cls)
        {
            switch (cls)
            {
                case "sm_unitwindow_1146": return LocalizedTextManager.getText("sm_tree_martial");
                case "sm_unitwindow_1149": return LocalizedTextManager.getText("sm_tree_psi");
                case "sm_unitwindow_1148": return LocalizedTextManager.getText("sm_tree_mage");
                case "sm_unitwindow_1147": return LocalizedTextManager.getText("sm_tree_mind");
                default: return LocalizedTextManager.getText("sm_tree_mech");
            }
        }

        private static void ShowRow(UnitWindow window, string label, object value, string iconPath = null, Color? color = null)
        {
            try
            {
                Color c = color ?? Color.white;
                string colorHex = "#" + ColorUtility.ToHtmlStringRGB(c);
                window.showStatRow(label, value, colorHex, MetaType.None, -1L, pColorText: false, iconPath, null, null, pLocalize: false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(UnitWindow), "OnEnable")]
    public static class SuperMechUnitWindowIconButton
    {
        private static GameObject _infoButton;

        [HarmonyPrefix]
        public static void Prefix(UnitWindow __instance)
        {
            try
            {
                if (__instance?.GetActor() == null) return;
                if (_infoButton == null)
                {
                    CreateInfoButton(__instance);
                }
                UpdateButtonVisibility(__instance);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 单位窗口图标按钮Patch异常: " + e.Message);
            }
        }

        private static void CreateInfoButton(UnitWindow window)
        {
            try
            {
                Transform background = window.transform.Find("Background");
                if (background == null)
                {
                    Debug.LogWarning("[超神机械师] 未找到UnitWindow.Background");
                    return;
                }

                float buttonX = 156f;
                float buttonY = 84f;
                float buttonSize = 32f;

                Sprite iconSprite = null;
                try
                {
                    iconSprite = SpriteTextureLoader.getSprite("ui/iconBook");
                }
                catch { }

                if (iconSprite == null)
                {
                    try
                    {
                        iconSprite = SpriteTextureLoader.getSprite("ui/Icons/iconKnowledge");
                    }
                    catch { }
                }

                GameObject buttonObj = new GameObject("SMInfoIconButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObj.transform.SetParent(background, false);

                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(buttonX, -buttonY);
                rectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);
                rectTransform.localScale = Vector3.one;

                Image image = buttonObj.GetComponent<Image>();
                if (iconSprite != null)
                {
                    image.sprite = iconSprite;
                    image.color = new Color(0.3f, 0.85f, 1f, 0.9f);
                }
                else
                {
                    image.color = new Color(0.1f, 0.3f, 0.4f, 0.9f);
                }

                Button button = buttonObj.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    Actor actor = window.GetActor();
                    if (actor != null && actor.isAlive())
                    {
                        SMUnitInfoWindow.Show(actor);
                    }
                });

                try
                {
                    var tipButton = buttonObj.AddComponent<TipButton>();
                    if (tipButton != null)
                    {
                        tipButton.textOnClick = LocalizedTextManager.getText("sm_ui_open_info");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[超神机械师] 添加TipButton失败: " + e.Message);
                }

                _infoButton = buttonObj;
                Debug.Log("[超神机械师] 单位窗口信息图标按钮已创建");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 创建信息图标按钮失败: " + e.Message);
            }
        }

        private static void UpdateButtonVisibility(UnitWindow window)
        {
            try
            {
                if (_infoButton == null) return;
                bool shouldShow = false;
                Actor actor = window.GetActor();
                if (actor != null && actor.isAlive())
                {
                    shouldShow = true;
                }
                _infoButton.SetActive(shouldShow);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 更新信息按钮可见性失败: " + e.Message);
            }
        }
    }
}
