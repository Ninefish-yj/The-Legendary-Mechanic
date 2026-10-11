using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 建筑升级系统
    /// 建筑随文明发展自动升级，升级后范围扩大+效果增强
    /// 等级存储在建筑data中，不依赖WB原生upgrade_to
    /// </summary>
    public static class SuperMechBuildingUpgrade
    {
        private const string LevelKey = "sm_building_level";

        /// <summary>获取建筑等级（1-3）</summary>
        public static int GetLevel(Building building)
        {
            if (building == null || building.data == null) return 1;
            building.data.get(LevelKey, out int level, 1);
            return Mathf.Clamp(level, 1, 3);
        }

        /// <summary>设置建筑等级</summary>
        public static void SetLevel(Building building, int level)
        {
            if (building == null || building.data == null) return;
            building.data.set(LevelKey, Mathf.Clamp(level, 1, 3));
        }

        /// <summary>获取建筑效果范围（格）</summary>
        public static float GetRange(Building building)
        {
            int level = GetLevel(building);
            return level switch
            {
                1 => 30f,
                2 => 40f,
                3 => 50f,
                _ => 30f
            };
        }

        /// <summary>获取建筑效果倍率</summary>
        public static float GetMultiplier(Building building)
        {
            int level = GetLevel(building);
            return level switch
            {
                1 => 1f,
                2 => 1.5f,
                3 => 2f,
                _ => 1f
            };
        }

        /// <summary>Tick：检查所有自定义建筑是否满足升级条件</summary>
        public static void Tick()
        {
            if (World.world?.buildings == null) return;
            if (Time.frameCount % 512 != 0) return; // 每512tick检查一次

            foreach (var building in World.world.buildings)
            {
                if (building == null || building.asset == null || building.isRuin()) continue;
                if (!IsCustomBuilding(building.asset.id)) continue;

                int currentLevel = GetLevel(building);
                if (currentLevel >= 3) continue;

                if (CheckUpgradeCondition(building, currentLevel + 1))
                {
                    SetLevel(building, currentLevel + 1);
                    Debug.Log($"[超神机械师] 建筑{building.asset.id}升级到{currentLevel + 1}级");
                }
            }
        }

        /// <summary>检查是否满足升级条件</summary>
        private static bool CheckUpgradeCondition(Building building, int targetLevel)
        {
            float existenceTime = building.getExistenceTime();
            // 至少存在一定时间才能升级
            if (targetLevel == 2 && existenceTime < 120f) return false;
            if (targetLevel == 3 && existenceTime < 300f) return false;

            switch (building.asset.id)
            {
                case SuperMechBuilding.MechFactory:
                    return CountNearbyUnits(building, a => a.hasTrait(SuperMechTraits.ClassMech))
                           >= (targetLevel == 2 ? 5 : 10);

                case SuperMechBuilding.MercGuild:
                    return CountNearbyUnits(building, SuperMechAwakened.IsAwakened)
                           >= (targetLevel == 2 ? 20 : 40);

                case SuperMechBuilding.StarBank:
                    return CountNearbyUnits(building, a => SuperMechAdvancement.GetExactRankIndex(a) >= 8)
                           >= (targetLevel == 2 ? 3 : 5);

                case SuperMechBuilding.BlackStarPalace:
                    return CountNearbyUnits(building, a => a.hasTrait("sm_player_hanxiao")
                           && SuperMechAdvancement.GetExactRankIndex(a) >= (targetLevel == 2 ? 10 : 13)) > 0;

                case SuperMechBuilding.CompanyHQ:
                    return CountNearbyUnits(building, a => a.hasTrait("sm_player") || a.hasTrait("sm_player_hanxiao"))
                           >= (targetLevel == 2 ? 5 : 10);

                default:
                    return false;
            }
        }

        /// <summary>统计建筑周围满足条件的单位数量</summary>
        private static int CountNearbyUnits(Building building, System.Predicate<Actor> predicate)
        {
            int count = 0;
            float range = GetRange(building);
            foreach (var a in World.world.units)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(a.current_position, building.current_position);
                if (dist < range && predicate(a))
                    count++;
            }
            return count;
        }

        /// <summary>判断是否是模组自定义建筑（v0.95.0 移至SuperMechBuilding）</summary>
        private static bool IsCustomBuilding(string id)
        {
            return SuperMechBuilding.IsCustomBuilding(id);
        }
    }
}
