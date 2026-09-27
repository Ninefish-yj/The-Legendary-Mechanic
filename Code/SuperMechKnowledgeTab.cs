using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]


    public static class SuperMechKnowledgeTab
    {
        private const string TabName = "SuperMechKnowledgeTab";
        private const string ContainerName = "SuperMechKnowledgeContent";

        private static bool _callbacksRegistered;
        private static WindowMetaTab _knowTab;
        private static GameObject _container;
        private static UnitWindow _boundWindow;
        private static readonly FieldInfo TabsListField =
            AccessTools.Field(typeof(WindowMetaTabButtonsContainer), "_tabs");

        public static void Postfix(UnitWindow __instance)
        {
            try { Refresh(__instance); } catch { }
        }

        private static void Refresh(UnitWindow window)
        {
            if (window == null) return;
            Actor actor = GetActor(window);
            if (actor == null || !actor.isAlive()) return;

            ScrollWindow scroll = window.scroll_window;
            if (scroll == null)
            {
                Component host = (Component)(object)window;
                scroll = host.GetComponent<ScrollWindow>() ?? host.GetComponentInParent<ScrollWindow>();
            }
            if (scroll?.tabs == null) return;

            WindowMetaTab tab = FindOrCreateTab(scroll, window);
            if (tab == null) return;

            tab.gameObject.SetActive(true);
            try { tab.toggleActive(true); } catch { }
            EnsureContainer(scroll);
            WireTab(tab, scroll);
            RegisterCallbacks(scroll);

            if (_container != null && !tab.tab_elements.Contains(_container.transform))
            {
                tab.tab_elements.Add(_container.transform);
                try { scroll.tabs.GetType().GetMethod("refillTabsWithContent",
                    BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(scroll.tabs, null); } catch { }
            }

            bool onKnowTab = scroll.tabs != null && scroll.tabs.isActiveTab(tab);
            if (onKnowTab)
            {
                RenderContent(actor);
            }
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                FieldInfo fi = typeof(UnitWindow).GetField("_actor", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null) return fi.GetValue(window) as Actor;
                PropertyInfo pi = typeof(UnitWindow).GetProperty("actor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null) return pi.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        private static WindowMetaTab FindOrCreateTab(ScrollWindow scroll, UnitWindow window)
        {
            if (_knowTab != null && _boundWindow == window) return _knowTab;

            foreach (WindowMetaTab t in scroll.tabs.GetComponentsInChildren<WindowMetaTab>(true))
            {
                if (t.name == TabName) { _knowTab = t; _boundWindow = window; return t; }
            }

            WindowMetaTab source = FindCloneSource(scroll.tabs);
            if (source == null) return null;

            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetAsLastSibling();

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            DragOrderElement dragElem = newTab.GetComponent<DragOrderElement>();
            if (dragElem != null) Object.DestroyImmediate(dragElem);

            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

            try
            {
                var tabsField = typeof(WindowMetaTabButtonsContainer).GetField("_tabs",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (tabsField != null)
                {
                    var tabsList = tabsField.GetValue(scroll.tabs) as List<WindowMetaTab>;
                    if (tabsList != null && !tabsList.Contains(newTab))
                    {
                        tabsList.Add(newTab);
                        newTab.container = scroll.tabs;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 注册知识Tab到_tabs失败: " + e.Message);
            }

            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ =>
            {
                scroll.tabs.showTab(newTab);
                if (_boundWindow != null)
                {
                    try { Refresh(_boundWindow); } catch { }
                }
            });

            newTab.gameObject.SetActive(true);

            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = LocalizedTextManager.getText("sm_tab_knowledge_tip");
                tip.textOnClickDescription = string.Empty;
                tip.text_description_2 = string.Empty;
            }

            Image[] allImages = tabObj.GetComponentsInChildren<Image>(true);
            Image icon = null;
            foreach (Image img in allImages)
            {
                RectTransform rt = img.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.x < 50 && rt.sizeDelta.y < 50)
                {
                    icon = img;
                    break;
                }
            }
            if (icon == null && allImages.Length > 1) icon = allImages[1];
            if (icon != null)
            {
                try { icon.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconGenius"); } catch { }
            }

            CanvasGroup cg = newTab.GetComponent<CanvasGroup>();
            if (cg == null) cg = newTab.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            _knowTab = newTab;
            _boundWindow = window;
            return newTab;
        }

        private static WindowMetaTab FindCloneSource(WindowMetaTabButtonsContainer container)
        {
            WindowMetaTab[] all = container.GetComponentsInChildren<WindowMetaTab>(true);
            foreach (WindowMetaTab t in all)
            {
                if (t.name != TabName && t.gameObject.activeSelf) return t;
            }
            return all.Length > 0 ? all[0] : null;
        }

        private static void EnsureContainer(ScrollWindow scroll)
        {
            if (_container != null) return;
            Transform scrollContent = scroll.transform_content;
            Transform existing = scrollContent.Find(ContainerName);
            if (existing != null)
            {
                _container = existing.gameObject;
                return;
            }
            _container = new GameObject(ContainerName, typeof(RectTransform));
            _container.transform.SetParent(scrollContent, false);

            RectTransform rt = _container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ContentSizeFitter fitter = _container.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = _container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            _container.SetActive(false);
        }

        private static void WireTab(WindowMetaTab tab, ScrollWindow scroll)
        {
            if (tab.tab_action == null)
            {
                tab.tab_action = new WindowMetaTabEvent();
                tab.tab_action.AddListener(_ => scroll.tabs.showTab(tab));
            }
        }

        private static void RegisterCallbacks(ScrollWindow scroll)
        {
            if (_callbacksRegistered || scroll.tabs == null) return;
            _callbacksRegistered = true;
            scroll.tabs.addTabShowCallback(OnTabShow);
            scroll.tabs.addTabHideCallback(OnTabHide);
        }

        private static void OnTabShow(WindowMetaTab tab)
        {
            if (tab != _knowTab) return;
            Actor actor = GetActor(_boundWindow);
            if (actor != null) RenderContent(actor);
        }

        private static void OnTabHide()
        {
        }

        private static void RenderContent(Actor actor)
        {
            if (_container == null) return;
            foreach (Transform child in _container.transform) Object.Destroy(child.gameObject);

            int pot = SuperMechPotential.GetPotential(actor);

            if (!SuperMechTalent.HasTalent(actor))
            {
                GameObject mortalCard = new GameObject("MortalCard", typeof(RectTransform));
                mortalCard.transform.SetParent(_container.transform, false);
                LayoutElement mcLE = mortalCard.AddComponent<LayoutElement>();
                mcLE.minHeight = 200f;
                mcLE.flexibleHeight = 0f;
                Image mcBg = mortalCard.AddComponent<Image>();
                mcBg.color = new Color(0.05f, 0.06f, 0.09f, 0.95f);
                Image mcBorder = mortalCard.AddComponent<Image>();
                mcBorder.color = new Color(0.3f, 0.35f, 0.45f, 0.5f);

                GameObject iconContainer = new GameObject("BigIcon", typeof(RectTransform));
                iconContainer.transform.SetParent(mortalCard.transform, false);
                Image bigIcon = iconContainer.AddComponent<Image>();
                Sprite mortalSprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconWeak");
                if (mortalSprite != null) bigIcon.sprite = mortalSprite;
                bigIcon.color = new Color(0.5f, 0.55f, 0.65f, 0.8f);
                RectTransform bigIconRt = iconContainer.GetComponent<RectTransform>();
                bigIconRt.anchorMin = new Vector2(0.5f, 0.55f);
                bigIconRt.anchorMax = new Vector2(0.5f, 0.55f);
                bigIconRt.pivot = new Vector2(0.5f, 0.5f);
                bigIconRt.sizeDelta = new Vector2(64, 64);

                GameObject titleContainer = new GameObject("MortalTitle", typeof(RectTransform));
                titleContainer.transform.SetParent(mortalCard.transform, false);
                Text mortalTitle = titleContainer.AddComponent<Text>();
                mortalTitle.text = LocalizedTextManager.getText("sm_ui_mortal_title");
                mortalTitle.fontSize = 22;
                mortalTitle.fontStyle = FontStyle.Bold;
                mortalTitle.color = new Color(0.85f, 0.75f, 0.4f);
                mortalTitle.alignment = TextAnchor.MiddleCenter;
                mortalTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (mortalTitle.font == null) mortalTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform titleRt = titleContainer.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 0.35f);
                titleRt.anchorMax = new Vector2(1, 0.35f);
                titleRt.pivot = new Vector2(0.5f, 0.5f);
                titleRt.sizeDelta = new Vector2(0, 28);

                GameObject descContainer = new GameObject("MortalDesc", typeof(RectTransform));
                descContainer.transform.SetParent(mortalCard.transform, false);
                Text mortalDesc = descContainer.AddComponent<Text>();
                mortalDesc.text = LocalizedTextManager.getText("sm_ui_mortal_desc1") + "\n" + LocalizedTextManager.getText("sm_ui_mortal_desc2");
                mortalDesc.fontSize = 12;
                mortalDesc.color = new Color(0.6f, 0.65f, 0.75f);
                mortalDesc.alignment = TextAnchor.MiddleCenter;
                mortalDesc.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (mortalDesc.font == null) mortalDesc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform descRt = descContainer.GetComponent<RectTransform>();
                descRt.anchorMin = new Vector2(0.1f, 0.18f);
                descRt.anchorMax = new Vector2(0.9f, 0.18f);
                descRt.pivot = new Vector2(0.5f, 0.5f);
                descRt.sizeDelta = new Vector2(0, 40);

                GameObject btnContainer = new GameObject("AwakenBtn", typeof(RectTransform));
                btnContainer.transform.SetParent(mortalCard.transform, false);
                Image btnBg = btnContainer.AddComponent<Image>();
                btnBg.color = new Color(0.15f, 0.3f, 0.5f, 0.9f);
                Button awakenBtn = btnContainer.AddComponent<Button>();
                awakenBtn.targetGraphic = btnBg;
                awakenBtn.onClick.AddListener(() =>
                {
                    SuperMechTalent.GrantTalents(actor);
                    if (!actor.hasTrait("sm_rank_00_f"))
                        actor.addTrait("sm_rank_00_f");
                    SuperMechAdvancement.SetExactRank(actor, 0);
                    SuperMechSpecialty.AssignRandomSpecialty(actor);
                    SuperMechPerks.GrantRandomPerks(actor);
                    SuperMechQi.SetQi(actor, 100f);
                    SuperMechQi.SetQiMax(actor, 100f);
                    SuperMechPotential.SetPotential(actor, 5);
                    string[] starterIds = { "sm_eq_gray_ring", "sm_eq_green_boots" };
                    int count = Random.Range(1, 3);
                    for (int i = 0; i < count; i++)
                        SuperMechEquipBag.AddToBag(actor, starterIds[Random.Range(0, starterIds.Length)]);
                    SuperMechCustomStats.SyncStats(actor);
                    Debug.Log($"[超神机械师] {actor.name} 激发潜能，踏入超能（初始5潜能点）");
                    RenderContent(actor);
                });
                Text btnText = btnContainer.AddComponent<Text>();
                btnText.text = LocalizedTextManager.getText("sm_ui_awaken_potential");
                btnText.fontSize = 14;
                btnText.fontStyle = FontStyle.Bold;
                btnText.color = Color.white;
                btnText.alignment = TextAnchor.MiddleCenter;
                if (btnText.font == null) btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform btnRt = btnContainer.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.3f, 0.05f);
                btnRt.anchorMax = new Vector2(0.7f, 0.05f);
                btnRt.pivot = new Vector2(0.5f, 0.5f);
                btnRt.sizeDelta = new Vector2(0, 32);
                return;
            }

            if (!SuperMechProfession.HasProfession(actor))
            {
                var talents2 = SuperMechTalent.GetTalents(actor);
                string talentText2 = "";
                foreach (var t in talents2)
                {
                    talentText2 += $"{t.specificPower}（{SuperMechTalent.GetTalentName(t.type)}·{SuperMechTalent.RatingNames[t.rating]}） ";
                }
                AddInfoRow(_container.transform, LocalizedTextManager.getText("sm_ui_talent_tendency"), talentText2.Trim());

                AddHeader(_container.transform, LocalizedTextManager.getText("sm_ui_wild_title"));
                AddInfoRow(_container.transform, LocalizedTextManager.getText("sm_ui_status"), LocalizedTextManager.getText("sm_ui_wild_desc"));
                AddSectionHeader(_container.transform, LocalizedTextManager.getText("sm_ui_select_class"));

                var directions = new[]
                {
                    new { name = LocalizedTextManager.getText("sm_class_mech"), type = SuperMechProfession.ProfessionType.Mechanical, color = new Color(0.3f, 0.5f, 0.7f) },
                    new { name = LocalizedTextManager.getText("sm_class_martial"), type = SuperMechProfession.ProfessionType.Martial, color = new Color(0.7f, 0.3f, 0.3f) },
                    new { name = LocalizedTextManager.getText("sm_class_psi"), type = SuperMechProfession.ProfessionType.Psi, color = new Color(0.5f, 0.3f, 0.7f) },
                    new { name = LocalizedTextManager.getText("sm_class_mage"), type = SuperMechProfession.ProfessionType.Mage, color = new Color(0.3f, 0.7f, 0.5f) },
                    new { name = LocalizedTextManager.getText("sm_class_mind"), type = SuperMechProfession.ProfessionType.Mind, color = new Color(0.7f, 0.5f, 0.3f) }
                };
                foreach (var d in directions)
                {
                    AddActionButton(_container.transform, d.name, () =>
                    {
                        SuperMechProfession.SetProfession(actor, d.type);
                        Debug.Log($"[超神机械师] {actor.name} 选定主职业方向：{d.name}");
                        RenderContent(actor);
                    }, d.color);
                }
                return;
            }

            string cls = SuperMechProfession.GetClass(actor);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);

            GameObject panelHost = new GameObject("KnowledgePanelHost", typeof(RectTransform));
            panelHost.transform.SetParent(_container.transform, false);
            LayoutElement panelLe = panelHost.AddComponent<LayoutElement>();
            panelLe.minHeight = 0f;
            panelLe.preferredHeight = -1f;
            panelLe.flexibleHeight = 0f;
            SuperMechKnowledgePanel.Ensure(panelHost.transform, actor);

            string[] allPrefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] allClassKeys = { "sm_class_mech", "sm_class_martial", "sm_class_psi", "sm_class_mage", "sm_class_mind" };
            string[] allClassNames = System.Array.ConvertAll(allClassKeys, k => LocalizedTextManager.getText(k));
            for (int i = 0; i < allPrefixes.Length; i++)
            {
                if (allPrefixes[i] == prefix) continue;
                var crossDefs = SuperMechKnowledge.GetAllByPrefix(allPrefixes[i]);
                int crossUnlocked = 0;
                foreach (var d in crossDefs) if (SuperMechKnowledge.IsUnlocked(actor, d.id)) crossUnlocked++;
                if (crossUnlocked > 0)
                {
                    AddSectionHeader(_container.transform, $"sm_knowledgetab_844", new Color(0.5f, 0.5f, 0.7f));
                    GameObject crossRow = null;
                    int crossIdx = 0;
                    foreach (var def in crossDefs)
                    {
                        if (crossIdx % 4 == 0) crossRow = AddIconRow(_container.transform);
                        bool cUnlocked = SuperMechKnowledge.IsUnlocked(actor, def.id);
                        int cActualCost = SuperMechPotential.GetActualCost(actor, def.id, def.cost);
                        bool cCanUnlock = !cUnlocked && pot >= cActualCost;
                        AddKnowledgeIcon(crossRow.transform, actor, def, allClassNames[i], cUnlocked, cCanUnlock, cActualCost);
                        crossIdx++;
                    }
                }
            }

            var synergies = SuperMechKnowledgeSynergy.GetActiveSynergies(actor);
            if (synergies.Count > 0)
            {
                AddSectionHeader(_container.transform, $"sm_knowledgetab_845");
                foreach (var syn in synergies)
                {
                    string bonusText = "";
                    if (syn.dmgMul > 1f) bonusText += $"{LocalizedTextManager.getText("sm_ui_damage")}+{((syn.dmgMul - 1f) * 100):0}% ";
                    if (syn.hpMul > 1f) bonusText += $"{LocalizedTextManager.getText("sm_ui_health")}+{((syn.hpMul - 1f) * 100):0}% ";
                    if (syn.speedMul > 1f) bonusText += $"{LocalizedTextManager.getText("sm_ui_attack_speed")}+{((syn.speedMul - 1f) * 100):0}% ";
                    if (syn.qiBonus > 0) bonusText += $"{LocalizedTextManager.getText("sm_qi_qi")}+{syn.qiBonus:0} ";
                    if (syn.potentialBonus > 0) bonusText += $"{LocalizedTextManager.getText("sm_ui_potential")}+{syn.potentialBonus}";
                    AddInfoRow(_container.transform, syn.name, bonusText.Trim());
                }
            }

            var fusionRecipes = SuperMechKnowledgeFusion.GetAvailableRecipes(actor);
            var learnedRecipes = SuperMechKnowledgeFusion.GetLearnedRecipes(actor);
            if (SuperMechAwakened.IsAwakened(actor))
            {
                AddSectionHeader(_container.transform, $"sm_knowledgetab_846");

                bool isMech = SuperMechBranch.GetClass(actor).Contains("sm_knowledgetab_847");
                foreach (var recipe in learnedRecipes)
                {
                    string bonusText = $"sm_knowledgetab_848";
                    string craftHint = (isMech && recipe.productType == "sm_knowledgetab_849") ? "sm_knowledgetab_850" : "";
                    AddInfoRow(_container.transform, $"✓ {recipe.equipName}{craftHint}", $"[{recipe.productType}] {bonusText}");
                }

                if (fusionRecipes.Count > 0)
                {
                    AddInfoRow(_container.transform, "sm_knowledgetab_851", "");
                    foreach (var recipe in fusionRecipes)
                    {
                        if (learnedRecipes.Exists(r => r.id == recipe.id)) continue;
                        string status = $"sm_knowledgetab_852";
                        AddInfoRow(_container.transform, recipe.equipName, $"[{recipe.productType}] {status}");
                    }
                    AddInfoRow(_container.transform, "sm_knowledgetab_853", "sm_knowledgetab_854");
                }
            }

            AddSectionHeader(_container.transform, LocalizedTextManager.getText("sm_ui_operation"));

            bool isFused = SuperMechMechFusion.IsFused(actor);
            string fuseBtnText = isFused ?
                $"sm_knowledgetab_855" :
                "sm_knowledgetab_856";
            AddActionButton(_container.transform, fuseBtnText, () =>
            {
                if (isFused) SuperMechMechFusion.Unfuse(actor);
                else SuperMechMechFusion.TryFuse(actor);
                RenderContent(actor);
            }, new Color(0.3f, 0.5f, 0.7f));

            if (SuperMechAwakened.IsAwakened(actor))
            {
                bool fusionCD = SuperMechKnowledgeFusion.IsOnCooldown(actor);
                string fusionBtnText = fusionCD ? "sm_knowledgetab_857" : $"sm_knowledgetab_858";
                AddActionButton(_container.transform, fusionBtnText, () =>
                {
                    if (fusionCD) return;
                    var available = SuperMechKnowledgeFusion.GetAvailableRecipes(actor);
                    if (available.Count > 0)
                    {
                        var recipe = available[Random.Range(0, available.Count)];
                        SuperMechKnowledgeFusion.TryFuse(actor, recipe.id);
                    }
                    RenderContent(actor);
                }, new Color(0.5f, 0.3f, 0.6f));
            }

            var learned = SuperMechKnowledgeFusion.GetLearnedRecipes(actor);
            bool craftCD = SuperMechKnowledgeFusion.IsCraftOnCooldown(actor);
            if (learned.Count > 0 && SuperMechAwakened.IsAwakened(actor))
            {
                string craftBtnText = craftCD ? "sm_knowledgetab_859" : $"sm_knowledgetab_860";
                AddActionButton(_container.transform, craftBtnText, () =>
                {
                    if (craftCD) return;
                    foreach (var recipe in learned)
                    {
                        if (recipe.productType == "sm_knowledgetab_849" || recipe.productType == "sm_knowledgetab_861" || recipe.productType == "sm_knowledgetab_862")
                        {
                            if (SuperMechKnowledgeFusion.CraftEquip(actor, recipe.id)) break;
                        }
                    }
                    RenderContent(actor);
                }, new Color(0.4f, 0.6f, 0.3f));
            }

            if (actor.hasTrait(SuperMechTraits.ClassMech))
            {
                var craftRecipes = SuperMechCrafting.GetAvailableRecipes(actor);
                float craftCD2 = SuperMechCrafting.GetCraftCooldown(actor);
                string craftBtnText = craftCD2 > 0 ?
                    $"sm_knowledgetab_863" :
                    $"sm_knowledgetab_864";
                AddActionButton(_container.transform, craftBtnText, () =>
                {
                    if (craftCD2 > 0) return;
                    if (craftRecipes.Count > 0)
                    {
                        var recipe = craftRecipes[Random.Range(0, craftRecipes.Count)];
                        SuperMechCrafting.TryCraft(actor, recipe.id, actor.current_tile);
                    }
                    RenderContent(actor);
                }, new Color(0.6f, 0.4f, 0.2f));
            }

            int subLearned = 0;
            string subList = "";
            foreach (string subId in SuperMechSubClass.AllSubClasses)
            {
                if (actor.hasTrait(subId))
                {
                    subLearned++;
                    string subName = subId.Replace("sm_sub_", "");
                    subList += subName + " ";
                }
            }
            int subTotal = SuperMechSubClass.AllSubClasses.Length;
            if (subLearned < subTotal)
            {
                string subBtnText = pot >= 2 ?
                    $"sm_knowledgetab_865" :
                    $"sm_knowledgetab_866";
                AddActionButton(_container.transform, subBtnText, () =>
                {
                    if (SuperMechPotential.GetPotential(actor) < 2) return;
                    var unlearned = new System.Collections.Generic.List<string>();
                    foreach (string subId in SuperMechSubClass.AllSubClasses)
                    {
                        if (!actor.hasTrait(subId)) unlearned.Add(subId);
                    }
                    if (unlearned.Count > 0)
                    {
                        string newSub = unlearned[Random.Range(0, unlearned.Count)];
                        actor.addTrait(newSub);
                        SuperMechPotential.AddPotential(actor, -2);
                        Debug.Log($"[超神机械师] {actor.name} 学习副职业: {newSub}");
                    }
                    RenderContent(actor);
                }, new Color(0.4f, 0.4f, 0.6f));
            }
            if (subLearned > 0)
            {
                AddInfoRow(_container.transform, "sm_knowledgetab_867", subList.Trim());
            }
        }

        private static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (LocalizedTextManager.current_font != null) txt.font = LocalizedTextManager.current_font;
            return txt;
        }

        private static void AddInfoRow(Transform parent, string left, string right)
        {
            left = LocalizedTextManager.getText(left);
            right = LocalizedTextManager.getText(right);
            GameObject go = new GameObject("InfoRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            GameObject leftGo = new GameObject("Left", typeof(RectTransform));
            leftGo.transform.SetParent(go.transform, false);
            Text lt = leftGo.AddComponent<Text>();
            lt.text = left;
            lt.fontSize = 12;
            lt.color = Color.white;
            lt.horizontalOverflow = HorizontalWrapMode.Overflow;
            lt.verticalOverflow = VerticalWrapMode.Truncate;
            if (lt.font == null) lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var leftLE = leftGo.AddComponent<LayoutElement>();
            leftLE.minWidth = 80;
            leftLE.flexibleWidth = 1;

            GameObject rightGo = new GameObject("Right", typeof(RectTransform));
            rightGo.transform.SetParent(go.transform, false);
            Text rt2 = rightGo.AddComponent<Text>();
            rt2.text = right;
            rt2.fontSize = 12;
            rt2.color = new Color(0.7f, 0.7f, 0.7f);
            rt2.alignment = TextAnchor.MiddleRight;
            rt2.horizontalOverflow = HorizontalWrapMode.Overflow;
            rt2.verticalOverflow = VerticalWrapMode.Truncate;
            if (rt2.font == null) rt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rightLE = rightGo.AddComponent<LayoutElement>();
            rightLE.minWidth = 80;
            rightLE.flexibleWidth = 2;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
        }

        private static void AddKnowledgeIcon(Transform parent, Actor actor, SuperMechKnowledge.KnowledgeDef def, string branch, bool unlocked, bool canUnlock, int actualCost = 0)
        {
            SuperMechKnowledgeButton btn = SuperMechKnowledgeButton.Create(parent);
            btn.gameObject.SetActive(true);

            string iconPath = def.icon ?? GetTierIcon(def.tier);
            Sprite sprite = SpriteTextureLoader.getSprite(iconPath);
            btn.Setup(def.id, sprite, unlocked, canUnlock, def.tier);

            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            if (canUnlock)
            {
                btn.button.onClick.AddListener(() =>
                {
                    if (SuperMechPotential.UnlockNode(actor, def.id, def.cost))
                    {
                        RenderContent(actor);
                    }
                });
            }

            var tip = btn.gameObject.AddComponent<TipButton>();
            bool crossClass = SuperMechPotential.IsCrossClass(actor, def.id);
            bool hasSynergy = SuperMechPotential.HasPowerSynergy(actor, SuperMechPotential.GetKnowledgePrefix(def.id));
            float intel = actor.stats["intelligence"];
            string costText = $"sm_knowledgetab_868";
            if (crossClass)
            {
                string intelText = intel < 10f ? "sm_knowledgetab_869" : intel >= 20f ? "sm_knowledgetab_870" : "sm_knowledgetab_871";
                string synergyText = hasSynergy ? "sm_knowledgetab_872" : "";
                costText = $"sm_knowledgetab_873";
            }
            else if (hasSynergy)
            {
                costText = $"sm_knowledgetab_874";
            }
            tip.textOnClick = LocalizedTextManager.getText("sm_knowledgetab_875");
        }

        private static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.6f, 0.6f, 0.6f);
                case 1: return new Color(0.3f, 0.8f, 0.3f);
                case 2: return new Color(0.3f, 0.5f, 1f);
                case 3: return new Color(0.7f, 0.4f, 1f);
                case 4: return new Color(1f, 0.84f, 0f);
                default: return Color.white;
            }
        }

        private static string GetTierIcon(int tier)
        {
            switch (tier)
            {
                case 0: return "ui/Icons/skills/iconSkillBlock";
                case 1: return "ui/Icons/skills/iconSkillDash";
                case 2: return "ui/Icons/skills/iconSkillDodge";
                case 3: return "ui/Icons/skills/iconSkillBackstep";
                case 4: return "ui/Icons/skills/iconSkillDeflectProjectile";
                default: return "ui/Icons/skills/iconSkillBlock";
            }
        }

        private static GameObject AddIconRow(Transform parent)
        {
            GameObject go = new GameObject("IconRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 42);
            return go;
        }
    }
}
