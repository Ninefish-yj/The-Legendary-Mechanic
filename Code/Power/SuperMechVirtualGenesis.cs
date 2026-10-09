using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>虚拟创世系统（原著：韩萧融合世界树后的标志性能力）
    /// 原著设定：韩萧融合世界树后获得【虚拟创世（伪）】，超神级后去"伪"
    /// 可以创造虚拟空间，空间内修炼加速、存储单位、模拟文明
    /// </summary>
    public static class SuperMechVirtualGenesis
    {
        public enum GenesisLevel { None, Pseudo, True }

        public class GenesisState
        {
            public GenesisLevel level = GenesisLevel.None;
            public int spaceLevel = 0;           // 虚拟空间等级（1-10）
            public float energy = 0f;            // 创世能量
            public long createdAt = 0;           // 创造时间
            public int storedUnits = 0;          // 存储单位数
        }

        private static readonly Dictionary<long, GenesisState> _states = new Dictionary<long, GenesisState>();

        public const int MinRankForPseudo = 10;   // S阶（超A级）可获得虚拟创世（伪）
        public const int MinRankForTrue = 13;     // X阶（超神级）去伪成真
        public const float EnergyPerSpaceLevel = 100f;

        /// <summary>检查单位是否拥有虚拟创世能力</summary>
        public static bool HasGenesis(Actor a)
        {
            if (a == null) return false;
            return _states.ContainsKey(a.id) && _states[a.id].level > GenesisLevel.None;
        }

        /// <summary>获取虚拟创世状态</summary>
        public static GenesisState GetState(Actor a)
        {
            if (a == null) return null;
            if (_states.TryGetValue(a.id, out var s)) return s;
            return null;
        }

        /// <summary>尝试觉醒虚拟创世（伪）——韩萧融合世界树后获得</summary>
        public static bool TryAwakenPseudo(Actor a)
        {
            if (a == null || !a.isAlive()) return false;
            // 只有降临者（韩萧化身）可以获得
            if (!SuperMechAwakened.IsAwakened(a)) return false;
            if (SuperMechActorContextRegistry.GetRank(a) < MinRankForPseudo) return false;
            if (HasGenesis(a)) return false;

            _states[a.id] = new GenesisState
            {
                level = GenesisLevel.Pseudo,
                spaceLevel = 1,
                energy = 50f,
                createdAt = System.DateTime.Now.Ticks
            };

            Debug.Log($"[超神机械师]【虚拟创世】{a.getName()} 融合世界树，觉醒【虚拟创世（伪）】！");
            return true;
        }

        /// <summary>尝试去伪成真——X阶后虚拟创世进化</summary>
        public static bool TryAscendToTrue(Actor a)
        {
            if (a == null) return false;
            var state = GetState(a);
            if (state == null || state.level != GenesisLevel.Pseudo) return false;
            if (SuperMechActorContextRegistry.GetRank(a) < MinRankForTrue) return false;

            state.level = GenesisLevel.True;
            state.spaceLevel = 10;
            state.energy = 1000f;

            Debug.Log($"[超神机械师]【虚拟创世】{a.getName()} 达到超神级，【虚拟创世（伪）】去伪成真！");
            return true;
        }

        /// <summary>获取虚拟空间内的修炼加成</summary>
        public static float GetCultivationBonus(Actor a)
        {
            var state = GetState(a);
            if (state == null) return 1f;
            // 虚拟创世（伪）：1.5倍，每级+0.1；虚拟创世（真）：3倍
            if (state.level == GenesisLevel.True) return 3f;
            return 1.5f + (state.spaceLevel - 1) * 0.1f;
        }

        /// <summary>获取虚拟空间内的经验加成</summary>
        public static float GetExpBonus(Actor a)
        {
            var state = GetState(a);
            if (state == null) return 1f;
            if (state.level == GenesisLevel.True) return 5f;
            return 2f + (state.spaceLevel - 1) * 0.2f;
        }

        /// <summary>消耗创世能量升级虚拟空间</summary>
        public static bool UpgradeSpace(Actor a)
        {
            var state = GetState(a);
            if (state == null || state.level == GenesisLevel.None) return false;
            if (state.spaceLevel >= 10) return false;
            float cost = EnergyPerSpaceLevel * state.spaceLevel;
            if (state.energy < cost) return false;

            state.energy -= cost;
            state.spaceLevel++;
            return true;
        }

        /// <summary>增加创世能量（修炼/战斗获得）</summary>
        public static void AddEnergy(Actor a, float amount)
        {
            var state = GetState(a);
            if (state == null) return;
            state.energy += amount;
            if (state.energy > 10000f) state.energy = 10000f;
        }

        /// <summary>Tick：自动恢复创世能量，检查去伪条件</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.units == null) return;

            foreach (var kv in _states)
            {
                Actor a = null;
                foreach (var u in World.world.units) if (u != null && u.id == kv.Key) { a = u; break; }
                if (a == null || !a.isAlive()) continue;

                var state = kv.Value;
                // 缓慢恢复创世能量
                state.energy += 0.1f * state.spaceLevel;
                if (state.energy > 10000f) state.energy = 10000f;

                // 检查去伪条件
                if (state.level == GenesisLevel.Pseudo &&
                    SuperMechActorContextRegistry.GetRank(a) >= MinRankForTrue)
                {
                    TryAscendToTrue(a);
                }
            }
        }

        /// <summary>世界树融合事件：所有降临者有机会觉醒虚拟创世</summary>
        public static void OnWorldTreeUnion()
        {
            if (World.world == null || World.world.units == null) return;
            foreach (Actor a in World.world.units)
            {
                if (a == null || !a.isAlive()) continue;
                if (SuperMechAwakened.IsAwakened(a) &&
                    SuperMechActorContextRegistry.GetRank(a) >= MinRankForPseudo)
                {
                    TryAwakenPseudo(a);
                }
            }
        }

        public static void Clear()
        {
            _states.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_states, alive);
        }

        // === 存档 ===
        [System.Serializable]
        public class GenesisSaveData
        {
            public long id;
            public int level;
            public int spaceLevel;
            public float energy;
            public long createdAt;
            public int storedUnits;
        }

        public static List<GenesisSaveData> Save()
        {
            var list = new List<GenesisSaveData>();
            foreach (var kv in _states)
            {
                list.Add(new GenesisSaveData
                {
                    id = kv.Key,
                    level = (int)kv.Value.level,
                    spaceLevel = kv.Value.spaceLevel,
                    energy = kv.Value.energy,
                    createdAt = kv.Value.createdAt,
                    storedUnits = kv.Value.storedUnits
                });
            }
            return list;
        }

        public static void Load(List<GenesisSaveData> data)
        {
            _states.Clear();
            if (data == null) return;
            foreach (var d in data)
            {
                _states[d.id] = new GenesisState
                {
                    level = (GenesisLevel)d.level,
                    spaceLevel = d.spaceLevel,
                    energy = d.energy,
                    createdAt = d.createdAt,
                    storedUnits = d.storedUnits
                };
            }
        }
    }
}
