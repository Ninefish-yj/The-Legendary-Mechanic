using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.41 顶层超A·异神遗产（用户：异神是原著顶层超A个体，属暗面系/超脱层已升维——
    /// 不再作为特殊单位"降临"进玩家世界；玩家世界的超A自然演化，原著个体不空降）
    /// 原著依据：异神="异能之神"，顶层超A之一（ch712/713）；存在依赖三大文明默许（ch1002/1018）；
    /// 异神遗产=技术资料（ch1008，谁得到谁继承准宇宙级文明核心技术）。
    /// 机制：不再生成特殊"异神"单位——异神状态=玩家世界顶层超A（X阶）的格局状态；
    /// 顶层超A（X阶·超神级）被击杀→遗落异神遗产（最强文明科技+30、全体觉醒潜能+3，仅一次）。
    /// </summary>
    public static class SuperMechEsGod
    {
        public class EsGodSaveData
        {
            public bool heritageClaimed;         // 顶层超A遗产是否已掉落
        }

        private static readonly EsGodSaveData _data = new EsGodSaveData();
        public static EsGodSaveData Data => _data;

        // 数值（原著：巅峰超A级·异能之神——顶层超A个体，个体伟力巅峰）
        private const float TopSuperAMult = 2.5f;           // 顶层超A伤害倍率（原著：顶层超A碾压普通单位）
        private const int TechHeritageReward = 30;          // 遗产：最强文明科技+30（原著ch1008技术资料）
        private const int PotentialHeritageReward = 3;      // 遗产：全体觉醒潜能+3

        // ============ 顶层超A判定（X阶·超神级=rank≥13，超A=rank≥10） ============

        public static bool IsTopSuperA(Actor a)
        {
            return a != null && SuperMechAwakened.IsAwakened(a) && SuperMechAdvancement.GetExactRankIndex(a) >= 13;
        }

        /// <summary>当前存活顶层超A（X阶）个体数</summary>
        public static int CountTopSuperA()
        {
            if (World.world == null || World.world.units == null) return 0;
            int count = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (IsTopSuperA(a)) count++;
            }
            return count;
        }

        /// <summary>顶层超A伤害倍率：×2.5（原著：顶层超A·个体伟力巅峰，碾压普通单位）</summary>
        public static float GetTopSuperAMult(Actor a)
        {
            if (a == null || !IsTopSuperA(a)) return 1f;
            return TopSuperAMult;
        }

        /// <summary>顶层超A被击杀→遗落异神遗产（原著ch1008技术资料，仅一次，不复活）</summary>
        public static void OnTopSuperAKilled(Actor killer, Actor target)
        {
            if (target == null || !IsTopSuperA(target)) return;
            if (_data.heritageClaimed) return;

            _data.heritageClaimed = true;
            Kingdom top = FindStrongestKingdom();
            if (top != null) SuperMechCivilization.AddTechPoints(top, TechHeritageReward);

            if (World.world.units != null && World.world.units.units_only_alive != null)
            {
                foreach (var a in World.world.units.units_only_alive)
                {
                    if (a == null || !a.isAlive()) continue;
                    if (SuperMechAwakened.IsAwakened(a)) SuperMechPotential.AddPotential(a, PotentialHeritageReward);
                }
            }
            Debug.Log($"[超神机械师] 【异神遗产】顶层超A被击杀，遗落异神遗产：最强文明科技+{TechHeritageReward}、全体觉醒潜能+{PotentialHeritageReward}");
        }

        // ============ 工具 ============

        private static Kingdom FindStrongestKingdom()
        {
            try
            {
                if (World.world == null || World.world.kingdoms == null || World.world.kingdoms.list == null) return null;
                Kingdom best = null;
                int bestLevel = -1;
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.wild || !k.isCiv()) continue;
                    int lv = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
                    if (lv > bestLevel) { bestLevel = lv; best = k; }
                }
                return best;
            }
            catch { return null; }
        }

        // ============ 存档 ============

        public static EsGodSaveData Save() => _data;

        public static void Load(EsGodSaveData data)
        {
            if (data == null) return;
            _data.heritageClaimed = data.heritageClaimed;
        }

        public static void Clear()
        {
            _data.heritageClaimed = false;
        }
    }
}
