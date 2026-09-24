using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using SuperMech.Code;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 创建「超神机械师」专属神权tab，把所有神权按钮放进去。
    /// 参考蛊真人 GzPowersTab：TabManager.CreateTab + AddPowerButton + UpdateLayout。
    /// 必须在所有 powers 注册之后调用（PowerButton.OnEnable 按名字查 powers）。
    /// </summary>
    public static class SuperMechUI
    {
        private const string Layout = "tools";
        private static bool _inited;
        private static PowersTab _tab;

        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] Buttons =
        {
            (SuperMechPowers.SummonRanger,   "ui/powers/power_summon_units", "召唤机械游骑兵", "在点击位置生成一个机械游骑兵单位"),
            (SuperMechPowers.SummonMech,     "ui/powers/power_summon_units", "召唤机甲",       "在点击位置生成一个机甲单位"),
            (SuperMechPowers.AwakenPsi,      "ui/powers/power_bless",        "赋予异能系觉醒", "点击单位，赋予异能系觉醒特质"),
            (SuperMechPowers.AwakenMech,     "ui/powers/power_bless",        "赋予机械系觉醒", "点击单位，赋予机械系觉醒特质"),
            (SuperMechPowers.AwakenMartial,  "ui/powers/power_bless",        "赋予武道系觉醒", "点击单位，赋予武道系觉醒特质"),
            (SuperMechPowers.DisasterAlien,  "ui/powers/power_meteor",       "异化之灾",       "在点击位置生成异化体（天灾）"),
            ("sm_enter_sanctuary",           "ui/Icons/actor_traits/iconHardSkin",           "进入圣所",       "消耗3块圣所钥匙碎片进入圣所，获得跨存档buff"),
            ("sm_give_sm_pcult_resonance",   "ui/powers/power_bless",        "传授：基因共鸣", "点击异能系单位，传授基因共鸣功法"),
            ("sm_give_sm_pcult_meditation",  "ui/powers/power_bless",        "传授：冥想",     "点击魔法系单位，传授冥想功法"),
            ("sm_give_sm_pcult_mind_train",  "ui/powers/power_bless",        "传授：心灵锻炼", "点击念力系单位，传授心灵锻炼功法"),
            (SuperMechPowers.CheckPotential, "ui/powers/power_bless",        "查看潜能点",     "点击单位，查看气力/潜能点/觉醒点/已解锁知识"),
            (SuperMechPowers.UnlockArmed,    "ui/powers/power_bless",        "解锁·武装系知识", "点击机械系单位，消耗2潜能点解锁武装系知识节点（转职后其他分支×3）"),
            (SuperMechPowers.UnlockEnergy,   "ui/powers/power_bless",        "解锁·能量系知识", "点击机械系单位，消耗2潜能点解锁能量系知识节点（转职后其他分支×3）"),
            (SuperMechPowers.UnlockVirtual,  "ui/powers/power_bless",        "解锁·虚拟系知识", "点击机械系单位，消耗3潜能点解锁虚拟系知识节点（转职后其他分支×3）"),
            // 造兵闭环（原著 ch3/ch50：制造→经验→升级→解锁）
            ("sm_craft_ranger",    "ui/powers/power_summon_units", "制造·游骑兵",     "点击机械师单位，制造游骑兵（气力+5，需tier1）"),
            ("sm_craft_drone",     "ui/powers/power_summon_units", "制造·侦察无人机", "点击机械师单位，制造无人机（气力+8，需tier2）"),
            ("sm_craft_mech",      "ui/powers/power_summon_units", "制造·战斗机甲",   "点击机械师单位，制造机甲（气力+20，需磁环tier4）"),
            ("sm_craft_fortress",  "ui/powers/power_summon_units", "制造·战争堡垒",   "点击机械师单位，制造堡垒（气力+50，需战争tier6）"),
            ("sm_craft_virtual",   "ui/powers/power_summon_units", "制造·虚拟生命体", "点击机械师单位，制造虚拟生命（气力+80，需虚拟tier7）"),
            // 五系三分支选择（机械原著ch50，其他四系参考同人二创）
            ("sm_select_gunner",        "ui/powers/power_bless", "机械·枪炮师",   "磁环以上机械师，武装分支（伤害+30%攻速+20%）"),
            ("sm_select_mech",          "ui/powers/power_bless", "机械·机械师",   "磁环以上机械师，能量分支（制造+50%智力+10）"),
            ("sm_select_martial",       "ui/powers/power_bless", "机械·械武者",   "磁环以上机械师，操控分支（生命+40%护甲+5）"),
            ("sm_select_martial_body",  "ui/powers/power_bless", "武道·体魄",     "D阶以上武道系，体魄分支（生命+50%耐力+10）"),
            ("sm_select_martial_tactic","ui/powers/power_bless", "武道·战术",     "D阶以上武道系，战术分支（攻速+25%暴击+10%）"),
            ("sm_select_martial_power", "ui/powers/power_bless", "武道·超能",     "D阶以上武道系，超能分支（离体波动/闪气/暴气，伤害+40%）"),
            ("sm_select_psi_attack",    "ui/powers/power_bless", "异能·攻效",     "D阶以上异能系，能级强化（伤害+50%暴击+5%）"),
            ("sm_select_psi_cycle",     "ui/powers/power_bless", "异能·循环",     "D阶以上异能系，持久力强化（生命+30%耐力+10）"),
            ("sm_select_psi_func",      "ui/powers/power_bless", "异能·功能",     "D阶以上异能系，操控强化（智力+15攻速+15%）"),
            ("sm_select_mage_element",  "ui/powers/power_bless", "魔法·元素",     "D阶以上魔法系，元素分支（伤害+45%暴击+8%）"),
            ("sm_select_mage_change",   "ui/powers/power_bless", "魔法·变化",     "D阶以上魔法系，变化分支（攻速+30%移速+20%）"),
            ("sm_select_mage_create",   "ui/powers/power_bless", "魔法·造物",     "D阶以上魔法系，造物分支（生命+35%经验+30%）"),
            ("sm_select_mind_soul",     "ui/powers/power_bless", "念力·灵魂",     "D阶以上念力系，灵魂分支（智力+20暴击+12%）"),
            ("sm_select_mind_law",      "ui/powers/power_bless", "念力·法则",     "D阶以上念力系，法则分支（伤害+35%全属性+5）"),
            ("sm_select_mind_reality",  "ui/powers/power_bless", "念力·现实",     "D阶以上念力系，现实分支（生命+45%护甲+8）"),
        };

        public static void Init()
        {
            if (_inited) return;
            try
            {
                _inited = true;

                // 注册tab的本地化文本
                LocalizedTextManager.add("supermech.tab", "超神机械师", pReplace: true);
                LocalizedTextManager.add("supermech.tab_desc", "超神机械师模组：五系觉醒、机械军团、圣所轮回", pReplace: true);

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconHardSkin");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null，专属tab创建失败");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                foreach (var (id, iconPath, tipTitle, tipDesc) in Buttons)
                {
                    try
                    {
                        GodPower power = AssetManager.powers.get(id);
                        if (power == null)
                        {
                            Debug.LogWarning("[超神机械师] power未注册，跳过按钮: " + id);
                            continue;
                        }

                        Sprite icon = SpriteTextureLoader.getSprite(iconPath);
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(id, icon);
                        if (btn == null)
                        {
                            Debug.LogWarning("[超神机械师] CreateGodPowerButton返回null: " + id);
                            continue;
                        }

                        // 防御：显式绑定（PowerButton.OnEnable 可能早于注册）
                        btn.godPower = power;

                        // tooltip
                        try
                        {
                            TipButton tip = btn.GetComponent<TipButton>();
                            if (tip == null) tip = btn.gameObject.AddComponent<TipButton>();
                            tip.textOnClick = tipTitle + "\n" + tipDesc;
                        }
                        catch { }

                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[超神机械师] 创建按钮失败 " + id + ": " + e.Message);
                    }
                }

                _tab.UpdateLayout();
                Debug.Log("[超神机械师] 专属神权tab创建成功，按钮已注入");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 专属tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }
    }
}
