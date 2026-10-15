using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义生物生成事件系统
    /// 对照原著：星际生物稀有出现、异化灾害爆发、世界树入侵
    /// v0.175.0: 已切换到自定义ActorAsset（sm_void_dragon/sm_star_beast/sm_infected/sm_worldtree_soldier）
    /// </summary>
    public static class SuperMechCreatureSpawner
    {
        private static float _nextVoidDragonCheck;
        private static float _nextStarBeastCheck;
        private static readonly HashSet<long> _activeCreatures = new HashSet<long>();

        // 生成配置（对照原著出现频率）
        private const float VoidDragonCheckInterval = 300f;   // 每5分钟检查一次
        private const float VoidDragonSpawnChance = 0.15f;    // 15%概率生成
        private const int VoidDragonMaxAlive = 1;             // 同时最多1只

        private const float StarBeastCheckInterval = 240f;    // 每4分钟检查一次
        private const float StarBeastSpawnChance = 0.25f;     // 25%概率生成
        private const int StarBeastMaxAlive = 3;              // 同时最多3只

        /// <summary>Tick：检查稀有生物生成
        /// v0.100.0 科技→单位互动：星际生物需要航天科技才能发现</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.units == null) return;

            CleanupDead();

            // 检查是否有文明研究了足够的航天科技
            bool canDiscoverStarBeast = HasAnyKingdomResearched("space_t3"); // 亚光速飞船
            bool canDiscoverVoidDragon = HasAnyKingdomResearched("space_t4"); // 曲速引擎

            // 虚空巨龙（需要曲速引擎才能发现）
            if (canDiscoverVoidDragon && Time.time >= _nextVoidDragonCheck)
            {
                _nextVoidDragonCheck = Time.time + VoidDragonCheckInterval;
                if (CountCreatureType("sm_void_dragon_marker") < VoidDragonMaxAlive && Random.value < VoidDragonSpawnChance)
                {
                    SpawnVoidDragon();
                }
            }

            // 星游兽（需要亚光速飞船才能发现）
            if (canDiscoverStarBeast && Time.time >= _nextStarBeastCheck)
            {
                _nextStarBeastCheck = Time.time + StarBeastCheckInterval;
                if (CountCreatureType("sm_star_beast_marker") < StarBeastMaxAlive && Random.value < StarBeastSpawnChance)
                {
                    SpawnStarBeast();
                }
            }
        }

        /// <summary>检查是否有任意文明研究了指定科技</summary>
        private static bool HasAnyKingdomResearched(string techId)
        {
            if (World.world?.kingdoms?.list == null) return false;
            foreach (var k in World.world.kingdoms.list)
            {
                if (k != null && SuperMechTech.IsResearched(k.getID(), techId))
                    return true;
            }
            return false;
        }

        /// <summary>生成虚空巨龙（原著：星际生物，身躯成千上万米）</summary>
        public static Actor SpawnVoidDragon()
        {
            WorldTile tile = SMWorldUtils.FindEdgeTile();
            if (tile == null) return null;

            // v0.175.0: 切换到自定义ActorAsset sm_void_dragon（克隆dragon+强化属性）
            Actor dragon = World.world.units.createNewUnit("sm_void_dragon", tile, pMiracleSpawn: true, pAdultAge: true);
            if (dragon == null) return null;

            dragon.addTrait("sm_void_dragon_marker"); // 保留marker用于统计和AI识别
            dragon.data.name = LocalizedTextManager.getText("sm_creature_void_dragon");
            _activeCreatures.Add(dragon.id);

            Debug.Log("[超神机械师] 虚空巨龙降临！原著：身躯成千上万米，暗能量如同空气");
            return dragon;
        }

        /// <summary>生成星游兽（原著：能肉身穿行宇宙的星际生物）</summary>
        public static Actor SpawnStarBeast()
        {
            WorldTile tile = SMWorldUtils.FindEdgeTile();
            if (tile == null) return null;

            // v0.175.0: 切换到自定义ActorAsset sm_star_beast（克隆orc+强化属性）
            Actor beast = World.world.units.createNewUnit("sm_star_beast", tile, pMiracleSpawn: true, pAdultAge: true);
            if (beast == null) return null;

            beast.addTrait("sm_star_beast_marker"); // 保留marker用于统计和AI识别
            beast.data.name = LocalizedTextManager.getText("sm_creature_star_beast");
            _activeCreatures.Add(beast.id);

            Debug.Log("[超神机械师] 星游兽出现！原著：肉身穿行宇宙，别指望能轻松伤到它");
            return beast;
        }

        /// <summary>生成异化怪物（原著ch513：异化原体影响微生物，生物异化暴走）</summary>
        public static List<Actor> SpawnInfectedHorde(int count = 5)
        {
            var result = new List<Actor>();
            for (int i = 0; i < count; i++)
            {
                WorldTile tile = SMWorldUtils.FindEdgeTile();
                if (tile == null) continue;

                // v0.175.0: 切换到自定义ActorAsset sm_infected（克隆zombie+强化属性）
                Actor infected = World.world.units.createNewUnit("sm_infected", tile, pMiracleSpawn: true, pAdultAge: true);
                if (infected == null) continue;

                infected.addTrait("sm_disaster_infected"); // ActorAsset已包含，保留确保兼容
                infected.data.name = LocalizedTextManager.getText("sm_creature_infected");
                _activeCreatures.Add(infected.id);
                result.Add(infected);
            }

            if (result.Count > 0)
                Debug.Log($"[超神机械师] 异化爆发！生成{result.Count}只异化怪物（原著ch513）");
            return result;
        }

        /// <summary>生成世界树战士（原著：世界树文明基础单位）</summary>
        public static Actor SpawnWorldTreeSoldier(WorldTile tile = null)
        {
            if (tile == null) tile = SMWorldUtils.FindEdgeTile();
            if (tile == null) return null;

            // v0.175.0: 切换到自定义ActorAsset sm_worldtree_soldier（克隆human+强化属性）
            Actor soldier = World.world.units.createNewUnit("sm_worldtree_soldier", tile, pMiracleSpawn: true, pAdultAge: true);
            if (soldier == null) return null;

            soldier.addTrait("sm_world_tree"); // ActorAsset已包含，保留确保兼容
            soldier.data.name = LocalizedTextManager.getText("sm_creature_worldtree_soldier");
            _activeCreatures.Add(soldier.id);
            return soldier;
        }

        /// <summary>清理死亡生物</summary>
        private static void CleanupDead()
        {
            if (_activeCreatures.Count == 0) return;
            var toRemove = new List<long>();
            foreach (var id in _activeCreatures)
            {
                Actor a = World.world.units.get(id);
                if (a == null || !a.isAlive()) toRemove.Add(id);
            }
            foreach (var id in toRemove) _activeCreatures.Remove(id);
        }

        /// <summary>统计某类型生物数量</summary>
        private static int CountCreatureType(string markerTrait)
        {
            int count = 0;
            foreach (var id in _activeCreatures)
            {
                Actor a = World.world.units.get(id);
                if (a != null && a.isAlive() && a.hasTrait(markerTrait)) count++;
            }
            return count;
        }

        /// <summary>重置（新世界加载时）</summary>
        public static void Reset()
        {
            _activeCreatures.Clear();
            _nextVoidDragonCheck = 0f;
            _nextStarBeastCheck = 0f;
        }
    }
}
