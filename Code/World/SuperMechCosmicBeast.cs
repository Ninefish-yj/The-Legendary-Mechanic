using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.69.0 宇宙异兽体系（原著向）
    /// 原著依据：
    ///  1. 特殊生物：依靠天赋属性获得强大力量、不成为五系超能者、处于超能体系评级标准之外
    ///     （如虚空生物、异化病原体）——异兽即此类评级标准之外的宇宙威胁；
    ///  2. 宇宙生命（虚空逐星者等）：不借助载具在宇宙中游弋的强大生物；
    ///  3. 异兽/宇宙深空威胁随世界发展愈演愈烈。
    /// 真实系统适配：异兽周期性地从世界边缘（虚空）入侵，向文明聚居区移动；
    /// 击杀异兽获得潜能收益；异兽带专属特质，挂接战斗补丁获得凶性伤害；
    /// 异兽不加入任何势力与王国（评级标准之外，不影响文明等级计算）。
    /// </summary>
    public static class SuperMechCosmicBeast
    {
        public class BeastData
        {
            public long actorId;
            public int strength = 1;     // 强度档位（随击杀数成长）
            public float lastX, lastY;   // 上次位置（用于死亡结算定位）
            public int moveCooldown = 0; // 移动重定向冷却
        }

        public class CosmicBeastSaveData
        {
            public int killedCount = 0;
            public int nextSpawnInTicks = 500;
        }

        private static readonly Dictionary<long, BeastData> _beasts = new Dictionary<long, BeastData>();
        private static int _killedCount = 0;
        private static int _nextSpawnInTicks = (int)SuperMechConfig.CosmicBeastInterval;

        public const string BeastTrait = "sm_cosmic_beast";
        public const float BeastDamageMult = 3f; // 异兽凶性：伤害×3（挂接战斗补丁）

        public static int KilledCount => _killedCount;
        public static int ActiveCount => _beasts.Count;
        public static int NextSpawnInTicks => _nextSpawnInTicks;

        /// <summary>每Tick更新：存活检查/移动/波次生成</summary>
        public static void TickCosmicBeasts(float delta)
        {
            if (!SuperMechConfig.CosmicBeastEnabled) return;
            if (World.world == null || World.world.units == null) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            // 存活检查与击杀结算
            var dead = new List<BeastData>();
            foreach (var kv in _beasts)
            {
                var b = kv.Value;
                var a = FindActorById(b.actorId);
                if (a == null || !a.isAlive())
                {
                    dead.Add(b);
                    // 击杀结算：附近最强单位获得潜能（异兽浑身是宝）
                    AwardKillReward(b);
                    _killedCount++;
                    SuperMechEventBus.Publish("CosmicBeastSlain", new CosmicBeastEvent { strength = b.strength, total = _killedCount });
                    Debug.Log($"[超神机械师] 宇宙异兽被击杀！世界击杀总数 {_killedCount}");
                }
            }
            foreach (var b in dead) _beasts.Remove(b.actorId);

            // 波次倒计时与生成
            _nextSpawnInTicks--;
            if (_nextSpawnInTicks <= 0)
            {
                SpawnBeast();
                _nextSpawnInTicks = GetNextInterval();
            }

            // 存活异兽向最近文明单位移动
            foreach (var kv in _beasts)
            {
                var b = kv.Value;
                var a = FindActorById(b.actorId);
                if (a == null || !a.isAlive() || a.current_tile == null) continue;
                b.lastX = a.current_tile.x;
                b.lastY = a.current_tile.y;
                b.moveCooldown--;
                if (b.moveCooldown > 0) continue;
                b.moveCooldown = 8;
                var target = FindNearestCivilizedUnit(a, units);
                if (target != null && target.current_tile != null)
                    a.moveTo(target.current_tile);
            }
        }

        private static void SpawnBeast()
        {
            var tile = FindEdgeTile();
            if (tile == null) return;
            // 视觉占位：复用已验证可用的原版物种；异兽身份由特质 sm_cosmic_beast 与高生命体现
            // （原著：特殊生物在评级标准之外，靠天赋属性获得强大力量）
            string species = "human";

            Actor beast = World.world.units.spawnNewUnit(species, tile, false, true, 6f, null, false, true);
            if (beast == null) return;
            beast.name = "宇宙异兽";
            if (!beast.hasTrait(BeastTrait)) beast.addTrait(BeastTrait);

            // 强度成长：基础+随击杀数成长（原著：宇宙深空威胁愈演愈烈）
            int strength = 1 + _killedCount / 3;
            int bonus = 500 + strength * 300;
            beast.data.health += bonus;
            beast.data.health = Mathf.Max(beast.data.health, 1000);

            _beasts[beast.id] = new BeastData
            {
                actorId = beast.id,
                strength = strength,
                lastX = beast.current_tile != null ? beast.current_tile.x : tile.x,
                lastY = beast.current_tile != null ? beast.current_tile.y : tile.y
            };
            SuperMechEventBus.Publish("CosmicBeastSpawned", new CosmicBeastEvent { strength = strength });
            Debug.Log($"[超神机械师] 宇宙异兽从虚空边缘入侵！强度档位 {strength}，生命 {beast.data.health}");
        }

        /// <summary>击杀奖励：异兽死亡位置附近（12格）最强单位获得潜能（原著：异兽浑身是宝）</summary>
        private static void AwardKillReward(BeastData b)
        {
            if (World.world == null || World.world.units == null) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            Actor best = null;
            float bestScore = -1f;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.current_tile == null) continue;
                if (a.hasTrait(BeastTrait)) continue;
                float dist = Mathf.Abs(a.current_tile.x - b.lastX) + Mathf.Abs(a.current_tile.y - b.lastY);
                if (dist > 12f) continue;
                float score = dist * 0.1f + SuperMechAdvancement.GetExactRankIndex(a);
                if (score > bestScore) { bestScore = score; best = a; }
            }
            if (best != null)
            {
                SuperMechPotential.AddPotential(best, 10 + b.strength * 5);
                // v0.70.x 异兽素材强化：击杀者获得攻击强化Buff（原著：异兽浑身是宝）
                SuperMechCombatEnhance.ApplyBuff(best, SuperMechCombatEnhance.BuffAtk,
                    SuperMechCombatEnhance.BuffDurationTicks, SuperMechCombatEnhance.BuffAtkValue);
                Debug.Log($"[超神机械师] {best.name} 获得异兽素材潜能奖励 +{10 + b.strength * 5} 与攻击强化");
            }
        }

        private static Actor FindNearestCivilizedUnit(Actor beast, List<Actor> units)
        {
            Actor best = null;
            float bestDist = float.MaxValue;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.id == beast.id || a.current_tile == null) continue;
                if (a.kingdom == null) continue; // 只袭击文明聚居区
                float dist = Mathf.Abs(a.current_tile.x - beast.current_tile.x) + Mathf.Abs(a.current_tile.y - beast.current_tile.y);
                if (dist < bestDist) { bestDist = dist; best = a; }
            }
            return best;
        }

        private static WorldTile FindEdgeTile()
        {
            if (World.world == null) return null;
            int w = MapBox.width, h = MapBox.height;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Random.value < 0.5f ? Random.Range(0, Mathf.Max(1, w / 8)) : Random.Range(w - w / 8, w);
                int y = Random.Range(0, h);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                WorldTile t = World.world.GetTile(x, y);
                if (t != null && t.Type != TileLibrary.deep_ocean && t.Type != TileLibrary.close_ocean && t.Type != TileLibrary.mountains)
                    return t;
            }
            return null;
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units)
                if (a != null && a.id == id) return a;
            return null;
        }

        private static int GetNextInterval()
        {
            // 击杀越多，异兽入侵越频繁（原著：威胁愈演愈烈）；基准间隔由配置 CosmicBeastInterval 控制
            return Mathf.Max(150, (int)SuperMechConfig.CosmicBeastInterval - _killedCount * 20);
        }

        public static CosmicBeastSaveData Save()
        {
            return new CosmicBeastSaveData { killedCount = _killedCount, nextSpawnInTicks = _nextSpawnInTicks };
        }

        public static void Load(CosmicBeastSaveData data)
        {
            _beasts.Clear();
            if (data == null) return;
            _killedCount = data.killedCount;
            _nextSpawnInTicks = data.nextSpawnInTicks;
        }

        public static void Clear()
        {
            _beasts.Clear();
            _killedCount = 0;
            _nextSpawnInTicks = 500;
        }
    }

    /// <summary>宇宙异兽事件（事件总线）</summary>
    public class CosmicBeastEvent
    {
        public int strength;
        public int total;
    }
}
