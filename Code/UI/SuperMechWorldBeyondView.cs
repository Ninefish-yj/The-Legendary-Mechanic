using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 世界之外·高维存在系统（独立页签）
    /// 暗面宇宙超脱后成为高维存在，观察玩家世界并按状态留言
    /// 纯观察者层，不干涉世界、不锁剧情
    /// 与其他系统的边界：
    ///   - 前代文明档案：只读圣所跨迭代记录，不修改圣所数据
    ///   - 超脱者消息流：独立状态机，不依赖其他系统
    ///   - 沉浸监测层：只读世界状态，不修改任何数据
    ///   - 事件驱动：里程碑触发时写入圣所留言（调用圣所接口，不直接操作圣所内部）
    /// </summary>
    public static class SuperMechWorldBeyondView
    {
        private static RectTransform _root;
        private static int _subTab = 0; // 0=消息流, 1=监测层
        private static WorldState _lastState = WorldState.Early;
        private static readonly List<Message> _messageHistory = new List<Message>(); // 已触发的历史消息
        private const int MaxVisibleMessages = 5; // 最多显示5条

        // ═══ 超脱者消息流：按世界状态动态切换 ═══

        public enum WorldState
        {
            Early,          // 早期（无超A）
            XRankAppeared,  // 出现X阶
            OrderEstablished, // 秩序确立
            CouncilFormed,  // 超A协会成立
            PurgeStorm,     // 清算风暴
            SuperclusterAlliance, // 超星团同盟
            Institutionalized, // 体制化
            SanctuaryRealm  // 超能圣域
        }

        private struct Message
        {
            public string speaker;
            public string content;
            public WorldState state;
            public Message(string s, string c, WorldState st) { speaker = s; content = c; state = st; }
        }

        private static readonly List<Message> _messages = new List<Message>
        {
            // 早期
            new Message("韩萧", LocalizedTextManager.getText("sm_wb_msg_hanxiao_early"), WorldState.Early),
            new Message("地球玩家", LocalizedTextManager.getText("sm_wb_msg_earth_early"), WorldState.Early),
            // 出现X阶
            new Message("韩萧", LocalizedTextManager.getText("sm_wb_msg_hanxiao_xrank"), WorldState.XRankAppeared),
            // 秩序确立
            new Message("地球玩家", LocalizedTextManager.getText("sm_wb_msg_earth_order"), WorldState.OrderEstablished),
            // 超A协会成立
            new Message("地球玩家", LocalizedTextManager.getText("sm_wb_msg_earth_assoc"), WorldState.CouncilFormed),
            // 清算风暴
            new Message("韩萧", LocalizedTextManager.getText("sm_wb_msg_hanxiao_purge"), WorldState.PurgeStorm),
            // 超星团同盟
            new Message("地球玩家", LocalizedTextManager.getText("sm_wb_msg_earth_alliance"), WorldState.SuperclusterAlliance),
            // 体制化
            new Message("韩萧", LocalizedTextManager.getText("sm_wb_msg_hanxiao_system"), WorldState.Institutionalized),
            // 超能圣域
            new Message("韩萧", LocalizedTextManager.getText("sm_wb_msg_hanxiao_balance"), WorldState.SanctuaryRealm),
        };

        /// <summary>检测当前世界状态</summary>
        private static WorldState DetectWorldState()
        {
            int superACount = CountSuperA();
            int xCount = CountXRank();

            if (xCount > 0) return WorldState.XRankAppeared;
            if (superACount == 0) return WorldState.Early;

            // 简化状态检测：按超A数量推断
            if (superACount >= 20) return WorldState.SanctuaryRealm;
            if (superACount >= 15) return WorldState.Institutionalized;
            if (superACount >= 10) return WorldState.SuperclusterAlliance;
            if (superACount >= 7) return WorldState.PurgeStorm;
            if (superACount >= 5) return WorldState.CouncilFormed;
            if (superACount >= 3) return WorldState.OrderEstablished;
            return WorldState.Early;
        }

        private static int CountSuperA()
        {
            int count = 0;
            if (World.world?.units?.units_only_alive == null) return 0;
            foreach (var a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) >= 10) count++; // S阶=超A级
            }
            return count;
        }

        private static int CountXRank()
        {
            int count = 0;
            if (World.world?.units?.units_only_alive == null) return 0;
            foreach (var a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                if (SuperMechAdvancement.GetExactRankIndex(a) >= 13) count++; // X阶
            }
            return count;
        }

        // ═══ 主绘制入口 ═══

        public static void Draw(RectTransform content)
        {
            if (content == null) return;
            _root = content;

            // 清空
            var children = new List<GameObject>();
            foreach (Transform c in content) children.Add(c.gameObject);
            foreach (var go in children) Object.DestroyImmediate(go);

            // 背景
            var bg = new GameObject("WBBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(content, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.95f);

            // 顶部标题
            var title = SuperMechUiSkin.MakeText(content, LocalizedTextManager.getText("sm_ui_worldbeyond_title"), 18, TextAnchor.MiddleCenter);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -8);
            titleRect.sizeDelta = new Vector2(0, 28);
            title.color = new Color(0.6f, 0.8f, 1f);

            // 引言题词
            var quote = SuperMechUiSkin.MakeText(content,
                LocalizedTextManager.getText("sm_wb_quote"),
                11, TextAnchor.MiddleCenter);
            var quoteRect = quote.GetComponent<RectTransform>();
            quoteRect.anchorMin = new Vector2(0, 1); quoteRect.anchorMax = new Vector2(1, 1);
            quoteRect.pivot = new Vector2(0.5f, 1);
            quoteRect.anchoredPosition = new Vector2(0, -38);
            quoteRect.sizeDelta = new Vector2(0, 20);
            quote.color = new Color(0.5f, 0.5f, 0.6f);

            // 子页签栏
            string[] subTabs = { LocalizedTextManager.getText("sm_wb_tab_messages"), LocalizedTextManager.getText("sm_wb_tab_monitor") };
            float subX = 10f;
            for (int i = 0; i < subTabs.Length; i++)
            {
                int idx = i;
                var btn = MakeSubTabButton(content, subTabs[i], idx == _subTab);
                var br = btn.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(0, 1);
                br.pivot = new Vector2(0, 1);
                br.anchoredPosition = new Vector2(subX, -64);
                br.sizeDelta = new Vector2(100, 28);
                btn.GetComponent<Button>().onClick.AddListener(() => { _subTab = idx; Draw(content); });
                subX += 106f;
            }

            // 内容区
            var areaGo = new GameObject("WBArea", typeof(RectTransform));
            areaGo.transform.SetParent(content, false);
            var area = areaGo.GetComponent<RectTransform>();
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(10, 10);
            area.offsetMax = new Vector2(-10, -100);

            if (_subTab == 0) DrawMessageFlow(area);
            else DrawMonitorLayer(area);
        }

        // ═══ 子页签1：超脱者消息流（即时弹出式，只显示最近5条）═══

        private static void DrawMessageFlow(RectTransform area)
        {
            WorldState state = DetectWorldState();

            // 状态变化时，把旧状态的消息加入历史
            if (state != _lastState)
            {
                foreach (var msg in _messages)
                {
                    if (msg.state == _lastState && !_messageHistory.Contains(msg))
                    {
                        _messageHistory.Add(msg);
                    }
                }
                _lastState = state;
            }

            // 收集要显示的消息：当前状态的消息 + 最近历史消息（总共不超过MaxVisibleMessages）
            var visible = new List<Message>();
            foreach (var msg in _messages)
            {
                if (msg.state == state) visible.Add(msg);
            }
            // 从历史末尾往前取，直到达到上限
            for (int i = _messageHistory.Count - 1; i >= 0 && visible.Count < MaxVisibleMessages; i--)
            {
                visible.Insert(0, _messageHistory[i]);
            }

            var scrollGo = new GameObject("MsgScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(area, false);
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(0, 36); sr.offsetMax = Vector2.zero; // 底部留清除按钮空间
            scrollGo.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.08f, 0.8f);

            var contentGo = new GameObject("MsgContent", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var cr = contentGo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.sizeDelta = new Vector2(0, 600);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = cr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;

            float y = -12f;
            string lastSpeaker = null;
            foreach (var msg in visible)
            {
                bool isCurrent = msg.state == state;
                bool isHanXiao = msg.speaker == "韩萧" || msg.speaker == LocalizedTextManager.getText("sm_wb_speaker_hanxiao");
                bool sameAsLast = lastSpeaker == msg.speaker;

                float msgHeight = 60f;
                var msgGo = new GameObject("MsgItem", typeof(RectTransform));
                msgGo.transform.SetParent(cr, false);
                var mr = msgGo.GetComponent<RectTransform>();
                mr.anchorMin = new Vector2(0, 1); mr.anchorMax = new Vector2(1, 1);
                mr.pivot = new Vector2(0.5f, 1);
                mr.anchoredPosition = new Vector2(0, y);
                mr.sizeDelta = new Vector2(0, msgHeight);

                if (!sameAsLast) y -= 8f;

                // 头像
                var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                avatarGo.transform.SetParent(msgGo.transform, false);
                var ar = avatarGo.GetComponent<RectTransform>();
                ar.anchorMin = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                ar.anchorMax = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                ar.pivot = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                ar.anchoredPosition = isHanXiao ? new Vector2(8, -4) : new Vector2(-8, -4);
                ar.sizeDelta = new Vector2(32, 32);
                var avatarImg = avatarGo.GetComponent<Image>();
                avatarImg.color = isHanXiao ? new Color(0.2f, 0.5f, 0.9f, isCurrent ? 0.9f : 0.4f) : new Color(0.3f, 0.7f, 0.4f, isCurrent ? 0.9f : 0.4f);
                var avatarTxt = SuperMechUiSkin.MakeText(avatarGo.transform, isHanXiao ? "黑" : "玩", 14, TextAnchor.MiddleCenter);
                avatarTxt.color = new Color(1, 1, 1, isCurrent ? 1f : 0.5f);
                avatarTxt.fontStyle = FontStyle.Bold;
                var atRect = avatarTxt.GetComponent<RectTransform>();
                atRect.anchorMin = Vector2.zero; atRect.anchorMax = Vector2.one;
                atRect.offsetMin = Vector2.zero; atRect.offsetMax = Vector2.zero;

                // 用户名
                if (!sameAsLast)
                {
                    var nameGo = new GameObject("Name", typeof(RectTransform));
                    nameGo.transform.SetParent(msgGo.transform, false);
                    var nr = nameGo.GetComponent<RectTransform>();
                    nr.anchorMin = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                    nr.anchorMax = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                    nr.pivot = isHanXiao ? new Vector2(0, 1) : new Vector2(1, 1);
                    nr.anchoredPosition = isHanXiao ? new Vector2(48, -2) : new Vector2(-48, -2);
                    nr.sizeDelta = new Vector2(200, 16);
                    var nameTxt = SuperMechUiSkin.MakeText(nameGo.transform, msg.speaker, 11, isHanXiao ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
                    nameTxt.color = isHanXiao ? new Color(0.5f, 0.75f, 1f, isCurrent ? 1f : 0.5f) : new Color(0.5f, 0.9f, 0.6f, isCurrent ? 1f : 0.5f);
                }

                // 消息气泡
                var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
                bubbleGo.transform.SetParent(msgGo.transform, false);
                var br = bubbleGo.GetComponent<RectTransform>();
                br.anchorMin = isHanXiao ? new Vector2(0, 0) : new Vector2(1, 0);
                br.anchorMax = isHanXiao ? new Vector2(0, 0) : new Vector2(1, 0);
                br.pivot = isHanXiao ? new Vector2(0, 0) : new Vector2(1, 0);
                br.anchoredPosition = isHanXiao ? new Vector2(48, 4) : new Vector2(-48, 4);
                br.sizeDelta = new Vector2(area.rect.width - 80, 40);
                var bubbleImg = bubbleGo.GetComponent<Image>();
                bubbleImg.color = isCurrent
                    ? (isHanXiao ? new Color(0.15f, 0.25f, 0.45f, 0.95f) : new Color(0.15f, 0.35f, 0.2f, 0.95f))
                    : new Color(0.08f, 0.1f, 0.15f, 0.35f); // 旧消息淡出

                var contentTxt = SuperMechUiSkin.MakeText(bubbleGo.transform, msg.content, 12, TextAnchor.MiddleLeft);
                contentTxt.color = isCurrent ? new Color(0.95f, 0.95f, 1f) : new Color(0.5f, 0.55f, 0.6f, 0.6f);
                contentTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                contentTxt.verticalOverflow = VerticalWrapMode.Truncate;
                var ctRect = contentTxt.GetComponent<RectTransform>();
                ctRect.anchorMin = Vector2.zero; ctRect.anchorMax = Vector2.one;
                ctRect.offsetMin = new Vector2(10, 4); ctRect.offsetMax = new Vector2(-10, -4);

                // 当前状态标记
                if (isCurrent)
                {
                    var markGo = new GameObject("CurrentMark", typeof(RectTransform));
                    markGo.transform.SetParent(bubbleGo.transform, false);
                    var mkR = markGo.GetComponent<RectTransform>();
                    mkR.anchorMin = new Vector2(1, 1); mkR.anchorMax = new Vector2(1, 1);
                    mkR.pivot = new Vector2(1, 1);
                    mkR.anchoredPosition = new Vector2(-4, -2);
                    mkR.sizeDelta = new Vector2(40, 12);
                    var markTxt = SuperMechUiSkin.MakeText(markGo.transform, LocalizedTextManager.getText("sm_wb_current"), 9, TextAnchor.MiddleRight);
                    markTxt.color = new Color(1f, 0.85f, 0.4f);
                }

                lastSpeaker = msg.speaker;
                y -= msgHeight + (sameAsLast ? 2f : 10f);
            }
            cr.sizeDelta = new Vector2(0, -y + 20);

            // 底部清除历史按钮
            var clearBtnGo = new GameObject("ClearBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            clearBtnGo.transform.SetParent(area, false);
            var cbr = clearBtnGo.GetComponent<RectTransform>();
            cbr.anchorMin = new Vector2(1, 0); cbr.anchorMax = new Vector2(1, 0);
            cbr.pivot = new Vector2(1, 0);
            cbr.anchoredPosition = new Vector2(-8, 6);
            cbr.sizeDelta = new Vector2(100, 24);
            var clearImg = clearBtnGo.GetComponent<Image>();
            clearImg.color = new Color(0.3f, 0.15f, 0.15f, 0.8f);
            var clearBtn = clearBtnGo.GetComponent<Button>();
            clearBtn.onClick.AddListener(() =>
            {
                _messageHistory.Clear();
                if (_root != null) Draw(_root);
            });
            var clearTxt = SuperMechUiSkin.MakeText(clearBtnGo.transform, LocalizedTextManager.getText("sm_wb_clear_history"), 11, TextAnchor.MiddleCenter);
            clearTxt.color = new Color(1f, 0.7f, 0.7f);
            var ctRect2 = clearTxt.GetComponent<RectTransform>();
            ctRect2.anchorMin = Vector2.zero; ctRect2.anchorMax = Vector2.one;
            ctRect2.offsetMin = Vector2.zero; ctRect2.offsetMax = Vector2.zero;
        }

        // ═══ 子页签2：世界监测层（高维存在的观察视角，只显示核心指标）═══

        private static void DrawMonitorLayer(RectTransform area)
        {
            int superA = CountSuperA();
            int xRank = CountXRank();
            WorldState state = DetectWorldState();

            string[] monitorItems = {
                $"{LocalizedTextManager.getText("sm_ui_worldbeyond_stage")}: {GetStateText(state)}",
                $"{LocalizedTextManager.getText("sm_wb_supera_count")}: {superA}",
                $"{LocalizedTextManager.getText("sm_wb_xrank_count")}: {xRank}",
                $"{LocalizedTextManager.getText("sm_ui_worldbeyond_state")}: {GetBeyondStateText()}",
                $"{LocalizedTextManager.getText("sm_wb_observer_focus")}: {GetObserverFocus(state)}",
            };

            var scrollGo = new GameObject("MonScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(area, false);
            var sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.6f);

            var contentGo = new GameObject("MonContent", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var cr = contentGo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.sizeDelta = new Vector2(0, monitorItems.Length * 32 + 20);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = cr;
            scroll.horizontal = false;
            scroll.vertical = true;

            float y = -10f;
            foreach (var item in monitorItems)
            {
                var row = SuperMechUiSkin.MakeText(cr, item, 12, TextAnchor.MiddleLeft);
                var rr = row.GetComponent<RectTransform>();
                rr.anchorMin = new Vector2(0, 1); rr.anchorMax = new Vector2(1, 1);
                rr.pivot = new Vector2(0.5f, 1);
                rr.anchoredPosition = new Vector2(0, y);
                rr.sizeDelta = new Vector2(-20, 28);
                row.color = new Color(0.75f, 0.8f, 0.9f);
                y -= 32f;
            }
        }

        private static string GetStateText(WorldState s)
        {
            return s switch
            {
                WorldState.Early => LocalizedTextManager.getText("sm_wb_stage_early"),
                WorldState.XRankAppeared => LocalizedTextManager.getText("sm_wb_stage_xrank"),
                WorldState.OrderEstablished => LocalizedTextManager.getText("sm_wb_stage_order"),
                WorldState.CouncilFormed => LocalizedTextManager.getText("sm_wb_stage_assoc"),
                WorldState.PurgeStorm => LocalizedTextManager.getText("sm_wb_stage_purge"),
                WorldState.SuperclusterAlliance => LocalizedTextManager.getText("sm_wb_stage_alliance"),
                WorldState.Institutionalized => LocalizedTextManager.getText("sm_wb_stage_system"),
                WorldState.SanctuaryRealm => LocalizedTextManager.getText("sm_wb_stage_sanctuary"),
                _ => LocalizedTextManager.getText("sm_wb_stage_unknown")
            };
        }

        private static string GetObserverFocus(WorldState s)
        {
            return s switch
            {
                WorldState.Early => LocalizedTextManager.getText("sm_wb_focus_early"),
                WorldState.XRankAppeared => LocalizedTextManager.getText("sm_wb_focus_xrank"),
                WorldState.OrderEstablished => LocalizedTextManager.getText("sm_wb_focus_order"),
                WorldState.CouncilFormed => LocalizedTextManager.getText("sm_wb_focus_assoc"),
                WorldState.PurgeStorm => LocalizedTextManager.getText("sm_wb_focus_purge"),
                WorldState.SuperclusterAlliance => LocalizedTextManager.getText("sm_wb_focus_alliance"),
                WorldState.Institutionalized => LocalizedTextManager.getText("sm_wb_focus_system"),
                WorldState.SanctuaryRealm => LocalizedTextManager.getText("sm_wb_focus_balance"),
                _ => LocalizedTextManager.getText("sm_wb_focus_observing")
            };
        }

        private static string GetBeyondStateText()
        {
            var phase = SuperMechWorldBeyond.CurrentPhase;
            return phase switch
            {
                SuperMechWorldBeyond.BeyondPhase.None => LocalizedTextManager.getText("sm_wb_dark_notentered"),
                SuperMechWorldBeyond.BeyondPhase.InformationStripping => LocalizedTextManager.getText("sm_wb_dark_peeling"),
                SuperMechWorldBeyond.BeyondPhase.DarkIteration => $"{LocalizedTextManager.getText("sm_wb_dark_iterating")}（{SuperMechWorldBeyond.DarkIteration}/3）",
                SuperMechWorldBeyond.BeyondPhase.Merging => LocalizedTextManager.getText("sm_wb_dark_overlapping"),
                SuperMechWorldBeyond.BeyondPhase.Transcended => LocalizedTextManager.getText("sm_wb_dark_transcended"),
                _ => LocalizedTextManager.getText("sm_wb_dark_unknown")
            };
        }

        // ═══ UI工具 ═══

        private static GameObject MakeSubTabButton(Transform parent, string text, bool active)
        {
            var go = new GameObject("SubTab", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = active ? new Color(0.15f, 0.2f, 0.35f, 0.9f) : new Color(0.08f, 0.1f, 0.16f, 0.7f);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var label = SuperMechUiSkin.MakeText(go.transform, text, 12, TextAnchor.MiddleCenter);
            label.color = active ? new Color(0.8f, 0.9f, 1f) : new Color(0.5f, 0.55f, 0.65f);
            var lr = label.GetComponent<RectTransform>();
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

            return go;
        }
    }
}
