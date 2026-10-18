using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.39 方案A：超能详情页直接嵌入原版单位面板。
    /// 不再自绘 SuperMechActorDetailView，改为 Harmony Postfix
    /// UnitWindow.showStatsRows()，在原版属性行（生日/金钱/战利品等）
    /// 后面追加超能属性行（职业/阶位/觉醒等级/知识/装备/法术）。
    /// 优点：零布局问题、风格和游戏完全一致、自动适配所有分辨率。
    /// </summary>
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindowStatsPatch
    {
        private static MethodInfo _showStatRowMethod;

        static void Postfix(UnitWindow __instance)
        {
            try
            {
                // 获取 actor（internal 属性 actor => SelectedUnit.unit）
                var actorProp = typeof(UnitWindow).GetProperty("actor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (actorProp == null) return;
                var actor = actorProp.GetValue(__instance) as Actor;
                if (actor == null || actor.isRekt()) return;

                // 缓存 showStatRow 方法（带颜色参数的重载）
                if (_showStatRowMethod == null)
                {
                    _showStatRowMethod = typeof(StatsWindow).GetMethod("showStatRow",
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new[] {
                            typeof(string), typeof(object), typeof(string),
                            typeof(MetaType), typeof(long), typeof(bool),
                            typeof(string), typeof(string), typeof(TooltipDataGetter), typeof(bool)
                        },
                        null);
                }
                if (_showStatRowMethod == null) return;

                // 只在有超能数据时显示（避免普通单位也显示空行）
                bool hasAny = false;
                var prof = SuperMechProfession.GetProfession(actor);
                if (prof != ProfessionType.None) hasAny = true;
                int rankIdx = SuperMechAdvancement.GetRankIndex(actor);
                if (rankIdx > 0) hasAny = true;
                int awakenedLv = SuperMechAwakened.GetLevel(actor);
                if (awakenedLv > 0) hasAny = true;
                int knowledgeCount = SuperMechKnowledge.GetUnlockedCount(actor, "");
                int equipCount = SuperMechEquipBag.GetBag(actor)?.Count ?? 0;
                int spellCount = SuperMechSpell.GetLearnedCount(actor);
                if (knowledgeCount > 0 || equipCount > 0 || spellCount > 0) hasAny = true;

                if (!hasAny) return;

                // 分隔线（用空行模拟）
                ShowRow(__instance, "sm_ui_super_mech_divider", "──────────", "#8B7355");

                // 职业
                if (prof != ProfessionType.None)
                {
                    ShowRow(__instance, "sm_ui_profession",
                        SuperMechProfession.GetProfessionName(prof), "#FFD700");
                }

                // 阶位
                if (rankIdx > 0)
                {
                    ShowRow(__instance, "sm_ui_rank",
                        SuperMechRanks.GetRankNameWithSub(actor), "#FF6B6B");
                }

                // 觉醒等级
                if (awakenedLv > 0)
                {
                    ShowRow(__instance, "sm_ui_awakened_level",
                        SuperMechAwakened.GetLevelText(actor), "#4ECDC4");
                }

                // 知识数量
                if (knowledgeCount > 0)
                {
                    ShowRow(__instance, "sm_ui_knowledge_count",
                        knowledgeCount.ToString(), "#95E1D3");
                }

                // 装备数量
                if (equipCount > 0)
                {
                    ShowRow(__instance, "sm_ui_equipment_count",
                        equipCount.ToString(), "#F38181");
                }

                // 法术数量
                if (spellCount > 0)
                {
                    ShowRow(__instance, "sm_ui_spell_count",
                        spellCount.ToString(), "#AA96DA");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] UnitWindow属性行注入异常: " + e.Message);
            }
        }

        private static void ShowRow(UnitWindow instance, string id, object value, string color)
        {
            if (_showStatRowMethod == null) return;
            _showStatRowMethod.Invoke(instance, new object[] {
                id, value, color, MetaType.None, -1L, false,
                null, null, null, true
            });
        }
    }
}
