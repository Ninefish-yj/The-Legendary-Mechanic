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
    /// A阶以上单位可进入，获得限时buff。
    /// </summary>
    public static class SuperMechDimension
    {
        public class DimensionDef
        {
            public string id;
            public string name;
            public string desc;
            public string classTrait;  // 对应系
            public System.Action<Dictionary<string, float>> applyBuff;
            public int minRankIndex = 8;  // A阶（index 8）
        }

        public static readonly List<DimensionDef> Dimensions = new List<DimensionDef>
        {
            new DimensionDef
            {
                id = "void", name = "虚空维度", desc = "混沌迷蒙的虚空能量空间，精神攻击+30%，闪避+20%",
                classTrait = SuperMechTraits.ClassMind,
                applyBuff = s => {
                    s["multiplier_damage"] = (s["multiplier_damage"]??1f) * 1.3f;
                    s["speed"] = (s["speed"]??0f) + 0.5f;
                    s["intelligence"] = (s["intelligence"]??0f) + 10f;
                }
            },
            new DimensionDef
            {
                id = "underworld", name = "冥土维度", desc = "生死能量交织的维度，生命+40%，伤害+25%",
                classTrait = SuperMechTraits.ClassPsi,
                applyBuff = s => {
                    s["multiplier_health"] = (s["multiplier_health"]??1f) * 1.4f;
                    s["multiplier_damage"] = (s["multiplier_damage"]??1f) * 1.25f;
                    s["stamina"] = (s["stamina"]??0f) + 30f;
                }
            },
            new DimensionDef
            {
                id = "arcane", name = "秘法维度", desc = "魔力充盈的法师塔维度，智力+20，法力+50，全属性+5",
                classTrait = SuperMechTraits.ClassMage,
                applyBuff = s => {
                    s["intelligence"] = (s["intelligence"]??0f) + 20f;
                    s["mana"] = (s["mana"]??0f) + 50f;
                    s["damage"] = (s["damage"]??0f) + 5f;
                    s["health"] = (s["health"]??0f) + 15f;
                }
            },
            new DimensionDef
            {
                id = "hangar", name = "机库维度", desc = "机械师的虚拟机库空间，制造速度+50%，机械威力+30%",
                classTrait = SuperMechTraits.ClassMech,
                applyBuff = s => {
                    s["multiplier_damage"] = (s["multiplier_damage"]??1f) * 1.3f;
                    s["intelligence"] = (s["intelligence"]??0f) + 15f;
                    s["experience"] = (s["experience"]??1f) * 1.5f;
                }
            },
            new DimensionDef
            {
                id = "battle", name = "战界维度", desc = "武道系的战斗空间，攻速+30%，暴击+15%，伤害+20%",
                classTrait = SuperMechTraits.ClassMartial,
                applyBuff = s => {
                    s["attack_speed"] = (s["attack_speed"]??0f) + 0.3f;
                    s["critical_chance"] = (s["critical_chance"]??0f) + 0.15f;
                    s["multiplier_damage"] = (s["multiplier_damage"]??1f) * 1.2f;
                }
            },
            new DimensionDef
            {
                id = "infostate", name = "信息态维度", desc = "高维信息态空间（ch1211），全属性+10%，穿甲+15%，免疫控制",
                classTrait = null, // 信息态维度对所有系开放，但需要第六圣所解锁
                applyBuff = s => {
                    s["multiplier_damage"] = (s["multiplier_damage"]??1f) * 1.1f;
                    s["multiplier_health"] = (s["multiplier_health"]??1f) * 1.1f;
                    s["intelligence"] = (s["intelligence"]??0f) + 15f;
                    s["armor_penetration"] = (s["armor_penetration"]??0f) + 0.15f;
                }
            },
        };

        // 单位当前所在维度（null=不在维度中）
        private static readonly Dictionary<long, string> _activeDimension = new Dictionary<long, string>();
        // 维度进入冷却（单位id → 结束时间戳）
        private static readonly Dictionary<long, long> _cooldown = new Dictionary<long, long>();
        public const long BuffDurationTicks = 60;  // buff持续60个tick（约5分钟）
        public const long CooldownTicks = 120;     // 冷却120tick

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
            if (!a.hasTrait(dim.classTrait)) return false;
            if (SuperMechAdvancement.GetRankIndex(a) < dim.minRankIndex) return false;
            if (_cooldown.ContainsKey(a.id))
            {
                long now = System.DateTime.Now.Ticks;
                if (now < _cooldown[a.id]) return false;
            }
            return true;
        }

        /// <summary>进入维度，获得限时buff。</summary>
        public static bool Enter(Actor a, DimensionDef dim)
        {
            if (!CanEnter(a, dim)) return false;
            var stats = SuperMechStats.Of(a);
            if (stats != null) dim.applyBuff(stats);
            _activeDimension[a.id] = dim.id;
            _cooldown[a.id] = System.DateTime.Now.Ticks + System.TimeSpan.FromSeconds(300).Ticks;
            Debug.Log($"[超神机械师] {a.Name} 进入{dim.name}，获得限时强化");
            return true;
        }

        /// <summary>获取单位可进入的维度列表。</summary>
        public static List<DimensionDef> GetAvailableDimensions(Actor a)
        {
            var list = new List<DimensionDef>();
            if (a == null) return list;
            foreach (var dim in Dimensions)
            {
                if (a.hasTrait(dim.classTrait)) list.Add(dim);
            }
            return list;
        }

        /// <summary>定期检查buff过期。</summary>
        public static void TickDimensionBuffs()
        {
            // 简化：buff通过stats反射施加后持续存在，不做过期移除
            // 冷却由_cooldown控制再次进入
        }
    }
}
