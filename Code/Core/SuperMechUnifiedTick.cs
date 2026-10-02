using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechUnifiedTick
    {
        private static int _tickCounter = 0;
        private static float _lastSaveTime = 0f;

        public static void Tick()
        {
            if (World.world == null) return;

            _tickCounter++;
            int group = _tickCounter % 4;

            try
            {
                if (group == 0)
                {
                    SuperMechQi.TickQiLevels();
                    SuperMechCorePower.TickCorePowers();
                    SuperMechQiAttribute.TickAutoAssign();
                    SuperMechRefinement.TickRefinement();
                    SuperMechRefinement.TickCultivation();
                    SuperMechCustomStats.TickSync();
                    SuperMechCrossMod.TickEnergySync();
                }
                else if (group == 1)
                {
                    SuperMechAdvancement.TickAutoAwakening();
                    SuperMechAdvancement.TickPromotions();
                    SuperMechPotential.TickPotential();
                    SuperMechSubClass.TickSubLevels();
                    SuperMechAwakened.TickXp();
                    SuperMechAwakened.TickAutoPlay();
                    SuperMechAdvancementTask.TickTasks();
                    SuperMechHeritage.TickHeritage();
                    SuperMechKnowledgeSynergy.TickSynergy();
                    SuperMechMechFusion.TickFusion();
                    SuperMechSkills.TickAutoLearnAll();
                }
                else if (group == 2)
                {
                    SuperMechIntuition.TickIntuition();
                    SuperMechDivinity.TickNativeInsight();
                    SuperMechTranscendence.TickLegacySense();
                    SuperMechTranscendence.TickAdvancementTask();
                    SuperMechTranscendence.TickAutoAttempt();
                    SuperMechInfoState.TickInfoState();
                }
                else
                {
                    SuperMechMageTower.TickMageTowers();
                    SuperMechRelic.TickRelicDrops();
                    SuperMechFavorite.TickAutoFavorite();
                    SuperMechAura.TickAura();
                    SuperMechDimension.TickDimensionBuffs();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 统一tick异常(group={group}): {e.Message}\n{e.StackTrace}");
            }

            float now = Time.time;
            if (now - _lastSaveTime >= 60f)
            {
                _lastSaveTime = now;
                try { SuperMechSaveData.Save(); }
                catch (System.Exception e) { Debug.LogError($"[超神机械师] 自动存档异常: {e.Message}"); }
                try { CleanupDeadActors(); }
                catch (System.Exception e) { Debug.LogError($"[超神机械师] 死单位清理异常: {e.Message}"); }
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
            removed += SuperMechAura.CleanupDead(alive);
            removed += SuperMechCrafting.CleanupDead(alive);
            removed += SuperMechRefinement.CleanupDead(alive);
            removed += SuperMechSubClass.CleanupDead(alive);
            removed += SuperMechLegend.CleanupDead(alive);
            removed += SuperMechIntuition.CleanupDead(alive);
            removed += SuperMechTranscendence.CleanupDead(alive);
            removed += SuperMechDimension.CleanupDead(alive);
            removed += SuperMechMageTower.CleanupDead(alive);
            removed += SuperMechRelic.CleanupDead(alive);
            removed += SuperMechCosmicRelic.CleanupDead(alive);
            removed += SuperMechSanctuary.CleanupDead(alive);
            removed += SuperMechSkills.CleanupDead(alive);
            removed += SuperMechPotentialRating.CleanupDead(alive);
            removed += SuperMechQiAttribute.CleanupDead(alive);
            removed += SuperMechAdvancementTask.CleanupDead(alive);
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
            try { SuperMechAdvancementTask.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAdvancementTask"); }
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
            try { SuperMechRace.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechRace"); }
            try { SuperMechAdvancement.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAdvancement"); }
            try { SuperMechAura.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechAura"); }
            try { SuperMechCrafting.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechCrafting"); }
            try { SuperMechPotentialRating.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechPotentialRating"); }
            try { SMWindowFrame.ClearAll(); } catch { Debug.LogWarning("[超神机械师] 清理WindowFrame失败"); }
            try { SuperMechAwakenWindow.ClearStaticState(); } catch { Debug.LogWarning("[超神机械师] 清理AwakenWindow失败"); }
            try { SuperMechStatsIcon.ClearStaticState(); } catch { Debug.LogWarning("[超神机械师] 清理StatsIcon失败"); }
            try { SuperMechProfession.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechProfession"); }
            try { SuperMechTalent.Clear(); } catch { Debug.LogWarning("[超神机械师] 清理数据失败: SuperMechTalent"); }
        }
    }
}
