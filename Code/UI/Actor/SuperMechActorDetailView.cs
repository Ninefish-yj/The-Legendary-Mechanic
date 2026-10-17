using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>超能详情面板（v0.195 从单位面板拆分）
    /// 单位面板只显示核心信息，详细属性和功能按钮放这里</summary>
    public class SuperMechActorDetailView : MonoBehaviour
    {
        private static SuperMechActorDetailView _instance;
        public static bool IsOpen => _instance != null;

        private GameObject _panel;
        private GameObject _content;
        private Actor _actor;

        public static void Toggle(Actor actor)
        {
            if (_instance != null) { _instance.Close(); return; }
            Open(actor);
        }

        public static void Open(Actor actor)
        {
            if (_instance != null || actor == null) return;
            var canvas = SuperMechUguiWindow.GetCanvas();
            var go = new GameObject("SMActorDetailView");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<SuperMechActorDetailView>();
            _instance._actor = actor;
            _instance.Build();
        }

        public void Close()
        {
            if (_panel != null) Destroy(_panel);
            if (_instance == this) _instance = null;
            Destroy(gameObject);
        }

        private void Build()
        {
            // 半透明背景
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.35f); // v0.237.4 遮罩调淡(0.6→0.35)，避免盖住画面看不清
            bgImg.raycastTarget = true;

            // 主面板
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 520);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.sprite = SuperMechUiSkin.Panel;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;

            // 标题栏
            var titleGo = new GameObject("TitleBar");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 44);
            titleRect.anchoredPosition = Vector2.zero;
            var titleImg = titleGo.AddComponent<Image>();
            titleImg.sprite = SuperMechUiSkin.TitleBar;
            titleImg.type = Image.Type.Sliced;
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = SuperMechUiSkin.CyanColor;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_actor_detail");
            titleText.raycastTarget = false;

            // 关闭按钮
            var closeBtn = SuperMechUiSkin.MakeButton(_panel.transform, "X", 14, () => Close());
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.sizeDelta = new Vector2(36, 36);
            closeRect.anchoredPosition = new Vector2(-6, -4);

            // 滚动区域
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(_panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(8, 8);
            scrollRect.offsetMax = new Vector2(-8, -52);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0);
            scrollImg.raycastTarget = false;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 20f;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0);
            viewportImg.raycastTarget = false;
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewportRect;

            // Content
            _content = new GameObject("Content");
            _content.transform.SetParent(viewportGo.transform, false);
            var contentRect = _content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 800);
            scroll.content = contentRect;
            var fitter = _content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = _content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 填充内容
            FillContent();
        }

        private void FillContent()
        {
            Actor a = _actor;
            if (a == null || !a.isAlive()) return;

            // === 职业详情 ===
            AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_class"));
            bool hasProfession = SuperMechProfession.HasProfession(a);
            if (hasProfession)
            {
                string cls = SuperMechProfession.GetClass(a);
                string clsAspect = SuperMechUnitWindow.GetClassAspect(cls);
                string clsText = cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）");
                AddInfoRow(LocalizedTextManager.getText("sm_ui_class"), clsText);

                if (SuperMechAwakened.IsAwakened(a))
                {
                    string stage = SuperMechStage.GetStageName(a);
                    if (stage != "—" && stage != "sm_knowledgetab_829")
                    {
                        string progress = SuperMechStage.GetStageProgressText(a);
                        string stageText = string.IsNullOrEmpty(progress) ? stage : $"{stage}（{progress}）";
                        AddInfoRow(LocalizedTextManager.getText("sm_ui_class_stage"), stageText);
                    }
                }

                if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech))
                {
                    string specName = SuperMechSpecialty.GetSpecialtyName(a);
                    if (specName != LocalizedTextManager.getText("sm_spec_none"))
                        AddInfoRow(LocalizedTextManager.getText("sm_ui_specialization"), specName);
                }

                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    string mageType = SuperMechMageType.GetMageTypeName(a);
                    if (!string.IsNullOrEmpty(mageType))
                    {
                        string mageSpec = SuperMechMageType.GetSpecName(a);
                        string displayText = string.IsNullOrEmpty(mageSpec) ? mageType : $"{mageType}（{mageSpec}）";
                        AddInfoRow(LocalizedTextManager.getText("sm_ui_mage_type"), displayText);
                    }
                }
            }
            else
            {
                AddInfoRow(LocalizedTextManager.getText("sm_ui_class"), LocalizedTextManager.getText("sm_ui_wild"));
            }

            // === 战斗数值 ===
            AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_combat"));
            int qiLayer = SuperMechQiLayer.GetLayer(a);
            if (qiLayer > 0)
                AddInfoRow(LocalizedTextManager.getText("sm_ui_qi_layer"), SuperMechQiLayer.GetLayerText(a));
            int pot = SuperMechPotential.GetPotential(a);
            if (pot > 0)
                AddInfoRow(LocalizedTextManager.getText("sm_ui_potential"), pot.ToString());

            // === 社会关系 ===
            AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_social"));
            if (SuperMechConfig.FactionEnabled)
            {
                var faction = SuperMechFaction.GetFaction(a);
                if (faction != null)
                {
                    bool isLeader = faction.leaderId == a.id;
                    string factionText = isLeader
                        ? string.Format(LocalizedTextManager.getText("sm_ui_faction_leader"), faction.name, faction.level)
                        : string.Format(LocalizedTextManager.getText("sm_ui_faction_member"), faction.name, faction.level);
                    AddInfoRow(LocalizedTextManager.getText("sm_ui_faction_label"), factionText);
                }
            }
            if (SuperMechAwakened.IsAwakened(a))
                AddInfoRow(LocalizedTextManager.getText("sm_ui_identity"), LocalizedTextManager.getText("sm_ui_awakened"));

            // === 个人经济 ===
            int inal = SuperMechInal.GetInal(a);
            bool isSuperA = SuperMechSupermA.IsSuperA(a);
            if (inal > 0 || isSuperA)
            {
                AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_economy"));
                AddInfoRow(LocalizedTextManager.getText("sm_inal_balance"), inal.ToString("N0"));
                if (isSuperA && a.kingdom != null)
                {
                    int salary = SuperMechInal.CalcSalary(a, a.kingdom);
                    AddInfoRow(LocalizedTextManager.getText("sm_inal_salary"), $"{salary:N0} / 32tick");
                }
                var company = SuperMechCompany.GetCompanyByActor(a);
                if (company != null)
                    AddInfoRow(LocalizedTextManager.getText("sm_company_title"), SuperMechCompany.GetDisplayText(company));
            }

            // === 特殊信息 ===
            AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_special"));
            int divLayers = SuperMechDivinity.GetTotalLayers(a);
            string sanctuaryInfo = GetSanctuaryInfo(a);
            if (divLayers > 0)
                AddInfoRow(LocalizedTextManager.getText("sm_ui_divinity"), $"{divLayers}{LocalizedTextManager.getText("sm_ui_divinity_layer")}");
            if (!string.IsNullOrEmpty(sanctuaryInfo))
                AddInfoRow(LocalizedTextManager.getText("sm_ui_sanctuary"), sanctuaryInfo);
            string speciesId = SuperMechSpeciesEvolution.GetSpeciesId(a.data.id);
            if (speciesId != "human")
            {
                AddInfoRow(LocalizedTextManager.getText("sm_ui_species"), SuperMechSpeciesEvolution.GetSpeciesName(a.data.id));
                string talent = SuperMechSpeciesEvolution.GetSpeciesTalent(a.data.id);
                if (!string.IsNullOrEmpty(talent))
                    AddInfoRow(LocalizedTextManager.getText("sm_ui_species_talent"), talent);
            }
            string mechType = SuperMechMechLegion.GetUnitType(a);
            if (!string.IsNullOrEmpty(mechType))
            {
                AddInfoRow(LocalizedTextManager.getText("sm_ui_mech_type"), SuperMechMechLegion.GetUnitName(mechType));
                string summoner = SuperMechMechLegion.GetSummonerName(a);
                if (!string.IsNullOrEmpty(summoner))
                    AddInfoRow(LocalizedTextManager.getText("sm_ui_mech_summoner"), summoner);
                int lifeLeft = SuperMechMechLegion.GetLifeLeft(a);
                if (lifeLeft > 0)
                    AddInfoRow(LocalizedTextManager.getText("sm_ui_mech_life"), $"{lifeLeft}");
            }
            string specText = SuperMechExclusiveTrait.GetDisplay(a);
            if (!string.IsNullOrEmpty(specText))
                AddInfoRow(LocalizedTextManager.getText("sm_ui_specialties"), specText);
            string tplText = SuperMechExclusiveTrait.GetTemplateDisplay(a);
            if (tplText != null)
                AddInfoRow(LocalizedTextManager.getText("sm_ui_templates"), tplText);

            // === 功能按钮（按类型分组）===
            AddSectionTitle(LocalizedTextManager.getText("sm_ui_section_functions"));

            // 成长类
            bool hasGrowth = SuperMechVirtualGenesis.HasGenesis(a) || SuperMechSpeciesEvolution.GetEvolvableSpecies(a.data.id).Count > 0;
            if (hasGrowth)
            {
                AddGroupLabel(LocalizedTextManager.getText("sm_ui_group_growth"));
                if (SuperMechVirtualGenesis.HasGenesis(a))
                    AddFunctionButton(LocalizedTextManager.getText("sm_ui_virtual_genesis"), new Color(0.5f, 0.9f, 0.8f), () => SuperMechWindowManager.OpenVirtualGenesis(a));
                var evolvable = SuperMechSpeciesEvolution.GetEvolvableSpecies(a.data.id);
                if (evolvable.Count > 0)
                    AddFunctionButton(LocalizedTextManager.getText("sm_ui_species_evolve"), new Color(0.8f, 0.9f, 0.6f), () => SuperMechSpeciesEvolution.AutoEvolve(a.data.id));
            }

            // 战斗类
            AddGroupLabel(LocalizedTextManager.getText("sm_ui_group_combat"));
            if (SuperMechBranch.HasBranch(a, SuperMechBranch.BranchMech))
                AddFunctionButton(LocalizedTextManager.getText("sm_ui_mech_legion"), new Color(0.7f, 0.8f, 1.0f), () => SuperMechWindowManager.OpenMechLegion(a));
            AddFunctionButton(LocalizedTextManager.getText("sm_ui_combat_stats"), new Color(0.9f, 0.5f, 0.4f), () => SuperMechCombatStatsView.Toggle(a));

            // 特殊类
            AddGroupLabel(LocalizedTextManager.getText("sm_ui_group_special"));
            AddFunctionButton(LocalizedTextManager.getText("sm_ui_cr_entry"), new Color(0.95f, 0.75f, 0.3f), () => SuperMechWindowManager.OpenCosmicRelic(a));
            AddFunctionButton(LocalizedTextManager.getText("sm_ui_dim_entry"), new Color(0.75f, 0.55f, 0.95f), () => SuperMechWindowManager.OpenDimension(a));

            // 任务
            AddFunctionButton(LocalizedTextManager.getText("sm_ui_quests"), new Color(0.7f, 0.6f, 0.3f), () => SuperMechQuestView.Toggle(a));
        }

        private void AddSectionTitle(string text)
        {
            // v0.237 统一走共享规范组件
            SuperMechUiBuilder.CreateSectionTitle(_content.transform, text);
        }

        private void AddInfoRow(string label, string value)
        {
            // v0.237 统一走共享规范组件（金色值）
            SuperMechUiBuilder.CreateInfoRow(_content.transform, label, value);
        }

        private void AddFunctionButton(string text, Color color, Action onClick)
        {
            var btn = SuperMechUiSkin.MakeButton(_content.transform, text, 13, () =>
            {
                try { onClick?.Invoke(); }
                catch (Exception e) { Debug.LogError("[超神机械师] 详情面板按钮异常: " + e.Message); }
            });
            var btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null) btnText.color = color;
            var layoutElem = btn.AddComponent<LayoutElement>();
            layoutElem.minHeight = 40;
            layoutElem.preferredHeight = 40;
        }

        private void AddGroupLabel(string text)
        {
            var go = new GameObject("GroupLabel");
            go.transform.SetParent(_content.transform, false);
            var rect = go.AddComponent<RectTransform>();
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 10;
            txt.fontStyle = FontStyle.Bold;
            txt.color = SuperMechUiSkin.TextDim;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = "  " + text;
            txt.raycastTarget = false;
            var layoutElem = go.AddComponent<LayoutElement>();
            layoutElem.minHeight = 20;
            layoutElem.preferredHeight = 20;
        }

        private static string GetSanctuaryInfo(Actor a)
        {
            var parts = new List<string>();
            string[] names = { "sm_sanctuary_name_1", "sm_sanctuary_name_2", "sm_sanctuary_name_3",
                               "sm_sanctuary_name_4", "sm_sanctuary_name_5", "sm_sanctuary_name_6" };
            for (int i = 0; i < 6; i++)
            {
                float auth = SuperMechSanctuary.GetAuthority(a, i);
                if (auth > 0)
                    parts.Add($"{LocalizedTextManager.getText(names[i])}:{auth:F0}");
            }
            return string.Join(" ", parts);
        }
    }
}
