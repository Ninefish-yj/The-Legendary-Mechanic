using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 知识树面板：参考天人武道神藏Tab，在单位面板添加"知识"Tab。
    /// 显示单位已解锁的知识节点（按系/分支/阶位分组），不再注册为特质。
    /// </summary>
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
                // 更新_tabs_with_content列表
                try { scroll.tabs.GetType().GetMethod("refillTabsWithContent",
                    BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(scroll.tabs, null); } catch { }
            }

            // 只有在知识Tab激活时才渲染内容
            // tab_elements机制会自动处理显示/隐藏，不需要手动HideOtherContent/RestoreOtherContent
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

            // 参考天人武道：克隆到source的父对象，放在最后面（不影响原版Tab的拖拽排序索引）
            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetAsLastSibling();

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            // 立即移除拖拽排序组件（用DestroyImmediate，避免延迟销毁期间被DragOrderContainer扫描到）
            DragOrderElement dragElem = newTab.GetComponent<DragOrderElement>();
            if (dragElem != null) Object.DestroyImmediate(dragElem);

            // 清空而不是new（参考天人武道）
            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

            // 关键：把克隆的Tab注册到_tabs列表，否则isActiveTab/showTab都不认识它
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

            // 重置tab_action
            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ =>
            {
                scroll.tabs.showTab(newTab);
                // Tab切换时触发内容刷新
                if (_boundWindow != null)
                {
                    try { Refresh(_boundWindow); } catch { }
                }
            });

            newTab.gameObject.SetActive(true);

            // 用TipButton设置文本（参考蛊真人：title+description合并到textOnClick）
            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = LocalizedTextManager.getText("sm_tab_knowledge_tip");
                tip.textOnClickDescription = string.Empty;
                tip.text_description_2 = string.Empty;
            }

            // 设置图标（精确找到图标Image，跳过背景Image）
            Image[] allImages = tabObj.GetComponentsInChildren<Image>(true);
            Image icon = null;
            foreach (Image img in allImages)
            {
                // 跳过全屏背景（sizeDelta接近Tab大小），找较小的图标
                RectTransform rt = img.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.x < 50 && rt.sizeDelta.y < 50)
                {
                    icon = img;
                    break;
                }
            }
            if (icon == null && allImages.Length > 1) icon = allImages[1]; // 第二个Image通常是图标
            if (icon != null)
            {
                try { icon.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconGenius"); } catch { }
            }

            // CanvasGroup控制可见性（参考天人武道）
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
            if (_callbacksRegistered) return;
            _callbacksRegistered = true;
        }

        private static void RenderContent(Actor actor)
        {
            if (_container == null) return;
            foreach (Transform child in _container.transform) Object.Destroy(child.gameObject);

            int pot = SuperMechPotential.GetPotential(actor);

            // 第一步：未踏入超能（无天赋倾向）
            if (!SuperMechTalent.HasTalent(actor))
            {
                AddHeader(_container.transform, "普通人");
                AddInfoRow(_container.transform, "这个单位还没有踏入超能", "点击下方按钮激发潜能，获得天赋倾向");
                AddSectionHeader(_container.transform, LocalizedTextManager.getText("sm_ui_operation"));
                AddActionButton(_container.transform, LocalizedTextManager.getText("sm_ui_awaken_potential"), () =>
                {
                    SuperMechTalent.GrantTalents(actor);
                    // 激发潜能即获得F阶（原著：踏入超能就是F阶）
                    if (!actor.hasTrait("sm_rank_00_f"))
                        actor.addTrait("sm_rank_00_f");
                    SuperMechAdvancement.SetExactRank(actor, 0);
                    SuperMechSpecialty.AssignRandomSpecialty(actor);
                    SuperMechPerks.GrantRandomPerks(actor);  // 随机赋予1-2个天赋专长
                    SuperMechQi.SetQi(actor, 100f);
                    SuperMechQi.SetQiMax(actor, 100f);
                    // 初始潜能点（原著ch48：升级获得潜能点，刚踏入超能给5点初始）
                    SuperMechPotential.SetPotential(actor, 5);
                    // 初始装备：1-2件低品质装备
                    string[] starterIds = { "sm_eq_gray", "sm_eq_green" };
                    int count = Random.Range(1, 3);
                    for (int i = 0; i < count; i++)
                        SuperMechEquipBag.AddToBag(actor, starterIds[Random.Range(0, starterIds.Length)]);
                    // 立即同步自定义属性到BaseStats（确保属性面板显示）
                    SuperMechCustomStats.SyncStats(actor);
                    Debug.Log($"[超神机械师] {actor.name} 激发潜能，踏入超能（初始5潜能点）");
                    RenderContent(actor);
                }, new Color(0.2f, 0.4f, 0.6f));
                return;
            }

            // 第二步：已踏入超能但没选定方向（野生超能者）
            if (!SuperMechProfession.HasProfession(actor))
            {
                // 显示天赋倾向（含具体异能类型）
                var talents2 = SuperMechTalent.GetTalents(actor);
                string talentText2 = "";
                foreach (var t in talents2)
                {
                    talentText2 += $"{t.specificPower}（{SuperMechTalent.GetTalentName(t.type)}·{SuperMechTalent.RatingNames[t.rating]}） ";
                }
                AddInfoRow(_container.transform, "天赋倾向", talentText2.Trim());

                AddHeader(_container.transform, "野生超能者（未选定方向）");
                AddInfoRow(_container.transform, "状态", "有天赋但没系统学习职业知识，靠本能战斗");
                AddSectionHeader(_container.transform, LocalizedTextManager.getText("sm_ui_select_class"));

                // 五个方向按钮
                var directions = new[]
                {
                    new { name = "机械系", type = SuperMechProfession.ProfessionType.Mechanical, color = new Color(0.3f, 0.5f, 0.7f) },
                    new { name = "武道系", type = SuperMechProfession.ProfessionType.Martial, color = new Color(0.7f, 0.3f, 0.3f) },
                    new { name = "异能系", type = SuperMechProfession.ProfessionType.Psi, color = new Color(0.5f, 0.3f, 0.7f) },
                    new { name = "魔法系", type = SuperMechProfession.ProfessionType.Mage, color = new Color(0.3f, 0.7f, 0.5f) },
                    new { name = "念力系", type = SuperMechProfession.ProfessionType.Mind, color = new Color(0.7f, 0.5f, 0.3f) }
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

            // 第三步：已选定方向，显示知识树
            string cls = SuperMechProfession.GetClass(actor);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            bool isAwakened = SuperMechAwakened.IsAwakened(actor);

            // 天赋倾向文本（用于详细信息框）
            var talents = SuperMechTalent.GetTalents(actor);
            string talentText = "";
            foreach (var t in talents)
            {
                talentText += $"{t.specificPower}（{SuperMechTalent.GetTalentName(t.type)}·{SuperMechTalent.RatingNames[t.rating]}） ";
            }

            // === 详细信息框（从主面板移过来，用框框起来）===
            // 框1：修炼状态
            Transform detail1 = CreateDetailBox(_container.transform, LocalizedTextManager.getText("sm_ui_cultivation"),
                new Color(0.05f, 0.08f, 0.12f, 0.9f), new Color(0.25f, 0.35f, 0.5f, 0.8f));
            string cultStatus = SuperMechCultivationStatus.GetStatusSummary(actor);
            AddInfoRow(detail1, "修炼境界", cultStatus);
            // 气力详细值
            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
            string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : "未入流";
            string qiLabel = "气力";
            if (cls == "机械系" && SuperMechStage.GetStage(actor) >= 4) qiLabel = "械力";
            AddInfoRow(detail1, qiLabel, $"{qi:F0}/{qiMax:F0}【{qiLvText}】");
            // 分系核心能量
            if (cls == "异能系")
                AddInfoRow(detail1, "基因链", $"{SuperMechCorePower.GetGeneStageName(actor)}（{SuperMechCorePower.GetGeneProgress(actor):F0}%）");
            else if (cls == "魔法系")
                AddInfoRow(detail1, "魔力池", $"{SuperMechCorePower.GetManaStageName(actor)}（{SuperMechCorePower.GetManaProgress(actor):F0}%）");
            else if (cls == "念力系")
                AddInfoRow(detail1, "精神力", $"{SuperMechCorePower.GetMindStageName(actor)}（{SuperMechCorePower.GetMindProgress(actor):F0}%）");
            // 气力属性
            string qiAttr = SuperMechQiAttribute.GetAttribute(actor);
            if (qiAttr != SuperMechQiAttribute.AttrNone)
                AddInfoRow(detail1, "气力属性", qiAttr);

            // 框2：天赋与专长
            Transform detail2 = CreateDetailBox(_container.transform, LocalizedTextManager.getText("sm_ui_talent"),
                new Color(0.06f, 0.05f, 0.1f, 0.9f), new Color(0.4f, 0.3f, 0.5f, 0.8f));
            AddInfoRow(detail2, "天赋倾向", talentText.Trim());
            if (actor.hasTrait(SuperMechTraits.ClassPsi))
            {
                var specs = SuperMechSpecialty.GetSpecialties(actor);
                if (specs.Count > 0)
                {
                    string specText = "";
                    foreach (var s in specs) specText += LocalizedTextManager.getText("trait_" + s) + " ";
                    AddInfoRow(detail2, "具体异能", specText.Trim());
                }
            }
            var perks = SuperMechPerks.GetPerks(actor);
            if (perks.Count > 0)
            {
                string perkText = "";
                foreach (var p in perks) perkText += LocalizedTextManager.getText("trait_" + p) + " ";
                AddInfoRow(detail2, "专长", perkText.Trim());
            }
            // 异能潜力评级
            if (actor.hasTrait(SuperMechTraits.ClassPsi))
            {
                string rating = SuperMechPotentialRating.GetRating(actor);
                if (!string.IsNullOrEmpty(rating))
                    AddInfoRow(detail2, "潜力评级", $"{rating}级（气力增长×{SuperMechPotentialRating.GetQiGrowthMult(actor):F1}）");
            }

            // 框3：职业信息
            Transform detail3 = CreateDetailBox(_container.transform, LocalizedTextManager.getText("sm_ui_profession"),
                new Color(0.05f, 0.1f, 0.08f, 0.9f), new Color(0.3f, 0.5f, 0.35f, 0.8f));
            if (isAwakened)
            {
                AddInfoRow(detail3, "职业等级", SuperMechAwakened.GetLevelText(actor));
                if (SuperMechAwakened.CanAdvanceStage(actor))
                {
                    AddInfoRow(detail3, "转职", "可转职！");
                    string reqText = SuperMechAdvancementTask.GetReqText(actor);
                    if (reqText != null) AddInfoRow(detail3, "转职条件", reqText);
                }
            }
            string stage = SuperMechStage.GetStageName(actor);
            if (stage != "—" && stage != "未入门") AddInfoRow(detail3, "职业阶段", stage);
            string branch = SuperMechBranch.GetBranchName(actor);
            if (branch != "未选择") AddInfoRow(detail3, "分支", branch);
            string treeName = SuperMechUnitWindow.GetKnowledgeTreeName(cls);
            int unlocked = SuperMechKnowledge.GetUnlockedCount(actor, prefix);
            AddInfoRow(detail3, "职业树", $"{treeName}（{unlocked}节点）");
            var skills = SuperMechSkills.GetLearned(actor);
            if (skills.Count > 0)
                AddInfoRow(detail3, "职业技能", string.Join("、", skills.ConvertAll(s => s.name)));
            // 副职业
            string subText = SuperMechSubClass.GetSubLevelText(actor);
            if (!string.IsNullOrEmpty(subText)) AddInfoRow(detail3, "副职业", subText);
            // 提炼法
            string refineText = SuperMechRefinement.GetStatusText(actor);
            if (!string.IsNullOrEmpty(refineText)) AddInfoRow(detail3, "提炼法", refineText);

            // 框4：特殊状态与物品
            Transform detail4 = CreateDetailBox(_container.transform, LocalizedTextManager.getText("sm_ui_special"),
                new Color(0.08f, 0.06f, 0.05f, 0.9f), new Color(0.5f, 0.4f, 0.25f, 0.8f));
            // 传说度
            int legend = SuperMechLegend.GetLegend(actor);
            if (legend > 0)
            {
                string legendText = isAwakened ?
                    $"{SuperMechLegend.GetTierName(actor)}（{legend}点，突破+{SuperMechLegend.GetBreakthroughBonus(actor):P0}）" :
                    $"{SuperMechLegend.GetTierName(actor)}（隐约感到突破契机）";
                AddInfoRow(detail4, "传说度", legendText);
            }
            // 信息态
            string infoText = SuperMechInfoState.GetStatusText(actor);
            if (!string.IsNullOrEmpty(infoText)) AddInfoRow(detail4, "信息态", infoText);
            // 冥冥感应
            var destiny = SuperMechIntuition.GetDestiny(actor);
            if (destiny != null)
                AddInfoRow(detail4, "冥冥感应", destiny.completed ? $"【{destiny.name}】已证道！" : $"【{destiny.name}】{destiny.progress:F0}/{destiny.target:F0}");
            else if (SuperMechAdvancement.GetExactRankIndex(actor) >= 10)
                AddInfoRow(detail4, "冥冥感应", "正在感应中...");
            // 超神突破
            if (SuperMechAdvancement.GetExactRankIndex(actor) >= 12 || SuperMechTranscendence.IsTranscended(actor))
            {
                AddInfoRow(detail4, "超神突破", SuperMechTranscendence.GetStatusText(actor));
                int catalyst = SuperMechTranscendence.GetCatalystLayers(actor);
                if (catalyst > 0) AddInfoRow(detail4, "神之催化", $"{catalyst}层（成功率+{catalyst * 10}%）");
            }
            // 装备
            string relic = SuperMechRelic.GetCurrentEquipName(actor);
            if (relic != "无")
                AddInfoRow(detail4, "装备", $"{relic}（耐久{SuperMechEquipBreak.GetDurability(actor):0}%）");
            // 械力融合
            if (SuperMechMechFusion.IsFused(actor))
                AddInfoRow(detail4, "械力融合", $"Lv{SuperMechMechFusion.GetFusionLevel(actor)}（属性×{SuperMechMechFusion.GetFusionMultiplier(actor):0.0}）");
            // 宇宙宝物
            string cosmic = SuperMechCosmicRelic.GetEquippedName(actor);
            if (!string.IsNullOrEmpty(cosmic)) AddInfoRow(detail4, "宇宙宝物", cosmic);
            // 法师塔
            if (actor.hasTrait(SuperMechTraits.ClassMage))
            {
                string tower = SuperMechMageTower.GetTowerName(actor);
                if (tower != "无") AddInfoRow(detail4, "法师塔", tower + "（完全状态）");
            }
            // 次级维度
            string dim = SuperMechDimension.GetActiveDimension(actor);
            if (!string.IsNullOrEmpty(dim)) AddInfoRow(detail4, "次级维度", dim + " 强化中");
            // 气势震慑状态
            if (SuperMechAura.IsStunned(actor))
                AddInfoRow(detail4, "状态", "【震慑眩晕】被高阶气势压制，无法行动");
            else if (SuperMechAura.IsSuppressed(actor))
                AddInfoRow(detail4, "状态", "【被气势震慑】速度/攻击降低");

            // 三层知识面板（信息栏+图谱层+知识库层）
            GameObject panelHost = new GameObject("KnowledgePanelHost", typeof(RectTransform));
            panelHost.transform.SetParent(_container.transform, false);
            LayoutElement panelLe = panelHost.AddComponent<LayoutElement>();
            panelLe.minHeight = 520f;
            panelLe.preferredHeight = 520f;
            panelLe.flexibleHeight = 0f;
            SuperMechKnowledgePanel.Ensure(panelHost.transform, actor);

            // 跨系兼修（原著ch611：其他分支知识潜能点费用×3）
            string[] allPrefixes = { "mech", "martial", "psi", "mage", "mind" };
            string[] allClassNames = { "机械系", "武道系", "异能系", "魔法系", "念力系" };
            for (int i = 0; i < allPrefixes.Length; i++)
            {
                if (allPrefixes[i] == prefix) continue; // 跳过主职业
                var crossDefs = SuperMechKnowledge.GetAllByPrefix(allPrefixes[i]);
                int crossUnlocked = 0;
                foreach (var d in crossDefs) if (SuperMechKnowledge.IsUnlocked(actor, d.id)) crossUnlocked++;
                if (crossUnlocked > 0) // 只有解锁了跨系知识才显示
                {
                    AddSectionHeader(_container.transform, $"跨系兼修·{allClassNames[i]}（{crossUnlocked}/{crossDefs.Count}，消耗×3）", new Color(0.5f, 0.5f, 0.7f));
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

            // 知识协同效应（特定知识组合触发额外加成）
            var synergies = SuperMechKnowledgeSynergy.GetActiveSynergies(actor);
            if (synergies.Count > 0)
            {
                AddSectionHeader(_container.transform, $"知识协同（{synergies.Count}个已激活）");
                foreach (var syn in synergies)
                {
                    string bonusText = "";
                    if (syn.dmgMul > 1f) bonusText += $"伤害+{((syn.dmgMul - 1f) * 100):0}% ";
                    if (syn.hpMul > 1f) bonusText += $"生命+{((syn.hpMul - 1f) * 100):0}% ";
                    if (syn.speedMul > 1f) bonusText += $"攻速+{((syn.speedMul - 1f) * 100):0}% ";
                    if (syn.qiBonus > 0) bonusText += $"气力+{syn.qiBonus:0} ";
                    if (syn.potentialBonus > 0) bonusText += $"潜能+{syn.potentialBonus}";
                    AddInfoRow(_container.transform, syn.name, bonusText.Trim());
                }
            }

            // 知识融合（原著ch107：消耗经验融合知识，学会后永久存脑子里）
            var fusionRecipes = SuperMechKnowledgeFusion.GetAvailableRecipes(actor);
            var learnedRecipes = SuperMechKnowledgeFusion.GetLearnedRecipes(actor);
            if (SuperMechAwakened.IsAwakened(actor))
            {
                AddSectionHeader(_container.transform, $"知识融合（已学会{learnedRecipes.Count}个）");

                // 已学会的融合知识（永久属性加成）
                bool isMech = SuperMechBranch.GetClass(actor).Contains("机械");
                foreach (var recipe in learnedRecipes)
                {
                    string bonusText = $"伤害×{recipe.dmgMul} 生命×{recipe.hpMul} 攻速×{recipe.speedMul}";
                    string craftHint = (isMech && recipe.productType == "图纸") ? " [可制造]" : "";
                    AddInfoRow(_container.transform, $"✓ {recipe.equipName}{craftHint}", $"[{recipe.productType}] {bonusText}");
                }

                // 可融合的配方
                if (fusionRecipes.Count > 0)
                {
                    AddInfoRow(_container.transform, "—— 可融合 ——", "");
                    foreach (var recipe in fusionRecipes)
                    {
                        if (learnedRecipes.Exists(r => r.id == recipe.id)) continue; // 已学会的不重复显示
                        string status = $"经验{recipe.xpCost} 成功率{(recipe.successRate * 100):0}%";
                        AddInfoRow(_container.transform, recipe.equipName, $"[{recipe.productType}] {status}");
                    }
                    AddInfoRow(_container.transform, "提示", "用神权「知识融合」随机融合一个配方");
                }
            }

            // ◆ 操作区域（械力融合/知识融合/制造装备/机械造兵）
            AddSectionHeader(_container.transform, LocalizedTextManager.getText("sm_ui_operation"));

            // 械力融合（所有超能者可用，融合装备提升属性）
            bool isFused = SuperMechMechFusion.IsFused(actor);
            string fuseBtnText = isFused ?
                $"解除械力融合（Lv{SuperMechMechFusion.GetFusionLevel(actor)} 属性×{SuperMechMechFusion.GetFusionMultiplier(actor):0.0}）" :
                "械力融合（融合装备提升属性）";
            AddActionButton(_container.transform, fuseBtnText, () =>
            {
                if (isFused) SuperMechMechFusion.Unfuse(actor);
                else SuperMechMechFusion.TryFuse(actor);
                RenderContent(actor);
            }, new Color(0.3f, 0.5f, 0.7f));

            // 知识融合（仅降临者，消耗经验随机融合配方）
            if (SuperMechAwakened.IsAwakened(actor))
            {
                bool fusionCD = SuperMechKnowledgeFusion.IsOnCooldown(actor);
                string fusionBtnText = fusionCD ? "知识融合（冷却中...）" : $"知识融合（可融合{fusionRecipes.Count}个配方）";
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

            // 制造装备（仅学会图纸的降临者）
            var learned = SuperMechKnowledgeFusion.GetLearnedRecipes(actor);
            bool craftCD = SuperMechKnowledgeFusion.IsCraftOnCooldown(actor);
            if (learned.Count > 0 && SuperMechAwakened.IsAwakened(actor))
            {
                string craftBtnText = craftCD ? "制造装备（冷却中...）" : $"制造装备（{learned.Count}张图纸）";
                AddActionButton(_container.transform, craftBtnText, () =>
                {
                    if (craftCD) return;
                    // 制造第一个可制造的图纸
                    foreach (var recipe in learned)
                    {
                        if (recipe.productType == "图纸" || recipe.productType == "秘法" || recipe.productType == "配方")
                        {
                            if (SuperMechKnowledgeFusion.CraftEquip(actor, recipe.id)) break;
                        }
                    }
                    RenderContent(actor);
                }, new Color(0.4f, 0.6f, 0.3f));
            }

            // 机械造兵（仅机械系）
            if (actor.hasTrait(SuperMechTraits.ClassMech))
            {
                var craftRecipes = SuperMechCrafting.GetAvailableRecipes(actor);
                float craftCD2 = SuperMechCrafting.GetCraftCooldown(actor);
                string craftBtnText = craftCD2 > 0 ?
                    $"机械造兵（冷却{craftCD2:F1}s，已造{SuperMechCrafting.GetMinionCount(actor)}个）" :
                    $"机械造兵（{craftRecipes.Count}种可造，已造{SuperMechCrafting.GetMinionCount(actor)}个）";
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

            // 副职业学习（所有超能者可用，消耗2潜能点学习一个未拥有的副职业）
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
                    $"学习副职业（{subLearned}/{subTotal}，消耗2潜能点）" :
                    $"学习副职业（{subLearned}/{subTotal}，潜能点不足）";
                AddActionButton(_container.transform, subBtnText, () =>
                {
                    if (SuperMechPotential.GetPotential(actor) < 2) return;
                    // 随机学习一个未拥有的副职业
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
                AddInfoRow(_container.transform, "已学副职业", subList.Trim());
            }
        }

        private static void AddHeader(Transform parent, string text)
        {
            GameObject go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.84f, 0f);
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28);
        }

        private static void AddSectionHeader(Transform parent, string text, Color? color = null)
        {
            GameObject go = new GameObject("Section", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.color = color ?? new Color(0.6f, 0.8f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 22);
        }

        private static void AddActionButton(Transform parent, string text, System.Action onClick, Color bgColor)
        {
            GameObject go = new GameObject("ActionButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(4, 4, 2, 2);

            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 12;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 操作按钮异常: " + e.Message); } });

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 26);
        }

        /// <summary>创建带框的详细信息容器（背景+1px边框），返回内容区域Transform。</summary>
        private static Transform CreateDetailBox(Transform parent, string title, Color bgColor, Color borderColor)
        {
            GameObject box = new GameObject("DetailBox_" + title, typeof(RectTransform));
            box.transform.SetParent(parent, false);
            LayoutElement le = box.AddComponent<LayoutElement>();
            le.minHeight = 40f;
            le.flexibleHeight = 0f;

            // 背景
            Image bg = box.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            // 边框（4个1px Image）
            string[] borderNames = { "Top", "Bottom", "Left", "Right" };
            Vector2[] anchorMin = { new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 0) };
            Vector2[] anchorMax = { new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            Vector2[] pivot = { new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) };
            Vector2[] sizeDelta = { new Vector2(0, 1f), new Vector2(0, 1f), new Vector2(1f, 0), new Vector2(1f, 0) };
            for (int i = 0; i < 4; i++)
            {
                GameObject border = new GameObject("Border" + borderNames[i], typeof(RectTransform));
                border.transform.SetParent(box.transform, false);
                Image bImg = border.AddComponent<Image>();
                bImg.color = borderColor;
                bImg.raycastTarget = false;
                RectTransform brt = border.GetComponent<RectTransform>();
                brt.anchorMin = anchorMin[i];
                brt.anchorMax = anchorMax[i];
                brt.pivot = pivot[i];
                brt.sizeDelta = sizeDelta[i];
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;
            }

            // 标题
            Text titleText = CreateText(box.transform, "◆ " + title, 13, TextAnchor.MiddleLeft);
            titleText.color = new Color(0.8f, 0.85f, 1f);
            RectTransform titleRt = titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 20f);
            titleRt.offsetMin = new Vector2(8, 0);
            titleRt.offsetMax = new Vector2(-8, 0);

            // 内容区域
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(box.transform, false);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(8, 8, 4, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(0, 0);
            contentRt.offsetMax = new Vector2(0, -20f);

            return content.transform;
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

        /// <summary>知识图标（原版图标+品质颜色边框，参考原版特质/物品面板）</summary>
        private static void AddKnowledgeIcon(Transform parent, Actor actor, SuperMechKnowledge.KnowledgeDef def, string branch, bool unlocked, bool canUnlock, int actualCost = 0)
        {
            Color qColor = GetTierColor(def.tier);

            GameObject go = new GameObject("KnowledgeIcon", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            // 品质边框
            var borderImg = go.AddComponent<Image>();
            borderImg.color = unlocked ? qColor : new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // 内部深色区域
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            innerGo.transform.SetParent(go.transform, false);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            RectTransform innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.08f, 0.08f);
            innerRt.anchorMax = new Vector2(0.92f, 0.92f);
            innerRt.offsetMin = Vector2.zero;
            innerRt.offsetMax = Vector2.zero;

            // 知识节点独特图标（按系别+分支+阶位组合）
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(innerGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            string iconPath = def.icon ?? GetTierIcon(def.tier);
            Sprite sprite = SpriteTextureLoader.getSprite(iconPath);
            if (sprite != null) iconImg.sprite = sprite;
            iconImg.color = unlocked ? Color.white : (canUnlock ? new Color(0.6f, 0.8f, 1f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.6f));
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            // 已解锁标记（角落✓）
            if (unlocked)
            {
                var markGo = new GameObject("Mark", typeof(RectTransform));
                markGo.transform.SetParent(go.transform, false);
                Text markText = markGo.AddComponent<Text>();
                markText.text = "✓";
                markText.fontSize = 10;
                markText.fontStyle = FontStyle.Bold;
                markText.alignment = TextAnchor.UpperRight;
                markText.color = new Color(0.5f, 1f, 0.5f);
                if (markText.font == null) markText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                RectTransform markRt = markGo.GetComponent<RectTransform>();
                markRt.anchorMin = new Vector2(0.6f, 0.6f);
                markRt.anchorMax = new Vector2(1f, 1f);
                markRt.offsetMin = new Vector2(0, -2);
                markRt.offsetMax = new Vector2(-2, 0);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 44);

            // 可解锁的点击解锁
            if (canUnlock)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = borderImg;
                btn.onClick.AddListener(() =>
                {
                    if (SuperMechPotential.UnlockNode(actor, def.id, def.cost))
                    {
                        RenderContent(actor);
                    }
                });
            }

            // Tooltip（名称+描述+消耗+分支，跨系显示智力门槛和搭配）
            var tip = go.AddComponent<TipButton>();
            bool crossClass = SuperMechPotential.IsCrossClass(actor, def.id);
            bool hasSynergy = SuperMechPotential.HasPowerSynergy(actor, SuperMechPotential.GetKnowledgePrefix(def.id));
            float intel = actor.stats["intelligence"];
            string costText = $"{actualCost}潜能点";
            if (crossClass)
            {
                string intelText = intel < 10f ? "（智力不足，×5）" : intel >= 20f ? "（高智力，×2）" : "（跨系×3）";
                string synergyText = hasSynergy ? " 异能搭配-30%" : "";
                costText = $"{actualCost}潜能点 {intelText}{synergyText}";
            }
            else if (hasSynergy)
            {
                costText = $"{actualCost}潜能点（异能搭配-30%）";
            }
            tip.textOnClick = $"{def.name}\n{def.desc}\n分支: {branch} | 消耗: {costText}";
        }

        /// <summary>按阶位获取品质颜色（基础=灰，进阶=绿，高端=蓝，尖端=紫，终极=金）</summary>
        private static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.6f, 0.6f, 0.6f); // 基础-灰
                case 1: return new Color(0.3f, 0.8f, 0.3f); // 进阶-绿
                case 2: return new Color(0.3f, 0.5f, 1f);   // 高端-蓝
                case 3: return new Color(0.7f, 0.4f, 1f);   // 尖端-紫
                case 4: return new Color(1f, 0.84f, 0f);    // 终极-金
                default: return Color.white;
            }
        }

        /// <summary>按阶位获取原版图标</summary>
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

        /// <summary>图标网格行（每行4个）</summary>
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
