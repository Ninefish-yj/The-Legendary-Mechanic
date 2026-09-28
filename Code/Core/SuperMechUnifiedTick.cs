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
                    SuperMechSanctuary.TickDivinity();
                    SuperMechSanctuary.TickDeadTracking();
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
            if (removed > 0 && SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] 清理{removed}条死亡单位数据");
        }

        public static void ClearAll()
        {
            try { SuperMechQi.Clear(); } catch { }
            try { SuperMechStage.Clear(); } catch { }
            try { SuperMechPotential.Clear(); } catch { }
            try { SuperMechKnowledge.Clear(); } catch { }
            try { SuperMechKnowledgeSynergy.Clear(); } catch { }
            try { SuperMechEquipAffix.Clear(); } catch { }
            try { SuperMechEquipBreak.Clear(); } catch { }
            try { SuperMechMechFusion.Clear(); } catch { }
            try { SuperMechKnowledgeFusion.Clear(); } catch { }
            try { SuperMechDivinity.Clear(); } catch { }
            try { SuperMechTranscendence.Clear(); } catch { }
            try { SuperMechInfoState.Clear(); } catch { }
            try { SuperMechAwakened.Clear(); } catch { }
            try { SuperMechAdvancementTask.Clear(); } catch { }
            try { SuperMechHeritage.Clear(); } catch { }
            try { SuperMechIntuition.Clear(); } catch { }
            try { SuperMechQiAttribute.Clear(); } catch { }
            try { SuperMechCorePower.Clear(); } catch { }
            try { SuperMechSubClass.Clear(); } catch { }
            try { SuperMechLegend.Clear(); } catch { }
            try { SuperMechSanctuary.Clear(); } catch { }
            try { SuperMechMageTower.Clear(); } catch { }
            try { SuperMechRelic.Clear(); } catch { }
            try { SuperMechCosmicRelic.Clear(); } catch { }
            try { SuperMechEquipBag.Clear(); } catch { }
            try { SuperMechRace.Clear(); } catch { }
            try { SuperMechAdvancement.Clear(); } catch { }
            try { SuperMechAura.Clear(); } catch { }
            try { SuperMechCrafting.Clear(); } catch { }
            try { SuperMechPotentialRating.Clear(); } catch { }
            try { SMWindowFrame.ClearAll(); } catch { }
            try { SuperMechCustomTabs.ClearStaticState(); } catch { }
            try { SuperMechAwakenWindow.ClearStaticState(); } catch { }
            try { SuperMechStatsIcon.ClearStaticState(); } catch { }
            try { SuperMechProfession.Clear(); } catch { }
            try { SuperMechTalent.Clear(); } catch { }
            try { SuperMechReflection.ClearCache(); } catch { }
            Debug.Log("[超神机械师] 所有系统数据已清空（世界切换）");
        }
    }
}
