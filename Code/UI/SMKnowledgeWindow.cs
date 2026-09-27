using System.Collections.Generic;
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

    public static class SMKnowledgeWindow
    {
        private static readonly Color[] TierColors = {
            new Color(0.3f, 0.71f, 0.67f),
            new Color(0.15f, 0.65f, 0.6f),
            new Color(0f, 0.54f, 0.48f),
            new Color(0f, 0.47f, 0.42f),
            new Color(0f, 0.3f, 0.25f)
        };

        public static void Create(Transform parent, Actor actor)
        {
            if (parent == null || actor == null) return;

            VerticalLayoutGroup vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter csf = parent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string prefix = SuperMechKnowledge.GetPrefixForClass(SuperMechProfession.GetClass(actor));
            if (string.IsNullOrEmpty(prefix)) prefix = "mech";
            var allKnowledge = SuperMechKnowledge.GetAllByPrefix(prefix);
            int unlockedCount = SuperMechKnowledge.GetUnlockedCount(actor, prefix);

            CreateProgressBar(parent, unlockedCount, allKnowledge.Count);

            GameObject graphObj = new GameObject("CubeOverview", typeof(RectTransform));
            graphObj.transform.SetParent(parent, false);
            RectTransform graphRt = graphObj.GetComponent<RectTransform>();
            graphRt.sizeDelta = new Vector2(0, 300f);
            SMCubeKnowledge graph = graphObj.AddComponent<SMCubeKnowledge>();
            graph.Init(actor);

            for (int tier = 0; tier <= 4; tier++)
            {
                var tierKnowledge = allKnowledge.FindAll(k => k.tier == tier);
                if (tierKnowledge.Count == 0) continue;

                CreateKnowledgeElement(parent, actor, tier, tierKnowledge, prefix);
            }
        }

        private static void CreateProgressBar(Transform parent, int unlocked, int total)
        {
            GameObject barObj = new GameObject("ProgressBar", typeof(RectTransform));
            barObj.transform.SetParent(parent, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0, 24f);

            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(barObj.transform, false);
            Image fill = fillObj.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.85f, 1f, 0.8f);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(total > 0 ? (float)unlocked / total : 0, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Text text = SuperMechUtils.CreateText(barObj.transform,
                $"{unlocked}/{total}", 11, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private static void CreateKnowledgeElement(Transform parent, Actor actor, int tier, List<KnowledgeDef> knowledge, string prefix)
        {
            GameObject elementObj = new GameObject($"KnowledgeElement_Tier{tier}", typeof(RectTransform));
            elementObj.transform.SetParent(parent, false);
            RectTransform elementRt = elementObj.GetComponent<RectTransform>();
            elementRt.sizeDelta = new Vector2(0, 100f);

            Image bg = elementObj.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.1f, 0.6f);

            VerticalLayoutGroup vlg = elementObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            vlg.childAlignment = TextAnchor.UpperLeft;

            GameObject headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(elementObj.transform, false);
            HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = false;
            hlg.childControlWidth = false;
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(0, 20f);

            Image tierIcon = new GameObject("TierIcon", typeof(RectTransform)).AddComponent<Image>();
            tierIcon.transform.SetParent(headerObj.transform, false);
            tierIcon.color = TierColors[tier];
            RectTransform iconRt = tierIcon.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(16, 16);

            int tierUnlocked = knowledge.FindAll(k => SuperMechKnowledge.IsUnlocked(actor, k.id)).Count;
            Text title = SuperMechUtils.CreateText(headerObj.transform,
                $"{LocalizedTextManager.getText("sm_ui_tier")} {tier + 1}  {tierUnlocked}/{knowledge.Count}",
                11, TextAnchor.MiddleLeft, TierColors[tier]);
            title.fontStyle = FontStyle.Bold;

            GameObject gridObj = new GameObject("Icons", typeof(RectTransform));
            gridObj.transform.SetParent(elementObj.transform, false);
            GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(24, 24);
            glg.spacing = new Vector2(3, 3);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;
            glg.childAlignment = TextAnchor.UpperLeft;
            RectTransform gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(0, 60f);

            foreach (var def in knowledge)
            {
                bool unlocked = SuperMechKnowledge.IsUnlocked(actor, def.id);
                GameObject iconObj = new GameObject(def.id, typeof(RectTransform));
                iconObj.transform.SetParent(gridObj.transform, false);
                Image img = iconObj.AddComponent<Image>();
                img.color = unlocked ? TierColors[tier] : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                try
                {
                    Sprite sprite = SpriteTextureLoader.getSprite(def.icon);
                    if (sprite != null) img.sprite = sprite;
                }
                catch { }
                Button btn = iconObj.AddComponent<Button>();
                string kid = def.id;
                btn.onClick.AddListener(() =>
                {
                    if (!unlocked && actor != null)
                    {
                        SuperMechKnowledge.Unlock(actor, kid);
                    }
                });
            }
        }
    }
}
