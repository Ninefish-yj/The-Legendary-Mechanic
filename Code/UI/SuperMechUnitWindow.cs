using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

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
                ShowCustomStats(__instance, actor);
                AddInfoButton(__instance, actor);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 单位面板主要信息失败: {e.Message}\n{e.StackTrace}");
            }
            return true;
        }

        private static void AddInfoButton(UnitWindow window, Actor actor)
        {
            try
            {
                Transform content = window.transform.Find("Background/Scroll View/Viewport/Content");
                if (content == null) return;

                string btnName = "SMInfoBtn";
                Transform existing = content.Find(btnName);
                if (existing != null)
                {
                    UnityEngine.Object.Destroy(existing.gameObject);
                }

                GameObject btnGo = new GameObject(btnName, typeof(RectTransform));
                btnGo.transform.SetParent(content, false);
                RectTransform rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(180f, 26f);
                rt.anchoredPosition = new Vector2(0f, -4f);

                Image bg = btnGo.AddComponent<Image>();
                bg.color = new Color(0.2f, 0.35f, 0.5f, 0.9f);

                Button btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => SMUnitInfoWindow.Show(actor));

                Text txt = SuperMechUtils.CreateText(btnGo.transform, LocalizedTextManager.getText("sm_ui_open_info"), 12, TextAnchor.MiddleCenter, Color.white);
                txt.fontStyle = FontStyle.Bold;
                RectTransform txtRt = txt.rectTransform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 添加信息按钮失败: " + e.Message);
            }
        }

        private static readonly Color InfoColor = new Color(1f, 0.9f, 0.6f);
        private static readonly Color StatColor = new Color(0.7f, 0.85f, 1f);

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

        private static void ShowCustomStats(UnitWindow window, Actor a)
        {
            try
            {
                float div = a.stats[SuperMechCustomStats.StatDivinityLayers];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1152"), div.ToString("F0") + LocalizedTextManager.getText("sm_unitwindow_1153"), null, StatColor);

                int sanctuaryTotal = 0;
                string[] sanctuaryStats = {
                    SuperMechCustomStats.StatSanctuary1,
                    SuperMechCustomStats.StatSanctuary2,
                    SuperMechCustomStats.StatSanctuary3,
                    SuperMechCustomStats.StatSanctuary4,
                    SuperMechCustomStats.StatSanctuary5,
                    SuperMechCustomStats.StatSanctuary6
                };
                foreach (var s in sanctuaryStats) sanctuaryTotal += (int)a.stats[s];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1154"), sanctuaryTotal + LocalizedTextManager.getText("sm_unitwindow_1155"), null, StatColor);

                float mechAff = a.stats[SuperMechCustomStats.StatMechAffinity];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1156"), mechAff.ToString("F0") + "%", null, StatColor);

                float mageAff = a.stats[SuperMechCustomStats.StatMageAffinity];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1157"), mageAff.ToString("F0") + "%", null, StatColor);

                float mystery = a.stats[SuperMechCustomStats.StatMystery];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1158"), mystery.ToString("F0"), null, StatColor);
                float charm = a.stats[SuperMechCustomStats.StatCharm];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1159"), charm.ToString("F0"), null, StatColor);
                float luck = a.stats[SuperMechCustomStats.StatLuck];
                ShowRow(window, LocalizedTextManager.getText("sm_unitwindow_1160"), luck.ToString("F0"), null, StatColor);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 自定义属性显示失败: " + e.Message);
            }
        }
    }
}
