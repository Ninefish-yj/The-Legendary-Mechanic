using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星图窗口 - 面板与弹窗部分（SelectPlanet/RefreshButtons/ShowUnitSelectPanel/ShowUnitManagePanel/ShowEventPopup/ShowColonyPanel）</summary>
    public partial class SuperMechStarView : MonoBehaviour
    {
        private void SelectPlanet(string id)
        {
            _selectedPlanet = id;
            // v0.223 选中星球时显示右侧详情面板
            if (_infoPanel != null) _infoPanel.SetActive(true);
            var p = SuperMechStarSystem.GetPlanet(id);
            if (p == null || _infoText == null) return;

            // 高亮选中的星球（改光晕亮度）
            foreach (var kv in _planetNodes)
            {
                var glow = kv.Value.transform.Find("Glow")?.GetComponent<Image>();
                if (glow != null)
                {
                    if (kv.Key == id)
                        glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0.8f);
                    else
                        glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, glow.color.a * 0.5f);
                }
            }

            string typeName = p.type switch
            {
                PlanetType.Rocky => LocalizedTextManager.getText("sm_ui_planet_rocky"),
                PlanetType.Gas => LocalizedTextManager.getText("sm_ui_planet_gas"),
                PlanetType.Ice => LocalizedTextManager.getText("sm_ui_planet_ice"),
                PlanetType.Lava => LocalizedTextManager.getText("sm_ui_planet_lava"),
                PlanetType.Ocean => LocalizedTextManager.getText("sm_ui_planet_ocean"),
                PlanetType.Desert => LocalizedTextManager.getText("sm_ui_planet_desert"),
                PlanetType.Forest => LocalizedTextManager.getText("sm_ui_planet_forest"),
                PlanetType.Star => LocalizedTextManager.getText("sm_ui_star_type"),
                _ => "?"
            };

            // 恒星类型名称
            string starTypeName = p.starType switch
            {
                StarType.RedDwarf => LocalizedTextManager.getText("sm_ui_star_red_dwarf"),
                StarType.YellowDwarf => LocalizedTextManager.getText("sm_ui_star_yellow_dwarf"),
                StarType.BlueGiant => LocalizedTextManager.getText("sm_ui_star_blue_giant"),
                StarType.RedGiant => LocalizedTextManager.getText("sm_ui_star_red_giant"),
                StarType.WhiteDwarf => LocalizedTextManager.getText("sm_ui_star_white_dwarf"),
                StarType.NeutronStar => LocalizedTextManager.getText("sm_ui_star_neutron"),
                StarType.BlackHole => LocalizedTextManager.getText("sm_ui_star_black_hole"),
                _ => "?"
            };

            bool isStar = p.type == PlanetType.Star;
            _infoText.text =
                $"<color=#ffd700><size=14><b>{p.name}</b></size></color>\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_type")}:</color> {typeName}\n";

            // 恒星显示光谱类型、温度、质量、半径、光度、年龄
            if (isStar)
            {
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_type")}:</color> {starTypeName}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_temp")}:</color> {p.temperature:F0}K\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_mass")}:</color> {p.starMass:F2} M☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_radius")}:</color> {p.starRadius:F2} R☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_luminosity")}:</color> {p.starLuminosity:F2} L☉\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_age")}:</color> {p.starAge:F1} {LocalizedTextManager.getText("sm_ui_star_age_unit")}\n";
            }
            else
            {
                // 行星显示温度、宜居带、物理参数
                string tempColor = p.inHabitableZone ? "#7fff7f" : (p.temperature > 500 ? "#ff7f7f" : "#7f7fff");
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_temp")}:</color> <color={tempColor}>{p.temperature:F0}K ({p.temperature - 273:F0}°C)</color>\n";
                string habitableText = p.inHabitableZone
                    ? $"<color=#7fff7f>{LocalizedTextManager.getText("sm_ui_habitable")}</color>"
                    : $"<color=#aaaaaa>{LocalizedTextManager.getText("sm_ui_not_habitable")}</color>";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_habitable")}:</color> {habitableText}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_mass")}:</color> {p.mass:F2} M⊕\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_radius")}:</color> {p.radius:F2} R⊕\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_gravity")}:</color> {p.gravity:F2} g\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_density")}:</color> {p.density:F1} g/cm³\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_rotation")}:</color> {p.rotationPeriod:F1} {LocalizedTextManager.getText("sm_ui_hour")}\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_axial_tilt")}:</color> {p.axialTilt:F1}°\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_moon")}:</color> {p.moonCount} {LocalizedTextManager.getText("sm_ui_star_moon_unit")}\n";
                if (p.tidallyLocked)
                    _infoText.text += $"<color=#ff9966>{LocalizedTextManager.getText("sm_ui_star_tidally_locked")}</color>\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_atmosphere")}:</color> {(p.atmosphere * 100):F0}%\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_magnetic")}:</color> {(p.magneticField * 100):F0}%\n";
                _infoText.text += $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_star_eccentricity")}:</color> {p.eccentricity:F3}\n";
            }

            _infoText.text +=
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_resource")}:</color> {p.resource:F0}/{p.maxResource:F0}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_colony")}:</color> {p.colonyLevel}\n" +
                $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_units")}:</color> {p.units.Count}\n" +
                (p.colonyLevel > 0 ? $"<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_owner")}:</color> {p.ownerKingdom}\n" : "\n") +
                $"\n<color=#93A4BC>{LocalizedTextManager.getText("sm_ui_planet_coord")}:</color> ({p.x:F0}, {p.y:F0})\n";

            if (!p.discovered && p.id != SuperMechStarSystem.MAIN_PLANET)
            {
                _infoText.text += "\n<color=#ffcc66>" + LocalizedTextManager.getText("sm_ui_planet_uncolonized") + "</color>";
            }

            // 根据星球状态创建交互按钮
            RefreshButtons(p);
        }

        /// <summary>根据星球状态刷新交互按钮</summary>
        private void RefreshButtons(PlanetData p)
        {
            if (_buttonArea == null) return;
            // 清空旧按钮
            foreach (Transform child in _buttonArea) Destroy(child.gameObject);

            bool isMain = p.id == SuperMechStarSystem.MAIN_PLANET;
            bool isStar = p.type == PlanetType.Star;
            float btnY = 0f;
            const float btnHeight = 44f; // v0.238.7 30→44：手机触控最小点击目标（原30px缩放后约10-17px，手指点不准=不灵敏）
            const float btnGap = 6f;

            // 恒星：只能查看信息，不能操作
            if (isStar)
            {
                var hintGo = new GameObject("Hint");
                hintGo.transform.SetParent(_buttonArea, false);
                var hintRect = hintGo.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0, 1);
                hintRect.anchorMax = new Vector2(1, 1);
                hintRect.pivot = new Vector2(0.5f, 1);
                hintRect.sizeDelta = new Vector2(0, 40);
                hintRect.anchoredPosition = new Vector2(0, -4);
                var hintText = hintGo.AddComponent<Text>();
                hintText.font = SuperMechUiSkin.DefaultFont;
                hintText.fontSize = 11;
                hintText.color = new Color(0.8f, 0.7f, 0.5f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = LocalizedTextManager.getText("sm_ui_star_cannot_land");
                hintText.raycastTarget = false;
                return;
            }

            // 已发现：进入星球（多世界系统）
            if (p.discovered || isMain)
            {
                string enterLabel = isMain ?
                    LocalizedTextManager.getText("sm_ui_enter_main_world") :
                    LocalizedTextManager.getText("sm_ui_enter_planet");
                CreateActionButton(enterLabel, new Color(0.9f, 0.7f, 0.2f, 0.9f), () =>
                {
                    if (SuperMechMultiWorld.IsSwitching) return;
                    Close();
                    SuperMechMultiWorld.SwitchToPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);
            }

            // 未发现：派遣探险队（打开单位选择面板）
            if (!p.discovered && !isMain)
            {
                CreateActionButton(LocalizedTextManager.getText("sm_ui_send_explorer"), new Color(0.2f, 0.5f, 0.8f, 0.9f), () =>
                {
                    _pendingSendPlanet = p;
                    ShowUnitSelectPanel();
                }, ref btnY, btnHeight, btnGap);
            }

            // 已发现但未殖民：建立殖民地（消耗500资源+需要至少1个驻留单位）
            if (p.discovered && p.colonyLevel == 0 && !isMain)
            {
                int colonizeCost = 500;
                bool canColonize = p.resource >= colonizeCost && p.units.Count > 0;
                string colonizeLabel = canColonize
                    ? LocalizedTextManager.getText("sm_ui_colonize") + $" ({colonizeCost})"
                    : LocalizedTextManager.getText("sm_ui_colonize") + " (" + LocalizedTextManager.getText("sm_ui_need_unit") + ")";
                CreateActionButton(colonizeLabel, new Color(0.2f, 0.7f, 0.3f, 0.9f), () =>
                {
                    if (p.resource >= colonizeCost && p.units.Count > 0)
                    {
                        p.resource -= colonizeCost;
                        p.colonyLevel = 1;
                        p.ownerKingdom = p.units[0].kingdomName ?? "player";
                        p.maxResource = 1000; // 殖民后解锁资源上限
                        SelectPlanet(p.id);
                        RefreshMap();
                    }
                }, ref btnY, btnHeight, btnGap);
            }

            // 已殖民：升级殖民地 + 派遣单位 + 采集资源 + 运回资源 + 召回单位
            if (p.colonyLevel > 0 && !isMain)
            {
                // 殖民地升级消耗资源（每级消耗递增）
                int upgradeCost = p.colonyLevel * 200;
                string upgradeLabel = p.colonyLevel < 5
                    ? string.Format(LocalizedTextManager.getText("sm_ui_upgrade_colony"), p.colonyLevel) + $" ({upgradeCost})"
                    : LocalizedTextManager.getText("sm_ui_colony_max");
                CreateActionButton(upgradeLabel, new Color(0.8f, 0.6f, 0.2f, 0.9f), () =>
                {
                    if (p.colonyLevel < 5 && p.resource >= upgradeCost)
                    {
                        p.resource -= upgradeCost;
                        p.colonyLevel++;
                        SelectPlanet(p.id);
                    }
                }, ref btnY, btnHeight, btnGap);

                CreateActionButton(LocalizedTextManager.getText("sm_ui_send_unit"), new Color(0.5f, 0.3f, 0.8f, 0.9f), () =>
                {
                    _pendingSendPlanet = p;
                    ShowUnitSelectPanel();
                }, ref btnY, btnHeight, btnGap);

                // 有驻留单位时显示召回按钮
                if (p.units.Count > 0)
                {
                    CreateActionButton(string.Format(LocalizedTextManager.getText("sm_ui_recall_unit"), p.units.Count), new Color(0.3f, 0.5f, 0.7f, 0.9f), () =>
                    {
                        // 召回一个单位回主世界
                        if (p.units.Count > 0)
                        {
                            var snapshot = p.units[0];
                            p.units.RemoveAt(0);
                            // 在主世界生成该单位
                            SuperMechStarSystem.SpawnUnitAtMain(snapshot);
                            SelectPlanet(p.id);
                            RefreshMap();
                        }
                    }, ref btnY, btnHeight, btnGap);

                    // 单位管理：打开单独分配面板（Stellaris式人口分配）
                    int miningCnt = 0, researchCnt = 0, defenseCnt = 0, idleCnt = 0;
                    foreach (var u in p.units)
                    {
                        if (u == null) continue;
                        if (u.assignment == "mining") miningCnt++;
                        else if (u.assignment == "research") researchCnt++;
                        else if (u.assignment == "defense") defenseCnt++;
                        else idleCnt++;
                    }
                    string manageLabel = LocalizedTextManager.getText("sm_ui_unit_manage") +
                        $" (矿:{miningCnt} 研:{researchCnt} 防:{defenseCnt} 闲:{idleCnt})";
                    CreateActionButton(manageLabel, new Color(0.6f, 0.4f, 0.2f, 0.9f), () =>
                    {
                        ShowUnitManagePanel(p);
                    }, ref btnY, btnHeight, btnGap);
                }

                CreateActionButton(string.Format(LocalizedTextManager.getText("sm_ui_collect_resource"), p.resource), new Color(0.8f, 0.3f, 0.3f, 0.9f), () =>
                {
                    float collected = Mathf.Min(p.resource, 100f * p.colonyLevel);
                    p.resource -= collected;
                    var main = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
                    if (main != null) main.resource += collected;
                    SelectPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);

                // 戴森球式：自动运输开关
                string autoLabel = p.autoTransport
                    ? LocalizedTextManager.getText("sm_ui_auto_transport_on")
                    : LocalizedTextManager.getText("sm_ui_auto_transport_off");
                CreateActionButton(autoLabel, p.autoTransport ? new Color(0.3f, 0.7f, 0.4f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.9f), () =>
                {
                    p.autoTransport = !p.autoTransport;
                    SelectPlanet(p.id);
                }, ref btnY, btnHeight, btnGap);

                // 探索按钮：有驻留单位时可触发事件
                if (p.units.Count > 0)
                {
                    CreateActionButton(LocalizedTextManager.getText("sm_ui_explore_planet"), new Color(0.6f, 0.3f, 0.8f, 0.9f), () =>
                    {
                        StarEvents.Init();
                        var rng = new System.Random();
                        var evt = StarEvents.RollEvent(rng, p.type, p.colonyLevel);
                        if (evt != null)
                        {
                            float defense = ColonyManager.CalculateDefenseReduction(p);
                            var result = StarEvents.ResolveEvent(evt, p, rng);
                            if ((result.resultType == EventResultType.DamageUnit || result.resultType == EventResultType.LoseUnit)
                                && rng.NextDouble() < defense)
                            {
                                result.resultMessage = LocalizedTextManager.getText("sm_star_evt_defended") + result.resultMessage;
                                result.unitsLost = 0;
                            }
                            p.pendingEvent = result;
                            ShowEventPopup(result);
                            SelectPlanet(p.id);
                        }
                    }, ref btnY, btnHeight, btnGap);
                }

                // 殖民地建设按钮
                CreateActionButton(LocalizedTextManager.getText("sm_ui_colony_building"), new Color(0.3f, 0.6f, 0.5f, 0.9f), () =>
                {
                    ShowColonyPanel(p);
                }, ref btnY, btnHeight, btnGap);

                // 有待处理事件时显示事件按钮
                if (p.pendingEvent != null)
                {
                    CreateActionButton(LocalizedTextManager.getText("sm_ui_view_event"), new Color(0.9f, 0.5f, 0.2f, 0.9f), () =>
                    {
                        ShowEventPopup(p.pendingEvent);
                    }, ref btnY, btnHeight, btnGap);
                }
            }

            // v0.231 宇宙深化入口（所有星球都显示）
            CreateActionButton(LocalizedTextManager.getText("sm_ui_space_expansion"), new Color(0.15f, 0.6f, 0.7f, 0.9f), () =>
            {
                if (SuperMechSpaceExpansionUI.Instance != null)
                    SuperMechSpaceExpansionUI.Instance.Open(p.id);
            }, ref btnY, btnHeight, btnGap);

            // 主世界：显示驻留单位数量和资源
            if (isMain)
            {
                var hintGo = new GameObject("Hint");
                hintGo.transform.SetParent(_buttonArea, false);
                var hintRect = hintGo.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0, 1);
                hintRect.anchorMax = new Vector2(1, 1);
                hintRect.pivot = new Vector2(0.5f, 1);
                hintRect.sizeDelta = new Vector2(0, 56);
                // v0.238.6 修复重叠：anchoredPosition 从固定 -4 改为跟随按钮流 btnY，三行提示不再压住按钮
                hintRect.anchoredPosition = new Vector2(0, -btnY);
                var hintText = hintGo.AddComponent<Text>();
                hintText.font = SuperMechUiSkin.DefaultFont;
                hintText.fontSize = 11;
                hintText.color = new Color(0.7f, 0.8f, 0.9f);
                hintText.alignment = TextAnchor.UpperCenter;
                hintText.text = string.Format(LocalizedTextManager.getText("sm_ui_main_planet_info"), p.resource);
                hintText.raycastTarget = false;
                btnY += 56 + btnGap; // 纳入布局流，避免与按钮重叠
            }
        }

        /// <summary>创建一个操作按钮</summary>
        private void CreateActionButton(string text, Color bgColor, UnityEngine.Events.UnityAction action, ref float y, float height, float gap)
        {
            var btnGo = new GameObject("ActionBtn");
            btnGo.transform.SetParent(_buttonArea, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.sizeDelta = new Vector2(0, height);
            btnRect.anchoredPosition = new Vector2(0, -y);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = bgColor;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            var btnText = textGo.AddComponent<Text>();
            btnText.font = SuperMechUiSkin.DefaultFont;
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = text;
            btnText.raycastTarget = false;

            y += height + gap;
        }

        /// <summary>显示事件结果弹窗</summary>
        /// <summary>显示单位选择面板（派遣单位时用）</summary>
        private void ShowUnitSelectPanel()
        {
            HideUnitSelectPanel();
            if (_pendingSendPlanet == null) return;

            // 创建半透明遮罩
            var overlayGo = new GameObject("UnitSelectOverlay");
            overlayGo.transform.SetParent(transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(HideUnitSelectPanel);
            _unitSelectPanel = overlayGo;

            // 选择面板
            var panelGo = new GameObject("UnitSelectPanel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 350);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 14;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_select_unit") + " -> " + _pendingSendPlanet.name;

            // 单位列表（滚动区域）
            var scrollGo = new GameObject("UnitScroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -45);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            // 收集玩家单位
            var allUnits = World.world.units.getSimpleList();
            var playerUnits = new System.Collections.Generic.List<Actor>();
            foreach (var u in allUnits)
            {
                if (u != null && u.isAlive() && u.kingdom != null && u.kingdom.name == "player")
                    playerUnits.Add(u);
            }

            float itemY = 0f;
            foreach (var unit in playerUnits)
            {
                if (unit == null) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(unit);
                string rankName = rank >= 0 && rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[rank].name) : "?";
                bool canBodyFlight = rank >= 6;

                var itemGo = new GameObject("UnitItem");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 32);
                itemRect.anchoredPosition = new Vector2(0, -itemY);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
                var itemBtn = itemGo.AddComponent<Button>();
                long unitId = unit.data.id;
                itemBtn.onClick.AddListener(() =>
                {
                    bool success = SuperMechStarSystem.SendUnit(unitId, SuperMechStarSystem.MAIN_PLANET, _pendingSendPlanet.id);
                    if (success)
                    {
                        Debug.Log($"[超神机械师] 派遣 {unit.data.name}({rankName}) 前往 {_pendingSendPlanet.name}");
                        HideUnitSelectPanel();
                        SelectPlanet(_pendingSendPlanet.id);
                        RefreshMap();
                    }
                });

                var itemTextGo = new GameObject("Text");
                itemTextGo.transform.SetParent(itemGo.transform, false);
                var itemTextRect = itemTextGo.AddComponent<RectTransform>();
                itemTextRect.anchorMin = Vector2.zero;
                itemTextRect.anchorMax = Vector2.one;
                itemTextRect.offsetMin = new Vector2(8, 0);
                itemTextRect.offsetMax = new Vector2(-8, 0);
                var itemText = itemTextGo.AddComponent<Text>();
                itemText.font = SuperMechUiSkin.DefaultFont;
                itemText.fontSize = 12;
                itemText.color = canBodyFlight ? new Color(1f, 0.8f, 0.3f) : Color.white;
                itemText.alignment = TextAnchor.MiddleLeft;
                string flightTag = canBodyFlight ? " [肉身横渡]" : "";
                itemText.text = $"{unit.data.name} ({rankName}){flightTag}";
                itemText.raycastTarget = false;

                itemY += 34f;
            }

            // 无单位提示
            if (playerUnits.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(contentGo.transform, false);
                var emptyRect = emptyGo.AddComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.sizeDelta = new Vector2(300, 30);
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = SuperMechUiSkin.DefaultFont;
                emptyText.fontSize = 12;
                emptyText.color = new Color(0.6f, 0.6f, 0.6f);
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.text = LocalizedTextManager.getText("sm_ui_no_player_unit");
            }

            // 调整content高度
            contentRect.sizeDelta = new Vector2(0, Mathf.Max(300, itemY));
        }

        /// <summary>隐藏单位选择面板</summary>
        private void HideUnitSelectPanel()
        {
            if (_unitSelectPanel != null)
            {
                Destroy(_unitSelectPanel);
                _unitSelectPanel = null;
            }
            _pendingSendPlanet = null;
        }

        private GameObject _unitManagePanel;

        /// <summary>Stellaris式单位管理面板：每个单位单独分配任务</summary>
        private void ShowUnitManagePanel(PlanetData planet)
        {
            HideUnitManagePanel();
            _managePlanet = planet;

            // 遮罩
            var overlayGo = new GameObject("UnitManageOverlay");
            overlayGo.transform.SetParent(transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.onClick.AddListener(HideUnitManagePanel);
            _unitManagePanel = overlayGo;

            // 面板
            var panelGo = new GameObject("UnitManagePanel");
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(450, 400);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 35);
            titleRect.anchoredPosition = new Vector2(0, -5);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 14;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = LocalizedTextManager.getText("sm_ui_unit_manage") + " - " + planet.name;

            // 列表内容
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(panelGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -45);

            float itemY = 0f;
            for (int i = 0; i < planet.units.Count; i++)
            {
                var u = planet.units[i];
                if (u == null) continue;
                int idx = i;

                var itemGo = new GameObject($"Unit_{idx}");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 36);
                itemRect.anchoredPosition = new Vector2(0, -itemY);
                var itemImg = itemGo.AddComponent<Image>();
                itemImg.color = idx % 2 == 0 ? new Color(0.1f, 0.15f, 0.25f, 0.9f) : new Color(0.08f, 0.12f, 0.2f, 0.9f);

                // 单位名称
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(itemGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0);
                nameRect.anchorMax = new Vector2(0.4f, 1);
                nameRect.offsetMin = new Vector2(8, 0);
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = SuperMechUiSkin.DefaultFont;
                nameText.fontSize = 11;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                string rankName = u.rank >= 0 && u.rank < SuperMechRanks.All.Count
                    ? LocalizedTextManager.getText(SuperMechRanks.All[u.rank].name) : "?";
                nameText.text = $"{u.name} ({rankName})";
                nameText.raycastTarget = false;

                // 四个任务按钮：空闲/采矿/研究/防御
                string[] tasks = { "idle", "mining", "research", "defense" };
                string[] taskLabels = { LocalizedTextManager.getText("sm_ui_star_idle"), LocalizedTextManager.getText("sm_ui_star_mining"), LocalizedTextManager.getText("sm_ui_star_research"), LocalizedTextManager.getText("sm_ui_star_defense") };
                Color[] taskColors = {
                    new Color(0.4f, 0.4f, 0.4f, 0.9f),
                    new Color(0.8f, 0.6f, 0.2f, 0.9f),
                    new Color(0.2f, 0.6f, 0.8f, 0.9f),
                    new Color(0.8f, 0.3f, 0.3f, 0.9f)
                };
                for (int t = 0; t < 4; t++)
                {
                    var btnGo = new GameObject($"TaskBtn_{t}");
                    btnGo.transform.SetParent(itemGo.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.4f + t * 0.15f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.4f + (t + 1) * 0.15f, 0.85f);
                    btnRect.offsetMin = new Vector2(2, 0);
                    btnRect.offsetMax = new Vector2(-2, 0);
                    var btnImg = btnGo.AddComponent<Image>();
                    bool isActive = u.assignment == tasks[t] || (tasks[t] == "idle" && string.IsNullOrEmpty(u.assignment));
                    btnImg.color = isActive ? taskColors[t] : new Color(0.2f, 0.2f, 0.2f, 0.6f);
                    var btn = btnGo.AddComponent<Button>();
                    string task = tasks[t];
                    btn.onClick.AddListener(() =>
                    {
                        u.assignment = task;
                        ShowUnitManagePanel(_managePlanet); // 刷新
                        SelectPlanet(_managePlanet.id);
                    });
                    var btnTextGo = new GameObject("Text");
                    btnTextGo.transform.SetParent(btnGo.transform, false);
                    var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.offsetMin = Vector2.zero;
                    btnTextRect.offsetMax = Vector2.zero;
                    var btnText = btnTextGo.AddComponent<Text>();
                    btnText.font = SuperMechUiSkin.DefaultFont;
                    btnText.fontSize = 11;
                    btnText.color = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                    btnText.alignment = TextAnchor.MiddleCenter;
                    btnText.text = taskLabels[t];
                    btnText.raycastTarget = false;
                }

                itemY += 38f;
            }

            // 无单位提示
            if (planet.units.Count == 0)
            {
                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(contentGo.transform, false);
                var emptyRect = emptyGo.AddComponent<RectTransform>();
                emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
                emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
                emptyRect.sizeDelta = new Vector2(300, 30);
                var emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = SuperMechUiSkin.DefaultFont;
                emptyText.fontSize = 12;
                emptyText.color = new Color(0.6f, 0.6f, 0.6f);
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.text = LocalizedTextManager.getText("sm_ui_no_unit");
            }
        }

        /// <summary>隐藏单位管理面板</summary>
        private void HideUnitManagePanel()
        {
            if (_unitManagePanel != null)
            {
                Destroy(_unitManagePanel);
                _unitManagePanel = null;
            }
            _managePlanet = null;
        }

        private void ShowEventPopup(StarEventResult result)
        {
            if (result == null) return;

            var popupGo = new GameObject("EventPopup");
            popupGo.transform.SetParent(transform, false);
            var popupRect = popupGo.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(360, 220);
            popupRect.anchoredPosition = Vector2.zero;

            var bgImg = popupGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(popupGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -8);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 16;
            titleText.color = result.success ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = result.eventName;
            titleText.raycastTarget = false;

            // 描述
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(popupGo.transform, false);
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0);
            descRect.anchorMax = new Vector2(0.95f, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.sizeDelta = new Vector2(0, 0);
            descRect.anchoredPosition = new Vector2(0, -55);
            var descText = descGo.AddComponent<Text>();
            descText.font = SuperMechUiSkin.DefaultFont;
            descText.fontSize = 12;
            descText.color = new Color(0.8f, 0.85f, 0.9f);
            descText.alignment = TextAnchor.UpperCenter;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Overflow;
            descText.text = result.eventDesc + "\n\n" + result.resultMessage;
            descText.raycastTarget = false;

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(popupGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(120, 32);
            closeRect.anchoredPosition = new Vector2(0, 12);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.3f, 0.4f, 0.6f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => Destroy(popupGo));
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = SuperMechUiSkin.DefaultFont;
            closeText.fontSize = 12;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = LocalizedTextManager.getText("sm_ui_close");
            closeText.raycastTarget = false;
        }

        /// <summary>显示殖民地建设面板</summary>
        private void ShowColonyPanel(PlanetData planet)
        {
            ColonyManager.Init();

            var panelGo = new GameObject("ColonyPanel");
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420, 400);
            panelRect.anchoredPosition = Vector2.zero;

            var bgImg = panelGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.1f, 0.15f, 0.97f);

            // 标题
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 36);
            titleRect.anchoredPosition = new Vector2(0, -6);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = SuperMechUiSkin.DefaultFont;
            titleText.fontSize = 14;
            titleText.color = new Color(0.9f, 0.8f, 0.5f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = planet.name + " - " + LocalizedTextManager.getText("sm_ui_colony_building");
            titleText.raycastTarget = false;

            // v0.185 殖民地总览信息
            var overviewGo = new GameObject("Overview");
            overviewGo.transform.SetParent(panelGo.transform, false);
            var overviewRect = overviewGo.AddComponent<RectTransform>();
            overviewRect.anchorMin = new Vector2(0, 1);
            overviewRect.anchorMax = new Vector2(1, 1);
            overviewRect.pivot = new Vector2(0.5f, 1);
            overviewRect.sizeDelta = new Vector2(0, 50);
            overviewRect.anchoredPosition = new Vector2(0, -42);
            var overviewText = overviewGo.AddComponent<Text>();
            overviewText.font = SuperMechUiSkin.DefaultFont;
            overviewText.fontSize = 11;
            overviewText.color = new Color(0.7f, 0.8f, 0.9f);
            overviewText.alignment = TextAnchor.UpperLeft;
            overviewText.horizontalOverflow = HorizontalWrapMode.Wrap;
            int buildingCount = planet.buildings != null ? planet.buildings.Count : 0;
            int unitCount = planet.units != null ? planet.units.Count : 0;
            overviewText.text = $"等级: {planet.colonyLevel}  资源: {Mathf.RoundToInt(planet.resource)}  建筑: {buildingCount}  驻留单位: {unitCount}";
            overviewText.raycastTarget = false;

            // 建筑列表滚动区域
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.82f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 360);

            float itemY = 0f;
            foreach (var def in ColonyManager.GetAllDefs())
            {
                // 查找当前建筑等级
                int level = 0;
                bool isBuilding = false;
                float buildProgress = 0f;
                if (planet.buildings != null)
                {
                    foreach (var b in planet.buildings)
                    {
                        if (b.type == def.type)
                        {
                            level = b.level;
                            isBuilding = b.isBuilding;
                            buildProgress = b.buildProgress;
                            break;
                        }
                    }
                }

                bool locked = planet.colonyLevel < def.requiredColonyLevel;
                bool maxed = level >= def.maxLevel;
                float cost = ColonyManager.GetUpgradeCost(def, level);

                // 建筑条目背景
                var itemGo = new GameObject("BuildingItem");
                itemGo.transform.SetParent(contentGo.transform, false);
                var itemRect = itemGo.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.sizeDelta = new Vector2(0, 42);
                itemRect.anchoredPosition = new Vector2(0, -itemY);

                var itemBg = itemGo.AddComponent<Image>();
                itemBg.color = locked ? new Color(0.15f, 0.15f, 0.15f, 0.8f) : new Color(0.15f, 0.18f, 0.22f, 0.8f);

                // 建筑名称和等级
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(itemGo.transform, false);
                var nameRect = nameGo.AddComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.02f, 0);
                nameRect.anchorMax = new Vector2(0.6f, 1);
                nameRect.offsetMin = Vector2.zero;
                nameRect.offsetMax = Vector2.zero;
                var nameText = nameGo.AddComponent<Text>();
                nameText.font = SuperMechUiSkin.DefaultFont;
                nameText.fontSize = 11;
                nameText.color = locked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                string statusText = isBuilding ? $" ({Mathf.RoundToInt(buildProgress * 100)}%)" : (maxed ? " (MAX)" : $" Lv.{level}");
                nameText.text = LocalizedTextManager.getText(def.nameKey) + statusText;
                nameText.raycastTarget = false;

                // 升级按钮
                if (!locked && !maxed && !isBuilding)
                {
                    var btnGo = new GameObject("UpgradeBtn");
                    btnGo.transform.SetParent(itemGo.transform, false);
                    var btnRect = btnGo.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.7f, 0.15f);
                    btnRect.anchorMax = new Vector2(0.98f, 0.85f);
                    btnRect.offsetMin = Vector2.zero;
                    btnRect.offsetMax = Vector2.zero;
                    var btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = new Color(0.2f, 0.5f, 0.3f, 0.9f);
                    var btn = btnGo.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        bool success = ColonyManager.UpgradeBuilding(planet, def.type);
                        if (success)
                        {
                            Destroy(panelGo);
                            ShowColonyPanel(planet);
                            SelectPlanet(planet.id);
                        }
                    });
                    var btnTextGo = new GameObject("Text");
                    btnTextGo.transform.SetParent(btnGo.transform, false);
                    var btnTextRect = btnTextGo.AddComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.offsetMin = Vector2.zero;
                    btnTextRect.offsetMax = Vector2.zero;
                    var btnText = btnTextGo.AddComponent<Text>();
                    btnText.font = SuperMechUiSkin.DefaultFont;
                    btnText.fontSize = 10;
                    btnText.color = Color.white;
                    btnText.alignment = TextAnchor.MiddleCenter;
                    btnText.text = $"{Mathf.RoundToInt(cost)}₡";
                    btnText.raycastTarget = false;
                }
                else if (locked)
                {
                    var lockGo = new GameObject("LockText");
                    lockGo.transform.SetParent(itemGo.transform, false);
                    var lockRect = lockGo.AddComponent<RectTransform>();
                    lockRect.anchorMin = new Vector2(0.7f, 0);
                    lockRect.anchorMax = new Vector2(0.98f, 1);
                    lockRect.offsetMin = Vector2.zero;
                    lockRect.offsetMax = Vector2.zero;
                    var lockText = lockGo.AddComponent<Text>();
                    lockText.font = SuperMechUiSkin.DefaultFont;
                    lockText.fontSize = 10;
                    lockText.color = new Color(0.5f, 0.5f, 0.5f);
                    lockText.alignment = TextAnchor.MiddleCenter;
                    lockText.text = $"需殖民地Lv.{def.requiredColonyLevel}";
                    lockText.raycastTarget = false;
                }

                itemY += 46f;
            }

            // 关闭按钮
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(panelGo.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.pivot = new Vector2(0.5f, 0);
            closeRect.sizeDelta = new Vector2(100, 30);
            closeRect.anchoredPosition = new Vector2(0, 8);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.4f, 0.3f, 0.3f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => Destroy(panelGo));
            var closeTextGo = new GameObject("Text");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = SuperMechUiSkin.DefaultFont;
            closeText.fontSize = 12;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = LocalizedTextManager.getText("sm_ui_close");
            closeText.raycastTarget = false;
        }
    }
}
