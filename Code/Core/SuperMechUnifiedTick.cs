using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechUnifiedTick
    {
        private const int AutoDisableThreshold = 5;
        private static int _tickCounter = 0;
        private static float _lastSaveTime = 0f;
        private static readonly System.Collections.Generic.Dictionary<string, int> _failCount = new();
        private static readonly System.Collections.Generic.HashSet<string> _disabled = new();

        private static void SafeRun(string name, System.Action action)
        {
            if (_disabled.Contains(name)) return;
            try
            {
                action();
                _failCount[name] = 0; // 成功后清零
            }
            catch (System.Exception e)
            {
                _failCount.TryGetValue(name, out int c);
                int next = c + 1;
                _failCount[name] = next;
                if (next >= AutoDisableThreshold)
                {
                    _disabled.Add(name);
                    Debug.LogError($"[超神机械师] 子系统[{name}]连续失败{next}次，已自动禁用");
                }
                else if (next <= 3 || next % 20 == 0)
                {
                    Debug.LogError($"[超神机械师] 子系统[{name}]异常(第{next}次): {e.Message}");
                }
            }
        }

        public static void Tick()
        {
            if (World.world == null) return;

            _tickCounter++;
            int group = _tickCounter % 4;

            if (group == 0)
            {
                SafeRun("气力等级", () => SuperMechQi.TickQiLevels());
                SafeRun("核心神力", () => SuperMechCorePower.TickCorePowers());
                SafeRun("属性自动分配", () => SuperMechQiAttribute.TickAutoAssign());
                SafeRun("气力提炼", () => SuperMechQiRefine.TickRefine());
                SafeRun("冥想修炼", () => SuperMechMeditation.TickCultivation());
                SafeRun("气力锻炼", () => SuperMechQiRefine.TickCultivation());
                SafeRun("自定义属性同步", () => SuperMechCustomStats.TickSync());
            }
            else if (group == 1)
            {
                SafeRun("自动觉醒", () => SuperMechAdvancement.TickAutoAwakening());
                SafeRun("阶位晋升", () => SuperMechAdvancement.TickPromotions());
                SafeRun("潜能", () => SuperMechPotential.TickPotential());
                SafeRun("职业子等级", () => SuperMechSubClass.TickSubLevels());
                SafeRun("觉醒经验", () => SuperMechAwakened.TickXp());
                SafeRun("自动玩法", () => SuperMechAwakened.TickAutoPlay());
                SafeRun("进阶任务", () => SuperMechStageTask.TickTasks());
                SafeRun("传承", () => SuperMechHeritage.TickHeritage());
                SafeRun("知识协同", () => SuperMechKnowledgeSynergy.TickSynergy());
                SafeRun("机械融合", () => SuperMechMechFusion.TickFusion());
                SafeRun("技能自动学习", () => SuperMechSkills.TickAutoLearnAll());
                SafeRun("技能运行时", () => SuperMechSkillRuntime.Tick());
                SafeRun("降临者濒死保护", () => SuperMechConceptImmortal.TickGracePeriod());
                SafeRun("超能者竞争", () => SuperMechAI.TickCompetition());
                SafeRun("NPC自动修炼", () => SuperMechAI.TickNpcKnowledge());
                SafeRun("自动访问圣所", () => SuperMechAI.TickVisitSanctuary());
                SafeRun("机械军团AI", () => SuperMechAI.TickMechLegion());
                SafeRun("专属专长", () => SuperMechExclusiveTrait.TickGrant());
            }
            else if (group == 2)
            {
                SafeRun("直觉", () => SuperMechIntuition.TickIntuition());
                SafeRun("神性洞察", () => SuperMechDivinity.TickNativeInsight());
                SafeRun("传承感知", () => SuperMechTranscendence.TickLegacySense());
                SafeRun("神化任务", () => SuperMechTranscendence.TickAdvancementTask());
                SafeRun("自动神化尝试", () => SuperMechTranscendence.TickAutoAttempt());
                SafeRun("信息状态", () => SuperMechInfoState.TickInfoState());
                SafeRun("信息态记录", () => SuperMechInformationState.Tick(1.25f));
                SafeRun("圣所能量恢复", () => SuperMechSanctuary.RegenerateEnergy(1.25f));
            }
            else
            {
                SafeRun("法师塔", () => SuperMechMageTower.TickMageTowers());
                SafeRun("遗物掉落", () => SuperMechRelic.TickRelicDrops());
                SafeRun("自动收藏", () => SuperMechFavorite.TickAutoFavorite());
                SafeRun("光环", () => SuperMechAura.TickAura());
                SafeRun("维度增益", () => SuperMechDimension.TickDimensionBuffs());
                SafeRun("虚拟创世", () => SuperMechVirtualGenesis.Tick());
                SafeRun("内空间脉冲", () => SuperMechInnerSpace.TickPulse());
                SafeRun("内空间负面效果", () => SuperMechInnerSpace.TickDomainEffects());
                SafeRun("势力系统", () => SuperMechFaction.TickFactions());
                SafeRun("文明系统", () => SuperMechCivilization.TickCivilizations());
                SafeRun("玩家降临", () => SuperMechPlayer.TickPlayer(1.25f));
                SafeRun("世界树入侵", () => SuperMechWorldTree.TickWorldTree(1.25f));
                SafeRun("个体伟力", () => SuperMechSupermA.Tick(1.25f));
                SafeRun("公司发行", () => SuperMechCompany.TickIssue());
                SafeRun("伊纳尔俸禄", () => SuperMechInal.TickSalary());
            }

            float now = Time.time;
            if (now - _lastSaveTime >= 60f)
            {
                _lastSaveTime = now;
                SafeRun("文明统计更新", () => SuperMechCosmicIteration.TickUpdate());
                SafeRun("自动存档", () => SuperMechSaveData.Save());
                SafeRun("死单位清理", () => CleanupDeadActors());
            }
        }

        private static void CleanupDeadActors()
        {
            if (World.world == null || World.world.units == null) return;
            var alive = new System.Collections.Generic.HashSet<long>();
            foreach (var a in World.world.units) if (a != null) alive.Add(a.id);

            int removed = 0;
            removed += SuperMechQi.CleanupDead(alive);
            removed += SuperMechStage.CleanupDead(alive);
            removed += SuperMechPotential.CleanupDead(alive);
            removed += SuperMechKnowledge.CleanupDead(alive);
            removed += SuperMechKnowledgeSynergy.CleanupDead(alive);
            removed += SuperMechEquipAffix.CleanupDead(alive);
            removed += SuperMechEquipBreak.CleanupDead(alive);
            removed += SuperMechMechFusion.CleanupDead(alive);
            removed += SuperMechKnowledgeFusion.CleanupDead(alive);
            removed += SuperMechDivinity.CleanupDead(alive);
            removed += SuperMechAdvancement.CleanupDead(alive);
            removed += SuperMechAwakened.CleanupDead(alive);
            removed += SuperMechHeritage.CleanupDead(alive);
            removed += SuperMechCorePower.CleanupDead(alive);
            removed += SuperMechInfoState.CleanupDead(alive);
            removed += SuperMechEquipBag.CleanupDead(alive);
            removed += SuperMechEquipModify.CleanupDead(alive);
            removed += SuperMechAura.CleanupDead(alive);
            removed += SuperMechInnerSpace.CleanupDead(alive);
            removed += SuperMechCrafting.CleanupDead(alive);
            removed += SuperMechQiRefine.CleanupDead(alive);
            removed += SuperMechSubClass.CleanupDead(alive);
            removed += SuperMechLegend.CleanupDead(alive);
            removed += SuperMechIntuition.CleanupDead(alive);
            removed += SuperMechTranscendence.CleanupDead(alive);
            removed += SuperMechDimension.CleanupDead(alive);
            removed += SuperMechVirtualGenesis.CleanupDead(alive);
            removed += SuperMechMageTower.CleanupDead(alive);
            removed += SuperMechRelic.CleanupDead(alive);
            removed += SuperMechCosmicRelic.CleanupDead(alive);
            removed += SuperMechSanctuary.CleanupDead(alive);
            removed += SuperMechSkills.CleanupDead(alive);
            removed += SuperMechSkillRuntime.CleanupDead(alive);
            removed += SuperMechPotentialRating.CleanupDead(alive);
            removed += SuperMechQiAttribute.CleanupDead(alive);
            removed += SuperMechStageTask.CleanupDead(alive);
            removed += SuperMechAI.CleanupDead(alive);
            removed += SuperMechFaction.CleanupDead(alive);
            removed += SuperMechPlayer.CleanupDead(alive);
            removed += SuperMechActorContextRegistry.CleanupDead(alive);
        }

        public static void ClearAll()
        {
            try { SuperMechQi.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理Qi数据失败"); }
            try { SuperMechStage.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理Stage数据失败"); }
            try { SuperMechPotential.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechPotential"); }
            try { SuperMechKnowledge.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechKnowledge"); }
            try { SuperMechKnowledgeSynergy.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechKnowledgeSynergy"); }
            try { SuperMechEquipAffix.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechEquipAffix"); }
            try { SuperMechEquipBreak.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechEquipBreak"); }
            try { SuperMechMechFusion.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechMechFusion"); }
            try { SuperMechKnowledgeFusion.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechKnowledgeFusion"); }
            try { SuperMechDivinity.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechDivinity"); }
            try { SuperMechTranscendence.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechTranscendence"); }
            try { SuperMechInfoState.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechInfoState"); }
            try { SuperMechAwakened.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAwakened"); }
            try { SuperMechStageTask.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechStageTask"); }
            try { SuperMechHeritage.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechHeritage"); }
            try { SuperMechIntuition.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechIntuition"); }
            try { SuperMechQiAttribute.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechQiAttribute"); }
            try { SuperMechCorePower.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechCorePower"); }
            try { SuperMechSubClass.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechSubClass"); }
            try { SuperMechLegend.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechLegend"); }
            try { SuperMechSanctuary.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechSanctuary"); }
            try { SuperMechMageTower.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechMageTower"); }
            try { SuperMechRelic.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechRelic"); }
            try { SuperMechCosmicRelic.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechCosmicRelic"); }
            try { SuperMechEquipBag.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechEquipBag"); }
            try { SuperMechEquipModify.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechEquipModify"); }
            try { SuperMechRace.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechRace"); }
            try { SuperMechAdvancement.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAdvancement"); }
            try { SuperMechAura.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAura"); }
            try { SuperMechCrafting.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechCrafting"); }
            try { SuperMechPotentialRating.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechPotentialRating"); }
            try { SuperMechWindowFrameInner.ClearAll(); } catch { Debug.LogWarning("[超神机械师] 清理WindowFrame失败"); }
            try { SuperMechQiRefine.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理精炼数据失败"); }
            try { SuperMechWindowManager.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理窗口缓存失败"); }
            try { SuperMechEventLogger.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理日志资源失败"); }
            try { SuperMechRankView.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理排行榜缓存失败"); }
            try { SuperMechCleanup.ClearUiStaticState(); } catch { Debug.LogWarning("[超神机械师] 清理UI静态状态失败"); }
            try { SuperMechStatsIcon.ClearStaticState(); } catch { Debug.LogWarning("[超神机械师] 清理StatsIcon失败"); }
            try { SuperMechProfession.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechProfession"); }
            try { SuperMechTalent.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechTalent"); }
            try { SuperMechAI.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAI"); }
            try { SuperMechDimension.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechDimension"); }
            try { SuperMechVirtualGenesis.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechVirtualGenesis"); }
            try { SuperMechFaction.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechFaction"); }
            try { SuperMechPlayer.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechPlayer"); }
            try { SuperMechWorldTree.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechWorldTree"); }
            try { SuperMechSupermA.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechSupermA"); }
            try { SuperMechInal.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechInal"); }
            try { SuperMechEventBus.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理事件总线失败"); }
        }
    }
}
