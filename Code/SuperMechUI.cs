using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 「超神机械师」专属神权Tab（参考蛊真人简洁风格）。
    /// 只保留核心地图交互神权+开窗按钮，其他功能放到单位面板/超能者面板。
    /// </summary>
    public static class SuperMechUI
    {
        private const string Layout = "tools";
        private static bool _inited;
        private static PowersTab _tab;

        // 核心地图交互神权（精简版，参考蛊真人）
        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] CorePowers =
        {
            // 召唤类
            (SuperMechPowers.SummonAwakened,  "ui/powers/power_summon_units", "召唤降临者", "在点击位置生成一个有面板的降临者单位（走等级职业体系）"),
            // 天灾类
            (SuperMechPowers.DisasterAlien,   "ui/powers/power_meteor",       "异化之灾",     "在点击位置生成异化体（天灾）"),
            // 突破类
            (SuperMechPowers.AttemptTranscend,"ui/powers/power_bless",        "冲击超神级",   "点击单位尝试突破超神级（需满足三条件，有恶性变异风险）"),
        };

        // 五系觉醒神权（合并为一个，点击后打开选择系别的窗口）
        private const string AwakenPowerId = "sm_awaken_all";
        private const string AwakenPowerName = "五系觉醒";

        // 开窗按钮（不进入神力模式，直接开窗）
        private static readonly (string name, string tip, System.Action action)[] WindowButtons =
        {
            ("超能者面板", "管理选中单位：觉醒/转职/制造/知识/修炼/融合/查看属性", () => SuperMechPanel.Show()),
            ("六圣所",     "查看六圣所解锁进度与碎片（跨存档）",                   () => SuperMechSanctuaryWindow.Show()),
            ("排行榜",     "超能者欧纳/气力/阶位排行榜（前20名）",                 () => SuperMechRankWindow.Show()),
        };

        public static void Init()
        {
            if (_inited) return;
            try
            {
                _inited = true;

                LocalizedTextManager.add("supermech.tab", "超神机械师", pReplace: true);
                LocalizedTextManager.add("supermech.tab_desc", "五系觉醒·机械军团·圣所轮回", pReplace: true);

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconChosenOne");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                // 注册五系觉醒神权（合并版）
                RegisterAwakenPower();

                // 核心神权按钮
                foreach (var (id, iconPath, tipTitle, tipDesc) in CorePowers)
                {
                    try
                    {
                        GodPower power = AssetManager.powers.get(id);
                        if (power == null) { Debug.LogWarning("[超神机械师] 神权未注册: " + id); continue; }
                        Sprite icon = SpriteTextureLoader.getSprite(iconPath);
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(id, icon);
                        if (btn == null) continue;
                        btn.godPower = power;
                        SetupTooltip(btn, tipTitle, tipDesc);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 神权按钮失败 " + id + ": " + e.Message); }
                }

                // 五系觉醒按钮（打开选择系别的窗口）
                try
                {
                    GodPower power = AssetManager.powers.get(AwakenPowerId);
                    if (power != null)
                    {
                        Sprite icon = SpriteTextureLoader.getSprite("ui/powers/power_bless");
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(AwakenPowerId, icon);
                        if (btn != null)
                        {
                            btn.godPower = power;
                            SetupTooltip(btn, AwakenPowerName, "点击单位后选择觉醒系别（机械/武道/异能/魔法/念力）");
                            _tab.AddPowerButton(Layout, btn);
                        }
                    }
                }
                catch (System.Exception e) { Debug.LogError("[超神机械师] 五系觉醒按钮失败: " + e.Message); }

                // 开窗按钮
                foreach (var (name, tip, action) in WindowButtons)
                {
                    try
                    {
                        Sprite icon = SpriteTextureLoader.getSprite("ui/powers/power_bless");
                        PowerButton btn = PowerButtonCreator.CreateSimpleButton(name, () => { try { action?.Invoke(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗异常: " + e.Message); } }, icon);
                        if (btn == null) continue;
                        SetupTooltip(btn, name, tip);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗按钮失败 " + name + ": " + e.Message); }
                }

                _tab.UpdateLayout();
                Debug.Log($"[超神机械师] 专属Tab创建成功：{CorePowers.Length + 1}神权+{WindowButtons.Length}窗口（精简版）");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] Tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }

        /// <summary>注册五系觉醒神权（合并版，点击后打开选择系别窗口）。</summary>
        private static void RegisterAwakenPower()
        {
            if (AssetManager.powers.get(AwakenPowerId) != null) return;
            LocalizedTextManager.add(AwakenPowerName, AwakenPowerName, pReplace: true);
            var p = new GodPower
            {
                id = AwakenPowerId,
                name = AwakenPowerName,
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                bool applied = false;
                tile.doUnits(u =>
                {
                    if (u == null || applied) return;
                    // 打开选择系别的窗口
                    SuperMechAwakenWindow.Show(u);
                    applied = true;
                });
                return applied;
            };
            AssetManager.powers.add(p);
        }

        private static void SetupTooltip(PowerButton btn, string title, string desc)
        {
            try
            {
                if (btn == null) return;
                var tip = btn.GetComponent<WorldTip>();
                if (tip == null) tip = btn.gameObject.AddComponent<WorldTip>();
                if (tip.text != null) tip.text.text = $"{title}\n{desc}";
            }
            catch { }
        }
    }
}
