using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using SuperMech.Code;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 「超神机械师」专属神权Tab。
    /// 地图交互神权（召唤/天灾/觉醒/突破）+ 开窗按钮（面板/圣所/排行/配置）。
    /// </summary>
    public static class SuperMechUI
    {
        private const string Layout = "tools";
        private static bool _inited;
        private static PowersTab _tab;

        // 地图交互神权（需要画笔/点地图/点单位）
        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] MapPowers =
        {
            // 召唤类
            (SuperMechPowers.SummonRanger,    "ui/powers/power_summon_units", "召唤机械游骑兵", "在点击位置生成一个机械游骑兵单位"),
            (SuperMechPowers.SummonMech,      "ui/powers/power_summon_units", "召唤机甲",       "在点击位置生成一个机甲单位"),
            (SuperMechPowers.SummonAwakened,  "ui/powers/power_summon_units", "召唤降临者",     "在点击位置生成一个有面板的降临者单位（走等级职业体系）"),
            // 天灾类
            (SuperMechPowers.DisasterAlien,   "ui/powers/power_meteor",       "异化之灾",       "在点击位置生成异化体（天灾）"),
            // 突破类
            (SuperMechPowers.AttemptTranscend,"ui/powers/power_bless",        "冲击超神级",     "点击单位尝试突破超神级（需满足三条件，有恶性变异风险）"),
            (SuperMechPowers.MechFusion,     "ui/powers/power_bless",        "械力融合",       "点击机械系单位将装备与身体融合（需磁环阶段+蓝色以上装备）"),
            (SuperMechPowers.KnowledgeFusion,"ui/powers/power_bless",        "知识融合",       "点击降临者单位消耗经验融合知识获得图纸（原著ch107）"),
            // 知识类
            (SuperMechPowers.CheckPotential,  "ui/powers/power_bless",        "查看潜能点",     "点击单位查看气力/潜能点/觉醒点/已解锁知识数"),
            (SuperMechPowers.UnlockArmed,     "ui/powers/power_bless",        "解锁知识·武装系", "点击单位消耗潜能点解锁武装系知识节点"),
            (SuperMechPowers.UnlockEnergy,    "ui/powers/power_bless",        "解锁知识·能量系", "点击单位消耗潜能点解锁能量系知识节点"),
            (SuperMechPowers.UnlockVirtual,   "ui/powers/power_bless",        "解锁知识·虚拟系", "点击单位消耗潜能点解锁虚拟系知识节点"),
            // 制造类（机械师专属，点单位制造）
            ("sm_craft_ranger",  "ui/powers/power_summon_units", "制造·游骑兵",     "点击机械师单位制造游骑兵（tier1）"),
            ("sm_craft_drone",   "ui/powers/power_summon_units", "制造·侦察无人机", "点击机械师单位制造侦察无人机（tier2）"),
            ("sm_craft_mech",    "ui/powers/power_summon_units", "制造·战斗机甲",   "点击机械师单位制造战斗机甲（tier4）"),
            ("sm_craft_fortress","ui/powers/power_summon_units", "制造·战争堡垒",   "点击机械师单位制造战争堡垒（tier6）"),
            ("sm_craft_virtual", "ui/powers/power_summon_units", "制造·虚拟生命体", "点击机械师单位制造虚拟生命体（tier7）"),
        };

        // 五系觉醒神权（点单位赋予觉醒）
        private static readonly (string id, string icon, string tipTitle, string tipDesc, string traitId)[] AwakenPowers =
        {
            ("sm_awaken_mech",    "ui/powers/power_bless", "觉醒·机械系", "点击单位赋予机械系觉醒", SuperMechTraits.ClassMech),
            ("sm_awaken_martial", "ui/powers/power_bless", "觉醒·武道系", "点击单位赋予武道系觉醒", SuperMechTraits.ClassMartial),
            ("sm_awaken_psi",     "ui/powers/power_bless", "觉醒·异能系", "点击单位赋予异能系觉醒", SuperMechTraits.ClassPsi),
            ("sm_awaken_mage",    "ui/powers/power_bless", "觉醒·魔法系", "点击单位赋予魔法系觉醒", SuperMechTraits.ClassMage),
            ("sm_awaken_mind",    "ui/powers/power_bless", "觉醒·念力系", "点击单位赋予念力系觉醒", SuperMechTraits.ClassMind),
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

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconChosenOne");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                // 注册五系觉醒神权
                RegisterAwakenPowers();

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

                // 五系觉醒神权按钮
                foreach (var (id, iconPath, tipTitle, tipDesc, traitId) in AwakenPowers)
                {
                    try
                    {
                        GodPower power = AssetManager.powers.get(id);
                        if (power == null) { Debug.LogWarning("[超神机械师] 觉醒神权未注册: " + id); continue; }
                        Sprite icon = SpriteTextureLoader.getSprite(iconPath);
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(id, icon);
                        if (btn == null) continue;
                        btn.godPower = power;
                        SetupTooltip(btn, tipTitle, tipDesc);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 觉醒神权按钮失败 " + id + ": " + e.Message); }
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
                Debug.Log($"[超神机械师] 专属Tab创建成功：{MapPowers.Length}神权+{AwakenPowers.Length}觉醒+{WindowButtons.Length}窗口");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] Tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }

        /// <summary>注册五系觉醒神权（点单位赋予对应系觉醒特质）。</summary>
        private static void RegisterAwakenPowers()
        {
            foreach (var (id, icon, name, desc, traitId) in AwakenPowers)
            {
                if (AssetManager.powers.get(id) != null) continue; // 已注册
                LocalizedTextManager.add(name, name, pReplace: true);
                var p = new GodPower
                {
                    id = id,
                    name = name,
                    path_icon = icon,
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
                        if (SuperMechAdvancement.IsSuperMechUnit(u))
                        {
                            // 已有觉醒，替换系
                            u.removeTrait(SuperMechTraits.ClassMech);
                            u.removeTrait(SuperMechTraits.ClassMartial);
                            u.removeTrait(SuperMechTraits.ClassPsi);
                            u.removeTrait(SuperMechTraits.ClassMage);
                            u.removeTrait(SuperMechTraits.ClassMind);
                        }
                        u.addTrait(traitId);
                        SuperMechStage.SetStage(u, 1); // 入门者
                        SuperMechSpecialty.AssignRandomSpecialty(u);
                        Debug.Log($"[超神机械师] {u.name} 觉醒为{name}");
                        applied = true;
                    });
                    return applied;
                };
                AssetManager.powers.add(p);
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
