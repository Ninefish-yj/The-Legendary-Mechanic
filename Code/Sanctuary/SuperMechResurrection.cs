using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所复活系统：消耗圣所能量复活死亡单位
    /// 限制：超A级（S阶以上）才能复活，最多复活3次，消耗钥匙碎片
    /// </summary>
    public static class SuperMechResurrection
    {
        public const int MinRankForResurrect = 10; // S阶（index=10）以上才能复活
        public const int MaxResurrectCount = 3;   // 最多复活3次
        public const int ResurrectCost = 5;       // 每次复活消耗5个钥匙碎片

        /// <summary>检查是否可以复活该信息态</summary>
        public static bool CanResurrect(SuperMechInformationState.InformationStateRecord state)
        {
            if (state == null) return false;
            if (state.rankIndex < MinRankForResurrect) return false; // 必须S阶以上
            if (state.reviveCount >= MaxResurrectCount) return false; // 复活次数上限
            if (SuperMechSanctuary.Data.key_fragments < ResurrectCost) return false; // 钥匙碎片不足
            if (World.world == null || World.world.units == null) return false;
            // 检查是否已存活（同名同种族单位可能已存在）
            return true;
        }

        /// <summary>执行复活，返回复活的Actor，失败返回null</summary>
        public static Actor Resurrect(SuperMechInformationState.InformationStateRecord state)
        {
            if (!CanResurrect(state)) return null;

            // 消耗钥匙碎片
            SuperMechSanctuary.Data.key_fragments -= ResurrectCost;
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

            // 恢复阶位和气力
            SuperMechAdvancement.SetExactRank(newActor, Mathf.Max(0, state.rankIndex - 2)); // 复活后降2阶
            SuperMechQi.SetQi(newActor, state.qi * 0.5f); // 复活后气力减半

            // 恢复潜能
            SuperMechPotential.SetPotential(newActor, Mathf.Max(1, state.potential / 2));

            // 恢复部分知识（50%）
            if (state.knowledge != null)
            {
                int restoreCount = Mathf.CeilToInt(state.knowledge.Count * 0.5f);
                for (int i = 0; i < restoreCount && i < state.knowledge.Count; i++)
                {
                    SuperMechKnowledge.Unlock(newActor, state.knowledge[i]);
                }
            }

            // 记录复活次数
            SuperMechSanctuary.SetReviveCount(newActor, state.reviveCount + 1);

            // 从信息态列表中移除
            SuperMechInformationState.RemoveDeadState(state.actorId);

            Debug.Log($"[超神机械师] 复活成功: {state.name} (原阶位{state.rankIndex}→{Mathf.Max(0, state.rankIndex - 2)}, 气力{state.qi * 0.5f:F0})");
            return newActor;
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

            // 从中心向外搜索
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
