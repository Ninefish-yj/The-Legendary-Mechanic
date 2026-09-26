using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 次级维度系统（原著 ch1071/ch1072）。
    /// 次级维度空间由多种位面组成，物理规则与主宇宙不同。
    /// - 虚空维度：念力系，虚空能量，精神攻击强化
    /// - 冥土维度：异能系，生死能量，生命/死亡强化
    /// - 秘法维度：魔法系，魔力充盈，法术强化
    /// - 机库维度：机械系，机械空间，制造/机械强化
    /// A阶以上单位可进入，获得限时buff，过期自动移除。
    /// </summary>
    public static class SuperMechDimension
    {
        public class DimensionDef
        {
            public string id;
            public string name;
            public string desc;
            public string classTrait;  // 对应系
            public System.Action<BaseStats> applyBuff;
            public System.Action<BaseStats> removeBuff;  // 反向移除buff
            public int minRankIndex = 8;  // A阶（index 8）
        }

        public static readonly List<DimensionDef> Dimensions = new List<DimensionDef>
        {
            new DimensionDef
            {
                id = "void", name = "虚空维度", desc = "混沌迷蒙的虚空能量空间，精神攻击+30%，闪避+20%",
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
                id = "underworld", name = "冥土维度", desc = "生死能量交织的维度，生命+40%，伤害+25%",
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
                id = "arcane", name = "秘法维度", desc = "魔力充盈的法师塔维度，智力+20，法力+50，全属性+5",
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
                id = "hangar", name = "机库维度", desc = "机械师的虚拟机库空间，制造速度+50%，机械威力+30%",
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
                id = "battle", name = "战界维度", desc = "武道系的战斗空间，攻速+30%，暴击+15%，伤害+20%",
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
            new DimensionDef
            {
                id = "infostate", name = "信息态维度", desc = "高维信息态空间（ch1211），全属性+10%，穿甲+15%，免疫控制",
                classTrait = null, // 信息态维度对所有系开放，但需要第六圣所解锁
                applyBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.1f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * 1.1f;
                    s["intelligence"] = (s["intelligence"]) + 15f;
                    s["armor_penetration"] = (s["armor_penetration"]) + 0.15f;
                },
                removeBuff = s => {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_damage"], 1f)) s["multiplier_damage"] = 0f;
                    s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) / 1.1f;
                    if (Mathf.Approximately(s["multiplier_health"], 1f)) s["multiplier_health"] = 0f;
                    s["intelligence"] = (s["intelligence"]) - 15f;
                    s["armor_penetration"] = (s["armor_penetration"]) - 0.15f;
                }
            },
        };

        // 单位当前所在维度（null=不在维度中）
        private static readonly Dictionary<long, string> _activeDimension = new Dictionary<long, string>();
        // buff结束时间戳（单位id → DateTime.Ticks）
        private static readonly Dictionary<long, long> _buffEndTime = new Dictionary<long, long>();
        // 维度进入冷却（单位id → 结束时间戳）
        private static readonly Dictionary<long, long> _cooldown = new Dictionary<long, long>();
        public const float BuffDurationSeconds = 120f;  // buff持续120秒（2分钟）
        public const float CooldownSeconds = 300f;      // 冷却300秒（5分钟）

        /// <summary>获取维度定义。</summary>
        public static DimensionDef GetDef(string id)
        {
            foreach (var d in Dimensions) if (d.id == id) return d;
            return null;
        }

        /// <summary>获取单位当前所在维度名。</summary>
        public static string GetActiveDimension(Actor a)
        {
            if (a == null) return null;
            if (_activeDimension.TryGetValue(a.id, out string dim)) return dim;
            return null;
        }

        /// <summary>单位是否可以进入指定维度。</summary>
        public static bool CanEnter(Actor a, DimensionDef dim)
        {
            if (a == null || dim == null) return false;
            // 已经在维度中，不能重复进入（防止buff叠加）
            if (_activeDimension.ContainsKey(a.id)) return false;
            if (dim.classTrait != null && !a.hasTrait(dim.classTrait)) return false;
            if (SuperMechAdvancement.GetRankIndex(a) < dim.minRankIndex) return false;
            if (_cooldown.TryGetValue(a.id, out long cdEnd))
            {
                long now = System.DateTime.Now.Ticks;
                if (now < cdEnd) return false;
            }
            return true;
        }

        /// <summary>进入维度，获得限时buff。</summary>
        public static bool Enter(Actor a, DimensionDef dim)
        {
            if (!CanEnter(a, dim)) return false;
            var stats = SuperMechStats.Of(a);
            if (stats != null) dim.applyBuff(stats);
            long now = System.DateTime.Now.Ticks;
            _activeDimension[a.id] = dim.id;
            _buffEndTime[a.id] = now + System.TimeSpan.FromSeconds(BuffDurationSeconds).Ticks;
            _cooldown[a.id] = now + System.TimeSpan.FromSeconds(CooldownSeconds).Ticks;
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 进入{dim.name}，获得限时强化（{BuffDurationSeconds}秒）");
            return true;
        }

        /// <summary>获取单位可进入的维度列表。</summary>
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

        /// <summary>定期检查buff过期，过期后自动移除。</summary>
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
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师] {a.name} 的{dim?.name ?? "维度"}buff已过期");
                }
                _activeDimension.Remove(id);
                _buffEndTime.Remove(id);
            }
        }

        /// <summary>清空数据。</summary>
        public static void Clear()
        {
            _activeDimension.Clear();
            _buffEndTime.Clear();
            _cooldown.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_activeDimension, alive);
            removed += SuperMechCleanup.CleanDict(_buffEndTime, alive);
            removed += SuperMechCleanup.CleanDict(_cooldown, alive);
            return removed;
        }
    }
}
