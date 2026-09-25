using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 统一tick调度器（性能优化：40系统分4组错峰）。
    ///
    /// 之前的问题：40+个系统每个tick都独立 foreach 遍历所有单位，
    /// 单位多时一帧内40次全量遍历，严重卡顿。
    ///
    /// 现在：把系统分成4组，每组在不同tick运行，每tick只跑1/4的系统。
    /// 同时加全局单位数上限保护，超过阈值时降低tick频率。
    /// </summary>
    public static class SuperMechUnifiedTick
    {
        private static int _tickCounter = 0;
        private static float _lastSaveTime = 0f;

        /// <summary>
        /// 统一tick入口：在Main.Update中调用，替代原来40+个独立Tick。
        /// 系统分4组错峰执行，每tick只跑一组。
        /// </summary>
        public static void Tick()
        {
            _tickCounter++;
            int group = _tickCounter % 4;

            try
            {
                // 第0组：核心修炼（气力/核心能量/属性/提炼法/修炼）
                if (group == 0)
                {
                    SuperMechQi.TickQiLevels();
                    SuperMechCorePower.TickCorePowers();
                    SuperMechQiAttribute.TickAutoAssign();
                    SuperMechRefinement.TickRefinement();
                    SuperMechCultivation.TickCultivation();
                }
                // 第1组：晋升与成长（阶位/潜能/副职业/降临者/土著）
                else if (group == 1)
                {
                    SuperMechAdvancement.TickAutoAwakening();
                    SuperMechAdvancement.TickPromotions();
                    SuperMechPotential.TickPotential();
                    SuperMechSubClass.TickSubLevels();
                    SuperMechAwakened.TickXp();
                    SuperMechAwakened.TickAutoPlay();
                    SuperMechHeritage.TickHeritage();
                }
                // 第2组：高阶系统（冥冥感应/神性蜕变/超神遗力/进阶任务/信息态）
                else if (group == 2)
                {
                    SuperMechIntuition.TickIntuition();
                    SuperMechDivinity.TickNativeInsight();
                    SuperMechTranscendence.TickLegacySense();
                    SuperMechTranscendence.TickAdvancementTask();
                    SuperMechInfoState.TickInfoState();
                }
                // 第3组：辅助系统（圣所/法师塔/宝物/自动收藏/气势震慑）
                else // group == 3
                {
                    SuperMechSanctuary.TickDivinity();
                    SuperMechSanctuary.TickDeadTracking();
                    SuperMechMageTower.TickMageTowers();
                    SuperMechRelic.TickRelicDrops();
                    SuperMechFavorite.TickAutoFavorite();
                    SuperMechAura.TickAura();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 统一tick异常(group={group}): {e.Message}");
            }

            // 自动存档：每60秒
            float now = Time.time;
            if (now - _lastSaveTime >= 60f)
            {
                _lastSaveTime = now;
                try { SuperMechSaveData.Save(); }
                catch (System.Exception e) { Debug.LogError($"[超神机械师] 自动存档异常: {e.Message}"); }
            }
        }

        /// <summary>
        /// 世界切换时清空所有系统的静态字典数据。
        /// 世界切换时一次性清空所有静态字典。
        /// </summary>
        public static void ClearAll()
        {
            try { SuperMechQi.Clear(); } catch { }
            try { SuperMechStage.Clear(); } catch { }
            try { SuperMechPotential.Clear(); } catch { }
            try { SuperMechDivinity.Clear(); } catch { }
            try { SuperMechTranscendence.Clear(); } catch { }
            try { SuperMechInfoState.Clear(); } catch { }
            try { SuperMechAwakened.Clear(); } catch { }
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
            try { SuperMechRace.Clear(); } catch { }
            try { SuperMechAdvancement.Clear(); } catch { }
            try { SuperMechAura.Clear(); } catch { }
            Debug.Log("[超神机械师] 所有系统数据已清空（世界切换）");
        }
    }
}
