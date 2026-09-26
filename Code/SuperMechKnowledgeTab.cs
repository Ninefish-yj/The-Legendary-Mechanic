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
                tab.tab_elements.Clear();
                tab.tab_elements.Add(_container.transform);
            }

            // 只有在知识Tab激活时才渲染内容，避免内容出现在其他Tab（如原版装备Tab）中
            bool onKnowTab = scroll.tabs != null && scroll.tabs.isActiveTab(tab);
            if (!onKnowTab && _container != null) _container.SetActive(false);
            if (onKnowTab) RenderContent(actor);
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

            // 参考天人武道：克隆到source的父对象，设置兄弟索引
            GameObject tabObj = Object.Instantiate(source.gameObject, source.transform.parent);
            tabObj.name = TabName;
            tabObj.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

            WindowMetaTab newTab = tabObj.GetComponent<WindowMetaTab>();
            if (newTab == null) return null;

            // 清空而不是new（参考天人武道）
            if (newTab.tab_elements != null) newTab.tab_elements.Clear();
            else newTab.tab_elements = new List<Transform>();

            // 重置tab_action
            if (newTab.tab_action == null) newTab.tab_action = new WindowMetaTabEvent();
            else newTab.tab_action.RemoveAllListeners();
            newTab.tab_action.AddListener(_ => scroll.tabs.showTab(newTab));

            newTab.gameObject.SetActive(true);

            // 用TipButton设置文本（参考蛊真人：title+description合并到textOnClick）
            TipButton tip = newTab.GetComponent<TipButton>();
            if (tip != null)
            {
                tip.textOnClick = "知识\n查看已解锁的知识节点与职业树";
                tip.text_description_2 = string.Empty;
            }

            // 设置图标
            Image icon = tabObj.GetComponentInChildren<Image>();
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

            // 未觉醒单位：显示觉醒按钮
            if (!SuperMechAdvancement.IsSuperMechUnit(actor))
            {
                AddHeader(_container.transform, "未觉醒");
                AddInfoRow(_container.transform, "这个单位还没有觉醒超能系", "点击下方按钮选择觉醒系别");
                AddSectionHeader(_container.transform, "◆ 操作");
                AddActionButton(_container.transform, "五系觉醒", () => SuperMechAwakenWindow.Show(actor), new Color(0.2f, 0.4f, 0.6f));
                return;
            }

            string cls = SuperMechBranch.GetClass(actor);
            string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
            string treeName = GetTreeName(prefix);

            // 标题
            AddHeader(_container.transform, $"{treeName}（{cls}）");

            // 潜能点/觉醒点
            int pot = SuperMechPotential.GetPotential(actor);
            int awk = SuperMechPotential.GetAwakening(actor);
            AddInfoRow(_container.transform, $"潜能点: {pot}", $"觉醒点: {awk}");

            // 操作区域（整合原超能者面板功能）
            AddSectionHeader(_container.transform, "◆ 操作");
            string stage = SuperMechStage.GetStageName(actor);
            string branch = SuperMechBranch.GetBranchName(actor);
            AddInfoRow(_container.transform, $"职业阶段: {stage}", $"分支: {(string.IsNullOrEmpty(branch) ? "未选择" : branch)}");
            if (SuperMechAwakened.CanAdvanceStage(actor))
                AddInfoRow(_container.transform, "转职", "可转职！完成进阶任务后自动转职");
            if (cls == "机械系")
                AddInfoRow(_container.transform, "制造", "机械系可制造机械单位（需达到对应阶段）");

            // 神之催化按钮（仅SS阶以上显示）
            int rankIdx = SuperMechAdvancement.GetExactRankIndex(actor);
            if (rankIdx >= 12) // SS阶以上
            {
                int layers = SuperMechTranscendence.GetCatalystLayers(actor);
                string btnText = layers > 0 ? $"神之催化（{layers}/5层）" : "神之催化";
                AddActionButton(_container.transform, btnText, () =>
                {
                    if (SuperMechTranscendence.CatalyzeBreakthrough(actor))
                    {
                        int newLayers = SuperMechTranscendence.GetCatalystLayers(actor);
                        Debug.Log($"[超神机械师] 神之催化：{actor.name} 获得第{newLayers}层催化");
                        RenderContent(actor); // 刷新面板
                    }
                }, new Color(0.6f, 0.4f, 0.1f));
            }

            AddInfoRow(_container.transform, "提示", "点击蓝色卡片解锁知识");

            // 按阶位分组显示所有知识（卡片式网格布局，参考魔兽世界天赋树）
            string[] tierNames = { "基础", "进阶", "高端", "尖端", "终极" };
            string[] branchNames = GetBranchNames(prefix);

            int totalUnlocked = 0;
            int totalAll = 0;
            for (int tier = 0; tier < 5; tier++)
            {
                var allDefs = SuperMechKnowledge.GetAllByTier(prefix, tier);
                if (allDefs.Count == 0) continue;
                totalAll += allDefs.Count;

                int tierUnlocked = 0;
                foreach (var def in allDefs)
                {
                    if (SuperMechKnowledge.IsUnlocked(actor, def.id)) tierUnlocked++;
                }
                totalUnlocked += tierUnlocked;

                // 前置依赖：上一阶至少学1个才能学下一阶
                bool tierUnlocked_flag = (tier == 0) || SuperMechKnowledge.GetTierKnowledgeCount(actor, prefix, tier - 1) > 0;

                // 阶位标题+进度条
                AddSectionHeader(_container.transform, $"{tierNames[tier]}知识{(tierUnlocked_flag ? "" : " 🔒需先学上一阶")}");
                AddProgressBar(_container.transform, tierUnlocked, allDefs.Count, "进度");

                // 网格布局：每行2个卡片
                GameObject currentRow = null;
                int cardIndex = 0;
                foreach (var def in allDefs)
                {
                    if (cardIndex % 2 == 0)
                        currentRow = AddGridRow(_container.transform);

                    bool unlocked = SuperMechKnowledge.IsUnlocked(actor, def.id);
                    bool canUnlock = !unlocked && tierUnlocked_flag && pot >= def.cost;
                    string branchName = def.branch < branchNames.Length ? branchNames[def.branch] : "?";
                    AddKnowledgeRow(currentRow.transform, actor, def, branchName, unlocked, canUnlock);
                    cardIndex++;
                }
            }

            // 总计
            AddHeader(_container.transform, $"已解锁: {totalUnlocked} / {totalAll}");

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
        }

        private static string GetTreeName(string prefix)
        {
            switch (prefix)
            {
                case "mech": return "机械知识树";
                case "martial": return "御气技巧树";
                case "mage": return "魔法知识树";
                case "mind": return "精神修炼树";
                case "psi": return "基因树";
                default: return "知识树";
            }
        }

        private static string[] GetBranchNames(string prefix)
        {
            switch (prefix)
            {
                case "mech": return new[] { "枪炮师", "机械师", "械武者" };
                case "martial": return new[] { "敏捷", "力量", "防御" };
                case "mage": return new[] { "专精法师", "魔网法师", "元素" };
                case "mind": return new[] { "灵魂", "法则", "现实" };
                case "psi": return new[] { "能级", "操控", "持久力" };
                default: return new[] { "?", "?", "?" };
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

        private static void AddSectionHeader(Transform parent, string text)
        {
            GameObject go = new GameObject("Section", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.6f, 0.8f, 1f);
            t.alignment = TextAnchor.MiddleLeft;
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
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 操作按钮异常: " + e.Message); } });

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 26);
        }

        private static void AddInfoRow(Transform parent, string left, string right)
        {
            GameObject go = new GameObject("InfoRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;

            GameObject leftGo = new GameObject("Left", typeof(RectTransform));
            leftGo.transform.SetParent(go.transform, false);
            Text lt = leftGo.AddComponent<Text>();
            lt.text = left;
            lt.fontSize = 12;
            lt.color = Color.white;
            if (lt.font == null) lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rightGo = new GameObject("Right", typeof(RectTransform));
            rightGo.transform.SetParent(go.transform, false);
            Text rt2 = rightGo.AddComponent<Text>();
            rt2.text = right;
            rt2.fontSize = 12;
            rt2.color = new Color(0.7f, 0.7f, 0.7f);
            rt2.alignment = TextAnchor.MiddleRight;
            if (rt2.font == null) rt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20);
        }

        private static void AddKnowledgeRow(Transform parent, Actor actor, SuperMechKnowledge.KnowledgeDef def, string branch, bool unlocked, bool canUnlock)
        {
            // 卡片式知识节点（参考魔兽世界天赋树/原神天赋面板）
            GameObject go = new GameObject("KnowledgeCard", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;

            // 左侧状态指示点
            GameObject dotGo = new GameObject("Dot", typeof(RectTransform));
            dotGo.transform.SetParent(go.transform, false);
            Image dotImg = dotGo.AddComponent<Image>();
            dotImg.color = unlocked ? new Color(0.3f, 1f, 0.3f) : (canUnlock ? new Color(0.4f, 0.7f, 1f) : new Color(0.4f, 0.4f, 0.4f));
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(8, 8);

            // 名称+分支
            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textLayout = textGo.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 0;
            textLayout.childForceExpandWidth = true;

            Text nt = textGo.AddComponent<Text>();
            nt.text = def.name;
            nt.fontSize = 11;
            nt.fontStyle = FontStyle.Bold;
            nt.color = unlocked ? new Color(0.9f, 0.9f, 0.9f) : (canUnlock ? new Color(0.7f, 0.85f, 1f) : new Color(0.5f, 0.5f, 0.5f));
            if (nt.font == null) nt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Text bt = textGo.AddComponent<Text>();
            bt.text = branch;
            bt.fontSize = 9;
            bt.color = new Color(0.5f, 0.7f, 1f);
            if (bt.font == null) bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 右侧消耗/状态
            GameObject costGo = new GameObject("Cost", typeof(RectTransform));
            costGo.transform.SetParent(go.transform, false);
            Text ct = costGo.AddComponent<Text>();
            ct.text = unlocked ? "✓" : $"{def.cost}点";
            ct.fontSize = 11;
            ct.fontStyle = FontStyle.Bold;
            ct.color = unlocked ? new Color(0.3f, 1f, 0.3f) : (canUnlock ? new Color(1f, 0.84f, 0f) : new Color(0.5f, 0.5f, 0.5f));
            ct.alignment = TextAnchor.MiddleRight;
            if (ct.font == null) ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform costRt = costGo.GetComponent<RectTransform>();
            costRt.sizeDelta = new Vector2(35, 0);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            // 背景色+边框效果
            var img = go.AddComponent<Image>();
            if (unlocked)
                img.color = new Color(0.12f, 0.25f, 0.12f, 0.8f);
            else if (canUnlock)
                img.color = new Color(0.1f, 0.18f, 0.3f, 0.8f);
            else
                img.color = new Color(0.08f, 0.08f, 0.08f, 0.6f);

            // 可解锁的添加点击事件+高亮边框
            if (canUnlock)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() =>
                {
                    if (SuperMechPotential.UnlockNode(actor, def.id, def.cost))
                    {
                        RenderContent(actor);
                    }
                });
                // 可解锁的用亮色边框模拟
                img.color = new Color(0.15f, 0.25f, 0.4f, 0.9f);
            }
        }

        /// <summary>添加网格行（每行2个卡片）</summary>
        private static GameObject AddGridRow(Transform parent)
        {
            GameObject go = new GameObject("GridRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
            return go;
        }

        /// <summary>添加进度条（参考主流游戏天赋进度）</summary>
        private static void AddProgressBar(Transform parent, int current, int total, string label)
        {
            GameObject go = new GameObject("ProgressBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;

            Text labelText = go.AddComponent<Text>();
            labelText.text = label;
            labelText.fontSize = 10;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            if (labelText.font == null) labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform labelRt = labelText.GetComponent<RectTransform>();
            labelRt.sizeDelta = new Vector2(60, 0);

            // 进度条背景
            GameObject barBg = new GameObject("BarBg", typeof(RectTransform));
            barBg.transform.SetParent(go.transform, false);
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // 进度条填充
            GameObject barFill = new GameObject("BarFill", typeof(RectTransform));
            barFill.transform.SetParent(barBg.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.7f, 1f, 0.9f);
            RectTransform fillRt = barFill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(total > 0 ? (float)current / total : 0, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Text countText = go.AddComponent<Text>();
            countText.text = $"{current}/{total}";
            countText.fontSize = 10;
            countText.color = new Color(0.8f, 0.8f, 0.8f);
            countText.alignment = TextAnchor.MiddleRight;
            if (countText.font == null) countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform countRt = countText.GetComponent<RectTransform>();
            countRt.sizeDelta = new Vector2(40, 0);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 18);
        }
    }
}
