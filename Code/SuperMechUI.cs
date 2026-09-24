using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using SuperMech.Code;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 「超神机械师」专属神权Tab。
    /// 只有真正需要地图交互的才做神权（召唤×2、天灾×1）。
    /// 单位管理/圣所/排行全部走窗口，不用"点单位神权"。
    /// </summary>
    public static class SuperMechUI
    {
        private const string Layout = "tools";
        private static bool _inited;
        private static PowersTab _tab;

        // 地图交互神权（需要画笔/点地图）
        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] MapPowers =
        {
            (SuperMechPowers.SummonRanger,  "ui/powers/power_summon_units", "召唤机械游骑兵", "在点击位置生成一个机械游骑兵单位"),
            (SuperMechPowers.SummonMech,    "ui/powers/power_summon_units", "召唤机甲",       "在点击位置生成一个机甲单位"),
            (SuperMechPowers.DisasterAlien, "ui/powers/power_meteor",       "异化之灾",       "在点击位置生成异化体（天灾）"),
        };

        // 开窗按钮（不进入神力模式，直接开窗）
        private static readonly (string name, string tip, System.Action action)[] WindowButtons =
        {
            ("超能者面板", "管理选中单位：觉醒/转职/制造/知识/修炼/查看属性", () => SuperMechPanel.Show()),
            ("六圣所",     "查看六圣所解锁进度与碎片（跨存档）",             () => SuperMechSanctuaryWindow.Show()),
            ("排行榜",     "超能者欧纳/气力/阶位排行榜（前20名）",           () => SuperMechRankWindow.Show()),
        };

        public static void Init()
        {
            if (_inited) return;
            try
            {
                _inited = true;

                LocalizedTextManager.add("supermech.tab", "超神机械师", pReplace: true);
                LocalizedTextManager.add("supermech.tab_desc", "五系觉醒·机械军团·圣所轮回", pReplace: true);

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconHardSkin");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                // 地图神权按钮
                foreach (var (id, iconPath, tipTitle, tipDesc) in MapPowers)
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

                // 开窗按钮（SimpleButton不进入神力模式）
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
                Debug.Log($"[超神机械师] 专属Tab创建成功：{MapPowers.Length}神权+{WindowButtons.Length}窗口");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] Tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }

        private static void SetupTooltip(PowerButton button, string title, string description)
        {
            if (button == null) return;
            try
            {
                TipButton tip = button.GetComponent<TipButton>();
                if (tip == null) tip = button.gameObject.AddComponent<TipButton>();
                tip.textOnClick = title + "\n" + description;
            }
            catch { }
        }
    }
}
