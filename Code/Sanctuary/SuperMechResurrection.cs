using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所复活系统（原著设定）。
    /// 原著核心限制：
    /// 1. 只有超A级（S阶）及以上才能通过圣所复苏
    /// 2. 人格悖论：复苏的到底是不是原来的个体，一直是争论主题
    /// 3. 随机失去能力：生前越强复苏越完整，复苏次数过多信息丢失越严重
    /// 4. 唯一存在性：宇宙里只能同时存在一个你，未死亡时复苏无效
    /// 5. 无法跨越迭代：仅能复苏同一迭代的个体
    /// 6. 媒介消耗：消耗含有该超A生物信息的媒介（圣所能量）
    /// </summary>
    public static class SuperMechResurrection
    {
        /// <summary>最低复活阶位：S阶（超A级）及以上</summary>
        public const int MinRankForResurrect = 10;

        /// <summary>基础复活消耗的圣所能量</summary>
        public const float BaseResurrectEnergyCost = 1000f;

        /// <summary>每次复活后信息完整度衰减系数</summary>
        public const float InformationDecayPerRevive = 0.15f;

        /// <summary>信息完整度下限（低于此值无法复活）</summary>
        public const float MinInformationIntegrity = 0.2f;

        /// <summary>
        /// 计算信息态完整度（0~1）。
        /// 生前越强（阶位越高）基础完整度越高，复苏次数越多完整度越低。
        /// 原著：生前越强复苏越完整，复苏次数过多信息丢失越严重。
        /// </summary>
        public static float CalculateInformationIntegrity(SuperMechInformationState.InformationStateRecord state)
        {
            if (state == null) return 0f;
            // 基础完整度：阶位越高越完整（S阶=0.7, X阶=1.0）
            float baseIntegrity = Mathf.Clamp01(0.5f + state.rankIndex * 0.04f);
            // 复苏次数衰减
            float decay = Mathf.Pow(1f - InformationDecayPerRevive, state.reviveCount);
            return Mathf.Clamp01(baseIntegrity * decay);
        }

        /// <summary>检查是否可以复活该信息态（原著限制）</summary>
        public static bool CanResurrect(SuperMechInformationState.InformationStateRecord state)
        {
            if (state == null) return false;
            // 1. 必须超A级（S阶）及以上
            if (state.rankIndex < MinRankForResurrect) return false;
            // 2. 信息完整度必须高于下限
            float integrity = CalculateInformationIntegrity(state);
            if (integrity < MinInformationIntegrity) return false;
            // 3. 唯一存在性：检查是否已有同名同种族单位存活
            if (IsAlreadyAlive(state)) return false;
            // 4. 同一迭代限制（当前迭代ID匹配）
            if (state.iterationId != SuperMechCosmicIteration.CurrentIteration) return false;
            // 5. 圣所能量足够
            float cost = GetResurrectCost(state);
            if (SuperMechSanctuary.Data.sanctuary_energy < cost) return false;
            if (World.world == null || World.world.units == null) return false;
            return true;
        }

        /// <summary>检查是否已有同名同种族单位存活（唯一存在性）</summary>
        private static bool IsAlreadyAlive(SuperMechInformationState.InformationStateRecord state)
        {
            if (state == null || World.world == null || World.world.units == null) return false;
            var units = World.world.units.units_only_alive;
            if (units == null) return false;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (a.name == state.name && a.asset?.id == state.species)
                    return true;
            }
            return false;
        }

        /// <summary>计算复活消耗的圣所能量（媒介消耗）</summary>
        public static float GetResurrectCost(SuperMechInformationState.InformationStateRecord state)
        {
            if (state == null) return BaseResurrectEnergyCost;
            // 阶位越高消耗越大，复苏次数越多消耗越大
            float rankMultiplier = 1f + state.rankIndex * 0.2f;
            float reviveMultiplier = 1f + state.reviveCount * 0.5f;
            return BaseResurrectEnergyCost * rankMultiplier * reviveMultiplier;
        }

        /// <summary>执行复活，返回复活的Actor，失败返回null</summary>
        public static Actor Resurrect(SuperMechInformationState.InformationStateRecord state)
        {
            if (!CanResurrect(state)) return null;

            // 计算信息完整度
            float integrity = CalculateInformationIntegrity(state);

            // 消耗圣所能量（媒介）
            float cost = GetResurrectCost(state);
            SuperMechSanctuary.Data.sanctuary_energy -= cost;
            SuperMechSanctuary.Data.total_resurrections++;
            SuperMechSanctuary.Save();

            // 在地图中心附近找一个空位生成
            WorldTile tile = FindResurrectTile();
            if (tile == null)
            {
                Debug.LogError("[超神机械师] 复活失败：找不到可用地块");
                return null;
            }

            // 创建新单位（用原种族）
            string speciesId = string.IsNullOrEmpty(state.species) ? "human" : state.species;
            Actor newActor = World.world.units.spawnNewUnit(speciesId, tile, false, true, 6f, null, false, true);
            if (newActor == null)
            {
                Debug.LogError("[超神机械师] 复活失败：单位创建失败");
                return null;
            }

            // 恢复名字
            if (!string.IsNullOrEmpty(state.name))
            {
                newActor.name = state.name;
            }

            // 恢复职业
            RestoreClass(newActor, state.classTrait);

            // 按信息完整度恢复阶位和气力（原著：随机失去能力，完整度决定保留比例）
            int restoredRank = Mathf.RoundToInt(state.rankIndex * integrity);
            SuperMechAdvancement.SetExactRank(newActor, Mathf.Max(0, restoredRank));
            SuperMechQi.SetQi(newActor, state.qi * integrity);

            // 恢复潜能（按完整度）
            SuperMechPotential.SetPotential(newActor, Mathf.Max(1, Mathf.RoundToInt(state.potential * integrity)));

            // 随机恢复知识（按完整度，随机选择保留哪些）
            if (state.knowledge != null && state.knowledge.Count > 0)
            {
                int restoreCount = Mathf.RoundToInt(state.knowledge.Count * integrity);
                var shuffled = new List<string>(state.knowledge);
                ShuffleList(shuffled);
                for (int i = 0; i < restoreCount && i < shuffled.Count; i++)
                {
                    SuperMechKnowledge.Unlock(newActor, shuffled[i]);
                }
            }

            // 记录复活次数
            SuperMechSanctuary.SetReviveCount(newActor, state.reviveCount + 1);

            // 从信息态列表中移除（已复活）
            SuperMechInformationState.RemoveDeadState(state.actorId);

            Debug.Log($"[超神机械师] 复活成功: {state.name} (信息完整度{integrity:F0%}, 原阶位{state.rankIndex}→{restoredRank}, 消耗能量{cost:F0})");

            // v0.29.0 UI重构：推送复活事件到原生事件日志
            int reviveCount = state.reviveCount + 1;
            string tPartial = LocalizedTextManager.getText("sm_res_partial_lost");
            string tFull = LocalizedTextManager.getText("sm_res_full_keep");
            string lostAbilities = integrity < 1f ? string.Format(tPartial, integrity * 100) : tFull;
            SMEventLogger.LogResurrection(newActor, reviveCount, lostAbilities);

            return newActor;
        }

        /// <summary>列表随机打乱（Fisher-Yates）</summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>恢复职业特质</summary>
        private static void RestoreClass(Actor a, string classTrait)
        {
            if (a == null || string.IsNullOrEmpty(classTrait)) return;
            switch (classTrait)
            {
                case "mech":
                    a.addTrait(SuperMechTraits.ClassMech);
                    break;
                case "martial":
                    a.addTrait(SuperMechTraits.ClassMartial);
                    break;
                case "psi":
                    a.addTrait(SuperMechTraits.ClassPsi);
                    break;
                case "mage":
                    a.addTrait(SuperMechTraits.ClassMage);
                    break;
                case "mind":
                    a.addTrait(SuperMechTraits.ClassMind);
                    break;
            }
        }

        /// <summary>找一个可用的复活地块（地图中心附近）</summary>
        private static WorldTile FindResurrectTile()
        {
            if (World.world == null) return null;
            int cx = MapBox.width / 2;
            int cy = MapBox.height / 2;

            for (int r = 0; r < 50; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int x = cx + dx;
                        int y = cy + dy;
                        if (x < 0 || y < 0 || x >= MapBox.width || y >= MapBox.height) continue;
                        WorldTile tile = World.world.GetTile(x, y);
                        if (tile != null && tile.Type != TileLibrary.deep_ocean && tile.Type != TileLibrary.close_ocean && tile.Type != TileLibrary.mountains)
                        {
                            return tile;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>获取可复活的信息态列表</summary>
        public static List<SuperMechInformationState.InformationStateRecord> GetResurrectableStates()
        {
            var result = new List<SuperMechInformationState.InformationStateRecord>();
            foreach (var state in SuperMechInformationState.GetDeadStates())
            {
                if (CanResurrect(state)) result.Add(state);
            }
            return result;
        }
    }
}
