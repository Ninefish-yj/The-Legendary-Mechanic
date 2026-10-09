using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>次级维度世界系统（原著第1071章：次级维度世界）
    /// 原著设定：无数泡泡堆叠的平行空间，空间壁比主宇宙薄弱，可频繁跳跃穿梭
    /// 天然维度：虚空维度、冥土维度、邪能维度
    /// 人造维度：机械维度（兵营/兵工厂）、奥术维度（法师塔存储）
    /// </summary>
    public static class SuperMechDimension
    {
        public enum DimensionType { Natural, Artificial, Special }

        public class DimensionDef
        {
            public string id;
            public string name;
            public string desc;
            public string lore;           // 原著世界观描述
            public string anchor;         // 维度锚点坐标（原著：锚点坐标才是领航指针）
            public DimensionType type;    // 天然/人造/特殊
            public string classTrait;     // 进入所需职业（null=任意）
            public System.Action<BaseStats> applyBuff;
            public System.Action<BaseStats> removeBuff;
            public int minRankIndex = 8;  // 最低进入阶位（原著：超A级才能自由穿梭）
        }

        public static readonly List<DimensionDef> Dimensions = new List<DimensionDef>
        {
            // === 天然维度 ===
            new DimensionDef
            {
                id = "void", name = "sm_dimension_void", desc = "sm_dimension_void_desc",
                lore = "sm_dimension_void_lore", anchor = "VOID-001",
                type = DimensionType.Natural,
                classTrait = SuperMechTraits.ClassMind,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.3f;
                    s["speed"] = (s["speed"]) + 0.5f;
                    s["intelligence"] = (s["intelligence"]) + 10f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.3f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["speed"] = (s["speed"]) - 0.5f;
                    s["intelligence"] = (s["intelligence"]) - 10f;
                }
            },
            new DimensionDef
            {
                id = "underworld", name = "sm_dimension_underworld", desc = "sm_dimension_underworld_desc",
                lore = "sm_dimension_underworld_lore", anchor = "UNDER-001",
                type = DimensionType.Natural,
                classTrait = SuperMechTraits.ClassPsi,
                applyBuff = s => {
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.4f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.25f;
                    s["stamina"] = (s["stamina"]) + 30f;
                },
                removeBuff = s => {
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / 1.4f;
                    if (Mathf.Approximately(s["multiplier_health"], 1f)) s["multiplier_health"] = 0f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.25f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["stamina"] = (s["stamina"]) - 30f;
                }
            },
            new DimensionDef
            {
                id = "fel", name = "sm_dimension_fel", desc = "sm_dimension_fel_desc",
                lore = "sm_dimension_fel_lore", anchor = "FEL-001",
                type = DimensionType.Natural,
                classTrait = null,
                minRankIndex = 10,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.35f;
                    s["attack_speed"] = (s["attack_speed"]) + 0.2f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.35f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["attack_speed"] = (s["attack_speed"]) - 0.2f;
                }
            },
            // === 人造维度 ===
            new DimensionDef
            {
                id = "hangar", name = "sm_dimension_hangar", desc = "sm_dimension_hangar_desc",
                lore = "sm_dimension_hangar_lore", anchor = "MECH-001",
                type = DimensionType.Artificial,
                classTrait = SuperMechTraits.ClassMech,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.3f;
                    s["intelligence"] = (s["intelligence"]) + 15f;
                    s["experience"] = ((s["experience"] == 0f ? 1f : s["experience"])) * 1.5f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.3f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["intelligence"] = (s["intelligence"]) - 15f;
                    s["experience"] = ((s["experience"] == 0f ? 1f : s["experience"])) / 1.5f;
                    if (Mathf.Approximately(s["experience"], 1f)) s["experience"] = 0f;
                }
            },
            new DimensionDef
            {
                id = "arcane", name = "sm_dimension_arcane", desc = "sm_dimension_arcane_desc",
                lore = "sm_dimension_arcane_lore", anchor = "ARCANE-001",
                type = DimensionType.Artificial,
                classTrait = SuperMechTraits.ClassMage,
                applyBuff = s => {
                    s["intelligence"] = (s["intelligence"]) + 20f;
                    s["mana"] = (s["mana"]) + 50f;
                    s["damage"] = (s["damage"]) + 5f;
                    s["health"] = (s["health"]) + 15f;
                },
                removeBuff = s => {
                    s["intelligence"] = (s["intelligence"]) - 20f;
                    s["mana"] = (s["mana"]) - 50f;
                    s["damage"] = (s["damage"]) - 5f;
                    s["health"] = (s["health"]) - 15f;
                }
            },
            new DimensionDef
            {
                id = "battle", name = "sm_dimension_battle", desc = "sm_dimension_battle_desc",
                lore = "sm_dimension_battle_lore", anchor = "BATTLE-001",
                type = DimensionType.Artificial,
                classTrait = SuperMechTraits.ClassMartial,
                applyBuff = s => {
                    s["attack_speed"] = (s["attack_speed"]) + 0.3f;
                    s["critical_chance"] = (s["critical_chance"]) + 0.15f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.2f;
                },
                removeBuff = s => {
                    s["attack_speed"] = (s["attack_speed"]) - 0.3f;
                    s["critical_chance"] = (s["critical_chance"]) - 0.15f;
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.2f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                }
            },
            // === 特殊维度 ===
            new DimensionDef
            {
                id = "infostate", name = "sm_dimension_infostate", desc = "sm_dimension_infostate_desc",
                lore = "sm_dimension_infostate_lore", anchor = "INFO-000",
                type = DimensionType.Special,
                classTrait = null,
                minRankIndex = 12,
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.1f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.1f;
                    s["intelligence"] = (s["intelligence"]) + 15f;
                    s["damage"] = (s["damage"]) + 5f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_health"], 1f)) s["multiplier_health"] = 0f;
                    s["intelligence"] = (s["intelligence"]) - 15f;
                    s["damage"] = (s["damage"]) - 5f;
                }
            },
        };

        public static readonly Dictionary<long, string> _activeDimension = new Dictionary<long, string>();
        public static readonly Dictionary<long, long> _buffEndTime = new Dictionary<long, long>();
        public static readonly Dictionary<long, long> _cooldown = new Dictionary<long, long>();
        public const float BuffDurationSeconds = 120f;
        public const float CooldownSeconds = 300f;

        /// <summary>维度跳跃记录（原著：空间壁跳跃，每次承受空间乱流撕扯）</summary>
        public static readonly Dictionary<long, int> _jumpCount = new Dictionary<long, int>();
        public static readonly Dictionary<long, long> _firstJumpTime = new Dictionary<long, long>();
        public const int MaxJumpsBeforeRest = 8;  // 原著：连续跳跃给载具带来极高负荷
        public const float JumpRecoverySeconds = 120f;  // 2分钟后跳跃负荷重置

        public static DimensionDef GetDef(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var d in Dimensions) if (d.id == id) return d;
            return null;
        }

        public static string GetActiveDimension(Actor a)
        {
            if (a == null) return null;
            if (_activeDimension.TryGetValue(a.id, out string dim)) return dim;
            return null;
        }

        public static bool CanEnter(Actor a, DimensionDef dim)
        {
            if (a == null || dim == null) return false;
            if (_activeDimension.ContainsKey(a.id)) return false;
            if (dim.classTrait != null && !a.hasTrait(dim.classTrait)) return false;
            if (SuperMechActorContextRegistry.GetRank(a) < dim.minRankIndex) return false;
            if (_cooldown.TryGetValue(a.id, out long cdEnd))
            {
                long now = System.DateTime.Now.Ticks;
                if (now < cdEnd) return false;
            }
            // 维度跳跃负荷检查（原著：连续跳跃承受空间乱流撕扯，休息后恢复）
            long now = System.DateTime.Now.Ticks;
            if (_jumpCount.TryGetValue(a.id, out int jumps) && jumps >= MaxJumpsBeforeRest)
            {
                // 检查是否超过恢复时间
                if (_firstJumpTime.TryGetValue(a.id, out long firstTime))
                {
                    if (now - firstTime >= System.TimeSpan.FromSeconds(JumpRecoverySeconds).Ticks)
                    {
                        // 恢复：重置跳跃计数
                        _jumpCount[a.id] = 0;
                        _firstJumpTime.Remove(a.id);
                    }
                    else
                    {
                        return false; // 仍在负荷期
                    }
                }
                else
                {
                    _jumpCount[a.id] = 0; // 无记录则重置
                }
            }
            return true;
        }

        public static bool Enter(Actor a, DimensionDef dim)
        {
            if (!CanEnter(a, dim)) return false;
            var stats = SuperMechStats.Of(a);
            if (stats != null) dim.applyBuff(stats);
            long now = System.DateTime.Now.Ticks;
            _activeDimension[a.id] = dim.id;
            _buffEndTime[a.id] = now + System.TimeSpan.FromSeconds(BuffDurationSeconds).Ticks;
            _cooldown[a.id] = now + System.TimeSpan.FromSeconds(CooldownSeconds).Ticks;
            // 记录跳跃次数
            if (_jumpCount.ContainsKey(a.id))
            {
                _jumpCount[a.id]++;
            }
            else
            {
                _jumpCount[a.id] = 1;
                _firstJumpTime[a.id] = now;
            }
            return true;
        }

        public static void Exit(Actor a)
        {
            if (a == null) return;
            if (_activeDimension.TryGetValue(a.id, out string dimId))
            {
                var dim = GetDef(dimId);
                var stats = SuperMechStats.Of(a);
                if (dim != null && stats != null) dim.removeBuff(stats);
                _activeDimension.Remove(a.id);
                _buffEndTime.Remove(a.id);
            }
        }

        public static List<DimensionDef> GetAvailableDimensions(Actor a)
        {
            var list = new List<DimensionDef>();
            if (a == null) return list;
            foreach (var dim in Dimensions)
            {
                if (dim.classTrait == null || a.hasTrait(dim.classTrait)) list.Add(dim);
            }
            return list;
        }

        /// <summary>获取维度类型的本地化名称</summary>
        public static string GetDimensionTypeName(DimensionType type)
        {
            switch (type)
            {
                case DimensionType.Natural: return LocalizedTextManager.getText("sm_dim_type_natural");
                case DimensionType.Artificial: return LocalizedTextManager.getText("sm_dim_type_artificial");
                case DimensionType.Special: return LocalizedTextManager.getText("sm_dim_type_special");
                default: return "?";
            }
        }

        public static void TickDimensionBuffs()
        {
            if (World.world == null || World.world.units == null) return;
            long now = System.DateTime.Now.Ticks;
            var expired = new List<long>();
            foreach (var kv in _buffEndTime)
            {
                if (now >= kv.Value) expired.Add(kv.Key);
            }
            foreach (long id in expired)
            {
                Actor a = null;
                foreach (var u in World.world.units) if (u != null && u.id == id) { a = u; break; }
                if (a != null && _activeDimension.TryGetValue(id, out string dimId))
                {
                    var dim = GetDef(dimId);
                    var stats = SuperMechStats.Of(a);
                    if (dim != null && stats != null) dim.removeBuff(stats);
                }
                _activeDimension.Remove(id);
                _buffEndTime.Remove(id);
            }
        }

        /// <summary>重置跳跃计数（休息后恢复）</summary>
        public static void ResetJumpCount(Actor a)
        {
            if (a != null) _jumpCount.Remove(a.id);
        }

        public static void Clear()
        {
            _activeDimension.Clear();
            _buffEndTime.Clear();
            _cooldown.Clear();
            _jumpCount.Clear();
            _firstJumpTime.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_activeDimension, alive);
            removed += SuperMechCleanup.CleanDict(_buffEndTime, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            removed += SuperMechCleanup.CleanDict(_jumpCount, alive);
            removed += SuperMechCleanup.CleanDict(_firstJumpTime, alive);
            return removed;
        }
    }
}
