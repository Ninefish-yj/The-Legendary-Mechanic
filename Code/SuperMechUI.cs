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
        // 图标使用原版确认存在的路径，避免sprite为null导致光标图标崩溃
        // 五系觉醒和神之催化已移到单位面板知识Tab的操作区域
        private static readonly (string id, string icon, string tipTitle, string tipDesc)[] CorePowers =
        {
            // 召唤类
            (SuperMechPowers.SummonAwakened,  "iconSprite",      "召唤降临者", "在点击位置生成一个有面板的降临者单位（走等级职业体系）"),
            // 天灾类
            (SuperMechPowers.DisasterAlien,   "iconDiscord",     "异化之灾",     "在点击位置生成异化体（天灾）"),
        };

        // 开窗按钮（不进入神力模式，直接开窗）
        private static readonly (string name, string tip, System.Action action)[] WindowButtons =
        {
            ("六圣所",     "查看六圣所解锁进度与碎片（跨存档）", () => SuperMechSanctuaryWindow.Show()),
            ("排行榜",     "超能者欧纳/气力/阶位排行榜（前20名）", () => SuperMechRankWindow.Show()),
        };

        public static void Init()
        {
            if (_inited) return;
            try
            {
                _inited = true;

                LocalizedTextManager.add("supermech.tab", "超神机械师", pReplace: true);
                LocalizedTextManager.add("supermech.tab_desc", "五系觉醒·机械军团·圣所轮回", pReplace: true);

                // 注册缺失的本地化key（单位面板行标签+五系觉醒描述）
                LocalizedTextManager.add("体系", "体系", pReplace: true);
                LocalizedTextManager.add("提示", "提示", pReplace: true);
                LocalizedTextManager.add("觉醒_机械系", "觉醒·机械系", pReplace: true);
                LocalizedTextManager.add("觉醒_机械系_description", "点击单位赋予机械系觉醒（械感天赋·机械知识树）", pReplace: true);
                LocalizedTextManager.add("觉醒_武道系", "觉醒·武道系", pReplace: true);
                LocalizedTextManager.add("觉醒_武道系_description", "点击单位赋予武道系觉醒（体魄天赋·御气技巧树）", pReplace: true);
                LocalizedTextManager.add("觉醒_异能系", "觉醒·异能系", pReplace: true);
                LocalizedTextManager.add("觉醒_异能系_description", "点击单位赋予异能系觉醒（异能潜力·基因树）", pReplace: true);
                LocalizedTextManager.add("觉醒_魔法系", "觉醒·魔法系", pReplace: true);
                LocalizedTextManager.add("觉醒_魔法系_description", "点击单位赋予魔法系觉醒（魔法天赋·魔法知识树）", pReplace: true);
                LocalizedTextManager.add("觉醒_念力系", "觉醒·念力系", pReplace: true);
                LocalizedTextManager.add("觉醒_念力系_description", "点击单位赋予念力系觉醒（精神天赋·精神修炼树）", pReplace: true);

                // 批量注册单位面板所有行标签（ShowRow的第一个参数会被当作本地化key查找）
                string[] panelLabels = {
                    "阶位", "种族", "名号", "职业", "职业等级", "职业阶段", "职业树", "职业技能",
                    "体系", "转职", "转职条件", "气力", "气力属性", "气力上限", "欧纳",
                    "械感", "魔感", "神秘", "魅力", "幸运", "精神力", "魔力池", "基因链",
                    "传承度", "冥冥感应", "神性蜕变", "超神遗力", "信息态", "潜力评级",
                    "副职业", "专长", "械力融合", "知识融合", "装备", "宇宙宝物", "圣所",
                    "法师塔", "次级维度", "提炼法", "传说度", "超神突破", "状态", "潜能点",
                    "分支", "提示", "制造", "操作"
                };
                foreach (var label in panelLabels)
                    LocalizedTextManager.add(label, label, pReplace: true);

                // 注册神权和Tab的tooltip文本（TipButton会把textOnClickDescription当本地化key查找）
                string[] tooltipTexts = {
                    "召唤降临者", "召唤降临者_description", "在点击位置生成一个有面板的降临者单位（走等级职业体系）",
                    "异化之灾", "异化之灾_天灾_description", "在点击位置生成异化体（天灾）",
                    "神之催化", "神之催化_description", "点击SS阶以上单位施加催化效果，降低突破门槛、提升成功率（每层+10%，最多5层）",
                    "冲击超神级", "冲击超神级_description",
                    "五系觉醒", "五系觉醒_description", "点击单位后选择觉醒系别（机械/武道/异能/魔法/念力）",
                    "六圣所", "查看六圣所解锁进度与碎片（跨存档）",
                    "排行榜", "超能者欧纳/气力/阶位排行榜（前20名）",
                    "装备背包", "查看与管理单位的装备背包",
                    "知识", "查看已解锁的知识节点与职业树"
                };
                foreach (var text in tooltipTexts)
                    LocalizedTextManager.add(text, text, pReplace: true);

                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconChosenOne");
                _tab = TabManager.CreateTab("supermech_mod_tab", "supermech.tab", "supermech.tab_desc", tabIcon);
                if (_tab == null)
                {
                    Debug.LogError("[超神机械师] TabManager.CreateTab 返回 null");
                    _inited = false;
                    return;
                }

                _tab.SetLayout(new List<string> { Layout });

                // 核心神权按钮
                foreach (var (id, iconPath, tipTitle, tipDesc) in CorePowers)
                {
                    try
                    {
                        GodPower power = AssetManager.powers.get(id);
                        if (power == null) { Debug.LogWarning("[超神机械师] 神权未注册: " + id); continue; }
                        Sprite icon = SpriteTextureLoader.getSprite(iconPath);
                        if (icon == null) icon = SpriteTextureLoader.getSprite("iconDivineLight"); // fallback
                        PowerButton btn = PowerButtonCreator.CreateGodPowerButton(id, icon);
                        if (btn == null) continue;
                        btn.godPower = power;
                        // 防御：确保icon.sprite不为null，避免drawCursorSprite崩溃
                        if (btn.icon != null && btn.icon.sprite == null && icon != null)
                            btn.icon.sprite = icon;
                        SetupTooltip(btn, tipTitle, tipDesc);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 神权按钮失败 " + id + ": " + e.Message); }
                }

                // 五系觉醒和神之催化已移到单位面板知识Tab的操作区域

                // 开窗按钮
                foreach (var (name, tip, action) in WindowButtons)
                {
                    try
                    {
                        Sprite icon = SpriteTextureLoader.getSprite("iconUnity");
                        if (icon == null) icon = SpriteTextureLoader.getSprite("iconDivineLight");
                        PowerButton btn = PowerButtonCreator.CreateSimpleButton(name, () => { try { action?.Invoke(); } catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗异常: " + e.Message); } }, icon);
                        if (btn == null) continue;
                        if (btn.icon != null && btn.icon.sprite == null && icon != null)
                            btn.icon.sprite = icon;
                        SetupTooltip(btn, name, tip);
                        _tab.AddPowerButton(Layout, btn);
                    }
                    catch (System.Exception e) { Debug.LogError("[超神机械师] 开窗按钮失败 " + name + ": " + e.Message); }
                }

                _tab.UpdateLayout();
                Debug.Log($"[超神机械师] 专属Tab创建成功：{CorePowers.Length}神权+{WindowButtons.Length}窗口（五系觉醒/神之催化在单位面板）");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] Tab初始化失败: " + e.Message + "\n" + e.StackTrace);
                _inited = false;
            }
        }

        private static void SetupTooltip(PowerButton btn, string title, string desc)
        {
            try
            {
                if (btn == null) return;
                // 参考蛊真人：title和description合并成一个字符串，设置到textOnClick
                // 不要分别设置textOnClickDescription（会被当作本地化key查找导致缺失）
                var tipBtn = btn.GetComponent<TipButton>();
                if (tipBtn == null) tipBtn = btn.gameObject.AddComponent<TipButton>();
                tipBtn.textOnClick = title + "\n" + desc;
            }
            catch { }
        }
    }
}
